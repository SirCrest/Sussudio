using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Video+audio decoder for Flashback .ts files.
/// Decodes HEVC/H.264 video via D3D11VA (GPU-direct) or software fallback to NV12/P010,
/// and AAC audio to f32le interleaved stereo 48kHz.
/// This type is NOT thread-safe — all calls must come from the playback controller's thread.
/// </summary>
internal sealed unsafe partial class FlashbackDecoder : IDisposable
{
    private const int OutputAudioSampleRate = 48000;
    private const int OutputAudioChannels = 2;
    private const int VideoFrameBufferCount = 2;
    private const int MaxSupportedInputStreams = 64;
    private const int MaxDecodedVideoDimension = 8192;
    private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;
    private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;
    private const int MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024;
    private const int MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000;

    private AVFormatContext* _formatCtx;
    private AVCodecContext* _videoCodecCtx;
    private AVCodecContext* _audioCodecCtx;
    private SwrContext* _swrCtx;
    private AVFrame* _videoFrame;
    private AVFrame* _audioFrame;
    private AVPacket* _packet;

    private int _videoStreamIndex = -1;
    private int _audioStreamIndex = -1;
    private AVRational _videoTimeBase;
    private AVRational _audioTimeBase;

    // Double-buffered video output: alternate between two managed buffers
    // so the renderer can consume one while we decode the next.
    // Only used for software decode path.
    private byte[]?[] _videoFrameBuffers = new byte[VideoFrameBufferCount][];
    private GCHandle[] _videoFrameHandles = new GCHandle[VideoFrameBufferCount];
    private int _currentVideoBufferIndex;

    // Pending frame stash: SeekTo() decodes forward to the target frame but must
    // not discard it — stash it so the next TryDecodeNextVideoFrame() returns it.
    private DecodedVideoFrame _pendingVideoFrame;
    private bool _hasPendingVideoFrame;
    private bool _suppressRecoverableSeekLogsForNextVideoFrame;

    // SeekTo forward-decode cap observability
    private long _seekToCapHits;
    private bool _lastSeekHitForwardDecodeCap;

    private bool _isOpen;
    private bool _disposed;
    private bool _initialized;
    private string? _currentFilePath;
    private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;

    /// <summary>
    /// When set, audio packets encountered during video reads are decoded and
    /// delivered here immediately — no stashing, no separate audio loop.
    /// This keeps audio naturally interleaved with video, like any video player.
    /// </summary>
    public Action<DecodedAudioChunk>? AudioChunkCallback { get; set; }

    // Video info (populated after OpenFile)
    private int _videoWidth;
    private int _videoHeight;
    private bool _isHdr;
    private double _frameRate;
    private double _metadataFrameRate;
    private int _ptsCalibrationCount;
    private long _firstCalibrationPtsTicks;
    private long _lastCalibrationPtsTicks;
    private const int PtsCalibrationFrames = 10;
    private TimeSpan _currentPosition;
    private AVPixelFormat _decodedPixelFormat;
    private bool _needsConvert;

    // D3D11VA hardware decode state (persistent across file opens)
    private AVBufferRef* _d3d11HwDeviceCtx;
    private bool _isD3D11HwAccelerated;
    private IntPtr _d3dDevicePtr;
    private IntPtr _d3dContextPtr;

    public bool IsOpen => _isOpen;
    public int VideoWidth => _videoWidth;
    public int VideoHeight => _videoHeight;
    public bool IsHdr => _isHdr;
    public double FrameRate => _frameRate;
    public TimeSpan CurrentPosition => _currentPosition;
    public bool IsD3D11HwAccelerated => _isD3D11HwAccelerated;
    public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;

    public readonly record struct PlaybackDecodePhaseTimings(
        double ReceiveMs,
        double FeedMs,
        double ReadMs,
        double SendMs,
        double AudioMs,
        double ConvertMs);

    /// <summary>
    /// Total number of times SeekTo() hit the forward-decode cap AND the best frame
    /// was more than one frame interval behind the seek target (i.e., a missed seek).
    /// </summary>
    public long SeekToForwardDecodeCapHits => Interlocked.Read(ref _seekToCapHits);

    /// <summary>
    /// True if the most recent SeekTo() call hit the forward-decode cap and the
    /// returned frame's PTS was more than one frame interval behind the target.
    /// Reset to false on each SeekTo() entry.
    /// </summary>
    public bool LastSeekHitForwardDecodeCap => _lastSeekHitForwardDecodeCap;

    /// <summary>
    /// Initializes the decoder with D3D11 device pointers for GPU-direct decode.
    /// Must be called before <see cref="OpenFile"/>.
    /// </summary>
    public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)
    {
        ThrowIfDisposed();

        if (_initialized)
        {
            return;
        }

        LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

        _d3dDevicePtr = d3dDevicePtr;
        _d3dContextPtr = d3dContextPtr;

        // Create persistent D3D11VA hw device context (reused across all file opens)
        if (d3dDevicePtr != IntPtr.Zero && d3dContextPtr != IntPtr.Zero)
        {
            try
            {
                var hwDeviceCtx = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
                if (hwDeviceCtx != null)
                {
                    var hwCtx = (AVHWDeviceContext*)hwDeviceCtx->data;
                    var d3d11vaCtx = (AVD3D11VADeviceContext*)hwCtx->hwctx;
                    d3d11vaCtx->device = (FFmpeg.AutoGen.ID3D11Device*)d3dDevicePtr;
                    d3d11vaCtx->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)d3dContextPtr;

                    var initResult = ffmpeg.av_hwdevice_ctx_init(hwDeviceCtx);
                    if (initResult >= 0)
                    {
                        _d3d11HwDeviceCtx = hwDeviceCtx;
                        Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=true device=0x{d3dDevicePtr:X}");
                    }
                    else
                    {
                        ffmpeg.av_buffer_unref(&hwDeviceCtx);
                        Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=false reason=init_fail code={initResult}");
                    }
                }
                else
                {
                    Logger.Log("FLASHBACK_DECODER_INIT d3d11va=false reason=alloc_fail");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }
        else
        {
            Logger.Log("FLASHBACK_DECODER_INIT d3d11va=false reason=no_device");
        }

        _initialized = true;
    }

    /// <summary>
    /// Opens a .ts or .mp4 file for decoding.
    /// </summary>
    public void OpenFile(string filePath)
    {
        ThrowIfNotInitialized();
        ThrowIfDisposed();

        if (_isOpen)
        {
            CloseFile();
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        _currentFilePath = filePath;

        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                Logger.Log($"FLASHBACK_DECODER_OPEN_FAIL reason=file_not_found path='{filePath}'");
                throw new System.IO.FileNotFoundException($"File not found: '{filePath}'", filePath);
            }

            // Open input
            AVFormatContext* formatCtx = null;
            ThrowIfError(
                ffmpeg.avformat_open_input(&formatCtx, filePath, null, null),
                "avformat_open_input");
            _formatCtx = formatCtx;
            _formatCtx->flags |= ffmpeg.AVFMT_FLAG_GENPTS;

            // Rotated MPEG-TS segments can start mid-stream before the next IDR/SPS.
            // Use the same larger probe window as the exporter so playback can recover
            // dimensions and extradata from high-bitrate 4K120 flashback segments.
            _formatCtx->probesize = MaxMpegTsProbeSizeBytes;
            _formatCtx->max_analyze_duration = MaxMpegTsAnalyzeDurationUs;

            ThrowIfError(
                ffmpeg.avformat_find_stream_info(_formatCtx, null),
                "avformat_find_stream_info");
            if (!TryGetInputStreamCount(_formatCtx, out var streamCount, out var streamCountFailure))
            {
                throw CreateException(streamCountFailure);
            }

            // Find video stream
            _videoStreamIndex = ffmpeg.av_find_best_stream(
                _formatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            if (!IsValidStreamIndex(_videoStreamIndex, streamCount))
            {
                throw CreateException("No video stream found in file.");
            }

            // Find audio stream (optional)
            _audioStreamIndex = ffmpeg.av_find_best_stream(
                _formatCtx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);
            if (_audioStreamIndex >= 0 && !IsValidStreamIndex(_audioStreamIndex, streamCount))
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_stream_index index={_audioStreamIndex} stream_count={streamCount}; audio disabled");
                _audioStreamIndex = -1;
            }

            // Set up video decoder
            InitializeVideoDecoder();

            // Set up audio decoder (if present)
            if (_audioStreamIndex >= 0)
            {
                InitializeAudioDecoder();
            }

            // Allocate shared packet
            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                throw CreateException("Failed to allocate AVPacket.");
            }

            _currentPosition = TimeSpan.Zero;
            _isOpen = true;

            Logger.Log($"FLASHBACK_DECODER_OPEN path='{filePath}' " +
                       $"video={_videoWidth}x{_videoHeight} fps={_frameRate:F2} hdr={_isHdr} " +
                       $"hw_accel={((_isD3D11HwAccelerated ? "D3D11VA" : "Software"))} " +
                       $"audio={(_audioStreamIndex >= 0 ? "yes" : "no")}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_DECODER_OPEN_WARN path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");
            CloseFileCore();
            throw;
        }
    }

    /// <summary>
    /// Closes the currently open file and frees per-file decoder resources.
    /// The D3D11VA device context is NOT freed (persistent across files).
    /// </summary>
    public void CloseFile()
    {
        if (!_isOpen)
        {
            return;
        }

        var closedPath = _currentFilePath;
        CloseFileCore();
        Logger.Log($"FLASHBACK_DECODER_CLOSE path='{closedPath}'");
    }

    /// <summary>
    /// Seeks to the nearest keyframe at or before <paramref name="target"/>.
    /// Fast seek suitable for scrubbing.
    /// </summary>
    public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)
    {
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();

        var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);
        var result = ffmpeg.av_seek_frame(
            _formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
        cancellationToken.ThrowIfCancellationRequested();

        if (result < 0)
        {
            var streamSeekResult = result;
            var timestampUs = ToAvTimeBaseTimestamp(target);
            result = ffmpeg.av_seek_frame(
                _formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);
            cancellationToken.ThrowIfCancellationRequested();

            if (result < 0)
            {
                Logger.Log(
                    $"FLASHBACK_DECODER_SEEK_WARN keyframe_seek_failed code={result} " +
                    $"stream_code={streamSeekResult} target_ms={(long)target.TotalMilliseconds} stream_ts={streamTimestamp}");
                return false;
            }

            Logger.Log(
                $"FLASHBACK_DECODER_SEEK_FALLBACK_OK target_ms={(long)target.TotalMilliseconds} " +
                $"stream_ts={streamTimestamp} us_ts={timestampUs}");
        }

        if (_videoCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_videoCodecCtx);
        }

        if (_audioCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_audioCodecCtx);
        }

        // Clear any stashed pending frame — it's from before the seek point
        if (_hasPendingVideoFrame)
        {
            ReleaseHeldFrameBestEffort(_pendingVideoFrame, "seek_keyframe_pending");
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
        }

        _suppressRecoverableSeekLogsForNextVideoFrame = true;

        Logger.Log(
            $"FLASHBACK_DECODER_SEEK_OK target_ms={(long)target.TotalMilliseconds} " +
            $"stream_index={_videoStreamIndex} stream_ts={streamTimestamp}");
        return true;
    }

    /// <summary>
    /// Seeks to the exact frame at <paramref name="target"/> by first seeking to the
    /// nearest preceding keyframe, then decoding forward until the target PTS is reached.
    /// </summary>
    public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)
    {
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();
        _lastSeekHitForwardDecodeCap = false;

        if (!SeekToKeyframe(target, cancellationToken))
        {
            return false;
        }

        // Decode forward until we reach (or pass) the target PTS.
        // Stash the target frame so the next TryDecodeNextVideoFrame() returns it
        // instead of skipping past it (fixes off-by-one on seek).
        // Cap at 960 frames (8s at 120fps) to prevent CPU saturation on scrub.
        const int maxForwardFrames = 960;
        var targetTicks = target.Ticks;
        DecodedVideoFrame? bestFrame = null;
        var bestFrameTransferred = false;
        try
        {
            for (var i = 0; i < maxForwardFrames; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))
                {
                    // Reached EOF before target — return best frame if we have one
                    if (bestFrame != null)
                    {
                        _currentPosition = bestFrame.Value.Pts;
                        _pendingVideoFrame = bestFrame.Value;
                        _hasPendingVideoFrame = true;
                        bestFrameTransferred = true;
                        return true;
                    }
                    return false;
                }

                if (frame.Pts.Ticks >= targetTicks)
                {
                    _currentPosition = frame.Pts;
                    _pendingVideoFrame = frame;
                    _hasPendingVideoFrame = true;
                    if (bestFrame != null)
                    {
                        ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_replace_best");
                        bestFrame = null;
                    }
                    return true;
                }

                // Keep the closest frame in case we hit the limit
                if (bestFrame != null) ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_best_superseded");
                bestFrame = frame;
            }

            // Hit frame limit — return the closest frame we decoded
            if (bestFrame != null)
            {
                var bestMs = (long)bestFrame.Value.Pts.TotalMilliseconds;
                var targetMs = (long)target.TotalMilliseconds;
                var gapMs = targetMs - bestMs;
                // One frame interval in ms (guard against zero/negative frame rate)
                var frameIntervalMs = _frameRate > 0.0 ? (long)(1000.0 / _frameRate) : 0L;
                if (gapMs > frameIntervalMs)
                {
                    _lastSeekHitForwardDecodeCap = true;
                    Interlocked.Increment(ref _seekToCapHits);
                    Logger.Log($"FLASHBACK_DECODER_SEEK_CAP_HIT target_ms={targetMs} best_ms={bestMs} gap_ms={gapMs} frames_decoded={maxForwardFrames}");
                }
                else
                {
                    Logger.Log($"FLASHBACK_DECODER_SEEK_FRAME_LIMIT target_ms={targetMs} best_ms={bestMs} frames={maxForwardFrames}");
                }
                _currentPosition = bestFrame.Value.Pts;
                _pendingVideoFrame = bestFrame.Value;
                _hasPendingVideoFrame = true;
                bestFrameTransferred = true;
                return true;
            }
            return false;
        }
        finally
        {
            if (!bestFrameTransferred && bestFrame != null)
            {
                ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_best_abandoned");
            }
        }
    }

    /// <summary>
    /// Decodes the next video frame.
    /// For D3D11VA: returns a <see cref="DecodedVideoFrame"/> with <see cref="DecodedVideoFrame.IsD3D11Texture"/> = true.
    /// For software: returns raw NV12/P010 data in <see cref="DecodedVideoFrame.Data"/>.
    /// </summary>
    public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)
    {
        frame = default;
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();
        _lastDecodePhaseTimings = default;

        // Return stashed frame from SeekTo() before decoding new ones
        if (_hasPendingVideoFrame)
        {
            frame = _pendingVideoFrame;
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
            return true;
        }

        using var recoverableSeekLogScope = BeginRecoverableSeekLogSuppressionIfNeeded();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // First try to receive a frame from the decoder (may have buffered frames)
            var receiveStart = Stopwatch.GetTimestamp();
            var receiveResult = ffmpeg.avcodec_receive_frame(_videoCodecCtx, _videoFrame);
            AddLastDecodeReceiveMs(ElapsedMsSince(receiveStart));
            if (receiveResult == 0)
            {
                // Got a decoded frame — convert and return
                var convertStart = Stopwatch.GetTimestamp();
                frame = ConvertAndOutputVideoFrame();
                AddLastDecodeConvertMs(ElapsedMsSince(convertStart));
                if (frame.Width <= 0)
                    return false; // clone failed, treat as decode failure
                return true;
            }

            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                // Decoder needs more packets
                var feedStart = Stopwatch.GetTimestamp();
                if (!FeedNextVideoPacket(cancellationToken))
                {
                    // Temporary EOF on live fMP4 — do NOT enter drain mode.
                    // The encoder is still appending; drain mode is permanent and
                    // would prevent decoding any future frames.
                    AddLastDecodeFeedMs(ElapsedMsSince(feedStart));
                    return false;
                }

                AddLastDecodeFeedMs(ElapsedMsSince(feedStart));
                continue;
            }

            if (receiveResult == ffmpeg.AVERROR_EOF)
            {
                // Decoder was previously drained — reset so it can accept new packets
                ffmpeg.avcodec_flush_buffers(_videoCodecCtx);
                return false;
            }

            // Unexpected error
            Logger.Log($"FLASHBACK_DECODER_VIDEO_ERROR receive_frame code={receiveResult}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AudioChunkCallback = null;
        CloseFileCore();

        // Free persistent D3D11VA device context
        if (_d3d11HwDeviceCtx != null)
        {
            var h = _d3d11HwDeviceCtx;
            ffmpeg.av_buffer_unref(&h);
            _d3d11HwDeviceCtx = null;
        }

        Logger.Log("FLASHBACK_DECODER_DISPOSED");
    }

    // ── Private: Initialization ─────────────────────────────────────────────

    private void InitializeVideoDecoder()
    {
        // Reset hw accel flag — it persists across file opens but must reflect
        // the decoder chosen for THIS file (D3D11VA may fail for some streams).
        _isD3D11HwAccelerated = false;

        var videoStream = _formatCtx->streams[_videoStreamIndex];
        _videoTimeBase = videoStream->time_base;

        var codecPar = videoStream->codecpar;
        _videoWidth = codecPar->width;
        _videoHeight = codecPar->height;
        ValidateVideoDimensions(_videoWidth, _videoHeight);

        // Determine pixel format and HDR status
        _decodedPixelFormat = (AVPixelFormat)codecPar->format;
        _isHdr = (codecPar->codec_id == AVCodecID.AV_CODEC_ID_HEVC ||
                  codecPar->codec_id == AVCodecID.AV_CODEC_ID_AV1) &&
                 (_decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P10LE ||
                  _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_P010LE);

        // Calculate frame rate
        if (videoStream->avg_frame_rate.den > 0 && videoStream->avg_frame_rate.num > 0)
        {
            _frameRate = (double)videoStream->avg_frame_rate.num / videoStream->avg_frame_rate.den;
        }
        else if (videoStream->r_frame_rate.den > 0 && videoStream->r_frame_rate.num > 0)
        {
            _frameRate = (double)videoStream->r_frame_rate.num / videoStream->r_frame_rate.den;
        }
        else
        {
            _frameRate = 30.0; // fallback
            Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN reason=framerate_fallback default=30.0 path='{_currentFilePath}'");
        }

        Logger.Log($"FLASHBACK_DECODER_STREAM_INFO " +
                   $"avg_frame_rate={{num={videoStream->avg_frame_rate.num}, den={videoStream->avg_frame_rate.den}}} " +
                   $"r_frame_rate={{num={videoStream->r_frame_rate.num}, den={videoStream->r_frame_rate.den}}} " +
                   $"time_base={{num={videoStream->time_base.num}, den={videoStream->time_base.den}}} " +
                   $"computed_fps={_frameRate:F4}");

        _metadataFrameRate = _frameRate;
        _ptsCalibrationCount = 0;
        _firstCalibrationPtsTicks = 0;
        _lastCalibrationPtsTicks = 0;

        // Try D3D11VA hardware decode first, fall back to software
        if (TryInitializeD3D11VADecoder(codecPar))
        {
            _videoFrame = ffmpeg.av_frame_alloc();
            if (_videoFrame == null)
                throw CreateException("Failed to allocate video frame.");

            Logger.Log($"FLASHBACK_DECODER_VIDEO hw_accel=D3D11VA " +
                       $"sw_fmt={(_isHdr ? "P010" : "NV12")} {_videoWidth}x{_videoHeight}");
            return;
        }

        // Software fallback
        var codec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
        if (codec == null)
        {
            throw CreateException($"No decoder found for codec_id={codecPar->codec_id}.");
        }

        _videoCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_videoCodecCtx == null)
        {
            throw CreateException("Failed to allocate video codec context.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_to_context(_videoCodecCtx, codecPar),
            "avcodec_parameters_to_context(video)");

        // MJPEG frames are independently decodable; FFmpeg auto-threading can add
        // avoidable per-frame latency spikes at 4K120 playback.
        if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)
        {
            _videoCodecCtx->thread_count = 1;
        }

        ThrowIfError(
            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),
            "avcodec_open2(video)");

        // Allocate frames
        _videoFrame = ffmpeg.av_frame_alloc();
        if (_videoFrame == null)
        {
            throw CreateException("Failed to allocate video frame.");
        }

        // Determine target format and whether manual conversion is needed
        var targetFormat = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;

        var actualDecodedFormat = _videoCodecCtx->pix_fmt;
        if (actualDecodedFormat != AVPixelFormat.AV_PIX_FMT_NONE)
        {
            _decodedPixelFormat = actualDecodedFormat;
        }

        _needsConvert = _decodedPixelFormat != targetFormat;

        if (_needsConvert)
        {
            var canConvert =
                _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P ||
                _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P10LE;

            if (!canConvert)
            {
                throw CreateException($"Unsupported decoded format {_decodedPixelFormat} -> {targetFormat}");
            }
        }

        AllocateVideoOutputBuffers();

        var videoCodecName = codec->name != null ? Marshal.PtrToStringAnsi((IntPtr)codec->name) : "?";
        Logger.Log($"FLASHBACK_DECODER_VIDEO codec={videoCodecName} hw_accel=Software " +
                   $"pix_fmt={_decodedPixelFormat} target={targetFormat} " +
                   $"needs_convert={_needsConvert}");
    }

    private void AllocateVideoOutputBuffers()
    {
        var outputFrameSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);
        for (var i = 0; i < VideoFrameBufferCount; i++)
        {
            _videoFrameBuffers[i] = ArrayPool<byte>.Shared.Rent(outputFrameSize);
            _videoFrameHandles[i] = GCHandle.Alloc(_videoFrameBuffers[i], GCHandleType.Pinned);
        }
        _currentVideoBufferIndex = 0;
    }

    // ── Private: Decode Loop ────────────────────────────────────────────────

    /// <summary>
    /// Reads packets until a video packet is sent to the decoder.
    /// Audio packets are decoded inline via AudioChunkCallback.
    /// </summary>
    private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ffmpeg.av_packet_unref(_packet);
            var readStart = Stopwatch.GetTimestamp();
            var readResult = ffmpeg.av_read_frame(_formatCtx, _packet);
            AddLastDecodeReadMs(ElapsedMsSince(readStart));
            if (readResult < 0)
            {
                // Clear AVIO EOF flag so subsequent reads can see newly appended data.
                // Without this, C stdio's fread EOF is cached and av_read_frame keeps
                // returning EOF even after the encoder writes more to the file.
                if (_formatCtx->pb != null)
                    _formatCtx->pb->eof_reached = 0;
                return false;
            }

            if (_packet->stream_index == _videoStreamIndex)
            {
                var sendStart = Stopwatch.GetTimestamp();
                var sendResult = ffmpeg.avcodec_send_packet(_videoCodecCtx, _packet);
                AddLastDecodeSendMs(ElapsedMsSince(sendStart));
                ffmpeg.av_packet_unref(_packet);
                if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN send_packet code={sendResult}");
                    continue;
                }

                return true;
            }

            // Decode audio inline — keeps A/V naturally interleaved
            try
            {
                if (_packet->stream_index == _audioStreamIndex && _audioCodecCtx != null)
                {
                    var audioStart = Stopwatch.GetTimestamp();
                    DecodeAndDeliverAudioPacket(_packet);
                    AddLastDecodeAudioMs(ElapsedMsSince(audioStart));
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    // ── Private: Cleanup ────────────────────────────────────────────────────

    // ── Private: Helpers ────────────────────────────────────────────────────

}

// ── Output Types ────────────────────────────────────────────────────────────

/// <summary>
/// A decoded video frame. Either a D3D11 texture (GPU-direct) or raw NV12/P010 data.
/// For D3D11: <see cref="TexturePtr"/> and <see cref="SubresourceIndex"/> are valid.
/// For software: <see cref="Data"/> is valid until the next decode call.
/// </summary>
internal readonly struct DecodedVideoFrame
{
    public IntPtr Data { get; init; }
    public int DataLength { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsHdr { get; init; }
    public TimeSpan Pts { get; init; }
    public IntPtr TexturePtr { get; init; }
    public int SubresourceIndex { get; init; }
    public bool IsD3D11Texture { get; init; }
    public IntPtr HeldFrame { get; init; } // AVFrame* that must be freed after texture is consumed
}

/// <summary>
/// A decoded audio chunk in f32le interleaved stereo 48kHz format.
/// <see cref="Samples"/> is rented from <see cref="ArrayPool{T}"/> — the caller should
/// return it via <c>ArrayPool&lt;byte&gt;.Shared.Return(chunk.Samples)</c> when done.
/// </summary>
internal readonly struct DecodedAudioChunk
{
    public byte[] Samples { get; init; }
    public int ValidLength { get; init; }
    public TimeSpan Pts { get; init; }
}

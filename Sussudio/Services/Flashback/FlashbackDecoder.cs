using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Services.Audio;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Video+audio decoder for Flashback .ts files.
/// Decodes HEVC/H.264 video via D3D11VA (GPU-direct) or software fallback to NV12/P010,
/// and AAC audio to f32le interleaved stereo 48kHz.
/// This type is NOT thread-safe — all calls must come from the playback controller's thread.
/// </summary>
internal sealed unsafe class FlashbackDecoder : IDisposable
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
    private const int MaxHardwareConfigCount = 64;
    private const int AvCodecHwConfigMethodHwDeviceCtx = 0x01;
    private const int AvCodecHwConfigMethodHwFramesCtx = 0x02;
    private const int AvCodecHwConfigMethodInternal = 0x04;
    private const int AvCodecHwConfigMethodAdHoc = 0x08;

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

    // get_format callback: tells the decoder to use D3D11VA when available.
    // Must be stored as a field to prevent GC collection while the decoder is alive.
    private static readonly AVCodecContext_get_format GetFormatD3D11Callback = GetFormatD3D11;

    private static AVPixelFormat GetFormatD3D11(AVCodecContext* ctx, AVPixelFormat* fmt)
    {
        for (var p = fmt; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
        {
            if (*p == AVPixelFormat.AV_PIX_FMT_D3D11)
                return AVPixelFormat.AV_PIX_FMT_D3D11;
        }
        // D3D11 not offered — log the available formats for diagnostics
        var offered = new System.Text.StringBuilder();
        for (var p = fmt; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
        {
            if (offered.Length > 0) offered.Append(',');
            offered.Append((int)*p);
        }
        Logger.Log($"FLASHBACK_DECODER_D3D11VA_NOT_OFFERED formats=[{offered}] fallback={(int)*fmt}");
        return *fmt;
    }

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

        var timestampUs = ToAvTimeBaseTimestamp(target);
        var result = ffmpeg.av_seek_frame(
            _formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);
        cancellationToken.ThrowIfCancellationRequested();

        if (result < 0)
        {
            Logger.Log($"FLASHBACK_DECODER_SEEK_WARN keyframe_seek_failed code={result} target_ms={(long)target.TotalMilliseconds}");
            return false;
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

        Logger.Log($"FLASHBACK_DECODER_SEEK_OK target_ms={(long)target.TotalMilliseconds}");
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

    /// <summary>
    /// Attempts to initialize a D3D11VA hardware decoder using the persistent device context.
    /// Returns true on success. Output textures live on the same D3D11 device as the renderer.
    /// </summary>
    private bool TryInitializeD3D11VADecoder(AVCodecParameters* codecPar)
    {
        if (_d3d11HwDeviceCtx == null)
            return false;

        // Only H264, HEVC, AV1 are supported
        if (codecPar->codec_id != AVCodecID.AV_CODEC_ID_H264 &&
            codecPar->codec_id != AVCodecID.AV_CODEC_ID_HEVC &&
            codecPar->codec_id != AVCodecID.AV_CODEC_ID_AV1)
        {
            return false;
        }

        // Use the generic decoder (not a specific hw decoder name) — D3D11VA is activated
        // by attaching hw_device_ctx + hw_frames_ctx to the generic decoder.
        var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);
        if (codec == null)
        {
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder id={codecPar->codec_id}");
            return false;
        }

        AVCodecContext* decoderCtx = null;

        try
        {
            decoderCtx = ffmpeg.avcodec_alloc_context3(codec);
            if (decoderCtx == null)
            {
                Logger.Log("FLASHBACK_DECODER_D3D11VA_SKIP reason=alloc_context_fail");
                return false;
            }

            var paramsResult = ffmpeg.avcodec_parameters_to_context(decoderCtx, codecPar);
            if (paramsResult < 0)
            {
                Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=params_to_ctx_fail code={paramsResult}");
                goto cleanup;
            }

            // For D3D11VA decoding: provide hw_device_ctx and a get_format callback
            // that selects AV_PIX_FMT_D3D11. The decoder creates its own hw_frames_ctx
            // internally during avcodec_open2.
            decoderCtx->hw_device_ctx = ffmpeg.av_buffer_ref(_d3d11HwDeviceCtx);
            if (decoderCtx->hw_device_ctx == null)
            {
                Logger.Log("FLASHBACK_DECODER_D3D11VA_SKIP reason=hw_device_ref_fail");
                goto cleanup;
            }

            decoderCtx->get_format = GetFormatD3D11Callback;
            decoderCtx->extra_hw_frames = 4;

            var openResult = ffmpeg.avcodec_open2(decoderCtx, codec, null);
            if (openResult < 0)
            {
                Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=open_fail code={openResult} codec={codecName}");
                goto cleanup;
            }

            // Note: pix_fmt is not D3D11 yet — get_format callback runs on first decode.
            // Success — transfer ownership
            _videoCodecCtx = decoderCtx;
            _isD3D11HwAccelerated = true;
            _needsConvert = false;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        }

    cleanup:
        if (decoderCtx != null) ffmpeg.avcodec_free_context(&decoderCtx);
        return false;
    }

    private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)
    {
        codecName = codecId.ToString();
        var preferredName = GetPreferredD3D11DecoderName(codecId);
        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            var preferred = ffmpeg.avcodec_find_decoder_by_name(preferredName);
            if (preferred != null &&
                TryDescribeD3D11DecoderCandidate(preferred, codecId, "preferred", out codecName))
            {
                Logger.Log($"FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
                return preferred;
            }
        }

        var generic = ffmpeg.avcodec_find_decoder(codecId);
        if (generic != null &&
            TryDescribeD3D11DecoderCandidate(generic, codecId, "generic", out codecName))
        {
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_SELECT source=generic codec={codecName}");
            return generic;
        }

        return null;
    }

    private static string? GetPreferredD3D11DecoderName(AVCodecID codecId)
        => codecId switch
        {
            AVCodecID.AV_CODEC_ID_AV1 => "av1",
            AVCodecID.AV_CODEC_ID_HEVC => "hevc",
            AVCodecID.AV_CODEC_ID_H264 => "h264",
            _ => null
        };

    private static bool TryDescribeD3D11DecoderCandidate(
        AVCodec* codec,
        AVCodecID codecId,
        string source,
        out string codecName)
    {
        codecName = GetCodecName(codec, codecId);
        var hardwareConfigSummary = DescribeHardwareConfigs(codec, out var hasD3D11DeviceConfig);
        Logger.Log(
            $"FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        return hasD3D11DeviceConfig;
    }

    private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)
    {
        hasD3D11DeviceConfig = false;
        var parts = new List<string>();

        for (var i = 0; i < MaxHardwareConfigCount; i++)
        {
            var config = ffmpeg.avcodec_get_hw_config(codec, i);
            if (config == null)
            {
                break;
            }

            var pixelFormat = config->pix_fmt;
            var deviceType = config->device_type;
            var methods = config->methods;
            var supportsD3D11DeviceCtx =
                pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11 &&
                deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA &&
                (methods & AvCodecHwConfigMethodHwDeviceCtx) != 0;

            hasD3D11DeviceConfig |= supportsD3D11DeviceCtx;
            parts.Add(
                $"idx={i}:pix_fmt={GetPixelFormatName(pixelFormat)} device={GetHardwareDeviceName(deviceType)} methods={FormatHardwareConfigMethods(methods)}");
        }

        return parts.Count == 0 ? "none" : string.Join(";", parts);
    }

    private static string FormatHardwareConfigMethods(int methods)
    {
        var parts = new List<string>(4);
        if ((methods & AvCodecHwConfigMethodHwDeviceCtx) != 0) parts.Add("HW_DEVICE_CTX");
        if ((methods & AvCodecHwConfigMethodHwFramesCtx) != 0) parts.Add("HW_FRAMES_CTX");
        if ((methods & AvCodecHwConfigMethodInternal) != 0) parts.Add("INTERNAL");
        if ((methods & AvCodecHwConfigMethodAdHoc) != 0) parts.Add("AD_HOC");
        var knownMask = AvCodecHwConfigMethodHwDeviceCtx |
                        AvCodecHwConfigMethodHwFramesCtx |
                        AvCodecHwConfigMethodInternal |
                        AvCodecHwConfigMethodAdHoc;
        var unknown = methods & ~knownMask;
        if (unknown != 0) parts.Add($"UNKNOWN_0x{unknown:X}");
        return parts.Count == 0 ? "none" : string.Join("+", parts);
    }

    private static string GetCodecName(AVCodec* codec, AVCodecID codecId)
    {
        if (codec != null && codec->name != null)
        {
            return Marshal.PtrToStringAnsi((IntPtr)codec->name) ?? codecId.ToString();
        }

        return ffmpeg.avcodec_get_name(codecId) ?? codecId.ToString();
    }

    private static string GetPixelFormatName(AVPixelFormat pixelFormat)
    {
        return ffmpeg.av_get_pix_fmt_name(pixelFormat) ?? pixelFormat.ToString();
    }

    private static string GetHardwareDeviceName(AVHWDeviceType deviceType)
    {
        return ffmpeg.av_hwdevice_get_type_name(deviceType) ?? deviceType.ToString();
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

    private void InitializeAudioDecoder()
    {
        var audioStream = _formatCtx->streams[_audioStreamIndex];
        _audioTimeBase = audioStream->time_base;

        var codecPar = audioStream->codecpar;

        var codec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
        if (codec == null)
        {
            Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN no decoder for codec_id={codecPar->codec_id}, audio disabled");
            _audioStreamIndex = -1;
            return;
        }

        _audioCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_audioCodecCtx == null)
        {
            throw CreateException("Failed to allocate audio codec context.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_to_context(_audioCodecCtx, codecPar),
            "avcodec_parameters_to_context(audio)");

        ThrowIfError(
            ffmpeg.avcodec_open2(_audioCodecCtx, codec, null),
            "avcodec_open2(audio)");

        // Allocate audio decode frame
        _audioFrame = ffmpeg.av_frame_alloc();
        if (_audioFrame == null)
        {
            throw CreateException("Failed to allocate audio frame.");
        }

        // Set up SwrContext: decoded format (typically fltp) → f32le interleaved stereo 48kHz
        InitializeAudioResampler();

        var audioCodecName = codec->name != null ? Marshal.PtrToStringAnsi((IntPtr)codec->name) : "?";
        Logger.Log($"FLASHBACK_DECODER_AUDIO codec={audioCodecName} " +
                   $"sample_rate={_audioCodecCtx->sample_rate} sample_fmt={_audioCodecCtx->sample_fmt} " +
                   $"channels={_audioCodecCtx->ch_layout.nb_channels}");
    }

    private void InitializeAudioResampler()
    {
        AVChannelLayout outputLayout = default;
        ffmpeg.av_channel_layout_default(&outputLayout, OutputAudioChannels);

        var swrCtx = _swrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &outputLayout,                              // output layout: stereo
                AVSampleFormat.AV_SAMPLE_FMT_FLT,           // output format: f32le interleaved
                OutputAudioSampleRate,                       // output sample rate: 48kHz
                &_audioCodecCtx->ch_layout,                 // input layout: from codec
                _audioCodecCtx->sample_fmt,                 // input format: from codec (typically fltp)
                _audioCodecCtx->sample_rate,                // input sample rate: from codec
                0,
                null);
            _swrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2(decode)");

            if (_swrCtx == null)
            {
                throw CreateException("Failed to allocate audio resampler.");
            }

            ThrowIfError(ffmpeg.swr_init(_swrCtx), "swr_init(decode)");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&outputLayout);
        }
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

    private void AddLastDecodeReceiveMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ReceiveMs = _lastDecodePhaseTimings.ReceiveMs + elapsedMs };

    private void AddLastDecodeFeedMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { FeedMs = _lastDecodePhaseTimings.FeedMs + elapsedMs };

    private void AddLastDecodeReadMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ReadMs = _lastDecodePhaseTimings.ReadMs + elapsedMs };

    private void AddLastDecodeSendMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { SendMs = _lastDecodePhaseTimings.SendMs + elapsedMs };

    private void AddLastDecodeAudioMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { AudioMs = _lastDecodePhaseTimings.AudioMs + elapsedMs };

    private void AddLastDecodeConvertMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ConvertMs = _lastDecodePhaseTimings.ConvertMs + elapsedMs };

    private static double ElapsedMsSince(long startTimestamp)
        => (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

    /// <summary>
    /// Sends an audio packet to the decoder and delivers any resulting chunks
    /// via <see cref="AudioChunkCallback"/>. If no callback is set, audio is
    /// silently decoded (keeps the decoder state advancing) but not delivered.
    /// </summary>
    private void DecodeAndDeliverAudioPacket(AVPacket* packet)
    {
        // Always feed packets to the codec so it tracks PTS position correctly.
        // During seek/scrub (callback null), we decode but discard the output.
        // This ensures audio and video codecs are at the same position when
        // playback starts — no suppression or drift compensation needed.
        var sendResult = ffmpeg.avcodec_send_packet(_audioCodecCtx, packet);
        if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            return;

        while (ffmpeg.avcodec_receive_frame(_audioCodecCtx, _audioFrame) == 0)
        {
            var callback = AudioChunkCallback;
            if (callback == null)
            {
                ffmpeg.av_frame_unref(_audioFrame);
                continue; // Codec advanced, but no delivery during seek/scrub
            }

            var chunk = ConvertAndOutputAudioFrame();
            if (chunk.ValidLength > 0)
            {
                try
                {
                    callback(chunk);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_DECODE_AUDIO_CALLBACK_FAIL type={ex.GetType().Name} msg={ex.Message}");
                    // Caller is responsible for returning buffer on success; on failure we must return it
                    if (chunk.Samples != null)
                        ArrayPool<byte>.Shared.Return(chunk.Samples);
                }
            }
            else if (chunk.Samples != null && chunk.Samples.Length > 0)
                ArrayPool<byte>.Shared.Return(chunk.Samples);
        }
    }

    // ── Private: Frame Conversion ───────────────────────────────────────────

    private DecodedVideoFrame ConvertAndOutputVideoFrame()
    {
        // Calculate PTS first (used by both paths). MPEG-TS frames can have
        // AV_NOPTS_VALUE in pts even when FFmpeg recovered a usable timestamp.
        var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_videoFrame), _videoTimeBase);

        _currentPosition = pts;

        if (_ptsCalibrationCount < PtsCalibrationFrames && pts > TimeSpan.Zero)
        {
            if (_ptsCalibrationCount == 0)
                _firstCalibrationPtsTicks = pts.Ticks;
            _lastCalibrationPtsTicks = pts.Ticks;
            _ptsCalibrationCount++;

            if (_ptsCalibrationCount == PtsCalibrationFrames && _lastCalibrationPtsTicks > _firstCalibrationPtsTicks)
            {
                var elapsedSec = (_lastCalibrationPtsTicks - _firstCalibrationPtsTicks) / (double)TimeSpan.TicksPerSecond;
                if (elapsedSec > 0.001)
                {
                    var measuredFps = (PtsCalibrationFrames - 1) / elapsedSec;
                    if (_metadataFrameRate > measuredFps * 1.5 && measuredFps > 10)
                    {
                        Logger.Log($"FLASHBACK_DECODER_FPS_OVERRIDE metadata={_metadataFrameRate:F2} measured={measuredFps:F2}");
                        _frameRate = measuredFps;
                    }
                }
            }
        }

        // Check actual frame format — D3D11VA may silently fall back to software
        // (get_format callback runs on first decode, not during avcodec_open2).
        var actualFormat = (AVPixelFormat)_videoFrame->format;
        if (_isD3D11HwAccelerated && actualFormat != AVPixelFormat.AV_PIX_FMT_D3D11)
        {
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_FALLBACK actual_fmt={actualFormat} — switching to software path");
            _isD3D11HwAccelerated = false;
            _decodedPixelFormat = actualFormat;
            var targetFmt = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _needsConvert = actualFormat != targetFmt;
            AllocateVideoOutputBuffers();
        }

        if (_isD3D11HwAccelerated)
        {
            // D3D11VA path: frame->data[0] is ID3D11Texture2D*, data[1] is subresource index.
            // Clone the AVFrame to hold the D3D11VA surface reference — _videoFrame is reused
            // by the next avcodec_receive_frame call, which can release the surface back to the
            // pool before the renderer copies it. The cloned frame must be freed after the
            // texture is consumed (via HeldFrame).
            var clonedFrame = ffmpeg.av_frame_clone(_videoFrame);
            ffmpeg.av_frame_unref(_videoFrame); // release pool slot immediately
            if (clonedFrame == null)
            {
                Logger.Log("FLASHBACK_DECODE_CLONE_FAIL reason='av_frame_clone returned null'");
                return default;
            }

            if (!TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight, out var d3dFrameFailure))
            {
                Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_d3d11_frame detail='{d3dFrameFailure}' w={_videoWidth} h={_videoHeight}");
                ffmpeg.av_frame_free(&clonedFrame);
                return default;
            }

            var texturePtr = (IntPtr)clonedFrame->data[0];
            var subresource = (int)(long)clonedFrame->data[1];

            return new DecodedVideoFrame
            {
                TexturePtr = texturePtr,
                SubresourceIndex = subresource,
                Width = _videoWidth,
                Height = _videoHeight,
                IsHdr = _isHdr,
                Pts = pts,
                IsD3D11Texture = true,
                HeldFrame = (IntPtr)clonedFrame
            };
        }

        // Software decode path
        if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)
        {
            _decodedPixelFormat = actualFormat;
            var targetFmt = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _needsConvert = _decodedPixelFormat != targetFmt;
        }

        if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))
        {
            Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame detail='{frameFailure}' fmt={_decodedPixelFormat} w={_videoWidth} h={_videoHeight}");
            ffmpeg.av_frame_unref(_videoFrame);
            return default;
        }

        try
        {
            var outputSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);

            // Select the next buffer in the double-buffer ring
            var bufferIndex = _currentVideoBufferIndex;
            _currentVideoBufferIndex = (_currentVideoBufferIndex + 1) % VideoFrameBufferCount;

            var buffer = _videoFrameBuffers[bufferIndex]!;
            if (buffer.Length < outputSize)
            {
                Logger.Log($"FLASHBACK_DECODER_VIDEO_REALLOC old={buffer.Length} new={outputSize}");
                if (_videoFrameHandles[bufferIndex].IsAllocated)
                {
                    _videoFrameHandles[bufferIndex].Free();
                }

                ArrayPool<byte>.Shared.Return(buffer);
                buffer = ArrayPool<byte>.Shared.Rent(outputSize);
                _videoFrameBuffers[bufferIndex] = buffer;
                _videoFrameHandles[bufferIndex] = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }

            var dataPtr = _videoFrameHandles[bufferIndex].AddrOfPinnedObject();

            if (_needsConvert)
            {
                if (!_isHdr)
                    ConvertYuv420pToNv12((byte*)dataPtr);
                else
                    ConvertYuv420p10leToP010((byte*)dataPtr);
            }
            else
            {
                CopyFramePlanesToBuffer((byte*)dataPtr, outputSize);
            }

            return new DecodedVideoFrame
            {
                Data = dataPtr,
                DataLength = outputSize,
                Width = _videoWidth,
                Height = _videoHeight,
                IsHdr = _isHdr,
                Pts = pts,
                IsD3D11Texture = false,
                HeldFrame = IntPtr.Zero
            };
        }
        finally
        {
            ffmpeg.av_frame_unref(_videoFrame);
        }
    }

    private void CopyFramePlanesToBuffer(byte* dest, int destSize)
    {
        if (_isHdr)
        {
            var yLinesize = _videoWidth * 2;
            var yPlaneSize = yLinesize * _videoHeight;
            var uvLinesize = _videoWidth * 2;

            CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0],
                      dest, yLinesize, _videoHeight);
            CopyPlane(_videoFrame->data[1], _videoFrame->linesize[1],
                      dest + yPlaneSize, uvLinesize, _videoHeight / 2);
        }
        else
        {
            var yPlaneSize = _videoWidth * _videoHeight;

            CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0],
                      dest, _videoWidth, _videoHeight);
            CopyPlane(_videoFrame->data[1], _videoFrame->linesize[1],
                      dest + yPlaneSize, _videoWidth, _videoHeight / 2);
        }
    }

    private static bool TryValidateSoftwareVideoFrame(
        AVFrame* frame,
        AVPixelFormat format,
        int width,
        int height,
        bool isHdr,
        out string failure)
    {
        failure = string.Empty;
        if (frame == null)
        {
            failure = "frame_null";
            return false;
        }

        if (frame->width > 0 && frame->width != width)
        {
            failure = $"width_mismatch frame={frame->width} expected={width}";
            return false;
        }

        if (frame->height > 0 && frame->height != height)
        {
            failure = $"height_mismatch frame={frame->height} expected={height}";
            return false;
        }

        var targetFormat = isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
        if (format == targetFormat)
        {
            var lumaBytes = isHdr ? width * 2 : width;
            var chromaBytes = isHdr ? width * 2 : width;
            return TryValidatePlane(frame, 0, lumaBytes, out failure) &&
                   TryValidatePlane(frame, 1, chromaBytes, out failure);
        }

        if (!isHdr && format == AVPixelFormat.AV_PIX_FMT_YUV420P)
        {
            return TryValidatePlane(frame, 0, width, out failure) &&
                   TryValidatePlane(frame, 1, width / 2, out failure) &&
                   TryValidatePlane(frame, 2, width / 2, out failure);
        }

        if (isHdr && format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE)
        {
            return TryValidatePlane(frame, 0, width * 2, out failure) &&
                   TryValidatePlane(frame, 1, width, out failure) &&
                   TryValidatePlane(frame, 2, width, out failure);
        }

        failure = $"unsupported_format:{format}";
        return false;
    }

    private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)
    {
        var plane = (uint)planeIndex;
        if (frame->data[plane] == null)
        {
            failure = $"plane_{planeIndex}_null";
            return false;
        }

        if (frame->linesize[plane] < minLineSize)
        {
            failure = $"plane_{planeIndex}_linesize:{frame->linesize[plane]}<{minLineSize}";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)
    {
        failure = string.Empty;
        if (frame == null)
        {
            failure = "frame_null";
            return false;
        }

        if (frame->width > 0 && frame->width != width)
        {
            failure = $"width_mismatch frame={frame->width} expected={width}";
            return false;
        }

        if (frame->height > 0 && frame->height != height)
        {
            failure = $"height_mismatch frame={frame->height} expected={height}";
            return false;
        }

        if (frame->data[0] == null)
        {
            failure = "texture_null";
            return false;
        }

        var subresource = (long)frame->data[1];
        if (subresource < 0 || subresource > int.MaxValue)
        {
            failure = $"subresource_out_of_range:{subresource}";
            return false;
        }

        return true;
    }

    private static void CopyPlane(byte* src, int srcLinesize, byte* dst, int dstLinesize, int height)
    {
        if (srcLinesize == dstLinesize)
        {
            Buffer.MemoryCopy(src, dst, (long)dstLinesize * height, (long)srcLinesize * height);
            return;
        }

        var copyWidth = Math.Min(srcLinesize, dstLinesize);
        for (var y = 0; y < height; y++)
        {
            Buffer.MemoryCopy(
                src + y * srcLinesize,
                dst + y * dstLinesize,
                dstLinesize, copyWidth);
        }
    }

    private void ConvertYuv420pToNv12(byte* dest)
    {
        var w = _videoWidth;
        var h = _videoHeight;

        CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0], dest, w, h);

        var uvDest = dest + w * h;
        var halfW = w / 2;
        var uStride = _videoFrame->linesize[1];
        var vStride = _videoFrame->linesize[2];

        for (var row = 0; row < h / 2; row++)
        {
            var uRow = _videoFrame->data[1] + row * uStride;
            var vRow = _videoFrame->data[2] + row * vStride;
            var destRow = uvDest + row * w;

            InterleaveUvRow(uRow, vRow, destRow, halfW);
        }
    }

    private void ConvertYuv420p10leToP010(byte* dest)
    {
        var w = _videoWidth;
        var h = _videoHeight;

        CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0], dest, w * 2, h);

        var uvDest = (ushort*)(dest + w * h * 2);
        var halfW = w / 2;
        var uStride = _videoFrame->linesize[1];
        var vStride = _videoFrame->linesize[2];

        for (var row = 0; row < h / 2; row++)
        {
            var uRow = (ushort*)(_videoFrame->data[1] + row * uStride);
            var vRow = (ushort*)(_videoFrame->data[2] + row * vStride);
            var destRow = uvDest + row * w;

            for (var col = 0; col < halfW; col++)
            {
                destRow[col * 2] = uRow[col];
                destRow[col * 2 + 1] = vRow[col];
            }
        }
    }

    private static void InterleaveUvRow(byte* uRow, byte* vRow, byte* dest, int halfW)
    {
        var col = 0;

        if (Avx2.IsSupported)
        {
            for (; col + 32 <= halfW; col += 32)
            {
                var u = Avx.LoadVector256(uRow + col);
                var v = Avx.LoadVector256(vRow + col);

                var lo = Avx2.UnpackLow(u, v);
                var hi = Avx2.UnpackHigh(u, v);

                var out0 = Avx2.Permute2x128(lo, hi, 0x20);
                var out1 = Avx2.Permute2x128(lo, hi, 0x31);

                Avx.Store(dest + col * 2, out0);
                Avx.Store(dest + col * 2 + 32, out1);
            }
        }
        else if (Sse2.IsSupported)
        {
            for (; col + 16 <= halfW; col += 16)
            {
                var u = Sse2.LoadVector128(uRow + col);
                var v = Sse2.LoadVector128(vRow + col);

                var lo = Sse2.UnpackLow(u, v);
                var hi = Sse2.UnpackHigh(u, v);

                Sse2.Store(dest + col * 2, lo);
                Sse2.Store(dest + col * 2 + 16, hi);
            }
        }

        for (; col < halfW; col++)
        {
            dest[col * 2] = uRow[col];
            dest[col * 2 + 1] = vRow[col];
        }
    }

    private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)
    {
        if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)
        {
            return TimeSpan.Zero;
        }

        var seconds = (double)pts * timeBase.num / timeBase.den;
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)
    {
        if (frame == null)
        {
            return ffmpeg.AV_NOPTS_VALUE;
        }

        if (frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE)
        {
            return frame->best_effort_timestamp;
        }

        return frame->pts;
    }

    private static long ToAvTimeBaseTimestamp(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return 0;
        }

        var microseconds = value.TotalMilliseconds * 1000.0;
        if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)microseconds;
    }

    // ── Private: Audio Conversion ───────────────────────────────────────────

    private DecodedAudioChunk ConvertAndOutputAudioFrame()
    {
        var inputSamples = _audioFrame->nb_samples;
        var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);
        byte[]? result = null;
        var returnResultToPool = true;

        try
        {
            if (inputSamples <= 0)
            {
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            var maxOutputSamples = ffmpeg.swr_get_out_samples(_swrCtx, inputSamples);
            if (maxOutputSamples < 0)
            {
                maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);
            }

            if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size input_samples={inputSamples} max_output_samples={maxOutputSamples}");
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            result = ArrayPool<byte>.Shared.Rent(outputBytesNeeded);

            int outputSamplesProduced;
            fixed (byte* outputPtr = result)
            {
                var outputPlanes = stackalloc byte*[1];
                outputPlanes[0] = outputPtr;

                outputSamplesProduced = ffmpeg.swr_convert(
                    _swrCtx,
                    outputPlanes, maxOutputSamples,
                    _audioFrame->extended_data, inputSamples);
            }

            if (outputSamplesProduced <= 0)
            {
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size output_samples={outputSamplesProduced} buffer_bytes={result.Length}");
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            returnResultToPool = false;
            return new DecodedAudioChunk
            {
                Samples = result,
                ValidLength = validBytes,
                Pts = pts
            };
        }
        finally
        {
            ffmpeg.av_frame_unref(_audioFrame);
            if (returnResultToPool && result is { Length: > 0 })
            {
                ArrayPool<byte>.Shared.Return(result);
            }
        }
    }

    private static int ToBoundedAudioSampleCount(long sampleCount)
    {
        var maxSamples = MaxDecodedAudioFrameBytes / (OutputAudioChannels * sizeof(float));
        if (sampleCount <= 0)
        {
            return 0;
        }

        if (sampleCount > maxSamples)
        {
            return maxSamples;
        }

        return (int)sampleCount;
    }

    private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)
    {
        bytes = 0;
        if (sampleCount <= 0)
        {
            return false;
        }

        var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);
        if (calculated <= 0 || calculated > MaxDecodedAudioFrameBytes || calculated > int.MaxValue)
        {
            return false;
        }

        bytes = (int)calculated;
        return true;
    }

    // ── Private: Cleanup ────────────────────────────────────────────────────

    private void CloseFileCore()
    {
        Logger.Log($"FLASHBACK_DECODER_CLOSE_CORE path='{_currentFilePath}' had_swr={_swrCtx != null} had_video={_videoCodecCtx != null} had_audio={_audioCodecCtx != null}");
        _isOpen = false;

        // Clear any stashed pending frame (free held D3D11VA surface if present)
        if (_hasPendingVideoFrame)
        {
            ReleaseHeldFrameBestEffort(_pendingVideoFrame, "close_pending");
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
        }

        // Free pinned handles (software decode path only)
        for (var i = 0; i < VideoFrameBufferCount; i++)
        {
            if (_videoFrameHandles[i].IsAllocated)
            {
                _videoFrameHandles[i].Free();
            }

            if (_videoFrameBuffers[i] != null)
            {
                ArrayPool<byte>.Shared.Return(_videoFrameBuffers[i]!);
                _videoFrameBuffers[i] = null;
            }
        }

        if (_packet != null)
        {
            var pkt = _packet;
            ffmpeg.av_packet_free(&pkt);
            _packet = null;
        }

        if (_videoFrame != null)
        {
            var frame = _videoFrame;
            ffmpeg.av_frame_free(&frame);
            _videoFrame = null;
        }

        if (_audioFrame != null)
        {
            var frame = _audioFrame;
            ffmpeg.av_frame_free(&frame);
            _audioFrame = null;
        }

        // Note: do NOT free _d3d11HwDeviceCtx here — it's persistent across files
        // Note: _isD3D11HwAccelerated is reset per-file in InitializeVideoDecoder

        if (_swrCtx != null)
        {
            ffmpeg.swr_convert(_swrCtx, null, 0, null, 0);
            var swr = _swrCtx;
            ffmpeg.swr_free(&swr);
            _swrCtx = null;
        }

        if (_videoCodecCtx != null)
        {
            var ctx = _videoCodecCtx;
            ffmpeg.avcodec_free_context(&ctx);
            _videoCodecCtx = null;
        }

        if (_audioCodecCtx != null)
        {
            var ctx = _audioCodecCtx;
            ffmpeg.avcodec_free_context(&ctx);
            _audioCodecCtx = null;
        }

        if (_formatCtx != null)
        {
            var fmt = _formatCtx;
            ffmpeg.avformat_close_input(&fmt);
            _formatCtx = null;
        }

        _videoStreamIndex = -1;
        _audioStreamIndex = -1;
        _videoWidth = 0;
        _videoHeight = 0;
        _isHdr = false;
        _frameRate = 0;
        _metadataFrameRate = 0;
        _ptsCalibrationCount = 0;
        _firstCalibrationPtsTicks = 0;
        _lastCalibrationPtsTicks = 0;
        _currentPosition = TimeSpan.Zero;
        _currentFilePath = null;
        _needsConvert = false;
        _currentVideoBufferIndex = 0;
    }

    // ── Private: Helpers ────────────────────────────────────────────────────

    private static int CalculateFrameBufferSize(int width, int height, bool isHdr)
    {
        ValidateVideoDimensions(width, height);
        var pixels = (long)width * height;
        var bytes = isHdr ? pixels * 3 : pixels + pixels / 2;
        if (bytes <= 0 || bytes > MaxDecodedVideoFrameBytes || bytes > int.MaxValue)
        {
            throw CreateException($"Invalid decoded video frame size: {bytes} bytes for {width}x{height} hdr={isHdr}.");
        }

        return (int)bytes;
    }

    private static void ValidateVideoDimensions(int width, int height)
    {
        if (width <= 0 ||
            height <= 0 ||
            width > MaxDecodedVideoDimension ||
            height > MaxDecodedVideoDimension ||
            (width & 1) != 0 ||
            (height & 1) != 0)
        {
            throw CreateException($"Invalid video dimensions: {width}x{height}.");
        }
    }

    private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)
    {
        streamCount = 0;
        if (formatCtx == null)
        {
            failureMessage = "input context was not available.";
            return false;
        }

        var nativeStreamCount = formatCtx->nb_streams;
        if (nativeStreamCount == 0)
        {
            failureMessage = "input had no streams.";
            return false;
        }

        if (nativeStreamCount > MaxSupportedInputStreams)
        {
            failureMessage = $"input stream count {nativeStreamCount} exceeds supported maximum {MaxSupportedInputStreams}.";
            return false;
        }

        streamCount = (int)nativeStreamCount;
        failureMessage = string.Empty;
        return true;
    }

    private static bool IsValidStreamIndex(int streamIndex, int streamCount)
        => streamIndex >= 0 && streamIndex < streamCount;

    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("FlashbackDecoder has not been initialized. Call Initialize() first.");
        }
    }

    private void ThrowIfNotOpen()
    {
        ThrowIfDisposed();
        if (!_isOpen)
        {
            throw new InvalidOperationException("No file is open. Call OpenFile() first.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"FLASHBACK_DECODER_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException(
            $"FLASHBACK_DECODER_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private static InvalidOperationException CreateException(string message)
    {
        Logger.Log($"FLASHBACK_DECODER_ERROR {message}");
        return new InvalidOperationException($"FLASHBACK_DECODER_ERROR {message}");
    }

    /// <summary>
    /// Frees the cloned AVFrame held by a D3D11VA <see cref="DecodedVideoFrame"/>,
    /// releasing the D3D11VA surface reference back to the decoder pool.
    /// Safe to call on software frames (no-op when <see cref="DecodedVideoFrame.HeldFrame"/> is zero).
    /// </summary>
    internal static void ReleaseHeldFrame(DecodedVideoFrame frame)
    {
        if (frame.HeldFrame != IntPtr.Zero)
        {
            var heldFrame = (AVFrame*)frame.HeldFrame;
            ffmpeg.av_frame_free(&heldFrame);
        }
    }

    private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)
    {
        try
        {
            ReleaseHeldFrame(frame);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
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

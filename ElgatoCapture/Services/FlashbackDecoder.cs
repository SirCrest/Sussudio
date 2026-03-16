using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using FFmpeg.AutoGen;

namespace ElgatoCapture.Services;

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

    // Audio output buffer (reused per decode call)
    private byte[]? _audioOutputBuffer;
    private int _audioOutputBufferSize;

    // Cross-stream packet stash: when reading video packets we may encounter audio
    // packets (and vice versa). Stash them here instead of discarding, since
    // av_read_frame is forward-only and discarding loses frames permanently.
    private readonly Queue<IntPtr> _pendingVideoPackets = new();
    private readonly Queue<IntPtr> _pendingAudioPackets = new();

    private bool _isOpen;
    private bool _disposed;
    private bool _initialized;
    private string? _currentFilePath;

    // Video info (populated after OpenFile)
    private int _videoWidth;
    private int _videoHeight;
    private bool _isHdr;
    private double _frameRate;
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
    private static readonly AVCodecContext_get_format _getFormatD3D11 = GetFormatD3D11;

    private static AVPixelFormat GetFormatD3D11(AVCodecContext* ctx, AVPixelFormat* fmt)
    {
        for (var p = fmt; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
        {
            if (*p == AVPixelFormat.AV_PIX_FMT_D3D11)
                return AVPixelFormat.AV_PIX_FMT_D3D11;
        }
        // D3D11 not offered — return first format (software fallback)
        return *fmt;
    }

    public bool IsOpen => _isOpen;
    public int VideoWidth => _videoWidth;
    public int VideoHeight => _videoHeight;
    public bool IsHdr => _isHdr;
    public double FrameRate => _frameRate;
    public TimeSpan CurrentPosition => _currentPosition;
    public bool IsD3D11HwAccelerated => _isD3D11HwAccelerated;

    /// <summary>
    /// Initializes the decoder with D3D11 device pointers for GPU-direct decode.
    /// Must be called before <see cref="OpenFile"/>.
    /// </summary>
    public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)
    {
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
                Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=false reason=exception msg='{ex.Message}'");
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

            ThrowIfError(
                ffmpeg.avformat_find_stream_info(_formatCtx, null),
                "avformat_find_stream_info");

            // Find video stream
            _videoStreamIndex = ffmpeg.av_find_best_stream(
                _formatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            if (_videoStreamIndex < 0)
            {
                throw CreateException("No video stream found in file.");
            }

            // Find audio stream (optional)
            _audioStreamIndex = ffmpeg.av_find_best_stream(
                _formatCtx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);

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
        catch
        {
            // Clean up on failure
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

        CloseFileCore();
        Logger.Log($"FLASHBACK_DECODER_CLOSE path='{_currentFilePath}'");
        _currentFilePath = null;
    }

    /// <summary>
    /// Seeks to the nearest keyframe at or before <paramref name="target"/>.
    /// Fast seek suitable for scrubbing.
    /// </summary>
    public bool SeekToKeyframe(TimeSpan target)
    {
        ThrowIfNotOpen();

        var timestampUs = (long)(target.TotalSeconds * ffmpeg.AV_TIME_BASE);
        var result = ffmpeg.av_seek_frame(
            _formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);

        if (result < 0)
        {
            Logger.Log($"FLASHBACK_DECODER_SEEK_WARN keyframe_seek_failed code={result} target_ms={(long)target.TotalMilliseconds}");
            return false;
        }

        // Flush stashed cross-stream packets — they're from before the seek point
        while (_pendingVideoPackets.Count > 0)
        {
            var pkt = (AVPacket*)_pendingVideoPackets.Dequeue();
            ffmpeg.av_packet_free(&pkt);
        }
        while (_pendingAudioPackets.Count > 0)
        {
            var pkt = (AVPacket*)_pendingAudioPackets.Dequeue();
            ffmpeg.av_packet_free(&pkt);
        }

        if (_videoCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_videoCodecCtx);
        }

        if (_audioCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_audioCodecCtx);
        }

        _currentPosition = target;
        Logger.Log($"FLASHBACK_DECODER_SEEK_OK target_ms={(long)target.TotalMilliseconds}");
        return true;
    }

    /// <summary>
    /// Seeks to the exact frame at <paramref name="target"/> by first seeking to the
    /// nearest preceding keyframe, then decoding forward until the target PTS is reached.
    /// </summary>
    public bool SeekTo(TimeSpan target)
    {
        ThrowIfNotOpen();

        if (!SeekToKeyframe(target))
        {
            return false;
        }

        // Decode forward until we reach (or pass) the target PTS
        var targetTicks = target.Ticks;
        while (true)
        {
            if (!TryDecodeNextVideoFrame(out var frame))
            {
                // Reached end before target — position at last decoded frame
                return true;
            }

            if (frame.Pts.Ticks >= targetTicks)
            {
                _currentPosition = frame.Pts;
                return true;
            }
        }
    }

    /// <summary>
    /// Decodes the next video frame.
    /// For D3D11VA: returns a <see cref="DecodedVideoFrame"/> with <see cref="DecodedVideoFrame.IsD3D11Texture"/> = true.
    /// For software: returns raw NV12/P010 data in <see cref="DecodedVideoFrame.Data"/>.
    /// </summary>
    public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame)
    {
        frame = default;
        ThrowIfNotOpen();

        while (true)
        {
            // First try to receive a frame from the decoder (may have buffered frames)
            var receiveResult = ffmpeg.avcodec_receive_frame(_videoCodecCtx, _videoFrame);
            if (receiveResult == 0)
            {
                // Got a decoded frame — convert and return
                frame = ConvertAndOutputVideoFrame();
                return true;
            }

            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                // Decoder needs more packets
                if (!FeedNextVideoPacket())
                {
                    // No more packets — try to drain
                    var sendResult = ffmpeg.avcodec_send_packet(_videoCodecCtx, null);
                    if (sendResult < 0)
                    {
                        return false;
                    }

                    receiveResult = ffmpeg.avcodec_receive_frame(_videoCodecCtx, _videoFrame);
                    if (receiveResult == 0)
                    {
                        frame = ConvertAndOutputVideoFrame();
                        return true;
                    }

                    return false;
                }

                continue;
            }

            if (receiveResult == ffmpeg.AVERROR_EOF)
            {
                return false;
            }

            // Unexpected error
            Logger.Log($"FLASHBACK_DECODER_VIDEO_ERROR receive_frame code={receiveResult}");
            return false;
        }
    }

    /// <summary>
    /// Decodes the next audio chunk from the file.
    /// Returns f32le interleaved stereo 48kHz samples.
    /// </summary>
    public bool TryDecodeNextAudioChunk(out DecodedAudioChunk chunk)
    {
        chunk = default;

        if (_audioCodecCtx == null || _audioStreamIndex < 0)
        {
            return false;
        }

        ThrowIfNotOpen();

        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_frame(_audioCodecCtx, _audioFrame);
            if (receiveResult == 0)
            {
                chunk = ConvertAndOutputAudioFrame();
                return true;
            }

            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                if (!FeedNextAudioPacket())
                {
                    var sendResult = ffmpeg.avcodec_send_packet(_audioCodecCtx, null);
                    if (sendResult < 0)
                    {
                        return false;
                    }

                    receiveResult = ffmpeg.avcodec_receive_frame(_audioCodecCtx, _audioFrame);
                    if (receiveResult == 0)
                    {
                        chunk = ConvertAndOutputAudioFrame();
                        return true;
                    }

                    return false;
                }

                continue;
            }

            if (receiveResult == ffmpeg.AVERROR_EOF)
            {
                return false;
            }

            Logger.Log($"FLASHBACK_DECODER_AUDIO_ERROR receive_frame code={receiveResult}");
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
        var videoStream = _formatCtx->streams[_videoStreamIndex];
        _videoTimeBase = videoStream->time_base;

        var codecPar = videoStream->codecpar;
        _videoWidth = codecPar->width;
        _videoHeight = codecPar->height;

        // Determine pixel format and HDR status
        _decodedPixelFormat = (AVPixelFormat)codecPar->format;
        _isHdr = codecPar->codec_id == AVCodecID.AV_CODEC_ID_HEVC &&
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
        var codec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
        if (codec == null)
        {
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=codec_not_found id={codecPar->codec_id}");
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

            decoderCtx->get_format = _getFormatD3D11;
            decoderCtx->extra_hw_frames = 16;

            var openResult = ffmpeg.avcodec_open2(decoderCtx, codec, null);
            if (openResult < 0)
            {
                Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=open_fail code={openResult}");
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
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_SKIP reason=exception msg='{ex.Message}'");
        }

    cleanup:
        if (decoderCtx != null) ffmpeg.avcodec_free_context(&decoderCtx);
        return false;
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
    /// Audio packets encountered along the way are stashed for later audio decode.
    /// </summary>
    private bool FeedNextVideoPacket()
    {
        // Drain stashed video packets first (put there by FeedNextAudioPacket)
        while (_pendingVideoPackets.Count > 0)
        {
            var stashed = (AVPacket*)_pendingVideoPackets.Dequeue();
            var sendResult = ffmpeg.avcodec_send_packet(_videoCodecCtx, stashed);
            ffmpeg.av_packet_free(&stashed);
            if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN send_stashed code={sendResult}");
                continue;
            }
            return true;
        }

        while (true)
        {
            ffmpeg.av_packet_unref(_packet);
            var readResult = ffmpeg.av_read_frame(_formatCtx, _packet);
            if (readResult < 0)
            {
                return false; // EOF or error
            }

            if (_packet->stream_index == _videoStreamIndex)
            {
                var sendResult = ffmpeg.avcodec_send_packet(_videoCodecCtx, _packet);
                ffmpeg.av_packet_unref(_packet);
                if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN send_packet code={sendResult}");
                    continue;
                }

                return true;
            }

            // Stash audio packets instead of discarding
            if (_packet->stream_index == _audioStreamIndex)
            {
                var clone = ffmpeg.av_packet_clone(_packet);
                if (clone != null) _pendingAudioPackets.Enqueue((IntPtr)clone);
            }

            ffmpeg.av_packet_unref(_packet);
        }
    }

    /// <summary>
    /// Reads packets until an audio packet is sent to the decoder.
    /// Video packets encountered along the way are stashed for later video decode.
    /// </summary>
    private bool FeedNextAudioPacket()
    {
        // Drain stashed audio packets first (put there by FeedNextVideoPacket)
        while (_pendingAudioPackets.Count > 0)
        {
            var stashed = (AVPacket*)_pendingAudioPackets.Dequeue();
            var sendResult = ffmpeg.avcodec_send_packet(_audioCodecCtx, stashed);
            ffmpeg.av_packet_free(&stashed);
            if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN send_stashed code={sendResult}");
                continue;
            }
            return true;
        }

        while (true)
        {
            ffmpeg.av_packet_unref(_packet);
            var readResult = ffmpeg.av_read_frame(_formatCtx, _packet);
            if (readResult < 0)
            {
                return false;
            }

            if (_packet->stream_index == _audioStreamIndex)
            {
                var sendResult = ffmpeg.avcodec_send_packet(_audioCodecCtx, _packet);
                ffmpeg.av_packet_unref(_packet);
                if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN send_packet code={sendResult}");
                    continue;
                }

                return true;
            }

            // Stash video packets instead of discarding
            if (_packet->stream_index == _videoStreamIndex)
            {
                var clone = ffmpeg.av_packet_clone(_packet);
                if (clone != null) _pendingVideoPackets.Enqueue((IntPtr)clone);
            }

            ffmpeg.av_packet_unref(_packet);
        }
    }

    // ── Private: Frame Conversion ───────────────────────────────────────────

    private DecodedVideoFrame ConvertAndOutputVideoFrame()
    {
        // Calculate PTS first (used by both paths)
        var pts = TimeSpan.Zero;
        if (_videoFrame->pts != ffmpeg.AV_NOPTS_VALUE && _videoTimeBase.den > 0)
        {
            var seconds = (double)_videoFrame->pts * _videoTimeBase.num / _videoTimeBase.den;
            pts = TimeSpan.FromSeconds(seconds);
        }

        _currentPosition = pts;

        if (_isD3D11HwAccelerated)
        {
            // D3D11VA path: frame->data[0] is ID3D11Texture2D*, data[1] is subresource index.
            // Don't unref _videoFrame here — avcodec_receive_frame unrefs on next call.
            // SubmitTexture will AddRef the COM texture to keep it alive.
            var texturePtr = (IntPtr)_videoFrame->data[0];
            var subresource = (int)(long)_videoFrame->data[1];

            return new DecodedVideoFrame
            {
                TexturePtr = texturePtr,
                SubresourceIndex = subresource,
                Width = _videoWidth,
                Height = _videoHeight,
                IsHdr = _isHdr,
                Pts = pts,
                IsD3D11Texture = true
            };
        }

        // Software decode path
        var targetFormat = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
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

        ffmpeg.av_frame_unref(_videoFrame);

        return new DecodedVideoFrame
        {
            Data = dataPtr,
            DataLength = outputSize,
            Width = _videoWidth,
            Height = _videoHeight,
            IsHdr = _isHdr,
            Pts = pts,
            IsD3D11Texture = false
        };
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

    // ── Private: Audio Conversion ───────────────────────────────────────────

    private DecodedAudioChunk ConvertAndOutputAudioFrame()
    {
        var inputSamples = _audioFrame->nb_samples;
        var maxOutputSamples = ffmpeg.swr_get_out_samples(_swrCtx, inputSamples);
        if (maxOutputSamples < 0)
        {
            maxOutputSamples = inputSamples * 2;
        }

        var outputBytesNeeded = maxOutputSamples * OutputAudioChannels * sizeof(float);
        if (_audioOutputBuffer == null || _audioOutputBufferSize < outputBytesNeeded)
        {
            if (_audioOutputBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_audioOutputBuffer);
            }

            _audioOutputBuffer = ArrayPool<byte>.Shared.Rent(outputBytesNeeded);
            _audioOutputBufferSize = outputBytesNeeded;
        }

        int outputSamplesProduced;
        fixed (byte* outputPtr = _audioOutputBuffer)
        {
            var outputPlanes = stackalloc byte*[1];
            outputPlanes[0] = outputPtr;

            outputSamplesProduced = ffmpeg.swr_convert(
                _swrCtx,
                outputPlanes, maxOutputSamples,
                _audioFrame->extended_data, inputSamples);
        }

        var pts = TimeSpan.Zero;
        if (_audioFrame->pts != ffmpeg.AV_NOPTS_VALUE && _audioTimeBase.den > 0)
        {
            var seconds = (double)_audioFrame->pts * _audioTimeBase.num / _audioTimeBase.den;
            pts = TimeSpan.FromSeconds(seconds);
        }

        ffmpeg.av_frame_unref(_audioFrame);

        if (outputSamplesProduced <= 0)
        {
            return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
        }

        var validBytes = outputSamplesProduced * OutputAudioChannels * sizeof(float);
        var result = ArrayPool<byte>.Shared.Rent(validBytes);
        Buffer.BlockCopy(_audioOutputBuffer, 0, result, 0, validBytes);

        return new DecodedAudioChunk
        {
            Samples = result,
            ValidLength = validBytes,
            Pts = pts
        };
    }

    // ── Private: Cleanup ────────────────────────────────────────────────────

    private void CloseFileCore()
    {
        Logger.Log($"FLASHBACK_DECODER_CLOSE_CORE path='{_currentFilePath}' had_swr={_swrCtx != null} had_video={_videoCodecCtx != null} had_audio={_audioCodecCtx != null}");
        _isOpen = false;

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

        _isD3D11HwAccelerated = false;
        // Note: do NOT free _d3d11HwDeviceCtx here — it's persistent across files

        if (_swrCtx != null)
        {
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

        if (_audioOutputBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_audioOutputBuffer);
            _audioOutputBuffer = null;
            _audioOutputBufferSize = 0;
        }

        // Free any stashed cross-stream packets
        while (_pendingVideoPackets.Count > 0)
        {
            var pkt = (AVPacket*)_pendingVideoPackets.Dequeue();
            ffmpeg.av_packet_free(&pkt);
        }
        while (_pendingAudioPackets.Count > 0)
        {
            var pkt = (AVPacket*)_pendingAudioPackets.Dequeue();
            ffmpeg.av_packet_free(&pkt);
        }

        _videoStreamIndex = -1;
        _audioStreamIndex = -1;
        _videoWidth = 0;
        _videoHeight = 0;
        _isHdr = false;
        _frameRate = 0;
        _currentPosition = TimeSpan.Zero;
        _needsConvert = false;
        _currentVideoBufferIndex = 0;
    }

    // ── Private: Helpers ────────────────────────────────────────────────────

    private static int CalculateFrameBufferSize(int width, int height, bool isHdr)
    {
        if (isHdr)
        {
            // P010: Y plane (w*h*2) + UV plane (w*(h/2)*2)
            return width * height * 2 + width * (height / 2) * 2;
        }

        // NV12: Y plane (w*h) + UV plane (w*(h/2))
        return width * height + width * (height / 2);
    }

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

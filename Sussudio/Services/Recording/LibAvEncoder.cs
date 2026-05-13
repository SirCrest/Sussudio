using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

/// <summary>
/// In-process libav encoder for MP4 recording.
/// This type is not thread-safe; all libav calls must be serialized onto one thread.
/// </summary>
internal sealed unsafe partial class LibAvEncoder : IDisposable
{
    /// <summary>Forwards to <see cref="FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs"/>.</summary>
    internal static IDisposable SuppressRecoverableSeekFfmpegLogs()
        => FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs();

    /// <summary>
    /// State for a single AAC audio stream (audio-in or microphone).
    /// All pointer-typed fields are valid only while the encoder is open.
    /// </summary>
    private unsafe struct AudioStreamState
    {
        public AVCodecContext* CodecCtx;
        public AVStream* Stream;
        public AVFrame* Frame;
        public SwrContext* SwrCtx;
        public int FrameSize;
        /// <summary>Capacity of <see cref="ResampleBuffer"/> in bytes.</summary>
        public int AccumulatorCapacity;
        /// <summary>Interleaved-float accumulator for partial input frames.</summary>
        public byte* ResampleBuffer;
        /// <summary>Capacity of <see cref="SampleQueueBuffer"/> in samples per channel.</summary>
        public int SampleQueueCapacity;
        /// <summary>Planar-float sample queue awaiting encoding.</summary>
        public byte* SampleQueueBuffer;
        /// <summary>Number of valid samples currently in <see cref="SampleQueueBuffer"/>.</summary>
        public int BufferedSamples;
        /// <summary>Number of bytes currently in <see cref="ResampleBuffer"/>.</summary>
        public int AccumulatorBytes;
        /// <summary>Running PTS counter (in samples) for this stream.</summary>
        public long NextPts;
        /// <summary>Cached copy of <see cref="AVCodecContext.time_base"/> set at open time.</summary>
        public AVRational CachedTimeBase;
    }

    private AVFormatContext* _formatCtx;
    private AVCodecContext* _videoCodecCtx;
    private AVStream* _videoStream;
    private AVFrame* _videoFrame;
    private AVPacket* _packet;
    private AVBSFContext* _bsfCtx;
    private LibAvEncoderOptions? _options;
    private long _nextVideoPts;
    private long _encodedFrameCount;
    private long _droppedFrameCount;
    private long _videoPacketsWritten;
    private long _audioSamplesReceived;
    private long _micSamplesReceived;
    private long _totalBytesWritten;
    private AudioStreamState _audio;
    private AudioStreamState _mic;
    private bool _isOpen;
    private bool _headerWritten;
    private AVRational _cachedVideoTimeBase;
    private bool _flushSent;
    private AVBufferRef* _hwDeviceCtx;
    private AVBufferRef* _hwFramesCtx;
    private AVFrame* _hwFrame;
    private bool _useHardwareFrames;
    private bool _useCudaHardwareFrames;
    private volatile bool _forceNextKeyframe;
    private IntPtr[]? _hwPoolTextures; // individual ArraySize=1 D3D11 textures for the hw frames pool
    private int _hwPoolIndex; // round-robin index into _hwPoolTextures

    /// <summary>No-op free callback for av_buffer_create — our pool textures outlive individual frames.</summary>
    private static readonly av_buffer_create_free HwPoolTextureFreeDelegate = (opaque, data) => { /* intentional no-op */ };
    private static readonly av_buffer_create_free_func HwPoolTextureFree = HwPoolTextureFreeDelegate;

    public long EncodedFrameCount => _encodedFrameCount;
    public long DroppedFrameCount => _droppedFrameCount;
    public long VideoPacketsWritten => Interlocked.Read(ref _videoPacketsWritten);
    public long AudioSamplesReceived => _audioSamplesReceived;
    public long MicrophoneSamplesReceived => _micSamplesReceived;
    public long TotalBytesWritten => _totalBytesWritten;
    public bool IsEncoding => _isOpen;
    public bool AudioEnabled => _options?.AudioEnabled == true && _audio.CodecCtx != null && _audio.Stream != null;
    public bool MicrophoneEnabled => _options?.MicrophoneEnabled == true && _mic.CodecCtx != null && _mic.Stream != null;
    public string VideoCodecName => _options?.CodecName ?? string.Empty;
    public string OutputPath => _options?.OutputPath ?? string.Empty;
    public bool UseHardwareFrames => _useHardwareFrames;
    public bool UseCudaHardwareFrames => _useCudaHardwareFrames;
    public long NextVideoPts => _nextVideoPts;

    /// <summary>
    /// Sets initial PTS counters for video (frame units) and audio (sample units).
    /// Used when continuing encoding after a sink-only cycle so file-level
    /// timestamps continue from the previous session.
    /// Must be called after <see cref="Initialize"/> and before encoding any frames.
    /// </summary>
    public void SetInitialPts(long videoPts, long audioPts)
    {
        Interlocked.Exchange(ref _nextVideoPts, videoPts);
        Interlocked.Exchange(ref _audio.NextPts, audioPts);
        Interlocked.Exchange(ref _mic.NextPts, audioPts);
    }

    public void SkipVideoFrame() { Interlocked.Increment(ref _nextVideoPts); }

    /// <summary>Forwards to <see cref="FfmpegRuntimeInit.EnsureInitialized"/>.</summary>
    public static void InitializeFFmpeg(bool requireNativeRuntime = false)
        => FfmpegRuntimeInit.EnsureInitialized(requireNativeRuntime);

    public void Initialize(LibAvEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_isOpen || _formatCtx != null || _videoCodecCtx != null)
        {
            throw new InvalidOperationException("LibAvEncoder is already initialized.");
        }

        ValidateOptions(options);
        _options = options;

        try
        {
            var codec = ffmpeg.avcodec_find_encoder_by_name(options.CodecName);
            if (codec == null)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=avcodec_find_encoder_by_name codec='{options.CodecName}' " +
                    "msg=Encoder not available.");
            }

            AVFormatContext* formatCtx = null;
            ThrowIfError(
                ffmpeg.avformat_alloc_output_context2(&formatCtx, null, options.ContainerFormat, options.OutputPath),
                "avformat_alloc_output_context2");
            if (formatCtx == null)
            {
                throw CreateLibAvException(
                    "LIBAV_ENCODER_ERROR operation=avformat_alloc_output_context2 msg=Output context allocation returned null.");
            }

            _formatCtx = formatCtx;

            _videoStream = ffmpeg.avformat_new_stream(_formatCtx, codec);
            if (_videoStream == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream msg=Stream allocation returned null.");
            }

            _videoCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
            if (_videoCodecCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3 msg=Codec context allocation returned null.");
            }

            ConfigureVideoCodecContext(_videoCodecCtx, options);

            // For MP4: SPS/PPS goes in moov atom via extradata (GLOBAL_HEADER).
            // For MPEG-TS: SPS/PPS must be inline with every IDR so each segment
            // is independently decodable after segment rotation.
            if (options.ContainerFormat != "mpegts" &&
                (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                _videoCodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            ApplyEncoderPrivateOptions(_videoCodecCtx, options);
            InitializeHardwareFramesIfNeeded(options);

            ThrowIfError(ffmpeg.avcodec_open2(_videoCodecCtx, codec, null), "avcodec_open2");

            // After avcodec_open2, NVENC creates its own hw_frames_ctx if we only provided hw_device_ctx.
            // Grab the reference so we can use av_hwframe_get_buffer later.
            if (_useHardwareFrames &&
                !_useCudaHardwareFrames &&
                _hwFramesCtx == null &&
                _videoCodecCtx->hw_frames_ctx != null)
            {
                _hwFramesCtx = ffmpeg.av_buffer_ref(_videoCodecCtx->hw_frames_ctx);
                if (_hwFramesCtx == null)
                {
                    Logger.Log("LIBAV_ENCODER_HW_FRAMES_WARN stage=post_open2_frames_ref msg='Failed to ref codec hw_frames_ctx' fallback=software");
                    _useHardwareFrames = false;
                    _videoCodecCtx->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
                }
                else
                {
                    Logger.Log("LIBAV_ENCODER_HW_FRAMES mode=d3d11va_nvenc_managed frames_ctx_from_codec=true");
                }
            }
            else if (_useHardwareFrames &&
                     !_useCudaHardwareFrames &&
                     _videoCodecCtx->hw_frames_ctx == null)
            {
                Logger.Log("LIBAV_ENCODER_HW_FRAMES_WARN stage=post_open2 msg='NVENC did not create hw_frames_ctx' fallback=software");
                _useHardwareFrames = false;
                _videoCodecCtx->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            }

            _videoStream->time_base = _videoCodecCtx->time_base;
            _cachedVideoTimeBase = _videoCodecCtx->time_base;
            _videoStream->avg_frame_rate = _videoCodecCtx->framerate;
            _videoStream->r_frame_rate = _videoCodecCtx->framerate;

            ThrowIfError(
                ffmpeg.avcodec_parameters_from_context(_videoStream->codecpar, _videoCodecCtx),
                "avcodec_parameters_from_context");

            InitializeVideoBitstreamFilterIfNeeded(options);
            InitializeAudioIfNeeded(options);
            InitializeMicrophoneIfNeeded(options);

            ThrowIfError(ffmpeg.avio_open2(&_formatCtx->pb, options.OutputPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2");

            AVDictionary* muxerOptions = null;
            try
            {
                ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, "open");
                ThrowIfError(ffmpeg.avformat_write_header(_formatCtx, &muxerOptions), "avformat_write_header");
                _headerWritten = true;
            }
            finally
            {
                ffmpeg.av_dict_free(&muxerOptions);
            }

            if (_useHardwareFrames)
            {
                _hwFrame = ffmpeg.av_frame_alloc();
                if (_hwFrame == null)
                {
                    throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(hw) msg=Hardware frame allocation returned null.");
                }

                _hwFrame->format = (int)(_useCudaHardwareFrames ? AVPixelFormat.AV_PIX_FMT_CUDA : AVPixelFormat.AV_PIX_FMT_D3D11);
                _hwFrame->width = options.Width;
                _hwFrame->height = options.Height;

                _videoFrame = ffmpeg.av_frame_alloc();
                if (_videoFrame == null)
                {
                    throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc msg=Frame allocation returned null.");
                }

                var swFormat = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
                _videoFrame->format = (int)swFormat;
                _videoFrame->width = options.Width;
                _videoFrame->height = options.Height;
                ThrowIfError(ffmpeg.av_frame_get_buffer(_videoFrame, 32), "av_frame_get_buffer(sw)");
            }
            else
            {
                _videoFrame = ffmpeg.av_frame_alloc();
                if (_videoFrame == null)
                {
                    throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc msg=Frame allocation returned null.");
                }

                _videoFrame->format = (int)_videoCodecCtx->pix_fmt;
                _videoFrame->width = options.Width;
                _videoFrame->height = options.Height;
                ThrowIfError(ffmpeg.av_frame_get_buffer(_videoFrame, 32), "av_frame_get_buffer");
            }

            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_packet_alloc msg=Packet allocation returned null.");
            }

            _nextVideoPts = 0;
            _audio.NextPts = 0;
            _mic.NextPts = 0;
            _encodedFrameCount = 0;
            _droppedFrameCount = 0;
            _videoPacketsWritten = 0;
            _audioSamplesReceived = 0;
            _micSamplesReceived = 0;
            _lastSyncLogVideoFrame = 0;
            _driftCorrectionAppliedSamples = 0;
            _lastDriftCorrectionVideoFrame = 0;
            _audio.AccumulatorBytes = 0;
            _audio.BufferedSamples = 0;
            _mic.AccumulatorBytes = 0;
            _mic.BufferedSamples = 0;
            _flushSent = false;
            _isOpen = true;

            Logger.Log(
                $"LIBAV_ENCODER_OPEN codec='{options.CodecName}' output='{options.OutputPath}' " +
                $"width={options.Width} height={options.Height} fps={options.FrameRate.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"bitrate={options.BitRate} pix_fmt='{(options.IsP010 ? "p010le" : "nv12")}' hdr={options.HdrEnabled} split_encode='{options.SplitEncodeMode}' " +
                $"audio={options.AudioEnabled} audio_rate={options.AudioSampleRate} audio_channels={options.AudioChannels} audio_bitrate={options.AudioBitRate} " +
                $"microphone={options.MicrophoneEnabled} mic_rate={options.MicrophoneSampleRate} mic_channels={options.MicrophoneChannels} mic_bitrate={options.MicrophoneBitRate} " +
                $"hw_frames={_useHardwareFrames}");
        }
        catch
        {
            CleanupResources(writeTrailer: false);
            throw;
        }
    }

    public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)
    {
        EnsureOpen();

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        if (width != options.Width || height != options.Height)
        {
            _droppedFrameCount++;
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendVideoFrame msg=Frame dimensions do not match encoder state width={width} height={height} expected_width={options.Width} expected_height={options.Height}");
        }

        var expectedSize = GetExpectedFrameSizeBytes(options.Width, options.Height, options.IsP010);
        if (frameData.Length < expectedSize)
        {
            _droppedFrameCount++;
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendVideoFrame msg=Frame payload too small actual={frameData.Length} expected={expectedSize}");
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(_videoFrame), "av_frame_make_writable");

        CopyPackedFrameToVideoFrame(frameData[..expectedSize], options);
        if (_forceNextKeyframe)
        {
            _videoFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _forceNextKeyframe = false;
        }

        _videoFrame->pts = Interlocked.Increment(ref _nextVideoPts) - 1;
        LogAvSyncIfDue();

        var attachedHdrSideData = false;
        if (options.HdrEnabled && _encodedFrameCount == 0)
        {
            attachedHdrSideData = AttachHdrFrameSideDataIfNeeded(options);
        }

        try
        {
            var sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _videoFrame);
            if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                DrainEncoderPackets();
                sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _videoFrame);
            }

            ThrowIfError(sendResult, "avcodec_send_frame");

            // Reset pict_type after send so the forced-keyframe flag doesn't stick.
            // _videoFrame is reused across calls and av_frame_make_writable does NOT
            // clear pict_type, so without this every subsequent frame would be I-frame.
            _videoFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_NONE;

            DrainEncoderPackets();
            _encodedFrameCount++;
        }
        catch
        {
            _droppedFrameCount++;
            throw;
        }
        finally
        {
            if (attachedHdrSideData)
            {
                ffmpeg.av_frame_remove_side_data(_videoFrame, AVFrameSideDataType.AV_FRAME_DATA_MASTERING_DISPLAY_METADATA);
                ffmpeg.av_frame_remove_side_data(_videoFrame, AVFrameSideDataType.AV_FRAME_DATA_CONTENT_LIGHT_LEVEL);
            }
        }
    }

    /// <summary>
    /// Encode a D3D11 texture directly on GPU via hardware frames.
    /// The caller must have done AddRef on the texture; this method does NOT AddRef/Release.
    /// The source texture is copied into a pool texture via CopySubresourceRegion on the immediate context.
    /// </summary>
    public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)
    {
        EnsureOpen();

        if (d3d11Texture == IntPtr.Zero)
        {
            throw new ArgumentException("D3D11 texture pointer is null.", nameof(d3d11Texture));
        }

        if (!_useHardwareFrames || _useCudaHardwareFrames || _hwFramesCtx == null || _hwFrame == null || _hwPoolTextures == null)
        {
            throw new InvalidOperationException("Hardware frames are not initialized. Use SendVideoFrame for CPU path.");
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");

        // Pick a pool texture via round-robin. With 8 textures and NVENC pipeline depth
        // of ~4 frames, we never wrap around while the encoder still references a texture.
        var poolTextures = _hwPoolTextures;
        var poolTexture = poolTextures[_hwPoolIndex & (poolTextures.Length - 1)];
        _hwPoolIndex++;

        // GPU-GPU copy: source reader texture → our pool texture (ArraySize=1, subresource=0)
        var deviceCtx = (void*)options.D3D11DeviceContextPtr;
        if (deviceCtx == null)
        {
            throw new InvalidOperationException("D3D11 device context is null.");
        }

        var vtable = *(void***)deviceCtx;
        var copySubresourceRegion = (delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, uint, void*, uint, void*, void>)vtable[46];
        copySubresourceRegion(
            deviceCtx,
            (void*)poolTexture,
            0, // destination subresource = 0 (ArraySize=1)
            0, 0, 0,
            (void*)d3d11Texture,
            (uint)subresourceIndex,
            null);

        // CopySubresourceRegion is void — after a TDR (GPU device-removed), the call
        // silently no-ops and subsequent frames encode from stale/garbage texture data.
        // Proactively check device health to fail fast rather than corrupt the recording.
        CheckDeviceRemoved(options.D3D11DevicePtr);

        // Construct the AVFrame manually (bypass FFmpeg's pool which doesn't support
        // individual textures). The pool texture outlives the frame, so use no-op free.
        ffmpeg.av_frame_unref(_hwFrame);
        _hwFrame->format = (int)AVPixelFormat.AV_PIX_FMT_D3D11;
        _hwFrame->width = options.Width;
        _hwFrame->height = options.Height;
        _hwFrame->hw_frames_ctx = ffmpeg.av_buffer_ref(_hwFramesCtx);
        _hwFrame->data[0] = (byte*)poolTexture;
        _hwFrame->data[1] = (byte*)(nint)0; // subresource index = 0
        // Create a buffer ref with no-op free so av_frame_unref doesn't release our pool texture
        _hwFrame->buf[0] = ffmpeg.av_buffer_create(
            (byte*)poolTexture, 0, HwPoolTextureFree, null, 0);

        if (_forceNextKeyframe)
        {
            _hwFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _forceNextKeyframe = false;
        }

        _hwFrame->pts = Interlocked.Increment(ref _nextVideoPts) - 1;
        LogAvSyncIfDue();

        var attachedHdrSideData = false;
        if (options.HdrEnabled && _encodedFrameCount == 0)
        {
            attachedHdrSideData = AttachHdrFrameSideDataToHwFrame(options);
        }

        try
        {
            var sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                DrainEncoderPackets();
                sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            }

            ThrowIfError(sendResult, "avcodec_send_frame(hw)");
            DrainEncoderPackets();
            _encodedFrameCount++;
        }
        catch
        {
            _droppedFrameCount++;
            throw;
        }
        finally
        {
            if (attachedHdrSideData)
            {
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_MASTERING_DISPLAY_METADATA);
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_CONTENT_LIGHT_LEVEL);
            }

            ffmpeg.av_frame_unref(_hwFrame);
        }
    }

    /// <summary>
    /// Encode a CUDA-resident decoded frame directly.
    /// Zero-copy is preserved when decoder and encoder share the same hw_frames_ctx.
    /// </summary>
    public void SendCudaVideoFrame(AVFrame* decodedFrame)
    {
        EnsureOpen();

        if (!_useCudaHardwareFrames || _hwFrame == null)
        {
            throw new InvalidOperationException("CUDA hardware frames are not initialized.");
        }

        if (decodedFrame == null)
        {
            throw new ArgumentNullException(nameof(decodedFrame));
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");

        ffmpeg.av_frame_unref(_hwFrame);
        var refResult = ffmpeg.av_frame_ref(_hwFrame, decodedFrame);
        if (refResult < 0)
        {
            throw new InvalidOperationException($"av_frame_ref(cuda) failed: code={refResult} msg='{GetErrorString(refResult)}'");
        }

        if (_forceNextKeyframe)
        {
            _hwFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _forceNextKeyframe = false;
        }

        _hwFrame->pts = Interlocked.Increment(ref _nextVideoPts) - 1;
        LogAvSyncIfDue();

        var attachedHdrSideData = false;
        if (options.HdrEnabled && _encodedFrameCount == 0)
        {
            attachedHdrSideData = AttachHdrFrameSideDataToHwFrame(options);
        }

        try
        {
            var sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                DrainEncoderPackets();
                sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            }

            ThrowIfError(sendResult, "avcodec_send_frame(cuda)");
            DrainEncoderPackets();
            _encodedFrameCount++;
        }
        catch
        {
            _droppedFrameCount++;
            throw;
        }
        finally
        {
            if (attachedHdrSideData)
            {
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_MASTERING_DISPLAY_METADATA);
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_CONTENT_LIGHT_LEVEL);
            }

            ffmpeg.av_frame_unref(_hwFrame);
        }
    }

    public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_audio.CodecCtx == null || _audio.Stream == null || _audio.Frame == null || _audio.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(options.AudioChannels * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendAudioSamples msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        _audioSamplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(_audio.FrameSize * inputBlockAlign);

        if (_audio.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _audio.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref _audio, remaining[..copyBytes], _audio.AccumulatorBytes);
            _audio.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_audio.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref _audio, _audio.ResampleBuffer, _audio.FrameSize,
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);
                _audio.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref _audio, inputPtr, _audio.FrameSize,
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref _audio, remaining, 0);
            _audio.AccumulatorBytes = remaining.Length;
        }
    }

    public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_mic.CodecCtx == null || _mic.Stream == null || _mic.Frame == null || _mic.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(options.MicrophoneChannels * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendMicrophoneSamples msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        _micSamplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(_mic.FrameSize * inputBlockAlign);

        if (_mic.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _mic.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref _mic, remaining[..copyBytes], _mic.AccumulatorBytes);
            _mic.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_mic.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref _mic, _mic.ResampleBuffer, _mic.FrameSize,
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);
                _mic.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref _mic, inputPtr, _mic.FrameSize,
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref _mic, remaining, 0);
            _mic.AccumulatorBytes = remaining.Length;
        }
    }

    public RotateOutputResult RotateOutput(string newPath)
    {
        EnsureOpen();

        if (string.IsNullOrWhiteSpace(newPath))
        {
            throw new ArgumentException("New output path is required.", nameof(newPath));
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var previousPath = options.OutputPath;
        var previousEncodedFrames = _encodedFrameCount;

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(newPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (_audio.CodecCtx != null)
        {
            // CRITICAL: Do NOT call FlushPendingAudioSamples() here.
            // It uses flushPartialFrame=true, which sends a partial AAC frame
            // (< 1024 samples). The AAC encoder interprets partial frames as
            // "end of stream" and rejects all subsequent frames with EINVAL.
            // Instead, drain only full frames and carry partial samples into
            // the next segment.
            DrainBufferedFrames(ref _audio, flushPartialFrame: false);
            DrainStreamEncoderPackets(ref _audio);
        }

        if (_mic.CodecCtx != null)
        {
            DrainBufferedFrames(ref _mic, flushPartialFrame: false);
            DrainStreamEncoderPackets(ref _mic);
        }

        DrainEncoderPackets();

        if (_headerWritten && _formatCtx != null)
        {
            ThrowIfError(ffmpeg.av_write_trailer(_formatCtx), "av_write_trailer(rotate)");
        }

        // Capture total bytes AFTER drains and trailer so the count includes
        // all bytes flushed to the completed segment, but BEFORE the reset
        // in ReinitializeOutputContext / ResetSegmentRuntimeState.
        var previousTotalBytes = _totalBytesWritten;

        CloseCurrentOutputIo();
        FreeCurrentOutputContext();
        try
        {
            ReinitializeOutputContext(newPath);
        }
        catch (Exception ex)
        {
            _isOpen = false;
            Logger.Log($"LIBAV_ENCODER_ROTATE_FAILED path='{newPath}' error={ex.Message}");
            throw;
        }

        ResetSegmentRuntimeState();
        _options = options with { OutputPath = newPath };

        Logger.Log(
            $"LIBAV_ENCODER_ROTATE old_output='{previousPath}' new_output='{newPath}' frames={previousEncodedFrames} bytes={previousTotalBytes}");
        return new RotateOutputResult(previousPath, previousEncodedFrames, previousTotalBytes);
    }

    public void FlushAndClose()
    {
        if (!_isOpen && _formatCtx == null && _videoCodecCtx == null && _audio.CodecCtx == null && _mic.CodecCtx == null)
        {
            return;
        }

        try
        {
            if (_isOpen && !_flushSent)
            {
                try
                {
                    var flushResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, null);
                    if (flushResult != ffmpeg.AVERROR_EOF)
                    {
                        ThrowIfError(flushResult, "avcodec_send_frame(flush)");
                        _flushSent = true;
                    }

                    DrainEncoderPackets();
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_ENCODER_WARNING video_flush_error msg='{ex.Message}'");
                }
            }

            if (_audio.CodecCtx != null)
            {
                FlushPendingStreamSamples(ref _audio, "audio_flush",
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);

                var flushResult = ffmpeg.avcodec_send_frame(_audio.CodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(audio_flush)");
                }

                DrainStreamEncoderPackets(ref _audio);
            }

            if (_mic.CodecCtx != null)
            {
                FlushPendingStreamSamples(ref _mic, "mic_flush",
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);

                var flushResult = ffmpeg.avcodec_send_frame(_mic.CodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(mic_flush)");
                }

                DrainStreamEncoderPackets(ref _mic);
            }
        }
        finally
        {
            CleanupResources(writeTrailer: true);
        }
    }

    public void Dispose()
    {
        FlushAndClose();
    }

    private void InitializeAudioIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.AudioEnabled)
        {
            return;
        }

        var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC);
        if (codec == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_find_encoder(audio) codec='aac' msg=Encoder not available.");
        }

        _audio.Stream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (_audio.Stream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(audio) msg=Stream allocation returned null.");
        }

        _audio.CodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_audio.CodecCtx == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3(audio) msg=Codec context allocation returned null.");
        }

        ConfigureAudioCodecContext(_audio.CodecCtx, options, codec);

        // Skip GLOBAL_HEADER for MPEG-TS — AAC needs ADTS framing per segment.
        if (options.ContainerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            _audio.CodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(_audio.CodecCtx, codec, null), "avcodec_open2(audio)");

        _audio.FrameSize = _audio.CodecCtx->frame_size;
        if (_audio.FrameSize <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeAudioIfNeeded msg=Unexpected AAC frame size value={_audio.FrameSize}");
        }

        _audio.Stream->time_base = _audio.CodecCtx->time_base;
        _audio.CachedTimeBase = _audio.CodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_audio.Stream->codecpar, _audio.CodecCtx),
            "avcodec_parameters_from_context(audio)");

        InitializeAudioResampler(options);
        AllocateAudioFrame();
        AllocateAudioAccumulator(options);
        AllocateAudioSampleQueue(options);
    }

    private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.MicrophoneEnabled)
        {
            return;
        }

        var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC);
        if (codec == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_find_encoder(mic) codec='aac' msg=Encoder not available.");
        }

        _mic.Stream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (_mic.Stream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(mic) msg=Stream allocation returned null.");
        }

        _mic.CodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_mic.CodecCtx == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3(mic) msg=Codec context allocation returned null.");
        }

        _mic.CodecCtx->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        _mic.CodecCtx->sample_rate = options.MicrophoneSampleRate;
        _mic.CodecCtx->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        _mic.CodecCtx->bit_rate = options.MicrophoneBitRate;
        _mic.CodecCtx->time_base = new AVRational { num = 1, den = options.MicrophoneSampleRate };
        ffmpeg.av_channel_layout_default(&_mic.CodecCtx->ch_layout, options.MicrophoneChannels);

        if (!IsSampleFormatSupported(codec, _mic.CodecCtx->sample_fmt))
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeMicrophoneIfNeeded msg=Requested sample format '{_mic.CodecCtx->sample_fmt}' is not supported by AAC encoder.");
        }

        // Skip GLOBAL_HEADER for MPEG-TS — AAC needs ADTS framing per segment.
        if (options.ContainerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            _mic.CodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(_mic.CodecCtx, codec, null), "avcodec_open2(mic)");

        _mic.FrameSize = _mic.CodecCtx->frame_size;
        if (_mic.FrameSize <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeMicrophoneIfNeeded msg=Unexpected AAC frame size value={_mic.FrameSize}");
        }

        _mic.Stream->time_base = _mic.CodecCtx->time_base;
        _mic.CachedTimeBase = _mic.CodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_mic.Stream->codecpar, _mic.CodecCtx),
            "avcodec_parameters_from_context(mic)");

        AVChannelLayout inputLayout = default;
        ffmpeg.av_channel_layout_default(&inputLayout, options.MicrophoneChannels);
        var swrCtx = _mic.SwrCtx;
        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &_mic.CodecCtx->ch_layout,
                _mic.CodecCtx->sample_fmt,
                _mic.CodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                options.MicrophoneSampleRate,
                0,
                null);
            _mic.SwrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2(mic)");
            if (_mic.SwrCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2(mic) msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(_mic.SwrCtx), "swr_init(mic)");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }

        _mic.Frame = ffmpeg.av_frame_alloc();
        if (_mic.Frame == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(mic) msg=Frame allocation returned null.");
        }

        _mic.Frame->format = (int)_mic.CodecCtx->sample_fmt;
        _mic.Frame->nb_samples = _mic.FrameSize;
        _mic.Frame->sample_rate = _mic.CodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&_mic.Frame->ch_layout, &_mic.CodecCtx->ch_layout), "av_channel_layout_copy(mic_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(_mic.Frame, 0), "av_frame_get_buffer(mic)");
        if (_mic.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_get_buffer(mic) msg=extended_data was null.");
        }

        _mic.AccumulatorCapacity = checked(_mic.FrameSize * options.MicrophoneChannels * sizeof(float));
        _mic.ResampleBuffer = (byte*)ffmpeg.av_malloc((ulong)_mic.AccumulatorCapacity);
        if (_mic.ResampleBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(mic_accumulator) msg=Allocation returned null size={_mic.AccumulatorCapacity}.");
        }

        _mic.SampleQueueCapacity = checked((_mic.FrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(_mic.SampleQueueCapacity * options.MicrophoneChannels * sizeof(float));
        _mic.SampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (_mic.SampleQueueBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(mic_sample_queue) msg=Allocation returned null size={queueBytes}.");
        }
    }

    private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)
    {
        var attached = false;

        if (!string.IsNullOrWhiteSpace(options.HdrMasterDisplayMetadata))
        {
            var masteringMetadata = ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame);
            if (masteringMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_mastering_display_metadata_create_side_data msg=Allocation returned null.");
            }

            HdrMasterDisplayMetadata.Apply(masteringMetadata, options.HdrMasterDisplayMetadata);
            attached = true;
        }

        if (options.HdrMaxCll > 0 && options.HdrMaxFall > 0)
        {
            var lightMetadata = ffmpeg.av_content_light_metadata_create_side_data(_videoFrame);
            if (lightMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_content_light_metadata_create_side_data msg=Allocation returned null.");
            }

            lightMetadata->MaxCLL = (uint)options.HdrMaxCll;
            lightMetadata->MaxFALL = (uint)options.HdrMaxFall;
            attached = true;
        }

        return attached;
    }

    private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)
    {
        var attached = false;

        if (!string.IsNullOrWhiteSpace(options.HdrMasterDisplayMetadata))
        {
            var masteringMetadata = ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame);
            if (masteringMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_mastering_display_metadata_create_side_data(hw) msg=Allocation returned null.");
            }

            HdrMasterDisplayMetadata.Apply(masteringMetadata, options.HdrMasterDisplayMetadata);
            attached = true;
        }

        if (options.HdrMaxCll > 0 && options.HdrMaxFall > 0)
        {
            var lightMetadata = ffmpeg.av_content_light_metadata_create_side_data(_hwFrame);
            if (lightMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_content_light_metadata_create_side_data(hw) msg=Allocation returned null.");
            }

            lightMetadata->MaxCLL = (uint)options.HdrMaxCll;
            lightMetadata->MaxFALL = (uint)options.HdrMaxFall;
            attached = true;
        }

        return attached;
    }

    private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)
    {
        codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        codecContext->sample_rate = options.AudioSampleRate;
        codecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        codecContext->bit_rate = options.AudioBitRate;
        codecContext->time_base = new AVRational { num = 1, den = options.AudioSampleRate };
        ffmpeg.av_channel_layout_default(&codecContext->ch_layout, options.AudioChannels);

        if (!IsSampleFormatSupported(codec, codecContext->sample_fmt))
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=ConfigureAudioCodecContext msg=Requested sample format '{codecContext->sample_fmt}' is not supported by AAC encoder.");
        }
    }

    private void InitializeAudioResampler(LibAvEncoderOptions options)
    {
        AVChannelLayout inputLayout = default;
        ffmpeg.av_channel_layout_default(&inputLayout, options.AudioChannels);
        var swrCtx = _audio.SwrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &_audio.CodecCtx->ch_layout,
                _audio.CodecCtx->sample_fmt,
                _audio.CodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                options.AudioSampleRate,
                0,
                null);
            _audio.SwrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2");
            if (_audio.SwrCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2 msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(_audio.SwrCtx), "swr_init");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }
    }

    private void AllocateAudioFrame()
    {
        _audio.Frame = ffmpeg.av_frame_alloc();
        if (_audio.Frame == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(audio) msg=Frame allocation returned null.");
        }

        _audio.Frame->format = (int)_audio.CodecCtx->sample_fmt;
        _audio.Frame->nb_samples = _audio.FrameSize;
        _audio.Frame->sample_rate = _audio.CodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&_audio.Frame->ch_layout, &_audio.CodecCtx->ch_layout), "av_channel_layout_copy(audio_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(_audio.Frame, 0), "av_frame_get_buffer(audio)");

        if (_audio.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_get_buffer(audio) msg=extended_data was null.");
        }
    }

    private void AllocateAudioAccumulator(LibAvEncoderOptions options)
    {
        _audio.AccumulatorCapacity = checked(_audio.FrameSize * options.AudioChannels * sizeof(float));
        _audio.ResampleBuffer = (byte*)ffmpeg.av_malloc((ulong)_audio.AccumulatorCapacity);
        if (_audio.ResampleBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(audio_accumulator) msg=Allocation returned null size={_audio.AccumulatorCapacity}.");
        }
    }

    private void AllocateAudioSampleQueue(LibAvEncoderOptions options)
    {
        _audio.SampleQueueCapacity = checked((_audio.FrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(_audio.SampleQueueCapacity * options.AudioChannels * sizeof(float));
        _audio.SampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (_audio.SampleQueueBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(audio_sample_queue) msg=Allocation returned null size={queueBytes}.");
        }
    }

    private void DrainEncoderPackets()
    {
        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_packet(_videoCodecCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "avcodec_receive_packet");

            try
            {
                if (_bsfCtx != null)
                {
                    WriteFilteredPackets();
                }
                else
                {
                    WritePacket(_packet, useBsfTimeBase: false);
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void DrainStreamEncoderPackets(ref AudioStreamState s)
    {
        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_packet(s.CodecCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "avcodec_receive_packet(audio)");

            try
            {
                WriteStreamPacket(ref s, _packet);
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void WriteFilteredPackets()
    {
        var sendResult = ffmpeg.av_bsf_send_packet(_bsfCtx, _packet);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainBsfPackets();
            sendResult = ffmpeg.av_bsf_send_packet(_bsfCtx, _packet);
        }

        ThrowIfError(sendResult, "av_bsf_send_packet");

        ffmpeg.av_packet_unref(_packet);
        DrainBsfPackets();
    }

    private void DrainBsfPackets()
    {
        while (true)
        {
            var receiveResult = ffmpeg.av_bsf_receive_packet(_bsfCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "av_bsf_receive_packet");

            try
            {
                WritePacket(_packet, useBsfTimeBase: true);
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void WritePacket(AVPacket* packet, bool useBsfTimeBase)
    {
        var sourceTimeBase = useBsfTimeBase && _bsfCtx != null ? _bsfCtx->time_base_out : _videoCodecCtx->time_base;
        ffmpeg.av_packet_rescale_ts(packet, sourceTimeBase, _videoStream->time_base);
        packet->stream_index = _videoStream->index;
        var packetSize = packet->size;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_formatCtx, packet), "av_interleaved_write_frame");
        Interlocked.Increment(ref _videoPacketsWritten);
        _totalBytesWritten += packetSize;
    }

    private void WriteStreamPacket(ref AudioStreamState s, AVPacket* packet)
    {
        ffmpeg.av_packet_rescale_ts(packet, s.CodecCtx->time_base, s.Stream->time_base);
        packet->stream_index = s.Stream->index;
        var packetSize = packet->size;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_formatCtx, packet), "av_interleaved_write_frame(audio)");
        _totalBytesWritten += packetSize;
    }

    private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)
    {
        var rowBytes = options.IsP010 ? options.Width * 2 : options.Width;
        var uvHeight = options.Height / 2;
        var yBytes = rowBytes * options.Height;
        var uvBytes = rowBytes * uvHeight;
        if (frameData.Length < yBytes + uvBytes)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyPackedFrameToVideoFrame msg=Frame buffer shorter than computed planes actual={frameData.Length} expected={yBytes + uvBytes}");
        }

        fixed (byte* sourceStart = frameData)
        {
            CopyPlane(sourceStart, _videoFrame->data[0], _videoFrame->linesize[0], rowBytes, options.Height);
            CopyPlane(sourceStart + yBytes, _videoFrame->data[1], _videoFrame->linesize[1], rowBytes, uvHeight);
        }
    }

    private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)
    {
        if (destinationStart == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyPlane msg=Destination plane pointer is null.");
        }

        var totalBytes = (long)rowBytes * rowCount;
        if (destinationStride == rowBytes)
        {
            Buffer.MemoryCopy(sourceStart, destinationStart, totalBytes, totalBytes);
            return;
        }

        for (var row = 0; row < rowCount; row++)
        {
            Buffer.MemoryCopy(
                sourceStart + (row * rowBytes),
                destinationStart + (row * destinationStride),
                rowBytes,
                rowBytes);
        }
    }

    private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,
        bool trackDriftCorrection, double driftCorrectionThresholdMs)
    {
        if (s.CodecCtx == null || s.Stream == null || s.Frame == null || s.SwrCtx == null || inputSamples <= 0)
        {
            return;
        }

        var channelCount = GetStreamChannelCount(ref s);
        if (s.SampleQueueBuffer == null || s.SampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio sample queue is not allocated.");
        }

        if (s.BufferedSamples < 0 || s.BufferedSamples > s.SampleQueueCapacity)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio queue sample count was out of range buffered={s.BufferedSamples} capacity={s.SampleQueueCapacity}.");
        }

        var availableSamples = s.SampleQueueCapacity - s.BufferedSamples;
        if (availableSamples < inputSamples + MaxDriftCorrectionSamplesPerPass)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio queue capacity exhausted buffered={s.BufferedSamples} available={availableSamples} requested={inputSamples}.");
        }

        var inputData = stackalloc byte*[1];
        inputData[0] = inputPtr;

        var outputData = stackalloc byte*[channelCount];
        for (var channel = 0; channel < channelCount; channel++)
        {
            outputData[channel] = (byte*)(GetStreamQueuePlane(ref s, channel) + s.BufferedSamples);
        }

        var convertedSamples = ffmpeg.swr_convert(
            s.SwrCtx,
            outputData,
            availableSamples,
            inputData,
            inputSamples);
        if (convertedSamples < 0)
        {
            ThrowIfError(convertedSamples, "swr_convert");
        }

        var queuedSamples = s.BufferedSamples + convertedSamples;
        var queuedStreamSamples = s.NextPts + queuedSamples;
        var correctionSamples = GetDriftCorrectionSamples(
            queuedStreamSamples,
            s.CodecCtx->sample_rate,
            out var correctionVideoFrame,
            out var driftMs,
            driftCorrectionThresholdMs);
        var appliedCorrectionSamples = 0;

        if (correctionSamples < 0)
        {
            var trimmedSamples = Math.Min(-correctionSamples, queuedSamples);
            queuedSamples -= trimmedSamples;
            appliedCorrectionSamples -= trimmedSamples;
        }
        else if (correctionSamples > 0)
        {
            AppendSilentStreamSamples(ref s, queuedSamples, correctionSamples, channelCount);
            queuedSamples += correctionSamples;
            appliedCorrectionSamples += correctionSamples;
        }

        if (trackDriftCorrection && (correctionSamples == 0 || appliedCorrectionSamples == correctionSamples))
        {
            _lastDriftCorrectionVideoFrame = correctionVideoFrame;
        }

        s.BufferedSamples = queuedSamples;
        DrainBufferedFrames(ref s, flushPartialFrame: false);

        if (trackDriftCorrection && appliedCorrectionSamples != 0)
        {
            _driftCorrectionAppliedSamples += appliedCorrectionSamples;
            Logger.Log(
                $"LIBAV_AV_DRIFT_CORRECTION videoFrame={_nextVideoPts} driftMs={driftMs:F1} " +
                $"correctionSamples={appliedCorrectionSamples} totalCorrectionSamples={_driftCorrectionAppliedSamples}");
        }
    }

    private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)
    {
        while (s.BufferedSamples >= s.FrameSize || (flushPartialFrame && s.BufferedSamples > 0))
        {
            var sampleCount = s.BufferedSamples >= s.FrameSize
                ? s.FrameSize
                : s.BufferedSamples;
            SendPreparedStreamFrame(ref s, sampleCount);
            RemoveQueuedStreamSamples(ref s, sampleCount);
        }
    }

    private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)
    {
        if (s.CodecCtx == null || s.Frame == null || sampleCount <= 0)
        {
            return;
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(s.Frame), "av_frame_make_writable(audio)");
        CopyQueuedSamplesToStreamFrame(ref s, sampleCount);

        s.Frame->nb_samples = sampleCount;
        var nextPts = s.NextPts;
        s.Frame->pts = nextPts;

        var sendResult = ffmpeg.avcodec_send_frame(s.CodecCtx, s.Frame);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainStreamEncoderPackets(ref s);
            sendResult = ffmpeg.avcodec_send_frame(s.CodecCtx, s.Frame);
        }

        ThrowIfError(sendResult, "avcodec_send_frame(audio)");
        s.NextPts = nextPts + sampleCount;
        DrainStreamEncoderPackets(ref s);
    }

    private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)
    {
        if (s.CodecCtx == null || s.Frame == null || s.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Audio frame storage was not initialized.");
        }

        var bytesPerSample = ffmpeg.av_get_bytes_per_sample(s.CodecCtx->sample_fmt);
        if (bytesPerSample <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Unsupported sample format '{s.CodecCtx->sample_fmt}'.");
        }

        var channelCount = GetStreamChannelCount(ref s);
        if (ffmpeg.av_sample_fmt_is_planar(s.CodecCtx->sample_fmt) == 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Expected planar audio frame layout.");
        }

        var planeBytes = sampleCount * bytesPerSample;
        for (var channel = 0; channel < channelCount; channel++)
        {
            var source = GetStreamQueuePlane(ref s, channel);
            var destination = (float*)s.Frame->extended_data[channel];
            if (destination == null)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Audio plane pointer was null channel={channel}.");
            }

            Buffer.MemoryCopy(source, destination, planeBytes, planeBytes);
        }
    }

    private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        if (sampleCount > s.BufferedSamples)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=RemoveQueuedStreamSamples msg=Cannot remove more samples than buffered remove={sampleCount} buffered={s.BufferedSamples}.");
        }

        var remainingSamples = s.BufferedSamples - sampleCount;
        if (remainingSamples > 0)
        {
            var channelCount = GetStreamChannelCount(ref s);
            for (var channel = 0; channel < channelCount; channel++)
            {
                var plane = GetStreamQueuePlane(ref s, channel);
                new ReadOnlySpan<float>(plane + sampleCount, remainingSamples)
                    .CopyTo(new Span<float>(plane, remainingSamples));
            }
        }

        s.BufferedSamples = remainingSamples;
    }

    private void AppendSilentStreamSamples(ref AudioStreamState s, int startSample, int sampleCount, int channelCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        for (var channel = 0; channel < channelCount; channel++)
        {
            new Span<float>(GetStreamQueuePlane(ref s, channel) + startSample, sampleCount).Clear();
        }
    }

    private float* GetStreamQueuePlane(ref AudioStreamState s, int channel)
    {
        if (s.SampleQueueBuffer == null || s.SampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetStreamQueuePlane msg=Audio sample queue was not initialized.");
        }

        return (float*)(s.SampleQueueBuffer + (channel * s.SampleQueueCapacity * sizeof(float)));
    }

    private int GetStreamChannelCount(ref AudioStreamState s)
    {
        var channelCount = (int)(s.CodecCtx != null && s.CodecCtx->ch_layout.nb_channels > 0
            ? s.CodecCtx->ch_layout.nb_channels
            : 0);
        if (channelCount <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetStreamChannelCount msg=Audio channel count was not available.");
        }

        return channelCount;
    }

    private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,
        bool trackDriftCorrection = false, double driftCorrectionThresholdMs = DriftCorrectionThresholdMs)
    {
        if (s.CodecCtx == null || s.Frame == null)
        {
            return;
        }

        if (s.AccumulatorBytes > 0)
        {
            var inputChannels = (int)(s.CodecCtx->ch_layout.nb_channels > 0
                ? s.CodecCtx->ch_layout.nb_channels
                : 0);
            var inputBlockAlign = checked(inputChannels * sizeof(float));
            if (inputBlockAlign <= 0)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=FlushPendingStreamSamples({streamLabel}) msg=Channel count was not available.");
            }

            if (s.AccumulatorBytes % inputBlockAlign != 0)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=FlushPendingStreamSamples({streamLabel}) msg=Accumulator is not sample-aligned bytes={s.AccumulatorBytes} block_align={inputBlockAlign}");
            }

            var pendingSamples = s.AccumulatorBytes / inputBlockAlign;
            if (pendingSamples > 0)
            {
                EncodeStreamChunk(ref s, s.ResampleBuffer, pendingSamples,
                    trackDriftCorrection, driftCorrectionThresholdMs);
            }

            s.AccumulatorBytes = 0;
        }

        DrainBufferedFrames(ref s, flushPartialFrame: true);
    }

    private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)
    {
        if (source.IsEmpty)
        {
            return;
        }

        if (s.ResampleBuffer == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyToAccumulator msg=Audio accumulator buffer is null.");
        }

        fixed (byte* sourcePtr = source)
        {
            Buffer.MemoryCopy(
                sourcePtr,
                s.ResampleBuffer + destinationOffset,
                s.AccumulatorCapacity - destinationOffset,
                source.Length);
        }
    }

    private void EnsureOpen()
    {
        if (!_isOpen || _formatCtx == null || _videoCodecCtx == null || _videoStream == null || _videoFrame == null || _packet == null)
        {
            throw new InvalidOperationException("LibAvEncoder is not initialized.");
        }
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"LIBAV_ENCODER_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException($"LIBAV_ENCODER_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private static InvalidOperationException CreateLibAvException(string message)
    {
        Logger.Log(message);
        return new InvalidOperationException(message);
    }

    /// <summary>
    /// Checks ID3D11Device::GetDeviceRemovedReason (vtable slot 39) to detect TDR.
    /// CopySubresourceRegion is void-return, so after a device-removed event all
    /// context calls silently no-op. This proactive check surfaces the error before
    /// NVENC encodes from stale/garbage textures, allowing the caller to finalize
    /// the recording and preserve already-encoded data.
    /// </summary>
    private static void CheckDeviceRemoved(IntPtr d3d11Device)
    {
        if (d3d11Device == IntPtr.Zero)
            return;

        var deviceVtable = *(IntPtr*)d3d11Device;
        // ID3D11Device vtable layout: IUnknown (0-2) + ID3D11Device methods (3+).
        // CreateTexture2D = slot 5 (validated elsewhere in this file).
        // GetDeviceRemovedReason = slot 39 (3 IUnknown + 36 ID3D11Device methods before it).
        var getDeviceRemovedReason =
            (delegate* unmanaged[Stdcall]<IntPtr, int>)*(IntPtr*)(deviceVtable + 39 * IntPtr.Size);
        var hr = getDeviceRemovedReason(d3d11Device);

        if (hr < 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_DEVICE_REMOVED hr=0x{unchecked((uint)hr):X8} " +
                "msg=GPU device was removed (TDR). Recording will be finalized with frames encoded so far.");
        }
    }
}

internal sealed record LibAvEncoderOptions
{
    public required string OutputPath { get; init; }
    public string ContainerFormat { get; init; } = "mp4";
    public required string CodecName { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required double FrameRate { get; init; }
    public int? FrameRateNumerator { get; init; }
    public int? FrameRateDenominator { get; init; }
    public required uint BitRate { get; init; }
    public required bool IsP010 { get; init; }
    public string? NvencPreset { get; init; }
    public string SplitEncodeMode { get; init; } = "Auto";
    public int GopSize { get; init; } = -1;
    /// <summary>
    /// Use frag_keyframe+empty_moov instead of faststart for MP4.
    /// Required for flashback segments that are read while still being written.
    /// </summary>
    public bool FragmentedMp4 { get; init; }
    public bool AudioEnabled { get; init; }
    public int AudioSampleRate { get; init; } = 48_000;
    public int AudioChannels { get; init; } = 2;
    public int AudioBitRate { get; init; } = 320_000;
    public bool MicrophoneEnabled { get; init; }
    public int MicrophoneSampleRate { get; init; } = 48_000;
    public int MicrophoneChannels { get; init; } = 2;
    public int MicrophoneBitRate { get; init; } = 320_000;
    public bool HdrEnabled { get; init; }
    public bool IsFullRangeInput { get; init; }
    public string? HdrMasterDisplayMetadata { get; init; }
    public int HdrMaxCll { get; init; }
    public int HdrMaxFall { get; init; }
    public IntPtr D3D11DevicePtr { get; init; }
    public IntPtr D3D11DeviceContextPtr { get; init; }
    public IntPtr CudaHwDeviceCtxPtr { get; init; }
    public IntPtr CudaHwFramesCtxPtr { get; init; }
}

internal readonly record struct RotateOutputResult(string PreviousPath, long PreviousEncodedFrames, long PreviousTotalBytes);

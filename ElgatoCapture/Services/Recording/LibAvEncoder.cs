using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using FFmpeg.AutoGen;

namespace ElgatoCapture.Services;

/// <summary>
/// In-process libav encoder for MP4 recording.
/// This type is not thread-safe; all libav calls must be serialized onto one thread.
/// </summary>
internal sealed unsafe class LibAvEncoder : IDisposable
{
    private const long AvSyncLogCadenceFrames = 300;
    private const long MinimumAvSyncVideoFrames = 30;
    // Drift correction disabled: capture card audio and video share the same USB bus
    // clock, so there is no ongoing drift. The apparent drift at startup is a one-time
    // offset from WASAPI pre-buffering. Correcting it causes audible pops because 480-
    // sample block insertions/removals create hard discontinuities in the waveform.
    // Players handle the small initial A/V offset in the container transparently.
    private const double DriftCorrectionThresholdMs = double.MaxValue;
    private const double MicDriftCorrectionThresholdMs = 200.0;
    private const int MaxDriftCorrectionSamplesPerPass = 480;

    private static readonly Regex MasterDisplayMetadataRegex = new(
        @"^G\((\d+),(\d+)\)B\((\d+),(\d+)\)R\((\d+),(\d+)\)WP\((\d+),(\d+)\)L\((\d+),(\d+)\)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly object FfmpegInitSync = new();
    private static bool _ffmpegInitialized;

    private AVFormatContext* _formatCtx;
    private AVCodecContext* _videoCodecCtx;
    private AVCodecContext* _audioCodecCtx;
    private AVCodecContext* _micCodecCtx;
    private AVStream* _videoStream;
    private AVStream* _audioStream;
    private AVStream* _micStream;
    private AVFrame* _videoFrame;
    private AVFrame* _audioFrame;
    private AVFrame* _micFrame;
    private AVPacket* _packet;
    private AVBSFContext* _bsfCtx;
    private SwrContext* _swrCtx;
    private SwrContext* _micSwrCtx;
    private LibAvEncoderOptions? _options;
    private long _nextVideoPts;
    private long _nextAudioPts;
    private long _nextMicPts;
    private long _encodedFrameCount;
    private long _droppedFrameCount;
    private long _audioSamplesReceived;
    private long _micSamplesReceived;
    private long _lastSyncLogVideoFrame;
    private long _driftCorrectionAppliedSamples;
    private long _lastDriftCorrectionVideoFrame;
    private long _totalBytesWritten;
    private byte* _resampleBuffer;
    private byte* _audioSampleQueueBuffer;
    private byte* _micResampleBuffer;
    private byte* _micSampleQueueBuffer;
    private int _audioFrameSize;
    private int _micFrameSize;
    private int _accumulatorCapacity;
    private int _audioSampleQueueCapacity;
    private int _micAccumulatorCapacity;
    private int _micSampleQueueCapacity;
    private int _audioAccumulatorBytes;
    private int _audioBufferedSamples;
    private int _micAccumulatorBytes;
    private int _micBufferedSamples;
    private bool _isOpen;
    private bool _headerWritten;
    private AVRational _cachedVideoTimeBase;
    private AVRational _cachedAudioTimeBase;
    private AVRational _cachedMicTimeBase;
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
    private static readonly av_buffer_create_free _hwPoolTextureFreeDelegate = (opaque, data) => { /* intentional no-op */ };
    private static readonly av_buffer_create_free_func _hwPoolTextureFree = _hwPoolTextureFreeDelegate;

    public long EncodedFrameCount => _encodedFrameCount;
    public long DroppedFrameCount => _droppedFrameCount;
    public long AudioSamplesReceived => _audioSamplesReceived;
    public long MicrophoneSamplesReceived => _micSamplesReceived;
    public long TotalBytesWritten => _totalBytesWritten;
    public bool IsEncoding => _isOpen;
    public bool AudioEnabled => _options?.AudioEnabled == true && _audioCodecCtx != null && _audioStream != null;
    public bool MicrophoneEnabled => _options?.MicrophoneEnabled == true && _micCodecCtx != null && _micStream != null;
    public string VideoCodecName => _options?.CodecName ?? string.Empty;
    public string OutputPath => _options?.OutputPath ?? string.Empty;
    public bool UseHardwareFrames => _useHardwareFrames;
    public bool UseCudaHardwareFrames => _useCudaHardwareFrames;
    public long NextVideoPts => _nextVideoPts;

    public void SkipVideoFrame() { Interlocked.Increment(ref _nextVideoPts); }

    public static void InitializeFFmpeg(bool requireNativeRuntime = false)
    {
        lock (FfmpegInitSync)
        {
            if (_ffmpegInitialized)
            {
                return;
            }

            if (!FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot(out var runtimeRoot))
            {
                var message =
                    $"FFmpeg native runtime not found. assembly_dir='{FfmpegRuntimeLocator.GetAssemblyBaseDirectory()}'";
                Logger.Log($"LIBAV_RUNTIME_MISSING {message}");
                if (requireNativeRuntime)
                {
                    throw new InvalidOperationException(message);
                }

                return;
            }

            ffmpeg.RootPath = runtimeRoot;

            try
            {
                Logger.Log($"LIBAV_INIT root_path='{ffmpeg.RootPath}' avcodec_version={ffmpeg.avcodec_version()}");
                _ffmpegInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Log($"LIBAV_INIT_ERROR root_path='{ffmpeg.RootPath}' type={ex.GetType().Name} msg={ex.Message}");
                if (requireNativeRuntime)
                {
                    throw new InvalidOperationException(
                        $"FFmpeg native runtime failed to initialize from '{ffmpeg.RootPath}': {ex.Message}",
                        ex);
                }
            }
        }
    }

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

            InitializeHdrBitstreamFilterIfNeeded(options);
            InitializeAudioIfNeeded(options);
            InitializeMicrophoneIfNeeded(options);

            ThrowIfError(ffmpeg.avio_open2(&_formatCtx->pb, options.OutputPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2");

            AVDictionary* muxerOptions = null;
            try
            {
                if (options.ContainerFormat == "mp4")
                {
                    ThrowIfError(ffmpeg.av_dict_set(&muxerOptions, "movflags", "+faststart", 0), "av_dict_set(movflags)");
                }
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
            _nextAudioPts = 0;
            _nextMicPts = 0;
            _encodedFrameCount = 0;
            _droppedFrameCount = 0;
            _audioSamplesReceived = 0;
            _micSamplesReceived = 0;
            _lastSyncLogVideoFrame = 0;
            _driftCorrectionAppliedSamples = 0;
            _lastDriftCorrectionVideoFrame = 0;
            _audioAccumulatorBytes = 0;
            _audioBufferedSamples = 0;
            _micAccumulatorBytes = 0;
            _micBufferedSamples = 0;
            _flushSent = false;
            _isOpen = true;

            Logger.Log(
                $"LIBAV_ENCODER_OPEN codec='{options.CodecName}' output='{options.OutputPath}' " +
                $"width={options.Width} height={options.Height} fps={options.FrameRate.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"bitrate={options.BitRate} pix_fmt='{(options.IsP010 ? "p010le" : "nv12")}' hdr={options.HdrEnabled} " +
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
            (byte*)poolTexture, 0, _hwPoolTextureFree, null, 0);

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

        if (_audioCodecCtx == null || _audioStream == null || _audioFrame == null || _swrCtx == null || f32leSamples.IsEmpty)
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
        var frameBytes = checked(_audioFrameSize * inputBlockAlign);

        if (_audioAccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _audioAccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAudioAccumulator(remaining[..copyBytes], _audioAccumulatorBytes);
            _audioAccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_audioAccumulatorBytes == frameBytes)
            {
                EncodeAudioChunk(_resampleBuffer, _audioFrameSize);
                _audioAccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeAudioChunk(inputPtr, _audioFrameSize);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAudioAccumulator(remaining, 0);
            _audioAccumulatorBytes = remaining.Length;
        }
    }

    public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_micCodecCtx == null || _micStream == null || _micFrame == null || _micSwrCtx == null || f32leSamples.IsEmpty)
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
        var frameBytes = checked(_micFrameSize * inputBlockAlign);

        if (_micAccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _micAccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToMicAccumulator(remaining[..copyBytes], _micAccumulatorBytes);
            _micAccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_micAccumulatorBytes == frameBytes)
            {
                EncodeMicChunk(_micResampleBuffer, _micFrameSize);
                _micAccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeMicChunk(inputPtr, _micFrameSize);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToMicAccumulator(remaining, 0);
            _micAccumulatorBytes = remaining.Length;
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
        var previousTotalBytes = _totalBytesWritten;

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(newPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (_audioCodecCtx != null)
        {
            // CRITICAL: Do NOT call FlushPendingAudioSamples() here.
            // It uses flushPartialFrame=true, which sends a partial AAC frame
            // (< 1024 samples). The AAC encoder interprets partial frames as
            // "end of stream" and rejects all subsequent frames with EINVAL.
            // Instead, drain only full frames and carry partial samples into
            // the next segment.
            DrainBufferedAudioFrames(flushPartialFrame: false);
            DrainAudioEncoderPackets();
        }

        if (_micCodecCtx != null)
        {
            DrainBufferedMicFrames(flushPartialFrame: false);
            DrainMicEncoderPackets();
        }

        DrainEncoderPackets();

        if (_headerWritten && _formatCtx != null)
        {
            ThrowIfError(ffmpeg.av_write_trailer(_formatCtx), "av_write_trailer(rotate)");
        }

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
        if (!_isOpen && _formatCtx == null && _videoCodecCtx == null && _audioCodecCtx == null && _micCodecCtx == null)
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

            if (_audioCodecCtx != null)
            {
                FlushPendingAudioSamples();

                var flushResult = ffmpeg.avcodec_send_frame(_audioCodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(audio_flush)");
                }

                DrainAudioEncoderPackets();
            }

            if (_micCodecCtx != null)
            {
                FlushPendingMicSamples();

                var flushResult = ffmpeg.avcodec_send_frame(_micCodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(mic_flush)");
                }

                DrainMicEncoderPackets();
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

    private void ConfigureVideoCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options)
    {
        var frameRate = ResolveFrameRate(options);
        codecContext->width = options.Width;
        codecContext->height = options.Height;
        codecContext->time_base = Invert(frameRate);
        codecContext->framerate = frameRate;
        codecContext->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
        codecContext->bit_rate = options.BitRate;
        codecContext->gop_size = options.GopSize > 0 ? options.GopSize : Math.Max(1, (int)Math.Round(options.FrameRate * 2, MidpointRounding.AwayFromZero));
        codecContext->max_b_frames = 0;

        if (!options.HdrEnabled)
        {
            // MJPEG sources decode to full-range YUV (0-255). Without this flag,
            // NVENC treats the data as limited range (16-235), darkening the output.
            if (options.IsFullRangeInput)
            {
                codecContext->color_range = AVColorRange.AVCOL_RANGE_JPEG;
                codecContext->colorspace = AVColorSpace.AVCOL_SPC_BT709;
                codecContext->color_primaries = AVColorPrimaries.AVCOL_PRI_BT709;
                codecContext->color_trc = AVColorTransferCharacteristic.AVCOL_TRC_BT709;
            }

            return;
        }

        codecContext->color_primaries = AVColorPrimaries.AVCOL_PRI_BT2020;
        codecContext->color_trc = AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084;
        codecContext->colorspace = AVColorSpace.AVCOL_SPC_BT2020_NCL;
        codecContext->color_range = AVColorRange.AVCOL_RANGE_MPEG;
    }

    private void ApplyEncoderPrivateOptions(AVCodecContext* codecContext, LibAvEncoderOptions options)
    {
        if (!options.CodecName.Contains("_nvenc", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var preset = MapNvencPreset(options.NvencPreset);
        ThrowIfError(ffmpeg.av_opt_set(codecContext->priv_data, "preset", preset, 0), "av_opt_set(preset)");
    }

    /// <summary>
    /// Creates a single D3D11 Texture2D (ArraySize=1) via raw vtable call.
    /// Returns the texture pointer (caller owns the reference) or IntPtr.Zero on failure.
    /// </summary>
    private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)
    {
        // D3D11_TEXTURE2D_DESC layout (44 bytes):
        // 0: Width(4) 4: Height(4) 8: MipLevels(4) 12: ArraySize(4)
        // 16: Format(4) 20: SampleDesc.Count(4) 24: SampleDesc.Quality(4)
        // 28: Usage(4) 32: BindFlags(4) 36: CPUAccessFlags(4) 40: MiscFlags(4)
        var texDesc = stackalloc byte[44];
        new Span<byte>(texDesc, 44).Clear();
        *(uint*)(texDesc + 0) = (uint)width;
        *(uint*)(texDesc + 4) = (uint)height;
        *(uint*)(texDesc + 8) = 1; // MipLevels
        *(uint*)(texDesc + 12) = 1; // ArraySize — individual textures, not array
        *(uint*)(texDesc + 16) = isP010 ? 104u : 103u; // DXGI_FORMAT_P010=104, DXGI_FORMAT_NV12=103
        *(uint*)(texDesc + 20) = 1; // SampleDesc.Count
        *(uint*)(texDesc + 28) = 0; // D3D11_USAGE_DEFAULT
        *(uint*)(texDesc + 32) = bindFlags;

        // ID3D11Device vtable slot 5 = CreateTexture2D
        var vtable = *(IntPtr*)d3d11Device;
        var createTexture2DPtr = *(IntPtr*)(vtable + 5 * IntPtr.Size);
        IntPtr ppTexture = IntPtr.Zero;
        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, byte*, IntPtr, IntPtr*, int>)createTexture2DPtr)(
            d3d11Device, texDesc, IntPtr.Zero, &ppTexture);

        if (hr < 0)
        {
            Logger.Log($"LIBAV_ENCODER_HW_FRAMES_CREATE_TEX_FAIL hr=0x{unchecked((uint)hr):X8} w={width} h={height}");
            return IntPtr.Zero;
        }

        return ppTexture;
    }

    private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)
    {
        if (options.CudaHwDeviceCtxPtr != IntPtr.Zero && options.CudaHwFramesCtxPtr != IntPtr.Zero)
        {
            InitializeCudaHardwareFrames(options);
            return;
        }

        if (options.D3D11DevicePtr == IntPtr.Zero)
        {
            Logger.Log("LIBAV_ENCODER_HW_FRAMES skip=no_device");
            return;
        }

        if (options.D3D11DeviceContextPtr == IntPtr.Zero)
        {
            Logger.Log("LIBAV_ENCODER_HW_FRAMES skip=no_device_context");
            return;
        }

        AVBufferRef* hwDeviceCtx = null;
        AVBufferRef* hwFramesCtx = null;
        AVBufferRef* codecHwDeviceCtx = null;
        AVBufferRef* codecHwFramesCtx = null;
        var stage = "av_hwdevice_ctx_alloc";

        try
        {
            hwDeviceCtx = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
            if (hwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to allocate D3D11VA device context.");
            }

            stage = "av_hwdevice_ctx_init";
            var hwDeviceCtxData = (AVHWDeviceContext*)hwDeviceCtx->data;
            var d3d11vaDeviceCtx = (AVD3D11VADeviceContext*)hwDeviceCtxData->hwctx;
            d3d11vaDeviceCtx->device = (FFmpeg.AutoGen.ID3D11Device*)options.D3D11DevicePtr;
            d3d11vaDeviceCtx->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)options.D3D11DeviceContextPtr;

            var initResult = ffmpeg.av_hwdevice_ctx_init(hwDeviceCtx);
            if (initResult < 0)
            {
                throw new InvalidOperationException($"code={initResult} (0x{unchecked((uint)initResult):X8}) msg='{GetErrorString(initResult)}'");
            }

            stage = "av_hwframe_ctx_alloc";
            hwFramesCtx = ffmpeg.av_hwframe_ctx_alloc(hwDeviceCtx);
            if (hwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to allocate hardware frames context.");
            }

            const int poolSize = 8;
            const uint bindFlags = 0x20; // D3D11_BIND_RENDER_TARGET — required by NVENC

            var framesCtx = (AVHWFramesContext*)hwFramesCtx->data;
            framesCtx->format = AVPixelFormat.AV_PIX_FMT_D3D11;
            framesCtx->sw_format = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            framesCtx->width = options.Width;
            framesCtx->height = options.Height;
            // initial_pool_size = 0: skip FFmpeg's internal pool. NV12/P010 texture arrays
            // (ArraySize>1) fail with E_INVALIDARG on some GPUs, and FFmpeg's pool mechanism
            // doesn't work with externally-provided individual textures. We manage our own
            // pool of ArraySize=1 textures and construct AVFrames manually in SendGpuVideoFrame.
            framesCtx->initial_pool_size = 0;

            var d3d11FramesCtx = (AVD3D11VAFramesContext*)framesCtx->hwctx;
            d3d11FramesCtx->BindFlags = bindFlags;

            stage = "av_hwframe_ctx_init";
            var framesInitResult = ffmpeg.av_hwframe_ctx_init(hwFramesCtx);
            if (framesInitResult < 0)
            {
                throw new InvalidOperationException(
                    $"code={framesInitResult} (0x{unchecked((uint)framesInitResult):X8}) " +
                    $"msg='{GetErrorString(framesInitResult)}'");
            }

            // Pre-create individual ArraySize=1 textures for our own pool
            stage = "pre_create_pool_textures";
            var poolTextures = new IntPtr[poolSize];
            for (var i = 0; i < poolSize; i++)
            {
                var tex = CreateSingleTexture2D(
                    options.D3D11DevicePtr, options.Width, options.Height, options.IsP010, bindFlags);
                if (tex == IntPtr.Zero)
                {
                    for (var j = 0; j < i; j++) Marshal.Release(poolTextures[j]);
                    throw new InvalidOperationException(
                        $"CreateTexture2D failed for pool texture {i} " +
                        $"(w={options.Width} h={options.Height} fmt={(options.IsP010 ? "P010" : "NV12")})");
                }
                poolTextures[i] = tex;
            }

            _hwPoolTextures = poolTextures;
            _hwPoolIndex = 0;

            Logger.Log(
                $"LIBAV_ENCODER_HW_FRAMES_POOL " +
                $"created {poolSize} individual textures, pool_bypass=true " +
                $"(w={options.Width} h={options.Height} fmt={(options.IsP010 ? "P010" : "NV12")} " +
                $"bindFlags=0x{bindFlags:X})");

            stage = "av_buffer_ref(hw_device_ctx)";
            codecHwDeviceCtx = ffmpeg.av_buffer_ref(hwDeviceCtx);
            if (codecHwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to reference hardware device context.");
            }

            stage = "av_buffer_ref(hw_frames_ctx)";
            codecHwFramesCtx = ffmpeg.av_buffer_ref(hwFramesCtx);
            if (codecHwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to reference hardware frames context.");
            }

            _videoCodecCtx->hw_device_ctx = codecHwDeviceCtx;
            codecHwDeviceCtx = null;
            _videoCodecCtx->hw_frames_ctx = codecHwFramesCtx;
            codecHwFramesCtx = null;
            _videoCodecCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;

            _hwDeviceCtx = hwDeviceCtx;
            hwDeviceCtx = null;
            _hwFramesCtx = hwFramesCtx;
            hwFramesCtx = null;
            _useHardwareFrames = true;
            _useCudaHardwareFrames = false;
            Logger.Log(
                $"LIBAV_ENCODER_HW_FRAMES mode=d3d11va sw_format={(options.IsP010 ? "p010le" : "nv12")} " +
                $"pool_size=8 width={options.Width} height={options.Height}");
        }
        catch (Exception ex)
        {
            if (codecHwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwFramesCtx);
            }

            if (codecHwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwDeviceCtx);
            }

            if (hwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&hwFramesCtx);
            }

            if (hwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&hwDeviceCtx);
            }

            // Release pool textures if we created them but failed at a later stage
            if (_hwPoolTextures != null)
            {
                for (var i = 0; i < _hwPoolTextures.Length; i++)
                {
                    if (_hwPoolTextures[i] != IntPtr.Zero)
                    {
                        Marshal.Release(_hwPoolTextures[i]);
                    }
                }
                _hwPoolTextures = null;
            }

            _videoCodecCtx->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _useHardwareFrames = false;
            _useCudaHardwareFrames = false;
            Logger.Log($"LIBAV_ENCODER_HW_FRAMES_WARN stage={stage} msg='{ex.Message}' fallback=software");
        }
    }

    private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)
    {
        AVBufferRef* codecHwDeviceCtx = null;
        AVBufferRef* codecHwFramesCtx = null;
        var stage = "av_buffer_ref(cuda_device)";

        try
        {
            codecHwDeviceCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwDeviceCtxPtr);
            if (codecHwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to reference CUDA device context for encoder.");
            }

            stage = "av_buffer_ref(cuda_frames)";
            codecHwFramesCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwFramesCtxPtr);
            if (codecHwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to reference CUDA frames context for encoder.");
            }

            _videoCodecCtx->hw_device_ctx = codecHwDeviceCtx;
            codecHwDeviceCtx = null;
            _videoCodecCtx->hw_frames_ctx = codecHwFramesCtx;
            codecHwFramesCtx = null;
            _videoCodecCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_CUDA;

            _hwDeviceCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwDeviceCtxPtr);
            if (_hwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to retain CUDA device context for encoder.");
            }

            _hwFramesCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwFramesCtxPtr);
            if (_hwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to retain CUDA frames context for encoder.");
            }

            _useHardwareFrames = true;
            _useCudaHardwareFrames = true;

            Logger.Log(
                $"LIBAV_ENCODER_HW_FRAMES mode=cuda sw_format=nv12 width={options.Width} height={options.Height}");
        }
        catch (Exception ex)
        {
            if (_videoCodecCtx->hw_frames_ctx != null)
            {
                ffmpeg.av_buffer_unref(&_videoCodecCtx->hw_frames_ctx);
            }

            if (_videoCodecCtx->hw_device_ctx != null)
            {
                ffmpeg.av_buffer_unref(&_videoCodecCtx->hw_device_ctx);
            }

            if (codecHwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwFramesCtx);
            }

            if (codecHwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwDeviceCtx);
            }

            if (_hwFramesCtx != null)
            {
                var hwFramesCtx = _hwFramesCtx;
                ffmpeg.av_buffer_unref(&hwFramesCtx);
                _hwFramesCtx = null;
            }

            if (_hwDeviceCtx != null)
            {
                var hwDeviceCtx = _hwDeviceCtx;
                ffmpeg.av_buffer_unref(&hwDeviceCtx);
                _hwDeviceCtx = null;
            }

            _videoCodecCtx->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _useHardwareFrames = false;
            _useCudaHardwareFrames = false;
            Logger.Log($"LIBAV_ENCODER_HW_FRAMES_WARN stage={stage} msg='{ex.Message}' fallback=software");
        }
    }

    private void InitializeHdrBitstreamFilterIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.HdrEnabled)
        {
            return;
        }

        var filterName = GetHdrBitstreamFilterName(options.CodecName);
        if (filterName == null)
        {
            return;
        }

        var filter = ffmpeg.av_bsf_get_by_name(filterName);
        if (filter == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_BSF_INIT_FAIL codec='{options.CodecName}' filter='{filterName}' msg=Filter not available.");
        }

        AVBSFContext* bsfCtx = null;
        ThrowIfError(ffmpeg.av_bsf_alloc(filter, &bsfCtx), "av_bsf_alloc");
        _bsfCtx = bsfCtx;
        ThrowIfError(ffmpeg.avcodec_parameters_from_context(_bsfCtx->par_in, _videoCodecCtx), "avcodec_parameters_from_context(bsf)");
        _bsfCtx->time_base_in = _videoCodecCtx->time_base;

        var optionTarget = _bsfCtx->priv_data != null ? _bsfCtx->priv_data : _bsfCtx;
        var searchFlags = ffmpeg.AV_OPT_SEARCH_CHILDREN;

        if (filterName.Equals("hevc_metadata", StringComparison.OrdinalIgnoreCase))
        {
            ThrowIfError(ffmpeg.av_opt_set(optionTarget, "colour_primaries", "9", searchFlags), "av_opt_set(hevc_metadata.colour_primaries)");
            ThrowIfError(ffmpeg.av_opt_set(optionTarget, "transfer_characteristics", "16", searchFlags), "av_opt_set(hevc_metadata.transfer_characteristics)");
            ThrowIfError(ffmpeg.av_opt_set(optionTarget, "matrix_coefficients", "9", searchFlags), "av_opt_set(hevc_metadata.matrix_coefficients)");
        }
        else
        {
            ThrowIfError(ffmpeg.av_opt_set(optionTarget, "color_primaries", "9", searchFlags), "av_opt_set(av1_metadata.color_primaries)");
            ThrowIfError(ffmpeg.av_opt_set(optionTarget, "transfer_characteristics", "16", searchFlags), "av_opt_set(av1_metadata.transfer_characteristics)");
            ThrowIfError(ffmpeg.av_opt_set(optionTarget, "matrix_coefficients", "9", searchFlags), "av_opt_set(av1_metadata.matrix_coefficients)");
        }

        ThrowIfError(ffmpeg.av_bsf_init(_bsfCtx), "av_bsf_init");
        Logger.Log($"LIBAV_ENCODER_BSF_INIT codec='{options.CodecName}' filter='{filterName}'");
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

        _audioStream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (_audioStream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(audio) msg=Stream allocation returned null.");
        }

        _audioCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_audioCodecCtx == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3(audio) msg=Codec context allocation returned null.");
        }

        ConfigureAudioCodecContext(_audioCodecCtx, options, codec);

        // Skip GLOBAL_HEADER for MPEG-TS — AAC needs ADTS framing per segment.
        if (options.ContainerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            _audioCodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(_audioCodecCtx, codec, null), "avcodec_open2(audio)");

        _audioFrameSize = _audioCodecCtx->frame_size;
        if (_audioFrameSize <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeAudioIfNeeded msg=Unexpected AAC frame size value={_audioFrameSize}");
        }

        _audioStream->time_base = _audioCodecCtx->time_base;
        _cachedAudioTimeBase = _audioCodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_audioStream->codecpar, _audioCodecCtx),
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

        _micStream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (_micStream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(mic) msg=Stream allocation returned null.");
        }

        _micCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_micCodecCtx == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3(mic) msg=Codec context allocation returned null.");
        }

        _micCodecCtx->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        _micCodecCtx->sample_rate = options.MicrophoneSampleRate;
        _micCodecCtx->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        _micCodecCtx->bit_rate = options.MicrophoneBitRate;
        _micCodecCtx->time_base = new AVRational { num = 1, den = options.MicrophoneSampleRate };
        ffmpeg.av_channel_layout_default(&_micCodecCtx->ch_layout, options.MicrophoneChannels);

        if (!IsSampleFormatSupported(codec, _micCodecCtx->sample_fmt))
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeMicrophoneIfNeeded msg=Requested sample format '{_micCodecCtx->sample_fmt}' is not supported by AAC encoder.");
        }

        // Skip GLOBAL_HEADER for MPEG-TS — AAC needs ADTS framing per segment.
        if (options.ContainerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            _micCodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(_micCodecCtx, codec, null), "avcodec_open2(mic)");

        _micFrameSize = _micCodecCtx->frame_size;
        if (_micFrameSize <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeMicrophoneIfNeeded msg=Unexpected AAC frame size value={_micFrameSize}");
        }

        _micStream->time_base = _micCodecCtx->time_base;
        _cachedMicTimeBase = _micCodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_micStream->codecpar, _micCodecCtx),
            "avcodec_parameters_from_context(mic)");

        AVChannelLayout inputLayout = default;
        ffmpeg.av_channel_layout_default(&inputLayout, options.MicrophoneChannels);
        var swrCtx = _micSwrCtx;
        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &_micCodecCtx->ch_layout,
                _micCodecCtx->sample_fmt,
                _micCodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                options.MicrophoneSampleRate,
                0,
                null);
            _micSwrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2(mic)");
            if (_micSwrCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2(mic) msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(_micSwrCtx), "swr_init(mic)");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }

        _micFrame = ffmpeg.av_frame_alloc();
        if (_micFrame == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(mic) msg=Frame allocation returned null.");
        }

        _micFrame->format = (int)_micCodecCtx->sample_fmt;
        _micFrame->nb_samples = _micFrameSize;
        _micFrame->sample_rate = _micCodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&_micFrame->ch_layout, &_micCodecCtx->ch_layout), "av_channel_layout_copy(mic_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(_micFrame, 0), "av_frame_get_buffer(mic)");
        if (_micFrame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_get_buffer(mic) msg=extended_data was null.");
        }

        _micAccumulatorCapacity = checked(_micFrameSize * options.MicrophoneChannels * sizeof(float));
        _micResampleBuffer = (byte*)ffmpeg.av_malloc((ulong)_micAccumulatorCapacity);
        if (_micResampleBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(mic_accumulator) msg=Allocation returned null size={_micAccumulatorCapacity}.");
        }

        _micSampleQueueCapacity = checked((_micFrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(_micSampleQueueCapacity * options.MicrophoneChannels * sizeof(float));
        _micSampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (_micSampleQueueBuffer == null)
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

            ApplyMasterDisplayMetadata(masteringMetadata, options.HdrMasterDisplayMetadata);
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

            ApplyMasterDisplayMetadata(masteringMetadata, options.HdrMasterDisplayMetadata);
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
        var swrCtx = _swrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &_audioCodecCtx->ch_layout,
                _audioCodecCtx->sample_fmt,
                _audioCodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                options.AudioSampleRate,
                0,
                null);
            _swrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2");
            if (_swrCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2 msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(_swrCtx), "swr_init");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }
    }

    private void AllocateAudioFrame()
    {
        _audioFrame = ffmpeg.av_frame_alloc();
        if (_audioFrame == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(audio) msg=Frame allocation returned null.");
        }

        _audioFrame->format = (int)_audioCodecCtx->sample_fmt;
        _audioFrame->nb_samples = _audioFrameSize;
        _audioFrame->sample_rate = _audioCodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&_audioFrame->ch_layout, &_audioCodecCtx->ch_layout), "av_channel_layout_copy(audio_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(_audioFrame, 0), "av_frame_get_buffer(audio)");

        if (_audioFrame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_get_buffer(audio) msg=extended_data was null.");
        }
    }

    private void AllocateAudioAccumulator(LibAvEncoderOptions options)
    {
        _accumulatorCapacity = checked(_audioFrameSize * options.AudioChannels * sizeof(float));
        _resampleBuffer = (byte*)ffmpeg.av_malloc((ulong)_accumulatorCapacity);
        if (_resampleBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(audio_accumulator) msg=Allocation returned null size={_accumulatorCapacity}.");
        }
    }

    private void AllocateAudioSampleQueue(LibAvEncoderOptions options)
    {
        _audioSampleQueueCapacity = checked((_audioFrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(_audioSampleQueueCapacity * options.AudioChannels * sizeof(float));
        _audioSampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (_audioSampleQueueBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(audio_sample_queue) msg=Allocation returned null size={queueBytes}.");
        }
    }

    private static void ApplyMasterDisplayMetadata(AVMasteringDisplayMetadata* metadata, string masterDisplayMetadata)
    {
        var match = MasterDisplayMetadataRegex.Match(masterDisplayMetadata);
        if (!match.Success)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=ApplyMasterDisplayMetadata msg=Invalid mastering metadata format value='{masterDisplayMetadata}'.");
        }

        var primaries = metadata->display_primaries;
        var red = primaries[0];
        red[0] = ToChromaticityRational(match.Groups[5].Value);
        red[1] = ToChromaticityRational(match.Groups[6].Value);
        primaries[0] = red;

        var green = primaries[1];
        green[0] = ToChromaticityRational(match.Groups[1].Value);
        green[1] = ToChromaticityRational(match.Groups[2].Value);
        primaries[1] = green;

        var blue = primaries[2];
        blue[0] = ToChromaticityRational(match.Groups[3].Value);
        blue[1] = ToChromaticityRational(match.Groups[4].Value);
        primaries[2] = blue;
        metadata->display_primaries = primaries;

        var whitePoint = metadata->white_point;
        whitePoint[0] = ToChromaticityRational(match.Groups[7].Value);
        whitePoint[1] = ToChromaticityRational(match.Groups[8].Value);
        metadata->white_point = whitePoint;

        metadata->max_luminance = ToLuminanceRational(match.Groups[9].Value);
        metadata->min_luminance = ToLuminanceRational(match.Groups[10].Value);
        metadata->has_primaries = 1;
        metadata->has_luminance = 1;
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

    private void DrainAudioEncoderPackets()
    {
        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_packet(_audioCodecCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "avcodec_receive_packet(audio)");

            try
            {
                WriteAudioPacket(_packet);
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void DrainMicEncoderPackets()
    {
        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_packet(_micCodecCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "avcodec_receive_packet(mic)");

            try
            {
                WriteMicPacket(_packet);
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
        _totalBytesWritten += packetSize;
    }

    private void WriteAudioPacket(AVPacket* packet)
    {
        ffmpeg.av_packet_rescale_ts(packet, _audioCodecCtx->time_base, _audioStream->time_base);
        packet->stream_index = _audioStream->index;
        var packetSize = packet->size;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_formatCtx, packet), "av_interleaved_write_frame(audio)");
        _totalBytesWritten += packetSize;
    }

    private void WriteMicPacket(AVPacket* packet)
    {
        ffmpeg.av_packet_rescale_ts(packet, _micCodecCtx->time_base, _micStream->time_base);
        packet->stream_index = _micStream->index;
        var packetSize = packet->size;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_formatCtx, packet), "av_interleaved_write_frame(mic)");
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

    private void EncodeAudioChunk(byte* inputPtr, int inputSamples)
    {
        if (_audioCodecCtx == null || _audioStream == null || _audioFrame == null || _swrCtx == null || inputSamples <= 0)
        {
            return;
        }

        var channelCount = GetAudioChannelCount();
        if (_audioSampleQueueBuffer == null || _audioSampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=EncodeAudioChunk msg=Audio sample queue is not allocated.");
        }

        if (_audioBufferedSamples < 0 || _audioBufferedSamples > _audioSampleQueueCapacity)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeAudioChunk msg=Audio queue sample count was out of range buffered={_audioBufferedSamples} capacity={_audioSampleQueueCapacity}.");
        }

        var availableSamples = _audioSampleQueueCapacity - _audioBufferedSamples;
        if (availableSamples < inputSamples + MaxDriftCorrectionSamplesPerPass)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeAudioChunk msg=Audio queue capacity exhausted buffered={_audioBufferedSamples} available={availableSamples} requested={inputSamples}.");
        }

        var inputData = stackalloc byte*[1];
        inputData[0] = inputPtr;

        var outputData = stackalloc byte*[channelCount];
        for (var channel = 0; channel < channelCount; channel++)
        {
            outputData[channel] = (byte*)(GetAudioQueuePlane(channel) + _audioBufferedSamples);
        }

        var convertedSamples = ffmpeg.swr_convert(
            _swrCtx,
            outputData,
            availableSamples,
            inputData,
            inputSamples);
        if (convertedSamples < 0)
        {
            ThrowIfError(convertedSamples, "swr_convert");
        }

        var queuedSamples = _audioBufferedSamples + convertedSamples;
        var queuedAudioSamples = _nextAudioPts + queuedSamples;
        var correctionSamples = GetDriftCorrectionSamples(
            queuedAudioSamples,
            _audioCodecCtx->sample_rate,
            out var correctionVideoFrame,
            out var driftMs);
        var appliedCorrectionSamples = 0;

        if (correctionSamples < 0)
        {
            var trimmedSamples = Math.Min(-correctionSamples, queuedSamples);
            queuedSamples -= trimmedSamples;
            appliedCorrectionSamples -= trimmedSamples;
        }
        else if (correctionSamples > 0)
        {
            AppendSilentSamples(queuedSamples, correctionSamples, channelCount);
            queuedSamples += correctionSamples;
            appliedCorrectionSamples += correctionSamples;
        }

        if (correctionSamples == 0 || appliedCorrectionSamples == correctionSamples)
        {
            _lastDriftCorrectionVideoFrame = correctionVideoFrame;
        }

        _audioBufferedSamples = queuedSamples;
        DrainBufferedAudioFrames(flushPartialFrame: false);

        if (appliedCorrectionSamples != 0)
        {
            _driftCorrectionAppliedSamples += appliedCorrectionSamples;
            Logger.Log(
                $"LIBAV_AV_DRIFT_CORRECTION videoFrame={_nextVideoPts} driftMs={driftMs:F1} " +
                $"correctionSamples={appliedCorrectionSamples} totalCorrectionSamples={_driftCorrectionAppliedSamples}");
        }
    }

    private void EncodeMicChunk(byte* inputPtr, int inputSamples)
    {
        if (_micCodecCtx == null || _micStream == null || _micFrame == null || _micSwrCtx == null || inputSamples <= 0)
        {
            return;
        }

        var channelCount = GetMicChannelCount();
        if (_micSampleQueueBuffer == null || _micSampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=EncodeMicChunk msg=Microphone sample queue is not allocated.");
        }

        if (_micBufferedSamples < 0 || _micBufferedSamples > _micSampleQueueCapacity)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeMicChunk msg=Microphone queue sample count was out of range buffered={_micBufferedSamples} capacity={_micSampleQueueCapacity}.");
        }

        var availableSamples = _micSampleQueueCapacity - _micBufferedSamples;
        if (availableSamples < inputSamples + MaxDriftCorrectionSamplesPerPass)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeMicChunk msg=Microphone queue capacity exhausted buffered={_micBufferedSamples} available={availableSamples} requested={inputSamples}.");
        }

        var inputData = stackalloc byte*[1];
        inputData[0] = inputPtr;

        var outputData = stackalloc byte*[channelCount];
        for (var channel = 0; channel < channelCount; channel++)
        {
            outputData[channel] = (byte*)(GetMicQueuePlane(channel) + _micBufferedSamples);
        }

        var convertedSamples = ffmpeg.swr_convert(
            _micSwrCtx,
            outputData,
            availableSamples,
            inputData,
            inputSamples);
        if (convertedSamples < 0)
        {
            ThrowIfError(convertedSamples, "swr_convert(mic)");
        }

        var queuedSamples = _micBufferedSamples + convertedSamples;
        var queuedMicSamples = _nextMicPts + queuedSamples;
        var correctionSamples = GetDriftCorrectionSamples(
            queuedMicSamples,
            _micCodecCtx->sample_rate,
            out _,
            out _,
            MicDriftCorrectionThresholdMs);

        if (correctionSamples < 0)
        {
            var trimmedSamples = Math.Min(-correctionSamples, queuedSamples);
            queuedSamples -= trimmedSamples;
        }
        else if (correctionSamples > 0)
        {
            AppendSilentMicSamples(queuedSamples, correctionSamples, channelCount);
            queuedSamples += correctionSamples;
        }

        _micBufferedSamples = queuedSamples;
        while (_micBufferedSamples >= _micFrameSize)
        {
            SendPreparedMicFrame(_micFrameSize);
            RemoveQueuedMicSamples(_micFrameSize);
        }
    }

    private void DrainBufferedAudioFrames(bool flushPartialFrame)
    {
        while (_audioBufferedSamples >= _audioFrameSize || (flushPartialFrame && _audioBufferedSamples > 0))
        {
            var sampleCount = _audioBufferedSamples >= _audioFrameSize
                ? _audioFrameSize
                : _audioBufferedSamples;
            SendPreparedAudioFrame(sampleCount);
            RemoveQueuedAudioSamples(sampleCount);
        }
    }

    private void DrainBufferedMicFrames(bool flushPartialFrame)
    {
        while (_micBufferedSamples >= _micFrameSize || (flushPartialFrame && _micBufferedSamples > 0))
        {
            var sampleCount = _micBufferedSamples >= _micFrameSize
                ? _micFrameSize
                : _micBufferedSamples;
            SendPreparedMicFrame(sampleCount);
            RemoveQueuedMicSamples(sampleCount);
        }
    }

    private void SendPreparedAudioFrame(int sampleCount)
    {
        if (_audioCodecCtx == null || _audioFrame == null || sampleCount <= 0)
        {
            return;
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(_audioFrame), "av_frame_make_writable(audio)");
        CopyQueuedSamplesToAudioFrame(sampleCount);

        _audioFrame->nb_samples = sampleCount;
        var nextPts = _nextAudioPts;
        _audioFrame->pts = nextPts;

        var sendResult = ffmpeg.avcodec_send_frame(_audioCodecCtx, _audioFrame);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainAudioEncoderPackets();
            sendResult = ffmpeg.avcodec_send_frame(_audioCodecCtx, _audioFrame);
        }

        ThrowIfError(sendResult, "avcodec_send_frame(audio)");
        _nextAudioPts = nextPts + sampleCount;
        DrainAudioEncoderPackets();
    }

    private void CopyQueuedSamplesToAudioFrame(int sampleCount)
    {
        if (_audioCodecCtx == null || _audioFrame == null || _audioFrame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToAudioFrame msg=Audio frame storage was not initialized.");
        }

        var bytesPerSample = ffmpeg.av_get_bytes_per_sample(_audioCodecCtx->sample_fmt);
        if (bytesPerSample <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToAudioFrame msg=Unsupported sample format '{_audioCodecCtx->sample_fmt}'.");
        }

        var channelCount = GetAudioChannelCount();
        if (ffmpeg.av_sample_fmt_is_planar(_audioCodecCtx->sample_fmt) == 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToAudioFrame msg=Expected planar audio frame layout.");
        }

        var planeBytes = sampleCount * bytesPerSample;
        for (var channel = 0; channel < channelCount; channel++)
        {
            var source = GetAudioQueuePlane(channel);
            var destination = (float*)_audioFrame->extended_data[channel];
            if (destination == null)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToAudioFrame msg=Audio plane pointer was null channel={channel}.");
            }

            Buffer.MemoryCopy(source, destination, planeBytes, planeBytes);
        }
    }

    private void RemoveQueuedAudioSamples(int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        if (sampleCount > _audioBufferedSamples)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=RemoveQueuedAudioSamples msg=Cannot remove more samples than buffered remove={sampleCount} buffered={_audioBufferedSamples}.");
        }

        var remainingSamples = _audioBufferedSamples - sampleCount;
        if (remainingSamples > 0)
        {
            var channelCount = GetAudioChannelCount();
            for (var channel = 0; channel < channelCount; channel++)
            {
                var plane = GetAudioQueuePlane(channel);
                new ReadOnlySpan<float>(plane + sampleCount, remainingSamples)
                    .CopyTo(new Span<float>(plane, remainingSamples));
            }
        }

        _audioBufferedSamples = remainingSamples;
    }

    private void AppendSilentSamples(int startSample, int sampleCount, int channelCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        for (var channel = 0; channel < channelCount; channel++)
        {
            new Span<float>(GetAudioQueuePlane(channel) + startSample, sampleCount).Clear();
        }
    }

    private float* GetAudioQueuePlane(int channel)
    {
        if (_audioSampleQueueBuffer == null || _audioSampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetAudioQueuePlane msg=Audio sample queue was not initialized.");
        }

        return (float*)(_audioSampleQueueBuffer + (channel * _audioSampleQueueCapacity * sizeof(float)));
    }

    private int GetAudioChannelCount()
    {
        var channelCount = (int)(_audioCodecCtx != null && _audioCodecCtx->ch_layout.nb_channels > 0
            ? _audioCodecCtx->ch_layout.nb_channels
            : _options?.AudioChannels ?? 0);
        if (channelCount <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetAudioChannelCount msg=Audio channel count was not available.");
        }

        return channelCount;
    }

    private void FlushPendingAudioSamples()
    {
        if (_audioCodecCtx == null || _audioFrame == null)
        {
            return;
        }

        if (_audioAccumulatorBytes > 0)
        {
            var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
            var inputBlockAlign = checked(options.AudioChannels * sizeof(float));
            if (_audioAccumulatorBytes % inputBlockAlign != 0)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=FlushPendingAudioSamples msg=Accumulator is not sample-aligned bytes={_audioAccumulatorBytes} block_align={inputBlockAlign}");
            }

            var pendingSamples = _audioAccumulatorBytes / inputBlockAlign;
            if (pendingSamples > 0)
            {
                EncodeAudioChunk(_resampleBuffer, pendingSamples);
            }

            _audioAccumulatorBytes = 0;
        }

        DrainBufferedAudioFrames(flushPartialFrame: true);
    }

    private void SendPreparedMicFrame(int sampleCount)
    {
        if (_micCodecCtx == null || _micFrame == null || sampleCount <= 0)
        {
            return;
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(_micFrame), "av_frame_make_writable(mic)");
        CopyQueuedMicSamplesToFrame(sampleCount);

        _micFrame->nb_samples = sampleCount;
        var nextPts = _nextMicPts;
        _micFrame->pts = nextPts;

        var sendResult = ffmpeg.avcodec_send_frame(_micCodecCtx, _micFrame);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainMicEncoderPackets();
            sendResult = ffmpeg.avcodec_send_frame(_micCodecCtx, _micFrame);
        }

        ThrowIfError(sendResult, "avcodec_send_frame(mic)");
        _nextMicPts = nextPts + sampleCount;
        DrainMicEncoderPackets();
    }

    private void CopyQueuedMicSamplesToFrame(int sampleCount)
    {
        if (_micCodecCtx == null || _micFrame == null || _micFrame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedMicSamplesToFrame msg=Microphone frame storage was not initialized.");
        }

        var bytesPerSample = ffmpeg.av_get_bytes_per_sample(_micCodecCtx->sample_fmt);
        if (bytesPerSample <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyQueuedMicSamplesToFrame msg=Unsupported sample format '{_micCodecCtx->sample_fmt}'.");
        }

        var channelCount = GetMicChannelCount();
        if (ffmpeg.av_sample_fmt_is_planar(_micCodecCtx->sample_fmt) == 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedMicSamplesToFrame msg=Expected planar audio frame layout.");
        }

        var planeBytes = sampleCount * bytesPerSample;
        for (var channel = 0; channel < channelCount; channel++)
        {
            var source = GetMicQueuePlane(channel);
            var destination = (float*)_micFrame->extended_data[channel];
            if (destination == null)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=CopyQueuedMicSamplesToFrame msg=Microphone plane pointer was null channel={channel}.");
            }

            Buffer.MemoryCopy(source, destination, planeBytes, planeBytes);
        }
    }

    private void RemoveQueuedMicSamples(int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        if (sampleCount > _micBufferedSamples)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=RemoveQueuedMicSamples msg=Cannot remove more samples than buffered remove={sampleCount} buffered={_micBufferedSamples}.");
        }

        var remainingSamples = _micBufferedSamples - sampleCount;
        if (remainingSamples > 0)
        {
            var channelCount = GetMicChannelCount();
            for (var channel = 0; channel < channelCount; channel++)
            {
                var plane = GetMicQueuePlane(channel);
                new ReadOnlySpan<float>(plane + sampleCount, remainingSamples)
                    .CopyTo(new Span<float>(plane, remainingSamples));
            }
        }

        _micBufferedSamples = remainingSamples;
    }

    private void AppendSilentMicSamples(int startSample, int sampleCount, int channelCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        for (var channel = 0; channel < channelCount; channel++)
        {
            new Span<float>(GetMicQueuePlane(channel) + startSample, sampleCount).Clear();
        }
    }

    private float* GetMicQueuePlane(int channel)
    {
        if (_micSampleQueueBuffer == null || _micSampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetMicQueuePlane msg=Microphone sample queue was not initialized.");
        }

        return (float*)(_micSampleQueueBuffer + (channel * _micSampleQueueCapacity * sizeof(float)));
    }

    private int GetMicChannelCount()
    {
        var channelCount = (int)(_micCodecCtx != null && _micCodecCtx->ch_layout.nb_channels > 0
            ? _micCodecCtx->ch_layout.nb_channels
            : _options?.MicrophoneChannels ?? 0);
        if (channelCount <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetMicChannelCount msg=Microphone channel count was not available.");
        }

        return channelCount;
    }

    private void FlushPendingMicSamples()
    {
        if (_micCodecCtx == null || _micFrame == null)
        {
            return;
        }

        if (_micAccumulatorBytes > 0)
        {
            var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
            var inputBlockAlign = checked(options.MicrophoneChannels * sizeof(float));
            if (_micAccumulatorBytes % inputBlockAlign != 0)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=FlushPendingMicSamples msg=Accumulator is not sample-aligned bytes={_micAccumulatorBytes} block_align={inputBlockAlign}");
            }

            var pendingSamples = _micAccumulatorBytes / inputBlockAlign;
            if (pendingSamples > 0)
            {
                EncodeMicChunk(_micResampleBuffer, pendingSamples);
            }

            _micAccumulatorBytes = 0;
        }

        while (_micBufferedSamples > 0)
        {
            var sampleCount = _micBufferedSamples >= _micFrameSize
                ? _micFrameSize
                : _micBufferedSamples;
            SendPreparedMicFrame(sampleCount);
            RemoveQueuedMicSamples(sampleCount);
        }
    }

    private void CopyToAudioAccumulator(ReadOnlySpan<byte> source, int destinationOffset)
    {
        if (source.IsEmpty)
        {
            return;
        }

        if (_resampleBuffer == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyToAudioAccumulator msg=Audio accumulator buffer is null.");
        }

        fixed (byte* sourcePtr = source)
        {
            Buffer.MemoryCopy(
                sourcePtr,
                _resampleBuffer + destinationOffset,
                _accumulatorCapacity - destinationOffset,
                source.Length);
        }
    }

    private void CopyToMicAccumulator(ReadOnlySpan<byte> source, int destinationOffset)
    {
        if (source.IsEmpty)
        {
            return;
        }

        if (_micResampleBuffer == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyToMicAccumulator msg=Microphone accumulator buffer is null.");
        }

        fixed (byte* sourcePtr = source)
        {
            Buffer.MemoryCopy(
                sourcePtr,
                _micResampleBuffer + destinationOffset,
                _micAccumulatorCapacity - destinationOffset,
                source.Length);
        }
    }

    private void CloseCurrentOutputIo()
    {
        if (_formatCtx == null || _formatCtx->pb == null)
        {
            return;
        }

        ThrowIfError(ffmpeg.avio_closep(&_formatCtx->pb), "avio_closep(rotate)");
    }

    private void FreeCurrentOutputContext()
    {
        if (_formatCtx == null)
        {
            return;
        }

        ffmpeg.avformat_free_context(_formatCtx);
        _formatCtx = null;
        _videoStream = null;
        _audioStream = null;
        _micStream = null;
        _headerWritten = false;
    }

    private void ReinitializeOutputContext(string outputPath)
    {
        var containerFormat = _options?.ContainerFormat ?? "mp4";
        AVFormatContext* formatCtx = null;
        ThrowIfError(
            ffmpeg.avformat_alloc_output_context2(&formatCtx, null, containerFormat, outputPath),
            "avformat_alloc_output_context2(rotate)");
        if (formatCtx == null)
        {
            throw CreateLibAvException(
                "LIBAV_ENCODER_ERROR operation=avformat_alloc_output_context2(rotate) msg=Output context allocation returned null.");
        }

        _formatCtx = formatCtx;
        ReinitializeVideoStream();
        ReinitializeAudioStream();
        ReinitializeMicrophoneStream();
        ReinitializeHdrBitstreamFilter();

        ThrowIfError(ffmpeg.avio_open2(&_formatCtx->pb, outputPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2(rotate)");

        AVDictionary* muxerOptions = null;
        try
        {
            if (containerFormat == "mp4")
            {
                ThrowIfError(ffmpeg.av_dict_set(&muxerOptions, "movflags", "+faststart", 0), "av_dict_set(movflags,rotate)");
            }
            ThrowIfError(ffmpeg.avformat_write_header(_formatCtx, &muxerOptions), "avformat_write_header(rotate)");
            _headerWritten = true;
        }
        finally
        {
            ffmpeg.av_dict_free(&muxerOptions);
        }
    }

    private void ReinitializeVideoStream()
    {
        if (_formatCtx == null || _videoCodecCtx == null)
        {
            throw new InvalidOperationException("Video rotation state is not initialized.");
        }

        _videoStream = ffmpeg.avformat_new_stream(_formatCtx, null);
        if (_videoStream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(rotate_video) msg=Stream allocation returned null.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_videoStream->codecpar, _videoCodecCtx),
            "avcodec_parameters_from_context(rotate_video)");
        _videoStream->time_base = _videoCodecCtx->time_base;
        _videoStream->avg_frame_rate = _videoCodecCtx->framerate;
        _videoStream->r_frame_rate = _videoCodecCtx->framerate;
    }

    private void ReinitializeAudioStream()
    {
        if (_formatCtx == null || _audioCodecCtx == null)
        {
            return;
        }

        _audioStream = ffmpeg.avformat_new_stream(_formatCtx, null);
        if (_audioStream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(rotate_audio) msg=Stream allocation returned null.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_audioStream->codecpar, _audioCodecCtx),
            "avcodec_parameters_from_context(rotate_audio)");
        _audioStream->time_base = _audioCodecCtx->time_base;
    }

    private void ReinitializeMicrophoneStream()
    {
        if (_formatCtx == null || _micCodecCtx == null)
        {
            return;
        }

        _micStream = ffmpeg.avformat_new_stream(_formatCtx, null);
        if (_micStream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(rotate_mic) msg=Stream allocation returned null.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_micStream->codecpar, _micCodecCtx),
            "avcodec_parameters_from_context(rotate_mic)");
        _micStream->time_base = _micCodecCtx->time_base;
    }

    private void ReinitializeHdrBitstreamFilter()
    {
        if (_bsfCtx != null)
        {
            var existingBsf = _bsfCtx;
            ffmpeg.av_bsf_free(&existingBsf);
            _bsfCtx = null;
        }

        var options = _options;
        if (options != null)
        {
            InitializeHdrBitstreamFilterIfNeeded(options);
        }
    }

    private void ResetSegmentRuntimeState()
    {
        // CRITICAL: Do NOT reset _nextVideoPts, _nextAudioPts, _nextMicPts.
        // NVENC has 1+ frame pipeline latency. After RotateOutput, the first
        // packet received from the encoder still carries the PREVIOUS segment's
        // PTS. If we reset to 0, the second packet (PTS=0) arrives AFTER that
        // old-PTS packet, causing av_interleaved_write_frame to fail with
        // EINVAL (non-monotonic PTS). Keeping PTS continuous across segments
        // is safe — FlashbackExporter remaps PTS per segment during concat.
        //
        // Also do NOT reset audio accumulators — the AAC encoder's internal
        // frame accumulator carries partial frames across segment boundaries.
        // Resetting would lose those samples and break A/V sync.
        _forceNextKeyframe = true;
        _encodedFrameCount = 0;
        _droppedFrameCount = 0;
        _audioSamplesReceived = 0;
        _micSamplesReceived = 0;
        _lastSyncLogVideoFrame = 0;
        _driftCorrectionAppliedSamples = 0;
        _lastDriftCorrectionVideoFrame = 0;
        _totalBytesWritten = 0;
        _flushSent = false;
    }

    private void CleanupResources(bool writeTrailer)
    {
        var outputPath = _options?.OutputPath;
        var normalClose = _isOpen;

        try
        {
            if (writeTrailer && _headerWritten && _formatCtx != null)
            {
                var trailerResult = ffmpeg.av_write_trailer(_formatCtx);
                if (trailerResult < 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_ERROR operation=av_write_trailer code={trailerResult} msg='{GetErrorString(trailerResult)}'");
                }
            }
        }
        finally
        {
            var useCudaHardwareFrames = _useCudaHardwareFrames;
            if (_formatCtx != null && _formatCtx->pb != null)
            {
                var closeResult = ffmpeg.avio_closep(&_formatCtx->pb);
                if (closeResult < 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_ERROR operation=avio_closep code={closeResult} msg='{GetErrorString(closeResult)}'");
                }
            }

            if (_bsfCtx != null)
            {
                var bsfCtx = _bsfCtx;
                ffmpeg.av_bsf_free(&bsfCtx);
                _bsfCtx = null;
            }

            if (_packet != null)
            {
                var packet = _packet;
                ffmpeg.av_packet_free(&packet);
                _packet = null;
            }

            if (_hwFrame != null)
            {
                var hwFrame = _hwFrame;
                ffmpeg.av_frame_free(&hwFrame);
                _hwFrame = null;
            }

            if (_hwFramesCtx != null)
            {
                var hwFramesCtx = _hwFramesCtx;
                ffmpeg.av_buffer_unref(&hwFramesCtx);
                _hwFramesCtx = null;
            }

            if (_hwDeviceCtx != null)
            {
                var hwDeviceCtx = _hwDeviceCtx;
                ffmpeg.av_buffer_unref(&hwDeviceCtx);
                _hwDeviceCtx = null;
            }

            _useHardwareFrames = false;
            _useCudaHardwareFrames = false;

            // Release our individual pool textures. Since initial_pool_size=0,
            // FFmpeg's frames context has no texture pool of its own. We own
            // all 8 textures and must Release each one.
            if (!useCudaHardwareFrames && _hwPoolTextures != null)
            {
                for (var i = 0; i < _hwPoolTextures.Length; i++)
                {
                    if (_hwPoolTextures[i] != IntPtr.Zero)
                    {
                        Marshal.Release(_hwPoolTextures[i]);
                        _hwPoolTextures[i] = IntPtr.Zero;
                    }
                }
                _hwPoolTextures = null;
            }

            if (_audioFrame != null)
            {
                var audioFrame = _audioFrame;
                ffmpeg.av_frame_free(&audioFrame);
                _audioFrame = null;
            }

            if (_micFrame != null)
            {
                var micFrame = _micFrame;
                ffmpeg.av_frame_free(&micFrame);
                _micFrame = null;
            }

            if (_videoFrame != null)
            {
                var videoFrame = _videoFrame;
                ffmpeg.av_frame_free(&videoFrame);
                _videoFrame = null;
            }

            if (_swrCtx != null)
            {
                var swrCtx = _swrCtx;
                ffmpeg.swr_free(&swrCtx);
                _swrCtx = null;
            }

            if (_micSwrCtx != null)
            {
                var micSwrCtx = _micSwrCtx;
                ffmpeg.swr_free(&micSwrCtx);
                _micSwrCtx = null;
            }

            if (_audioCodecCtx != null)
            {
                var audioCodecCtx = _audioCodecCtx;
                ffmpeg.avcodec_free_context(&audioCodecCtx);
                _audioCodecCtx = null;
            }

            if (_micCodecCtx != null)
            {
                var micCodecCtx = _micCodecCtx;
                ffmpeg.avcodec_free_context(&micCodecCtx);
                _micCodecCtx = null;
            }

            if (_videoCodecCtx != null)
            {
                var videoCodecCtx = _videoCodecCtx;
                ffmpeg.avcodec_free_context(&videoCodecCtx);
                _videoCodecCtx = null;
            }

            if (_resampleBuffer != null)
            {
                ffmpeg.av_free(_resampleBuffer);
                _resampleBuffer = null;
            }

            if (_audioSampleQueueBuffer != null)
            {
                ffmpeg.av_free(_audioSampleQueueBuffer);
                _audioSampleQueueBuffer = null;
            }

            if (_micResampleBuffer != null)
            {
                ffmpeg.av_free(_micResampleBuffer);
                _micResampleBuffer = null;
            }

            if (_micSampleQueueBuffer != null)
            {
                ffmpeg.av_free(_micSampleQueueBuffer);
                _micSampleQueueBuffer = null;
            }

            if (_formatCtx != null)
            {
                ffmpeg.avformat_free_context(_formatCtx);
                _formatCtx = null;
            }

            _videoStream = null;
            _audioStream = null;
            _micStream = null;
            _audioFrameSize = 0;
            _micFrameSize = 0;
            _accumulatorCapacity = 0;
            _audioSampleQueueCapacity = 0;
            _micAccumulatorCapacity = 0;
            _micSampleQueueCapacity = 0;
            _audioAccumulatorBytes = 0;
            _audioBufferedSamples = 0;
            _micAccumulatorBytes = 0;
            _micBufferedSamples = 0;
            _nextVideoPts = 0;
            _nextAudioPts = 0;
            _nextMicPts = 0;
            var finalMicSamplesReceived = _micSamplesReceived;
            _micSamplesReceived = 0;
            _cachedMicTimeBase = default;
            _isOpen = false;
            _headerWritten = false;
            _flushSent = false;

            var outputBytes = 0L;
            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
            {
                outputBytes = new FileInfo(outputPath).Length;
            }

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                if (normalClose)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLOSE output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} mic_samples={finalMicSamplesReceived} file_bytes={outputBytes}");
                }
                else if (_headerWritten || _encodedFrameCount > 0 || outputBytes > 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLEANUP init_failed=true output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} mic_samples={finalMicSamplesReceived} file_bytes={outputBytes}");
                }
            }
        }
    }

    private void EnsureOpen()
    {
        if (!_isOpen || _formatCtx == null || _videoCodecCtx == null || _videoStream == null || _videoFrame == null || _packet == null)
        {
            throw new InvalidOperationException("LibAvEncoder is not initialized.");
        }
    }

    private static void ValidateOptions(LibAvEncoderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            throw new ArgumentException("OutputPath is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.CodecName))
        {
            throw new ArgumentException("CodecName is required.", nameof(options));
        }

        if (options.Width <= 0 || options.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Width and Height must be positive.");
        }

        if (options.FrameRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "FrameRate must be positive.");
        }

        if (options.FrameRateNumerator.HasValue != options.FrameRateDenominator.HasValue)
        {
            throw new ArgumentException("FrameRateNumerator and FrameRateDenominator must be provided together.", nameof(options));
        }

        if (options.FrameRateNumerator is <= 0 || options.FrameRateDenominator is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "FrameRate numerator/denominator must be positive when provided.");
        }

        if (options.BitRate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "BitRate must be positive.");
        }

        if (!options.AudioEnabled)
        {
            goto ValidateHdrOptions;
        }

        if (options.AudioSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AudioSampleRate must be positive.");
        }

        if (options.AudioChannels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AudioChannels must be positive.");
        }

        if (options.AudioBitRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AudioBitRate must be positive.");
        }

        if (options.MicrophoneEnabled)
        {
            if (options.MicrophoneSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MicrophoneSampleRate must be positive.");
            }

            if (options.MicrophoneChannels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MicrophoneChannels must be positive.");
            }

            if (options.MicrophoneBitRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MicrophoneBitRate must be positive.");
            }
        }

ValidateHdrOptions:
        if (!options.HdrEnabled)
        {
            return;
        }

        if (!options.IsP010)
        {
            throw new InvalidOperationException("HDR10 encoding requires P010 input.");
        }

        if (!options.CodecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) &&
            !options.CodecName.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"HDR10 encoding requires HEVC or AV1, but codec '{options.CodecName}' was requested.");
        }
    }

    private static string? GetHdrBitstreamFilterName(string codecName)
    {
        if (codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase))
        {
            return "hevc_metadata";
        }

        if (codecName.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            return "av1_metadata";
        }

        return null;
    }

    private static int GetExpectedFrameSizeBytes(int width, int height, bool isP010)
        => isP010 ? width * height * 3 : (width * height * 3) / 2;

    private static string MapNvencPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset) || preset.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return "p4";
        }

        if (preset.Equals("Fast", StringComparison.OrdinalIgnoreCase))
        {
            return "p1";
        }

        if (preset.Equals("Slow", StringComparison.OrdinalIgnoreCase))
        {
            return "p7";
        }

        return preset.ToLowerInvariant();
    }

    private static bool IsSampleFormatSupported(AVCodec* codec, AVSampleFormat sampleFormat)
    {
        void* supportedFormats = null;
        var supportedFormatCount = 0;
        var result = ffmpeg.avcodec_get_supported_config(
            null,
            codec,
            AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT,
            0,
            &supportedFormats,
            &supportedFormatCount);
        if (result < 0 || supportedFormats == null || supportedFormatCount <= 0)
        {
            return true;
        }

        var formats = (AVSampleFormat*)supportedFormats;
        for (var i = 0; i < supportedFormatCount; i++)
        {
            if (formats[i] == sampleFormat)
            {
                return true;
            }
        }

        return false;
    }

    private static AVRational ToAvRational(double value)
    {
        var rational = ffmpeg.av_d2q(value, 1_000_000);
        if (rational.num == 0 || rational.den == 0)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=ToAvRational msg=Unable to convert frame rate value={value.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        return rational;
    }

    private static AVRational ResolveFrameRate(LibAvEncoderOptions options)
    {
        if (options.FrameRateNumerator.HasValue && options.FrameRateDenominator.HasValue)
        {
            return new AVRational
            {
                num = options.FrameRateNumerator.Value,
                den = options.FrameRateDenominator.Value
            };
        }

        return ToAvRational(options.FrameRate);
    }

    private static AVRational Invert(AVRational value)
    {
        if (value.num == 0)
        {
            return new AVRational { num = 0, den = 1 };
        }

        return new AVRational
        {
            num = value.den,
            den = value.num
        };
    }

    private static AVRational ToChromaticityRational(string value)
        => new()
        {
            num = int.Parse(value, CultureInfo.InvariantCulture),
            den = 50_000
        };

    private static AVRational ToLuminanceRational(string value)
        => new()
        {
            num = int.Parse(value, CultureInfo.InvariantCulture),
            den = 10_000
        };

    private int GetDriftCorrectionSamples(long audioSamples, int sampleRate, out long correctionVideoFrame, out double driftMs,
        double thresholdMs = DriftCorrectionThresholdMs)
    {
        correctionVideoFrame = 0;
        driftMs = 0.0;

        if (_options == null ||
            (!_options.AudioEnabled && !_options.MicrophoneEnabled) ||
            _nextVideoPts < MinimumAvSyncVideoFrames ||
            _nextVideoPts - _lastDriftCorrectionVideoFrame < AvSyncLogCadenceFrames)
        {
            return 0;
        }

        if (!TryGetAvSyncState(audioSamples, out var videoFrame, out _, out _, out _, out driftMs))
        {
            return 0;
        }

        correctionVideoFrame = videoFrame;
        if (Math.Abs(driftMs) <= thresholdMs || sampleRate <= 0)
        {
            return 0;
        }

        var correctionSamples = (int)(-(driftMs / 1000.0) * sampleRate);
        return Math.Clamp(correctionSamples, -MaxDriftCorrectionSamplesPerPass, MaxDriftCorrectionSamplesPerPass);
    }

    public bool TryGetCurrentAvSyncDrift(out double driftMs, out long correctionSamples)
    {
        driftMs = 0.0;
        correctionSamples = _driftCorrectionAppliedSamples;

        // Use cached time_base values instead of dereferencing codec context pointers.
        // The codec contexts can be freed by FlushAndClose on the encoding thread,
        // but the cached time_base structs are plain value types set once during open.
        var vtb = _cachedVideoTimeBase;
        var atb = _cachedAudioTimeBase;
        if (vtb.num <= 0 || vtb.den <= 0 || atb.num <= 0 || atb.den <= 0)
        {
            return false;
        }

        var videoFrame = _nextVideoPts;
        var audioSamples = _nextAudioPts + _audioBufferedSamples;
        var videoTimeSec = videoFrame * vtb.num / (double)vtb.den;
        var audioTimeSec = audioSamples * atb.num / (double)atb.den;
        driftMs = (audioTimeSec - videoTimeSec) * 1000.0;
        return true;
    }

    private void LogAvSyncIfDue()
    {
        if (_options == null ||
            !_options.AudioEnabled ||
            _nextVideoPts < MinimumAvSyncVideoFrames ||
            _nextVideoPts - _lastSyncLogVideoFrame < AvSyncLogCadenceFrames)
        {
            return;
        }

        if (!TryGetAvSyncState(_nextAudioPts + _audioBufferedSamples, out var videoFrame, out var videoTimeSec, out var audioSamples, out var audioTimeSec, out var driftMs))
        {
            return;
        }

        _lastSyncLogVideoFrame = videoFrame;

        Logger.Log(
            $"LIBAV_AV_SYNC videoFrame={videoFrame} videoSec={videoTimeSec:F3} " +
            $"audioSamples={audioSamples} audioSec={audioTimeSec:F3} driftMs={driftMs:F1} " +
            $"totalCorrectionSamples={_driftCorrectionAppliedSamples}");

        if (Math.Abs(driftMs) > 500.0)
        {
            Logger.Log(
                $"LIBAV_AV_SYNC_DRIFT_WARNING videoFrame={videoFrame} driftMs={driftMs:F1} " +
                $"audioSamples={audioSamples} — drift exceeds 500ms, investigate audio delivery");
        }
    }

    private bool TryGetAvSyncState(
        long audioSamples,
        out long videoFrame,
        out double videoTimeSec,
        out long reportedAudioSamples,
        out double audioTimeSec,
        out double driftMs)
    {
        videoFrame = _nextVideoPts;
        videoTimeSec = 0.0;
        reportedAudioSamples = audioSamples;
        audioTimeSec = 0.0;
        driftMs = 0.0;

        if (_videoCodecCtx == null ||
            _audioCodecCtx == null ||
            _videoCodecCtx->time_base.num <= 0 ||
            _videoCodecCtx->time_base.den <= 0 ||
            _audioCodecCtx->time_base.num <= 0 ||
            _audioCodecCtx->time_base.den <= 0)
        {
            return false;
        }

        videoTimeSec = videoFrame * _videoCodecCtx->time_base.num / (double)_videoCodecCtx->time_base.den;
        audioTimeSec = reportedAudioSamples * _audioCodecCtx->time_base.num / (double)_audioCodecCtx->time_base.den;
        driftMs = (audioTimeSec - videoTimeSec) * 1000.0;
        return true;
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
    public int GopSize { get; init; } = -1;
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

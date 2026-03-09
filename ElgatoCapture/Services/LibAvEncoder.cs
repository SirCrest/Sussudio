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
    private static readonly Regex MasterDisplayMetadataRegex = new(
        @"^G\((\d+),(\d+)\)B\((\d+),(\d+)\)R\((\d+),(\d+)\)WP\((\d+),(\d+)\)L\((\d+),(\d+)\)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static int _ffmpegInitialized;

    private AVFormatContext* _formatCtx;
    private AVCodecContext* _videoCodecCtx;
    private AVCodecContext* _audioCodecCtx;
    private AVStream* _videoStream;
    private AVStream* _audioStream;
    private AVFrame* _videoFrame;
    private AVFrame* _audioFrame;
    private AVPacket* _packet;
    private AVBSFContext* _bsfCtx;
    private SwrContext* _swrCtx;
    private LibAvEncoderOptions? _options;
    private long _nextVideoPts;
    private long _nextAudioPts;
    private long _encodedFrameCount;
    private long _droppedFrameCount;
    private long _audioSamplesReceived;
    private long _totalBytesWritten;
    private byte* _resampleBuffer;
    private int _audioFrameSize;
    private int _accumulatorCapacity;
    private int _audioAccumulatorBytes;
    private bool _isOpen;
    private bool _headerWritten;
    private bool _flushSent;
    private AVBufferRef* _hwDeviceCtx;
    private AVBufferRef* _hwFramesCtx;
    private AVFrame* _hwFrame;
    private bool _useHardwareFrames;
    private bool _useCudaHardwareFrames;
    private IntPtr[]? _hwPoolTextures; // individual ArraySize=1 D3D11 textures for the hw frames pool
    private int _hwPoolIndex; // round-robin index into _hwPoolTextures

    /// <summary>No-op free callback for av_buffer_create — our pool textures outlive individual frames.</summary>
    private static readonly av_buffer_create_free _hwPoolTextureFreeDelegate = (opaque, data) => { /* intentional no-op */ };
    private static readonly av_buffer_create_free_func _hwPoolTextureFree = _hwPoolTextureFreeDelegate;

    public long EncodedFrameCount => _encodedFrameCount;
    public long DroppedFrameCount => _droppedFrameCount;
    public long AudioSamplesReceived => _audioSamplesReceived;
    public long TotalBytesWritten => _totalBytesWritten;
    public bool IsEncoding => _isOpen;
    public bool AudioEnabled => _options?.AudioEnabled == true && _audioCodecCtx != null && _audioStream != null;
    public string VideoCodecName => _options?.CodecName ?? string.Empty;
    public string OutputPath => _options?.OutputPath ?? string.Empty;
    public bool UseHardwareFrames => _useHardwareFrames;
    public bool UseCudaHardwareFrames => _useCudaHardwareFrames;

    public static void InitializeFFmpeg()
    {
        if (Interlocked.Exchange(ref _ffmpegInitialized, 1) != 0)
        {
            return;
        }

        ffmpeg.RootPath = AppContext.BaseDirectory;

        try
        {
            Logger.Log($"LIBAV_INIT root_path='{ffmpeg.RootPath}' avcodec_version={ffmpeg.avcodec_version()}");
        }
        catch (Exception ex)
        {
            Logger.Log($"LIBAV_INIT_ERROR root_path='{ffmpeg.RootPath}' type={ex.GetType().Name} msg={ex.Message}");
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
                ffmpeg.avformat_alloc_output_context2(&formatCtx, null, "mp4", options.OutputPath),
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

            if ((_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
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
            _videoStream->avg_frame_rate = _videoCodecCtx->framerate;
            _videoStream->r_frame_rate = _videoCodecCtx->framerate;

            ThrowIfError(
                ffmpeg.avcodec_parameters_from_context(_videoStream->codecpar, _videoCodecCtx),
                "avcodec_parameters_from_context");

            InitializeHdrBitstreamFilterIfNeeded(options);
            InitializeAudioIfNeeded(options);

            ThrowIfError(ffmpeg.avio_open2(&_formatCtx->pb, options.OutputPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2");

            AVDictionary* muxerOptions = null;
            try
            {
                ThrowIfError(ffmpeg.av_dict_set(&muxerOptions, "movflags", "+faststart", 0), "av_dict_set(movflags)");
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
            _encodedFrameCount = 0;
            _droppedFrameCount = 0;
            _audioSamplesReceived = 0;
            _audioAccumulatorBytes = 0;
            _flushSent = false;
            _isOpen = true;

            Logger.Log(
                $"LIBAV_ENCODER_OPEN codec='{options.CodecName}' output='{options.OutputPath}' " +
                $"width={options.Width} height={options.Height} fps={options.FrameRate.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"bitrate={options.BitRate} pix_fmt='{(options.IsP010 ? "p010le" : "nv12")}' hdr={options.HdrEnabled} " +
                $"audio={options.AudioEnabled} audio_rate={options.AudioSampleRate} audio_channels={options.AudioChannels} audio_bitrate={options.AudioBitRate} " +
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
        _videoFrame->pts = _nextVideoPts++;

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

        _hwFrame->pts = _nextVideoPts++;

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

        _hwFrame->pts = _nextVideoPts++;

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

    public void FlushAndClose()
    {
        if (!_isOpen && _formatCtx == null && _videoCodecCtx == null && _audioCodecCtx == null)
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

        if ((_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
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

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_audioStream->codecpar, _audioCodecCtx),
            "avcodec_parameters_from_context(audio)");

        InitializeAudioResampler(options);
        AllocateAudioFrame();
        AllocateAudioAccumulator(options);
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

        ThrowIfError(ffmpeg.av_frame_make_writable(_audioFrame), "av_frame_make_writable(audio)");

        var inputData = stackalloc byte*[1];
        inputData[0] = inputPtr;

        var convertedSamples = ffmpeg.swr_convert(
            _swrCtx,
            _audioFrame->extended_data,
            inputSamples,
            inputData,
            inputSamples);
        if (convertedSamples < 0)
        {
            ThrowIfError(convertedSamples, "swr_convert");
        }

        if (convertedSamples != inputSamples)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=swr_convert msg=Unexpected sample count converted={convertedSamples} expected={inputSamples}");
        }

        _audioFrame->nb_samples = convertedSamples;
        _audioFrame->pts = _nextAudioPts;
        _nextAudioPts += convertedSamples;

        var sendResult = ffmpeg.avcodec_send_frame(_audioCodecCtx, _audioFrame);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainAudioEncoderPackets();
            sendResult = ffmpeg.avcodec_send_frame(_audioCodecCtx, _audioFrame);
        }

        ThrowIfError(sendResult, "avcodec_send_frame(audio)");
        DrainAudioEncoderPackets();
    }

    private void FlushPendingAudioSamples()
    {
        if (_audioCodecCtx == null || _audioFrame == null || _audioAccumulatorBytes <= 0)
        {
            return;
        }

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

            if (_audioCodecCtx != null)
            {
                var audioCodecCtx = _audioCodecCtx;
                ffmpeg.avcodec_free_context(&audioCodecCtx);
                _audioCodecCtx = null;
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

            if (_formatCtx != null)
            {
                ffmpeg.avformat_free_context(_formatCtx);
                _formatCtx = null;
            }

            _videoStream = null;
            _audioStream = null;
            _audioFrameSize = 0;
            _audioAccumulatorBytes = 0;
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
                        $"LIBAV_ENCODER_CLOSE output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} file_bytes={outputBytes}");
                }
                else if (_headerWritten || _encodedFrameCount > 0 || outputBytes > 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLEANUP init_failed=true output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} file_bytes={outputBytes}");
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
    public bool HdrEnabled { get; init; }
    public string? HdrMasterDisplayMetadata { get; init; }
    public int HdrMaxCll { get; init; }
    public int HdrMaxFall { get; init; }
    public IntPtr D3D11DevicePtr { get; init; }
    public IntPtr D3D11DeviceContextPtr { get; init; }
    public IntPtr CudaHwDeviceCtxPtr { get; init; }
    public IntPtr CudaHwFramesCtxPtr { get; init; }
}

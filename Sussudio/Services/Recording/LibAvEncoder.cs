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
    private long _totalBytesWritten;
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
    public long TotalBytesWritten => _totalBytesWritten;
    public bool IsEncoding => _isOpen;
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

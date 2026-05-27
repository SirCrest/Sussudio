using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
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

    /// <summary>No-op free callback for av_buffer_create; our pool textures outlive individual frames.</summary>
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


    /// <summary>Forwards to <see cref="FfmpegRuntimeInit.EnsureInitialized"/>.</summary>
    public static void InitializeFFmpeg(bool requireNativeRuntime = false)
        => FfmpegRuntimeInit.EnsureInitialized(requireNativeRuntime);

    private static void ValidateOptions(LibAvEncoderOptions options)
    {
        ValidateRequiredVideoOptions(options);

        if (options.AudioEnabled)
        {
            ValidateAudioOptions(options);
        }

        ValidateHdrOptions(options);
    }

    private static void ValidateRequiredVideoOptions(LibAvEncoderOptions options)
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
    }

    private static void ValidateAudioOptions(LibAvEncoderOptions options)
    {
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
    }

    private static void ValidateHdrOptions(LibAvEncoderOptions options)
    {
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

    private static string? GetVideoBitstreamFilterSpec(LibAvEncoderOptions options)
    {
        var filters = new List<string>();
        if (options.HdrEnabled)
        {
            var hdrFilter = GetHdrBitstreamFilterSpec(options.CodecName);
            if (hdrFilter != null)
            {
                filters.Add(hdrFilter);
            }
        }

        var parameterSetFilter = GetMpegTsParameterSetBitstreamFilterName(options);
        if (parameterSetFilter != null)
        {
            filters.Add(parameterSetFilter);
        }

        return filters.Count == 0
            ? null
            : string.Join(",", filters);
    }

    private static string? GetHdrBitstreamFilterSpec(string codecName)
    {
        if (codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase))
        {
            return "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9";
        }

        if (codecName.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            return "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9";
        }

        return null;
    }

    private static string? GetMpegTsParameterSetBitstreamFilterName(LibAvEncoderOptions options)
        => IsMpegTsParameterSetFilterCandidate(options) ? "dump_extra" : null;

    private static bool IsMpegTsParameterSetFilterCandidate(LibAvEncoderOptions options)
        => string.Equals(options.ContainerFormat, "mpegts", StringComparison.OrdinalIgnoreCase) &&
           (options.CodecName.Contains("h264", StringComparison.OrdinalIgnoreCase) ||
            options.CodecName.Contains("hevc", StringComparison.OrdinalIgnoreCase));

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

    private static bool SupportsSplitEncodeMode(string codecName)
        => codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
           codecName.Contains("265", StringComparison.OrdinalIgnoreCase) ||
           codecName.Contains("av1", StringComparison.OrdinalIgnoreCase);

    private static bool TryMapSplitEncodeMode(string? splitEncodeMode, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(splitEncodeMode) ||
            splitEncodeMode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (splitEncodeMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            value = 15;
            return true;
        }

        if (splitEncodeMode.Equals("2-way", StringComparison.OrdinalIgnoreCase) ||
            splitEncodeMode.Equals("2", StringComparison.OrdinalIgnoreCase))
        {
            value = 2;
            return true;
        }

        if (splitEncodeMode.Equals("3-way", StringComparison.OrdinalIgnoreCase) ||
            splitEncodeMode.Equals("3", StringComparison.OrdinalIgnoreCase))
        {
            value = 3;
            return true;
        }

        return false;
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

        if (!TryMapSplitEncodeMode(options.SplitEncodeMode, out var splitEncodeMode))
        {
            throw new InvalidOperationException($"Unknown split encode mode '{options.SplitEncodeMode}'.");
        }

        if (SupportsSplitEncodeMode(options.CodecName))
        {
            ThrowIfError(
                ffmpeg.av_opt_set_int(codecContext->priv_data, "split_encode_mode", splitEncodeMode, 0),
                "av_opt_set_int(split_encode_mode)");
        }
        else if (splitEncodeMode is 2 or 3)
        {
            throw new InvalidOperationException(
                $"Split encode mode '{options.SplitEncodeMode}' is not supported by codec '{options.CodecName}'.");
        }

        if (IsMpegTsParameterSetFilterCandidate(options))
        {
            ThrowIfError(
                ffmpeg.av_opt_set_int(codecContext->priv_data, "forced-idr", 1, 0),
                "av_opt_set_int(forced-idr)");
        }
    }

    private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)
    {
        var filterSpec = GetVideoBitstreamFilterSpec(options);
        if (filterSpec == null)
        {
            return;
        }

        AVBSFContext* bsfCtx = null;
        ThrowIfError(ffmpeg.av_bsf_list_parse_str(filterSpec, &bsfCtx), "av_bsf_list_parse_str");
        if (bsfCtx == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_BSF_INIT_FAIL codec='{options.CodecName}' filter='{filterSpec}' msg=Filter chain allocation returned null.");
        }

        _bsfCtx = bsfCtx;
        ThrowIfError(ffmpeg.avcodec_parameters_from_context(_bsfCtx->par_in, _videoCodecCtx), "avcodec_parameters_from_context(bsf)");
        _bsfCtx->time_base_in = _videoCodecCtx->time_base;

        ThrowIfError(ffmpeg.av_bsf_init(_bsfCtx), "av_bsf_init");
        Logger.Log($"LIBAV_ENCODER_BSF_INIT codec='{options.CodecName}' filter='{filterSpec}' hdr={options.HdrEnabled}");
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

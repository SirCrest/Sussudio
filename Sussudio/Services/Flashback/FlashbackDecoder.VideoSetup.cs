using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using FFmpeg.AutoGen;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    // get_format callback: tells the decoder to use D3D11VA when available.
    // Must be stored as a field to prevent GC collection while the decoder is alive.
    private static readonly AVCodecContext_get_format GetFormatD3D11Callback = GetFormatD3D11;

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

    private static AVPixelFormat GetFormatD3D11(AVCodecContext* ctx, AVPixelFormat* fmt)
    {
        for (var p = fmt; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
        {
            if (*p == AVPixelFormat.AV_PIX_FMT_D3D11)
                return AVPixelFormat.AV_PIX_FMT_D3D11;
        }

        var offered = new StringBuilder();
        for (var p = fmt; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
        {
            if (offered.Length > 0) offered.Append(',');
            offered.Append((int)*p);
        }

        Logger.Log($"FLASHBACK_DECODER_D3D11VA_NOT_OFFERED formats=[{offered}] fallback={(int)*fmt}");
        return *fmt;
    }

    /// <summary>
    /// Attempts to initialize a D3D11VA hardware decoder using the persistent device context.
    /// Returns true on success. Output textures live on the same D3D11 device as the renderer.
    /// </summary>
    private bool TryInitializeD3D11VADecoder(AVCodecParameters* codecPar)
    {
        if (_d3d11HwDeviceCtx == null)
            return false;

        if (codecPar->codec_id != AVCodecID.AV_CODEC_ID_H264 &&
            codecPar->codec_id != AVCodecID.AV_CODEC_ID_HEVC &&
            codecPar->codec_id != AVCodecID.AV_CODEC_ID_AV1)
        {
            return false;
        }

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

            // D3D11VA is activated by attaching the device context and selecting
            // AV_PIX_FMT_D3D11 from get_format during avcodec_open2.
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

    private const int MaxHardwareConfigCount = 64;
    private const int AvCodecHwConfigMethodHwDeviceCtx = 0x01;
    private const int AvCodecHwConfigMethodHwFramesCtx = 0x02;
    private const int AvCodecHwConfigMethodInternal = 0x04;
    private const int AvCodecHwConfigMethodAdHoc = 0x08;

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

    private void InitializeVideoDecoder()
    {
        // Reset hw accel flag; it persists across file opens but must reflect
        // the decoder chosen for this file.
        _isD3D11HwAccelerated = false;

        var videoStream = _formatCtx->streams[_videoStreamIndex];
        _videoTimeBase = videoStream->time_base;

        var codecPar = videoStream->codecpar;
        _videoWidth = codecPar->width;
        _videoHeight = codecPar->height;
        ValidateVideoDimensions(_videoWidth, _videoHeight);

        _decodedPixelFormat = (AVPixelFormat)codecPar->format;
        _isHdr = (codecPar->codec_id == AVCodecID.AV_CODEC_ID_HEVC ||
                  codecPar->codec_id == AVCodecID.AV_CODEC_ID_AV1) &&
                 (_decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P10LE ||
                  _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_P010LE);

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
            _frameRate = 30.0;
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

        if (TryInitializeD3D11VADecoder(codecPar))
        {
            _videoFrame = ffmpeg.av_frame_alloc();
            if (_videoFrame == null)
            {
                throw CreateException("Failed to allocate video frame.");
            }

            Logger.Log($"FLASHBACK_DECODER_VIDEO hw_accel=D3D11VA " +
                       $"sw_fmt={(_isHdr ? "P010" : "NV12")} {_videoWidth}x{_videoHeight}");
            return;
        }

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

        _videoFrame = ffmpeg.av_frame_alloc();
        if (_videoFrame == null)
        {
            throw CreateException("Failed to allocate video frame.");
        }

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
}

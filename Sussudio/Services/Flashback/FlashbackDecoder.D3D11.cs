using System;
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
}

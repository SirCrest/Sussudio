using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
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
}

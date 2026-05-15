using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private static LiveSignalText BuildLiveSignalText(CaptureRuntimeSnapshot runtime, string? encoderCodecName)
    {
        var width = runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth;
        var height = runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight;
        var resolution = width.HasValue && height.HasValue
            ? $"{width.Value}x{height.Value}"
            : LiveInfoUnavailable;

        var frameRateValue = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate;
        var frameRate = frameRateValue.HasValue && frameRateValue.Value > 0
            ? frameRateValue.Value.ToString("0.00")
            : LiveInfoUnavailable;

        var pixelFormat =
            runtime.ReaderSourceSubtype ??
            runtime.VideoNegotiatedSubtype ??
            runtime.NegotiatedPixelFormat ??
            runtime.LatestObservedFramePixelFormat ??
            runtime.RequestedReaderSubtype ??
            runtime.RequestedPixelFormat;
        var codecSuffix = encoderCodecName switch
        {
            "hevc_nvenc" => " / HEVC",
            "h264_nvenc" => " / H264",
            "av1_nvenc" => " / AV1",
            _ => ""
        };
        var pixelFormatText = string.IsNullOrWhiteSpace(pixelFormat)
            ? LiveInfoUnavailable
            : pixelFormat + codecSuffix;

        return new LiveSignalText(resolution, frameRate, pixelFormatText);
    }

    private readonly record struct LiveSignalText(
        string Resolution,
        string FrameRate,
        string PixelFormat);
}

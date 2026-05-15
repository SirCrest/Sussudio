using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static class LiveSignalTextPresentationBuilder
{
    internal static LiveSignalTextPresentation Build(
        CaptureRuntimeSnapshot runtime,
        string? encoderCodecName,
        string unavailableText)
    {
        var width = runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth;
        var height = runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight;
        var resolution = width.HasValue && height.HasValue
            ? $"{width.Value}x{height.Value}"
            : unavailableText;

        var frameRateValue = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate;
        var frameRate = frameRateValue.HasValue && frameRateValue.Value > 0
            ? frameRateValue.Value.ToString("0.00")
            : unavailableText;

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
            ? unavailableText
            : pixelFormat + codecSuffix;

        return new LiveSignalTextPresentation(resolution, frameRate, pixelFormatText);
    }
}

internal readonly record struct LiveSignalTextPresentation(
    string Resolution,
    string FrameRate,
    string PixelFormat);

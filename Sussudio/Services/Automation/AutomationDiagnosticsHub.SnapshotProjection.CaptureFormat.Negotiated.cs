using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,
            Height = captureRuntime.NegotiatedHeight ?? captureRuntime.ActualHeight,
            FrameRate = captureRuntime.NegotiatedFrameRate ?? captureRuntime.ActualFrameRate,
            FrameRateArg = captureRuntime.NegotiatedFrameRateArg ?? captureRuntime.ActualFrameRateArg,
            FrameRateNumerator = captureRuntime.NegotiatedFrameRateNumerator,
            FrameRateDenominator = captureRuntime.NegotiatedFrameRateDenominator,
            PixelFormat = captureRuntime.NegotiatedPixelFormat,
            MediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken
        };

    private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Negotiated.Width,
            Height = captureFormat.Negotiated.Height,
            FrameRate = captureFormat.Negotiated.FrameRate,
            FrameRateArg = captureFormat.Negotiated.FrameRateArg,
            FrameRateNumerator = captureFormat.Negotiated.FrameRateNumerator,
            FrameRateDenominator = captureFormat.Negotiated.FrameRateDenominator,
            PixelFormat = captureFormat.Negotiated.PixelFormat,
            MediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken
        };

    private readonly record struct CaptureFormatNegotiatedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? MediaSubtypeToken { get; init; }
    }

    private readonly record struct CaptureFormatNegotiatedFlattenedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? MediaSubtypeToken { get; init; }
    }
}

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.NegotiatedWidth,
            Height = captureFormat.NegotiatedHeight,
            FrameRate = captureFormat.NegotiatedFrameRate,
            FrameRateArg = captureFormat.NegotiatedFrameRateArg,
            FrameRateNumerator = captureFormat.NegotiatedFrameRateNumerator,
            FrameRateDenominator = captureFormat.NegotiatedFrameRateDenominator,
            PixelFormat = captureFormat.NegotiatedPixelFormat,
            MediaSubtypeToken = captureFormat.NegotiatedMediaSubtypeToken
        };

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

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.RequestedWidth,
            Height = captureFormat.RequestedHeight,
            FrameRate = captureFormat.RequestedFrameRate,
            FrameRateArg = captureFormat.RequestedFrameRateArg,
            FrameRateNumerator = captureFormat.RequestedFrameRateNumerator,
            FrameRateDenominator = captureFormat.RequestedFrameRateDenominator,
            PixelFormat = captureFormat.RequestedPixelFormat,
            Format = captureFormat.RequestedFormat,
            Quality = captureFormat.RequestedQuality,
            HdrEnabled = captureFormat.RequestedHdrEnabled,
            HdrMasteringMetadata = captureFormat.RequestedHdrMasteringMetadata,
            AudioEnabled = captureFormat.RequestedAudioEnabled
        };

    private readonly record struct CaptureFormatRequestedFlattenedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? Format { get; init; }
        public string? Quality { get; init; }
        public bool? HdrEnabled { get; init; }
        public bool? HdrMasteringMetadata { get; init; }
        public bool? AudioEnabled { get; init; }
    }
}

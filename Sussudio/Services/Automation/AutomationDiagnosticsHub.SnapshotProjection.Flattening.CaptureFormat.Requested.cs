namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Requested.Width,
            Height = captureFormat.Requested.Height,
            FrameRate = captureFormat.Requested.FrameRate,
            FrameRateArg = captureFormat.Requested.FrameRateArg,
            FrameRateNumerator = captureFormat.Requested.FrameRateNumerator,
            FrameRateDenominator = captureFormat.Requested.FrameRateDenominator,
            PixelFormat = captureFormat.Requested.PixelFormat,
            Format = captureFormat.Requested.Format,
            Quality = captureFormat.Requested.Quality,
            HdrEnabled = captureFormat.Requested.HdrEnabled,
            HdrMasteringMetadata = captureFormat.Requested.HdrMasteringMetadata,
            AudioEnabled = captureFormat.Requested.AudioEnabled
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

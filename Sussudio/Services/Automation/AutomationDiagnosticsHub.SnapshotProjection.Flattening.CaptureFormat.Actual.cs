namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.ActualWidth,
            Height = captureFormat.ActualHeight,
            FrameRate = captureFormat.ActualFrameRate,
            FrameRateArg = captureFormat.ActualFrameRateArg
        };

    private readonly record struct CaptureFormatActualFlattenedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
    }
}

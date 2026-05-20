namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            ActivationReason = captureFormat.HdrActivationReason,
            AutoDowngraded = captureFormat.HdrAutoDowngraded,
            AutoDowngradeReason = captureFormat.HdrAutoDowngradeReason,
            RequestedButSourceNot10Bit = captureFormat.HdrRequestedButSourceNot10Bit
        };

    private readonly record struct CaptureFormatHdrRequestFlattenedProjection
    {
        public string ActivationReason { get; init; }
        public bool AutoDowngraded { get; init; }
        public string AutoDowngradeReason { get; init; }
        public bool RequestedButSourceNot10Bit { get; init; }
    }
}

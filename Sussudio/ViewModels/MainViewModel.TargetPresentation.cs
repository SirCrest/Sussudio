using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Target presentation. Applies HDR runtime state and selected capture target
/// details to the UI target label.
/// </summary>
public partial class MainViewModel
{
    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
    }

    private void UpdateTargetSummary()
    {
        SourceTargetSummaryText = SourceTelemetryPresentationBuilder.BuildTargetSummary(
            GetSelectedResolutionDisplayText(),
            SelectedFrameRate,
            SelectedFriendlyFrameRate,
            SelectedExactFrameRate,
            SelectedExactFrameRateArg,
            HdrRuntimeState);
    }
}

namespace Sussudio.ViewModels;

/// <summary>
/// Source target summary presentation. Applies the selected capture target,
/// source-derived frame-rate display, and HDR runtime state to the UI label.
/// </summary>
public partial class MainViewModel
{
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

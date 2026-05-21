using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)
        {
            return new MainViewModelSourceTelemetryController(
                new MainViewModelSourceTelemetryControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,
                    BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,
                    SetSourceWidth = value => viewModel.SourceWidth = value,
                    SetSourceHeight = value => viewModel.SourceHeight = value,
                    SetSourceIsHdr = value => viewModel.SourceIsHdr = value,
                    IsRecording = () => viewModel.IsRecording,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetIsHdrEnabled = value => viewModel.IsHdrEnabled = value,
                    SetSourceTelemetryAvailability = value => viewModel.SourceTelemetryAvailability = value,
                    SetSourceTelemetryOriginDetail = value => viewModel.SourceTelemetryOriginDetail = value,
                    SetSourceTelemetryConfidence = value => viewModel.SourceTelemetryConfidence = value,
                    SetSourceTelemetryDiagnosticSummary = value => viewModel.SourceTelemetryDiagnosticSummary = value,
                    GetSourceTelemetryTimestampUtc = () => viewModel.SourceTelemetryTimestampUtc,
                    SetSourceTelemetryTimestampUtc = value => viewModel.SourceTelemetryTimestampUtc = value,
                    SetDetectedSourceFrameRate = value => viewModel.DetectedSourceFrameRate = value,
                    SetDetectedSourceFrameRateArg = value => viewModel.DetectedSourceFrameRateArg = value,
                    SetSourceFrameRateOrigin = value => viewModel.SourceFrameRateOrigin = value,
                    GetSourceTelemetrySummaryText = () => viewModel.SourceTelemetrySummaryText,
                    SetSourceTelemetrySummaryText = value => viewModel.SourceTelemetrySummaryText = value,
                    GetLastSourceModeKey = () => viewModel._lastSourceModeKey,
                    SetLastSourceModeKey = value => viewModel._lastSourceModeKey = value,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    IsAutoResolutionValue = MainViewModel.IsAutoResolutionValue,
                    HasUserOverriddenResolutionForCurrentMode = () => viewModel._hasUserOverriddenResolutionForCurrentMode,
                    SetHasUserOverriddenResolutionForCurrentMode = value => viewModel._hasUserOverriddenResolutionForCurrentMode = value,
                    IsAutoFrameRateSelected = () => viewModel.IsAutoFrameRateSelected,
                    HasUserOverriddenFrameRateForCurrentMode = () => viewModel._hasUserOverriddenFrameRateForCurrentMode,
                    SetHasUserOverriddenFrameRateForCurrentMode = value => viewModel._hasUserOverriddenFrameRateForCurrentMode = value,
                    ForceSourceAutoRetarget = () => viewModel._forceSourceAutoRetarget,
                    SetForceSourceAutoRetarget = value => viewModel._forceSourceAutoRetarget = value,
                    AvailableResolutionCount = () => viewModel.AvailableResolutions.Count,
                    SetPendingModeOptionsRefresh = value => viewModel._pendingModeOptionsRefresh = value,
                    RebuildResolutionOptions = viewModel.RebuildResolutionOptions,
                    UpdateTargetSummary = viewModel.UpdateTargetSummary,
                });
        }
    }
}

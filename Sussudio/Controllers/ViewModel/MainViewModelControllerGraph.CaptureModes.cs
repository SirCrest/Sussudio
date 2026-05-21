using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)
        {
            return new MainViewModelFrameRateTimingResolver(
                new MainViewModelFrameRateTimingResolverContext
                {
                    GetResolutionToFormats = () => viewModel._resolutionToFormats,
                    GetRuntimeSnapshot = () => viewModel._captureService.GetRuntimeSnapshot(),
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    AvailableFrameRates = viewModel.AvailableFrameRates,
                });
        }

        private static MainViewModelCaptureModeOptionRebuildController CreateCaptureModeOptionRebuildController(MainViewModel viewModel)
        {
            return new MainViewModelCaptureModeOptionRebuildController(
                new MainViewModelCaptureModeOptionRebuildControllerContext
                {
                    AvailableFormats = viewModel.AvailableFormats,
                    AvailableFrameRates = viewModel.AvailableFrameRates,
                    AvailableResolutions = viewModel.AvailableResolutions,
                    AvailableVideoFormats = viewModel.AvailableVideoFormats,
                    AutoResolutionValue = AutoResolutionValue,
                    AutoFrameRateValue = AutoFrameRateValue,
                    GetResolutionToFormats = () => viewModel._resolutionToFormats,
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,
                    TryResolveResolutionKey = viewModel.TryResolveResolutionKey,
                    GetEffectiveResolutionKey = viewModel.GetEffectiveResolutionKey,
                    ApplyResolvedFrameRateSelection = viewModel.ApplyResolvedFrameRateSelection,
                    GetSelectedResolutionDisplayText = viewModel.GetSelectedResolutionDisplayText,
                    BuildHdrSupportHintForResolution = viewModel.BuildHdrSupportHintForResolution,
                    UpdateTargetSummary = viewModel.UpdateTargetSummary,
                    NotifySelectedResolutionChanged = () => viewModel.OnPropertyChanged(nameof(SelectedResolution)),
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    SetSelectedResolution = value => viewModel.SelectedResolution = value,
                    GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                    GetSelectedVideoFormat = () => viewModel.SelectedVideoFormat,
                    SetSelectedVideoFormat = value => viewModel.SelectedVideoFormat = value,
                    SetSelectedFormat = value => viewModel.SelectedFormat = value,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    ShowAllCaptureOptions = () => viewModel.ShowAllCaptureOptions,
                    IsAutoFrameRateSelected = () => viewModel.IsAutoFrameRateSelected,
                    SetIsAutoFrameRateSelected = value => viewModel.IsAutoFrameRateSelected = value,
                    HasUserOverriddenResolutionForCurrentMode = () => viewModel._hasUserOverriddenResolutionForCurrentMode,
                    HasUserOverriddenFrameRateForCurrentMode = () => viewModel._hasUserOverriddenFrameRateForCurrentMode,
                    IsPendingSdrAutoSelectionForDeviceChange = () => viewModel._pendingSdrAutoSelectionForDeviceChange,
                    SetPendingSdrAutoSelectionForDeviceChange = value => viewModel._pendingSdrAutoSelectionForDeviceChange = value,
                    GetPendingSdrAutoFriendlyFrameRateBucket = () => viewModel._pendingSdrAutoFriendlyFrameRateBucket,
                    SetPendingSdrAutoFriendlyFrameRateBucket = value => viewModel._pendingSdrAutoFriendlyFrameRateBucket = value,
                    IsForceSourceAutoRetarget = () => viewModel._forceSourceAutoRetarget,
                    SetForceSourceAutoRetarget = value => viewModel._forceSourceAutoRetarget = value,
                    GetLastKnownResolutionKey = () => viewModel._lastKnownResolutionKey,
                    SetLastKnownResolutionKey = value => viewModel._lastKnownResolutionKey = value,
                    SetIsRebuildingModeOptions = value => viewModel._isRebuildingModeOptions = value,
                    SetIsApplyingAutomaticResolutionSelection = value => viewModel._isApplyingAutomaticResolutionSelection = value,
                    SetIsApplyingAutomaticFrameRateSelection = value => viewModel._isApplyingAutomaticFrameRateSelection = value,
                    IsSuppressFormatChangeReinitialize = () => viewModel._suppressFormatChangeReinitialize,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    SetDetectedSourceFrameRate = value => viewModel.DetectedSourceFrameRate = value,
                    SetDetectedSourceFrameRateArg = value => viewModel.DetectedSourceFrameRateArg = value,
                    SetSourceFrameRateOrigin = value => viewModel.SourceFrameRateOrigin = value,
                    SetAutoResolvedWidth = value => viewModel.AutoResolvedWidth = value,
                    SetAutoResolvedHeight = value => viewModel.AutoResolvedHeight = value,
                    SetAutoResolvedFrameRate = value => viewModel.AutoResolvedFrameRate = value,
                    SetHdrResolutionSupportHint = value => viewModel.HdrResolutionSupportHint = value,
                    SetDisabledResolutionReason = value => viewModel.DisabledResolutionReason = value,
                    SetStatusText = value => viewModel.StatusText = value,
                },
                viewModel._frameRateTimingResolver);
        }
    }
}

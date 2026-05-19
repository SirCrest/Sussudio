namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelCaptureSettingsAutomationController(
                new MainViewModelCaptureSettingsAutomationControllerContext
                {
                    InvokeBooleanOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableResolutions = () => viewModel.AvailableResolutions,
                    GetAvailableFrameRates = () => viewModel.AvailableFrameRates,
                    GetAvailableVideoFormats = () => viewModel.AvailableVideoFormats,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    SetSelectedResolution = value => viewModel.SelectedResolution = value,
                    SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                    SetSelectedVideoFormat = value => viewModel.SelectedVideoFormat = value,
                    SetMjpegDecoderCount = value => viewModel.MjpegDecoderCount = value,
                    SelectAutoFrameRate = viewModel.SelectAutoFrameRate,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,
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
                    GetResolutionToFormats = () => viewModel._resolutionToFormats,
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,
                    TryResolveResolutionKey = viewModel.TryResolveResolutionKey,
                    GetEffectiveResolutionKey = viewModel.GetEffectiveResolutionKey,
                    ResolvePreferredTimingFamily = viewModel.ResolvePreferredTimingFamily,
                    ResolveDetectedSourceFrameRate = viewModel.ResolveDetectedSourceFrameRate,
                    BuildFrameRateTimingVariants = viewModel.BuildFrameRateTimingVariants,
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
                });
        }
    }
}

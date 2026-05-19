using System;
using System.Linq;
using System.Threading;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)
        {
            return new MainViewModelDeviceAudioRequestController(
                new MainViewModelDeviceAudioRequestControllerContext
                {
                    EnqueueUiOperation = (operation, operationName, allowDuringDispose) =>
                        viewModel.EnqueueUiOperation(operation, operationName, allowDuringDispose),
                    IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,
                    IsLoadingSettings = () => viewModel._isLoadingSettings,
                    IsRefreshingDeviceAudioControls = () => viewModel._isRefreshingDeviceAudioControls,
                    IsDeviceAudioControlSupported = () => viewModel.IsDeviceAudioControlSupported,
                    IsRecording = () => viewModel.IsRecording,
                    GetSelectedDeviceAudioMode = () => viewModel.SelectedDeviceAudioMode,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    SaveSettings = viewModel.SaveSettings,
                    RefreshDeviceAudioControlsAsync = viewModel.RefreshDeviceAudioControlsAsync,
                    ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>
                        viewModel.ApplyDeviceAudioModeAsync(reason, targetDevice: targetDevice, cancellationToken: cancellationToken),
                    ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>
                        viewModel.ApplyAnalogAudioGainAsync(reason, targetDevice: targetDevice, cancellationToken: cancellationToken),
                    IsCurrentSelectedDevice = viewModel.IsCurrentSelectedDevice,
                });
        }

        private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)
        {
            return new MainViewModelDeviceFormatProbeController(
                new MainViewModelDeviceFormatProbeControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    ReadDeviceScanGeneration = () => Interlocked.Read(ref viewModel._deviceScanGeneration),
                    FindDeviceById = deviceId => viewModel.Devices.FirstOrDefault(
                        device => string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase)),
                    SetPendingSdrAutoSelectionForDeviceChange = value => viewModel._pendingSdrAutoSelectionForDeviceChange = value,
                    SetPendingSdrAutoFriendlyFrameRateBucket = value => viewModel._pendingSdrAutoFriendlyFrameRateBucket = value,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    IsRecording = () => viewModel.IsRecording,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    RebuildSelectedDeviceCapabilities = (device, resetTelemetryState) =>
                        viewModel.RebuildSelectedDeviceCapabilities(device, resetTelemetryState),
                    CreateRetargetApplier = () => new MainViewModelDeviceFormatProbeRetargetApplier(
                        new MainViewModelDeviceFormatProbeRetargetApplierContext
                        {
                            IsHdrEnabled = () => viewModel.IsHdrEnabled,
                            GetSelectedResolution = () => viewModel.SelectedResolution,
                            SetSelectedResolution = value => viewModel.SelectedResolution = value,
                            GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                            SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                            GetSelectedFormat = () => viewModel.SelectedFormat,
                            AvailableResolutionsContains = value => viewModel.AvailableResolutions.Any(
                                option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)),
                            SetIsRebuildingModeOptions = value => viewModel._isRebuildingModeOptions = value,
                            SetIsApplyingAutomaticResolutionSelection = value => viewModel._isApplyingAutomaticResolutionSelection = value,
                            SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                            RebuildFrameRateOptions = viewModel.RebuildFrameRateOptions,
                            ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,
                            EnqueueUiOperation = (operation, operationName) => viewModel.EnqueueUiOperation(operation, operationName),
                            GetCaptureRuntimeSnapshot = viewModel.GetCaptureRuntimeSnapshot,
                            UpdateSelectedFormat = viewModel.UpdateSelectedFormat,
                            UpdateTargetSummary = viewModel.UpdateTargetSummary,
                        }),
                });
        }

        private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelDeviceRefreshController(
                new MainViewModelDeviceRefreshControllerContext
                {
                    SetStatusText = value => viewModel.StatusText = value,
                    IncrementDeviceScanGeneration = () => Interlocked.Increment(ref viewModel._deviceScanGeneration),
                    GetSelectedAudioInputDeviceId = () => viewModel.SelectedAudioInputDevice?.Id,
                    GetSelectedMicrophoneDeviceId = () => viewModel.SelectedMicrophoneDevice?.Id,
                    GetSelectedDeviceId = () => viewModel.SelectedDevice?.Id,
                    EnumerateCaptureDeviceDiscoveryAsync = () =>
                        viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false),
                    ApplyStartupAudioDeviceScan = viewModel.ApplyStartupAudioDeviceScan,
                    ReplaceDevices = devices => ReplaceCollection(viewModel.Devices, devices),
                    GetDevices = () => viewModel.Devices,
                    BeginBackgroundFormatProbe = (device, scanGeneration) =>
                        viewModel._deviceService.BeginBackgroundFormatProbe(device, scanGeneration),
                    GetLastDiscoverySummary = () => viewModel._deviceService.LastDiscoverySummary,
                    SetSelectedDevice = device => viewModel.SelectedDevice = device,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetPendingSavedDeviceId = () => viewModel._pendingSavedDeviceId,
                    SetPendingSavedDeviceId = value => viewModel._pendingSavedDeviceId = value,
                },
                previewLifecycleController);
        }

        private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)
        {
            return new MainViewModelSourceTelemetryController(
                new MainViewModelSourceTelemetryControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,
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

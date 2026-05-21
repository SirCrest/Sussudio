using System;
using System.Linq;
using System.Threading;
using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
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
    }
}

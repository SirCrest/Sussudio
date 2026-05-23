using System;
using System.Threading;
using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private MainViewModelControllerGraph(
            MainViewModelUiDispatchController uiDispatchController,
            MainViewModelRecordingTransitionController recordingTransitionController,
            MainViewModelPreviewLifecycleController previewLifecycleController,
            MainViewModelDeviceAudioRequestController deviceAudioRequestController,
            MainViewModelRecordingCapabilityController recordingCapabilityController,
            MainViewModelCaptureSettingsAutomationController captureSettingsAutomationController,
            MainViewModelRecordingSettingsAutomationController recordingSettingsAutomationController,
            MainViewModelCaptureModeOptionRebuildController captureModeOptionRebuildController,
            MainViewModelDeviceFormatProbeController deviceFormatProbeController,
            MainViewModelSourceTelemetryController sourceTelemetryController,
            MainViewModelDeviceRefreshController deviceRefreshController,
            MainViewModelRuntimeLifecycleController runtimeLifecycleController,
            MainViewModelDisposalController disposalController)
        {
            UiDispatchController = uiDispatchController;
            RecordingTransitionController = recordingTransitionController;
            PreviewLifecycleController = previewLifecycleController;
            DeviceAudioRequestController = deviceAudioRequestController;
            RecordingCapabilityController = recordingCapabilityController;
            CaptureSettingsAutomationController = captureSettingsAutomationController;
            RecordingSettingsAutomationController = recordingSettingsAutomationController;
            CaptureModeOptionRebuildController = captureModeOptionRebuildController;
            DeviceFormatProbeController = deviceFormatProbeController;
            SourceTelemetryController = sourceTelemetryController;
            DeviceRefreshController = deviceRefreshController;
            RuntimeLifecycleController = runtimeLifecycleController;
            DisposalController = disposalController;
        }

        public MainViewModelUiDispatchController UiDispatchController { get; }
        public MainViewModelRecordingTransitionController RecordingTransitionController { get; }
        public MainViewModelPreviewLifecycleController PreviewLifecycleController { get; }
        public MainViewModelDeviceAudioRequestController DeviceAudioRequestController { get; }
        public MainViewModelRecordingCapabilityController RecordingCapabilityController { get; }
        public MainViewModelCaptureSettingsAutomationController CaptureSettingsAutomationController { get; }
        public MainViewModelRecordingSettingsAutomationController RecordingSettingsAutomationController { get; }
        public MainViewModelCaptureModeOptionRebuildController CaptureModeOptionRebuildController { get; }
        public MainViewModelDeviceFormatProbeController DeviceFormatProbeController { get; }
        public MainViewModelSourceTelemetryController SourceTelemetryController { get; }
        public MainViewModelDeviceRefreshController DeviceRefreshController { get; }
        public MainViewModelRuntimeLifecycleController RuntimeLifecycleController { get; }
        public MainViewModelDisposalController DisposalController { get; }

        public static MainViewModelControllerGraph Create(MainViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            var uiDispatchController = CreateUiDispatchController(viewModel);
            var previewLifecycleController = CreatePreviewLifecycleController(viewModel);
            var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);
            var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);
            var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);
            var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);
            var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);
            var captureModeOptionRebuildController = CreateCaptureModeOptionRebuildController(viewModel);
            var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);
            var sourceTelemetryController = CreateSourceTelemetryController(viewModel);
            var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);
            var runtimeLifecycleController = CreateRuntimeLifecycleController(
                viewModel,
                previewLifecycleController,
                deviceFormatProbeController,
                sourceTelemetryController);
            var disposalController = CreateDisposalController(viewModel, deviceAudioRequestController, runtimeLifecycleController);

            return new MainViewModelControllerGraph(
                uiDispatchController,
                recordingTransitionController,
                previewLifecycleController,
                deviceAudioRequestController,
                recordingCapabilityController,
                captureSettingsAutomationController,
                recordingSettingsAutomationController,
                captureModeOptionRebuildController,
                deviceFormatProbeController,
                sourceTelemetryController,
                deviceRefreshController,
                runtimeLifecycleController,
                disposalController);
        }

        private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)
        {
            return new MainViewModelUiDispatchController(
                new MainViewModelUiDispatchControllerContext
                {
                    DispatcherQueue = viewModel._dispatcherQueue,
                    IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,
                    Log = message => Logger.Log(message),
                    LogException = exception => Logger.LogException(exception),
                    SetStatusText = value => viewModel.StatusText = value,
                });
        }

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

        private static MainViewModelDisposalController CreateDisposalController(
            MainViewModel viewModel,
            MainViewModelDeviceAudioRequestController deviceAudioRequestController,
            MainViewModelRuntimeLifecycleController runtimeLifecycleController)
        {
            return new MainViewModelDisposalController(
                new MainViewModelDisposalControllerContext
                {
                    TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,
                    CancelActiveFlashbackExport = viewModel.CancelActiveFlashbackExportForDispose,
                    CancelPendingAudioControlWork = deviceAudioRequestController.CancelPendingAudioControlWork,
                    StopRuntimeForDispose = runtimeLifecycleController.StopForDispose,
                    CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),
                    DisposeSessionCoordinatorAsync = () => viewModel._sessionCoordinator.DisposeAsync().AsTask(),
                    DisposeCaptureServiceAsync = () => viewModel._captureService.DisposeAsync().AsTask(),
                    DisposeCaptureService = viewModel._captureService.Dispose,
                    AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,
                });
        }
    }
}

using System;
using System.Threading;
using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelControllerGraph
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
            var recordingTransitionController = new MainViewModelRecordingTransitionController(viewModel, previewLifecycleController);
            var deviceAudioRequestController = new MainViewModelDeviceAudioRequestController(viewModel);
            var recordingCapabilityController = new MainViewModelRecordingCapabilityController(viewModel);
            var captureSettingsAutomationController = new MainViewModelCaptureSettingsAutomationController(viewModel);
            var recordingSettingsAutomationController = new MainViewModelRecordingSettingsAutomationController(viewModel);
            var captureModeOptionRebuildController = new MainViewModelCaptureModeOptionRebuildController(viewModel);
            var deviceFormatProbeController = new MainViewModelDeviceFormatProbeController(viewModel);
            var sourceTelemetryController = new MainViewModelSourceTelemetryController(viewModel);
            var deviceRefreshController = new MainViewModelDeviceRefreshController(viewModel, previewLifecycleController);
            var runtimeLifecycleController = new MainViewModelRuntimeLifecycleController(viewModel, previewLifecycleController);
            var disposalController = new MainViewModelDisposalController(viewModel);

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

        private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)
        {
            return new MainViewModelPreviewLifecycleController(
                viewModel,
                new MainViewModelPreviewLifecycleControllerContext
                {
                    SessionCoordinator = viewModel._sessionCoordinator,
                    BuildCaptureSettings = viewModel.BuildCaptureSettings,
                    InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,
                    IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,
                    ApplyLatestSourceTelemetryForPreviewStart = () =>
                        viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot(
                            viewModel._captureService.GetLatestSourceTelemetrySnapshot(),
                            allowAutoRetarget: true),
                });
        }
    }
}

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
            var runtimeLifecycleController = CreateRuntimeLifecycleController(viewModel, previewLifecycleController);
            var disposalController = CreateDisposalController(viewModel);

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
                new MainViewModelPreviewLifecycleControllerContext
                {
                    SessionCoordinator = viewModel._sessionCoordinator,
                    BuildCaptureSettings = viewModel.BuildCaptureSettings,
                    InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,
                    CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(
                        new MainViewModelPreviewReinitializeControllerContext
                        {
                            SelectedDevice = () => viewModel.SelectedDevice,
                            SelectedFormat = () => viewModel.SelectedFormat,
                            IsRecording = () => viewModel.IsRecording,
                            IsInitialized = () => viewModel.IsInitialized,
                            SetIsInitialized = value => viewModel.IsInitialized = value,
                            IsPreviewing = () => viewModel.IsPreviewing,
                            IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,
                            SetIsPreviewReinitializing = value => viewModel.IsPreviewReinitializing = value,
                            SetStatusText = value => viewModel.StatusText = value,
                            CancelPreviewRestartAfterReinitialize = () => viewModel._cancelPreviewRestartAfterReinitialize,
                            SetCancelPreviewRestartAfterReinitialize = value => viewModel._cancelPreviewRestartAfterReinitialize = value,
                            IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),
                            ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),
                            PendingFlashbackCycleTask = () => viewModel._pendingFlashbackCycleTask,
                            ClearPendingFlashbackCycleIfSameAndCompleted = task =>
                            {
                                if (ReferenceEquals(viewModel._pendingFlashbackCycleTask, task) && task.IsCompleted)
                                {
                                    viewModel._pendingFlashbackCycleTask = null;
                                }
                            },
                            WaitReinitializeGateAsync = viewModel._previewReinitializeGate.WaitAsync,
                            ReleaseReinitializeGate = () => viewModel._previewReinitializeGate.Release(),
                            NotifyPreviewReinitRequestedAsync = viewModel.NotifyPreviewReinitRequestedAsync,
                            NotifyRendererStopAsync = viewModel.NotifyRendererStopAsync,
                        },
                        controller),
                    SelectedDevice = () => viewModel.SelectedDevice,
                    SetSelectedDevice = device => viewModel.SelectedDevice = device,
                    IsInitialized = () => viewModel.IsInitialized,
                    SetIsInitialized = value => viewModel.IsInitialized = value,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    SetIsPreviewing = value => viewModel.IsPreviewing = value,
                    IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,
                    IsRecording = () => viewModel.IsRecording,
                    ShouldStartAudioPreview = () => viewModel.IsAudioPreviewEnabled && viewModel.IsAudioEnabled,
                    IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,
                    SetStatusText = value => viewModel.StatusText = value,
                    RaisePreviewStartRequested = () => viewModel.PreviewStartRequested?.Invoke(viewModel, EventArgs.Empty),
                    RaisePreviewStopRequested = () => viewModel.PreviewStopRequested?.Invoke(viewModel, EventArgs.Empty),
                    ApplyLatestSourceTelemetryForPreviewStart = () =>
                        viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot(
                            viewModel._captureService.GetLatestSourceTelemetrySnapshot(),
                            allowAutoRetarget: true),
                });
        }

        private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelRuntimeLifecycleController(
                new MainViewModelRuntimeLifecycleControllerContext
                {
                    CreateEventIngressController = () => new MainViewModelRuntimeEventIngressController(viewModel, previewLifecycleController),
                    CreateTimer = viewModel._dispatcherQueue.CreateTimer,
                    GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,
                    GetLatestSourceTelemetrySnapshot = viewModel._captureService.GetLatestSourceTelemetrySnapshot,
                    SetLatestSourceTelemetrySnapshot = snapshot => viewModel._latestSourceTelemetry = snapshot,
                    ApplySourceTelemetrySnapshot = viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot,
                    UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot = () => viewModel.UpdateHdrRuntimeStatusFromCapture(),
                    UpdateHdrRuntimeStatusFromCaptureWithSnapshot = snapshot => viewModel.UpdateHdrRuntimeStatusFromCapture(snapshot),
                    UpdateLiveCaptureInfoWithoutSnapshot = () => viewModel.UpdateLiveCaptureInfo(),
                    UpdateLiveCaptureInfoWithSnapshot = snapshot => viewModel.UpdateLiveCaptureInfo(snapshot),
                    ResetLiveCaptureInfo = viewModel.ResetLiveCaptureInfo,
                    UpdateDiskSpace = viewModel.UpdateDiskSpace,
                    RefreshSourceTelemetrySummaryAge = viewModel._sourceTelemetryController.RefreshSourceTelemetrySummaryAge,
                    IsRecording = () => viewModel.IsRecording,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsFlashbackActive = () => viewModel._captureService.IsFlashbackActive,
                    GetRecordingElapsed = () => viewModel._recordingStopwatch.Elapsed,
                    SetRecordingTime = value => viewModel.RecordingTime = value,
                    UpdateRecordingStats = viewModel.UpdateRecordingStats,
                    UpdateFlashbackBitrate = viewModel.UpdateFlashbackBitrate,
                    DisposeAudioDeviceWatcher = viewModel._audioDeviceWatcher.Dispose,
                });
        }

        private static MainViewModelDisposalController CreateDisposalController(MainViewModel viewModel)
        {
            return new MainViewModelDisposalController(
                new MainViewModelDisposalControllerContext
                {
                    TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,
                    CancelActiveFlashbackExport = viewModel.CancelActiveFlashbackExportForDispose,
                    CancelPendingAudioControlWork = viewModel._deviceAudioRequestController.CancelPendingAudioControlWork,
                    StopRuntimeForDispose = viewModel._runtimeLifecycleController.StopForDispose,
                    CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),
                    DisposeSessionCoordinatorAsync = () => viewModel._sessionCoordinator.DisposeAsync().AsTask(),
                    DisposeCaptureServiceAsync = () => viewModel._captureService.DisposeAsync().AsTask(),
                    DisposeCaptureService = viewModel._captureService.Dispose,
                });
        }
    }
}

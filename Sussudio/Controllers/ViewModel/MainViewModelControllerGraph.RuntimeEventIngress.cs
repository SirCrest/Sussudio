using System;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController,
            MainViewModelDeviceFormatProbeController deviceFormatProbeController,
            MainViewModelSourceTelemetryController sourceTelemetryController)
        {
            return new MainViewModelRuntimeEventIngressController(
                new MainViewModelRuntimeEventIngressControllerContext
                {
                    AttachFormatProbeCompleted = handler => viewModel._deviceService.FormatProbeCompleted += handler,
                    DetachFormatProbeCompleted = handler => viewModel._deviceService.FormatProbeCompleted -= handler,
                    OnDeviceFormatProbeCompleted = deviceFormatProbeController.OnDeviceFormatProbeCompleted,
                    AttachCaptureStatusChanged = handler => viewModel._captureService.StatusChanged += handler,
                    DetachCaptureStatusChanged = handler => viewModel._captureService.StatusChanged -= handler,
                    AttachCaptureErrorOccurred = handler => viewModel._captureService.ErrorOccurred += handler,
                    DetachCaptureErrorOccurred = handler => viewModel._captureService.ErrorOccurred -= handler,
                    AttachCapturePreCleanupRequested = handler => viewModel._captureService.PreCleanupRequested += handler,
                    DetachCapturePreCleanupRequested = handler => viewModel._captureService.PreCleanupRequested -= handler,
                    AttachFrameCaptured = handler => viewModel._captureService.FrameCaptured += handler,
                    DetachFrameCaptured = handler => viewModel._captureService.FrameCaptured -= handler,
                    AttachAudioLevelUpdated = handler => viewModel._captureService.AudioLevelUpdated += handler,
                    DetachAudioLevelUpdated = handler => viewModel._captureService.AudioLevelUpdated -= handler,
                    OnAudioLevelUpdated = viewModel.OnAudioLevelUpdated,
                    AttachMicrophoneAudioLevelUpdated = handler => viewModel._captureService.MicrophoneAudioLevelUpdated += handler,
                    DetachMicrophoneAudioLevelUpdated = handler => viewModel._captureService.MicrophoneAudioLevelUpdated -= handler,
                    OnMicrophoneAudioLevelUpdated = viewModel.OnMicrophoneAudioLevelUpdated,
                    AttachSourceTelemetryUpdated = handler => viewModel._captureService.SourceTelemetryUpdated += handler,
                    DetachSourceTelemetryUpdated = handler => viewModel._captureService.SourceTelemetryUpdated -= handler,
                    OnSourceTelemetryUpdated = sourceTelemetryController.OnSourceTelemetryUpdated,
                    AttachAudioDevicesChanged = handler => viewModel._audioDeviceWatcher.DevicesChanged += handler,
                    DetachAudioDevicesChanged = handler => viewModel._audioDeviceWatcher.DevicesChanged -= handler,
                    OnAudioDevicesChanged = viewModel.OnAudioDevicesChanged,
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,
                    SetStatusText = value => viewModel.StatusText = value,
                    UpdateLiveCaptureInfo = snapshot => viewModel.UpdateLiveCaptureInfo(snapshot),
                    UpdateHdrRuntimeStatusFromCapture = snapshot => viewModel.UpdateHdrRuntimeStatusFromCapture(snapshot),
                    SetIsInitialized = value => viewModel.IsInitialized = value,
                    IsCaptureInitialized = () => viewModel._captureService.IsInitialized,
                    IsInitialized = () => viewModel.IsInitialized,
                    SetIsPreviewing = value => viewModel.IsPreviewing = value,
                    IsVideoPreviewActive = () => viewModel._captureService.IsVideoPreviewActive,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    SetIsRecording = value => viewModel.IsRecording = value,
                    IsCaptureRecording = () => viewModel._captureService.IsRecording,
                    IsRecording = () => viewModel.IsRecording,
                    ResetAudioMeter = viewModel.ResetAudioMeter,
                    GetPreviewRendererStopHandlers = () =>
                    {
                        var handlers = viewModel.PreviewRendererStopRequested;
                        return handlers != null
                            ? Array.ConvertAll(handlers.GetInvocationList(), handler => (Func<Task>)handler)
                            : Array.Empty<Func<Task>>();
                    },
                    ReinitializeDeviceAsync = previewLifecycleController.ReinitializeDeviceAsync,
                    EnqueueUiOperation = (operation, operationName) => viewModel.EnqueueUiOperation(operation, operationName),
                });
        }
    }
}

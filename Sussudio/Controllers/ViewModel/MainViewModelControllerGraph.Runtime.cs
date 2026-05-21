using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelRuntimeLifecycleController(
                new MainViewModelRuntimeLifecycleControllerContext
                {
                    CreateEventIngressController = () => CreateRuntimeEventIngressController(viewModel, previewLifecycleController),
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

    }
}

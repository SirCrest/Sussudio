using System;
using System.Threading;
using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
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
                            PreviewReinitializeDebounceMs = PreviewReinitializeDebounceMs,
                            PendingFlashbackCycleTask = () => viewModel._pendingFlashbackCycleTask,
                            FlashbackCycleBeforeReinitializeTimeoutMs = FlashbackCycleBeforeReinitializeTimeoutMs,
                            AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,
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
    }
}

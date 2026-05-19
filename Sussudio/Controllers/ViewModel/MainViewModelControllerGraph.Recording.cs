namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelRecordingTransitionController CreateRecordingTransitionController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelRecordingTransitionController(
                new MainViewModelRecordingTransitionControllerContext
                {
                    InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    IsRecording = () => viewModel.IsRecording,
                    SetIsRecording = value => viewModel.IsRecording = value,
                    IsInitialized = () => viewModel.IsInitialized,
                    HasSelectedDevice = () => viewModel.SelectedDevice != null,
                    GetStatusText = () => viewModel.StatusText,
                    SetStatusText = value => viewModel.StatusText = value,
                    SetIsRecordingTransitioning = value => viewModel.IsRecordingTransitioning = value,
                    BuildCaptureSettings = viewModel.BuildCaptureSettings,
                    StartRecordingAsync = (settings, cancellationToken) =>
                        viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken),
                    StopRecordingAsync = cancellationToken =>
                        viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken),
                    GetSessionIsRecording = () => viewModel._sessionCoordinator.Snapshot.IsRecording,
                    RestartRecordingStopwatch = viewModel._recordingStopwatch.Restart,
                    StopRecordingStopwatch = viewModel._recordingStopwatch.Stop,
                    ClearRecordingBitrateSamples = viewModel._recordingBitrateSamples.Clear,
                    SetRecordingSizeInfo = value => viewModel.RecordingSizeInfo = value,
                    SetRecordingBitrateInfo = value => viewModel.RecordingBitrateInfo = value,
                    GetRecordingTime = () => viewModel.RecordingTime,
                },
                previewLifecycleController);
        }

    }
}

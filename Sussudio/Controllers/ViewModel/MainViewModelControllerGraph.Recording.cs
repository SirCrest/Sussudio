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

        private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)
        {
            return new MainViewModelRecordingCapabilityController(
                new MainViewModelRecordingCapabilityControllerContext
                {
                    DefaultRecordingFormat = DefaultRecordingFormat,
                    HevcRecordingFormat = HevcRecordingFormat,
                    Av1RecordingFormat = Av1RecordingFormat,
                    GetAvailableRecordingFormats = () => viewModel.AvailableRecordingFormats,
                    ReplaceAvailableRecordingFormats = formats =>
                    {
                        viewModel.AvailableRecordingFormats.Clear();
                        foreach (var format in formats)
                        {
                            viewModel.AvailableRecordingFormats.Add(format);
                        }
                    },
                    GetSelectedRecordingFormat = () => viewModel.SelectedRecordingFormat,
                    SetSelectedRecordingFormat = value => viewModel.SelectedRecordingFormat = value,
                    NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetStatusText = value => viewModel.StatusText = value,
                    IsFfmpegMissing = () => viewModel.IsFfmpegMissing,
                    SetIsFfmpegMissing = value => viewModel.IsFfmpegMissing = value,
                    HasUiThreadAccess = () => viewModel._dispatcherQueue.HasThreadAccess,
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetAvailableSplitEncodeModes = () => viewModel.AvailableSplitEncodeModes,
                    ReplaceAvailableSplitEncodeModes = modes =>
                    {
                        viewModel.AvailableSplitEncodeModes.Clear();
                        foreach (var mode in modes)
                        {
                            viewModel.AvailableSplitEncodeModes.Add(mode);
                        }
                    },
                    GetSelectedSplitEncodeMode = () => viewModel.SelectedSplitEncodeMode,
                    SetSelectedSplitEncodeMode = value => viewModel.SelectedSplitEncodeMode = value,
                    AvailableSplitEncodeModesContains = value => viewModel.AvailableSplitEncodeModes.Contains(value),
                });
        }

    }
}

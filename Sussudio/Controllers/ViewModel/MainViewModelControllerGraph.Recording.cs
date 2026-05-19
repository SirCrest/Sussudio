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

        private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelRecordingSettingsAutomationController(
                new MainViewModelRecordingSettingsAutomationControllerContext
                {
                    InvokeRecordingFormatOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeEncoderSettingsOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableRecordingFormats = () => viewModel.AvailableRecordingFormats,
                    GetAvailableQualities = () => viewModel.AvailableQualities,
                    GetAvailableSplitEncodeModes = () => viewModel.AvailableSplitEncodeModes,
                    GetAvailablePresets = () => viewModel.AvailablePresets,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetSuppressFlashbackFormatCycle = value => viewModel._suppressFlashbackFormatCycle = value,
                    SetSuppressFlashbackEncoderSettingsCycle = value => viewModel._suppressFlashbackEncoderSettingsCycle = value,
                    SetSelectedRecordingFormat = value => viewModel.SelectedRecordingFormat = value,
                    GetSelectedQuality = () => viewModel.SelectedQuality,
                    SetSelectedQuality = value => viewModel.SelectedQuality = value,
                    GetSelectedSplitEncodeMode = () => viewModel.SelectedSplitEncodeMode,
                    SetSelectedSplitEncodeMode = value => viewModel.SelectedSplitEncodeMode = value,
                    GetSelectedPreset = () => viewModel.SelectedPreset,
                    SetSelectedPreset = value => viewModel.SelectedPreset = value,
                    GetCustomBitrateMbps = () => viewModel.CustomBitrateMbps,
                    SetCustomBitrateMbps = value => viewModel.CustomBitrateMbps = value,
                    SetOutputPath = value => viewModel.OutputPath = value,
                    UpdateRecordingFormatAsync = (format, cancellationToken) =>
                        viewModel._sessionCoordinator.UpdateRecordingFormatAsync(format, cancellationToken),
                    CycleFlashbackEncoderSettingsAsync = (quality, customBitrateMbps, nvencPreset, splitEncodeMode, cancellationToken) =>
                        viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                            quality,
                            customBitrateMbps,
                            nvencPreset,
                            splitEncodeMode,
                            cancellationToken),
                });
        }
    }
}

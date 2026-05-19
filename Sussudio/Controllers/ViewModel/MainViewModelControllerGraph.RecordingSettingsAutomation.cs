namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
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

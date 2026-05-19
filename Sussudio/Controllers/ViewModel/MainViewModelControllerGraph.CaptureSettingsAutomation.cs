namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelCaptureSettingsAutomationController(
                new MainViewModelCaptureSettingsAutomationControllerContext
                {
                    InvokeBooleanOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableResolutions = () => viewModel.AvailableResolutions,
                    GetAvailableFrameRates = () => viewModel.AvailableFrameRates,
                    GetAvailableVideoFormats = () => viewModel.AvailableVideoFormats,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    SetSelectedResolution = value => viewModel.SelectedResolution = value,
                    SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                    SetSelectedVideoFormat = value => viewModel.SelectedVideoFormat = value,
                    SetMjpegDecoderCount = value => viewModel.MjpegDecoderCount = value,
                    SelectAutoFrameRate = viewModel.SelectAutoFrameRate,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,
                });
        }
    }
}

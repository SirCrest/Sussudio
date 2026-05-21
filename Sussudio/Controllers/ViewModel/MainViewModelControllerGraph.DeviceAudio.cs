using System.Threading;
using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)
        {
            return new MainViewModelDeviceAudioRequestController(
                new MainViewModelDeviceAudioRequestControllerContext
                {
                    EnqueueUiOperation = (operation, operationName, allowDuringDispose) =>
                        viewModel.EnqueueUiOperation(operation, operationName, allowDuringDispose),
                    IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,
                    IsLoadingSettings = () => viewModel._isLoadingSettings,
                    IsRefreshingDeviceAudioControls = () => viewModel._isRefreshingDeviceAudioControls,
                    IsDeviceAudioControlSupported = () => viewModel.IsDeviceAudioControlSupported,
                    IsRecording = () => viewModel.IsRecording,
                    GetSelectedDeviceAudioMode = () => viewModel.SelectedDeviceAudioMode,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    SaveSettings = viewModel.SaveSettings,
                    RefreshDeviceAudioControlsAsync = viewModel.RefreshDeviceAudioControlsAsync,
                    ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>
                        viewModel.ApplyDeviceAudioModeAsync(reason, targetDevice: targetDevice, cancellationToken: cancellationToken),
                    ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>
                        viewModel.ApplyAnalogAudioGainAsync(reason, targetDevice: targetDevice, cancellationToken: cancellationToken),
                    IsCurrentSelectedDevice = viewModel.IsCurrentSelectedDevice,
                });
        }
    }
}

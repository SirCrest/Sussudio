using System.Threading;
using Sussudio.Controllers;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelDeviceRefreshController(
                new MainViewModelDeviceRefreshControllerContext
                {
                    SetStatusText = value => viewModel.StatusText = value,
                    IncrementDeviceScanGeneration = () => Interlocked.Increment(ref viewModel._deviceScanGeneration),
                    GetSelectedAudioInputDeviceId = () => viewModel.SelectedAudioInputDevice?.Id,
                    GetSelectedMicrophoneDeviceId = () => viewModel.SelectedMicrophoneDevice?.Id,
                    GetSelectedDeviceId = () => viewModel.SelectedDevice?.Id,
                    EnumerateCaptureDeviceDiscoveryAsync = () =>
                        viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false),
                    ApplyStartupAudioDeviceScan = viewModel.ApplyStartupAudioDeviceScan,
                    ReplaceDevices = devices => ReplaceCollection(viewModel.Devices, devices),
                    GetDevices = () => viewModel.Devices,
                    BeginBackgroundFormatProbe = (device, scanGeneration) =>
                        viewModel._deviceService.BeginBackgroundFormatProbe(device, scanGeneration),
                    GetLastDiscoverySummary = () => viewModel._deviceService.LastDiscoverySummary,
                    SetSelectedDevice = device => viewModel.SelectedDevice = device,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetPendingSavedDeviceId = () => viewModel._pendingSavedDeviceId,
                    SetPendingSavedDeviceId = value => viewModel._pendingSavedDeviceId = value,
                },
                previewLifecycleController);
        }

    }
}

using System;
using Microsoft.UI.Dispatching;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

// Construction seam for the root compatibility view model. MainViewModel keeps
// the XAML/automation-facing property surface, while this type owns the default
// service graph until a fuller composition root can inject feature view models.
internal sealed class MainViewModelDependencies
{
    private MainViewModelDependencies(
        DeviceService deviceService,
        CaptureService captureService,
        CaptureSessionCoordinator sessionCoordinator,
        NativeXuAudioControlService deviceAudioControlService,
        DispatcherQueue dispatcherQueue,
        AudioDeviceWatcher audioDeviceWatcher)
    {
        DeviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        CaptureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        SessionCoordinator = sessionCoordinator ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        DeviceAudioControlService = deviceAudioControlService ?? throw new ArgumentNullException(nameof(deviceAudioControlService));
        DispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        AudioDeviceWatcher = audioDeviceWatcher ?? throw new ArgumentNullException(nameof(audioDeviceWatcher));
    }

    public DeviceService DeviceService { get; }
    public CaptureService CaptureService { get; }
    public CaptureSessionCoordinator SessionCoordinator { get; }
    public NativeXuAudioControlService DeviceAudioControlService { get; }
    public DispatcherQueue DispatcherQueue { get; }
    public AudioDeviceWatcher AudioDeviceWatcher { get; }

    public static MainViewModelDependencies CreateDefault()
    {
        var captureService = new CaptureService();
        return new MainViewModelDependencies(
            new DeviceService(),
            captureService,
            new CaptureSessionCoordinator(captureService),
            new NativeXuAudioControlService(),
            DispatcherQueue.GetForCurrentThread(),
            new AudioDeviceWatcher());
    }
}

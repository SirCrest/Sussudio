using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing live-signal adapter. LiveSignalInfoController owns debounce timers,
// visibility state, the small scale/fade animation for the signal pill, and
// live source-signal property-change projection.
public sealed partial class MainWindow
{
    private LiveSignalInfoController _liveSignalInfoController = null!;

    private void InitializeLiveSignalInfoController()
    {
        _liveSignalInfoController = new LiveSignalInfoController(new LiveSignalInfoControllerContext
        {
            DispatcherQueue = DispatcherQueue,
            LiveSignalInfoPanel = LiveSignalInfoPanel,
            LiveSignalInfoScale = LiveSignalInfoScale,
            LiveResolutionTextBlock = LiveResolutionTextBlock,
            LiveFrameRateTextBlock = LiveFrameRateTextBlock,
            LivePixelFormatTextBlock = LivePixelFormatTextBlock,
        });
    }

    private void UpdateLiveSignalInfoVisibility()
        => _liveSignalInfoController.Update(
            ViewModel.LiveResolution,
            ViewModel.LiveFrameRate,
            ViewModel.LivePixelFormat);

    private void StopLiveSignalInfoTimers()
        => _liveSignalInfoController.StopTimers();

    private bool TryHandleLiveSignalPropertyChanged(string propertyName)
        => _liveSignalInfoController.TryHandlePropertyChanged(
            propertyName,
            ViewModel.LiveResolution,
            ViewModel.LiveFrameRate,
            ViewModel.LivePixelFormat);
}

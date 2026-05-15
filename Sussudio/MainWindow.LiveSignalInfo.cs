using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing live-signal adapter. LiveSignalInfoController owns debounce timers,
// visibility state, and the small scale/fade animation for the signal pill.
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
}

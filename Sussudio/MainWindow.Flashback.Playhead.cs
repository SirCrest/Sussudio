using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing Flashback playhead motion adapter.
public sealed partial class MainWindow
{
    private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;

    private void InitializeFlashbackPlayheadMotionController()
    {
        _flashbackPlayheadMotionController = new FlashbackPlayheadMotionController(new FlashbackPlayheadMotionControllerContext
        {
            ViewModel = ViewModel,
            DispatcherQueue = _dispatcherQueue,
            IsWindowClosing = () => _isWindowClosing,
            IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,
            ScrubArea = FlashbackScrubArea,
            Playhead = FlashbackPlayhead,
            PlayheadHandle = FlashbackPlayheadHandle,
            PlayheadTimeBorder = FlashbackPlayheadTimeBorder,
        });
    }

    private void RequestFlashbackPlayheadSnapOnNextUpdate()
        => _flashbackPlayheadMotionController.RequestSnapOnNextUpdate();

    private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)
        => _flashbackPlayheadMotionController.PositionMagneticPlayhead(x, trackWidth);

    private void RefreshFlashbackCtiMotion(string reason)
        => _flashbackPlayheadMotionController.RefreshCtiMotion(reason);

    private void StopFlashbackCtiAnchorTimer()
        => _flashbackPlayheadMotionController.StopCtiAnchorTimer();
}

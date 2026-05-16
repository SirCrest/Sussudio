namespace Sussudio;

// XAML-facing Flashback CTI motion adapter. The controller owns steady-state
// compositor extrapolation and anchor-timer correction.
public sealed partial class MainWindow
{
    private void RefreshFlashbackCtiMotion(string reason)
        => _flashbackPlayheadMotionController.RefreshCtiMotion(reason);

    private void StopFlashbackCtiAnchorTimer()
        => _flashbackPlayheadMotionController.StopCtiAnchorTimer();
}

namespace Sussudio;

// XAML-facing adapter for the compact frame-time overlay. Text and graph
// projection live in FrameTimeOverlayPresentationController.
public sealed partial class MainWindow
{
    private bool IsFrameTimeOverlayVisible()
        => _statsOverlayController.IsFrameTimeOverlayVisible;

    private void UpdateFrameTimeOverlay(StatsSnapshot snapshot)
    {
        if (!IsFrameTimeOverlayVisible())
        {
            return;
        }

        _frameTimeOverlayPresentationController.Apply(snapshot);
    }
}

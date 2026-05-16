using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for the compact frame-time overlay. The presentation
// controller owns text application and delegates graph math to the geometry helper.
public sealed partial class MainWindow
{
    private FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController = null!;

    private void InitializeFrameTimeOverlayPresentationController()
    {
        _frameTimeOverlayPresentationController = new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext
        {
            SourceValue = FrameTime_SourceValue,
            VisualValue = FrameTime_VisualValue,
            PreviewValue = FrameTime_PreviewValue,
            LatencyValue = FrameTime_LatencyValue,
            StatusValue = FrameTime_StatusValue,
            Canvas = FrameTime_Canvas,
            VisualLine = FrameTime_VisualLine,
            PreviewLine = FrameTime_PreviewLine,
            ExpectedLine = FrameTime_ExpectedLine
        });
    }

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

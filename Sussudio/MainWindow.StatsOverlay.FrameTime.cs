using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private StatsOverlayFrameTimeTargetsContext CreateStatsOverlayFrameTimeTargetsContext()
        => new()
        {
            FrameTimeSourceValue = FrameTime_SourceValue,
            FrameTimeVisualValue = FrameTime_VisualValue,
            FrameTimePreviewValue = FrameTime_PreviewValue,
            FrameTimeLatencyValue = FrameTime_LatencyValue,
            FrameTimeStatusValue = FrameTime_StatusValue,
            FrameTimeCanvas = FrameTime_Canvas,
            FrameTimeVisualLine = FrameTime_VisualLine,
            FrameTimePreviewLine = FrameTime_PreviewLine,
            FrameTimeExpectedLine = FrameTime_ExpectedLine,
        };

    private void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayCompositionController.SetFrameTimeOverlayVisible(visible);

    private bool IsFrameTimeOverlayVisible()
        => _statsOverlayCompositionController.IsFrameTimeOverlayVisible;
}

using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private StatsOverlaySnapshotSourceContext CreateStatsOverlaySnapshotSourceContext()
        => new()
        {
            GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,
            GetRenderer = () => _previewRendererHostController.Renderer,
            GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs,
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsRecording = () => ViewModel.IsRecording,
        };

    private StatsSnapshot GetStatsSnapshot()
        => _statsOverlayCompositionController.GetStatsSnapshot();
}

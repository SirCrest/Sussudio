using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing stats snapshot adapter. StatsSnapshotProvider owns snapshot
// orchestration; StatsSnapshotProvider.RenderMetrics owns renderer metric
// acquisition and null fallback policy.
public sealed partial class MainWindow
{
    private StatsSnapshotProvider _statsSnapshotProvider = null!;

    private void InitializeStatsSnapshotProvider()
    {
        _statsSnapshotProvider = new StatsSnapshotProvider(new StatsSnapshotProviderContext
        {
            GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,
            GetRenderer = () => _previewRendererHostController.Renderer,
            GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs,
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsRecording = () => ViewModel.IsRecording
        });
    }

    private StatsSnapshot GetStatsSnapshot()
        => _statsSnapshotProvider.GetSnapshot();
}

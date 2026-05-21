using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private StatsOverlayCompositionController _statsOverlayCompositionController = null!;

    private void InitializeStatsOverlayCompositionController()
    {
        _statsOverlayCompositionController = new StatsOverlayCompositionController(new StatsOverlayCompositionControllerContext
        {
            Shell = CreateStatsOverlayShellContext(),
            SnapshotSources = CreateStatsOverlaySnapshotSourceContext(),
            DockTargets = CreateStatsOverlayDockTargetsContext(),
            HardwareSources = CreateStatsOverlayHardwareSourceContext(),
            FrameTimeTargets = CreateStatsOverlayFrameTimeTargetsContext(),
        });
    }
}

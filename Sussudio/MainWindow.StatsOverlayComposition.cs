using Sussudio.Controllers;

namespace Sussudio;

// Stats overlay controller composition. Stats dock controller graph composition
// lives in MainWindow.StatsDockComposition.cs, and the binding adapter lives in
// MainWindow.StatsOverlay.cs.
public sealed partial class MainWindow
{
    private StatsOverlayController _statsOverlayController = null!;

    private void InitializeStatsOverlayController()
    {
        InitializeFrameTimeOverlayPresentationController();
        InitializeStatsDockRefreshController();
        _statsOverlayController = new StatsOverlayController(new StatsOverlayControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            StatsToggle = StatsToggle,
            StatsDockPanel = StatsDockPanel,
            FrameTimeOverlay = FrameTimeOverlay,
            FrameTimeOverlayToggle = FrameTimeOverlayToggle,
            IsWindowClosing = () => _isWindowClosing,
            SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,
            GetStatsSnapshot = GetStatsSnapshot,
            UpdateStatsDock = _statsDockRefreshController.RefreshDock,
            UpdateFrameTimeOverlay = UpdateFrameTimeOverlay,
            Log = message => Logger.Log(message)
        });
    }
}

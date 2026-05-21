using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private StatsOverlayShellContext CreateStatsOverlayShellContext()
        => new()
        {
            DispatcherQueue = _dispatcherQueue,
            StatsToggle = StatsToggle,
            StatsDockPanel = StatsDockPanel,
            FrameTimeOverlay = FrameTimeOverlay,
            FrameTimeOverlayToggle = FrameTimeOverlayToggle,
            IsWindowClosing = () => _isWindowClosing,
            SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,
            Log = message => Logger.Log(message),
        };
}

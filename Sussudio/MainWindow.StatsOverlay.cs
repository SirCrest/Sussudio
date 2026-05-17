namespace Sussudio;

// XAML-facing stats dock presentation and visibility adapter. Controller
// composition lives in MainWindow.StatsOverlayComposition.cs.
public sealed partial class MainWindow
{
    private void AttachStatsOverlayToggleBindings()
        => _statsOverlayController.AttachToggleBindings();

    private void DetachStatsOverlayToggleBindings()
        => _statsOverlayController.DetachToggleBindings();

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayController.SyncStatsVisibility(visible, immediate);

    private void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayController.SetFrameTimeOverlayVisible(visible);

    private void StartStatsDockPolling()
        => _statsOverlayController.StartPolling();

    private void StopStatsDockPolling()
        => _statsOverlayController.StopPolling();

    private void ShowStatsDockPanel()
        => _statsOverlayController.ShowDockPanel();

    private void HideStatsDockPanel(bool immediate = false)
        => _statsOverlayController.HideDockPanel(immediate);
}

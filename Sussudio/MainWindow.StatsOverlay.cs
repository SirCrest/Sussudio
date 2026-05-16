using Microsoft.UI.Xaml;

namespace Sussudio;

// XAML-facing stats dock presentation and visibility adapter. Controller
// composition lives in MainWindow.StatsOverlayComposition.cs and
// MainWindow.StatsDockComposition.cs.
public sealed partial class MainWindow
{
    private void StatsToggle_Checked(object sender, RoutedEventArgs e)
        => _statsOverlayController.HandleStatsToggleChecked();

    private void StatsToggle_Unchecked(object sender, RoutedEventArgs e)
        => _statsOverlayController.HandleStatsToggleUnchecked();

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayController.SyncStatsVisibility(visible, immediate);

    private void FrameTimeOverlayToggle_Checked(object sender, RoutedEventArgs e)
        => _statsOverlayController.SetFrameTimeOverlayVisible(true);

    private void FrameTimeOverlayToggle_Unchecked(object sender, RoutedEventArgs e)
        => _statsOverlayController.SetFrameTimeOverlayVisible(false);

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

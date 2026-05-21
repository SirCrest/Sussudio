namespace Sussudio;

public sealed partial class MainWindow
{
    private void AttachStatsOverlayToggleBindings()
        => _statsOverlayCompositionController.AttachToggleBindings();

    private void DetachStatsOverlayToggleBindings()
        => _statsOverlayCompositionController.DetachToggleBindings();

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayCompositionController.ApplyStatsVisibility(visible, immediate);

    private void StartStatsDockPolling()
        => _statsOverlayCompositionController.StartPolling();

    private void StopStatsDockPolling()
        => _statsOverlayCompositionController.StopPolling();

    private void ShowStatsDockPanel()
        => _statsOverlayCompositionController.ShowDockPanel();

    private void HideStatsDockPanel(bool immediate = false)
        => _statsOverlayCompositionController.HideDockPanel(immediate);
}

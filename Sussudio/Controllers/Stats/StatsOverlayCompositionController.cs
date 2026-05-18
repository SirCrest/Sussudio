using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed partial class StatsOverlayCompositionController
{
    private readonly StatsOverlayController _statsOverlayController;
    private readonly StatsDockControllerGraph _statsDockControllerGraph;
    private readonly StatsSnapshotProvider _statsSnapshotProvider;
    private readonly FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController;
    private readonly StatsSectionChromeController _statsSectionChromeController;

    public StatsOverlayCompositionController(StatsOverlayCompositionControllerContext context)
    {
        _statsSnapshotProvider = CreateSnapshotProvider(context);
        _frameTimeOverlayPresentationController = CreateFrameTimeOverlayPresentationController(context);
        _statsDockControllerGraph = CreateDockControllerGraph(context);
        _statsOverlayController = CreateOverlayController(context);
        _statsSectionChromeController = CreateSectionChromeController(context);
    }

    public void AttachToggleBindings()
        => _statsOverlayController.AttachToggleBindings();

    public void DetachToggleBindings()
        => _statsOverlayController.DetachToggleBindings();

    public void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayController.SyncStatsVisibility(visible, immediate);

    public bool TryHandlePropertyChanged(string propertyName, bool isStatsVisible)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsStatsVisible):
                ApplyStatsVisibility(isStatsVisible);
                return true;

            default:
                return false;
        }
    }

    public void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayController.SetFrameTimeOverlayVisible(visible);

    public bool IsFrameTimeOverlayVisible
        => _statsOverlayController.IsFrameTimeOverlayVisible;

    public void StartPolling()
        => _statsOverlayController.StartPolling();

    public void StopPolling()
        => _statsOverlayController.StopPolling();

    public void ShowDockPanel()
        => _statsOverlayController.ShowDockPanel();

    public void HideDockPanel(bool immediate = false)
        => _statsOverlayController.HideDockPanel(immediate);

    public StatsSnapshot GetStatsSnapshot()
        => _statsSnapshotProvider.GetSnapshot();

    public void ToggleSectionFromHeader(object sender)
        => _statsSectionChromeController.ToggleFromHeader(sender);

    public void SetSectionVisible(string section, bool visible)
        => _statsSectionChromeController.SetVisible(section, visible);

    private void UpdateFrameTimeOverlay(StatsSnapshot snapshot)
    {
        if (!IsFrameTimeOverlayVisible)
        {
            return;
        }

        _frameTimeOverlayPresentationController.Apply(snapshot);
    }
}

using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private FlashbackTimelineController _flashbackTimelineController = null!;

    private void InitializeFlashbackTimelineController()
    {
        _flashbackTimelineController = new FlashbackTimelineController(new FlashbackTimelineControllerContext
        {
            ViewModel = ViewModel,
            FlashbackToggle = FlashbackToggle,
            FlashbackTimelinePanel = FlashbackTimelinePanel,
            FlashbackTrackBackground = FlashbackTrackBackground,
            FlashbackScrubArea = FlashbackScrubArea,
            FlashbackPlayhead = FlashbackPlayhead,
            FlashbackLiveEdge = FlashbackLiveEdge,
            SnapPlayheadOnNextOpen = RequestFlashbackPlayheadSnapOnNextUpdate,
            StartStatusPolling = StartFlashbackStatusPolling,
            StopStatusPolling = StopFlashbackStatusPolling,
            ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,
        });
    }

    private void FlashbackToggle_Checked(object sender, RoutedEventArgs e)
        => _flashbackTimelineController.OnToggleChecked();

    private void FlashbackToggle_Unchecked(object sender, RoutedEventArgs e)
        => _flashbackTimelineController.OnToggleUnchecked();

    private void ApplyFlashbackTimelineVisibility(bool show)
        => _flashbackTimelineController.ApplyVisibility(show);

    private void ApplyFlashbackTimelineLockout()
        => _flashbackTimelineController.ApplyLockout();

    private void CollapseFlashbackTimelineImmediately()
        => _flashbackTimelineController.CollapseImmediately();
}

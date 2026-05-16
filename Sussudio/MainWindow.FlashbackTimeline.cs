using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing Flashback timeline adapter. FlashbackTimelineController owns
// timeline visibility, lockout, and show/hide animation state.
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

    private void SyncFlashbackTimelineToggle(bool isVisible)
        => _flashbackTimelineController.SyncToggle(isVisible);

    private void CollapseFlashbackTimelineImmediately()
        => _flashbackTimelineController.CollapseImmediately();

    private void ResetFlashbackTimelineAnimationForFullScreen()
        => _flashbackTimelineController.ResetAnimationForFullScreen();

    private void ClearFlashbackScrubInteractionForLockout()
        => _flashbackScrubInteractionController.ClearForLockout();
}

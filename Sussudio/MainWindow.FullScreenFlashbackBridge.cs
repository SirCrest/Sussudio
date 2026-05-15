using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Sussudio;

// Flashback behavior needed by full-screen transitions and key handling. The
// generic full-screen XAML adapter stays in MainWindow.FullScreen.cs.
public sealed partial class MainWindow
{
    private void HandleFlashbackFullScreenKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Flashback keyboard shortcuts (only when timeline is visible).
        if (!ViewModel.IsFlashbackEnabled || FlashbackTimelinePanel.Visibility != Visibility.Visible)
        {
            return;
        }

        switch (e.Key)
        {
            case Windows.System.VirtualKey.I:
                FlashbackInButton_Click(sender, e);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.O:
                FlashbackOutButton_Click(sender, e);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.Space:
                FlashbackPlayPauseButton_Click(sender, e);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.L:
                FlashbackGoLiveButton_Click(sender, e);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.Left:
                if (!ViewModel.FlashbackNudge(TimeSpan.FromSeconds(-1)))
                {
                    ViewModel.ReportFlashbackPlaybackRejection("nudge left", "FLASHBACK_UI_NUDGE_REJECTED direction=left");
                }

                e.Handled = true;
                return;
            case Windows.System.VirtualKey.Right:
                if (!ViewModel.FlashbackNudge(TimeSpan.FromSeconds(1)))
                {
                    ViewModel.ReportFlashbackPlaybackRejection("nudge right", "FLASHBACK_UI_NUDGE_REJECTED direction=right");
                }

                e.Handled = true;
                return;
        }
    }

    private bool ShouldShowFlashbackTimeline()
    {
        return ViewModel.IsFlashbackEnabled && ViewModel.IsFlashbackTimelineVisible;
    }

    private void EndFlashbackScrubForFullScreen()
    {
        if (!_isFlashbackScrubbing)
        {
            return;
        }

        var carriedPosition = _lastScrubPointerPosition;
        Logger.Log($"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        _isFlashbackScrubbing = false;
        _lastScrubUpdateTick = 0;
        _lastScrubPointerPosition = null;
        var ended = carriedPosition.HasValue
            ? ViewModel?.FlashbackEndScrubAt(carriedPosition.Value) ?? false
            : ViewModel?.FlashbackEndScrub() ?? false;
        if (!ended)
        {
            ViewModel?.ReportFlashbackPlaybackRejection("scrub end (fullscreen_enter)", "FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter");
        }
    }
}

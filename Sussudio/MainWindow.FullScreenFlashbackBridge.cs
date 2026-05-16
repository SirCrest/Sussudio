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

        if (_flashbackCommandController.HandleFullScreenKeyboardCommand(e.Key))
        {
            e.Handled = true;
        }
    }

    private bool ShouldShowFlashbackTimeline()
    {
        return ViewModel.IsFlashbackEnabled && ViewModel.IsFlashbackTimelineVisible;
    }

    private void EndFlashbackScrubForFullScreen()
        => _flashbackScrubInteractionController.EndForFullScreen();
}

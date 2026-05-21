using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Sussudio.Controllers;

internal sealed partial class FullScreenController
{
    private void PrepareChromeForOverlay()
    {
        _context.ResetFlashbackTimelineAnimation();

        _context.ControlBarBorder.Opacity = 1;
        if (_context.ControlBarBorder.RenderTransform is TranslateTransform controlBarTranslate)
        {
            controlBarTranslate.Y = 0;
        }

        var showTimeline = ShouldShowFlashbackTimeline();
        _context.FlashbackTimelinePanel.Visibility = showTimeline ? Visibility.Visible : Visibility.Collapsed;
        _context.FlashbackTimelinePanel.Height = double.NaN;
        _context.FlashbackTimelinePanel.Opacity = 1;
        _context.FlashbackTimelinePanel.IsHitTestVisible = _context.ViewModel.IsFlashbackEnabled;
        _context.SyncFlashbackTimelineToggle(showTimeline);
    }

    private void ApplyFullScreenChromeMaterials()
    {
        _preFullScreenControlBarBackground ??= _context.ControlBarBorder.Background;
        _preFullScreenFlashbackTimelineBackground ??= _context.FlashbackTimelinePanel.Background;

        _context.ControlBarBorder.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(0xF2, 0x14, 0x14, 0x14));
        _context.FlashbackTimelinePanel.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(0xF2, 0x20, 0x20, 0x20));
    }

    private void RestoreWindowedChromeMaterials()
    {
        if (_preFullScreenControlBarBackground != null)
        {
            _context.ControlBarBorder.Background = _preFullScreenControlBarBackground;
            _preFullScreenControlBarBackground = null;
        }

        if (_preFullScreenFlashbackTimelineBackground != null)
        {
            _context.FlashbackTimelinePanel.Background = _preFullScreenFlashbackTimelineBackground;
            _preFullScreenFlashbackTimelineBackground = null;
        }
    }

    private bool ShouldShowFlashbackTimeline()
        => _context.ViewModel.IsFlashbackEnabled && _context.ViewModel.IsFlashbackTimelineVisible;

    private void UpdateButtonState()
    {
        if (_isFullScreen)
        {
            _context.FullScreenButtonIcon.Glyph = "\uE73F";
            ToolTipService.SetToolTip(_context.FullScreenButton, "Exit full screen");
            _context.FullScreenMenuItem.Text = "Exit Full Screen";
            if (_context.FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE73F";
        }
        else
        {
            _context.FullScreenButtonIcon.Glyph = "\uE740";
            ToolTipService.SetToolTip(_context.FullScreenButton, "Full screen");
            _context.FullScreenMenuItem.Text = "Enter Full Screen";
            if (_context.FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE740";
        }
    }
}

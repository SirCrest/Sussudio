using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackTimelineControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleButton FlashbackToggle { get; init; }
    public required FrameworkElement FlashbackTimelinePanel { get; init; }
    public required FrameworkElement FlashbackTrackBackground { get; init; }
    public required FrameworkElement FlashbackScrubArea { get; init; }
    public required FrameworkElement FlashbackPlayhead { get; init; }
    public required FrameworkElement FlashbackLiveEdge { get; init; }
    public required Action SnapPlayheadOnNextOpen { get; init; }
    public required Action StartStatusPolling { get; init; }
    public required Action StopStatusPolling { get; init; }
    public required Action ClearScrubInteraction { get; init; }
}

internal sealed class FlashbackTimelineController
{
    private readonly FlashbackTimelineControllerContext _context;
    private readonly FlashbackTimelineAnimationController _animationController;
    private bool _suppressToggle;

    public FlashbackTimelineController(FlashbackTimelineControllerContext context)
    {
        _context = context;
        _animationController = new FlashbackTimelineAnimationController(
            context.FlashbackTimelinePanel,
            context.SnapPlayheadOnNextOpen,
            ShouldKeepTimelineVisibleAfterAnimation);
    }

    public void OnToggleChecked()
    {
        if (_suppressToggle)
        {
            return;
        }

        if (!_context.ViewModel.IsFlashbackEnabled)
        {
            ApplyLockout();
            return;
        }

        _context.ViewModel.IsFlashbackTimelineVisible = true;
    }

    public void OnToggleUnchecked()
    {
        if (_suppressToggle)
        {
            return;
        }

        _context.ViewModel.IsFlashbackTimelineVisible = false;
    }

    public void ApplyVisibility(bool show)
    {
        if (show && !_context.ViewModel.IsFlashbackEnabled)
        {
            _context.ViewModel.IsFlashbackTimelineVisible = false;
            show = false;
        }

        SyncToggle(show);
        _context.FlashbackToggle.IsEnabled = _context.ViewModel.IsFlashbackEnabled;
        _context.FlashbackTimelinePanel.IsHitTestVisible = _context.ViewModel.IsFlashbackEnabled;

        if (show)
        {
            if (!_animationController.IsAnimating && _context.FlashbackTimelinePanel.Visibility != Visibility.Visible)
            {
                _animationController.Animate(show: true);
            }

            _context.StartStatusPolling();
            return;
        }

        _context.StopStatusPolling();
        if (!_animationController.IsAnimating && _context.FlashbackTimelinePanel.Visibility != Visibility.Collapsed)
        {
            _animationController.Animate(show: false);
        }
    }

    public void ApplyTrackSize(double width, double height)
    {
        _context.FlashbackTrackBackground.Width = width;
        _context.FlashbackTrackBackground.Height = height;
        _context.FlashbackScrubArea.Width = width;
        _context.FlashbackScrubArea.Height = height;
        _context.FlashbackPlayhead.Height = height;
        _context.FlashbackLiveEdge.Height = height;

        Canvas.SetLeft(_context.FlashbackLiveEdge, width - 2);
    }

    public void ApplyLockout()
    {
        var flashbackEnabled = _context.ViewModel.IsFlashbackEnabled;
        _context.FlashbackToggle.IsEnabled = flashbackEnabled;
        _context.FlashbackTimelinePanel.IsHitTestVisible = flashbackEnabled;
        if (flashbackEnabled)
        {
            return;
        }

        if (_context.ViewModel.IsFlashbackTimelineVisible)
        {
            _context.ViewModel.IsFlashbackTimelineVisible = false;
        }

        SyncToggle(isVisible: false);
        _context.StopStatusPolling();
        _context.ClearScrubInteraction();
        CollapseImmediately();
    }

    public void SyncToggle(bool isVisible)
    {
        if (_context.FlashbackToggle.IsChecked == isVisible)
        {
            return;
        }

        _suppressToggle = true;
        try
        {
            _context.FlashbackToggle.IsChecked = isVisible;
        }
        finally
        {
            _suppressToggle = false;
        }
    }

    public void CollapseImmediately()
        => _animationController.CollapseImmediately();

    public void ResetAnimationForFullScreen()
        => _animationController.ResetForFullScreen();

    private bool ShouldKeepTimelineVisibleAfterAnimation()
        => _context.ViewModel.IsFlashbackEnabled &&
           _context.ViewModel.IsFlashbackTimelineVisible;
}

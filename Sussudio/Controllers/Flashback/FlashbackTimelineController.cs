using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
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

internal sealed class FlashbackTimelineAnimationController
{
    private readonly FrameworkElement _timelinePanel;
    private readonly Action _snapPlayheadOnNextOpen;
    private readonly Func<bool> _shouldRemainVisible;
    private Storyboard? _timelineStoryboard;

    public FlashbackTimelineAnimationController(
        FrameworkElement timelinePanel,
        Action snapPlayheadOnNextOpen,
        Func<bool> shouldRemainVisible)
    {
        _timelinePanel = timelinePanel;
        _snapPlayheadOnNextOpen = snapPlayheadOnNextOpen;
        _shouldRemainVisible = shouldRemainVisible;
    }

    public bool IsAnimating { get; private set; }

    public void Animate(bool show)
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        IsAnimating = true;
        if (show)
        {
            _snapPlayheadOnNextOpen();
        }

        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            _timelinePanel.Opacity = 0;
            _timelinePanel.Height = double.NaN;
            _timelinePanel.Visibility = Visibility.Visible;
            _timelinePanel.UpdateLayout();
            targetHeight = _timelinePanel.ActualHeight;
            _timelinePanel.Height = 0;
        }
        else
        {
            targetHeight = _timelinePanel.ActualHeight;
            _timelinePanel.Height = targetHeight;
        }

        var heightAnimation = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnimation, _timelinePanel);
        Storyboard.SetTargetProperty(heightAnimation, "Height");

        var fadeAnimation = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fadeAnimation, _timelinePanel);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnimation);
        storyboard.Children.Add(fadeAnimation);
        storyboard.Completed += (_, _) => CompleteAnimation(storyboard);

        _timelineStoryboard = storyboard;
        storyboard.Begin();
    }

    public void CollapseImmediately()
    {
        StopCurrentAnimation();
        _timelinePanel.Visibility = Visibility.Collapsed;
        _timelinePanel.Height = double.NaN;
        _timelinePanel.Opacity = 1;
    }

    public void ResetForFullScreen()
    {
        StopCurrentAnimation();
    }

    private void CompleteAnimation(Storyboard storyboard)
    {
        if (!ReferenceEquals(_timelineStoryboard, storyboard))
        {
            return;
        }

        if (_shouldRemainVisible())
        {
            _timelinePanel.Height = double.NaN;
            _timelinePanel.Opacity = 1;
        }
        else
        {
            _timelinePanel.Visibility = Visibility.Collapsed;
            _timelinePanel.Height = double.NaN;
            _timelinePanel.Opacity = 1;
        }

        _timelineStoryboard = null;
        IsAnimating = false;
    }

    private void StopCurrentAnimation()
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        IsAnimating = false;
    }
}

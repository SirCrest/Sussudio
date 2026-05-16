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
    private Storyboard? _timelineStoryboard;
    private bool _isAnimating;
    private bool _suppressToggle;

    public FlashbackTimelineController(FlashbackTimelineControllerContext context)
    {
        _context = context;
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
            if (!_isAnimating && _context.FlashbackTimelinePanel.Visibility != Visibility.Visible)
            {
                AnimateTimeline(show: true);
            }

            _context.StartStatusPolling();
            return;
        }

        _context.StopStatusPolling();
        if (!_isAnimating && _context.FlashbackTimelinePanel.Visibility != Visibility.Collapsed)
        {
            AnimateTimeline(show: false);
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
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        _isAnimating = false;
        _context.FlashbackTimelinePanel.Visibility = Visibility.Collapsed;
        _context.FlashbackTimelinePanel.Height = double.NaN;
        _context.FlashbackTimelinePanel.Opacity = 1;
    }

    public void ResetAnimationForFullScreen()
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        _isAnimating = false;
    }

    private void AnimateTimeline(bool show)
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        _isAnimating = true;
        if (show)
        {
            _context.SnapPlayheadOnNextOpen();
        }

        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            _context.FlashbackTimelinePanel.Opacity = 0;
            _context.FlashbackTimelinePanel.Height = double.NaN;
            _context.FlashbackTimelinePanel.Visibility = Visibility.Visible;
            _context.FlashbackTimelinePanel.UpdateLayout();
            targetHeight = _context.FlashbackTimelinePanel.ActualHeight;
            _context.FlashbackTimelinePanel.Height = 0;
        }
        else
        {
            targetHeight = _context.FlashbackTimelinePanel.ActualHeight;
            _context.FlashbackTimelinePanel.Height = targetHeight;
        }

        var heightAnim = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnim, _context.FlashbackTimelinePanel);
        Storyboard.SetTargetProperty(heightAnim, "Height");

        var fade = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, _context.FlashbackTimelinePanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_timelineStoryboard, storyboard))
            {
                return;
            }

            var shouldRemainVisible = show &&
                                      _context.ViewModel.IsFlashbackEnabled &&
                                      _context.ViewModel.IsFlashbackTimelineVisible;
            if (shouldRemainVisible)
            {
                _context.FlashbackTimelinePanel.Height = double.NaN;
                _context.FlashbackTimelinePanel.Opacity = 1;
            }
            else
            {
                _context.FlashbackTimelinePanel.Visibility = Visibility.Collapsed;
                _context.FlashbackTimelinePanel.Height = double.NaN;
                _context.FlashbackTimelinePanel.Opacity = 1;
            }

            _timelineStoryboard = null;
            _isAnimating = false;
        };
        _timelineStoryboard = storyboard;
        storyboard.Begin();
    }
}

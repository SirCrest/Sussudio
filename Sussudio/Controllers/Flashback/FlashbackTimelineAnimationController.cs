using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

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

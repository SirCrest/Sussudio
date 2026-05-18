using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class SettingsShelfControllerContext
{
    public required FrameworkElement SettingsOverlayPanel { get; init; }
}

internal sealed class SettingsShelfController
{
    private readonly SettingsShelfControllerContext _context;
    private bool _isAnimating;

    public SettingsShelfController(SettingsShelfControllerContext context)
    {
        _context = context;
    }

    public bool IsAnimating => _isAnimating;

    public bool IsVisible => _context.SettingsOverlayPanel.Visibility == Visibility.Visible;

    public void Toggle()
    {
        if (_isAnimating)
        {
            return;
        }

        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void ApplyVisibility(bool visible)
    {
        if (_isAnimating)
        {
            return;
        }

        if (visible == IsVisible)
        {
            return;
        }

        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public bool TryHandlePropertyChanged(string propertyName, bool isSettingsVisible)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsSettingsVisible):
                ApplyVisibility(isSettingsVisible);
                return true;

            default:
                return false;
        }
    }

    public void Show()
        => Animate(show: true);

    public void Hide()
        => Animate(show: false);

    public void ResetAnimationState()
        => _isAnimating = false;

    private void Animate(bool show)
    {
        _isAnimating = true;
        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            _context.SettingsOverlayPanel.Opacity = 0;
            _context.SettingsOverlayPanel.Height = double.NaN;
            _context.SettingsOverlayPanel.Visibility = Visibility.Visible;
            _context.SettingsOverlayPanel.UpdateLayout();
            targetHeight = _context.SettingsOverlayPanel.ActualHeight;
            _context.SettingsOverlayPanel.Height = 0;
        }
        else
        {
            targetHeight = _context.SettingsOverlayPanel.ActualHeight;
            _context.SettingsOverlayPanel.Height = targetHeight;
        }

        var heightAnim = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnim, _context.SettingsOverlayPanel);
        Storyboard.SetTargetProperty(heightAnim, "Height");

        var fade = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, _context.SettingsOverlayPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (show)
            {
                _context.SettingsOverlayPanel.Height = double.NaN;
                _context.SettingsOverlayPanel.Opacity = 1;
            }
            else
            {
                _context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;
                _context.SettingsOverlayPanel.Height = double.NaN;
                _context.SettingsOverlayPanel.Opacity = 1;
            }

            _isAnimating = false;
        };
        storyboard.Begin();
    }
}

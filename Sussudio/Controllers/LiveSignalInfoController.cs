using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed class LiveSignalInfoControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required StackPanel LiveSignalInfoPanel { get; init; }
    public required ScaleTransform LiveSignalInfoScale { get; init; }
}

internal sealed class LiveSignalInfoController
{
    private const string LiveInfoUnavailable = "\u2014";
    private static readonly TimeSpan ShowDebounce = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan HideDebounce = TimeSpan.FromMilliseconds(800);

    private readonly LiveSignalInfoControllerContext _context;
    private bool _visible;
    private DispatcherQueueTimer? _showDebounceTimer;
    private DispatcherQueueTimer? _hideDebounceTimer;
    private string _liveResolution = LiveInfoUnavailable;
    private string _liveFrameRate = LiveInfoUnavailable;
    private string _livePixelFormat = LiveInfoUnavailable;

    public LiveSignalInfoController(LiveSignalInfoControllerContext context)
    {
        _context = context;
    }

    public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)
    {
        _liveResolution = liveResolution;
        _liveFrameRate = liveFrameRate;
        _livePixelFormat = livePixelFormat;

        if (HasCompleteLiveSignal() && !_visible)
        {
            if (_showDebounceTimer is null)
            {
                _showDebounceTimer = _context.DispatcherQueue.CreateTimer();
                _showDebounceTimer.Interval = ShowDebounce;
                _showDebounceTimer.IsRepeating = false;
                _showDebounceTimer.Tick += (_, _) =>
                {
                    _showDebounceTimer = null;
                    if (HasCompleteLiveSignal() && !_visible)
                    {
                        _visible = true;
                        AnimateIn();
                    }
                };
            }

            _showDebounceTimer.Start();
        }
        else if (!HasCompleteLiveSignal())
        {
            StopShowDebounce();

            if (_visible)
            {
                if (_hideDebounceTimer is null)
                {
                    _hideDebounceTimer = _context.DispatcherQueue.CreateTimer();
                    _hideDebounceTimer.Interval = HideDebounce;
                    _hideDebounceTimer.IsRepeating = false;
                    _hideDebounceTimer.Tick += (_, _) =>
                    {
                        _hideDebounceTimer = null;
                        if (HasMissingLiveSignal() && _visible)
                        {
                            _visible = false;
                            AnimateOut();
                        }
                    };
                }

                _hideDebounceTimer.Start();
            }
        }
        else if (HasCompleteLiveSignal() && _hideDebounceTimer is not null)
        {
            StopHideDebounce();
        }
    }

    public void StopTimers()
    {
        StopShowDebounce();
        StopHideDebounce();
    }

    private bool HasCompleteLiveSignal()
        => _liveResolution != LiveInfoUnavailable &&
           _liveFrameRate != LiveInfoUnavailable &&
           _livePixelFormat != LiveInfoUnavailable;

    private bool HasMissingLiveSignal()
        => _liveResolution == LiveInfoUnavailable ||
           _liveFrameRate == LiveInfoUnavailable ||
           _livePixelFormat == LiveInfoUnavailable;

    private void StopShowDebounce()
    {
        _showDebounceTimer?.Stop();
        _showDebounceTimer = null;
    }

    private void StopHideDebounce()
    {
        _hideDebounceTimer?.Stop();
        _hideDebounceTimer = null;
    }

    private void AnimateIn()
    {
        _context.LiveSignalInfoPanel.Opacity = 0;
        _context.LiveSignalInfoPanel.Visibility = Visibility.Visible;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, _context.LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        var scaleX = new DoubleAnimation
        {
            From = 0.92,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(scaleX, _context.LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        storyboard.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            From = 0.92,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(scaleY, _context.LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        storyboard.Children.Add(scaleY);

        storyboard.Begin();
    }

    private void AnimateOut()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, _context.LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        storyboard.Completed += (_, _) =>
        {
            _context.LiveSignalInfoPanel.Visibility = Visibility.Collapsed;
        };

        storyboard.Begin();
    }
}

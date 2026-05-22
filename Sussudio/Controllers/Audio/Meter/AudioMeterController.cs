using System;
using System.Numerics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class AudioMeterControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Border AudioMeterTrack { get; init; }
    public required FrameworkElement AudioMeterContent { get; init; }
    public required Border AudioMeterRawFill { get; init; }
    public required Border AudioMeterFill { get; init; }
    public required RectangleGeometry AudioMeterRawClip { get; init; }
    public required RectangleGeometry AudioMeterColorClip { get; init; }
    public required Border AudioPeakHoldIndicator { get; init; }
    public required TranslateTransform AudioPeakHoldTranslate { get; init; }
    public required Border AudioRangeMinMarker { get; init; }
    public required TranslateTransform AudioRangeMinTranslate { get; init; }
    public required Border AudioRangeMaxMarker { get; init; }
    public required TranslateTransform AudioRangeMaxTranslate { get; init; }
    public required Border MicMeterTrack { get; init; }
    public required FrameworkElement MicMeterContent { get; init; }
    public required RectangleGeometry MicMeterClip { get; init; }
}

internal sealed class AudioMeterController
{
    private const long AudioPeakHoldDurationMs = 1500;
    private const double AudioPeakHoldDecayPerSecond = 0.8;
    private const long AudioRangeWindowMs = 3000;
    private static readonly SolidColorBrush MonitoringPeakBrush =
        new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
    private static readonly SolidColorBrush IdlePeakBrush =
        new(Windows.UI.Color.FromArgb(255, 160, 160, 160));

    private readonly AudioMeterControllerContext _context;
    private double _audioPeakHoldLevel;
    private long _audioPeakHoldTimestamp;
    private double _audioRangeMin = 1.0;
    private double _audioRangeMax;
    private long _audioRangeResetTimestamp;
    private double _audioMeterDisplayLevel;
    private double _audioMeterTargetLevel;
    private double _micMeterDisplayLevel;
    private double _micMeterTargetLevel;
    private LinearGradientBrush? _audioMeterColorBrush;
    private DispatcherQueueTimer? _audioMeterAnimationTimer;
    private Storyboard? _audioMeterMonitoringStoryboard;

    public AudioMeterController(AudioMeterControllerContext context)
    {
        _context = context;
    }

    public void Initialize()
    {
        _audioMeterAnimationTimer = _context.DispatcherQueue.CreateTimer();
        _audioMeterAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _audioMeterAnimationTimer.IsRepeating = true;
        _audioMeterAnimationTimer.Tick += (_, _) => AnimateTick();

        _audioMeterColorBrush = (LinearGradientBrush)_context.AudioMeterFill.Background;

        SetupRoundedContentClip(_context.AudioMeterContent, 3f);
        SetupRoundedContentClip(_context.MicMeterContent, 3f);
    }

    public void AnimateTick()
    {
        _audioMeterTargetLevel = _context.ViewModel.AudioMeterTarget;
        var target = _audioMeterTargetLevel;
        var nowMs = Environment.TickCount64;

        if (target >= _audioMeterDisplayLevel)
        {
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.4;
        }
        else
        {
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.06;
        }

        if (_audioMeterDisplayLevel < 0.001)
        {
            _audioMeterDisplayLevel = 0;
        }

        if (target >= _audioPeakHoldLevel)
        {
            _audioPeakHoldLevel = target;
            _audioPeakHoldTimestamp = nowMs;
        }
        else if (nowMs - _audioPeakHoldTimestamp > AudioPeakHoldDurationMs)
        {
            var dt = (nowMs - _audioPeakHoldTimestamp - AudioPeakHoldDurationMs) / 1000.0;
            _audioPeakHoldLevel = Math.Max(0, _audioPeakHoldLevel - (AudioPeakHoldDecayPerSecond * dt));
            _audioPeakHoldTimestamp = nowMs - AudioPeakHoldDurationMs;
        }

        if (nowMs - _audioRangeResetTimestamp > AudioRangeWindowMs)
        {
            _audioRangeMin = target;
            _audioRangeMax = target;
            _audioRangeResetTimestamp = nowMs;
        }
        else
        {
            if (target < _audioRangeMin) _audioRangeMin = target;
            if (target > _audioRangeMax) _audioRangeMax = target;
        }

        var trackWidth = _context.AudioMeterTrack.ActualWidth;
        if (trackWidth > 0)
        {
            var trackHeight = _context.AudioMeterTrack.ActualHeight > 0 ? _context.AudioMeterTrack.ActualHeight : 8;
            var rawLevel = _audioMeterDisplayLevel;
            var colorLevel = rawLevel * _context.ViewModel.PreviewVolume;

            _context.AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * rawLevel, trackHeight);
            _context.AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * colorLevel, trackHeight);

            _context.AudioPeakHoldTranslate.X = TranslateMarker(trackWidth, _audioPeakHoldLevel, _context.AudioPeakHoldIndicator.Width);
            _context.AudioRangeMinTranslate.X = TranslateMarker(trackWidth, _audioRangeMin, _context.AudioRangeMinMarker.Width);
            _context.AudioRangeMaxTranslate.X = TranslateMarker(trackWidth, _audioRangeMax, _context.AudioRangeMaxMarker.Width);
        }

        if (_context.ViewModel.IsMicrophoneEnabled)
        {
            _micMeterTargetLevel = Math.Clamp(_context.ViewModel.MicrophoneMeterTarget, 0.0, 1.0);
            if (_micMeterTargetLevel > _micMeterDisplayLevel)
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.4;
            }
            else
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.25;
            }

            if (_micMeterDisplayLevel < 0.001)
            {
                _micMeterDisplayLevel = 0;
            }

            var micTrackWidth = _context.MicMeterTrack.ActualWidth - 2;
            if (micTrackWidth > 0)
            {
                var micFillWidth = _micMeterDisplayLevel * micTrackWidth;
                _context.MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, micFillWidth, 8);
            }
        }
        else if (_micMeterDisplayLevel != 0 || _micMeterTargetLevel != 0)
        {
            ResetMicrophoneVisuals();
        }

        if (_audioMeterDisplayLevel == 0 &&
            _audioPeakHoldLevel == 0 &&
            target == 0 &&
            _micMeterDisplayLevel == 0 &&
            _micMeterTargetLevel == 0)
        {
            _audioMeterAnimationTimer?.Stop();
            _context.ViewModel.ResetAudioMeterTimerFlag();
        }
    }

    public void ResetVisuals()
    {
        _audioPeakHoldLevel = 0;
        _audioPeakHoldTimestamp = 0;
        _audioRangeMin = 1.0;
        _audioRangeMax = 0;
        _audioRangeResetTimestamp = 0;
        _audioMeterDisplayLevel = 0;
        _context.AudioPeakHoldTranslate.X = 0;
        _context.AudioRangeMinTranslate.X = 0;
        _context.AudioRangeMaxTranslate.X = 0;
        _audioMeterTargetLevel = 0;
        _context.AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        _context.AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        ResetMicrophoneVisuals();
    }

    public void ResetMicrophoneVisuals()
    {
        _micMeterDisplayLevel = 0;
        _micMeterTargetLevel = 0;
        _context.MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
    }

    public void SetAudioMeterTargetLevel(double targetLevel)
    {
        _audioMeterTargetLevel = Math.Clamp(targetLevel, 0.0, 1.0);
    }

    public void EnsureTimerRunning()
    {
        if (_audioMeterAnimationTimer is { IsRunning: false })
        {
            _audioMeterAnimationTimer.Start();
        }
    }

    public void StopTimer()
    {
        _audioMeterAnimationTimer?.Stop();
        _audioMeterAnimationTimer = null;
    }

    public void SetMonitoringState(bool isMonitoring)
    {
        if (_audioMeterColorBrush == null) return;

        _audioMeterMonitoringStoryboard?.Stop();

        _context.AudioPeakHoldIndicator.Background = isMonitoring ? MonitoringPeakBrush : IdlePeakBrush;

        var duration = TimeSpan.FromMilliseconds(isMonitoring ? 260 : 220);
        var easing = new CubicEase { EasingMode = isMonitoring ? EasingMode.EaseOut : EasingMode.EaseIn };
        var storyboard = new Storyboard();

        AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0, duration, easing);
        AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4, duration, easing);
        AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2, duration, easing);
        AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3, duration, easing);

        _audioMeterMonitoringStoryboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            _audioMeterMonitoringStoryboard = null;
        };
        storyboard.Begin();
    }

    public void AnimateDisabled(bool isDisabled)
    {
        _audioMeterMonitoringStoryboard?.Stop();
        _audioMeterMonitoringStoryboard = null;
        var targetOpacity = isDisabled ? 0.0 : 1.0;
        var duration = TimeSpan.FromMilliseconds(300);
        var easing = new CubicEase { EasingMode = isDisabled ? EasingMode.EaseIn : EasingMode.EaseOut };

        var storyboard = new Storyboard();

        foreach (var element in new UIElement[]
                 {
                     _context.AudioMeterRawFill,
                     _context.AudioMeterFill,
                     _context.AudioPeakHoldIndicator,
                     _context.AudioRangeMinMarker,
                     _context.AudioRangeMaxMarker
                 })
        {
            var anim = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = new Duration(duration),
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, "Opacity");
            storyboard.Children.Add(anim);
        }

        if (!isDisabled)
        {
            storyboard.Completed += (_, _) =>
            {
                SetMonitoringState(_context.ViewModel.IsAudioPreviewActive);
            };
        }

        storyboard.Begin();
    }

    public static double TranslateMarker(double trackWidth, double level, double markerWidth)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var availableWidth = Math.Max(0, trackWidth - markerWidth);
        return availableWidth * clamped;
    }

    private static void SetupRoundedContentClip(FrameworkElement element, float cornerRadius)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var geo = visual.Compositor.CreateRoundedRectangleGeometry();
        geo.CornerRadius = new Vector2(cornerRadius, cornerRadius);
        visual.Clip = visual.Compositor.CreateGeometricClip(geo);
        element.SizeChanged += (_, e) =>
        {
            geo.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }

    private static void AddOpacityAnimation(
        Storyboard storyboard,
        UIElement element,
        double targetOpacity,
        TimeSpan duration,
        EasingFunctionBase easing)
    {
        var anim = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, element);
        Storyboard.SetTargetProperty(anim, "Opacity");
        storyboard.Children.Add(anim);
    }
}

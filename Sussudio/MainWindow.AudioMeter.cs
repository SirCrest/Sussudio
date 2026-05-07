using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Audio-meter animation and rendering. The view model supplies level values;
// this partial turns them into smoothed, stacked visual meters without changing
// capture or monitoring state.
public sealed partial class MainWindow
{
    private void AnimateAudioMeterTick()
    {
        _audioMeterTargetLevel = ViewModel.AudioMeterTarget;
        var target = _audioMeterTargetLevel;
        var nowMs = Environment.TickCount64;

        // Smoothly interpolate display level toward target
        if (target >= _audioMeterDisplayLevel)
        {
            // Attack: fast snap toward peaks
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.4;
        }
        else
        {
            // Decay: smooth falloff
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.06;
        }

        // Snap to zero when very close to avoid lingering
        if (_audioMeterDisplayLevel < 0.001)
        {
            _audioMeterDisplayLevel = 0;
        }

        // Peak hold
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

        // Range tracking
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

        // Update visuals — two-layer meter: raw (grey) + volume-adjusted (color)
        var trackWidth = AudioMeterTrack.ActualWidth;
        if (trackWidth > 0)
        {
            var trackHeight = AudioMeterTrack.ActualHeight > 0 ? AudioMeterTrack.ActualHeight : 8;
            var rawLevel = _audioMeterDisplayLevel;
            var colorLevel = rawLevel * ViewModel.PreviewVolume;

            AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * rawLevel, trackHeight);
            AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * colorLevel, trackHeight);

            // Peak hold + range markers track raw signal
            AudioPeakHoldTranslate.X = TranslateMarker(trackWidth, _audioPeakHoldLevel, AudioPeakHoldIndicator.Width);
            AudioRangeMinTranslate.X = TranslateMarker(trackWidth, _audioRangeMin, AudioRangeMinMarker.Width);
            AudioRangeMaxTranslate.X = TranslateMarker(trackWidth, _audioRangeMax, AudioRangeMaxMarker.Width);
        }

        if (ViewModel.IsMicrophoneEnabled)
        {
            _micMeterTargetLevel = Math.Clamp(ViewModel.MicrophoneMeterTarget, 0.0, 1.0);
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

            var micTrackWidth = MicMeterTrack.ActualWidth - 2;
            if (micTrackWidth > 0)
            {
                var micFillWidth = _micMeterDisplayLevel * micTrackWidth;
                MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, micFillWidth, 8);
            }
        }
        else if (_micMeterDisplayLevel != 0 || _micMeterTargetLevel != 0)
        {
            _micMeterDisplayLevel = 0;
            _micMeterTargetLevel = 0;
            MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        }

        if (_audioMeterDisplayLevel == 0 &&
            _audioPeakHoldLevel == 0 &&
            target == 0 &&
            _micMeterDisplayLevel == 0 &&
            _micMeterTargetLevel == 0)
        {
            _audioMeterAnimationTimer?.Stop();
            ViewModel.ResetAudioMeterTimerFlag();
        }
    }
    private void ResetAudioMeterVisuals()
    {
        _audioPeakHoldLevel = 0;
        _audioPeakHoldTimestamp = 0;
        _audioRangeMin = 1.0;
        _audioRangeMax = 0;
        _audioRangeResetTimestamp = 0;
        _audioMeterDisplayLevel = 0;
        AudioPeakHoldTranslate.X = 0;
        AudioRangeMinTranslate.X = 0;
        AudioRangeMaxTranslate.X = 0;
        _audioMeterTargetLevel = 0;
        AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        _micMeterDisplayLevel = 0;
        _micMeterTargetLevel = 0;
        MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
    }
    private void InitializeAudioMeterBrushes()
    {
        _audioMeterAnimationTimer = _dispatcherQueue.CreateTimer();
        _audioMeterAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _audioMeterAnimationTimer.IsRepeating = true;
        _audioMeterAnimationTimer.Tick += (_, _) => AnimateAudioMeterTick();

        _audioMeterColorBrush = (LinearGradientBrush)AudioMeterFill.Background;

        // Clip content Grids to inner rounded rect (track CornerRadius=4, BorderThickness=1 → inner radius 3)
        SetupRoundedContentClip(AudioMeterContent, 3f);
        SetupRoundedContentClip(MicMeterContent, 3f);
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
    private void EnsureAudioMeterTimerRunning()
    {
        if (_audioMeterAnimationTimer is { IsRunning: false })
        {
            _audioMeterAnimationTimer.Start();
        }
    }
    private void SetAudioMeterMonitoringState(bool isMonitoring)
    {
        if (_audioMeterColorBrush == null) return;

        _audioMeterMonitoringStoryboard?.Stop();

        // Color layer visible only when monitoring; grey raw layer always shows through.
        AudioPeakHoldIndicator.Background = isMonitoring
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 160, 160));

        var duration = TimeSpan.FromMilliseconds(isMonitoring ? 260 : 220);
        var easing = new CubicEase { EasingMode = isMonitoring ? EasingMode.EaseOut : EasingMode.EaseIn };
        var storyboard = new Storyboard();

        AddOpacityAnimation(storyboard, AudioMeterFill, isMonitoring ? 1.0 : 0.0, duration, easing);
        AddOpacityAnimation(storyboard, AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4, duration, easing);
        AddOpacityAnimation(storyboard, AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2, duration, easing);
        AddOpacityAnimation(storyboard, AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3, duration, easing);

        _audioMeterMonitoringStoryboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            _audioMeterMonitoringStoryboard = null;
        };
        storyboard.Begin();
    }
    private void AnimateAudioMeterDisabled(bool isDisabled)
    {
        _audioMeterMonitoringStoryboard?.Stop();
        _audioMeterMonitoringStoryboard = null;
        var targetOpacity = isDisabled ? 0.0 : 1.0;
        var duration = TimeSpan.FromMilliseconds(300);
        var easing = new CubicEase { EasingMode = isDisabled ? EasingMode.EaseIn : EasingMode.EaseOut };

        var storyboard = new Storyboard();

        foreach (var element in new UIElement[] { AudioMeterRawFill, AudioMeterFill, AudioPeakHoldIndicator, AudioRangeMinMarker, AudioRangeMaxMarker })
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
                SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
            };
        }

        storyboard.Begin();
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
    private static double TranslateMarker(double trackWidth, double level, double markerWidth)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var availableWidth = Math.Max(0, trackWidth - markerWidth);
        return availableWidth * clamped;
    }
}

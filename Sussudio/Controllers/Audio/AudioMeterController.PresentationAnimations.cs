using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed partial class AudioMeterController
{
    private static readonly SolidColorBrush MonitoringPeakBrush =
        new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
    private static readonly SolidColorBrush IdlePeakBrush =
        new(Windows.UI.Color.FromArgb(255, 160, 160, 160));

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

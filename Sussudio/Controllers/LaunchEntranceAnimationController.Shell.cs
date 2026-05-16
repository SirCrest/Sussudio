using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed partial class LaunchEntranceAnimationController
{
    private Storyboard? _activeStoryboard;

    private void PlayEntranceAnimation()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        // 1. Control bar: slide up 20px + fade in (0ms, 350ms)
        var barFade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing
        };
        Storyboard.SetTarget(barFade, _context.ControlBarBorder);
        Storyboard.SetTargetProperty(barFade, "Opacity");
        storyboard.Children.Add(barFade);

        var barSlide = new DoubleAnimation
        {
            From = 20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(barSlide, (TranslateTransform)_context.ControlBarBorder.RenderTransform);
        Storyboard.SetTargetProperty(barSlide, "Y");
        storyboard.Children.Add(barSlide);

        // 2. Buttons stagger: 50ms offset, 200ms each (starting at 150ms)
        var buttons = _context.GetEntranceButtons();
        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            var beginTime = TimeSpan.FromMilliseconds(150 + (i * 50));
            var duration = TimeSpan.FromMilliseconds(200);

            var buttonFade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                BeginTime = beginTime,
                Duration = duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(buttonFade, button);
            Storyboard.SetTargetProperty(buttonFade, "Opacity");
            storyboard.Children.Add(buttonFade);

            if (button.RenderTransform is ScaleTransform transform)
            {
                var scaleX = new DoubleAnimation
                {
                    From = 0.85,
                    To = 1.0,
                    BeginTime = beginTime,
                    Duration = duration,
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleX, transform);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation
                {
                    From = 0.85,
                    To = 1.0,
                    BeginTime = beginTime,
                    Duration = duration,
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleY, transform);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");
                storyboard.Children.Add(scaleY);
            }
        }

        // 3. Stats row: slide down 10px + fade in (600ms begin, 300ms duration)
        var statsFade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing
        };
        Storyboard.SetTarget(statsFade, _context.StatsRow);
        Storyboard.SetTargetProperty(statsFade, "Opacity");
        storyboard.Children.Add(statsFade);

        var statsSlide = new DoubleAnimation
        {
            From = -10,
            To = 0,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(statsSlide, (TranslateTransform)_context.StatsRow.RenderTransform);
        Storyboard.SetTargetProperty(statsSlide, "Y");
        storyboard.Children.Add(statsSlide);

        // 4. Preview shell: only reveal it if the first visual is already confirmed.
        if (_context.IsPreviewFirstVisualConfirmed())
        {
            _context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);
        }
        else
        {
            Sussudio.Logger.Log("LAUNCH_PREVIEW_REVEAL_DEFERRED reason=waiting-for-first-visual");
        }

        storyboard.Completed += (_, _) =>
        {
            _activeStoryboard = null;
        };

        _activeStoryboard = storyboard;
        storyboard.Begin();

        // 5. Control bar shadow depth fade-in (Composition animation, compositor thread)
        // Delayed so the bar appears first, then gains depth.
        _context.FadeInControlBarShadow();
    }
}

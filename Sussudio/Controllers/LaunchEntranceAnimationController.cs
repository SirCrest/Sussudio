using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed class LaunchEntranceAnimationControllerContext
{
    public required UIElement SplashContent { get; init; }
    public required UIElement SplashOverlay { get; init; }
    public required ScaleTransform SplashScale { get; init; }
    public required FrameworkElement ControlBarBorder { get; init; }
    public required FrameworkElement StatsRow { get; init; }
    public required FrameworkElement PreviewBorder { get; init; }
    public required ScaleTransform PreviewBorderScale { get; init; }
    public required Func<IReadOnlyList<FrameworkElement>> GetEntranceButtons { get; init; }
    public required Func<bool> IsPreviewFirstVisualConfirmed { get; init; }
    public required Action StartSplashLoadingPhrases { get; init; }
    public required Action StopSplashLoadingPhrases { get; init; }
    public required Action<Storyboard, EasingFunctionBase, int, int> AddPreviewShellEntranceAnimations { get; init; }
    public required Action FadeInControlBarShadow { get; init; }
}

internal sealed class LaunchEntranceAnimationController
{
    private readonly LaunchEntranceAnimationControllerContext _context;
    private bool _played;
    private Storyboard? _activeStoryboard;

    public LaunchEntranceAnimationController(LaunchEntranceAnimationControllerContext context)
    {
        _context = context;
    }

    public void PrepareInitialState()
    {
        _context.ControlBarBorder.Opacity = 0;
        _context.ControlBarBorder.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 1.0);
        _context.ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };

        _context.StatsRow.Opacity = 0;
        _context.StatsRow.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0);
        _context.StatsRow.RenderTransform = new TranslateTransform { Y = -8 };

        _context.PreviewBorder.Opacity = 0;
        _context.PreviewBorderScale.ScaleX = 0.97;
        _context.PreviewBorderScale.ScaleY = 0.97;

        foreach (var button in _context.GetEntranceButtons())
        {
            button.Opacity = 0;
            if (button.RenderTransform is ScaleTransform transform)
            {
                transform.ScaleX = 0.85;
                transform.ScaleY = 0.85;
            }
        }
    }

    public void PlaySplashAndEntrance()
    {
        if (_played)
        {
            return;
        }

        _played = true;
        _context.StartSplashLoadingPhrases();

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var easingIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        // Phase 0: grey window settles in first; title and loading text bloom in
        // shortly after so the entrance feels staged rather than slapped down.
        var contentFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            BeginTime = TimeSpan.FromMilliseconds(180),
            Duration = TimeSpan.FromMilliseconds(420),
            EasingFunction = easing
        };
        Storyboard.SetTarget(contentFadeIn, _context.SplashContent);
        Storyboard.SetTargetProperty(contentFadeIn, "Opacity");

        // Phase 1: keep the splash up long enough for hidden device priming to begin,
        // then ease into the chrome while the preview shell waits for real frames.
        var splashFade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            BeginTime = TimeSpan.FromMilliseconds(3000),
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easingIn
        };
        Storyboard.SetTarget(splashFade, _context.SplashOverlay);
        Storyboard.SetTargetProperty(splashFade, "Opacity");

        var splashScaleX = new DoubleAnimation
        {
            From = 1.0,
            To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(3000),
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleX, _context.SplashScale);
        Storyboard.SetTargetProperty(splashScaleX, "ScaleX");

        var splashScaleY = new DoubleAnimation
        {
            From = 1.0,
            To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(3000),
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleY, _context.SplashScale);
        Storyboard.SetTargetProperty(splashScaleY, "ScaleY");

        var splashStoryboard = new Storyboard();
        splashStoryboard.Children.Add(contentFadeIn);
        splashStoryboard.Children.Add(splashFade);
        splashStoryboard.Children.Add(splashScaleX);
        splashStoryboard.Children.Add(splashScaleY);
        splashStoryboard.Completed += (_, _) =>
        {
            _context.StopSplashLoadingPhrases();
            _context.SplashOverlay.Visibility = Visibility.Collapsed;
            PlayEntranceAnimation();
        };
        splashStoryboard.Begin();
    }

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

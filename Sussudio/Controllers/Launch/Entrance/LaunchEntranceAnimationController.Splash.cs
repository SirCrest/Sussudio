using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed partial class LaunchEntranceAnimationController
{
    private bool _played;

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
}

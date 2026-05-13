using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;

namespace Sussudio;

// Shared WinUI animation helpers for preview, settings, stats, and transient
// visual states. Capture state is updated elsewhere; animations should make
// state changes legible without becoming the source of truth.
public sealed partial class MainWindow
{
    private void PlaySplashAndEntrance()
    {
        if (_entranceAnimationPlayed) return;
        _entranceAnimationPlayed = true;

        StartSplashLoadingPhrases();

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var easingIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        // Phase 0: grey window settles in first; title and loading text bloom in
        // shortly after so the entrance feels staged rather than slapped down.
        var contentFadeIn = new DoubleAnimation
        {
            From = 0, To = 1,
            BeginTime = TimeSpan.FromMilliseconds(180),
            Duration = TimeSpan.FromMilliseconds(420),
            EasingFunction = easing
        };
        Storyboard.SetTarget(contentFadeIn, SplashContent);
        Storyboard.SetTargetProperty(contentFadeIn, "Opacity");

        // Phase 1: keep the splash up long enough for hidden device priming to begin,
        // then ease into the chrome while the preview shell waits for real frames.
        var splashFade = new DoubleAnimation
        {
            From = 1, To = 0,
            BeginTime = TimeSpan.FromMilliseconds(3000),
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easingIn
        };
        Storyboard.SetTarget(splashFade, SplashOverlay);
        Storyboard.SetTargetProperty(splashFade, "Opacity");

        var splashScaleX = new DoubleAnimation
        {
            From = 1.0, To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(3000),
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleX, SplashScale);
        Storyboard.SetTargetProperty(splashScaleX, "ScaleX");

        var splashScaleY = new DoubleAnimation
        {
            From = 1.0, To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(3000),
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleY, SplashScale);
        Storyboard.SetTargetProperty(splashScaleY, "ScaleY");

        var splashSb = new Storyboard();
        splashSb.Children.Add(contentFadeIn);
        splashSb.Children.Add(splashFade);
        splashSb.Children.Add(splashScaleX);
        splashSb.Children.Add(splashScaleY);
        splashSb.Completed += (_, _) =>
        {
            StopSplashLoadingPhrases();
            SplashOverlay.Visibility = Visibility.Collapsed;
            PlayEntranceAnimation();
        };
        splashSb.Begin();
    }
    private void PlayEntranceAnimation()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        // 1. Control bar: slide up 20px + fade in (0ms, 350ms)
        var barFade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing
        };
        Storyboard.SetTarget(barFade, ControlBarBorder);
        Storyboard.SetTargetProperty(barFade, "Opacity");
        storyboard.Children.Add(barFade);

        var barSlide = new DoubleAnimation
        {
            From = 20, To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(barSlide, (TranslateTransform)ControlBarBorder.RenderTransform);
        Storyboard.SetTargetProperty(barSlide, "Y");
        storyboard.Children.Add(barSlide);

        // 2. Buttons stagger: 50ms offset, 200ms each (starting at 150ms)
        var buttons = GetEntranceButtons();
        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            var beginTime = TimeSpan.FromMilliseconds(150 + (i * 50));
            var duration = TimeSpan.FromMilliseconds(200);

            var buttonFade = new DoubleAnimation
            {
                From = 0, To = 1,
                BeginTime = beginTime, Duration = duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(buttonFade, button);
            Storyboard.SetTargetProperty(buttonFade, "Opacity");
            storyboard.Children.Add(buttonFade);

            if (button.RenderTransform is ScaleTransform transform)
            {
                var scaleX = new DoubleAnimation
                {
                    From = 0.85, To = 1.0,
                    BeginTime = beginTime, Duration = duration,
                    EasingFunction = easing, EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleX, transform);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation
                {
                    From = 0.85, To = 1.0,
                    BeginTime = beginTime, Duration = duration,
                    EasingFunction = easing, EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleY, transform);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");
                storyboard.Children.Add(scaleY);
            }
        }

        // 3. Stats row: slide down 10px + fade in (600ms begin, 300ms duration)
        var statsFade = new DoubleAnimation
        {
            From = 0, To = 1,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing
        };
        Storyboard.SetTarget(statsFade, StatsRow);
        Storyboard.SetTargetProperty(statsFade, "Opacity");
        storyboard.Children.Add(statsFade);

        var statsSlide = new DoubleAnimation
        {
            From = -10, To = 0,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(statsSlide, (TranslateTransform)StatsRow.RenderTransform);
        Storyboard.SetTargetProperty(statsSlide, "Y");
        storyboard.Children.Add(statsSlide);

        // 4. Preview shell: only reveal it if the first visual is already confirmed.
        if (_previewFirstVisualConfirmed)
        {
            AddPreviewShellEntranceAnimations(storyboard, easing, beginMs: 900, durationMs: 400);
        }
        else
        {
            Logger.Log("LAUNCH_PREVIEW_REVEAL_DEFERRED reason=waiting-for-first-visual");
        }

        storyboard.Completed += (_, _) =>
        {
            _entranceStoryboard = null;
        };

        _entranceStoryboard = storyboard;
        storyboard.Begin();

        // 5. Control bar shadow depth fade-in (Composition animation, compositor thread)
        // Delayed so the bar appears first, then gains depth.
        FadeInShadow(_controlBarShadowVisual, delayMs: 400, durationMs: 500);
    }
    private void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)
    {
        var beginTime = TimeSpan.FromMilliseconds(beginMs);
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var previewFade = new DoubleAnimation
        {
            To = 1,
            BeginTime = beginTime,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(previewFade, PreviewBorder);
        Storyboard.SetTargetProperty(previewFade, "Opacity");
        storyboard.Children.Add(previewFade);

        var previewScaleX = new DoubleAnimation
        {
            To = 1.0,
            BeginTime = beginTime,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleX, PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleX, "ScaleX");
        storyboard.Children.Add(previewScaleX);

        var previewScaleY = new DoubleAnimation
        {
            To = 1.0,
            BeginTime = beginTime,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleY, PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleY, "ScaleY");
        storyboard.Children.Add(previewScaleY);
    }
    private static void FadeInShadow(SpriteVisual? visual, int delayMs, int durationMs)
    {
        if (visual == null) return;
        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(0f, 0f);
        anim.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);
        anim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        visual.StartAnimation("Opacity", anim);
    }
    private static void FadeOutShadow(SpriteVisual? visual, int durationMs)
    {
        if (visual == null) return;
        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);
        visual.StartAnimation("Opacity", anim);
    }
    private void AnimateRecordButtonWidth(double from, double to, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, RecordButton);
        Storyboard.SetTargetProperty(anim, "Width");

        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Completed += (_, _) =>
        {
            // Set final width explicitly (NaN for pill, 36 for circle)
            RecordButton.Width = to == 36 ? 36 : double.NaN;
            onCompleted?.Invoke();
        };
        sb.Begin();
    }
    private void ResetPreviewContentTransform()
    {
        PreviewContentGrid.Opacity = 1.0;
        PreviewContentScale.ScaleX = 1.0;
        PreviewContentScale.ScaleY = 1.0;
    }
    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += (_, _) => tcs.TrySetResult(true);
        storyboard.Begin();
        return tcs.Task;
    }
    private Task AnimatePreviewTransitionAsync(double opacityTarget, double scaleTarget, int durationMs, EasingMode easingMode)
    {
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var easing = new CubicEase { EasingMode = easingMode };

        var fade = new DoubleAnimation { To = opacityTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(fade, PreviewContentGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var scaleX = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleX, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleY, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        return BeginStoryboardAsync(storyboard);
    }
    private Task AnimatePreviewShellInAsync(int durationMs)
    {
        if (PreviewBorder.Opacity >= 0.999 &&
            Math.Abs(PreviewBorderScale.ScaleX - 1.0) < 0.001 &&
            Math.Abs(PreviewBorderScale.ScaleY - 1.0) < 0.001)
        {
            return Task.CompletedTask;
        }

        var storyboard = new Storyboard();
        AddPreviewShellEntranceAnimations(
            storyboard,
            new CubicEase { EasingMode = EasingMode.EaseOut },
            beginMs: 0,
            durationMs: durationMs);
        return BeginStoryboardAsync(storyboard);
    }
    private Task AnimatePreviewOutAsync()
    {
        FadeOutShadow(_videoShadowVisual, durationMs: 150);
        return AnimatePreviewTransitionAsync(0.0, 0.97, 200, EasingMode.EaseIn);
    }
    private Task AnimatePreviewInAsync()
    {
        FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
        return Task.WhenAll(
            AnimatePreviewShellInAsync(350),
            AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut));
    }
    private void PreparePreviewStartupPresentation()
    {
        StopPreviewFadeInTimer();
        FadeOutElement(NoDevicePlaceholder);
        StartPreviewStartupOverlay();
        PreviewContentGrid.Opacity = 0.0;
        PreviewContentScale.ScaleX = 0.97;
        PreviewContentScale.ScaleY = 0.97;
    }
    private void RevealPreviewUnavailablePlaceholder()
    {
        StopPreviewStartupOverlay();
        StopPreviewFadeInTimer();
        ResetPreviewContentTransform();
        _ = AnimatePreviewShellInAsync(300);
        FadeInElement(NoDevicePlaceholder);
    }
    private static void FadeOutElement(UIElement element)
    {
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Completed += (_, _) =>
        {
            element.Visibility = Visibility.Collapsed;
            element.Opacity = 1.0;
        };
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
    private static void FadeInElement(UIElement element)
    {
        element.Opacity = 0.0;
        element.Visibility = Visibility.Visible;
        var animation = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}

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

namespace Sussudio;

// Shared WinUI animation helpers for preview, settings, stats, and transient
// visual states. Capture state is updated elsewhere; animations should make
// state changes legible without becoming the source of truth.
public sealed partial class MainWindow
{
    private FrameworkElement[] GetControlBarButtons() => new FrameworkElement[]
    {
        SettingsToggleButton,
        OpenRecordingsButton,
        ScreenshotButton,
        RecordButton,
        PreviewButton,
        HdrToggle,
        AudioRecordToggle,
        TrueHdrPreviewToggle,
        AudioPreviewToggle,
        StatsToggle,
        FrameTimeOverlayToggle
    };
    private void SetupButtonHoverAnimations()
    {
        foreach (var button in GetControlBarButtons())
        {
            var isHovered = false;
            button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            button.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };

            button.PointerEntered += (_, _) =>
            {
                isHovered = true;
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 1.08, TimeSpan.FromMilliseconds(100));
                }
            };

            button.PointerExited += (_, _) =>
            {
                isHovered = false;
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 1.0, TimeSpan.FromMilliseconds(100));
                }
            };

            button.PointerPressed += (_, _) =>
            {
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 0.95, TimeSpan.FromMilliseconds(60));
                }
            };

            button.PointerReleased += (_, _) =>
            {
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, isHovered ? 1.08 : 1.0, TimeSpan.FromMilliseconds(60));
                }
            };
        }
    }
    private FrameworkElement[] GetEntranceButtons() => GetControlBarButtons();

    // Fallback if SplashPhrases.md is missing or unreadable. Keep this list short —
    // the markdown file is the source of truth and ships next to the executable.
    private static readonly string[] DefaultSplashLoadingPhrases =
    {
        "Reticulating splines",
        "Re-rounding corners",
        "Warming the silicon",
        "Calibrating HDR",
        "Summoning Phil",
    };

    private static string[]? _cachedSplashPhrases;

    private static string[] LoadSplashPhrases()
    {
        if (_cachedSplashPhrases is not null) return _cachedSplashPhrases;

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "SplashPhrases.md");
            if (File.Exists(path))
            {
                var phrases = new List<string>();
                // Only collect lines that live under a `## ` section heading. Anything
                // before the first such heading (preamble, rules, etc.) is ignored.
                bool inPhraseSection = false;
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("##"))
                    {
                        inPhraseSection = true;
                        continue;
                    }
                    if (line.StartsWith('#')) continue;
                    if (!inPhraseSection) continue;
                    if (line.StartsWith("<!--")) continue;
                    if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
                        line = line[2..].Trim();
                    if (line.Length == 0) continue;
                    // Strip a trailing "..." in case someone adds it back in the file.
                    while (line.EndsWith('.')) line = line[..^1].TrimEnd();
                    if (line.Length == 0) continue;
                    phrases.Add(line);
                }
                if (phrases.Count > 0)
                {
                    _cachedSplashPhrases = phrases.ToArray();
                    return _cachedSplashPhrases;
                }
            }
        }
        catch
        {
            // Fall through to defaults — splash must never block startup.
        }

        _cachedSplashPhrases = DefaultSplashLoadingPhrases;
        return _cachedSplashPhrases;
    }

    private DispatcherTimer? _splashPhraseTimer;
    private string[] _splashPhrases = Array.Empty<string>();
    private int _splashPhraseIndex;

    // Two stacked text blocks ping-pong roles each cycle: the one at the line
    // position slides up and fades out while the new one slides up from below.
    private TextBlock[] _splashTextBlocks = Array.Empty<TextBlock>();
    private TranslateTransform[] _splashTransforms = Array.Empty<TranslateTransform>();
    private int _splashActiveBlock;
    private const double SplashLineHeight = 18;
    private const double SplashSlideDurationMs = 240;

    // Pace runs in streaks: mode persists for a few ticks before re-rolling, so the
    // rhythm feels like real work — mostly steady, occasional pauses, rare flurries.
    private enum SplashPaceMode { Burst, Normal, Stuck, LongStuck }
    private SplashPaceMode _splashPaceMode;
    private int _splashPaceTicksLeft;

    private TimeSpan NextSplashPhraseInterval()
    {
        if (_splashPaceTicksLeft <= 0)
        {
            var roll = Random.Shared.NextDouble();
            if (roll < 0.20)
            {
                _splashPaceMode = SplashPaceMode.Burst;
                _splashPaceTicksLeft = Random.Shared.Next(2, 6); // 2–5 fast ticks
            }
            else if (roll < 0.70)
            {
                _splashPaceMode = SplashPaceMode.Normal;
                _splashPaceTicksLeft = Random.Shared.Next(1, 4); // 1–3 medium ticks
            }
            else if (roll < 0.90)
            {
                _splashPaceMode = SplashPaceMode.Stuck;
                _splashPaceTicksLeft = 1;
            }
            else
            {
                _splashPaceMode = SplashPaceMode.LongStuck;
                _splashPaceTicksLeft = 1;
            }
        }
        _splashPaceTicksLeft--;

        // Burst minimum stays above SplashSlideDurationMs so animations don't overlap.
        int ms = _splashPaceMode switch
        {
            SplashPaceMode.Burst     => Random.Shared.Next(280, 420),
            SplashPaceMode.Normal    => Random.Shared.Next(380, 900),
            SplashPaceMode.Stuck     => Random.Shared.Next(900, 1500),
            SplashPaceMode.LongStuck => Random.Shared.Next(1500, 2500),
            _ => 600,
        };
        return TimeSpan.FromMilliseconds(ms);
    }

    private void StartSplashLoadingPhrases()
    {
        if (SplashLoadingTextA is null || SplashLoadingTextB is null) return;

        _splashPhrases = LoadSplashPhrases();
        if (_splashPhrases.Length == 0) return;

        _splashTextBlocks = new[] { SplashLoadingTextA, SplashLoadingTextB };
        _splashTransforms = new[] { SplashLoadingTransformA, SplashLoadingTransformB };

        // Let NextSplashPhraseInterval roll a fresh mode for the first tick — the
        // splash sometimes opens with a thoughtful pause instead of a flurry.
        _splashPaceTicksLeft = 0;

        _splashActiveBlock = 0;
        _splashPhraseIndex = Random.Shared.Next(_splashPhrases.Length);
        _splashTextBlocks[0].Text = _splashPhrases[_splashPhraseIndex] + "...";
        _splashTextBlocks[0].Opacity = 0.6;
        _splashTransforms[0].Y = 0;
        _splashTextBlocks[1].Text = string.Empty;
        _splashTextBlocks[1].Opacity = 0;
        _splashTransforms[1].Y = SplashLineHeight;

        _splashPhraseTimer = new DispatcherTimer { Interval = NextSplashPhraseInterval() };
        _splashPhraseTimer.Tick += (_, _) =>
        {
            if (_splashPhraseTimer is not null)
                _splashPhraseTimer.Interval = NextSplashPhraseInterval();
            CycleSplashPhrase();
        };
        _splashPhraseTimer.Start();
    }

    private void CycleSplashPhrase()
    {
        if (_splashTextBlocks.Length < 2 || _splashPhrases.Length == 0) return;

        _splashPhraseIndex = (_splashPhraseIndex + 1) % _splashPhrases.Length;
        var nextPhrase = _splashPhrases[_splashPhraseIndex] + "...";

        int outIdx = _splashActiveBlock;
        int inIdx = 1 - _splashActiveBlock;
        var outBlock = _splashTextBlocks[outIdx];
        var outXform = _splashTransforms[outIdx];
        var inBlock = _splashTextBlocks[inIdx];
        var inXform = _splashTransforms[inIdx];

        // Snap outgoing to the line position and prep incoming below.
        // Explicit reset means a fresh state even if the previous animation was interrupted.
        outXform.Y = 0;
        outBlock.Opacity = 0.6;
        inBlock.Text = nextPhrase;
        inXform.Y = SplashLineHeight;
        inBlock.Opacity = 0;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromMilliseconds(SplashSlideDurationMs);
        var sb = new Storyboard();

        var outY = new DoubleAnimation { To = -SplashLineHeight, Duration = dur, EasingFunction = ease };
        Storyboard.SetTarget(outY, outXform);
        Storyboard.SetTargetProperty(outY, "Y");
        sb.Children.Add(outY);

        var outOp = new DoubleAnimation { To = 0, Duration = dur, EasingFunction = ease };
        Storyboard.SetTarget(outOp, outBlock);
        Storyboard.SetTargetProperty(outOp, "Opacity");
        sb.Children.Add(outOp);

        var inY = new DoubleAnimation { To = 0, Duration = dur, EasingFunction = ease };
        Storyboard.SetTarget(inY, inXform);
        Storyboard.SetTargetProperty(inY, "Y");
        sb.Children.Add(inY);

        var inOp = new DoubleAnimation { To = 0.6, Duration = dur, EasingFunction = ease };
        Storyboard.SetTarget(inOp, inBlock);
        Storyboard.SetTargetProperty(inOp, "Opacity");
        sb.Children.Add(inOp);

        sb.Begin();
        _splashActiveBlock = inIdx;
    }

    private void StopSplashLoadingPhrases()
    {
        _splashPhraseTimer?.Stop();
        _splashPhraseTimer = null;
    }

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
        for (var i = 0; i < buttons.Length; i++)
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
    private static void AnimateScale(ScaleTransform target, double to, TimeSpan duration)
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        var scaleX = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleX, target);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleY, target);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Begin();
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
    private void PrimePreviewAudioFadeIn()
    {
        var volumeTarget = ViewModel.PreviewVolume > 0 ? ViewModel.PreviewVolume : _savedPreviewVolume;
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        if (volumeTarget <= 0)
        {
            _savedPreviewVolume = 0;
            _isVolumeFadingIn = false;
            ViewModel.VolumeSaveOverride = null;
            PreviewVolumeSlider.Value = 0;
            PreviewVolumeLabel.Text = "0%";
            return;
        }

        _savedPreviewVolume = volumeTarget;
        _isVolumeFadingIn = true;
        ViewModel.VolumeSaveOverride = volumeTarget;
        ViewModel.SuppressVolumeSave = true;
        try
        {
            ViewModel.PreviewVolume = 0;
            PreviewVolumeSlider.Value = 0;
            PreviewVolumeLabel.Text = "0%";
        }
        finally
        {
            ViewModel.SuppressVolumeSave = false;
        }

        Logger.Log($"PREVIEW_AUDIO_FADE_PRIMED targetPct={volumeTarget * 100:0}");
    }
    private void StartPreviewAudioFadeIn(int durationMs = 900)
    {
        if (!_isVolumeFadingIn)
        {
            return;
        }

        var volumeTarget = Math.Clamp(_savedPreviewVolume, 0.0, 1.0);
        if (volumeTarget <= 0)
        {
            CompletePreviewAudioFadeIn(applyTarget: false);
            return;
        }

        _previewVolumeFadeStoryboard?.Stop();
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var volumeAnim = new DoubleAnimation
        {
            From = PreviewVolumeSlider.Value,
            To = volumeTarget * 100,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);
        Storyboard.SetTargetProperty(volumeAnim, "Value");

        var storyboard = new Storyboard();
        storyboard.Children.Add(volumeAnim);
        storyboard.Completed += (_, _) => CompletePreviewAudioFadeIn(applyTarget: true);
        _previewVolumeFadeStoryboard = storyboard;
        ViewModel.SuppressVolumeSave = true;
        ViewModel.VolumeSaveOverride = volumeTarget;
        Logger.Log($"PREVIEW_AUDIO_FADE_IN_STARTED targetPct={volumeTarget * 100:0} durationMs={durationMs}");
        storyboard.Begin();
    }
    private void CompletePreviewAudioFadeIn(bool applyTarget)
    {
        _previewVolumeFadeStoryboard = null;
        _isVolumeFadingIn = false;
        ViewModel.SuppressVolumeSave = false;
        ViewModel.VolumeSaveOverride = null;
        if (applyTarget && _savedPreviewVolume > 0)
        {
            ViewModel.PreviewVolume = _savedPreviewVolume;
            PreviewVolumeSlider.Value = _savedPreviewVolume * 100;
            PreviewVolumeLabel.Text = $"{(int)(_savedPreviewVolume * 100)}%";
        }
    }
    private async Task StartPreviewAudioFadeOutAsync(int durationMs = 450)
    {
        var volumeTarget = ViewModel.PreviewVolume > 0 ? ViewModel.PreviewVolume : _savedPreviewVolume;
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        if (volumeTarget > 0)
        {
            _savedPreviewVolume = volumeTarget;
            ViewModel.VolumeSaveOverride = volumeTarget;
        }

        _isVolumeFadingIn = false;
        _previewVolumeFadeStoryboard?.Stop();
        if (PreviewVolumeSlider.Value <= 0.001 && ViewModel.PreviewVolume <= 0.001)
        {
            ViewModel.PreviewVolume = 0;
            PreviewVolumeSlider.Value = 0;
            PreviewVolumeLabel.Text = "0%";
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var volumeAnim = new DoubleAnimation
        {
            From = PreviewVolumeSlider.Value,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);
        Storyboard.SetTargetProperty(volumeAnim, "Value");

        var storyboard = new Storyboard();
        storyboard.Children.Add(volumeAnim);
        _previewVolumeFadeStoryboard = storyboard;
        ViewModel.SuppressVolumeSave = true;
        Logger.Log($"PREVIEW_AUDIO_FADE_OUT_STARTED fromPct={PreviewVolumeSlider.Value:0} durationMs={durationMs}");
        await BeginStoryboardAsync(storyboard);
        _previewVolumeFadeStoryboard = null;
        ViewModel.PreviewVolume = 0;
        PreviewVolumeSlider.Value = 0;
        PreviewVolumeLabel.Text = "0%";
        ViewModel.SuppressVolumeSave = false;
        Logger.Log("PREVIEW_AUDIO_FADE_OUT_COMPLETED");
    }
    private void CancelPreviewAudioFadeInForUser()
    {
        _previewVolumeFadeStoryboard?.Pause();
        _previewVolumeFadeStoryboard = null;
        _isVolumeFadingIn = false;
        ViewModel.SuppressVolumeSave = false;
        ViewModel.VolumeSaveOverride = null;
        _savedPreviewVolume = ViewModel.PreviewVolume;
    }
    private void UpdateLiveSignalInfoVisibility()
    {
        const string emDash = "\u2014";
        bool allReal =
            ViewModel.LiveResolution != emDash &&
            ViewModel.LiveFrameRate != emDash &&
            ViewModel.LivePixelFormat != emDash;

        if (allReal && !_liveSignalInfoVisible)
        {
            // Debounce: wait 500ms for values to stabilize before animating in.
            // During startup the pipeline cascades through Requested → Negotiated → Actual,
            // and each level can change the text width. Animating mid-cascade looks jerky.
            if (_liveSignalDebounceTimer == null)
            {
                _liveSignalDebounceTimer = DispatcherQueue.CreateTimer();
                _liveSignalDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
                _liveSignalDebounceTimer.IsRepeating = false;
                _liveSignalDebounceTimer.Tick += (_, _) =>
                {
                    _liveSignalDebounceTimer = null;
                    // Re-check: values might have reverted during the wait
                    bool stillReal =
                        ViewModel.LiveResolution != emDash &&
                        ViewModel.LiveFrameRate != emDash &&
                        ViewModel.LivePixelFormat != emDash;
                    if (stillReal && !_liveSignalInfoVisible)
                    {
                        _liveSignalInfoVisible = true;
                        AnimateLiveSignalInfoIn();
                    }
                };
            }
            _liveSignalDebounceTimer.Start();
        }
        else if (!allReal)
        {
            // Cancel any pending show debounce
            if (_liveSignalDebounceTimer != null)
            {
                _liveSignalDebounceTimer.Stop();
                _liveSignalDebounceTimer = null;
            }

            // Debounce hide: during Hz transitions the source bounces through
            // unstable states briefly. Wait 800ms before hiding to avoid flicker.
            if (_liveSignalInfoVisible)
            {
                if (_liveSignalHideDebounceTimer == null)
                {
                    _liveSignalHideDebounceTimer = DispatcherQueue.CreateTimer();
                    _liveSignalHideDebounceTimer.Interval = TimeSpan.FromMilliseconds(800);
                    _liveSignalHideDebounceTimer.IsRepeating = false;
                    _liveSignalHideDebounceTimer.Tick += (_, _) =>
                    {
                        _liveSignalHideDebounceTimer = null;
                        bool stillGone =
                            ViewModel.LiveResolution == emDash ||
                            ViewModel.LiveFrameRate == emDash ||
                            ViewModel.LivePixelFormat == emDash;
                        if (stillGone && _liveSignalInfoVisible)
                        {
                            _liveSignalInfoVisible = false;
                            AnimateLiveSignalInfoOut();
                        }
                    };
                }
                _liveSignalHideDebounceTimer.Start();
            }
        }
        else if (allReal && _liveSignalHideDebounceTimer != null)
        {
            // Signal recovered before hide debounce fired — cancel the hide
            _liveSignalHideDebounceTimer.Stop();
            _liveSignalHideDebounceTimer = null;
        }
    }
    private void AnimateLiveSignalInfoIn()
    {
        LiveSignalInfoPanel.Opacity = 0;
        LiveSignalInfoPanel.Visibility = Visibility.Visible;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        var scaleX = new DoubleAnimation
        {
            From = 0.92, To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleX, LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        storyboard.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            From = 0.92, To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleY, LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        storyboard.Children.Add(scaleY);

        storyboard.Begin();
    }
    private void AnimateLiveSignalInfoOut()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 1, To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        storyboard.Completed += (_, _) =>
        {
            LiveSignalInfoPanel.Visibility = Visibility.Collapsed;
        };

        storyboard.Begin();
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

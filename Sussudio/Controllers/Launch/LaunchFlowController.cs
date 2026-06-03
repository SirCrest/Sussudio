using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed class LaunchStartupControllerContext
{
    public required FrameworkElement MainContent { get; init; }
    public required RoutedEventHandler LoadedHandler { get; init; }
    public required Action ScheduleNativeShellRevealAfterFirstFrame { get; init; }
    public required Func<Func<Task>, string, Task> RunUiEventHandlerAsync { get; init; }
    public required Func<Task> InitializeViewModelAsync { get; init; }
    public required Action PrimePreviewAudioFadeIn { get; init; }
    public required Func<Task> RefreshDevicesAsync { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsPreviewFirstVisualConfirmed { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
    public required Action StartAutomationHost { get; init; }
    public required Action PlaySplashAndEntrance { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed class LaunchStartupController
{
    private readonly LaunchStartupControllerContext _context;

    public LaunchStartupController(LaunchStartupControllerContext context)
    {
        _context = context;
    }

    public void HandleLoaded(string operationName)
    {
        _context.MainContent.Loaded -= _context.LoadedHandler;
        _context.ScheduleNativeShellRevealAfterFirstFrame();

        _ = _context.RunUiEventHandlerAsync(async () =>
        {
            _context.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            await _context.InitializeViewModelAsync();
            // LoadSettings just pushed saved volume to CaptureService; re-prime it
            // so WASAPI playback starts silent and fades in only after live frames render.
            _context.PrimePreviewAudioFadeIn();
            await _context.RefreshDevicesAsync();
            if (!_context.IsPreviewing() && !_context.IsPreviewFirstVisualConfirmed())
            {
                _context.RevealPreviewUnavailablePlaceholder();
            }

            _context.StartAutomationHost();
        }, operationName);

        _context.PlaySplashAndEntrance();
    }
}

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

internal sealed class SplashLoadingPhraseControllerContext
{
    public required TextBlock SplashLoadingTextA { get; init; }
    public required TextBlock SplashLoadingTextB { get; init; }
    public required TranslateTransform SplashLoadingTransformA { get; init; }
    public required TranslateTransform SplashLoadingTransformB { get; init; }
}

internal sealed class SplashLoadingPhraseController
{
    private const double SplashLineHeight = 18;
    private const double SplashSlideDurationMs = 240;

    private readonly SplashLoadingPhraseControllerContext _context;
    private readonly SplashLoadingPhrasePacingPolicy _pacingPolicy = new();
    private DispatcherTimer? _splashPhraseTimer;
    private string[] _splashPhrases = Array.Empty<string>();
    private int _splashPhraseIndex;
    private TextBlock[] _splashTextBlocks = Array.Empty<TextBlock>();
    private TranslateTransform[] _splashTransforms = Array.Empty<TranslateTransform>();
    private int _splashActiveBlock;

    public SplashLoadingPhraseController(SplashLoadingPhraseControllerContext context)
    {
        _context = context;
    }

    public void Start()
    {
        _splashPhrases = SplashLoadingPhraseCatalog.Load();
        if (_splashPhrases.Length == 0)
        {
            return;
        }

        _splashTextBlocks = new[] { _context.SplashLoadingTextA, _context.SplashLoadingTextB };
        _splashTransforms = new[] { _context.SplashLoadingTransformA, _context.SplashLoadingTransformB };

        _pacingPolicy.Reset();
        _splashActiveBlock = 0;
        _splashPhraseIndex = Random.Shared.Next(_splashPhrases.Length);
        _splashTextBlocks[0].Text = _splashPhrases[_splashPhraseIndex] + "...";
        _splashTextBlocks[0].Opacity = 0.6;
        _splashTransforms[0].Y = 0;
        _splashTextBlocks[1].Text = string.Empty;
        _splashTextBlocks[1].Opacity = 0;
        _splashTransforms[1].Y = SplashLineHeight;

        _splashPhraseTimer = new DispatcherTimer { Interval = _pacingPolicy.NextInterval() };
        _splashPhraseTimer.Tick += (_, _) =>
        {
            if (_splashPhraseTimer is not null)
            {
                _splashPhraseTimer.Interval = _pacingPolicy.NextInterval();
            }

            CyclePhrase();
        };
        _splashPhraseTimer.Start();
    }

    public void Stop()
    {
        _splashPhraseTimer?.Stop();
        _splashPhraseTimer = null;
    }

    private void CyclePhrase()
    {
        if (_splashTextBlocks.Length < 2 || _splashPhrases.Length == 0) return;

        _splashPhraseIndex = (_splashPhraseIndex + 1) % _splashPhrases.Length;
        var nextPhrase = _splashPhrases[_splashPhraseIndex] + "...";

        var outIdx = _splashActiveBlock;
        var inIdx = 1 - _splashActiveBlock;
        var outBlock = _splashTextBlocks[outIdx];
        var outXform = _splashTransforms[outIdx];
        var inBlock = _splashTextBlocks[inIdx];
        var inXform = _splashTransforms[inIdx];

        outXform.Y = 0;
        outBlock.Opacity = 0.6;
        inBlock.Text = nextPhrase;
        inXform.Y = SplashLineHeight;
        inBlock.Opacity = 0;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(SplashSlideDurationMs);
        var storyboard = new Storyboard();

        var outY = new DoubleAnimation { To = -SplashLineHeight, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(outY, outXform);
        Storyboard.SetTargetProperty(outY, "Y");
        storyboard.Children.Add(outY);

        var outOp = new DoubleAnimation { To = 0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(outOp, outBlock);
        Storyboard.SetTargetProperty(outOp, "Opacity");
        storyboard.Children.Add(outOp);

        var inY = new DoubleAnimation { To = 0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(inY, inXform);
        Storyboard.SetTargetProperty(inY, "Y");
        storyboard.Children.Add(inY);

        var inOp = new DoubleAnimation { To = 0.6, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(inOp, inBlock);
        Storyboard.SetTargetProperty(inOp, "Opacity");
        storyboard.Children.Add(inOp);

        storyboard.Begin();
        _splashActiveBlock = inIdx;
    }
}

internal static class SplashLoadingPhraseCatalog
{
    private static readonly string[] DefaultSplashLoadingPhrases =
    {
        "Reticulating splines",
        "Re-rounding corners",
        "Warming the silicon",
        "Calibrating HDR",
        "Summoning Phil",
    };

    private static string[]? _cachedSplashPhrases;

    public static string[] Load()
    {
        if (_cachedSplashPhrases is not null) return _cachedSplashPhrases;

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "SplashPhrases.md");
            if (File.Exists(path))
            {
                var phrases = new List<string>();
                var inPhraseSection = false;
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
                    {
                        line = line[2..].Trim();
                    }
                    if (line.Length == 0) continue;

                    while (line.EndsWith('.'))
                    {
                        line = line[..^1].TrimEnd();
                    }

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
            // Splash copy must never block startup.
        }

        _cachedSplashPhrases = DefaultSplashLoadingPhrases;
        return _cachedSplashPhrases;
    }
}

internal sealed class SplashLoadingPhrasePacingPolicy
{
    private SplashLoadingPhrasePaceMode _mode;
    private int _ticksLeft;

    public TimeSpan NextInterval()
        => NextInterval(Random.Shared.NextDouble, Random.Shared.Next);

    internal TimeSpan NextInterval(Func<double> nextDouble, Func<int, int, int> nextInt)
    {
        if (_ticksLeft <= 0)
        {
            var roll = nextDouble();
            if (roll < 0.20)
            {
                _mode = SplashLoadingPhrasePaceMode.Burst;
                _ticksLeft = nextInt(2, 6);
            }
            else if (roll < 0.70)
            {
                _mode = SplashLoadingPhrasePaceMode.Normal;
                _ticksLeft = nextInt(1, 4);
            }
            else if (roll < 0.90)
            {
                _mode = SplashLoadingPhrasePaceMode.Stuck;
                _ticksLeft = 1;
            }
            else
            {
                _mode = SplashLoadingPhrasePaceMode.LongStuck;
                _ticksLeft = 1;
            }
        }

        _ticksLeft--;

        var ms = _mode switch
        {
            SplashLoadingPhrasePaceMode.Burst => nextInt(280, 420),
            SplashLoadingPhrasePaceMode.Normal => nextInt(380, 900),
            SplashLoadingPhrasePaceMode.Stuck => nextInt(900, 1500),
            SplashLoadingPhrasePaceMode.LongStuck => nextInt(1500, 2500),
            _ => 600,
        };
        return TimeSpan.FromMilliseconds(ms);
    }

    public void Reset()
    {
        _mode = default;
        _ticksLeft = 0;
    }
}

internal enum SplashLoadingPhrasePaceMode
{
    Burst,
    Normal,
    Stuck,
    LongStuck,
}

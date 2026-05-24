using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

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

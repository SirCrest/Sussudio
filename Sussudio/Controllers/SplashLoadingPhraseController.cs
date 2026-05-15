using System;
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
    private DispatcherTimer? _splashPhraseTimer;
    private string[] _splashPhrases = Array.Empty<string>();
    private int _splashPhraseIndex;
    private TextBlock[] _splashTextBlocks = Array.Empty<TextBlock>();
    private TranslateTransform[] _splashTransforms = Array.Empty<TranslateTransform>();
    private int _splashActiveBlock;
    private SplashPaceMode _splashPaceMode;
    private int _splashPaceTicksLeft;

    public SplashLoadingPhraseController(SplashLoadingPhraseControllerContext context)
    {
        _context = context;
    }

    private enum SplashPaceMode { Burst, Normal, Stuck, LongStuck }

    public void Start()
    {
        _splashPhrases = SplashLoadingPhraseCatalog.Load();
        if (_splashPhrases.Length == 0)
        {
            return;
        }

        _splashTextBlocks = new[] { _context.SplashLoadingTextA, _context.SplashLoadingTextB };
        _splashTransforms = new[] { _context.SplashLoadingTransformA, _context.SplashLoadingTransformB };

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
            {
                _splashPhraseTimer.Interval = NextSplashPhraseInterval();
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

    private TimeSpan NextSplashPhraseInterval()
    {
        if (_splashPaceTicksLeft <= 0)
        {
            var roll = Random.Shared.NextDouble();
            if (roll < 0.20)
            {
                _splashPaceMode = SplashPaceMode.Burst;
                _splashPaceTicksLeft = Random.Shared.Next(2, 6);
            }
            else if (roll < 0.70)
            {
                _splashPaceMode = SplashPaceMode.Normal;
                _splashPaceTicksLeft = Random.Shared.Next(1, 4);
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

        var ms = _splashPaceMode switch
        {
            SplashPaceMode.Burst => Random.Shared.Next(280, 420),
            SplashPaceMode.Normal => Random.Shared.Next(380, 900),
            SplashPaceMode.Stuck => Random.Shared.Next(900, 1500),
            SplashPaceMode.LongStuck => Random.Shared.Next(1500, 2500),
            _ => 600,
        };
        return TimeSpan.FromMilliseconds(ms);
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

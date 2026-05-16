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

internal sealed partial class LaunchEntranceAnimationController
{
    private readonly LaunchEntranceAnimationControllerContext _context;

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
}

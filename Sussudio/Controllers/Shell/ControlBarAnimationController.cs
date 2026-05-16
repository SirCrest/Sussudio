using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed class ControlBarAnimationControllerContext
{
    public required IReadOnlyList<FrameworkElement> ControlBarButtons { get; init; }
}

internal sealed class ControlBarAnimationController
{
    private readonly ControlBarAnimationControllerContext _context;

    public ControlBarAnimationController(ControlBarAnimationControllerContext context)
    {
        _context = context;
    }

    public IReadOnlyList<FrameworkElement> EntranceButtons => _context.ControlBarButtons;

    public void AttachHoverAnimations()
    {
        foreach (var button in _context.ControlBarButtons)
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
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed class RecordButtonAnimationControllerContext
{
    public required FrameworkElement RecordButton { get; init; }
}

internal sealed class RecordButtonAnimationController
{
    private readonly RecordButtonAnimationControllerContext _context;

    public RecordButtonAnimationController(RecordButtonAnimationControllerContext context)
    {
        _context = context;
    }

    public void AnimateWidth(double from, double to, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, _context.RecordButton);
        Storyboard.SetTargetProperty(anim, "Width");

        var storyboard = new Storyboard();
        storyboard.Children.Add(anim);
        storyboard.Completed += (_, _) =>
        {
            _context.RecordButton.Width = to == 36 ? 36 : double.NaN;
            onCompleted?.Invoke();
        };
        storyboard.Begin();
    }
}

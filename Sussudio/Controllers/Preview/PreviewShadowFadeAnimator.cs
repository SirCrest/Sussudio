using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace Sussudio.Controllers;

internal static class PreviewShadowFadeAnimator
{
    public static void FadeIn(SpriteVisual? visual, int delayMs, int durationMs)
    {
        if (visual == null) return;

        var compositor = visual.Compositor;
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        animation.Duration = TimeSpan.FromMilliseconds(durationMs);
        animation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        visual.StartAnimation("Opacity", animation);
    }

    public static void FadeOut(SpriteVisual? visual, int durationMs)
    {
        if (visual == null) return;

        var compositor = visual.Compositor;
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        animation.Duration = TimeSpan.FromMilliseconds(durationMs);
        visual.StartAnimation("Opacity", animation);
    }
}

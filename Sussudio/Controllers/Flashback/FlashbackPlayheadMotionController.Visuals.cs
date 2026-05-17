using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Sussudio.Controllers;

internal sealed partial class FlashbackPlayheadMotionController
{
    private void EnsureFlashbackPlayheadVisuals()
    {
        if (_flashbackPlayheadVisualsReady) return;

        _flashbackPlayheadVisual = ElementCompositionPreview.GetElementVisual(_context.Playhead);
        _flashbackPlayheadHandleVisual = ElementCompositionPreview.GetElementVisual(_context.PlayheadHandle);
        _flashbackPlayheadLabelVisual = ElementCompositionPreview.GetElementVisual(_context.PlayheadTimeBorder);
        _flashbackPlayheadCompositor = _flashbackPlayheadVisual.Compositor;
        _flashbackPlayheadEaseLinear = _flashbackPlayheadCompositor.CreateLinearEasingFunction();
        _flashbackPlayheadEaseWeighted = _flashbackPlayheadCompositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0.7f), new Vector2(0.1f, 1.0f));

        ElementCompositionPreview.SetIsTranslationEnabled(_context.Playhead, true);
        ElementCompositionPreview.SetIsTranslationEnabled(_context.PlayheadHandle, true);
        ElementCompositionPreview.SetIsTranslationEnabled(_context.PlayheadTimeBorder, true);

        // Anchor Canvas.Left at 0; from now on Translation.X carries the position.
        Canvas.SetLeft(_context.Playhead, 0);
        Canvas.SetLeft(_context.PlayheadHandle, 0);
        Canvas.SetLeft(_context.PlayheadTimeBorder, 0);

        _flashbackPlayheadVisualsReady = true;
        // First placement after init must snap; otherwise the playhead would
        // sweep from x=0 when the timeline opens.
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)
    {
        EnsureFlashbackPlayheadVisuals();

        _context.PlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = _context.PlayheadTimeBorder.DesiredSize.Width;
        var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));

        var lineX = (float)(x - 1);
        var handleX = (float)(x - 5);
        var labelTargetX = (float)labelX;

        if (_snapFlashbackPlayheadOnNextUpdate)
        {
            _snapFlashbackPlayheadOnNextUpdate = false;
            motion = FlashbackPlayheadMotion.Snap;
        }

        if (motion == FlashbackPlayheadMotion.Snap)
        {
            SnapFlashbackPlayheadX(_flashbackPlayheadVisual, lineX);
            SnapFlashbackPlayheadX(_flashbackPlayheadHandleVisual, handleX);
            SnapFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelTargetX);
            return;
        }

        // Magnetic ease toward pointer.
        AnimateFlashbackPlayheadX(_flashbackPlayheadVisual, lineX, _flashbackPlayheadEaseWeighted, FlashbackPlayheadDurationMagnetic);
        AnimateFlashbackPlayheadX(_flashbackPlayheadHandleVisual, handleX, _flashbackPlayheadEaseWeighted, FlashbackPlayheadDurationMagnetic);
        AnimateFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelTargetX, _flashbackPlayheadEaseWeighted, FlashbackPlayheadDurationMagnetic);
    }

    private void StartLinearPlayheadExtrapolation(double fracStart, double fracEnd, double trackW, TimeSpan duration, bool explicitStart)
    {
        if (_flashbackPlayheadCompositor == null) return;
        var linear = _flashbackPlayheadEaseLinear;
        if (linear == null) return;

        var startX = fracStart * trackW;
        var endX = fracEnd * trackW;

        _context.PlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = _context.PlayheadTimeBorder.DesiredSize.Width;
        var labelStart = (float)Math.Clamp(startX - labelW / 2, 0, Math.Max(0, trackW - labelW));
        var labelEnd = (float)Math.Clamp(endX - labelW / 2, 0, Math.Max(0, trackW - labelW));

        StartLinearKeyframe(_flashbackPlayheadVisual, (float)(startX - 1), (float)(endX - 1), duration, linear, explicitStart);
        StartLinearKeyframe(_flashbackPlayheadHandleVisual, (float)(startX - 5), (float)(endX - 5), duration, linear, explicitStart);
        StartLinearKeyframe(_flashbackPlayheadLabelVisual, labelStart, labelEnd, duration, linear, explicitStart);
    }

    private static void StartLinearKeyframe(Visual? v, float startX, float endX, TimeSpan duration, CompositionEasingFunction linear, bool explicitStart)
    {
        if (v == null) return;
        var anim = v.Compositor.CreateScalarKeyFrameAnimation();
        if (explicitStart) anim.InsertKeyFrame(0f, startX);
        anim.InsertKeyFrame(1f, endX, linear);
        anim.Duration = duration;
        v.StartAnimation("Translation.X", anim);
    }

    private void SnapPlayheadVisualsToFraction(double frac, double trackW)
    {
        var x = frac * trackW;
        SnapFlashbackPlayheadX(_flashbackPlayheadVisual, (float)(x - 1));
        SnapFlashbackPlayheadX(_flashbackPlayheadHandleVisual, (float)(x - 5));

        _context.PlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = _context.PlayheadTimeBorder.DesiredSize.Width;
        var labelX = (float)Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackW - labelW));
        SnapFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelX);
    }

    private void AnimateFlashbackPlayheadX(Visual? visual, float targetX, CompositionEasingFunction? easing, TimeSpan duration)
    {
        if (visual == null || _flashbackPlayheadCompositor == null || easing == null) return;
        var anim = _flashbackPlayheadCompositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, targetX, easing);
        anim.Duration = duration;
        visual.StartAnimation("Translation.X", anim);
    }

    private static void SnapFlashbackPlayheadX(Visual? visual, float targetX)
    {
        if (visual == null) return;
        visual.StopAnimation("Translation.X");
        visual.Properties.InsertVector3("Translation", new Vector3(targetX, 0f, 0f));
    }
}

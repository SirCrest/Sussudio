using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Sussudio;

// Flashback current-time-indicator visuals. Scrub/playback commands stay in
// MainWindow.Flashback.cs; steady-state CTI extrapolation lives in
// MainWindow.FlashbackPlayhead.CtiMotion.cs.
public sealed partial class MainWindow
{
    private enum FlashbackPlayheadMotion
    {
        Snap,
        Magnetic,
    }

    private Visual? _flashbackPlayheadVisual;
    private Visual? _flashbackPlayheadHandleVisual;
    private Visual? _flashbackPlayheadLabelVisual;
    private Compositor? _flashbackPlayheadCompositor;
    private CompositionEasingFunction? _flashbackPlayheadEaseWeighted;
    private bool _flashbackPlayheadVisualsReady;
    private bool _snapFlashbackPlayheadOnNextUpdate;
    private static readonly TimeSpan FlashbackPlayheadDurationMagnetic = TimeSpan.FromMilliseconds(60);

    private void EnsureFlashbackPlayheadVisuals()
    {
        if (_flashbackPlayheadVisualsReady) return;

        _flashbackPlayheadVisual = ElementCompositionPreview.GetElementVisual(FlashbackPlayhead);
        _flashbackPlayheadHandleVisual = ElementCompositionPreview.GetElementVisual(FlashbackPlayheadHandle);
        _flashbackPlayheadLabelVisual = ElementCompositionPreview.GetElementVisual(FlashbackPlayheadTimeBorder);
        _flashbackPlayheadCompositor = _flashbackPlayheadVisual.Compositor;
        _flashbackPlayheadEaseLinear = _flashbackPlayheadCompositor.CreateLinearEasingFunction();
        _flashbackPlayheadEaseWeighted = _flashbackPlayheadCompositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0.7f), new Vector2(0.1f, 1.0f));

        ElementCompositionPreview.SetIsTranslationEnabled(FlashbackPlayhead, true);
        ElementCompositionPreview.SetIsTranslationEnabled(FlashbackPlayheadHandle, true);
        ElementCompositionPreview.SetIsTranslationEnabled(FlashbackPlayheadTimeBorder, true);

        // Anchor Canvas.Left at 0; from now on Translation.X carries the position.
        Canvas.SetLeft(FlashbackPlayhead, 0);
        Canvas.SetLeft(FlashbackPlayheadHandle, 0);
        Canvas.SetLeft(FlashbackPlayheadTimeBorder, 0);

        _flashbackPlayheadVisualsReady = true;
        // First placement after init must snap; otherwise the playhead would
        // sweep from x=0 when the timeline opens.
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    // Pointer-driven scrub uses this. Snap or short Magnetic ease toward an
    // absolute x. Steady-state Playing/Paused/Live motion is driven by
    // RefreshFlashbackCtiMotion, not this method.
    private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)
    {
        EnsureFlashbackPlayheadVisuals();

        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
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

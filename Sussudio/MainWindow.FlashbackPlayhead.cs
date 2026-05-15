using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Sussudio.Models;
using Sussudio.Controllers;

namespace Sussudio;

// Flashback current-time-indicator visuals. Scrub/playback commands stay in
// MainWindow.Flashback.cs; this partial owns only compositor playhead motion.
public sealed partial class MainWindow
{
    // CTI motion — the timeline UI is an abstraction over the video pipeline.
    // The video pipeline ticks at 30Hz; the display refreshes at 60-144Hz. The
    // playhead is therefore driven NOT by per-tick eases (which restart at
    // every source update and reintroduce 30Hz stutter) but by a single long-
    // horizon linear extrapolation that the compositor evaluates every frame.
    //
    // Anchor model. We trust the video layer's "current position" only at
    // discrete moments — state edges (play/pause/seek/scrub-end/Live) plus a
    // 1Hz drift correction. At each anchor we compute (fraction_now,
    // fraction_in_60_seconds) and start a 60s linear ScalarKeyFrameAnimation
    // on Translation.X. The implicit-start animation reads the visual's
    // current Translation.X and tweens linearly to the horizon target —
    // velocity is continuous across re-anchors, so the user sees smooth flow.
    //
    // During Playing in a live-recording buffer both position and buffer-end
    // grow at 1ms/ms; the playhead moves slowly because the fraction barely
    // changes. The 60s linear segment is a faithful local approximation of the
    // exact (pos+t)/(buf+t) hyperbola for any reasonable buffer length.
    //
    // Active scrub is the one exception: each PointerMoved event fires a
    // short Magnetic ease (60ms cubic ease-out) toward the pointer x. That
    // looks tight under the finger and absorbs 16ms pointer jitter without
    // visible step.
    private enum FlashbackPlayheadMotion
    {
        Snap,
        Magnetic,
    }

    private Visual? _flashbackPlayheadVisual;
    private Visual? _flashbackPlayheadHandleVisual;
    private Visual? _flashbackPlayheadLabelVisual;
    private Compositor? _flashbackPlayheadCompositor;
    private CompositionEasingFunction? _flashbackPlayheadEaseLinear;
    private CompositionEasingFunction? _flashbackPlayheadEaseWeighted;
    private bool _flashbackPlayheadVisualsReady;
    private bool _snapFlashbackPlayheadOnNextUpdate;
    private FlashbackPlaybackState? _flashbackLastCtiState;
    private DispatcherQueueTimer? _flashbackCtiAnchorTimer;
    private bool _flashbackCtiAnchorRunning;
    private static readonly TimeSpan FlashbackPlayheadDurationMagnetic   = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan FlashbackCtiExtrapolationHorizon    = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FlashbackCtiAnchorDriftCorrection   = TimeSpan.FromMilliseconds(1000);

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
        // First placement after init must snap — otherwise the playhead would
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

    // Continuous-time CTI motion. Drives the playhead, handle, and floating
    // time label via a single 60-second linear ScalarKeyFrameAnimation on
    // Translation.X. The compositor evaluates this animation at the display's
    // refresh rate (60-144Hz), so motion is fluid regardless of the 30Hz
    // playback-position polling cadence. Re-anchored only on:
    //   - state edges (play/pause/Live/Scrubbing transitions)
    //   - panel show / SizeChanged
    //   - explicit seek (Paused/Live/Scrubbing position writes)
    //   - 1Hz drift correction during Playing or Paused
    // Active scrub is excluded — pointer events drive the visual via
    // PositionFlashbackPlayhead(.., Magnetic).
    private void RefreshFlashbackCtiMotion(string reason)
    {
        if (_isFlashbackScrubbing) return;
        if (_isWindowClosing) return;

        EnsureFlashbackPlayheadVisuals();

        var trackW = FlashbackScrubArea.ActualWidth;
        if (!FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)) return;

        var state = ViewModel.FlashbackState;

        // Anchor-timer lifecycle: only run during steady states with motion.
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Paused)
            StartFlashbackCtiAnchorTimer();
        else
            StopFlashbackCtiAnchorTimer();

        var stateChanged = state != _flashbackLastCtiState;
        _flashbackLastCtiState = state;

        var explicitStart = stateChanged
                          || _snapFlashbackPlayheadOnNextUpdate
                          || reason == "size_changed"
                          || reason == "panel_show"
                          || reason == "scrub_end"
                          || reason == "seek";
        if (_snapFlashbackPlayheadOnNextUpdate) _snapFlashbackPlayheadOnNextUpdate = false;

        if (state == FlashbackPlaybackState.Live)
        {
            // Right-edge pin. No motion to extrapolate.
            SnapPlayheadVisualsToFraction(1.0, trackW);
            return;
        }

        var bufferDurMs = ViewModel.FlashbackBufferFilledDuration.TotalMilliseconds;
        if (bufferDurMs <= 0) return;

        var posMs = ViewModel.FlashbackPlaybackPosition.TotalMilliseconds;

        var posRate = state == FlashbackPlaybackState.Playing ? 1.0 : 0.0;
        var bufRate = ViewModel.IsFlashbackEnabled ? 1.0 : 0.0;
        var horizonMs = FlashbackCtiExtrapolationHorizon.TotalMilliseconds;

        var posHorizon = Math.Max(0.0, posMs + posRate * horizonMs);
        var bufHorizon = Math.Max(1.0, bufferDurMs + bufRate * horizonMs);

        var fracNow = Math.Clamp(posMs / bufferDurMs, 0.0, 1.0);
        var fracHorizon = Math.Clamp(posHorizon / bufHorizon, 0.0, 1.0);

        StartLinearPlayheadExtrapolation(fracNow, fracHorizon, trackW, FlashbackCtiExtrapolationHorizon, explicitStart);
    }

    private void StartLinearPlayheadExtrapolation(double fracStart, double fracEnd, double trackW, TimeSpan duration, bool explicitStart)
    {
        if (_flashbackPlayheadCompositor == null) return;
        var linear = _flashbackPlayheadEaseLinear;
        if (linear == null) return;

        var startX = fracStart * trackW;
        var endX = fracEnd * trackW;

        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
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

        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
        var labelX = (float)Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackW - labelW));
        SnapFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelX);
    }

    private void StartFlashbackCtiAnchorTimer()
    {
        _flashbackCtiAnchorTimer ??= _dispatcherQueue.CreateTimer();
        if (_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Interval = FlashbackCtiAnchorDriftCorrection;
        _flashbackCtiAnchorTimer.IsRepeating = true;
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Tick += FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Start();
        _flashbackCtiAnchorRunning = true;
    }

    private void StopFlashbackCtiAnchorTimer()
    {
        if (_flashbackCtiAnchorTimer == null || !_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Stop();
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorRunning = false;
    }

    private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_isWindowClosing) return;
            RefreshFlashbackCtiMotion("anchor_tick");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CTI_ANCHOR_TICK_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }
}

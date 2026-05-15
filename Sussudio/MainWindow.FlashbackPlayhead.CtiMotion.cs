using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// Flashback CTI playback motion. Visual creation and pointer magnetic movement
// stay in MainWindow.FlashbackPlayhead.cs; this partial owns steady-state
// compositor extrapolation and anchor-timer correction.
public sealed partial class MainWindow
{
    // The timeline UI is an abstraction over the video pipeline. The video
    // pipeline ticks at 30Hz; the display refreshes at 60-144Hz. The playhead
    // is therefore driven by a long-horizon linear extrapolation that the
    // compositor evaluates every frame, not by per-tick eases that restart at
    // every source update and reintroduce 30Hz stutter.
    //
    // Anchor model: trust the video layer's current position only at state
    // edges (play/pause/seek/scrub-end/Live) plus a 1Hz drift correction. At
    // each anchor we compute fraction_now and fraction_in_60_seconds, then
    // start a 60s linear ScalarKeyFrameAnimation on Translation.X. The
    // implicit-start animation reads the visual's current Translation.X and
    // tweens linearly to the horizon target, keeping velocity continuous
    // across re-anchors.
    private FlashbackPlaybackState? _flashbackLastCtiState;
    private DispatcherQueueTimer? _flashbackCtiAnchorTimer;
    private CompositionEasingFunction? _flashbackPlayheadEaseLinear;
    private bool _flashbackCtiAnchorRunning;
    private static readonly TimeSpan FlashbackCtiExtrapolationHorizon = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FlashbackCtiAnchorDriftCorrection = TimeSpan.FromMilliseconds(1000);

    // Continuous-time CTI motion. Drives the playhead, handle, and floating
    // time label via a single 60-second linear ScalarKeyFrameAnimation on
    // Translation.X. The compositor evaluates this animation at the display's
    // refresh rate (60-144Hz), so motion is fluid regardless of the 30Hz
    // playback-position polling cadence. Re-anchored only on:
    //   - state edges (play/pause/Live/Scrubbing transitions)
    //   - panel show / SizeChanged
    //   - explicit seek (Paused/Live/Scrubbing position writes)
    //   - 1Hz drift correction during Playing or Paused
    // Active scrub is excluded; pointer events drive the visual via
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

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackPlayheadMotionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Func<bool> IsScrubbing { get; init; }
    public required FrameworkElement ScrubArea { get; init; }
    public required FrameworkElement Playhead { get; init; }
    public required FrameworkElement PlayheadHandle { get; init; }
    public required FrameworkElement PlayheadTimeBorder { get; init; }
}

internal sealed class FlashbackPlayheadMotionController
{
    private enum FlashbackPlayheadMotion
    {
        Snap,
        Magnetic,
    }

    private readonly FlashbackPlayheadMotionControllerContext _context;
    private Visual? _flashbackPlayheadVisual;
    private Visual? _flashbackPlayheadHandleVisual;
    private Visual? _flashbackPlayheadLabelVisual;
    private Compositor? _flashbackPlayheadCompositor;
    private CompositionEasingFunction? _flashbackPlayheadEaseWeighted;
    private bool _flashbackPlayheadVisualsReady;
    private bool _snapFlashbackPlayheadOnNextUpdate;
    private FlashbackPlaybackState? _flashbackLastCtiState;
    private DispatcherQueueTimer? _flashbackCtiAnchorTimer;
    private CompositionEasingFunction? _flashbackPlayheadEaseLinear;
    private bool _flashbackCtiAnchorRunning;
    private static readonly TimeSpan FlashbackPlayheadDurationMagnetic = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan FlashbackCtiExtrapolationHorizon = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FlashbackCtiAnchorDriftCorrection = TimeSpan.FromMilliseconds(1000);

    public FlashbackPlayheadMotionController(FlashbackPlayheadMotionControllerContext context)
    {
        _context = context;
    }

    public void RequestSnapOnNextUpdate()
    {
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    public void PositionMagneticPlayhead(double x, double trackWidth)
    {
        PositionFlashbackPlayhead(x, trackWidth, FlashbackPlayheadMotion.Magnetic);
    }

    public void RefreshCtiMotion(string reason)
    {
        if (_context.IsScrubbing()) return;
        if (_context.IsWindowClosing()) return;

        EnsureFlashbackPlayheadVisuals();

        var trackW = _context.ScrubArea.ActualWidth;
        if (!FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)) return;

        var state = _context.ViewModel.FlashbackState;

        // Anchor-timer lifecycle: only run during steady states with motion.
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Paused)
            StartFlashbackCtiAnchorTimer();
        else
            StopCtiAnchorTimer();

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

        var bufferDurMs = _context.ViewModel.FlashbackBufferFilledDuration.TotalMilliseconds;
        if (bufferDurMs <= 0) return;

        var posMs = _context.ViewModel.FlashbackPlaybackPosition.TotalMilliseconds;

        var posRate = state == FlashbackPlaybackState.Playing ? 1.0 : 0.0;
        var bufRate = _context.ViewModel.IsFlashbackEnabled ? 1.0 : 0.0;
        var horizonMs = FlashbackCtiExtrapolationHorizon.TotalMilliseconds;

        var posHorizon = Math.Max(0.0, posMs + posRate * horizonMs);
        var bufHorizon = Math.Max(1.0, bufferDurMs + bufRate * horizonMs);

        var fracNow = Math.Clamp(posMs / bufferDurMs, 0.0, 1.0);
        var fracHorizon = Math.Clamp(posHorizon / bufHorizon, 0.0, 1.0);

        StartLinearPlayheadExtrapolation(fracNow, fracHorizon, trackW, FlashbackCtiExtrapolationHorizon, explicitStart);
    }

    public void StopCtiAnchorTimer()
    {
        if (_flashbackCtiAnchorTimer == null || !_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Stop();
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorRunning = false;
    }

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

    private void StartFlashbackCtiAnchorTimer()
    {
        _flashbackCtiAnchorTimer ??= _context.DispatcherQueue.CreateTimer();
        if (_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Interval = FlashbackCtiAnchorDriftCorrection;
        _flashbackCtiAnchorTimer.IsRepeating = true;
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Tick += FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Start();
        _flashbackCtiAnchorRunning = true;
    }

    private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_context.IsWindowClosing()) return;
            RefreshCtiMotion("anchor_tick");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CTI_ANCHOR_TICK_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
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

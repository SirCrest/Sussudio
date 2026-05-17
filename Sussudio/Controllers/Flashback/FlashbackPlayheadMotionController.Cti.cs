using System;
using Microsoft.UI.Dispatching;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class FlashbackPlayheadMotionController
{
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
}

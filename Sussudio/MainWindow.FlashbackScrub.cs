using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

// Flashback pointer scrub interaction. This partial owns active scrub state,
// scrub throttling, and pointer lifecycle around scrub commands.
public sealed partial class MainWindow
{
    private bool _isFlashbackScrubbing;
    private TimeSpan? _lastScrubPointerPosition;
    private long _lastScrubUpdateTick;

    private void FlashbackScrubArea_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var targetPosition = ComputeFlashbackScrubPosition(e);
        if (!ViewModel.FlashbackBeginScrub(targetPosition))
        {
            _lastScrubPointerPosition = null;
            ViewModel.ReportFlashbackPlaybackRejection("scrub begin", "FLASHBACK_UI_SCRUB_BEGIN_REJECTED");
            return;
        }

        _isFlashbackScrubbing = true;
        _lastScrubPointerPosition = targetPosition;
        _lastScrubUpdateTick = 0;
        (sender as UIElement)?.CapturePointer(e.Pointer);
        UpdateFlashbackScrubVisual(e);
    }

    private void FlashbackScrubArea_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isFlashbackScrubbing) return;

        // Throttle scrub updates to ~60fps to avoid flooding the decoder.
        var now = Environment.TickCount64;
        if (now - _lastScrubUpdateTick < 16) return;
        _lastScrubUpdateTick = now;

        var targetPosition = ComputeFlashbackScrubPosition(e);
        if (!ViewModel.FlashbackUpdateScrub(targetPosition))
        {
            ViewModel.ReportFlashbackPlaybackRejection("scrub update", "FLASHBACK_UI_SCRUB_UPDATE_REJECTED");
            EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "update_rejected");
            return;
        }

        _lastScrubPointerPosition = targetPosition;
        UpdateFlashbackScrubVisual(e);
    }

    private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        TimeSpan? releasePosition = null;
        if (_isFlashbackScrubbing)
        {
            var targetPosition = ComputeFlashbackScrubPosition(e);
            releasePosition = targetPosition;
            _lastScrubPointerPosition = targetPosition;
            if (!ViewModel.FlashbackUpdateScrub(targetPosition))
            {
                ViewModel.ReportFlashbackPlaybackRejection("scrub release update", "FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED");
            }
            else
            {
                UpdateFlashbackScrubVisual(e);
            }
        }

        EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "released", releasePosition);
    }

    private void FlashbackScrubArea_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CANCELED carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "cancelled", carriedPosition);
    }

    private void FlashbackScrubArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CAPTURE_LOST carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "capture_lost", carriedPosition);
    }

    private void EndFlashbackScrubInteraction(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)
    {
        if (!_isFlashbackScrubbing)
        {
            return;
        }

        _isFlashbackScrubbing = false;
        _lastScrubUpdateTick = 0;
        _lastScrubPointerPosition = null;
        element?.ReleasePointerCapture(pointer);
        var ended = releasePosition.HasValue
            ? ViewModel.FlashbackEndScrubAt(releasePosition.Value)
            : ViewModel.FlashbackEndScrub();
        if (!ended)
        {
            ViewModel.ReportFlashbackPlaybackRejection($"scrub end ({reason})", $"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}");
        }

        Logger.Log($"FLASHBACK_UI_SCRUB_END reason={reason}");
        // Hand the visual back to the extrapolation driver from wherever the
        // pointer left it.
        RefreshFlashbackCtiMotion("scrub_end");
    }

    private void UpdateFlashbackScrubVisual(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(FlashbackScrubArea).Position;
        var width = FlashbackScrubArea.ActualWidth;
        if (!FlashbackTimelineGeometry.TryComputeFraction(pos.X, width, out var fraction)) return;

        var x = Math.Clamp(fraction * width, 0, width);
        // Magnetic = ease-out toward the pointer; longer than the 16ms pointer
        // throttle so successive events overlap into a single smooth trail
        // rather than 16ms-stepped jitter.
        PositionFlashbackPlayhead(x, width, FlashbackPlayheadMotion.Magnetic);

        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        if (FlashbackTimelineGeometry.IsUsableDuration(bufferDuration))
        {
            ViewModel.FlashbackPlaybackPosition = FlashbackTimelineGeometry.ComputePosition(fraction, bufferDuration);
        }
    }

    private TimeSpan ComputeFlashbackScrubPosition(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(FlashbackScrubArea).Position;
        var width = FlashbackScrubArea.ActualWidth;
        return FlashbackTimelineGeometry.TryComputePosition(
            pos.X,
            width,
            ViewModel.FlashbackBufferFilledDuration,
            out var position)
            ? position
            : TimeSpan.Zero;
    }
}

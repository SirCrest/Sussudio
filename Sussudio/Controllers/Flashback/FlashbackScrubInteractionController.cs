using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackScrubInteractionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required FrameworkElement ScrubArea { get; init; }
    public required Action<double, double> PositionMagneticPlayhead { get; init; }
    public required Action<string> RefreshCtiMotion { get; init; }
    public required Func<long> GetTickCount64 { get; init; }
}

internal sealed class FlashbackScrubInteractionController
{
    private readonly FlashbackScrubInteractionControllerContext _context;
    private bool _isScrubbing;
    private TimeSpan? _lastPointerPosition;
    private long _lastUpdateTick;

    public FlashbackScrubInteractionController(FlashbackScrubInteractionControllerContext context)
    {
        _context = context;
    }

    public bool IsScrubbing => _isScrubbing;

    public void PointerPressed(UIElement? element, PointerRoutedEventArgs e)
    {
        var targetPosition = ComputeScrubPosition(e);
        if (!_context.ViewModel.FlashbackBeginScrub(targetPosition))
        {
            _lastPointerPosition = null;
            _context.ViewModel.ReportFlashbackPlaybackRejection("scrub begin", "FLASHBACK_UI_SCRUB_BEGIN_REJECTED");
            return;
        }

        _isScrubbing = true;
        _lastPointerPosition = targetPosition;
        _lastUpdateTick = 0;
        element?.CapturePointer(e.Pointer);
        UpdateVisual(e);
    }

    public void PointerMoved(UIElement? element, PointerRoutedEventArgs e)
    {
        if (!_isScrubbing) return;

        // Throttle scrub updates to ~60fps to avoid flooding the decoder.
        var now = _context.GetTickCount64();
        if (now - _lastUpdateTick < 16) return;
        _lastUpdateTick = now;

        var targetPosition = ComputeScrubPosition(e);
        if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("scrub update", "FLASHBACK_UI_SCRUB_UPDATE_REJECTED");
            End(element, e.Pointer, "update_rejected");
            return;
        }

        _lastPointerPosition = targetPosition;
        UpdateVisual(e);
    }

    public void PointerReleased(UIElement? element, PointerRoutedEventArgs e)
    {
        TimeSpan? releasePosition = null;
        if (_isScrubbing)
        {
            var targetPosition = ComputeScrubPosition(e);
            releasePosition = targetPosition;
            _lastPointerPosition = targetPosition;
            if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))
            {
                _context.ViewModel.ReportFlashbackPlaybackRejection("scrub release update", "FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED");
            }
            else
            {
                UpdateVisual(e);
            }
        }

        End(element, e.Pointer, "released", releasePosition);
    }

    public void PointerCanceled(UIElement? element, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isScrubbing ? _lastPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CANCELED carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        End(element, e.Pointer, "cancelled", carriedPosition);
    }

    public void PointerCaptureLost(UIElement? element, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isScrubbing ? _lastPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CAPTURE_LOST carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        End(element, e.Pointer, "capture_lost", carriedPosition);
    }

    public void EndForFullScreen()
    {
        if (!_isScrubbing)
        {
            return;
        }

        var carriedPosition = _lastPointerPosition;
        Logger.Log($"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        ClearLocalState();
        var ended = carriedPosition.HasValue
            ? _context.ViewModel.FlashbackEndScrubAt(carriedPosition.Value)
            : _context.ViewModel.FlashbackEndScrub();
        if (!ended)
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("scrub end (fullscreen_enter)", "FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter");
        }
    }

    public void ClearForLockout()
    {
        ClearLocalState();
    }

    private void End(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)
    {
        if (!_isScrubbing)
        {
            return;
        }

        ClearLocalState();
        element?.ReleasePointerCapture(pointer);
        var ended = releasePosition.HasValue
            ? _context.ViewModel.FlashbackEndScrubAt(releasePosition.Value)
            : _context.ViewModel.FlashbackEndScrub();
        if (!ended)
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection($"scrub end ({reason})", $"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}");
        }

        Logger.Log($"FLASHBACK_UI_SCRUB_END reason={reason}");
        // Hand the visual back to the extrapolation driver from wherever the
        // pointer left it.
        _context.RefreshCtiMotion("scrub_end");
    }

    private void ClearLocalState()
    {
        _isScrubbing = false;
        _lastUpdateTick = 0;
        _lastPointerPosition = null;
    }

    private void UpdateVisual(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(_context.ScrubArea).Position;
        var width = _context.ScrubArea.ActualWidth;
        if (!FlashbackTimelineGeometry.TryComputeFraction(pos.X, width, out var fraction)) return;

        var x = Math.Clamp(fraction * width, 0, width);
        // Magnetic = ease-out toward the pointer; longer than the 16ms pointer
        // throttle so successive events overlap into a single smooth trail
        // rather than 16ms-stepped jitter.
        _context.PositionMagneticPlayhead(x, width);

        var bufferDuration = _context.ViewModel.FlashbackBufferFilledDuration;
        if (FlashbackTimelineGeometry.IsUsableDuration(bufferDuration))
        {
            _context.ViewModel.FlashbackPlaybackPosition = FlashbackTimelineGeometry.ComputePosition(fraction, bufferDuration);
        }
    }

    private TimeSpan ComputeScrubPosition(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(_context.ScrubArea).Position;
        var width = _context.ScrubArea.ActualWidth;
        return FlashbackTimelineGeometry.TryComputePosition(
            pos.X,
            width,
            _context.ViewModel.FlashbackBufferFilledDuration,
            out var position)
            ? position
            : TimeSpan.Zero;
    }
}

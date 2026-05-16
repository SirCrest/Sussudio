using System;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackPlaybackUiCoordinatorContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Action<double, double> ApplyTrackSize { get; init; }
    public required Action RequestPlayheadSnapOnNextUpdate { get; init; }
    public required Action UpdateMarkers { get; init; }
    public required Action<string> RefreshCtiMotion { get; init; }
    public required Func<bool> IsScrubbing { get; init; }
    public required Action StartPlaybackPolling { get; init; }
    public required Action StopPlaybackPolling { get; init; }
    public required FlashbackPlaybackPresentationController PlaybackPresentation { get; init; }
}

internal sealed class FlashbackPlaybackUiCoordinator
{
    private readonly FlashbackPlaybackUiCoordinatorContext _context;

    public FlashbackPlaybackUiCoordinator(FlashbackPlaybackUiCoordinatorContext context)
    {
        _context = context;
    }

    public void HandleTrackSizeChanged(double width, double height)
    {
        _context.ApplyTrackSize(width, height);

        // Track resize jumps the playhead to its layout-correct position
        // without sweeping through stale translation from the old width.
        _context.RequestPlayheadSnapOnNextUpdate();

        UpdatePosition();
        _context.UpdateMarkers();
        _context.RefreshCtiMotion("size_changed");
    }

    public void UpdateState()
    {
        var state = _context.ViewModel.FlashbackState;
        _context.PlaybackPresentation.UpdateState(state);

        // Keep the 30Hz playback timer running during Playing; its writes to
        // FlashbackPlaybackPosition still feed label text and VM consumers. CTI
        // visuals are driven by long-horizon extrapolation re-anchored on edges.
        if (state == FlashbackPlaybackState.Playing)
        {
            _context.StartPlaybackPolling();
        }
        else
        {
            _context.StopPlaybackPolling();
        }

        _context.RefreshCtiMotion("state_change");
    }

    public void UpdateBufferFill()
    {
        var duration = _context.ViewModel.FlashbackBufferFilledDuration;
        _context.PlaybackPresentation.UpdateBufferFill(duration);
    }

    public void UpdateBufferPresentation()
    {
        UpdateBufferFill();
        UpdatePosition();
        _context.UpdateMarkers();
    }

    // Position-changed handler. Visual CTI motion is driven by RefreshCtiMotion;
    // this method refreshes label text. For Paused/Live states a position change
    // implies seek or scrub-end, so it also re-anchors. Playing ticks deliberately
    // skip re-anchor.
    public void UpdatePosition()
    {
        var state = _context.ViewModel.FlashbackState;
        var bufferDuration = _context.ViewModel.FlashbackBufferFilledDuration;
        _context.PlaybackPresentation.UpdatePosition(
            state,
            bufferDuration,
            _context.ViewModel.FlashbackGapFromLive);

        if (!_context.IsScrubbing()
            && state != FlashbackPlaybackState.Playing
            && state != FlashbackPlaybackState.Scrubbing)
        {
            _context.RefreshCtiMotion("position_change");
        }
    }
}

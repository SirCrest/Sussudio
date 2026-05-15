using System;
using Sussudio.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio;

// Flashback timeline presentation glue. Command behavior lives in
// FlashbackCommandController; scrub/playhead, markers, playback, export, and
// settings each have their own focused controller or adapter partial.
public sealed partial class MainWindow
{
    private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var w = e.NewSize.Width;
        var h = e.NewSize.Height;

        // Size elements that fill the track.
        FlashbackTrackBackground.Width = w;
        FlashbackTrackBackground.Height = h;
        FlashbackScrubArea.Width = w;
        FlashbackScrubArea.Height = h;
        FlashbackPlayhead.Height = h;
        FlashbackLiveEdge.Height = h;

        Canvas.SetLeft(FlashbackLiveEdge, w - 2);

        // Track resize jumps the playhead to its layout-correct position
        // without sweeping through stale translation from the old width.
        _snapFlashbackPlayheadOnNextUpdate = true;

        UpdateFlashbackPositionUI();
        UpdateFlashbackMarkers();
        RefreshFlashbackCtiMotion("size_changed");
    }

    private void UpdateFlashbackStateUI()
    {
        var state = ViewModel.FlashbackState;
        _flashbackPlaybackPresentationController.UpdateState(state);

        // Keep the 30Hz playback timer running during Playing; its writes to
        // FlashbackPlaybackPosition still feed label text and VM consumers. CTI
        // visuals are driven by long-horizon extrapolation re-anchored on edges.
        if (state == FlashbackPlaybackState.Playing)
        {
            StartFlashbackPlaybackPolling();
        }
        else
        {
            StopFlashbackPlaybackPolling();
        }

        RefreshFlashbackCtiMotion("state_change");
    }

    private void UpdateFlashbackBufferFill()
    {
        var duration = ViewModel.FlashbackBufferFilledDuration;
        _flashbackPlaybackPresentationController.UpdateBufferFill(duration);
    }

    private static string FormatDiskSize(long bytes)
    {
        const double scale = 1024;
        double value = Math.Max(0, bytes);
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var unit = 0;
        while (value >= scale && unit < units.Length - 1)
        {
            value /= scale;
            unit++;
        }

        return unit >= 3 ? $"{value:F1} {units[unit]}" : $"{Math.Round(value):0} {units[unit]}";
    }

    // Position-changed handler. Visual CTI motion is driven by
    // RefreshFlashbackCtiMotion; this method refreshes label text. For
    // Paused/Live states a position change implies seek or scrub-end, so it
    // also re-anchors. Playing ticks deliberately skip re-anchor.
    private void UpdateFlashbackPositionUI()
    {
        var state = ViewModel.FlashbackState;
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        _flashbackPlaybackPresentationController.UpdatePosition(
            state,
            bufferDuration,
            ViewModel.FlashbackGapFromLive);

        if (!_flashbackScrubInteractionController.IsScrubbing
            && state != FlashbackPlaybackState.Playing
            && state != FlashbackPlaybackState.Scrubbing)
        {
            RefreshFlashbackCtiMotion("position_change");
        }
    }
}

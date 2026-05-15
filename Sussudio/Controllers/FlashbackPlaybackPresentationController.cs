using System;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed class FlashbackPlaybackPresentationControllerContext
{
    public required FontIcon PlayPauseIcon { get; init; }
    public required Button GoLiveButton { get; init; }
    public required TextBlock BufferDurationText { get; init; }
    public required TextBlock PlayheadTimeText { get; init; }
}

internal sealed class FlashbackPlaybackPresentationController
{
    private readonly FlashbackPlaybackPresentationControllerContext _context;

    public FlashbackPlaybackPresentationController(FlashbackPlaybackPresentationControllerContext context)
    {
        _context = context;
    }

    public static string GetPlayPauseGlyph(FlashbackPlaybackState state)
        => state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Live
            ? "\uE769"
            : "\uE768";

    public static bool IsGoLiveEnabled(FlashbackPlaybackState state)
        => state != FlashbackPlaybackState.Live && state != FlashbackPlaybackState.Disabled;

    public static string FormatPositionLabel(
        FlashbackPlaybackState state,
        TimeSpan bufferDuration,
        TimeSpan gapFromLive)
    {
        if (state == FlashbackPlaybackState.Live)
        {
            return "LIVE";
        }

        var totalText = FlashbackMarkerPresentationController.FormatDuration(bufferDuration);
        return $"-{FlashbackMarkerPresentationController.FormatDuration(gapFromLive)} / {totalText}";
    }

    public void UpdateState(FlashbackPlaybackState state)
    {
        _context.PlayPauseIcon.Glyph = GetPlayPauseGlyph(state);
        _context.GoLiveButton.IsEnabled = IsGoLiveEnabled(state);
    }

    public void UpdateBufferFill(TimeSpan duration)
    {
        _context.BufferDurationText.Text = FlashbackMarkerPresentationController.FormatDuration(duration);
    }

    public void UpdatePosition(
        FlashbackPlaybackState state,
        TimeSpan bufferDuration,
        TimeSpan gapFromLive)
    {
        _context.PlayheadTimeText.Text = FormatPositionLabel(state, bufferDuration, gapFromLive);
    }
}

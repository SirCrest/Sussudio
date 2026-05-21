using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Seek and scrub decoded-frame display handoff ---

    private bool TryDecodeAndDisplaySeekFrame(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        CommandKind kind,
        TimeSpan bufferPosition,
        TimeSpan validStartPts,
        ref TimeSpan filePts,
        CancellationToken cancellationToken)
    {
        var gotFrame = TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken);
        var frameOwned = gotFrame;
        try
        {
            if (!gotFrame &&
                TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $"seek_display:{kind}", out var adjacentFilePts, cancellationToken))
            {
                filePts = adjacentFilePts;
                cancellationToken.ThrowIfCancellationRequested();
                gotFrame = TryDecodeNextVideoFrameWithMetrics(decoder, out frame, cancellationToken);
                frameOwned = gotFrame;
            }

            if (gotFrame)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var submitted = TrySubmitAndHoldFrame(frame, "seek");
                frameOwned = false;
                if (!submitted)
                {
                    PlaybackPosition = bufferPosition;
                    SetSeekDisplayFailure(kind, "submit_failed", bufferPosition);
                    return false;
                }
                Interlocked.Exchange(ref _lastVideoPtsTicks, frame.Pts.Ticks);

                var actualPosition = SaturatingSubtract(frame.Pts, validStartPts);
                if (actualPosition < TimeSpan.Zero) actualPosition = TimeSpan.Zero;
                PlaybackPosition = actualPosition;
            }
            else
            {
                PlaybackPosition = bufferPosition;
                RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);
            }
        }
        finally
        {
            if (frameOwned)
            {
                ReleaseHeldFrameBestEffort(frame, "seek_cancelled");
            }
        }

        return gotFrame;
    }

    private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        RecordPlaybackDroppedFrame("seek_display_no_frame");
        SetSeekDisplayFailure(kind, "no_frame", bufferPosition);
        Logger.Log(
            $"FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE kind={kind} pos_ms={(long)bufferPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds}");
    }
}

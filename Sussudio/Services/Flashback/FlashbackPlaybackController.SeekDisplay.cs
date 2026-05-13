using System;
using System.Threading;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Seek and scrub frame display ---

    private bool SeekAndDisplayKeyframe(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan bufferPosition,
        TimeSpan validStartPts,
        CommandKind kind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Suppress audio delivery during scrub — prevents audio accumulation
        // in the WASAPI queue. Audio callback is re-enabled on Play/EndScrub.
        decoder.AudioChunkCallback = null;
        SafeFlushPlayback("seek_display_keyframe");

        bufferPosition = ClampPosition(bufferPosition, validStartPts);

        if (!decoder.IsOpen)
        {
            // No file — use requested position as fallback
            PlaybackPosition = bufferPosition;
            SetSeekDisplayFailure(kind, "no_file", bufferPosition);
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_NO_FILE pos_ms={(long)bufferPosition.TotalMilliseconds}");
            return false;
        }

        try
        {
            // Map buffer position to file PTS (offset by frozen valid start)
            var filePts = SaturatingAdd(bufferPosition, validStartPts);
            cancellationToken.ThrowIfCancellationRequested();

            // Clamp to current valid range: if eviction advanced ValidStartPts past
            // frozenValidStart, positions near the left edge map to evicted data.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (filePts < currentValidStart)
            {
                filePts = currentValidStart;
                bufferPosition = SaturatingSubtract(filePts, validStartPts);
                if (bufferPosition < TimeSpan.Zero) bufferPosition = TimeSpan.Zero;
            }

            if (!decoder.SeekToKeyframe(filePts, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Active fMP4 segment: demuxer caches fragment index at open time.
                // New fragments written since open aren't visible — reopen and retry.
                // Only for fMP4; .ts handles appended data via eof_reached reset.
                if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
                {
                    if (!ShouldSkipActiveFmp4ReopenNearLive(filePts, "seek_keyframe"))
                    {
                        Logger.Log($"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}");
                        if (TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, "seek_keyframe", cancellationToken))
                            goto seekSuccess;
                    }
                }

                PlaybackPosition = bufferPosition;
                SetSeekDisplayFailure(kind, "seek_failed", bufferPosition);
                Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL offset_ms={(long)filePts.TotalMilliseconds}");
                return false;
            }
            seekSuccess:
            cancellationToken.ThrowIfCancellationRequested();

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

                    // Set position to actual decoded frame PTS mapped back to buffer position
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

            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_OK pos_ms={(long)PlaybackPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds} got_frame={gotFrame}");
            return gotFrame;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // On error, use requested position as fallback
            PlaybackPosition = bufferPosition;
            SetSeekDisplayFailure(kind, ex.GetType().Name, bufferPosition);
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_ERROR type={ex.GetType().Name} error='{ex.Message}'");
            return false;
        }
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

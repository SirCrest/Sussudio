using System;
using System.Threading;

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

            var gotFrame = TryDecodeAndDisplaySeekFrame(
                decoder,
                ref fileOpen,
                kind,
                bufferPosition,
                validStartPts,
                ref filePts,
                cancellationToken);

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
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback-thread end-scrub command handler ---

    private void HandleEndScrubCommand(
        PlaybackCommand cmd,
        CancellationTokenSource cts,
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref bool isScrubbing,
        TimeSpan frozenValidStart,
        ref TimeSpan? pendingExactResumeTarget,
        ref TimeSpan frameDuration,
        Queue<DecodedVideoFrame> prebufferedFrames,
        Stopwatch pacingStopwatch)
    {
        if (!isScrubbing)
        {
            MarkCommandNoOp(CommandKind.EndScrub, "not_scrubbing", cmd.Position);
            return;
        }
        var endScrubPosition = ClampPosition(cmd.Position, frozenValidStart);
        PlaybackPosition = endScrubPosition;
        isScrubbing = false;
        isPlaying = _wasPlayingBeforeScrub;
        var endScrubTarget = SaturatingAdd(endScrubPosition, frozenValidStart);
        if (isPlaying)
        {
            pendingExactResumeTarget = null;
            ResetPlaybackMetrics();
            pacingStopwatch.Restart();

            // Re-seek to the current position using SeekTo (not SeekToKeyframe).
            // SeekTo forward-decodes from keyframe to target, which advances
            // BOTH the video and audio codecs to the same PTS. Without this,
            // the audio codec is stuck at the keyframe (~1s behind video).
            if (decoder is { IsOpen: true })
            {
                decoder.AudioChunkCallback = null; // null during forward-decode
                if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, "end_scrub", cts.Token))
                {
                    isPlaying = false;
                    pendingExactResumeTarget = null;
                    RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "end_scrub_seek_failed");
                    return;
                }
                if (TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "end_scrub"))
                {
                    isPlaying = false;
                    return;
                }
                frameDuration = ResolveFrameDuration(decoder);
            }
            if (decoder != null)
            {
                RestoreAudioCallback(decoder, endScrubTarget.Ticks);
                SafeFlushPlayback("end_scrub_resume");
                PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, endScrubTarget, "end_scrub_resume", cts.Token);
                SafeResumePlaybackRendering("end_scrub_resume");
            }
            pacingStopwatch.Restart();
        }
        else
        {
            pendingExactResumeTarget = endScrubTarget;
        }
        SetState(isPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused);
        var endScrubBufDur = _bufferManager.BufferedDuration;
        Logger.Log($"FLASHBACK_ENDSCRUB pos_ms={(long)PlaybackPosition.TotalMilliseconds} bufferDur_ms={(long)endScrubBufDur.TotalMilliseconds} gapFromLive_ms={SaturatingSubtract(endScrubBufDur, PlaybackPosition).TotalMilliseconds:F0} resumePlay={isPlaying}");
        return;
    }
}

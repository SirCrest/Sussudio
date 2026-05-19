using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback-thread play command handler ---

    private void HandlePlayCommand(
        CancellationTokenSource cts,
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref bool isScrubbing,
        ref TimeSpan frozenValidStart,
        ref TimeSpan? pendingExactResumeTarget,
        ref TimeSpan frameDuration,
        Queue<DecodedVideoFrame> prebufferedFrames,
        Stopwatch pacingStopwatch)
    {
        if (isPlaying)
        {
            MarkCommandNoOp(CommandKind.Play, "already_playing");
            return;
        }
        isScrubbing = false;
        isPlaying = true;
        SafeSuppressPreviewSubmission("play");
        SuppressLiveAudio();
        SafePauseRendering("play");
        ResetPlaybackMetrics();
        pacingStopwatch.Restart();

        if (State == FlashbackPlaybackState.Live)
            frozenValidStart = _bufferManager.ValidStartPts;
        decoder ??= CreateDecoder();
        var prevFile = _currentOpenFilePath;
        var pendingPlayTarget = pendingExactResumeTarget ?? SaturatingAdd(PlaybackPosition, frozenValidStart);
        EnsureFileOpen(decoder, ref fileOpen, pendingPlayTarget);
        if (!IsDecoderFileReady(decoder, fileOpen))
        {
            Logger.Log("FLASHBACK_PLAYBACK_PLAY_NO_FILE â€” restoring live");
            SetNoFileFailure(CommandKind.Play, PlaybackPosition);
            isPlaying = false;
            pendingExactResumeTarget = null;
            ReleasePlaybackFrameForLive("play_no_file");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("play_no_file");
            SafeResumeRendering("play_no_file");
            SetState(FlashbackPlaybackState.Live);
            return;
        }
        var requireExactResumeSeek = pendingExactResumeTarget.HasValue;
        var seekTarget = pendingPlayTarget;
        if (State == FlashbackPlaybackState.Paused &&
            IsSamePlaybackPath(prevFile, _currentOpenFilePath) &&
            !requireExactResumeSeek)
        {
            // Resume from Paused â€” decoder is already positioned at the
            // correct frame (set by Pause or scrub). Skip the expensive
            // re-seek which flushes codec state and decodes forward from
            // a keyframe, potentially landing on a different frame.
            Logger.Log($"FLASHBACK_PLAYBACK_RESUME_NO_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        }
        else
        {
            // Playing from Live or file changed â€” full seek required.
            // Audio callback is null during SeekTo so audio packets between
            // keyframe and target are skipped (not decoded). After seek,
            // the audio codec is clean and the next audio packet in the file
            // is at the video target position. No suppression needed.
            decoder.AudioChunkCallback = null;
            if (requireExactResumeSeek)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_RESUME_EXACT_SEEK target_ms={(long)seekTarget.TotalMilliseconds} display_pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
            }
            if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, "play", cts.Token))
            {
                isPlaying = false;
                pendingExactResumeTarget = null;
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "play_seek_failed");
                return;
            }
            if (TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "play"))
            {
                isPlaying = false;
                pendingExactResumeTarget = null;
                return;
            }
        }
        pendingExactResumeTarget = null;
        frameDuration = ResolveFrameDuration(decoder);
        RestoreAudioCallback(decoder, seekTarget.Ticks);
        SafeFlushPlayback("play");
        PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, seekTarget, "play", cts.Token);
        SafeResumePlaybackRendering("play");
        pacingStopwatch.Restart();

        SetState(FlashbackPlaybackState.Playing);
        Logger.Log($"FLASHBACK_PLAYBACK_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        return;
    }
}

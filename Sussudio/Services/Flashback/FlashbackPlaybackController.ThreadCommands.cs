using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback-thread play/pause/go-live/nudge command handlers ---

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
            Logger.Log("FLASHBACK_PLAYBACK_PLAY_NO_FILE — restoring live");
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
            // Resume from Paused — decoder is already positioned at the
            // correct frame (set by Pause or scrub). Skip the expensive
            // re-seek which flushes codec state and decodes forward from
            // a keyframe, potentially landing on a different frame.
            Logger.Log($"FLASHBACK_PLAYBACK_RESUME_NO_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        }
        else
        {
            // Playing from Live or file changed — full seek required.
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

    private void HandlePauseCommand(
        Channel<PlaybackCommand> commandChannel,
        CancellationTokenSource cts,
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref TimeSpan frozenValidStart,
        ref TimeSpan? pendingExactResumeTarget,
        Stopwatch pacingStopwatch)
    {
        if (isPlaying)
        {
            // Pause from Playing state — last decoded frame is already displayed
            // and held via _previousHeldFrame. PlaybackPosition is already set
            // to the last decoded frame's PTS. No seek needed.
            isPlaying = false;
            SafePauseRendering("pause");
            pacingStopwatch.Stop();
            SetState(FlashbackPlaybackState.Paused);
            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        }
        else if (State == FlashbackPlaybackState.Live)
        {
            // Pause from Live state — freeze at current buffer edge
            SafeSuppressPreviewSubmission("pause_from_live");
            SuppressLiveAudio();
            SafePauseRendering("pause_from_live");

            frozenValidStart = _bufferManager.ValidStartPts;
            var pauseTarget = ResolvePauseFromLiveTarget(frozenValidStart);
            var pausePos = ClampPosition(SaturatingSubtract(pauseTarget, frozenValidStart), frozenValidStart);
            if (ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))
            {
                PlaybackPosition = pausePos;
                pendingExactResumeTarget = SaturatingAdd(pausePos, frozenValidStart);
                SetState(FlashbackPlaybackState.Paused);
                Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY pos_ms={(long)pausePos.TotalMilliseconds}");
                return;
            }
            decoder ??= CreateDecoder();
            EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(pausePos, frozenValidStart));
            cts.Token.ThrowIfCancellationRequested();
            if (!IsDecoderFileReady(decoder, fileOpen))
            {
                pendingExactResumeTarget = null;
                SetNoFileFailure(CommandKind.Pause, pausePos);
                ReleasePlaybackFrameForLive("pause_from_live_no_file");
                RestoreLiveAudio();
                SafeResumePreviewSubmission("pause_from_live_no_file");
                SafeResumeRendering("pause_from_live_no_file");
                SetState(FlashbackPlaybackState.Live);
                Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_NO_FILE pos_ms={(long)pausePos.TotalMilliseconds}");
                return;
            }

            if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, pausePos, frozenValidStart, CommandKind.Pause, cts.Token))
            {
                pendingExactResumeTarget = null;
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "pause_from_live_display_failed");
                return;
            }

            pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);

            SetState(FlashbackPlaybackState.Paused);
            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE pos_ms={(long)PlaybackPosition.TotalMilliseconds} target_ms={(long)pauseTarget.TotalMilliseconds} frozen_frame=true");
        }
        return;
    }

    private void HandleGoLiveCommand(
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref bool isScrubbing,
        ref TimeSpan? pendingExactResumeTarget)
    {
        isPlaying = false;
        isScrubbing = false;
        pendingExactResumeTarget = null;
        RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "go_live");
        Logger.Log("FLASHBACK_PLAYBACK_GO_LIVE");
        return;
    }

    private void HandleNudgeCommand(
        PlaybackCommand cmd,
        CancellationTokenSource cts,
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref bool isScrubbing,
        TimeSpan frozenValidStart,
        ref TimeSpan? pendingExactResumeTarget)
    {
        pendingExactResumeTarget = null;
        var nudgedPos = SaturatingAdd(PlaybackPosition, cmd.Delta);
        nudgedPos = ClampPosition(nudgedPos, frozenValidStart);
        decoder ??= CreateDecoder();
        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(decoder, fileOpen))
        {
            SetNoFileFailure(CommandKind.Nudge, nudgedPos);
            PlaybackPosition = nudgedPos;
            isPlaying = false;
            isScrubbing = false;
            ReleasePlaybackFrameForLive("nudge_no_file");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("nudge_no_file");
            SafeResumeRendering("nudge_no_file");
            SetState(FlashbackPlaybackState.Live);
            Logger.Log($"FLASHBACK_PLAYBACK_NUDGE_NO_FILE pos_ms={(long)nudgedPos.TotalMilliseconds}");
            return;
        }

        // F7 fix: forward nudge decodes next frame for frame-accuracy;
        // backward nudge requires full seek (keyframe snap acceptable)
        if (cmd.Delta.Ticks > 0)
        {
            var got = TryDecodeNextVideoFrameWithMetrics(decoder, out var nudgeFrame, cts.Token);
            if (got)
            {
                if (!TrySubmitAndHoldFrame(nudgeFrame, "nudge"))
                {
                    return;
                }
                var actualPos = SaturatingSubtract(nudgeFrame.Pts, frozenValidStart);
                if (actualPos < TimeSpan.Zero) actualPos = TimeSpan.Zero;
                PlaybackPosition = actualPos;
                return;
            }
            // Forward decode failed (EOF) — fall through to full seek
        }
        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token))
        {
            isPlaying = false;
            isScrubbing = false;
            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "nudge_display_failed");
        }
        return;
    }

}

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback-thread pause/go-live/stop/nudge command handlers ---

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
            // Pause from Playing state â€” last decoded frame is already displayed
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
            // Pause from Live state â€” freeze at current buffer edge
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

    private void HandleStopCommand(
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref bool isScrubbing,
        ref TimeSpan? pendingExactResumeTarget)
    {
        isPlaying = false;
        isScrubbing = false;
        pendingExactResumeTarget = null;
        RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "thread_stop");
        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
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
            // Forward decode failed (EOF) â€” fall through to full seek
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

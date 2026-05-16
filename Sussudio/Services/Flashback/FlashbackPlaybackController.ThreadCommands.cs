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
    // --- Playback-thread command handlers ---

    private void HandleSeekCommand(
        ref PlaybackCommand cmd,
        Channel<PlaybackCommand> commandChannel,
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
        cmd = ResolveSeekCommandPosition(cmd);
        while (commandChannel.Reader.TryPeek(out var newerSeek) &&
               newerSeek.Kind == CommandKind.Seek)
        {
            if (!commandChannel.Reader.TryRead(out newerSeek))
            {
                break;
            }

            TrackCommandDequeued(newerSeek);
            newerSeek = ResolveSeekCommandPosition(newerSeek);
            cmd = newerSeek;
        }

        _wasPlayingBeforeScrub = isPlaying || State == FlashbackPlaybackState.Live;
        isPlaying = false;
        isScrubbing = false;
        frozenValidStart = _bufferManager.ValidStartPts;
        SafeSuppressPreviewSubmission("seek");
        SuppressLiveAudio();
        SafePauseRendering("seek");

        cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };
        var seekResumeTarget = SaturatingAdd(cmd.Position, frozenValidStart);
        if (ShouldYieldSeekToQueuedPlay(commandChannel))
        {
            PlaybackPosition = cmd.Position;
            pendingExactResumeTarget = seekResumeTarget;
            MarkCommandNoOp(CommandKind.Seek, "superseded_by_play", cmd.Position);
            SetState(FlashbackPlaybackState.Paused);
            return;
        }
        decoder ??= CreateDecoder();
        EnsureFileOpen(decoder, ref fileOpen, seekResumeTarget);
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(decoder, fileOpen))
        {
            pendingExactResumeTarget = null;
            SetNoFileFailure(CommandKind.Seek, cmd.Position);
            Logger.Log("FLASHBACK_PLAYBACK_SEEK_NO_FILE - restoring live");
            ReleasePlaybackFrameForLive("seek_no_file");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("seek_no_file");
            SafeResumeRendering("seek_no_file");
            SetState(FlashbackPlaybackState.Live);
            return;
        }

        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token))
        {
            isPlaying = false;
            isScrubbing = false;
            pendingExactResumeTarget = null;
            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "seek_display_failed");
            return;
        }
        isPlaying = _wasPlayingBeforeScrub;
        if (isPlaying)
        {
            pendingExactResumeTarget = null;
            ResetPlaybackMetrics();
            pacingStopwatch.Restart();
            var coalescedSeekTarget = seekResumeTarget;
            decoder.AudioChunkCallback = null;
            if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, "seek_resume", cts.Token))
            {
                isPlaying = false;
                pendingExactResumeTarget = null;
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "seek_resume_failed");
                return;
            }
            if (TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "seek_resume"))
            {
                isPlaying = false;
                return;
            }
            frameDuration = ResolveFrameDuration(decoder);
            RestoreAudioCallback(decoder, coalescedSeekTarget.Ticks);
            SafeFlushPlayback("seek_resume");
            PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, coalescedSeekTarget, "seek_resume", cts.Token);
            SafeResumePlaybackRendering("seek_resume");
            pacingStopwatch.Restart();
        }
        else
        {
            pendingExactResumeTarget = seekResumeTarget;
        }
        SetState(isPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused);
        Logger.Log($"FLASHBACK_PLAYBACK_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds} resumePlay={isPlaying}");
        return;
    }

    private void HandleBeginScrubCommand(
        ref PlaybackCommand cmd,
        CancellationTokenSource cts,
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isPlaying,
        ref bool isScrubbing,
        ref TimeSpan frozenValidStart,
        ref TimeSpan? pendingExactResumeTarget)
    {
        pendingExactResumeTarget = null;
        // Only capture the resume-state on first entry into Scrubbing.
        // A second BeginScrub arriving while we're already scrubbing
        // (UI re-press race, MCP automation racing pointer-pressed)
        // would otherwise sample isPlaying=false (set by the prior
        // BeginScrub) and State=Scrubbing, clobbering the original
        // capture and causing EndScrub to land in Paused instead of
        // resuming Playing/Live.
        if (!isScrubbing)
        {
            _wasPlayingBeforeScrub = isPlaying || State == FlashbackPlaybackState.Live;
            frozenValidStart = _bufferManager.ValidStartPts;
        }
        else
        {
            var proposedValidStart = _bufferManager.ValidStartPts;
            Logger.Log($"FLASHBACK_PLAYBACK_BEGIN_SCRUB_DUPLICATE existing_frozen_ms={frozenValidStart.TotalMilliseconds:F0} new_proposed_ms={proposedValidStart.TotalMilliseconds:F0}");
        }
        isPlaying = false;
        isScrubbing = true;
        SafeSuppressPreviewSubmission("begin_scrub");
        SuppressLiveAudio();
        SafePauseRendering("begin_scrub");
        SetState(FlashbackPlaybackState.Scrubbing);

        cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };
        decoder ??= CreateDecoder();
        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(decoder, fileOpen))
        {
            Logger.Log("FLASHBACK_PLAYBACK_SCRUB_NO_FILE — restoring live");
            isScrubbing = false;
            pendingExactResumeTarget = null;
            SetNoFileFailure(CommandKind.BeginScrub, cmd.Position);
            ReleasePlaybackFrameForLive("scrub_no_file");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("scrub_no_file");
            SafeResumeRendering("scrub_no_file");
            SetState(FlashbackPlaybackState.Live);
            return;
        }
        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token))
        {
            isScrubbing = false;
            pendingExactResumeTarget = null;
            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "begin_scrub_display_failed");
        }
        return;
    }

    private void HandleUpdateScrubCommand(
        ref PlaybackCommand cmd,
        Channel<PlaybackCommand> commandChannel,
        CancellationTokenSource cts,
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        ref bool isScrubbing,
        ref TimeSpan? pendingExactResumeTarget,
        TimeSpan frozenValidStart)
    {
        pendingExactResumeTarget = null;
        cmd = ResolveScrubUpdateCommandPosition(cmd);
        if (!isScrubbing)
        {
            MarkCommandNoOp(CommandKind.UpdateScrub, "not_scrubbing", cmd.Position);
            return;
        }
        // Drain stale UpdateScrub commands only. Leave control commands queued
        // so their latency/accounting stays tied to the original command.
        while (commandChannel.Reader.TryPeek(out var newer) &&
               newer.Kind == CommandKind.UpdateScrub)
        {
            if (!commandChannel.Reader.TryRead(out newer))
            {
                break;
            }

            TrackCommandDequeued(newer);
            newer = ResolveScrubUpdateCommandPosition(newer);
            cmd = newer;
        }
        cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };
        if (ShouldYieldScrubUpdateToQueuedControl(commandChannel))
        {
            PlaybackPosition = cmd.Position;
            MarkCommandNoOp(CommandKind.UpdateScrub, "superseded_by_control", cmd.Position);
            return;
        }
        decoder ??= CreateDecoder();
        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(decoder, fileOpen))
        {
            SetNoFileFailure(CommandKind.UpdateScrub, cmd.Position);
            isScrubbing = false;
            pendingExactResumeTarget = null;
            ReleasePlaybackFrameForLive("scrub_update_no_file");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("scrub_update_no_file");
            SafeResumeRendering("scrub_update_no_file");
            SetState(FlashbackPlaybackState.Live);
            Logger.Log($"FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE pos_ms={(long)cmd.Position.TotalMilliseconds}");
            return;
        }
        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token))
        {
            isScrubbing = false;
            pendingExactResumeTarget = null;
            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "scrub_update_display_failed");
        }
        return;
    }

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

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
    // --- Playback-thread seek/scrub command handlers ---

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
            Logger.Log("FLASHBACK_PLAYBACK_SCRUB_NO_FILE â€” restoring live");
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
}

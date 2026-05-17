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
    // --- Playback-thread seek command handler ---

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
}

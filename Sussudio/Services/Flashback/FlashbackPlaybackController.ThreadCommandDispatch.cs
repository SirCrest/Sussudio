using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback-thread command dispatch ---

    private bool ExecutePlaybackCommand(
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
        var commandStarted = Stopwatch.GetTimestamp();
        Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);
        Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);
        ClearPrebufferedFrames(prebufferedFrames, $"command_{cmd.Kind}");
        try
        {
            switch (cmd.Kind)
            {
                case CommandKind.Stop:
                    HandleStopCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);
                    return false;

                case CommandKind.Seek:
                    HandleSeekCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch);
                    break;

                case CommandKind.BeginScrub:
                    HandleBeginScrubCommand(ref cmd, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget);
                    break;

                case CommandKind.UpdateScrub:
                    HandleUpdateScrubCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isScrubbing, ref pendingExactResumeTarget, frozenValidStart);
                    break;

                case CommandKind.EndScrub:
                    HandleEndScrubCommand(cmd, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch);
                    break;

                case CommandKind.Play:
                    HandlePlayCommand(cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch);
                    break;

                case CommandKind.Pause:
                    HandlePauseCommand(commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref frozenValidStart, ref pendingExactResumeTarget, pacingStopwatch);
                    break;

                case CommandKind.GoLive:
                    HandleGoLiveCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);
                    break;

                case CommandKind.Nudge:
                    HandleNudgeCommand(cmd, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, frozenValidStart, ref pendingExactResumeTarget);
                    break;
            }
        }
        finally
        {
            var commandElapsedMs = Stopwatch.GetElapsedTime(commandStarted).TotalMilliseconds;
            Volatile.Write(ref _activeCommandStartedTimestamp, 0);
            Volatile.Write(ref _activeCommandKind, -1);
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_COMPLETE kind={cmd.Kind} duration_ms={commandElapsedMs:0.###}");
        }

        return true;
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
}

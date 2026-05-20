using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback thread ---

    private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)
    {
        FlashbackDecoder? decoder = null;
        var pacingStopwatch = new Stopwatch();
        var frameDuration = TimeSpan.Zero;
        var isPlaying = false;
        var isScrubbing = false;
        var fileOpen = false;
        var frozenValidStart = TimeSpan.Zero; // captured when leaving Live, used for position mapping
        TimeSpan? pendingExactResumeTarget = null;
        var prebufferedFrames = new Queue<DecodedVideoFrame>();

        // Set 1ms timer resolution for accurate Thread.Sleep pacing.
        // Without this, Sleep(8) at 120fps sleeps ~15ms (default granularity) → half-speed.
        timeBeginPeriod(1);
        using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));
        try
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_ENTER");
            while (true)
            {
                PlaybackCommand cmd;
                if (isPlaying)
                {
                    if (!commandChannel.Reader.TryRead(out cmd))
                    {
                        if (cts.IsCancellationRequested)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested");
                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "thread_cancelled");
                            return;
                        }

                        if (decoder is { IsOpen: true })
                        {
                            if (!PaceAndDecodeFrame(decoder, prebufferedFrames, commandChannel, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token))
                            {
                                isPlaying = false;
                            }
                        }
                        continue;
                    }
                    TrackCommandDequeued(cmd);
                }
                else
                {
                    if (!commandChannel.Reader.TryRead(out cmd))
                    {
                        var canRead = commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();
                        if (!canRead)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed");
                            isScrubbing = false;
                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "channel_closed");
                            return;
                        }

                        if (_disposedFlag != 0)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                            isScrubbing = false;
                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "thread_disposed");
                            return;
                        }
                        if (!commandChannel.Reader.TryRead(out cmd))
                        {
                            continue;
                        }
                    }

                    TrackCommandDequeued(cmd);
                }

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
                            return;

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
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_CANCELLED");
            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "thread_cancelled");
        }
        catch (Exception ex)
        {
            SetLastCommandFailure(ex.GetType().Name + ":" + ex.Message);
            Logger.Log($"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'");
            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, "thread_fatal");
        }
        finally
        {
            CompletePlaybackThreadExit(prebufferedFrames, cts, commandChannel);
        }

        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
    }

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private const int CommandQueueCapacity = 256;

    private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);

    private readonly object _playbackThreadSync = new();
    private readonly string _playbackMmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK") ?? "Playback";
    private readonly int _playbackMmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY", 1, -2, 2);

    private Channel<PlaybackCommand> _commandChannel;
    private Thread? _playbackThread;
    private int _playbackThreadStarted;
    private CancellationTokenSource? _playCts;

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uMilliseconds);

    private bool EnsurePlaybackThread(CommandKind commandKind)
    {
        lock (_playbackThreadSync)
        {
            if (_disposedFlag != 0) return RejectCommand(commandKind, "disposed", "disposed", false);
            if (Volatile.Read(ref _playbackThreadStarted) != 0)
            {
                if (_playbackThread is { IsAlive: true })
                {
                    return true;
                }

                Logger.Log("FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
                DrainAbandonedCommandsOnThreadExit(_commandChannel);
                DisposePlaybackCtsBestEffort(_playCts, "recover_stale_thread");
                _playCts = null;
                _playbackThread = null;
                Interlocked.Exchange(ref _pendingCommands, 0);
                ClearQueuedCommandSlotsBarrier();
                Volatile.Write(ref _playbackThreadStarted, 0);
            }

            if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)
                return true;

            // Recreate the command channel because StopPlaybackThread completes it.
            _commandChannel = CreateCommandChannel();
            var commandChannel = _commandChannel;
            _playCts = new CancellationTokenSource();
            var threadCts = _playCts;
            _playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))
            {
                Name = "FlashbackPlayback",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            try
            {
                _playbackThread.Start();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                DisposePlaybackCtsBestEffort(_playCts, "thread_start_fail");
                _playCts = null;
                _playbackThread = null;
                Interlocked.Exchange(ref _playbackThreadStarted, 0);
                return RejectCommand(
                    commandKind,
                    $"thread_start_failed:{ex.GetType().Name}:{ex.Message}",
                    $"thread_start_failed type={ex.GetType().Name}",
                    false);
            }
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_START");
            return true;
        }
    }

    private bool StopPlaybackThread(TimeSpan timeout, string operation)
    {
        lock (_playbackThreadSync)
        {
            var stopStarted = Stopwatch.GetTimestamp();
            var thread = _playbackThread;
            var threadWasAlive = Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true };
            var activeKindAtRequest = FormatActiveCommandKind(Volatile.Read(ref _activeCommandKind));
            var activeElapsedMsAtRequest = GetActiveCommandElapsedMs(stopStarted);
            if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })
            {
                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });
            }

            _commandChannel.Writer.TryComplete();

            try
            {
                _playCts?.Cancel();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }

            var threadExited = true;
            if (thread is { IsAlive: true })
            {
                if (ReferenceEquals(Thread.CurrentThread, thread))
                {
                    Logger.Log("FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self");
                    SetLastCommandFailure("thread_join_skipped:self");
                    threadExited = false;
                }
                else if (!thread.Join(timeout))
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT op={operation} timeout_ms={timeout.TotalMilliseconds:0}");
                    SetLastCommandFailure($"thread_join_timeout:{operation}");
                    threadExited = false;
                }
            }

            var stopElapsedMs = Stopwatch.GetElapsedTime(stopStarted).TotalMilliseconds;
            Logger.Log(
                $"FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE op={operation} duration_ms={stopElapsedMs:0.###} " +
                $"thread_was_alive={threadWasAlive} thread_exited={threadExited} " +
                $"active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###} " +
                $"pending={Volatile.Read(ref _pendingCommands)}");

            if (threadExited)
            {
                ApplyDeferredPreviewAttachAfterStopTimeout();
                DisposePlaybackCtsBestEffort(_playCts, "stop_thread");
                _playCts = null;
                _playbackThread = null;
                Interlocked.Exchange(ref _pendingCommands, 0);
                ClearQueuedCommandSlotsBarrier();
                Volatile.Write(ref _playbackThreadStarted, 0);
            }

            return threadExited;
        }
    }

    private void CompletePlaybackThreadExit(
        Queue<DecodedVideoFrame> prebufferedFrames,
        CancellationTokenSource cts,
        Channel<PlaybackCommand> commandChannel)
    {
        ClearPrebufferedFrames(prebufferedFrames, "thread_exit");
        timeEndPeriod(1);
        CompleteCommandChannelForThreadExit(commandChannel);
        DrainAbandonedCommandsOnThreadExit(commandChannel);
        var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);
        var ownsCts = ReferenceEquals(cts, _playCts);
        if (ownsPlaybackThread)
        {
            _playbackThread = null;
        }
        if (ownsCts)
        {
            _playCts = null;
        }
        DisposePlaybackCtsBestEffort(cts, "thread_exit");
        if (ownsPlaybackThread || ownsCts)
        {
            Volatile.Write(ref _playbackThreadStarted, 0);
        }
        ApplyDeferredPreviewAttachAfterStopTimeout();
    }

    private void DrainAbandonedCommandsOnThreadExit(Channel<PlaybackCommand> commandChannel)
    {
        var abandoned = 0;
        while (commandChannel.Reader.TryRead(out var command))
        {
            DecrementPendingCommands();
            ClearQueuedCommandSlotForDroppedCommand(command);
            if (command.Kind != CommandKind.Stop)
            {
                abandoned++;
            }
        }

        if (abandoned > 0)
        {
            Interlocked.Add(ref _commandsDropped, abandoned);
            if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))
            {
                SetLastCommandFailure($"abandoned_on_exit:{abandoned}");
            }
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_ABANDONED count={abandoned}");
        }

        if (Volatile.Read(ref _pendingCommands) > 0)
        {
            Interlocked.Exchange(ref _pendingCommands, 0);
        }

        ClearQueuedCommandSlotsBarrier();
    }

    private static void CompleteCommandChannelForThreadExit(Channel<PlaybackCommand> commandChannel)
    {
        try
        {
            commandChannel.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private Channel<PlaybackCommand> CreateCommandChannel()
        => Channel.CreateBounded<PlaybackCommand>(
            new BoundedChannelOptions(CommandQueueCapacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });

    private void RestoreLiveForPlaybackThreadExit(
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        string operation)
    {
        CleanupDecoder(ref decoder, ref fileOpen);
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SetState(FlashbackPlaybackState.Live);
    }

    private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}

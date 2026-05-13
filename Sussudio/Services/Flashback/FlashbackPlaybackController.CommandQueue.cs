using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Command coalescing helpers (called from public state transition methods) ---

    private bool SendSeekCommand(TimeSpan position)
    {
        lock (_seekSlotSync)
        {
            if (_queuedSeekSlot is { } queuedSlot)
            {
                _queuedScrubUpdateSlot = null;
                queuedSlot.LatestTicks = position.Ticks;
                TrackCoalescedSeekCommand();
                ClearLastCommandFailure();
                return true;
            }

            var slot = new SeekIntentSlot(position.Ticks);
            _queuedSeekSlot = slot;
            if (!SendCommandCore(new PlaybackCommand { Kind = CommandKind.Seek, Position = position, SeekSlot = slot }))
            {
                ClearQueuedSeekSlotUnsafe(slot);
                return false;
            }

            _queuedScrubUpdateSlot = null;
            return true;
        }
    }

    private bool SendUpdateScrubCommand(TimeSpan position)
    {
        lock (_seekSlotSync)
        {
            Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);
            if (_queuedScrubUpdateSlot is { } queuedSlot)
            {
                _queuedSeekSlot = null;
                queuedSlot.LatestTicks = position.Ticks;
                TrackCoalescedScrubUpdate();
                ClearLastCommandFailure();
                return true;
            }

            var slot = new ScrubUpdateIntentSlot(position.Ticks);
            _queuedScrubUpdateSlot = slot;
            if (!SendCommandCore(new PlaybackCommand { Kind = CommandKind.UpdateScrub, Position = position, ScrubUpdateSlot = slot }))
            {
                ClearQueuedScrubUpdateSlotUnsafe(slot);
                return false;
            }

            _queuedSeekSlot = null;
            return true;
        }
    }

    private bool SendEndScrubCommand(TimeSpan? position)
    {
        lock (_seekSlotSync)
        {
            var commandTicks = position?.Ticks ??
                               _queuedScrubUpdateSlot?.LatestTicks ??
                               Interlocked.Read(ref _latestScrubUpdateTicks);
            var commandPosition = TimeSpan.FromTicks(commandTicks);
            if (position.HasValue)
            {
                Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Value.Ticks);
            }

            if (!SendCommandCore(new PlaybackCommand
            {
                Kind = CommandKind.EndScrub,
                Position = commandPosition,
                HasPositionOverride = position.HasValue
            }))
            {
                return false;
            }

            _queuedSeekSlot = null;
            _queuedScrubUpdateSlot = null;
            return true;
        }
    }

    // --- Command dispatch ---

    private bool SendCommand(PlaybackCommand command)
    {
        lock (_seekSlotSync)
        {
            if (!SendCommandCore(command))
            {
                return false;
            }

            if (command.Kind != CommandKind.Seek)
            {
                _queuedSeekSlot = null;
            }

            if (command.Kind != CommandKind.UpdateScrub)
            {
                _queuedScrubUpdateSlot = null;
            }

            return true;
        }
    }

    private bool SendCommandCore(PlaybackCommand command)
    {
        if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)
        {
            return RejectCommand(command.Kind, "disposed", "disposed", false);
        }

        var queuedCommand = new PlaybackCommand
        {
            Kind = command.Kind,
            Position = command.Position,
            Delta = command.Delta,
            HasPositionOverride = command.HasPositionOverride,
            SeekSlot = command.SeekSlot,
            ScrubUpdateSlot = command.ScrubUpdateSlot,
            QueuedTimestamp = Stopwatch.GetTimestamp()
        };

        var pending = Interlocked.Increment(ref _pendingCommands);
        var droppedOldest = false;
        var droppedCommand = default(PlaybackCommand);
        if (!_commandChannel.Writer.TryWrite(queuedCommand) &&
            (!IsCommandChannelOpenForDropRetry() ||
             !TryDropOldestQueuedCommandForNewCommand(out droppedCommand) ||
             !(droppedOldest = _commandChannel.Writer.TryWrite(queuedCommand))))
        {
            DecrementPendingCommands();
            Interlocked.Increment(ref _commandsDropped);
            var detail = FormatCommandDetail(command);
            SetLastCommandFailure($"write_failed:{command.Kind}{detail}");
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP kind={command.Kind}{detail}");
            return false;
        }

        if (droppedOldest)
        {
            TrackDroppedQueuedCommand(droppedCommand, queuedCommand.Kind);
        }

        Interlocked.Increment(ref _commandsEnqueued);
        UpdateMaxPendingCommands(pending);
        MarkCommandQueued(command.Kind);
        return true;
    }

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

        // Recreate the command channel — the previous one was completed by StopPlaybackThread.
        // A completed channel silently drops all TryWrite calls.
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

    private PlaybackCommand ResolveSeekCommandPosition(PlaybackCommand command)
    {
        var slot = command.SeekSlot;
        if (slot is null)
        {
            return command;
        }

        lock (_seekSlotSync)
        {
            var resolved = command with { Position = TimeSpan.FromTicks(slot.LatestTicks) };
            if (ReferenceEquals(_queuedSeekSlot, slot))
            {
                _queuedSeekSlot = null;
            }

            return resolved;
        }
    }

    private PlaybackCommand ResolveScrubUpdateCommandPosition(PlaybackCommand command)
    {
        var slot = command.ScrubUpdateSlot;
        if (slot is null)
        {
            return command;
        }

        lock (_seekSlotSync)
        {
            var resolved = command with { Position = TimeSpan.FromTicks(slot.LatestTicks) };
            if (ReferenceEquals(_queuedScrubUpdateSlot, slot))
            {
                _queuedScrubUpdateSlot = null;
            }

            return resolved;
        }
    }

    private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)
    {
        if (!commandChannel.Reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)
    {
        if (!commandChannel.Reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)
    {
        if (!commandChannel.Reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    private void ClearQueuedSeekSlotUnsafe(SeekIntentSlot slot)
    {
        if (ReferenceEquals(_queuedSeekSlot, slot))
        {
            _queuedSeekSlot = null;
        }
    }

    private void ClearQueuedScrubUpdateSlotUnsafe(ScrubUpdateIntentSlot slot)
    {
        if (ReferenceEquals(_queuedScrubUpdateSlot, slot))
        {
            _queuedScrubUpdateSlot = null;
        }
    }

    private void ClearQueuedCommandSlotsBarrier()
    {
        lock (_seekSlotSync)
        {
            _queuedSeekSlot = null;
            _queuedScrubUpdateSlot = null;
        }
    }

    private void ClearQueuedCommandSlotForDroppedCommand(PlaybackCommand command)
    {
        lock (_seekSlotSync)
        {
            if (command.SeekSlot != null && ReferenceEquals(_queuedSeekSlot, command.SeekSlot))
            {
                _queuedSeekSlot = null;
            }

            if (command.ScrubUpdateSlot != null && ReferenceEquals(_queuedScrubUpdateSlot, command.ScrubUpdateSlot))
            {
                _queuedScrubUpdateSlot = null;
            }
        }
    }

    private bool IsCommandChannelOpenForDropRetry()
    {
        try
        {
            var canWrite = _commandChannel.Writer.WaitToWriteAsync();
            return !canWrite.IsCompletedSuccessfully || canWrite.Result;
        }
        catch (Exception ex) when (ex is ChannelClosedException or InvalidOperationException)
        {
            return false;
        }
    }

    private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)
    {
        if (!_commandChannel.Reader.TryRead(out droppedCommand))
        {
            return false;
        }

        DecrementPendingCommands();
        return true;
    }

    private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)
    {
        ClearQueuedCommandSlotForDroppedCommand(droppedCommand);

        if (droppedCommand.Kind == CommandKind.Stop)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP_OLD kind=Stop new_kind={newCommandKind} reason=channel_full");
            return;
        }

        Interlocked.Increment(ref _commandsDropped);
        var detail = FormatCommandDetail(droppedCommand);
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{detail} new_kind={newCommandKind} reason=channel_full");
    }

    private Channel<PlaybackCommand> CreateCommandChannel()
        => Channel.CreateBounded<PlaybackCommand>(
            new BoundedChannelOptions(CommandQueueCapacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
}

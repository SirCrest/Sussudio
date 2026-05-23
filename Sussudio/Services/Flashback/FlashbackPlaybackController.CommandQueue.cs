using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private enum CommandKind
    {
        Seek,
        BeginScrub,
        UpdateScrub,
        EndScrub,
        Play,
        Pause,
        GoLive,
        Nudge,
        Stop
    }

    private readonly struct PlaybackCommand
    {
        public CommandKind Kind { get; init; }
        public TimeSpan Position { get; init; }
        public TimeSpan Delta { get; init; }
        public bool HasPositionOverride { get; init; }
        public SeekIntentSlot? SeekSlot { get; init; }
        public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }
        public long QueuedTimestamp { get; init; }
    }

    private sealed class SeekIntentSlot
    {
        public SeekIntentSlot(long ticks)
        {
            LatestTicks = ticks;
        }

        public long LatestTicks;
    }

    private sealed class ScrubUpdateIntentSlot
    {
        public ScrubUpdateIntentSlot(long ticks)
        {
            LatestTicks = ticks;
        }

        public long LatestTicks;
    }

    public long CommandsEnqueued => Interlocked.Read(ref _commandsEnqueued);
    public long CommandsProcessed => Interlocked.Read(ref _commandsProcessed);
    public long CommandsDropped => Interlocked.Read(ref _commandsDropped);
    public long CommandsSkippedNotReady => Interlocked.Read(ref _commandsSkippedNotReady);
    public long ScrubUpdatesCoalesced => Interlocked.Read(ref _scrubUpdatesCoalesced);
    public long SeekCommandsCoalesced => Interlocked.Read(ref _seekCommandsCoalesced);
    public int CommandQueueCapacityCommands => CommandQueueCapacity;
    public int PendingCommands => Volatile.Read(ref _pendingCommands);
    public int MaxPendingCommands => Volatile.Read(ref _maxPendingCommands);
    public long LastCommandQueueLatencyMs => Interlocked.Read(ref _lastCommandQueueLatencyMs);
    public long MaxCommandQueueLatencyMs => Interlocked.Read(ref _maxCommandQueueLatencyMs);
    public string MaxCommandQueueLatencyCommand => Volatile.Read(ref _maxCommandQueueLatencyCommand);
    public long LastCommandQueuedUtcUnixMs => Interlocked.Read(ref _lastCommandQueuedUtcUnixMs);
    public long LastCommandProcessedUtcUnixMs => Interlocked.Read(ref _lastCommandProcessedUtcUnixMs);
    public long LastCommandFailureUtcUnixMs => Interlocked.Read(ref _lastCommandFailureUtcUnixMs);
    public string LastCommandQueued => Volatile.Read(ref _lastCommandQueued);
    public string LastCommandProcessed => Volatile.Read(ref _lastCommandProcessed);
    public string LastCommandFailure => Volatile.Read(ref _lastCommandFailure);
    public bool PlaybackThreadAlive => _playbackThread is { IsAlive: true };

    private long _latestScrubUpdateTicks;
    private long _scrubUpdatesCoalesced;
    private long _seekCommandsCoalesced;
    private long _commandsSkippedNotReady;
    private long _lastCommandFailureUtcUnixMs;
    private readonly object _seekSlotSync = new();
    private SeekIntentSlot? _queuedSeekSlot;
    private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;
    private string _lastCommandFailure = string.Empty;

    private bool IsReady => _initialized && _disposedFlag == 0;

    /// <summary>
    /// Returns true (and logs a skip) when the controller is not ready to accept commands.
    /// </summary>
    private bool IsNotReady(CommandKind kind, TimeSpan? position = null)
    {
        if (IsReady) return false;
        return RejectCommand(
            kind,
            "not_ready",
            $"not_ready initialized={_initialized} disposed={_disposedFlag != 0}",
            true,
            position);
    }

    // --- Public command entry points ---

    public bool BeginScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.BeginScrub, position)) return false;
        if (!EnsurePlaybackThread(CommandKind.BeginScrub)) return false;
        Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);
        return SendCommand(new PlaybackCommand { Kind = CommandKind.BeginScrub, Position = position });
    }

    public bool Seek(TimeSpan position)
    {
        if (IsNotReady(CommandKind.Seek, position)) return false;
        if (!EnsurePlaybackThread(CommandKind.Seek)) return false;
        return SendSeekCommand(position);
    }

    public bool UpdateScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.UpdateScrub, position)) return false;
        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, "thread_not_running", "thread_not_running", false, position);
        return SendUpdateScrubCommand(position);
    }

    public bool EndScrub() => EndScrubAt(null);

    public bool EndScrubAt(TimeSpan position) => EndScrubAt((TimeSpan?)position);

    private bool EndScrubAt(TimeSpan? position)
    {
        if (IsNotReady(CommandKind.EndScrub, position)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.EndScrub, "live_thread_not_running", position);
            return false;
        }
        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.EndScrub, "thread_not_running", "thread_not_running", false, position);
        return SendEndScrubCommand(position);
    }

    public bool Play()
    {
        if (IsNotReady(CommandKind.Play)) return false;
        if (!EnsurePlaybackThread(CommandKind.Play)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Play });
    }

    public bool Pause()
    {
        if (IsNotReady(CommandKind.Pause)) return false;
        if (!EnsurePlaybackThread(CommandKind.Pause)) return false; // Thread must be running to handle Live->Paused
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });
    }

    public bool GoLive()
    {
        if (IsNotReady(CommandKind.GoLive)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.GoLive, "live_thread_not_running");
            return false;
        }
        if (!EnsurePlaybackThread(CommandKind.GoLive)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });
    }

    public bool NudgePosition(TimeSpan delta)
    {
        if (IsNotReady(CommandKind.Nudge)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.Nudge, "live_thread_not_running", delta: delta);
            return false;
        }
        if (!EnsurePlaybackThread(CommandKind.Nudge)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Nudge, Delta = delta });
    }

    // --- Command queue write/drop policy ---

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

    private bool RejectCommand(
        CommandKind kind,
        string failure,
        string reason,
        bool returnValue,
        TimeSpan? position = null)
    {
        Interlocked.Increment(ref _commandsSkippedNotReady);
        var detail = FormatCommandDetail(position: position);
        SetLastCommandFailure($"{failure}:{kind}{detail}");
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}{detail}");
        return returnValue;
    }

    private void SetNoFileFailure(CommandKind kind, TimeSpan position)
    {
        SetLastCommandFailure($"no_file:{kind}{FormatCommandDetail(position: position)}");
    }

    private void SetReopenFailure(string reason, string detail, TimeSpan position)
    {
        SetLastCommandFailure($"reopen_failed:{reason}:{detail}{FormatCommandDetail(position: position)}");
    }

    private void SetSeekDisplayFailure(CommandKind kind, string detail, TimeSpan position)
    {
        SetLastCommandFailure($"seek_display_failed:{kind}:{detail}{FormatCommandDetail(position: position)}");
    }

    private static string FormatCommandDetail(PlaybackCommand command)
        => FormatCommandDetail(command.Position, command.Delta);

    private static string FormatCommandDetail(TimeSpan? position = null, TimeSpan? delta = null)
    {
        if (position.HasValue)
        {
            return $" pos_ms={position.Value.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)}";
        }

        if (delta.HasValue)
        {
            return $" delta_ms={delta.Value.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)}";
        }

        return string.Empty;
    }

    private void SetLastCommandFailure(string failure)
    {
        Volatile.Write(ref _lastCommandFailure, failure);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void ClearLastCommandFailure()
    {
        Volatile.Write(ref _lastCommandFailure, string.Empty);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);
    }

    private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)
    {
        ClearLastCommandFailure();
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
    }
}

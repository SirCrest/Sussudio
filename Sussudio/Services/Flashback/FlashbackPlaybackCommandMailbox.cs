using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Owns one controller's bounded playback-command transport. The controller
/// owns command admission and failure projection; the playback thread owns
/// command execution and all playback state.
/// </summary>
internal sealed class FlashbackPlaybackCommandMailbox
{
    internal const int Capacity = 256;

    internal enum CommandKind
    {
        Seek,
        BeginScrub,
        UpdateScrub,
        EndScrub,
        Play,
        Pause,
        GoLive,
        Nudge,
        Stop,
        Warm
    }

    internal readonly struct Command
    {
        public CommandKind Kind { get; init; }
        public TimeSpan Position { get; init; }
        public TimeSpan Delta { get; init; }
        public bool HasPositionOverride { get; init; }
        public SeekIntentSlot? SeekSlot { get; init; }
        public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }
        public long QueuedTimestamp { get; init; }
    }

    internal sealed class Generation
    {
        internal Generation(Channel<Command> channel)
        {
            Channel = channel;
        }

        internal Channel<Command> Channel { get; }
        internal ChannelReader<Command> Reader => Channel.Reader;
    }

    internal enum EnqueueDisposition
    {
        Rejected,
        Enqueued,
        Coalesced
    }

    internal sealed class SeekIntentSlot
    {
        internal SeekIntentSlot(long ticks)
        {
            LatestTicks = ticks;
        }

        internal long LatestTicks;
    }

    internal sealed class ScrubUpdateIntentSlot
    {
        internal ScrubUpdateIntentSlot(long ticks)
        {
            LatestTicks = ticks;
        }

        internal long LatestTicks;
    }

    private readonly object _slotSync = new();
    private Generation _currentGeneration;
    private long _latestScrubUpdateTicks;
    private SeekIntentSlot? _queuedSeekSlot;
    private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;

    private long _commandsEnqueued;
    private long _commandsProcessed;
    private long _commandsDropped;
    private long _scrubUpdatesCoalesced;
    private long _seekCommandsCoalesced;
    private int _pendingCommands;
    private int _maxPendingCommands;
    private long _lastCommandQueueLatencyMs;
    private long _maxCommandQueueLatencyMs;
    private string _maxCommandQueueLatencyCommand = "None";
    private long _lastCommandQueuedUtcUnixMs;
    private long _lastCommandProcessedUtcUnixMs;
    private string _lastCommandQueued = "None";
    private string _lastCommandProcessed = "None";

    internal FlashbackPlaybackCommandMailbox()
    {
        _currentGeneration = CreateGeneration();
    }

    internal long CommandsEnqueued => Interlocked.Read(ref _commandsEnqueued);
    internal long CommandsProcessed => Interlocked.Read(ref _commandsProcessed);
    internal long CommandsDropped => Interlocked.Read(ref _commandsDropped);
    internal long ScrubUpdatesCoalesced => Interlocked.Read(ref _scrubUpdatesCoalesced);
    internal long SeekCommandsCoalesced => Interlocked.Read(ref _seekCommandsCoalesced);
    internal int PendingCommands => Volatile.Read(ref _pendingCommands);
    internal int MaxPendingCommands => Volatile.Read(ref _maxPendingCommands);
    internal long LastCommandQueueLatencyMs => Interlocked.Read(ref _lastCommandQueueLatencyMs);
    internal long MaxCommandQueueLatencyMs => Interlocked.Read(ref _maxCommandQueueLatencyMs);
    internal string MaxCommandQueueLatencyCommand => Volatile.Read(ref _maxCommandQueueLatencyCommand);
    internal long LastCommandQueuedUtcUnixMs => Interlocked.Read(ref _lastCommandQueuedUtcUnixMs);
    internal long LastCommandProcessedUtcUnixMs => Interlocked.Read(ref _lastCommandProcessedUtcUnixMs);
    internal string LastCommandQueued => Volatile.Read(ref _lastCommandQueued);
    internal string LastCommandProcessed => Volatile.Read(ref _lastCommandProcessed);
    internal Generation CurrentGeneration => Volatile.Read(ref _currentGeneration);

    internal Generation RenewGeneration()
    {
        var generation = CreateGeneration();
        Volatile.Write(ref _currentGeneration, generation);
        return generation;
    }

    internal void SetLatestScrubUpdate(TimeSpan position)
        => Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);

    internal bool TryEnqueue(Command command)
    {
        lock (_slotSync)
        {
            if (!TryEnqueueCore(command))
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

    internal EnqueueDisposition TryEnqueueSeek(TimeSpan position)
    {
        lock (_slotSync)
        {
            if (_queuedSeekSlot is { } queuedSlot)
            {
                _queuedScrubUpdateSlot = null;
                queuedSlot.LatestTicks = position.Ticks;
                TrackCoalescedSeekCommand();
                return EnqueueDisposition.Coalesced;
            }

            var slot = new SeekIntentSlot(position.Ticks);
            _queuedSeekSlot = slot;
            if (!TryEnqueueCore(new Command { Kind = CommandKind.Seek, Position = position, SeekSlot = slot }))
            {
                ClearQueuedSeekSlotUnsafe(slot);
                return EnqueueDisposition.Rejected;
            }

            _queuedScrubUpdateSlot = null;
            return EnqueueDisposition.Enqueued;
        }
    }

    internal EnqueueDisposition TryEnqueueScrubUpdate(TimeSpan position)
    {
        lock (_slotSync)
        {
            SetLatestScrubUpdate(position);
            if (_queuedScrubUpdateSlot is { } queuedSlot)
            {
                _queuedSeekSlot = null;
                queuedSlot.LatestTicks = position.Ticks;
                TrackCoalescedScrubUpdate();
                return EnqueueDisposition.Coalesced;
            }

            var slot = new ScrubUpdateIntentSlot(position.Ticks);
            _queuedScrubUpdateSlot = slot;
            if (!TryEnqueueCore(new Command { Kind = CommandKind.UpdateScrub, Position = position, ScrubUpdateSlot = slot }))
            {
                ClearQueuedScrubUpdateSlotUnsafe(slot);
                return EnqueueDisposition.Rejected;
            }

            _queuedSeekSlot = null;
            return EnqueueDisposition.Enqueued;
        }
    }

    internal bool TryEnqueueEndScrub(TimeSpan? position, out Command command)
    {
        lock (_slotSync)
        {
            var commandTicks = position?.Ticks ??
                               _queuedScrubUpdateSlot?.LatestTicks ??
                               Interlocked.Read(ref _latestScrubUpdateTicks);
            command = new Command
            {
                Kind = CommandKind.EndScrub,
                Position = TimeSpan.FromTicks(commandTicks),
                HasPositionOverride = position.HasValue
            };
            if (position.HasValue)
            {
                SetLatestScrubUpdate(position.Value);
            }

            if (!TryEnqueueCore(command))
            {
                return false;
            }

            _queuedSeekSlot = null;
            _queuedScrubUpdateSlot = null;
            return true;
        }
    }

    internal Command ResolveLatestPosition(Command command)
    {
        lock (_slotSync)
        {
            if (command.SeekSlot is { } seekSlot)
            {
                var resolvedSeek = command with { Position = TimeSpan.FromTicks(seekSlot.LatestTicks) };
                ClearQueuedSeekSlotUnsafe(seekSlot);
                return resolvedSeek;
            }

            if (command.ScrubUpdateSlot is { } scrubSlot)
            {
                var resolvedScrub = command with { Position = TimeSpan.FromTicks(scrubSlot.LatestTicks) };
                ClearQueuedScrubUpdateSlotUnsafe(scrubSlot);
                return resolvedScrub;
            }

            return command;
        }
    }

    internal bool TryReadForPlayback(Generation generation, out Command command)
    {
        if (!generation.Reader.TryRead(out command))
        {
            return false;
        }

        TrackCommandDequeued(command);
        return true;
    }

    internal void TrackCommandDequeued(Command command)
    {
        Interlocked.Increment(ref _commandsProcessed);
        DecrementPendingCommands();
        TrackCommandQueueLatency(command);
        Interlocked.Exchange(ref _lastCommandProcessedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandProcessed, command.Kind.ToString());
    }

    internal bool ShouldYieldScrubUpdateToQueuedControl(ChannelReader<Command> reader)
    {
        if (!reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    internal bool ShouldYieldSeekToQueuedPlay(ChannelReader<Command> reader)
    {
        if (!reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    internal bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(ChannelReader<Command> reader)
    {
        if (!reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    internal void Complete(Generation generation)
    {
        try
        {
            generation.Channel.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    internal int DrainAbandoned(Generation generation)
    {
        var abandoned = 0;
        while (generation.Reader.TryRead(out var command))
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
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_ABANDONED count={abandoned}");
        }

        ResetPendingAndClearSlots();
        return abandoned;
    }

    internal void ResetPendingAndClearSlots()
    {
        if (Volatile.Read(ref _pendingCommands) > 0)
        {
            Interlocked.Exchange(ref _pendingCommands, 0);
        }

        lock (_slotSync)
        {
            _queuedSeekSlot = null;
            _queuedScrubUpdateSlot = null;
        }
    }

    private static Generation CreateGeneration()
        => new(Channel.CreateBounded<Command>(
            new BoundedChannelOptions(Capacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            }));

    private bool TryEnqueueCore(Command command)
    {
        var queuedCommand = command with { QueuedTimestamp = Stopwatch.GetTimestamp() };
        var pending = Interlocked.Increment(ref _pendingCommands);
        var generation = CurrentGeneration;
        var written = generation.Channel.Writer.TryWrite(queuedCommand);
        if (!written && IsOpenForDropRetry(generation) && TryDropOldest(generation, out var droppedCommand))
        {
            // Removal is final even if the writer completes before replacement.
            TrackDroppedQueuedCommand(droppedCommand, queuedCommand.Kind);
            written = generation.Channel.Writer.TryWrite(queuedCommand);
        }

        if (!written)
        {
            DecrementPendingCommands();
            Interlocked.Increment(ref _commandsDropped);
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP kind={command.Kind}{FormatCommandDetail(command)}");
            return false;
        }

        Interlocked.Increment(ref _commandsEnqueued);
        AtomicMax.Update(ref _maxPendingCommands, pending);
        Interlocked.Exchange(ref _lastCommandQueuedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandQueued, command.Kind.ToString());
        return true;
    }

    private static bool IsOpenForDropRetry(Generation generation)
    {
        try
        {
            var canWrite = generation.Channel.Writer.WaitToWriteAsync();
            return !canWrite.IsCompletedSuccessfully || canWrite.Result;
        }
        catch (Exception ex) when (ex is ChannelClosedException or InvalidOperationException)
        {
            return false;
        }
    }

    private bool TryDropOldest(Generation generation, out Command droppedCommand)
    {
        if (!generation.Reader.TryRead(out droppedCommand))
        {
            return false;
        }

        DecrementPendingCommands();
        return true;
    }

    private void TrackDroppedQueuedCommand(Command droppedCommand, CommandKind newCommandKind)
    {
        ClearQueuedCommandSlotForDroppedCommand(droppedCommand);

        if (droppedCommand.Kind == CommandKind.Stop)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP_OLD kind=Stop new_kind={newCommandKind} reason=channel_full");
            return;
        }

        Interlocked.Increment(ref _commandsDropped);
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{FormatCommandDetail(droppedCommand)} new_kind={newCommandKind} reason=channel_full");
    }

    private void TrackCoalescedScrubUpdate()
    {
        var dropped = Interlocked.Increment(ref _commandsDropped);
        var coalesced = Interlocked.Increment(ref _scrubUpdatesCoalesced);
        if (coalesced == 1 || coalesced % 120 == 0)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SCRUB_COALESCED count={coalesced} dropped={dropped}");
        }
    }

    private void TrackCoalescedSeekCommand()
    {
        var coalesced = Interlocked.Increment(ref _seekCommandsCoalesced);
        if (coalesced == 1 || coalesced % 120 == 0)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_COALESCED count={coalesced}");
        }
    }

    private void TrackCommandQueueLatency(Command command)
    {
        if (command.QueuedTimestamp <= 0)
        {
            return;
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - command.QueuedTimestamp;
        var latencyMs = Math.Max(0, (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency));
        Interlocked.Exchange(ref _lastCommandQueueLatencyMs, latencyMs);
        while (true)
        {
            var current = Interlocked.Read(ref _maxCommandQueueLatencyMs);
            if (latencyMs <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _maxCommandQueueLatencyMs, latencyMs, current) == current)
            {
                Volatile.Write(ref _maxCommandQueueLatencyCommand, command.Kind.ToString());
                return;
            }
        }
    }

    private void DecrementPendingCommands()
        => AtomicCounter.TryDecrement(ref _pendingCommands);

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

    private void ClearQueuedCommandSlotForDroppedCommand(Command command)
    {
        lock (_slotSync)
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

    internal static string FormatCommandDetail(Command command)
        => command.Kind switch
        {
            CommandKind.Nudge => FormatCommandDetail(delta: command.Delta),
            CommandKind.Seek or CommandKind.BeginScrub or CommandKind.UpdateScrub or CommandKind.EndScrub
                => FormatCommandDetail(position: command.Position),
            _ => string.Empty
        };

    // Also used by FlashbackPlaybackController, which logs the same command
    // suffix from the playback thread; the mailbox owns the command shape.
    internal static string FormatCommandDetail(TimeSpan? position = null, TimeSpan? delta = null)
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
}

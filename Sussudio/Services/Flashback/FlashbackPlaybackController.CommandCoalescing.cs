using System;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Command coalescing and slot resolution ---

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
}

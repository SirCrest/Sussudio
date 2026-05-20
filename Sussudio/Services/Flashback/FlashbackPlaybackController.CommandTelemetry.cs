using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private long _commandsEnqueued;
    private long _commandsProcessed;
    private long _commandsDropped;
    private long _commandsSkippedNotReady;
    private int _pendingCommands;
    private int _maxPendingCommands;
    private long _lastCommandQueueLatencyMs;
    private long _maxCommandQueueLatencyMs;
    private string _maxCommandQueueLatencyCommand = "None";
    private long _lastCommandQueuedUtcUnixMs;
    private long _lastCommandProcessedUtcUnixMs;
    private long _lastCommandFailureUtcUnixMs;
    private string _lastCommandQueued = "None";
    private string _lastCommandProcessed = "None";
    private string _lastCommandFailure = string.Empty;
    private int _activeCommandKind = -1;
    private long _activeCommandStartedTimestamp;

    // --- Command readiness guards ---

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

    // --- Command telemetry ---

    private void SetLastCommandFailure(string failure)
    {
        Volatile.Write(ref _lastCommandFailure, failure);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void MarkCommandQueued(CommandKind kind)
    {
        Interlocked.Exchange(ref _lastCommandQueuedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandQueued, kind.ToString());
        ClearLastCommandFailure();
    }

    private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)
    {
        ClearLastCommandFailure();
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
    }

    private void SetLastSubmitFailure(string failure)
    {
        Volatile.Write(ref _lastSubmitFailure, failure);
        Interlocked.Exchange(ref _lastSubmitFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void ClearLastSubmitFailure()
    {
        Volatile.Write(ref _lastSubmitFailure, string.Empty);
        Interlocked.Exchange(ref _lastSubmitFailureUtcUnixMs, 0);
    }

    private void RecordPlaybackDroppedFrame(string reason)
    {
        Interlocked.Increment(ref _playbackDroppedFrames);
        Volatile.Write(ref _lastPlaybackDropReason, reason);
        Interlocked.Exchange(ref _lastPlaybackDropUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void ClearLastCommandFailure()
    {
        Volatile.Write(ref _lastCommandFailure, string.Empty);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);
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

    private void TrackCommandDequeued(PlaybackCommand command)
    {
        Interlocked.Increment(ref _commandsProcessed);
        DecrementPendingCommands();
        TrackCommandQueueLatency(command);
        Interlocked.Exchange(ref _lastCommandProcessedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandProcessed, command.Kind.ToString());
    }

    private void TrackCommandQueueLatency(PlaybackCommand command)
    {
        if (command.QueuedTimestamp <= 0)
        {
            return;
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - command.QueuedTimestamp;
        var latencyMs = Math.Max(0, (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency));
        Interlocked.Exchange(ref _lastCommandQueueLatencyMs, latencyMs);
        UpdateMaxCommandQueueLatency(command.Kind, latencyMs);
    }

    private void UpdateMaxCommandQueueLatency(CommandKind commandKind, long latencyMs)
    {
        while (true)
        {
            var current = Interlocked.Read(ref _maxCommandQueueLatencyMs);
            if (latencyMs <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _maxCommandQueueLatencyMs, latencyMs, current) == current)
            {
                Volatile.Write(ref _maxCommandQueueLatencyCommand, commandKind.ToString());
                return;
            }
        }
    }

    private void UpdateMaxPendingCommands(int value)
        => AtomicMax.Update(ref _maxPendingCommands, value);

    private void DecrementPendingCommands()
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingCommands);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _pendingCommands, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private static string FormatActiveCommandKind(int rawKind)
    {
        if (rawKind < 0) return "None";
        return Enum.IsDefined(typeof(CommandKind), rawKind)
            ? ((CommandKind)rawKind).ToString()
            : rawKind.ToString(CultureInfo.InvariantCulture);
    }

    private double GetActiveCommandElapsedMs(long nowTimestamp)
    {
        var started = Volatile.Read(ref _activeCommandStartedTimestamp);
        if (started <= 0) return 0;
        return Stopwatch.GetElapsedTime(started, nowTimestamp).TotalMilliseconds;
    }
}

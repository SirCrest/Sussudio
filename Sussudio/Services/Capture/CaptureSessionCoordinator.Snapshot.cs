using System;
using System.Threading;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    public CaptureSessionSnapshot Snapshot
    {
        get
        {
            lock (_snapshotLock)
            {
                var oldestPendingCommandAgeMs = _pendingCommandEnqueuedAtUtc.Count > 0
                    ? Math.Max(0L, (long)(DateTimeOffset.UtcNow - _pendingCommandEnqueuedAtUtc.Peek()).TotalMilliseconds)
                    : 0L;
                return new CaptureSessionSnapshot
                {
                    LastTransitionUtc = _lastTransitionUtc,
                    LastCommand = _lastCommand,
                    LastCorrelationId = _lastCorrelationId,
                    LastError = _lastError,
                    CommandsEnqueued = Volatile.Read(ref _commandsEnqueued),
                    CommandsCompleted = Volatile.Read(ref _commandsCompleted),
                    CommandsFailed = Volatile.Read(ref _commandsFailed),
                    CommandsCanceled = Volatile.Read(ref _commandsCanceled),
                    CommandsCoalesced = Volatile.Read(ref _commandsCoalesced),
                    PendingCommands = Volatile.Read(ref _pendingCommands),
                    MaxPendingCommands = Volatile.Read(ref _maxPendingCommands),
                    OldestPendingCommandAgeMs = oldestPendingCommandAgeMs,
                    LastCommandQueueLatencyMs = Volatile.Read(ref _lastCommandQueueLatencyMs),
                    MaxCommandQueueLatencyMs = Volatile.Read(ref _maxCommandQueueLatencyMs),
                    LastOutcome = _lastOutcome,
                    SessionState = _captureService.SessionState,
                    IsRecording = _captureService.IsRecording,
                    IsInitialized = _captureService.IsInitialized,
                    IsVideoPreviewActive = _captureService.IsVideoPreviewActive,
                    IsAudioPreviewActive = _captureService.IsAudioPreviewActive
                };
            }
        }
    }

    private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)
    {
        lock (_snapshotLock)
        {
            _lastTransitionUtc = DateTimeOffset.UtcNow;
            _lastCommand = command.Kind;
            _lastCorrelationId = command.CorrelationId;
            _lastOutcome = outcome;
            _lastError = error;
        }
    }

    private void TrackPendingCommandEnqueued(DateTimeOffset enqueuedAtUtc)
    {
        lock (_snapshotLock)
        {
            _pendingCommandEnqueuedAtUtc.Enqueue(enqueuedAtUtc);
        }
    }

    private void RemoveOldestPendingCommand()
    {
        lock (_snapshotLock)
        {
            if (_pendingCommandEnqueuedAtUtc.Count > 0)
            {
                _pendingCommandEnqueuedAtUtc.Dequeue();
            }
        }
    }

    private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)
    {
        var latencyMs = Math.Max(0L, (long)(DateTimeOffset.UtcNow - enqueuedAtUtc).TotalMilliseconds);
        Volatile.Write(ref _lastCommandQueueLatencyMs, latencyMs);
        AtomicMax.Update(ref _maxCommandQueueLatencyMs, latencyMs);
    }
}

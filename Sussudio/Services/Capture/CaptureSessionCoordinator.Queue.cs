using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    private sealed class CoordinatorWorkItem
    {
        public required CaptureCommand Command { get; init; }
        public required Func<CancellationToken, Task> Operation { get; init; }
        public required CancellationToken CancellationToken { get; init; }
        public required TaskCompletionSource<object?> Completion { get; init; }
        public required CancellationTokenRegistration CancellationRegistration { get; init; }
        public bool PropagateCancellationToOperation { get; init; }
        public int? CoalescingGeneration { get; init; }
    }

    private Task EnqueueAsync(
        CaptureCommandKind kind,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken,
        bool coalesceLatest = false,
        bool propagateCancellationToOperation = false)
    {
        ThrowIfDisposed();
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var coalescingGeneration = coalesceLatest
            ? Interlocked.Increment(ref _latestFlashbackEncoderCycleGeneration)
            : (int?)null;
        var correlationId = $"{kind}-{Guid.NewGuid():N}";
        var command = new CaptureCommand(kind, correlationId, DateTimeOffset.UtcNow);
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled(cancellationToken);
            });
        }

        var workItem = new CoordinatorWorkItem
        {
            Command = command,
            Operation = operation,
            CancellationToken = cancellationToken,
            Completion = completion,
            CancellationRegistration = cancellationRegistration,
            PropagateCancellationToOperation = propagateCancellationToOperation,
            CoalescingGeneration = coalescingGeneration
        };

        var pending = Interlocked.Increment(ref _pendingCommands);
        Interlocked.Increment(ref _commandsEnqueued);
        AtomicMax.Update(ref _maxPendingCommands, pending);
        TrackPendingCommandEnqueued(command.EnqueuedAtUtc);
        if (!_queue.Writer.TryWrite(workItem))
        {
            RemoveOldestPendingCommand();
            DisposeCancellationRegistrationBestEffort(cancellationRegistration, "enqueue_failed");
            DecrementPendingCommands("enqueue_failed");
            Interlocked.Increment(ref _commandsFailed);
            Logger.LogEvent("CAP-COORD-ENQUEUE-FAIL", $"{kind} corr={correlationId}");
            if (Volatile.Read(ref _isDisposed))
            {
                throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));
            }
            throw new InvalidOperationException("Failed to enqueue capture command.");
        }

        return completion.Task;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed))
        {
            throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));
        }
    }

}

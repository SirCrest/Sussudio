using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var workItem in _queue.Reader.ReadAllAsync(_workerCancellation.Token))
            {
                var sw = Stopwatch.StartNew();
                Logger.LogEvent("CAP-COORD-START", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId}");
                RecordCommandQueueLatency(workItem.Command.EnqueuedAtUtc);

                try
                {
                    DisposeCancellationRegistrationBestEffort(workItem.CancellationRegistration, "begin_process");

                    if (workItem.CoalescingGeneration is int generation &&
                        generation != Volatile.Read(ref _latestFlashbackEncoderCycleGeneration))
                    {
                        workItem.Completion.TrySetResult(null);
                        Interlocked.Increment(ref _commandsCompleted);
                        Interlocked.Increment(ref _commandsCoalesced);
                        UpdateSnapshot(workItem.Command, CaptureCommandOutcome.Coalesced, null);
                        Logger.LogEvent("CAP-COORD-SKIP", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} stale_generation={generation}");
                        continue;
                    }

                    if (workItem.CancellationToken.IsCancellationRequested)
                    {
                        workItem.Completion.TrySetCanceled(workItem.CancellationToken);
                        Interlocked.Increment(ref _commandsCanceled);
                        UpdateSnapshot(workItem.Command, CaptureCommandOutcome.Canceled, "Canceled before execution");
                        continue;
                    }

                    using var linkedCancellation = workItem.PropagateCancellationToOperation
                        ? CancellationTokenSource.CreateLinkedTokenSource(
                            workItem.CancellationToken,
                            _workerCancellation.Token)
                        : CancellationTokenSource.CreateLinkedTokenSource(_workerCancellation.Token);
                    var operationToken = linkedCancellation.Token;

                    if (operationToken.IsCancellationRequested)
                    {
                        workItem.Completion.TrySetCanceled(operationToken);
                        Interlocked.Increment(ref _commandsCanceled);
                        UpdateSnapshot(workItem.Command, CaptureCommandOutcome.Canceled, "Canceled before execution");
                        continue;
                    }

                    await workItem.Operation(operationToken);
                    workItem.Completion.TrySetResult(null);
                    Interlocked.Increment(ref _commandsCompleted);
                    UpdateSnapshot(workItem.Command, CaptureCommandOutcome.Completed, null);
                    Logger.LogEvent("CAP-COORD-DONE", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                catch (OperationCanceledException oce) when (
                    _workerCancellation.IsCancellationRequested ||
                    (workItem.PropagateCancellationToOperation && workItem.CancellationToken.IsCancellationRequested))
                {
                    var cancelToken = workItem.PropagateCancellationToOperation && workItem.CancellationToken.IsCancellationRequested
                        ? workItem.CancellationToken
                        : _workerCancellation.Token;
                    workItem.Completion.TrySetCanceled(cancelToken);
                    Interlocked.Increment(ref _commandsCanceled);
                    UpdateSnapshot(workItem.Command, CaptureCommandOutcome.Canceled, oce.Message);
                    Logger.LogEvent("CAP-COORD-CANCEL", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    workItem.Completion.TrySetException(ex);
                    Interlocked.Increment(ref _commandsFailed);
                    UpdateSnapshot(workItem.Command, CaptureCommandOutcome.Failed, ex.Message);
                    Logger.LogException(ex);
                    Logger.LogEvent("CAP-COORD-FAIL", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                finally
                {
                    sw.Stop();
                    RemoveOldestPendingCommand();
                    DecrementPendingCommands("process_complete");
                }
            }
        }
        catch (OperationCanceledException)
        {
            /* Expected during disposal - worker loop cancelled via CancellationToken */
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
        finally
        {
            Exception failure = Volatile.Read(ref _isDisposed)
                ? new ObjectDisposedException(nameof(CaptureSessionCoordinator))
                : new OperationCanceledException("Capture session coordinator stopped.");
            FailPendingCommands(failure);
        }
    }

    private void FailPendingCommands(Exception ex)
    {
        while (_queue.Reader.TryRead(out var pending))
        {
            RemoveOldestPendingCommand();
            DisposeCancellationRegistrationBestEffort(pending.CancellationRegistration, "fail_pending");
            if (pending.Completion.Task.IsCanceled)
            {
                Interlocked.Increment(ref _commandsCanceled);
                UpdateSnapshot(pending.Command, CaptureCommandOutcome.Canceled, "Canceled before execution");
            }
            else if (pending.Completion.TrySetException(ex))
            {
                Interlocked.Increment(ref _commandsFailed);
                UpdateSnapshot(pending.Command, CaptureCommandOutcome.Failed, ex.Message);
            }
            else if (pending.Completion.Task.IsCanceled)
            {
                Interlocked.Increment(ref _commandsCanceled);
                UpdateSnapshot(pending.Command, CaptureCommandOutcome.Canceled, "Canceled before execution");
            }
            else if (pending.Completion.Task.IsFaulted)
            {
                Interlocked.Increment(ref _commandsFailed);
                UpdateSnapshot(pending.Command, CaptureCommandOutcome.Failed, ex.Message);
            }

            DecrementPendingCommands("fail_pending");
        }
    }

    private void DecrementPendingCommands(string operation)
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingCommands);
            if (current <= 0)
            {
                Logger.Log($"CAPTURE_COORD_PENDING_UNDERFLOW op={operation}");
                return;
            }

            if (Interlocked.CompareExchange(ref _pendingCommands, current - 1, current) == current)
            {
                return;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Serializes all capture lifecycle mutations onto one worker. Public callers
// may enqueue from UI, automation, and background diagnostics, but CaptureService
// itself should only see one transition at a time.
public sealed partial class CaptureSessionCoordinator : IDisposable, IAsyncDisposable
{
    private readonly CaptureService _captureService;
    private readonly Channel<CoordinatorWorkItem> _queue;
    private readonly CancellationTokenSource _workerCancellation = new();
    private readonly Task _workerTask;
    private readonly object _snapshotLock = new();
    private readonly object _disposeLock = new();
    private bool _isDisposed;
    private int _pendingCommands;
    private int _latestFlashbackEncoderCycleGeneration;
    private int _maxPendingCommands;
    private long _commandsEnqueued;
    private long _commandsCompleted;
    private long _commandsFailed;
    private long _commandsCanceled;
    private long _commandsCoalesced;
    private long _lastCommandQueueLatencyMs;
    private long _maxCommandQueueLatencyMs;
    private long _lastFlashbackCommandRejectionUtcUnixMs;
    private string _lastFlashbackCommandRejection = string.Empty;
    private readonly Queue<DateTimeOffset> _pendingCommandEnqueuedAtUtc = new();
    private DateTimeOffset _lastTransitionUtc = DateTimeOffset.UtcNow;
    private CaptureCommandKind? _lastCommand;
    private string? _lastCorrelationId;
    private string? _lastError;
    private CaptureCommandOutcome _lastOutcome = CaptureCommandOutcome.None;
    private const int DefaultDisposeDrainTimeoutMs = 15_000;
    private const int DefaultDisposeCancelTimeoutMs = 1_000;

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

    public CaptureSessionCoordinator(CaptureService captureService)
    {
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _queue = Channel.CreateUnbounded<CoordinatorWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _workerTask = Task.Run(ProcessQueueAsync);
    }

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

    public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Initialize, ct => _captureService.InitializeAsync(device, settings, ct), cancellationToken);

    public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartVideoPreview, ct => _captureService.StartVideoPreviewAsync(settings, ct), cancellationToken);

    public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopVideoPreview, ct => _captureService.StopVideoPreviewAsync(ct), cancellationToken);

    public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopVideoPreview, ct => _captureService.StopVideoPreviewWithTeardownAsync(ct), cancellationToken);

    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartRecording, ct => _captureService.StartRecordingAsync(settings, ct), cancellationToken);

    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopRecording, ct => _captureService.StopRecordingAsync(ct), cancellationToken);

    // Used exclusively by MainViewModel.StopRecordingForEmergencyAsync → routes through the
    // same coordinator queue but signals CaptureService to use EmergencyStopTimeoutMs (5s)
    // instead of StopTimeoutMs (30s) so the emergency stop fits inside App.xaml.cs's 8s wrapper.
    public Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopRecording, ct => _captureService.StopRecordingAsync(emergency: true, ct), cancellationToken);

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartAudioPreview, ct => _captureService.StartAudioPreviewAsync(ct), cancellationToken);

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopAudioPreview, ct => _captureService.StopAudioPreviewAsync(ct), cancellationToken);

    public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopAudioPreview, ct => _captureService.StopAudioPreviewWithTeardownAsync(ct), cancellationToken);

    public Task UpdateAudioMonitoringAsync(bool enabled, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateAudioMonitoring,
            async ct =>
            {
                if (enabled)
                {
                    await _captureService.StartAudioPreviewAsync(ct).ConfigureAwait(false);
                    _captureService.SetMonitoringMuted(false);
                }
                else
                {
                    _captureService.SetMonitoringMuted(true);
                    await _captureService.StopAudioPreviewAsync(ct).ConfigureAwait(false);
                }
            },
            cancellationToken);

    internal void SetPreviewVolume(double volume)
    {
        ThrowIfDisposed();
        _captureService.SetPreviewVolume((float)volume);
    }

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateAudioInput,
            ct => _captureService.UpdateAudioInputAsync(audioDeviceId, audioDeviceName, ct),
            cancellationToken);

    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? micDeviceId, string? micDeviceName, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateMicrophoneMonitor,
            ct => _captureService.UpdateMicrophoneMonitorAsync(enabled, micDeviceId, micDeviceName, ct),
            cancellationToken);

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Cleanup, ct => _captureService.CleanupAsync(ct), cancellationToken);

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
            /* Expected during disposal — worker loop cancelled via CancellationToken */
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

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed))
        {
            throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));
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

    // REVIEWED 2026-04-07: IDisposable fallback only — MainViewModel.DisposeAsync
    // calls DisposeAsync directly. This sync path is never hit in production.
    public void Dispose()
    {
        if (!TryBeginDispose()) return;
        Task.Run(() => CoreDisposeAsync().AsTask()).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (!TryBeginDispose()) return;
        await CoreDisposeAsync().ConfigureAwait(false);
    }

    private bool TryBeginDispose()
    {
        lock (_disposeLock)
        {
            if (Volatile.Read(ref _isDisposed)) return false;
            Volatile.Write(ref _isDisposed, true);
        }
        return true;
    }

    private async ValueTask CoreDisposeAsync()
    {
        _queue.Writer.TryComplete();
        var drainTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS",
            DefaultDisposeDrainTimeoutMs,
            1000,
            300000);

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromMilliseconds(drainTimeoutMs)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.Log($"CaptureSessionCoordinator dispose drain timed out after {drainTimeoutMs} ms; canceling worker.");
            CancelWorkerBestEffort();
            await WaitForWorkerCancellationAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* Expected during disposal — worker task was cancelled */
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
        finally
        {
            DisposeWorkerCancellationWhenSafe();
        }
    }

    private async Task WaitForWorkerCancellationAsync()
    {
        var cancelTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_COORDINATOR_DISPOSE_CANCEL_TIMEOUT_MS",
            DefaultDisposeCancelTimeoutMs,
            100,
            300000);

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromMilliseconds(cancelTimeoutMs)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.Log($"CaptureSessionCoordinator worker cancellation timed out after {cancelTimeoutMs} ms.");
        }
        catch (OperationCanceledException)
        {
            /* Expected during disposal - worker task was cancelled */
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private void DisposeWorkerCancellationWhenSafe()
    {
        if (_workerTask.IsCompleted)
        {
            DisposeWorkerCancellationBestEffort("worker_completed");
            return;
        }

        _ = _workerTask.ContinueWith(
            static (_, state) =>
            {
                var cancellation = (CancellationTokenSource)state!;
                try
                {
                    cancellation.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Log($"CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN op=worker_continuation type={ex.GetType().Name} msg='{ex.Message}'");
                }
            },
            _workerCancellation,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void DisposeCancellationRegistrationBestEffort(
        CancellationTokenRegistration registration,
        string operation)
    {
        try
        {
            registration.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_COORD_CANCEL_REG_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CancelWorkerBestEffort()
    {
        try
        {
            _workerCancellation.Cancel();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_COORD_WORKER_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void DisposeWorkerCancellationBestEffort(string operation)
    {
        try
        {
            _workerCancellation.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}

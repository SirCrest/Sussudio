using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public enum CaptureCommandKind
{
    Initialize,
    StartRecording,
    StopRecording,
    StartAudioPreview,
    StopAudioPreview,
    UpdateAudioInput,
    Cleanup
}

public readonly record struct CaptureCommand(
    CaptureCommandKind Kind,
    string CorrelationId,
    DateTimeOffset EnqueuedAtUtc);

public sealed class CaptureSessionSnapshot
{
    public DateTimeOffset LastTransitionUtc { get; init; }
    public CaptureCommandKind? LastCommand { get; init; }
    public string? LastCorrelationId { get; init; }
    public string? LastError { get; init; }
    public int PendingCommands { get; init; }
    public CaptureSessionState SessionState { get; init; }
    public bool IsRecording { get; init; }
    public bool IsInitialized { get; init; }
    public bool IsAudioPreviewActive { get; init; }
}

public interface ICaptureSessionCoordinator : IDisposable, IAsyncDisposable
{
    CaptureSessionSnapshot Snapshot { get; }

    Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default);
    Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default);
    Task StopRecordingAsync(CancellationToken cancellationToken = default);
    Task StartAudioPreviewAsync(CancellationToken cancellationToken = default);
    Task StopAudioPreviewAsync(CancellationToken cancellationToken = default);
    Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default);
    Task CleanupAsync(CancellationToken cancellationToken = default);
}

public sealed class CaptureSessionCoordinator : ICaptureSessionCoordinator
{
    private readonly CaptureService _captureService;
    private readonly Channel<CoordinatorWorkItem> _queue;
    private readonly CancellationTokenSource _workerCancellation = new();
    private readonly Task _workerTask;
    private readonly object _snapshotLock = new();
    private bool _isDisposed;
    private int _pendingCommands;
    private DateTimeOffset _lastTransitionUtc = DateTimeOffset.UtcNow;
    private CaptureCommandKind? _lastCommand;
    private string? _lastCorrelationId;
    private string? _lastError;
    private const int DefaultDisposeDrainTimeoutMs = 15_000;

    private sealed class CoordinatorWorkItem
    {
        public required CaptureCommand Command { get; init; }
        public required Func<CancellationToken, Task> Operation { get; init; }
        public required CancellationToken CancellationToken { get; init; }
        public required TaskCompletionSource<object?> Completion { get; init; }
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
                return new CaptureSessionSnapshot
                {
                    LastTransitionUtc = _lastTransitionUtc,
                    LastCommand = _lastCommand,
                    LastCorrelationId = _lastCorrelationId,
                    LastError = _lastError,
                    PendingCommands = Volatile.Read(ref _pendingCommands),
                    SessionState = _captureService.SessionState,
                    IsRecording = _captureService.IsRecording,
                    IsInitialized = _captureService.IsInitialized,
                    IsAudioPreviewActive = _captureService.IsAudioPreviewActive
                };
            }
        }
    }

    public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Initialize, _ => _captureService.InitializeAsync(device, settings), cancellationToken);

    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartRecording, _ => _captureService.StartRecordingAsync(settings), cancellationToken);

    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopRecording, _ => _captureService.StopRecordingAsync(), cancellationToken);

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartAudioPreview, _ => _captureService.StartAudioPreviewAsync(), cancellationToken);

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopAudioPreview, _ => _captureService.StopAudioPreviewAsync(), cancellationToken);

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateAudioInput,
            _ => _captureService.UpdateAudioInputAsync(audioDeviceId, audioDeviceName),
            cancellationToken);

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Cleanup, _ => _captureService.CleanupAsync(), cancellationToken);

    private Task EnqueueAsync(
        CaptureCommandKind kind,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var correlationId = $"{kind}-{Guid.NewGuid():N}";
        var command = new CaptureCommand(kind, correlationId, DateTimeOffset.UtcNow);
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workItem = new CoordinatorWorkItem
        {
            Command = command,
            Operation = operation,
            CancellationToken = cancellationToken,
            Completion = completion
        };

        Interlocked.Increment(ref _pendingCommands);
        if (!_queue.Writer.TryWrite(workItem))
        {
            Interlocked.Decrement(ref _pendingCommands);
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

                try
                {
                    if (workItem.CancellationToken.IsCancellationRequested)
                    {
                        workItem.Completion.TrySetCanceled(workItem.CancellationToken);
                        UpdateSnapshot(workItem.Command, "Canceled before execution");
                        continue;
                    }

                    await workItem.Operation(workItem.CancellationToken);
                    workItem.Completion.TrySetResult(null);
                    UpdateSnapshot(workItem.Command, null);
                    Logger.LogEvent("CAP-COORD-DONE", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                catch (OperationCanceledException oce) when (workItem.CancellationToken.IsCancellationRequested)
                {
                    workItem.Completion.TrySetCanceled(workItem.CancellationToken);
                    UpdateSnapshot(workItem.Command, oce.Message);
                    Logger.LogEvent("CAP-COORD-CANCEL", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    workItem.Completion.TrySetException(ex);
                    UpdateSnapshot(workItem.Command, ex.Message);
                    Logger.LogException(ex);
                    Logger.LogEvent("CAP-COORD-FAIL", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                finally
                {
                    sw.Stop();
                    Interlocked.Decrement(ref _pendingCommands);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disposal/cancellation.
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
        finally
        {
            Exception failure = _isDisposed
                ? new ObjectDisposedException(nameof(CaptureSessionCoordinator))
                : new OperationCanceledException("Capture session coordinator stopped.");
            FailPendingCommands(failure);
        }
    }

    private void UpdateSnapshot(CaptureCommand command, string? error)
    {
        lock (_snapshotLock)
        {
            _lastTransitionUtc = DateTimeOffset.UtcNow;
            _lastCommand = command.Kind;
            _lastCorrelationId = command.CorrelationId;
            _lastError = error;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));
        }
    }

    private void FailPendingCommands(Exception ex)
    {
        while (_queue.Reader.TryRead(out var pending))
        {
            pending.Completion.TrySetException(ex);
            Interlocked.Decrement(ref _pendingCommands);
        }
    }

    private static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _queue.Writer.TryComplete();
        var drainTimeoutMs = GetIntFromEnv(
            "ELGATOCAPTURE_COORDINATOR_DISPOSE_TIMEOUT_MS",
            DefaultDisposeDrainTimeoutMs,
            1000,
            300000);

        try
        {
            var completed = Task.WhenAny(_workerTask, Task.Delay(drainTimeoutMs)).GetAwaiter().GetResult();
            if (completed != _workerTask)
            {
                Logger.Log($"CaptureSessionCoordinator dispose timed out after {drainTimeoutMs} ms; cancelling worker.");
                _workerCancellation.Cancel();

                var cancelWaitMs = Math.Clamp(drainTimeoutMs / 3, 1000, 10000);
                completed = Task.WhenAny(_workerTask, Task.Delay(cancelWaitMs)).GetAwaiter().GetResult();
                if (completed != _workerTask)
                {
                    Logger.Log($"CaptureSessionCoordinator worker still running after cancel wait ({cancelWaitMs} ms).");
                }
            }

            if (_workerTask.IsCompleted)
            {
                _workerTask.GetAwaiter().GetResult();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if cancellation has already been requested.
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
        finally
        {
            _workerCancellation.Cancel();
            _workerCancellation.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _queue.Writer.TryComplete();
        var drainTimeoutMs = GetIntFromEnv(
            "ELGATOCAPTURE_COORDINATOR_DISPOSE_TIMEOUT_MS",
            DefaultDisposeDrainTimeoutMs,
            1000,
            300000);

        try
        {
            var completed = await Task.WhenAny(_workerTask, Task.Delay(drainTimeoutMs));
            if (completed != _workerTask)
            {
                Logger.Log($"CaptureSessionCoordinator async dispose timed out after {drainTimeoutMs} ms; cancelling worker.");
                _workerCancellation.Cancel();

                var cancelWaitMs = Math.Clamp(drainTimeoutMs / 3, 1000, 10000);
                completed = await Task.WhenAny(_workerTask, Task.Delay(cancelWaitMs));
                if (completed != _workerTask)
                {
                    Logger.Log($"CaptureSessionCoordinator worker still running after async cancel wait ({cancelWaitMs} ms).");
                }
            }

            if (_workerTask.IsCompleted)
            {
                await _workerTask;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
        finally
        {
            _workerCancellation.Cancel();
            _workerCancellation.Dispose();
        }
    }
}

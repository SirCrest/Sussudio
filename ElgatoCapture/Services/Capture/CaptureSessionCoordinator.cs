using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.Services.Capture;

public enum CaptureCommandKind
{
    Initialize,
    StartVideoPreview,
    StopVideoPreview,
    StartRecording,
    StopRecording,
    StartAudioPreview,
    StopAudioPreview,
    UpdateAudioMonitoring,
    UpdateAudioInput,
    UpdateMicrophoneMonitor,
    SetFlashbackEnabled,
    UpdateFlashbackSettings,
    Cleanup,
    RestartFlashback,
    UpdateFlashbackRecordingFormat,
    CycleFlashbackEncoderSettings
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
    public long CommandsEnqueued { get; init; }
    public long CommandsCompleted { get; init; }
    public long CommandsFailed { get; init; }
    public long CommandsCanceled { get; init; }
    public int PendingCommands { get; init; }
    public int MaxPendingCommands { get; init; }
    public long OldestPendingCommandAgeMs { get; init; }
    public long LastCommandQueueLatencyMs { get; init; }
    public long MaxCommandQueueLatencyMs { get; init; }
    public CaptureSessionState SessionState { get; init; }
    public bool IsRecording { get; init; }
    public bool IsInitialized { get; init; }
    public bool IsVideoPreviewActive { get; init; }
    public bool IsAudioPreviewActive { get; init; }
}

internal readonly record struct FlashbackPlaybackSnapshot(
    bool IsActive,
    FlashbackPlaybackState State,
    TimeSpan PlaybackPosition,
    TimeSpan GapFromLive,
    TimeSpan? InPoint,
    TimeSpan? OutPoint)
{
    public static FlashbackPlaybackSnapshot Disabled { get; } = new(
        false,
        FlashbackPlaybackState.Disabled,
        TimeSpan.Zero,
        TimeSpan.Zero,
        null,
        null);
}

internal readonly record struct FlashbackBufferStatus(
    bool IsActive,
    TimeSpan BufferDuration,
    TimeSpan FilledDuration,
    long DiskBytes,
    bool IsDiskWarningActive)
{
    public static FlashbackBufferStatus Inactive { get; } = new(
        false,
        TimeSpan.Zero,
        TimeSpan.Zero,
        0,
        false);
}

public sealed class CaptureSessionCoordinator : IDisposable, IAsyncDisposable
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
    private long _lastCommandQueueLatencyMs;
    private long _maxCommandQueueLatencyMs;
    private readonly Queue<DateTimeOffset> _pendingCommandEnqueuedAtUtc = new();
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
                    PendingCommands = Volatile.Read(ref _pendingCommands),
                    MaxPendingCommands = Volatile.Read(ref _maxPendingCommands),
                    OldestPendingCommandAgeMs = oldestPendingCommandAgeMs,
                    LastCommandQueueLatencyMs = Volatile.Read(ref _lastCommandQueueLatencyMs),
                    MaxCommandQueueLatencyMs = Volatile.Read(ref _maxCommandQueueLatencyMs),
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

    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.RestartFlashback,
            ct => _captureService.RestartFlashbackAsync(ct),
            cancellationToken,
            propagateCancellationToOperation: true);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return EnqueueAsync(
            CaptureCommandKind.RestartFlashback,
            ct => _captureService.RestartFlashbackAsync(settings, ct),
            cancellationToken,
            propagateCancellationToOperation: true);
    }

    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateFlashbackRecordingFormat,
            ct => _captureService.UpdateRecordingFormatAsync(format, ct),
            cancellationToken);

    public Task CycleFlashbackEncoderSettingsAsync(
        VideoQuality? quality = null,
        double? customBitrateMbps = null,
        string? nvencPreset = null,
        CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.CycleFlashbackEncoderSettings,
            ct => _captureService.CycleFlashbackEncoderSettingsAsync(quality, customBitrateMbps, nvencPreset, ct),
            cancellationToken,
            coalesceLatest: true);

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.SetFlashbackEnabled,
            ct => _captureService.SetFlashbackEnabledAsync(enabled, ct),
            cancellationToken,
            propagateCancellationToOperation: true);

    public Task UpdateFlashbackSettingsAsync(int bufferMinutes, bool gpuDecode, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateFlashbackSettings,
            ct => _captureService.UpdateFlashbackSettingsAsync(bufferMinutes, gpuDecode, ct),
            cancellationToken);

    internal bool IsFlashbackActive => _captureService.IsFlashbackActive;

    internal long FlashbackTotalBytesWritten => _captureService.FlashbackTotalBytesWritten;

    internal FlashbackBufferStatus GetFlashbackBufferStatus()
    {
        ThrowIfDisposed();
        var bufferManager = _captureService.FlashbackBufferManager;
        if (bufferManager == null || !_captureService.IsFlashbackActive)
        {
            return FlashbackBufferStatus.Inactive;
        }

        return new FlashbackBufferStatus(
            true,
            bufferManager.Options.BufferDuration,
            bufferManager.BufferedDuration,
            _captureService.FlashbackDiskBytes,
            bufferManager.IsDiskWarningActive);
    }

    internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()
    {
        ThrowIfDisposed();
        var controller = _captureService.FlashbackPlaybackController;
        return controller == null || controller.IsDisposed
            ? FlashbackPlaybackSnapshot.Disabled
            : new FlashbackPlaybackSnapshot(
                true,
                controller.State,
                controller.PlaybackPosition,
                controller.GapFromLive,
                controller.InPoint,
                controller.OutPoint);
    }

    internal bool FlashbackBeginScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackBeginScrub), out var controller)) return false;
        return controller.BeginScrub(position);
    }

    internal bool FlashbackSeek(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackSeek), out var controller)) return false;
        return controller.Seek(position);
    }

    internal bool FlashbackUpdateScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackUpdateScrub), out var controller)) return false;
        return controller.UpdateScrub(position);
    }

    internal bool FlashbackEndScrub()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackEndScrub), out var controller)) return false;
        return controller.EndScrub();
    }

    internal bool FlashbackPlay()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackPlay), out var controller)) return false;
        return controller.Play();
    }

    internal bool FlashbackPause()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackPause), out var controller)) return false;
        return controller.Pause();
    }

    internal bool FlashbackGoLive()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)) return false;
        return controller.GoLive();
    }

    internal bool FlashbackNudge(TimeSpan delta)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackNudge), out var controller)) return false;
        return controller.NudgePosition(delta);
    }

    internal TimeSpan? FlashbackSetInPoint()
    {
        return TryGetActiveFlashback(nameof(FlashbackSetInPoint), out var controller)
            ? controller.SetInPoint()
            : null;
    }

    internal TimeSpan? FlashbackSetOutPoint()
    {
        return TryGetActiveFlashback(nameof(FlashbackSetOutPoint), out var controller)
            ? controller.SetOutPoint()
            : null;
    }

    internal bool FlashbackClearInOutPoints()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackClearInOutPoints), out var controller)) return false;
        controller.ClearInOutPoints();
        return true;
    }

    internal Task<FinalizeResult> ExportFlashbackRangeAsync(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _captureService.ExportFlashbackRangeAsync(inPoint, outPoint, outputPath, progress, cancellationToken);
    }

    internal Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _captureService.ExportFlashbackLastNSecondsAsync(seconds, outputPath, progress, cancellationToken);
    }

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        ThrowIfDisposed();
        return _captureService.GetFlashbackSegments();
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Cleanup, ct => _captureService.CleanupAsync(ct), cancellationToken);

    private bool TryGetActiveFlashback(
        string command,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FlashbackPlaybackController? controller)
    {
        ThrowIfDisposed();
        controller = _captureService.FlashbackPlaybackController;
        if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })
        {
            return true;
        }

        var reason = controller == null
            ? "missing_controller"
            : controller.IsDisposed
                ? "disposed"
                : !controller.IsInitialized
                ? "not_initialized"
                : $"state_{controller.State}";
        Logger.Log($"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}");
        return false;
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
        UpdateMaxInt(ref _maxPendingCommands, pending);
        TrackPendingCommandEnqueued(command.EnqueuedAtUtc);
        if (!_queue.Writer.TryWrite(workItem))
        {
            RemoveOldestPendingCommand();
            DisposeCancellationRegistrationBestEffort(cancellationRegistration, "enqueue_failed");
            Interlocked.Decrement(ref _pendingCommands);
            Logger.LogEvent("CAP-COORD-ENQUEUE-FAIL", $"{kind} corr={correlationId}");
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
                        UpdateSnapshot(workItem.Command, "Skipped stale coalesced command");
                        Logger.LogEvent("CAP-COORD-SKIP", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} stale_generation={generation}");
                        continue;
                    }

                    if (workItem.CancellationToken.IsCancellationRequested)
                    {
                        workItem.Completion.TrySetCanceled(workItem.CancellationToken);
                        Interlocked.Increment(ref _commandsCanceled);
                        UpdateSnapshot(workItem.Command, "Canceled before execution");
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
                        UpdateSnapshot(workItem.Command, "Canceled before execution");
                        continue;
                    }

                    await workItem.Operation(operationToken);
                    workItem.Completion.TrySetResult(null);
                    Interlocked.Increment(ref _commandsCompleted);
                    UpdateSnapshot(workItem.Command, null);
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
                    UpdateSnapshot(workItem.Command, oce.Message);
                    Logger.LogEvent("CAP-COORD-CANCEL", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    workItem.Completion.TrySetException(ex);
                    Interlocked.Increment(ref _commandsFailed);
                    UpdateSnapshot(workItem.Command, ex.Message);
                    Logger.LogException(ex);
                    Logger.LogEvent("CAP-COORD-FAIL", $"{workItem.Command.Kind} corr={workItem.Command.CorrelationId} in {sw.ElapsedMilliseconds} ms");
                }
                finally
                {
                    sw.Stop();
                    RemoveOldestPendingCommand();
                    Interlocked.Decrement(ref _pendingCommands);
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
            pending.Completion.TrySetException(ex);
            Interlocked.Increment(ref _commandsFailed);
            Interlocked.Decrement(ref _pendingCommands);
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
        UpdateMaxLong(ref _maxCommandQueueLatencyMs, latencyMs);
    }

    private static void UpdateMaxInt(ref int target, int candidate)
    {
        var current = Volatile.Read(ref target);
        while (candidate > current)
        {
            var observed = Interlocked.CompareExchange(ref target, candidate, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    private static void UpdateMaxLong(ref long target, long candidate)
    {
        var current = Volatile.Read(ref target);
        while (candidate > current)
        {
            var observed = Interlocked.CompareExchange(ref target, candidate, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
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
        CancelWorkerBestEffort();
        var drainTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "ELGATOCAPTURE_COORDINATOR_DISPOSE_TIMEOUT_MS",
            DefaultDisposeDrainTimeoutMs,
            1000,
            300000);

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromMilliseconds(drainTimeoutMs)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.Log($"CaptureSessionCoordinator dispose timed out after {drainTimeoutMs} ms.");
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

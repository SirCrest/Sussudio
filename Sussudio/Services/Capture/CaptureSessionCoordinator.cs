using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
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

    // Used exclusively by MainViewModel.StopRecordingForEmergencyAsync -> routes through the
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

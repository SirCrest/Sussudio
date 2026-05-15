using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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

}

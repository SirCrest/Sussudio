using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Automation;

// Polling diagnostics aggregator for automation, ssctl, and MCP tools. It
// builds a single snapshot from UI state, capture runtime state, verifier
// results, and health counters, then turns sustained bad signals into recent
// diagnostic events and performance-timeline samples.
public sealed partial class AutomationDiagnosticsHub : IAutomationDiagnosticsHub
{
    private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;
    private readonly Func<CancellationToken, Task<PreviewRuntimeSnapshot>> _previewSnapshotProvider;
    private readonly IRecordingVerifier _recordingVerifier;
    private readonly object _stateLock = new();
    private readonly List<DiagnosticsEvent> _recentEvents = new();
    private readonly Dictionary<string, long> _eventThrottleTicks = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeAlerts = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _verificationGate = new(1, 1);
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private AutomationSnapshot _latestSnapshot = new();
    private RecordingVerificationResult? _lastVerification;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;
    private bool _wasRecording;
    private long _lastRecordedBytes;
    private string? _cachedFinalOutputPath;
    private long? _cachedFinalOutputSize;
    private long _muteLowSignalStartTick;
    private long _recordingNoGrowthStartTick;
    private long _lastFlashbackExportCompletionEventId;
    private Task? _autoVerificationTask;
    private int _verificationInProgress;
    private int _autoVerificationScheduled;

    // Fixed-size timeline ring used by long diagnostic sessions. It keeps the
    // last few minutes of high-level performance evidence in memory without
    // requiring every poll to become an unbounded event.
    private readonly PerformanceTimelineEntry[] _timelineBuffer = new PerformanceTimelineEntry[TimelineCapacity];
    private int _timelineHead;
    private int _timelineCount;
    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private long _lastProcessCpuSampleTimestamp;
    private double _lastProcessCpuTotalMs;

    private const int MaxRecentEvents = 500;
    private const int PollIntervalMs = 500;
    private const int LowSignalMuteThresholdMs = 8000;
    private const int RecordingNoGrowthThresholdMs = 4000;
    private const double AudioSignalThreshold = 0.008;
    private const int CapturePerfectionMinSamples = 180;
    private const int PreviewPerfectionMinSamples = 120;
    private const int VerificationPerfectionMinSamples = 120;
    private const int TimelineCapacity = 240;
    private const int FlashbackPlaybackCommandStallThresholdMs = 1000;
    private const int FlashbackPlaybackCommandFailureRecentMs = 30000;
    private const int FlashbackExportStallThresholdMs = 30000;
    private const double FlashbackPlaybackSlowFpsRatio = 0.75;
    private const double CaptureOnePercentLowWarningRatio = 0.98;
    private const double PreviewOnePercentLowWarningRatio = 0.98;
    private const double FlashbackPlaybackOnePercentLowWarningRatio = 0.98;
    private const int FlashbackPlaybackMinFramesForPerfAlert = 60;
    private const int FlashbackPlaybackOnePercentLowMinimumFrames = 1200;
    private const double FlashbackPlaybackAudioMasterFallbackWarningRatio = 0.50;
    private const int FlashbackPlaybackAudioQueueBacklogWarningDepth = 24;
    private const long FlashbackTempDriveLowFreeBytes = 5L * 1024L * 1024L * 1024L;
    private const long FlashbackRecordingBackpressureWarningMs = 100;
    private const double FlashbackRecordingQueueDepthWarningRatio = 0.75;
    private const double FlashbackAudioQueueDepthWarningRatio = 0.90;
    private const long FlashbackRecordingQueueAgeWarningMs = 500;

    private readonly double _perfectionCaptureDropPercentThreshold;
    private readonly double _perfectionCaptureP95MultiplierThreshold;
    private readonly double _perfectionPreviewSlowPercentThreshold;
    private readonly double _perfectionVerificationDropPercentThreshold;

    public event EventHandler<AutomationSnapshot>? SnapshotUpdated;

    public AutomationDiagnosticsHub(
        IAutomationSnapshotQueryPort snapshotQueryPort,
        Func<CancellationToken, Task<PreviewRuntimeSnapshot>> previewSnapshotProvider,
        IRecordingVerifier recordingVerifier)
    {
        _snapshotQueryPort = snapshotQueryPort ?? throw new ArgumentNullException(nameof(snapshotQueryPort));
        _previewSnapshotProvider = previewSnapshotProvider ?? throw new ArgumentNullException(nameof(previewSnapshotProvider));
        _recordingVerifier = recordingVerifier ?? throw new ArgumentNullException(nameof(recordingVerifier));
        _perfectionCaptureDropPercentThreshold = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PERF_CAPTURE_DROP_PCT", 0.10, 0.0, 50.0);
        _perfectionCaptureP95MultiplierThreshold = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PERF_CAPTURE_P95_MULT", 1.30, 1.0, 10.0);
        _perfectionPreviewSlowPercentThreshold = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PERF_PREVIEW_SLOW_PCT", 2.00, 0.0, 100.0);
        _perfectionVerificationDropPercentThreshold = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PERF_VERIFY_DROP_PCT", 0.25, 0.0, 100.0);
    }

    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AutomationDiagnosticsHub));
        }

        if (_loopTask != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        AddEvent(DiagnosticsSeverity.Info, DiagnosticsCategory.System, "Diagnostics hub started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask == null)
        {
            return;
        }

        _cts?.Cancel();
        var loopTask = _loopTask;
        _loopTask = null;
        var autoVerificationTask = _autoVerificationTask;
        _autoVerificationTask = null;
        Interlocked.Exchange(ref _autoVerificationScheduled, 0);

        try
        {
            await Task.WhenAny(loopTask, Task.Delay(5000, cancellationToken)).ConfigureAwait(false);
            if (autoVerificationTask != null)
            {
                await Task.WhenAny(autoVerificationTask, Task.Delay(5000, cancellationToken)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown - stop/dispose requested while awaiting loop tasks */
        }

        _cts?.Dispose();
        _cts = null;
        AddEvent(DiagnosticsSeverity.Info, DiagnosticsCategory.System, "Diagnostics hub stopped.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Never block the UI thread on a Task that may itself need the UI thread
        // to make progress (StopAsync awaits items that may have dispatched back).
        // Task.Run breaks the ambient SynchronizationContext so StopAsync can
        // complete without re-entering a captured UI dispatcher. The budget is
        // 12 s: StopAsync has two consecutive 5 s Task.WhenAny waits internally
        // (loopTask + autoVerificationTask), so 12 s covers both with margin.
        // Callers that need deterministic teardown should call DisposeAsync.
        var stoppedCleanly = false;
        try
        {
            var stop = Task.Run(() => StopAsync());
            if (stop.Wait(TimeSpan.FromSeconds(12)))
            {
                stoppedCleanly = true;
            }
            else
            {
                Logger.Log("DIAGHUB_DISPOSE_TIMEOUT msg='StopAsync did not complete within 12 s; abandoning'");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"DIAGHUB_DISPOSE_FAULT type={ex.GetType().Name} msg='{ex.Message}'");
        }

        if (stoppedCleanly)
        {
            _currentProcess.Dispose();
        }
        else
        {
            // StopAsync did not complete within the budget; the abandoned RunLoopAsync
            // may still call _currentProcess.Refresh() / WorkingSet64. Disposing the
            // handle here would race with those reads and produce ObjectDisposedException
            // churn on the loop thread. Skip the dispose; the kernel reclaims the
            // process handle when the host process exits (Dispose is only invoked from
            // teardown paths, so the leak is bounded).
            Logger.Log("DIAGHUB_DISPOSE_SKIPPED_PROCESS_HANDLE reason=stop_timeout");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
        _currentProcess.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown - exit the refresh loop */
                break;
            }
            catch (Exception ex)
            {
                AddEvent(DiagnosticsSeverity.Error, DiagnosticsCategory.System, $"Diagnostics refresh failed: {ex.Message}");
            }

            try
            {
                await Task.Delay(PollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown - exit the refresh loop */
                break;
            }
        }
    }
}

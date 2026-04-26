using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.Services.Automation;

public sealed class AutomationDiagnosticsHub : IAutomationDiagnosticsHub
{
    private readonly IAutomationViewModel _viewModel;
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
    private long _muteLowSignalStartTick;
    private long _recordingNoGrowthStartTick;
    private Task? _autoVerificationTask;
    private int _verificationInProgress;
    private int _autoVerificationScheduled;
    private readonly PerformanceTimelineEntry[] _timelineBuffer = new PerformanceTimelineEntry[TimelineCapacity];
    private int _timelineHead;
    private int _timelineCount;
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    private const int MaxRecentEvents = 500;
    private const int PollIntervalMs = 500;
    private const int LowSignalMuteThresholdMs = 8000;
    private const int RecordingNoGrowthThresholdMs = 4000;
    private const double AudioSignalThreshold = 0.008;
    private const int CapturePerfectionMinSamples = 180;
    private const int PreviewPerfectionMinSamples = 120;
    private const int VerificationPerfectionMinSamples = 120;
    private const int TimelineCapacity = 240;

    private readonly double _perfectionCaptureDropPercentThreshold;
    private readonly double _perfectionCaptureP95MultiplierThreshold;
    private readonly double _perfectionPreviewSlowPercentThreshold;
    private readonly double _perfectionVerificationDropPercentThreshold;

    public event EventHandler<AutomationSnapshot>? SnapshotUpdated;

    public AutomationDiagnosticsHub(
        IAutomationViewModel viewModel,
        Func<CancellationToken, Task<PreviewRuntimeSnapshot>> previewSnapshotProvider,
        IRecordingVerifier recordingVerifier)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _previewSnapshotProvider = previewSnapshotProvider ?? throw new ArgumentNullException(nameof(previewSnapshotProvider));
        _recordingVerifier = recordingVerifier ?? throw new ArgumentNullException(nameof(recordingVerifier));
        _perfectionCaptureDropPercentThreshold = EnvironmentHelpers.GetDoubleFromEnv("ELGATOCAPTURE_PERF_CAPTURE_DROP_PCT", 0.10, 0.0, 50.0);
        _perfectionCaptureP95MultiplierThreshold = EnvironmentHelpers.GetDoubleFromEnv("ELGATOCAPTURE_PERF_CAPTURE_P95_MULT", 1.30, 1.0, 10.0);
        _perfectionPreviewSlowPercentThreshold = EnvironmentHelpers.GetDoubleFromEnv("ELGATOCAPTURE_PERF_PREVIEW_SLOW_PCT", 2.00, 0.0, 100.0);
        _perfectionVerificationDropPercentThreshold = EnvironmentHelpers.GetDoubleFromEnv("ELGATOCAPTURE_PERF_VERIFY_DROP_PCT", 0.25, 0.0, 100.0);
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
            /* Expected during shutdown — stop/dispose requested while awaiting loop tasks */
        }

        _cts?.Dispose();
        _cts = null;
        AddEvent(DiagnosticsSeverity.Info, DiagnosticsCategory.System, "Diagnostics hub stopped.");
    }

    public AutomationSnapshot GetLatestSnapshot()
    {
        lock (_stateLock)
        {
            return _latestSnapshot;
        }
    }

    public Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default)
        => RefreshSnapshotAsync(cancellationToken);

    public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline(int maxEntries = 240)
    {
        lock (_stateLock)
        {
            var count = Math.Min(_timelineCount, Math.Max(0, maxEntries));
            if (count == 0)
            {
                return Array.Empty<PerformanceTimelineEntry>();
            }

            var result = new PerformanceTimelineEntry[count];
            var oldest = (_timelineHead - _timelineCount + TimelineCapacity) % TimelineCapacity;
            var skip = _timelineCount - count;
            var readIndex = (oldest + skip) % TimelineCapacity;
            for (var i = 0; i < count; i++)
            {
                result[i] = _timelineBuffer[readIndex];
                readIndex = (readIndex + 1) % TimelineCapacity;
            }

            return result;
        }
    }

    public IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100)
    {
        lock (_stateLock)
        {
            var take = Math.Clamp(maxEvents, 1, MaxRecentEvents);
            if (_recentEvents.Count <= take)
            {
                return _recentEvents.ToArray();
            }

            return _recentEvents.Skip(_recentEvents.Count - take).ToArray();
        }
    }

    public async Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default)
    {
        await _verificationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _verificationInProgress);
        try
        {
            var runtimeSnapshot = await _viewModel
                .GetCaptureRuntimeSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);

            var verification = await _recordingVerifier
                .VerifyAsync(runtimeSnapshot.LastOutputPath, runtimeSnapshot, cancellationToken)
                .ConfigureAwait(false);

            lock (_stateLock)
            {
                _lastVerification = verification;
            }

            var mismatchDetail = !verification.Succeeded && !string.IsNullOrWhiteSpace(verification.PrimaryMismatchCode)
                ? $" [{verification.PrimaryMismatchCode}"
                + (verification.PrimaryMismatchExpected != null ? $", expected={verification.PrimaryMismatchExpected}" : string.Empty)
                + (verification.PrimaryMismatchActual != null ? $", actual={verification.PrimaryMismatchActual}" : string.Empty)
                + "]"
                : string.Empty;

            AddEvent(
                verification.Succeeded ? DiagnosticsSeverity.Info : DiagnosticsSeverity.Error,
                DiagnosticsCategory.Verification,
                $"{verification.Message}{mismatchDetail}");

            await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return verification;
        }
        finally
        {
            Interlocked.Decrement(ref _verificationInProgress);
            _verificationGate.Release();
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub post-verification snapshot refresh: {ex.Message}");
                }
            }
        }
    }

    public async Task<RecordingVerificationResult> VerifyFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _verificationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _verificationInProgress);
        try
        {
            var runtimeSnapshot = await _viewModel
                .GetCaptureRuntimeSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);

            var verification = await _recordingVerifier
                .VerifyAsync(filePath, runtimeSnapshot, cancellationToken)
                .ConfigureAwait(false);

            lock (_stateLock)
            {
                _lastVerification = verification;
            }

            AddEvent(
                verification.Succeeded ? DiagnosticsSeverity.Info : DiagnosticsSeverity.Error,
                DiagnosticsCategory.Verification,
                $"File verification ({System.IO.Path.GetFileName(filePath)}): {verification.Message}");

            await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return verification;
        }
        finally
        {
            Interlocked.Decrement(ref _verificationInProgress);
            _verificationGate.Release();
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub post-verification snapshot refresh: {ex.Message}");
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
        _currentProcess.Dispose();
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
                /* Expected during shutdown — exit the refresh loop */
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
                /* Expected during shutdown — exit the refresh loop */
                break;
            }
        }
    }

    private async Task<AutomationSnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModelSnapshot = await _viewModel
            .GetViewModelRuntimeSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var captureRuntime = await _viewModel
            .GetCaptureRuntimeSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var health = await _viewModel
            .GetCaptureHealthSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var recordingStats = await _viewModel
            .GetRecordingStatsSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var previewRuntime = await _previewSnapshotProvider(cancellationToken).ConfigureAwait(false);

        var nowTick = Environment.TickCount64;
        var recordingStarted = viewModelSnapshot.IsRecording && !_wasRecording;
        if (recordingStarted)
        {
            _lastRecordedBytes = recordingStats.TotalBytes;
            _recordingNoGrowthStartTick = 0;
        }

        var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;
        var audioContextActive = viewModelSnapshot.IsAudioEnabled &&
                                 (viewModelSnapshot.IsAudioPreviewEnabled || viewModelSnapshot.IsRecording);
        if (audioContextActive && !audioSignalPresent)
        {
            if (_muteLowSignalStartTick == 0)
            {
                _muteLowSignalStartTick = nowTick;
            }
        }
        else
        {
            _muteLowSignalStartTick = 0;
        }

        var audioMutedSuspected = audioContextActive &&
                                  _muteLowSignalStartTick > 0 &&
                                  nowTick - _muteLowSignalStartTick >= LowSignalMuteThresholdMs;

        var recordingFileGrowing = true;
        var totalBytes = recordingStats.TotalBytes;
        if (viewModelSnapshot.IsRecording)
        {
            if (totalBytes > _lastRecordedBytes)
            {
                _recordingNoGrowthStartTick = 0;
                recordingFileGrowing = true;
            }
            else
            {
                if (_recordingNoGrowthStartTick == 0)
                {
                    _recordingNoGrowthStartTick = nowTick;
                }

                recordingFileGrowing = nowTick - _recordingNoGrowthStartTick < RecordingNoGrowthThresholdMs;
            }

            _lastRecordedBytes = totalBytes;
        }
        else
        {
            _lastRecordedBytes = totalBytes;
            _recordingNoGrowthStartTick = 0;
            recordingFileGrowing = false;
        }

        RecordingVerificationResult? lastVerification;
        lock (_stateLock)
        {
            if (recordingStarted)
            {
                _lastVerification = null;
            }

            lastVerification = _lastVerification;
        }
        var performance = EvaluatePerformance(
            isPreviewing: viewModelSnapshot.IsPreviewing,
            isRecording: viewModelSnapshot.IsRecording,
            recordingFileGrowing: recordingFileGrowing,
            previewGpuActive: previewRuntime.GpuActive,
            previewBlankSuspected: previewRuntime.BlankSuspected,
            previewStalled: previewRuntime.StallSuspected,
            previewCadenceSampleCount: previewRuntime.DisplayCadenceSampleCount,
            previewCadenceSlowFramePercent: previewRuntime.DisplayCadenceSlowFramePercent,
            captureCadenceSampleCount: health.CaptureCadenceSampleCount,
            captureCadenceExpectedIntervalMs: health.CaptureCadenceExpectedIntervalMs,
            captureCadenceP95IntervalMs: health.CaptureCadenceP95IntervalMs,
            captureCadenceDropPercent: health.CaptureCadenceEstimatedDropPercent,
            lastVerification: lastVerification);
        var hdrTruthVerdict = BuildHdrTruthVerdict(captureRuntime, viewModelSnapshot.IsHdrEnabled, lastVerification);
        var previewHdrInputDetected =
            IsHdrSubtype(captureRuntime.NegotiatedPixelFormat) ||
            (captureRuntime.RequestedHdrEnabled ?? false) ||
            viewModelSnapshot.IsHdrEnabled;
        var previewToneMapMode = !previewHdrInputDetected
            ? "None"
            : previewRuntime.GpuActive
                ? "Auto"
                : "Unavailable";

        bool lastOutputExists = false;
        long? lastOutputSize = null;
        if (!string.IsNullOrWhiteSpace(captureRuntime.LastOutputPath))
        {
            try
            {
                lastOutputSize = new FileInfo(captureRuntime.LastOutputPath).Length;
                lastOutputExists = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub output file probe: {ex.Message}");
            }
        }

        // Memory & GC metrics (all APIs are thread-safe and microsecond-cheap)
        _currentProcess.Refresh();
        var memoryWorkingSetMb = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
        var memoryPrivateBytesMb = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
        var memoryManagedHeapMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        var memoryTotalAllocatedMb = GC.GetTotalAllocatedBytes(precise: false) / (1024.0 * 1024.0);
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var memoryGcHeapSizeMb = gcMemoryInfo.HeapSizeBytes / (1024.0 * 1024.0);
        var gcPauseTimePercent = gcMemoryInfo.PauseTimePercentage;
        var gcFragmentationPercent = gcMemoryInfo.HeapSizeBytes > 0
            ? gcMemoryInfo.FragmentedBytes * 100.0 / gcMemoryInfo.HeapSizeBytes
            : 0.0;
        var gcGen0 = GC.CollectionCount(0);
        var gcGen1 = GC.CollectionCount(1);
        var gcGen2 = GC.CollectionCount(2);
        ThreadPool.GetAvailableThreads(out var tpWorkerAvailable, out var tpIoAvailable);
        ThreadPool.GetMaxThreads(out var tpWorkerMax, out var tpIoMax);

        var snapshot = new AutomationSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsInitialized = viewModelSnapshot.IsInitialized,
            IsPreviewing = viewModelSnapshot.IsPreviewing,
            IsRecording = viewModelSnapshot.IsRecording,
            VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,
            IsAudioEnabled = viewModelSnapshot.IsAudioEnabled,
            IsAudioPreviewEnabled = viewModelSnapshot.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = viewModelSnapshot.IsCustomAudioInputEnabled,
            SessionState = captureRuntime.SessionState,
            StatusText = viewModelSnapshot.StatusText,
            PerformanceScore = performance.Score,
            PerformancePerfectionMet = performance.PerfectionMet,
            PerformanceSummary = performance.Summary,
            PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,
            PerformanceThresholdCaptureP95Multiplier = _perfectionCaptureP95MultiplierThreshold,
            PerformanceThresholdPreviewSlowPercent = _perfectionPreviewSlowPercentThreshold,
            PerformanceThresholdVerificationDropPercent = _perfectionVerificationDropPercentThreshold,
            SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,
            SelectedDeviceName = viewModelSnapshot.SelectedDeviceName,
            SelectedAudioInputDeviceId = viewModelSnapshot.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = viewModelSnapshot.SelectedAudioInputDeviceName,
            SelectedResolution = viewModelSnapshot.SelectedResolution,
            SelectedFrameRate = viewModelSnapshot.SelectedFrameRate,
            SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),
            SelectedExactFrameRate = viewModelSnapshot.SelectedExactFrameRate ?? viewModelSnapshot.SelectedFrameRate,
            SelectedExactFrameRateArg = viewModelSnapshot.SelectedExactFrameRateArg,
            DisabledResolutionReason = viewModelSnapshot.DisabledResolutionReason,
            DisabledFrameRateReason = viewModelSnapshot.DisabledFrameRateReason,
            DetectedSourceFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,
            DetectedSourceFrameRateArg = viewModelSnapshot.DetectedSourceFrameRateArg ?? captureRuntime.DetectedSourceFrameRateArg,
            SourceFrameRateOrigin = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceFrameRateOrigin) &&
                                    !string.Equals(viewModelSnapshot.SourceFrameRateOrigin, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? viewModelSnapshot.SourceFrameRateOrigin
                : captureRuntime.SourceFrameRateOrigin,
            SourceWidth = viewModelSnapshot.SourceWidth ?? captureRuntime.SourceWidth,
            SourceHeight = viewModelSnapshot.SourceHeight ?? captureRuntime.SourceHeight,
            SourceIsHdr = viewModelSnapshot.SourceIsHdr ?? captureRuntime.SourceIsHdr,
            SourceVideoFormat = captureRuntime.SourceVideoFormat,
            SourceColorimetry = captureRuntime.SourceColorimetry,
            SourceQuantization = captureRuntime.SourceQuantization,
            SourceHdrTransferFunction = captureRuntime.SourceHdrTransferFunction,
            SourceHdrTransferCode = captureRuntime.SourceHdrTransferCode,
            SourceFirmware = captureRuntime.SourceFirmware,
            SourceAudioFormat = captureRuntime.SourceAudioFormat,
            SourceAudioSampleRate = captureRuntime.SourceAudioSampleRate,
            SourceInputSource = captureRuntime.SourceInputSource,
            SourceUsbHostProtocol = captureRuntime.SourceUsbHostProtocol,
            SourceHdcpMode = captureRuntime.SourceHdcpMode,
            SourceHdcpVersion = captureRuntime.SourceHdcpVersion,
            SourceRxTxHdcpVersion = captureRuntime.SourceRxTxHdcpVersion,
            SourceRawTimingHex = captureRuntime.SourceRawTimingHex,
            SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability) &&
                                          !string.Equals(viewModelSnapshot.SourceTelemetryAvailability, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? viewModelSnapshot.SourceTelemetryAvailability
                : captureRuntime.SourceTelemetryAvailability,
            SourceTelemetryOriginDetail = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryOriginDetail) &&
                                          !string.Equals(viewModelSnapshot.SourceTelemetryOriginDetail, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? viewModelSnapshot.SourceTelemetryOriginDetail
                : captureRuntime.SourceTelemetryOriginDetail,
            SourceTelemetryConfidence = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryConfidence) &&
                                        !string.Equals(viewModelSnapshot.SourceTelemetryConfidence, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? viewModelSnapshot.SourceTelemetryConfidence
                : captureRuntime.SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary = viewModelSnapshot.SourceTelemetryDiagnosticSummary ?? captureRuntime.SourceTelemetryDiagnosticSummary,
            SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,
            SourceTelemetryTimestampUtc = viewModelSnapshot.SourceTelemetryTimestampUtc ?? captureRuntime.SourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = ResolveTelemetryAgeSeconds(
                viewModelSnapshot.SourceTelemetryAgeSeconds,
                viewModelSnapshot.SourceTelemetryTimestampUtc ?? captureRuntime.SourceTelemetryTimestampUtc,
                DateTimeOffset.UtcNow),
            SourceTelemetryBackend = captureRuntime.SourceTelemetryBackend,
            SourceTelemetrySuppressed = captureRuntime.SourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = captureRuntime.SourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = captureRuntime.SourceTelemetryCircuitState,
            SourceTelemetrySummaryText = viewModelSnapshot.SourceTelemetrySummaryText,
            SourceTargetSummaryText = viewModelSnapshot.SourceTargetSummaryText,
            SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,
            SelectedQuality = viewModelSnapshot.SelectedQuality,
            SelectedPreset = viewModelSnapshot.SelectedPreset,
            SelectedSplitEncodeMode = viewModelSnapshot.SelectedSplitEncodeMode,
            SelectedVideoFormat = viewModelSnapshot.SelectedVideoFormat,
            CustomBitrateMbps = viewModelSnapshot.CustomBitrateMbps,
            ShowAllCaptureOptions = viewModelSnapshot.ShowAllCaptureOptions,
            PreviewVolumePercent = viewModelSnapshot.PreviewVolumePercent,
            IsStatsVisible = viewModelSnapshot.IsStatsVisible,
            IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,
            IsHdrEnabled = viewModelSnapshot.IsHdrEnabled,
            HdrOutputActive = captureRuntime.HdrOutputActive,
            HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)
                ? viewModelSnapshot.HdrRuntimeState
                : captureRuntime.HdrRuntimeState,
            HdrReadinessReason = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrReadinessReason)
                ? viewModelSnapshot.HdrReadinessReason
                : captureRuntime.HdrReadinessReason,
            HdrWarmupState = captureRuntime.HdrWarmupState,
            HdrWarmupRequiredP010Frames = captureRuntime.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = captureRuntime.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = captureRuntime.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = captureRuntime.HdrDowngradeCode,
            RequestedPipelineMode = captureRuntime.RequestedPipelineMode,
            ActivePipelineMode = captureRuntime.ActivePipelineMode,
            PipelineModeMatched = captureRuntime.PipelineModeMatched,
            PipelineModeStatus = captureRuntime.PipelineModeStatus,
            PipelineModeReason = captureRuntime.PipelineModeReason,
            TelemetryAlignmentStatus = captureRuntime.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,
            OutputPath = viewModelSnapshot.OutputPath,
            RecordingTime = viewModelSnapshot.RecordingTime,
            RecordingSizeInfo = viewModelSnapshot.RecordingSizeInfo,
            RecordingBitrateInfo = viewModelSnapshot.RecordingBitrateInfo,
            AudioPeak = viewModelSnapshot.AudioPeak,
            AudioClipping = viewModelSnapshot.AudioClipping,
            AudioSignalPresent = audioSignalPresent,
            AudioMutedSuspected = audioMutedSuspected,
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            IngestVideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            IngestVideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth,
            WasapiCaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            WasapiCaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            WasapiPlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs,
            MemoryPreference = captureRuntime.MemoryPreference,
            VideoRequestedSubtype = captureRuntime.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,
            PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,
            EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,
            EncoderVideoFramesEncoded = health.VideoFramesConverted,
            EncoderLastEnqueueAgeMs = health.LastVideoEnqueueAgeMs,
            EncoderLastWriteAgeMs = health.LastVideoWriteAgeMs,
            RecordingBackend = captureRuntime.RecordingBackend,
            AudioPathMode = captureRuntime.AudioPathMode,
            MuxResult = captureRuntime.MuxSucceeded.HasValue
                ? (captureRuntime.MuxSucceeded.Value ? "Succeeded" : "Failed")
                : "NotAttempted",
            RequestedWidth = captureRuntime.RequestedWidth,
            RequestedHeight = captureRuntime.RequestedHeight,
            RequestedFrameRate = captureRuntime.RequestedFrameRate,
            RequestedFrameRateArg = captureRuntime.RequestedFrameRateArg,
            RequestedFrameRateNumerator = captureRuntime.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = captureRuntime.RequestedFrameRateDenominator,
            RequestedPixelFormat = captureRuntime.RequestedPixelFormat,
            RequestedFormat = captureRuntime.RequestedFormat,
            RequestedQuality = captureRuntime.RequestedQuality,
            RequestedHdrEnabled = captureRuntime.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = captureRuntime.RequestedHdrMasteringMetadata,
            RequestedAudioEnabled = captureRuntime.RequestedAudioEnabled,
            HdrActivationReason = captureRuntime.HdrActivationReason,
            HdrAutoDowngraded = captureRuntime.HdrAutoDowngraded,
            HdrAutoDowngradeReason = captureRuntime.HdrAutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit,
            ActualWidth = captureRuntime.ActualWidth,
            ActualHeight = captureRuntime.ActualHeight,
            ActualFrameRate = captureRuntime.ActualFrameRate,
            ActualFrameRateArg = captureRuntime.ActualFrameRateArg,
            NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,
            NegotiatedHeight = captureRuntime.NegotiatedHeight ?? captureRuntime.ActualHeight,
            NegotiatedFrameRate = captureRuntime.NegotiatedFrameRate ?? captureRuntime.ActualFrameRate,
            NegotiatedFrameRateArg = captureRuntime.NegotiatedFrameRateArg ?? captureRuntime.ActualFrameRateArg,
            NegotiatedFrameRateNumerator = captureRuntime.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = captureRuntime.NegotiatedFrameRateDenominator,
            NegotiatedPixelFormat = captureRuntime.NegotiatedPixelFormat,
            RequestedReaderSubtype = captureRuntime.RequestedReaderSubtype,
            ReaderSourceStreamType = captureRuntime.ReaderSourceStreamType,
            ReaderSourceSubtype = captureRuntime.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureRuntime.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureRuntime.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureRuntime.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureRuntime.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureRuntime.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureRuntime.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureRuntime.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureRuntime.ObservedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = captureRuntime.EncoderInputPixelFormat,
            EncoderOutputPixelFormat = captureRuntime.EncoderOutputPixelFormat,
            EncoderVideoCodec = captureRuntime.EncoderVideoCodec,
            EncoderVideoProfile = captureRuntime.EncoderVideoProfile,
            EncoderTenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed,
            MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken,
            PreviewFramesArrived = previewRuntime.FramesArrived,
            PreviewFramesDisplayed = previewRuntime.FramesDisplayed,
            PreviewFramesDropped = previewRuntime.FramesDropped,
            PreviewCadenceSampleCount = previewRuntime.DisplayCadenceSampleCount,
            PreviewCadenceObservedFps = previewRuntime.DisplayCadenceObservedFps,
            PreviewCadenceExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
            PreviewCadenceAverageIntervalMs = previewRuntime.DisplayCadenceAverageIntervalMs,
            PreviewCadenceP95IntervalMs = previewRuntime.DisplayCadenceP95IntervalMs,
            PreviewCadenceMaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            PreviewCadenceJitterStdDevMs = previewRuntime.DisplayCadenceJitterStdDevMs,
            PreviewCadenceSlowFrameCount = previewRuntime.DisplayCadenceSlowFrameCount,
            PreviewCadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,
            PreviewGpuActive = previewRuntime.GpuActive,
            PreviewPlaceholderVisible = previewRuntime.PlaceholderVisible,
            PreviewGpuElementVisible = previewRuntime.GpuElementVisible,
            PreviewCpuElementVisible = previewRuntime.CpuElementVisible,
            PreviewRendererAttached = previewRuntime.RendererAttached,
            PreviewStartupState = previewRuntime.StartupState,
            PreviewAttemptId = previewRuntime.StartupAttemptId,
            PreviewStartupElapsedMs = previewRuntime.StartupElapsedMs,
            PreviewStartupTimeoutMs = previewRuntime.StartupTimeoutMs,
            PreviewGpuSignalMediaOpened = previewRuntime.StartupGpuSignalMediaOpened,
            PreviewGpuSignalFirstFrame = previewRuntime.StartupGpuSignalFirstFrame,
            PreviewGpuSignalPlaybackAdvancing = previewRuntime.StartupGpuSignalPlaybackAdvancing,
            PreviewStartupRequiredSignals = previewRuntime.StartupRequiredSignals,
            PreviewStartupReceivedSignals = previewRuntime.StartupReceivedSignals,
            PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),
            PreviewStartupMissingSignals = previewRuntime.StartupMissingSignals,
            PreviewRecoveryAttemptCount = previewRuntime.StartupRecoveryAttemptCount,
            PreviewLastFailureReason = previewRuntime.StartupLastFailureReason,
            PreviewFirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,
            PreviewBlankSuspected = previewRuntime.BlankSuspected,
            PreviewStalled = previewRuntime.StallSuspected,
            PreviewRendererMode = previewRuntime.RendererMode,
            PreviewD3DSwapChainAddress = previewRuntime.D3DSwapChainAddress,
            PreviewD3DFramesSubmitted = previewRuntime.D3DFramesSubmitted,
            PreviewD3DFramesRendered = previewRuntime.D3DFramesRendered,
            PreviewD3DFramesDropped = previewRuntime.D3DFramesDropped,
            PreviewD3DPendingFrameCount = previewRuntime.D3DPendingFrameCount,
            PreviewD3DInputColorSpace = previewRuntime.D3DInputColorSpace,
            PreviewD3DOutputColorSpace = previewRuntime.D3DOutputColorSpace,
            PreviewD3DCpuTimingSampleCount = previewRuntime.D3DCpuTimingSampleCount,
            PreviewD3DInputUploadCpuAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            PreviewD3DInputUploadCpuP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            PreviewD3DInputUploadCpuMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            PreviewD3DRenderSubmitCpuAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            PreviewD3DRenderSubmitCpuP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            PreviewD3DRenderSubmitCpuMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PreviewD3DPresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PreviewD3DPresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PreviewD3DPresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            PreviewD3DTotalFrameCpuAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            PreviewD3DTotalFrameCpuP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            PreviewD3DTotalFrameCpuMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs,
            PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,
            PreviewGpuNaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            PreviewGpuNaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            PreviewGpuPositionMs = previewRuntime.GpuPositionMs,
            PreviewGpuPositionEventCount = previewRuntime.GpuPositionEventCount,
            PreviewHdrInputDetected = previewHdrInputDetected,
            PreviewToneMapMode = previewToneMapMode,
            PreviewColorContext = captureRuntime.NegotiatedPixelFormat,
            ConversionQueueDepth = health.ConversionQueueDepth,
            FfmpegVideoQueueDepth = health.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = health.FfmpegAudioQueueDepth,
            VideoFramesArrived = health.VideoFramesArrived,
            VideoFramesQueued = health.VideoFramesQueued,
            VideoFramesDropped = health.VideoFramesDropped,
            VideoFramesDroppedBacklog = health.VideoFramesDroppedBacklog,
            VideoFramesConverted = health.VideoFramesConverted,
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoDropsQueueSaturated = health.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction,
            RecordingEncodingFailed = health.RecordingEncodingFailed,
            RecordingEncodingFailureType = health.RecordingEncodingFailureType,
            RecordingEncodingFailureMessage = health.RecordingEncodingFailureMessage,
            RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,
            RecordingVideoQueueMaxDepth = health.RecordingVideoQueueMaxDepth,
            RecordingVideoFramesSubmittedToEncoder = health.RecordingVideoFramesSubmittedToEncoder,
            RecordingVideoEncoderPts = health.RecordingVideoEncoderPts,
            RecordingVideoEncoderPacketsWritten = health.RecordingVideoEncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = health.RecordingVideoEncoderDroppedFrames,
            RecordingVideoSequenceGaps = health.RecordingVideoSequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = health.RecordingVideoQueueOldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = health.RecordingVideoQueueLastLatencyMs,
            RecordingVideoQueueLatencySampleCount = health.RecordingVideoQueueLatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = health.RecordingVideoQueueLatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = health.RecordingVideoQueueLatencyP95Ms,
            RecordingVideoQueueLatencyMaxMs = health.RecordingVideoQueueLatencyMaxMs,
            RecordingVideoBackpressureWaitMs = health.RecordingVideoBackpressureWaitMs,
            RecordingVideoBackpressureEvents = health.RecordingVideoBackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = health.RecordingVideoBackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs,
            RecordingGpuQueueDepth = health.RecordingGpuQueueDepth,
            RecordingGpuQueueCapacity = health.RecordingGpuQueueCapacity,
            RecordingGpuQueueMaxDepth = health.RecordingGpuQueueMaxDepth,
            RecordingGpuFramesEnqueued = health.RecordingGpuFramesEnqueued,
            RecordingGpuFramesDropped = health.RecordingGpuFramesDropped,
            RecordingCudaQueueDepth = health.RecordingCudaQueueDepth,
            RecordingCudaQueueCapacity = health.RecordingCudaQueueCapacity,
            RecordingCudaQueueMaxDepth = health.RecordingCudaQueueMaxDepth,
            RecordingCudaFramesEnqueued = health.RecordingCudaFramesEnqueued,
            RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,
            FlashbackEncodingFailed = health.FlashbackEncodingFailed,
            FlashbackEncodingFailureType = health.FlashbackEncodingFailureType,
            FlashbackEncodingFailureMessage = health.FlashbackEncodingFailureMessage,
            FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,
            FlashbackVideoQueueMaxDepth = health.FlashbackVideoQueueMaxDepth,
            FlashbackVideoFramesSubmittedToEncoder = health.FlashbackVideoFramesSubmittedToEncoder,
            FlashbackVideoEncoderPts = health.FlashbackVideoEncoderPts,
            FlashbackVideoEncoderPacketsWritten = health.FlashbackVideoEncoderPacketsWritten,
            FlashbackVideoEncoderDroppedFrames = health.FlashbackVideoEncoderDroppedFrames,
            FlashbackVideoSequenceGaps = health.FlashbackVideoSequenceGaps,
            FlashbackVideoQueueOldestFrameAgeMs = health.FlashbackVideoQueueOldestFrameAgeMs,
            FlashbackVideoQueueLastLatencyMs = health.FlashbackVideoQueueLastLatencyMs,
            FlashbackVideoQueueLatencySampleCount = health.FlashbackVideoQueueLatencySampleCount,
            FlashbackVideoQueueLatencyAvgMs = health.FlashbackVideoQueueLatencyAvgMs,
            FlashbackVideoQueueLatencyP95Ms = health.FlashbackVideoQueueLatencyP95Ms,
            FlashbackVideoQueueLatencyMaxMs = health.FlashbackVideoQueueLatencyMaxMs,
            FlashbackVideoBackpressureWaitMs = health.FlashbackVideoBackpressureWaitMs,
            FlashbackVideoBackpressureEvents = health.FlashbackVideoBackpressureEvents,
            FlashbackVideoBackpressureLastWaitMs = health.FlashbackVideoBackpressureLastWaitMs,
            FlashbackVideoBackpressureMaxWaitMs = health.FlashbackVideoBackpressureMaxWaitMs,
            FlashbackGpuQueueDepth = health.FlashbackGpuQueueDepth,
            FlashbackGpuQueueCapacity = health.FlashbackGpuQueueCapacity,
            FlashbackGpuQueueMaxDepth = health.FlashbackGpuQueueMaxDepth,
            FlashbackGpuFramesEnqueued = health.FlashbackGpuFramesEnqueued,
            FlashbackGpuFramesDropped = health.FlashbackGpuFramesDropped,
            AudioDropsQueueSaturated = health.AudioDropsQueueSaturated,
            AudioDropsBacklogEviction = health.AudioDropsBacklogEviction,
            AudioChunksDropped = health.AudioChunksDropped,
            AudioQueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,
            AudioQueueDropsFileWriter = health.AudioChunksDropped,
            EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,
            ExpectedCaptureFrameRate = health.ExpectedFrameRate,
            CaptureCadenceSampleCount = health.CaptureCadenceSampleCount,
            CaptureCadenceObservedFps = health.CaptureCadenceObservedFps,
            CaptureCadenceExpectedIntervalMs = health.CaptureCadenceExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = health.CaptureCadenceAverageIntervalMs,
            CaptureCadenceP95IntervalMs = health.CaptureCadenceP95IntervalMs,
            CaptureCadenceMaxIntervalMs = health.CaptureCadenceMaxIntervalMs,
            CaptureCadenceJitterStdDevMs = health.CaptureCadenceJitterStdDevMs,
            CaptureCadenceSevereGapCount = health.CaptureCadenceSevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent,
            MjpegDecodeSampleCount = health.MjpegDecodeSampleCount,
            MjpegDecodeAvgMs = health.MjpegDecodeAvgMs,
            MjpegDecodeP95Ms = health.MjpegDecodeP95Ms,
            MjpegDecodeMaxMs = health.MjpegDecodeMaxMs,
            MjpegInteropCopySampleCount = health.MjpegInteropCopySampleCount,
            MjpegInteropCopyAvgMs = health.MjpegInteropCopyAvgMs,
            MjpegInteropCopyP95Ms = health.MjpegInteropCopyP95Ms,
            MjpegInteropCopyMaxMs = health.MjpegInteropCopyMaxMs,
            MjpegCallbackSampleCount = health.MjpegCallbackSampleCount,
            MjpegCallbackAvgMs = health.MjpegCallbackAvgMs,
            MjpegCallbackP95Ms = health.MjpegCallbackP95Ms,
            MjpegCallbackMaxMs = health.MjpegCallbackMaxMs,
            MjpegDecoderCount = health.MjpegDecoderCount,
            MjpegReorderSampleCount = health.MjpegReorderSampleCount,
            MjpegReorderAvgMs = health.MjpegReorderAvgMs,
            MjpegReorderP95Ms = health.MjpegReorderP95Ms,
            MjpegReorderMaxMs = health.MjpegReorderMaxMs,
            MjpegPipelineSampleCount = health.MjpegPipelineSampleCount,
            MjpegPipelineAvgMs = health.MjpegPipelineAvgMs,
            MjpegPipelineP95Ms = health.MjpegPipelineP95Ms,
            MjpegPipelineMaxMs = health.MjpegPipelineMaxMs,
            MjpegTotalDecoded = health.MjpegTotalDecoded,
            MjpegTotalEmitted = health.MjpegTotalEmitted,
            MjpegTotalDropped = health.MjpegTotalDropped,
            MjpegCompressedFramesQueued = health.MjpegCompressedFramesQueued,
            MjpegCompressedFramesDequeued = health.MjpegCompressedFramesDequeued,
            MjpegCompressedDropsQueueFull = health.MjpegCompressedDropsQueueFull,
            MjpegCompressedDropsByteBudget = health.MjpegCompressedDropsByteBudget,
            MjpegCompressedDropsDisposed = health.MjpegCompressedDropsDisposed,
            MjpegDecodeFailures = health.MjpegDecodeFailures,
            MjpegReorderCollisions = health.MjpegReorderCollisions,
            MjpegEmitFailures = health.MjpegEmitFailures,
            MjpegCompressedQueueDepth = health.MjpegCompressedQueueDepth,
            MjpegCompressedQueueBytes = health.MjpegCompressedQueueBytes,
            MjpegCompressedQueueByteBudget = health.MjpegCompressedQueueByteBudget,
            MjpegReorderSkips = health.MjpegReorderSkips,
            MjpegReorderBufferDepth = health.MjpegReorderBufferDepth,
            MjpegPreviewJitterEnabled = health.MjpegPreviewJitterEnabled,
            MjpegPreviewJitterTargetDepth = health.MjpegPreviewJitterTargetDepth,
            MjpegPreviewJitterMaxDepth = health.MjpegPreviewJitterMaxDepth,
            MjpegPreviewJitterQueueDepth = health.MjpegPreviewJitterQueueDepth,
            MjpegPreviewJitterTotalQueued = health.MjpegPreviewJitterTotalQueued,
            MjpegPreviewJitterTotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            MjpegPreviewJitterTotalDropped = health.MjpegPreviewJitterTotalDropped,
            MjpegPreviewJitterUnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            MjpegPreviewJitterInputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            MjpegPreviewJitterInputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            MjpegPreviewJitterInputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            MjpegPreviewJitterInputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            MjpegPreviewJitterOutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            MjpegPreviewJitterOutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            MjpegPreviewJitterOutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            MjpegPreviewJitterOutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            MjpegPreviewJitterLatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            MjpegPreviewJitterLatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            MjpegPreviewJitterLatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs,
            MjpegPreviewJitterDeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            MjpegPreviewJitterTargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount,
            MjpegPacketHashSampleCount = health.MjpegPacketHashSampleCount,
            MjpegPacketHashUniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            MjpegPacketHashDuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            MjpegPacketHashLongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            MjpegPacketHashInputObservedFps = health.MjpegPacketHashInputObservedFps,
            MjpegPacketHashUniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            MjpegPacketHashLastHash = health.MjpegPacketHashLastHash,
            MjpegPacketHashLastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            MjpegPacketHashPattern = health.MjpegPacketHashPattern,
            MjpegPacketHashRecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            MjpegPacketHashRecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            MjpegPacketHashRecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags,
            VisualCadenceSampleCount = health.VisualCadenceSampleCount,
            VisualCadenceChangedFrameCount = health.VisualCadenceChangedFrameCount,
            VisualCadenceRepeatFrameCount = health.VisualCadenceRepeatFrameCount,
            VisualCadenceLongestRepeatRun = health.VisualCadenceLongestRepeatRun,
            VisualCadenceOutputObservedFps = health.VisualCadenceOutputObservedFps,
            VisualCadenceChangeObservedFps = health.VisualCadenceChangeObservedFps,
            VisualCadenceRepeatFramePercent = health.VisualCadenceRepeatFramePercent,
            VisualCadenceLastDelta = health.VisualCadenceLastDelta,
            VisualCadenceAverageDelta = health.VisualCadenceAverageDelta,
            VisualCadenceP95Delta = health.VisualCadenceP95Delta,
            VisualCadenceMotionScore = health.VisualCadenceMotionScore,
            VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,
            VisualCadenceRecentOutputIntervalsMs = health.VisualCadenceRecentOutputIntervalsMs,
            VisualCadenceRecentChangeIntervalsMs = health.VisualCadenceRecentChangeIntervalsMs,
            VisualCenterCadenceSampleCount = health.VisualCenterCadenceSampleCount,
            VisualCenterCadenceChangedFrameCount = health.VisualCenterCadenceChangedFrameCount,
            VisualCenterCadenceRepeatFrameCount = health.VisualCenterCadenceRepeatFrameCount,
            VisualCenterCadenceLongestRepeatRun = health.VisualCenterCadenceLongestRepeatRun,
            VisualCenterCadenceOutputObservedFps = health.VisualCenterCadenceOutputObservedFps,
            VisualCenterCadenceChangeObservedFps = health.VisualCenterCadenceChangeObservedFps,
            VisualCenterCadenceRepeatFramePercent = health.VisualCenterCadenceRepeatFramePercent,
            VisualCenterCadenceLastDelta = health.VisualCenterCadenceLastDelta,
            VisualCenterCadenceAverageDelta = health.VisualCenterCadenceAverageDelta,
            VisualCenterCadenceP95Delta = health.VisualCenterCadenceP95Delta,
            VisualCenterCadenceMotionScore = health.VisualCenterCadenceMotionScore,
            VisualCenterCadenceMotionConfidence = health.VisualCenterCadenceMotionConfidence,
            VisualCenterCadenceRecentOutputIntervalsMs = health.VisualCenterCadenceRecentOutputIntervalsMs,
            VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,
            MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder
                ? Array.ConvertAll(
                    perDecoder,
                    worker => new MjpegDecoderAutomationSnapshot(
                        worker.WorkerIndex,
                        worker.SampleCount,
                        worker.AvgMs,
                        worker.P95Ms,
                        worker.MaxMs))
                : Array.Empty<MjpegDecoderAutomationSnapshot>(),
            RecordingVideoBytes = recordingStats.VideoBytes,
            RecordingAudioBytes = recordingStats.AudioBytes,
            RecordingTotalBytes = recordingStats.TotalBytes,
            RecordingFileGrowing = recordingFileGrowing,
            LastOutputPath = captureRuntime.LastOutputPath,
            LastFinalizeStatus = captureRuntime.LastFinalizeStatus,
            LastFinalizeUtc = captureRuntime.LastFinalizeUtc,
            LastOutputExists = lastOutputExists,
            LastOutputSizeBytes = lastOutputSize,
            LastVerification = lastVerification,
            HdrTruthVerdict = hdrTruthVerdict,
            MemoryWorkingSetMb = memoryWorkingSetMb,
            MemoryPrivateBytesMb = memoryPrivateBytesMb,
            MemoryManagedHeapMb = memoryManagedHeapMb,
            MemoryTotalAllocatedMb = memoryTotalAllocatedMb,
            MemoryGcHeapSizeMb = memoryGcHeapSizeMb,
            MemoryGcGen0Collections = gcGen0,
            MemoryGcGen1Collections = gcGen1,
            MemoryGcGen2Collections = gcGen2,
            MemoryGcPauseTimePercent = gcPauseTimePercent,
            MemoryGcFragmentationPercent = gcFragmentationPercent,
            ThreadPoolWorkerAvailable = tpWorkerAvailable,
            ThreadPoolWorkerMax = tpWorkerMax,
            ThreadPoolIoAvailable = tpIoAvailable,
            ThreadPoolIoMax = tpIoMax,
            AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs = captureRuntime.AvSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,
            FlashbackActive = health.FlashbackActive,
            FlashbackBufferedDurationMs = health.FlashbackBufferedDurationMs,
            FlashbackDiskBytes = health.FlashbackDiskBytes,
            FlashbackOutputBytes = health.FlashbackOutputBytes,
            FlashbackFilePath = health.FlashbackFilePath,
            FlashbackEncodedFrames = health.FlashbackEncodedFrames,
            FlashbackDroppedFrames = health.FlashbackDroppedFrames,
            FlashbackGpuEncoding = health.FlashbackGpuEncoding,
            EncoderCodecName = health.EncoderCodecName,
            EncoderTargetBitRate = health.EncoderTargetBitRate,
            EncoderWidth = health.EncoderWidth,
            EncoderHeight = health.EncoderHeight,
            EncoderFrameRate = health.EncoderFrameRate,
            FlashbackVideoQueueDepth = health.FlashbackVideoQueueDepth,
            FlashbackAudioQueueDepth = health.FlashbackAudioQueueDepth,
            FlashbackPlaybackState = health.FlashbackPlaybackState,
            FlashbackPlaybackPositionMs = health.FlashbackPlaybackPositionMs,
            FlashbackDecoderHwAccel = health.FlashbackDecoderHwAccel,
            FlashbackPlaybackFrameCount = health.FlashbackPlaybackFrameCount,
            FlashbackPlaybackLateFrames = health.FlashbackPlaybackLateFrames,
            FlashbackPlaybackObservedFps = health.FlashbackPlaybackObservedFps,
            FlashbackPlaybackAvgFrameMs = health.FlashbackPlaybackAvgFrameMs,
            FlashbackAvDriftMs = health.FlashbackAvDriftMs,
            LastExportPath = health.LastExportPath,
            LastExportSuccess = health.LastExportSuccess,
            LastExportMessage = health.LastExportMessage
        };

        var verificationIdle = Volatile.Read(ref _verificationInProgress) == 0 &&
                               Volatile.Read(ref _autoVerificationScheduled) == 0;
        var shouldAutoVerify = !snapshot.IsRecording &&
                               _wasRecording &&
                               !string.IsNullOrWhiteSpace(snapshot.LastOutputPath) &&
                               verificationIdle;

        UpdateAlerts(snapshot);

        lock (_stateLock)
        {
            _latestSnapshot = snapshot;

            _timelineBuffer[_timelineHead] = new PerformanceTimelineEntry
            {
                TimestampUtc = snapshot.TimestampUtc,
                CaptureFps = snapshot.CaptureCadenceObservedFps,
                PreviewFps = snapshot.PreviewCadenceObservedFps,
                VideoQueueDepth = snapshot.FfmpegVideoQueueDepth,
                VideoDrops = snapshot.VideoDropsQueueSaturated,
                CaptureCadenceP95Ms = snapshot.CaptureCadenceP95IntervalMs,
                PipelineLatencyMs = snapshot.EstimatedPipelineLatencyMs,
                MemoryWorkingSetMb = snapshot.MemoryWorkingSetMb,
                MemoryManagedHeapMb = snapshot.MemoryManagedHeapMb,
                GcGen0Collections = snapshot.MemoryGcGen0Collections,
                GcGen1Collections = snapshot.MemoryGcGen1Collections,
                GcGen2Collections = snapshot.MemoryGcGen2Collections,
                GcPauseTimePercent = snapshot.MemoryGcPauseTimePercent,
                ThreadPoolWorkerAvailable = snapshot.ThreadPoolWorkerAvailable,
                ThreadPoolIoAvailable = snapshot.ThreadPoolIoAvailable
            };
            _timelineHead = (_timelineHead + 1) % TimelineCapacity;
            if (_timelineCount < TimelineCapacity)
            {
                _timelineCount++;
            }
        }

        SnapshotUpdated?.Invoke(this, snapshot);
        _wasRecording = snapshot.IsRecording;

        if (shouldAutoVerify &&
            _cts is { IsCancellationRequested: false } cts &&
            Interlocked.CompareExchange(ref _autoVerificationScheduled, 1, 0) == 0)
        {
            AddEvent(
                DiagnosticsSeverity.Info,
                DiagnosticsCategory.Verification,
                "Automatic recording verification started.");
            _autoVerificationTask = Task.Run(async () =>
            {
                try
                {
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }

                    await VerifyLastRecordingAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    /* Expected during shutdown — auto-verification cancelled */
                }
                catch (Exception ex)
                {
                    AddEvent(
                        DiagnosticsSeverity.Error,
                        DiagnosticsCategory.Verification,
                        $"Automatic recording verification failed: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _autoVerificationScheduled, 0);
                }
            });
        }

        return snapshot;
    }

    private void UpdateAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "preview-blank",
            snapshot.PreviewBlankSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            "Preview appears active but no frames are being displayed.",
            "Preview blank condition cleared.");

        SetAlertState(
            "preview-stall",
            snapshot.PreviewStalled,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            "Preview frame flow appears stalled.",
            "Preview stall condition cleared.");

        var startupTimeoutMs = snapshot.PreviewStartupTimeoutMs > 0 ? snapshot.PreviewStartupTimeoutMs : 2000;
        SetAlertState(
            "preview-startup-timeout",
            snapshot.IsPreviewing &&
            !snapshot.PreviewFirstVisualConfirmed &&
            string.Equals(snapshot.PreviewStartupState, "WaitingForFirstVisual", StringComparison.OrdinalIgnoreCase) &&
            snapshot.PreviewStartupElapsedMs.GetValueOrDefault() >= startupTimeoutMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            string.IsNullOrWhiteSpace(snapshot.PreviewStartupMissingSignals)
                ? $"Preview startup waiting for first visual beyond {startupTimeoutMs}ms (attempt={snapshot.PreviewAttemptId ?? "none"})."
                : $"Preview startup waiting for first visual beyond {startupTimeoutMs}ms (attempt={snapshot.PreviewAttemptId ?? "none"}, missing={snapshot.PreviewStartupMissingSignals}).",
            "Preview startup visual confirmation recovered.");

        SetAlertState(
            "preview-startup-failed",
            string.Equals(snapshot.PreviewStartupState, "Failed", StringComparison.OrdinalIgnoreCase),
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Preview,
            string.IsNullOrWhiteSpace(snapshot.PreviewLastFailureReason)
                ? string.IsNullOrWhiteSpace(snapshot.PreviewStartupMissingSignals)
                    ? "Preview startup failed before first visual confirmation."
                    : $"Preview startup failed (missing={snapshot.PreviewStartupMissingSignals})."
                : $"Preview startup failed: {snapshot.PreviewLastFailureReason}",
            "Preview startup failure cleared.");

        SetAlertState(
            "preview-cadence-slow",
            snapshot.PreviewCadenceSampleCount >= 60 && snapshot.PreviewCadenceSlowFramePercent >= 8.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            $"Preview cadence degraded: slowFrames={snapshot.PreviewCadenceSlowFramePercent:0.##}% " +
            $"p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms expected={snapshot.PreviewCadenceExpectedIntervalMs:0.##}ms.",
            "Preview cadence returned to healthy range.");

        SetAlertState(
            "audio-muted-suspect",
            snapshot.AudioMutedSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Audio,
            "Audio is enabled but sustained low signal suggests muted or disconnected input.",
            "Audio signal recovered.");

        SetAlertState(
            "recording-not-growing",
            snapshot.IsRecording && !snapshot.RecordingFileGrowing,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Recording,
            "Recording is active but output bytes are not increasing.",
            "Recording output growth resumed.");

        SetAlertState(
            "capture-cadence-drop",
            snapshot.CaptureCadenceSampleCount >= 120 && snapshot.CaptureCadenceEstimatedDropPercent >= 1.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence drop estimate={snapshot.CaptureCadenceEstimatedDropPercent:0.##}% " +
            $"(estDropped={snapshot.CaptureCadenceEstimatedDroppedFrames}, severeGaps={snapshot.CaptureCadenceSevereGapCount}).",
            "Capture cadence drop estimate returned to healthy range.");

        SetAlertState(
            "hdr-parity-mismatch",
            snapshot.LastVerification?.HdrParity is { Requested: true, Verified: false },
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Verification,
            $"HDR parity mismatch: {snapshot.LastVerification?.HdrParity?.Status ?? "Unknown"}",
            "HDR parity mismatch cleared.");

        SetAlertState(
            "pipeline-mode-violation",
            snapshot.IsRecording && !snapshot.PipelineModeMatched,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Capture,
            string.IsNullOrWhiteSpace(snapshot.PipelineModeReason)
                ? $"Pipeline mode violation: requested={snapshot.RequestedPipelineMode}, active={snapshot.ActivePipelineMode}."
                : $"Pipeline mode violation: {snapshot.PipelineModeReason}",
            "Pipeline mode contract restored.");

        if (!snapshot.IsRecording && _wasRecording)
        {
            AddEvent(
                DiagnosticsSeverity.Info,
                DiagnosticsCategory.Recording,
                $"Recording stopped with status: {snapshot.LastFinalizeStatus}");
        }

        SetAlertState(
            "performance-perfection-not-met",
            !snapshot.PerformancePerfectionMet,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.System,
            $"Performance below perfection threshold (score={snapshot.PerformanceScore:0.##}): {snapshot.PerformanceSummary}",
            "Performance returned to perfection threshold.",
            throttleMs: 5000);
    }

    private readonly record struct PerformanceEvaluation(double Score, bool PerfectionMet, string Summary);

    private PerformanceEvaluation EvaluatePerformance(
        bool isPreviewing,
        bool isRecording,
        bool recordingFileGrowing,
        bool previewGpuActive,
        bool previewBlankSuspected,
        bool previewStalled,
        int previewCadenceSampleCount,
        double previewCadenceSlowFramePercent,
        int captureCadenceSampleCount,
        double captureCadenceExpectedIntervalMs,
        double captureCadenceP95IntervalMs,
        double captureCadenceDropPercent,
        RecordingVerificationResult? lastVerification)
    {
        var reasons = new List<string>();
        var penalty = 0.0;

        if (previewBlankSuspected || previewStalled)
        {
            penalty += 40;
            reasons.Add("preview health degraded (blank/stalled)");
        }

        if (isRecording && !recordingFileGrowing)
        {
            penalty += 25;
            reasons.Add("recording file growth stalled");
        }

        if (captureCadenceSampleCount >= CapturePerfectionMinSamples)
        {
            if (captureCadenceDropPercent > _perfectionCaptureDropPercentThreshold)
            {
                var over = captureCadenceDropPercent - _perfectionCaptureDropPercentThreshold;
                penalty += Math.Min(35, over * 6.0);
                reasons.Add($"capture drop {captureCadenceDropPercent:0.###}%");
            }

            if (captureCadenceExpectedIntervalMs > 0 && captureCadenceP95IntervalMs > 0)
            {
                var p95Ratio = captureCadenceP95IntervalMs / captureCadenceExpectedIntervalMs;
                if (p95Ratio > _perfectionCaptureP95MultiplierThreshold)
                {
                    penalty += Math.Min(25, (p95Ratio - _perfectionCaptureP95MultiplierThreshold) * 45.0);
                    reasons.Add($"capture p95 ratio {p95Ratio:0.###}x");
                }
            }
        }
        else if (isRecording)
        {
            penalty += 5;
            reasons.Add("capture cadence samples insufficient");
        }

        if (isPreviewing && !previewGpuActive && previewCadenceSampleCount >= PreviewPerfectionMinSamples)
        {
            if (previewCadenceSlowFramePercent > _perfectionPreviewSlowPercentThreshold)
            {
                var over = previewCadenceSlowFramePercent - _perfectionPreviewSlowPercentThreshold;
                penalty += Math.Min(20, over * 2.0);
                reasons.Add($"preview slow frames {previewCadenceSlowFramePercent:0.###}%");
            }
        }

        if (lastVerification is { CadenceSampleCount: >= VerificationPerfectionMinSamples } verification &&
            verification.CadenceEstimatedDropPercent.GetValueOrDefault() > _perfectionVerificationDropPercentThreshold)
        {
            var verifyDrop = verification.CadenceEstimatedDropPercent.GetValueOrDefault();
            var over = verifyDrop - _perfectionVerificationDropPercentThreshold;
            penalty += Math.Min(25, over * 4.0);
            reasons.Add($"file cadence drop {verifyDrop:0.###}%");
        }

        if (lastVerification != null && !lastVerification.Succeeded)
        {
            penalty += 20;
            reasons.Add("verification failed");
        }

        var score = Math.Clamp(100.0 - penalty, 0.0, 100.0);
        var perfectionMet = reasons.Count == 0 && score >= 99.0;
        var summary = reasons.Count == 0
            ? "Perfection thresholds satisfied."
            : string.Join(", ", reasons.Take(4));

        return new PerformanceEvaluation(score, perfectionMet, summary);
    }

    private void AddEventThrottled(
        string key,
        DiagnosticsSeverity severity,
        DiagnosticsCategory category,
        string message,
        int throttleMs = 3000)
    {
        var nowTick = Environment.TickCount64;
        lock (_stateLock)
        {
            if (_eventThrottleTicks.TryGetValue(key, out var lastTick) && nowTick - lastTick < throttleMs)
            {
                return;
            }

            _eventThrottleTicks[key] = nowTick;
        }

        AddEvent(severity, category, message);
    }

    private void SetAlertState(
        string key,
        bool active,
        DiagnosticsSeverity activeSeverity,
        DiagnosticsCategory category,
        string activeMessage,
        string resolvedMessage,
        int throttleMs = 3000)
    {
        bool shouldEmitResolved;
        lock (_stateLock)
        {
            var wasActive = _activeAlerts.Contains(key);
            if (active)
            {
                _activeAlerts.Add(key);
                shouldEmitResolved = false;
            }
            else
            {
                shouldEmitResolved = wasActive;
                _activeAlerts.Remove(key);
                _eventThrottleTicks.Remove(key);
            }
        }

        if (active)
        {
            AddEventThrottled(key, activeSeverity, category, activeMessage, throttleMs);
            return;
        }

        if (shouldEmitResolved)
        {
            AddEvent(DiagnosticsSeverity.Info, category, resolvedMessage);
        }
    }

    private void AddEvent(DiagnosticsSeverity severity, DiagnosticsCategory category, string message, string? correlationId = null)
    {
        var evt = new DiagnosticsEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Severity = severity,
            Category = category,
            Message = message,
            CorrelationId = correlationId
        };

        lock (_stateLock)
        {
            _recentEvents.Add(evt);
            if (_recentEvents.Count > MaxRecentEvents)
            {
                _recentEvents.RemoveRange(0, _recentEvents.Count - MaxRecentEvents);
            }
        }
    }

    private static bool IsHdrSubtype(string? subtype)
        => MediaFormat.IsHdrPixelFormat(subtype);

    private static HdrTruthVerdict BuildHdrTruthVerdict(
        CaptureRuntimeSnapshot captureRuntime,
        bool hdrEnabledInUi,
        RecordingVerificationResult? lastVerification)
    {
        static string NormalizeFormatToken(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "unknown";
            }

            var value = text.Trim();
            if (value.Contains("P010", StringComparison.OrdinalIgnoreCase))
            {
                return "P010";
            }

            if (value.Contains("NV12", StringComparison.OrdinalIgnoreCase))
            {
                return "NV12";
            }

            return value.ToUpperInvariant();
        }

        var evidence = new List<string>(capacity: 8);
        var observedFormatToken = NormalizeFormatToken(
            captureRuntime.LatestObservedFramePixelFormat ??
            captureRuntime.FirstObservedFramePixelFormat ??
            captureRuntime.NegotiatedPixelFormat);
        var hasP010 = captureRuntime.ObservedP010FrameCount > 0 || string.Equals(observedFormatToken, "P010", StringComparison.OrdinalIgnoreCase);
        var hasNv12 = captureRuntime.ObservedNv12FrameCount > 0 || string.Equals(observedFormatToken, "NV12", StringComparison.OrdinalIgnoreCase);
        var pipelineFormat = hasP010
            ? "P010"
            : hasNv12
                ? "NV12"
                : observedFormatToken;

        if (hasP010)
        {
            evidence.Add($"observed-p010-frames={captureRuntime.ObservedP010FrameCount}");
        }
        if (hasNv12)
        {
            evidence.Add($"observed-nv12-frames={captureRuntime.ObservedNv12FrameCount}");
        }

        string effectiveBitDepth;
        if (string.Equals(pipelineFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            effectiveBitDepth = "8bit-like";
        }
        else if (string.Equals(pipelineFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            if (captureRuntime.ObservedP010Likely8BitUpscaled == true)
            {
                effectiveBitDepth = "8bit-like";
                evidence.Add("p010-samples-look-upscaled-8bit=true");
            }
            else if (captureRuntime.ObservedP010BitDepthSampleCount > 0)
            {
                effectiveBitDepth = captureRuntime.ObservedP010Low2BitNonZeroPercent >= 0.50
                    ? "10bit"
                    : "8bit-like";
                evidence.Add(
                    $"p010-low2-nonzero-pct={captureRuntime.ObservedP010Low2BitNonZeroPercent:0.###} (samples={captureRuntime.ObservedP010BitDepthSampleCount})");
            }
            else
            {
                effectiveBitDepth = "unknown";
                evidence.Add("p010-bitdepth-samples=0");
            }
        }
        else
        {
            effectiveBitDepth = "unknown";
        }

        string metadataState;
        if (lastVerification is null)
        {
            metadataState = "unknown";
            evidence.Add("metadata=verification-not-run");
        }
        else if (lastVerification.HdrColorimetryValid == false)
        {
            metadataState = "invalid";
            evidence.Add("metadata=colorimetry-invalid");
        }
        else if (lastVerification.HdrMetadataPresent == true)
        {
            metadataState = "present-valid";
            evidence.Add("metadata=present-valid");
        }
        else if (lastVerification.HdrMetadataPresent == false)
        {
            metadataState = "missing";
            evidence.Add("metadata=missing");
        }
        else
        {
            metadataState = "unknown";
            evidence.Add("metadata=unknown");
        }

        var captureHdrLike =
            string.Equals(pipelineFormat, "P010", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(effectiveBitDepth, "10bit", StringComparison.OrdinalIgnoreCase);
        var sourceHdr = captureRuntime.SourceIsHdr;
        string sourceVsCaptureParity;
        if (!sourceHdr.HasValue)
        {
            sourceVsCaptureParity = "unknown";
        }
        else if (sourceHdr.Value == captureHdrLike)
        {
            sourceVsCaptureParity = "match";
        }
        else
        {
            sourceVsCaptureParity = "mismatch";
            evidence.Add($"source-hdr={sourceHdr.Value}, capture-hdr-like={captureHdrLike}");
        }

        var finalClassification = pipelineFormat switch
        {
            "NV12" => "sdr-8bit",
            "P010" when string.Equals(effectiveBitDepth, "10bit", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(metadataState, "present-valid", StringComparison.OrdinalIgnoreCase)
                => "true-hdr10",
            "P010" => "p010-sdr",
            _ => "inconclusive"
        };

        if (hdrEnabledInUi && string.Equals(finalClassification, "sdr-8bit", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add("hdr-enabled-ui-while-effective-path-is-sdr-8bit");
        }

        return new HdrTruthVerdict
        {
            PipelineFormat = pipelineFormat,
            EffectiveBitDepth = effectiveBitDepth,
            HdrMetadataState = metadataState,
            SourceVsCaptureParity = sourceVsCaptureParity,
            FinalClassification = finalClassification,
            Evidence = evidence
        };
    }

    private static int? ResolveTelemetryAgeSeconds(int? reportedAgeSeconds, DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (reportedAgeSeconds.HasValue)
        {
            return Math.Max(0, reportedAgeSeconds.Value);
        }

        if (!timestampUtc.HasValue)
        {
            return null;
        }

        var age = nowUtc - timestampUtc.Value;
        if (age < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Floor(age.TotalSeconds);
    }

}

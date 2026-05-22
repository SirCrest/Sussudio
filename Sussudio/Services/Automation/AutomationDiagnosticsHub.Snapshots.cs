using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    public AutomationSnapshot GetLatestSnapshot()
    {
        lock (_stateLock)
        {
            return _latestSnapshot;
        }
    }

    public Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default)
        => RefreshSnapshotAsync(cancellationToken);

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

        var viewModelSnapshot = await _snapshotQueryPort
            .GetViewModelRuntimeSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var captureRuntime = await _snapshotQueryPort
            .GetCaptureRuntimeSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var health = await _snapshotQueryPort
            .GetCaptureHealthSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var recordingStats = await _snapshotQueryPort
            .GetRecordingStatsSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        var previewRuntime = await _previewSnapshotProvider(cancellationToken).ConfigureAwait(false);

        var nowTick = Environment.TickCount64;
        var recordingStarted = viewModelSnapshot.IsRecording && !_wasRecording;
        var audioSignal = UpdateAudioSignalState(viewModelSnapshot, nowTick);
        var recordingFileGrowing = UpdateRecordingFileGrowthState(
            viewModelSnapshot,
            recordingStats,
            recordingStarted,
            nowTick);

        var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                health.ExpectedFrameRate,
                health.VisualCadenceSampleCount,
                health.VisualCadenceChangeObservedFps,
                health.VisualCadenceRepeatFramePercent,
                health.VisualCadenceLongestRepeatRun);
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
            captureCadenceExpectedFrameRate: health.ExpectedFrameRate,
            captureCadenceOnePercentLowFps: health.CaptureCadenceOnePercentLowFps,
            previewCadenceExpectedIntervalMs: previewRuntime.DisplayCadenceExpectedIntervalMs,
            previewCadenceOnePercentLowFps: previewRuntime.DisplayCadenceOnePercentLowFps,
            visualCadenceHealthy: visualCadenceHealthy,
            captureCadenceDropPercent: health.CaptureCadenceEstimatedDropPercent,
            lastVerification: lastVerification);
        var recentPreviewJitter = UpdatePreviewJitterRecentCounters(health, nowTick);
        var recentMjpeg = UpdateMjpegRecentCounters(health, nowTick);
        var recentRenderer = UpdateD3DRendererRecentCounters(previewRuntime, nowTick);
        var (recentD3DMissedRefreshes, recentD3DStatsFailures) = UpdateD3DFrameStatsRecentCounters(previewRuntime, nowTick);
        var recentD3DFrameLatencyWaitTimeouts = UpdateD3DFrameLatencyWaitRecentCounters(previewRuntime, nowTick);
        var recentFlashbackRecording = UpdateFlashbackRecordingRecentCounters(health, nowTick);
        var diagnostic = BuildDiagnosticEvaluation(
            health,
            captureRuntime,
            previewRuntime,
            viewModelSnapshot.IsPreviewing,
            viewModelSnapshot.IsRecording,
            performance,
            recentMjpeg,
            recentPreviewJitter.Underflows,
            recentPreviewJitter.DeadlineDrops,
            recentRenderer,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures,
            recentFlashbackRecording);
        var previewPacingClassification = ClassifyPreviewPacing(
            viewModelSnapshot,
            health,
            previewRuntime,
            recentMjpeg,
            recentPreviewJitter,
            recentRenderer,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures,
            recentD3DFrameLatencyWaitTimeouts);
        var hdrTruthVerdict = BuildHdrTruthVerdict(captureRuntime, viewModelSnapshot.IsHdrEnabled, lastVerification);
        var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);

        var lastOutput = ProbeLastOutput(captureRuntime.LastOutputPath, viewModelSnapshot.IsRecording);
        var processResources = CaptureProcessResourceSnapshot();
        var snapshot = BuildAutomationSnapshot(
            viewModelSnapshot,
            captureRuntime,
            health,
            recordingStats,
            previewRuntime,
            performance,
            diagnostic,
            previewPacingClassification,
            previewHdrState,
            audioSignal,
            recordingFileGrowing,
            hdrTruthVerdict,
            lastOutput,
            processResources,
            lastVerification,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);

        UpdateAlerts(snapshot, recentFlashbackRecording);

        lock (_stateLock)
        {
            _latestSnapshot = snapshot;
            AppendPerformanceTimelineEntry(snapshot);
        }

        SnapshotUpdated?.Invoke(this, snapshot);
        _wasRecording = snapshot.IsRecording;

        ScheduleAutoVerificationIfNeeded(shouldAutoVerify);

        return snapshot;
    }

    private LastOutputProbe ProbeLastOutput(string? lastOutputPath, bool isRecording)
    {
        if (string.IsNullOrWhiteSpace(lastOutputPath))
        {
            _cachedFinalOutputSize = null;
            _cachedFinalOutputPath = null;
            return LastOutputProbe.Empty;
        }

        // While recording, the file is still growing, so re-stat each poll.
        // Once recording stops, the size is final and cached until the path changes.
        var isFinalAndCached = !isRecording &&
                               _cachedFinalOutputSize.HasValue &&
                               string.Equals(_cachedFinalOutputPath, lastOutputPath, StringComparison.Ordinal);
        if (isFinalAndCached)
        {
            return new LastOutputProbe(true, _cachedFinalOutputSize);
        }

        try
        {
            var size = new FileInfo(lastOutputPath).Length;
            if (!isRecording)
            {
                _cachedFinalOutputSize = size;
                _cachedFinalOutputPath = lastOutputPath;
            }

            return new LastOutputProbe(true, size);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub output file probe: {ex.Message}");
            return LastOutputProbe.Empty;
        }
    }

    private readonly record struct LastOutputProbe(bool Exists, long? SizeBytes)
    {
        public static LastOutputProbe Empty { get; } = new(false, null);
    }

    private ProcessResourceSnapshot CaptureProcessResourceSnapshot()
    {
        // Memory & GC metrics are thread-safe and microsecond-cheap.
        _currentProcess.Refresh();
        var processCpuTotalMs = _currentProcess.TotalProcessorTime.TotalMilliseconds;
        var processCpuPercent = CalculateProcessCpuPercent(processCpuTotalMs);
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        ThreadPool.GetAvailableThreads(out var threadPoolWorkerAvailable, out var threadPoolIoAvailable);
        ThreadPool.GetMaxThreads(out var threadPoolWorkerMax, out var threadPoolIoMax);

        return new ProcessResourceSnapshot(
            MemoryWorkingSetMb: _currentProcess.WorkingSet64 / (1024.0 * 1024.0),
            MemoryPrivateBytesMb: _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0),
            MemoryManagedHeapMb: GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            MemoryTotalAllocatedMb: GC.GetTotalAllocatedBytes(precise: false) / (1024.0 * 1024.0),
            ProcessCpuPercent: processCpuPercent,
            ProcessCpuTotalProcessorTimeMs: processCpuTotalMs,
            MemoryGcHeapSizeMb: gcMemoryInfo.HeapSizeBytes / (1024.0 * 1024.0),
            MemoryGcGen0Collections: GC.CollectionCount(0),
            MemoryGcGen1Collections: GC.CollectionCount(1),
            MemoryGcGen2Collections: GC.CollectionCount(2),
            MemoryGcPauseTimePercent: gcMemoryInfo.PauseTimePercentage,
            MemoryGcFragmentationPercent: gcMemoryInfo.HeapSizeBytes > 0
                ? gcMemoryInfo.FragmentedBytes * 100.0 / gcMemoryInfo.HeapSizeBytes
                : 0.0,
            ThreadPoolWorkerAvailable: threadPoolWorkerAvailable,
            ThreadPoolWorkerMax: threadPoolWorkerMax,
            ThreadPoolIoAvailable: threadPoolIoAvailable,
            ThreadPoolIoMax: threadPoolIoMax);
    }

    private double CalculateProcessCpuPercent(double processCpuTotalMs)
    {
        var nowTimestamp = Stopwatch.GetTimestamp();
        var previousTimestamp = _lastProcessCpuSampleTimestamp;
        var previousCpuTotalMs = _lastProcessCpuTotalMs;

        _lastProcessCpuSampleTimestamp = nowTimestamp;
        _lastProcessCpuTotalMs = processCpuTotalMs;

        if (previousTimestamp <= 0)
        {
            return 0.0;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(previousTimestamp, nowTimestamp).TotalMilliseconds;
        if (elapsedMs <= 0)
        {
            return 0.0;
        }

        var cpuDeltaMs = Math.Max(0.0, processCpuTotalMs - previousCpuTotalMs);
        var cpuCapacityMs = elapsedMs * Math.Max(1, Environment.ProcessorCount);
        return Math.Clamp(cpuDeltaMs * 100.0 / cpuCapacityMs, 0.0, 100.0);
    }

    private readonly record struct ProcessResourceSnapshot(
        double MemoryWorkingSetMb,
        double MemoryPrivateBytesMb,
        double MemoryManagedHeapMb,
        double MemoryTotalAllocatedMb,
        double ProcessCpuPercent,
        double ProcessCpuTotalProcessorTimeMs,
        double MemoryGcHeapSizeMb,
        int MemoryGcGen0Collections,
        int MemoryGcGen1Collections,
        int MemoryGcGen2Collections,
        double MemoryGcPauseTimePercent,
        double MemoryGcFragmentationPercent,
        int ThreadPoolWorkerAvailable,
        int ThreadPoolWorkerMax,
        int ThreadPoolIoAvailable,
        int ThreadPoolIoMax);

    private AudioSignalState UpdateAudioSignalState(ViewModelRuntimeSnapshot viewModelSnapshot, long nowTick)
    {
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

        return new AudioSignalState(audioSignalPresent, audioMutedSuspected);
    }

    private bool UpdateRecordingFileGrowthState(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        RecordingStats recordingStats,
        bool recordingStarted,
        long nowTick)
    {
        if (recordingStarted)
        {
            _lastRecordedBytes = recordingStats.TotalBytes;
            _recordingNoGrowthStartTick = 0;
        }

        var totalBytes = recordingStats.TotalBytes;
        if (!viewModelSnapshot.IsRecording)
        {
            _lastRecordedBytes = totalBytes;
            _recordingNoGrowthStartTick = 0;
            return false;
        }

        var recordingFileGrowing = true;
        if (totalBytes > _lastRecordedBytes)
        {
            _recordingNoGrowthStartTick = 0;
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
        return recordingFileGrowing;
    }

    private readonly record struct AudioSignalState(bool SignalPresent, bool MutedSuspected);

}

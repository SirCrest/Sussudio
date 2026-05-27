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

internal readonly record struct D3DRendererRecentCounters(
    long Submitted,
    long Rendered,
    long Dropped)
{
    public static D3DRendererRecentCounters Empty { get; } = new(0, 0, 0);
}

internal readonly record struct MjpegRecentCounters(
    long TotalDropped,
    long DecodeFailures,
    long EmitFailures,
    long CompressedQueueDrops)
{
    public static MjpegRecentCounters Empty { get; } = new(0, 0, 0, 0);

    public long Failures => DecodeFailures + EmitFailures + CompressedQueueDrops;
}

internal readonly record struct PreviewJitterRecentCounters(
    long Dropped,
    long Underflows,
    long DeadlineDrops,
    long ScheduleLateCount,
    double ScheduleLateMs)
{
    public static PreviewJitterRecentCounters Empty { get; } = new(0, 0, 0, 0, 0);
}

internal readonly record struct FlashbackRecordingRecentCounters(
    long DroppedFrames,
    long EncoderDroppedFrames,
    long SequenceGaps,
    long GpuFramesDropped,
    long BackpressureEvents)
{
    public static FlashbackRecordingRecentCounters Empty { get; } = new(0, 0, 0, 0, 0);
}

public sealed partial class AutomationDiagnosticsHub
{
    private long _lastPreviewJitterTotalDropped;
    private long _lastPreviewJitterUnderflows;
    private long _lastPreviewJitterDeadlineDrops;
    private long _lastPreviewJitterScheduleLateCount;
    private double _lastPreviewJitterLastScheduleLateMs;
    private long _lastPreviewJitterEvalTick;
    private long _lastD3DFramesSubmitted;
    private long _lastD3DFramesRendered;
    private long _lastD3DFramesDropped;
    private long _lastD3DRendererEvalTick;
    private long _lastD3DFrameStatsMissedRefreshes;
    private long _lastD3DFrameStatsFailures;
    private long _lastD3DFrameStatsEvalTick;
    private long _lastD3DFrameLatencyWaitTimeouts;
    private long _lastD3DFrameLatencyWaitEvalTick;
    private long _lastMjpegTotalDropped;
    private long _lastMjpegDecodeFailures;
    private long _lastMjpegEmitFailures;
    private long _lastMjpegCompressedDropsQueueFull;
    private long _lastMjpegEvalTick;
    private long _lastFlashbackDroppedFrames;
    private long _lastFlashbackVideoEncoderDroppedFrames;
    private long _lastFlashbackVideoSequenceGaps;
    private long _lastFlashbackGpuFramesDropped;
    private long _lastFlashbackVideoBackpressureEvents;
    private long _lastFlashbackRecordingEvalTick;

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

    private PreviewJitterRecentCounters UpdatePreviewJitterRecentCounters(
        CaptureHealthSnapshot health,
        long nowTick)
    {
        var totalDropped = Math.Max(0, health.MjpegPreviewJitterTotalDropped);
        var underflows = Math.Max(0, health.MjpegPreviewJitterUnderflowCount);
        var deadlineDrops = Math.Max(0, health.MjpegPreviewJitterDeadlineDropCount);
        var scheduleLateCount = Math.Max(0, health.MjpegPreviewJitterScheduleLateCount);
        var lastScheduleLateMs = Math.Max(0, health.MjpegPreviewJitterLastScheduleLateMs);
        var previousTick = Interlocked.Exchange(ref _lastPreviewJitterEvalTick, nowTick);
        var previousTotalDropped = Interlocked.Exchange(ref _lastPreviewJitterTotalDropped, totalDropped);
        var previousUnderflows = Interlocked.Exchange(ref _lastPreviewJitterUnderflows, underflows);
        var previousDeadlineDrops = Interlocked.Exchange(ref _lastPreviewJitterDeadlineDrops, deadlineDrops);
        var previousScheduleLateCount = Interlocked.Exchange(ref _lastPreviewJitterScheduleLateCount, scheduleLateCount);
        var previousLastScheduleLateMs = _lastPreviewJitterLastScheduleLateMs;
        _lastPreviewJitterLastScheduleLateMs = lastScheduleLateMs;

        if (previousTick == 0 || nowTick < previousTick)
        {
            return PreviewJitterRecentCounters.Empty;
        }

        var recentScheduleLateCount = Math.Max(0, scheduleLateCount - previousScheduleLateCount);
        return new PreviewJitterRecentCounters(
            Math.Max(0, totalDropped - previousTotalDropped),
            Math.Max(0, underflows - previousUnderflows),
            Math.Max(0, deadlineDrops - previousDeadlineDrops),
            recentScheduleLateCount,
            recentScheduleLateCount > 0 ? Math.Max(0, lastScheduleLateMs) : Math.Max(0, lastScheduleLateMs - previousLastScheduleLateMs));
    }

    private MjpegRecentCounters UpdateMjpegRecentCounters(
        CaptureHealthSnapshot health,
        long nowTick)
    {
        var totalDropped = Math.Max(0, health.MjpegTotalDropped);
        var decodeFailures = Math.Max(0, health.MjpegDecodeFailures);
        var emitFailures = Math.Max(0, health.MjpegEmitFailures);
        var compressedQueueDrops = Math.Max(0, health.MjpegCompressedDropsQueueFull);
        var previousTick = Interlocked.Exchange(ref _lastMjpegEvalTick, nowTick);
        var previousTotalDropped = Interlocked.Exchange(ref _lastMjpegTotalDropped, totalDropped);
        var previousDecodeFailures = Interlocked.Exchange(ref _lastMjpegDecodeFailures, decodeFailures);
        var previousEmitFailures = Interlocked.Exchange(ref _lastMjpegEmitFailures, emitFailures);
        var previousCompressedQueueDrops = Interlocked.Exchange(ref _lastMjpegCompressedDropsQueueFull, compressedQueueDrops);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return MjpegRecentCounters.Empty;
        }

        return new MjpegRecentCounters(
            Math.Max(0, totalDropped - previousTotalDropped),
            Math.Max(0, decodeFailures - previousDecodeFailures),
            Math.Max(0, emitFailures - previousEmitFailures),
            Math.Max(0, compressedQueueDrops - previousCompressedQueueDrops));
    }

    private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(
        PreviewRuntimeSnapshot previewRuntime,
        long nowTick)
    {
        var submitted = Math.Max(0, previewRuntime.D3DFramesSubmitted);
        var rendered = Math.Max(0, previewRuntime.D3DFramesRendered);
        var dropped = Math.Max(0, previewRuntime.D3DFramesDropped);
        var previousTick = Interlocked.Exchange(ref _lastD3DRendererEvalTick, nowTick);
        var previousSubmitted = Interlocked.Exchange(ref _lastD3DFramesSubmitted, submitted);
        var previousRendered = Interlocked.Exchange(ref _lastD3DFramesRendered, rendered);
        var previousDropped = Interlocked.Exchange(ref _lastD3DFramesDropped, dropped);

        if (previousTick <= 0)
        {
            return D3DRendererRecentCounters.Empty;
        }

        return new D3DRendererRecentCounters(
            Math.Max(0, submitted - previousSubmitted),
            Math.Max(0, rendered - previousRendered),
            Math.Max(0, dropped - previousDropped));
    }

    private (long RecentMissedRefreshes, long RecentFailures) UpdateD3DFrameStatsRecentCounters(
        PreviewRuntimeSnapshot previewRuntime,
        long nowTick)
    {
        var missedRefreshes = Math.Max(0, previewRuntime.D3DFrameStatsMissedRefreshCount);
        var failures = Math.Max(0, previewRuntime.D3DFrameStatsFailureCount);
        var previousTick = Interlocked.Exchange(ref _lastD3DFrameStatsEvalTick, nowTick);
        var previousMissedRefreshes = Interlocked.Exchange(ref _lastD3DFrameStatsMissedRefreshes, missedRefreshes);
        var previousFailures = Interlocked.Exchange(ref _lastD3DFrameStatsFailures, failures);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return (0, 0);
        }

        return (
            Math.Max(0, missedRefreshes - previousMissedRefreshes),
            Math.Max(0, failures - previousFailures));
    }

    private long UpdateD3DFrameLatencyWaitRecentCounters(
        PreviewRuntimeSnapshot previewRuntime,
        long nowTick)
    {
        var timeouts = Math.Max(0, previewRuntime.D3DFrameLatencyWaitTimeoutCount);
        var previousTick = Interlocked.Exchange(ref _lastD3DFrameLatencyWaitEvalTick, nowTick);
        var previousTimeouts = Interlocked.Exchange(ref _lastD3DFrameLatencyWaitTimeouts, timeouts);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return 0;
        }

        return Math.Max(0, timeouts - previousTimeouts);
    }

    private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(
        CaptureHealthSnapshot snapshot,
        long nowTick)
    {
        var droppedFrames = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackDroppedFrames) : 0;
        var encoderDroppedFrames = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoEncoderDroppedFrames) : 0;
        var sequenceGaps = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoSequenceGaps) : 0;
        var gpuFramesDropped = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackGpuFramesDropped) : 0;
        var backpressureEvents = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoBackpressureEvents) : 0;

        var previousTick = Interlocked.Exchange(ref _lastFlashbackRecordingEvalTick, nowTick);
        var previousDroppedFrames = Interlocked.Exchange(ref _lastFlashbackDroppedFrames, droppedFrames);
        var previousEncoderDroppedFrames = Interlocked.Exchange(ref _lastFlashbackVideoEncoderDroppedFrames, encoderDroppedFrames);
        var previousSequenceGaps = Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps);
        var previousGpuFramesDropped = Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped);
        var previousBackpressureEvents = Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return FlashbackRecordingRecentCounters.Empty;
        }

        return new FlashbackRecordingRecentCounters(
            Math.Max(0, droppedFrames - previousDroppedFrames),
            Math.Max(0, encoderDroppedFrames - previousEncoderDroppedFrames),
            Math.Max(0, sequenceGaps - previousSequenceGaps),
            Math.Max(0, gpuFramesDropped - previousGpuFramesDropped),
            Math.Max(0, backpressureEvents - previousBackpressureEvents));
    }

    private static PreviewPacingClassification ClassifyPreviewPacing(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        MjpegRecentCounters recentMjpeg,
        PreviewJitterRecentCounters recentPreviewJitter,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        long recentD3DFrameLatencyWaitTimeouts)
        => PreviewPacingSlowStageClassifier.Classify(
            new PreviewPacingClassificationInput
            {
                IsPreviewing = viewModelSnapshot.IsPreviewing,
                TargetFrameRate = health.ExpectedFrameRate,
                PreviewCadenceSampleCount = previewRuntime.DisplayCadenceSampleCount,
                PreviewCadenceSampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
                PreviewCadenceExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
                PreviewCadenceObservedFps = previewRuntime.DisplayCadenceObservedFps,
                PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
                PreviewCadenceP99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
                CaptureCadenceSampleCount = health.CaptureCadenceSampleCount,
                CaptureCadenceSampleDurationMs = health.CaptureCadenceSampleDurationMs,
                CaptureExpectedFrameRate = health.ExpectedFrameRate,
                CaptureCadenceOnePercentLowFps = health.CaptureCadenceOnePercentLowFps,
                CaptureCadenceP99IntervalMs = health.CaptureCadenceP99IntervalMs,
                CaptureCadenceSevereGapCount = health.CaptureCadenceSevereGapCount,
                CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
                CaptureCadenceEstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent,
                MjpegPipelineSampleCount = health.MjpegPipelineSampleCount,
                MjpegDecodeP95Ms = health.MjpegDecodeP95Ms,
                MjpegPipelineP95Ms = health.MjpegPipelineP95Ms,
                MjpegPipelineMaxMs = health.MjpegPipelineMaxMs,
                RecentMjpegDropped = recentMjpeg.TotalDropped,
                RecentMjpegFailures = recentMjpeg.Failures,
                MjpegPreviewJitterEnabled = health.MjpegPreviewJitterEnabled,
                RecentPreviewJitterDropped = recentPreviewJitter.Dropped,
                RecentPreviewJitterUnderflows = recentPreviewJitter.Underflows,
                RecentPreviewJitterDeadlineDrops = recentPreviewJitter.DeadlineDrops,
                RecentPreviewJitterScheduleLateCount = recentPreviewJitter.ScheduleLateCount,
                RecentPreviewJitterScheduleLateMs = recentPreviewJitter.ScheduleLateMs,
                MjpegPreviewJitterScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount,
                MjpegPreviewJitterMaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
                MjpegPreviewJitterLatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
                MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,
                RecentRendererSubmitted = Math.Max(recentRenderer.Submitted, recentRenderer.Rendered + recentRenderer.Dropped),
                RecentRendererDropped = recentRenderer.Dropped,
                PreviewD3DPendingFrameCount = previewRuntime.D3DPendingFrameCount,
                PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
                PreviewD3DRenderSubmitCpuP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
                PreviewD3DPresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
                PreviewD3DTotalFrameCpuP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
                PreviewD3DFrameLatencyWaitP95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
                PreviewD3DFrameLatencyWaitMaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs,
                PreviewD3DFrameLatencyWaitTimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
                RecentD3DFrameLatencyWaitTimeoutCount = recentD3DFrameLatencyWaitTimeouts,
                RecentD3DMissedRefreshes = recentD3DMissedRefreshes,
                RecentD3DStatsFailures = recentD3DStatsFailures,
                PreviewD3DLastDropReason = previewRuntime.D3DLastDropReason,
                VisualCadenceSampleCount = health.VisualCadenceSampleCount,
                VisualCadenceChangeObservedFps = health.VisualCadenceChangeObservedFps,
                VisualCadenceRepeatFramePercent = health.VisualCadenceRepeatFramePercent,
                VisualCadenceLongestRepeatRun = health.VisualCadenceLongestRepeatRun,
                VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,
                MjpegPacketHashSampleCount = health.MjpegPacketHashSampleCount,
                MjpegPacketHashInputObservedFps = health.MjpegPacketHashInputObservedFps,
                MjpegPacketHashUniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
                MjpegPacketHashDuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent
            });

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

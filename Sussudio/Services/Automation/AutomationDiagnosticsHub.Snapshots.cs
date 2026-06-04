using System;
using System.Collections.Generic;
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

internal readonly record struct SnapshotFragment<T>(
    T Value,
    long CollectionEpoch,
    long ProducerEpoch,
    long ProducerEpochBefore,
    long ProducerEpochAfter,
    bool ProducerChangedDuringCapture,
    DateTimeOffset CollectedUtc);

internal readonly record struct SnapshotCollectionStamp(
    long Epoch,
    DateTimeOffset StartedUtc,
    DateTimeOffset CompletedUtc,
    long DurationMs,
    long ViewModelEpoch,
    long CaptureRuntimeEpoch,
    long CaptureHealthEpoch,
    long RecordingStatsEpoch,
    long PreviewRuntimeEpoch,
    long OutputEpoch,
    long SourceTelemetryEpoch,
    bool Mixed,
    string MixedReason);

public sealed partial class AutomationDiagnosticsHub
{
    private long _snapshotCollectionEpoch;
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
    private long _lastOutputEpoch;
    private LastOutputSignature _lastOutputSignature;

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

        var collectionEpoch = Interlocked.Increment(ref _snapshotCollectionEpoch);
        var collectionStartedUtc = DateTimeOffset.UtcNow;
        var viewModelFragment = await CaptureSnapshotFragmentAsync(
            collectionEpoch,
            token => _snapshotQueryPort.GetViewModelRuntimeSnapshotAsync(token),
            snapshot => snapshot.CaptureSessionEpoch,
            cancellationToken).ConfigureAwait(false);
        var captureRuntimeFragment = await CaptureSnapshotFragmentAsync(
            collectionEpoch,
            token => _snapshotQueryPort.GetCaptureSnapshotProducerEpochAsync(token),
            token => _snapshotQueryPort.GetCaptureRuntimeSnapshotAsync(token),
            snapshot => snapshot.CaptureSessionEpoch,
            cancellationToken).ConfigureAwait(false);
        var healthFragment = await CaptureSnapshotFragmentAsync(
            collectionEpoch,
            token => _snapshotQueryPort.GetCaptureSnapshotProducerEpochAsync(token),
            token => _snapshotQueryPort.GetCaptureHealthSnapshotAsync(token),
            snapshot => snapshot.CaptureSessionEpoch,
            cancellationToken).ConfigureAwait(false);
        var recordingStatsFragment = await CaptureSnapshotFragmentAsync(
            collectionEpoch,
            token => _snapshotQueryPort.GetCaptureSnapshotProducerEpochAsync(token),
            token => _snapshotQueryPort.GetRecordingStatsSnapshotAsync(token),
            snapshot => snapshot.CaptureSessionEpoch,
            cancellationToken).ConfigureAwait(false);
        var previewRuntimeFragment = await CaptureSnapshotFragmentAsync(
            collectionEpoch,
            _previewSnapshotProvider,
            _previewSnapshotProvider,
            snapshot => snapshot.PreviewRuntimeEpoch,
            cancellationToken).ConfigureAwait(false);
        var viewModelSnapshot = viewModelFragment.Value;
        var captureRuntime = captureRuntimeFragment.Value;
        var health = healthFragment.Value;
        var recordingStats = recordingStatsFragment.Value;
        var previewRuntime = previewRuntimeFragment.Value;

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

        var lastOutputFragment = CaptureSnapshotFragment(
            collectionEpoch,
            () => ProbeLastOutput(captureRuntime.LastOutputPath, viewModelSnapshot.IsRecording),
            () => ProbeLastOutput(captureRuntime.LastOutputPath, viewModelSnapshot.IsRecording),
            snapshot => snapshot.OutputEpoch);
        var processResourcesFragment = CaptureSnapshotFragment(
            collectionEpoch,
            CaptureProcessResourceSnapshot,
            _ => 0L);
        var snapshotCollection = BuildSnapshotCollectionStamp(
            collectionEpoch,
            collectionStartedUtc,
            DateTimeOffset.UtcNow,
            viewModelFragment,
            captureRuntimeFragment,
            healthFragment,
            recordingStatsFragment,
            previewRuntimeFragment,
            lastOutputFragment,
            processResourcesFragment);
        diagnostic = ApplySnapshotCollectionDiagnostic(diagnostic, snapshotCollection);
        var snapshot = BuildAutomationSnapshot(
            viewModelSnapshot,
            captureRuntime,
            health,
            recordingStats,
            previewRuntime,
            snapshotCollection,
            performance,
            diagnostic,
            previewPacingClassification,
            previewHdrState,
            audioSignal,
            recordingFileGrowing,
            hdrTruthVerdict,
            lastOutputFragment.Value,
            processResourcesFragment.Value,
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

    private static async Task<SnapshotFragment<T>> CaptureSnapshotFragmentAsync<T>(
        long collectionEpoch,
        Func<CancellationToken, Task<T>> capture,
        Func<T, long> getProducerEpoch,
        CancellationToken cancellationToken)
    {
        var value = await capture(cancellationToken).ConfigureAwait(false);
        var producerEpoch = getProducerEpoch(value);
        return new SnapshotFragment<T>(
            value,
            collectionEpoch,
            producerEpoch,
            producerEpoch,
            producerEpoch,
            ProducerChangedDuringCapture: false,
            DateTimeOffset.UtcNow);
    }

    private static async Task<SnapshotFragment<T>> CaptureSnapshotFragmentAsync<T>(
        long collectionEpoch,
        Func<CancellationToken, Task<T>> captureCurrentProducerState,
        Func<CancellationToken, Task<T>> capture,
        Func<T, long> getProducerEpoch,
        CancellationToken cancellationToken)
    {
        var producerEpochBefore = getProducerEpoch(await captureCurrentProducerState(cancellationToken).ConfigureAwait(false));
        var value = await capture(cancellationToken).ConfigureAwait(false);
        var collectedUtc = DateTimeOffset.UtcNow;
        var producerEpochAfter = getProducerEpoch(await captureCurrentProducerState(cancellationToken).ConfigureAwait(false));
        var producerEpoch = getProducerEpoch(value);
        return new SnapshotFragment<T>(
            value,
            collectionEpoch,
            producerEpoch,
            producerEpochBefore,
            producerEpochAfter,
            ProducerChangedDuringCapture(producerEpoch, producerEpochBefore, producerEpochAfter),
            collectedUtc);
    }

    private static async Task<SnapshotFragment<T>> CaptureSnapshotFragmentAsync<T>(
        long collectionEpoch,
        Func<CancellationToken, Task<long>> getCurrentProducerEpoch,
        Func<CancellationToken, Task<T>> capture,
        Func<T, long> getProducerEpoch,
        CancellationToken cancellationToken)
    {
        var producerEpochBefore = await getCurrentProducerEpoch(cancellationToken).ConfigureAwait(false);
        var value = await capture(cancellationToken).ConfigureAwait(false);
        var collectedUtc = DateTimeOffset.UtcNow;
        var producerEpochAfter = await getCurrentProducerEpoch(cancellationToken).ConfigureAwait(false);
        var producerEpoch = getProducerEpoch(value);
        return new SnapshotFragment<T>(
            value,
            collectionEpoch,
            producerEpoch,
            producerEpochBefore,
            producerEpochAfter,
            ProducerChangedDuringCapture(producerEpoch, producerEpochBefore, producerEpochAfter),
            collectedUtc);
    }

    private static SnapshotFragment<T> CaptureSnapshotFragment<T>(
        long collectionEpoch,
        Func<T> capture,
        Func<T, long> getProducerEpoch)
    {
        var value = capture();
        var producerEpoch = getProducerEpoch(value);
        return new SnapshotFragment<T>(
            value,
            collectionEpoch,
            producerEpoch,
            producerEpoch,
            producerEpoch,
            ProducerChangedDuringCapture: false,
            DateTimeOffset.UtcNow);
    }

    private static SnapshotFragment<T> CaptureSnapshotFragment<T>(
        long collectionEpoch,
        Func<T> captureCurrentProducerState,
        Func<T> capture,
        Func<T, long> getProducerEpoch)
    {
        var producerEpochBefore = getProducerEpoch(captureCurrentProducerState());
        var value = capture();
        var collectedUtc = DateTimeOffset.UtcNow;
        var producerEpochAfter = getProducerEpoch(captureCurrentProducerState());
        var producerEpoch = getProducerEpoch(value);
        return new SnapshotFragment<T>(
            value,
            collectionEpoch,
            producerEpoch,
            producerEpochBefore,
            producerEpochAfter,
            ProducerChangedDuringCapture(producerEpoch, producerEpochBefore, producerEpochAfter),
            collectedUtc);
    }

    private static SnapshotFragment<T> CaptureSnapshotFragment<T>(
        long collectionEpoch,
        Func<long> getCurrentProducerEpoch,
        Func<T> capture,
        Func<T, long> getProducerEpoch)
    {
        var producerEpochBefore = getCurrentProducerEpoch();
        var value = capture();
        var collectedUtc = DateTimeOffset.UtcNow;
        var producerEpochAfter = getCurrentProducerEpoch();
        var producerEpoch = getProducerEpoch(value);
        return new SnapshotFragment<T>(
            value,
            collectionEpoch,
            producerEpoch,
            producerEpochBefore,
            producerEpochAfter,
            ProducerChangedDuringCapture(producerEpoch, producerEpochBefore, producerEpochAfter),
            collectedUtc);
    }

    private static bool ProducerChangedDuringCapture(long producerEpoch, long producerEpochBefore, long producerEpochAfter)
        => producerEpochBefore != producerEpochAfter ||
           producerEpoch != producerEpochBefore ||
           producerEpoch != producerEpochAfter;

    private static SnapshotCollectionStamp BuildSnapshotCollectionStamp(
        long collectionEpoch,
        DateTimeOffset startedUtc,
        DateTimeOffset completedUtc,
        SnapshotFragment<ViewModelRuntimeSnapshot> viewModel,
        SnapshotFragment<CaptureRuntimeSnapshot> captureRuntime,
        SnapshotFragment<CaptureHealthSnapshot> health,
        SnapshotFragment<RecordingStats> recordingStats,
        SnapshotFragment<PreviewRuntimeSnapshot> previewRuntime,
        SnapshotFragment<LastOutputProbe> lastOutput,
        SnapshotFragment<ProcessResourceSnapshot> processResources)
    {
        var mixedReasons = BuildSnapshotMixedEpochReasons(
            viewModel.Value,
            captureRuntime.Value,
            health.Value,
            recordingStats.Value,
            previewRuntime.Value,
            captureRuntime,
            health,
            recordingStats,
            previewRuntime,
            lastOutput);
        var sourceTelemetryEpoch = ResolveSourceTelemetryEpoch(viewModel.Value, captureRuntime.Value, health.Value);
        var effectiveCompletedUtc = processResources.CollectedUtc > completedUtc
            ? processResources.CollectedUtc
            : completedUtc;
        return new SnapshotCollectionStamp(
            collectionEpoch,
            startedUtc,
            effectiveCompletedUtc,
            ComputeSnapshotCollectionDurationMs(startedUtc, effectiveCompletedUtc),
            viewModel.ProducerEpoch,
            captureRuntime.ProducerEpoch,
            health.ProducerEpoch,
            recordingStats.ProducerEpoch,
            previewRuntime.ProducerEpoch,
            lastOutput.ProducerEpoch,
            sourceTelemetryEpoch,
            mixedReasons.Count > 0,
            string.Join(",", mixedReasons));
    }

    private static List<string> BuildSnapshotMixedEpochReasons(
        ViewModelRuntimeSnapshot viewModel,
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health,
        RecordingStats recordingStats,
        PreviewRuntimeSnapshot previewRuntime,
        SnapshotFragment<CaptureRuntimeSnapshot> captureRuntimeFragment,
        SnapshotFragment<CaptureHealthSnapshot> healthFragment,
        SnapshotFragment<RecordingStats> recordingStatsFragment,
        SnapshotFragment<PreviewRuntimeSnapshot> previewRuntimeFragment,
        SnapshotFragment<LastOutputProbe> lastOutputFragment)
    {
        var reasons = new List<string>();

        AddEpochMismatchReason(
            reasons,
            "capture_session_epoch",
            viewModel.CaptureSessionEpoch,
            captureRuntime.CaptureSessionEpoch,
            health.CaptureSessionEpoch,
            recordingStats.CaptureSessionEpoch);
        AddEpochMismatchReason(
            reasons,
            "source_telemetry_epoch",
            viewModel.SourceTelemetryEpoch,
            captureRuntime.SourceTelemetryEpoch,
            health.SourceTelemetryEpoch);
        AddProducerChangeReason(reasons, "capture_runtime_producer_changed", captureRuntimeFragment);
        AddProducerChangeReason(reasons, "capture_health_producer_changed", healthFragment);
        AddProducerChangeReason(reasons, "recording_stats_producer_changed", recordingStatsFragment);
        AddProducerChangeReason(reasons, "preview_runtime_producer_changed", previewRuntimeFragment);
        AddProducerChangeReason(reasons, "output_producer_changed", lastOutputFragment);

        if (viewModel.IsInitialized != captureRuntime.IsInitialized)
        {
            reasons.Add("view_model_capture_initialized");
        }

        if (viewModel.IsRecording != captureRuntime.IsRecording)
        {
            reasons.Add("view_model_capture_recording");
        }

        if (viewModel.IsPreviewing != previewRuntime.IsPreviewing)
        {
            reasons.Add("view_model_preview_runtime");
        }

        if (captureRuntime.IsInitialized &&
            !string.IsNullOrWhiteSpace(viewModel.SelectedDeviceId) &&
            !string.IsNullOrWhiteSpace(captureRuntime.CurrentDeviceId) &&
            !string.Equals(viewModel.SelectedDeviceId, captureRuntime.CurrentDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("view_model_capture_device");
        }

        if (!string.Equals(
                viewModel.SourceTelemetryAvailability,
                captureRuntime.SourceTelemetryAvailability,
                StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("source_telemetry_availability");
        }

        if (viewModel.SourceTelemetryTimestampUtc.HasValue != captureRuntime.SourceTelemetryTimestampUtc.HasValue)
        {
            reasons.Add("source_telemetry_timestamp_presence");
        }
        else if (viewModel.SourceTelemetryTimestampUtc.HasValue &&
                 Math.Abs((viewModel.SourceTelemetryTimestampUtc.Value - captureRuntime.SourceTelemetryTimestampUtc!.Value).TotalMilliseconds) > 1.0)
        {
            reasons.Add("source_telemetry_timestamp");
        }

        if (viewModel.SourceWidth.HasValue &&
            captureRuntime.SourceWidth.HasValue &&
            viewModel.SourceWidth.Value != captureRuntime.SourceWidth.Value)
        {
            reasons.Add("source_signal_width");
        }

        if (viewModel.SourceHeight.HasValue &&
            captureRuntime.SourceHeight.HasValue &&
            viewModel.SourceHeight.Value != captureRuntime.SourceHeight.Value)
        {
            reasons.Add("source_signal_height");
        }

        return reasons;
    }

    private static void AddEpochMismatchReason(List<string> reasons, string reason, params long[] epochs)
    {
        if (epochs.Length <= 1)
        {
            return;
        }

        var first = epochs[0];
        for (var i = 1; i < epochs.Length; i++)
        {
            if (epochs[i] != first)
            {
                reasons.Add(reason);
                return;
            }
        }
    }

    private static void AddProducerChangeReason<T>(List<string> reasons, string reason, SnapshotFragment<T> fragment)
    {
        if (fragment.ProducerChangedDuringCapture)
        {
            reasons.Add(reason);
        }
    }

    private static long ResolveSourceTelemetryEpoch(
        ViewModelRuntimeSnapshot viewModel,
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
        => Math.Max(viewModel.SourceTelemetryEpoch, Math.Max(captureRuntime.SourceTelemetryEpoch, health.SourceTelemetryEpoch));

    private static long ComputeSnapshotCollectionDurationMs(DateTimeOffset startedUtc, DateTimeOffset completedUtc)
        => Math.Max(0L, (long)Math.Ceiling((completedUtc - startedUtc).TotalMilliseconds));

    private static DiagnosticEvaluation ApplySnapshotCollectionDiagnostic(
        DiagnosticEvaluation diagnostic,
        SnapshotCollectionStamp snapshotCollection)
    {
        if (!snapshotCollection.Mixed)
        {
            return diagnostic;
        }

        var evidence = AppendDiagnosticEvidence(
            diagnostic.Evidence,
            $"snapshot_epoch={snapshotCollection.Epoch} producer_epochs=vm:{snapshotCollection.ViewModelEpoch},capture:{snapshotCollection.CaptureRuntimeEpoch},health:{snapshotCollection.CaptureHealthEpoch},recording:{snapshotCollection.RecordingStatsEpoch},preview:{snapshotCollection.PreviewRuntimeEpoch},output:{snapshotCollection.OutputEpoch},source:{snapshotCollection.SourceTelemetryEpoch} reason={snapshotCollection.MixedReason}");

        return string.Equals(diagnostic.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase)
            ? diagnostic with
            {
                HealthStatus = "Warning",
                LikelyStage = "snapshot_epoch",
                Summary = "Automation diagnostics snapshot was collected from mixed source epochs.",
                Evidence = evidence
            }
            : diagnostic with { Evidence = evidence };
    }

    private static string AppendDiagnosticEvidence(string existingEvidence, string additionalEvidence)
        => string.IsNullOrWhiteSpace(existingEvidence)
            ? additionalEvidence
            : $"{existingEvidence}; {additionalEvidence}";

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
            return BuildLastOutputProbe(string.Empty, exists: false, sizeBytes: null, isRecording: false);
        }

        // While recording, the file is still growing, so re-stat each poll.
        // Once recording stops, the size is final and cached until the path changes.
        var isFinalAndCached = !isRecording &&
                               _cachedFinalOutputSize.HasValue &&
                               string.Equals(_cachedFinalOutputPath, lastOutputPath, StringComparison.Ordinal);
        if (isFinalAndCached)
        {
            return BuildLastOutputProbe(lastOutputPath, exists: true, sizeBytes: _cachedFinalOutputSize, isRecording: false);
        }

        try
        {
            var size = new FileInfo(lastOutputPath).Length;
            if (!isRecording)
            {
                _cachedFinalOutputSize = size;
                _cachedFinalOutputPath = lastOutputPath;
            }

            return BuildLastOutputProbe(lastOutputPath, exists: true, sizeBytes: size, isRecording: isRecording);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub output file probe: {ex.Message}");
            return BuildLastOutputProbe(lastOutputPath, exists: false, sizeBytes: null, isRecording: isRecording);
        }
    }

    private LastOutputProbe BuildLastOutputProbe(string path, bool exists, long? sizeBytes, bool isRecording)
    {
        var signature = new LastOutputSignature(path, exists, sizeBytes, isRecording);
        if (!EqualityComparer<LastOutputSignature>.Default.Equals(signature, _lastOutputSignature))
        {
            _lastOutputSignature = signature;
            Interlocked.Increment(ref _lastOutputEpoch);
        }

        return new LastOutputProbe(exists, sizeBytes, Volatile.Read(ref _lastOutputEpoch));
    }

    private readonly record struct LastOutputSignature(string Path, bool Exists, long? SizeBytes, bool IsRecording);

    private readonly record struct LastOutputProbe(bool Exists, long? SizeBytes, long OutputEpoch);

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

    public async Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default)
    {
        await _verificationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _verificationInProgress);
        try
        {
            var runtimeSnapshot = await _snapshotQueryPort
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
                    Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub post-verification snapshot refresh: {ex.Message}");
                }
            }
        }
    }

    public async Task<RecordingVerificationResult> VerifyFileAsync(
        string filePath,
        string? verificationProfile = null,
        CancellationToken cancellationToken = default)
    {
        await _verificationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _verificationInProgress);
        try
        {
            var runtimeSnapshot = await _snapshotQueryPort
                .GetCaptureRuntimeSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);
            runtimeSnapshot = ApplyVerificationProfile(runtimeSnapshot, filePath, verificationProfile);

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
                $"File verification ({Path.GetFileName(filePath)}): {verification.Message}");

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
                    Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub post-verification snapshot refresh: {ex.Message}");
                }
            }
        }
    }

    private static CaptureRuntimeSnapshot ApplyVerificationProfile(
        CaptureRuntimeSnapshot runtimeSnapshot,
        string filePath,
        string? verificationProfile)
    {
        if (!string.Equals(verificationProfile, "flashback-export", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(runtimeSnapshot.FlashbackExportVerificationFormat))
        {
            return runtimeSnapshot;
        }

        return new CaptureRuntimeSnapshot
        {
            TimestampUtc = runtimeSnapshot.TimestampUtc,
            CaptureSessionEpoch = runtimeSnapshot.CaptureSessionEpoch,
            SourceTelemetryEpoch = runtimeSnapshot.SourceTelemetryEpoch,
            RequestedWidth = runtimeSnapshot.RequestedWidth,
            RequestedHeight = runtimeSnapshot.RequestedHeight,
            RequestedFrameRate = runtimeSnapshot.RequestedFrameRate,
            RequestedFrameRateArg = runtimeSnapshot.RequestedFrameRateArg,
            RequestedFrameRateNumerator = runtimeSnapshot.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = runtimeSnapshot.RequestedFrameRateDenominator,
            RequestedFormat = runtimeSnapshot.RequestedFormat,
            RequestedHdrEnabled = runtimeSnapshot.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = runtimeSnapshot.RequestedHdrMasteringMetadata,
            HdrOutputActive = runtimeSnapshot.HdrOutputActive,
            HdrAutoDowngraded = runtimeSnapshot.HdrAutoDowngraded,
            NegotiatedWidth = runtimeSnapshot.NegotiatedWidth,
            NegotiatedHeight = runtimeSnapshot.NegotiatedHeight,
            NegotiatedFrameRate = runtimeSnapshot.NegotiatedFrameRate,
            NegotiatedFrameRateArg = runtimeSnapshot.NegotiatedFrameRateArg,
            NegotiatedFrameRateNumerator = runtimeSnapshot.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = runtimeSnapshot.NegotiatedFrameRateDenominator,
            AudioBufferHealthStatus = runtimeSnapshot.AudioBufferHealthStatus,
            AudioBufferHealthReason = runtimeSnapshot.AudioBufferHealthReason,
            AudioBufferUnderrunDetected = runtimeSnapshot.AudioBufferUnderrunDetected,
            AudioBufferOverrunDetected = runtimeSnapshot.AudioBufferOverrunDetected,
            AudioBufferUnderrunEvents = runtimeSnapshot.AudioBufferUnderrunEvents,
            AudioBufferOverrunEvents = runtimeSnapshot.AudioBufferOverrunEvents,
            FlashbackExportOutputPath = filePath,
            FlashbackExportVerificationFormat = runtimeSnapshot.FlashbackExportVerificationFormat,
            FlashbackCodecDowngradeReason = runtimeSnapshot.FlashbackCodecDowngradeReason
        };
    }

    private bool ShouldAutoVerifySnapshot(AutomationSnapshot snapshot)
    {
        var verificationIdle = Volatile.Read(ref _verificationInProgress) == 0 &&
                               Volatile.Read(ref _autoVerificationScheduled) == 0;
        return !snapshot.IsRecording &&
               _wasRecording &&
               !string.IsNullOrWhiteSpace(snapshot.LastOutputPath) &&
               verificationIdle;
    }

    private RecordingVerificationResult? CaptureLastVerificationForSnapshot(bool recordingStarted)
    {
        lock (_stateLock)
        {
            if (recordingStarted)
            {
                _lastVerification = null;
            }

            return _lastVerification;
        }
    }

    private void ScheduleAutoVerificationIfNeeded(bool shouldAutoVerify)
    {
        if (!shouldAutoVerify ||
            _cts is not { IsCancellationRequested: false } cts ||
            Interlocked.CompareExchange(ref _autoVerificationScheduled, 1, 0) != 0)
        {
            return;
        }

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
                /* Expected during shutdown - auto-verification cancelled */
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

    private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        ObserveFlashbackExportCompletion(snapshot);
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                snapshot.ExpectedCaptureFrameRate,
                snapshot.CaptureCadenceSampleCount,
                snapshot.CaptureCadenceOnePercentLowFps);
        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                snapshot.PreviewCadenceExpectedIntervalMs,
                snapshot.PreviewCadenceSampleCount,
                snapshot.PreviewCadenceOnePercentLowFps);
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                snapshot.SelectedFrameRate,
                snapshot.VisualCadenceSampleCount,
                snapshot.VisualCadenceChangeObservedFps,
                snapshot.VisualCadenceRepeatFramePercent,
                snapshot.VisualCadenceLongestRepeatRun);
        var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);

        UpdateSignalAlerts(
            snapshot,
            captureOnePercentLowDegraded,
            previewOnePercentLowDegraded,
            visualCadenceHealthy,
            previewSlowFrameDetail);

        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);
        UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);

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

    private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)
    {
        if (snapshot.FlashbackExportActive ||
            snapshot.FlashbackExportId <= 0 ||
            snapshot.FlashbackExportCompletedUtcUnixMs <= 0)
        {
            return;
        }

        var previousId = Interlocked.Read(ref _lastFlashbackExportCompletionEventId);
        if (snapshot.FlashbackExportId <= previousId ||
            Interlocked.CompareExchange(
                ref _lastFlashbackExportCompletionEventId,
                snapshot.FlashbackExportId,
                previousId) != previousId)
        {
            return;
        }

        var status = string.IsNullOrWhiteSpace(snapshot.FlashbackExportStatus)
            ? "Unknown"
            : snapshot.FlashbackExportStatus;
        var severity = status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)
            ? DiagnosticsSeverity.Info
            : status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                ? DiagnosticsSeverity.Warning
                : DiagnosticsSeverity.Error;
        var message = string.IsNullOrWhiteSpace(snapshot.FlashbackExportMessage)
            ? status
            : snapshot.FlashbackExportMessage;
        var failureKind = string.IsNullOrWhiteSpace(snapshot.FlashbackExportFailureKind)
            ? "None"
            : snapshot.FlashbackExportFailureKind;

        AddEvent(
            severity,
            DiagnosticsCategory.Flashback,
            $"Flashback export completed: status={status} id={snapshot.FlashbackExportId} " +
            $"elapsed={snapshot.FlashbackExportElapsedMs}ms progress={snapshot.FlashbackExportPercent:0.##}% " +
            $"segments={snapshot.FlashbackExportSegmentsProcessed}/{snapshot.FlashbackExportTotalSegments} " +
            $"bytes={snapshot.FlashbackExportOutputBytes} kind={failureKind} path={snapshot.FlashbackExportOutputPath} message={message}");
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

    public IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100)
    {
        lock (_stateLock)
        {
            var take = Math.Clamp(maxEvents, 1, MaxRecentEvents);
            if (_recentEvents.Count <= take)
            {
                return _recentEvents.ToArray();
            }

            return _recentEvents.GetRange(_recentEvents.Count - take, take).ToArray();
        }
    }

    private void UpdateFlashbackPlaybackAlerts(AutomationSnapshot snapshot, long nowUnixMs)
    {
        UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);
        UpdateFlashbackPlaybackPerformanceAlerts(snapshot);
    }

    private void UpdateFlashbackRecordingAlerts(
        AutomationSnapshot snapshot,
        FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        var flashbackRecordingRecentBackpressure =
            flashbackRecordingRecent.BackpressureEvents > 0 &&
            snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs;
        var flashbackRecordingQueueBacklog =
            IsFlashbackRecordingQueueBackedUp(
                snapshot.FlashbackVideoQueueDepth,
                snapshot.FlashbackVideoQueueCapacity,
                snapshot.FlashbackVideoQueueOldestFrameAgeMs);
        var flashbackAudioQueueBacklog =
            IsFlashbackAudioQueueBackedUp(
                snapshot.FlashbackAudioQueueDepth,
                snapshot.FlashbackAudioQueueCapacity);
        var flashbackRecordingRecentForceRotateGap =
            snapshot.FlashbackActive &&
            flashbackRecordingRecent.SequenceGaps > 0 &&
            snapshot.FlashbackVideoQueueRejectedFrames > 0 &&
            IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason);
        var exportLastProgressAgeMs = snapshot.FlashbackExportActive
            ? Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)
            : 0;

        UpdateFlashbackExportAlerts(snapshot, exportLastProgressAgeMs, flashbackRecordingRecent, flashbackRecordingRecentForceRotateGap);
        UpdateFlashbackStorageAlerts(snapshot);
        UpdateFlashbackEncoderAlerts(snapshot);
        UpdateFlashbackRecordingDegradationAlert(
            snapshot,
            flashbackRecordingRecent,
            flashbackRecordingRecentForceRotateGap,
            flashbackRecordingRecentBackpressure,
            flashbackRecordingQueueBacklog,
            flashbackAudioQueueBacklog);
    }

    private void UpdateFlashbackExportAlerts(
        AutomationSnapshot snapshot,
        long exportLastProgressAgeMs,
        FlashbackRecordingRecentCounters flashbackRecordingRecent,
        bool flashbackRecordingRecentForceRotateGap)
    {
        SetAlertState(
            "flashback-export-stalled",
            snapshot.FlashbackExportActive &&
            exportLastProgressAgeMs >= FlashbackExportStallThresholdMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback export has not reported progress for {exportLastProgressAgeMs}ms " +
            $"(id={snapshot.FlashbackExportId}, status={snapshot.FlashbackExportStatus}, " +
            $"progress={snapshot.FlashbackExportPercent:0.##}%, segments={snapshot.FlashbackExportSegmentsProcessed}/{snapshot.FlashbackExportTotalSegments}).",
            "Flashback export progress resumed.",
            throttleMs: 10000);

        SetAlertState(
            "flashback-export-rotation-gap",
            flashbackRecordingRecentForceRotateGap,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback export rotation skipped live-edge frames: recentSeqGaps={flashbackRecordingRecent.SequenceGaps} " +
            $"queueRejects={snapshot.FlashbackVideoQueueRejectedFrames} lastReject={snapshot.FlashbackVideoQueueLastRejectReason} " +
            $"exportStatus={snapshot.FlashbackExportStatus} exportId={snapshot.FlashbackExportId}.",
            "Flashback export rotation is no longer skipping live-edge frames.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackStorageAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-temp-cache-pressure",
            snapshot.FlashbackActive &&
            (snapshot.FlashbackStartupCacheOverBudget ||
             (snapshot.FlashbackTempDriveFreeBytes >= 0 && snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes)),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback temp storage is under pressure: freeBytes={snapshot.FlashbackTempDriveFreeBytes} " +
            $"cacheBytes={snapshot.FlashbackStartupCacheBytes} budgetBytes={snapshot.FlashbackStartupCacheBudgetBytes} " +
            $"sessions={snapshot.FlashbackStartupCacheSessionCount} deleted={snapshot.FlashbackStartupCacheDeletedSessionCount} " +
            $"freedBytes={snapshot.FlashbackStartupCacheFreedBytes} overBudget={snapshot.FlashbackStartupCacheOverBudget}.",
            "Flashback temp storage returned to healthy range.",
            throttleMs: 10000);
    }

    private void UpdateFlashbackEncoderAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-encoding-failed",
            snapshot.FlashbackEncodingFailed,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Flashback,
            string.IsNullOrWhiteSpace(snapshot.FlashbackEncodingFailureMessage)
                ? $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"}."
                : $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"} message={snapshot.FlashbackEncodingFailureMessage}.",
            "Flashback encoder failure cleared.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackRecordingDegradationAlert(
        AutomationSnapshot snapshot,
        FlashbackRecordingRecentCounters flashbackRecordingRecent,
        bool flashbackRecordingRecentForceRotateGap,
        bool flashbackRecordingRecentBackpressure,
        bool flashbackRecordingQueueBacklog,
        bool flashbackAudioQueueBacklog)
    {
        SetAlertState(
            "flashback-recording-degraded",
            snapshot.FlashbackActive &&
            (flashbackRecordingRecent.DroppedFrames > 0 ||
             flashbackRecordingRecent.EncoderDroppedFrames > 0 ||
             (flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap) ||
             flashbackRecordingRecent.GpuFramesDropped > 0 ||
             flashbackRecordingRecentBackpressure ||
             flashbackRecordingQueueBacklog ||
             flashbackAudioQueueBacklog),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback recording path degraded: recentDropped={flashbackRecordingRecent.DroppedFrames} recentEncoderDrops={flashbackRecordingRecent.EncoderDroppedFrames} " +
            $"recentSeqGaps={flashbackRecordingRecent.SequenceGaps} recentGpuOverloads={flashbackRecordingRecent.GpuFramesDropped} " +
            $"recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents} " +
            $"totals=dropped:{snapshot.FlashbackDroppedFrames},encoderDrops:{snapshot.FlashbackVideoEncoderDroppedFrames},seqGaps:{snapshot.FlashbackVideoSequenceGaps},gpuOverloads:{snapshot.FlashbackGpuFramesDropped} " +
            $"forceRotate={snapshot.FlashbackForceRotateActive} requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining} " +
            $"queue={snapshot.FlashbackVideoQueueDepth}/{snapshot.FlashbackVideoQueueCapacity} maxQueue={snapshot.FlashbackVideoQueueMaxDepth} " +
            $"audioQueue={snapshot.FlashbackAudioQueueDepth}/{snapshot.FlashbackAudioQueueCapacity} " +
            $"backpressure={snapshot.FlashbackVideoBackpressureWaitMs}ms/{snapshot.FlashbackVideoBackpressureEvents} last={snapshot.FlashbackVideoBackpressureLastWaitMs}ms max={snapshot.FlashbackVideoBackpressureMaxWaitMs}ms.",
            "Flashback recording path returned to healthy range.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackPlaybackCommandAlerts(AutomationSnapshot snapshot, long nowUnixMs)
    {
        var playbackCommandQueueAgeMs =
            snapshot.FlashbackPlaybackPendingCommands > 0 &&
            snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > 0 &&
            snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs
                ? Math.Max(0, nowUnixMs - snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs)
                : 0;
        var playbackCommandFailureAgeMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0
            ? Math.Max(0, nowUnixMs - snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs)
            : 0;
        var playbackCommandFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)
            ? "None"
            : snapshot.FlashbackPlaybackLastCommandFailure;
        var playbackCommandFailedRecently =
            playbackCommandFailureAgeMs > 0 &&
            playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs;

        SetAlertState(
            "flashback-playback-command-stalled",
            playbackCommandQueueAgeMs >= FlashbackPlaybackCommandStallThresholdMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback command queue has not drained for {playbackCommandQueueAgeMs}ms " +
            $"(pending={snapshot.FlashbackPlaybackPendingCommands}/{snapshot.FlashbackPlaybackCommandQueueCapacity}, maxPending={snapshot.FlashbackPlaybackMaxPendingCommands}, " +
            $"lastLatency={snapshot.FlashbackPlaybackLastCommandQueueLatencyMs}ms, maxLatency={snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand}, " +
            $"lastQueued={snapshot.FlashbackPlaybackLastCommandQueued}, lastProcessed={snapshot.FlashbackPlaybackLastCommandProcessed}, " +
            $"lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}, threadAlive={snapshot.FlashbackPlaybackThreadAlive}).",
            "Flashback playback command queue drained.",
            throttleMs: 1000);

        SetAlertState(
            "flashback-playback-command-failed",
            playbackCommandFailedRecently,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback command failed recently: lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs} " +
            $"pending={snapshot.FlashbackPlaybackPendingCommands}/{snapshot.FlashbackPlaybackCommandQueueCapacity} " +
            $"lastQueued={snapshot.FlashbackPlaybackLastCommandQueued} lastProcessed={snapshot.FlashbackPlaybackLastCommandProcessed} " +
            $"threadAlive={snapshot.FlashbackPlaybackThreadAlive} state={snapshot.FlashbackPlaybackState}.",
            "Flashback playback command failures cleared.",
            throttleMs: 1000);
    }

    private void UpdateSignalAlerts(
        AutomationSnapshot snapshot,
        bool captureOnePercentLowDegraded,
        bool previewOnePercentLowDegraded,
        bool visualCadenceHealthy,
        string previewSlowFrameDetail)
    {
        UpdatePreviewSignalAlerts(snapshot, previewOnePercentLowDegraded, visualCadenceHealthy, previewSlowFrameDetail);
        UpdateAudioSignalAlerts(snapshot);
        UpdateRecordingGrowthAlerts(snapshot);
        UpdateCaptureSignalAlerts(snapshot, captureOnePercentLowDegraded);
    }

    private void UpdatePreviewSignalAlerts(
        AutomationSnapshot snapshot,
        bool previewOnePercentLowDegraded,
        bool visualCadenceHealthy,
        string previewSlowFrameDetail)
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
            $"p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms expected={snapshot.PreviewCadenceExpectedIntervalMs:0.##}ms{previewSlowFrameDetail}.",
            "Preview cadence returned to healthy range.");

        SetAlertState(
            "preview-display-low-1pct",
            previewOnePercentLowDegraded && !visualCadenceHealthy,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            $"Preview/display 1% low is below target: onePercentLow={snapshot.PreviewCadenceOnePercentLowFps:0.##}fps " +
            $"target={(snapshot.PreviewCadenceExpectedIntervalMs > 0 ? 1000.0 / snapshot.PreviewCadenceExpectedIntervalMs : 0):0.##}fps " +
            $"avg={snapshot.PreviewCadenceObservedFps:0.##}fps p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms " +
            $"p99={snapshot.PreviewCadenceP99IntervalMs:0.##}ms max={snapshot.PreviewCadenceMaxIntervalMs:0.##}ms{previewSlowFrameDetail}{FormatVisualCadenceAlertDetail(snapshot)}.",
            "Preview/display 1% low returned to target range.",
            throttleMs: 5000);
    }

    private void UpdateCaptureSignalAlerts(
        AutomationSnapshot snapshot,
        bool captureOnePercentLowDegraded)
    {
        SetAlertState(
            "capture-cadence-drop",
            snapshot.CaptureCadenceSampleCount >= 120 && snapshot.CaptureCadenceEstimatedDropPercent >= 1.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence drop estimate={snapshot.CaptureCadenceEstimatedDropPercent:0.##}% " +
            $"(estDropped={snapshot.CaptureCadenceEstimatedDroppedFrames}, severeGaps={snapshot.CaptureCadenceSevereGapCount}).",
            "Capture cadence drop estimate returned to healthy range.");

        SetAlertState(
            "capture-cadence-low-1pct",
            captureOnePercentLowDegraded,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence 1% low is below target: onePercentLow={snapshot.CaptureCadenceOnePercentLowFps:0.##}fps " +
            $"target={snapshot.ExpectedCaptureFrameRate:0.##}fps avg={snapshot.CaptureCadenceObservedFps:0.##}fps " +
            $"p95={snapshot.CaptureCadenceP95IntervalMs:0.##}ms p99={snapshot.CaptureCadenceP99IntervalMs:0.##}ms max={snapshot.CaptureCadenceMaxIntervalMs:0.##}ms.",
            "Capture cadence 1% low returned to target range.",
            throttleMs: 5000);
    }

    private void UpdateAudioSignalAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "audio-muted-suspect",
            snapshot.AudioMutedSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Audio,
            "Audio is enabled but sustained low signal suggests muted or disconnected input.",
            "Audio signal recovered.");
    }

    private void UpdateRecordingGrowthAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "recording-not-growing",
            snapshot.IsRecording && !snapshot.RecordingFileGrowing,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Recording,
            "Recording is active but output bytes are not increasing.",
            "Recording output growth resumed.");
    }

    private void UpdateFlashbackPlaybackPerformanceAlerts(AutomationSnapshot snapshot)
    {
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            snapshot.FlashbackPlaybackTargetFps,
            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));
        var selectedCaptureFps = snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate);
        var playbackActive =
            string.Equals(snapshot.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase);

        UpdateFlashbackPlaybackCadenceAlerts(
            snapshot,
            playbackTargetFps,
            selectedCaptureFps,
            playbackActive);
        UpdateFlashbackPlaybackAudioAlerts(snapshot, playbackActive);
        UpdateFlashbackPlaybackSubmitFailureAlert(snapshot);
    }

    private void UpdateFlashbackPlaybackCadenceAlerts(
        AutomationSnapshot snapshot,
        double playbackTargetFps,
        double selectedCaptureFps,
        bool playbackActive)
    {
        var playbackTargetBelowSelection =
            playbackActive &&
            selectedCaptureFps >= 90 &&
            snapshot.FlashbackPlaybackTargetFps > 0 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio;
        var playbackPresentCadenceCapped =
            playbackActive &&
            snapshot.FlashbackPlaybackTargetFps >= 90 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.PreviewCadenceSampleCount >= PreviewPerfectionMinSamples &&
            snapshot.PreviewCadenceObservedFps > 0 &&
            snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackSlow =
            playbackActive &&
            playbackTargetFps > 0 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackObservedFps > 0 &&
            snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackFrametimeDegraded =
            IsFlashbackPlaybackFrametimeDegraded(
                snapshot.FlashbackPlaybackState,
                playbackTargetFps,
                snapshot.FlashbackPlaybackFrameCount,
                snapshot.FlashbackPlaybackCadenceSampleCount,
                snapshot.FlashbackPlaybackOnePercentLowFps);

        SetAlertState(
            "flashback-playback-target-below-selection",
            playbackTargetBelowSelection,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback target is below the selected capture rate: playbackTarget={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"selected={selectedCaptureFps:0.##}fps encoder={snapshot.EncoderFrameRate:0.##}fps expected={snapshot.ExpectedCaptureFrameRate:0.##}fps " +
            $"source={(snapshot.DetectedSourceFrameRate ?? 0):0.##}fps observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps frames={snapshot.FlashbackPlaybackFrameCount}.",
            "Flashback playback target matches the selected capture rate.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-present-capped",
            playbackPresentCadenceCapped,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is targeting HFR but D3D present cadence is below target: target={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"playbackObserved={snapshot.FlashbackPlaybackObservedFps:0.##}fps presentObserved={snapshot.PreviewCadenceObservedFps:0.##}fps " +
            $"present1pctLow={snapshot.PreviewCadenceOnePercentLowFps:0.##}fps sync={snapshot.PreviewD3DPresentSyncInterval} " +
            $"latency={snapshot.PreviewD3DMaxFrameLatency} buffers={snapshot.PreviewD3DSwapChainBufferCount} " +
            $"renderDrops={snapshot.PreviewD3DFramesDropped} lastDrop={snapshot.PreviewD3DLastDropReason}.",
            "Flashback playback present cadence returned to the HFR target range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-slow",
            playbackSlow,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is below target rate: observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps target={playbackTargetFps:0.##}fps " +
            $"selected={selectedCaptureFps:0.##}fps encoder={snapshot.EncoderFrameRate:0.##}fps present={snapshot.PreviewCadenceObservedFps:0.##}fps " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} late={snapshot.FlashbackPlaybackLateFrames} dropped={snapshot.FlashbackPlaybackDroppedFrames} submitFailures={snapshot.FlashbackPlaybackSubmitFailures} " +
            $"audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={snapshot.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks} " +
            $"switches={snapshot.FlashbackPlaybackSegmentSwitches} fmp4Reopens={snapshot.FlashbackPlaybackFmp4Reopens} writeHeadWaits={snapshot.FlashbackPlaybackWriteHeadWaits}.",
            "Flashback playback returned to target rate.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-frametime-degraded",
            playbackFrametimeDegraded,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frametime degraded: onePercentLow={snapshot.FlashbackPlaybackOnePercentLowFps:0.##}fps target={playbackTargetFps:0.##}fps " +
            $"p99={snapshot.FlashbackPlaybackP99FrameMs:0.##}ms max={snapshot.FlashbackPlaybackMaxFrameMs:0.##}ms slow={snapshot.FlashbackPlaybackSlowFramePercent:0.##}% " +
            $"ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={snapshot.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{snapshot.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={snapshot.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap} " +
            $"decodeP99={snapshot.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={snapshot.FlashbackPlaybackDecodeMaxMs:0.##}ms " +
            $"decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase} decodeReceive={snapshot.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms " +
            $"decodeFeed={snapshot.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={snapshot.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={snapshot.FlashbackPlaybackMaxDecodeSendMs:0.##}ms " +
            $"decodeAudio={snapshot.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={snapshot.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={snapshot.FlashbackPlaybackMaxDecodePositionMs}ms " +
            $"samples={snapshot.FlashbackPlaybackCadenceSampleCount} " +
            $"audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={snapshot.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks}.",
            "Flashback playback frametime returned to target range.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackPlaybackSubmitFailureAlert(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-playback-submit-failures",
            snapshot.FlashbackPlaybackSubmitFailures > 0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frame submission failed: submitFailures={snapshot.FlashbackPlaybackSubmitFailures} state={snapshot.FlashbackPlaybackState} " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} threadAlive={snapshot.FlashbackPlaybackThreadAlive}.",
            "Flashback playback frame submission recovered.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackPlaybackAudioAlerts(
        AutomationSnapshot snapshot,
        bool playbackActive)
    {
        var playbackAudioMasterFallbackDominant =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio;
        var playbackAudioQueueBacklog =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth;

        SetAlertState(
            "flashback-playback-audio-master-fallback",
            playbackAudioMasterFallbackDominant,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is using wall-clock pacing instead of audio-master pacing: " +
            $"fallbacks={snapshot.FlashbackPlaybackAudioMasterFallbacks} frames={snapshot.FlashbackPlaybackFrameCount} " +
            $"target={snapshot.FlashbackPlaybackTargetFps:0.##}fps observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps " +
            $"avDrift={snapshot.FlashbackAvDriftMs:0.##}ms renderCallbacks={snapshot.WasapiPlaybackRenderCallbackCount} " +
            $"renderSilence={snapshot.WasapiPlaybackRenderSilenceCount} queueDepth={snapshot.WasapiPlaybackQueueDepth}.",
            "Flashback playback returned to audio-master pacing.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-audio-queue-backlog",
            playbackAudioQueueBacklog,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback audio queue is backing up: queueDepth={snapshot.WasapiPlaybackQueueDepth} " +
            $"drops={snapshot.WasapiPlaybackQueueDropCount} renderSilence={snapshot.WasapiPlaybackRenderSilenceCount} " +
            $"avDrift={snapshot.FlashbackAvDriftMs:0.##}ms target={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks}.",
            "Flashback playback audio queue returned to healthy depth.",
            throttleMs: 5000);
    }
}

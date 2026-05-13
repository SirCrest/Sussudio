using System;
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

}

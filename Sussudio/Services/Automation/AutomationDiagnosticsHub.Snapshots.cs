using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var previewPacingClassification = PreviewPacingSlowStageClassifier.Classify(
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
        var lastOutputPath = captureRuntime.LastOutputPath;
        if (!string.IsNullOrWhiteSpace(lastOutputPath))
        {
            // While recording, the file is still growing — re-stat each poll. Once
            // recording stops, the size is final and the cached value is reused
            // until the path changes.
            var isFinalAndCached = !viewModelSnapshot.IsRecording &&
                                   _cachedFinalOutputSize.HasValue &&
                                   string.Equals(_cachedFinalOutputPath, lastOutputPath, StringComparison.Ordinal);
            if (isFinalAndCached)
            {
                lastOutputSize = _cachedFinalOutputSize;
                lastOutputExists = true;
            }
            else
            {
                try
                {
                    lastOutputSize = new FileInfo(lastOutputPath).Length;
                    lastOutputExists = true;
                    if (!viewModelSnapshot.IsRecording)
                    {
                        _cachedFinalOutputSize = lastOutputSize;
                        _cachedFinalOutputPath = lastOutputPath;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub output file probe: {ex.Message}");
                }
            }
        }
        else
        {
            _cachedFinalOutputSize = null;
            _cachedFinalOutputPath = null;
        }

        // Memory & GC metrics (all APIs are thread-safe and microsecond-cheap)
        _currentProcess.Refresh();
        var processCpuTotalMs = _currentProcess.TotalProcessorTime.TotalMilliseconds;
        var processCpuPercent = CalculateProcessCpuPercent(processCpuTotalMs);
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
            DiagnosticHealthStatus = diagnostic.HealthStatus,
            DiagnosticLikelyStage = diagnostic.LikelyStage,
            DiagnosticSummary = diagnostic.Summary,
            DiagnosticEvidence = diagnostic.Evidence,
            DiagnosticSourceLane = diagnostic.SourceLane,
            DiagnosticDecodeLane = diagnostic.DecodeLane,
            DiagnosticPreviewLane = diagnostic.PreviewLane,
            DiagnosticRenderLane = diagnostic.RenderLane,
            DiagnosticPresentLane = diagnostic.PresentLane,
            DiagnosticRecordingLane = diagnostic.RecordingLane,
            DiagnosticAudioLane = diagnostic.AudioLane,
            PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,
            PreviewPacingSlowStageConfidence = previewPacingClassification.Confidence,
            PreviewPacingSlowStageEvidence = previewPacingClassification.Evidence,
            CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,
            CaptureCommandCommandsCompleted = viewModelSnapshot.CaptureCommandCommandsCompleted,
            CaptureCommandCommandsFailed = viewModelSnapshot.CaptureCommandCommandsFailed,
            CaptureCommandCommandsCanceled = viewModelSnapshot.CaptureCommandCommandsCanceled,
            CaptureCommandCommandsCoalesced = viewModelSnapshot.CaptureCommandCommandsCoalesced,
            CaptureCommandPendingCommands = viewModelSnapshot.CaptureCommandPendingCommands,
            CaptureCommandMaxPendingCommands = viewModelSnapshot.CaptureCommandMaxPendingCommands,
            CaptureCommandOldestPendingCommandAgeMs = viewModelSnapshot.CaptureCommandOldestPendingCommandAgeMs,
            CaptureCommandLastQueueLatencyMs = viewModelSnapshot.CaptureCommandLastQueueLatencyMs,
            CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,
            CaptureCommandLastCommand = viewModelSnapshot.CaptureCommandLastCommand,
            CaptureCommandLastOutcome = viewModelSnapshot.CaptureCommandLastOutcome,
            CaptureCommandLastCorrelationId = viewModelSnapshot.CaptureCommandLastCorrelationId,
            CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,
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
            SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(
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
            WasapiCaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            WasapiPlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs,
            MemoryPreference = captureRuntime.MemoryPreference,
            VideoRequestedSubtype = captureRuntime.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureRuntime.FrameLedgerCapacity,
            FrameLedgerEventCount = captureRuntime.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureRuntime.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,
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
            RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,
            RecordingIntegrityComplete = captureRuntime.RecordingIntegrityComplete,
            RecordingIntegrityBackend = captureRuntime.RecordingIntegrityBackend,
            RecordingIntegrityCompletedUtc = captureRuntime.RecordingIntegrityCompletedUtc,
            RecordingIntegritySourceFrames = captureRuntime.RecordingIntegritySourceFrames,
            RecordingIntegrityAcceptedFrames = captureRuntime.RecordingIntegrityAcceptedFrames,
            RecordingIntegrityPipelineDroppedFrames = captureRuntime.RecordingIntegrityPipelineDroppedFrames,
            RecordingIntegrityQueueDroppedFrames = captureRuntime.RecordingIntegrityQueueDroppedFrames,
            RecordingIntegritySubmittedFrames = captureRuntime.RecordingIntegritySubmittedFrames,
            RecordingIntegrityEncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,
            RecordingIntegrityPacketsWritten = captureRuntime.RecordingIntegrityPacketsWritten,
            RecordingIntegrityEncoderDroppedFrames = captureRuntime.RecordingIntegrityEncoderDroppedFrames,
            RecordingIntegritySequenceGaps = captureRuntime.RecordingIntegritySequenceGaps,
            RecordingIntegrityQueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,
            RecordingIntegrityQueueOldestFrameAgeMs = captureRuntime.RecordingIntegrityQueueOldestFrameAgeMs,
            RecordingIntegrityBackpressureWaitMs = captureRuntime.RecordingIntegrityBackpressureWaitMs,
            RecordingIntegrityBackpressureEvents = captureRuntime.RecordingIntegrityBackpressureEvents,
            RecordingIntegrityBackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs,
            RecordingIntegrityAudioStatus = captureRuntime.RecordingIntegrityAudioStatus,
            RecordingIntegrityAudioEnabled = captureRuntime.RecordingIntegrityAudioEnabled,
            RecordingIntegrityAudioCaptureActive = captureRuntime.RecordingIntegrityAudioCaptureActive,
            RecordingIntegrityAudioFramesArrived = captureRuntime.RecordingIntegrityAudioFramesArrived,
            RecordingIntegrityAudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,
            RecordingIntegrityAudioSamplesEncoded = captureRuntime.RecordingIntegrityAudioSamplesEncoded,
            RecordingIntegrityAudioDropEvents = captureRuntime.RecordingIntegrityAudioDropEvents,
            RecordingIntegrityAudioDiscontinuities = captureRuntime.RecordingIntegrityAudioDiscontinuities,
            RecordingIntegrityAudioTimestampErrors = captureRuntime.RecordingIntegrityAudioTimestampErrors,
            RecordingIntegrityAudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps,
            RecordingIntegrityAvSyncDriftMs = captureRuntime.RecordingIntegrityAvSyncDriftMs,
            RecordingIntegrityAvSyncDriftRateMsPerSec = captureRuntime.RecordingIntegrityAvSyncDriftRateMsPerSec,
            RecordingIntegrityEncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,
            RecordingIntegrityEncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples,
            RecordingIntegrityReason = captureRuntime.RecordingIntegrityReason,
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
            PreviewCadenceP99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
            PreviewCadenceMaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
            PreviewCadenceFivePercentLowFps = previewRuntime.DisplayCadenceFivePercentLowFps,
            PreviewCadenceSampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
            PreviewCadenceRecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,
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
            PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,
            PreviewD3DMaxFrameLatency = previewRuntime.D3DMaxFrameLatency,
            PreviewD3DSwapChainBufferCount = previewRuntime.D3DSwapChainBufferCount,
            PreviewD3DSwapChainAddress = previewRuntime.D3DSwapChainAddress,
            PreviewD3DFramesSubmitted = previewRuntime.D3DFramesSubmitted,
            PreviewD3DFramesRendered = previewRuntime.D3DFramesRendered,
            PreviewD3DFramesDropped = previewRuntime.D3DFramesDropped,
            PreviewD3DRenderThreadFailureCount = previewRuntime.D3DRenderThreadFailureCount,
            PreviewD3DLastRenderThreadFailureType = previewRuntime.D3DLastRenderThreadFailureType,
            PreviewD3DLastRenderThreadFailureMessage = previewRuntime.D3DLastRenderThreadFailureMessage,
            PreviewD3DLastRenderThreadFailureHResult = previewRuntime.D3DLastRenderThreadFailureHResult,
            PreviewD3DPendingFrameCount = previewRuntime.D3DPendingFrameCount,
            PreviewD3DInputColorSpace = previewRuntime.D3DInputColorSpace,
            PreviewD3DOutputColorSpace = previewRuntime.D3DOutputColorSpace,
            PreviewD3DCpuTimingSampleCount = previewRuntime.D3DCpuTimingSampleCount,
            PreviewD3DInputUploadCpuAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            PreviewD3DInputUploadCpuP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
            PreviewD3DInputUploadCpuMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            PreviewD3DRenderSubmitCpuAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            PreviewD3DRenderSubmitCpuP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            PreviewD3DRenderSubmitCpuP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
            PreviewD3DRenderSubmitCpuMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PreviewD3DPresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PreviewD3DPresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PreviewD3DPresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
            PreviewD3DPresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            PreviewD3DTotalFrameCpuAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            PreviewD3DTotalFrameCpuP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            PreviewD3DTotalFrameCpuP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
            PreviewD3DTotalFrameCpuMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs,
            PreviewD3DPipelineLatencySampleCount = previewRuntime.D3DPipelineLatencySampleCount,
            PreviewD3DPipelineLatencyAvgMs = previewRuntime.D3DPipelineLatencyAvgMs,
            PreviewD3DPipelineLatencyP95Ms = previewRuntime.D3DPipelineLatencyP95Ms,
            PreviewD3DPipelineLatencyP99Ms = previewRuntime.D3DPipelineLatencyP99Ms,
            PreviewD3DPipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,
            PreviewD3DFrameLatencyWaitEnabled = previewRuntime.D3DFrameLatencyWaitEnabled,
            PreviewD3DFrameLatencyWaitHandleActive = previewRuntime.D3DFrameLatencyWaitHandleActive,
            PreviewD3DFrameLatencyWaitCallCount = previewRuntime.D3DFrameLatencyWaitCallCount,
            PreviewD3DFrameLatencyWaitSignaledCount = previewRuntime.D3DFrameLatencyWaitSignaledCount,
            PreviewD3DFrameLatencyWaitTimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
            PreviewD3DFrameLatencyWaitUnexpectedResultCount = previewRuntime.D3DFrameLatencyWaitUnexpectedResultCount,
            PreviewD3DFrameLatencyWaitLastResult = previewRuntime.D3DFrameLatencyWaitLastResult,
            PreviewD3DFrameLatencyWaitLastMs = previewRuntime.D3DFrameLatencyWaitLastMs,
            PreviewD3DFrameLatencyWaitSampleCount = previewRuntime.D3DFrameLatencyWaitSampleCount,
            PreviewD3DFrameLatencyWaitAvgMs = previewRuntime.D3DFrameLatencyWaitAvgMs,
            PreviewD3DFrameLatencyWaitP95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
            PreviewD3DFrameLatencyWaitP99Ms = previewRuntime.D3DFrameLatencyWaitP99Ms,
            PreviewD3DFrameLatencyWaitMaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs,
            PreviewD3DFrameStatsSampleCount = previewRuntime.D3DFrameStatsSampleCount,
            PreviewD3DFrameStatsSuccessCount = previewRuntime.D3DFrameStatsSuccessCount,
            PreviewD3DFrameStatsFailureCount = previewRuntime.D3DFrameStatsFailureCount,
            PreviewD3DFrameStatsLastError = previewRuntime.D3DFrameStatsLastError,
            PreviewD3DFrameStatsPresentCount = previewRuntime.D3DFrameStatsPresentCount,
            PreviewD3DFrameStatsPresentRefreshCount = previewRuntime.D3DFrameStatsPresentRefreshCount,
            PreviewD3DFrameStatsSyncRefreshCount = previewRuntime.D3DFrameStatsSyncRefreshCount,
            PreviewD3DFrameStatsSyncQpcTime = previewRuntime.D3DFrameStatsSyncQpcTime,
            PreviewD3DFrameStatsLastPresentDelta = previewRuntime.D3DFrameStatsLastPresentDelta,
            PreviewD3DFrameStatsLastPresentRefreshDelta = previewRuntime.D3DFrameStatsLastPresentRefreshDelta,
            PreviewD3DFrameStatsLastSyncRefreshDelta = previewRuntime.D3DFrameStatsLastSyncRefreshDelta,
            PreviewD3DFrameStatsMissedRefreshCount = previewRuntime.D3DFrameStatsMissedRefreshCount,
            PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,
            PreviewD3DFrameStatsRecentFailureCount = recentD3DStatsFailures,
            PreviewD3DLastSubmittedPreviewPresentId = previewRuntime.D3DLastSubmittedPreviewPresentId,
            PreviewD3DLastSubmittedSourceSequenceNumber = previewRuntime.D3DLastSubmittedSourceSequenceNumber,
            PreviewD3DLastSubmittedSourcePtsTicks = previewRuntime.D3DLastSubmittedSourcePtsTicks,
            PreviewD3DLastSubmittedQpc = previewRuntime.D3DLastSubmittedQpc,
            PreviewD3DLastSubmittedUtcUnixMs = previewRuntime.D3DLastSubmittedUtcUnixMs,
            PreviewD3DLastRenderedPreviewPresentId = previewRuntime.D3DLastRenderedPreviewPresentId,
            PreviewD3DLastRenderedSourceSequenceNumber = previewRuntime.D3DLastRenderedSourceSequenceNumber,
            PreviewD3DLastRenderedSourcePtsTicks = previewRuntime.D3DLastRenderedSourcePtsTicks,
            PreviewD3DLastRenderedQpc = previewRuntime.D3DLastRenderedQpc,
            PreviewD3DLastRenderedUtcUnixMs = previewRuntime.D3DLastRenderedUtcUnixMs,
            PreviewD3DLastRenderedSchedulerToPresentMs = previewRuntime.D3DLastRenderedSchedulerToPresentMs,
            PreviewD3DLastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,
            PreviewD3DLastDroppedPreviewPresentId = previewRuntime.D3DLastDroppedPreviewPresentId,
            PreviewD3DLastDroppedSourceSequenceNumber = previewRuntime.D3DLastDroppedSourceSequenceNumber,
            PreviewD3DLastDroppedSourcePtsTicks = previewRuntime.D3DLastDroppedSourcePtsTicks,
            PreviewD3DLastDroppedQpc = previewRuntime.D3DLastDroppedQpc,
            PreviewD3DLastDroppedUtcUnixMs = previewRuntime.D3DLastDroppedUtcUnixMs,
            PreviewD3DLastDropReason = previewRuntime.D3DLastDropReason,
            PreviewD3DRecentSlowFrames = previewRuntime.D3DRecentSlowFrames,
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
            RecordingVideoQueueLatencyP99Ms = health.RecordingVideoQueueLatencyP99Ms,
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
            FatalCleanupInProgress = health.FatalCleanupInProgress,
            FlashbackCleanupInProgress = health.FlashbackCleanupInProgress,
            FlashbackForceRotateActive = health.FlashbackForceRotateActive,
            FlashbackForceRotateRequested = health.FlashbackForceRotateRequested,
            FlashbackForceRotateDraining = health.FlashbackForceRotateDraining,
            FlashbackTempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,
            FlashbackStartupCacheBudgetBytes = health.FlashbackStartupCacheBudgetBytes,
            FlashbackStartupCacheBytes = health.FlashbackStartupCacheBytes,
            FlashbackStartupCacheSessionCount = health.FlashbackStartupCacheSessionCount,
            FlashbackStartupCacheDeletedSessionCount = health.FlashbackStartupCacheDeletedSessionCount,
            FlashbackStartupCacheFreedBytes = health.FlashbackStartupCacheFreedBytes,
            FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,
            FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,
            FlashbackVideoQueueMaxDepth = health.FlashbackVideoQueueMaxDepth,
            FlashbackVideoFramesSubmittedToEncoder = health.FlashbackVideoFramesSubmittedToEncoder,
            FlashbackVideoEncoderPts = health.FlashbackVideoEncoderPts,
            FlashbackVideoEncoderPacketsWritten = health.FlashbackVideoEncoderPacketsWritten,
            FlashbackVideoEncoderDroppedFrames = health.FlashbackVideoEncoderDroppedFrames,
            FlashbackVideoSequenceGaps = health.FlashbackVideoSequenceGaps,
            FlashbackVideoQueueRejectedFrames = health.FlashbackVideoQueueRejectedFrames,
            FlashbackVideoQueueLastRejectReason = health.FlashbackVideoQueueLastRejectReason,
            FlashbackVideoQueueOldestFrameAgeMs = health.FlashbackVideoQueueOldestFrameAgeMs,
            FlashbackVideoQueueLastLatencyMs = health.FlashbackVideoQueueLastLatencyMs,
            FlashbackVideoQueueLatencySampleCount = health.FlashbackVideoQueueLatencySampleCount,
            FlashbackVideoQueueLatencyAvgMs = health.FlashbackVideoQueueLatencyAvgMs,
            FlashbackVideoQueueLatencyP95Ms = health.FlashbackVideoQueueLatencyP95Ms,
            FlashbackVideoQueueLatencyP99Ms = health.FlashbackVideoQueueLatencyP99Ms,
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
            FlashbackGpuQueueRejectedFrames = health.FlashbackGpuQueueRejectedFrames,
            FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,
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
            CaptureCadenceP99IntervalMs = health.CaptureCadenceP99IntervalMs,
            CaptureCadenceMaxIntervalMs = health.CaptureCadenceMaxIntervalMs,
            CaptureCadenceOnePercentLowFps = health.CaptureCadenceOnePercentLowFps,
            CaptureCadenceFivePercentLowFps = health.CaptureCadenceFivePercentLowFps,
            CaptureCadenceSampleDurationMs = health.CaptureCadenceSampleDurationMs,
            CaptureCadenceRecentIntervalsMs = health.CaptureCadenceRecentIntervalsMs,
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
            MjpegPreviewJitterResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount,
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
            MjpegPreviewJitterClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            MjpegPreviewJitterTargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount,
            MjpegPreviewJitterLastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            MjpegPreviewJitterLastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            MjpegPreviewJitterLastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            MjpegPreviewJitterLastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            MjpegPreviewJitterLastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            MjpegPreviewJitterLastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,
            MjpegPreviewJitterLastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            MjpegPreviewJitterLastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            MjpegPreviewJitterLastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            MjpegPreviewJitterLastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            MjpegPreviewJitterLastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            MjpegPreviewJitterMaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount,
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
            ProcessCpuPercent = processCpuPercent,
            ProcessCpuTotalProcessorTimeMs = processCpuTotalMs,
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
            FlashbackTotalBytesWritten = health.FlashbackTotalBytesWritten,
            FlashbackOutputBytes = health.FlashbackOutputBytes,
            FlashbackFilePath = health.FlashbackFilePath,
            FlashbackEncodedFrames = health.FlashbackEncodedFrames,
            FlashbackDroppedFrames = health.FlashbackDroppedFrames,
            FlashbackGpuEncoding = health.FlashbackGpuEncoding,
            FlashbackBackendSettingsStale = health.FlashbackBackendSettingsStale,
            FlashbackBackendSettingsStaleReason = health.FlashbackBackendSettingsStaleReason,
            FlashbackBackendActiveFormat = health.FlashbackBackendActiveFormat,
            FlashbackBackendRequestedFormat = health.FlashbackBackendRequestedFormat,
            FlashbackBackendActivePreset = health.FlashbackBackendActivePreset,
            FlashbackBackendRequestedPreset = health.FlashbackBackendRequestedPreset,
            FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,
            FlashbackCodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,
            EncoderCodecName = health.EncoderCodecName,
            EncoderTargetBitRate = health.EncoderTargetBitRate,
            EncoderWidth = health.EncoderWidth,
            EncoderHeight = health.EncoderHeight,
            EncoderFrameRate = health.EncoderFrameRate,
            EncoderFrameRateNumerator = health.EncoderFrameRateNumerator,
            EncoderFrameRateDenominator = health.EncoderFrameRateDenominator,
            FlashbackVideoQueueDepth = health.FlashbackVideoQueueDepth,
            FlashbackAudioQueueDepth = health.FlashbackAudioQueueDepth,
            FlashbackAudioQueueCapacity = health.FlashbackAudioQueueCapacity,
            FlashbackPlaybackState = health.FlashbackPlaybackState,
            FlashbackPlaybackPositionMs = health.FlashbackPlaybackPositionMs,
            FlashbackDecoderHwAccel = health.FlashbackDecoderHwAccel,
            FlashbackPlaybackFrameCount = health.FlashbackPlaybackFrameCount,
            FlashbackPlaybackLateFrames = health.FlashbackPlaybackLateFrames,
            FlashbackPlaybackDroppedFrames = health.FlashbackPlaybackDroppedFrames,
            FlashbackPlaybackAudioMasterDelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            FlashbackPlaybackAudioMasterDelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            FlashbackPlaybackAudioMasterFallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            FlashbackPlaybackAudioMasterStaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            FlashbackPlaybackAudioMasterLastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            FlashbackPlaybackAudioMasterLastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs,
            FlashbackPlaybackSegmentSwitches = health.FlashbackPlaybackSegmentSwitches,
            FlashbackPlaybackFmp4Reopens = health.FlashbackPlaybackFmp4Reopens,
            FlashbackPlaybackWriteHeadWaits = health.FlashbackPlaybackWriteHeadWaits,
            FlashbackPlaybackNearLiveSnaps = health.FlashbackPlaybackNearLiveSnaps,
            FlashbackPlaybackDecodeErrorSnaps = health.FlashbackPlaybackDecodeErrorSnaps,
            FlashbackPlaybackSubmitFailures = health.FlashbackPlaybackSubmitFailures,
            FlashbackPlaybackLastDropUtcUnixMs = health.FlashbackPlaybackLastDropUtcUnixMs,
            FlashbackPlaybackLastDropReason = health.FlashbackPlaybackLastDropReason,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = health.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            FlashbackPlaybackLastSubmitFailure = health.FlashbackPlaybackLastSubmitFailure,
            FlashbackPlaybackLastSegmentSwitchUtcUnixMs = health.FlashbackPlaybackLastSegmentSwitchUtcUnixMs,
            FlashbackPlaybackLastFmp4ReopenUtcUnixMs = health.FlashbackPlaybackLastFmp4ReopenUtcUnixMs,
            FlashbackPlaybackLastWriteHeadWaitGapMs = health.FlashbackPlaybackLastWriteHeadWaitGapMs,
            FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,
            FlashbackPlaybackObservedFps = health.FlashbackPlaybackObservedFps,
            FlashbackPlaybackAvgFrameMs = health.FlashbackPlaybackAvgFrameMs,
            FlashbackPlaybackCadenceSampleCount = health.FlashbackPlaybackCadenceSampleCount,
            FlashbackPlaybackP95FrameMs = health.FlashbackPlaybackP95FrameMs,
            FlashbackPlaybackP99FrameMs = health.FlashbackPlaybackP99FrameMs,
            FlashbackPlaybackMaxFrameMs = health.FlashbackPlaybackMaxFrameMs,
            FlashbackPlaybackSlowFrames = health.FlashbackPlaybackSlowFrames,
            FlashbackPlaybackSlowFramePercent = health.FlashbackPlaybackSlowFramePercent,
            FlashbackPlaybackOnePercentLowFps = health.FlashbackPlaybackOnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = health.FlashbackPlaybackFivePercentLowFps,
            FlashbackPlaybackSampleDurationMs = health.FlashbackPlaybackSampleDurationMs,
            FlashbackPlaybackRecentFrameIntervalsMs = health.FlashbackPlaybackRecentFrameIntervalsMs,
            FlashbackPlaybackPtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,
            FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs = health.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs,
            FlashbackPlaybackLastPtsCadenceDeltaMs = health.FlashbackPlaybackLastPtsCadenceDeltaMs,
            FlashbackPlaybackLastPtsCadenceExpectedMs = health.FlashbackPlaybackLastPtsCadenceExpectedMs,
            FlashbackPlaybackSeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = health.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            FlashbackPlaybackDecodeSampleCount = health.FlashbackPlaybackDecodeSampleCount,
            FlashbackPlaybackDecodeAvgMs = health.FlashbackPlaybackDecodeAvgMs,
            FlashbackPlaybackDecodeP95Ms = health.FlashbackPlaybackDecodeP95Ms,
            FlashbackPlaybackDecodeP99Ms = health.FlashbackPlaybackDecodeP99Ms,
            FlashbackPlaybackDecodeMaxMs = health.FlashbackPlaybackDecodeMaxMs,
            FlashbackPlaybackMaxDecodePhase = health.FlashbackPlaybackMaxDecodePhase,
            FlashbackPlaybackMaxDecodeReceiveMs = health.FlashbackPlaybackMaxDecodeReceiveMs,
            FlashbackPlaybackMaxDecodeFeedMs = health.FlashbackPlaybackMaxDecodeFeedMs,
            FlashbackPlaybackMaxDecodeReadMs = health.FlashbackPlaybackMaxDecodeReadMs,
            FlashbackPlaybackMaxDecodeSendMs = health.FlashbackPlaybackMaxDecodeSendMs,
            FlashbackPlaybackMaxDecodeAudioMs = health.FlashbackPlaybackMaxDecodeAudioMs,
            FlashbackPlaybackMaxDecodeConvertMs = health.FlashbackPlaybackMaxDecodeConvertMs,
            FlashbackPlaybackMaxDecodeUtcUnixMs = health.FlashbackPlaybackMaxDecodeUtcUnixMs,
            FlashbackPlaybackMaxDecodePositionMs = health.FlashbackPlaybackMaxDecodePositionMs,
            FlashbackAvDriftMs = health.FlashbackAvDriftMs,
            FlashbackPlaybackThreadAlive = health.FlashbackPlaybackThreadAlive,
            FlashbackPlaybackCommandsEnqueued = health.FlashbackPlaybackCommandsEnqueued,
            FlashbackPlaybackCommandsProcessed = health.FlashbackPlaybackCommandsProcessed,
            FlashbackPlaybackCommandsDropped = health.FlashbackPlaybackCommandsDropped,
            FlashbackPlaybackCommandsSkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            FlashbackPlaybackScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            FlashbackPlaybackSeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            FlashbackPlaybackCommandQueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            FlashbackPlaybackPendingCommands = health.FlashbackPlaybackPendingCommands,
            FlashbackPlaybackMaxPendingCommands = health.FlashbackPlaybackMaxPendingCommands,
            FlashbackPlaybackLastCommandQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            FlashbackPlaybackLastCommandQueued = health.FlashbackPlaybackLastCommandQueued,
            FlashbackPlaybackLastCommandProcessed = health.FlashbackPlaybackLastCommandProcessed,
            FlashbackPlaybackLastCommandQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            FlashbackPlaybackLastCommandProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,
            FlashbackExportActive = health.FlashbackExportActive,
            FlashbackExportId = health.FlashbackExportId,
            FlashbackExportStatus = health.FlashbackExportStatus,
            FlashbackExportOutputPath = health.FlashbackExportOutputPath,
            FlashbackExportStartedUtcUnixMs = health.FlashbackExportStartedUtcUnixMs,
            FlashbackExportLastProgressUtcUnixMs = health.FlashbackExportLastProgressUtcUnixMs,
            FlashbackExportCompletedUtcUnixMs = health.FlashbackExportCompletedUtcUnixMs,
            FlashbackExportElapsedMs = health.FlashbackExportElapsedMs,
            FlashbackExportLastProgressAgeMs = health.FlashbackExportLastProgressAgeMs,
            FlashbackExportOutputBytes = health.FlashbackExportOutputBytes,
            FlashbackExportThroughputBytesPerSec = health.FlashbackExportThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = health.FlashbackExportSegmentsProcessed,
            FlashbackExportTotalSegments = health.FlashbackExportTotalSegments,
            FlashbackExportPercent = health.FlashbackExportPercent,
            FlashbackExportInPointMs = health.FlashbackExportInPointMs,
            FlashbackExportOutPointMs = health.FlashbackExportOutPointMs,
            FlashbackExportMessage = health.FlashbackExportMessage,
            FlashbackExportFailureKind = health.FlashbackExportFailureKind,
            FlashbackExportForceRotateFallbacks = health.FlashbackExportForceRotateFallbacks,
            FlashbackExportLastForceRotateFallbackUtcUnixMs = health.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            FlashbackExportLastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,
            FlashbackExportLastForceRotateFallbackInPointMs = health.FlashbackExportLastForceRotateFallbackInPointMs,
            FlashbackExportLastForceRotateFallbackOutPointMs = health.FlashbackExportLastForceRotateFallbackOutPointMs,
            LastExportId = health.LastExportId,
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

        UpdateAlerts(snapshot, recentFlashbackRecording);

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
                CaptureCadenceAverageMs = snapshot.CaptureCadenceAverageIntervalMs,
                CaptureCadenceP95Ms = snapshot.CaptureCadenceP95IntervalMs,
                CaptureCadenceP99Ms = snapshot.CaptureCadenceP99IntervalMs,
                CaptureCadenceMaxMs = snapshot.CaptureCadenceMaxIntervalMs,
                CaptureCadenceOnePercentLowFps = snapshot.CaptureCadenceOnePercentLowFps,
                CaptureCadenceFivePercentLowFps = snapshot.CaptureCadenceFivePercentLowFps,
                PreviewCadenceAverageMs = snapshot.PreviewCadenceAverageIntervalMs,
                PreviewCadenceP95Ms = snapshot.PreviewCadenceP95IntervalMs,
                PreviewCadenceP99Ms = snapshot.PreviewCadenceP99IntervalMs,
                PreviewCadenceMaxMs = snapshot.PreviewCadenceMaxIntervalMs,
                PreviewCadenceOnePercentLowFps = snapshot.PreviewCadenceOnePercentLowFps,
                PreviewCadenceFivePercentLowFps = snapshot.PreviewCadenceFivePercentLowFps,
                PreviewCadenceSlowFramePercent = snapshot.PreviewCadenceSlowFramePercent,
                VisualCadenceChangeObservedFps = snapshot.VisualCadenceChangeObservedFps,
                VisualCadenceRepeatFramePercent = snapshot.VisualCadenceRepeatFramePercent,
                VisualCadenceMotionConfidence = snapshot.VisualCadenceMotionConfidence,
                MjpegPacketHashInputObservedFps = snapshot.MjpegPacketHashInputObservedFps,
                MjpegPacketHashUniqueObservedFps = snapshot.MjpegPacketHashUniqueObservedFps,
                MjpegPacketHashDuplicateFramePercent = snapshot.MjpegPacketHashDuplicateFramePercent,
                MjpegPreviewJitterEnabled = snapshot.MjpegPreviewJitterEnabled,
                MjpegPreviewJitterTargetDepth = snapshot.MjpegPreviewJitterTargetDepth,
                MjpegPreviewJitterMaxDepth = snapshot.MjpegPreviewJitterMaxDepth,
                MjpegPreviewJitterQueueDepth = snapshot.MjpegPreviewJitterQueueDepth,
                MjpegPreviewJitterTotalDropped = snapshot.MjpegPreviewJitterTotalDropped,
                MjpegPreviewJitterDeadlineDropCount = snapshot.MjpegPreviewJitterDeadlineDropCount,
                MjpegPreviewJitterClearedDropCount = snapshot.MjpegPreviewJitterClearedDropCount,
                MjpegPreviewJitterUnderflowCount = snapshot.MjpegPreviewJitterUnderflowCount,
                MjpegPreviewJitterResumeReprimeCount = snapshot.MjpegPreviewJitterResumeReprimeCount,
                MjpegPreviewJitterLatencyP95Ms = snapshot.MjpegPreviewJitterLatencyP95Ms,
                MjpegPreviewJitterLatencyMaxMs = snapshot.MjpegPreviewJitterLatencyMaxMs,
                MjpegPreviewJitterLastDropReason = snapshot.MjpegPreviewJitterLastDropReason,
                MjpegPreviewJitterLastUnderflowReason = snapshot.MjpegPreviewJitterLastUnderflowReason,
                MjpegPreviewJitterLastUnderflowInputAgeMs = snapshot.MjpegPreviewJitterLastUnderflowInputAgeMs,
                MjpegPreviewJitterLastUnderflowOutputAgeMs = snapshot.MjpegPreviewJitterLastUnderflowOutputAgeMs,
                MjpegPreviewJitterMaxScheduleLateMs = snapshot.MjpegPreviewJitterMaxScheduleLateMs,
                MjpegPreviewJitterScheduleLateCount = snapshot.MjpegPreviewJitterScheduleLateCount,
                PreviewD3DPendingFrameCount = snapshot.PreviewD3DPendingFrameCount,
                PreviewD3DPresentCallP95Ms = snapshot.PreviewD3DPresentCallP95Ms,
                PreviewD3DTotalFrameCpuP95Ms = snapshot.PreviewD3DTotalFrameCpuP95Ms,
                PreviewD3DInputUploadCpuP99Ms = snapshot.PreviewD3DInputUploadCpuP99Ms,
                PreviewD3DRenderSubmitCpuP99Ms = snapshot.PreviewD3DRenderSubmitCpuP99Ms,
                PreviewD3DPresentCallP99Ms = snapshot.PreviewD3DPresentCallP99Ms,
                PreviewD3DTotalFrameCpuP99Ms = snapshot.PreviewD3DTotalFrameCpuP99Ms,
                PreviewD3DPipelineLatencyP95Ms = snapshot.PreviewD3DPipelineLatencyP95Ms,
                PreviewD3DPipelineLatencyP99Ms = snapshot.PreviewD3DPipelineLatencyP99Ms,
                PreviewD3DPipelineLatencyMaxMs = snapshot.PreviewD3DPipelineLatencyMaxMs,
                PreviewD3DFrameLatencyWaitTimeoutCount = snapshot.PreviewD3DFrameLatencyWaitTimeoutCount,
                PreviewD3DFrameLatencyWaitP95Ms = snapshot.PreviewD3DFrameLatencyWaitP95Ms,
                PreviewD3DFrameLatencyWaitMaxMs = snapshot.PreviewD3DFrameLatencyWaitMaxMs,
                PreviewD3DFrameStatsRecentMissedRefreshCount = snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount,
                PreviewD3DFrameStatsRecentFailureCount = snapshot.PreviewD3DFrameStatsRecentFailureCount,
                PreviewD3DLastRenderedSchedulerToPresentMs = snapshot.PreviewD3DLastRenderedSchedulerToPresentMs,
                PreviewD3DLastRenderedPipelineLatencyMs = snapshot.PreviewD3DLastRenderedPipelineLatencyMs,
                PreviewD3DLastDropReason = snapshot.PreviewD3DLastDropReason,
                PreviewPacingLikelySlowStage = snapshot.PreviewPacingLikelySlowStage,
                PreviewPacingSlowStageConfidence = snapshot.PreviewPacingSlowStageConfidence,
                PreviewPacingSlowStageEvidence = snapshot.PreviewPacingSlowStageEvidence,
                FlashbackPlaybackState = snapshot.FlashbackPlaybackState,
                FlashbackPlaybackTargetFps = snapshot.FlashbackPlaybackTargetFps,
                FlashbackPlaybackObservedFps = snapshot.FlashbackPlaybackObservedFps,
                FlashbackPlaybackP99FrameMs = snapshot.FlashbackPlaybackP99FrameMs,
                FlashbackPlaybackMaxFrameMs = snapshot.FlashbackPlaybackMaxFrameMs,
                FlashbackPlaybackOnePercentLowFps = snapshot.FlashbackPlaybackOnePercentLowFps,
                FlashbackPlaybackFivePercentLowFps = snapshot.FlashbackPlaybackFivePercentLowFps,
                FlashbackPlaybackSlowFramePercent = snapshot.FlashbackPlaybackSlowFramePercent,
                FlashbackPlaybackDecodeP99Ms = snapshot.FlashbackPlaybackDecodeP99Ms,
                FlashbackPlaybackDecodeMaxMs = snapshot.FlashbackPlaybackDecodeMaxMs,
                FlashbackPlaybackMaxDecodePhase = snapshot.FlashbackPlaybackMaxDecodePhase,
                FlashbackPlaybackMaxDecodeReceiveMs = snapshot.FlashbackPlaybackMaxDecodeReceiveMs,
                FlashbackPlaybackMaxDecodeFeedMs = snapshot.FlashbackPlaybackMaxDecodeFeedMs,
                FlashbackPlaybackMaxDecodeReadMs = snapshot.FlashbackPlaybackMaxDecodeReadMs,
                FlashbackPlaybackMaxDecodeSendMs = snapshot.FlashbackPlaybackMaxDecodeSendMs,
                FlashbackPlaybackMaxDecodeAudioMs = snapshot.FlashbackPlaybackMaxDecodeAudioMs,
                FlashbackPlaybackMaxDecodeConvertMs = snapshot.FlashbackPlaybackMaxDecodeConvertMs,
                FlashbackPlaybackMaxDecodeUtcUnixMs = snapshot.FlashbackPlaybackMaxDecodeUtcUnixMs,
                FlashbackPlaybackMaxDecodePositionMs = snapshot.FlashbackPlaybackMaxDecodePositionMs,
                FlashbackPlaybackSeekForwardDecodeCapHits = snapshot.FlashbackPlaybackSeekForwardDecodeCapHits,
                FlashbackPlaybackLastSeekHitForwardDecodeCap = snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap,
                FlashbackPlaybackPendingCommands = snapshot.FlashbackPlaybackPendingCommands,
                FlashbackPlaybackMaxPendingCommands = snapshot.FlashbackPlaybackMaxPendingCommands,
                FlashbackPlaybackCommandsEnqueued = snapshot.FlashbackPlaybackCommandsEnqueued,
                FlashbackPlaybackCommandsProcessed = snapshot.FlashbackPlaybackCommandsProcessed,
                FlashbackPlaybackCommandsDropped = snapshot.FlashbackPlaybackCommandsDropped,
                FlashbackPlaybackCommandsSkippedNotReady = snapshot.FlashbackPlaybackCommandsSkippedNotReady,
                FlashbackPlaybackScrubUpdatesCoalesced = snapshot.FlashbackPlaybackScrubUpdatesCoalesced,
                FlashbackPlaybackSeekCommandsCoalesced = snapshot.FlashbackPlaybackSeekCommandsCoalesced,
                FlashbackPlaybackLastCommandQueued = snapshot.FlashbackPlaybackLastCommandQueued,
                FlashbackPlaybackLastCommandProcessed = snapshot.FlashbackPlaybackLastCommandProcessed,
                FlashbackPlaybackMaxCommandQueueLatencyMs = snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs,
                FlashbackPlaybackMaxCommandQueueLatencyCommand = snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand,
                FlashbackPlaybackSubmitFailures = snapshot.FlashbackPlaybackSubmitFailures,
                FlashbackPlaybackLastDropUtcUnixMs = snapshot.FlashbackPlaybackLastDropUtcUnixMs,
                FlashbackPlaybackLastDropReason = snapshot.FlashbackPlaybackLastDropReason,
                FlashbackPlaybackLastSubmitFailureUtcUnixMs = snapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
                FlashbackPlaybackLastSubmitFailure = snapshot.FlashbackPlaybackLastSubmitFailure,
                FlashbackPlaybackDroppedFrames = snapshot.FlashbackPlaybackDroppedFrames,
                FlashbackPlaybackAudioMasterDelayDoubles = snapshot.FlashbackPlaybackAudioMasterDelayDoubles,
                FlashbackPlaybackAudioMasterDelayShrinks = snapshot.FlashbackPlaybackAudioMasterDelayShrinks,
                FlashbackPlaybackAudioMasterFallbacks = snapshot.FlashbackPlaybackAudioMasterFallbacks,
                FlashbackPlaybackAudioMasterUnavailableFallbacks = snapshot.FlashbackPlaybackAudioMasterUnavailableFallbacks,
                FlashbackPlaybackAudioMasterStaleFallbacks = snapshot.FlashbackPlaybackAudioMasterStaleFallbacks,
                FlashbackPlaybackAudioMasterDriftOutlierFallbacks = snapshot.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
                FlashbackPlaybackAudioMasterLastFallbackReason = snapshot.FlashbackPlaybackAudioMasterLastFallbackReason,
                FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = snapshot.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs,
                FlashbackPlaybackSegmentSwitches = snapshot.FlashbackPlaybackSegmentSwitches,
                FlashbackPlaybackFmp4Reopens = snapshot.FlashbackPlaybackFmp4Reopens,
                FlashbackPlaybackWriteHeadWaits = snapshot.FlashbackPlaybackWriteHeadWaits,
                FlashbackPlaybackNearLiveSnaps = snapshot.FlashbackPlaybackNearLiveSnaps,
                FlashbackPlaybackDecodeErrorSnaps = snapshot.FlashbackPlaybackDecodeErrorSnaps,
                FlashbackPlaybackLastWriteHeadWaitGapMs = snapshot.FlashbackPlaybackLastWriteHeadWaitGapMs,
                FlashbackPlaybackLastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs,
                FlashbackPlaybackLastCommandFailure = snapshot.FlashbackPlaybackLastCommandFailure,
                FlashbackBackendSettingsStale = snapshot.FlashbackBackendSettingsStale,
                FlashbackBackendSettingsStaleReason = snapshot.FlashbackBackendSettingsStaleReason,
                FlashbackBackendActiveFormat = snapshot.FlashbackBackendActiveFormat,
                FlashbackBackendRequestedFormat = snapshot.FlashbackBackendRequestedFormat,
                FlashbackBackendActivePreset = snapshot.FlashbackBackendActivePreset,
                FlashbackBackendRequestedPreset = snapshot.FlashbackBackendRequestedPreset,
                FlashbackVideoQueueRejectedFrames = snapshot.FlashbackVideoQueueRejectedFrames,
                FlashbackVideoQueueLastRejectReason = snapshot.FlashbackVideoQueueLastRejectReason,
                FlashbackGpuQueueRejectedFrames = snapshot.FlashbackGpuQueueRejectedFrames,
                FlashbackGpuQueueLastRejectReason = snapshot.FlashbackGpuQueueLastRejectReason,
                FatalCleanupInProgress = snapshot.FatalCleanupInProgress,
                FlashbackCleanupInProgress = snapshot.FlashbackCleanupInProgress,
                FlashbackForceRotateRequested = snapshot.FlashbackForceRotateRequested,
                FlashbackForceRotateDraining = snapshot.FlashbackForceRotateDraining,
                FlashbackExportActive = snapshot.FlashbackExportActive,
                FlashbackExportStatus = snapshot.FlashbackExportStatus,
                FlashbackExportFailureKind = snapshot.FlashbackExportFailureKind,
                FlashbackExportElapsedMs = snapshot.FlashbackExportElapsedMs,
                FlashbackExportLastProgressAgeMs = snapshot.FlashbackExportLastProgressAgeMs,
                FlashbackExportOutputBytes = snapshot.FlashbackExportOutputBytes,
                FlashbackExportThroughputBytesPerSec = snapshot.FlashbackExportThroughputBytesPerSec,
                FlashbackExportSegmentsProcessed = snapshot.FlashbackExportSegmentsProcessed,
                FlashbackExportTotalSegments = snapshot.FlashbackExportTotalSegments,
                FlashbackExportPercent = snapshot.FlashbackExportPercent,
                FlashbackExportInPointMs = snapshot.FlashbackExportInPointMs,
                FlashbackExportOutPointMs = snapshot.FlashbackExportOutPointMs,
                FlashbackExportMessage = snapshot.FlashbackExportMessage,
                FlashbackExportForceRotateFallbacks = snapshot.FlashbackExportForceRotateFallbacks,
                FlashbackExportLastForceRotateFallbackUtcUnixMs = snapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs,
                FlashbackExportLastForceRotateFallbackSegments = snapshot.FlashbackExportLastForceRotateFallbackSegments,
                FlashbackExportLastForceRotateFallbackInPointMs = snapshot.FlashbackExportLastForceRotateFallbackInPointMs,
                FlashbackExportLastForceRotateFallbackOutPointMs = snapshot.FlashbackExportLastForceRotateFallbackOutPointMs,
                PipelineLatencyMs = snapshot.EstimatedPipelineLatencyMs,
                ProcessCpuPercent = snapshot.ProcessCpuPercent,
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

}

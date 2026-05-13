using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string FormatPreviewSlowFrameAlertDetail(AutomationSnapshot snapshot)
    {
        if (snapshot.PreviewD3DRecentSlowFrames.Length <= 0)
        {
            return string.Empty;
        }

        var frame = snapshot.PreviewD3DRecentSlowFrames[^1];
        var reason = string.IsNullOrWhiteSpace(frame.SlowReason) ? "unknown" : frame.SlowReason;
        return $" latestSlowFrameReason={reason} over={frame.WorstOverBudgetMs:0.##}ms interval={frame.PresentIntervalMs:0.##}ms inputUpload={frame.InputUploadCpuMs:0.##}ms renderSubmit={frame.RenderSubmitCpuMs:0.##}ms total={frame.TotalFrameCpuMs:0.##}ms presentCall={frame.PresentCallMs:0.##}ms pipeline={frame.PipelineLatencyMs:0.##}ms pending={frame.PendingFrameCount}";
    }

    private static string FormatVisualCadenceAlertDetail(AutomationSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSampleCount <= 0)
        {
            return string.Empty;
        }

        return $" visualChanges={snapshot.VisualCadenceChangeObservedFps:0.##}fps visualOutput={snapshot.VisualCadenceOutputObservedFps:0.##}fps repeat={snapshot.VisualCadenceRepeatFramePercent:0.###}% longestRepeatRun={snapshot.VisualCadenceLongestRepeatRun} confidence={snapshot.VisualCadenceMotionConfidence}";
    }

    private static string FormatMjpegDuplicateCadenceDetail(CaptureHealthSnapshot health)
        =>
            $"mjpg fingerprint samples={health.MjpegPacketHashSampleCount} input={health.MjpegPacketHashInputObservedFps:0.##}fps unique={health.MjpegPacketHashUniqueObservedFps:0.##}fps dup={health.MjpegPacketHashDuplicateFramePercent:0.###}% pattern={health.MjpegPacketHashPattern} longestDup={health.MjpegPacketHashLongestDuplicateRun}";

    private static string FormatEncoderFrameRate(CaptureHealthSnapshot health)
    {
        if (health.EncoderFrameRateNumerator is int numerator &&
            health.EncoderFrameRateDenominator is int denominator &&
            denominator > 0)
        {
            return $"{health.EncoderFrameRate:0.##}fps({numerator}/{denominator})";
        }

        return $"{health.EncoderFrameRate:0.##}fps";
    }

    private static double ResolveFlashbackPlaybackTargetFps(double flashbackPlaybackTargetFps, double fallbackFrameRate)
        => flashbackPlaybackTargetFps > 0 ? flashbackPlaybackTargetFps : fallbackFrameRate;

    private static bool IsFlashbackPlaybackFrametimeDegraded(
        string state,
        double targetFrameRate,
        long frameCount,
        int cadenceSampleCount,
        double onePercentLowFps)
        =>
            string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase) &&
            targetFrameRate > 0 &&
            frameCount >= FlashbackPlaybackOnePercentLowMinimumFrames &&
            cadenceSampleCount >= FlashbackPlaybackOnePercentLowMinimumFrames &&
            onePercentLowFps > 0 &&
            onePercentLowFps < targetFrameRate * FlashbackPlaybackOnePercentLowWarningRatio;

    private static bool IsCaptureOnePercentLowDegraded(
        double targetFrameRate,
        int cadenceSampleCount,
        double onePercentLowFps)
        =>
            targetFrameRate > 0 &&
            cadenceSampleCount >= CapturePerfectionMinSamples &&
            onePercentLowFps > 0 &&
            onePercentLowFps < targetFrameRate * CaptureOnePercentLowWarningRatio;

    private static bool IsPreviewOnePercentLowDegraded(
        double expectedIntervalMs,
        int cadenceSampleCount,
        double onePercentLowFps)
    {
        if (expectedIntervalMs <= 0 ||
            cadenceSampleCount < PreviewPerfectionMinSamples ||
            onePercentLowFps <= 0)
        {
            return false;
        }

        var targetFrameRate = 1000.0 / expectedIntervalMs;
        return onePercentLowFps < targetFrameRate * PreviewOnePercentLowWarningRatio;
    }

    private static bool IsVisualCadenceHealthy(
        double targetFrameRate,
        int sampleCount,
        double changeObservedFps,
        double repeatFramePercent,
        long longestRepeatRun)
        =>
            targetFrameRate > 0 &&
            sampleCount >= PreviewPerfectionMinSamples &&
            changeObservedFps >= targetFrameRate * PreviewOnePercentLowWarningRatio &&
            repeatFramePercent <= 1.0 &&
            longestRepeatRun <= 1;

    private static bool IsMjpegDuplicateCadenceDetected(CaptureHealthSnapshot health)
    {
        if (health.ExpectedFrameRate < 90 ||
            health.MjpegPacketHashSampleCount < PreviewPerfectionMinSamples ||
            health.MjpegPacketHashInputObservedFps < health.ExpectedFrameRate * 0.90 ||
            health.MjpegPacketHashDuplicateFramePercent < 20.0)
        {
            return false;
        }

        var uniqueCadenceBelowTarget =
            health.MjpegPacketHashUniqueObservedFps > 0 &&
            health.MjpegPacketHashUniqueObservedFps <= health.ExpectedFrameRate * 0.75;
        var visualCadenceBelowTarget =
            health.VisualCadenceSampleCount >= PreviewPerfectionMinSamples &&
            health.VisualCadenceChangeObservedFps > 0 &&
            health.VisualCadenceChangeObservedFps <= health.ExpectedFrameRate * 0.75 &&
            health.VisualCadenceRepeatFramePercent >= 20.0;
        var telemetryBelowTarget =
            health.SourceFrameRateExact is > 0 &&
            health.SourceFrameRateExact.Value <= health.ExpectedFrameRate * 0.75;

        return uniqueCadenceBelowTarget || visualCadenceBelowTarget || telemetryBelowTarget;
    }

    private static bool IsFlashbackRecordingQueueBackedUp(
        int queueDepth,
        int queueCapacity,
        long oldestFrameAgeMs)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * FlashbackRecordingQueueDepthWarningRatio) &&
            oldestFrameAgeMs >= FlashbackRecordingQueueAgeWarningMs;

    private static bool IsFlashbackAudioQueueBackedUp(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * FlashbackAudioQueueDepthWarningRatio);

    private static bool IsFlashbackForceRotateRejectReason(string? reason)
        =>
            string.Equals(reason, "force_rotate_draining", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(reason, "force_rotate_queue_guard", StringComparison.OrdinalIgnoreCase);

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
        double captureCadenceExpectedFrameRate,
        double captureCadenceOnePercentLowFps,
        double previewCadenceExpectedIntervalMs,
        double previewCadenceOnePercentLowFps,
        bool visualCadenceHealthy,
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

            if (IsCaptureOnePercentLowDegraded(
                    captureCadenceExpectedFrameRate,
                    captureCadenceSampleCount,
                    captureCadenceOnePercentLowFps))
            {
                var target = captureCadenceExpectedFrameRate * CaptureOnePercentLowWarningRatio;
                var deficit = Math.Max(0.0, target - captureCadenceOnePercentLowFps);
                penalty += Math.Min(25, deficit * 1.5);
                reasons.Add($"capture 1% low {captureCadenceOnePercentLowFps:0.##}fps");
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

        if (isPreviewing &&
            !visualCadenceHealthy &&
            IsPreviewOnePercentLowDegraded(
                previewCadenceExpectedIntervalMs,
                previewCadenceSampleCount,
                previewCadenceOnePercentLowFps))
        {
            var target = 1000.0 / previewCadenceExpectedIntervalMs * PreviewOnePercentLowWarningRatio;
            var deficit = Math.Max(0.0, target - previewCadenceOnePercentLowFps);
            penalty += Math.Min(20, deficit * 1.25);
            reasons.Add($"preview 1% low {previewCadenceOnePercentLowFps:0.##}fps");
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

    private static DiagnosticEvaluation BuildDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        bool isPreviewing,
        bool isRecording,
        PerformanceEvaluation performance,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        FlashbackRecordingRecentCounters recentFlashbackRecording)
    {
        var sourceTarget = health.ExpectedFrameRate > 0
            ? $"{1000.0 / health.ExpectedFrameRate:0.##}ms"
            : "n/a";
        var sourceLane =
            $"source target={sourceTarget} avg={health.CaptureCadenceAverageIntervalMs:0.##}ms p95={health.CaptureCadenceP95IntervalMs:0.##}ms p99={health.CaptureCadenceP99IntervalMs:0.##}ms max={health.CaptureCadenceMaxIntervalMs:0.##}ms rate={health.CaptureCadenceObservedFps:0.##}/{health.ExpectedFrameRate:0.##}fps 1pctLow={health.CaptureCadenceOnePercentLowFps:0.##}fps gaps={health.CaptureCadenceSevereGapCount} drops={health.CaptureCadenceEstimatedDroppedFrames} ({health.CaptureCadenceEstimatedDropPercent:0.###}%)";
        var decodeLane =
            $"decode p95={health.MjpegDecodeP95Ms:0.##}ms callbackP95={health.MjpegCallbackP95Ms:0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures} recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}";
        var previewLastDropReason = string.IsNullOrWhiteSpace(health.MjpegPreviewJitterLastDropReason)
            ? "none"
            : health.MjpegPreviewJitterLastDropReason;
        var previewLane =
            $"preview scheduler target={health.MjpegPreviewJitterTargetDepth} depth={health.MjpegPreviewJitterQueueDepth}/{health.MjpegPreviewJitterMaxDepth} dropped={health.MjpegPreviewJitterTotalDropped} clearedDrops={health.MjpegPreviewJitterClearedDropCount} deadlineDrops={health.MjpegPreviewJitterDeadlineDropCount} underflows={health.MjpegPreviewJitterUnderflowCount} resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount} recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows} lastDropReason={previewLastDropReason}";
        var rendererSubmitted = Math.Max(
            previewRuntime.D3DFramesSubmitted,
            previewRuntime.D3DFramesRendered + previewRuntime.D3DFramesDropped);
        var rendererDropPercent = DiagnosticThresholds.CalculatePercent(previewRuntime.D3DFramesDropped, rendererSubmitted);
        var recentRendererSubmitted = Math.Max(
            recentRenderer.Submitted,
            recentRenderer.Rendered + recentRenderer.Dropped);
        var recentRendererDropPercent = DiagnosticThresholds.CalculatePercent(recentRenderer.Dropped, recentRendererSubmitted);
        var renderLane =
            $"render submitted={previewRuntime.D3DFramesSubmitted} rendered={previewRuntime.D3DFramesRendered} dropped={previewRuntime.D3DFramesDropped} ({rendererDropPercent:0.###}%) " +
            $"recentSubmitted={recentRendererSubmitted} recentDropped={recentRenderer.Dropped} ({recentRendererDropPercent:0.###}%) " +
            $"cpuP95={previewRuntime.D3DTotalFrameCpuP95Ms:0.##}ms cpuP99={previewRuntime.D3DTotalFrameCpuP99Ms:0.##}ms pipelineP95={previewRuntime.D3DPipelineLatencyP95Ms:0.##}ms pipelineP99={previewRuntime.D3DPipelineLatencyP99Ms:0.##}ms lastPipeline={previewRuntime.D3DLastRenderedPipelineLatencyMs:0.##}ms";
        var presentTarget = previewRuntime.DisplayCadenceExpectedIntervalMs > 0
            ? $"{previewRuntime.DisplayCadenceExpectedIntervalMs:0.##}ms"
            : "n/a";
        var dxgiStats = previewRuntime.D3DFrameStatsSuccessCount > 0
            ? $" dxgiStats ok={previewRuntime.D3DFrameStatsSuccessCount}/{previewRuntime.D3DFrameStatsSampleCount} pc={previewRuntime.D3DFrameStatsPresentCount} prc={previewRuntime.D3DFrameStatsPresentRefreshCount} prDelta={previewRuntime.D3DFrameStatsLastPresentRefreshDelta} missed={previewRuntime.D3DFrameStatsMissedRefreshCount} recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}"
            : previewRuntime.D3DFrameStatsSampleCount > 0
                ? $" dxgiStats err={previewRuntime.D3DFrameStatsLastError} fail={previewRuntime.D3DFrameStatsFailureCount}/{previewRuntime.D3DFrameStatsSampleCount} recentFail={recentD3DStatsFailures}"
                : string.Empty;
        var presentLane =
            $"present target={presentTarget} avg={previewRuntime.DisplayCadenceAverageIntervalMs:0.##}ms p95={previewRuntime.DisplayCadenceP95IntervalMs:0.##}ms p99={previewRuntime.DisplayCadenceP99IntervalMs:0.##}ms max={previewRuntime.DisplayCadenceMaxIntervalMs:0.##}ms slow={previewRuntime.DisplayCadenceSlowFramePercent:0.##}% rate={previewRuntime.DisplayCadenceObservedFps:0.##}fps 1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps sync={previewRuntime.D3DPresentSyncInterval} latency={previewRuntime.D3DMaxFrameLatency} buffers={previewRuntime.D3DSwapChainBufferCount} swap={previewRuntime.D3DSwapChainAddress}{dxgiStats}";
        var visualLane =
            $"visual crop samples={health.VisualCadenceSampleCount} output={health.VisualCadenceOutputObservedFps:0.##}fps changes={health.VisualCadenceChangeObservedFps:0.##}fps repeat={health.VisualCadenceRepeatFramePercent:0.###}% repeatFrames={health.VisualCadenceRepeatFrameCount} longestRepeatRun={health.VisualCadenceLongestRepeatRun} confidence={health.VisualCadenceMotionConfidence}";
        var mjpegDuplicateLane = FormatMjpegDuplicateCadenceDetail(health);
        var sourceSignalLane =
            $"{sourceLane} | source telemetry {health.SourceWidth ?? 0}x{health.SourceHeight ?? 0}@{(health.SourceFrameRateExact ?? 0):0.##}fps hdr={health.SourceIsHdr?.ToString() ?? "Unknown"} availability={health.SourceTelemetryAvailability}/{health.SourceTelemetryConfidence}";
        var recordingLane =
            $"recording integrity={captureRuntime.RecordingIntegrityStatus} complete={captureRuntime.RecordingIntegrityComplete} seqGaps={captureRuntime.RecordingIntegritySequenceGaps} queueDrops={captureRuntime.RecordingIntegrityQueueDroppedFrames}";
        var audioLane =
            $"audio integrity={captureRuntime.RecordingIntegrityAudioStatus} drops={captureRuntime.RecordingIntegrityAudioDropEvents} disc={captureRuntime.RecordingIntegrityAudioDiscontinuities} gaps={captureRuntime.RecordingIntegrityAudioCallbackGaps}";
        var flashbackRecordingLane =
            $"flashback recording active={health.FlashbackActive} failed={health.FlashbackEncodingFailed} type={health.FlashbackEncodingFailureType ?? "None"} " +
            $"dropped={health.FlashbackDroppedFrames} encoderDrops={health.FlashbackVideoEncoderDroppedFrames} seqGaps={health.FlashbackVideoSequenceGaps} " +
            $"queueRejects={health.FlashbackVideoQueueRejectedFrames} lastReject={health.FlashbackVideoQueueLastRejectReason ?? "None"} " +
            $"backendStale={health.FlashbackBackendSettingsStale} staleReason={health.FlashbackBackendSettingsStaleReason} activeFormat={health.FlashbackBackendActiveFormat} requestedFormat={health.FlashbackBackendRequestedFormat} activePreset={health.FlashbackBackendActivePreset} requestedPreset={health.FlashbackBackendRequestedPreset} " +
            $"gpuOverloads={health.FlashbackGpuFramesDropped} forceRotate={health.FlashbackForceRotateActive} requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining} queue={health.FlashbackVideoQueueDepth}/{health.FlashbackVideoQueueCapacity} maxQueue={health.FlashbackVideoQueueMaxDepth} " +
            $"audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity} " +
            $"queueAgeMs={health.FlashbackVideoQueueOldestFrameAgeMs} backpressure={health.FlashbackVideoBackpressureWaitMs}ms/{health.FlashbackVideoBackpressureEvents} lastBackpressure={health.FlashbackVideoBackpressureLastWaitMs}ms maxBackpressure={health.FlashbackVideoBackpressureMaxWaitMs}ms " +
            $"fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}";
        var exportFailureKind = string.IsNullOrWhiteSpace(health.FlashbackExportFailureKind)
            ? "None"
            : health.FlashbackExportFailureKind;
        var exportLane =
            $"export active={health.FlashbackExportActive} status={health.FlashbackExportStatus} kind={exportFailureKind} id={health.FlashbackExportId} lastResultId={health.LastExportId} progress={health.FlashbackExportPercent:0.##}% segments={health.FlashbackExportSegmentsProcessed}/{health.FlashbackExportTotalSegments} elapsedMs={health.FlashbackExportElapsedMs} progressAgeMs={health.FlashbackExportLastProgressAgeMs} bytes={health.FlashbackExportOutputBytes} throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##} lastProgressUtc={health.FlashbackExportLastProgressUtcUnixMs} completedUtc={health.FlashbackExportCompletedUtcUnixMs}";
        var tempCacheLane =
            $"flashback temp freeBytes={health.FlashbackTempDriveFreeBytes} cacheBytes={health.FlashbackStartupCacheBytes} budgetBytes={health.FlashbackStartupCacheBudgetBytes} sessions={health.FlashbackStartupCacheSessionCount} deleted={health.FlashbackStartupCacheDeletedSessionCount} freedBytes={health.FlashbackStartupCacheFreedBytes} overBudget={health.FlashbackStartupCacheOverBudget}";
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var playbackCommandQueueAgeMs =
            health.FlashbackPlaybackPendingCommands > 0 &&
            health.FlashbackPlaybackLastCommandQueuedUtcUnixMs > 0 &&
            health.FlashbackPlaybackLastCommandQueuedUtcUnixMs > health.FlashbackPlaybackLastCommandProcessedUtcUnixMs
                ? Math.Max(0, nowUnixMs - health.FlashbackPlaybackLastCommandQueuedUtcUnixMs)
                : 0;
        var playbackCommandFailureAgeMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0
            ? Math.Max(0, nowUnixMs - health.FlashbackPlaybackLastCommandFailureUtcUnixMs)
            : 0;
        var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)
            ? "None"
            : health.FlashbackPlaybackLastCommandFailure;
        var playbackCommandFailedRecently =
            playbackCommandFailureAgeMs > 0 &&
            playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs;
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            health.FlashbackPlaybackTargetFps,
            health.ExpectedFrameRate);
        var playbackCommandLane =
            $"playback commands pending={health.FlashbackPlaybackPendingCommands}/{health.FlashbackPlaybackCommandQueueCapacity} maxPending={health.FlashbackPlaybackMaxPendingCommands} lastLatency={health.FlashbackPlaybackLastCommandQueueLatencyMs}ms maxLatency={health.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={health.FlashbackPlaybackMaxCommandQueueLatencyCommand} lastQueued={health.FlashbackPlaybackLastCommandQueued} lastProcessed={health.FlashbackPlaybackLastCommandProcessed} queuedAge={playbackCommandQueueAgeMs}ms lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs} threadAlive={health.FlashbackPlaybackThreadAlive}";
        var playbackPerfLane =
            $"playback perf state={health.FlashbackPlaybackState} fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##} target={health.FlashbackPlaybackTargetFps:0.##} encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##} " +
            $"1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps p99={health.FlashbackPlaybackP99FrameMs:0.##}ms max={health.FlashbackPlaybackMaxFrameMs:0.##}ms slow={health.FlashbackPlaybackSlowFramePercent:0.##}% ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={health.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={health.FlashbackPlaybackLastSeekHitForwardDecodeCap} decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={health.FlashbackPlaybackDecodeMaxMs:0.##}ms decodePhase={health.FlashbackPlaybackMaxDecodePhase} decodeReceive={health.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms decodeFeed={health.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={health.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={health.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={health.FlashbackPlaybackMaxDecodePositionMs}ms samples={health.FlashbackPlaybackCadenceSampleCount} frames={health.FlashbackPlaybackFrameCount} late={health.FlashbackPlaybackLateFrames} dropped={health.FlashbackPlaybackDroppedFrames} audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={health.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={health.FlashbackPlaybackAudioMasterFallbacks} submitFailures={health.FlashbackPlaybackSubmitFailures} switches={health.FlashbackPlaybackSegmentSwitches} fmp4Reopens={health.FlashbackPlaybackFmp4Reopens} writeHeadWaits={health.FlashbackPlaybackWriteHeadWaits} nearLiveSnaps={health.FlashbackPlaybackNearLiveSnaps} decodeErrorSnaps={health.FlashbackPlaybackDecodeErrorSnaps}";
        var playbackSlow =
            string.Equals(health.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase) &&
            playbackTargetFps > 0 &&
            health.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            health.FlashbackPlaybackObservedFps > 0 &&
            health.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackFrametimeDegraded =
            IsFlashbackPlaybackFrametimeDegraded(
                health.FlashbackPlaybackState,
                playbackTargetFps,
                health.FlashbackPlaybackFrameCount,
                health.FlashbackPlaybackCadenceSampleCount,
                health.FlashbackPlaybackOnePercentLowFps);
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                health.ExpectedFrameRate,
                health.CaptureCadenceSampleCount,
                health.CaptureCadenceOnePercentLowFps);
        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                previewRuntime.DisplayCadenceExpectedIntervalMs,
                previewRuntime.DisplayCadenceSampleCount,
                previewRuntime.DisplayCadenceOnePercentLowFps);
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                health.ExpectedFrameRate,
                health.VisualCadenceSampleCount,
                health.VisualCadenceChangeObservedFps,
                health.VisualCadenceRepeatFramePercent,
                health.VisualCadenceLongestRepeatRun);
        var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);
        var flashbackTempPressure =
            health.FlashbackActive &&
            (health.FlashbackStartupCacheOverBudget ||
             (health.FlashbackTempDriveFreeBytes >= 0 && health.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes));
        var flashbackForceRotateRejectWithoutDamage =
            health.FlashbackActive &&
            health.FlashbackVideoSequenceGaps > 0 &&
            health.FlashbackVideoQueueRejectedFrames > 0 &&
            health.FlashbackDroppedFrames <= 0 &&
            health.FlashbackVideoEncoderDroppedFrames <= 0 &&
            health.FlashbackGpuFramesDropped <= 0 &&
            recentFlashbackRecording.BackpressureEvents <= 0 &&
            !IsFlashbackRecordingQueueBackedUp(
                health.FlashbackVideoQueueDepth,
                health.FlashbackVideoQueueCapacity,
                health.FlashbackVideoQueueOldestFrameAgeMs) &&
            IsFlashbackForceRotateRejectReason(health.FlashbackVideoQueueLastRejectReason);
        var flashbackRecordingRecentBackpressure =
            recentFlashbackRecording.BackpressureEvents > 0 &&
            health.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs;
        var flashbackRecordingDegraded =
            health.FlashbackActive &&
            (recentFlashbackRecording.DroppedFrames > 0 ||
             recentFlashbackRecording.EncoderDroppedFrames > 0 ||
             (!flashbackForceRotateRejectWithoutDamage &&
              recentFlashbackRecording.SequenceGaps > 0) ||
             recentFlashbackRecording.GpuFramesDropped > 0 ||
             flashbackRecordingRecentBackpressure ||
             IsFlashbackRecordingQueueBackedUp(
                 health.FlashbackVideoQueueDepth,
                 health.FlashbackVideoQueueCapacity,
                 health.FlashbackVideoQueueOldestFrameAgeMs) ||
             IsFlashbackAudioQueueBackedUp(
                 health.FlashbackAudioQueueDepth,
                 health.FlashbackAudioQueueCapacity));
        var flashbackBackendSettingsUnexpectedlyStale =
            health.FlashbackActive &&
            health.FlashbackBackendSettingsStale &&
            !isRecording;
        var flashbackExportRotationGap =
            flashbackForceRotateRejectWithoutDamage &&
            (health.FlashbackExportActive ||
             health.FlashbackForceRotateActive ||
             health.FlashbackForceRotateRequested ||
             health.FlashbackForceRotateDraining);
        var exportLastProgressAgeMs = health.FlashbackExportActive
            ? Math.Max(0, health.FlashbackExportLastProgressAgeMs)
            : 0;

        if (flashbackTempPressure)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_storage",
                "Flashback temp storage is under pressure.",
                tempCacheLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.FlashbackEncodingFailed)
        {
            return new DiagnosticEvaluation(
                "Critical",
                "flashback_recording",
                "Flashback encoder has failed.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (flashbackExportRotationGap)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_export",
                "Flashback export rotation skipped live-edge frames.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (flashbackBackendSettingsUnexpectedlyStale)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_recording",
                "Flashback backend settings differ from requested settings.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (flashbackRecordingDegraded)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_recording",
                "Flashback recording path is dropping or backing up.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.FlashbackExportActive)
        {
            if (exportLastProgressAgeMs >= FlashbackExportStallThresholdMs)
            {
                return new DiagnosticEvaluation(
                    "Warning",
                    "flashback_export",
                    "Flashback export progress is stalled.",
                    $"{exportLane} progressAgeMs={exportLastProgressAgeMs}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Busy",
                "flashback_export",
                "Flashback export is running.",
                exportLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackCommandQueueAgeMs >= FlashbackPlaybackCommandStallThresholdMs)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command queue is stalled.",
                playbackCommandLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackCommandFailedRecently)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command failed recently.",
                playbackCommandLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback is below target rate.",
                playbackPerfLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackFrametimeDegraded)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback frametime is below target.",
                playbackPerfLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.FlashbackPlaybackSubmitFailures > 0)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback frame submission failed.",
                playbackPerfLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (!isPreviewing && !isRecording)
        {
            return new DiagnosticEvaluation(
                "Idle",
                "diagnostic_unavailable",
                "Preview and recording are idle.",
                "Start preview or recording to collect live frame-lane diagnostics.",
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.CaptureCadenceSampleCount < 30)
        {
            return new DiagnosticEvaluation(
                "WarmingUp",
                "diagnostic_unavailable",
                "Waiting for enough capture cadence samples.",
                sourceLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var recordingIntegrityIncomplete =
            string.Equals(captureRuntime.RecordingIntegrityStatus, "Incomplete", StringComparison.OrdinalIgnoreCase);
        var recordingIntegrityFailed =
            health.RecordingEncodingFailed ||
            (recordingIntegrityIncomplete && !isRecording);

        if (recordingIntegrityFailed)
        {
            return new DiagnosticEvaluation(
                "Critical",
                "recording",
                "Recording integrity is the likely failure point.",
                recordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (!string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "Clean", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "Disabled", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "NotStarted", StringComparison.OrdinalIgnoreCase))
        {
            return new DiagnosticEvaluation(
                "Warning",
                "audio",
                "Audio integrity is degraded.",
                audioLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.CaptureCadenceEstimatedDroppedFrames > 0 ||
            health.CaptureCadenceSevereGapCount > 0 ||
            health.CaptureCadenceEstimatedDropPercent > 0.1)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_capture",
                "Source/capture cadence is the likely stutter stage.",
                sourceLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (captureOnePercentLowDegraded)
        {
            if (isPreviewing &&
                visualCadenceHealthy &&
                health.CaptureCadenceEstimatedDroppedFrames <= 0 &&
                health.CaptureCadenceSevereGapCount <= 0 &&
                health.CaptureCadenceEstimatedDropPercent <= 0)
            {
                return new DiagnosticEvaluation(
                    "Healthy",
                    "none",
                    "Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.",
                    $"{sourceLane} | {visualLane}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Warning",
                "source_capture",
                "Source/capture 1% low is below target.",
                sourceLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (mjpegDuplicateCadenceDetected)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_signal",
                "Captured HFR MJPEG cadence contains repeated source frames.",
                $"{mjpegDuplicateLane} | {visualLane} | {sourceSignalLane}",
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (recentMjpeg.DecodeFailures > 0 ||
            recentMjpeg.EmitFailures > 0 ||
            recentMjpeg.CompressedQueueDrops > 0 ||
            recentMjpeg.TotalDropped > 0)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "mjpeg_decode",
                "MJPEG decode/reorder is dropping or failing frames.",
                decodeLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var previewSubmitFailed = string.Equals(
            health.MjpegPreviewJitterLastDropReason,
            "submit-failed",
            StringComparison.OrdinalIgnoreCase);
        if (previewSubmitFailed ||
            (recentPreviewDeadlineDrops > 0 && !visualCadenceHealthy) ||
            recentPreviewUnderflows > 3)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "preview_scheduler",
                previewSubmitFailed
                    ? "Preview scheduler failed to submit frames."
                    : "Preview scheduler is skipping stale or missing frames.",
                previewLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (recentRendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples &&
            recentRendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "renderer",
                "Renderer pacing is the likely preview bottleneck.",
                renderLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var presentCadenceOverBudget =
            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&
            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;
        var unsyncedPresentCallSlow =
            previewRuntime.D3DPresentSyncInterval == 0 &&
            previewRuntime.D3DPresentCallP95Ms > 4.0;
        if (presentCadenceOverBudget ||
            unsyncedPresentCallSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "present_display",
                "Present/display cadence is the likely preview bottleneck.",
                presentLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (previewOnePercentLowDegraded)
        {
            if (visualCadenceHealthy)
            {
                return new DiagnosticEvaluation(
                    "Healthy",
                    "none",
                    "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.",
                    $"{presentLane} | {visualLane}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Warning",
                "present_display",
                "Present/display 1% low is below target.",
                presentLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var summary = performance.PerfectionMet
            ? "No degraded frame lane detected."
            : performance.Summary;
        return new DiagnosticEvaluation(
            performance.PerfectionMet ? "Healthy" : "Warning",
            performance.PerfectionMet ? "none" : "mixed",
            summary,
            performance.PerfectionMet ? "All monitored frame lanes are within current thresholds." : performance.Summary,
            sourceLane,
            decodeLane,
            previewLane,
            renderLane,
            presentLane,
            recordingLane,
            audioLane);
    }
}

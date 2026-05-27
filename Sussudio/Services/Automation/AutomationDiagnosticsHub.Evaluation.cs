using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

internal readonly record struct PerformanceEvaluation(double Score, bool PerfectionMet, string Summary);

internal readonly record struct DiagnosticEvaluation(
    string HealthStatus,
    string LikelyStage,
    string Summary,
    string Evidence,
    string SourceLane,
    string DecodeLane,
    string PreviewLane,
    string RenderLane,
    string PresentLane,
    string RecordingLane,
    string AudioLane);

public sealed partial class AutomationDiagnosticsHub
{
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
        var lanes = BuildDiagnosticEvaluationLanes(
            health,
            captureRuntime,
            previewRuntime,
            recentMjpeg,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops,
            recentRenderer,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures,
            playbackTargetFps,
            playbackCommandQueueAgeMs,
            playbackCommandFailureAgeMs,
            playbackCommandFailure);
        var sourceLane = lanes.Source;
        var decodeLane = lanes.Decode;
        var previewLane = lanes.Preview;
        var renderLane = lanes.Render;
        var presentLane = lanes.Present;
        var recordingLane = lanes.Recording;
        var audioLane = lanes.Audio;
        var flashbackDiagnostic = TryBuildFlashbackDiagnosticEvaluation(
            health,
            isRecording,
            recentFlashbackRecording,
            lanes,
            playbackTargetFps,
            playbackCommandQueueAgeMs,
            playbackCommandFailedRecently);
        if (flashbackDiagnostic.HasValue)
        {
            return flashbackDiagnostic.Value;
        }

        var realtimeDiagnostic = TryBuildRealtimeDiagnosticEvaluation(
            health,
            captureRuntime,
            previewRuntime,
            isPreviewing,
            isRecording,
            recentMjpeg,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops,
            lanes);
        if (realtimeDiagnostic.HasValue)
        {
            return realtimeDiagnostic.Value;
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

    private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        double playbackTargetFps,
        long playbackCommandQueueAgeMs,
        long playbackCommandFailureAgeMs,
        string playbackCommandFailure)
    {
        var sourceLane = BuildSourceLane(health);
        var decodeLane = BuildDecodeLane(health, recentMjpeg);
        var previewLane = BuildPreviewLane(
            health,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops);
        var renderLane = BuildRenderLane(previewRuntime, recentRenderer);
        var presentLane = BuildPresentLane(
            previewRuntime,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);
        var visualLane = BuildVisualLane(health);
        var mjpegDuplicateLane = FormatMjpegDuplicateCadenceDetail(health);
        var sourceSignalLane = BuildSourceSignalLane(health, sourceLane);
        var recordingLane = BuildRecordingLane(captureRuntime);
        var audioLane = BuildAudioLane(captureRuntime);
        var flashbackRecordingLane = BuildFlashbackRecordingLane(health);
        var exportLane = BuildFlashbackExportLane(health);
        var tempCacheLane = BuildFlashbackTempCacheLane(health);
        var playbackCommandLane = BuildFlashbackPlaybackCommandLane(
            health,
            playbackCommandQueueAgeMs,
            playbackCommandFailureAgeMs,
            playbackCommandFailure);
        var playbackPerfLane = BuildFlashbackPlaybackPerformanceLane(
            health,
            previewRuntime,
            playbackTargetFps);

        return new DiagnosticEvaluationLanes(
            sourceLane,
            decodeLane,
            previewLane,
            renderLane.Text,
            presentLane,
            visualLane,
            mjpegDuplicateLane,
            sourceSignalLane,
            recordingLane,
            audioLane,
            flashbackRecordingLane,
            exportLane,
            tempCacheLane,
            playbackCommandLane,
            playbackPerfLane,
            renderLane.RecentSubmitted,
            renderLane.RecentDropPercent);
    }

    private static string BuildDecodeLane(
        CaptureHealthSnapshot health,
        MjpegRecentCounters recentMjpeg)
    {
        return $"decode p95={health.MjpegDecodeP95Ms:0.##}ms callbackP95={health.MjpegCallbackP95Ms:0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures} recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}";
    }

    private static string BuildSourceLane(CaptureHealthSnapshot health)
    {
        var sourceTarget = health.ExpectedFrameRate > 0
            ? $"{1000.0 / health.ExpectedFrameRate:0.##}ms"
            : "n/a";

        return $"source target={sourceTarget} avg={health.CaptureCadenceAverageIntervalMs:0.##}ms p95={health.CaptureCadenceP95IntervalMs:0.##}ms p99={health.CaptureCadenceP99IntervalMs:0.##}ms max={health.CaptureCadenceMaxIntervalMs:0.##}ms rate={health.CaptureCadenceObservedFps:0.##}/{health.ExpectedFrameRate:0.##}fps 1pctLow={health.CaptureCadenceOnePercentLowFps:0.##}fps gaps={health.CaptureCadenceSevereGapCount} drops={health.CaptureCadenceEstimatedDroppedFrames} ({health.CaptureCadenceEstimatedDropPercent:0.###}%)";
    }

    private static string BuildPreviewLane(
        CaptureHealthSnapshot health,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops)
    {
        var previewLastDropReason = string.IsNullOrWhiteSpace(health.MjpegPreviewJitterLastDropReason)
            ? "none"
            : health.MjpegPreviewJitterLastDropReason;

        return $"preview scheduler target={health.MjpegPreviewJitterTargetDepth} depth={health.MjpegPreviewJitterQueueDepth}/{health.MjpegPreviewJitterMaxDepth} dropped={health.MjpegPreviewJitterTotalDropped} clearedDrops={health.MjpegPreviewJitterClearedDropCount} deadlineDrops={health.MjpegPreviewJitterDeadlineDropCount} underflows={health.MjpegPreviewJitterUnderflowCount} resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount} recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows} lastDropReason={previewLastDropReason}";
    }

    private static DiagnosticEvaluationRenderLane BuildRenderLane(
        PreviewRuntimeSnapshot previewRuntime,
        D3DRendererRecentCounters recentRenderer)
    {
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

        return new DiagnosticEvaluationRenderLane(
            renderLane,
            recentRendererSubmitted,
            recentRendererDropPercent);
    }

    private static string BuildPresentLane(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var presentTarget = previewRuntime.DisplayCadenceExpectedIntervalMs > 0
            ? $"{previewRuntime.DisplayCadenceExpectedIntervalMs:0.##}ms"
            : "n/a";
        var dxgiStats = previewRuntime.D3DFrameStatsSuccessCount > 0
            ? $" dxgiStats ok={previewRuntime.D3DFrameStatsSuccessCount}/{previewRuntime.D3DFrameStatsSampleCount} pc={previewRuntime.D3DFrameStatsPresentCount} prc={previewRuntime.D3DFrameStatsPresentRefreshCount} prDelta={previewRuntime.D3DFrameStatsLastPresentRefreshDelta} missed={previewRuntime.D3DFrameStatsMissedRefreshCount} recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}"
            : previewRuntime.D3DFrameStatsSampleCount > 0
                ? $" dxgiStats err={previewRuntime.D3DFrameStatsLastError} fail={previewRuntime.D3DFrameStatsFailureCount}/{previewRuntime.D3DFrameStatsSampleCount} recentFail={recentD3DStatsFailures}"
                : string.Empty;

        return $"present target={presentTarget} avg={previewRuntime.DisplayCadenceAverageIntervalMs:0.##}ms p95={previewRuntime.DisplayCadenceP95IntervalMs:0.##}ms p99={previewRuntime.DisplayCadenceP99IntervalMs:0.##}ms max={previewRuntime.DisplayCadenceMaxIntervalMs:0.##}ms slow={previewRuntime.DisplayCadenceSlowFramePercent:0.##}% rate={previewRuntime.DisplayCadenceObservedFps:0.##}fps 1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps sync={previewRuntime.D3DPresentSyncInterval} latency={previewRuntime.D3DMaxFrameLatency} buffers={previewRuntime.D3DSwapChainBufferCount} swap={previewRuntime.D3DSwapChainAddress}{dxgiStats}";
    }

    private static string BuildVisualLane(CaptureHealthSnapshot health)
    {
        return $"visual crop samples={health.VisualCadenceSampleCount} output={health.VisualCadenceOutputObservedFps:0.##}fps changes={health.VisualCadenceChangeObservedFps:0.##}fps repeat={health.VisualCadenceRepeatFramePercent:0.###}% repeatFrames={health.VisualCadenceRepeatFrameCount} longestRepeatRun={health.VisualCadenceLongestRepeatRun} confidence={health.VisualCadenceMotionConfidence}";
    }

    private static string BuildSourceSignalLane(
        CaptureHealthSnapshot health,
        string sourceLane)
    {
        return $"{sourceLane} | source telemetry {health.SourceWidth ?? 0}x{health.SourceHeight ?? 0}@{(health.SourceFrameRateExact ?? 0):0.##}fps hdr={health.SourceIsHdr?.ToString() ?? "Unknown"} availability={health.SourceTelemetryAvailability}/{health.SourceTelemetryConfidence}";
    }

    private static string BuildRecordingLane(CaptureRuntimeSnapshot captureRuntime)
    {
        return $"recording integrity={captureRuntime.RecordingIntegrityStatus} complete={captureRuntime.RecordingIntegrityComplete} seqGaps={captureRuntime.RecordingIntegritySequenceGaps} queueDrops={captureRuntime.RecordingIntegrityQueueDroppedFrames}";
    }

    private static string BuildAudioLane(CaptureRuntimeSnapshot captureRuntime)
    {
        return $"audio integrity={captureRuntime.RecordingIntegrityAudioStatus} drops={captureRuntime.RecordingIntegrityAudioDropEvents} disc={captureRuntime.RecordingIntegrityAudioDiscontinuities} gaps={captureRuntime.RecordingIntegrityAudioCallbackGaps}";
    }

    private static string BuildFlashbackRecordingLane(CaptureHealthSnapshot health)
    {
        return
            $"flashback recording active={health.FlashbackActive} failed={health.FlashbackEncodingFailed} type={health.FlashbackEncodingFailureType ?? "None"} " +
            $"dropped={health.FlashbackDroppedFrames} encoderDrops={health.FlashbackVideoEncoderDroppedFrames} seqGaps={health.FlashbackVideoSequenceGaps} " +
            $"queueRejects={health.FlashbackVideoQueueRejectedFrames} lastReject={health.FlashbackVideoQueueLastRejectReason ?? "None"} " +
            $"backendStale={health.FlashbackBackendSettingsStale} staleReason={health.FlashbackBackendSettingsStaleReason} activeFormat={health.FlashbackBackendActiveFormat} requestedFormat={health.FlashbackBackendRequestedFormat} activePreset={health.FlashbackBackendActivePreset} requestedPreset={health.FlashbackBackendRequestedPreset} " +
            $"gpuOverloads={health.FlashbackGpuFramesDropped} forceRotate={health.FlashbackForceRotateActive} requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining} queue={health.FlashbackVideoQueueDepth}/{health.FlashbackVideoQueueCapacity} maxQueue={health.FlashbackVideoQueueMaxDepth} " +
            $"audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity} " +
            $"queueAgeMs={health.FlashbackVideoQueueOldestFrameAgeMs} backpressure={health.FlashbackVideoBackpressureWaitMs}ms/{health.FlashbackVideoBackpressureEvents} lastBackpressure={health.FlashbackVideoBackpressureLastWaitMs}ms maxBackpressure={health.FlashbackVideoBackpressureMaxWaitMs}ms " +
            $"fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}";
    }

    private static string BuildFlashbackExportLane(CaptureHealthSnapshot health)
    {
        var exportFailureKind = string.IsNullOrWhiteSpace(health.FlashbackExportFailureKind)
            ? "None"
            : health.FlashbackExportFailureKind;

        return $"export active={health.FlashbackExportActive} status={health.FlashbackExportStatus} kind={exportFailureKind} id={health.FlashbackExportId} lastResultId={health.LastExportId} progress={health.FlashbackExportPercent:0.##}% segments={health.FlashbackExportSegmentsProcessed}/{health.FlashbackExportTotalSegments} elapsedMs={health.FlashbackExportElapsedMs} progressAgeMs={health.FlashbackExportLastProgressAgeMs} bytes={health.FlashbackExportOutputBytes} throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##} lastProgressUtc={health.FlashbackExportLastProgressUtcUnixMs} completedUtc={health.FlashbackExportCompletedUtcUnixMs}";
    }

    private static string BuildFlashbackTempCacheLane(CaptureHealthSnapshot health)
    {
        return $"flashback temp freeBytes={health.FlashbackTempDriveFreeBytes} cacheBytes={health.FlashbackStartupCacheBytes} budgetBytes={health.FlashbackStartupCacheBudgetBytes} sessions={health.FlashbackStartupCacheSessionCount} deleted={health.FlashbackStartupCacheDeletedSessionCount} freedBytes={health.FlashbackStartupCacheFreedBytes} overBudget={health.FlashbackStartupCacheOverBudget}";
    }

    private static string BuildFlashbackPlaybackCommandLane(
        CaptureHealthSnapshot health,
        long playbackCommandQueueAgeMs,
        long playbackCommandFailureAgeMs,
        string playbackCommandFailure)
    {
        return $"playback commands pending={health.FlashbackPlaybackPendingCommands}/{health.FlashbackPlaybackCommandQueueCapacity} maxPending={health.FlashbackPlaybackMaxPendingCommands} lastLatency={health.FlashbackPlaybackLastCommandQueueLatencyMs}ms maxLatency={health.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={health.FlashbackPlaybackMaxCommandQueueLatencyCommand} lastQueued={health.FlashbackPlaybackLastCommandQueued} lastProcessed={health.FlashbackPlaybackLastCommandProcessed} queuedAge={playbackCommandQueueAgeMs}ms lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs} threadAlive={health.FlashbackPlaybackThreadAlive}";
    }

    private static string BuildFlashbackPlaybackPerformanceLane(
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        double playbackTargetFps)
    {
        return
            $"playback perf state={health.FlashbackPlaybackState} fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##} target={health.FlashbackPlaybackTargetFps:0.##} encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##} " +
            $"1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps p99={health.FlashbackPlaybackP99FrameMs:0.##}ms max={health.FlashbackPlaybackMaxFrameMs:0.##}ms slow={health.FlashbackPlaybackSlowFramePercent:0.##}% ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={health.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={health.FlashbackPlaybackLastSeekHitForwardDecodeCap} decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={health.FlashbackPlaybackDecodeMaxMs:0.##}ms decodePhase={health.FlashbackPlaybackMaxDecodePhase} decodeReceive={health.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms decodeFeed={health.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={health.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={health.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={health.FlashbackPlaybackMaxDecodePositionMs}ms samples={health.FlashbackPlaybackCadenceSampleCount} frames={health.FlashbackPlaybackFrameCount} late={health.FlashbackPlaybackLateFrames} dropped={health.FlashbackPlaybackDroppedFrames} audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={health.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={health.FlashbackPlaybackAudioMasterFallbacks} submitFailures={health.FlashbackPlaybackSubmitFailures} switches={health.FlashbackPlaybackSegmentSwitches} fmp4Reopens={health.FlashbackPlaybackFmp4Reopens} writeHeadWaits={health.FlashbackPlaybackWriteHeadWaits} nearLiveSnaps={health.FlashbackPlaybackNearLiveSnaps} decodeErrorSnaps={health.FlashbackPlaybackDecodeErrorSnaps}";
    }

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

    private readonly record struct DiagnosticEvaluationLanes(
        string Source,
        string Decode,
        string Preview,
        string Render,
        string Present,
        string Visual,
        string MjpegDuplicate,
        string SourceSignal,
        string Recording,
        string Audio,
        string FlashbackRecording,
        string Export,
        string TempCache,
        string PlaybackCommand,
        string PlaybackPerf,
        long RecentRendererSubmitted,
        double RecentRendererDropPercent);

    private readonly record struct DiagnosticEvaluationRenderLane(
        string Text,
        long RecentSubmitted,
        double RecentDropPercent);
}

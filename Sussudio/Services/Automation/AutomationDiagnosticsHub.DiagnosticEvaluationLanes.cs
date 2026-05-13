using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
        var playbackCommandLane =
            $"playback commands pending={health.FlashbackPlaybackPendingCommands}/{health.FlashbackPlaybackCommandQueueCapacity} maxPending={health.FlashbackPlaybackMaxPendingCommands} lastLatency={health.FlashbackPlaybackLastCommandQueueLatencyMs}ms maxLatency={health.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={health.FlashbackPlaybackMaxCommandQueueLatencyCommand} lastQueued={health.FlashbackPlaybackLastCommandQueued} lastProcessed={health.FlashbackPlaybackLastCommandProcessed} queuedAge={playbackCommandQueueAgeMs}ms lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs} threadAlive={health.FlashbackPlaybackThreadAlive}";
        var playbackPerfLane =
            $"playback perf state={health.FlashbackPlaybackState} fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##} target={health.FlashbackPlaybackTargetFps:0.##} encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##} " +
            $"1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps p99={health.FlashbackPlaybackP99FrameMs:0.##}ms max={health.FlashbackPlaybackMaxFrameMs:0.##}ms slow={health.FlashbackPlaybackSlowFramePercent:0.##}% ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={health.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={health.FlashbackPlaybackLastSeekHitForwardDecodeCap} decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={health.FlashbackPlaybackDecodeMaxMs:0.##}ms decodePhase={health.FlashbackPlaybackMaxDecodePhase} decodeReceive={health.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms decodeFeed={health.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={health.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={health.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={health.FlashbackPlaybackMaxDecodePositionMs}ms samples={health.FlashbackPlaybackCadenceSampleCount} frames={health.FlashbackPlaybackFrameCount} late={health.FlashbackPlaybackLateFrames} dropped={health.FlashbackPlaybackDroppedFrames} audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={health.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={health.FlashbackPlaybackAudioMasterFallbacks} submitFailures={health.FlashbackPlaybackSubmitFailures} switches={health.FlashbackPlaybackSegmentSwitches} fmp4Reopens={health.FlashbackPlaybackFmp4Reopens} writeHeadWaits={health.FlashbackPlaybackWriteHeadWaits} nearLiveSnaps={health.FlashbackPlaybackNearLiveSnaps} decodeErrorSnaps={health.FlashbackPlaybackDecodeErrorSnaps}";

        return new DiagnosticEvaluationLanes(
            sourceLane,
            decodeLane,
            previewLane,
            renderLane,
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
            recentRendererSubmitted,
            recentRendererDropPercent);
    }

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
}

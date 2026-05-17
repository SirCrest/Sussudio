using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildSourceLane(CaptureHealthSnapshot health)
    {
        var sourceTarget = health.ExpectedFrameRate > 0
            ? $"{1000.0 / health.ExpectedFrameRate:0.##}ms"
            : "n/a";

        return $"source target={sourceTarget} avg={health.CaptureCadenceAverageIntervalMs:0.##}ms p95={health.CaptureCadenceP95IntervalMs:0.##}ms p99={health.CaptureCadenceP99IntervalMs:0.##}ms max={health.CaptureCadenceMaxIntervalMs:0.##}ms rate={health.CaptureCadenceObservedFps:0.##}/{health.ExpectedFrameRate:0.##}fps 1pctLow={health.CaptureCadenceOnePercentLowFps:0.##}fps gaps={health.CaptureCadenceSevereGapCount} drops={health.CaptureCadenceEstimatedDroppedFrames} ({health.CaptureCadenceEstimatedDropPercent:0.###}%)";
    }

    private static string BuildDecodeLane(
        CaptureHealthSnapshot health,
        MjpegRecentCounters recentMjpeg)
    {
        return $"decode p95={health.MjpegDecodeP95Ms:0.##}ms callbackP95={health.MjpegCallbackP95Ms:0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures} recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}";
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
}

using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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

    private readonly record struct DiagnosticEvaluationRenderLane(
        string Text,
        long RecentSubmitted,
        double RecentDropPercent);
}

using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}

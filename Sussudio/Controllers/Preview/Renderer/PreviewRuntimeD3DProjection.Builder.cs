namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);
        var rendererState = PreviewRuntimeD3DRendererStatePolicy.Evaluate(d3d, input.IsPreviewing);
        var displayCadence = PreviewRuntimeD3DDisplayCadencePolicy.Evaluate(d3d, input.PreviewMinPresentationIntervalMs);
        var renderCpuTiming = PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate(d3d);
        var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);
        var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);
        var frameLatencyWait = PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate(d3d);
        var pipelineLatency = PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate(d3d);

        var projection = new PreviewRuntimeD3DProjection();
        projection.ApplyFrameCounters(frameCounters);
        projection.ApplyRendererState(rendererState);
        projection.ApplyDisplayCadence(displayCadence);
        projection.ApplyRenderCpuTiming(renderCpuTiming);
        projection.ApplyPipelineLatency(pipelineLatency);
        projection.ApplyFrameLatencyWait(frameLatencyWait);
        projection.ApplyFrameStatistics(frameStatistics);
        projection.ApplyFrameOwnership(frameOwnership);
        return projection;
    }
}

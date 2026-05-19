namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DFrameCounters(
    bool GpuActive,
    bool RendererAttached,
    long FramesArrived,
    long FramesDisplayed,
    long FramesDropped,
    long D3DFramesSubmitted,
    long D3DFramesRendered,
    long D3DFramesDropped);

internal static class PreviewRuntimeD3DFrameCounterPolicy
{
    public static PreviewRuntimeD3DFrameCounters Evaluate(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var gpuActive = d3d != null;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;

        return new PreviewRuntimeD3DFrameCounters(
            GpuActive: gpuActive,
            RendererAttached: d3d != null || input.PreviewSourceAttached,
            FramesArrived: gpuActive ? d3dFramesSubmitted : input.FramesArrived,
            FramesDisplayed: gpuActive ? d3dFramesRendered : input.FramesDisplayed,
            FramesDropped: gpuActive ? d3dFramesDropped : input.FramesDropped,
            D3DFramesSubmitted: d3dFramesSubmitted,
            D3DFramesRendered: d3dFramesRendered,
            D3DFramesDropped: d3dFramesDropped);
    }
}

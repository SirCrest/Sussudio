namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeSnapshotSurfaceProjection(
    bool IsPreviewing,
    bool GpuActive,
    bool PlaceholderVisible,
    bool GpuElementVisible,
    bool CpuElementVisible,
    bool RendererAttached,
    long FramesArrived,
    long FramesDisplayed,
    long FramesDropped,
    bool BlankSuspected,
    bool StallSuspected);

internal static class PreviewRuntimeSnapshotSurfaceProjectionPolicy
{
    public static PreviewRuntimeSnapshotSurfaceProjection Evaluate(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection,
        PreviewRuntimeSnapshotHealth health)
        => new(
            IsPreviewing: input.IsPreviewing,
            GpuActive: d3dProjection.GpuActive,
            PlaceholderVisible: input.PlaceholderVisible,
            GpuElementVisible: input.GpuElementVisible,
            CpuElementVisible: input.CpuElementVisible,
            RendererAttached: d3dProjection.RendererAttached,
            FramesArrived: d3dProjection.FramesArrived,
            FramesDisplayed: d3dProjection.FramesDisplayed,
            FramesDropped: d3dProjection.FramesDropped,
            BlankSuspected: health.BlankSuspected,
            StallSuspected: health.StallSuspected);
}

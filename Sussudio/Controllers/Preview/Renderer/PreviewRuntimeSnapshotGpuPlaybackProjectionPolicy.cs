namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeSnapshotGpuPlaybackProjection(
    string PlaybackState,
    int NaturalVideoWidth,
    int NaturalVideoHeight,
    double PositionMs,
    long PositionEventCount);

internal static class PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy
{
    public static PreviewRuntimeSnapshotGpuPlaybackProjection Evaluate(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection)
        => new(
            PlaybackState: d3dProjection.GpuPlaybackState,
            NaturalVideoWidth: d3dProjection.GpuNaturalVideoWidth,
            NaturalVideoHeight: d3dProjection.GpuNaturalVideoHeight,
            PositionMs: d3dProjection.GpuPositionMs,
            PositionEventCount: input.GpuPositionEventCount);
}

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            PlaybackState = previewSummary.GpuPlaybackState,
            NaturalVideoWidth = previewSummary.GpuNaturalVideoWidth,
            NaturalVideoHeight = previewSummary.GpuNaturalVideoHeight,
            PositionMs = previewSummary.GpuPositionMs,
            PositionEventCount = previewSummary.GpuPositionEventCount
        };

    private readonly record struct PreviewRuntimeGpuPlaybackFlattenedProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }
}

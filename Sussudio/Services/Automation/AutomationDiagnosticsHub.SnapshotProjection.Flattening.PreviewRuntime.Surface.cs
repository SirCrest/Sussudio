namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            GpuActive = previewSummary.GpuActive,
            PlaceholderVisible = previewSummary.PlaceholderVisible,
            GpuElementVisible = previewSummary.GpuElementVisible,
            CpuElementVisible = previewSummary.CpuElementVisible,
            RendererAttached = previewSummary.RendererAttached
        };

    private readonly record struct PreviewRuntimeSurfaceFlattenedProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
    }
}

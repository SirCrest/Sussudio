namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            HdrInputDetected = previewSummary.HdrInputDetected,
            ToneMapMode = previewSummary.ToneMapMode,
            ColorContext = previewSummary.ColorContext,
            AdapterColorMetadata = previewSummary.AdapterColorMetadata
        };

    private readonly record struct PreviewRuntimeColorFlattenedProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }
}

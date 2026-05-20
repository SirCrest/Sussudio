namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            FramesArrived = previewSummary.FramesArrived,
            FramesDisplayed = previewSummary.FramesDisplayed,
            FramesDropped = previewSummary.FramesDropped,
            EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs
        };

    private readonly record struct PreviewRuntimeFrameFlattenedProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }
}

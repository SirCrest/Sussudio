namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(
        MjpegPreviewJitterProjection previewJitter)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),
            Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),
            Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),
            Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)
        };

    private readonly record struct MjpegPreviewJitterFlattenedProjection
    {
        public MjpegPreviewJitterQueueFlattenedProjection Queue { get; init; }
        public MjpegPreviewJitterTimingFlattenedProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveFlattenedProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventFlattenedProjection Events { get; init; }
    }
}

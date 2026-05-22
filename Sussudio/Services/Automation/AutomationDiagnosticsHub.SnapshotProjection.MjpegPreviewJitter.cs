using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueProjection(health),
            Timing = BuildMjpegPreviewJitterTimingProjection(health),
            Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),
            Events = BuildMjpegPreviewJitterEventProjection(health)
        };

    private readonly record struct MjpegPreviewJitterProjection
    {
        public MjpegPreviewJitterQueueProjection Queue { get; init; }
        public MjpegPreviewJitterTimingProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventProjection Events { get; init; }
    }

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

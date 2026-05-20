using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Enabled = health.MjpegPreviewJitterEnabled,
            TargetDepth = health.MjpegPreviewJitterTargetDepth,
            MaxDepth = health.MjpegPreviewJitterMaxDepth,
            QueueDepth = health.MjpegPreviewJitterQueueDepth,
            TotalQueued = health.MjpegPreviewJitterTotalQueued,
            TotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            TotalDropped = health.MjpegPreviewJitterTotalDropped,
            UnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
    }
}

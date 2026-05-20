namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(
        MjpegPreviewJitterQueueProjection queue)
        => new()
        {
            Enabled = queue.Enabled,
            TargetDepth = queue.TargetDepth,
            MaxDepth = queue.MaxDepth,
            QueueDepth = queue.QueueDepth,
            TotalQueued = queue.TotalQueued,
            TotalSubmitted = queue.TotalSubmitted,
            TotalDropped = queue.TotalDropped,
            UnderflowCount = queue.UnderflowCount,
            ResumeReprimeCount = queue.ResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueFlattenedProjection
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

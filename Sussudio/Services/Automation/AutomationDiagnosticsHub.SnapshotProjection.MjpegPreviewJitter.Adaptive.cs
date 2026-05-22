using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            ClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            TargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }

    private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(
        MjpegPreviewJitterAdaptiveProjection adaptive)
        => new()
        {
            DeadlineDropCount = adaptive.DeadlineDropCount,
            ClearedDropCount = adaptive.ClearedDropCount,
            TargetIncreaseCount = adaptive.TargetIncreaseCount,
            TargetDecreaseCount = adaptive.TargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveFlattenedProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }
}

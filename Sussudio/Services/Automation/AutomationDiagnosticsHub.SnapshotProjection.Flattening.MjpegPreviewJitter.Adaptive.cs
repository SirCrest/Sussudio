namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AudioDropsFlattenedProjection BuildAudioDropsFlattenedProjection(AudioDropsProjection audioDrops)
        => new()
        {
            QueueSaturated = audioDrops.QueueSaturated,
            BacklogEviction = audioDrops.BacklogEviction,
            ChunksDropped = audioDrops.ChunksDropped,
            QueueDropsRealtime = audioDrops.QueueDropsRealtime,
            QueueDropsFileWriter = audioDrops.QueueDropsFileWriter
        };

    private readonly record struct AudioDropsFlattenedProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }
}

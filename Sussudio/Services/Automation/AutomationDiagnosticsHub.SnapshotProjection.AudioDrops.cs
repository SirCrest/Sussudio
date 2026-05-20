using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)
        => new()
        {
            QueueSaturated = health.AudioDropsQueueSaturated,
            BacklogEviction = health.AudioDropsBacklogEviction,
            ChunksDropped = health.AudioChunksDropped,
            QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,
            QueueDropsFileWriter = health.AudioChunksDropped
        };

    private readonly record struct AudioDropsProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }
}

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(
        FlashbackPlaybackCommandProjection commands)
        => new()
        {
            ThreadAlive = commands.ThreadAlive,
            Enqueued = commands.Enqueued,
            Processed = commands.Processed,
            Dropped = commands.Dropped,
            SkippedNotReady = commands.SkippedNotReady,
            ScrubUpdatesCoalesced = commands.ScrubUpdatesCoalesced,
            SeekCommandsCoalesced = commands.SeekCommandsCoalesced,
            QueueCapacity = commands.QueueCapacity,
            Pending = commands.Pending,
            MaxPending = commands.MaxPending,
            LastQueueLatencyMs = commands.LastQueueLatencyMs,
            MaxQueueLatencyMs = commands.MaxQueueLatencyMs,
            MaxQueueLatencyCommand = commands.MaxQueueLatencyCommand,
            LastQueued = commands.LastQueued,
            LastProcessed = commands.LastProcessed,
            LastQueuedUtcUnixMs = commands.LastQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = commands.LastProcessedUtcUnixMs,
            LastFailureUtcUnixMs = commands.LastFailureUtcUnixMs,
            LastFailure = commands.LastFailure
        };

    private readonly record struct FlashbackPlaybackCommandFlattenedProjection
    {
        public bool ThreadAlive { get; init; }
        public long Enqueued { get; init; }
        public long Processed { get; init; }
        public long Dropped { get; init; }
        public long SkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int QueueCapacity { get; init; }
        public int Pending { get; init; }
        public int MaxPending { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string MaxQueueLatencyCommand { get; init; }
        public string LastQueued { get; init; }
        public string LastProcessed { get; init; }
        public long LastQueuedUtcUnixMs { get; init; }
        public long LastProcessedUtcUnixMs { get; init; }
        public long LastFailureUtcUnixMs { get; init; }
        public string LastFailure { get; init; }
    }
}

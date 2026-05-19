using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)
        => new()
        {
            ThreadAlive = health.FlashbackPlaybackThreadAlive,
            Enqueued = health.FlashbackPlaybackCommandsEnqueued,
            Processed = health.FlashbackPlaybackCommandsProcessed,
            Dropped = health.FlashbackPlaybackCommandsDropped,
            SkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            QueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            Pending = health.FlashbackPlaybackPendingCommands,
            MaxPending = health.FlashbackPlaybackMaxPendingCommands,
            LastQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            MaxQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastQueued = health.FlashbackPlaybackLastCommandQueued,
            LastProcessed = health.FlashbackPlaybackLastCommandProcessed,
            LastQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            LastFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastFailure = health.FlashbackPlaybackLastCommandFailure
        };

    private readonly record struct FlashbackPlaybackCommandProjection
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

using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackPlaybackCommandHealthSnapshotFields CaptureFlashbackPlaybackCommandHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.CommandsEnqueued ?? 0,
            fbPlayback?.CommandsProcessed ?? 0,
            fbPlayback?.CommandsDropped ?? 0,
            fbPlayback?.CommandsSkippedNotReady ?? 0,
            fbPlayback?.ScrubUpdatesCoalesced ?? 0,
            fbPlayback?.SeekCommandsCoalesced ?? 0,
            fbPlayback?.CommandQueueCapacityCommands ?? 0,
            fbPlayback?.PendingCommands ?? 0,
            fbPlayback?.MaxPendingCommands ?? 0,
            fbPlayback?.LastCommandQueueLatencyMs ?? 0,
            fbPlayback?.MaxCommandQueueLatencyMs ?? 0,
            fbPlayback?.MaxCommandQueueLatencyCommand ?? "None",
            fbPlayback?.LastCommandQueued ?? "None",
            fbPlayback?.LastCommandProcessed ?? "None",
            fbPlayback?.LastCommandQueuedUtcUnixMs ?? 0,
            fbPlayback?.LastCommandProcessedUtcUnixMs ?? 0,
            fbPlayback?.LastCommandFailureUtcUnixMs ?? 0,
            fbPlayback?.LastCommandFailure ?? string.Empty);

    private readonly record struct FlashbackPlaybackCommandHealthSnapshotFields(
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        int CommandQueueCapacity,
        int PendingCommands,
        int MaxPendingCommands,
        long LastCommandQueueLatencyMs,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        string LastCommandQueued,
        string LastCommandProcessed,
        long LastCommandQueuedUtcUnixMs,
        long LastCommandProcessedUtcUnixMs,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure);
}

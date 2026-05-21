using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackCommandsProjection BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(
        AutomationSnapshot snapshot)
        => new(
            PendingCommands: snapshot.FlashbackPlaybackPendingCommands,
            MaxPendingCommands: snapshot.FlashbackPlaybackMaxPendingCommands,
            CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued,
            CommandsProcessed: snapshot.FlashbackPlaybackCommandsProcessed,
            CommandsDropped: snapshot.FlashbackPlaybackCommandsDropped,
            CommandsSkippedNotReady: snapshot.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced: snapshot.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced: snapshot.FlashbackPlaybackSeekCommandsCoalesced,
            LastCommandQueued: snapshot.FlashbackPlaybackLastCommandQueued,
            LastCommandProcessed: snapshot.FlashbackPlaybackLastCommandProcessed,
            MaxCommandQueueLatencyMs: snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxCommandQueueLatencyCommand: snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastCommandFailureUtcUnixMs: snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastCommandFailure: snapshot.FlashbackPlaybackLastCommandFailure);

    private readonly record struct PerformanceTimelineFlashbackPlaybackCommandsProjection(
        int PendingCommands,
        int MaxPendingCommands,
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        string LastCommandQueued,
        string LastCommandProcessed,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure);
}

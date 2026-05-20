using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(
        bool observed,
        JsonElement endSnapshot,
        FlashbackPlaybackSessionMetrics metrics) =>
        new(
            PendingCommandsAtEnd: observed ? GetInt(endSnapshot, "FlashbackPlaybackPendingCommands") : 0,
            MaxPendingCommandsObserved: metrics.MaxPendingCommandsObserved,
            MaxCommandQueueLatencyMsObserved: metrics.MaxCommandQueueLatencyMsObserved,
            MaxCommandQueueLatencyCommandObserved: metrics.MaxCommandQueueLatencyCommandObserved,
            CommandsDroppedAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackCommandsDropped"),
            CommandsSkippedNotReadyAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackCommandsSkippedNotReady"),
            ScrubUpdatesCoalescedAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced"),
            SeekCommandsCoalescedAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSeekCommandsCoalesced"),
            LastCommandFailureAtEnd: observed ? GetString(endSnapshot, "FlashbackPlaybackLastCommandFailure") ?? string.Empty : string.Empty,
            LastCommandFailureUtcUnixMsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs"));
}

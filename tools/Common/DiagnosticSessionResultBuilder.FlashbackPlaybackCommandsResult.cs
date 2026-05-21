namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(
        int FlashbackPlaybackPendingCommandsAtEnd,
        int FlashbackPlaybackMaxPendingCommandsObserved,
        int FlashbackPlaybackMaxCommandQueueLatencyMsObserved,
        string FlashbackPlaybackMaxCommandQueueLatencyCommandObserved,
        long FlashbackPlaybackCommandsDroppedAtEnd,
        long FlashbackPlaybackCommandsSkippedNotReadyAtEnd,
        long FlashbackPlaybackScrubUpdatesCoalescedAtEnd,
        long FlashbackPlaybackSeekCommandsCoalescedAtEnd,
        string FlashbackPlaybackLastCommandFailureAtEnd,
        long FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd);

    private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved: playbackResultMetrics.MaxPendingCommandsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved: playbackResultMetrics.MaxCommandQueueLatencyMsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd: playbackResultMetrics.CommandsDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd: playbackResultMetrics.CommandsSkippedNotReadyAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd: playbackResultMetrics.ScrubUpdatesCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd: playbackResultMetrics.SeekCommandsCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd);
}

namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackResultMetrics
{
    public int PendingCommandsAtEnd { get; init; }
    public int MaxPendingCommandsObserved { get; init; }
    public int MaxCommandQueueLatencyMsObserved { get; init; }
    public string MaxCommandQueueLatencyCommandObserved { get; init; } = string.Empty;
    public long CommandsDroppedAtEnd { get; init; }
    public long CommandsSkippedNotReadyAtEnd { get; init; }
    public long ScrubUpdatesCoalescedAtEnd { get; init; }
    public long SeekCommandsCoalescedAtEnd { get; init; }
    public string LastCommandFailureAtEnd { get; init; } = string.Empty;
    public long LastCommandFailureUtcUnixMsAtEnd { get; init; }
}

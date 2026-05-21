namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackSessionMetrics
{
    public int MaxPendingCommandsObserved { get; set; }
    public int MaxCommandQueueLatencyMsObserved { get; set; }
    public string MaxCommandQueueLatencyCommandObserved { get; set; } = string.Empty;
}

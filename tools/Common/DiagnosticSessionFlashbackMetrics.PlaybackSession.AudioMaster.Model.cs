namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackSessionMetrics
{
    public long MaxAudioMasterDelayDoublesObserved { get; set; }
    public long MaxAudioMasterDelayShrinksObserved { get; set; }
    public long MaxAudioMasterFallbacksObserved { get; set; }
    public double MaxAudioBufferedDurationMsObserved { get; set; }
    public double MaxAudioQueueDurationMsObserved { get; set; }
    public double MaxAbsAvDriftMsObserved { get; set; }
}

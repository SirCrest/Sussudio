namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackSessionMetrics
{
    public double MaxDecodeP99MsObserved { get; set; }
    public double MaxDecodeMsObserved { get; set; }
    public string MaxDecodePhaseObserved { get; set; } = string.Empty;
    public double MaxDecodeReceiveMsObserved { get; set; }
    public double MaxDecodeFeedMsObserved { get; set; }
    public double MaxDecodeReadMsObserved { get; set; }
    public double MaxDecodeSendMsObserved { get; set; }
    public double MaxDecodeAudioMsObserved { get; set; }
    public double MaxDecodeConvertMsObserved { get; set; }
    public long MaxDecodeUtcUnixMsObserved { get; set; }
    public long MaxDecodePositionMsObserved { get; set; }
}

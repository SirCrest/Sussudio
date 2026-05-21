namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackResultMetrics
{
    public double DecodeAvgMsAtEnd { get; init; }
    public double DecodeP95MsAtEnd { get; init; }
    public double DecodeP99MsAtEnd { get; init; }
    public double DecodeMaxMsAtEnd { get; init; }
    public string MaxDecodePhaseAtEnd { get; init; } = string.Empty;
    public double MaxDecodeReceiveMsAtEnd { get; init; }
    public double MaxDecodeFeedMsAtEnd { get; init; }
    public double MaxDecodeReadMsAtEnd { get; init; }
    public double MaxDecodeSendMsAtEnd { get; init; }
    public double MaxDecodeAudioMsAtEnd { get; init; }
    public double MaxDecodeConvertMsAtEnd { get; init; }
    public long MaxDecodeUtcUnixMsAtEnd { get; init; }
    public long MaxDecodePositionMsAtEnd { get; init; }
}

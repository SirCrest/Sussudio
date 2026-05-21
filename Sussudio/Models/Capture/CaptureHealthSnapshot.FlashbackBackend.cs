namespace Sussudio.Models;

public sealed partial class CaptureHealthSnapshot
{
    public long FlashbackOutputBytes { get; init; }
    public string? FlashbackFilePath { get; init; }
    public long FlashbackEncodedFrames { get; init; }
    public long FlashbackDroppedFrames { get; init; }
    public bool FlashbackGpuEncoding { get; init; }
    public bool FlashbackBackendSettingsStale { get; init; }
    public string FlashbackBackendSettingsStaleReason { get; init; } = string.Empty;
    public string FlashbackBackendActiveFormat { get; init; } = string.Empty;
    public string FlashbackBackendRequestedFormat { get; init; } = string.Empty;
    public string FlashbackBackendActivePreset { get; init; } = string.Empty;
    public string FlashbackBackendRequestedPreset { get; init; } = string.Empty;
    public string? EncoderCodecName { get; init; }
    public uint EncoderTargetBitRate { get; init; }
    public int EncoderWidth { get; init; }
    public int EncoderHeight { get; init; }
    public double EncoderFrameRate { get; init; }
    public int? EncoderFrameRateNumerator { get; init; }
    public int? EncoderFrameRateDenominator { get; init; }
    public int FlashbackVideoQueueDepth { get; init; }
    public int FlashbackAudioQueueDepth { get; init; }
    public int FlashbackAudioQueueCapacity { get; init; }
}

using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackDecodeProjection BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(
        AutomationSnapshot snapshot)
        => new(
            DecodeP99Ms: snapshot.FlashbackPlaybackDecodeP99Ms,
            DecodeMaxMs: snapshot.FlashbackPlaybackDecodeMaxMs,
            MaxDecodePhase: snapshot.FlashbackPlaybackMaxDecodePhase,
            MaxDecodeReceiveMs: snapshot.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxDecodeFeedMs: snapshot.FlashbackPlaybackMaxDecodeFeedMs,
            MaxDecodeReadMs: snapshot.FlashbackPlaybackMaxDecodeReadMs,
            MaxDecodeSendMs: snapshot.FlashbackPlaybackMaxDecodeSendMs,
            MaxDecodeAudioMs: snapshot.FlashbackPlaybackMaxDecodeAudioMs,
            MaxDecodeConvertMs: snapshot.FlashbackPlaybackMaxDecodeConvertMs,
            MaxDecodeUtcUnixMs: snapshot.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxDecodePositionMs: snapshot.FlashbackPlaybackMaxDecodePositionMs,
            SeekForwardDecodeCapHits: snapshot.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap: snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap);

    private readonly record struct PerformanceTimelineFlashbackPlaybackDecodeProjection(
        double DecodeP99Ms,
        double DecodeMaxMs,
        string MaxDecodePhase,
        double MaxDecodeReceiveMs,
        double MaxDecodeFeedMs,
        double MaxDecodeReadMs,
        double MaxDecodeSendMs,
        double MaxDecodeAudioMs,
        double MaxDecodeConvertMs,
        long MaxDecodeUtcUnixMs,
        long MaxDecodePositionMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap);
}

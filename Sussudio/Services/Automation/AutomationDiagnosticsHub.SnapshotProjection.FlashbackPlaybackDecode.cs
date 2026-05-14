using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)
        => new()
        {
            SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = health.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            SampleCount = health.FlashbackPlaybackDecodeSampleCount,
            AvgMs = health.FlashbackPlaybackDecodeAvgMs,
            P95Ms = health.FlashbackPlaybackDecodeP95Ms,
            P99Ms = health.FlashbackPlaybackDecodeP99Ms,
            MaxMs = health.FlashbackPlaybackDecodeMaxMs,
            MaxPhase = health.FlashbackPlaybackMaxDecodePhase,
            MaxReceiveMs = health.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxFeedMs = health.FlashbackPlaybackMaxDecodeFeedMs,
            MaxReadMs = health.FlashbackPlaybackMaxDecodeReadMs,
            MaxSendMs = health.FlashbackPlaybackMaxDecodeSendMs,
            MaxAudioMs = health.FlashbackPlaybackMaxDecodeAudioMs,
            MaxConvertMs = health.FlashbackPlaybackMaxDecodeConvertMs,
            MaxUtcUnixMs = health.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs
        };

    private readonly record struct FlashbackPlaybackDecodeProjection
    {
        public long SeekForwardDecodeCapHits { get; init; }
        public bool LastSeekHitForwardDecodeCap { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
        public string MaxPhase { get; init; }
        public double MaxReceiveMs { get; init; }
        public double MaxFeedMs { get; init; }
        public double MaxReadMs { get; init; }
        public double MaxSendMs { get; init; }
        public double MaxAudioMs { get; init; }
        public double MaxConvertMs { get; init; }
        public long MaxUtcUnixMs { get; init; }
        public long MaxPositionMs { get; init; }
    }
}

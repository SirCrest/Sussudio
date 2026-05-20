namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(
        FlashbackPlaybackDecodeProjection decode)
        => new()
        {
            SeekForwardDecodeCapHits = decode.SeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = decode.LastSeekHitForwardDecodeCap,
            SampleCount = decode.SampleCount,
            AvgMs = decode.AvgMs,
            P95Ms = decode.P95Ms,
            P99Ms = decode.P99Ms,
            MaxMs = decode.MaxMs,
            MaxPhase = decode.MaxPhase,
            MaxReceiveMs = decode.MaxReceiveMs,
            MaxFeedMs = decode.MaxFeedMs,
            MaxReadMs = decode.MaxReadMs,
            MaxSendMs = decode.MaxSendMs,
            MaxAudioMs = decode.MaxAudioMs,
            MaxConvertMs = decode.MaxConvertMs,
            MaxUtcUnixMs = decode.MaxUtcUnixMs,
            MaxPositionMs = decode.MaxPositionMs
        };

    private readonly record struct FlashbackPlaybackDecodeFlattenedProjection
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

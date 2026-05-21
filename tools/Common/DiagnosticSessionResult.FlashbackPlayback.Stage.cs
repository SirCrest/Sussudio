namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Flashback playback stage and seek summary.
    public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }
    public long FlashbackPlaybackSubmitFailuresDelta { get; init; }
    public long FlashbackPlaybackSegmentSwitchesAtEnd { get; init; }
    public long FlashbackPlaybackFmp4ReopensAtEnd { get; init; }
    public long FlashbackPlaybackWriteHeadWaitsAtEnd { get; init; }
    public long FlashbackPlaybackNearLiveSnapsAtEnd { get; init; }
    public long FlashbackPlaybackDecodeErrorSnapsAtEnd { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHitsDelta { get; init; }
    public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }
}

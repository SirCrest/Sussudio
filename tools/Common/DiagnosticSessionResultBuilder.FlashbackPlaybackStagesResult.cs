namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(
        long FlashbackPlaybackSubmitFailuresAtEnd,
        long FlashbackPlaybackSubmitFailuresDelta,
        long FlashbackPlaybackSegmentSwitchesAtEnd,
        long FlashbackPlaybackFmp4ReopensAtEnd,
        long FlashbackPlaybackWriteHeadWaitsAtEnd,
        long FlashbackPlaybackNearLiveSnapsAtEnd,
        long FlashbackPlaybackDecodeErrorSnapsAtEnd,
        long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd,
        long FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd,
        long FlashbackPlaybackSeekForwardDecodeCapHitsDelta,
        bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd);

    private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackSubmitFailuresAtEnd: playbackResultMetrics.SubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd: playbackResultMetrics.SegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd: playbackResultMetrics.Fmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd: playbackResultMetrics.WriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd: playbackResultMetrics.NearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd: playbackResultMetrics.DecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd: playbackResultMetrics.LastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd: playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd);
}

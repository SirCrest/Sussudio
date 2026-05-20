using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(
        bool observed,
        JsonElement endSnapshot,
        FlashbackPlaybackSessionMetrics metrics) =>
        new(
            SubmitFailuresAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSubmitFailures"),
            SegmentSwitchesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSegmentSwitches"),
            Fmp4ReopensAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackFmp4Reopens"),
            WriteHeadWaitsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackWriteHeadWaits"),
            NearLiveSnapsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackNearLiveSnaps"),
            DecodeErrorSnapsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackDecodeErrorSnaps"),
            LastWriteHeadWaitGapMsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs"),
            SeekForwardDecodeCapHitsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits"),
            SeekForwardDecodeCapHitsDelta: observed
                ? GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")
                : 0,
            LastSeekHitForwardDecodeCapAtEnd: observed &&
                                               GetBool(endSnapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap"));
}

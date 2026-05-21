using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackStagesProjection BuildPerformanceTimelineFlashbackPlaybackStagesProjection(
        AutomationSnapshot snapshot)
        => new(
            SubmitFailures: snapshot.FlashbackPlaybackSubmitFailures,
            LastDropUtcUnixMs: snapshot.FlashbackPlaybackLastDropUtcUnixMs,
            LastDropReason: snapshot.FlashbackPlaybackLastDropReason,
            LastSubmitFailureUtcUnixMs: snapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            LastSubmitFailure: snapshot.FlashbackPlaybackLastSubmitFailure,
            SegmentSwitches: snapshot.FlashbackPlaybackSegmentSwitches,
            Fmp4Reopens: snapshot.FlashbackPlaybackFmp4Reopens,
            WriteHeadWaits: snapshot.FlashbackPlaybackWriteHeadWaits,
            NearLiveSnaps: snapshot.FlashbackPlaybackNearLiveSnaps,
            DecodeErrorSnaps: snapshot.FlashbackPlaybackDecodeErrorSnaps,
            LastWriteHeadWaitGapMs: snapshot.FlashbackPlaybackLastWriteHeadWaitGapMs);

    private readonly record struct PerformanceTimelineFlashbackPlaybackStagesProjection(
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long LastWriteHeadWaitGapMs);
}

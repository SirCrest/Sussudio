using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackPlaybackStateHealthSnapshotFields CaptureFlashbackPlaybackStateHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.State.ToString() ?? "N/A",
            (long)(fbPlayback?.PlaybackPosition.TotalMilliseconds ?? 0),
            fbPlayback?.DecoderHwAccel ?? "N/A",
            fbPlayback?.PlaybackFrameCount ?? 0,
            fbPlayback?.PlaybackLateFrames ?? 0,
            fbPlayback?.PlaybackDroppedFrames ?? 0,
            fbPlayback?.PlaybackSegmentSwitches ?? 0,
            fbPlayback?.PlaybackFmp4Reopens ?? 0,
            fbPlayback?.PlaybackWriteHeadWaits ?? 0,
            fbPlayback?.PlaybackNearLiveSnaps ?? 0,
            fbPlayback?.PlaybackDecodeErrorSnaps ?? 0,
            fbPlayback?.PlaybackSubmitFailures ?? 0,
            fbPlayback?.LastPlaybackDropUtcUnixMs ?? 0,
            fbPlayback?.LastPlaybackDropReason ?? string.Empty,
            fbPlayback?.LastSubmitFailureUtcUnixMs ?? 0,
            fbPlayback?.LastSubmitFailure ?? string.Empty,
            fbPlayback?.LastSegmentSwitchUtcUnixMs ?? 0,
            fbPlayback?.LastFmp4ReopenUtcUnixMs ?? 0,
            fbPlayback?.LastWriteHeadWaitGapMs ?? 0,
            fbPlayback?.PlaybackTargetFps ?? 0,
            fbPlayback?.PlaybackObservedFps ?? 0,
            fbPlayback?.PlaybackAvgFrameMs ?? 0,
            fbPlayback?.PlaybackPtsCadenceMismatchCount ?? 0,
            fbPlayback?.LastPlaybackPtsCadenceMismatchUtcUnixMs ?? 0,
            fbPlayback?.LastPlaybackPtsCadenceDeltaMs ?? 0,
            fbPlayback?.LastPlaybackPtsCadenceExpectedMs ?? 0,
            fbPlayback?.PlaybackSeekForwardDecodeCapHits ?? 0,
            fbPlayback?.LastPlaybackSeekHitForwardDecodeCap ?? false,
            fbPlayback?.AvDriftMs ?? 0,
            fbPlayback?.PlaybackThreadAlive ?? false);

    private readonly record struct FlashbackPlaybackStateHealthSnapshotFields(
        string State,
        long PositionMs,
        string DecoderHwAccel,
        long FrameCount,
        long LateFrames,
        long DroppedFrames,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long LastSegmentSwitchUtcUnixMs,
        long LastFmp4ReopenUtcUnixMs,
        long LastWriteHeadWaitGapMs,
        double TargetFps,
        double ObservedFps,
        double AvgFrameMs,
        long PtsCadenceMismatchCount,
        long LastPtsCadenceMismatchUtcUnixMs,
        double LastPtsCadenceDeltaMs,
        double LastPtsCadenceExpectedMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap,
        double AvDriftMs,
        bool ThreadAlive);
}

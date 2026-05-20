using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var state = CaptureFlashbackPlaybackStateHealthSnapshotFields(fbPlayback);
        var cadence = CaptureFlashbackPlaybackCadenceHealthSnapshotFields(fbPlayback);
        var decode = CaptureFlashbackPlaybackDecodeHealthSnapshotFields(fbPlayback);
        var audioMaster = CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(fbPlayback);
        var commands = CaptureFlashbackPlaybackCommandHealthSnapshotFields(fbPlayback);

        return new FlashbackPlaybackHealthSnapshotFields(
            state.State,
            state.PositionMs,
            state.DecoderHwAccel,
            state.FrameCount,
            state.LateFrames,
            state.DroppedFrames,
            audioMaster.DelayDoubles,
            audioMaster.DelayShrinks,
            audioMaster.Fallbacks,
            audioMaster.UnavailableFallbacks,
            audioMaster.StaleFallbacks,
            audioMaster.DriftOutlierFallbacks,
            audioMaster.LastFallbackReason,
            audioMaster.LastFallbackDriftMs,
            audioMaster.LastFallbackClockAgeMs,
            state.SegmentSwitches,
            state.Fmp4Reopens,
            state.WriteHeadWaits,
            state.NearLiveSnaps,
            state.DecodeErrorSnaps,
            state.SubmitFailures,
            state.LastDropUtcUnixMs,
            state.LastDropReason,
            state.LastSubmitFailureUtcUnixMs,
            state.LastSubmitFailure,
            state.LastSegmentSwitchUtcUnixMs,
            state.LastFmp4ReopenUtcUnixMs,
            state.LastWriteHeadWaitGapMs,
            state.TargetFps,
            state.ObservedFps,
            state.AvgFrameMs,
            cadence.SampleCount,
            cadence.P95FrameMs,
            cadence.P99FrameMs,
            cadence.MaxFrameMs,
            cadence.SlowFrames,
            cadence.SlowFramePercent,
            cadence.OnePercentLowFps,
            cadence.FivePercentLowFps,
            cadence.SampleDurationMs,
            cadence.RecentFrameIntervalsMs,
            state.PtsCadenceMismatchCount,
            state.LastPtsCadenceMismatchUtcUnixMs,
            state.LastPtsCadenceDeltaMs,
            state.LastPtsCadenceExpectedMs,
            state.SeekForwardDecodeCapHits,
            state.LastSeekHitForwardDecodeCap,
            decode.SampleCount,
            decode.AvgMs,
            decode.P95Ms,
            decode.P99Ms,
            decode.MaxMs,
            decode.MaxPhase,
            decode.MaxReceiveMs,
            decode.MaxFeedMs,
            decode.MaxReadMs,
            decode.MaxSendMs,
            decode.MaxAudioMs,
            decode.MaxConvertMs,
            decode.MaxUtcUnixMs,
            decode.MaxPositionMs,
            state.AvDriftMs,
            state.ThreadAlive,
            commands.CommandsEnqueued,
            commands.CommandsProcessed,
            commands.CommandsDropped,
            commands.CommandsSkippedNotReady,
            commands.ScrubUpdatesCoalesced,
            commands.SeekCommandsCoalesced,
            commands.CommandQueueCapacity,
            commands.PendingCommands,
            commands.MaxPendingCommands,
            commands.LastCommandQueueLatencyMs,
            commands.MaxCommandQueueLatencyMs,
            commands.MaxCommandQueueLatencyCommand,
            commands.LastCommandQueued,
            commands.LastCommandProcessed,
            commands.LastCommandQueuedUtcUnixMs,
            commands.LastCommandProcessedUtcUnixMs,
            commands.LastCommandFailureUtcUnixMs,
            commands.LastCommandFailure);
    }
}

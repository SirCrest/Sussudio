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

    private static FlashbackPlaybackCadenceHealthSnapshotFields CaptureFlashbackPlaybackCadenceHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics() ?? default;
        return new FlashbackPlaybackCadenceHealthSnapshotFields(
            playbackCadence.SampleCount,
            playbackCadence.P95FrameMs,
            playbackCadence.P99FrameMs,
            playbackCadence.MaxFrameMs,
            playbackCadence.SlowFrameCount,
            playbackCadence.SlowFramePercent,
            playbackCadence.OnePercentLowFps,
            playbackCadence.FivePercentLowFps,
            playbackCadence.SampleDurationMs,
            playbackCadence.RecentFrameIntervalsMs);
    }

    private static FlashbackPlaybackDecodeHealthSnapshotFields CaptureFlashbackPlaybackDecodeHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics() ?? default;
        return new FlashbackPlaybackDecodeHealthSnapshotFields(
            playbackDecode.SampleCount,
            playbackDecode.AvgMs,
            playbackDecode.P95Ms,
            playbackDecode.P99Ms,
            playbackDecode.MaxMs,
            fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty,
            fbPlayback?.PlaybackMaxDecodeReceiveMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeFeedMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeReadMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeSendMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeAudioMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeConvertMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeUtcUnixMs ?? 0,
            fbPlayback?.PlaybackMaxDecodePositionMs ?? 0);
    }

    private static FlashbackPlaybackAudioMasterHealthSnapshotFields CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.PlaybackAudioMasterDelayDoubles ?? 0,
            fbPlayback?.PlaybackAudioMasterDelayShrinks ?? 0,
            fbPlayback?.PlaybackAudioMasterFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterUnavailableFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterStaleFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterDriftOutlierFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterLastFallbackReason ?? string.Empty,
            fbPlayback?.PlaybackAudioMasterLastFallbackDriftMs ?? 0,
            fbPlayback?.PlaybackAudioMasterLastFallbackClockAgeMs ?? 0);

    private static FlashbackPlaybackCommandHealthSnapshotFields CaptureFlashbackPlaybackCommandHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.CommandsEnqueued ?? 0,
            fbPlayback?.CommandsProcessed ?? 0,
            fbPlayback?.CommandsDropped ?? 0,
            fbPlayback?.CommandsSkippedNotReady ?? 0,
            fbPlayback?.ScrubUpdatesCoalesced ?? 0,
            fbPlayback?.SeekCommandsCoalesced ?? 0,
            fbPlayback?.CommandQueueCapacityCommands ?? 0,
            fbPlayback?.PendingCommands ?? 0,
            fbPlayback?.MaxPendingCommands ?? 0,
            fbPlayback?.LastCommandQueueLatencyMs ?? 0,
            fbPlayback?.MaxCommandQueueLatencyMs ?? 0,
            fbPlayback?.MaxCommandQueueLatencyCommand ?? "None",
            fbPlayback?.LastCommandQueued ?? "None",
            fbPlayback?.LastCommandProcessed ?? "None",
            fbPlayback?.LastCommandQueuedUtcUnixMs ?? 0,
            fbPlayback?.LastCommandProcessedUtcUnixMs ?? 0,
            fbPlayback?.LastCommandFailureUtcUnixMs ?? 0,
            fbPlayback?.LastCommandFailure ?? string.Empty);

}

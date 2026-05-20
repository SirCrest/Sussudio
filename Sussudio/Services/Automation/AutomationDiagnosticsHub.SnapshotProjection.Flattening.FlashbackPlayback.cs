namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(
        FlashbackPlaybackProjection flashbackPlayback)
        => new()
        {
            State = flashbackPlayback.State,
            PositionMs = flashbackPlayback.PositionMs,
            DecoderHwAccel = flashbackPlayback.DecoderHwAccel,
            FrameCount = flashbackPlayback.FrameCount,
            LateFrames = flashbackPlayback.LateFrames,
            DroppedFrames = flashbackPlayback.DroppedFrames,
            AudioMasterDelayDoubles = flashbackPlayback.AudioMaster.DelayDoubles,
            AudioMasterDelayShrinks = flashbackPlayback.AudioMaster.DelayShrinks,
            AudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,
            AudioMasterUnavailableFallbacks = flashbackPlayback.AudioMaster.UnavailableFallbacks,
            AudioMasterStaleFallbacks = flashbackPlayback.AudioMaster.StaleFallbacks,
            AudioMasterDriftOutlierFallbacks = flashbackPlayback.AudioMaster.DriftOutlierFallbacks,
            AudioMasterLastFallbackReason = flashbackPlayback.AudioMaster.LastFallbackReason,
            AudioMasterLastFallbackDriftMs = flashbackPlayback.AudioMaster.LastFallbackDriftMs,
            AudioMasterLastFallbackClockAgeMs = flashbackPlayback.AudioMaster.LastFallbackClockAgeMs,
            SegmentSwitches = flashbackPlayback.SegmentSwitches,
            Fmp4Reopens = flashbackPlayback.Fmp4Reopens,
            WriteHeadWaits = flashbackPlayback.WriteHeadWaits,
            NearLiveSnaps = flashbackPlayback.NearLiveSnaps,
            DecodeErrorSnaps = flashbackPlayback.DecodeErrorSnaps,
            SubmitFailures = flashbackPlayback.SubmitFailures,
            LastDropUtcUnixMs = flashbackPlayback.LastDropUtcUnixMs,
            LastDropReason = flashbackPlayback.LastDropReason,
            LastSubmitFailureUtcUnixMs = flashbackPlayback.LastSubmitFailureUtcUnixMs,
            LastSubmitFailure = flashbackPlayback.LastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = flashbackPlayback.LastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = flashbackPlayback.LastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = flashbackPlayback.LastWriteHeadWaitGapMs,
            TargetFps = flashbackPlayback.TargetFps,
            ObservedFps = flashbackPlayback.ObservedFps,
            AvgFrameMs = flashbackPlayback.AvgFrameMs,
            CadenceSampleCount = flashbackPlayback.CadenceSampleCount,
            P95FrameMs = flashbackPlayback.P95FrameMs,
            P99FrameMs = flashbackPlayback.P99FrameMs,
            MaxFrameMs = flashbackPlayback.MaxFrameMs,
            SlowFrames = flashbackPlayback.SlowFrames,
            SlowFramePercent = flashbackPlayback.SlowFramePercent,
            OnePercentLowFps = flashbackPlayback.OnePercentLowFps,
            FivePercentLowFps = flashbackPlayback.FivePercentLowFps,
            SampleDurationMs = flashbackPlayback.SampleDurationMs,
            RecentFrameIntervalsMs = flashbackPlayback.RecentFrameIntervalsMs,
            PtsCadenceMismatchCount = flashbackPlayback.PtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = flashbackPlayback.LastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = flashbackPlayback.LastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = flashbackPlayback.LastPtsCadenceExpectedMs,
            SeekForwardDecodeCapHits = flashbackPlayback.Decode.SeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = flashbackPlayback.Decode.LastSeekHitForwardDecodeCap,
            DecodeSampleCount = flashbackPlayback.Decode.SampleCount,
            DecodeAvgMs = flashbackPlayback.Decode.AvgMs,
            DecodeP95Ms = flashbackPlayback.Decode.P95Ms,
            DecodeP99Ms = flashbackPlayback.Decode.P99Ms,
            DecodeMaxMs = flashbackPlayback.Decode.MaxMs,
            MaxDecodePhase = flashbackPlayback.Decode.MaxPhase,
            MaxDecodeReceiveMs = flashbackPlayback.Decode.MaxReceiveMs,
            MaxDecodeFeedMs = flashbackPlayback.Decode.MaxFeedMs,
            MaxDecodeReadMs = flashbackPlayback.Decode.MaxReadMs,
            MaxDecodeSendMs = flashbackPlayback.Decode.MaxSendMs,
            MaxDecodeAudioMs = flashbackPlayback.Decode.MaxAudioMs,
            MaxDecodeConvertMs = flashbackPlayback.Decode.MaxConvertMs,
            MaxDecodeUtcUnixMs = flashbackPlayback.Decode.MaxUtcUnixMs,
            MaxDecodePositionMs = flashbackPlayback.Decode.MaxPositionMs,
            AvDriftMs = flashbackPlayback.AvDriftMs,
            ThreadAlive = flashbackPlayback.Commands.ThreadAlive,
            CommandsEnqueued = flashbackPlayback.Commands.Enqueued,
            CommandsProcessed = flashbackPlayback.Commands.Processed,
            CommandsDropped = flashbackPlayback.Commands.Dropped,
            CommandsSkippedNotReady = flashbackPlayback.Commands.SkippedNotReady,
            ScrubUpdatesCoalesced = flashbackPlayback.Commands.ScrubUpdatesCoalesced,
            SeekCommandsCoalesced = flashbackPlayback.Commands.SeekCommandsCoalesced,
            CommandQueueCapacity = flashbackPlayback.Commands.QueueCapacity,
            PendingCommands = flashbackPlayback.Commands.Pending,
            MaxPendingCommands = flashbackPlayback.Commands.MaxPending,
            LastCommandQueueLatencyMs = flashbackPlayback.Commands.LastQueueLatencyMs,
            MaxCommandQueueLatencyMs = flashbackPlayback.Commands.MaxQueueLatencyMs,
            MaxCommandQueueLatencyCommand = flashbackPlayback.Commands.MaxQueueLatencyCommand,
            LastCommandQueued = flashbackPlayback.Commands.LastQueued,
            LastCommandProcessed = flashbackPlayback.Commands.LastProcessed,
            LastCommandQueuedUtcUnixMs = flashbackPlayback.Commands.LastQueuedUtcUnixMs,
            LastCommandProcessedUtcUnixMs = flashbackPlayback.Commands.LastProcessedUtcUnixMs,
            LastCommandFailureUtcUnixMs = flashbackPlayback.Commands.LastFailureUtcUnixMs,
            LastCommandFailure = flashbackPlayback.Commands.LastFailure
        };

    private readonly record struct FlashbackPlaybackFlattenedProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public long AudioMasterDelayDoubles { get; init; }
        public long AudioMasterDelayShrinks { get; init; }
        public long AudioMasterFallbacks { get; init; }
        public long AudioMasterUnavailableFallbacks { get; init; }
        public long AudioMasterStaleFallbacks { get; init; }
        public long AudioMasterDriftOutlierFallbacks { get; init; }
        public string AudioMasterLastFallbackReason { get; init; }
        public double AudioMasterLastFallbackDriftMs { get; init; }
        public double AudioMasterLastFallbackClockAgeMs { get; init; }
        public long SegmentSwitches { get; init; }
        public long Fmp4Reopens { get; init; }
        public long WriteHeadWaits { get; init; }
        public long NearLiveSnaps { get; init; }
        public long DecodeErrorSnaps { get; init; }
        public long SubmitFailures { get; init; }
        public long LastDropUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public long LastSubmitFailureUtcUnixMs { get; init; }
        public string LastSubmitFailure { get; init; }
        public long LastSegmentSwitchUtcUnixMs { get; init; }
        public long LastFmp4ReopenUtcUnixMs { get; init; }
        public long LastWriteHeadWaitGapMs { get; init; }
        public double TargetFps { get; init; }
        public double ObservedFps { get; init; }
        public double AvgFrameMs { get; init; }
        public int CadenceSampleCount { get; init; }
        public double P95FrameMs { get; init; }
        public double P99FrameMs { get; init; }
        public double MaxFrameMs { get; init; }
        public long SlowFrames { get; init; }
        public double SlowFramePercent { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentFrameIntervalsMs { get; init; }
        public long PtsCadenceMismatchCount { get; init; }
        public long LastPtsCadenceMismatchUtcUnixMs { get; init; }
        public double LastPtsCadenceDeltaMs { get; init; }
        public double LastPtsCadenceExpectedMs { get; init; }
        public long SeekForwardDecodeCapHits { get; init; }
        public bool LastSeekHitForwardDecodeCap { get; init; }
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeP99Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public string MaxDecodePhase { get; init; }
        public double MaxDecodeReceiveMs { get; init; }
        public double MaxDecodeFeedMs { get; init; }
        public double MaxDecodeReadMs { get; init; }
        public double MaxDecodeSendMs { get; init; }
        public double MaxDecodeAudioMs { get; init; }
        public double MaxDecodeConvertMs { get; init; }
        public long MaxDecodeUtcUnixMs { get; init; }
        public long MaxDecodePositionMs { get; init; }
        public double AvDriftMs { get; init; }
        public bool ThreadAlive { get; init; }
        public long CommandsEnqueued { get; init; }
        public long CommandsProcessed { get; init; }
        public long CommandsDropped { get; init; }
        public long CommandsSkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int CommandQueueCapacity { get; init; }
        public int PendingCommands { get; init; }
        public int MaxPendingCommands { get; init; }
        public long LastCommandQueueLatencyMs { get; init; }
        public long MaxCommandQueueLatencyMs { get; init; }
        public string MaxCommandQueueLatencyCommand { get; init; }
        public string LastCommandQueued { get; init; }
        public string LastCommandProcessed { get; init; }
        public long LastCommandQueuedUtcUnixMs { get; init; }
        public long LastCommandProcessedUtcUnixMs { get; init; }
        public long LastCommandFailureUtcUnixMs { get; init; }
        public string LastCommandFailure { get; init; }
    }
}

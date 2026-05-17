using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)
    {
        var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);
        var decode = BuildFlashbackPlaybackDecodeProjection(health);
        var commands = BuildFlashbackPlaybackCommandProjection(health);

        return new()
        {
            State = health.FlashbackPlaybackState,
            PositionMs = health.FlashbackPlaybackPositionMs,
            DecoderHwAccel = health.FlashbackDecoderHwAccel,
            FrameCount = health.FlashbackPlaybackFrameCount,
            LateFrames = health.FlashbackPlaybackLateFrames,
            DroppedFrames = health.FlashbackPlaybackDroppedFrames,
            AudioMaster = audioMaster,
            SegmentSwitches = health.FlashbackPlaybackSegmentSwitches,
            Fmp4Reopens = health.FlashbackPlaybackFmp4Reopens,
            WriteHeadWaits = health.FlashbackPlaybackWriteHeadWaits,
            NearLiveSnaps = health.FlashbackPlaybackNearLiveSnaps,
            DecodeErrorSnaps = health.FlashbackPlaybackDecodeErrorSnaps,
            SubmitFailures = health.FlashbackPlaybackSubmitFailures,
            LastDropUtcUnixMs = health.FlashbackPlaybackLastDropUtcUnixMs,
            LastDropReason = health.FlashbackPlaybackLastDropReason,
            LastSubmitFailureUtcUnixMs = health.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            LastSubmitFailure = health.FlashbackPlaybackLastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = health.FlashbackPlaybackLastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = health.FlashbackPlaybackLastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = health.FlashbackPlaybackLastWriteHeadWaitGapMs,
            TargetFps = health.FlashbackPlaybackTargetFps,
            ObservedFps = health.FlashbackPlaybackObservedFps,
            AvgFrameMs = health.FlashbackPlaybackAvgFrameMs,
            CadenceSampleCount = health.FlashbackPlaybackCadenceSampleCount,
            P95FrameMs = health.FlashbackPlaybackP95FrameMs,
            P99FrameMs = health.FlashbackPlaybackP99FrameMs,
            MaxFrameMs = health.FlashbackPlaybackMaxFrameMs,
            SlowFrames = health.FlashbackPlaybackSlowFrames,
            SlowFramePercent = health.FlashbackPlaybackSlowFramePercent,
            OnePercentLowFps = health.FlashbackPlaybackOnePercentLowFps,
            FivePercentLowFps = health.FlashbackPlaybackFivePercentLowFps,
            SampleDurationMs = health.FlashbackPlaybackSampleDurationMs,
            RecentFrameIntervalsMs = health.FlashbackPlaybackRecentFrameIntervalsMs,
            PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = health.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = health.FlashbackPlaybackLastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = health.FlashbackPlaybackLastPtsCadenceExpectedMs,
            Decode = decode,
            AvDriftMs = health.FlashbackAvDriftMs,
            Commands = commands
        };
    }

    private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)
        => new()
        {
            DelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            LastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs
        };

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

    private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)
        => new()
        {
            ThreadAlive = health.FlashbackPlaybackThreadAlive,
            Enqueued = health.FlashbackPlaybackCommandsEnqueued,
            Processed = health.FlashbackPlaybackCommandsProcessed,
            Dropped = health.FlashbackPlaybackCommandsDropped,
            SkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            QueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            Pending = health.FlashbackPlaybackPendingCommands,
            MaxPending = health.FlashbackPlaybackMaxPendingCommands,
            LastQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            MaxQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastQueued = health.FlashbackPlaybackLastCommandQueued,
            LastProcessed = health.FlashbackPlaybackLastCommandProcessed,
            LastQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            LastFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastFailure = health.FlashbackPlaybackLastCommandFailure
        };

    private readonly record struct FlashbackPlaybackProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterProjection AudioMaster { get; init; }
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
        public FlashbackPlaybackDecodeProjection Decode { get; init; }
        public double AvDriftMs { get; init; }
        public FlashbackPlaybackCommandProjection Commands { get; init; }
    }

    private readonly record struct FlashbackPlaybackAudioMasterProjection
    {
        public long DelayDoubles { get; init; }
        public long DelayShrinks { get; init; }
        public long Fallbacks { get; init; }
        public long UnavailableFallbacks { get; init; }
        public long StaleFallbacks { get; init; }
        public long DriftOutlierFallbacks { get; init; }
        public string LastFallbackReason { get; init; }
        public double LastFallbackDriftMs { get; init; }
        public double LastFallbackClockAgeMs { get; init; }
    }

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

    private readonly record struct FlashbackPlaybackCommandProjection
    {
        public bool ThreadAlive { get; init; }
        public long Enqueued { get; init; }
        public long Processed { get; init; }
        public long Dropped { get; init; }
        public long SkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int QueueCapacity { get; init; }
        public int Pending { get; init; }
        public int MaxPending { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string MaxQueueLatencyCommand { get; init; }
        public string LastQueued { get; init; }
        public string LastProcessed { get; init; }
        public long LastQueuedUtcUnixMs { get; init; }
        public long LastProcessedUtcUnixMs { get; init; }
        public long LastFailureUtcUnixMs { get; init; }
        public string LastFailure { get; init; }
    }
}

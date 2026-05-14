using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)
        => new()
        {
            State = health.FlashbackPlaybackState,
            PositionMs = health.FlashbackPlaybackPositionMs,
            DecoderHwAccel = health.FlashbackDecoderHwAccel,
            FrameCount = health.FlashbackPlaybackFrameCount,
            LateFrames = health.FlashbackPlaybackLateFrames,
            DroppedFrames = health.FlashbackPlaybackDroppedFrames,
            AudioMasterDelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            AudioMasterDelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            AudioMasterFallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            AudioMasterUnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            AudioMasterStaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            AudioMasterDriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            AudioMasterLastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            AudioMasterLastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            AudioMasterLastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs,
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
            SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = health.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            DecodeSampleCount = health.FlashbackPlaybackDecodeSampleCount,
            DecodeAvgMs = health.FlashbackPlaybackDecodeAvgMs,
            DecodeP95Ms = health.FlashbackPlaybackDecodeP95Ms,
            DecodeP99Ms = health.FlashbackPlaybackDecodeP99Ms,
            DecodeMaxMs = health.FlashbackPlaybackDecodeMaxMs,
            MaxDecodePhase = health.FlashbackPlaybackMaxDecodePhase,
            MaxDecodeReceiveMs = health.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxDecodeFeedMs = health.FlashbackPlaybackMaxDecodeFeedMs,
            MaxDecodeReadMs = health.FlashbackPlaybackMaxDecodeReadMs,
            MaxDecodeSendMs = health.FlashbackPlaybackMaxDecodeSendMs,
            MaxDecodeAudioMs = health.FlashbackPlaybackMaxDecodeAudioMs,
            MaxDecodeConvertMs = health.FlashbackPlaybackMaxDecodeConvertMs,
            MaxDecodeUtcUnixMs = health.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxDecodePositionMs = health.FlashbackPlaybackMaxDecodePositionMs,
            AvDriftMs = health.FlashbackAvDriftMs,
            ThreadAlive = health.FlashbackPlaybackThreadAlive,
            CommandsEnqueued = health.FlashbackPlaybackCommandsEnqueued,
            CommandsProcessed = health.FlashbackPlaybackCommandsProcessed,
            CommandsDropped = health.FlashbackPlaybackCommandsDropped,
            CommandsSkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            CommandQueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            PendingCommands = health.FlashbackPlaybackPendingCommands,
            MaxPendingCommands = health.FlashbackPlaybackMaxPendingCommands,
            LastCommandQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            MaxCommandQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxCommandQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastCommandQueued = health.FlashbackPlaybackLastCommandQueued,
            LastCommandProcessed = health.FlashbackPlaybackLastCommandProcessed,
            LastCommandQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            LastCommandProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            LastCommandFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastCommandFailure = health.FlashbackPlaybackLastCommandFailure
        };

    private readonly record struct FlashbackPlaybackProjection
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

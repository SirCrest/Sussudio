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
}

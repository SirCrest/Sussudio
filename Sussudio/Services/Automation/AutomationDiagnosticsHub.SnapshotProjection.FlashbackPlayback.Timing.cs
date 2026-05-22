using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)
        => new()
        {
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
            AvDriftMs = health.FlashbackAvDriftMs
        };

    private readonly record struct FlashbackPlaybackTimingProjection
    {
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
        public double AvDriftMs { get; init; }
    }

    private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(
        FlashbackPlaybackTimingProjection timing)
        => new()
        {
            SegmentSwitches = timing.SegmentSwitches,
            Fmp4Reopens = timing.Fmp4Reopens,
            WriteHeadWaits = timing.WriteHeadWaits,
            NearLiveSnaps = timing.NearLiveSnaps,
            DecodeErrorSnaps = timing.DecodeErrorSnaps,
            SubmitFailures = timing.SubmitFailures,
            LastDropUtcUnixMs = timing.LastDropUtcUnixMs,
            LastDropReason = timing.LastDropReason,
            LastSubmitFailureUtcUnixMs = timing.LastSubmitFailureUtcUnixMs,
            LastSubmitFailure = timing.LastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = timing.LastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = timing.LastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = timing.LastWriteHeadWaitGapMs,
            TargetFps = timing.TargetFps,
            ObservedFps = timing.ObservedFps,
            AvgFrameMs = timing.AvgFrameMs,
            CadenceSampleCount = timing.CadenceSampleCount,
            P95FrameMs = timing.P95FrameMs,
            P99FrameMs = timing.P99FrameMs,
            MaxFrameMs = timing.MaxFrameMs,
            SlowFrames = timing.SlowFrames,
            SlowFramePercent = timing.SlowFramePercent,
            OnePercentLowFps = timing.OnePercentLowFps,
            FivePercentLowFps = timing.FivePercentLowFps,
            SampleDurationMs = timing.SampleDurationMs,
            RecentFrameIntervalsMs = timing.RecentFrameIntervalsMs,
            PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = timing.LastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = timing.LastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = timing.LastPtsCadenceExpectedMs,
            AvDriftMs = timing.AvDriftMs
        };

    private readonly record struct FlashbackPlaybackTimingFlattenedProjection
    {
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
        public double AvDriftMs { get; init; }
    }
}

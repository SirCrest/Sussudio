namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(
        FlashbackPlaybackProjection flashbackPlayback)
        => new()
        {
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
            AvDriftMs = flashbackPlayback.AvDriftMs
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

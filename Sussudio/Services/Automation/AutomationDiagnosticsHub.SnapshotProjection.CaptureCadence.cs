using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)
        => new()
        {
            ExpectedFrameRate = health.ExpectedFrameRate,
            SampleCount = health.CaptureCadenceSampleCount,
            ObservedFps = health.CaptureCadenceObservedFps,
            ExpectedIntervalMs = health.CaptureCadenceExpectedIntervalMs,
            AverageIntervalMs = health.CaptureCadenceAverageIntervalMs,
            P95IntervalMs = health.CaptureCadenceP95IntervalMs,
            P99IntervalMs = health.CaptureCadenceP99IntervalMs,
            MaxIntervalMs = health.CaptureCadenceMaxIntervalMs,
            OnePercentLowFps = health.CaptureCadenceOnePercentLowFps,
            FivePercentLowFps = health.CaptureCadenceFivePercentLowFps,
            SampleDurationMs = health.CaptureCadenceSampleDurationMs,
            RecentIntervalsMs = health.CaptureCadenceRecentIntervalsMs,
            JitterStdDevMs = health.CaptureCadenceJitterStdDevMs,
            SevereGapCount = health.CaptureCadenceSevereGapCount,
            EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
            EstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent,
            VisualSampleCount = health.VisualCadenceSampleCount,
            VisualChangedFrameCount = health.VisualCadenceChangedFrameCount,
            VisualRepeatFrameCount = health.VisualCadenceRepeatFrameCount,
            VisualLongestRepeatRun = health.VisualCadenceLongestRepeatRun,
            VisualOutputObservedFps = health.VisualCadenceOutputObservedFps,
            VisualChangeObservedFps = health.VisualCadenceChangeObservedFps,
            VisualRepeatFramePercent = health.VisualCadenceRepeatFramePercent,
            VisualLastDelta = health.VisualCadenceLastDelta,
            VisualAverageDelta = health.VisualCadenceAverageDelta,
            VisualP95Delta = health.VisualCadenceP95Delta,
            VisualMotionScore = health.VisualCadenceMotionScore,
            VisualMotionConfidence = health.VisualCadenceMotionConfidence,
            VisualRecentOutputIntervalsMs = health.VisualCadenceRecentOutputIntervalsMs,
            VisualRecentChangeIntervalsMs = health.VisualCadenceRecentChangeIntervalsMs,
            VisualCenterSampleCount = health.VisualCenterCadenceSampleCount,
            VisualCenterChangedFrameCount = health.VisualCenterCadenceChangedFrameCount,
            VisualCenterRepeatFrameCount = health.VisualCenterCadenceRepeatFrameCount,
            VisualCenterLongestRepeatRun = health.VisualCenterCadenceLongestRepeatRun,
            VisualCenterOutputObservedFps = health.VisualCenterCadenceOutputObservedFps,
            VisualCenterChangeObservedFps = health.VisualCenterCadenceChangeObservedFps,
            VisualCenterRepeatFramePercent = health.VisualCenterCadenceRepeatFramePercent,
            VisualCenterLastDelta = health.VisualCenterCadenceLastDelta,
            VisualCenterAverageDelta = health.VisualCenterCadenceAverageDelta,
            VisualCenterP95Delta = health.VisualCenterCadenceP95Delta,
            VisualCenterMotionScore = health.VisualCenterCadenceMotionScore,
            VisualCenterMotionConfidence = health.VisualCenterCadenceMotionConfidence,
            VisualCenterRecentOutputIntervalsMs = health.VisualCenterCadenceRecentOutputIntervalsMs,
            VisualCenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs
        };

    private readonly record struct CaptureCadenceProjection
    {
        public double ExpectedFrameRate { get; init; }
        public int SampleCount { get; init; }
        public double ObservedFps { get; init; }
        public double ExpectedIntervalMs { get; init; }
        public double AverageIntervalMs { get; init; }
        public double P95IntervalMs { get; init; }
        public double P99IntervalMs { get; init; }
        public double MaxIntervalMs { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentIntervalsMs { get; init; }
        public double JitterStdDevMs { get; init; }
        public long SevereGapCount { get; init; }
        public long EstimatedDroppedFrames { get; init; }
        public double EstimatedDropPercent { get; init; }
        public int VisualSampleCount { get; init; }
        public long VisualChangedFrameCount { get; init; }
        public long VisualRepeatFrameCount { get; init; }
        public long VisualLongestRepeatRun { get; init; }
        public double VisualOutputObservedFps { get; init; }
        public double VisualChangeObservedFps { get; init; }
        public double VisualRepeatFramePercent { get; init; }
        public double VisualLastDelta { get; init; }
        public double VisualAverageDelta { get; init; }
        public double VisualP95Delta { get; init; }
        public double VisualMotionScore { get; init; }
        public string VisualMotionConfidence { get; init; }
        public double[] VisualRecentOutputIntervalsMs { get; init; }
        public double[] VisualRecentChangeIntervalsMs { get; init; }
        public int VisualCenterSampleCount { get; init; }
        public long VisualCenterChangedFrameCount { get; init; }
        public long VisualCenterRepeatFrameCount { get; init; }
        public long VisualCenterLongestRepeatRun { get; init; }
        public double VisualCenterOutputObservedFps { get; init; }
        public double VisualCenterChangeObservedFps { get; init; }
        public double VisualCenterRepeatFramePercent { get; init; }
        public double VisualCenterLastDelta { get; init; }
        public double VisualCenterAverageDelta { get; init; }
        public double VisualCenterP95Delta { get; init; }
        public double VisualCenterMotionScore { get; init; }
        public string VisualCenterMotionConfidence { get; init; }
        public double[] VisualCenterRecentOutputIntervalsMs { get; init; }
        public double[] VisualCenterRecentChangeIntervalsMs { get; init; }
    }
}

using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static VisualCadenceProjection BuildVisualCadenceProjection(CaptureHealthSnapshot health)
        => new()
        {
            SampleCount = health.VisualCadenceSampleCount,
            ChangedFrameCount = health.VisualCadenceChangedFrameCount,
            RepeatFrameCount = health.VisualCadenceRepeatFrameCount,
            LongestRepeatRun = health.VisualCadenceLongestRepeatRun,
            OutputObservedFps = health.VisualCadenceOutputObservedFps,
            ChangeObservedFps = health.VisualCadenceChangeObservedFps,
            RepeatFramePercent = health.VisualCadenceRepeatFramePercent,
            LastDelta = health.VisualCadenceLastDelta,
            AverageDelta = health.VisualCadenceAverageDelta,
            P95Delta = health.VisualCadenceP95Delta,
            MotionScore = health.VisualCadenceMotionScore,
            MotionConfidence = health.VisualCadenceMotionConfidence,
            RecentOutputIntervalsMs = health.VisualCadenceRecentOutputIntervalsMs,
            RecentChangeIntervalsMs = health.VisualCadenceRecentChangeIntervalsMs,
            CenterSampleCount = health.VisualCenterCadenceSampleCount,
            CenterChangedFrameCount = health.VisualCenterCadenceChangedFrameCount,
            CenterRepeatFrameCount = health.VisualCenterCadenceRepeatFrameCount,
            CenterLongestRepeatRun = health.VisualCenterCadenceLongestRepeatRun,
            CenterOutputObservedFps = health.VisualCenterCadenceOutputObservedFps,
            CenterChangeObservedFps = health.VisualCenterCadenceChangeObservedFps,
            CenterRepeatFramePercent = health.VisualCenterCadenceRepeatFramePercent,
            CenterLastDelta = health.VisualCenterCadenceLastDelta,
            CenterAverageDelta = health.VisualCenterCadenceAverageDelta,
            CenterP95Delta = health.VisualCenterCadenceP95Delta,
            CenterMotionScore = health.VisualCenterCadenceMotionScore,
            CenterMotionConfidence = health.VisualCenterCadenceMotionConfidence,
            CenterRecentOutputIntervalsMs = health.VisualCenterCadenceRecentOutputIntervalsMs,
            CenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs
        };

    private readonly record struct VisualCadenceProjection
    {
        public int SampleCount { get; init; }
        public long ChangedFrameCount { get; init; }
        public long RepeatFrameCount { get; init; }
        public long LongestRepeatRun { get; init; }
        public double OutputObservedFps { get; init; }
        public double ChangeObservedFps { get; init; }
        public double RepeatFramePercent { get; init; }
        public double LastDelta { get; init; }
        public double AverageDelta { get; init; }
        public double P95Delta { get; init; }
        public double MotionScore { get; init; }
        public string MotionConfidence { get; init; }
        public double[] RecentOutputIntervalsMs { get; init; }
        public double[] RecentChangeIntervalsMs { get; init; }
        public int CenterSampleCount { get; init; }
        public long CenterChangedFrameCount { get; init; }
        public long CenterRepeatFrameCount { get; init; }
        public long CenterLongestRepeatRun { get; init; }
        public double CenterOutputObservedFps { get; init; }
        public double CenterChangeObservedFps { get; init; }
        public double CenterRepeatFramePercent { get; init; }
        public double CenterLastDelta { get; init; }
        public double CenterAverageDelta { get; init; }
        public double CenterP95Delta { get; init; }
        public double CenterMotionScore { get; init; }
        public string CenterMotionConfidence { get; init; }
        public double[] CenterRecentOutputIntervalsMs { get; init; }
        public double[] CenterRecentChangeIntervalsMs { get; init; }
    }

    private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(
        VisualCadenceProjection visualCadence)
        => new()
        {
            SampleCount = visualCadence.SampleCount,
            ChangedFrameCount = visualCadence.ChangedFrameCount,
            RepeatFrameCount = visualCadence.RepeatFrameCount,
            LongestRepeatRun = visualCadence.LongestRepeatRun,
            OutputObservedFps = visualCadence.OutputObservedFps,
            ChangeObservedFps = visualCadence.ChangeObservedFps,
            RepeatFramePercent = visualCadence.RepeatFramePercent,
            LastDelta = visualCadence.LastDelta,
            AverageDelta = visualCadence.AverageDelta,
            P95Delta = visualCadence.P95Delta,
            MotionScore = visualCadence.MotionScore,
            MotionConfidence = visualCadence.MotionConfidence,
            RecentOutputIntervalsMs = visualCadence.RecentOutputIntervalsMs,
            RecentChangeIntervalsMs = visualCadence.RecentChangeIntervalsMs,
            CenterSampleCount = visualCadence.CenterSampleCount,
            CenterChangedFrameCount = visualCadence.CenterChangedFrameCount,
            CenterRepeatFrameCount = visualCadence.CenterRepeatFrameCount,
            CenterLongestRepeatRun = visualCadence.CenterLongestRepeatRun,
            CenterOutputObservedFps = visualCadence.CenterOutputObservedFps,
            CenterChangeObservedFps = visualCadence.CenterChangeObservedFps,
            CenterRepeatFramePercent = visualCadence.CenterRepeatFramePercent,
            CenterLastDelta = visualCadence.CenterLastDelta,
            CenterAverageDelta = visualCadence.CenterAverageDelta,
            CenterP95Delta = visualCadence.CenterP95Delta,
            CenterMotionScore = visualCadence.CenterMotionScore,
            CenterMotionConfidence = visualCadence.CenterMotionConfidence,
            CenterRecentOutputIntervalsMs = visualCadence.CenterRecentOutputIntervalsMs,
            CenterRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs
        };

    private readonly record struct VisualCadenceFlattenedProjection
    {
        public int SampleCount { get; init; }
        public long ChangedFrameCount { get; init; }
        public long RepeatFrameCount { get; init; }
        public long LongestRepeatRun { get; init; }
        public double OutputObservedFps { get; init; }
        public double ChangeObservedFps { get; init; }
        public double RepeatFramePercent { get; init; }
        public double LastDelta { get; init; }
        public double AverageDelta { get; init; }
        public double P95Delta { get; init; }
        public double MotionScore { get; init; }
        public string MotionConfidence { get; init; }
        public double[] RecentOutputIntervalsMs { get; init; }
        public double[] RecentChangeIntervalsMs { get; init; }
        public int CenterSampleCount { get; init; }
        public long CenterChangedFrameCount { get; init; }
        public long CenterRepeatFrameCount { get; init; }
        public long CenterLongestRepeatRun { get; init; }
        public double CenterOutputObservedFps { get; init; }
        public double CenterChangeObservedFps { get; init; }
        public double CenterRepeatFramePercent { get; init; }
        public double CenterLastDelta { get; init; }
        public double CenterAverageDelta { get; init; }
        public double CenterP95Delta { get; init; }
        public double CenterMotionScore { get; init; }
        public string CenterMotionConfidence { get; init; }
        public double[] CenterRecentOutputIntervalsMs { get; init; }
        public double[] CenterRecentChangeIntervalsMs { get; init; }
    }
}

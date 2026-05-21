using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public int VisualCadenceSampleCount { get; init; }
    public long VisualCadenceChangedFrameCount { get; init; }
    public long VisualCadenceRepeatFrameCount { get; init; }
    public long VisualCadenceLongestRepeatRun { get; init; }
    public double VisualCadenceOutputObservedFps { get; init; }
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public double VisualCadenceLastDelta { get; init; }
    public double VisualCadenceAverageDelta { get; init; }
    public double VisualCadenceP95Delta { get; init; }
    public double VisualCadenceMotionScore { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
    public int VisualCenterCadenceSampleCount { get; init; }
    public long VisualCenterCadenceChangedFrameCount { get; init; }
    public long VisualCenterCadenceRepeatFrameCount { get; init; }
    public long VisualCenterCadenceLongestRepeatRun { get; init; }
    public double VisualCenterCadenceOutputObservedFps { get; init; }
    public double VisualCenterCadenceChangeObservedFps { get; init; }
    public double VisualCenterCadenceRepeatFramePercent { get; init; }
    public double VisualCenterCadenceLastDelta { get; init; }
    public double VisualCenterCadenceAverageDelta { get; init; }
    public double VisualCenterCadenceP95Delta { get; init; }
    public double VisualCenterCadenceMotionScore { get; init; }
    public string VisualCenterCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCenterCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
}

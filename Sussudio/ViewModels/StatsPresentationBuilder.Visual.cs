using System;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    private const double VisualRepeatTolerancePercent = 0.25;

    private static string FormatHz(double value)
    {
        value = Sanitize(value);
        if (value <= 0)
        {
            return "\u2014";
        }

        var rounded = Math.Round(value);
        return Math.Abs(value - rounded) <= 0.15
            ? $"{rounded:0} Hz"
            : $"{value:0.##} Hz";
    }

    private static string FormatVisualRepeatSummary(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return "\u2014";
        }

        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return FormatHz(snapshot.VisualCadenceOutputFps);
        }

        var repeat = FormatPercent(snapshot.VisualCadenceRepeatPercent);
        return $"{FormatHz(snapshot.VisualCadenceChangeFps)} ({repeat} repeat, run {FormatCount(snapshot.VisualCadenceLongestRepeatRun)})";
    }

    private static string FormatVisualCadenceSummary(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return "\u2014";
        }

        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return FormatHz(snapshot.VisualCadenceOutputFps);
        }

        return $"{FormatHz(snapshot.VisualCadenceChangeFps)} / {FormatPercent(snapshot.VisualCadenceRepeatPercent)} rep";
    }

    private static string FormatVisualMotionSummary(StatsSnapshot snapshot)
    {
        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return $"{FormatPercent(snapshot.VisualCadenceMotionScore)} px / {snapshot.VisualCadenceMotionConfidence}";
        }

        return $"{FormatPercent(snapshot.VisualCadenceRepeatPercent)} repeat / run {FormatCount(snapshot.VisualCadenceLongestRepeatRun)} / {FormatPercent(snapshot.VisualCadenceMotionScore)} px / {snapshot.VisualCadenceMotionConfidence}";
    }

    private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return false;
        }

        var expectedRepeatPercent = GetExpectedVisualRepeatPercent(snapshot);
        var allowedRepeatPercent = expectedRepeatPercent + VisualRepeatTolerancePercent;
        return snapshot.VisualCadenceLongestRepeatRun <= 1 &&
               snapshot.VisualCadenceRepeatPercent <= allowedRepeatPercent;
    }

    private static double GetExpectedVisualRepeatPercent(StatsSnapshot snapshot)
    {
        var sourceFps = Sanitize(snapshot.SourceFrameRateExact ?? snapshot.SourceExpectedFps);
        var outputFps = Sanitize(snapshot.VisualCadenceOutputFps);
        if (sourceFps <= 0 || outputFps <= sourceFps)
        {
            return 0;
        }

        return Math.Clamp((outputFps - sourceFps) / outputFps * 100.0, 0.0, 100.0);
    }
}

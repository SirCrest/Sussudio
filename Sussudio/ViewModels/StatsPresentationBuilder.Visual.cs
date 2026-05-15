using System;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
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
}

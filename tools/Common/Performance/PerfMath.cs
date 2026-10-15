using System.Globalization;

namespace Sussudio.Tools.Performance;

// Small numeric helpers shared by the frame-trace analyzer and the experiment statistics. Exact order statistics on
// the samples (no histogram approximation): the inputs are at most a few million values, and the analyzer and the
// verdict rules both need numbers a reader can verify by hand.
internal static class PerfMath
{
    /// <summary>Linear-interpolated percentile of an ascending sample list, 0 for an empty one. <paramref name="q"/> is 0..1.</summary>
    public static double Percentile(IReadOnlyList<double> sortedAscending, double q)
    {
        var count = sortedAscending.Count;
        if (count == 0)
        {
            return 0;
        }

        if (count == 1)
        {
            return sortedAscending[0];
        }

        var position = Math.Clamp(q, 0d, 1d) * (count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
        {
            return sortedAscending[lower];
        }

        var fraction = position - lower;
        return sortedAscending[lower] + ((sortedAscending[upper] - sortedAscending[lower]) * fraction);
    }

    public static double Mean(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sum = 0d;
        foreach (var value in values)
        {
            sum += value;
        }

        return sum / values.Count;
    }

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.ToArray();
        Array.Sort(sorted);
        return Percentile(sorted, 0.5);
    }

    /// <summary>Rounds to four decimals and maps NaN and infinity to zero so JSON and text always render.</summary>
    public static double Round(double value)
        => double.IsFinite(value) ? Math.Round(value, 4, MidpointRounding.AwayFromZero) : 0d;

    public static string Fixed(double value, int decimals = 2)
        => value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<PresentMonRow> rawRows,
        IReadOnlyList<PresentMonRow> selectedRows,
        IReadOnlyList<PresentMonSwapChainSummary> swapChains,
        bool displayedTimeColumnPresent,
        bool displayChangeColumnPresent,
        string? expectedSwapChainAddress,
        bool expectedSwapChainMatched)
    {
        var warnings = new List<string>();
        var excludedRows = rawRows.Count - selectedRows.Count;
        if (excludedRows > 0)
        {
            warnings.Add($"Excluded {excludedRows} non-selected PresentMon row(s), usually secondary or artifact swap-chain events.");
        }

        if (!string.IsNullOrWhiteSpace(expectedSwapChainAddress) && !expectedSwapChainMatched)
        {
            warnings.Add($"Expected swap chain {expectedSwapChainAddress} was not present; no fallback swap chain was selected.");
        }
        else if (selectedRows.Count == 0)
        {
            warnings.Add("No non-artifact swap-chain rows were found; preview pacing metrics are unavailable.");
        }

        if (!displayedTimeColumnPresent)
        {
            warnings.Add("DisplayedTime column is absent in this PresentMon schema; NotDisplayedFrameCount is unavailable.");
        }

        if (!displayChangeColumnPresent)
        {
            warnings.Add("MsBetweenDisplayChange column is absent; display-change pacing is unavailable.");
        }

        if (swapChains.Count > 1 && string.IsNullOrWhiteSpace(expectedSwapChainAddress))
        {
            warnings.Add("Multiple swap-chain addresses were present; summary uses the dominant nonzero swap chain.");
        }

        return warnings;
    }

    private static IReadOnlyDictionary<string, int> CountValues(IEnumerable<string> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in values.Select(value => value.Trim()).Where(value => value.Length > 0))
        {
            counts.TryGetValue(field, out var count);
            counts[field] = count + 1;
        }

        return counts;
    }

    private static PresentMonMetricSummary Summarize(IEnumerable<double?> values)
    {
        var sorted = values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        if (sorted.Count == 0)
        {
            return new PresentMonMetricSummary();
        }

        sorted.Sort();
        return new PresentMonMetricSummary
        {
            SampleCount = sorted.Count,
            Average = sorted.Average(),
            P50 = Percentile(sorted, 0.50),
            P95 = Percentile(sorted, 0.95),
            P99 = Percentile(sorted, 0.99),
            Max = sorted[^1]
        };
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var position = (sortedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
        {
            return sortedValues[lower];
        }

        var fraction = position - lower;
        return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * fraction;
    }
}

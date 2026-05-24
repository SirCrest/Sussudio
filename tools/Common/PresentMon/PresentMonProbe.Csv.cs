using System.Globalization;
using System.Text;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static PresentMonCaptureSummary ParseCsv(string path)
        => ParseCsv(path, expectedSwapChainAddress: null);

    private static PresentMonCaptureSummary ParseCsv(string path, string? expectedSwapChainAddress)
        => ParseCsv(path, expectedSwapChainAddress, options: null, captureStartUtcUnixMs: null);

    private static PresentMonCaptureSummary ParseCsv(
        string path,
        string? expectedSwapChainAddress,
        PresentMonProbeOptions? options,
        long? captureStartUtcUnixMs)
    {
        var csvRows = ReadCsvRows(path);
        if (!csvRows.HasHeader)
        {
            return new PresentMonCaptureSummary();
        }

        var rows = csvRows.Rows;

        var normalizedExpectedSwapChain = NormalizeSwapChainAddress(expectedSwapChainAddress);
        var selectedSwapChain = SelectPrimarySwapChain(rows, normalizedExpectedSwapChain);
        var expectedSwapChainMatched = !string.IsNullOrWhiteSpace(normalizedExpectedSwapChain) &&
                                       string.Equals(selectedSwapChain, normalizedExpectedSwapChain, StringComparison.OrdinalIgnoreCase);
        var selectedRows = selectedSwapChain == null
            ? new List<PresentMonRow>()
            : rows.Where(row => string.Equals(row.SwapChainAddress, selectedSwapChain, StringComparison.OrdinalIgnoreCase)).ToList();
        var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);
        var notDisplayed = csvRows.DisplayedTimeColumnPresent
            ? selectedRows.Count(row => !row.DisplayedTimeMs.HasValue)
            : 0;
        var displayChangeUnavailable = csvRows.DisplayChangeColumnPresent
            ? selectedRows.Count(row => !row.BetweenDisplayChangeMs.HasValue)
            : 0;
        var warnings = BuildWarnings(
            rows,
            selectedRows,
            swapChains,
            csvRows.DisplayedTimeColumnPresent,
            csvRows.DisplayChangeColumnPresent,
            normalizedExpectedSwapChain,
            expectedSwapChainMatched);
        var appCorrelation = BuildAppCorrelation(selectedRows, options, captureStartUtcUnixMs);

        return new PresentMonCaptureSummary
        {
            SampleCount = selectedRows.Count,
            RawSampleCount = rows.Count,
            ExcludedSampleCount = Math.Max(0, rows.Count - selectedRows.Count),
            ExpectedSwapChainAddress = normalizedExpectedSwapChain,
            SelectedSwapChainAddress = selectedSwapChain,
            ExpectedSwapChainMatched = expectedSwapChainMatched,
            BetweenPresentsMs = Summarize(selectedRows.Select(row => row.BetweenPresentsMs)),
            BetweenDisplayChangeMs = Summarize(selectedRows.Select(row => row.BetweenDisplayChangeMs)),
            DisplayedTimeMs = Summarize(selectedRows.Select(row => row.DisplayedTimeMs)),
            UntilDisplayedMs = Summarize(selectedRows.Select(row => row.UntilDisplayedMs)),
            InPresentApiMs = Summarize(selectedRows.Select(row => row.InPresentApiMs)),
            CpuBusyMs = Summarize(selectedRows.Select(row => row.CpuBusyMs)),
            GpuBusyMs = Summarize(selectedRows.Select(row => row.GpuBusyMs)),
            GpuTimeMs = Summarize(selectedRows.Select(row => row.GpuTimeMs)),
            DisplayLatencyMs = Summarize(selectedRows.Select(row => row.DisplayLatencyMs)),
            NotDisplayedFrameCount = notDisplayed,
            NotDisplayedFramePercent = selectedRows.Count <= 0 ? 0 : notDisplayed * 100.0 / selectedRows.Count,
            DisplayedTimeColumnPresent = csvRows.DisplayedTimeColumnPresent,
            DisplayChangeUnavailableCount = displayChangeUnavailable,
            DisplayChangeUnavailablePercent = selectedRows.Count <= 0 ? 0 : displayChangeUnavailable * 100.0 / selectedRows.Count,
            PresentModes = CountValues(selectedRows.Select(row => row.PresentMode)),
            PresentRuntimes = CountValues(selectedRows.Select(row => row.PresentRuntime)),
            SyncIntervals = CountValues(selectedRows.Select(row => row.SyncInterval)),
            AllowsTearing = CountValues(selectedRows.Select(row => row.AllowsTearing)),
            SwapChains = swapChains,
            AppCorrelation = appCorrelation,
            Warnings = warnings
        };
    }

    private static string? SelectPrimarySwapChain(IReadOnlyList<PresentMonRow> rows, string? expectedSwapChainAddress)
    {
        if (!string.IsNullOrWhiteSpace(expectedSwapChainAddress))
        {
            return rows.Any(row => string.Equals(row.SwapChainAddress, expectedSwapChainAddress, StringComparison.OrdinalIgnoreCase))
                ? expectedSwapChainAddress
                : null;
        }

        var selected = rows
            .Where(row => !IsArtifactSwapChain(row.SwapChainAddress))
            .GroupBy(row => row.SwapChainAddress, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        return selected?.Key;
    }

    private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(
        IReadOnlyList<PresentMonRow> rows,
        string? selectedSwapChain)
    {
        return rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.SwapChainAddress) ? "(missing)" : row.SwapChainAddress, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var groupRows = group.ToArray();
                return new PresentMonSwapChainSummary
                {
                    Address = group.Key,
                    SampleCount = groupRows.Length,
                    Selected = string.Equals(group.Key, selectedSwapChain, StringComparison.OrdinalIgnoreCase),
                    Artifact = IsArtifactSwapChain(group.Key),
                    BetweenPresentsMs = Summarize(groupRows.Select(row => row.BetweenPresentsMs)),
                    BetweenDisplayChangeMs = Summarize(groupRows.Select(row => row.BetweenDisplayChangeMs)),
                    UntilDisplayedMs = Summarize(groupRows.Select(row => row.UntilDisplayedMs)),
                    PresentModes = CountValues(groupRows.Select(row => row.PresentMode))
                };
            })
            .OrderByDescending(item => item.Selected)
            .ThenByDescending(item => item.SampleCount)
            .ToArray();
    }

    private static bool IsArtifactSwapChain(string? swapChainAddress)
        => string.IsNullOrWhiteSpace(swapChainAddress) ||
           string.Equals(swapChainAddress.Trim(), "0x0", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeSwapChainAddress(string? swapChainAddress)
    {
        if (string.IsNullOrWhiteSpace(swapChainAddress))
        {
            return null;
        }

        var value = swapChainAddress.Trim();
        if (value.Equals("0x0", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var digits = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : value;
        if (ulong.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric == 0 ? null : $"0x{numeric:X}";
        }

        return value.ToUpperInvariant();
    }

    private static string NormalizeHeader(string value)
        => value.Trim().TrimStart('\uFEFF');

    private static double? ReadMetric(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index,
        params string[] names)
    {
        var found = false;
        var fieldIndex = -1;
        foreach (var name in names)
        {
            if (index.TryGetValue(name, out fieldIndex))
            {
                found = true;
                break;
            }
        }

        if (!found || fieldIndex >= fields.Count)
        {
            return null;
        }

        var field = fields[fieldIndex].Trim();
        if (field.Length == 0 || string.Equals(field, "NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            !double.IsNaN(value) &&
            !double.IsInfinity(value))
        {
            return value;
        }

        return null;
    }

    private static string ReadField(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index,
        string name)
    {
        if (!index.TryGetValue(name, out var fieldIndex) || fieldIndex >= fields.Count)
        {
            return string.Empty;
        }

        return fields[fieldIndex].Trim();
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(ch);
            }
        }

        result.Add(builder.ToString());
        return result;
    }
}

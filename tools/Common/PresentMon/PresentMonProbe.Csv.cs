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

    private sealed record PresentMonCsvRows(
        bool HasHeader,
        IReadOnlyList<PresentMonRow> Rows,
        bool DisplayedTimeColumnPresent,
        bool DisplayChangeColumnPresent);

    private sealed record PresentMonRow(
        int RowIndex,
        string SwapChainAddress,
        string PresentMode,
        string PresentRuntime,
        string SyncInterval,
        string AllowsTearing,
        double? CpuStartTimeMs,
        double? BetweenPresentsMs,
        double? BetweenDisplayChangeMs,
        double? DisplayedTimeMs,
        double? UntilDisplayedMs,
        double? InPresentApiMs,
        double? CpuBusyMs,
        double? GpuBusyMs,
        double? GpuTimeMs,
        double? DisplayLatencyMs);

    private static PresentMonCsvRows ReadCsvRows(string path)
    {
        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new PresentMonCsvRows(
                HasHeader: false,
                Rows: Array.Empty<PresentMonRow>(),
                DisplayedTimeColumnPresent: false,
                DisplayChangeColumnPresent: false);
        }

        var headers = SplitCsvLine(headerLine);
        var index = BuildCsvHeaderIndex(headers);
        var displayedTimeColumnPresent = HasAnyColumn(index, "DisplayedTime");
        var displayChangeColumnPresent = HasAnyColumn(index, "MsBetweenDisplayChange");
        var rows = new List<PresentMonRow>();

        string? line;
        var rowIndex = 0;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            rows.Add(ReadRow(rowIndex++, fields, index));
        }

        return new PresentMonCsvRows(
            HasHeader: true,
            Rows: rows,
            DisplayedTimeColumnPresent: displayedTimeColumnPresent,
            DisplayChangeColumnPresent: displayChangeColumnPresent);
    }

    private static IReadOnlyDictionary<string, int> BuildCsvHeaderIndex(IReadOnlyList<string> headers)
    {
        return headers
            .Select((name, i) => (Name: NormalizeHeader(name), Index: i))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasAnyColumn(IReadOnlyDictionary<string, int> index, params string[] names)
        => names.Any(index.ContainsKey);

    private static PresentMonRow ReadRow(
        int rowIndex,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index)
    {
        return new PresentMonRow(
            RowIndex: rowIndex,
            SwapChainAddress: NormalizeSwapChainAddress(ReadField(fields, index, "SwapChainAddress")) ?? string.Empty,
            PresentMode: ReadField(fields, index, "PresentMode"),
            PresentRuntime: ReadField(fields, index, "PresentRuntime"),
            SyncInterval: ReadField(fields, index, "SyncInterval"),
            AllowsTearing: ReadField(fields, index, "AllowsTearing"),
            CpuStartTimeMs: ReadMetric(fields, index, "CPUStartTime"),
            BetweenPresentsMs: ReadMetric(fields, index, "MsBetweenPresents", "FrameTime"),
            BetweenDisplayChangeMs: ReadMetric(fields, index, "MsBetweenDisplayChange"),
            DisplayedTimeMs: ReadMetric(fields, index, "DisplayedTime"),
            UntilDisplayedMs: ReadMetric(fields, index, "MsUntilDisplayed"),
            InPresentApiMs: ReadMetric(fields, index, "MsInPresentAPI"),
            CpuBusyMs: ReadMetric(fields, index, "MsCPUBusy", "CPUBusy"),
            GpuBusyMs: ReadMetric(fields, index, "MsGPUBusy", "GPUBusy"),
            GpuTimeMs: ReadMetric(fields, index, "MsGPUTime", "GPUTime"),
            DisplayLatencyMs: ReadMetric(fields, index, "DisplayLatency"));
    }


    private static PresentMonAppCorrelation BuildAppCorrelation(
        IReadOnlyList<PresentMonRow> selectedRows,
        PresentMonProbeOptions? options,
        long? captureStartUtcUnixMs)
    {
        if (options?.AppPresentUtcUnixMs is not long appPresentUtcUnixMs || appPresentUtcUnixMs <= 0)
        {
            return new PresentMonAppCorrelation();
        }

        var startUtcUnixMs = options.CaptureStartUtcUnixMs ?? captureStartUtcUnixMs;
        if (!startUtcUnixMs.HasValue || startUtcUnixMs.Value <= 0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "Capture start timestamp was unavailable.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs
            };
        }

        var appOffsetMs = appPresentUtcUnixMs - startUtcUnixMs.Value;
        var candidates = selectedRows
            .Where(row => row.CpuStartTimeMs.HasValue)
            .Select(row => new
            {
                Row = row,
                DeltaMs = Math.Abs(row.CpuStartTimeMs!.Value - appOffsetMs)
            })
            .OrderBy(candidate => candidate.DeltaMs)
            .ToList();

        if (candidates.Count == 0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "No selected PresentMon rows exposed CPUStartTime.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs,
                AppPresentOffsetMs = appOffsetMs
            };
        }

        var best = candidates[0];
        if (best.DeltaMs > 50.0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "Nearest PresentMon row was outside the 50ms app-present correlation window.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs,
                AppPresentOffsetMs = appOffsetMs,
                PresentMonRowIndex = best.Row.RowIndex,
                PresentMonCpuStartTimeMs = best.Row.CpuStartTimeMs.GetValueOrDefault(),
                DeltaMs = best.DeltaMs,
                Outcome = ClassifyPresentOutcome(best.Row),
                PresentMode = best.Row.PresentMode,
                UntilDisplayedMs = best.Row.UntilDisplayedMs,
                DisplayLatencyMs = best.Row.DisplayLatencyMs
            };
        }

        return new PresentMonAppCorrelation
        {
            Available = true,
            Reason = "Nearest selected PresentMon row by app UTC present timestamp.",
            AppPresentId = options.AppPresentId ?? 0,
            AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
            AppPresentUtcUnixMs = appPresentUtcUnixMs,
            AppPresentOffsetMs = appOffsetMs,
            PresentMonRowIndex = best.Row.RowIndex,
            PresentMonCpuStartTimeMs = best.Row.CpuStartTimeMs.GetValueOrDefault(),
            DeltaMs = best.DeltaMs,
            Outcome = ClassifyPresentOutcome(best.Row),
            PresentMode = best.Row.PresentMode,
            UntilDisplayedMs = best.Row.UntilDisplayedMs,
            DisplayLatencyMs = best.Row.DisplayLatencyMs
        };
    }

    private static string ClassifyPresentOutcome(PresentMonRow row)
    {
        if (!row.DisplayedTimeMs.HasValue)
        {
            return "SupersededOrNotDisplayed";
        }

        if (row.UntilDisplayedMs.GetValueOrDefault() >= 16.0)
        {
            return "DisplayedLate";
        }

        return "Displayed";
    }


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

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
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
}

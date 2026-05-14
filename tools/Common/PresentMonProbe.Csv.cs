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
        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new PresentMonCaptureSummary();
        }

        var headers = SplitCsvLine(headerLine);
        var index = headers
            .Select((name, i) => (Name: NormalizeHeader(name), Index: i))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
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
            rows.Add(new PresentMonRow(
                RowIndex: rowIndex++,
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
                DisplayLatencyMs: ReadMetric(fields, index, "DisplayLatency")));
        }

        var normalizedExpectedSwapChain = NormalizeSwapChainAddress(expectedSwapChainAddress);
        var selectedSwapChain = SelectPrimarySwapChain(rows, normalizedExpectedSwapChain);
        var expectedSwapChainMatched = !string.IsNullOrWhiteSpace(normalizedExpectedSwapChain) &&
                                       string.Equals(selectedSwapChain, normalizedExpectedSwapChain, StringComparison.OrdinalIgnoreCase);
        var selectedRows = selectedSwapChain == null
            ? new List<PresentMonRow>()
            : rows.Where(row => string.Equals(row.SwapChainAddress, selectedSwapChain, StringComparison.OrdinalIgnoreCase)).ToList();
        var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);
        var notDisplayed = displayedTimeColumnPresent
            ? selectedRows.Count(row => !row.DisplayedTimeMs.HasValue)
            : 0;
        var displayChangeUnavailable = displayChangeColumnPresent
            ? selectedRows.Count(row => !row.BetweenDisplayChangeMs.HasValue)
            : 0;
        var warnings = BuildWarnings(
            rows,
            selectedRows,
            swapChains,
            displayedTimeColumnPresent,
            displayChangeColumnPresent,
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
            DisplayedTimeColumnPresent = displayedTimeColumnPresent,
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
}

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
}

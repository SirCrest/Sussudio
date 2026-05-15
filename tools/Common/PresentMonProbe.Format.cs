using System.Text;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    public static string Format(PresentMonProbeResult result)
    {
        if (!result.Success || result.Summary == null)
        {
            var detail = new StringBuilder(result.Message);
            if (result.Summary != null)
            {
                AppendSummaryContext(detail, result.Summary);
            }

            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                detail.AppendLine();
                detail.AppendLine(result.StdErr.Trim());
            }

            return detail.ToString();
        }

        var summary = result.Summary;
        var builder = new StringBuilder();
        builder.AppendLine(result.Message);
        builder.AppendLine($"Target: {result.TargetProcessName} ({result.TargetProcessId})");
        builder.AppendLine($"PresentMon: {result.PresentMonPath}");
        if (!string.IsNullOrWhiteSpace(summary.SelectedSwapChainAddress))
        {
            builder.AppendLine(
                $"Selected Swap Chain: {summary.SelectedSwapChainAddress} ({summary.SampleCount}/{summary.RawSampleCount} rows, excluded={summary.ExcludedSampleCount})");
        }
        if (!string.IsNullOrWhiteSpace(summary.ExpectedSwapChainAddress))
        {
            builder.AppendLine(
                $"Expected Swap Chain: {summary.ExpectedSwapChainAddress} matched={summary.ExpectedSwapChainMatched}");
        }

        if (!string.IsNullOrWhiteSpace(result.CsvPath))
        {
            builder.AppendLine($"CSV: {result.CsvPath}");
        }

        foreach (var warning in summary.Warnings)
        {
            builder.AppendLine($"Warning: {warning}");
        }

        AppendMetric(builder, "Between Presents", summary.BetweenPresentsMs);
        AppendMetric(builder, "Display Change", summary.BetweenDisplayChangeMs);
        AppendMetric(builder, "Displayed Time", summary.DisplayedTimeMs);
        AppendMetric(builder, "Until Displayed", summary.UntilDisplayedMs);
        AppendMetric(builder, "In Present API", summary.InPresentApiMs);
        AppendMetric(builder, "CPU Busy", summary.CpuBusyMs);
        AppendMetric(builder, "GPU Busy", summary.GpuBusyMs);
        AppendMetric(builder, "GPU Time", summary.GpuTimeMs);
        AppendMetric(builder, "Display Latency", summary.DisplayLatencyMs);
        if (summary.DisplayedTimeColumnPresent)
        {
            builder.AppendLine($"Not Displayed: {summary.NotDisplayedFrameCount}/{summary.SampleCount} ({summary.NotDisplayedFramePercent:0.##}%)");
        }

        if (summary.DisplayChangeUnavailableCount > 0)
        {
            builder.AppendLine($"Display Change Unavailable: {summary.DisplayChangeUnavailableCount}/{summary.SampleCount} ({summary.DisplayChangeUnavailablePercent:0.##}%)");
        }

        AppendAppCorrelation(builder, summary.AppCorrelation);
        AppendCounts(builder, "Present Modes", summary.PresentModes);
        AppendCounts(builder, "Present Runtimes", summary.PresentRuntimes);
        AppendCounts(builder, "Sync Intervals", summary.SyncIntervals);
        AppendCounts(builder, "Allows Tearing", summary.AllowsTearing);
        AppendSwapChains(builder, summary.SwapChains);
        return builder.ToString().TrimEnd();
    }

    private static void AppendSummaryContext(StringBuilder builder, PresentMonCaptureSummary summary)
    {
        if (!string.IsNullOrWhiteSpace(summary.ExpectedSwapChainAddress))
        {
            builder.AppendLine();
            builder.AppendLine($"Expected Swap Chain: {summary.ExpectedSwapChainAddress} matched={summary.ExpectedSwapChainMatched}");
        }

        if (!string.IsNullOrWhiteSpace(summary.SelectedSwapChainAddress))
        {
            builder.AppendLine($"Selected Swap Chain: {summary.SelectedSwapChainAddress} ({summary.SampleCount}/{summary.RawSampleCount} rows, excluded={summary.ExcludedSampleCount})");
        }

        foreach (var warning in summary.Warnings)
        {
            builder.AppendLine($"Warning: {warning}");
        }
    }

    private static void AppendMetric(StringBuilder builder, string label, PresentMonMetricSummary metric)
    {
        if (metric.SampleCount <= 0)
        {
            return;
        }

        builder.AppendLine(
            $"{label}: avg={metric.Average:0.###}ms p50={metric.P50:0.###}ms p95={metric.P95:0.###}ms p99={metric.P99:0.###}ms max={metric.Max:0.###}ms n={metric.SampleCount}");
    }

    private static void AppendAppCorrelation(StringBuilder builder, PresentMonAppCorrelation correlation)
    {
        if (!correlation.Available)
        {
            return;
        }

        builder.AppendLine(
            $"App Correlation: appPresent={correlation.AppPresentId} sourceSeq={correlation.AppSourceSequenceNumber} " +
            $"row={correlation.PresentMonRowIndex} delta={correlation.DeltaMs:0.###}ms outcome={correlation.Outcome} " +
            $"mode={correlation.PresentMode} untilDisplayed={FormatOptionalMs(correlation.UntilDisplayedMs)} displayLatency={FormatOptionalMs(correlation.DisplayLatencyMs)}");
    }

    private static string FormatOptionalMs(double? value)
        => value.HasValue ? $"{value.Value:0.###}ms" : "N/A";

    private static void AppendCounts(StringBuilder builder, string label, IReadOnlyDictionary<string, int> counts)
    {
        if (counts.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{label}: {string.Join(", ", counts.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key}={pair.Value}"))}");
    }

    private static void AppendSwapChains(StringBuilder builder, IReadOnlyList<PresentMonSwapChainSummary> swapChains)
    {
        if (swapChains.Count <= 1)
        {
            return;
        }

        builder.AppendLine("Swap Chains:");
        foreach (var swapChain in swapChains.OrderByDescending(item => item.Selected).ThenByDescending(item => item.SampleCount))
        {
            var marker = swapChain.Selected ? "*" : " ";
            var artifact = swapChain.Artifact ? " artifact" : string.Empty;
            builder.AppendLine(
                $"  {marker} {swapChain.Address}: rows={swapChain.SampleCount}{artifact} " +
                $"present_p95={swapChain.BetweenPresentsMs.P95:0.###}ms display_p95={swapChain.BetweenDisplayChangeMs.P95:0.###}ms");
        }
    }
}

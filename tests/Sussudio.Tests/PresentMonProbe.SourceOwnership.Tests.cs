using System.Threading.Tasks;

static partial class Program
{
    internal static Task PresentMonProbe_SourceOwnership_IsSplit()
    {
        static string ReadPresentMonProbeFile(string fileName)
            => ReadRepoFile($"tools/Common/PresentMon/{fileName}").Replace("\r\n", "\n");

        var rootText = ReadPresentMonProbeFile("PresentMonProbe.cs");
        var modelsText = ReadPresentMonProbeFile("PresentMonProbe.Models.cs");
        var formatText = ReadPresentMonProbeFile("PresentMonProbe.Format.cs");
        var csvText = ReadPresentMonProbeFile("PresentMonProbe.Csv.cs");
        var pathsText = ReadPresentMonProbeFile("PresentMonProbe.Paths.cs");
        var processText = ReadPresentMonProbeFile("PresentMonProbe.Process.cs");

        AssertContains(rootText, "public static async Task<PresentMonProbeResult> RunAsync(");
        AssertContains(rootText, "var targetProcess = ResolveTargetProcess(options);");
        AssertContains(rootText, "var presentMonPath = ResolvePresentMonPath(options.PresentMonPath);");
        AssertContains(rootText, "var outputPath = ResolveOutputPath(options.OutputFile);");
        AssertContains(rootText, "var arguments = BuildArguments(");
        AssertContains(rootText, "private static string BuildArguments(");
        AssertContains(rootText, "private static string QuoteArgument(");
        AssertContains(rootText, "private static string BuildResultMessage(");
        AssertContains(rootText, "Captured {summary.RawSampleCount} PresentMon frame rows");
        AssertContains(rootText, "expected swap chain {summary.ExpectedSwapChainAddress} was not present");
        AssertContains(rootText, "PresentMon capture did not produce frame rows.");
        AssertContains(rootText, "var run = await RunProcessAsync(");
        AssertContains(rootText, "summary = ParseCsv(outputPath, options.ExpectedSwapChainAddress, options, captureStartUtcUnixMs);");
        AssertContains(rootText, "TryDelete(outputPath);");

        AssertContains(rootText, "public readonly record struct PresentMonProbeCorrelation(");
        AssertContains(rootText, "public static PresentMonProbeOptions CreateOptions(");
        AssertContains(rootText, "ExpectedSwapChainAddress = string.IsNullOrWhiteSpace(swapChainAddress)");
        AssertContains(rootText, "AppPresentId = appPresentId ?? correlation.PresentId");
        AssertContains(rootText, "public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)");
        AssertContains(rootText, "PreviewD3DSwapChainAddress");
        AssertContains(rootText, "PreviewD3DLastRenderedPreviewPresentId");
        AssertContains(rootText, "PreviewD3DLastRenderedSourceSequenceNumber");
        AssertContains(rootText, "PreviewD3DLastRenderedUtcUnixMs");
        AssertContains(rootText, "private static long? GetPositiveLong(");
        AssertContains(rootText, "private static long? GetNonNegativeLong(");

        AssertContains(modelsText, "public sealed class PresentMonProbeOptions");
        AssertContains(modelsText, "public sealed class PresentMonProbeResult");
        AssertContains(modelsText, "public sealed class PresentMonCaptureSummary");
        AssertContains(modelsText, "public sealed class PresentMonAppCorrelation");
        AssertContains(modelsText, "public sealed class PresentMonSwapChainSummary");
        AssertContains(modelsText, "public sealed class PresentMonMetricSummary");

        AssertContains(formatText, "public static string Format(PresentMonProbeResult result)");
        AssertContains(formatText, "private static void AppendSummaryContext(");
        AssertContains(formatText, "private static void AppendMetric(");
        AssertContains(formatText, "private static void AppendAppCorrelation(");
        AssertContains(formatText, "private static void AppendSwapChains(");
        AssertDoesNotContain(formatText, "private static string BuildResultMessage(");

        AssertContains(csvText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertContains(csvText, "var csvRows = ReadCsvRows(path);");
        AssertContains(csvText, "var rows = csvRows.Rows;");
        AssertContains(csvText, "var selectedRows = selectedSwapChain == null");
        AssertContains(csvText, "var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);");
        AssertContains(csvText, "var warnings = BuildWarnings(");
        AssertContains(csvText, "var appCorrelation = BuildAppCorrelation(");
        AssertContains(csvText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertContains(csvText, "private static string? NormalizeSwapChainAddress(");
        AssertContains(csvText, "private static string NormalizeHeader(");
        AssertContains(csvText, "private static double? ReadMetric(");
        AssertContains(csvText, "private static List<string> SplitCsvLine(");
        AssertContains(csvText, "private static PresentMonCsvRows ReadCsvRows(string path)");
        AssertContains(csvText, "private sealed record PresentMonCsvRows(");
        AssertContains(csvText, "private sealed record PresentMonRow(");
        AssertContains(csvText, "private static IReadOnlyDictionary<string, int> BuildCsvHeaderIndex(");
        AssertContains(csvText, "private static PresentMonRow ReadRow(");
        AssertContains(csvText, "rows.Add(ReadRow(rowIndex++, fields, index));");
        AssertContains(csvText, "private static bool HasAnyColumn(");
        AssertContains(csvText, "private static PresentMonAppCorrelation BuildAppCorrelation(");
        AssertContains(csvText, "private static string ClassifyPresentOutcome(");
        AssertContains(csvText, "private static IReadOnlyList<string> BuildWarnings(");
        AssertContains(csvText, "private static PresentMonMetricSummary Summarize(");
        AssertContains(csvText, "private static double Percentile(");

        AssertContains(pathsText, "private static Process? ResolveTargetProcess(");
        AssertContains(pathsText, "private static string? ResolvePresentMonPath(");
        AssertContains(pathsText, "private static string ResolveOutputPath(");
        AssertContains(processText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertContains(processText, "private static async Task<string> TryReadAsync(");
        AssertContains(processText, "private static void TryKill(");
        AssertContains(processText, "private static void TryDelete(");
        AssertContains(processText, "private sealed class ProcessRun");

        AssertDoesNotContain(rootText, "private static Process? ResolveTargetProcess(");
        AssertDoesNotContain(rootText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertDoesNotContain(rootText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertDoesNotContain(rootText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertDoesNotContain(rootText, "private static PresentMonMetricSummary Summarize(");
        AssertDoesNotContain(rootText, "public static string Format(PresentMonProbeResult result)");
        AssertDoesNotContain(modelsText, "PreviewD3DSwapChainAddress");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Rows.cs")),
            "PresentMon CSV row ingestion lives with PresentMonProbe.Csv.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Correlation.cs")),
            "PresentMon CSV app correlation lives with PresentMonProbe.Csv.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Summary.cs")),
            "PresentMon CSV warnings and percentile summaries live with PresentMonProbe.Csv.cs");

        return Task.CompletedTask;
    }
}

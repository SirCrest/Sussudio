using System.Threading.Tasks;

static partial class Program
{
    private static Task PresentMonProbe_SourceOwnership_IsSplit()
    {
        var rootText = ReadRepoFile("tools/Common/PresentMonProbe.cs").Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("tools/Common/PresentMonProbe.Models.cs").Replace("\r\n", "\n");
        var formatText = ReadRepoFile("tools/Common/PresentMonProbe.Format.cs").Replace("\r\n", "\n");
        var resultMessageText = ReadRepoFile("tools/Common/PresentMonProbe.ResultMessage.cs").Replace("\r\n", "\n");
        var csvText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.cs").Replace("\r\n", "\n");
        var csvRowsText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.Rows.cs").Replace("\r\n", "\n");
        var fieldsText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.Fields.cs").Replace("\r\n", "\n");
        var swapChainsText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.SwapChains.cs").Replace("\r\n", "\n");
        var correlationText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.Correlation.cs").Replace("\r\n", "\n");
        var summaryText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.Summary.cs").Replace("\r\n", "\n");
        var csvModelsText = ReadRepoFile("tools/Common/PresentMonProbe.Csv.Models.cs").Replace("\r\n", "\n");
        var pathsText = ReadRepoFile("tools/Common/PresentMonProbe.Paths.cs").Replace("\r\n", "\n");
        var argumentsText = ReadRepoFile("tools/Common/PresentMonProbe.Arguments.cs").Replace("\r\n", "\n");
        var processText = ReadRepoFile("tools/Common/PresentMonProbe.Process.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "public static async Task<PresentMonProbeResult> RunAsync(");
        AssertContains(rootText, "var targetProcess = ResolveTargetProcess(options);");
        AssertContains(rootText, "var presentMonPath = ResolvePresentMonPath(options.PresentMonPath);");
        AssertContains(rootText, "var outputPath = ResolveOutputPath(options.OutputFile);");
        AssertContains(rootText, "var arguments = BuildArguments(");
        AssertContains(rootText, "var run = await RunProcessAsync(");
        AssertContains(rootText, "summary = ParseCsv(outputPath, options.ExpectedSwapChainAddress, options, captureStartUtcUnixMs);");
        AssertContains(rootText, "TryDelete(outputPath);");

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

        AssertContains(resultMessageText, "private static string BuildResultMessage(");
        AssertContains(resultMessageText, "Captured {summary.RawSampleCount} PresentMon frame rows");
        AssertContains(resultMessageText, "expected swap chain {summary.ExpectedSwapChainAddress} was not present");
        AssertContains(resultMessageText, "PresentMon capture did not produce frame rows.");

        AssertContains(csvText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertContains(csvText, "var csvRows = ReadCsvRows(path);");
        AssertContains(csvText, "var rows = csvRows.Rows;");
        AssertContains(csvText, "var selectedRows = selectedSwapChain == null");
        AssertContains(csvText, "var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);");
        AssertContains(csvText, "var warnings = BuildWarnings(");
        AssertContains(csvText, "var appCorrelation = BuildAppCorrelation(");
        AssertContains(csvRowsText, "private static PresentMonCsvRows ReadCsvRows(string path)");
        AssertContains(csvRowsText, "private static IReadOnlyDictionary<string, int> BuildCsvHeaderIndex(");
        AssertContains(csvRowsText, "private static PresentMonRow ReadRow(");
        AssertContains(csvRowsText, "rows.Add(ReadRow(rowIndex++, fields, index));");
        AssertContains(csvRowsText, "private static bool HasAnyColumn(");
        AssertContains(fieldsText, "private static string NormalizeHeader(");
        AssertContains(fieldsText, "private static double? ReadMetric(");
        AssertContains(fieldsText, "private static List<string> SplitCsvLine(");
        AssertContains(swapChainsText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertContains(swapChainsText, "private static string? NormalizeSwapChainAddress(");
        AssertContains(correlationText, "private static PresentMonAppCorrelation BuildAppCorrelation(");
        AssertContains(correlationText, "private static string ClassifyPresentOutcome(");
        AssertContains(summaryText, "private static IReadOnlyList<string> BuildWarnings(");
        AssertContains(summaryText, "private static PresentMonMetricSummary Summarize(");
        AssertContains(summaryText, "private static double Percentile(");
        AssertContains(csvModelsText, "private sealed record PresentMonCsvRows(");
        AssertContains(csvModelsText, "private sealed record PresentMonRow(");
        AssertDoesNotContain(csvText, "new StreamReader(path)");
        AssertDoesNotContain(csvText, "new PresentMonRow(");
        AssertDoesNotContain(fieldsText, "private static bool HasAnyColumn(");

        AssertContains(pathsText, "private static Process? ResolveTargetProcess(");
        AssertContains(pathsText, "private static string? ResolvePresentMonPath(");
        AssertContains(pathsText, "private static string ResolveOutputPath(");
        AssertContains(argumentsText, "private static string BuildArguments(");
        AssertContains(argumentsText, "private static string QuoteArgument(");
        AssertContains(processText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertContains(processText, "private static async Task<string> TryReadAsync(");
        AssertContains(processText, "private static void TryKill(");
        AssertContains(processText, "private static void TryDelete(");
        AssertContains(processText, "private sealed class ProcessRun");

        AssertDoesNotContain(rootText, "private static Process? ResolveTargetProcess(");
        AssertDoesNotContain(rootText, "private static string BuildArguments(");
        AssertDoesNotContain(rootText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertDoesNotContain(rootText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertDoesNotContain(rootText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertDoesNotContain(rootText, "private static PresentMonMetricSummary Summarize(");
        AssertDoesNotContain(rootText, "public static string Format(PresentMonProbeResult result)");
        AssertDoesNotContain(rootText, "private static string BuildResultMessage(");

        return Task.CompletedTask;
    }
}

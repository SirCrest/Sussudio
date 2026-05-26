using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PresentMonProbe_SourceOwnership_IsSplit()
    {
        static string ReadPresentMonProbeFile(string fileName)
            => ReadRepoFile($"tools/Common/PresentMon/{fileName}").Replace("\r\n", "\n");

        var rootText = ReadPresentMonProbeFile("PresentMonProbe.cs");
        var formatText = rootText;
        var csvText = ReadPresentMonProbeFile("PresentMonProbe.Csv.cs");

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

        AssertContains(rootText, "public sealed class PresentMonProbeOptions");
        AssertContains(rootText, "public sealed class PresentMonProbeResult");
        AssertContains(rootText, "public sealed class PresentMonCaptureSummary");
        AssertContains(rootText, "public sealed class PresentMonAppCorrelation");
        AssertContains(rootText, "public sealed class PresentMonSwapChainSummary");
        AssertContains(rootText, "public sealed class PresentMonMetricSummary");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Models.cs")),
            "PresentMon public DTOs live with PresentMonProbe.RunAsync and result formatting");

        AssertContains(formatText, "public static string Format(PresentMonProbeResult result)");
        AssertContains(formatText, "private static void AppendSummaryContext(");
        AssertContains(formatText, "private static void AppendMetric(");
        AssertContains(formatText, "private static void AppendAppCorrelation(");
        AssertContains(formatText, "private static void AppendSwapChains(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Format.cs")),
            "PresentMon result formatting lives with PresentMonProbe.RunAsync");

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

        AssertContains(rootText, "private static Process? ResolveTargetProcess(");
        AssertContains(rootText, "private static string? ResolvePresentMonPath(");
        AssertContains(rootText, "private static string ResolveOutputPath(");
        AssertContains(rootText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertContains(rootText, "private static async Task<string> TryReadAsync(");
        AssertContains(rootText, "private static void TryKill(");
        AssertContains(rootText, "private static void TryDelete(");
        AssertContains(rootText, "private sealed class ProcessRun");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Paths.cs")),
            "PresentMon path resolution lives with PresentMonProbe.RunAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Process.cs")),
            "PresentMon process supervision lives with PresentMonProbe.RunAsync");

        AssertDoesNotContain(rootText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertDoesNotContain(rootText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertDoesNotContain(rootText, "private static PresentMonMetricSummary Summarize(");
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

    internal static Task PresentMonParser_SelectsDominantNonArtifactSwapChain()
    {
        var toolAssembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var probeType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbe")
            ?? throw new InvalidOperationException("Sussudio.Tools.PresentMonProbe type not found.");
        var parseCsv = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string) not found.");
        var parseCsvWithExpectedSwapChain = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string,string) not found.");
        var optionsType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbeOptions")
            ?? throw new InvalidOperationException("PresentMonProbeOptions type not found.");
        var parseCsvWithCorrelation = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string), optionsType, typeof(long?) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv correlation overload not found.");

        var csvPath = Path.Combine(Path.GetTempPath(), $"presentmon_parser_{Guid.NewGuid():N}.csv");
        File.WriteAllText(
            csvPath,
            """
            Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,0.0000,8.3333,8.3333,NA,16.0000,0.0700,8.2500,2.0000,7.0000,NA
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,8.3333,8.3334,8.3334,NA,16.1000,0.0710,8.2600,2.1000,7.1000,NA
            Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
            """);

        try
        {
            var summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null.");

            AssertEqual(2, GetIntProperty(summary, "SampleCount"), "selected PresentMon sample count");
            AssertEqual(3, GetIntProperty(summary, "RawSampleCount"), "raw PresentMon sample count");
            AssertEqual(1, GetIntProperty(summary, "ExcludedSampleCount"), "excluded PresentMon sample count");
            AssertEqual("0xABC", GetStringProperty(summary, "SelectedSwapChainAddress"), "selected PresentMon swap chain");

            var betweenPresents = GetPropertyValue(summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("BetweenPresentsMs was null.");
            AssertNearlyEqual(8.33335, GetDoubleProperty(betweenPresents, "Average"), 0.0001, "selected PresentMon average");
            AssertNearlyEqual(8.3334, GetDoubleProperty(betweenPresents, "Max"), 0.0001, "selected PresentMon max");

            var swapChains = GetPropertyValue(summary, "SwapChains")
                ?? throw new InvalidOperationException("SwapChains was null.");
            AssertEqual(2, GetCountProperty(swapChains), "PresentMon swap chain summary count");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0xAAA,DXGI,0,0,0,Composed: Flip,0.0000,99.0000,99.0000,8.3333,16.0000,0.0700,8.2500,2.0000,7.0000,20.0000
                Sussudio.exe,1234,0x0000000000000BBB,DXGI,0,0,0,Composed: Flip,8.3333,8.3333,8.3333,8.3333,16.1000,0.0710,8.2600,2.1000,7.1000,20.1000
                """);

            var expectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xbbb" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for expected swap-chain CSV.");
            AssertEqual("0xBBB", GetStringProperty(expectedSwapChainSummary, "SelectedSwapChainAddress"), "expected PresentMon selected swap chain");
            AssertEqual(true, GetBoolProperty(expectedSwapChainSummary, "ExpectedSwapChainMatched"), "expected PresentMon swap chain matched");
            var expectedBetweenPresents = GetPropertyValue(expectedSwapChainSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("expected BetweenPresentsMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(expectedBetweenPresents, "Average"), 0.0001, "expected swap-chain PresentMon average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,GPUTime,DisplayedTime,MsUntilDisplayed,DisplayLatency
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,90.0000,8.3333,8.2000,6.0000,8.3333,6.0000,12.0000
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,104.0000,8.3333,8.2000,6.0000,NA,20.0000,18.0000
                """);
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("Failed to create PresentMonProbeOptions.");
            SetPropertyOrBackingField(options, "AppPresentId", 42L);
            SetPropertyOrBackingField(options, "AppSourceSequenceNumber", 1001L);
            SetPropertyOrBackingField(options, "AppPresentUtcUnixMs", 1105L);
            SetPropertyOrBackingField(options, "CaptureStartUtcUnixMs", 1000L);
            var correlatedSummary = parseCsvWithCorrelation.Invoke(null, new object?[] { csvPath, "0xBBB", options, 1000L })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for correlated CSV.");
            var appCorrelation = GetPropertyValue(correlatedSummary, "AppCorrelation")
                ?? throw new InvalidOperationException("AppCorrelation was null.");
            AssertEqual(true, GetBoolProperty(appCorrelation, "Available"), "PresentMon app correlation available");
            AssertEqual(42L, GetLongProperty(appCorrelation, "AppPresentId"), "PresentMon app present id");
            AssertEqual(1001L, GetLongProperty(appCorrelation, "AppSourceSequenceNumber"), "PresentMon app source sequence");
            AssertEqual(1, GetIntProperty(appCorrelation, "PresentMonRowIndex"), "PresentMon correlated row index");
            AssertNearlyEqual(1.0, GetDoubleProperty(appCorrelation, "DeltaMs"), 0.0001, "PresentMon app correlation delta");
            AssertEqual("SupersededOrNotDisplayed", GetStringProperty(appCorrelation, "Outcome"), "PresentMon app correlation outcome");

            var missingExpectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xCCC" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for missing expected swap-chain CSV.");
            AssertEqual(0, GetIntProperty(missingExpectedSwapChainSummary, "SampleCount"), "missing expected PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "RawSampleCount"), "missing expected raw PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "ExcludedSampleCount"), "missing expected excluded PresentMon sample count");
            AssertEqual("0xCCC", GetStringProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainAddress"), "missing expected PresentMon swap chain");
            AssertEqual(false, GetBoolProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainMatched"), "missing expected PresentMon swap chain matched");
            AssertEqual(string.Empty, GetStringProperty(missingExpectedSwapChainSummary, "SelectedSwapChainAddress"), "missing expected selected PresentMon swap chain");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,CPUWait,GPULatency,GPUTime,GPUBusy,GPUWait,VideoBusy,DisplayLatency,DisplayedTime
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,0.0000,9.0000,8.9000,0.1000,3.0000,6.0000,2.0000,4.0000,7.0000,22.0000,8.3333
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,9.0000,7.6666,7.5000,0.1666,3.0000,6.5000,2.5000,4.0000,7.0000,22.5000,8.3334
                """);

            var v2Summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for v2 CSV.");
            var v2BetweenPresents = GetPropertyValue(v2Summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("v2 BetweenPresentsMs was null.");
            var v2CpuBusy = GetPropertyValue(v2Summary, "CpuBusyMs")
                ?? throw new InvalidOperationException("v2 CpuBusyMs was null.");
            var v2GpuBusy = GetPropertyValue(v2Summary, "GpuBusyMs")
                ?? throw new InvalidOperationException("v2 GpuBusyMs was null.");
            var v2GpuTime = GetPropertyValue(v2Summary, "GpuTimeMs")
                ?? throw new InvalidOperationException("v2 GpuTimeMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(v2BetweenPresents, "Average"), 0.0001, "v2 PresentMon frame time average");
            AssertNearlyEqual(8.2, GetDoubleProperty(v2CpuBusy, "Average"), 0.0001, "v2 PresentMon CPU busy average");
            AssertNearlyEqual(2.25, GetDoubleProperty(v2GpuBusy, "Average"), 0.0001, "v2 PresentMon GPU busy average");
            AssertNearlyEqual(6.25, GetDoubleProperty(v2GpuTime, "Average"), 0.0001, "v2 PresentMon GPU time average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
                """);

            var artifactOnlySummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for artifact-only CSV.");
            AssertEqual(0, GetIntProperty(artifactOnlySummary, "SampleCount"), "artifact-only selected sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "RawSampleCount"), "artifact-only raw sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "ExcludedSampleCount"), "artifact-only excluded sample count");
            AssertEqual(string.Empty, GetStringProperty(artifactOnlySummary, "SelectedSwapChainAddress"), "artifact-only selected swap chain");

            File.WriteAllText(csvPath, "   \r\n");
            var emptyHeaderSummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for empty-header CSV.");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "SampleCount"), "empty-header selected sample count");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "RawSampleCount"), "empty-header raw sample count");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "ExcludedSampleCount"), "empty-header excluded sample count");
            AssertEqual(false, GetBoolProperty(emptyHeaderSummary, "DisplayedTimeColumnPresent"), "empty-header displayed-time column presence");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,AllowsTearing,PresentMode,MsBetweenPresents,MsBetweenPresents,DisplayedTime,MsBetweenDisplayChange
                Sussudio.exe,1234,0xDAD,DXGI,0,0,Composed: Flip,7.0000,99.0000,7.0000,7.0000
                """);
            var duplicateHeaderSummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for duplicate-header CSV.");
            var duplicateHeaderBetweenPresents = GetPropertyValue(duplicateHeaderSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("duplicate-header BetweenPresentsMs was null.");
            AssertEqual(1, GetIntProperty(duplicateHeaderSummary, "RawSampleCount"), "duplicate-header raw sample count");
            AssertEqual("0xDAD", GetStringProperty(duplicateHeaderSummary, "SelectedSwapChainAddress"), "duplicate-header selected swap chain");
            AssertNearlyEqual(7.0, GetDoubleProperty(duplicateHeaderBetweenPresents, "Average"), 0.0001, "duplicate header uses first metric occurrence");
        }
        finally
        {
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }

        return Task.CompletedTask;
    }
}

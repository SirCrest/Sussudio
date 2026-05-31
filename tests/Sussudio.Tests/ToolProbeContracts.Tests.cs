using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PresentMonProbe_SourceOwnership_IsUnified()
    {
        static string ReadPresentMonProbeFile(string fileName)
            => ReadRepoFile($"tools/Common/PresentMon/{fileName}").Replace("\r\n", "\n");

        var rootText = ReadPresentMonProbeFile("PresentMonProbe.cs");
        var formatText = rootText;
        var csvText = rootText;

        AssertContains(rootText, "public static class PresentMonProbe");
        AssertDoesNotContain(rootText, "partial class PresentMonProbe");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.cs")),
            "PresentMon CSV parsing and aggregation live with PresentMonProbe.RunAsync");

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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Rows.cs")),
            "PresentMon CSV row ingestion lives with PresentMonProbe.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Correlation.cs")),
            "PresentMon CSV app correlation lives with PresentMonProbe.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Summary.cs")),
            "PresentMon CSV warnings and percentile summaries live with PresentMonProbe.cs");

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

    internal static async Task SsctlPipeTransport_ExposesAdvancedAutomationCommandIds()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);

        // Verify PipeTransport exposes expected command routing.
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var sendCommandAsync = transportType.GetMethod(
                "SendCommandAsync",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(int?) },
                modifiers: null)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport.SendCommandAsync not found.");

        var pipeName = $"ssctl-pipe-transport-{Guid.NewGuid():N}";
        var transport = Activator.CreateInstance(transportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for transport test.");
        var request = await CapturePipeRequestAsync(
            pipeName,
            async () =>
            {
                var task = sendCommandAsync.Invoke(
                    transport,
                    new object?[]
                    {
                        "SetPreviewVolume",
                        new Dictionary<string, object?> { ["previewVolumePercent"] = 55.5 },
                        null
                    }) as Task
                    ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(34, request.GetProperty("command").GetInt32(), "PipeTransport SetPreviewVolume command id");
        AssertEqual(55.5, request.GetProperty("payload").GetProperty("previewVolumePercent").GetDouble(), "PipeTransport preview volume payload");

        JsonElement response = default;
        var responsePipeName = $"ssctl-pipe-response-{Guid.NewGuid():N}";
        var responseTransport = Activator.CreateInstance(transportType, responsePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for response test.");
        var responseRequests = await CapturePipeRequestsAsync(
                responsePipeName,
                expectedCount: 1,
                async () =>
                {
                    response = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            responseTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                _ => """
                     {
                       "Success": true,
                       "Message": "snapshot ready",
                       "Data": {
                         "value": 123
                       }
                     }
                     """)
            .ConfigureAwait(false);
        AssertEqual(1, responseRequests[0].GetProperty("command").GetInt32(), "PipeTransport GetSnapshot command id");
        AssertEqual("snapshot ready", response.GetProperty("Message").GetString(), "PipeTransport parsed response message");
        AssertEqual(123, response.GetProperty("Data").GetProperty("value").GetInt32(), "PipeTransport parsed response data");

        JsonElement retryResponse = default;
        var retryPipeName = $"ssctl-pipe-retry-{Guid.NewGuid():N}";
        var retryTransport = Activator.CreateInstance(transportType, retryPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for retry test.");
        var retryRequests = await CapturePipeRequestsAsync(
                retryPipeName,
                expectedCount: 2,
                async () =>
                {
                    retryResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            retryTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": false,
                        "Status": "not_ready",
                        "RetryAfterMs": 100,
                        "Message": "snapshot not ready"
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "snapshot ready after retry",
                        "Data": {
                          "attempt": 2
                        }
                      }
                      """)
            .ConfigureAwait(false);
        AssertEqual(1, retryRequests[0].GetProperty("command").GetInt32(), "PipeTransport retry first command id");
        AssertEqual(1, retryRequests[1].GetProperty("command").GetInt32(), "PipeTransport retry second command id");
        AssertEqual("snapshot ready after retry", retryResponse.GetProperty("Message").GetString(), "PipeTransport retry final message");
        AssertEqual(2, retryResponse.GetProperty("Data").GetProperty("attempt").GetInt32(), "PipeTransport retry final data");

        JsonElement invalidJsonResponse = default;
        var invalidPipeName = $"ssctl-pipe-invalid-{Guid.NewGuid():N}";
        var invalidTransport = Activator.CreateInstance(transportType, invalidPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for invalid JSON test.");
        var invalidRequest = await CapturePipeRequestWithRawResponseAsync(
                invalidPipeName,
                async () =>
                {
                    invalidJsonResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            invalidTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                "not-json")
            .ConfigureAwait(false);
        AssertEqual(1, invalidRequest.GetProperty("command").GetInt32(), "PipeTransport invalid JSON request command id");
        AssertEqual(false, invalidJsonResponse.GetProperty("Success").GetBoolean(), "PipeTransport invalid JSON response Success=false");
        AssertEqual("pipe-invalid-json", invalidJsonResponse.GetProperty("ErrorCode").GetString(), "PipeTransport invalid JSON response ErrorCode");
        var invalidJsonMessage = invalidJsonResponse.GetProperty("Message").GetString() ?? "";
        AssertEqual(
            true,
            invalidJsonMessage.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase) || invalidJsonMessage.Contains("pipe-invalid-json", StringComparison.OrdinalIgnoreCase),
            $"PipeTransport invalid JSON response Message should mention invalid JSON, got: {invalidJsonMessage}");

        var usageTransport = Activator.CreateInstance(transportType, $"ssctl-pipe-usage-{Guid.NewGuid():N}", (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for usage test.");
        Exception? usageException = null;
        try
        {
            await InvokePipeTransportSendCommandAsync(
                    sendCommandAsync,
                    usageTransport,
                    "DefinitelyNotACommand",
                    null,
                    null)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            usageException = ex;
        }

        AssertEqual("Sussudio.Tools.Ssctl.UsageException", usageException?.GetType().FullName, "PipeTransport unknown command exception type");
    }

    internal static Task KsAudioNodeProbe_SourceOwnership_IsConsolidated()
    {
        var programText = ReadRepoFile("tools/KsAudioNodeProbe/Program.cs");
        var scanWorkflowsText = ReadRepoFile("tools/KsAudioNodeProbe/Program.ScanWorkflows.cs");

        AssertContains(programText, "using static KsAudioNodeProbeNative;");
        AssertContains(programText, "KsAudioNodeProbeScanWorkflows.RunSetAndHold(handle)");
        AssertContains(programText, "KsAudioNodeProbeScanWorkflows.RunFullProbe(handle)");
        AssertContains(programText, "static class KsAudioNodeProbeNative");
        AssertContains(programText, "private const uint IoctlKsProperty = 0x002F0003;");
        AssertContains(programText, "private const int ErrorMoreData = 234;");
        AssertContains(programText, "public static List<string> EnumerateKsInterfaces");
        AssertContains(programText, "private static extern bool DeviceIoControl");
        AssertContains(programText, "private struct KsProperty");
        AssertContains(programText, "private struct SP_DEVICE_INTERFACE_DETAIL_DATA");
        AssertDoesNotContain(programText, "var anyHit = false");
        AssertDoesNotContain(programText, "== Extended node tests ==");
        AssertDoesNotContain(programText, "== ADC volume probe ==");
        AssertContains(scanWorkflowsText, "static class KsAudioNodeProbeScanWorkflows");
        AssertDoesNotContain(scanWorkflowsText, "static partial class KsAudioNodeProbeScanWorkflows");
        AssertContains(scanWorkflowsText, "public static int RunSetAndHold(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "public static void RunFullProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void EnumerateTopologyNodes(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunBruteForceNodePropertyScan(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunExtendedNodeTests(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunExtendedSetTest(");
        AssertContains(scanWorkflowsText, "private static void RunAdcVolumeProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunMuxProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunMuteProbe(SafeFileHandle handle)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "KsAudioNodeProbe", "Program.ScanWorkflows.Extended.cs")),
            "KS audio node scan workflow probes live with the main scan workflow owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "KsAudioNodeProbe", "Program.NativeInterop.cs")),
            "KS audio node probe private interop declarations live with the command entry point");

        return Task.CompletedTask;
    }

    internal static Task EgavdsAudioProbe_SourceOwnership_IsConsolidated()
    {
        var programText = ReadRepoFile("tools/EgavdsAudioProbe/Program.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(programText, "static class EgavdsProbe");
        AssertDoesNotContain(programText, "static partial class EgavdsProbe");
        AssertContains(programText, "static string? FindElgato4KXDevicePath()");
        AssertContains(programText, "EGAVDS_SetAudioInputSelection(handleRef, targetInput)");
        AssertContains(programText, "EGAVDS_SetLineInAudioGain(handleRef, setGain.Value)");
        AssertContains(programText, "private const string DLL = \"EGAVDeviceSupport\"");
        AssertContains(programText, "private static void RegisterSwigCallbacks()");
        AssertContains(programText, "SWIGRegisterExceptionCallbacks_EGAVDS");
        AssertContains(programText, "private static extern int EGAVDS_OpenDevice");
        AssertContains(programText, "private static extern bool SetupDiEnumDeviceInterfaces");
        AssertContains(programText, "private struct SP_DEVICE_INTERFACE_DATA");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "EgavdsAudioProbe", "Program.NativeInterop.cs")),
            "EGAVDS probe private interop declarations live with the probe command flow");
        AssertContains(agentMapText, "`tools/EgavdsAudioProbe/Program.cs` owns EGAVDS audio probe command flow,");
        AssertDoesNotContain(agentMapText, "`Program.NativeInterop.cs` owns EGAVDS");
        AssertDoesNotContain(cleanupPlanText, "`tools/EgavdsAudioProbe/Program.NativeInterop.cs`");

        return Task.CompletedTask;
    }

    private static async Task<JsonElement> InvokePipeTransportSendCommandAsync(
        MethodInfo sendCommandAsync,
        object transport,
        string commandName,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
    {
        var task = sendCommandAsync.Invoke(
                transport,
                new object?[]
                {
                    commandName,
                    payload,
                    responseTimeoutMs
                }) as Task<JsonElement>
            ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return Task<JsonElement>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<JsonElement> CapturePipeRequestWithRawResponseAsync(
        string pipeName,
        Func<Task> clientAction,
        string rawResponseLine)
    {
        var clientTask = Task.Run(clientAction);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        string requestLine;
        {
            using var serverPipe = new System.IO.Pipes.NamedPipeServerStream(
                pipeName,
                System.IO.Pipes.PipeDirection.InOut,
                1,
                System.IO.Pipes.PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);

            var connectTask = serverPipe.WaitForConnectionAsync(cts.Token);
            if (await Task.WhenAny(connectTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe request, but the client completed before connecting.");
            }

            await connectTask.ConfigureAwait(false);
            using var reader = new StreamReader(serverPipe, leaveOpen: true);
            var readTask = reader.ReadLineAsync().WaitAsync(cts.Token);
            if (await Task.WhenAny(readTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe payload, but the client completed before sending one.");
            }

            try
            {
                requestLine = await readTask.ConfigureAwait(false)
                    ?? throw new InvalidOperationException("No request received on raw-response pipe.");
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("Timed out waiting for raw-response pipe payload.", ex);
            }

            using var writer = new StreamWriter(serverPipe, leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(rawResponseLine)
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
        }

        await EnsureNoUnexpectedPipeRequestAsync(pipeName, 1, 1, clientTask, cts.Token)
            .ConfigureAwait(false);

        using var document = JsonDocument.Parse(requestLine);
        return document.RootElement.Clone();
    }
}

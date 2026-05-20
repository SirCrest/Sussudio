using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpPresentMonTools_RouteSnapshotCorrelation()
    {
        var presentMonTools = RequireMcpType("McpServer.Tools.PresentMonTools");

        var pipeName = NewMcpToolPipeName("presentmon-text");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string textResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    textResult = await InvokeMcpToolStringAsync(
                            presentMonTools,
                            "capture_presentmon",
                            pipeClient,
                            5,
                            -1,
                            "NoSuchSussudioProcess",
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            false,
                            true)
                        .ConfigureAwait(false);
                },
                _ => PresentMonSnapshotJson("0xABCDEF", 42, 0, 1700000000000))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertEqual("No running process matched pid=-1 name='NoSuchSussudioProcess'.", textResult, "capture_presentmon no-process text");

        var rawPipeName = NewMcpToolPipeName("presentmon-raw");
        var rawPipeClient = CreateMcpPipeClient(rawPipeName);
        object? rawResult = null;
        var rawRequests = await CapturePipeRequestsAsync(
                rawPipeName,
                expectedCount: 1,
                async () =>
                {
                    rawResult = await InvokeMcpToolResultAsync(
                            presentMonTools,
                            "capture_presentmon_raw",
                            rawPipeClient,
                            15,
                            -1,
                            "AnotherMissingProcess",
                            "0xEXPLICIT",
                            99L,
                            1001L,
                            1700000000999L,
                            @"C:\tools\missing-presentmon.exe",
                            @"C:\captures\presentmon.csv",
                            true,
                            false)
                        .ConfigureAwait(false);
                },
                _ => PresentMonSnapshotJson("0xSHOULD_NOT_WIN", 12, 34, 1700000000123))
            .ConfigureAwait(false);

        AssertCommandRequest(rawRequests[0], "GetSnapshot");
        AssertEqual(false, GetBoolProperty(rawResult!, "Success"), "capture_presentmon_raw missing process success");
        AssertEqual("No running process matched pid=-1 name='AnotherMissingProcess'.", GetStringProperty(rawResult!, "Message"), "capture_presentmon_raw no-process message");

        AssertPresentMonOptionsFallbackAndPrecedence();

        var rootText = ReadRepoFile("tools/McpServer/Tools/PresentMonTools.cs")
            .Replace("\r\n", "\n");
        var correlationText = ReadRepoFile("tools/McpServer/Tools/PresentMonTools.Correlation.cs")
            .Replace("\r\n", "\n");
        var optionsText = ReadRepoFile("tools/Common/PresentMon/PresentMonProbe.Options.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "[McpServerToolType]");
        AssertContains(rootText, "public static partial class PresentMonTools");
        AssertContains(rootText, "public static async Task<CallToolResult> capture_presentmon");
        AssertContains(rootText, "public static async Task<object> capture_presentmon_raw");
        AssertContains(rootText, "[McpServerTool(UseStructuredContent = true)");
        AssertContains(rootText, "PresentMonProbe.Format(result)");
        AssertContains(rootText, "PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(");
        AssertContains(rootText, "correlation: resolved");
        AssertDoesNotContain(rootText, "new PresentMonProbeOptions");
        AssertDoesNotContain(rootText, "ExpectedSwapChainAddress =");
        AssertDoesNotContain(rootText, "AppPresentId = appPresentId");
        AssertDoesNotContain(rootText, "SendCommandAsync(\"GetSnapshot\")");
        AssertDoesNotContain(rootText, "GetPositiveLong(");

        AssertContains(correlationText, "private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(");
        AssertContains(correlationText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(correlationText, "return PresentMonProbe.ReadPreviewCorrelation(snapshot);");
        AssertContains(correlationText, "catch (JsonException ex)");
        AssertContains(correlationText, "catch (IOException ex)");
        AssertDoesNotContain(correlationText, "private readonly record struct PresentMonCorrelation(");
        AssertDoesNotContain(correlationText, "GetPositiveLong(");

        AssertContains(optionsText, "public readonly record struct PresentMonProbeCorrelation(");
        AssertContains(optionsText, "public static PresentMonProbeOptions CreateOptions(");
        AssertContains(optionsText, "public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)");
    }

    private static void AssertPresentMonOptionsFallbackAndPrecedence()
    {
        var presentMonProbe = RequireMcpType("Sussudio.Tools.PresentMonProbe");
        var createOptions = presentMonProbe.GetMethod("CreateOptions", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PresentMonProbe.CreateOptions was not found.");
        var correlationType = RequireMcpType("Sussudio.Tools.PresentMonProbeCorrelation");
        var resolved = Activator.CreateInstance(
                correlationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "0xSNAPSHOT", 42L, 0L, 1700000000000L },
                culture: null)
            ?? throw new InvalidOperationException("PresentMonCorrelation could not be created.");

        var fallbackOptions = createOptions.Invoke(null, new object?[]
        {
            5,
            123,
            "Sussudio",
            null,
            null,
            null,
            null,
            null,
            @"C:\tools\PresentMon.exe",
            @"C:\captures\presentmon.csv",
            true,
            false,
            resolved
        }) ?? throw new InvalidOperationException("CreatePresentMonProbeOptions returned null.");

        AssertEqual(5, GetIntProperty(fallbackOptions, "DurationSeconds"), "PresentMon fallback DurationSeconds");
        AssertEqual(123, GetIntProperty(fallbackOptions, "ProcessId"), "PresentMon fallback ProcessId");
        AssertEqual("Sussudio", GetStringProperty(fallbackOptions, "ProcessName"), "PresentMon fallback ProcessName");
        AssertEqual("0xSNAPSHOT", GetStringProperty(fallbackOptions, "ExpectedSwapChainAddress"), "PresentMon fallback swap chain");
        AssertEqual(42L, GetLongProperty(fallbackOptions, "AppPresentId"), "PresentMon fallback present id");
        AssertEqual(0L, GetLongProperty(fallbackOptions, "AppSourceSequenceNumber"), "PresentMon fallback source sequence");
        AssertEqual(1700000000000L, GetLongProperty(fallbackOptions, "AppPresentUtcUnixMs"), "PresentMon fallback present UTC");
        AssertEqual(@"C:\tools\PresentMon.exe", GetStringProperty(fallbackOptions, "PresentMonPath"), "PresentMon fallback path");
        AssertEqual(@"C:\captures\presentmon.csv", GetStringProperty(fallbackOptions, "OutputFile"), "PresentMon fallback output");
        AssertEqual(true, GetBoolProperty(fallbackOptions, "KeepCsv"), "PresentMon fallback keep CSV");
        AssertEqual(false, GetBoolProperty(fallbackOptions, "TrackGpuVideo"), "PresentMon fallback track GPU video");

        var explicitOptions = createOptions.Invoke(null, new object?[]
        {
            15,
            -1,
            "OtherProcess",
            "0xEXPLICIT",
            99L,
            1001L,
            1700000000999L,
            null,
            null,
            null,
            false,
            true,
            resolved
        }) ?? throw new InvalidOperationException("CreatePresentMonProbeOptions returned null for explicit args.");

        AssertEqual(15, GetIntProperty(explicitOptions, "DurationSeconds"), "PresentMon explicit DurationSeconds");
        AssertEqual(-1, GetIntProperty(explicitOptions, "ProcessId"), "PresentMon explicit ProcessId");
        AssertEqual("OtherProcess", GetStringProperty(explicitOptions, "ProcessName"), "PresentMon explicit ProcessName");
        AssertEqual("0xEXPLICIT", GetStringProperty(explicitOptions, "ExpectedSwapChainAddress"), "PresentMon explicit swap chain");
        AssertEqual(99L, GetLongProperty(explicitOptions, "AppPresentId"), "PresentMon explicit present id");
        AssertEqual(1001L, GetLongProperty(explicitOptions, "AppSourceSequenceNumber"), "PresentMon explicit source sequence");
        AssertEqual(1700000000999L, GetLongProperty(explicitOptions, "AppPresentUtcUnixMs"), "PresentMon explicit present UTC");
        AssertEqual(string.Empty, GetStringProperty(explicitOptions, "PresentMonPath"), "PresentMon explicit null path");
        AssertEqual(string.Empty, GetStringProperty(explicitOptions, "OutputFile"), "PresentMon explicit null output");
        AssertEqual(false, GetBoolProperty(explicitOptions, "KeepCsv"), "PresentMon explicit keep CSV");
        AssertEqual(true, GetBoolProperty(explicitOptions, "TrackGpuVideo"), "PresentMon explicit track GPU video");
    }

    private static string PresentMonSnapshotJson(
        string swapChainAddress,
        long presentId,
        long sourceSequenceNumber,
        long presentUtcUnixMs)
    {
        return $$"""
                 {
                   "Success": true,
                   "Snapshot": {
                     "PreviewD3DSwapChainAddress": "{{swapChainAddress}}",
                     "PreviewD3DLastRenderedPreviewPresentId": {{presentId}},
                     "PreviewD3DLastRenderedSourceSequenceNumber": {{sourceSequenceNumber}},
                     "PreviewD3DLastRenderedUtcUnixMs": {{presentUtcUnixMs}}
                   }
                 }
                 """;
    }
}

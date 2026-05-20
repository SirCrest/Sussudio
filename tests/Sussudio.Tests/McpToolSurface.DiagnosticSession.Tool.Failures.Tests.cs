using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session-failure");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-health-test-{Guid.NewGuid():N}");
        object? toolResult = null;
        var result = string.Empty;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Critical",
                                 "DiagnosticLikelyStage": "flashback_playback",
                                 "DiagnosticSummary": "Playback cadence collapsed.",
                                 "DiagnosticEvidence": "1pctLow=5fps target=120fps",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": []
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(true, GetMcpToolResultIsError(toolResult), "diagnostic session failure MCP isError");
            AssertContains(result, "== Diagnostic Session: FAIL ==");
            AssertContains(result, "diagnostic health degraded during session");
            AssertContains(result, "health=Critical");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic health failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic health failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic health failure summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "diagnostic health degraded during session");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("Critical", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic health failure live health");
            AssertEqual("flashback_playback", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic health failure live stage");
            AssertEqual(1, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic health failure live warning count");
            AssertContains(liveDocument.RootElement.GetProperty("LastWarning").GetString() ?? string.Empty, "health=Critical");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic health warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }
}

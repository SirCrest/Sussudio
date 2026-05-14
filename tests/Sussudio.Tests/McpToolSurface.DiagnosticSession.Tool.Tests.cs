using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpDiagnosticSessionTool_RecordsSnapshotArtifacts()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-test-{Guid.NewGuid():N}");
        var result = string.Empty;
        object? toolResult = null;

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
                                 "IsPreviewing": false,
                                 "IsRecording": false,
                                 "FlashbackActive": false,
                                 "DiagnosticHealthStatus": "Idle",
                                 "DiagnosticLikelyStage": "diagnostic_unavailable",
                                 "DiagnosticSummary": "Preview and recording are idle.",
                                 "DiagnosticEvidence": "Start preview or recording to collect live frame-lane diagnostics.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 4,
                                 "PreviewD3DFrameStatsFailureCount": 1,
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 7,
                                 "PreviewD3DFrameStatsFailureCount": 2,
                                 "PreviewD3DRecentSlowFrames": [
                                   {
                                     "SlowReason": "present_interval",
                                     "WorstOverBudgetMs": 1.5,
                                     "PresentIntervalMs": 9.8,
                                     "TotalFrameCpuMs": 4.2,
                                     "PresentCallMs": 0.7,
                                     "PendingFrameCount": 1
                                   }
                                 ],
                                 "FrameLedgerRecentEvents": [
                                   {
                                     "SourceSequence": 7,
                                     "Stage": "CaptureArrived",
                                     "QpcTimestamp": 123456,
                                     "Accepted": true
                                   }
                                 ]
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": [
                                 {
                                   "TimestampUtc": "2026-04-26T00:00:00Z",
                                   "PerformanceScore": 100
                                 }
                               ]
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
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
            AssertEqual(false, GetMcpToolResultIsError(toolResult), "diagnostic session success MCP isError");
            AssertContains(result, "== Diagnostic Session: PASS ==");
            AssertContains(result, "Health: Healthy | Stage: none");
            AssertContains(result, "Preview D3D Perf: onePercentLowFpsEnd=0 onePercentLowFpsMin=0 missedRefreshDelta=3 statsFailureDelta=1 maxRecentSlowFrames=1 latestSlowReason=present_interval overBudgetMs=1.5 presentIntervalMs=9.8 totalFrameCpuMs=4.2 presentCallMs=0.7 pending=1");
            AssertContains(result, "Frame Ledger:");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            var samplesPath = Path.Combine(outputDirectory, "samples.json");
            var frameLedgerPath = Path.Combine(outputDirectory, "frame-ledger.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic session summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic session live artifact");
            AssertEqual(true, File.Exists(samplesPath), "diagnostic session samples artifact");
            AssertEqual(true, File.Exists(frameLedgerPath), "diagnostic session frame ledger artifact");
            AssertContains(result, $"Live: {livePath}");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic session terminal state");
            AssertEqual("summary", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic session last stage");
            AssertEqual(true, summaryDocument.RootElement.GetProperty("RunnerProcessId").GetInt32() > 0, "diagnostic session runner pid");
            AssertEqual(livePath, summaryDocument.RootElement.GetProperty("LivePath").GetString(), "diagnostic session live path");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("completed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic live terminal state");
            AssertEqual("summary-written", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic live last stage");
            AssertEqual("Healthy", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic live health status");
            AssertEqual("none", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic live likely stage");
            AssertEqual(0, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic live warning count");
            AssertEqual(string.Empty, liveDocument.RootElement.GetProperty("LastWarning").GetString(), "diagnostic live last warning");

            using var frameLedgerDocument = JsonDocument.Parse(File.ReadAllText(frameLedgerPath));
            AssertEqual(1, frameLedgerDocument.RootElement.GetProperty("EventCount").GetInt32(), "diagnostic session frame ledger event count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static async Task McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError()
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

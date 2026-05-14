using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-connect-retry-test-{Guid.NewGuid():N}");
        var getSnapshotAttempts = 0;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory: outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotAttempts++;
                    if (getSnapshotAttempts <= 2)
                    {
                        return Task.FromResult(ParseDiagnosticSessionJson("""
                            {
                              "Success": false,
                              "Status": "error",
                              "CommandLifecycle": "failed",
                              "Message": "Sussudio is not running or not responding. Start the app and try again.",
                              "ErrorCode": "pipe-connect-failed"
                            }
                            """));
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(true, GetBoolProperty(result, "Success"), "diagnostic synthetic connect retry result success");
            AssertEqual(true, getSnapshotAttempts >= 3, "diagnostic synthetic connect failure was retried");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic synthetic connect retry summary success");
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic synthetic connect retry terminal state");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}

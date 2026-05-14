using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-connect-retry-test-{Guid.NewGuid():N}");
        var getSnapshotAttempts = 0;

        try
        {
            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "observe");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);

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

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
            var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");

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

        static JsonElement ParseDiagnosticSessionJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }
}

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-failure-test-{Guid.NewGuid():N}");
        var getSnapshotCount = 0;

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
                    getSnapshotCount++;
                    if (getSnapshotCount == 3)
                    {
                        throw new InvalidOperationException("simulated final snapshot failure");
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

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic failure result success");
            AssertEqual("failed", GetPropertyValue(result, "TerminalState") as string, "diagnostic failure terminal state");
            AssertEqual("final-snapshot", GetPropertyValue(result, "LastStage") as string, "diagnostic failure last stage");
            AssertContains(GetPropertyValue(result, "UnhandledException") as string ?? string.Empty, "InvalidOperationException");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic failure summary success");
            AssertEqual("failed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure summary terminal state");
            AssertEqual("final-snapshot", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure summary last stage");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "final-snapshot");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("failed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure live terminal state");
            AssertEqual("final-snapshot", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure live last stage");
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
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
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

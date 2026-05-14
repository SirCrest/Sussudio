using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-unknown-initial-test-{Guid.NewGuid():N}");
        var commands = new List<string>();

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                "preview-only",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory: outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                commands.Add(command);
                if (command is "SetPreviewEnabled" or "SetRecordingEnabled" or "SetFlashbackEnabled")
                {
                    throw new InvalidOperationException($"Unexpected state mutation command: {command}");
                }

                return Task.FromResult(ParseDiagnosticSessionJson(command == "GetPerformanceTimeline"
                    ? """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "ok"
                      }
                      """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic unknown initial result success");
            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "skipped state-mutating scenario");
            AssertEqual(false, commands.Contains("SetPreviewEnabled"), "diagnostic unknown initial did not start preview");
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

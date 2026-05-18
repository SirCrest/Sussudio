using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-export-playback-test-{Guid.NewGuid():N}");
        var requests = new List<(string Command, Dictionary<string, object?>? Payload)>();
        var getSnapshotCount = 0;
        var goLiveRequested = false;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "flashback-export-playback",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);
            options.GetType().GetProperty("LeaveRunning")!.SetValue(options, true);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, payload, _) =>
            {
                requests.Add((command, payload));
                if (command == "FlashbackAction" &&
                    string.Equals(GetPayloadString(payload, "action"), "go-live", StringComparison.OrdinalIgnoreCase))
                {
                    goLiveRequested = true;
                }

                return Task.FromResult(command switch
                {
                    "GetSnapshot" => CreateSnapshotResponse(++getSnapshotCount),
                    "GetPerformanceTimeline" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """),
                    "WaitForCondition" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "condition met"
                        }
                        """),
                    "FlashbackExport" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Exported 120 packets from 1 segments"
                        }
                        """),
                    "VerifyFile" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Strict verification passed.",
                          "Data": {
                            "Succeeded": true,
                            "Message": "Strict verification passed."
                          }
                        }
                        """),
                    _ => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "ok"
                        }
                        """)
                });
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            if (!GetBoolProperty(result, "Success"))
            {
                var warnings = GetPropertyValue(result, "Warnings") as System.Collections.IEnumerable;
                var warningText = warnings == null
                    ? string.Empty
                    : string.Join(" | ", warnings.Cast<object?>().Select(item => item?.ToString() ?? string.Empty));
                throw new InvalidOperationException($"Assertion failed for flashback export playback diagnostic success: warnings={warningText}");
            }

            AssertEqual(true, requests.Any(request => request.Command == "SetFlashbackEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback enabled Flashback");
            AssertEqual(true, requests.Any(request => request.Command == "SetPreviewEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback started preview");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "pause"), "flashback export playback pauses before seek");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "seek" && GetPayloadDouble(request.Payload, "positionMs") == 1000d), "flashback export playback seeks to 1000ms");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "play"), "flashback export playback starts playback");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackExport" && GetPayloadDouble(request.Payload, "seconds") == 1d), "flashback export playback exports one second");
            AssertEqual(true, requests.Any(request => request.Command == "VerifyFile" && GetPayloadString(request.Payload, "verificationProfile") == "flashback-export"), "flashback export playback verifies export");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "go-live"), "flashback export playback returns live");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "flashback export playback summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export during playback verified");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export playback go-live requested");
            AssertEqual(0, summaryDocument.RootElement.GetProperty("Warnings").GetArrayLength(), "flashback export playback summary warning count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        JsonElement CreateSnapshotResponse(int snapshotIndex)
        {
            if (snapshotIndex == 1)
            {
                return ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Snapshot": {
                        "IsPreviewing": false,
                        "IsRecording": false,
                        "FlashbackActive": false,
                        "FlashbackPlaybackState": "Live",
                        "DiagnosticHealthStatus": "Healthy",
                        "DiagnosticLikelyStage": "none",
                        "DiagnosticSummary": "No degraded frame lane detected.",
                        "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                        "FrameLedgerRecentEvents": []
                      }
                    }
                    """);
            }

            var playbackState = !goLiveRequested && snapshotIndex >= 5 ? "Playing" : "Live";
            var playbackFrames = snapshotIndex <= 4 ? 0 : snapshotIndex * 16;
            return ParseDiagnosticSessionJson($$"""
                {
                  "Success": true,
                  "Snapshot": {
                    "IsPreviewing": true,
                    "IsRecording": false,
                    "FlashbackActive": true,
                    "FlashbackBufferedDurationMs": 12000,
                    "FlashbackEncodedFrames": 360,
                    "FlashbackPlaybackState": "{{playbackState}}",
                    "FlashbackPlaybackFrameCount": {{playbackFrames}},
                    "FlashbackPlaybackPendingCommands": 0,
                    "FlashbackPlaybackCommandsDropped": 0,
                    "FlashbackPlaybackCommandsSkippedNotReady": 0,
                    "FlashbackPlaybackSubmitFailures": 0,
                    "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                    "FlashbackPlaybackSeekCommandsCoalesced": 0,
                    "FlashbackExportActive": false,
                    "FlashbackExportStatus": "Succeeded",
                    "FlashbackExportMessage": "Exported 120 packets from 1 segments",
                    "FlashbackExportOutputPath": "flashback-export-playback.mp4",
                    "ExpectedCaptureFrameRate": 120,
                    "SelectedExactFrameRate": 120,
                    "PreviewCadenceObservedFps": 120,
                    "VisualCadenceChangeFps": 120,
                    "VisualCadenceRepeatPercent": 0,
                    "DiagnosticHealthStatus": "Healthy",
                    "DiagnosticLikelyStage": "none",
                    "DiagnosticSummary": "No degraded frame lane detected.",
                    "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                    "FrameLedgerRecentEvents": []
                  }
                }
                """);
        }

        static string? GetPayloadString(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) ? value?.ToString() : null;

        static bool? GetPayloadBool(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is bool boolValue ? boolValue : null;

        static double? GetPayloadDouble(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is IConvertible convertible
                ? convertible.ToDouble(CultureInfo.InvariantCulture)
                : null;

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "flashback export playback action array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected array to contain '{token}'.");
        }
    }
}

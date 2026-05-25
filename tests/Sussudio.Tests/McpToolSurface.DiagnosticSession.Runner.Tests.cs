using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Assembly LoadDiagnosticSessionRunnerAssembly()
    {
        return LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
    }

    private static object CreateDiagnosticSessionOptions(
        Assembly assembly,
        string scenario,
        int durationSeconds,
        int sampleIntervalMs,
        string outputDirectory)
    {
        var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
            ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
        var options = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");

        optionsType.GetProperty("Scenario")!.SetValue(options, scenario);
        optionsType.GetProperty("DurationSeconds")!.SetValue(options, durationSeconds);
        optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, sampleIntervalMs);
        optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);
        return options;
    }

    private static async Task<object> RunDiagnosticSessionRunnerAsync(
        Assembly assembly,
        object options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand,
        CancellationToken cancellationToken = default)
    {
        var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
            ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
        var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
        var task = runAsync.Invoke(null, new object?[] { options, sendCommand, cancellationToken }) as Task
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");

        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");
    }

    private static JsonElement ParseDiagnosticSessionJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    internal static async Task DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState()
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

    internal static Task DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var observationType = assembly.GetType("Sussudio.Tools.DiagnosticHealthObservation")
            ?? throw new InvalidOperationException("DiagnosticHealthObservation type was not found.");
        var sourceMetricsType = assembly.GetType("Sussudio.Tools.SourceCadenceSessionMetrics")
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics type was not found.");
        var sparseSourceWarning = healthPolicyType.GetMethod(
                "IsSparseSourceCaptureCadenceWarningRun",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sparse source-cadence classifier was not found.");

        var observation = Activator.CreateInstance(
                observationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "Warning", "source_capture", "source gaps=1 drops=1", 85_727L, 2 },
                culture: null)
            ?? throw new InvalidOperationException("DiagnosticHealthObservation instance could not be created.");
        var metrics = Activator.CreateInstance(sourceMetricsType, nonPublic: true)
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics instance could not be created.");
        sourceMetricsType.GetProperty("MaxSevereGapCountObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxDropPercentObserved")!.SetValue(metrics, 0.042);

        AssertEqual(
            true,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "sparse source cadence warning without source counter deltas");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 1L, 0L, 300, true })!,
            "source reader drop delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 1L, 300, true })!,
            "video ingest error delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, false })!,
            "unhealthy visual cadence blocks sparse source cadence tolerance");

        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 3L);
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "repeated source cadence drops block sparse source cadence tolerance");

        return Task.CompletedTask;
    }

    internal static async Task DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures()
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

    internal static async Task DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-concurrent-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");

        // Simulate a concurrent in-flight diagnostic session by holding the same exclusive
        // lock file the runner uses. A second RunAsync against this OutputDirectory must
        // fail fast with InvalidOperationException rather than corrupt the artifact set.
        FileStream? holderLock = null;
        try
        {
            holderLock = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (_, _, _) =>
                Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "should-not-be-called"
                    }
                    """));

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");

            Exception? captured = null;
            try
            {
                var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                    ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                captured = ex.InnerException;
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            if (captured is null)
            {
                throw new InvalidOperationException("Assertion failed: expected concurrent invocation to throw, but RunAsync completed.");
            }

            AssertEqual(typeof(InvalidOperationException), captured.GetType(), "diagnostic concurrent invocation exception type");
            AssertContains(captured.Message ?? string.Empty, "Another diagnostic session");

            // Artifacts must NOT have been written; only the lock file should exist.
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "summary.json")), "diagnostic concurrent invocation must not write summary");
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "session-live.json")), "diagnostic concurrent invocation must not write live state");
        }
        finally
        {
            holderLock?.Dispose();
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-failure-test-{Guid.NewGuid():N}");
        var getSnapshotCount = 0;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);

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

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

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

    internal static async Task DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow()
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

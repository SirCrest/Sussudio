using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class DiagnosticCancellationTests
    {
        [Theory]
        [InlineData("preview-only", false)]
        [InlineData("recording-only", false)]
        [InlineData("flashback", false)]
        [InlineData("preview-only", true)]
        [InlineData("recording-only", true)]
        [InlineData("flashback", true)]
        public Task CancellationRestoresAcknowledgedAndUnconfirmedStartup(string scenario, bool loseReply)
            => global::Program.DiagnosticCancellation_RestoresStartup(scenario, loseReply, leaveRunning: false);

        [Fact]
        public Task LeaveRunningRetainsTheStartedPreview()
            => global::Program.DiagnosticCancellation_RestoresStartup("preview-only", loseReply: false, leaveRunning: true);

        [Theory]
        [InlineData("Playing")]
        [InlineData("Paused")]
        [InlineData("Scrubbing")]
        public Task CancellationPreservesExistingCaptureAndPlayback(string playbackState)
            => global::Program.DiagnosticCancellation_PreservesInitialState(playbackState);

        [Theory]
        [InlineData("pipe-response-timeout")]
        [InlineData("pipe-protocol-error")]
        [InlineData("pipe-invalid-json")]
        [InlineData("pipe-io-error")]
        [InlineData("pipe-canceled")]
        public Task ReturnedTransportFailureReconcilesAdmittedPreview(string errorCode)
            => global::Program.DiagnosticCancellation_ReconcilesReturnedTransportFailure(errorCode, effectApplied: true);

        [Fact]
        public Task RejectedAppCommandDoesNotClaimStartupOwnership()
            => global::Program.DiagnosticCancellation_ReconcilesReturnedTransportFailure("validation-failed", effectApplied: false);

        [Theory]
        [InlineData("pipe-response-timeout")]
        [InlineData("pipe-invalid-json")]
        public Task ThrownTransportFailureReconcilesAdmittedPreview(string errorCode)
            => global::Program.DiagnosticCancellation_ReconcilesReturnedTransportFailure(errorCode, effectApplied: true, throwException: true);
    }
}

static partial class Program
{
    internal static async Task DiagnosticCancellation_ReconcilesReturnedTransportFailure(string errorCode, bool effectApplied, bool throwException = false)
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-reply-{Guid.NewGuid():N}");
        var preview = false;
        var stopped = false;
        CancellationToken? startupToken = null;
        var assembly = LoadDiagnosticSessionRunnerAssembly();
        Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? payload, int? _, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (command == "GetSnapshot")
                return Task.FromResult(DiagnosticCancellationSnapshot(preview, false, false, "Live"));
            if (command == "SetPreviewEnabled")
            {
                if (Convert.ToBoolean(payload!["enabled"]))
                {
                    startupToken = token;
                    preview = effectApplied;
                    if (throwException)
                    {
                        Exception error;
                        if (errorCode == "pipe-response-timeout")
                        {
                            var contractsReference = assembly.GetReferencedAssemblies()
                                .Single(reference => reference.Name == "Sussudio.Automation.Contracts");
                            var contracts = AssemblyLoadContext.GetLoadContext(assembly)!.LoadFromAssemblyName(contractsReference);
                            var timeoutType = contracts.GetType("Sussudio.Tools.AutomationPipeResponseTimeoutException")!;
                            error = (Exception)Activator.CreateInstance(timeoutType, "test response timed out", new TimeoutException())!;
                        }
                        else
                        {
                            error = new JsonException("test response contained invalid JSON");
                        }
                        return Task.FromException<JsonElement>(error);
                    }
                    return Task.FromResult(Sussudio.Tools.AutomationSyntheticErrorResponse.Create("test response was not received", errorCode));
                }
                Assert.NotEqual(startupToken, token);
                stopped = true;
                preview = false;
            }
            return Task.FromResult(ParseDiagnosticSessionJson("""{"Success":true,"Data":[]}"""));
        }

        try
        {
            var options = CreateDiagnosticSessionOptions(assembly, "preview-only", 0, 100, outputDirectory);
            var result = await RunTokenAwareDiagnosticSessionAsync(assembly, options, SendAsync, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            Assert.False(preview, "A returned transport failure cannot prove that an admitted preview start had no effect.");
            Assert.Equal(effectApplied, stopped);
            var reconciled = ((IEnumerable<string>)GetPropertyValue(result, "Actions")!)
                .Any(action => action.Contains("unconfirmed startup effects reconciled", StringComparison.Ordinal));
            Assert.Equal(effectApplied, reconciled);
            Assert.False(GetBoolProperty(result, "Success"));
            Assert.Null(GetPropertyValue(result, "UnhandledException"));
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    internal static async Task DiagnosticCancellation_RestoresStartup(string scenario, bool loseReply, bool leaveRunning)
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-cancel-{Guid.NewGuid():N}");
        using var cancellation = new CancellationTokenSource();
        var preview = false;
        var recording = false;
        var flashback = false;
        var transportReleased = true;
        var commands = new List<(string Command, bool? Enabled, bool FreshToken)>();
        var mutationToInterrupt = scenario switch
        {
            "recording-only" => "SetRecordingEnabled",
            "flashback" => "SetFlashbackEnabled",
            _ => "SetPreviewEnabled"
        };

        async Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? payload, int? _, CancellationToken token)
        {
            Assert.True(transportReleased, "Cleanup must wait until canceled transport work has released its resources.");
            token.ThrowIfCancellationRequested();
            var enabled = payload?.TryGetValue("enabled", out var value) == true ? Convert.ToBoolean(value) : (bool?)null;
            commands.Add((command, enabled, cancellation.IsCancellationRequested && !token.IsCancellationRequested));
            if (command == "GetSnapshot")
                return DiagnosticCancellationSnapshot(preview, recording, flashback, "Live");
            if (command == "SetPreviewEnabled") preview = enabled!.Value;
            if (command == "SetRecordingEnabled") recording = enabled!.Value;
            if (command == "SetFlashbackEnabled") flashback = enabled!.Value;

            var interrupt = !cancellation.IsCancellationRequested &&
                (loseReply ? command == mutationToInterrupt && enabled == true : command == "WaitForCondition");
            if (interrupt)
            {
                transportReleased = false;
                cancellation.Cancel();
                try
                {
                    await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                }
                finally
                {
                    // Model the actual pipe disposal that completes after cancellation.
                    await Task.Delay(50).ConfigureAwait(false);
                    transportReleased = true;
                }
            }

            return ParseDiagnosticSessionJson("""{"Success":true,"Message":"ok","Data":[]}""");
        }

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(assembly, scenario, 1, 100, outputDirectory);
            options.GetType().GetProperty("LeaveRunning")!.SetValue(options, leaveRunning);
            var result = await RunTokenAwareDiagnosticSessionAsync(assembly, options, SendAsync, cancellation.Token)
                .WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            Assert.True(cancellation.IsCancellationRequested);
            Assert.False(GetBoolProperty(result, "Success"));
            Assert.Equal(leaveRunning, preview);
            Assert.False(recording);
            Assert.False(flashback);
            foreach (var property in new[] { "SummaryPath", "SamplesPath", "FrameLedgerPath", "TimelinePath", "LivePath" })
                Assert.True(File.Exists(GetStringProperty(result, property)), $"Cancellation must retain {property}.");
            using var summary = JsonDocument.Parse(await File.ReadAllTextAsync(GetStringProperty(result, "SummaryPath")).ConfigureAwait(false));
            Assert.Equal("canceled", summary.RootElement.GetProperty("TerminalState").GetString());
            var cleanupMutations = commands.Where(item => item.Enabled == false).ToArray();
            Assert.Equal(leaveRunning ? 0 : scenario == "flashback" && !loseReply ? 2 : 1, cleanupMutations.Length);
            Assert.All(cleanupMutations, item => Assert.True(item.FreshToken));
            Assert.Contains(commands, item => item.Command == "GetSnapshot" && item.FreshToken);
            if (loseReply)
                Assert.Contains("unconfirmed startup effects reconciled", string.Join("\n", (IEnumerable<string>)GetPropertyValue(result, "Actions")!));
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    internal static async Task DiagnosticCancellation_PreservesInitialState(string playbackState)
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-initial-{Guid.NewGuid():N}");
        using var cancellation = new CancellationTokenSource();
        var commands = new List<string>();
        var snapshotCount = 0;
        Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? _, int? __, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            commands.Add(command);
            if (command == "GetSnapshot")
            {
                if (++snapshotCount == 2)
                {
                    cancellation.Cancel();
                    return Task.FromCanceled<JsonElement>(token);
                }
                return Task.FromResult(DiagnosticCancellationSnapshot(true, true, true, playbackState));
            }
            return Task.FromResult(ParseDiagnosticSessionJson("""{"Success":true,"Data":[]}"""));
        }

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(assembly, "flashback-playback", 1, 100, outputDirectory);
            await RunTokenAwareDiagnosticSessionAsync(assembly, options, SendAsync, cancellation.Token)
                .WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            Assert.DoesNotContain(commands, command => command.StartsWith("Set", StringComparison.Ordinal) || command == "FlashbackAction");
            Assert.True(File.Exists(Path.Combine(outputDirectory, "summary.json")));
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static JsonElement DiagnosticCancellationSnapshot(bool preview, bool recording, bool flashback, string playbackState)
        => JsonSerializer.SerializeToElement(new
        {
            Success = true,
            Snapshot = new
            {
                IsPreviewing = preview,
                IsRecording = recording,
                FlashbackActive = flashback,
                FlashbackPlaybackState = playbackState,
                DiagnosticHealthStatus = "Healthy",
                DiagnosticLikelyStage = "none",
                DiagnosticSummary = "test state",
                DiagnosticEvidence = "test state",
                FrameLedgerRecentEvents = Array.Empty<object>()
            }
        });

    private static async Task<object> RunTokenAwareDiagnosticSessionAsync(
        Assembly assembly, object options,
        Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> sender,
        CancellationToken token)
    {
        var runner = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")!;
        var method = runner.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(candidate =>
            candidate.Name == "RunAsync" && candidate.GetParameters().Length == 3 &&
            candidate.GetParameters()[1].ParameterType == sender.GetType());
        var task = (Task)method.Invoke(null, new object?[] { options, sender, token })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }
}

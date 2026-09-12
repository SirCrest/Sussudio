using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class DiagnosticCycleLifetimeTests
{
    [Theory]
    [InlineData("flashback-restart-cycle")]
    [InlineData("flashback-encoder-cycle")]
    [InlineData("flashback-lifecycle")]
    [InlineData("flashback-preview-cycle")]
    [InlineData("flashback-playback-preview-cycle")]
    [InlineData("flashback-recording-preview-cycle")]
    public async Task FaultAfterAnAcceptedMutationRestoresTheScenarioOwnedState(string scenario)
    {
        await using var fixture = new CycleFixture(scenario);
        var primary = new IOException("cycle operation failed after mutation");
        fixture.FailAfterMutation(primary);

        var error = await Assert.ThrowsAsync<IOException>(() => fixture.Run().WaitAsync(CycleFixture.WaitLimit));

        Assert.Same(primary, error);
        Assert.True(fixture.FaultInjected);
        AssertRestored(fixture);
        AssertOwnedRestoration(fixture);
    }

    [Theory]
    [InlineData("flashback-restart-cycle")]
    [InlineData("flashback-encoder-cycle")]
    [InlineData("flashback-lifecycle")]
    [InlineData("flashback-preview-cycle")]
    [InlineData("flashback-playback-preview-cycle")]
    [InlineData("flashback-recording-preview-cycle")]
    public async Task CallerCancellationDoesNotCancelTheIndependentRestoration(string scenario)
    {
        await using var fixture = new CycleFixture(scenario);
        using var caller = new CancellationTokenSource();
        var primary = new OperationCanceledException("caller canceled after cycle mutation", caller.Token);
        fixture.FailAfterMutation(primary, caller.Cancel);

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Run(caller.Token).WaitAsync(CycleFixture.WaitLimit));

        Assert.Same(primary, error);
        Assert.Equal(caller.Token, error.CancellationToken);
        Assert.True(caller.IsCancellationRequested);
        AssertRestored(fixture);
        AssertOwnedRestoration(fixture);
        var cleanupRequests = fixture.Requests.Where(request => request.IsCleanup).ToArray();
        Assert.NotEmpty(cleanupRequests);
        Assert.All(cleanupRequests, request =>
        {
            Assert.NotEqual(caller.Token, request.Token);
            Assert.True(request.Token.CanBeCanceled);
            Assert.False(request.CanceledAtSend);
        });
    }

    [Theory]
    [InlineData("flashback-restart-cycle")]
    [InlineData("flashback-encoder-cycle")]
    [InlineData("flashback-lifecycle")]
    [InlineData("flashback-preview-cycle")]
    [InlineData("flashback-playback-preview-cycle")]
    [InlineData("flashback-recording-preview-cycle")]
    public async Task ReadinessFailureDoesNotClaimMutationCleanup(string scenario)
    {
        await using var fixture = new CycleFixture(scenario);
        var primary = new IOException("readiness failed before mutation");
        fixture.Script = request =>
        {
            Assert.Equal("GetSnapshot", request.Command);
            return Task.FromException<JsonElement>(primary);
        };

        Assert.Same(primary, await Assert.ThrowsAsync<IOException>(
            () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));

        Assert.DoesNotContain(fixture.Requests, request => request.IsCleanup);
        Assert.All(fixture.Requests, request => Assert.Equal("GetSnapshot", request.Command));
        AssertRestored(fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedRestartAfterPrimingReturnsPlaybackLive(bool returnedFailure)
    {
        await using var fixture = new CycleFixture("flashback-restart-cycle");
        var primary = new IOException("restart transport failure");
        fixture.Script = request => request.Command == "RestartFlashback"
            ? returnedFailure
                ? Task.FromResult(CycleFixture.Failure("restart rejected"))
                : Task.FromException<JsonElement>(primary)
            : fixture.DefaultReply(request);

        if (returnedFailure)
        {
            await fixture.Run().WaitAsync(CycleFixture.WaitLimit);
            Assert.Contains(fixture.Warnings, warning => warning.Contains("restart rejected", StringComparison.Ordinal));
        }
        else
        {
            Assert.Same(primary, await Assert.ThrowsAsync<IOException>(
                () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));
        }

        Assert.Contains(fixture.Requests, request => request.Action == "seek");
        Assert.Equal("Live", fixture.PlaybackState);
        AssertOwnedRestoration(fixture);
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "FlashbackExport");
    }

    [Fact]
    public async Task AcceptedRestartWithAnActivePausedBufferStillOwnsLiveRestoration()
    {
        await using var fixture = new CycleFixture("flashback-restart-cycle");
        fixture.Script = request => request.Command == "RestartFlashback"
            ? Task.FromResult(CycleFixture.Success())
            : fixture.DefaultReply(request);

        await fixture.Run().WaitAsync(CycleFixture.WaitLimit);

        AssertRestored(fixture);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("playback did not return live after restart", StringComparison.Ordinal));
        Assert.Contains(fixture.Requests, request => request.IsCleanup && request.Action == "go-live");
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "FlashbackExport");
    }

    [Fact]
    public async Task PlaybackFailureBeforePreviewStopRestoresOnlyPlayback()
    {
        await using var fixture = new CycleFixture("flashback-playback-preview-cycle");
        var primary = new IOException("playing snapshot failed before preview stop");
        fixture.Script = request => request.Command == "GetSnapshot" && fixture.PlaybackState == "Playing"
            ? Task.FromException<JsonElement>(primary)
            : fixture.DefaultReply(request);

        Assert.Same(primary, await Assert.ThrowsAsync<IOException>(
            () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));

        Assert.Equal("Live", fixture.PlaybackState);
        Assert.True(fixture.IsPreviewing);
        Assert.Contains(fixture.Requests, request => request.IsCleanup && request.Action == "go-live");
        Assert.DoesNotContain(fixture.Requests, request => request.Command is "SetPreviewEnabled" or "SetFlashbackEnabled");
    }

    [Fact]
    public async Task LifecycleFailureBeforeDisableRestoresOnlyPlayback()
    {
        await using var fixture = new CycleFixture("flashback-lifecycle");
        var primary = new IOException("lifecycle seek failed before disable");
        fixture.Script = request => request.Action == "seek"
            ? Task.FromException<JsonElement>(primary)
            : fixture.DefaultReply(request);

        Assert.Same(primary, await Assert.ThrowsAsync<IOException>(
            () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));

        AssertRestored(fixture);
        Assert.Contains(fixture.Requests, request => request.IsCleanup && request.Action == "go-live");
        Assert.DoesNotContain(fixture.Requests, request => request.Command is "SetPreviewEnabled" or "SetFlashbackEnabled");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EncoderRestoreFailureDoesNotReplaceTheOriginalExportFailure(bool returnedCleanupFailure)
    {
        await using var fixture = new CycleFixture("flashback-encoder-cycle");
        var primary = new IOException("original encoder export failure");
        fixture.Script = request => request.Command == "FlashbackExport"
            ? Task.FromException<JsonElement>(primary)
            : fixture.DefaultReply(request);
        fixture.CleanupScript = request => request.Command == "SetPreset"
            ? returnedCleanupFailure
                ? Task.FromResult(CycleFixture.Failure("preset restore rejected"))
                : Task.FromException<JsonElement>(new InvalidOperationException("preset restore threw"))
            : fixture.DefaultReply(request);

        Assert.Same(primary, await Assert.ThrowsAsync<IOException>(
            () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));

        Assert.Contains(fixture.Requests, request => request.IsCleanup && request.Preset == CycleFixture.OriginalPreset);
        Assert.Contains(fixture.Warnings, warning => warning.Contains(
            returnedCleanupFailure ? "preset restore rejected" : "preset restore threw", StringComparison.Ordinal));
        Assert.NotEqual(CycleFixture.OriginalPreset, fixture.SelectedPreset);
    }

    [Theory]
    [InlineData("flashback-restart-cycle")]
    [InlineData("flashback-lifecycle")]
    [InlineData("flashback-preview-cycle")]
    [InlineData("flashback-playback-preview-cycle")]
    [InlineData("flashback-recording-preview-cycle")]
    public async Task SecondaryRestorationFailureCannotReplaceTheOriginalMutationFailure(string scenario)
    {
        await using var fixture = new CycleFixture(scenario);
        var primary = new IOException("original interrupted cycle mutation");
        fixture.FailAfterMutation(primary);
        fixture.CleanupScript = _ => Task.FromException<JsonElement>(new InvalidOperationException("secondary restoration failure"));

        Assert.Same(primary, await Assert.ThrowsAsync<IOException>(
            () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));

        Assert.Contains(fixture.Warnings, warning => warning.Contains("secondary restoration failure", StringComparison.Ordinal));
        Assert.DoesNotContain(fixture.Actions, action => action.EndsWith("returned live", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EncoderRestorationFailureWithoutAnEarlierExceptionStillFailsTheScenario()
    {
        await using var fixture = new CycleFixture("flashback-encoder-cycle");
        var cleanupFailure = new IOException("independent restore failure after successful export");
        fixture.CleanupScript = _ => Task.FromException<JsonElement>(cleanupFailure);

        Assert.Same(cleanupFailure, await Assert.ThrowsAsync<IOException>(
            () => fixture.Run().WaitAsync(CycleFixture.WaitLimit)));

        Assert.Contains(fixture.Requests, request => request.Command == "VerifyFile");
        Assert.Contains(fixture.Warnings, warning => warning.Contains(cleanupFailure.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task FailedPreviewCycleRemainsOwnedUntilItsRestoreReplyCompletes()
    {
        await using var fixture = new CycleFixture("flashback-preview-cycle");
        var primary = new IOException("preview stop reply was lost after mutation");
        fixture.FailAfterMutation(primary);
        var restoreEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var restoreReply = fixture.NewReply();
        fixture.CleanupScript = async request =>
        {
            if (request.Command == "SetPreviewEnabled" && request.Enabled == true)
            {
                restoreEntered.TrySetResult();
                await restoreReply.Task.ConfigureAwait(false);
            }
            return await fixture.DefaultReply(request).ConfigureAwait(false);
        };

        var scenario = fixture.Run();
        try
        {
            await restoreEntered.Task.WaitAsync(CycleFixture.WaitLimit);
            Assert.False(scenario.IsCompleted);
            Assert.False(fixture.IsPreviewing);
            restoreReply.TrySetResult(CycleFixture.Success());
            Assert.Same(primary, await Assert.ThrowsAsync<IOException>(() => scenario.WaitAsync(CycleFixture.WaitLimit)));
            Assert.True(fixture.IsPreviewing);
        }
        finally
        {
            restoreReply.TrySetResult(CycleFixture.Success());
        }
    }

    [Theory]
    [InlineData("flashback-restart-cycle")]
    [InlineData("flashback-encoder-cycle")]
    [InlineData("flashback-lifecycle")]
    [InlineData("flashback-preview-cycle")]
    [InlineData("flashback-playback-preview-cycle")]
    [InlineData("flashback-recording-preview-cycle")]
    public async Task FullRunnerRestoresFailedCycleWithoutStoppingTheExistingSession(string scenario)
    {
        await using var fixture = new CycleFixture(scenario);
        fixture.FailAfterMutation(new IOException("cycle failure retained by full runner"));

        var result = await fixture.RunFullRunnerAsync().WaitAsync(CycleFixture.WaitLimit);

        Assert.True(fixture.FaultInjected);
        Assert.False((bool)result.GetType().GetProperty("Success")!.GetValue(result)!);
        Assert.Contains("cycle failure retained by full runner", (string)result.GetType().GetProperty("UnhandledException")!.GetValue(result)!);
        AssertRestored(fixture);
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "SetRecordingEnabled");
        var previewChanges = fixture.Requests.Where(request => request.Command == "SetPreviewEnabled").ToArray();
        var flashbackChanges = fixture.Requests.Where(request => request.Command == "SetFlashbackEnabled").ToArray();
        if (scenario.Contains("preview-cycle", StringComparison.Ordinal))
            Assert.Equal(new bool?[] { false, true }, previewChanges.Select(request => request.Enabled));
        else
            Assert.Empty(previewChanges);
        if (scenario == "flashback-lifecycle")
            Assert.Equal(new bool?[] { false, true }, flashbackChanges.Select(request => request.Enabled));
        else
            Assert.Empty(flashbackChanges);
    }

    [Theory]
    [InlineData("flashback-restart-cycle")]
    [InlineData("flashback-encoder-cycle")]
    [InlineData("flashback-lifecycle")]
    [InlineData("flashback-preview-cycle")]
    [InlineData("flashback-playback-preview-cycle")]
    [InlineData("flashback-recording-preview-cycle")]
    public async Task FullRunnerCancellationRestoresTheExistingSessionWithIndependentTokens(string scenario)
    {
        await using var fixture = new CycleFixture(scenario);
        using var caller = new CancellationTokenSource();
        fixture.FailAfterMutation(new OperationCanceledException("cycle cancellation after mutation", caller.Token), caller.Cancel);

        var result = await fixture.RunFullRunnerAsync(caller.Token).WaitAsync(CycleFixture.WaitLimit);

        Assert.True(fixture.FaultInjected);
        Assert.True(caller.IsCancellationRequested);
        Assert.False((bool)result.GetType().GetProperty("Success")!.GetValue(result)!);
        AssertRestored(fixture);
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "SetRecordingEnabled");
        var cleanup = fixture.Requests.Where(request => request.IsCleanup).ToArray();
        Assert.NotEmpty(cleanup);
        Assert.All(cleanup, request =>
        {
            Assert.True(request.Token.CanBeCanceled);
            Assert.False(request.CanceledAtSend);
        });
    }

    private static void AssertRestored(CycleFixture fixture)
    {
        Assert.True(fixture.IsPreviewing);
        Assert.True(fixture.FlashbackActive);
        Assert.Equal("Live", fixture.PlaybackState);
        Assert.Equal(CycleFixture.OriginalPreset, fixture.SelectedPreset);
        Assert.Equal(fixture.InitiallyRecording, fixture.IsRecording);
    }

    private static void AssertOwnedRestoration(CycleFixture fixture)
    {
        var cleanup = fixture.Requests.Where(request => request.IsCleanup).ToArray();
        Assert.NotEmpty(cleanup);
        if (fixture.Scenario == "flashback-encoder-cycle")
            Assert.Contains(cleanup, request => request.Preset == CycleFixture.OriginalPreset);
        if (fixture.Scenario == "flashback-lifecycle")
            Assert.Contains(cleanup, request => request.Command == "SetFlashbackEnabled" && request.Enabled == true);
        if (fixture.Scenario.Contains("preview-cycle", StringComparison.Ordinal))
            Assert.Contains(cleanup, request => request.Command == "SetPreviewEnabled" && request.Enabled == true);
        if (fixture.Scenario is "flashback-restart-cycle" or "flashback-lifecycle" or "flashback-playback-preview-cycle")
            Assert.Contains(cleanup, request => request.Action == "go-live");
        Assert.DoesNotContain(cleanup, request => request.Command == "SetRecordingEnabled");
    }

    private sealed record Request(
        string Command, Dictionary<string, object?>? Payload, CancellationToken Token, bool IsCleanup)
    {
        internal bool CanceledAtSend { get; } = Token.IsCancellationRequested;
        internal string? Action => Payload?.GetValueOrDefault("action") as string;
        internal string? Preset => Payload?.GetValueOrDefault("preset") as string;
        internal bool? Enabled => Payload?.GetValueOrDefault("enabled") as bool?;
    }

    // Runs real diagnostic orchestration against a managed command/snapshot model.
    // No native capture, app process, pipe, hardware, or media file is involved.
    private sealed class CycleFixture : IAsyncDisposable
    {
        internal static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(8);
        internal const string OriginalPreset = "P4";
        private readonly CancellationTokenSource _limit = new(TimeSpan.FromSeconds(15));
        private readonly List<Task> _tasks = new();
        private readonly List<TaskCompletionSource<JsonElement>> _replies = new();
        private readonly object _stateGate = new();
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "Sussudio-cycle-lifetime-tests", Guid.NewGuid().ToString("N"));
        private long _snapshotNumber;
        private int _generation;
        private int _position;

        internal string Scenario { get; }
        internal bool InitiallyRecording => Scenario == "flashback-recording-preview-cycle";
        internal List<string> Actions { get; } = new();
        internal List<string> Warnings { get; } = new();
        internal ConcurrentQueue<Request> Requests { get; } = new();
        internal Func<Request, Task<JsonElement>>? Script { get; set; }
        internal Func<Request, Task<JsonElement>>? CleanupScript { get; set; }
        internal bool FaultInjected { get; private set; }
        internal bool IsPreviewing { get; private set; } = true;
        internal bool FlashbackActive { get; private set; } = true;
        internal bool IsRecording { get; private set; }
        internal string PlaybackState { get; private set; } = "Live";
        internal string SelectedPreset { get; private set; } = OriginalPreset;

        internal CycleFixture(string scenario)
        {
            Scenario = scenario;
            IsRecording = InitiallyRecording;
            Directory.CreateDirectory(_directory);
        }

        internal void FailAfterMutation(Exception error, Action? afterMutation = null)
        {
            Script = request =>
            {
                var shouldFail = Scenario switch
                {
                    "flashback-restart-cycle" => request.Action == "seek",
                    "flashback-encoder-cycle" => request.Command == "SetPreset" && request.Preset != OriginalPreset,
                    "flashback-lifecycle" => request.Command == "SetFlashbackEnabled" && request.Enabled == false,
                    _ => request.Command == "SetPreviewEnabled" && request.Enabled == false
                };
                if (!FaultInjected && shouldFail)
                {
                    FaultInjected = true;
                    lock (_stateGate) ApplyAcceptedMutation(request);
                    afterMutation?.Invoke();
                    return Task.FromException<JsonElement>(error);
                }
                return DefaultReply(request);
            };
        }

        internal TaskCompletionSource<JsonElement> NewReply()
        {
            var reply = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _replies.Add(reply);
            return reply;
        }

        internal Task Run(CancellationToken token = default)
        {
            var assembly = global::Program.LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
            var (owner, methodName, hasOutputDirectory) = Scenario switch
            {
                "flashback-restart-cycle" => ("DiagnosticSessionFlashbackCycleScenarios", "RunFlashbackRestartCycleAsync", true),
                "flashback-encoder-cycle" => ("DiagnosticSessionFlashbackCycleScenarios", "RunFlashbackEncoderCycleAsync", true),
                "flashback-lifecycle" => ("DiagnosticSessionFlashbackLifecycleScenarios", "RunFlashbackLifecycleAsync", false),
                "flashback-preview-cycle" => ("DiagnosticSessionFlashbackPreviewCycleScenarios", "RunFlashbackPreviewCycleAsync", true),
                "flashback-playback-preview-cycle" => ("DiagnosticSessionFlashbackPreviewCycleScenarios", "RunFlashbackPlaybackPreviewCycleAsync", true),
                "flashback-recording-preview-cycle" => ("DiagnosticSessionFlashbackPreviewCycleScenarios", "RunFlashbackRecordingPreviewCycleAsync", false),
                _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
            };
            var method = assembly.GetType("Sussudio.Tools." + owner, true)!
                .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
            var operationToken = token.CanBeCanceled ? token : _limit.Token;
            var hasCleanupSender = method.GetParameters().Length == (hasOutputDirectory ? 6 : 5);
            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sender =
                (command, payload, _) => Send(command, payload, operationToken,
                    isCleanup: !hasCleanupSender && command == "SetPreset" &&
                        payload?.GetValueOrDefault("preset") as string == OriginalPreset);
            Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> cleanupSender =
                (command, payload, _, cleanupToken) => Send(command, payload, cleanupToken, isCleanup: true);
            var arguments = new List<object>();
            if (hasOutputDirectory) arguments.Add(_directory);
            arguments.AddRange(new object[] { Actions, Warnings, sender });
            // Keep baseline execution meaningful: old arity reaches the old body,
            // while candidate arity receives an independently cancellable sender.
            if (method.GetParameters().Length == arguments.Count + 2)
                arguments.Add(cleanupSender);
            else
                Assert.Equal(arguments.Count + 1, method.GetParameters().Length);
            arguments.Add(operationToken);
            var task = InvokeTask(method, arguments.ToArray());
            _tasks.Add(task);
            return task;
        }

        internal async Task<object> RunFullRunnerAsync(CancellationToken cancellationToken = default)
        {
            var assembly = global::Program.LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions", true)!;
            var options = Activator.CreateInstance(optionsType)!;
            optionsType.GetProperty("Scenario")!.SetValue(options, Scenario);
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, _directory);
            CancellationToken? scenarioToken = null;
            Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> sender =
                (command, payload, _, token) =>
                {
                    scenarioToken ??= token;
                    return Send(command, payload, token, isCleanup: token != scenarioToken.Value);
                };
            var method = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner", true)!.GetMethods()
                .Single(candidate => candidate.Name == "RunAsync" && candidate.GetParameters()[1].ParameterType == sender.GetType());
            var task = InvokeTask(method, new object[] { options, sender,
                cancellationToken.CanBeCanceled ? cancellationToken : _limit.Token });
            _tasks.Add(task);
            await task.ConfigureAwait(false);
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }

        private Task<JsonElement> Send(string command, Dictionary<string, object?>? payload, CancellationToken token, bool isCleanup)
        {
            var request = new Request(command, payload is null ? null : new(payload), token, isCleanup);
            Requests.Enqueue(request);
            token.ThrowIfCancellationRequested();
            return (isCleanup ? CleanupScript : Script)?.Invoke(request) ?? DefaultReply(request);
        }

        private static Task InvokeTask(MethodInfo method, object[] arguments)
        {
            try { return (Task)method.Invoke(null, arguments)!; }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        private void ApplyAcceptedMutation(Request request)
        {
            switch (request.Action)
            {
                case "pause": PlaybackState = "Paused"; break;
                case "play": PlaybackState = "Playing"; break;
                case "seek": _position = (int)request.Payload!["positionMs"]!; break;
                case "go-live": PlaybackState = "Live"; break;
            }
            switch (request.Command)
            {
                case "SetPreset": SelectedPreset = request.Preset!; _generation++; break;
                case "SetPreviewEnabled": IsPreviewing = request.Enabled!.Value; break;
                case "SetFlashbackEnabled": FlashbackActive = request.Enabled!.Value; break;
                case "SetRecordingEnabled": IsRecording = request.Enabled!.Value; break;
            }
        }

        internal Task<JsonElement> DefaultReply(Request request)
        {
            lock (_stateGate)
            {
                ApplyAcceptedMutation(request);
                // Completed teardown/restart returns Live; a fault injected after
                // admission above leaves this completion unconfirmed instead.
                if (request.Command == "RestartFlashback" ||
                    request.Command is "SetPreviewEnabled" or "SetFlashbackEnabled" && request.Enabled == false)
                    PlaybackState = "Live";
                if (request.Command == "RestartFlashback") _generation++;
                if (request.Command != "GetSnapshot") return Task.FromResult(Success());
                var sample = ++_snapshotNumber;
                return Task.FromResult(JsonSerializer.SerializeToElement(new
                {
                    Success = true,
                    Snapshot = new
                    {
                        IsPreviewing, FlashbackActive, IsRecording, SelectedPreset,
                        RecordingBackend = IsRecording ? "Flashback" : "None",
                        RecordingFileGrowing = IsRecording,
                        RecordingFilePath = Path.Combine(_directory, "existing-recording.mp4"),
                        FlashbackBufferedDurationMs = 30000, FlashbackEncodedFrames = 2400 + sample * 100,
                        FlashbackFilePath = "synthetic-buffer-" + _generation + ".ts",
                        FlashbackPlaybackPositionMs = _position, FlashbackPlaybackState = PlaybackState,
                        FlashbackPlaybackFrameCount = PlaybackState == "Playing" ? 100 + sample * 10 : 0,
                        FlashbackPlaybackThreadAlive = PlaybackState != "Live", FlashbackPlaybackPendingCommands = 0,
                        FlashbackVideoFramesSubmittedToEncoder = 2400 + sample * 100,
                        FlashbackVideoEncoderPacketsWritten = 2400 + sample * 100,
                        IsAudioEnabled = true
                    }
                }));
            }
        }

        internal static JsonElement Success() => JsonSerializer.SerializeToElement(new { Success = true });
        internal static JsonElement Failure(string message) => JsonSerializer.SerializeToElement(new { Success = false, Message = message });

        public async ValueTask DisposeAsync()
        {
            _limit.Cancel();
            foreach (var reply in _replies) reply.TrySetCanceled(_limit.Token);
            try { await Task.WhenAll(_tasks).WaitAsync(WaitLimit); }
            catch when (_tasks.All(task => task.IsCompleted)) { /* Each primary outcome is asserted by its test. */ }
            finally
            {
                _limit.Dispose();
                if (_tasks.All(task => task.IsCompleted)) Directory.Delete(_directory, recursive: true);
            }
        }
    }
}

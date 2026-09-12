using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class DiagnosticExportCleanupTests
{
    [Theory]
    [InlineData("FlashbackExport")]
    [InlineData("VerifyFile")]
    public async Task PlaybackReturnedFailureStillReturnsLive(string failedCommand)
    {
        await using var fixture = new Fixture();
        fixture.Script = request => request.Command == failedCommand
            ? Task.FromResult(Failure("original returned failure")) : fixture.DefaultReply(request);
        await fixture.RunPlayback().WaitAsync(Fixture.Limit);
        Assert.Equal("Live", fixture.PlaybackState);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("original returned failure"));
        Assert.Single(fixture.Requests, request => request.Cleanup && request.Action == "go-live");
        Assert.DoesNotContain(fixture.Requests, request => request.Action == "clear-in-out-points");
    }

    [Theory]
    [InlineData("pause")]
    [InlineData("seek")]
    [InlineData("play")]
    [InlineData("FlashbackExport")]
    [InlineData("VerifyFile")]
    [InlineData("post-export-snapshot")]
    public async Task PlaybackThrownFailureKeepsItsIdentityWhenCleanupAlsoFails(string failedStep)
    {
        await using var fixture = new Fixture();
        var primary = new IOException("original scenario exception");
        fixture.Script = request => request.Cleanup
            ? Task.FromException<JsonElement>(new InvalidOperationException("cleanup failure"))
            : request.Step == failedStep || failedStep == "post-export-snapshot" && fixture.ExportRequested && request.Command == "GetSnapshot"
                ? Task.FromException<JsonElement>(primary) : fixture.DefaultReply(request);
        var actual = await Assert.ThrowsAsync<IOException>(() => fixture.RunPlayback().WaitAsync(Fixture.Limit));
        Assert.Same(primary, actual);
        Assert.Single(fixture.Requests, request => request.Cleanup && request.Action == "go-live");
        Assert.Contains(fixture.Warnings, warning => warning.Contains("cleanup failure"));
    }

    [Fact]
    public async Task PlaybackRestorationWaitsForLiveAfterTheGoLiveAcknowledgement()
    {
        await using var fixture = new Fixture();
        var snapshotRequested = NewSignal();
        var snapshotReply = fixture.NewReply();
        fixture.Script = request =>
        {
            if (request.Cleanup && request.Action == "go-live") return Task.FromResult(Success());
            if (request.Cleanup && request.Command == "GetSnapshot")
            {
                snapshotRequested.TrySetResult();
                return snapshotReply.Task;
            }
            return fixture.DefaultReply(request);
        };
        var scenario = fixture.RunPlayback();
        await snapshotRequested.Task.WaitAsync(Fixture.Limit);
        Assert.False(scenario.IsCompleted);
        Assert.Equal("Playing", fixture.PlaybackState);
        fixture.PlaybackState = "Live";
        snapshotReply.SetResult(fixture.Snapshot());
        await scenario.WaitAsync(Fixture.Limit);
        Assert.Empty(fixture.Warnings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupRejectionDoesNotClaimSuccessfulRestoration(bool disable)
    {
        await using var fixture = new Fixture();
        fixture.Script = request => request.Cleanup
            ? Task.FromResult(Failure("cleanup rejected")) : fixture.DefaultReply(request);
        await fixture.Run(disable).WaitAsync(Fixture.Limit);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("cleanup rejected"));
        Assert.DoesNotContain(disable ? "flashback re-enabled after disable/export" : "flashback export playback go-live requested", fixture.Actions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupExceptionPropagatesWhenThereWasNoOriginalFailure(bool disable)
    {
        await using var fixture = new Fixture();
        var failure = new IOException("cleanup exception");
        fixture.Script = request => request.Cleanup ? Task.FromException<JsonElement>(failure) : fixture.DefaultReply(request);
        var actual = await Assert.ThrowsAsync<IOException>(() => fixture.Run(disable).WaitAsync(Fixture.Limit));
        Assert.Same(failure, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReturnedFailureRemainsThePrimaryOutcomeWhenCleanupThrows(bool disable)
    {
        await using var fixture = new Fixture();
        fixture.Script = request => request.Cleanup
            ? Task.FromException<JsonElement>(new IOException("secondary cleanup exception"))
            : request.Command == "FlashbackExport" ? Task.FromResult(Failure("primary returned failure")) : fixture.DefaultReply(request);
        await fixture.Run(disable).WaitAsync(Fixture.Limit);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("primary returned failure"));
        Assert.Contains(fixture.Warnings, warning => warning.Contains("secondary cleanup exception"));
    }

    [Fact]
    public async Task DisableExportFailureKeepsThePendingDisableOwnedBeforeRestoring()
    {
        await using var fixture = new Fixture();
        var export = fixture.NewReply();
        var disable = fixture.NewReply();
        var disableEntered = NewSignal();
        var primary = new IOException("original export failure");
        fixture.Script = request =>
        {
            if (request.Command == "FlashbackExport") return export.Task;
            if (!request.Cleanup && request.Command == "SetFlashbackEnabled")
            {
                disableEntered.TrySetResult();
                return disable.Task;
            }
            if (request.Cleanup) return Task.FromException<JsonElement>(new InvalidOperationException("cleanup failed too"));
            return fixture.DefaultReply(request);
        };
        var scenario = fixture.RunDisable();
        await disableEntered.Task.WaitAsync(Fixture.Limit);
        export.SetException(primary);
        await AssertStillOwned(scenario);
        Assert.DoesNotContain(fixture.Requests, request => request.Cleanup);
        disable.SetException(new InvalidOperationException("disable failed too"));
        var actual = await Assert.ThrowsAsync<IOException>(() => scenario.WaitAsync(Fixture.Limit));
        Assert.Same(primary, actual);
        Assert.Contains(fixture.Requests, request => request.Cleanup && request.Enabled == true);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("disable failed too"));
        Assert.Contains(fixture.Warnings, warning => warning.Contains("cleanup failed too"));
    }

    [Fact]
    public async Task DisableFailureCannotReleaseThePendingExport()
    {
        await using var fixture = new Fixture();
        var export = fixture.NewReply();
        var disableEntered = NewSignal();
        var primary = new IOException("original disable failure");
        fixture.Script = request =>
        {
            if (request.Command == "FlashbackExport") return export.Task;
            if (!request.Cleanup && request.Command == "SetFlashbackEnabled")
            {
                disableEntered.TrySetResult();
                return Task.FromException<JsonElement>(primary);
            }
            return fixture.DefaultReply(request);
        };
        var scenario = fixture.RunDisable();
        await disableEntered.Task.WaitAsync(Fixture.Limit);
        await AssertStillOwned(scenario);
        Assert.DoesNotContain(fixture.Requests, request => request.Cleanup);
        export.SetResult(Success());
        var actual = await Assert.ThrowsAsync<IOException>(() => scenario.WaitAsync(Fixture.Limit));
        Assert.Same(primary, actual);
        Assert.True(fixture.Active);
        Assert.Contains("flashback re-enabled after disable/export", fixture.Actions);
    }

    [Fact]
    public async Task CancellationDuringThePreDisableDelayObservesExportWithoutClaimingDisableOwnership()
    {
        await using var fixture = new Fixture();
        var export = fixture.NewReply();
        fixture.Script = request =>
        {
            if (request.Command != "FlashbackExport") return fixture.DefaultReply(request);
            fixture.Caller.Cancel();
            return export.Task;
        };
        var scenario = fixture.RunDisable();
        await AssertStillOwned(scenario);
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "SetFlashbackEnabled");
        export.SetException(new IOException("export ended after cancellation"));
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scenario.WaitAsync(Fixture.Limit));
        Assert.Equal(fixture.Caller.Token, actual.CancellationToken);
        Assert.DoesNotContain(fixture.Requests, request => request.Cleanup);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("export ended after cancellation"));
    }

    [Theory]
    [InlineData("FlashbackExport")]
    [InlineData("VerifyFile")]
    [InlineData("SetFlashbackEnabled")]
    public async Task DisableReturnedFailureStillRestoresAnExistingActiveSession(string rejectedCommand)
    {
        await using var fixture = new Fixture();
        fixture.Script = request => !request.Cleanup && request.Command == rejectedCommand
            ? Task.FromResult(Failure("returned failure")) : fixture.DefaultReply(request);
        await fixture.RunDisable().WaitAsync(Fixture.Limit);
        Assert.True(fixture.Active);
        Assert.Contains(fixture.Warnings, warning => warning.Contains("returned failure"));
        Assert.Single(fixture.Requests, request => request.Cleanup && request.Enabled == true);
        Assert.Contains("flashback re-enabled after disable/export", fixture.Actions);
    }

    [Fact]
    public async Task ThrownVerificationFailureRestoresFlashbackAndKeepsTheOriginalException()
    {
        await using var fixture = new Fixture();
        var primary = new IOException("verification exception");
        fixture.Script = request => request.Command == "VerifyFile" ? Task.FromException<JsonElement>(primary) : fixture.DefaultReply(request);
        var actual = await Assert.ThrowsAsync<IOException>(() => fixture.RunDisable().WaitAsync(Fixture.Limit));
        Assert.Same(primary, actual);
        Assert.True(fixture.Active);
        Assert.Single(fixture.Requests, request => request.Cleanup && request.Enabled == true);
    }

    [Fact]
    public async Task ReenableAcknowledgementDoesNotClaimRestorationBeforeAnActiveSnapshot()
    {
        await using var fixture = new Fixture();
        var snapshotRequested = NewSignal();
        var snapshot = fixture.NewReply();
        fixture.Script = request =>
        {
            if (request.Cleanup && request.Enabled == true) return Task.FromResult(Success());
            if (request.Cleanup && request.Command == "GetSnapshot")
            {
                snapshotRequested.TrySetResult();
                return snapshot.Task;
            }
            return fixture.DefaultReply(request);
        };
        var scenario = fixture.RunDisable();
        await snapshotRequested.Task.WaitAsync(Fixture.Limit);
        Assert.False(scenario.IsCompleted);
        Assert.False(fixture.Active);
        Assert.DoesNotContain("flashback re-enabled after disable/export", fixture.Actions);
        fixture.Active = true;
        snapshot.SetResult(fixture.Snapshot());
        await scenario.WaitAsync(Fixture.Limit);
        Assert.Contains("flashback re-enabled after disable/export", fixture.Actions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullRunnerRegistrationRestoresWithAnIndependentTokenAfterCallerCancellation(bool disable)
    {
        await using var fixture = new Fixture();
        fixture.Script = request =>
        {
            if (!request.Cleanup && (disable ? request.Enabled == false : request.Command == "FlashbackExport"))
            {
                if (disable) fixture.Active = false;
                fixture.Caller.Cancel();
                return Task.FromCanceled<JsonElement>(fixture.Caller.Token);
            }
            return fixture.DefaultReply(request);
        };
        try { await fixture.RunFullRunner(disable).WaitAsync(Fixture.Limit); }
        catch (OperationCanceledException) { Assert.True(fixture.Caller.IsCancellationRequested); }
        Assert.True(fixture.Caller.IsCancellationRequested);
        var restoration = Assert.Single(fixture.Requests, request => request.Cleanup &&
            (disable ? request.Enabled == true : request.Action == "go-live"));
        Assert.True(restoration.Token.CanBeCanceled);
        Assert.False(restoration.Token.IsCancellationRequested);
        Assert.NotEqual(fixture.Caller.Token, restoration.Token);
        Assert.True(fixture.Active);
        Assert.Equal("Live", fixture.PlaybackState);
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "SetPreviewEnabled");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullRunnerRetainsTheChannelUntilDelayedRestorationAndItsLaterSnapshotFinish(bool disable)
    {
        await using var fixture = new Fixture();
        var cleanupStarted = NewSignal();
        var laterSnapshot = NewSignal();
        var allowRestoredSnapshot = 0;
        CancellationToken cleanupToken = default;
        fixture.Script = request =>
        {
            if (!request.Cleanup && (disable ? request.Enabled == false : request.Command == "FlashbackExport"))
            {
                if (disable) fixture.Active = false;
                fixture.Caller.Cancel();
                return Task.FromCanceled<JsonElement>(fixture.Caller.Token);
            }
            if (request.Cleanup && (disable ? request.Enabled == true : request.Action == "go-live"))
            {
                cleanupToken = request.Token;
                cleanupStarted.TrySetResult();
                return Task.FromResult(Success());
            }
            if (request.Command == "GetSnapshot" && request.Token == cleanupToken &&
                Volatile.Read(ref allowRestoredSnapshot) != 0)
            {
                fixture.Active = true;
                fixture.PlaybackState = "Live";
                laterSnapshot.TrySetResult();
            }
            return fixture.DefaultReply(request);
        };

        var runner = fixture.RunFullRunner(disable);
        try
        {
            await cleanupStarted.Task.WaitAsync(Fixture.Limit);
            // Every poll returns promptly, leaving the channel free between polls.
            // The old observer published a result after two seconds without restored state.
            Assert.NotSame(runner, await Task.WhenAny(runner, Task.Delay(TimeSpan.FromMilliseconds(2500))));
            Assert.False(laterSnapshot.Task.IsCompleted);
            Assert.False(fixture.SummaryExists);
        }
        finally
        {
            Volatile.Write(ref allowRestoredSnapshot, 1);
        }
        await laterSnapshot.Task.WaitAsync(Fixture.Limit);
        try { await runner.WaitAsync(Fixture.Limit); }
        catch (OperationCanceledException) { Assert.True(fixture.Caller.IsCancellationRequested); }
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "SetPreviewEnabled");
    }

    private static async Task AssertStillOwned(Task scenario)
    {
        Assert.NotSame(scenario, await Task.WhenAny(scenario, Task.Delay(75)));
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static JsonElement Success() => JsonSerializer.SerializeToElement(new { Success = true });
    private static JsonElement Failure(string message) => JsonSerializer.SerializeToElement(new { Success = false, Message = message });

    private sealed record Request(string Command, Dictionary<string, object?>? Payload, CancellationToken Token, bool Cleanup)
    {
        internal string? Action => Payload?.GetValueOrDefault("action") as string;
        internal bool? Enabled => Command == "SetFlashbackEnabled" ? Payload?.GetValueOrDefault("enabled") as bool? : null;
        internal string Step => Action ?? Command;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal static readonly TimeSpan Limit = TimeSpan.FromSeconds(8);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "Sussudio-export-cleanup-tests", Guid.NewGuid().ToString("N"));
        private readonly List<Task> _tasks = new();
        private readonly List<TaskCompletionSource<JsonElement>> _replies = new();
        private readonly Assembly _assembly = global::Program.LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
        internal CancellationTokenSource Caller { get; } = new(TimeSpan.FromSeconds(15));
        internal ConcurrentQueue<Request> Requests { get; } = new();
        internal List<string> Actions { get; } = new();
        internal List<string> Warnings { get; } = new();
        internal Func<Request, Task<JsonElement>>? Script { get; set; }
        internal bool Active { get; set; } = true;
        internal string PlaybackState { get; set; } = "Live";
        internal bool ExportRequested { get; private set; }
        internal bool SummaryExists => File.Exists(Path.Combine(_directory, "summary.json"));
        private long _frames;

        internal Fixture() => Directory.CreateDirectory(_directory);
        internal TaskCompletionSource<JsonElement> NewReply()
        {
            var reply = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _replies.Add(reply);
            return reply;
        }

        internal Task Run(bool disable) => disable ? RunDisable() : RunPlayback();
        internal Task RunPlayback() => RunScenario("RunFlashbackExportPlaybackAsync");
        internal Task RunDisable() => RunScenario("RunFlashbackDisableDuringExportAsync");
        private Task RunScenario(string name)
        {
            var method = _assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios", true)!
                .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sender = (command, payload, _) => Send(command, payload, Caller.Token, false);
            Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> cleanup = (command, payload, _, token) => Send(command, payload, token, true);
            var arguments = new object?[] { _directory, Actions, Warnings, sender, cleanup, Caller.Token };
            var task = (Task)method.Invoke(null, arguments)!;
            _tasks.Add(task);
            return task;
        }

        internal Task RunFullRunner(bool disable)
        {
            var type = _assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions", true)!;
            var options = Activator.CreateInstance(type)!;
            type.GetProperty("Scenario")!.SetValue(options, disable ? "flashback-disable-during-export" : "flashback-export-playback");
            type.GetProperty("DurationSeconds")!.SetValue(options, 5);
            type.GetProperty("OutputDirectory")!.SetValue(options, _directory);
            Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> sender =
                (command, payload, _, token) => Send(command, payload, token, Caller.IsCancellationRequested && !token.IsCancellationRequested);
            var method = _assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner", true)!.GetMethods()
                .Single(method => method.Name == "RunAsync" && method.GetParameters()[1].ParameterType == sender.GetType());
            var task = (Task)method.Invoke(null, new object[] { options, sender, Caller.Token })!;
            _tasks.Add(task);
            return task;
        }

        private Task<JsonElement> Send(string command, Dictionary<string, object?>? payload, CancellationToken token, bool cleanup)
        {
            token.ThrowIfCancellationRequested();
            var request = new Request(command, payload is null ? null : new(payload), token, cleanup);
            Requests.Enqueue(request);
            if (command == "FlashbackExport") ExportRequested = true;
            return Script?.Invoke(request) ?? DefaultReply(request);
        }

        internal Task<JsonElement> DefaultReply(Request request)
        {
            if (request.Enabled.HasValue) Active = request.Enabled.Value;
            if (request.Action == "pause") PlaybackState = "Paused";
            if (request.Action == "play") PlaybackState = "Playing";
            if (request.Action == "go-live") PlaybackState = "Live";
            return Task.FromResult(request.Command == "GetSnapshot" ? Snapshot() : Success());
        }

        internal JsonElement Snapshot() => JsonSerializer.SerializeToElement(new
        {
            Success = true,
            Snapshot = new
            {
                IsPreviewing = true, FlashbackActive = Active, FlashbackBufferedDurationMs = 30000,
                FlashbackEncodedFrames = 2400, FlashbackPlaybackState = PlaybackState,
                FlashbackPlaybackFrameCount = Interlocked.Add(ref _frames, 100), FlashbackPlaybackPendingCommands = 0,
                FlashbackPlaybackThreadAlive = false, IsRecording = false, IsAudioEnabled = true
            }
        });

        public async ValueTask DisposeAsync()
        {
            Caller.Cancel();
            foreach (var reply in _replies) reply.TrySetCanceled(Caller.Token);
            try { await Task.WhenAll(_tasks).WaitAsync(Limit); }
            catch when (_tasks.All(task => task.IsCompleted)) { /* Outcomes are asserted before fixture disposal. */ }
            finally
            {
                Caller.Dispose();
                if (_tasks.All(task => task.IsCompleted)) Directory.Delete(_directory, recursive: true);
            }
        }
    }
}

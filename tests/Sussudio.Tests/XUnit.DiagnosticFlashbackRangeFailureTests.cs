using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class DiagnosticFlashbackRangeFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadinessFailureBeforeAnySelectionMutationDoesNotClaimCleanupOwnership(bool throwReadiness)
    {
        await using var fixture = new RangeFixture();
        var snapshotCount = 0;
        var sentinel = new IOException("readiness unavailable");
        fixture.Script = request =>
        {
            Assert.Equal("GetSnapshot", request.Command);
            if (throwReadiness) return Task.FromException<JsonElement>(sentinel);
            return ++snapshotCount == 1 ? fixture.DefaultReply(request)
                : Task.FromResult(JsonSerializer.SerializeToElement(new { Success = true, Snapshot = new { FlashbackBufferedDurationMs = 1000 } }));
        };
        if (throwReadiness)
            Assert.Same(sentinel, await Assert.ThrowsAsync<IOException>(() => fixture.Run().WaitAsync(RangeFixture.WaitLimit)));
        else
        {
            await fixture.Run().WaitAsync(RangeFixture.WaitLimit);
            Assert.Contains("insufficient near-live range headroom", Assert.Single(fixture.Warnings));
        }
        Assert.All(fixture.Requests, request => Assert.Equal("GetSnapshot", request.Command));
    }

    [Fact]
    public async Task FullRunnerPreservesTheExistingSessionButRestoresTheFailedRangeOperation()
    {
        await using var fixture = new RangeFixture();
        fixture.Script = request => request.Command == "FlashbackExport"
            ? Task.FromException<JsonElement>(new IOException("range failure retained in runner result"))
            : fixture.DefaultReply(request);
        var result = await fixture.RunFullRunnerAsync().WaitAsync(RangeFixture.WaitLimit);
        Assert.False((bool)result.GetType().GetProperty("Success")!.GetValue(result)!);
        Assert.Contains("range failure retained in runner result", (string)result.GetType().GetProperty("UnhandledException")!.GetValue(result)!);
        AssertCleanupCommands(fixture);
        Assert.False(fixture.SelectionMarked);
        Assert.Equal("Live", fixture.PlaybackState);
        Assert.DoesNotContain(fixture.Requests, request => request.Command is "SetPreviewEnabled" or "SetFlashbackEnabled");
    }

    [Theory]
    [InlineData("FlashbackExport")]
    [InlineData("VerifyFile")]
    [InlineData("result-snapshot")]
    [InlineData("set-out-point")]
    public async Task ThrownRangeOperationRestoresSelectionAndLiveStateWithoutReplacingTheFailure(string faultAt)
    {
        await using var fixture = new RangeFixture();
        var primary = new IOException("original range transport failure");
        var failed = false;
        fixture.Script = request =>
        {
            if (!failed && (request.Command == faultAt || request.Action == faultAt ||
                faultAt == "result-snapshot" && request.Command == "GetSnapshot" && fixture.ExportRequested))
            {
                failed = true;
                return Task.FromException<JsonElement>(primary);
            }
            return fixture.DefaultReply(request);
        };
        var error = await Assert.ThrowsAsync<IOException>(() => fixture.Run().WaitAsync(RangeFixture.WaitLimit));
        Assert.Same(primary, error);
        AssertCleanupCommands(fixture);
        Assert.False(fixture.SelectionMarked);
        Assert.Equal("Live", fixture.PlaybackState);
        Assert.Empty(fixture.Warnings);
    }

    [Theory]
    [InlineData("clear-in-out-points")]
    [InlineData("go-live")]
    public async Task CleanupTransportFailureIsReportedWithoutMaskingTheOriginalFailure(string failingCleanupAction)
    {
        await using var fixture = new RangeFixture();
        var primary = new IOException("original export failure");
        fixture.Script = request =>
        {
            if (request.Command == "FlashbackExport") return Task.FromException<JsonElement>(primary);
            if (fixture.ExportRequested && request.Action == failingCleanupAction)
                return Task.FromException<JsonElement>(new InvalidOperationException("cleanup failure sentinel"));
            return fixture.DefaultReply(request);
        };
        var error = await Assert.ThrowsAsync<IOException>(() => fixture.Run().WaitAsync(RangeFixture.WaitLimit));
        Assert.Same(primary, error);
        AssertCleanupCommands(fixture);
        Assert.Contains(failingCleanupAction, Assert.Single(fixture.Warnings));
        Assert.Contains("cleanup failure sentinel", fixture.Warnings[0]);
    }

    [Fact]
    public async Task CallerCancellationKeepsItsIdentityWhenTheSameSenderRejectsCleanup()
    {
        await using var fixture = new RangeFixture();
        using var caller = new CancellationTokenSource();
        var primary = new OperationCanceledException("original caller cancellation", caller.Token);
        fixture.Script = request =>
        {
            if (request.Command == "FlashbackExport")
            {
                caller.Cancel();
                return Task.FromException<JsonElement>(primary);
            }
            if (caller.IsCancellationRequested) return Task.FromCanceled<JsonElement>(caller.Token);
            return fixture.DefaultReply(request);
        };
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Run(token: caller.Token).WaitAsync(RangeFixture.WaitLimit));
        Assert.Same(primary, error);
        Assert.Equal(caller.Token, error.CancellationToken);
        AssertCleanupCommands(fixture);
        Assert.Equal(2, fixture.Warnings.Count);
        Assert.All(fixture.Warnings, warning => Assert.Contains("cleanup", warning));
    }

    [Fact]
    public async Task CleanupAttemptsGoLiveAfterClearThrowsAndThenRethrowsTheFirstCleanupFailure()
    {
        await using var fixture = new RangeFixture();
        var clearFailure = new IOException("clear failure");
        fixture.Script = request => fixture.ExportRequested && request.Action == "clear-in-out-points"
            ? Task.FromException<JsonElement>(clearFailure)
            : fixture.ExportRequested && request.Action == "go-live"
                ? Task.FromException<JsonElement>(new InvalidOperationException("live failure"))
                : fixture.DefaultReply(request);
        var error = await Assert.ThrowsAsync<IOException>(() => fixture.Run().WaitAsync(RangeFixture.WaitLimit));
        Assert.Same(clearFailure, error);
        AssertCleanupCommands(fixture);
        Assert.Equal(2, fixture.Warnings.Count);
        Assert.DoesNotContain("flashback range export cleared range and went live", fixture.Actions);
    }

    [Fact]
    public async Task ReturnedCleanupRejectionIsVisibleAndDoesNotClaimSuccessfulRestoration()
    {
        await using var fixture = new RangeFixture();
        fixture.Script = request => fixture.ExportRequested && request.Action == "clear-in-out-points"
            ? Task.FromResult(JsonSerializer.SerializeToElement(new { Success = false, Message = "clear rejected" }))
            : fixture.DefaultReply(request);
        await fixture.Run().WaitAsync(RangeFixture.WaitLimit);
        AssertCleanupCommands(fixture);
        Assert.Contains("clear rejected", Assert.Single(fixture.Warnings));
        Assert.DoesNotContain("flashback range export cleared range and went live", fixture.Actions);
    }

    [Fact]
    public async Task ExportFailureKeepsTheAudioSwitchOwnedUntilItsRestoreCompletes()
    {
        await using var fixture = new RangeFixture();
        var exportReply = fixture.NewReply();
        var toggleReply = fixture.NewReply();
        var toggleEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var restoreObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var primary = new IOException("export failed while audio command was outstanding");
        fixture.Script = request =>
        {
            if (request.Command == "FlashbackExport") return exportReply.Task;
            if (request.Command == "SetAudioEnabled")
            {
                if (!(bool)request.Payload!["enabled"]!)
                {
                    toggleEntered.TrySetResult();
                    return toggleReply.Task;
                }
                restoreObserved.TrySetResult();
            }
            return fixture.DefaultReply(request);
        };
        var scenario = fixture.Run(switchAudio: true);
        try
        {
            await toggleEntered.Task.WaitAsync(RangeFixture.WaitLimit);
            exportReply.SetException(primary);
            // A checkpoint on the scenario's continuation is observable through its
            // completion; a pending audio reply must keep that scenario owned.
            var completionOrBound = await Task.WhenAny(scenario, Task.Delay(100));
            Assert.NotSame(scenario, completionOrBound);
            Assert.DoesNotContain(fixture.Requests, request => request.Action == "go-live");
            toggleReply.SetResult(RangeFixture.Success());
            var error = await Assert.ThrowsAsync<IOException>(() => scenario.WaitAsync(RangeFixture.WaitLimit));
            Assert.Same(primary, error);
            Assert.True(restoreObserved.Task.IsCompleted);
            AssertCleanupCommands(fixture);
            var sequence = fixture.Requests.Select(request => request.Action ?? request.Command).ToArray();
            Assert.Equal(new[] { "SetAudioEnabled", "clear-in-out-points", "go-live" }, sequence.TakeLast(3));
        }
        finally
        {
            // Baseline intentionally fails the ownership assertion. Release its
            // pending sender and wait for the observable final audio restoration.
            exportReply.TrySetResult(RangeFixture.Success());
            toggleReply.TrySetResult(RangeFixture.Success());
            await restoreObserved.Task.WaitAsync(RangeFixture.WaitLimit);
            await fixture.WaitForAudioRestoreActionAsync();
        }
    }

    private static void AssertCleanupCommands(RangeFixture fixture) =>
        Assert.Equal(new[] { "clear-in-out-points", "go-live" }, fixture.Requests.Where(request => request.Command == "FlashbackAction").TakeLast(2).Select(request => request.Action));

    private sealed record Request(string Command, Dictionary<string, object?>? Payload)
    {
        internal string? Action => Payload?.GetValueOrDefault("action") as string;
    }

    private sealed class RangeFixture : IAsyncDisposable
    {
        internal static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(8);
        private readonly CancellationTokenSource _limit = new(TimeSpan.FromSeconds(15));
        private readonly List<Task> _tasks = new();
        private readonly List<TaskCompletionSource<JsonElement>> _replies = new();
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "Sussudio-range-failure-tests", Guid.NewGuid().ToString("N"));
        internal List<string> Actions { get; } = new();
        internal List<string> Warnings { get; } = new();
        internal ConcurrentQueue<Request> Requests { get; } = new();
        internal Func<Request, Task<JsonElement>>? Script { get; set; }
        internal bool ExportRequested { get; private set; }
        internal bool SelectionMarked { get; private set; }
        internal string PlaybackState { get; private set; } = "Live";
        private int _position;

        internal RangeFixture() => Directory.CreateDirectory(_directory);
        internal TaskCompletionSource<JsonElement> NewReply()
        {
            var reply = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _replies.Add(reply);
            return reply;
        }

        internal Task Run(bool switchAudio = false, CancellationToken token = default)
        {
            var method = global::Program.LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath)
                .GetType("Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios", true)!
                .GetMethod("RunFlashbackRangeExportAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sender = (command, payload, _) =>
            {
                var request = new Request(command, payload is null ? null : new(payload));
                Requests.Enqueue(request);
                if (command == "FlashbackExport") ExportRequested = true;
                return Script?.Invoke(request) ?? DefaultReply(request);
            };
            var task = (Task)method.Invoke(null, new object[] { _directory, Actions, Warnings, sender,
                token.CanBeCanceled ? token : _limit.Token, "flashback range export", "flashback-range-export.mp4", 5000, switchAudio })!;
            _tasks.Add(task);
            return task;
        }

        internal async Task<object> RunFullRunnerAsync()
        {
            var assembly = global::Program.LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions", true)!;
            var options = Activator.CreateInstance(optionsType)!;
            optionsType.GetProperty("Scenario")!.SetValue(options, "flashback-range-export");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, _directory);
            Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> sender = (command, payload, _, token) =>
            {
                token.ThrowIfCancellationRequested();
                var request = new Request(command, payload is null ? null : new(payload));
                Requests.Enqueue(request);
                if (command == "FlashbackExport") ExportRequested = true;
                return Script?.Invoke(request) ?? DefaultReply(request);
            };
            var method = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner", true)!.GetMethods()
                .Single(method => method.Name == "RunAsync" && method.GetParameters()[1].ParameterType == sender.GetType());
            var task = (Task)method.Invoke(null, new object[] { options, sender, _limit.Token })!;
            _tasks.Add(task);
            await task;
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }

        internal Task<JsonElement> DefaultReply(Request request)
        {
            switch (request.Action)
            {
                case "seek": _position = (int)request.Payload!["positionMs"]!; break;
                case "pause": PlaybackState = "Paused"; break;
                case "set-in-point": case "set-out-point": SelectionMarked = true; break;
                case "clear-in-out-points": SelectionMarked = false; break;
                case "go-live": PlaybackState = "Live"; break;
            }
            return Task.FromResult(request.Command == "GetSnapshot" ? JsonSerializer.SerializeToElement(new
            {
                Success = true,
                Snapshot = new
                {
                    IsPreviewing = true, FlashbackActive = true, FlashbackBufferedDurationMs = 30000, FlashbackEncodedFrames = 2400,
                    FlashbackPlaybackPositionMs = _position, FlashbackPlaybackState = PlaybackState,
                    FlashbackPlaybackPendingCommands = 0, FlashbackExportInPointMs = 20000, FlashbackExportOutPointMs = 25000,
                    FlashbackExportStatus = "Succeeded", IsAudioEnabled = true
                }
            }) : Success());
        }

        internal async Task WaitForAudioRestoreActionAsync()
        {
            using var wait = new CancellationTokenSource(WaitLimit);
            while (!Actions.Contains("flashback range export audio switch restored audio enabled to True"))
                await Task.Delay(10, wait.Token);
        }

        internal static JsonElement Success() => JsonSerializer.SerializeToElement(new { Success = true });

        public async ValueTask DisposeAsync()
        {
            _limit.Cancel();
            foreach (var reply in _replies) reply.TrySetCanceled(_limit.Token);
            try { await Task.WhenAll(_tasks).WaitAsync(WaitLimit); }
            catch when (_tasks.All(task => task.IsCompleted)) { /* Primary outcomes are asserted in the tests. */ }
            finally
            {
                _limit.Dispose();
                if (_tasks.All(task => task.IsCompleted)) Directory.Delete(_directory, recursive: true);
            }
        }
    }
}

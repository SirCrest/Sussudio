using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class DiagnosticFlashbackScenarioTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScrubBurstWaitsForEveryReplyBeforeEndingAtTheLastRequestedPosition(bool partialFailure)
    {
        await using var fixture = new ScenarioFixture();
        var allUpdatesEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replies = Enumerable.Range(0, 16).Select(_ => fixture.NewReply()).ToArray();
        var updateIndex = 0;
        var state = "Live";
        fixture.Script = request =>
        {
            if (request.Command == "GetSnapshot") return Reply(Snapshot(("FlashbackPlaybackState", state)));
            Assert.Equal("FlashbackAction", request.Command);
            switch (request.Action)
            {
                case "begin-scrub": state = "Scrubbing"; break;
                case "update-scrub":
                    var index = updateIndex++;
                    if (updateIndex == 16) allUpdatesEntered.TrySetResult();
                    return replies[index].Task;
                case "end-scrub": Assert.Equal(600, request.Payload!["positionMs"]); break;
                case "go-live": state = "Live"; break;
            }
            return Reply(Success());
        };

        var scenario = fixture.RunStress("RunFlashbackScrubStressAsync");
        await allUpdatesEntered.Task.WaitAsync(ScenarioFixture.WaitLimit);
        Assert.False(scenario.IsCompleted);
        Assert.DoesNotContain(fixture.Requests, request => request.Action == "end-scrub");
        Assert.Equal(new[] { 250, 500, 750, 1000, 1250, 1500, 1750, 2000, 2250, 2500, 2750, 3000, 2400, 1800, 1200, 600 },
            fixture.Requests.Where(request => request.Action == "update-scrub").Select(request => (int)request.Payload!["positionMs"]!));
        for (var index = 15; index > 0; index--)
            replies[index].SetResult(partialFailure && index is 7 or 13 ? Failure("update rejected") : Success());
        Assert.DoesNotContain(fixture.Requests, request => request.Action == "end-scrub");
        replies[0].SetResult(Success());
        await scenario.WaitAsync(ScenarioFixture.WaitLimit);

        Assert.Equal(new[] { "end-scrub", "play", "go-live" }, fixture.Requests.Where(request => request.Command == "FlashbackAction").TakeLast(3).Select(request => request.Action));
        if (partialFailure) Assert.Equal("flashback scrub stress: 2 update-scrub command(s) failed", Assert.Single(fixture.Warnings));
        else Assert.Empty(fixture.Warnings);
    }

    [Theory]
    [InlineData("begin-scrub")]
    [InlineData("end-scrub")]
    public async Task ScrubRejectionStopsTheDependentPlaybackActions(string rejectedAction)
    {
        await using var fixture = new ScenarioFixture();
        fixture.Script = request => Reply(request.Command == "GetSnapshot"
            ? Snapshot(("FlashbackPlaybackState", "Scrubbing"))
            : request.Action == rejectedAction ? Failure("sentinel rejection") : Success());
        await fixture.RunStress("RunFlashbackScrubStressAsync").WaitAsync(ScenarioFixture.WaitLimit);
        Assert.Contains(rejectedAction + " failed - sentinel rejection", Assert.Single(fixture.Warnings));
        Assert.DoesNotContain(fixture.Requests, request => request.Action is "play" or "go-live");
        if (rejectedAction == "begin-scrub")
            Assert.DoesNotContain(fixture.Requests, request => request.Action is "update-scrub" or "end-scrub");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StressUsesWarmedDeltasAndReportsConsequentialHealthFailures(bool unhealthy)
    {
        await using var fixture = new ScenarioFixture();
        var state = "Live";
        var playingSamples = 0;
        fixture.Script = request =>
        {
            if (request.Command == "GetSnapshot")
            {
                var warmed = state == "Playing" && ++playingSamples > 1;
                return Reply(Snapshot(
                    ("FlashbackPlaybackState", state),
                    ("FlashbackPlaybackFrameCount", warmed ? 700 : 100),
                    ("FlashbackPlaybackTargetFps", 60),
                    ("FlashbackPlaybackObservedFps", unhealthy ? 50 : 60),
                    ("FlashbackPlaybackOnePercentLowFps", unhealthy ? 40 : 55),
                    ("FlashbackPlaybackAudioMasterFallbacks", warmed && unhealthy ? 13 : 10),
                    ("FlashbackPlaybackAudioMasterUnavailableFallbacks", 10),
                    ("FlashbackPlaybackAudioMasterStaleFallbacks", warmed && unhealthy ? 3 : 0),
                    ("FlashbackPlaybackCommandsDropped", state == "Live" && playingSamples > 0 ? 23 : 20),
                    ("FlashbackPlaybackScrubUpdatesCoalesced", state == "Live" && playingSamples > 0 ? unhealthy ? 11 : 13 : 10),
                    ("FlashbackPlaybackMaxPendingCommands", unhealthy ? 5 : 4),
                    ("FlashbackPlaybackMaxCommandQueueLatencyMs", unhealthy ? 751 : 750)));
            }
            if (request.Command == "FlashbackAction")
            {
                if (request.Action == "play") state = "Playing";
                if (request.Action == "go-live") state = "Live";
            }
            else if (request.Command == "VerifyFile") AssertVerification(request);
            else Assert.Equal("FlashbackExport", request.Command);
            return Reply(Success());
        };
        await fixture.RunStress("RunFlashbackStressAsync", withOutput: true).WaitAsync(ScenarioFixture.WaitLimit);
        Assert.Equal(2, playingSamples);
        Assert.Contains(fixture.Actions, action => action.Contains("warmed frames=600", StringComparison.Ordinal));
        Assert.Equal(new[] { "FlashbackExport", "VerifyFile", "GetSnapshot" }, fixture.Requests.TakeLast(3).Select(request => request.Command));
        var export = Assert.Single(fixture.Requests, request => request.Command == "FlashbackExport");
        Assert.Equal(export.Payload!["outputPath"], Assert.Single(fixture.Requests, request => request.Command == "VerifyFile").Payload!["filePath"]);
        if (!unhealthy) Assert.Empty(fixture.Warnings);
        else
        {
            Assert.Equal(5, fixture.Warnings.Count);
            Assert.Contains(fixture.Warnings, warning => warning.Contains("observed FPS below floor", StringComparison.Ordinal));
            Assert.Contains(fixture.Warnings, warning => warning.Contains("1% low below floor", StringComparison.Ordinal));
            Assert.Contains(fixture.Warnings, warning => warning.Contains("staleDelta=3", StringComparison.Ordinal));
            Assert.Contains(fixture.Warnings, warning => warning.Contains("nonCoalescedDropped=2", StringComparison.Ordinal));
            Assert.Contains(fixture.Warnings, warning => warning.Contains("maxPending=5/4 maxLatencyMs=751/750", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentExportsKeepReplyIdentityAndPreserveExistingOutputArtifacts(bool failFirstExport)
    {
        await using var fixture = new ScenarioFixture();
        var occupiedFile = Path.Combine(fixture.OutputDirectory, "flashback-concurrent-a.mp4");
        var occupiedDirectory = Path.Combine(fixture.OutputDirectory, "flashback-concurrent-b.mp4");
        await File.WriteAllTextAsync(occupiedFile, "preserved existing output");
        Directory.CreateDirectory(occupiedDirectory);
        var replies = new[] { fixture.NewReply(), fixture.NewReply() };
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        fixture.Script = request =>
        {
            if (request.Command == "GetSnapshot") return Reply(Snapshot());
            if (request.Command == "FlashbackExport")
            {
                var index = count++;
                if (count == 2) bothEntered.TrySetResult();
                return replies[index].Task;
            }
            AssertVerification(request);
            return Reply(failFirstExport ? Failure("verification sentinel") : Success());
        };
        var scenario = fixture.RunExport("RunFlashbackExportConcurrentAsync");
        await bothEntered.Task.WaitAsync(ScenarioFixture.WaitLimit);
        Assert.False(scenario.IsCompleted);
        replies[1].SetResult(Success());
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "VerifyFile");
        replies[0].SetResult(failFirstExport ? Failure("export sentinel") : Success());
        await scenario.WaitAsync(ScenarioFixture.WaitLimit);

        var outputPaths = fixture.Requests.Where(request => request.Command == "FlashbackExport").Select(request => (string)request.Payload!["outputPath"]!).ToArray();
        Assert.Equal(2, outputPaths.Distinct().Count());
        Assert.All(outputPaths, path => Assert.Equal(fixture.OutputDirectory, Path.GetDirectoryName(path)));
        Assert.NotEqual(occupiedFile, outputPaths[0]);
        Assert.NotEqual(occupiedDirectory, outputPaths[1]);
        Assert.Equal("preserved existing output", await File.ReadAllTextAsync(occupiedFile));
        Assert.True(Directory.Exists(occupiedDirectory));
        Assert.Equal(failFirstExport ? outputPaths.Skip(1) : outputPaths,
            fixture.Requests.Where(request => request.Command == "VerifyFile").Select(request => (string)request.Payload!["filePath"]!));
        Assert.Equal(failFirstExport
            ? new[] { "flashback concurrent export a: export sentinel", "flashback concurrent export b verification: verification sentinel" }
            : Array.Empty<string>(), fixture.Warnings);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("export-failure")]
    [InlineData("missing-snapshot")]
    [InlineData("verification-failure")]
    public async Task RangeExportMarksNearLiveSelectionAndCleansUpAfterReturnedOutcomes(string outcome)
    {
        await using var fixture = new ScenarioFixture();
        var position = 0;
        var exported = false;
        var wentLive = false;
        fixture.Script = request =>
        {
            if (request.Command == "GetSnapshot")
                return Reply(exported && !wentLive && outcome == "missing-snapshot" ? Success() : Snapshot(
                    ("FlashbackPlaybackPositionMs", position), ("FlashbackExportInPointMs", 20000),
                    ("FlashbackExportOutPointMs", 25000), ("FlashbackExportStatus", "Succeeded")));
            if (request.Command == "FlashbackAction")
            {
                if (request.Action == "seek") position = (int)request.Payload!["positionMs"]!;
                if (request.Action == "go-live") wentLive = true;
                return Reply(Success());
            }
            if (request.Command == "FlashbackExport")
            {
                exported = true;
                Assert.Equal(true, request.Payload!["useSelectionRange"]);
                return Reply(outcome == "export-failure" ? Failure("range export sentinel") : Success());
            }
            AssertVerification(request);
            return Reply(outcome == "verification-failure" ? Failure("range verify sentinel") : Success());
        };
        await fixture.RunExport("RunFlashbackRangeExportAsync").WaitAsync(ScenarioFixture.WaitLimit);
        Assert.Equal(new[] { 20000, 25000 }, fixture.Requests.Where(request => request.Action == "seek").Select(request => (int)request.Payload!["positionMs"]!));
        Assert.Equal(new[] { "clear-in-out-points", "pause", "seek", "set-in-point", "seek", "set-out-point", "FlashbackExport" },
            fixture.Requests.Where(request => request.Command != "GetSnapshot").Take(7).Select(request => request.Action ?? request.Command));
        Assert.Equal(new[] { "clear-in-out-points", "go-live" }, fixture.Requests.Where(request => request.Command == "FlashbackAction").TakeLast(2).Select(request => request.Action));
        Assert.Equal(outcome == "export-failure" ? 0 : 1, fixture.Requests.Count(request => request.Command == "VerifyFile"));
        if (outcome == "success") Assert.Empty(fixture.Warnings);
        else Assert.Contains(outcome switch
        {
            "export-failure" => "export failed - range export sentinel",
            "missing-snapshot" => "no snapshot returned after export",
            _ => "verification: range verify sentinel"
        }, Assert.Single(fixture.Warnings));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RejectedExportChecksPersistedFailureAndRecordingPreservation(bool recording, bool inconsistent)
    {
        await using var fixture = new ScenarioFixture();
        var exported = false;
        fixture.Script = request =>
        {
            if (request.Command == "FlashbackExport")
            {
                Assert.True(request.AllowFailure);
                exported = true;
                return Reply(inconsistent ? Success() : Failure("expected rejection"));
            }
            Assert.Equal("GetSnapshot", request.Command);
            Assert.False(request.AllowFailure);
            return Reply(Snapshot(
                ("IsRecording", !exported || !inconsistent), ("RecordingBackend", "Flashback"), ("RecordingFileGrowing", true),
                ("FlashbackExportStatus", inconsistent ? "Succeeded" : "Failed"),
                ("FlashbackExportFailureKind", inconsistent ? "None" : recording ? "UnavailableDuringRecording" : "BufferInactive"),
                ("FlashbackExportMessage", inconsistent ? "wrong message" : recording
                    ? "Flashback export is unavailable while Flashback is the active recording backend"
                    : "Flashback buffer not active"),
                ("LastExportSuccess", inconsistent)));
        };
        await fixture.RunRejected(recording).WaitAsync(ScenarioFixture.WaitLimit);
        Assert.Single(fixture.Requests, request => request.Command == "FlashbackExport");
        if (!inconsistent) Assert.Empty(fixture.Warnings);
        else
        {
            Assert.Equal(recording ? 6 : 5, fixture.Warnings.Count);
            foreach (var text in new[] { "unexpectedly succeeded", "expected Failed status", "failure kind", "unexpected message", "LastExportSuccess=false" })
                Assert.Contains(fixture.Warnings, warning => warning.Contains(text, StringComparison.Ordinal));
            if (recording) Assert.Contains(fixture.Warnings, warning => warning.Contains("recording backend changed", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("success")]
    [InlineData("disable-failure")]
    [InlineData("worker-alive")]
    [InlineData("export-failure")]
    public async Task DisableDuringExportWaitsForBothOutcomesAndStillReenables(string outcome)
    {
        await using var fixture = new ScenarioFixture();
        var exportReply = fixture.NewReply();
        var disableReply = fixture.NewReply();
        var disableEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disabled = false;
        var reenabled = false;
        fixture.Script = request =>
        {
            if (request.Command == "GetSnapshot") return Reply(Snapshot(
                ("FlashbackActive", !disabled || reenabled),
                ("FlashbackPlaybackThreadAlive", disabled && !reenabled && outcome == "worker-alive"),
                ("FlashbackPlaybackPendingCommands", disabled && !reenabled && outcome == "worker-alive" ? 2 : 0)));
            if (request.Command == "FlashbackExport") return exportReply.Task;
            if (request.Command == "VerifyFile") { AssertVerification(request); return Reply(Success()); }
            Assert.Equal("SetFlashbackEnabled", request.Command);
            if ((bool)request.Payload!["enabled"]!) { reenabled = true; return Reply(Success()); }
            disabled = true;
            Assert.False(exportReply.Task.IsCompleted);
            disableEntered.TrySetResult();
            return disableReply.Task;
        };
        var scenario = fixture.RunExport("RunFlashbackDisableDuringExportAsync");
        await disableEntered.Task.WaitAsync(ScenarioFixture.WaitLimit);
        disableReply.SetResult(outcome == "disable-failure" ? Failure("disable sentinel") : Success());
        Assert.False(scenario.IsCompleted);
        Assert.DoesNotContain(fixture.Requests, request => request.Command == "VerifyFile");
        Assert.False(reenabled);
        exportReply.SetResult(outcome == "export-failure" ? Failure("export sentinel") : Success());
        await scenario.WaitAsync(ScenarioFixture.WaitLimit);
        Assert.True(reenabled);
        Assert.Equal(new[] { false, true }, fixture.Requests.Where(request => request.Command == "SetFlashbackEnabled").Select(request => (bool)request.Payload!["enabled"]!));
        Assert.Equal(outcome == "export-failure" ? 0 : 1, fixture.Requests.Count(request => request.Command == "VerifyFile"));
        var commands = fixture.Requests.Select(request => request.Command).ToArray();
        var expectedCommands = new List<string> { "GetSnapshot", "FlashbackExport", "SetFlashbackEnabled" };
        if (outcome != "export-failure") expectedCommands.Add("VerifyFile");
        if (outcome != "disable-failure") expectedCommands.Add("GetSnapshot");
        expectedCommands.AddRange(new[] { "SetFlashbackEnabled", "GetSnapshot" });
        Assert.Equal(expectedCommands, commands);
        if (outcome == "success") Assert.Empty(fixture.Warnings);
        else if (outcome == "worker-alive")
        {
            Assert.Equal(2, fixture.Warnings.Count);
            Assert.Contains(fixture.Warnings, warning => warning.Contains("worker still alive", StringComparison.Ordinal));
            Assert.Contains(fixture.Warnings, warning => warning.Contains("pending=2", StringComparison.Ordinal));
        }
        else Assert.Contains(outcome == "disable-failure" ? "disable failed - disable sentinel" : "export failed - export sentinel", Assert.Single(fixture.Warnings));
    }

    [Theory]
    [InlineData("export-failure")]
    [InlineData("missing-segments")]
    [InlineData("zero-segments")]
    [InlineData("verification-failure")]
    [InlineData("success")]
    public async Task RotatedExportPreservesExportAndVerificationFailureEvidence(string outcome)
    {
        await using var fixture = new ScenarioFixture();
        fixture.Script = request =>
        {
            if (request.Command == "GetSnapshot") return Reply(Snapshot());
            if (request.Command == "FlashbackExport")
            {
                Assert.Equal(12, request.Payload!["seconds"]);
                Assert.Equal(300000, request.TimeoutMs);
                return Reply(outcome == "export-failure" ? Failure("rotation sentinel") : Success(outcome switch
                {
                    "missing-segments" => "export complete",
                    "zero-segments" => "exported from 0 segments",
                    _ => "exported from 2 segments"
                }));
            }
            AssertVerification(request);
            Assert.Equal(120000, request.TimeoutMs);
            return Reply(outcome == "verification-failure" ? Failure("verification sentinel") : Success());
        };
        await fixture.RunExport("RunFlashbackRotatedExportAsync").WaitAsync(ScenarioFixture.WaitLimit);
        Assert.Equal(outcome == "export-failure" ? 0 : 1, fixture.Requests.Count(request => request.Command == "VerifyFile"));
        if (outcome == "success") Assert.Empty(fixture.Warnings);
        else Assert.Contains(outcome switch
        {
            "export-failure" => "export failed - rotation sentinel",
            "verification-failure" => "verification: verification sentinel",
            _ => "expected segment export"
        }, Assert.Single(fixture.Warnings));
        Assert.Equal(outcome is not ("export-failure" or "verification-failure"), fixture.Actions.Contains("flashback rotated export verified"));
    }

    private static void AssertVerification(Request request)
    {
        Assert.Equal("VerifyFile", request.Command);
        Assert.Equal(true, request.Payload!["strict"]);
        Assert.Equal("flashback-export", request.Payload["verificationProfile"]);
        Assert.False(string.IsNullOrWhiteSpace((string?)request.Payload["filePath"]));
    }

    private static JsonElement Snapshot(params (string Name, object? Value)[] values)
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["FlashbackActive"] = true,
            ["FlashbackBufferedDurationMs"] = 30000,
            ["FlashbackEncodedFrames"] = 2400,
            ["FlashbackPlaybackState"] = "Live",
            ["FlashbackPlaybackPendingCommands"] = 0
        };
        foreach (var (name, value) in values) snapshot[name] = value;
        return JsonSerializer.SerializeToElement(new { Success = true, Snapshot = snapshot });
    }

    private static JsonElement Success(string message = "ok") => JsonSerializer.SerializeToElement(new { Success = true, Message = message });
    private static JsonElement Failure(string message) => JsonSerializer.SerializeToElement(new { Success = false, Message = message });
    private static Task<JsonElement> Reply(JsonElement response) => Task.FromResult(response);

    private sealed record Request(string Command, Dictionary<string, object?>? Payload, int? TimeoutMs, bool AllowFailure = false)
    {
        internal string? Action => Payload?.GetValueOrDefault("action") as string;
    }

    private sealed class ScenarioFixture : IAsyncDisposable
    {
        internal static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(8);
        private readonly CancellationTokenSource _cancellation = new(TimeSpan.FromSeconds(15));
        private readonly List<TaskCompletionSource<JsonElement>> _replies = new();
        private readonly List<Task> _scenarios = new();
        private readonly Assembly _assembly = global::Program.LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
        internal string OutputDirectory { get; } = Path.Combine(Path.GetTempPath(), "Sussudio-diagnostic-scenario-tests", Guid.NewGuid().ToString("N"));
        internal List<string> Actions { get; } = new();
        internal List<string> Warnings { get; } = new();
        internal ConcurrentQueue<Request> Requests { get; } = new();
        internal Func<Request, Task<JsonElement>> Script { get; set; } = _ => throw new InvalidOperationException("No scripted response.");

        internal ScenarioFixture() => Directory.CreateDirectory(OutputDirectory);

        internal TaskCompletionSource<JsonElement> NewReply()
        {
            var reply = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _replies.Add(reply);
            return reply;
        }

        internal Task RunStress(string method, bool withOutput = false) => withOutput
            ? Invoke("DiagnosticSessionFlashbackStressScenario", method, OutputDirectory, Actions, Warnings, Sender, _cancellation.Token)
            : Invoke("DiagnosticSessionFlashbackStressScenario", method, Actions, Warnings, Sender, _cancellation.Token);

        internal Task RunExport(string method) => Invoke("DiagnosticSessionFlashbackExportScenarios", method,
            OutputDirectory, Actions, Warnings, Sender, _cancellation.Token);

        internal Task RunRejected(bool recording)
        {
            var kind = _assembly.GetType("Sussudio.Tools.DiagnosticSessionScenarioKind", throwOnError: true)!;
            var planType = _assembly.GetType("Sussudio.Tools.DiagnosticSessionScenarioPlan", throwOnError: true)!;
            var plan = Activator.CreateInstance(planType, Enum.Parse(kind, recording ? "FlashbackRecordingExportRejected" : "FlashbackExportRejected"))!;
            Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sender = Send;
            return Invoke("DiagnosticSessionFlashbackExportScenarios", "RunSelectedRejectedExportScenariosAsync",
                plan, OutputDirectory, Actions, Warnings, sender, _cancellation.Token);
        }

        private Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> Sender => (command, payload, timeoutMs) => Send(command, payload, timeoutMs, false);

        private Task<JsonElement> Send(string command, Dictionary<string, object?>? payload, int? timeoutMs, bool allowFailure)
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            var request = new Request(command, payload is null ? null : new(payload), timeoutMs, allowFailure);
            Requests.Enqueue(request);
            return Script(request);
        }

        private Task Invoke(string type, string name, params object?[] arguments)
        {
            var method = _assembly.GetType("Sussudio.Tools." + type, throwOnError: true)!
                .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
            var supplied = arguments.Concat(Enumerable.Repeat<object?>(Type.Missing, method.GetParameters().Length - arguments.Length)).ToArray();
            var task = (Task)method.Invoke(null, supplied)!;
            _scenarios.Add(task);
            return task;
        }

        public async ValueTask DisposeAsync()
        {
            _cancellation.Cancel();
            foreach (var reply in _replies) reply.TrySetCanceled(_cancellation.Token);
            try { await Task.WhenAll(_scenarios).WaitAsync(WaitLimit); }
            catch when (_scenarios.All(task => task.IsCompleted)) { /* The test has already asserted its primary outcome. */ }
            finally
            {
                _cancellation.Dispose();
                if (_scenarios.All(task => task.IsCompleted)) Directory.Delete(OutputDirectory, recursive: true);
            }
        }
    }
}

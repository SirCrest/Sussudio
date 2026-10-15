using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sussudio.Tools.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the perf experiment runner (docs/architecture/performance-telemetry.md sections 2.5 and 7).
// Written before the runner, per the testing policy. The runner changes live process state (environment-variable
// knobs, preview restarts, recording), so the dangerous failures are the ones that leave the app changed:
//
//  1. Bad spec: not JSON, missing name, no variants, duplicate variant names, repeats or seconds out of range, a knob
//     value that is not a scalar, an unknown verdict statistic, a misspelled field. All problems are reported
//     together, nothing is sent.
//  2. Unknown knob, wrong type, out-of-range value, app-restart knob (the runner cannot relaunch the app),
//     recording-start knob without a recording, flashback-restart knob while Flashback is off, preview not running,
//     recording requested while already recording: all rejected before ANY knob, preview, recording or window command.
//  3. Knobs are restored on success, including a knob that was already overridden before the experiment (restored by
//     value, not reset to launch) and a knob that was unset (restored by reset).
//  4. A command failing mid-run (window begin, knob set, preview restart, recording start, window end): knobs
//     restored, an open window canceled, a recording the runner started stopped, preview running again.
//  5. Cancellation (Ctrl-C, MCP cancel) during warmup or measure: same as 4, and the restore commands must not
//     inherit the canceled token or they would silently do nothing.
//  6. A restore command failing: the other knobs are still restored, the failure is reported and restore is not
//     claimed verified.
//  7. Run order: variants alternate direction per repeat so drift cancels (ABBA...). Variants that touch different
//     knobs: a knob set only by A must be back at baseline when B runs.
//  8. Preview readiness is read back from state, not trusted from the acknowledgement; a preview that never comes
//     back fails the run and the cleanup tries to start it again.
//  9. Verdicts: separated ranges are better/worse (lower is better), overlapping or touching ranges are
//     inconclusive, a single repeat is inconclusive, a metric with no samples in one variant is inconclusive.
// 10. Window names stay within the app's 64-character limit and carry no control characters.
// 11. The recording option brackets each measure window and is stopped even on failure.
// 12. An artifact write failure never changes the status or skips the knob restore.
public sealed class PerfExperimentRunnerTests : IDisposable
{
    private const string Depth = "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH";
    private const string Latency = "SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY";
    private const string Poll = "SUSSUDIO_DIAGNOSTICS_POLL_MS";
    private const string RecordingKnob = "SUSSUDIO_TEST_RECORDING_KNOB";
    private const string FlashbackKnob = "SUSSUDIO_TEST_FLASHBACK_KNOB";
    private const string AppKnob = "SUSSUDIO_TEST_APP_KNOB";
    private const string ChoiceKnob = "SUSSUDIO_TEST_CHOICE";
    private const string BoolKnob = "SUSSUDIO_MJPEG_LOAD_SHED";

    private readonly string _outputRoot = Path.Combine(Path.GetTempPath(), "sussudio-perf-experiment-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_outputRoot))
        {
            Directory.Delete(_outputRoot, recursive: true);
        }
    }

    // A fake automation endpoint with the state the runner depends on: knobs with apply scopes, a preview whose
    // readiness lags the acknowledgement, a recording flag, open performance windows and per-window reports that
    // depend on the knob values that were effective when the preview last started.
    private sealed class FakeApp
    {
        public sealed record Knob(string Name, string Type, string Default, double? Min, double? Max, string[]? Choices, string Apply);

        private static readonly Knob[] Catalog =
        {
            new(Depth, "int", "3", 1, 60, null, "preview-restart"),
            new(Latency, "int", "1", 1, 3, null, "preview-restart"),
            new(Poll, "int", "500", 100, 10000, null, "live"),
            new(RecordingKnob, "int", "8", 1, 64, null, "recording-start"),
            new(FlashbackKnob, "int", "4", 1, 16, null, "flashback-restart"),
            new(AppKnob, "int", "1000", 100, 10000, null, "app-restart"),
            new(ChoiceKnob, "choice", "Playback", null, null, new[] { "Playback", "Pro Audio" }, "preview-restart"),
            new(BoolKnob, "bool", "1", 0, 1, null, "preview-restart"),
        };

        private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _launch = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _effective = new(StringComparer.Ordinal);
        private readonly HashSet<string> _openWindows = new(StringComparer.Ordinal);
        private int _lagRemaining;
        private bool _reportedPreviewing = true;
        private int _windowsEnded;

        public FakeApp()
        {
            foreach (var knob in Catalog)
            {
                _values[knob.Name] = null;
                _launch[knob.Name] = null;
                _effective[knob.Name] = null;
            }
        }

        public List<(string Command, JsonObject Payload, bool TokenWasCanceled)> Calls { get; } = new();

        public List<TimeSpan> Delays { get; } = new();

        public List<string> Markers { get; } = new();

        public bool Previewing { get; private set; } = true;

        public bool Recording { get; set; }

        public bool FlashbackActive { get; set; }

        public int PreviewLagPolls { get; set; }

        public bool PreviewNeverReturns { get; set; }

        public int RecordingsStarted { get; private set; }

        public IReadOnlyCollection<string> OpenWindows => _openWindows;

        public Func<string, JsonObject, bool>? FailWhen { get; set; }

        public Func<string, JsonObject, bool>? ThrowWhen { get; set; }

        public Func<int, JsonObject>? ReportFactory { get; set; }

        public int CancelOnDelayNumber { get; set; }

        public CancellationTokenSource? CancelSource { get; set; }

        public string? Value(string name) => _values[name];

        public string? Effective(string name) => _effective[name];

        public void SeedKnob(string name, string? launch, string? current)
        {
            _launch[name] = launch;
            _values[name] = current;
            _effective[name] = current;
        }

        public IEnumerable<string> CommandNames => Calls.Select(c => c.Command);

        public bool Touched()
            => Calls.Any(c => c.Command is "SetTuningKnob" or "SetPreviewEnabled" or "SetRecordingEnabled"
                or "RestartFlashback" or "PerfWindow" or "AddPerfMarker");

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            if (CancelOnDelayNumber > 0 && Delays.Count == CancelOnDelayNumber)
            {
                CancelSource!.Cancel();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<JsonElement> SendAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            CancellationToken cancellationToken)
        {
            // A real pipe client does not connect when the token is already canceled.
            cancellationToken.ThrowIfCancellationRequested();
            var body = new JsonObject();
            foreach (var (key, value) in payload ?? new Dictionary<string, object?>())
            {
                body[key] = value switch
                {
                    null => null,
                    bool flag => flag,
                    int number => number,
                    _ => value.ToString(),
                };
            }

            Calls.Add((command, body, cancellationToken.IsCancellationRequested));
            if (ThrowWhen?.Invoke(command, body) == true)
            {
                throw new InvalidOperationException("injected transport failure for " + command);
            }

            if (FailWhen?.Invoke(command, body) == true)
            {
                return Task.FromResult(Parse(new JsonObject
                {
                    ["Success"] = false,
                    ["Message"] = "injected failure for " + command,
                    ["ErrorCode"] = "invalid-request",
                }));
            }

            return Task.FromResult(Parse(Handle(command, body)));
        }

        private static JsonElement Parse(JsonNode node)
        {
            using var document = JsonDocument.Parse(node.ToJsonString());
            return document.RootElement.Clone();
        }

        private static JsonObject Ok(JsonNode? data = null, JsonNode? snapshot = null)
        {
            var response = new JsonObject { ["Success"] = true, ["Message"] = "ok" };
            if (data is not null)
            {
                response["Data"] = data;
            }

            if (snapshot is not null)
            {
                response["Snapshot"] = snapshot;
            }

            return response;
        }

        private JsonObject Fail(string message) => new() { ["Success"] = false, ["Message"] = message, ["ErrorCode"] = "invalid-request" };

        private JsonObject Handle(string command, JsonObject body)
        {
            switch (command)
            {
                case "GetTuningKnobs":
                    return Ok(KnobListing());
                case "GetSnapshot":
                    if (_lagRemaining > 0)
                    {
                        _lagRemaining--;
                        if (_lagRemaining == 0)
                        {
                            _reportedPreviewing = Previewing;
                        }
                    }

                    return Ok(snapshot: SnapshotNode());
                case "SetTuningKnob":
                    return SetKnob(body);
                case "SetPreviewEnabled":
                {
                    var enabled = body["enabled"]!.GetValue<bool>();
                    if (enabled == Previewing)
                    {
                        return Ok(snapshot: SnapshotNode());
                    }

                    Previewing = enabled && !PreviewNeverReturns;
                    _lagRemaining = PreviewLagPolls;
                    if (_lagRemaining == 0)
                    {
                        _reportedPreviewing = Previewing;
                    }

                    if (enabled)
                    {
                        foreach (var knob in Catalog.Where(k => k.Apply is "preview-restart" or "live"))
                        {
                            _effective[knob.Name] = _values[knob.Name];
                        }
                    }

                    return Ok(snapshot: SnapshotNode());
                }

                case "SetRecordingEnabled":
                {
                    var enabled = body["enabled"]!.GetValue<bool>();
                    if (enabled && !Recording)
                    {
                        RecordingsStarted++;
                        foreach (var knob in Catalog.Where(k => k.Apply == "recording-start"))
                        {
                            _effective[knob.Name] = _values[knob.Name];
                        }
                    }

                    Recording = enabled;
                    return Ok(snapshot: SnapshotNode());
                }

                case "RestartFlashback":
                    foreach (var knob in Catalog.Where(k => k.Apply == "flashback-restart"))
                    {
                        _effective[knob.Name] = _values[knob.Name];
                    }

                    return Ok(snapshot: SnapshotNode());
                case "AddPerfMarker":
                    Markers.Add(body["text"]!.GetValue<string>());
                    return Ok();
                case "PerfWindow":
                    return Window(body);
                default:
                    return Fail("unexpected command " + command);
            }
        }

        private JsonObject Window(JsonObject body)
        {
            var action = body["action"]!.GetValue<string>();
            var name = body["name"]?.GetValue<string>() ?? string.Empty;
            switch (action)
            {
                case "begin":
                    _openWindows.Add(name);
                    return Ok(new JsonObject { ["Action"] = "begin", ["Name"] = name });
                case "end":
                    if (!_openWindows.Remove(name))
                    {
                        return Fail("Unknown performance window " + name);
                    }

                    return Ok(ReportFactory?.Invoke(_windowsEnded++) ?? DefaultReport(_windowsEnded++));
                case "cancel":
                    return _openWindows.Remove(name) ? Ok() : Fail("Unknown performance window " + name);
                default:
                    return Fail("unexpected window action " + action);
            }
        }

        private JsonObject SetKnob(JsonObject body)
        {
            var name = body["name"]!.GetValue<string>();
            var knob = Catalog.FirstOrDefault(k => k.Name == name);
            if (knob is null)
            {
                return Fail("Unknown tuning knob " + name);
            }

            string? next;
            if (body["reset"]?.GetValue<bool>() == true)
            {
                next = _launch[name];
            }
            else
            {
                next = body["value"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(next))
                {
                    return Fail(name + " needs a value");
                }

                if (knob.Type == "int" && (!long.TryParse(next, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
                    || number < knob.Min || number > knob.Max))
                {
                    return Fail(name + " out of range");
                }
            }

            var previous = _values[name];
            _values[name] = next;
            if (knob.Apply == "live")
            {
                _effective[name] = next;
            }

            return Ok(new JsonObject { ["Success"] = true, ["Name"] = name, ["Old"] = previous, ["New"] = next, ["Apply"] = knob.Apply });
        }

        private JsonObject SnapshotNode() => new()
        {
            ["IsPreviewing"] = _reportedPreviewing,
            ["IsRecording"] = Recording,
            ["FlashbackActive"] = FlashbackActive,
        };

        private JsonObject KnobListing()
        {
            var knobs = new JsonArray();
            foreach (var knob in Catalog)
            {
                var node = new JsonObject
                {
                    ["Name"] = knob.Name,
                    ["Type"] = knob.Type,
                    ["Default"] = knob.Default,
                    ["Min"] = knob.Min,
                    ["Max"] = knob.Max,
                    ["Apply"] = knob.Apply,
                    ["Owner"] = "Test.cs",
                    ["Description"] = "fake",
                    ["Value"] = _values[knob.Name],
                    ["Launch"] = _launch[knob.Name],
                    ["Overridden"] = !string.Equals(_values[knob.Name], _launch[knob.Name], StringComparison.Ordinal),
                };
                if (knob.Choices is not null)
                {
                    node["Choices"] = new JsonArray(knob.Choices.Select(c => (JsonNode?)JsonValue.Create(c)).ToArray());
                }

                knobs.Add(node);
            }

            return new JsonObject { ["Count"] = Catalog.Length, ["Overridden"] = 0, ["Knobs"] = knobs, ["Excluded"] = new JsonArray() };
        }

        private int EffectiveDepth() => int.Parse(_effective[Depth] ?? "3", CultureInfo.InvariantCulture);

        // present_interval p99 grows with the jitter depth; capture.deliver is independent of it and noisy, so its
        // repeat ranges overlap between variants.
        private JsonObject DefaultReport(int windowIndex)
        {
            var depth = EffectiveDepth();
            var presentP99 = 20.0 + 3.0 * depth + windowIndex % 3 * 0.1;
            var deliverP99 = 1.0 + windowIndex * 37 % 10 / 100.0;
            return new JsonObject
            {
                ["Window"] = "w",
                ["DurationMs"] = 20000.0,
                ["Metrics"] = new JsonArray(
                    MetricNode("preview.present_interval", 1200, 16.67, 16.67, presentP99, presentP99 + 2, presentP99 + 4),
                    MetricNode("capture.deliver", 1200, 0.4, 0.4, deliverP99, deliverP99 + 0.5, deliverP99 + 1.0)),
                ["Counters"] = new JsonArray(new JsonObject
                {
                    ["Name"] = "preview.drop.stale-before-render",
                    ["Delta"] = depth >= 3 ? 2 : 0,
                    ["RatePerSec"] = 0.1,
                }),
                ["Threads"] = new JsonArray(new JsonObject { ["Role"] = "preview.render", ["CpuPercent"] = 5.0 + depth }),
                ["Process"] = new JsonObject { ["CpuPercentOfOneCore"] = 20.0 + depth },
            };
        }

        public static JsonObject MetricNode(string name, long count, double mean, double p50, double p99, double p999, double max) => new()
        {
            ["Name"] = name,
            ["Unit"] = "ms",
            ["Count"] = count,
            ["RatePerSec"] = count / 20.0,
            ["Mean"] = mean,
            ["P50"] = p50,
            ["P90"] = (p50 + p99) / 2,
            ["P99"] = p99,
            ["P999"] = p999,
            ["Max"] = max,
            ["MaxApprox"] = false,
        };
    }

    private static string Spec(
        string variants,
        int repeats = 3,
        string extra = "")
        => "{\"name\":\"jitter depth\",\"warmupSeconds\":2,\"measureSeconds\":10,\"repeats\":" + repeats.ToString(CultureInfo.InvariantCulture)
            + extra + ",\"variants\":[" + variants + "]}";

    private const string BaselineAndDepth2 =
        "{\"name\":\"A\",\"knobs\":{}},{\"name\":\"B\",\"knobs\":{\"" + Depth + "\":\"2\"}}";

    private PerfExperimentOptions Options(FakeApp app, bool writeArtifacts = false) => new()
    {
        DelayAsync = app.DelayAsync,
        OutputDirectory = Path.Combine(_outputRoot, Guid.NewGuid().ToString("N")),
        WriteArtifacts = writeArtifacts,
        ReadyTimeout = TimeSpan.FromSeconds(5),
        ReadyPollInterval = TimeSpan.FromMilliseconds(250),
    };

    private Task<PerfExperimentResult> RunAsync(
        FakeApp app,
        string specJson,
        PerfExperimentOptions? options = null,
        CancellationToken cancellationToken = default)
        => PerfExperimentRunner.RunAsync(PerfExperimentSpec.Parse(specJson), app.SendAsync, options ?? Options(app), cancellationToken);

    private static void AssertKnobsAtBaseline(FakeApp app)
    {
        foreach (var name in new[] { Depth, Latency, Poll, RecordingKnob, FlashbackKnob, AppKnob, ChoiceKnob, BoolKnob })
        {
            Assert.Null(app.Value(name));
        }
    }

    // ---- spec parsing ------------------------------------------------------------------------------------------

    [Fact]
    public void AFullSpecParsesWithDefaultsAndNormalizedKnobValues()
    {
        var spec = PerfExperimentSpec.Parse(
            "{\"name\":\"depth\",\"variants\":[{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"X\":2,\"Y\":true,\"Z\":0.25,\"W\":\"Pro Audio\"}}]}");

        Assert.Equal("depth", spec.Name);
        Assert.Equal(5, spec.WarmupSeconds);
        Assert.Equal(20, spec.MeasureSeconds);
        Assert.Equal(3, spec.Repeats);
        Assert.False(spec.Recording);
        Assert.Empty(spec.Variants[0].Knobs);
        Assert.Equal("2", spec.Variants[1].Knobs["X"]);
        Assert.Equal("true", spec.Variants[1].Knobs["Y"]);
        Assert.Equal("0.25", spec.Variants[1].Knobs["Z"]);
        Assert.Equal("Pro Audio", spec.Variants[1].Knobs["W"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"name\":\"\",\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"variants\":[]}")]
    [InlineData("{\"name\":\"x\"}")]
    [InlineData("{\"name\":\"x\",\"variants\":[{\"name\":\"A\"},{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"variants\":[{\"name\":\" \"}]}")]
    [InlineData("{\"name\":\"x\",\"repeats\":0,\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"measureSeconds\":0,\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"warmupSeconds\":-1,\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"repeats\":\"many\",\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"variants\":[{\"name\":\"A\",\"knobs\":{\"K\":{\"nested\":1}}}]}")]
    [InlineData("{\"name\":\"x\",\"variants\":[{\"name\":\"A\",\"knobs\":[1]}]}")]
    [InlineData("{\"name\":\"x\",\"verdictStatistic\":\"p42\",\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"metrics\":\"preview.present_interval\",\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"repeat\":3,\"variants\":[{\"name\":\"A\"}]}")]
    [InlineData("{\"name\":\"x\",\"variants\":[{\"name\":\"A\",\"knob\":{}}]}")]
    public void ABadSpecIsRejectedWithProblemsBeforeAnythingRuns(string json)
    {
        var error = Assert.Throws<PerfExperimentSpecException>(() => PerfExperimentSpec.Parse(json));
        Assert.NotEmpty(error.Problems);
    }

    [Fact]
    public void AllProblemsInASpecAreReportedTogether()
    {
        var error = Assert.Throws<PerfExperimentSpecException>(() => PerfExperimentSpec.Parse(
            "{\"name\":\"\",\"repeats\":0,\"measureSeconds\":0,\"variants\":[{\"name\":\"A\"},{\"name\":\"A\"}]}"));
        Assert.True(error.Problems.Count >= 4, string.Join("; ", error.Problems));
    }

    // ---- schedule ---------------------------------------------------------------------------------------------

    [Fact]
    public void VariantsAlternateDirectionEachRepeat()
    {
        Assert.Equal(
            new[] { (0, 0), (0, 1), (1, 1), (1, 0), (2, 0), (2, 1) },
            PerfExperimentRunner.BuildSchedule(2, 3).Select(s => (s.Repeat, s.Variant)).ToArray());
        Assert.Equal(
            new[] { 0, 1, 2, 2, 1, 0 },
            PerfExperimentRunner.BuildSchedule(3, 2).Select(s => s.Variant).ToArray());
        Assert.Equal(new[] { 0, 0 }, PerfExperimentRunner.BuildSchedule(1, 2).Select(s => s.Variant).ToArray());
    }

    // ---- rejection before any knob is touched -----------------------------------------------------------------

    [Theory]
    [InlineData("SUSSUDIO_NOT_A_KNOB", "1", "SUSSUDIO_NOT_A_KNOB")]
    [InlineData(AppKnob, "2000", AppKnob)]
    [InlineData(RecordingKnob, "4", RecordingKnob)]
    [InlineData(Depth, "seven", Depth)]
    [InlineData(Depth, "999", Depth)]
    [InlineData(Latency, "0", Latency)]
    [InlineData(ChoiceKnob, "Nope", ChoiceKnob)]
    [InlineData(BoolKnob, "maybe", BoolKnob)]
    public async Task AnUnusableKnobIsRejectedBeforeAnyKnobPreviewOrWindowCommand(string knob, string value, string expectedInProblem)
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + knob + "\":\"" + value + "\"}}"));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        Assert.Contains(result.Problems, problem => problem.Contains(expectedInProblem, StringComparison.Ordinal));
        Assert.False(app.Touched(), "commands sent: " + string.Join(", ", app.CommandNames));
        AssertKnobsAtBaseline(app);
        Assert.Empty(result.Runs);
    }

    [Fact]
    public async Task AnAppRestartKnobIsRejectedBecauseTheRunnerCannotRelaunchTheApp()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + AppKnob + "\":\"2000\"}}"));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        var problem = Assert.Single(result.Problems, p => p.Contains(AppKnob, StringComparison.Ordinal));
        Assert.Contains("restart", problem, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot", problem, StringComparison.OrdinalIgnoreCase);
        Assert.False(app.Touched());
    }

    [Fact]
    public async Task ARecordingStartKnobNeedsARecordingInTheSpec()
    {
        var app = new FakeApp();
        var rejected = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + RecordingKnob + "\":\"4\"}}"));
        Assert.Equal(PerfExperimentStatus.Rejected, rejected.Status);
        Assert.False(app.Touched());

        var accepted = await RunAsync(
            app,
            Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + RecordingKnob + "\":\"4\"}}", repeats: 2, extra: ",\"recording\":true"));
        Assert.Equal(PerfExperimentStatus.Completed, accepted.Status);
        Assert.Equal(4, app.RecordingsStarted);
        Assert.Null(app.Value(RecordingKnob));
    }

    [Fact]
    public async Task APreviewThatIsNotRunningIsRejectedWithoutStartingIt()
    {
        var app = new FakeApp();
        await app.SendAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = false }, null, CancellationToken.None);
        app.Calls.Clear();

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        Assert.Contains(result.Problems, p => p.Contains("preview", StringComparison.OrdinalIgnoreCase));
        Assert.False(app.Touched());
    }

    [Fact]
    public async Task RecordingRequestedWhileTheAppIsAlreadyRecordingIsRejectedAndLeftRunning()
    {
        var app = new FakeApp { Recording = true };
        var result = await RunAsync(app, Spec(BaselineAndDepth2, extra: ",\"recording\":true"));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        Assert.Contains(result.Problems, p => p.Contains("recording", StringComparison.OrdinalIgnoreCase));
        Assert.False(app.Touched());
        Assert.True(app.Recording);
    }

    [Fact]
    public async Task AFlashbackRestartKnobIsRejectedWhileFlashbackIsOff()
    {
        var app = new FakeApp { FlashbackActive = false };
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + FlashbackKnob + "\":\"8\"}}"));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        Assert.Contains(result.Problems, p => p.Contains(FlashbackKnob, StringComparison.Ordinal));
        Assert.False(app.Touched());
    }

    // ---- the happy path ---------------------------------------------------------------------------------------

    [Fact]
    public async Task ARunInterleavesVariantsRestartsThePreviewEachRunAndRestoresEveryKnob()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(new[] { "A", "B", "B", "A", "A", "B" }, result.Runs.Select(r => r.Variant).ToArray());
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, result.Runs.Select(r => r.Repeat).ToArray());

        // Knob restored to its pre-experiment (unset) value, and the running pipeline picked that value up again.
        AssertKnobsAtBaseline(app);
        Assert.Null(app.Effective(Depth));
        Assert.True(app.Previewing);
        Assert.Empty(app.OpenWindows);
        Assert.True(result.Restore.Verified);
        Assert.Empty(result.Restore.Errors);

        // Every run restarts the preview (so a fresh start is compared with a fresh start) and brackets one window.
        var windowBegins = app.Calls.Where(c => c.Command == "PerfWindow" && c.Payload["action"]!.GetValue<string>() == "begin").ToList();
        var windowEnds = app.Calls.Where(c => c.Command == "PerfWindow" && c.Payload["action"]!.GetValue<string>() == "end").ToList();
        Assert.Equal(6, windowBegins.Count);
        Assert.Equal(6, windowEnds.Count);
        Assert.Equal(
            result.Runs.Select(r => r.Window),
            windowBegins.Select(c => c.Payload["name"]!.GetValue<string>()));
        Assert.Equal(6, app.Markers.Count(m => m.Contains("warmup", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(app.Markers, m => m.Contains("variant B run 2", StringComparison.Ordinal));

        // warmup then measure for every run.
        Assert.Equal(12, app.Delays.Count);
        Assert.Equal(new[] { 2.0, 10.0 }, app.Delays.Take(2).Select(d => d.TotalSeconds).ToArray());
    }

    [Fact]
    public async Task VerdictsCompareVariantsAgainstTheFirstWithLowerBeingBetter()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        var present = Assert.Single(result.Metrics, m => m.Name == "preview.present_interval");
        var baseline = present.Variants[0];
        var candidate = present.Variants[1];
        Assert.Equal("A", baseline.Variant);
        Assert.Equal("baseline", baseline.Verdict.Value);
        Assert.Equal(3, baseline.Runs);
        // Depth 2 lowers p99 by 3 ms against the default depth 3, ranges do not overlap: better.
        Assert.Equal("better", candidate.Verdict.Value);
        Assert.InRange(candidate.DeltaVsFirst["p99"].Absolute, -3.5, -2.5);
        Assert.True(candidate.Stats["p99"].Max < baseline.Stats["p99"].Min);
        Assert.Equal(3, candidate.Stats["p99"].Values.Count);

        // capture.deliver does not depend on the knob and is noisy: ranges overlap, so no claim is made.
        var deliver = Assert.Single(result.Metrics, m => m.Name == "capture.deliver");
        Assert.Equal("inconclusive", deliver.Variants[1].Verdict.Value);

        // Drop counters, process CPU and per-thread CPU are compared with the same machinery.
        var drops = Assert.Single(result.Metrics, m => m.Name == "preview.drop.stale-before-render");
        Assert.Equal("better", drops.Variants[1].Verdict.Value);
        Assert.Equal("better", Assert.Single(result.Metrics, m => m.Name == "process.cpu_percent").Variants[1].Verdict.Value);
        Assert.Contains(result.Metrics, m => m.Name == "thread.preview.render.cpu_percent");
    }

    [Fact]
    public async Task AFocusListLimitsTheComparedMetricsToThoseNames()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2, extra: ",\"metrics\":[\"preview.present_interval\"]"));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(new[] { "preview.present_interval" }, result.Metrics.Select(m => m.Name).ToArray());
    }

    [Fact]
    public async Task AFocusMetricWithNoSamplesAnywhereIsReportedNotInvented()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2, extra: ",\"metrics\":[\"preview.nonexistent\"]"));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("preview.nonexistent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ASingleVariantRunsAndCompareNothing()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec("{\"name\":\"only\"}", repeats: 2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(2, result.Runs.Count);
        Assert.All(result.Metrics, metric => Assert.Equal("baseline", Assert.Single(metric.Variants).Verdict.Value));
        Assert.Empty(app.Calls.Where(c => c.Command == "SetTuningKnob"));
    }

    [Fact]
    public async Task AKnobThatWasAlreadyOverriddenIsRestoredByValueNotReset()
    {
        var app = new FakeApp();
        app.SeedKnob(Depth, launch: null, current: "5");

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal("5", app.Value(Depth));
        Assert.Equal("5", app.Effective(Depth));
        Assert.True(result.Restore.Verified);
    }

    [Fact]
    public async Task AKnobSetByOnlyOneVariantIsBackAtBaselineWhenTheOtherVariantRuns()
    {
        var app = new FakeApp();
        var valuesAtWindowBegin = new List<(string Depth, string Latency)>();
        app.FailWhen = (command, payload) =>
        {
            if (command == "PerfWindow" && payload["action"]!.GetValue<string>() == "begin")
            {
                valuesAtWindowBegin.Add((app.Value(Depth) ?? "unset", app.Value(Latency) ?? "unset"));
            }

            return false;
        };

        var result = await RunAsync(
            app,
            Spec("{\"name\":\"A\",\"knobs\":{\"" + Depth + "\":\"2\"}},{\"name\":\"B\",\"knobs\":{\"" + Latency + "\":\"2\"}}", repeats: 2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        // Run order is A B B A: A has depth only, B has latency only; each is at baseline for the knob it does not set.
        Assert.Equal(
            new[] { ("2", "unset"), ("unset", "2"), ("unset", "2"), ("2", "unset") },
            valuesAtWindowBegin.ToArray());
        AssertKnobsAtBaseline(app);
    }

    [Fact]
    public async Task ALiveKnobIsChangedWithoutRestartingThePreview()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + Poll + "\":\"1000\"}}", repeats: 2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.DoesNotContain(app.Calls, c => c.Command == "SetPreviewEnabled");
        Assert.Null(app.Value(Poll));
        Assert.Null(app.Effective(Poll));
    }

    [Fact]
    public async Task AFlashbackRestartKnobRestartsFlashbackAndIsRestored()
    {
        var app = new FakeApp { FlashbackActive = true };
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + FlashbackKnob + "\":\"8\"}}", repeats: 2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Contains(app.Calls, c => c.Command == "RestartFlashback");
        Assert.DoesNotContain(app.Calls, c => c.Command == "SetPreviewEnabled");
        Assert.Null(app.Value(FlashbackKnob));
        Assert.Null(app.Effective(FlashbackKnob));
    }

    // ---- failure and cancellation always restore ---------------------------------------------------------------

    [Theory]
    [InlineData("begin")]
    [InlineData("end")]
    public async Task AWindowCommandFailingMidRunRestoresKnobsAndCancelsTheWindow(string failingAction)
    {
        var app = new FakeApp();
        var seen = 0;
        app.FailWhen = (command, payload) =>
            command == "PerfWindow" && payload["action"]!.GetValue<string>() == failingAction && ++seen == 3;

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Contains("PerfWindow", result.Error);
        Assert.Equal(2, result.Runs.Count); // the two runs that finished before the failure are kept
        AssertKnobsAtBaseline(app);
        Assert.Null(app.Effective(Depth));
        Assert.Empty(app.OpenWindows);
        Assert.True(app.Previewing);
        Assert.True(result.Restore.Verified);
    }

    [Fact]
    public async Task ATransportExceptionIsAFailureWithTheSameRestore()
    {
        var app = new FakeApp();
        var seen = 0;
        app.ThrowWhen = (command, _) => command == "AddPerfMarker" && ++seen == 4;

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Contains("injected transport failure", result.Error);
        AssertKnobsAtBaseline(app);
        Assert.Empty(app.OpenWindows);
    }

    [Fact]
    public async Task ASetKnobFailureMidRunStillLeavesEveryKnobAtBaseline()
    {
        var app = new FakeApp();
        // The second variant sets two knobs; the second set is refused after the first was applied.
        app.FailWhen = (command, payload) => command == "SetTuningKnob" && payload["name"]!.GetValue<string>() == Latency
            && payload["value"]?.GetValue<string>() == "2";

        var result = await RunAsync(
            app,
            Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + Depth + "\":\"2\",\"" + Latency + "\":\"2\"}}"));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        AssertKnobsAtBaseline(app);
        Assert.Null(app.Effective(Depth));
    }

    [Fact]
    public async Task ARecordingStartedByTheRunnerIsStoppedWhenARunFails()
    {
        var app = new FakeApp();
        var seen = 0;
        app.FailWhen = (command, payload) =>
            command == "PerfWindow" && payload["action"]!.GetValue<string>() == "end" && ++seen == 2;

        var result = await RunAsync(app, Spec(BaselineAndDepth2, extra: ",\"recording\":true"));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.False(app.Recording);
        Assert.Empty(app.OpenWindows);
        AssertKnobsAtBaseline(app);
    }

    [Fact]
    public async Task RecordingBracketsEveryMeasureWindowWhenRequested()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2, repeats: 2, extra: ",\"recording\":true"));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(4, app.RecordingsStarted);
        Assert.False(app.Recording);

        var names = app.Calls.Select(c => c.Command == "PerfWindow" ? "window-" + c.Payload["action"]!.GetValue<string>()
            : c.Command == "SetRecordingEnabled" ? (c.Payload["enabled"]!.GetValue<bool>() ? "rec-on" : "rec-off")
            : c.Command).ToList();
        var firstBegin = names.IndexOf("window-begin");
        var firstEnd = names.IndexOf("window-end");
        Assert.InRange(names.IndexOf("rec-on"), 0, firstBegin);
        Assert.True(names.IndexOf("rec-off") > firstEnd);
    }

    [Theory]
    [InlineData(1)] // during the first warmup
    [InlineData(2)] // during the first measure
    [InlineData(6)] // during a later run
    public async Task CancellationRestoresKnobsStopsRecordingAndRestoresWithAFreshToken(int cancelOnDelayNumber)
    {
        var app = new FakeApp();
        using var cts = new CancellationTokenSource();
        app.CancelSource = cts;
        app.CancelOnDelayNumber = cancelOnDelayNumber;

        var result = await RunAsync(
            app,
            Spec(BaselineAndDepth2, extra: ",\"recording\":true"),
            cancellationToken: cts.Token);

        Assert.Equal(PerfExperimentStatus.Canceled, result.Status);
        AssertKnobsAtBaseline(app);
        Assert.Null(app.Effective(Depth));
        Assert.False(app.Recording);
        Assert.Empty(app.OpenWindows);
        Assert.True(app.Previewing);
        Assert.True(result.Restore.Verified);

        // Every command sent after the cancellation (the restore) was sent with a token that is not canceled.
        Assert.DoesNotContain(app.Calls, c => c.TokenWasCanceled);
        Assert.Contains(app.Calls, c => c.Command == "SetTuningKnob");
    }

    [Fact]
    public async Task AFailedRestoreCommandIsReportedAndTheOtherKnobsAreStillRestored()
    {
        var app = new FakeApp();
        var seen = 0;
        // Fail the run, then refuse to reset Depth during the restore.
        app.FailWhen = (command, payload) =>
        {
            if (command == "PerfWindow" && payload["action"]!.GetValue<string>() == "begin" && ++seen == 2)
            {
                return true;
            }

            return command == "SetTuningKnob" && payload["name"]!.GetValue<string>() == Depth && payload["reset"]?.GetValue<bool>() == true;
        };

        var result = await RunAsync(
            app,
            Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + Depth + "\":\"2\",\"" + Latency + "\":\"2\"}}"));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.False(result.Restore.Verified);
        Assert.Contains(result.Restore.Errors, e => e.Contains(Depth, StringComparison.Ordinal));
        Assert.Contains(result.Restore.Mismatches, m => m.Contains(Depth, StringComparison.Ordinal));
        Assert.Null(app.Value(Latency)); // the other knob was still restored
        Assert.Equal("2", app.Value(Depth));
    }

    // ---- preview readiness ------------------------------------------------------------------------------------

    [Fact]
    public async Task PreviewReadinessIsReadBackFromStateNotTrustedFromTheAcknowledgement()
    {
        var app = new FakeApp { PreviewLagPolls = 3 };
        var result = await RunAsync(app, Spec(BaselineAndDepth2, repeats: 1));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        var names = app.Calls.Select(c => c.Command == "SetPreviewEnabled"
            ? (c.Payload["enabled"]!.GetValue<bool>() ? "preview-on" : "preview-off")
            : c.Command == "PerfWindow" ? "window-" + c.Payload["action"]!.GetValue<string>()
            : c.Command).ToList();
        for (var i = 0; i < names.Count; i++)
        {
            if (names[i] != "window-begin")
            {
                continue;
            }

            var lastOn = names.LastIndexOf("preview-on", i);
            var polls = names.Skip(lastOn + 1).Take(i - lastOn - 1).Count(n => n == "GetSnapshot");
            Assert.True(polls >= 3, $"only {polls} state polls between preview start and measurement");
        }
    }

    [Fact]
    public async Task APreviewThatNeverComesBackFailsTheRunAndCleanupTriesToStartItAgain()
    {
        var app = new FakeApp();
        var restartAttempts = 0;
        app.FailWhen = (command, payload) =>
        {
            if (command == "SetPreviewEnabled" && payload["enabled"]!.GetValue<bool>() && ++restartAttempts == 1)
            {
                app.PreviewNeverReturns = true;
            }

            return false;
        };

        var result = await RunAsync(app, Spec(BaselineAndDepth2, repeats: 1));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Contains("preview", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(app.Calls.Count(c => c.Command == "SetPreviewEnabled" && c.Payload["enabled"]!.GetValue<bool>()) >= 2);
        AssertKnobsAtBaseline(app);
    }

    // ---- verdict rules ----------------------------------------------------------------------------------------

    [Fact]
    public void SeparatedRangesAreBetterOrWorseAndLowerIsBetter()
    {
        Assert.Equal("better", PerfExperimentStatistics.Judge(new[] { 30.0, 31, 32 }, new[] { 20.0, 21, 22 }, "p99").Value);
        Assert.Equal("worse", PerfExperimentStatistics.Judge(new[] { 20.0, 21, 22 }, new[] { 30.0, 31, 32 }, "p99").Value);
    }

    [Theory]
    [InlineData(new[] { 20.0, 25.0 }, new[] { 24.0, 30.0 })] // overlap
    [InlineData(new[] { 20.0, 25.0 }, new[] { 25.0, 30.0 })] // touching ranges do not separate
    [InlineData(new[] { 20.0, 21.0 }, new[] { 20.5 })]      // a single repeat cannot support a claim
    [InlineData(new[] { 20.0 }, new[] { 30.0 })]
    [InlineData(new double[0], new[] { 30.0, 31.0 })]
    public void OverlappingTouchingOrThinEvidenceIsInconclusive(double[] baseline, double[] candidate)
    {
        var verdict = PerfExperimentStatistics.Judge(baseline, candidate, "p99");
        Assert.Equal("inconclusive", verdict.Value);
        Assert.False(string.IsNullOrWhiteSpace(verdict.Reason));
    }

    [Fact]
    public async Task AMetricWithNoSamplesInOneVariantIsInconclusiveNotCompared()
    {
        var app = new FakeApp();
        var window = 0;
        app.ReportFactory = _ =>
        {
            // Variant B runs (windows 1, 2 and 5 in A B B A A B order) never record capture.deliver.
            var index = window++;
            var report = new JsonObject
            {
                ["Metrics"] = new JsonArray(
                    FakeApp.MetricNode("preview.present_interval", 1000, 16.7, 16.7, 20 + index % 2, 22, 24)),
                ["Counters"] = new JsonArray(),
                ["Threads"] = new JsonArray(),
                ["Process"] = new JsonObject { ["CpuPercentOfOneCore"] = 20.0 },
            };
            if (index is 0 or 3 or 4)
            {
                ((JsonArray)report["Metrics"]!).Add(FakeApp.MetricNode("capture.deliver", 1000, 0.4, 0.4, 1.0, 1.5, 2.0));
            }

            return report;
        };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        var deliver = Assert.Single(result.Metrics, m => m.Name == "capture.deliver");
        Assert.Equal("inconclusive", deliver.Variants[1].Verdict.Value);
        Assert.Contains("no samples", deliver.Variants[1].Verdict.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AMalformedWindowReportIsATolerableGapNotACrash()
    {
        var app = new FakeApp();
        var window = 0;
        app.ReportFactory = _ => window++ == 1
            ? new JsonObject { ["unexpected"] = "shape" }
            : new JsonObject
            {
                ["Metrics"] = new JsonArray(FakeApp.MetricNode("preview.present_interval", 1000, 16.7, 16.7, 20.0 + window, 22, 24)),
                ["Process"] = new JsonObject { ["CpuPercentOfOneCore"] = 20.0 },
            };

        var result = await RunAsync(app, Spec(BaselineAndDepth2, repeats: 2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("no metrics", StringComparison.OrdinalIgnoreCase));
        AssertKnobsAtBaseline(app);
    }

    // ---- naming, artifacts, output ----------------------------------------------------------------------------

    [Fact]
    public async Task WindowNamesStayWithinTheAppLimitAndHaveNoControlCharacters()
    {
        var app = new FakeApp();
        var longName = new string('x', 90);
        var spec = "{\"name\":\"" + longName + "\",\"warmupSeconds\":0,\"measureSeconds\":1,\"repeats\":1,\"variants\":[{\"name\":\"" + longName
            + "\"},{\"name\":\"tab\\there\"}]}";

        var result = await RunAsync(app, spec);

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.All(result.Runs, run =>
        {
            Assert.InRange(run.Window.Length, 1, 64);
            Assert.DoesNotContain(run.Window, char.IsControl);
        });
        Assert.Equal(result.Runs.Count, result.Runs.Select(r => r.Window).Distinct().Count());
    }

    [Fact]
    public async Task ArtifactsAreWrittenAndTheResultJsonParsesBackToTheSameStatus()
    {
        var app = new FakeApp();
        var options = Options(app, writeArtifacts: true);
        var result = await RunAsync(app, Spec(BaselineAndDepth2, repeats: 2), options);

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(options.OutputDirectory, result.OutputDirectory);

        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.OutputDirectory!, "result.json")));
        Assert.Equal("completed", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(4, json.RootElement.GetProperty("runs").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("restore").GetProperty("verified").GetBoolean());
        Assert.Contains("preview.present_interval", File.ReadAllText(Path.Combine(result.OutputDirectory!, "summary.md")));
        foreach (var run in result.Runs)
        {
            using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.OutputDirectory!, run.ReportFile!)));
            Assert.True(report.RootElement.TryGetProperty("Metrics", out _));
        }
    }

    [Fact]
    public async Task AnUnwritableOutputDirectoryDoesNotChangeTheStatusOrSkipTheRestore()
    {
        var app = new FakeApp();
        var blocker = Path.Combine(_outputRoot, "blocker");
        Directory.CreateDirectory(_outputRoot);
        File.WriteAllText(blocker, "a file where the output directory should be");
        var options = new PerfExperimentOptions
        {
            DelayAsync = app.DelayAsync,
            OutputDirectory = Path.Combine(blocker, "nested"),
            WriteArtifacts = true,
        };

        var result = await RunAsync(app, Spec(BaselineAndDepth2, repeats: 1), options);

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("artifact", StringComparison.OrdinalIgnoreCase));
        AssertKnobsAtBaseline(app);
    }

    [Fact]
    public async Task TheSummaryNamesTheVerdictsAndTheMethod()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2));
        var summary = result.FormatSummary();

        Assert.Contains("preview.present_interval", summary);
        Assert.Contains("better", summary);
        Assert.Contains("inconclusive", summary);
        Assert.Contains("lower is better", summary, StringComparison.OrdinalIgnoreCase);
    }
}

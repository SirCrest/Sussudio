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
// 13. The shipped example spec drifting from the real knob catalog (a renamed knob, a value the knob now rejects, a
//     knob that needs an app restart): it would only fail at preflight on a live app, so check it offline against the
//     real catalog and the real store validation.
//
// The experiment lease (section 2.12; the app side is in PerfExperimentLeaseTests), written before the runner used it:
//
// 14. Another experiment holds the lease: the run is rejected naming that experiment, before any knob, preview,
//     recording or window command, and no release is sent for a lease this runner never held.
// 15. The acquire reached the app but its reply was lost: the runner releases its own id so the orphaned lease does
//     not block the next experiment until it expires.
// 16. The lease is lost mid-run (the runner stalled past the TTL, or an operator forced it off): the next renew is
//     refused, the run fails naming why, and a window that was open when it was lost is not used. The restore still
//     runs when nobody else holds the lease; when another experiment does, the runner leaves its knobs, preview and
//     recording alone and says so.
// 17. Long warmup and measure waits renew the lease before its TTL lapses (virtual time).
// 18. Every knob change and restore carries the lease id, or the app would refuse the runner's own changes.
// 19. The lease is released after the restore whether the run completed, failed or was canceled, on the restore's
//     fresh token; a failed release is reported, not hidden.
// 20. Baselines come from the acquire response, read by the app under the lease, so a knob changed by hand between
//     preflight and the lease is restored to the value it had when the lease began.
//
// A knob several parts of the pipeline read (Apply pipeline-restart: HDR force-off), written before the runner modeled
// it:
//
// 21. Preview, Flashback and a recording each read the knob when they start. A run that restarts only the preview
//     measures a Flashback still running with the previous variant's value, and a restore leaves Flashback on the
//     experiment value. Every run must start each part that is running on the variant's value, and the restore must
//     restart the same parts.
// 22. A recording the runner did not start holds the capture, so restarting the preview does not rebuild it: rejected
//     before anything changes.
public sealed class PerfExperimentRunnerTests : IDisposable
{
    private const string Depth = "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH";
    private const string Latency = "SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY";
    private const string Poll = "SUSSUDIO_DIAGNOSTICS_POLL_MS";
    private const string RecordingKnob = "SUSSUDIO_TEST_RECORDING_KNOB";
    private const string FlashbackKnob = "SUSSUDIO_TEST_FLASHBACK_KNOB";
    private const string PipelineKnob = "SUSSUDIO_TEST_PIPELINE_KNOB";
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
            new(PipelineKnob, "bool", "0", 0, 1, null, "pipeline-restart"),
            new(AppKnob, "int", "1000", 100, 10000, null, "app-restart"),
            new(ChoiceKnob, "choice", "Playback", null, null, new[] { "Playback", "Pro Audio" }, "preview-restart"),
            new(BoolKnob, "bool", "1", 0, 1, null, "preview-restart"),
        };

        private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _launch = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _effective = new(StringComparer.Ordinal);
        private readonly HashSet<string> _openWindows = new(StringComparer.Ordinal);

        // The pipeline knob as each part of the pipeline last read it. Each part reads it when it starts; a preview start
        // does not while a recording holds the capture, because the app then reattaches the preview to that capture.
        private readonly Dictionary<string, string?> _pipelineReadBy = new(StringComparer.Ordinal)
        {
            ["preview"] = null,
            ["flashback"] = null,
            ["recording"] = null,
        };

        private int _lagRemaining;
        private bool _reportedPreviewing = true;
        private int _windowsEnded;

        // The lease, enforced like the app does: knobs it names refuse changes without its id, and it lapses when
        // virtual time (the sum of the runner's delays) passes its expiry, restoring the knobs it captured.
        private string? _leaseId;
        private string[] _leaseKnobs = Array.Empty<string>();
        private readonly Dictionary<string, string?> _leaseBaseline = new(StringComparer.Ordinal);
        private TimeSpan _leaseExpiresAt;

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

        public int StopsToIgnore { get; set; }

        public IReadOnlyCollection<string> OpenWindows => _openWindows;

        public Func<string, JsonObject, bool>? FailWhen { get; set; }

        public Func<string, JsonObject, bool>? ThrowWhen { get; set; }

        public Func<int, JsonObject>? ReportFactory { get; set; }

        public int CancelOnDelayNumber { get; set; }

        public CancellationTokenSource? CancelSource { get; set; }

        /// <summary>Another experiment's holder text while it holds the lease, with the knobs it leased.</summary>
        public string? ForeignHolder { get; set; }

        public string[] ForeignKnobs { get; set; } = Array.Empty<string>();

        /// <summary>When the delay with this 1-based number runs, the runner's lease is lost: expired, or taken over.</summary>
        public int LoseLeaseOnDelayNumber { get; set; }

        /// <summary>Runs on the lost lease: null for an expiry the app restored, or another experiment taking it.</summary>
        public Action<FakeApp>? OnLeaseLost { get; set; }

        /// <summary>Runs inside the acquire, before the app captures the baseline.</summary>
        public Action<FakeApp>? BeforeAcquire { get; set; }

        /// <summary>The app handles the command and then the reply is lost.</summary>
        public Func<string, JsonObject, bool>? LoseReplyWhen { get; set; }

        public string? LeaseId => _leaseId;

        public TimeSpan VirtualNow => Delays.Aggregate(TimeSpan.Zero, (sum, delay) => sum + delay);

        /// <summary>Virtual times at which the runner took or renewed its lease.</summary>
        public List<TimeSpan> LeaseRefreshes { get; } = new();

        public IEnumerable<JsonObject> LeaseCalls(string action)
            => Calls.Where(c => c.Command == "PerfExperimentLease" && c.Payload["action"]?.GetValue<string>() == action).Select(c => c.Payload);

        public void SetValue(string name, string? value) => _values[name] = value;

        public string? Value(string name) => _values[name];

        public string? Effective(string name) => _effective[name];

        /// <summary>The pipeline knob as <paramref name="part"/> (preview, flashback or recording) last read it.</summary>
        public string? PipelineValueIn(string part) => _pipelineReadBy[part];

        /// <summary>What each part was running with when each performance window began, in begin order.</summary>
        public List<(string? Preview, string? Flashback, string? Recording)> PipelineAtWindowBegin { get; } = new();

        public void SeedKnob(string name, string? launch, string? current)
        {
            _launch[name] = launch;
            _values[name] = current;
            _effective[name] = current;
        }

        public IEnumerable<string> CommandNames => Calls.Select(c => c.Command);

        public bool Touched()
            => Calls.Any(c => c.Command is "SetTuningKnob" or "SetPreviewEnabled" or "SetRecordingEnabled"
                or "RestartFlashback" or "PerfWindow" or "AddPerfMarker" or "PerfExperimentLease");

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            if (CancelOnDelayNumber > 0 && Delays.Count == CancelOnDelayNumber)
            {
                CancelSource!.Cancel();
            }

            if (LoseLeaseOnDelayNumber > 0 && Delays.Count == LoseLeaseOnDelayNumber && _leaseId is not null)
            {
                ExpireLease();
                OnLeaseLost?.Invoke(this);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private void ExpireLease()
        {
            foreach (var (name, value) in _leaseBaseline)
            {
                _values[name] = value;
            }

            _leaseId = null;
            _leaseKnobs = Array.Empty<string>();
            _leaseBaseline.Clear();
        }

        private JsonObject Lease(JsonObject body)
        {
            var action = body["action"]!.GetValue<string>();
            var id = body["lease"]?.GetValue<string>();
            if (_leaseId is not null && VirtualNow > _leaseExpiresAt)
            {
                ExpireLease();
            }

            JsonObject Holder(string holder) => new() { ["Holder"] = holder };
            switch (action)
            {
                case "acquire":
                    if (ForeignHolder is not null || (_leaseId is not null && _leaseId != id))
                    {
                        var holder = ForeignHolder ?? "another runner";
                        return new JsonObject
                        {
                            ["Success"] = false,
                            ["Message"] = $"Another perf experiment holds the tuning-knob lease: {holder}.",
                            ["ErrorCode"] = "invalid-state",
                            ["Data"] = new JsonObject { ["Active"] = Holder(holder) },
                        };
                    }

                    BeforeAcquire?.Invoke(this);
                    var ttl = body["ttlSeconds"]!.GetValue<int>();
                    var knobs = (body["knobs"] as JsonArray ?? new JsonArray()).Select(n => n!.GetValue<string>()).ToArray();
                    var baseline = new JsonArray();
                    if (_leaseId != id)
                    {
                        _leaseBaseline.Clear();
                        foreach (var knob in knobs)
                        {
                            _leaseBaseline[knob] = _values[knob];
                        }
                    }

                    foreach (var (name, value) in _leaseBaseline)
                    {
                        baseline.Add(new JsonObject { ["Name"] = name, ["Value"] = value, ["Launch"] = _launch[name] });
                    }

                    _leaseId = id;
                    _leaseKnobs = knobs;
                    _leaseExpiresAt = VirtualNow + TimeSpan.FromSeconds(ttl);
                    LeaseRefreshes.Add(VirtualNow);
                    return Ok(new JsonObject { ["Active"] = Holder(body["holder"]!.GetValue<string>()), ["Baseline"] = baseline });
                case "renew":
                    if (_leaseId is null || _leaseId != id)
                    {
                        return new JsonObject
                        {
                            ["Success"] = false,
                            ["Message"] = "The lease expired and the app restored its knobs.",
                            ["ErrorCode"] = "invalid-state",
                        };
                    }

                    _leaseExpiresAt = VirtualNow + TimeSpan.FromSeconds(body["ttlSeconds"]?.GetValue<int>() ?? 60);
                    LeaseRefreshes.Add(VirtualNow);
                    return Ok(new JsonObject { ["Active"] = Holder("runner") });
                case "release":
                    if (_leaseId is not null && _leaseId == id)
                    {
                        _leaseId = null;
                        _leaseKnobs = Array.Empty<string>();
                        _leaseBaseline.Clear();
                    }

                    return Ok(new JsonObject());
                case "status":
                    return Ok(new JsonObject
                    {
                        ["Active"] = ForeignHolder is not null ? Holder(ForeignHolder) : _leaseId is not null ? Holder("runner") : null,
                    });
                default:
                    return Fail("unexpected lease action " + action);
            }
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
                    IEnumerable<string> list => new JsonArray(list.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray()),
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

            var reply = Handle(command, body);
            if (LoseReplyWhen?.Invoke(command, body) == true)
            {
                throw new InvalidOperationException("injected lost reply for " + command);
            }

            return Task.FromResult(Parse(reply));
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

                        if (!Recording)
                        {
                            _pipelineReadBy["preview"] = _values[PipelineKnob];
                        }
                    }

                    return Ok(snapshot: SnapshotNode());
                }

                case "SetRecordingEnabled":
                {
                    var enabled = body["enabled"]!.GetValue<bool>();
                    if (!enabled && StopsToIgnore > 0)
                    {
                        // Acknowledged, but the recording keeps running and the reply's snapshot says so.
                        StopsToIgnore--;
                        return Ok(snapshot: SnapshotNode());
                    }

                    if (enabled && !Recording)
                    {
                        RecordingsStarted++;
                        foreach (var knob in Catalog.Where(k => k.Apply == "recording-start"))
                        {
                            _effective[knob.Name] = _values[knob.Name];
                        }

                        _pipelineReadBy["recording"] = _values[PipelineKnob];
                    }

                    Recording = enabled;
                    return Ok(snapshot: SnapshotNode());
                }

                case "RestartFlashback":
                    foreach (var knob in Catalog.Where(k => k.Apply == "flashback-restart"))
                    {
                        _effective[knob.Name] = _values[knob.Name];
                    }

                    _pipelineReadBy["flashback"] = _values[PipelineKnob];
                    return Ok(snapshot: SnapshotNode());
                case "AddPerfMarker":
                    Markers.Add(body["text"]!.GetValue<string>());
                    return Ok();
                case "PerfWindow":
                    return Window(body);
                case "PerfExperimentLease":
                    return Lease(body);
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
                    PipelineAtWindowBegin.Add((_pipelineReadBy["preview"], _pipelineReadBy["flashback"], _pipelineReadBy["recording"]));
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

            if (_leaseId is not null && VirtualNow > _leaseExpiresAt)
            {
                ExpireLease();
            }

            var leaseId = body["lease"]?.GetValue<string>();
            if ((ForeignHolder is not null && ForeignKnobs.Contains(name))
                || (_leaseId is not null && _leaseKnobs.Contains(name) && leaseId != _leaseId))
            {
                return Fail(name + " is leased to " + (ForeignHolder ?? "another runner"));
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
        foreach (var name in new[] { Depth, Latency, Poll, RecordingKnob, FlashbackKnob, PipelineKnob, AppKnob, ChoiceKnob, BoolKnob })
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

    [Fact]
    public void AnUnknownFieldDoesNotHideAnEmptyVariantList()
    {
        // The unknown-field problem lists the allowed field names, "variants" among them; it must not be mistaken for
        // a problem about the variant list and suppress that one.
        var error = Assert.Throws<PerfExperimentSpecException>(() => PerfExperimentSpec.Parse(
            "{\"name\":\"x\",\"repeat\":3,\"variants\":[]}"));
        Assert.Contains(error.Problems, problem => problem.Contains("repeat", StringComparison.Ordinal));
        Assert.Contains(error.Problems, problem => problem.Contains("at least one variant", StringComparison.Ordinal));
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

    [Fact]
    public async Task APipelineKnobIsRejectedWhileARecordingTheRunnerDidNotStartHoldsTheCapture()
    {
        var app = new FakeApp { Recording = true, FlashbackActive = true };
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + PipelineKnob + "\":\"1\"}}"));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        Assert.Contains(result.Problems, p => p.Contains(PipelineKnob, StringComparison.Ordinal)
            && p.Contains("recording", StringComparison.OrdinalIgnoreCase));
        Assert.False(app.Touched());
        Assert.True(app.Recording);
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
        Assert.DoesNotContain(app.Calls, c => c.Command == "SetTuningKnob");
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

    [Fact]
    public async Task APipelineKnobRestartsPreviewFlashbackAndTheRecordingSoEveryRunMeasuresItsOwnValue()
    {
        var app = new FakeApp { FlashbackActive = true };
        var result = await RunAsync(app, Spec(
            "{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + PipelineKnob + "\":\"1\"}}", repeats: 2, extra: ",\"recording\":true"));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(result.Runs.Count, app.PipelineAtWindowBegin.Count);
        for (var run = 0; run < result.Runs.Count; run++)
        {
            var expected = result.Runs[run].Variant == "B" ? "1" : null;
            Assert.Equal((expected, expected, expected), app.PipelineAtWindowBegin[run]);
        }

        Assert.Null(app.Value(PipelineKnob));
        Assert.Null(app.PipelineValueIn("preview"));
        Assert.Null(app.PipelineValueIn("flashback"));
        Assert.False(app.Recording);
        Assert.True(result.Restore.Verified);
    }

    [Fact]
    public async Task APipelineKnobWithFlashbackOffRestartsOnlyThePreview()
    {
        var app = new FakeApp { FlashbackActive = false };
        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + PipelineKnob + "\":\"1\"}}", repeats: 2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.DoesNotContain(app.Calls, c => c.Command == "RestartFlashback");
        for (var run = 0; run < result.Runs.Count; run++)
        {
            Assert.Equal(result.Runs[run].Variant == "B" ? "1" : null, app.PipelineAtWindowBegin[run].Preview);
        }

        Assert.Null(app.Value(PipelineKnob));
        Assert.Null(app.PipelineValueIn("preview"));
        Assert.True(result.Restore.Verified);
    }

    [Fact]
    public async Task APipelineKnobLeftSetByAFailedRunIsRestoredInPreviewAndFlashback()
    {
        var app = new FakeApp { FlashbackActive = true };
        var begins = 0;
        app.FailWhen = (command, payload) =>
            command == "PerfWindow" && payload["action"]!.GetValue<string>() == "begin" && ++begins == 2;

        var result = await RunAsync(app, Spec("{\"name\":\"A\"},{\"name\":\"B\",\"knobs\":{\"" + PipelineKnob + "\":\"1\"}}"));

        // Run B set the knob and started preview and Flashback on it before its window failed to open.
        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Contains(app.Calls, c => c.Command == "SetTuningKnob" && c.Payload["value"]?.GetValue<string>() == "1");
        Assert.Null(app.Value(PipelineKnob));
        Assert.Null(app.PipelineValueIn("preview"));
        Assert.Null(app.PipelineValueIn("flashback"));
        Assert.True(result.Restore.Verified);
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

    // Failure mode: the stop command succeeds but the app still reports a recording. The run fails on that, and the
    // restore must still own the recording it started and stop it, not treat the acknowledged request as a stop.
    [Fact]
    public async Task AStopTheAppAcknowledgedButDidNotPerformIsRetriedByTheRestore()
    {
        var app = new FakeApp { StopsToIgnore = 1 };

        var result = await RunAsync(app, Spec(BaselineAndDepth2, extra: ",\"recording\":true"));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.False(app.Recording);
        Assert.Equal(1, app.RecordingsStarted);
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

    // Failure mode: spec.json is the artifact another agent re-runs. If it is not a whole spec in the schema the runner
    // loads (an array of variants, or a spec missing the run settings), the re-run fails or measures something else.
    [Fact]
    public async Task TheWrittenSpecLoadsBackAsTheSameExperiment()
    {
        var app = new FakeApp();
        var options = Options(app, writeArtifacts: true);
        var specJson = Spec(
            BaselineAndDepth2,
            repeats: 2,
            extra: ",\"description\":\"depth sweep\",\"recording\":true,\"metrics\":[\"preview.present_interval\",\"capture.*\"],\"verdictStatistic\":\"p90\"");
        var result = await RunAsync(app, specJson, options);

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        var original = PerfExperimentSpec.Parse(specJson);
        var reloaded = PerfExperimentSpec.Load(Path.Combine(result.OutputDirectory!, "spec.json"));
        Assert.Equal(original.Name, reloaded.Name);
        Assert.Equal(original.Description, reloaded.Description);
        Assert.Equal(original.WarmupSeconds, reloaded.WarmupSeconds);
        Assert.Equal(original.MeasureSeconds, reloaded.MeasureSeconds);
        Assert.Equal(original.Repeats, reloaded.Repeats);
        Assert.Equal(original.Recording, reloaded.Recording);
        Assert.Equal(original.Metrics, reloaded.Metrics);
        Assert.Equal(original.VerdictStatistic, reloaded.VerdictStatistic);
        Assert.Equal(original.Variants.Select(v => v.Name), reloaded.Variants.Select(v => v.Name));
        Assert.All(original.Variants.Zip(reloaded.Variants), pair =>
            Assert.Equal(pair.First.Knobs.OrderBy(k => k.Key), pair.Second.Knobs.OrderBy(k => k.Key)));
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

    private sealed class ScratchKnobEnvironment : Sussudio.Services.Performance.ITuningKnobEnvironment
    {
        private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);

        public string? Get(string name) => _values.TryGetValue(name, out var value) ? value : null;

        public void Set(string name, string? value) => _values[name] = value;
    }

    [Fact]
    public void TheShippedExampleSpecIsValidAgainstTheRealKnobCatalog()
    {
        var path = Path.Combine(RuntimeContractSource.GetRepoRoot(), "scripts", "performance", "experiments", "example-jitter-depth.json");
        var spec = PerfExperimentSpec.Load(path);
        var catalog = Sussudio.Services.Performance.TuningKnobCatalog.Knobs;
        var store = new Sussudio.Services.Performance.TuningKnobStore(catalog, new ScratchKnobEnvironment(), _ => { });

        Assert.True(spec.Variants.Count >= 2);
        foreach (var knob in spec.Variants.SelectMany(variant => variant.Knobs))
        {
            var definition = catalog.SingleOrDefault(candidate => candidate.Name == knob.Key);
            Assert.True(definition is not null, $"{knob.Key} is not in the knob catalog.");
            Assert.NotEqual(Sussudio.Services.Performance.TuningKnobApply.AppRestart, definition!.Apply);

            var change = store.Set(knob.Key, knob.Value);
            Assert.True(change.Success, $"{knob.Key}={knob.Value} was rejected: {change.Error}");
        }
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

    // ---- the experiment lease ---------------------------------------------------------------------------------

    [Fact]
    public async Task AnotherExperimentHoldingTheLeaseRejectsTheRunBeforeAnyChange()
    {
        var app = new FakeApp { ForeignHolder = "McpServer pid 9: experiment 'other'", ForeignKnobs = new[] { Depth } };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Rejected, result.Status);
        Assert.Contains(result.Problems, p => p.Contains("experiment 'other'", StringComparison.Ordinal));
        Assert.DoesNotContain(app.Calls, c => c.Command is "SetTuningKnob" or "SetPreviewEnabled" or "SetRecordingEnabled" or "PerfWindow" or "AddPerfMarker");
        Assert.Empty(app.LeaseCalls("release"));
        Assert.Single(app.LeaseCalls("acquire"));
    }

    [Fact]
    public async Task ALostAcquireReplyIsReleasedSoTheLeaseDoesNotBlockTheNextExperiment()
    {
        var app = new FakeApp
        {
            LoseReplyWhen = (command, body) => command == "PerfExperimentLease" && body["action"]!.GetValue<string>() == "acquire",
        };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Contains("lease", result.Error, StringComparison.OrdinalIgnoreCase);
        var acquired = Assert.Single(app.LeaseCalls("acquire"))["lease"]!.GetValue<string>();
        Assert.Equal(acquired, Assert.Single(app.LeaseCalls("release"))["lease"]!.GetValue<string>());
        Assert.Null(app.LeaseId);
        Assert.DoesNotContain(app.Calls, c => c.Command is "SetTuningKnob" or "SetPreviewEnabled" or "PerfWindow");
    }

    [Fact]
    public async Task ALeaseThatExpiresMidRunFailsTheRunDropsItsWindowAndStillRestores()
    {
        // Delay 4 is the second run's measure: the lease lapses inside that window.
        var app = new FakeApp { LoseLeaseOnDelayNumber = 4 };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Contains("lease", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(result.Runs);
        AssertKnobsAtBaseline(app);
        Assert.Null(app.Effective(Depth));
        Assert.True(app.Previewing);
        Assert.Empty(app.OpenWindows);
        Assert.True(result.Restore.Verified, string.Join("; ", result.Restore.Errors.Concat(result.Restore.Mismatches)));
    }

    [Fact]
    public async Task ALeaseTakenOverByAnotherExperimentLeavesThatExperimentsKnobsAndPreviewAlone()
    {
        var app = new FakeApp
        {
            LoseLeaseOnDelayNumber = 4,
            OnLeaseLost = fake =>
            {
                fake.ForeignHolder = "McpServer pid 9: experiment 'other'";
                fake.ForeignKnobs = new[] { Depth };
                fake.SetValue(Depth, "7");
            },
        };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Failed, result.Status);
        Assert.Equal("7", app.Value(Depth));
        Assert.False(result.Restore.Verified);
        Assert.Contains(result.Restore.Errors, e => e.Contains("experiment 'other'", StringComparison.Ordinal));
        var lastRenew = app.Calls.FindLastIndex(c => c.Command == "PerfExperimentLease" && c.Payload["action"]!.GetValue<string>() == "renew");
        Assert.DoesNotContain(app.Calls.Skip(lastRenew + 1), c => c.Command is "SetTuningKnob" or "SetPreviewEnabled" or "SetRecordingEnabled" or "RestartFlashback");
        Assert.Empty(app.LeaseCalls("release"));
        Assert.Empty(app.OpenWindows);
    }

    [Fact]
    public async Task LongWaitsRenewTheLeaseBeforeItsTtlLapses()
    {
        var app = new FakeApp();
        var spec = "{\"name\":\"long\",\"warmupSeconds\":120,\"measureSeconds\":300,\"repeats\":2,\"variants\":[" + BaselineAndDepth2 + "]}";

        var result = await RunAsync(app, spec);

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal(4, result.Runs.Count);
        Assert.Equal(TimeSpan.FromSeconds(4 * (120 + 300)), app.VirtualNow);
        var refreshes = app.LeaseRefreshes.Append(app.VirtualNow).ToList();
        for (var i = 1; i < refreshes.Count; i++)
        {
            Assert.True(refreshes[i] - refreshes[i - 1] < TimeSpan.FromSeconds(60), $"no renew between {refreshes[i - 1]} and {refreshes[i]}");
        }

        AssertKnobsAtBaseline(app);
    }

    [Fact]
    public async Task EveryKnobChangeAndRestoreCarriesTheLeaseId()
    {
        var app = new FakeApp();
        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        var acquire = Assert.Single(app.LeaseCalls("acquire"));
        var id = acquire["lease"]!.GetValue<string>();
        var sets = app.Calls.Where(c => c.Command == "SetTuningKnob").ToList();
        Assert.NotEmpty(sets);
        Assert.All(sets, c => Assert.Equal(id, c.Payload["lease"]?.GetValue<string>()));
        Assert.Contains(Depth, acquire["knobs"]!.AsArray().Select(n => n!.GetValue<string>()));
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("failed")]
    [InlineData("canceled")]
    public async Task TheLeaseIsReleasedAfterTheRestoreOnAFreshToken(string outcome)
    {
        var app = new FakeApp();
        using var cts = new CancellationTokenSource();
        if (outcome == "failed")
        {
            var seen = 0;
            app.FailWhen = (command, payload) => command == "PerfWindow" && payload["action"]!.GetValue<string>() == "end" && ++seen == 2;
        }
        else if (outcome == "canceled")
        {
            app.CancelSource = cts;
            app.CancelOnDelayNumber = 3;
        }

        var result = await RunAsync(app, Spec(BaselineAndDepth2), cancellationToken: cts.Token);

        Assert.Equal(outcome, result.Status.ToString().ToLowerInvariant());
        var lastSet = app.Calls.FindLastIndex(c => c.Command == "SetTuningKnob");
        var release = app.Calls.FindLastIndex(c => c.Command == "PerfExperimentLease" && c.Payload["action"]!.GetValue<string>() == "release");
        Assert.True(release > lastSet, "the lease was released before the knobs were restored");
        Assert.False(app.Calls[release].TokenWasCanceled);
        Assert.Null(app.LeaseId);
        AssertKnobsAtBaseline(app);
    }

    [Fact]
    public async Task AFailedReleaseIsReportedNotHidden()
    {
        var app = new FakeApp
        {
            FailWhen = (command, payload) => command == "PerfExperimentLease" && payload["action"]!.GetValue<string>() == "release",
        };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.False(result.Restore.Verified);
        Assert.Contains(result.Restore.Errors, e => e.Contains("lease", StringComparison.OrdinalIgnoreCase));
        AssertKnobsAtBaseline(app);
    }

    [Fact]
    public async Task BaselinesComeFromTheLeaseNotTheEarlierPreflightRead()
    {
        // A knob changed by hand after the preflight read and before the lease: the lease's baseline is the truth.
        var app = new FakeApp { BeforeAcquire = fake => fake.SetValue(Depth, "5") };

        var result = await RunAsync(app, Spec(BaselineAndDepth2));

        Assert.Equal(PerfExperimentStatus.Completed, result.Status);
        Assert.Equal("5", app.Value(Depth));
        Assert.True(result.Restore.Verified, string.Join("; ", result.Restore.Mismatches));
    }
}

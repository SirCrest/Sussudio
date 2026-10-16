using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

internal sealed record PerfRunOutcome(string Window, JsonElement? Report, double DurationMs);

// The only code that changes the running app during an experiment: knobs, preview and Flashback restarts, recording,
// performance windows and markers, plus the restore that undoes all of it.
//
// Ownership rules:
//  - Preflight is read-only (GetTuningKnobs, GetSnapshot). Every problem is found before the first mutation.
//  - The app's experiment lease (PerfExperimentLease, section 2.12) is taken before the first mutation and released
//    after the restore. It is what keeps a second experiment, or a hand-typed knob change, off this one's knobs, and
//    what restores them if this process dies. Every knob change carries the lease id, the lease is renewed while the
//    runner waits, and a refused renew ends the experiment: the app has restored the knobs or someone else owns them.
//  - Restore does not trust this class's own bookkeeping. A command can reach the app and its reply can still be lost
//    (cancel, timeout, broken pipe), so every knob the spec names is restored unconditionally, a recording is stopped
//    when the app reports one running, and an open window is canceled. The result is then read back from the app.
//  - Restore runs on a fresh token. A canceled run token would make every restore command throw before it was sent.
internal sealed class PerfExperimentSession
{
    private const string LeaseCommand = "PerfExperimentLease";
    private const int MaxHolderLength = 200;

    private const int MaxWindowNameLength = 64;
    private const string ApplyLive = "live";
    private const string ApplyPreviewRestart = "preview-restart";
    private const string ApplyRecordingStart = "recording-start";
    private const string ApplyFlashbackRestart = "flashback-restart";
    private const string ApplyAppRestart = "app-restart";

    private sealed record KnobInfo(
        string Name,
        string Type,
        double? Min,
        double? Max,
        IReadOnlyList<string> Choices,
        string Apply,
        string? Value,
        string? Launch);

    private enum RestoreMode
    {
        Reset,
        SetBaseline,
    }

    private sealed record KnobBaseline(string Name, string Apply, string? Value, RestoreMode Mode);

    private readonly PerfAppLink _app;
    private readonly PerfExperimentSpec _spec;
    private readonly PerfExperimentOptions _options;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly List<string> _touched = new();
    private readonly Dictionary<string, KnobBaseline> _baselines = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _applied = new(StringComparer.Ordinal);
    private IReadOnlyList<IReadOnlyDictionary<string, string>> _variantKnobs = Array.Empty<IReadOnlyDictionary<string, string>>();
    private bool _restartPreview;
    private bool _restartFlashback;
    private bool _pipelineStale;
    private bool _flashbackStale;
    private bool _recordingStarted;
    private string? _openWindow;

    // The lease id is this runner's capability: the app accepts changes to leased knobs only with it.
    private readonly string _leaseId = Guid.NewGuid().ToString("N");
    private readonly Stopwatch _sinceRenewClock = new();
    private TimeSpan _sinceRenewWaited;
    private bool _leaseHeld;
    private string? _leaseLostReason;

    public PerfExperimentSession(PerfAppLink app, PerfExperimentSpec spec, PerfExperimentOptions options)
    {
        _app = app;
        _spec = spec;
        _options = options;
        _delay = options.DelayAsync ?? ((delay, token) => Task.Delay(delay, token));
    }

    // ---- preflight ---------------------------------------------------------------------------------------------

    /// <summary>Validates the spec against the live app without changing anything. Returns every problem found.</summary>
    public async Task<IReadOnlyList<string>> PreflightAsync(CancellationToken cancellationToken)
    {
        var problems = new List<string>();
        var wanted = new List<string>();
        foreach (var variant in _spec.Variants)
        {
            foreach (var name in variant.Knobs.Keys)
            {
                if (!wanted.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    wanted.Add(name);
                }
            }
        }

        var state = await _app.GetStateAsync(cancellationToken).ConfigureAwait(false);
        if (!state.Previewing)
        {
            problems.Add("Preview is not running. Start it first (ssctl preview start): the experiment measures the live preview pipeline and restarts it for preview-restart knobs, but it will not start it from nothing.");
        }

        if (_spec.Recording && state.Recording)
        {
            problems.Add("The spec asks the runner to record during every run, but a recording is already running. Stop it first, or set \"recording\" to false.");
        }

        if (wanted.Count > 0)
        {
            var catalog = await ReadKnobsAsync(cancellationToken).ConfigureAwait(false);
            var usable = new Dictionary<string, KnobInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in wanted)
            {
                if (!catalog.TryGetValue(name, out var info))
                {
                    problems.Add($"Unknown tuning knob {name}. List the real ones with: ssctl knobs");
                    continue;
                }

                var scopeProblem = ScopeProblem(info, state);
                if (scopeProblem is not null)
                {
                    problems.Add(scopeProblem);
                    continue;
                }

                if (info.Value is null && info.Launch is not null)
                {
                    problems.Add($"{info.Name} is unset but was set at launch, and a knob cannot be unset, so the runner could not restore it afterwards.");
                    continue;
                }

                usable[info.Name] = info;
            }

            var resolved = new List<IReadOnlyDictionary<string, string>>(_spec.Variants.Count);
            foreach (var variant in _spec.Variants)
            {
                var knobs = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var (name, text) in variant.Knobs)
                {
                    if (!usable.TryGetValue(name, out var info))
                    {
                        continue; // already reported above
                    }

                    if (TryNormalize(info, text, out var canonical, out var error))
                    {
                        knobs[info.Name] = canonical;
                    }
                    else
                    {
                        problems.Add($"Variant {variant.Name}: {error}");
                    }
                }

                resolved.Add(knobs);
            }

            if (problems.Count == 0)
            {
                _variantKnobs = resolved;
                foreach (var knobs in resolved)
                {
                    foreach (var name in knobs.Keys)
                    {
                        if (!_touched.Contains(name, StringComparer.Ordinal))
                        {
                            _touched.Add(name);
                        }
                    }
                }

                foreach (var name in _touched)
                {
                    var info = usable[name];
                    var mode = string.Equals(info.Value, info.Launch, StringComparison.Ordinal) ? RestoreMode.Reset : RestoreMode.SetBaseline;
                    _baselines[name] = new KnobBaseline(name, info.Apply, info.Value, mode);
                }

                _restartPreview = _touched.Any(name => _baselines[name].Apply == ApplyPreviewRestart);
                _restartFlashback = _touched.Any(name => _baselines[name].Apply == ApplyFlashbackRestart);
            }
        }
        else
        {
            _variantKnobs = _spec.Variants.Select(_ => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>()).ToList();
        }

        return problems;
    }

    private string? ScopeProblem(KnobInfo info, PerfAppState state)
    {
        switch (info.Apply)
        {
            case ApplyAppRestart:
                return $"{info.Name} is read once at app startup (Apply app-restart), so the runner cannot change it between runs. "
                    + "Relaunch the app with the value set and compare runs from separate launches instead.";
            case ApplyRecordingStart when !_spec.Recording:
                return $"{info.Name} takes effect when a recording starts (Apply recording-start). Set \"recording\": true so the runner starts a recording for every run.";
            case ApplyFlashbackRestart when !state.FlashbackActive:
                return $"{info.Name} takes effect when Flashback restarts (Apply flashback-restart), but Flashback is not active. Enable Flashback first.";
            case ApplyLive:
            case ApplyPreviewRestart:
            case ApplyRecordingStart:
            case ApplyFlashbackRestart:
                return null;
            default:
                return $"{info.Name} has an unknown apply scope '{info.Apply}', so the runner cannot tell how to apply it.";
        }
    }

    private async Task<Dictionary<string, KnobInfo>> ReadKnobsAsync(CancellationToken cancellationToken)
    {
        var reply = await _app.RequireAsync("GetTuningKnobs", null, cancellationToken).ConfigureAwait(false);
        var knobs = new Dictionary<string, KnobInfo>(StringComparer.OrdinalIgnoreCase);
        if (reply.Data is not { } data)
        {
            throw new PerfExperimentCommandException("GetTuningKnobs failed: the response carried no knob listing.");
        }

        foreach (var knob in PerfJson.GetArray(data, "Knobs"))
        {
            var name = PerfJson.GetString(knob, "Name");
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            knobs[name] = new KnobInfo(
                name,
                (PerfJson.GetString(knob, "Type") ?? string.Empty).ToLowerInvariant(),
                PerfJson.GetDouble(knob, "Min"),
                PerfJson.GetDouble(knob, "Max"),
                PerfJson.GetArray(knob, "Choices").Where(c => c.ValueKind == JsonValueKind.String).Select(c => c.GetString()!).ToList(),
                PerfJson.GetString(knob, "Apply") ?? string.Empty,
                PerfJson.GetString(knob, "Value"),
                PerfJson.GetString(knob, "Launch"));
        }

        return knobs;
    }

    // Mirrors TuningKnobStore.TryNormalize so a value the app would refuse is rejected here, before anything runs.
    private static bool TryNormalize(KnobInfo knob, string text, out string canonical, out string error)
    {
        canonical = string.Empty;
        error = string.Empty;
        switch (knob.Type)
        {
            case "int":
            {
                var min = (long)(knob.Min ?? long.MinValue);
                var max = (long)(knob.Max ?? long.MaxValue);
                if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                {
                    error = $"{knob.Name} expects an integer in [{min}, {max}]; got '{text}'.";
                    return false;
                }

                if (integer < min || integer > max)
                {
                    error = $"{knob.Name} must be an integer in [{min}, {max}]; got {text}.";
                    return false;
                }

                canonical = integer.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            case "double":
            {
                var min = knob.Min ?? double.MinValue;
                var max = knob.Max ?? double.MaxValue;
                if (!double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
                    || !double.IsFinite(number))
                {
                    error = $"{knob.Name} expects a finite number in [{min.ToString(CultureInfo.InvariantCulture)}, {max.ToString(CultureInfo.InvariantCulture)}]; got '{text}'.";
                    return false;
                }

                if (number < min || number > max)
                {
                    error = $"{knob.Name} must be a number in [{min.ToString(CultureInfo.InvariantCulture)}, {max.ToString(CultureInfo.InvariantCulture)}]; got {text}.";
                    return false;
                }

                canonical = number.ToString("R", CultureInfo.InvariantCulture);
                return true;
            }

            case "bool":
                if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    canonical = "1";
                    return true;
                }

                if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    canonical = "0";
                    return true;
                }

                error = $"{knob.Name} expects a boolean (0, 1, true or false); got '{text}'.";
                return false;
            case "choice":
            {
                var match = knob.Choices.FirstOrDefault(choice => choice.Equals(text, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    error = $"{knob.Name} expects one of: {string.Join(", ", knob.Choices)}; got '{text}'.";
                    return false;
                }

                canonical = match;
                return true;
            }

            default:
                error = $"{knob.Name} has an unsupported type '{knob.Type}'.";
                return false;
        }
    }

    // ---- the experiment lease ------------------------------------------------------------------------------------

    private int LeaseTtlSeconds => (int)Math.Ceiling(_options.LeaseTimeToLive.TotalSeconds);

    // A quarter of the TTL leaves three missed renewals of slack before the app gives the knobs back.
    private TimeSpan RenewInterval => TimeSpan.FromSeconds(Math.Max(1, LeaseTtlSeconds / 4.0));

    /// <summary>
    /// Takes the app's experiment lease on the knobs the preflight resolved. Returns the app's reason when another
    /// experiment holds it; nothing has changed then. On success the baselines are replaced by the app's own read, taken
    /// under the lease, so a knob changed by hand after the preflight read is restored to its value when the lease began.
    /// </summary>
    public async Task<string?> AcquireLeaseAsync(CancellationToken cancellationToken)
    {
        var reply = await _app.CallAsync(
            LeaseCommand,
            new Dictionary<string, object?>
            {
                ["action"] = "acquire",
                ["lease"] = _leaseId,
                ["holder"] = BuildHolder(_spec.Name),
                ["knobs"] = _touched.ToArray(),
                ["ttlSeconds"] = LeaseTtlSeconds,
            },
            cancellationToken,
            "PerfExperimentLease acquire").ConfigureAwait(false);
        if (!reply.Success)
        {
            return reply.Message;
        }

        _leaseHeld = true;
        MarkRenewed();
        if (reply.Data is { } data)
        {
            foreach (var knob in PerfJson.GetArray(data, "Baseline"))
            {
                var name = PerfJson.GetString(knob, "Name");
                if (name is not null && _baselines.TryGetValue(name, out var baseline))
                {
                    var value = PerfJson.GetString(knob, "Value");
                    var launch = PerfJson.GetString(knob, "Launch");
                    _baselines[name] = baseline with
                    {
                        Value = value,
                        Mode = string.Equals(value, launch, StringComparison.Ordinal) ? RestoreMode.Reset : RestoreMode.SetBaseline,
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The acquire's reply never arrived, so the app may hold a lease under this runner's id with nothing to restore.
    /// Release it so it does not block the next experiment until it expires. Best effort; never throws.
    /// </summary>
    public async Task ReleaseUnconfirmedLeaseAsync()
    {
        using var cleanup = new CancellationTokenSource(_options.CleanupTimeout);
        try
        {
            await _app.CallAsync(
                LeaseCommand,
                new Dictionary<string, object?> { ["action"] = "release", ["lease"] = _leaseId },
                cleanup.Token,
                "PerfExperimentLease release").ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Intentionally swallowed: the lease, if the app holds one, expires by itself and changed nothing.
        }
    }

    private async Task RenewLeaseAsync(CancellationToken cancellationToken)
    {
        if (!_leaseHeld)
        {
            return;
        }

        var reply = await _app.CallAsync(
            LeaseCommand,
            new Dictionary<string, object?> { ["action"] = "renew", ["lease"] = _leaseId, ["ttlSeconds"] = LeaseTtlSeconds },
            cancellationToken,
            "PerfExperimentLease renew").ConfigureAwait(false);
        if (!reply.Success)
        {
            _leaseHeld = false;
            _leaseLostReason = reply.Message;
            throw new PerfExperimentCommandException(
                $"The experiment lost its tuning-knob lease: {reply.Message} The experiment stopped, and the run in progress is not used.");
        }

        MarkRenewed();
    }

    private Task RenewLeaseIfDueAsync(CancellationToken cancellationToken)
        => _leaseHeld && (_sinceRenewWaited >= RenewInterval || _sinceRenewClock.Elapsed >= RenewInterval)
            ? RenewLeaseAsync(cancellationToken)
            : Task.CompletedTask;

    private void MarkRenewed()
    {
        _sinceRenewWaited = TimeSpan.Zero;
        _sinceRenewClock.Restart();
    }

    // Waits through the injected delay in steps no longer than the renew interval, renewing the lease between steps
    // when it is due, so a long warmup or measure never outlives the lease. Virtual time in tests counts as waited.
    private async Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var remaining = duration;
        while (remaining > TimeSpan.Zero)
        {
            var step = _leaseHeld && remaining > RenewInterval ? RenewInterval : remaining;
            await _delay(step, cancellationToken).ConfigureAwait(false);
            remaining -= step;
            _sinceRenewWaited += step;
            if (remaining > TimeSpan.Zero)
            {
                await RenewLeaseIfDueAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // Who runs the experiment, as other clients see it in the lease: the tool process and the experiment name.
    internal static string BuildHolder(string experiment)
    {
        var text = new StringBuilder();
        text.Append(Process.GetCurrentProcess().ProcessName)
            .Append(" pid ")
            .Append(Environment.ProcessId.ToString(CultureInfo.InvariantCulture))
            .Append(": experiment '");
        foreach (var ch in experiment)
        {
            text.Append(char.IsControl(ch) ? ' ' : ch);
        }

        text.Append('\'');
        return text.Length <= MaxHolderLength ? text.ToString() : text.ToString(0, MaxHolderLength - 4) + "...'";
    }

    // ---- one run -----------------------------------------------------------------------------------------------

    /// <summary>
    /// One run: put the variant's knobs in place, restart what those knobs need so a fresh start is compared with a fresh
    /// start, warm up, then measure one named performance window. <paramref name="repeat"/> is 1-based.
    /// </summary>
    public async Task<PerfRunOutcome> RunOnceAsync(int variantIndex, int repeat, int runNumber, CancellationToken cancellationToken)
    {
        var variant = _spec.Variants[variantIndex];
        await RenewLeaseAsync(cancellationToken).ConfigureAwait(false);
        await ApplyKnobsAsync(variantIndex, cancellationToken).ConfigureAwait(false);
        if (_restartPreview)
        {
            await RestartPreviewAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_restartFlashback)
        {
            await RestartFlashbackAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_spec.Recording)
        {
            await StartRecordingAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_spec.WarmupSeconds > 0)
        {
            await MarkAsync($"experiment variant {variant.Name} run {repeat} warmup", cancellationToken).ConfigureAwait(false);
            await WaitAsync(TimeSpan.FromSeconds(_spec.WarmupSeconds), cancellationToken).ConfigureAwait(false);
        }

        var window = BuildWindowName(runNumber, _spec.Name, variant.Name, repeat);
        await BeginWindowAsync(window, cancellationToken).ConfigureAwait(false);
        await MarkAsync($"experiment variant {variant.Name} run {repeat} measure", cancellationToken).ConfigureAwait(false);
        await WaitAsync(TimeSpan.FromSeconds(_spec.MeasureSeconds), cancellationToken).ConfigureAwait(false);

        // The window counts only if the lease held for all of it: a lease lost inside it means the app restored the
        // knobs, or another experiment changed them, while this window was measuring.
        await RenewLeaseAsync(cancellationToken).ConfigureAwait(false);
        var report = await EndWindowAsync(window, cancellationToken).ConfigureAwait(false);

        if (_spec.Recording)
        {
            await StopRecordingAsync(cancellationToken).ConfigureAwait(false);
        }

        var duration = report is { } data ? PerfJson.GetDouble(data, "DurationMs") : null;
        return new PerfRunOutcome(window, report, duration ?? _spec.MeasureSeconds * 1000d);
    }

    // A window name is 1 to 64 characters with no control characters, and unique per run so a leftover window from an
    // earlier failed run can never be mistaken for this one.
    internal static string BuildWindowName(int runNumber, string experiment, string variant, int repeat)
    {
        var suffix = $" #{runNumber.ToString(CultureInfo.InvariantCulture)}";
        var body = new StringBuilder();
        foreach (var ch in $"{experiment} {variant} r{repeat.ToString(CultureInfo.InvariantCulture)}")
        {
            body.Append(char.IsControl(ch) ? ' ' : ch);
        }

        var text = body.ToString().Trim();
        var room = MaxWindowNameLength - suffix.Length;
        if (text.Length > room)
        {
            text = text[..room];
            if (text.Length > 0 && char.IsHighSurrogate(text[^1]))
            {
                text = text[..^1];
            }

            text = text.TrimEnd();
        }

        return (text.Length == 0 ? "perf-experiment" : text) + suffix;
    }

    private async Task ApplyKnobsAsync(int variantIndex, CancellationToken cancellationToken)
    {
        var desired = _variantKnobs[variantIndex];
        foreach (var name in _touched)
        {
            if (desired.TryGetValue(name, out var value))
            {
                if (_applied.TryGetValue(name, out var current) && string.Equals(current, value, StringComparison.Ordinal))
                {
                    continue;
                }

                var reply = await _app.RequireAsync(
                    "SetTuningKnob",
                    new Dictionary<string, object?> { ["name"] = name, ["value"] = value, ["lease"] = _leaseId },
                    cancellationToken,
                    $"SetTuningKnob {name}").ConfigureAwait(false);
                NoteKnobChange(name, reply);
                _applied[name] = value;
            }
            else if (_applied.ContainsKey(name))
            {
                var reply = await RestoreKnobAsync(name, cancellationToken).ConfigureAwait(false);
                if (!reply.Success)
                {
                    throw new PerfExperimentCommandException($"{reply.Label} failed: {reply.Message}");
                }

                _applied.Remove(name);
            }
        }
    }

    private async Task<PerfReply> RestoreKnobAsync(string name, CancellationToken cancellationToken)
    {
        var baseline = _baselines[name];
        var payload = baseline.Mode == RestoreMode.Reset
            ? new Dictionary<string, object?> { ["name"] = name, ["reset"] = true, ["lease"] = _leaseId }
            : new Dictionary<string, object?> { ["name"] = name, ["value"] = baseline.Value, ["lease"] = _leaseId };
        var reply = await _app.CallAsync(
            "SetTuningKnob",
            payload,
            cancellationToken,
            $"SetTuningKnob {name} ({(baseline.Mode == RestoreMode.Reset ? "reset" : "restore")})").ConfigureAwait(false);
        if (reply.Success)
        {
            NoteKnobChange(name, reply);
        }

        return reply;
    }

    // The app reports Old and New for every change. A change that moved a knob a running part of the app already read
    // means that part must restart before the new value is real. A reply that cannot be read counts as a change.
    private bool NoteKnobChange(string name, PerfReply reply)
    {
        var changed = true;
        if (reply.Data is { } data && PerfJson.TryGet(data, "Old", out var old) && PerfJson.TryGet(data, "New", out var @new))
        {
            changed = !string.Equals(Display(old), Display(@new), StringComparison.Ordinal);
        }

        if (changed)
        {
            MarkStale(name);
        }

        return changed;
    }

    private void MarkStale(string name)
    {
        switch (_baselines[name].Apply)
        {
            case ApplyPreviewRestart:
                _pipelineStale = true;
                break;
            case ApplyFlashbackRestart:
                _flashbackStale = true;
                break;
        }
    }

    private static string? Display(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        _ => value.GetRawText(),
    };

    private async Task RestartPreviewAsync(CancellationToken cancellationToken)
    {
        // Stopping and starting is read back from the snapshot at both ends: an acknowledgement means the request was
        // accepted, not that frames are flowing again.
        await _app.RequireAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = false }, cancellationToken, "SetPreviewEnabled false").ConfigureAwait(false);
        await WaitForStateAsync(state => !state.Previewing, "Preview did not report stopped", cancellationToken).ConfigureAwait(false);
        await StartPreviewAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task StartPreviewAsync(CancellationToken cancellationToken)
    {
        await _app.RequireAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = true }, cancellationToken, "SetPreviewEnabled true").ConfigureAwait(false);
        _pipelineStale = false;
        await WaitForStateAsync(state => state.Previewing, "Preview did not report running again", cancellationToken).ConfigureAwait(false);
    }

    private async Task RestartFlashbackAsync(CancellationToken cancellationToken)
    {
        await _app.RequireAsync("RestartFlashback", null, cancellationToken).ConfigureAwait(false);
        _flashbackStale = false;
        await WaitForStateAsync(state => state.FlashbackActive, "Flashback did not report active again", cancellationToken).ConfigureAwait(false);
    }

    // Readiness is polled through the injected delay, so tests run in virtual time; the stopwatch bounds a real run
    // whose commands are slow even when the delays add up to less than the timeout.
    private async Task WaitForStateAsync(Func<PerfAppState, bool> ready, string failure, CancellationToken cancellationToken)
    {
        var interval = _options.ReadyPollInterval > TimeSpan.Zero ? _options.ReadyPollInterval : TimeSpan.FromMilliseconds(50);
        var waited = TimeSpan.Zero;
        var clock = Stopwatch.StartNew();
        while (true)
        {
            var state = await _app.GetStateAsync(cancellationToken).ConfigureAwait(false);
            if (ready(state))
            {
                return;
            }

            if (waited >= _options.ReadyTimeout || clock.Elapsed >= _options.ReadyTimeout)
            {
                throw new PerfExperimentCommandException(
                    $"{failure} within {_options.ReadyTimeout.TotalSeconds.ToString("0.#", CultureInfo.InvariantCulture)} s.");
            }

            await RenewLeaseIfDueAsync(cancellationToken).ConfigureAwait(false);
            await _delay(interval, cancellationToken).ConfigureAwait(false);
            waited += interval;
            _sinceRenewWaited += interval;
        }
    }

    private async Task StartRecordingAsync(CancellationToken cancellationToken)
    {
        // Marked before the command: if the request reaches the app and the reply is lost, restore still stops it.
        _recordingStarted = true;
        var reply = await _app.RequireAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = true }, cancellationToken, "SetRecordingEnabled true").ConfigureAwait(false);
        if (reply.Snapshot is { } snapshot && PerfJson.GetBool(snapshot, "IsRecording") == false)
        {
            throw new PerfExperimentCommandException("SetRecordingEnabled true failed: the app did not report a recording after the start request.");
        }
    }

    private async Task StopRecordingAsync(CancellationToken cancellationToken)
    {
        var reply = await _app.RequireAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = false }, cancellationToken, "SetRecordingEnabled false").ConfigureAwait(false);
        _recordingStarted = false;
        if (reply.Snapshot is { } snapshot && PerfJson.GetBool(snapshot, "IsRecording") == true)
        {
            throw new PerfExperimentCommandException("SetRecordingEnabled false failed: the app still reports a recording after the stop request.");
        }
    }

    private async Task BeginWindowAsync(string window, CancellationToken cancellationToken)
    {
        // Marked before the command for the same reason as the recording; a refusal clears it, because then no window exists.
        _openWindow = window;
        var reply = await _app.CallAsync(
            "PerfWindow",
            new Dictionary<string, object?> { ["action"] = "begin", ["name"] = window },
            cancellationToken,
            "PerfWindow begin").ConfigureAwait(false);
        if (!reply.Success)
        {
            _openWindow = null;
            throw new PerfExperimentCommandException($"{reply.Label} failed: {reply.Message}");
        }
    }

    private async Task<JsonElement?> EndWindowAsync(string window, CancellationToken cancellationToken)
    {
        var reply = await _app.RequireAsync(
            "PerfWindow",
            new Dictionary<string, object?> { ["action"] = "end", ["name"] = window },
            cancellationToken,
            "PerfWindow end").ConfigureAwait(false);
        _openWindow = null;
        return reply.Data;
    }

    // A marker only annotates the trace. A refused one is not worth failing a run that is otherwise measuring correctly.
    private async Task MarkAsync(string text, CancellationToken cancellationToken)
        => await _app.CallAsync("AddPerfMarker", new Dictionary<string, object?> { ["text"] = text }, cancellationToken).ConfigureAwait(false);

    // ---- restore -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the app to its pre-experiment state and reads it back, then releases the lease. Always attempts every
    /// step; never throws. Runs on a fresh token bounded by <see cref="PerfExperimentOptions.CleanupTimeout"/>, so a
    /// canceled run still restores. When the lease was lost and another experiment now holds it, only this runner's own
    /// window is canceled: the knobs, preview and recording belong to that experiment now.
    /// </summary>
    public async Task<PerfExperimentRestore> RestoreAsync()
    {
        var actions = new List<string>();
        var errors = new List<string>();
        var mismatches = new List<string>();
        using var cleanup = new CancellationTokenSource(_options.CleanupTimeout);
        var token = cleanup.Token;
        string? takenBy = null;

        await Step(errors, "cancel the open performance window", async () =>
        {
            if (_openWindow is null)
            {
                return;
            }

            // A refusal means the window is already gone (or never opened); only an unreachable app is an error.
            await _app.CallAsync(
                "PerfWindow",
                new Dictionary<string, object?> { ["action"] = "cancel", ["name"] = _openWindow },
                token).ConfigureAwait(false);
            actions.Add("canceled the open window");
            _openWindow = null;
        }).ConfigureAwait(false);

        if (_leaseLostReason is not null)
        {
            await Step(errors, "read who holds the lease now", async () =>
            {
                var reply = await _app.RequireAsync(LeaseCommand, new Dictionary<string, object?> { ["action"] = "status" }, token, "PerfExperimentLease status").ConfigureAwait(false);
                if (reply.Data is { } data && PerfJson.TryGet(data, "Active", out var active) && active.ValueKind == JsonValueKind.Object)
                {
                    takenBy = PerfJson.GetString(active, "Holder") ?? "another experiment";
                }
            }).ConfigureAwait(false);
        }

        if (takenBy is not null)
        {
            errors.Add(
                $"The knobs, preview and recording were left as they are: the lease was lost ({_leaseLostReason}) and {takenBy} "
                + "now holds it, so restoring them would disturb that experiment.");
            return new PerfExperimentRestore(false, actions, errors, mismatches);
        }

        await Step(errors, "stop the recording the runner started", async () =>
        {
            if (!_recordingStarted)
            {
                return;
            }

            var state = await _app.GetStateAsync(token).ConfigureAwait(false);
            if (state.Recording)
            {
                await _app.RequireAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = false }, token, "SetRecordingEnabled false").ConfigureAwait(false);
                actions.Add("stopped the recording the runner started");
            }

            _recordingStarted = false;
        }).ConfigureAwait(false);

        // When the lease lapsed the app put the knobs back itself, without restarting anything, so the restore below
        // sees no change. A preview or Flashback that read an experiment value still runs with it and must restart.
        if (_leaseLostReason is not null)
        {
            foreach (var name in _applied.Keys)
            {
                MarkStale(name);
            }
        }

        foreach (var name in _touched)
        {
            await Step(errors, $"restore {name}", async () =>
            {
                var reply = await RestoreKnobAsync(name, token).ConfigureAwait(false);
                if (!reply.Success)
                {
                    errors.Add($"{name}: could not be restored: {reply.Message}");
                }
                else if (_applied.Remove(name) || reply.Data is { } data && DiffersFromOld(data))
                {
                    actions.Add($"restored {name}");
                }
            }).ConfigureAwait(false);
        }

        await Step(errors, "restart what the restored knobs affect", async () =>
        {
            if (_touched.Count == 0 && !_pipelineStale && !_flashbackStale)
            {
                return;
            }

            var state = await _app.GetStateAsync(token).ConfigureAwait(false);
            if (!state.Previewing || _pipelineStale)
            {
                if (state.Previewing)
                {
                    await _app.RequireAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = false }, token, "SetPreviewEnabled false").ConfigureAwait(false);
                    await WaitForStateAsync(s => !s.Previewing, "Preview did not report stopped", token).ConfigureAwait(false);
                }

                await StartPreviewAsync(token).ConfigureAwait(false);
                actions.Add("restarted the preview");
            }

            if (_flashbackStale)
            {
                await RestartFlashbackAsync(token).ConfigureAwait(false);
                actions.Add("restarted Flashback");
            }
        }).ConfigureAwait(false);

        await Step(errors, "read the restored state back", async () =>
        {
            if (_touched.Count > 0)
            {
                var catalog = await ReadKnobsAsync(token).ConfigureAwait(false);
                foreach (var name in _touched)
                {
                    var expected = _baselines[name].Value;
                    var found = catalog.TryGetValue(name, out var info) ? info.Value : null;
                    if (!string.Equals(expected, found, StringComparison.Ordinal))
                    {
                        mismatches.Add($"{name}: expected {expected ?? "<unset>"}, found {found ?? "<unset>"}");
                    }
                }
            }

            var state = await _app.GetStateAsync(token).ConfigureAwait(false);
            if (!state.Previewing)
            {
                mismatches.Add("preview: not running");
            }

            if (_spec.Recording && state.Recording)
            {
                mismatches.Add("recording: still running");
            }
        }).ConfigureAwait(false);

        await Step(errors, "release the experiment lease", async () =>
        {
            if (!_leaseHeld)
            {
                return;
            }

            var reply = await _app.CallAsync(
                LeaseCommand,
                new Dictionary<string, object?> { ["action"] = "release", ["lease"] = _leaseId },
                token,
                "PerfExperimentLease release").ConfigureAwait(false);
            if (!reply.Success)
            {
                errors.Add($"Could not release the experiment lease: {reply.Message} It ends by itself within {LeaseTtlSeconds} s.");
                return;
            }

            _leaseHeld = false;
        }).ConfigureAwait(false);

        return new PerfExperimentRestore(errors.Count == 0 && mismatches.Count == 0, actions, errors, mismatches);
    }

    private static bool DiffersFromOld(JsonElement data)
        => PerfJson.TryGet(data, "Old", out var old)
            && PerfJson.TryGet(data, "New", out var @new)
            && !string.Equals(Display(old), Display(@new), StringComparison.Ordinal);

    private static async Task Step(List<string> errors, string what, Func<Task> step)
    {
        try
        {
            await step().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            errors.Add($"Could not {what}: the restore ran out of time.");
        }
        catch (Exception ex)
        {
            errors.Add($"Could not {what}: {ex.Message}");
        }
    }
}

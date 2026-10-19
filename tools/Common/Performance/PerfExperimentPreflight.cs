using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

// Read-only validation against the live app. A successful plan is the complete input the leased session needs;
// preflight never owns mutations or a lease.
internal static class PerfExperimentPreflight
{
    private const string ApplyLive = "live";
    private const string ApplyPreviewRestart = "preview-restart";
    private const string ApplyRecordingStart = "recording-start";
    private const string ApplyFlashbackRestart = "flashback-restart";
    private const string ApplyPipelineRestart = "pipeline-restart";
    private const string ApplyAppRestart = "app-restart";

    internal enum RestoreMode
    {
        Reset,
        SetBaseline,
    }

    internal sealed record Baseline(string Name, string Apply, string? Value, RestoreMode Mode);

    internal sealed record Plan(
        IReadOnlyList<IReadOnlyDictionary<string, string>> VariantKnobs,
        IReadOnlyList<string> Touched,
        IReadOnlyDictionary<string, Baseline> Baselines,
        bool RestartPreview,
        bool RestartFlashback);

    internal sealed record Result(Plan? Plan, IReadOnlyList<string> Problems);

    private sealed record KnobInfo(
        string Name,
        string Type,
        double? Min,
        double? Max,
        IReadOnlyList<string> Choices,
        string Apply,
        string? Value,
        string? Launch);

    public static async Task<Result> RunAsync(PerfAppLink app, PerfExperimentSpec spec, CancellationToken cancellationToken)
    {
        var problems = new List<string>();
        var wanted = new List<string>();
        foreach (var variant in spec.Variants)
        {
            foreach (var name in variant.Knobs.Keys)
            {
                if (!wanted.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    wanted.Add(name);
                }
            }
        }

        var state = await app.GetStateAsync(cancellationToken).ConfigureAwait(false);
        if (!state.Previewing)
        {
            problems.Add("Preview is not running. Start it first (ssctl preview start): the experiment measures the live preview pipeline and restarts it for preview-restart knobs, but it will not start it from nothing.");
        }

        if (spec.Recording && state.Recording)
        {
            problems.Add("The spec asks the runner to record during every run, but a recording is already running. Stop it first, or set \"recording\" to false.");
        }

        var resolved = new List<IReadOnlyDictionary<string, string>>(spec.Variants.Count);
        var touched = new List<string>();
        var baselines = new Dictionary<string, Baseline>(StringComparer.Ordinal);
        var restartPreview = false;
        var restartFlashback = false;
        if (wanted.Count > 0)
        {
            var catalog = await ReadKnobsAsync(app, cancellationToken).ConfigureAwait(false);
            var usable = new Dictionary<string, KnobInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in wanted)
            {
                if (!catalog.TryGetValue(name, out var info))
                {
                    problems.Add($"Unknown tuning knob {name}. List the real ones with: ssctl knobs");
                    continue;
                }

                var scopeProblem = ScopeProblem(info, state, spec);
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

            foreach (var variant in spec.Variants)
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

                resolved.Add(new ReadOnlyDictionary<string, string>(knobs));
            }

            if (problems.Count == 0)
            {
                // Keep first-use order: it determines lease payloads, knob application, and restore order.
                foreach (var knobs in resolved)
                {
                    foreach (var name in knobs.Keys)
                    {
                        if (!touched.Contains(name, StringComparer.Ordinal))
                        {
                            touched.Add(name);
                        }
                    }
                }

                foreach (var name in touched)
                {
                    var info = usable[name];
                    var mode = string.Equals(info.Value, info.Launch, StringComparison.Ordinal)
                        ? RestoreMode.Reset
                        : RestoreMode.SetBaseline;
                    baselines[name] = new Baseline(name, info.Apply, info.Value, mode);
                }

                // A pipeline knob is read by every part as it starts, so each run restarts the preview (which rebuilds
                // the capture) and, when Flashback was active at preflight, Flashback; recording starts after both.
                restartPreview = touched.Any(name => baselines[name].Apply is ApplyPreviewRestart or ApplyPipelineRestart);
                restartFlashback = touched.Any(name => baselines[name].Apply == ApplyFlashbackRestart
                    || (baselines[name].Apply == ApplyPipelineRestart && state.FlashbackActive));
            }
        }
        else
        {
            foreach (var _ in spec.Variants)
            {
                resolved.Add(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)));
            }
        }

        if (problems.Count > 0)
        {
            return new Result(null, Array.AsReadOnly(problems.ToArray()));
        }

        var plan = new Plan(
            Array.AsReadOnly(resolved.ToArray()),
            Array.AsReadOnly(touched.ToArray()),
            new ReadOnlyDictionary<string, Baseline>(new Dictionary<string, Baseline>(baselines, StringComparer.Ordinal)),
            restartPreview, restartFlashback);
        return new Result(plan, Array.Empty<string>());
    }

    private static string? ScopeProblem(KnobInfo info, PerfAppState state, PerfExperimentSpec spec)
    {
        switch (info.Apply)
        {
            case ApplyAppRestart:
                return $"{info.Name} is read once at app startup (Apply app-restart), so the runner cannot change it between runs. "
                    + "Relaunch the app with the value set and compare runs from separate launches instead.";
            case ApplyRecordingStart when !spec.Recording:
                return $"{info.Name} takes effect when a recording starts (Apply recording-start). Set \"recording\": true so the runner starts a recording for every run.";
            case ApplyFlashbackRestart when !state.FlashbackActive:
                return $"{info.Name} takes effect when Flashback restarts (Apply flashback-restart), but Flashback is not active. Enable Flashback first.";
            case ApplyPipelineRestart when state.Recording && !spec.Recording:
                return $"{info.Name} is read by preview, recordings and Flashback as each starts, and they share one capture (Apply pipeline-restart). "
                    + "A recording the runner did not start holds that capture, so restarting the preview would not apply the knob. Stop the recording first.";
            case ApplyLive:
            case ApplyPreviewRestart:
            case ApplyRecordingStart:
            case ApplyFlashbackRestart:
            case ApplyPipelineRestart:
                return null;
            default:
                return $"{info.Name} has an unknown apply scope '{info.Apply}', so the runner cannot tell how to apply it.";
        }
    }

    private static async Task<Dictionary<string, KnobInfo>> ReadKnobsAsync(PerfAppLink app, CancellationToken cancellationToken)
    {
        var reply = await app.RequireAsync("GetTuningKnobs", null, cancellationToken).ConfigureAwait(false);
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
                PerfJson.GetArray(knob, "Choices").Where(c => c.ValueKind == JsonValueKind.String).Select(c => c.GetString()!).ToArray(),
                PerfJson.GetString(knob, "Apply") ?? string.Empty,
                PerfJson.GetString(knob, "Value"),
                PerfJson.GetString(knob, "Launch"));
        }

        return knobs;
    }

    internal static async Task<Dictionary<string, string?>> ReadKnobValuesAsync(
        PerfAppLink app,
        CancellationToken cancellationToken)
    {
        var knobs = await ReadKnobsAsync(app, cancellationToken).ConfigureAwait(false);
        return knobs.ToDictionary(pair => pair.Key, pair => pair.Value.Value, StringComparer.OrdinalIgnoreCase);
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
                if (!double.TryParse(
                        text,
                        NumberStyles.Float | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out var number)
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
}

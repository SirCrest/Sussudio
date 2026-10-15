using System.Text.Json;

namespace Sussudio.Tools.Performance;

// Spec for a knob A/B experiment (docs/architecture/performance-telemetry.md section 7). Plain JSON:
//
//   {
//     "name": "jitter depth",              // required
//     "description": "...",                // optional
//     "warmupSeconds": 5,                  // 0..600, default 5: runs after every restart, before the measure window
//     "measureSeconds": 20,                // 1..3600, default 20
//     "repeats": 3,                        // 1..20, default 3: runs per variant; fewer than 2 can never give a verdict
//     "recording": false,                  // true: the runner records during every run (needed for recording-start knobs)
//     "metrics": ["preview.present_interval"],   // optional focus list; exact names, or a prefix ending in '*' or '.'
//     "verdictStatistic": "p99",           // mean, p50, p90, p99, p999 or max; default p99
//     "variants": [                        // 1..8; the first is the baseline every other variant is compared with
//       { "name": "A", "knobs": {} },
//       { "name": "B", "knobs": { "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH": 2 } }
//     ]
//   }
//
// Anything unrecognized is a problem, not silently ignored: a misspelled "repeat" must not run with the default.

public sealed record PerfExperimentVariant(string Name, IReadOnlyDictionary<string, string> Knobs);

/// <summary>An invalid spec. <see cref="Problems"/> lists every problem found, not just the first.</summary>
public sealed class PerfExperimentSpecException : Exception
{
    public PerfExperimentSpecException(IReadOnlyList<string> problems)
        : base("Invalid experiment spec: " + string.Join("; ", problems))
    {
        Problems = problems;
    }

    public IReadOnlyList<string> Problems { get; }
}

public sealed class PerfExperimentSpec
{
    public const int DefaultWarmupSeconds = 5;
    public const int DefaultMeasureSeconds = 20;
    public const int DefaultRepeats = 3;
    public const int MaxWarmupSeconds = 600;
    public const int MaxMeasureSeconds = 3600;
    public const int MaxRepeats = 20;
    public const int MaxVariants = 8;
    public const int MaxNameLength = 120;
    public const int MaxVariantNameLength = 120;
    public const string DefaultVerdictStatistic = "p99";

    public static readonly IReadOnlyList<string> VerdictStatistics = new[] { "mean", "p50", "p90", "p99", "p999", "max" };

    private static readonly string[] TopLevelFields =
    {
        "name", "description", "warmupSeconds", "measureSeconds", "repeats", "recording", "metrics", "verdictStatistic", "variants",
    };

    public required string Name { get; init; }

    public string? Description { get; init; }

    public int WarmupSeconds { get; init; } = DefaultWarmupSeconds;

    public int MeasureSeconds { get; init; } = DefaultMeasureSeconds;

    public int Repeats { get; init; } = DefaultRepeats;

    public bool Recording { get; init; }

    public IReadOnlyList<string> Metrics { get; init; } = Array.Empty<string>();

    public string VerdictStatistic { get; init; } = DefaultVerdictStatistic;

    public required IReadOnlyList<PerfExperimentVariant> Variants { get; init; }

    /// <summary>Reads and parses a spec file. An unreadable file is a <see cref="PerfExperimentSpecException"/> naming the path.</summary>
    public static PerfExperimentSpec Load(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new PerfExperimentSpecException(new[] { $"Cannot read the spec file '{path}': {ex.Message}" });
        }

        return Parse(json);
    }

    /// <summary>Parses spec JSON (comments and trailing commas allowed). Throws with every problem found.</summary>
    public static PerfExperimentSpec Parse(string json)
    {
        var problems = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(
                json ?? string.Empty,
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException ex)
        {
            throw new PerfExperimentSpecException(new[] { "The spec is not valid JSON: " + ex.Message });
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new PerfExperimentSpecException(new[] { "The spec must be a JSON object with a name and a list of variants." });
            }

            string? name = null;
            string? description = null;
            var warmup = DefaultWarmupSeconds;
            var measure = DefaultMeasureSeconds;
            var repeats = DefaultRepeats;
            var recording = false;
            var metrics = new List<string>();
            var statistic = DefaultVerdictStatistic;
            var variants = new List<PerfExperimentVariant>();
            var sawVariants = false;

            foreach (var property in root.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "name":
                        name = ReadName(property.Value, "name", MaxNameLength, problems);
                        break;
                    case "description":
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            description = property.Value.GetString();
                        }
                        else
                        {
                            problems.Add("description must be a string.");
                        }

                        break;
                    case "warmupSeconds":
                        warmup = ReadInt(property.Value, "warmupSeconds", 0, MaxWarmupSeconds, problems, warmup);
                        break;
                    case "measureSeconds":
                        measure = ReadInt(property.Value, "measureSeconds", 1, MaxMeasureSeconds, problems, measure);
                        break;
                    case "repeats":
                        repeats = ReadInt(property.Value, "repeats", 1, MaxRepeats, problems, repeats);
                        break;
                    case "recording":
                        if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        {
                            recording = property.Value.GetBoolean();
                        }
                        else
                        {
                            problems.Add("recording must be true or false.");
                        }

                        break;
                    case "metrics":
                        ReadMetrics(property.Value, metrics, problems);
                        break;
                    case "verdictStatistic":
                        statistic = ReadStatistic(property.Value, problems, statistic);
                        break;
                    case "variants":
                        sawVariants = true;
                        ReadVariants(property.Value, variants, problems);
                        break;
                    default:
                        problems.Add($"Unknown field '{property.Name}'. Allowed fields: {string.Join(", ", TopLevelFields)}.");
                        break;
                }
            }

            if (name is null && !problems.Any(p => p.StartsWith("name ", StringComparison.Ordinal)))
            {
                problems.Add("name is required.");
            }

            if (!sawVariants)
            {
                problems.Add("variants is required: list at least one variant ({\"name\": \"A\", \"knobs\": {}}).");
            }

            if (problems.Count > 0)
            {
                throw new PerfExperimentSpecException(problems);
            }

            return new PerfExperimentSpec
            {
                Name = name!,
                Description = description,
                WarmupSeconds = warmup,
                MeasureSeconds = measure,
                Repeats = repeats,
                Recording = recording,
                Metrics = metrics,
                VerdictStatistic = statistic,
                Variants = variants,
            };
        }
    }

    private static string? ReadName(JsonElement value, string field, int maxLength, List<string> problems)
    {
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            problems.Add($"{field} must be a non-empty string.");
            return null;
        }

        // Control characters (a tab pasted into a name) become spaces rather than failing the spec: names are labels, and
        // the performance window name derived from them is bounded and sanitized again where it is built.
        var text = new string(value.GetString()!.Select(ch => char.IsControl(ch) ? ' ' : ch).ToArray()).Trim();
        if (text.Length > maxLength)
        {
            problems.Add($"{field} must be at most {maxLength} characters.");
            return null;
        }

        return text.Length == 0 ? null : text;
    }

    private static int ReadInt(JsonElement value, string field, int min, int max, List<string> problems, int fallback)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number) || number < min || number > max)
        {
            problems.Add($"{field} must be an integer between {min} and {max}.");
            return fallback;
        }

        return number;
    }

    private static void ReadMetrics(JsonElement value, List<string> metrics, List<string> problems)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            problems.Add("metrics must be an array of metric names.");
            return;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                problems.Add("metrics must contain only non-empty metric names.");
                return;
            }

            metrics.Add(item.GetString()!.Trim());
        }
    }

    private static string ReadStatistic(JsonElement value, List<string> problems, string fallback)
    {
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim().ToLowerInvariant() : null;
        if (text is null || !VerdictStatistics.Contains(text))
        {
            problems.Add($"verdictStatistic must be one of {string.Join(", ", VerdictStatistics)}.");
            return fallback;
        }

        return text;
    }

    private static void ReadVariants(JsonElement value, List<PerfExperimentVariant> variants, List<string> problems)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            problems.Add("variants must be an array of {name, knobs} objects.");
            return;
        }

        var problemsBeforeVariants = problems.Count;
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            index++;
            var label = $"variant {index}";
            if (item.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"{label} must be an object with a name and knobs.");
                continue;
            }

            string? name = null;
            var knobs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in item.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "name":
                        name = ReadName(property.Value, label + " name", MaxVariantNameLength, problems);
                        break;
                    case "knobs":
                        ReadKnobs(property.Value, label, knobs, problems);
                        break;
                    default:
                        problems.Add($"Unknown field '{property.Name}' in {label}. Allowed fields: name, knobs.");
                        break;
                }
            }

            if (name is null)
            {
                if (!problems.Any(p => p.StartsWith(label + " name", StringComparison.Ordinal)))
                {
                    problems.Add($"{label} needs a name.");
                }

                continue;
            }

            if (variants.Any(existing => string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add($"Duplicate variant name '{name}'. Variant names must be unique.");
                continue;
            }

            variants.Add(new PerfExperimentVariant(name, knobs));
        }

        // An empty list is its own problem; skip it only when a variant was rejected above, which already explains it.
        if (variants.Count == 0 && problems.Count == problemsBeforeVariants)
        {
            problems.Add("variants must list at least one variant.");
        }

        if (variants.Count > MaxVariants)
        {
            problems.Add($"At most {MaxVariants} variants are supported; got {variants.Count}.");
        }
    }

    private static void ReadKnobs(JsonElement value, string label, Dictionary<string, string> knobs, List<string> problems)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            problems.Add($"knobs in {label} must be an object of knob name to value.");
            return;
        }

        foreach (var knob in value.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(knob.Name))
            {
                problems.Add($"{label} has a knob with an empty name.");
                continue;
            }

            string? text = knob.Value.ValueKind switch
            {
                JsonValueKind.String => knob.Value.GetString()?.Trim(),
                JsonValueKind.Number => knob.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            };
            if (string.IsNullOrEmpty(text))
            {
                problems.Add($"Knob {knob.Name} in {label} must be a non-empty string, a number or a boolean (leave a knob out to keep the baseline).");
                continue;
            }

            if (knobs.ContainsKey(knob.Name.Trim()))
            {
                problems.Add($"Knob {knob.Name} appears twice in {label}.");
                continue;
            }

            knobs[knob.Name.Trim()] = text;
        }
    }
}

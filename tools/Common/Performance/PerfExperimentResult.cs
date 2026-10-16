using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sussudio.Tools.Performance;

public enum PerfExperimentStatus
{
    /// <summary>Every scheduled run finished. Check <see cref="PerfExperimentResult.Restore"/> before trusting the app state.</summary>
    Completed,

    /// <summary>A command failed mid-experiment. Finished runs and their statistics are kept; the app was restored.</summary>
    Failed,

    /// <summary>The caller canceled (Ctrl-C, MCP cancel). Finished runs are kept; the app was restored.</summary>
    Canceled,

    /// <summary>The spec cannot run against this app as it is now. Nothing was changed.</summary>
    Rejected,
}

/// <summary>One finished run: which variant, which repeat (1-based), the performance window it measured, its raw report file.</summary>
public sealed record PerfExperimentRun(string Variant, int Repeat, string Window, string? ReportFile, double DurationMs);

/// <summary>
/// The state the app was returned to once the experiment ended, as read back from the app. <see cref="Verified"/> is true
/// only when every knob the spec touched matches its pre-experiment value, the preview is running again, and a
/// recording the runner started is stopped.
/// </summary>
public sealed record PerfExperimentRestore(
    bool Verified,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Mismatches);

public sealed record PerfExperimentSettings(
    string Name,
    string? Description,
    int WarmupSeconds,
    int MeasureSeconds,
    int Repeats,
    bool Recording,
    string VerdictStatistic,
    IReadOnlyList<PerfExperimentVariant> Variants);

public sealed record PerfExperimentResult(
    PerfExperimentStatus Status,
    PerfExperimentSettings Settings,
    string? Error,
    IReadOnlyList<string> Problems,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<PerfExperimentRun> Runs,
    IReadOnlyList<PerfExperimentMetric> Metrics,
    PerfExperimentRestore Restore,
    string StartedUtc,
    double ElapsedSeconds,
    string? OutputDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>True only for a completed experiment whose restore was read back and matched.</summary>
    [JsonIgnore]
    public bool Succeeded => Status == PerfExperimentStatus.Completed && Restore.Verified;

    /// <summary>Everything in the result as indented camel-case JSON (what <c>result.json</c> contains).</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>A plain-text report for a terminal or an agent: verdicts first, then the method, restore state and warnings.</summary>
    public string FormatSummary()
    {
        var text = new StringBuilder();
        var settings = Settings;
        text.AppendLine($"Perf experiment '{settings.Name}': {Describe(Status)}");
        if (!string.IsNullOrWhiteSpace(settings.Description))
        {
            text.AppendLine("  " + settings.Description);
        }

        if (Error is not null)
        {
            text.AppendLine("  error: " + Error);
        }

        foreach (var problem in Problems)
        {
            text.AppendLine("  problem: " + problem);
        }

        if (Status != PerfExperimentStatus.Rejected)
        {
            var recordingNote = settings.Recording ? ", recording during every run" : string.Empty;
            text.AppendLine(
                $"  {Runs.Count} run(s) of {settings.Variants.Count} variant(s); {settings.Repeats} repeat(s) planned, warmup {settings.WarmupSeconds} s, measure {settings.MeasureSeconds} s{recordingNote}, took {PerfMath.Fixed(ElapsedSeconds, 0)} s");
            text.AppendLine("  baseline: " + (settings.Variants.Count > 0 ? settings.Variants[0].Name : "none")
                + "; run order: " + (Runs.Count == 0 ? "none" : string.Join(' ', Runs.Select(run => $"{run.Variant}#{run.Repeat}"))));
            text.AppendLine(
                "  method: variants alternate direction each repeat so drift cancels; a variant is better or worse only when its "
                + $"per-repeat {settings.VerdictStatistic} range is entirely below or above the baseline's (lower is better); "
                + "ranges that overlap or touch, or fewer than 2 repeats, are inconclusive.");
        }

        if (Metrics.Count > 0)
        {
            text.AppendLine();
            foreach (var metric in OrderForReading(Metrics))
            {
                AppendMetric(text, metric);
            }
        }
        else if (Status != PerfExperimentStatus.Rejected)
        {
            text.AppendLine();
            text.AppendLine("No metric had samples in a finished run.");
        }

        text.AppendLine();
        AppendRestore(text);

        if (Warnings.Count > 0)
        {
            text.AppendLine("Warnings");
            foreach (var warning in Warnings)
            {
                text.AppendLine("  - " + warning);
            }
        }

        if (OutputDirectory is not null)
        {
            text.AppendLine("Artifacts: " + OutputDirectory);
        }

        return text.ToString();
    }

    /// <summary>The same findings as a Markdown report (what <c>summary.md</c> contains).</summary>
    public string ToMarkdown()
    {
        var text = new StringBuilder();
        var settings = Settings;
        text.AppendLine($"# Perf experiment: {settings.Name}");
        text.AppendLine();
        text.AppendLine($"Status: **{Describe(Status)}**" + (Error is null ? string.Empty : " - " + Error));
        if (!string.IsNullOrWhiteSpace(settings.Description))
        {
            text.AppendLine();
            text.AppendLine(settings.Description);
        }

        foreach (var problem in Problems)
        {
            text.AppendLine();
            text.AppendLine("- Problem: " + problem);
        }

        text.AppendLine();
        text.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{Runs.Count} run(s); {settings.Repeats} repeat(s) planned per variant; warmup {settings.WarmupSeconds} s; measure {settings.MeasureSeconds} s; verdict statistic {settings.VerdictStatistic}."));
        text.AppendLine();
        text.AppendLine("| Variant | Knobs |");
        text.AppendLine("| --- | --- |");
        foreach (var variant in settings.Variants)
        {
            var knobs = variant.Knobs.Count == 0
                ? "(baseline knobs)"
                : string.Join(", ", variant.Knobs.Select(knob => $"`{knob.Key}={knob.Value}`"));
            text.AppendLine($"| {variant.Name} | {knobs} |");
        }

        text.AppendLine();
        text.AppendLine("Run order: " + (Runs.Count == 0 ? "none" : string.Join(' ', Runs.Select(run => $"{run.Variant}#{run.Repeat}"))));
        text.AppendLine();
        text.AppendLine(
            "Method: variants alternate direction each repeat so drift cancels. A variant is better or worse only when its per-repeat "
            + $"{settings.VerdictStatistic} range is entirely below or above the baseline's (lower is better). Ranges that overlap or "
            + "touch, or fewer than 2 repeats, are inconclusive.");

        foreach (var metric in OrderForReading(Metrics))
        {
            text.AppendLine();
            text.AppendLine($"## {metric.Name} ({metric.Unit}, {metric.Statistic})");
            text.AppendLine();
            text.AppendLine("| Variant | Runs | Median | Range | Delta vs baseline | Verdict |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- |");
            foreach (var variant in metric.Variants)
            {
                var (median, range, delta) = Cells(metric, variant);
                text.AppendLine($"| {variant.Variant} | {variant.Runs} | {median} | {range} | {delta} | {variant.Verdict.Value} |");
            }
        }

        text.AppendLine();
        text.AppendLine("## Restore");
        text.AppendLine();
        AppendRestore(text);

        if (Warnings.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Warnings");
            text.AppendLine();
            foreach (var warning in Warnings)
            {
                text.AppendLine("- " + warning);
            }
        }

        return text.ToString();
    }

    private static string Describe(PerfExperimentStatus status) => status switch
    {
        PerfExperimentStatus.Completed => "completed",
        PerfExperimentStatus.Failed => "failed",
        PerfExperimentStatus.Canceled => "canceled",
        _ => "rejected (nothing was changed)",
    };

    // Metrics a reader should look at first: any worse variant, then any better/changed one, then the rest in order.
    private static IEnumerable<PerfExperimentMetric> OrderForReading(IReadOnlyList<PerfExperimentMetric> metrics)
        => metrics
            .Select((metric, index) => (metric, index))
            .OrderBy(entry => Rank(entry.metric))
            .ThenBy(entry => entry.index)
            .Select(entry => entry.metric);

    private static int Rank(PerfExperimentMetric metric)
    {
        if (metric.Variants.Any(v => v.Verdict.Value == PerfExperimentStatistics.Worse))
        {
            return 0;
        }

        return metric.Variants.Any(v => v.Verdict.Value is PerfExperimentStatistics.Better or PerfExperimentStatistics.Changed) ? 1 : 2;
    }

    private static void AppendMetric(StringBuilder text, PerfExperimentMetric metric)
    {
        text.AppendLine($"{metric.Name} ({metric.Unit}, {metric.Statistic})");
        foreach (var variant in metric.Variants)
        {
            var (median, range, delta) = Cells(metric, variant);
            text.AppendLine(
                $"  {variant.Variant.PadRight(14)} runs {variant.Runs,2}  median {median,9}  range {range,-21}"
                + (variant.Verdict.Value == PerfExperimentStatistics.Baseline ? "  baseline" : $"  {delta,-22}  {variant.Verdict.Value}"));
            if (variant.Verdict.Value != PerfExperimentStatistics.Baseline && variant.Verdict.Value != PerfExperimentStatistics.Better)
            {
                text.AppendLine("      " + variant.Verdict.Reason);
            }
        }
    }

    private static (string Median, string Range, string Delta) Cells(PerfExperimentMetric metric, PerfExperimentVariantMetric variant)
    {
        if (!variant.Stats.TryGetValue(metric.Statistic, out var stat))
        {
            return ("-", "-", "-");
        }

        var delta = "-";
        if (variant.DeltaVsFirst.TryGetValue(metric.Statistic, out var d))
        {
            delta = (d.Absolute >= 0 ? "+" : string.Empty) + Number(d.Absolute)
                + (d.Percent is { } percent ? $" ({(percent >= 0 ? "+" : string.Empty)}{PerfMath.Fixed(percent, 1)}%)" : string.Empty);
        }

        return (Number(stat.Median), $"{Number(stat.Min)} .. {Number(stat.Max)}", delta);
    }

    private static string Number(double value) => value.ToString("G5", CultureInfo.InvariantCulture);

    private void AppendRestore(StringBuilder text)
    {
        if (Restore.Verified)
        {
            text.AppendLine(Restore.Actions.Count == 0
                ? "Restore: nothing needed restoring."
                : "Restore: verified, the app is back to its pre-experiment state (" + string.Join("; ", Restore.Actions) + ").");
            return;
        }

        text.AppendLine("Restore: NOT verified. The app may still be in the experiment's state.");
        foreach (var error in Restore.Errors)
        {
            text.AppendLine("  error: " + error);
        }

        foreach (var mismatch in Restore.Mismatches)
        {
            text.AppendLine("  mismatch: " + mismatch);
        }
    }
}

using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

// Statistics for a perf experiment (docs/architecture/performance-telemetry.md section 7).
//
// Method: every run yields one window report. For each metric the verdict statistic (p99 by default) is read from
// each run, giving one value per repeat per variant. A variant is compared with the baseline (the first variant) by
// the RANGE of those per-repeat values: if the two ranges are separated the variant is better or worse (lower is
// better), if they overlap or touch nothing is claimed. That is deliberately conservative: it needs no distribution
// assumption, a few repeats are enough to be honest about run-to-run noise, and it can only say "better" when every
// repeat of one variant beat every repeat of the other. Fewer than two repeats can never support a claim.

public sealed record PerfExperimentVerdict(string Value, string Reason);

/// <summary>The per-repeat values of one statistic for one variant.</summary>
public sealed record PerfExperimentRange(IReadOnlyList<double> Values, double Min, double Max, double Median);

public sealed record PerfExperimentDelta(double Absolute, double? Percent);

public sealed record PerfExperimentVariantMetric(
    string Variant,
    int Runs,
    IReadOnlyDictionary<string, PerfExperimentRange> Stats,
    IReadOnlyDictionary<string, PerfExperimentDelta> DeltaVsFirst,
    PerfExperimentVerdict Verdict);

/// <summary>One compared quantity across variants. <see cref="Statistic"/> is the statistic the verdicts judge.</summary>
public sealed record PerfExperimentMetric(
    string Name,
    string Unit,
    string Kind,
    string Statistic,
    IReadOnlyList<PerfExperimentVariantMetric> Variants);

internal sealed record PerfMetricSample(string Name, string Unit, string Kind, IReadOnlyDictionary<string, double> Stats);

internal sealed record PerfRunSamples(int VariantIndex, int Repeat, IReadOnlyList<PerfMetricSample> Samples);

public static class PerfExperimentStatistics
{
    public const string Baseline = "baseline";
    public const string Better = "better";
    public const string Worse = "worse";
    public const string Changed = "changed";
    public const string Inconclusive = "inconclusive";

    internal const string HistogramKind = "histogram";
    internal const string CounterKind = "counter";
    internal const string ThreadKind = "thread";
    internal const string ProcessKind = "process";
    internal const string GcKind = "gc";
    internal const string GpuKind = "gpu";

    private static readonly string[] HistogramStatistics = { "mean", "p50", "p90", "p99", "p999", "max" };

    // A counter is only judged better or worse when its name says it counts something bad; a throughput counter has
    // no known direction, so a separated difference is reported as "changed" instead of being called worse.
    private static readonly string[] ProblemCounterWords =
    {
        "drop", "late", "miss", "error", "fail", "stall", "skip", "overrun", "underrun", "stale", "discard", "retry",
        "timeout", "glitch", "reset", "restart", "reject", "overflow", "starv", "backlog", "shed", "lost",
    };

    /// <summary>
    /// Compares a candidate's per-repeat values with the baseline's. Lower is better when <paramref name="lowerIsBetter"/>;
    /// otherwise a separated difference is reported as "changed". Ranges that overlap or touch, a single repeat, or a
    /// missing side are always inconclusive.
    /// </summary>
    public static PerfExperimentVerdict Judge(
        IReadOnlyList<double> baseline,
        IReadOnlyList<double> candidate,
        string statistic,
        bool lowerIsBetter = true)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        if (baseline.Count == 0)
        {
            return new PerfExperimentVerdict(Inconclusive, $"no samples of {statistic} in the baseline variant");
        }

        if (candidate.Count == 0)
        {
            return new PerfExperimentVerdict(Inconclusive, $"no samples of {statistic} in this variant");
        }

        if (baseline.Count < 2 || candidate.Count < 2)
        {
            return new PerfExperimentVerdict(
                Inconclusive,
                $"needs at least 2 repeats with samples in each variant (baseline {baseline.Count}, this variant {candidate.Count})");
        }

        var baselineMin = baseline.Min();
        var baselineMax = baseline.Max();
        var candidateMin = candidate.Min();
        var candidateMax = candidate.Max();
        var baselineRange = $"{Num(baselineMin)} to {Num(baselineMax)}";
        var candidateRange = $"{Num(candidateMin)} to {Num(candidateMax)}";

        if (candidateMax < baselineMin)
        {
            return new PerfExperimentVerdict(
                lowerIsBetter ? Better : Changed,
                $"{statistic} {candidateRange} over {candidate.Count} repeats is entirely below the baseline {baselineRange} over {baseline.Count}");
        }

        if (candidateMin > baselineMax)
        {
            return new PerfExperimentVerdict(
                lowerIsBetter ? Worse : Changed,
                $"{statistic} {candidateRange} over {candidate.Count} repeats is entirely above the baseline {baselineRange} over {baseline.Count}");
        }

        return new PerfExperimentVerdict(
            Inconclusive,
            $"{statistic} ranges overlap or touch: this variant {candidateRange}, baseline {baselineRange}");
    }

    /// <summary>True when a counter name says it counts a problem (drops, stalls, errors), so fewer is better.</summary>
    internal static bool IsProblemCounter(string name)
        => ProblemCounterWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when a metric name is selected by a focus entry: an exact name, or a prefix ending in '*' or '.'.</summary>
    internal static bool MatchesFocus(string metric, string entry)
    {
        if (entry.EndsWith('*'))
        {
            return metric.StartsWith(entry[..^1], StringComparison.OrdinalIgnoreCase);
        }

        if (entry.EndsWith('.'))
        {
            return metric.StartsWith(entry, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(metric, entry, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads the comparable quantities out of one window report (the Data of <c>PerfWindow end</c>): latency histograms,
    /// counter deltas, per-role thread CPU, process CPU, GC and GPU engine load. Anything absent or malformed is skipped.
    /// </summary>
    internal static IReadOnlyList<PerfMetricSample> ExtractSamples(JsonElement report)
    {
        var samples = new List<PerfMetricSample>();
        if (report.ValueKind != JsonValueKind.Object)
        {
            return samples;
        }

        foreach (var metric in PerfJson.GetArray(report, "Metrics"))
        {
            var name = PerfJson.GetString(metric, "Name");
            var count = PerfJson.GetDouble(metric, "Count");
            if (string.IsNullOrEmpty(name) || count is null or <= 0)
            {
                continue;
            }

            var stats = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var statistic in HistogramStatistics)
            {
                if (PerfJson.GetDouble(metric, statistic) is { } value)
                {
                    stats[statistic] = value;
                }
            }

            if (stats.Count > 0)
            {
                samples.Add(new PerfMetricSample(name, PerfJson.GetString(metric, "Unit") ?? string.Empty, HistogramKind, stats));
            }
        }

        foreach (var counter in PerfJson.GetArray(report, "Counters"))
        {
            var name = PerfJson.GetString(counter, "Name");
            var delta = PerfJson.GetDouble(counter, "Delta");
            if (string.IsNullOrEmpty(name) || delta is null)
            {
                continue;
            }

            var stats = new Dictionary<string, double>(StringComparer.Ordinal) { ["delta"] = delta.Value };
            if (PerfJson.GetDouble(counter, "RatePerSec") is { } rate)
            {
                stats["ratePerSec"] = rate;
            }

            samples.Add(new PerfMetricSample(name, "count", CounterKind, stats));
        }

        var roleCpu = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var thread in PerfJson.GetArray(report, "Threads"))
        {
            var role = PerfJson.GetString(thread, "Role");
            if (!string.IsNullOrEmpty(role) && PerfJson.GetDouble(thread, "CpuPercent") is { } cpu)
            {
                // Several threads can share a role (a worker pool); the role's load is their sum.
                roleCpu[role] = roleCpu.GetValueOrDefault(role) + cpu;
            }
        }

        foreach (var (role, cpu) in roleCpu)
        {
            samples.Add(Gauge($"thread.{role}.cpu_percent", "%", ThreadKind, cpu));
        }

        if (PerfJson.TryGet(report, "Process", out var process) && PerfJson.GetDouble(process, "CpuPercentOfOneCore") is { } processCpu)
        {
            samples.Add(Gauge("process.cpu_percent", "%", ProcessKind, processCpu));
        }

        if (PerfJson.TryGet(report, "Gc", out var gc))
        {
            if (PerfJson.GetDouble(gc, "PauseMs") is { } pause)
            {
                samples.Add(Gauge("gc.pause_ms", "ms", GcKind, pause));
            }

            if (PerfJson.GetDouble(gc, "Gen2") is { } gen2)
            {
                samples.Add(Gauge("gc.gen2_collections", "count", GcKind, gen2));
            }

            if (PerfJson.GetDouble(gc, "AllocatedBytes") is { } allocated)
            {
                samples.Add(Gauge("gc.allocated_mb", "MB", GcKind, allocated / (1024d * 1024d)));
            }
        }

        if (PerfJson.TryGet(report, "Gpu", out var gpu))
        {
            foreach (var engine in PerfJson.GetArray(gpu, "Engines"))
            {
                var type = PerfJson.GetString(engine, "Type");
                if (!string.IsNullOrEmpty(type) && PerfJson.GetDouble(engine, "MeanPercent") is { } mean)
                {
                    samples.Add(Gauge($"gpu.engine.{type}.mean_percent", "%", GpuKind, mean));
                }
            }
        }

        return samples;
    }

    /// <summary>
    /// Builds the per-metric comparison from every completed run. <paramref name="focus"/> (when not empty) limits the
    /// compared metrics; entries that matched nothing are reported through <paramref name="warnings"/>.
    /// </summary>
    internal static IReadOnlyList<PerfExperimentMetric> Compare(
        IReadOnlyList<PerfExperimentVariant> variants,
        IReadOnlyList<PerfRunSamples> runs,
        IReadOnlyList<string> focus,
        string histogramStatistic,
        List<string> warnings)
    {
        var order = new List<string>();
        var info = new Dictionary<string, PerfMetricSample>(StringComparer.Ordinal);
        foreach (var run in runs)
        {
            foreach (var sample in run.Samples)
            {
                if (info.TryAdd(sample.Name, sample))
                {
                    order.Add(sample.Name);
                }
            }
        }

        foreach (var entry in focus)
        {
            if (!order.Any(name => MatchesFocus(name, entry)))
            {
                warnings.Add($"Focus metric '{entry}' had no samples in any run, so it is not compared.");
            }
        }

        var selected = order
            .Where(name => focus.Count == 0 || focus.Any(entry => MatchesFocus(name, entry)))
            .ToList();

        var metrics = new List<PerfExperimentMetric>();
        foreach (var name in selected)
        {
            var first = info[name];
            var statistic = StatisticFor(first.Kind, histogramStatistic);
            var perVariant = new List<IReadOnlyList<PerfMetricSample?>>(variants.Count);
            for (var v = 0; v < variants.Count; v++)
            {
                perVariant.Add(runs
                    .Where(run => run.VariantIndex == v)
                    .Select(run => run.Samples.FirstOrDefault(sample => sample.Name == name))
                    .Where(sample => sample is not null)
                    .ToList());
            }

            // A counter that never fired anywhere says nothing; show it only when it was asked for by name.
            if (focus.Count == 0
                && first.Kind == CounterKind
                && perVariant.All(samples => samples.All(sample => sample!.Stats["delta"] == 0)))
            {
                continue;
            }

            metrics.Add(BuildMetric(variants, name, first, statistic, perVariant));
        }

        return metrics;
    }

    private static PerfExperimentMetric BuildMetric(
        IReadOnlyList<PerfExperimentVariant> variants,
        string name,
        PerfMetricSample first,
        string statistic,
        IReadOnlyList<IReadOnlyList<PerfMetricSample?>> perVariant)
    {
        var lowerIsBetter = first.Kind != CounterKind || IsProblemCounter(name);
        var statNames = first.Kind switch
        {
            HistogramKind => HistogramStatistics,
            CounterKind => new[] { "delta", "ratePerSec" },
            _ => new[] { "value" },
        };

        var ranges = new List<Dictionary<string, PerfExperimentRange>>();
        foreach (var samples in perVariant)
        {
            var map = new Dictionary<string, PerfExperimentRange>(StringComparer.Ordinal);
            foreach (var statName in statNames)
            {
                var values = samples
                    .Select(sample => sample!.Stats.TryGetValue(statName, out var value) ? value : double.NaN)
                    .Where(double.IsFinite)
                    .ToList();
                if (values.Count > 0)
                {
                    map[statName] = new PerfExperimentRange(
                        values.Select(PerfMath.Round).ToList(),
                        PerfMath.Round(values.Min()),
                        PerfMath.Round(values.Max()),
                        PerfMath.Round(PerfMath.Median(values)));
                }
            }

            ranges.Add(map);
        }

        var entries = new List<PerfExperimentVariantMetric>();
        for (var v = 0; v < variants.Count; v++)
        {
            var runCount = perVariant[v].Count;
            var deltas = new Dictionary<string, PerfExperimentDelta>(StringComparer.Ordinal);
            PerfExperimentVerdict verdict;
            if (v == 0)
            {
                verdict = new PerfExperimentVerdict(Baseline, $"the first variant; the others are compared with it ({runCount} runs)");
            }
            else
            {
                foreach (var (statName, range) in ranges[v])
                {
                    if (ranges[0].TryGetValue(statName, out var baselineRange))
                    {
                        var absolute = range.Median - baselineRange.Median;
                        deltas[statName] = new PerfExperimentDelta(
                            PerfMath.Round(absolute),
                            baselineRange.Median == 0 ? null : PerfMath.Round(absolute / Math.Abs(baselineRange.Median) * 100d));
                    }
                }

                var baselineValues = ranges[0].TryGetValue(statistic, out var baselineStat) ? baselineStat.Values : Array.Empty<double>();
                var candidateValues = ranges[v].TryGetValue(statistic, out var candidateStat) ? candidateStat.Values : Array.Empty<double>();
                verdict = Judge(baselineValues, candidateValues, statistic, lowerIsBetter);
            }

            entries.Add(new PerfExperimentVariantMetric(variants[v].Name, runCount, ranges[v], deltas, verdict));
        }

        return new PerfExperimentMetric(name, first.Unit, first.Kind, statistic, entries);
    }

    private static string StatisticFor(string kind, string histogramStatistic) => kind switch
    {
        HistogramKind => histogramStatistic,
        CounterKind => "delta",
        _ => "value",
    };

    private static PerfMetricSample Gauge(string name, string unit, string kind, double value)
        => new(name, unit, kind, new Dictionary<string, double>(StringComparer.Ordinal) { ["value"] = value });

    private static string Num(double value) => value.ToString("G5", CultureInfo.InvariantCulture);
}

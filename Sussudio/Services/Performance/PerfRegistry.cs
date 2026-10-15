using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Performance;

public enum PerfMetricKind
{
    Histogram,
    Counter,
}

/// <summary>Unit of the raw recorded values. Microsecond metrics are reported in milliseconds.</summary>
public enum PerfUnit
{
    Microseconds,
    Milliseconds,
    Bytes,
    Percent,
    Count,
}

/// <summary>Identity of a metric: a stable dotted name (wire contract), its kind and unit, and a one-line description.</summary>
public sealed record PerfMetricInfo(string Name, PerfMetricKind Kind, PerfUnit Unit, string Description);

/// <summary>One metric's value inside a <see cref="PerfRegistrySnapshot"/>: a histogram copy or a counter total.</summary>
public sealed class PerfMetricSample
{
    internal PerfMetricSample(PerfMetricInfo info, PerfHistogramSnapshot? histogram, long value)
    {
        Info = info;
        Histogram = histogram;
        Value = value;
    }

    public PerfMetricInfo Info { get; }

    /// <summary>Histogram copy, or null for a counter.</summary>
    public PerfHistogramSnapshot? Histogram { get; }

    /// <summary>Counter total (or delta); zero for a histogram.</summary>
    public long Value { get; }

    /// <summary>True when the metric recorded nothing (histogram count or counter value of zero).</summary>
    public bool IsIdle => Histogram is null ? Value == 0 : Histogram.Count == 0;
}

/// <summary>
/// Copy of every registered metric at one instant, or the difference of two such copies. A delta has the same
/// shape as a lifetime snapshot so reports and tools have one parser.
/// </summary>
public sealed class PerfRegistrySnapshot
{
    private readonly Dictionary<string, PerfMetricSample> _byName;

    internal PerfRegistrySnapshot(
        IReadOnlyList<PerfMetricSample> metrics,
        long timestamp,
        DateTime capturedUtc,
        bool isDelta,
        long elapsedTicks)
    {
        Metrics = metrics;
        Timestamp = timestamp;
        CapturedUtc = capturedUtc;
        IsDelta = isDelta;
        ElapsedTicks = elapsedTicks;
        _byName = new Dictionary<string, PerfMetricSample>(metrics.Count, StringComparer.Ordinal);
        foreach (var metric in metrics)
        {
            _byName[metric.Info.Name] = metric;
        }
    }

    public IReadOnlyList<PerfMetricSample> Metrics { get; }

    /// <summary>Stopwatch timestamp when the snapshot was taken.</summary>
    public long Timestamp { get; }

    public DateTime CapturedUtc { get; }

    public bool IsDelta { get; }

    /// <summary>For a delta, Stopwatch ticks between the baseline and this snapshot; zero otherwise.</summary>
    public long ElapsedTicks { get; }

    public PerfMetricSample? Find(string name) => _byName.TryGetValue(name, out var sample) ? sample : null;

    /// <summary>
    /// Difference against an earlier snapshot of the same registry. Metrics the baseline never saw count from zero;
    /// every histogram and counter is clamped at zero, so a baseline newer than this snapshot cannot go negative.
    /// </summary>
    public PerfRegistrySnapshot Subtract(PerfRegistrySnapshot baseline)
    {
        var deltas = new List<PerfMetricSample>(Metrics.Count);
        foreach (var current in Metrics)
        {
            var before = baseline.Find(current.Info.Name);
            if (current.Histogram is not null)
            {
                var histogram = before?.Histogram is { } earlier
                    ? current.Histogram.Subtract(earlier)
                    : current.Histogram.Subtract(PerfHistogramSnapshot.Empty);
                deltas.Add(new PerfMetricSample(current.Info, histogram, 0));
            }
            else
            {
                var difference = current.Value - (before?.Value ?? 0);
                deltas.Add(new PerfMetricSample(current.Info, null, difference < 0 ? 0 : difference));
            }
        }

        var elapsed = Timestamp - baseline.Timestamp;
        return new PerfRegistrySnapshot(deltas, Timestamp, CapturedUtc, isDelta: true, elapsed < 0 ? 0 : elapsed);
    }
}

/// <summary>
/// Append-only set of named histograms and counters. Registration takes a lock and happens at startup (or on the
/// first use of a dynamic name); recording touches only the returned <see cref="PerfHistogram"/> or
/// <see cref="PerfCounter"/> and never this class. <see cref="Snapshot"/> reads a copy-on-write array, no lock.
/// </summary>
public sealed class PerfRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _byName = new(StringComparer.Ordinal);
    private Entry[] _entries = Array.Empty<Entry>();
    private PerfMetricInfo[] _catalog = Array.Empty<PerfMetricInfo>();

    /// <summary>Every registered metric in registration order.</summary>
    public IReadOnlyList<PerfMetricInfo> Catalog => Volatile.Read(ref _catalog);

    /// <summary>Registers a histogram, or returns the existing one for the name. Throws when the name is a counter.</summary>
    public PerfHistogram Histogram(string name, PerfUnit unit, string description)
    {
        lock (_gate)
        {
            if (_byName.TryGetValue(name, out var existing))
            {
                return existing.Histogram
                    ?? throw new InvalidOperationException($"Perf metric '{name}' is already registered as a counter.");
            }

            var entry = new Entry(new PerfMetricInfo(name, PerfMetricKind.Histogram, unit, description), new PerfHistogram(), null);
            Add(entry);
            return entry.Histogram!;
        }
    }

    /// <summary>Registers a counter, or returns the existing one for the name. Throws when the name is a histogram.</summary>
    public PerfCounter Counter(string name, string description)
    {
        lock (_gate)
        {
            if (_byName.TryGetValue(name, out var existing))
            {
                return existing.Counter
                    ?? throw new InvalidOperationException($"Perf metric '{name}' is already registered as a histogram.");
            }

            var entry = new Entry(new PerfMetricInfo(name, PerfMetricKind.Counter, PerfUnit.Count, description), null, new PerfCounter());
            Add(entry);
            return entry.Counter!;
        }
    }

    /// <summary>Counter for a name only known at run time (for example one per drop reason).</summary>
    public PerfCounter GetOrAddCounter(string name, string description) => Counter(name, description);

    public PerfRegistrySnapshot Snapshot()
    {
        var entries = Volatile.Read(ref _entries);
        var samples = new PerfMetricSample[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            samples[i] = entry.Histogram is { } histogram
                ? new PerfMetricSample(entry.Info, histogram.Snapshot(), 0)
                : new PerfMetricSample(entry.Info, null, entry.Counter!.Read());
        }

        return new PerfRegistrySnapshot(samples, Stopwatch.GetTimestamp(), DateTime.UtcNow, isDelta: false, elapsedTicks: 0);
    }

    private void Add(Entry entry)
    {
        _byName[entry.Info.Name] = entry;

        var entries = new Entry[_entries.Length + 1];
        Array.Copy(_entries, entries, _entries.Length);
        entries[^1] = entry;

        var catalog = new PerfMetricInfo[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            catalog[i] = entries[i].Info;
        }

        Volatile.Write(ref _catalog, catalog);
        Volatile.Write(ref _entries, entries);
    }

    private sealed record Entry(PerfMetricInfo Info, PerfHistogram? Histogram, PerfCounter? Counter);
}

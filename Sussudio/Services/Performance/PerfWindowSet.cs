using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sussudio.Services.Performance;

public enum PerfWindowBeginOutcome
{
    Started,
    Restarted,
    LimitReached,
    InvalidName,
}

public readonly record struct PerfWindowBeginResult(PerfWindowBeginOutcome Outcome, int ActiveCount);

public sealed record PerfWindowInfo(string Name, DateTime StartedUtc, double ElapsedMs);

/// <summary>A window's delta since <c>begin</c>: the metric delta plus the system samples at both ends.</summary>
public sealed class PerfWindowReading<TSystem>
{
    internal PerfWindowReading(
        string name,
        DateTime startedUtc,
        long elapsedTicks,
        PerfRegistrySnapshot delta,
        TSystem baseline,
        TSystem current)
    {
        Name = name;
        StartedUtc = startedUtc;
        ElapsedTicks = elapsedTicks;
        Delta = delta;
        Baseline = baseline;
        Current = current;
    }

    public string Name { get; }

    public DateTime StartedUtc { get; }

    /// <summary>Stopwatch ticks since the window began.</summary>
    public long ElapsedTicks { get; }

    public double ElapsedMs => ElapsedTicks * 1000d / Stopwatch.Frequency;

    /// <summary>Every histogram and counter as recorded since <c>begin</c>.</summary>
    public PerfRegistrySnapshot Delta { get; }

    /// <summary>Process, thread and runtime totals captured when the window began.</summary>
    public TSystem Baseline { get; }

    /// <summary>The same totals captured now.</summary>
    public TSystem Current { get; }
}

/// <summary>
/// Up to <see cref="MaxWindows"/> named measurement windows over a <see cref="PerfRegistry"/>. <c>Begin</c>
/// captures a baseline of every metric plus a caller-defined system sample (thread CPU, process totals);
/// <c>Read</c> returns the delta without closing, <c>End</c> returns it and closes. Windows are control-plane
/// objects: they take a lock, but nothing on a recording path ever touches them.
/// </summary>
public sealed class PerfWindowSet<TSystem>
{
    public const int MaxWindows = 16;
    public const int MaxNameLength = 64;

    private readonly object _gate = new();
    private readonly Dictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly PerfRegistry _registry;
    private readonly Func<TSystem> _captureSystem;

    public PerfWindowSet(PerfRegistry registry, Func<TSystem> captureSystem)
    {
        _registry = registry;
        _captureSystem = captureSystem;
    }

    /// <summary>
    /// Starts a window. An existing name restarts from a fresh baseline and the outcome says so; a new name past
    /// the limit is rejected and leaves every existing window untouched.
    /// </summary>
    public PerfWindowBeginResult Begin(string name)
    {
        if (!IsValidName(name))
        {
            return new PerfWindowBeginResult(PerfWindowBeginOutcome.InvalidName, Count());
        }

        lock (_gate)
        {
            var exists = _windows.ContainsKey(name);
            if (!exists && _windows.Count >= MaxWindows)
            {
                return new PerfWindowBeginResult(PerfWindowBeginOutcome.LimitReached, _windows.Count);
            }

            _windows[name] = new Window(name, DateTime.UtcNow, _registry.Snapshot(), _captureSystem());
            return new PerfWindowBeginResult(
                exists ? PerfWindowBeginOutcome.Restarted : PerfWindowBeginOutcome.Started,
                _windows.Count);
        }
    }

    /// <summary>Delta since <c>begin</c> without closing the window, or null for an unknown name.</summary>
    public PerfWindowReading<TSystem>? Read(string? name)
    {
        Window? window;
        lock (_gate)
        {
            if (name is null || !_windows.TryGetValue(name, out window))
            {
                return null;
            }
        }

        return Measure(window);
    }

    /// <summary>Delta since <c>begin</c>, closing the window; null for an unknown name.</summary>
    public PerfWindowReading<TSystem>? End(string? name)
    {
        Window? window;
        lock (_gate)
        {
            if (name is null || !_windows.Remove(name, out window))
            {
                return null;
            }
        }

        return Measure(window);
    }

    /// <summary>Discards a window without a report. False when the name is unknown.</summary>
    public bool Cancel(string? name)
    {
        lock (_gate)
        {
            return name is not null && _windows.Remove(name);
        }
    }

    public IReadOnlyList<PerfWindowInfo> List()
    {
        Window[] windows;
        lock (_gate)
        {
            windows = _windows.Values.ToArray();
        }

        var now = Stopwatch.GetTimestamp();
        return windows
            .OrderBy(window => window.StartedUtc)
            .Select(window => new PerfWindowInfo(window.Name, window.StartedUtc, TicksToMs(now - window.Baseline.Timestamp)))
            .ToList();
    }

    private PerfWindowReading<TSystem> Measure(Window window)
    {
        var current = _registry.Snapshot();
        var system = _captureSystem();
        var delta = current.Subtract(window.Baseline);
        return new PerfWindowReading<TSystem>(window.Name, window.StartedUtc, delta.ElapsedTicks, delta, window.SystemSample, system);
    }

    private int Count()
    {
        lock (_gate)
        {
            return _windows.Count;
        }
    }

    private static bool IsValidName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return false;
        }

        foreach (var c in name)
        {
            if (char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }

    private static double TicksToMs(long ticks) => (ticks < 0 ? 0 : ticks) * 1000d / Stopwatch.Frequency;

    private sealed record Window(string Name, DateTime StartedUtc, PerfRegistrySnapshot Baseline, TSystem SystemSample);
}

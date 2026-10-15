using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sussudio.Services.Performance;

/// <summary>An agent-placed annotation ("variant B started") on the performance timeline.</summary>
public sealed record PerfMarker(DateTime TimestampUtc, long Timestamp, string Text);

/// <summary>
/// Bounded log of <see cref="PerfMarker"/>s. Markers are control-plane events (one per <c>perf mark</c> command),
/// so a lock is fine; the log keeps the newest <see cref="MaxMarkers"/> and truncates over-long text.
/// </summary>
public sealed class PerfMarkerLog
{
    public const int MaxMarkers = 256;
    public const int MaxTextLength = 512;

    private readonly object _gate = new();
    private readonly Queue<PerfMarker> _markers = new();

    public PerfMarker Add(string text)
    {
        var bounded = text.Length > MaxTextLength ? text.Substring(0, MaxTextLength) : text;
        var marker = new PerfMarker(DateTime.UtcNow, Stopwatch.GetTimestamp(), bounded);
        lock (_gate)
        {
            _markers.Enqueue(marker);
            while (_markers.Count > MaxMarkers)
            {
                _markers.Dequeue();
            }
        }

        return marker;
    }

    /// <summary>Markers oldest first.</summary>
    public IReadOnlyList<PerfMarker> Snapshot()
    {
        lock (_gate)
        {
            return _markers.ToArray();
        }
    }
}

/// <summary>Process-wide marker log used by <c>perf mark</c>, the perf report and trace dumps.</summary>
public static class PerfMarkers
{
    public static readonly PerfMarkerLog Log = new();

    public static PerfMarker Add(string text) => Log.Add(text);
}

/// <summary>Managed thread id to role name, so trace dumps label threads.</summary>
public sealed class PerfThreadNames
{
    private readonly ConcurrentDictionary<int, string> _names = new();

    /// <summary>Process-wide registry shared by HotThreads registration and trace dumps.</summary>
    public static PerfThreadNames Default { get; } = new();

    public void Set(int managedThreadId, string name) => _names[managedThreadId] = name;

    public IReadOnlyDictionary<int, string> Snapshot() => new Dictionary<int, string>(_names);
}

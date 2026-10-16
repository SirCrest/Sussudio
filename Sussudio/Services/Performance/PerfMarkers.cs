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

/// <summary>
/// Managed thread id to role name, so trace dumps label threads. A name belongs to one registration, not to the id:
/// managed ids are recycled and pool threads run many roles in turn, so a name that is never removed would label later
/// work on the same thread with a role it no longer has. <see cref="Retire"/> removes the live name only while the id
/// still maps to the retiring registration, and keeps a bounded dump-time label "&lt;role&gt; (retired)" so events a
/// retired thread left in the trace ring are still named, honestly marked.
/// </summary>
public sealed class PerfThreadNames
{
    /// <summary>Retired labels kept for dumps; the oldest is dropped past this.</summary>
    public const int MaxRetiredLabels = 256;

    /// <summary>Suffix a dump appends to the role of a thread that has exited.</summary>
    public const string RetiredSuffix = " (retired)";

    private readonly ConcurrentDictionary<int, ThreadNameEntry> _names = new();
    private readonly object _retiredGate = new();
    private readonly Dictionary<int, RetiredLabel> _retired = new();
    private long _retiredSerial;

    /// <summary>Process-wide registry shared by HotThreads registration and trace dumps.</summary>
    public static PerfThreadNames Default { get; } = new();

    /// <summary>
    /// Names a thread. The returned entry identifies this registration: pass it to <see cref="Retire"/> when the thread
    /// ends. Naming an id again replaces the previous name (the id was reused) and forgets any retired label for it.
    /// </summary>
    public ThreadNameEntry Set(int managedThreadId, string name)
    {
        var entry = new ThreadNameEntry(name);
        _names[managedThreadId] = entry;
        lock (_retiredGate)
        {
            _retired.Remove(managedThreadId);
        }

        return entry;
    }

    /// <summary>
    /// Ends a registration. The live name is removed only if the id still maps to <paramref name="entry"/> (an
    /// atomic compare-and-remove), so a thread that reused the id and registered since is left alone. Returns whether
    /// this call removed the name.
    /// </summary>
    public bool Retire(int managedThreadId, ThreadNameEntry entry)
    {
        if (!((ICollection<KeyValuePair<int, ThreadNameEntry>>)_names).Remove(new KeyValuePair<int, ThreadNameEntry>(managedThreadId, entry)))
        {
            return false;
        }

        lock (_retiredGate)
        {
            _retired[managedThreadId] = new RetiredLabel(entry.Name + RetiredSuffix, ++_retiredSerial);
            while (_retired.Count > MaxRetiredLabels)
            {
                var oldestId = 0;
                var oldestSerial = long.MaxValue;
                foreach (var pair in _retired)
                {
                    if (pair.Value.Serial < oldestSerial)
                    {
                        oldestSerial = pair.Value.Serial;
                        oldestId = pair.Key;
                    }
                }

                _retired.Remove(oldestId);
            }
        }

        return true;
    }

    /// <summary>Live names plus, for ids with no live name, the retired label ("&lt;role&gt; (retired)").</summary>
    public IReadOnlyDictionary<int, string> Snapshot()
    {
        var names = new Dictionary<int, string>(_names.Count);
        foreach (var pair in _names)
        {
            names[pair.Key] = pair.Value.Name;
        }

        lock (_retiredGate)
        {
            foreach (var pair in _retired)
            {
                names.TryAdd(pair.Key, pair.Value.Label);
            }
        }

        return names;
    }

    private readonly record struct RetiredLabel(string Label, long Serial);
}

/// <summary>Identity of one thread-name registration (compared by reference, never by name).</summary>
public sealed class ThreadNameEntry
{
    internal ThreadNameEntry(string name) => Name = name;

    public string Name { get; }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sussudio.Services.Performance;

public sealed record LogTapCount(string Key, long Count);

public sealed record LogTapCounts(long TotalLines, IReadOnlyList<LogTapCount> ByCaller, IReadOnlyList<LogTapCount> ByTag);

/// <summary>
/// Bounded ring of the most recent log lines plus per-caller and per-tag line counts, so an agent can read recent
/// log output and spot log spam without opening the log file. Fed by the logger's single writer task with each entry
/// in the logger's format <c>[HH:mm:ss.fff] [caller] message</c>; a tag is the leading UPPER_SNAKE token of the
/// message (or a leading <c>[EVENT-ID]</c>). Adding a line allocates nothing for keys already seen: lines are stored
/// by reference and counted through span-keyed tables. Distinct keys are bounded; overflow lands in
/// <see cref="OverflowKey"/> so the totals still add up.
/// </summary>
public sealed class LogTapBuffer
{
    public const int DefaultCapacity = 4096;
    public const int MaxDistinctKeys = 256;
    public const string OverflowKey = "<other>";
    public const string UnknownCaller = "<unknown>";

    private readonly object _gate = new();
    private readonly string[] _ring;
    private readonly KeyCounter _callers = new();
    private readonly KeyCounter _tags = new();
    private long _total;

    public LogTapBuffer(int capacity = DefaultCapacity)
    {
        _ring = new string[Math.Max(1, capacity)];
    }

    public int Capacity => _ring.Length;

    /// <summary>Records one logger entry. Never throws.</summary>
    public void Add(string? entry)
    {
        entry ??= string.Empty;
        ParseEntry(entry, out var caller, out var tag);
        lock (_gate)
        {
            _ring[(int)(_total % _ring.Length)] = entry;
            _total++;
            _callers.Increment(caller.IsEmpty ? UnknownCaller.AsSpan() : caller);
            if (!tag.IsEmpty)
            {
                _tags.Increment(tag);
            }
        }
    }

    /// <summary>
    /// The newest <paramref name="max"/> lines (clamped to 1..capacity) whose text contains
    /// <paramref name="contains"/> (case-insensitive), oldest first, without trailing newlines.
    /// </summary>
    public IReadOnlyList<string> Tail(int max, string? contains)
    {
        max = Math.Clamp(max, 1, _ring.Length);
        string[] snapshot;
        long total;
        lock (_gate)
        {
            total = _total;
            var count = (int)Math.Min(total, _ring.Length);
            snapshot = new string[count];
            for (var i = 0; i < count; i++)
            {
                snapshot[i] = _ring[(int)((total - count + i) % _ring.Length)];
            }
        }

        var matches = new List<string>(Math.Min(max, snapshot.Length));
        var filter = string.IsNullOrEmpty(contains) ? null : contains;
        for (var i = snapshot.Length - 1; i >= 0 && matches.Count < max; i--)
        {
            var line = snapshot[i];
            if (filter is null || line.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(line.TrimEnd('\n', '\r'));
            }
        }

        matches.Reverse();
        return matches;
    }

    /// <summary>Total lines seen and the per-caller and per-tag counts, each sorted by count descending.</summary>
    public LogTapCounts Counts()
    {
        lock (_gate)
        {
            return new LogTapCounts(_total, _callers.Snapshot(), _tags.Snapshot());
        }
    }

    /// <summary>Splits a logger entry into caller and tag spans. Empty spans mean "not present".</summary>
    internal static void ParseEntry(string entry, out ReadOnlySpan<char> caller, out ReadOnlySpan<char> tag)
    {
        caller = default;
        tag = default;
        var text = entry.AsSpan();
        while (text.Length > 0 && (text[text.Length - 1] == '\n' || text[text.Length - 1] == '\r'))
        {
            text = text.Slice(0, text.Length - 1);
        }

        // "[HH:mm:ss.fff] [caller] message"
        if (text.Length < 4 || text[0] != '[')
        {
            return;
        }

        var timestampEnd = text.IndexOf(']');
        if (timestampEnd < 0 || timestampEnd + 2 >= text.Length || text[timestampEnd + 1] != ' ' || text[timestampEnd + 2] != '[')
        {
            return;
        }

        var afterTimestamp = text.Slice(timestampEnd + 3);
        var callerEnd = afterTimestamp.IndexOf(']');
        if (callerEnd < 0)
        {
            return;
        }

        caller = afterTimestamp.Slice(0, callerEnd);
        var message = afterTimestamp.Slice(callerEnd + 1);
        if (!message.IsEmpty && message[0] == ' ')
        {
            message = message.Slice(1);
        }

        tag = ExtractTag(message);
    }

    private static ReadOnlySpan<char> ExtractTag(ReadOnlySpan<char> message)
    {
        if (message.IsEmpty)
        {
            return default;
        }

        // LogEvent form: "[EVENT-ID] message".
        if (message[0] == '[')
        {
            var close = message.IndexOf(']');
            if (close > 3)
            {
                var inner = message.Slice(1, close - 1);
                if (IsEventId(inner))
                {
                    return inner;
                }
            }

            return default;
        }

        // Leading UPPER_SNAKE token: [A-Z][A-Z0-9_]*, with at least one underscore, ended by a delimiter.
        if (message[0] < 'A' || message[0] > 'Z')
        {
            return default;
        }

        var length = 0;
        var underscores = 0;
        while (length < message.Length && IsUpperSnakeChar(message[length]))
        {
            if (message[length] == '_')
            {
                underscores++;
            }

            length++;
        }

        if (underscores == 0 || length < 3)
        {
            return default;
        }

        if (length < message.Length && message[length] is not (' ' or ':' or '=' or ',' or '\n' or '\r'))
        {
            return default;
        }

        return message.Slice(0, length);
    }

    private static bool IsUpperSnakeChar(char c) => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

    private static bool IsEventId(ReadOnlySpan<char> text)
    {
        if (text.Length < 3 || text[0] < 'A' || text[0] > 'Z')
        {
            return false;
        }

        foreach (var c in text)
        {
            if (!IsUpperSnakeChar(c) && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Open-addressing counter keyed by character spans; allocates only when a new key first appears.</summary>
    private sealed class KeyCounter
    {
        private const int TableSize = 1024; // power of two, at least 4x MaxDistinctKeys so probes stay short
        private readonly string?[] _keys = new string?[TableSize];
        private readonly long[] _counts = new long[TableSize];
        private int _distinct;
        private long _overflow;

        public void Increment(ReadOnlySpan<char> key)
        {
            var slot = (int)(Hash(key) & (TableSize - 1));
            while (true)
            {
                var existing = _keys[slot];
                if (existing is null)
                {
                    if (_distinct >= MaxDistinctKeys)
                    {
                        _overflow++;
                        return;
                    }

                    _keys[slot] = key.ToString();
                    _counts[slot] = 1;
                    _distinct++;
                    return;
                }

                if (key.SequenceEqual(existing))
                {
                    _counts[slot]++;
                    return;
                }

                slot = (slot + 1) & (TableSize - 1);
            }
        }

        public IReadOnlyList<LogTapCount> Snapshot()
        {
            var result = new List<LogTapCount>(_distinct + 1);
            for (var i = 0; i < TableSize; i++)
            {
                if (_keys[i] is { } key)
                {
                    result.Add(new LogTapCount(key, _counts[i]));
                }
            }

            if (_overflow > 0)
            {
                result.Add(new LogTapCount(OverflowKey, _overflow));
            }

            return result.OrderByDescending(c => c.Count).ThenBy(c => c.Key, StringComparer.Ordinal).ToList();
        }

        private static uint Hash(ReadOnlySpan<char> key)
        {
            // FNV-1a over UTF-16 code units.
            var hash = 2166136261u;
            foreach (var c in key)
            {
                hash = (hash ^ c) * 16777619u;
            }

            return hash;
        }
    }
}

using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

/// <summary>Name of an event stage plus what the analyzer decided it is. Cached per distinct name.</summary>
internal sealed class NameInfo
{
    private static readonly HashSet<string> AmbientNames = new(StringComparer.Ordinal)
    {
        "marker",
        "flashback.segment_rotation",
        "flashback.playback_decode",
        "flashback.playback_present",
    };

    private static readonly string[] AmbientPrefixes = { "ui.", "gc.", "automation.", "audio.", "hub.", "system." };

    public NameInfo(string name, byte[]? utf8)
    {
        Name = name;
        Utf8 = utf8;
        IsMarker = name == "marker";
        IsSampleTime = name == "capture.sample_time";
        IsAmbient = IsAmbientName(name);
        IsDrop = name.Contains("drop", StringComparison.OrdinalIgnoreCase);
        IsAggregate = name.EndsWith(".pipeline", StringComparison.Ordinal) || name.EndsWith(".render_total", StringComparison.Ordinal);
    }

    public string Name { get; }

    public byte[]? Utf8 { get; }

    public NameInfo? Next { get; set; }

    public bool IsMarker { get; }

    public bool IsSampleTime { get; }

    /// <summary>Stages that are never part of a frame, by name: GC, UI stalls, automation, audio, markers, Flashback playback.</summary>
    public bool IsAmbient { get; }

    public bool IsDrop { get; }

    /// <summary>A span that covers other stages (pipeline, render total). Never named as the stage that grew.</summary>
    public bool IsAggregate { get; }

    public int StageIndex { get; set; } = -1;

    private static bool IsAmbientName(string name)
    {
        if (AmbientNames.Contains(name))
        {
            return true;
        }

        foreach (var prefix in AmbientPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class NameInterner
{
    private const int MaxEntries = 2048;

    private readonly Dictionary<int, NameInfo> _buckets = new();
    private int _count;

    public NameInfo Get(ReadOnlySpan<byte> utf8)
    {
        var hash = Hash(utf8);
        _buckets.TryGetValue(hash, out var head);
        for (var entry = head; entry is not null; entry = entry.Next)
        {
            if (entry.Utf8 is not null && utf8.SequenceEqual(entry.Utf8))
            {
                return entry;
            }
        }

        var info = new NameInfo(Encoding.UTF8.GetString(utf8), utf8.ToArray());
        if (_count < MaxEntries)
        {
            info.Next = head;
            _buckets[hash] = info;
            _count++;
        }

        return info;
    }

    public NameInfo FromText(string name) => new(name, utf8: null);

    private static int Hash(ReadOnlySpan<byte> bytes)
    {
        var hash = 17;
        foreach (var value in bytes)
        {
            hash = unchecked((hash * 31) + value);
        }

        return hash;
    }
}

/// <summary>Consumes fields local to one trace event and folds completed events into the shared capture.</summary>
internal sealed class FrameTraceEventParser
{
    private enum EventKey : byte
    {
        None,
        Name,
        Ph,
        Ts,
        Dur,
        Args,
        Other,
    }

    private enum ArgKey : byte
    {
        None,
        Id,
        Text,
        Reason,
        Other,
    }

    private struct PendingEvent
    {
        public EventKey EventKey;
        public ArgKey ArgKey;
        public NameInfo? Name;
        public char Phase;
        public double Timestamp;
        public double Duration;
        public bool HasTimestamp;
        public bool HasDuration;
        public long Id;
        public string? Text;
        public string? Reason;
        public bool InArgs;
    }

    private readonly FrameTraceCapture _capture;
    private readonly NameInterner _names = new();
    private PendingEvent _pending;

    public FrameTraceEventParser(FrameTraceCapture capture)
    {
        _capture = capture;
    }

    public void StartEvent()
    {
        _pending = new PendingEvent { Id = -1 };
    }

    public void HandleToken(ref Utf8JsonReader reader, JsonTokenType token, int depth)
    {
        if (depth == 3)
        {
            switch (token)
            {
                case JsonTokenType.PropertyName:
                    _pending.EventKey = ClassifyEventKey(ref reader);
                    break;
                case JsonTokenType.StartObject:
                    _pending.InArgs = _pending.EventKey == EventKey.Args;
                    break;
                case JsonTokenType.EndObject:
                    _pending.InArgs = false;
                    break;
                case JsonTokenType.StartArray:
                case JsonTokenType.EndArray:
                    break;
                default:
                    ApplyEventScalar(ref reader, token);
                    break;
            }
        }
        else if (depth == 4 && _pending.InArgs)
        {
            if (token == JsonTokenType.PropertyName)
            {
                _pending.ArgKey = reader.ValueTextEquals("id"u8)
                    ? ArgKey.Id
                    : reader.ValueTextEquals("text"u8)
                        ? ArgKey.Text
                        : reader.ValueTextEquals("reason"u8) ? ArgKey.Reason : ArgKey.Other;
            }
            else
            {
                ApplyArgScalar(ref reader, token);
            }
        }
    }

    public void CompleteEvent()
    {
        var pending = _pending;
        StartEvent();
        ProcessEvent(in pending);
    }

    private static EventKey ClassifyEventKey(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("name"u8))
        {
            return EventKey.Name;
        }

        if (reader.ValueTextEquals("ph"u8))
        {
            return EventKey.Ph;
        }

        if (reader.ValueTextEquals("ts"u8))
        {
            return EventKey.Ts;
        }

        if (reader.ValueTextEquals("dur"u8))
        {
            return EventKey.Dur;
        }

        return reader.ValueTextEquals("args"u8) ? EventKey.Args : EventKey.Other;
    }

    private void ApplyEventScalar(ref Utf8JsonReader reader, JsonTokenType token)
    {
        switch (_pending.EventKey)
        {
            case EventKey.Name:
                if (token == JsonTokenType.String)
                {
                    _pending.Name = reader.ValueIsEscaped
                        ? _names.FromText(reader.GetString() ?? string.Empty)
                        : _names.Get(reader.ValueSpan);
                }
                break;
            case EventKey.Ph:
                if (token == JsonTokenType.String)
                {
                    var span = reader.ValueSpan;
                    _pending.Phase = !reader.ValueIsEscaped && span.Length == 1 ? (char)span[0] : '?';
                }
                break;
            case EventKey.Ts:
                if (token == JsonTokenType.Number && reader.TryGetDouble(out var ts))
                {
                    _pending.Timestamp = ts;
                    _pending.HasTimestamp = true;
                }
                break;
            case EventKey.Dur:
                if (token == JsonTokenType.Number && reader.TryGetDouble(out var dur))
                {
                    _pending.Duration = dur;
                    _pending.HasDuration = true;
                }
                break;
        }
    }

    private void ApplyArgScalar(ref Utf8JsonReader reader, JsonTokenType token)
    {
        switch (_pending.ArgKey)
        {
            case ArgKey.Id:
                if (FrameTraceReader.TryReadLong(ref reader, token, out var id))
                {
                    _pending.Id = id;
                }
                break;
            case ArgKey.Text:
                if (token == JsonTokenType.String)
                {
                    _pending.Text = reader.GetString();
                }
                break;
            case ArgKey.Reason:
                // A non-string reason does not invalidate its drop event.
                if (token == JsonTokenType.String)
                {
                    _pending.Reason = reader.GetString();
                }
                break;
        }
    }

    private void ProcessEvent(in PendingEvent pending)
    {
        if (pending.Phase == 'M')
        {
            return;
        }

        var isSpan = pending.Phase == 'X';
        var isInstant = pending.Phase is 'i' or 'I';
        if (pending.Name is null || !pending.HasTimestamp || (!isSpan && !isInstant) || (isSpan && !pending.HasDuration))
        {
            _capture.SkippedEvents++;
            return;
        }

        _capture.ParsedEvents++;
        var info = pending.Name;
        var startUs = pending.Timestamp;
        var durUs = isSpan ? Math.Max(0d, pending.Duration) : 0d;
        var endUs = startUs + durUs;
        if (startUs < _capture.MinUs)
        {
            _capture.MinUs = startUs;
        }

        if (endUs > _capture.MaxUs)
        {
            _capture.MaxUs = endUs;
        }

        if (info.IsMarker)
        {
            _capture.AddAmbient(info.Name, startUs, endUs, pending.Text);
            return;
        }

        if (isInstant)
        {
            ProcessInstant(info, pending.Id, startUs, endUs, pending.Text, pending.Reason);
            return;
        }

        ProcessSpan(info, pending.Id, startUs, durUs, endUs, pending.Text);
    }

    private void ProcessInstant(NameInfo info, long id, double startUs, double endUs, string? text, string? reason)
    {
        if (info.IsSampleTime && id >= 0)
        {
            var sampled = _capture.GetFrame(id);
            sampled.SampleUs = startUs;
            sampled.HasSample = true;
            sampled.TouchInstant(startUs);
            return;
        }

        if (info.IsDrop)
        {
            _capture.AddDrop(info.Name, reason);
            if (id >= 0 && !info.IsAmbient)
            {
                var dropped = _capture.GetFrame(id);
                dropped.Drops++;
                dropped.DropReason ??= string.IsNullOrWhiteSpace(reason) ? null : reason;
                dropped.TouchInstant(startUs);
            }

            return;
        }

        _capture.AddAmbient(info.Name, startUs, endUs, text);
    }

    private void ProcessSpan(NameInfo info, long id, double startUs, double durUs, double endUs, string? text)
    {
        if (id < 0 || info.IsAmbient)
        {
            _capture.AddAmbient(info.Name, startUs, endUs, text);
            return;
        }

        _capture.GetFrame(id).AddSpan(_capture.StageIndexFor(info), startUs, durUs);
    }
}

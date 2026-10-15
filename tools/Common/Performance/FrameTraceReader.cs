using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

// Streaming reader for the Chrome Trace Event JSON that FrameTraceRecorder writes. It never builds a JSON DOM: it walks
// tokens in a chunked buffer (so a multi-hundred-megabyte dump costs the accumulators, not the document), folds each
// event into per-frame accumulators as it is read, and classifies every failure into FrameTraceFormatException.
// Times stay in microseconds relative to the dump's origin; converting the absolute frame id (a QPC tick) to that
// domain needs qpcFrequency and originQpc, which the dump writes last, so conversion happens after the read.

internal readonly record struct AmbientEvent(double StartUs, double EndUs, string Name, string? Text);

internal sealed class AmbientAcc
{
    public int Count;
    public double TotalUs;
    public double MaxUs;
}

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
        IsAmbient = AmbientNames.Contains(name) || AmbientPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
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

/// <summary>Everything one frame did, folded from its events: per-stage duration, first start and last end.</summary>
internal sealed class FrameAcc
{
    private const int InitialStages = 12;

    public FrameAcc(long id)
    {
        Id = id;
        DurUs = new double[InitialStages];
        StartUs = new double[InitialStages];
        EndUs = new double[InitialStages];
        Array.Fill(StartUs, double.PositiveInfinity);
        Array.Fill(EndUs, double.NegativeInfinity);
    }

    public long Id { get; }

    public double FirstSpanUs { get; private set; } = double.PositiveInfinity;

    public double FirstAnyUs { get; private set; } = double.PositiveInfinity;

    public double LastEndUs { get; private set; } = double.NegativeInfinity;

    public double SampleUs { get; set; }

    public bool HasSample { get; set; }

    public int Drops { get; set; }

    /// <summary>The first drop reason recorded for this frame (args.reason of its preview.drop instant), if any.</summary>
    public string? DropReason { get; set; }

    public double ArrivalUs { get; set; }

    public double ArrivalIdUs { get; set; }

    public bool IdPlausible { get; set; }

    public double[] DurUs { get; private set; }

    public double[] StartUs { get; private set; }

    public double[] EndUs { get; private set; }

    public bool Has(int stage) => stage < StartUs.Length && !double.IsPositiveInfinity(StartUs[stage]);

    public void AddSpan(int stage, double startUs, double durUs)
    {
        if (stage >= StartUs.Length)
        {
            Grow(stage + 1);
        }

        DurUs[stage] += durUs;
        if (startUs < StartUs[stage])
        {
            StartUs[stage] = startUs;
        }

        var end = startUs + durUs;
        if (end > EndUs[stage])
        {
            EndUs[stage] = end;
        }

        Touch(startUs, end, isSpan: true);
    }

    public void TouchInstant(double us) => Touch(us, us, isSpan: false);

    private void Touch(double startUs, double endUs, bool isSpan)
    {
        if (startUs < FirstAnyUs)
        {
            FirstAnyUs = startUs;
        }

        if (isSpan && startUs < FirstSpanUs)
        {
            FirstSpanUs = startUs;
        }

        if (endUs > LastEndUs)
        {
            LastEndUs = endUs;
        }
    }

    private void Grow(int needed)
    {
        var size = Math.Max(needed, StartUs.Length * 2);
        var oldLength = StartUs.Length;
        DurUs = Resize(DurUs, size, 0d);
        StartUs = Resize(StartUs, size, double.PositiveInfinity);
        EndUs = Resize(EndUs, size, double.NegativeInfinity);
        _ = oldLength;
    }

    private static double[] Resize(double[] source, int size, double fill)
    {
        var grown = new double[size];
        Array.Copy(source, grown, source.Length);
        Array.Fill(grown, fill, source.Length, size - source.Length);
        return grown;
    }
}

internal sealed class FrameTraceCapture
{
    public const int MaxRetainedAmbientEvents = 500_000;

    public long ParsedEvents { get; set; }

    public long SkippedEvents { get; set; }

    public bool SawMetadataBlock { get; set; }

    public bool SawOrigin { get; set; }

    public double QpcFrequency { get; set; }

    public long OriginQpc { get; set; }

    public string? StartedUtc { get; set; }

    public long? Capacity { get; set; }

    public long? Recorded { get; set; }

    public long? Retained { get; set; }

    public long Overwritten { get; set; }

    public bool? Running { get; set; }

    public double MinUs { get; set; } = double.PositiveInfinity;

    public double MaxUs { get; set; } = double.NegativeInfinity;

    /// <summary>qpcFrequency and originQpc both present and usable, so a frame id can be placed on the trace time axis.</summary>
    public bool HasMetadata => SawMetadataBlock && SawOrigin && QpcFrequency > 0;

    public Dictionary<long, FrameAcc> Frames { get; } = new();

    public List<string> StageNames { get; } = new();

    public List<bool> StageAggregate { get; } = new();

    public Dictionary<string, int> StageIndexByName { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, AmbientAcc> Ambient { get; } = new(StringComparer.Ordinal);

    public List<AmbientEvent> AmbientEvents { get; } = new();

    public bool AmbientTruncated { get; set; }

    public Dictionary<string, int> Drops { get; } = new(StringComparer.Ordinal);

    public FrameAcc GetFrame(long id)
    {
        if (!Frames.TryGetValue(id, out var frame))
        {
            frame = new FrameAcc(id);
            Frames[id] = frame;
        }

        return frame;
    }

    public int StageIndexFor(NameInfo info)
    {
        if (info.StageIndex >= 0)
        {
            return info.StageIndex;
        }

        if (!StageIndexByName.TryGetValue(info.Name, out var index))
        {
            index = StageNames.Count;
            StageNames.Add(info.Name);
            StageAggregate.Add(info.IsAggregate);
            StageIndexByName[info.Name] = index;
        }

        info.StageIndex = index;
        return index;
    }

    public void AddAmbient(string name, double startUs, double endUs, string? text)
    {
        if (!Ambient.TryGetValue(name, out var acc))
        {
            acc = new AmbientAcc();
            Ambient[name] = acc;
        }

        var durUs = Math.Max(0d, endUs - startUs);
        acc.Count++;
        acc.TotalUs += durUs;
        if (durUs > acc.MaxUs)
        {
            acc.MaxUs = durUs;
        }

        if (AmbientEvents.Count < MaxRetainedAmbientEvents)
        {
            AmbientEvents.Add(new AmbientEvent(startUs, endUs, name, text));
        }
        else
        {
            AmbientTruncated = true;
        }
    }

    /// <summary>Distinct reasons kept per drop name; a trace with more folds the rest into "other" so memory stays bounded.</summary>
    public const int MaxDropReasonsPerName = 32;

    public Dictionary<string, Dictionary<string, int>> DropReasons { get; } = new(StringComparer.Ordinal);

    public void AddDrop(string name, string? reason)
    {
        Drops.TryGetValue(name, out var count);
        Drops[name] = count + 1;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        if (!DropReasons.TryGetValue(name, out var reasons))
        {
            reasons = new Dictionary<string, int>(StringComparer.Ordinal);
            DropReasons[name] = reasons;
        }

        var key = reason.Length > 64 ? reason[..64] : reason;
        if (!reasons.ContainsKey(key) && reasons.Count >= MaxDropReasonsPerName)
        {
            key = "other";
        }

        reasons.TryGetValue(key, out var reasonCount);
        reasons[key] = reasonCount + 1;
    }
}

internal static class FrameTraceReader
{
    private const int InitialBufferBytes = 64 * 1024;
    private const int MaxBufferBytes = 256 * 1024 * 1024;

    public static FrameTraceCapture Read(Stream stream)
    {
        var handler = new TokenHandler();
        var buffer = ArrayPool<byte>.Shared.Rent(InitialBufferBytes);
        try
        {
            var dataLength = 0;
            var trackedFrom = 0;
            var eof = false;
            var first = true;
            var readerState = default(JsonReaderState);
            long consumedTotal = 0;
            byte firstNonWhitespace = 0;
            byte lastNonWhitespace = 0;

            while (true)
            {
                while (!eof && dataLength < buffer.Length)
                {
                    var read = stream.Read(buffer, dataLength, buffer.Length - dataLength);
                    if (read <= 0)
                    {
                        eof = true;
                        break;
                    }

                    dataLength += read;
                }

                if (first)
                {
                    first = false;
                    if (dataLength >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                    {
                        Buffer.BlockCopy(buffer, 3, buffer, 0, dataLength - 3);
                        dataLength -= 3;
                    }
                }

                for (var i = trackedFrom; i < dataLength; i++)
                {
                    var value = buffer[i];
                    if (value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                    {
                        continue;
                    }

                    if (firstNonWhitespace == 0)
                    {
                        firstNonWhitespace = value;
                    }

                    lastNonWhitespace = value;
                }

                trackedFrom = dataLength;

                if (eof && firstNonWhitespace == 0)
                {
                    throw new FrameTraceFormatException("The frame trace is empty (no JSON content).");
                }

                if (firstNonWhitespace != 0 && firstNonWhitespace != (byte)'{' && consumedTotal == 0)
                {
                    throw new FrameTraceFormatException(
                        "The content is not a Chrome Trace Event JSON object (it does not start with '{'). Expected a dump from 'perf trace dump'.");
                }

                var reader = new Utf8JsonReader(new ReadOnlySpan<byte>(buffer, 0, dataLength), eof, readerState);
                try
                {
                    while (reader.Read())
                    {
                        handler.Handle(ref reader);
                    }
                }
                catch (JsonException ex)
                {
                    var offset = consumedTotal + reader.BytesConsumed;
                    throw Classify(handler, ex, eof, lastNonWhitespace, AllWhitespace(buffer, (int)reader.BytesConsumed, dataLength), offset);
                }

                var consumed = (int)reader.BytesConsumed;
                consumedTotal += consumed;
                readerState = reader.CurrentState;
                if (eof)
                {
                    break;
                }

                var leftover = dataLength - consumed;
                if (leftover >= buffer.Length)
                {
                    if (buffer.Length >= MaxBufferBytes)
                    {
                        throw new FrameTraceFormatException("A single JSON token in the trace is larger than the supported limit.");
                    }

                    var bigger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, consumed, bigger, 0, leftover);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = bigger;
                }
                else if (leftover > 0 && consumed > 0)
                {
                    Buffer.BlockCopy(buffer, consumed, buffer, 0, leftover);
                }

                dataLength = leftover;
                trackedFrom = leftover;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (!handler.RootClosed)
        {
            throw new FrameTraceFormatException("The frame trace is truncated: the JSON ends before the closing brace.");
        }

        if (!handler.SawEventsArray)
        {
            throw new FrameTraceFormatException(
                "The JSON has no \"traceEvents\" array, so it is not a frame trace dump (expected output of 'perf trace dump').");
        }

        return handler.Capture;
    }

    private static bool AllWhitespace(byte[] buffer, int from, int to)
    {
        for (var i = Math.Max(0, from); i < to; i++)
        {
            if (buffer[i] is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
            {
                return false;
            }
        }

        return true;
    }

    private static FrameTraceFormatException Classify(
        TokenHandler handler,
        JsonException ex,
        bool finalBlock,
        byte lastNonWhitespace,
        bool nothingLeftToRead,
        long offset)
    {
        if (handler.RootClosed)
        {
            return new FrameTraceFormatException($"Unexpected content after the end of the trace JSON (byte {offset}).", ex);
        }

        if (finalBlock && (nothingLeftToRead || lastNonWhitespace != (byte)'}'))
        {
            return new FrameTraceFormatException(
                $"The frame trace is truncated (or corrupt): the JSON ends before its closing braces, after {offset} bytes. "
                + "A dump that was interrupted or is still being written looks like this.",
                ex);
        }

        return new FrameTraceFormatException($"The frame trace is not valid JSON near byte {offset}: {ex.Message}", ex);
    }

    private enum Mode
    {
        None,
        Events,
        Meta,
    }

    private enum EventKey
    {
        None,
        Name,
        Ph,
        Ts,
        Dur,
        Args,
        Other,
    }

    private enum ArgKey
    {
        None,
        Id,
        Text,
        Reason,
        Other,
    }

    private enum MetaKey
    {
        None,
        Frequency,
        Origin,
        StartedUtc,
        Capacity,
        Recorded,
        Retained,
        Overwritten,
        Running,
        Other,
    }

    private sealed class TokenHandler
    {
        private readonly NameInterner _names = new();

        private Mode _pendingMode;
        private Mode _mode;
        private bool _evActive;
        private bool _inArgs;
        private EventKey _evKey;
        private ArgKey _argKey;
        private MetaKey _metaKey;

        private NameInfo? _name;
        private char _ph;
        private double _ts;
        private bool _hasTs;
        private double _dur;
        private bool _hasDur;
        private long _id = -1;
        private string? _text;
        private string? _reason;

        public FrameTraceCapture Capture { get; } = new();

        public bool RootClosed { get; private set; }

        public bool SawEventsArray { get; private set; }

        public void Handle(ref Utf8JsonReader reader)
        {
            var token = reader.TokenType;
            var depth = reader.CurrentDepth;

            if (depth == 0)
            {
                if (token == JsonTokenType.EndObject)
                {
                    RootClosed = true;
                }
                else if (token != JsonTokenType.StartObject)
                {
                    throw new FrameTraceFormatException("The JSON root is not an object; expected a Chrome Trace Event JSON object.");
                }

                return;
            }

            if (depth == 1)
            {
                HandleRootMember(ref reader, token);
                return;
            }

            if (_mode == Mode.Events)
            {
                HandleEventToken(ref reader, token, depth);
            }
            else if (_mode == Mode.Meta && depth == 2)
            {
                HandleMetaToken(ref reader, token);
            }
        }

        private void HandleRootMember(ref Utf8JsonReader reader, JsonTokenType token)
        {
            switch (token)
            {
                case JsonTokenType.PropertyName:
                    _mode = Mode.None;
                    if (reader.ValueTextEquals("traceEvents"u8))
                    {
                        _pendingMode = Mode.Events;
                    }
                    else if (reader.ValueTextEquals("sussudio"u8))
                    {
                        _pendingMode = Mode.Meta;
                    }
                    else
                    {
                        _pendingMode = Mode.None;
                    }

                    break;
                case JsonTokenType.StartArray:
                    if (_pendingMode == Mode.Events)
                    {
                        _mode = Mode.Events;
                        SawEventsArray = true;
                    }
                    else
                    {
                        _mode = Mode.None;
                    }

                    break;
                case JsonTokenType.StartObject:
                    if (_pendingMode == Mode.Events)
                    {
                        throw new FrameTraceFormatException("\"traceEvents\" is an object; expected an array of events.");
                    }

                    if (_pendingMode == Mode.Meta)
                    {
                        _mode = Mode.Meta;
                        Capture.SawMetadataBlock = true;
                    }
                    else
                    {
                        _mode = Mode.None;
                    }

                    break;
                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                    _mode = Mode.None;
                    break;
                default:
                    if (_pendingMode == Mode.Events)
                    {
                        throw new FrameTraceFormatException("\"traceEvents\" is not an array of events.");
                    }

                    break;
            }
        }

        private void HandleEventToken(ref Utf8JsonReader reader, JsonTokenType token, int depth)
        {
            if (depth == 2)
            {
                switch (token)
                {
                    case JsonTokenType.StartObject:
                        BeginEvent();
                        break;
                    case JsonTokenType.EndObject:
                        if (_evActive)
                        {
                            _evActive = false;
                            ProcessEvent();
                        }

                        break;
                    case JsonTokenType.StartArray:
                    case JsonTokenType.String:
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    case JsonTokenType.Null:
                        // Something other than an event where an event belongs.
                        Capture.SkippedEvents++;
                        break;
                }

                return;
            }

            if (!_evActive)
            {
                return;
            }

            if (depth == 3)
            {
                switch (token)
                {
                    case JsonTokenType.PropertyName:
                        _evKey = ClassifyEventKey(ref reader);
                        break;
                    case JsonTokenType.StartObject:
                        _inArgs = _evKey == EventKey.Args;
                        break;
                    case JsonTokenType.EndObject:
                        _inArgs = false;
                        break;
                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndArray:
                        break;
                    default:
                        ApplyEventScalar(ref reader, token);
                        break;
                }
            }
            else if (depth == 4 && _inArgs)
            {
                if (token == JsonTokenType.PropertyName)
                {
                    // args.arg (the numeric stage arg) is deliberately not read: the dump also writes the name of every
                    // arg that has a vocabulary (args.reason for a preview drop), and a number alone cannot be explained here.
                    _argKey = reader.ValueTextEquals("id"u8)
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

        private void HandleMetaToken(ref Utf8JsonReader reader, JsonTokenType token)
        {
            if (token == JsonTokenType.PropertyName)
            {
                _metaKey = reader.ValueTextEquals("qpcFrequency"u8) ? MetaKey.Frequency
                    : reader.ValueTextEquals("originQpc"u8) ? MetaKey.Origin
                    : reader.ValueTextEquals("startedUtc"u8) ? MetaKey.StartedUtc
                    : reader.ValueTextEquals("capacity"u8) ? MetaKey.Capacity
                    : reader.ValueTextEquals("recorded"u8) ? MetaKey.Recorded
                    : reader.ValueTextEquals("retained"u8) ? MetaKey.Retained
                    : reader.ValueTextEquals("overwritten"u8) ? MetaKey.Overwritten
                    : reader.ValueTextEquals("running"u8) ? MetaKey.Running
                    : MetaKey.Other;
                return;
            }

            switch (_metaKey)
            {
                case MetaKey.Frequency:
                    if (token == JsonTokenType.Number && reader.TryGetDouble(out var frequency))
                    {
                        Capture.QpcFrequency = frequency;
                    }

                    break;
                case MetaKey.Origin:
                    if (TryReadLong(ref reader, token, out var origin))
                    {
                        Capture.OriginQpc = origin;
                        Capture.SawOrigin = true;
                    }

                    break;
                case MetaKey.StartedUtc:
                    if (token == JsonTokenType.String)
                    {
                        Capture.StartedUtc = reader.GetString();
                    }

                    break;
                case MetaKey.Capacity:
                    if (TryReadLong(ref reader, token, out var capacity))
                    {
                        Capture.Capacity = capacity;
                    }

                    break;
                case MetaKey.Recorded:
                    if (TryReadLong(ref reader, token, out var recorded))
                    {
                        Capture.Recorded = recorded;
                    }

                    break;
                case MetaKey.Retained:
                    if (TryReadLong(ref reader, token, out var retained))
                    {
                        Capture.Retained = retained;
                    }

                    break;
                case MetaKey.Overwritten:
                    if (TryReadLong(ref reader, token, out var overwritten))
                    {
                        Capture.Overwritten = overwritten;
                    }

                    break;
                case MetaKey.Running:
                    if (token is JsonTokenType.True or JsonTokenType.False)
                    {
                        Capture.Running = token == JsonTokenType.True;
                    }

                    break;
            }
        }

        private void BeginEvent()
        {
            _evActive = true;
            _inArgs = false;
            _evKey = EventKey.None;
            _argKey = ArgKey.None;
            _name = null;
            _ph = '\0';
            _hasTs = false;
            _hasDur = false;
            _ts = 0;
            _dur = 0;
            _id = -1;
            _text = null;
            _reason = null;
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
            switch (_evKey)
            {
                case EventKey.Name:
                    if (token == JsonTokenType.String)
                    {
                        _name = reader.ValueIsEscaped
                            ? _names.FromText(reader.GetString() ?? string.Empty)
                            : _names.Get(reader.ValueSpan);
                    }

                    break;
                case EventKey.Ph:
                    if (token == JsonTokenType.String)
                    {
                        var span = reader.ValueSpan;
                        _ph = !reader.ValueIsEscaped && span.Length == 1 ? (char)span[0] : '?';
                    }

                    break;
                case EventKey.Ts:
                    if (token == JsonTokenType.Number && reader.TryGetDouble(out var ts))
                    {
                        _ts = ts;
                        _hasTs = true;
                    }

                    break;
                case EventKey.Dur:
                    if (token == JsonTokenType.Number && reader.TryGetDouble(out var dur))
                    {
                        _dur = dur;
                        _hasDur = true;
                    }

                    break;
            }
        }

        private void ApplyArgScalar(ref Utf8JsonReader reader, JsonTokenType token)
        {
            switch (_argKey)
            {
                case ArgKey.Id:
                    if (TryReadLong(ref reader, token, out var id))
                    {
                        _id = id;
                    }

                    break;
                case ArgKey.Text:
                    if (token == JsonTokenType.String)
                    {
                        _text = reader.GetString();
                    }

                    break;
                case ArgKey.Reason:
                    // A reason that is not a string is ignored: the drop is still counted, just without a reason.
                    if (token == JsonTokenType.String)
                    {
                        _reason = reader.GetString();
                    }

                    break;
            }
        }

        private static bool TryReadLong(ref Utf8JsonReader reader, JsonTokenType token, out long value)
        {
            value = 0;
            if (token != JsonTokenType.Number)
            {
                return false;
            }

            if (reader.TryGetInt64(out value))
            {
                return true;
            }

            if (reader.TryGetDouble(out var number) && number >= -9.2e18 && number <= 9.2e18 && Math.Floor(number) == number)
            {
                value = (long)number;
                return true;
            }

            return false;
        }

        private void ProcessEvent()
        {
            if (_ph == 'M')
            {
                return;
            }

            var isSpan = _ph == 'X';
            var isInstant = _ph is 'i' or 'I';
            if (_name is null || !_hasTs || (!isSpan && !isInstant) || (isSpan && !_hasDur))
            {
                Capture.SkippedEvents++;
                return;
            }

            Capture.ParsedEvents++;
            var info = _name;
            var startUs = _ts;
            var durUs = isSpan ? Math.Max(0d, _dur) : 0d;
            var endUs = startUs + durUs;
            if (startUs < Capture.MinUs)
            {
                Capture.MinUs = startUs;
            }

            if (endUs > Capture.MaxUs)
            {
                Capture.MaxUs = endUs;
            }

            if (info.IsMarker)
            {
                Capture.AddAmbient(info.Name, startUs, endUs, _text);
                return;
            }

            var id = _id;
            if (isInstant)
            {
                if (info.IsSampleTime && id >= 0)
                {
                    var sampled = Capture.GetFrame(id);
                    sampled.SampleUs = startUs;
                    sampled.HasSample = true;
                    sampled.TouchInstant(startUs);
                    return;
                }

                if (info.IsDrop)
                {
                    Capture.AddDrop(info.Name, _reason);
                    if (id >= 0 && !info.IsAmbient)
                    {
                        var dropped = Capture.GetFrame(id);
                        dropped.Drops++;
                        dropped.DropReason ??= string.IsNullOrWhiteSpace(_reason) ? null : _reason;
                        dropped.TouchInstant(startUs);
                    }

                    return;
                }

                Capture.AddAmbient(info.Name, startUs, endUs, _text);
                return;
            }

            if (id < 0 || info.IsAmbient)
            {
                Capture.AddAmbient(info.Name, startUs, endUs, _text);
                return;
            }

            Capture.GetFrame(id).AddSpan(Capture.StageIndexFor(info), startUs, durUs);
        }
    }
}

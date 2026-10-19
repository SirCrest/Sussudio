using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

// Streaming reader for the Chrome Trace Event JSON written by FrameTraceRecorder. It walks tokens from a pooled,
// chunked buffer without building a JSON DOM, so memory use follows the accumulated frame and ambient data rather than
// the document size. A trace can be hundreds of megabytes; the buffer grows only when a single token crosses a chunk.
// Event times are already relative microseconds. Absolute frame IDs are QPC ticks, so the origin and frequency written
// at the end of the dump are used later by the analysis builder to convert them onto the event time axis.
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

    private sealed class TokenHandler
    {
        private enum Mode
        {
            None,
            Events,
            Meta,
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
            Abandoned,
            Running,
            Other,
        }

        private readonly FrameTraceEventParser _events;
        private Mode _pendingMode;
        private Mode _mode;
        private bool _eventActive;
        private MetaKey _metaKey;

        public TokenHandler()
        {
            Capture = new FrameTraceCapture();
            _events = new FrameTraceEventParser(Capture);
        }

        public FrameTraceCapture Capture { get; }
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
                if (depth == 2)
                {
                    switch (token)
                    {
                        case JsonTokenType.StartObject:
                            _eventActive = true;
                            _events.StartEvent();
                            break;
                        case JsonTokenType.EndObject:
                            if (_eventActive)
                            {
                                _eventActive = false;
                                _events.CompleteEvent();
                            }
                            break;
                        case JsonTokenType.StartArray:
                        case JsonTokenType.String:
                        case JsonTokenType.Number:
                        case JsonTokenType.True:
                        case JsonTokenType.False:
                        case JsonTokenType.Null:
                            Capture.SkippedEvents++;
                            break;
                    }
                }
                else if (_eventActive)
                {
                    _events.HandleToken(ref reader, token, depth);
                }
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
                    _pendingMode = reader.ValueTextEquals("traceEvents"u8) ? Mode.Events
                        : reader.ValueTextEquals("sussudio"u8) ? Mode.Meta : Mode.None;
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
                    : reader.ValueTextEquals("abandoned"u8) ? MetaKey.Abandoned
                    : reader.ValueTextEquals("running"u8) ? MetaKey.Running : MetaKey.Other;
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
                case MetaKey.Abandoned:
                    if (TryReadLong(ref reader, token, out var abandoned))
                    {
                        Capture.Abandoned = abandoned;
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

    }

    internal static bool TryReadLong(ref Utf8JsonReader reader, JsonTokenType token, out long value)
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
}

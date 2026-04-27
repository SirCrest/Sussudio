using System;
using System.Diagnostics;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services.Capture;

internal sealed class FrameLedger
{
    public const int DefaultCapacity = 4096;

    private readonly object _sync = new();
    private readonly EventEntry[] _entries;
    private int _nextIndex;
    private int _count;
    private long _totalEventsRecorded;
    private long _eventsDroppedByRetention;

    public FrameLedger(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Frame ledger capacity must be positive.");
        }

        _entries = new EventEntry[capacity];
    }

    public int Capacity => _entries.Length;

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_entries);
            _nextIndex = 0;
            _count = 0;
            _totalEventsRecorded = 0;
            _eventsDroppedByRetention = 0;
        }
    }

    public void RecordCaptureArrived(FrameIdentity identity, string subsystem = "capture")
    {
        Record(
            sourceSequence: identity.SourceSequence,
            stage: FrameLedgerStage.CaptureArrived,
            qpcTimestamp: identity.CaptureArrivalQpc,
            subsystem: subsystem,
            queueDepth: null,
            byteDepth: identity.CompressedByteLength > 0 ? (long?)identity.CompressedByteLength : null,
            accepted: true,
            reason: null,
            identity: identity);
    }

    public void RecordEvent(
        long sourceSequence,
        FrameLedgerStage stage,
        long qpcTimestamp = 0,
        string subsystem = "",
        int? queueDepth = null,
        long? byteDepth = null,
        bool? accepted = null,
        string? reason = null)
    {
        Record(
            sourceSequence,
            stage,
            qpcTimestamp == 0 ? Stopwatch.GetTimestamp() : qpcTimestamp,
            string.IsNullOrWhiteSpace(subsystem) ? stage.ToString() : subsystem,
            queueDepth,
            byteDepth,
            accepted,
            reason,
            identity: null);
    }

    public FrameLedgerSummary GetSummary(int maxEvents = 64)
    {
        if (maxEvents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Frame ledger max event count cannot be negative.");
        }

        lock (_sync)
        {
            if (_count == 0 || maxEvents == 0)
            {
                return new FrameLedgerSummary(
                    Capacity,
                    _totalEventsRecorded,
                    _eventsDroppedByRetention,
                    RecentEventCount: 0,
                    OldestSourceSequence: null,
                    NewestSourceSequence: null,
                    RecentEvents: Array.Empty<FrameLedgerEventSnapshot>());
            }

            var eventCount = Math.Min(_count, maxEvents);
            var firstIndex = GetChronologicalIndex(_count - eventCount);
            var events = new FrameLedgerEventSnapshot[eventCount];
            long? oldest = null;
            long? newest = null;
            for (var i = 0; i < eventCount; i++)
            {
                var entry = _entries[(firstIndex + i) % Capacity];
                events[i] = entry.ToSnapshot();
                oldest ??= entry.SourceSequence;
                newest = entry.SourceSequence;
            }

            return new FrameLedgerSummary(
                Capacity,
                _totalEventsRecorded,
                _eventsDroppedByRetention,
                eventCount,
                oldest,
                newest,
                events);
        }
    }

    private void Record(
        long sourceSequence,
        FrameLedgerStage stage,
        long qpcTimestamp,
        string subsystem,
        int? queueDepth,
        long? byteDepth,
        bool? accepted,
        string? reason,
        FrameIdentity? identity)
    {
        lock (_sync)
        {
            _entries[_nextIndex] = new EventEntry(
                sourceSequence,
                stage,
                qpcTimestamp,
                subsystem,
                queueDepth,
                byteDepth,
                accepted,
                reason,
                identity);

            _nextIndex = (_nextIndex + 1) % Capacity;
            _totalEventsRecorded++;
            if (_count < Capacity)
            {
                _count++;
            }
            else
            {
                _eventsDroppedByRetention++;
            }
        }
    }

    private int GetChronologicalIndex(int offset)
    {
        var start = _count == Capacity ? _nextIndex : 0;
        return (start + offset) % Capacity;
    }

    private readonly struct EventEntry
    {
        public EventEntry(
            long sourceSequence,
            FrameLedgerStage stage,
            long qpcTimestamp,
            string subsystem,
            int? queueDepth,
            long? byteDepth,
            bool? accepted,
            string? reason,
            FrameIdentity? identity)
        {
            SourceSequence = sourceSequence;
            Stage = stage;
            QpcTimestamp = qpcTimestamp;
            Subsystem = subsystem;
            QueueDepth = queueDepth;
            ByteDepth = byteDepth;
            Accepted = accepted;
            Reason = reason;
            Identity = identity;
        }

        public long SourceSequence { get; }
        public FrameLedgerStage Stage { get; }
        public long QpcTimestamp { get; }
        public string Subsystem { get; }
        public int? QueueDepth { get; }
        public long? ByteDepth { get; }
        public bool? Accepted { get; }
        public string? Reason { get; }
        public FrameIdentity? Identity { get; }

        public FrameLedgerEventSnapshot ToSnapshot()
            => new(
                SourceSequence,
                Stage,
                QpcTimestamp,
                Subsystem,
                QueueDepth,
                ByteDepth,
                Accepted,
                Reason,
                Identity);
    }
}

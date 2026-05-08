using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

// Tracks per-frame queue dwell time and backpressure for a video encoder sink.
// LibAvRecordingSink and FlashbackEncoderSink had byte-identical copies of the
// same six methods plus parallel sets of state fields; this is the shared
// implementation. The enqueue-tick queue uses a caller-supplied lock so the
// sink can keep tick tracking and queue-depth changes atomic, matching the
// pre-extraction lock semantics exactly.
internal sealed class VideoQueueLatencyTracker
{
    private readonly string _logTagPrefix;
    private readonly object _enqueueTickLock;
    private readonly Queue<long> _enqueueTicks = new();

    private readonly object _latencySync = new();
    private readonly double[] _latencySamples;
    private int _latencySampleIndex;
    private int _latencySampleCount;

    private readonly object _sequenceSync = new();
    private long _lastSequenceNumber = -1;
    private long _sequenceGaps;

    private long _lastLatencyMs;
    private long _backpressureWaitMs;
    private long _backpressureEvents;
    private long _lastBackpressureWaitMs;
    private long _maxBackpressureWaitMs;

    public VideoQueueLatencyTracker(string logTagPrefix, object enqueueTickLock, int latencyWindowSize)
    {
        _logTagPrefix = logTagPrefix;
        _enqueueTickLock = enqueueTickLock;
        _latencySamples = new double[Math.Max(1, latencyWindowSize)];
    }

    public long LastLatencyMs => Interlocked.Read(ref _lastLatencyMs);
    public long BackpressureWaitMs => Interlocked.Read(ref _backpressureWaitMs);
    public long BackpressureEvents => Interlocked.Read(ref _backpressureEvents);
    public long LastBackpressureWaitMs => Interlocked.Read(ref _lastBackpressureWaitMs);
    public long MaxBackpressureWaitMs => Interlocked.Read(ref _maxBackpressureWaitMs);
    public long LastSequenceNumber => Interlocked.Read(ref _lastSequenceNumber);
    public long SequenceGaps => Interlocked.Read(ref _sequenceGaps);

    // Caller must hold the supplied enqueueTickLock.
    public void TrackEnqueueUnderLock(long enqueueTick)
    {
        _enqueueTicks.Enqueue(enqueueTick);
    }

    // Caller must hold the supplied enqueueTickLock.
    public void ClearEnqueueTicksUnderLock()
    {
        _enqueueTicks.Clear();
    }

    // Caller must hold the supplied enqueueTickLock.
    public void TrackDequeueUnderLock(long expectedEnqueueTick)
    {
        if (_enqueueTicks.Count == 0)
        {
            return;
        }

        var queuedTick = _enqueueTicks.Dequeue();
        if (queuedTick != expectedEnqueueTick)
        {
            Logger.Log($"{_logTagPrefix}_QUEUE_TICK_MISMATCH expected={expectedEnqueueTick} actual={queuedTick}");
        }
    }

    public long GetOldestFrameAgeMs(int currentDepth)
    {
        lock (_enqueueTickLock)
        {
            while (_enqueueTicks.Count > currentDepth)
            {
                _enqueueTicks.Dequeue();
            }

            return _enqueueTicks.Count == 0
                ? 0
                : Math.Max(0, Environment.TickCount64 - _enqueueTicks.Peek());
        }
    }

    public void RecordBackpressure(long startTick, long endTick)
    {
        if (startTick <= 0)
        {
            return;
        }

        var elapsedMs = Math.Max(0, endTick - startTick);
        if (elapsedMs <= 0)
        {
            return;
        }

        Interlocked.Increment(ref _backpressureEvents);
        Interlocked.Add(ref _backpressureWaitMs, elapsedMs);
        Interlocked.Exchange(ref _lastBackpressureWaitMs, elapsedMs);
        AtomicMax.Update(ref _maxBackpressureWaitMs, elapsedMs);
    }

    public void RecordPacketDequeued(long enqueueTick, long? sequenceNumber)
    {
        var latencyMs = Math.Max(0, Environment.TickCount64 - enqueueTick);
        Interlocked.Exchange(ref _lastLatencyMs, latencyMs);
        lock (_latencySync)
        {
            _latencySamples[_latencySampleIndex] = latencyMs;
            _latencySampleIndex = (_latencySampleIndex + 1) % _latencySamples.Length;
            if (_latencySampleCount < _latencySamples.Length)
            {
                _latencySampleCount++;
            }
        }

        if (sequenceNumber.HasValue)
        {
            lock (_sequenceSync)
            {
                var last = Interlocked.Read(ref _lastSequenceNumber);
                var current = sequenceNumber.Value;
                if (last >= 0 && current > last + 1)
                {
                    Interlocked.Add(ref _sequenceGaps, current - last - 1);
                }

                if (current > last)
                {
                    Interlocked.Exchange(ref _lastSequenceNumber, current);
                }
            }
        }
    }

    public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) GetMetrics()
    {
        double[] copy;
        int count;
        lock (_latencySync)
        {
            count = _latencySampleCount;
            if (count <= 0)
            {
                return (0, 0, 0, 0, 0);
            }

            copy = new double[count];
            Array.Copy(_latencySamples, copy, count);
        }

        Array.Sort(copy);
        var total = 0.0;
        for (var i = 0; i < copy.Length; i++)
        {
            total += copy[i];
        }

        var p95Index = Math.Clamp((int)Math.Ceiling(copy.Length * 0.95) - 1, 0, copy.Length - 1);
        var p99Index = Math.Clamp((int)Math.Ceiling(copy.Length * 0.99) - 1, 0, copy.Length - 1);
        return (copy.Length, total / copy.Length, copy[p95Index], copy[p99Index], copy[^1]);
    }

    public void ResetAll()
    {
        Interlocked.Exchange(ref _lastLatencyMs, 0);
        Interlocked.Exchange(ref _backpressureWaitMs, 0);
        Interlocked.Exchange(ref _backpressureEvents, 0);
        Interlocked.Exchange(ref _lastBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _maxBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _sequenceGaps, 0);
        Interlocked.Exchange(ref _lastSequenceNumber, -1);

        lock (_enqueueTickLock)
        {
            _enqueueTicks.Clear();
        }

        lock (_latencySync)
        {
            Array.Clear(_latencySamples, 0, _latencySamples.Length);
            _latencySampleCount = 0;
            _latencySampleIndex = 0;
        }
    }
}

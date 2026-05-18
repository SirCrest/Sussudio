using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private const long DefaultDecodedReorderByteBudget = 1024L * 1024 * 1024;
    private const int MinDecodedReorderCapacity = 32;
    private const int MaxDecodedReorderCapacity = 240;

    private readonly record struct DecodedFrame(
        long SeqNo,
        PooledVideoFrame Frame,
        long DecodedTick);

    // Workers complete out of order, so decoded frames wait here until the
    // emitter can advance _nextEmitSeq. Missing-sequence tracking is explicit
    // so a dropped compressed packet does not permanently stall the pipeline.
    private readonly SortedDictionary<long, DecodedFrame> _reorderFrames = new();
    private readonly SortedSet<long> _knownMissingSequences = new();
    private readonly object _reorderLock = new();
    private readonly int _decodedReorderCapacity;
    private int _reorderBufferDepth;
    private long _nextEmitSeq;
    private long _reorderSkips = 0;
    private long _missingSeqSinceTickMs = -1;

    private void DetectAndResetStall(bool emittedAny)
    {
        var depth = Volatile.Read(ref _reorderBufferDepth);
        if (emittedAny)
        {
            return;
        }

        if (depth <= 0)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, -1);
            return;
        }

        var nowTickMs = Environment.TickCount64;
        var missingSince = Interlocked.Read(ref _missingSeqSinceTickMs);
        if (missingSince < 0)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
            return;
        }

        // Strict shared path: record the stall, but do not synthesize skips for
        // recording/Flashback. Preview-only skipping is handled downstream.
        var thresholdMs = 1000;

        if (nowTickMs - missingSince <= thresholdMs)
        {
            return;
        }

        Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
        Logger.Log(
            $"MJPEG_REORDER_STRICT_WAIT nextEmit={_nextEmitSeq} " +
            $"depth={Volatile.Read(ref _reorderBufferDepth)} waited_ms={nowTickMs - missingSince}");
    }

    private void MarkKnownMissing(long seqNo, string reason)
    {
        lock (_reorderLock)
        {
            if (seqNo < _nextEmitSeq)
            {
                return;
            }

            _knownMissingSequences.Add(seqNo);
            Monitor.PulseAll(_reorderLock);
        }

        Logger.Log($"MJPEG_PIPELINE_KNOWN_MISSING seq={seqNo} reason={reason}");
        SignalEmitter("known_missing");
    }

    private bool ConsumeKnownMissingFrames()
    {
        var consumedAny = false;
        while (true)
        {
            long skippedSeq;
            lock (_reorderLock)
            {
                if (!_knownMissingSequences.Remove(_nextEmitSeq))
                {
                    return consumedAny;
                }

                skippedSeq = _nextEmitSeq++;
                Monitor.PulseAll(_reorderLock);
            }

            consumedAny = true;
            var skips = Interlocked.Increment(ref _reorderSkips);
            if (skips == 1 || skips % 30 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_KNOWN_MISSING_SKIP seq={skippedSeq} " +
                    $"nextEmit={Volatile.Read(ref _nextEmitSeq)} totalSkips={skips}");
            }
        }
    }

    private int GetNextSequenceState()
    {
        lock (_reorderLock)
        {
            if (_reorderFrames.TryGetValue(_nextEmitSeq, out _))
            {
                return 1;
            }

            if (_reorderFrames.Count == 0)
            {
                return 0;
            }

            var seqNo = PeekFirstSequenceUnderLock();
            return seqNo < _nextEmitSeq ? -1 : 2;
        }
    }

    // SortedDictionary keys are sorted, but Keys.First() goes through Enumerable.First
    // which boxes the struct enumerator. The struct GetEnumerator on the dictionary
    // itself avoids that, and we only need the smallest key.
    private long PeekFirstSequenceUnderLock()
    {
        using var e = _reorderFrames.GetEnumerator();
        e.MoveNext();
        return e.Current.Key;
    }

    private bool TryTakeNextDecodedFrame(out DecodedFrame frame)
    {
        lock (_reorderLock)
        {
            if (!_reorderFrames.TryGetValue(_nextEmitSeq, out frame))
            {
                return false;
            }

            _reorderFrames.Remove(_nextEmitSeq);
            Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);
            Monitor.PulseAll(_reorderLock);
            return true;
        }
    }

    private bool TryAddDecodedFrame(long seqNo, PooledVideoFrame frame, long decodedTick)
    {
        lock (_reorderLock)
        {
            while (!_stopped &&
                   Volatile.Read(ref _fatalErrorSignaled) == 0 &&
                   _reorderFrames.Count >= _decodedReorderCapacity &&
                   seqNo != _nextEmitSeq)
            {
                Monitor.Wait(_reorderLock, TimeSpan.FromMilliseconds(8));
            }

            if (_stopped || Volatile.Read(ref _fatalErrorSignaled) != 0)
            {
                return false;
            }

            if (_reorderFrames.ContainsKey(seqNo))
            {
                Interlocked.Increment(ref _reorderCollisions);
                var collisions = Interlocked.Increment(ref _totalFramesDropped);
                if (collisions == 1 || collisions % 120 == 0)
                {
                    Logger.Log(
                        $"MJPEG_REORDER_DUPLICATE seq={seqNo} " +
                        $"depth={Volatile.Read(ref _reorderBufferDepth)} " +
                        $"nextEmit={_nextEmitSeq} totalDropped={collisions}");
                }

                return false;
            }

            _reorderFrames.Add(seqNo, new DecodedFrame(seqNo, frame, decodedTick));
            Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);
            Monitor.PulseAll(_reorderLock);
            return true;
        }
    }

    private void DiscardStaleReorderFrames()
    {
        while (true)
        {
            DecodedFrame frame;
            lock (_reorderLock)
            {
                if (_reorderFrames.Count == 0)
                {
                    return;
                }

                var seqNo = PeekFirstSequenceUnderLock();
                if (seqNo >= _nextEmitSeq)
                {
                    return;
                }

                frame = _reorderFrames[seqNo];
                _reorderFrames.Remove(seqNo);
                Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);
                Monitor.PulseAll(_reorderLock);
            }

            Interlocked.Increment(ref _totalFramesDropped);
            frame.Frame.Dispose();
            Logger.Log(
                $"MJPEG_REORDER_DISCARD reason=stale seq={frame.SeqNo} nextEmit={_nextEmitSeq} " +
                $"depth={Volatile.Read(ref _reorderBufferDepth)}");
        }
    }

    private static int ResolveDecodedReorderCapacity(int width, int height)
    {
        var nv12Bytes = Math.Max(1L, (long)width * height * 3 / 2);
        var budgetedFrames = DefaultDecodedReorderByteBudget / nv12Bytes;
        return (int)Math.Clamp(budgetedFrames, MinDecodedReorderCapacity, MaxDecodedReorderCapacity);
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private void EmitLoop()
    {
        while (!_stopped || HasAliveWorkers() || Volatile.Read(ref _reorderBufferDepth) > 0)
        {
            _emitSignal.WaitOne(8);

            var emittedAny = DrainReadyFrames();
            if (Volatile.Read(ref _fatalErrorSignaled) != 0)
            {
                break;
            }

            DetectAndResetStall(emittedAny);
        }

        if (Volatile.Read(ref _fatalErrorSignaled) == 0)
        {
            DrainReadyFrames();
            DrainRemainingFramesInOrder();
        }
        else
        {
            DiscardRemainingReorderFrames("fatal_stop");
        }
    }

    private bool DrainReadyFrames()
    {
        var emittedAny = false;
        while (true)
        {
            DiscardStaleReorderFrames();
            emittedAny = ConsumeKnownMissingFrames() || emittedAny;
            if (!TryTakeNextDecodedFrame(out var frame))
            {
                break;
            }

            emittedAny = true;

            RecordTimingSample(_reorderLatencyMs, ref _reorderLatencyCount, ref _reorderLatencyIndex, GetElapsedMilliseconds(frame.DecodedTick, Stopwatch.GetTimestamp()));
            RecordTimingSample(_pipelineLatencyMs, ref _pipelineLatencyCount, ref _pipelineLatencyIndex, GetElapsedMilliseconds(frame.Frame.ArrivalTick, Stopwatch.GetTimestamp()));
            var emitted = false;

            try
            {
                NotifyPreviewFrameDecoded(frame.Frame);
                _emitCallback(frame.Frame);
                emitted = true;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _emitFailures);
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                frame.Frame.Dispose();
            }

            _nextEmitSeq++;
            if (emitted)
            {
                Interlocked.Increment(ref _totalFramesEmitted);
            }
        }

        if (emittedAny)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, -1);
        }

        return emittedAny;
    }

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

    private void NotifyPreviewFrameDecoded(PooledVideoFrame frame)
    {
        var callback = _previewCallback;
        if (callback == null ||
            !frame.TryAddLease(out var lease))
        {
            return;
        }

        try
        {
            callback(lease!);
            lease = null;
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"MJPEG_PREVIEW_CALLBACK_FAIL seq={frame.SequenceNumber} " +
                $"type={ex.GetType().Name} msg={ex.Message}");
        }
        finally
        {
            lease?.Dispose();
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

    private void DrainRemainingFramesInOrder()
    {
        List<DecodedFrame> remaining;
        lock (_reorderLock)
        {
            remaining = _reorderFrames.Values.ToList();
            _reorderFrames.Clear();
            _knownMissingSequences.Clear();
            Volatile.Write(ref _reorderBufferDepth, 0);
            Monitor.PulseAll(_reorderLock);
        }

        remaining.Sort((a, b) => a.SeqNo.CompareTo(b.SeqNo));

        foreach (var frame in remaining)
        {
            try
            {
                NotifyPreviewFrameDecoded(frame.Frame);
                _emitCallback(frame.Frame);
                Interlocked.Increment(ref _totalFramesEmitted);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _emitFailures);
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                frame.Frame.Dispose();
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Capture;

internal sealed partial class MjpegPreviewJitterBuffer
{
    private int GetDepth()
    {
        lock (_sync)
        {
            return _frames.Count;
        }
    }

    private BufferedFrame? TryDequeue()
        => TryDequeueCore(out _);

    private BufferedFrame? TryDequeueCore(out DequeueMissReason missReason)
    {
        missReason = DequeueMissReason.None;
        lock (_sync)
        {
            if (_frames.Count == 0)
            {
                missReason = DequeueMissReason.EmptyQueue;
                return null;
            }

            var index = GetNextPreviewFrameIndex(Stopwatch.GetTimestamp(), allowDeadlineSkip: true);
            if (index < 0)
            {
                missReason = DequeueMissReason.WaitingForSequence;
                return null;
            }

            var frame = _frames[index];
            _frames.RemoveAt(index);
            if (frame.SequenceNumber >= 0)
            {
                _nextPreviewSequence = frame.SequenceNumber + 1;
            }

            return frame;
        }
    }

    private bool AddFrameInOrder(BufferedFrame frame)
    {
        if (frame.SequenceNumber < 0)
        {
            _frames.Add(frame);
            return true;
        }

        if (frame.SequenceNumber < _nextPreviewSequence)
        {
            RecordDroppedFrame(frame.SequenceNumber, "late-sequence");
            frame.Dispose();
            Interlocked.Increment(ref _totalDropped);
            Interlocked.Increment(ref _deadlineDropCount);
            return false;
        }

        var index = _frames.FindIndex(candidate =>
            candidate.SequenceNumber >= 0 &&
            candidate.SequenceNumber > frame.SequenceNumber);
        if (index < 0)
        {
            _frames.Add(frame);
        }
        else
        {
            _frames.Insert(index, frame);
        }

        return true;
    }

    private BufferedFrame RemoveOldestFrame()
    {
        var oldestIndex = 0;
        for (var i = 1; i < _frames.Count; i++)
        {
            if (_frames[i].EnqueueTick < _frames[oldestIndex].EnqueueTick)
            {
                oldestIndex = i;
            }
        }

        var frame = _frames[oldestIndex];
        _frames.RemoveAt(oldestIndex);
        if (frame.SequenceNumber >= 0 && frame.SequenceNumber == _nextPreviewSequence)
        {
            _nextPreviewSequence++;
        }

        return frame;
    }

    private int GetNextPreviewFrameIndex(long nowTick, bool allowDeadlineSkip)
    {
        if (_frames.Count == 0)
        {
            return -1;
        }

        if (_nextPreviewSequence < 0)
        {
            var firstOrdered = _frames.FindIndex(frame => frame.SequenceNumber >= 0);
            return firstOrdered >= 0 ? firstOrdered : GetOldestFrameIndex();
        }

        var exact = _frames.FindIndex(frame => frame.SequenceNumber == _nextPreviewSequence);
        if (exact >= 0)
        {
            return exact;
        }

        if (!allowDeadlineSkip)
        {
            return -1;
        }

        var oldestIndex = GetOldestFrameIndex();
        var oldest = _frames[oldestIndex];
        if (!IsPastHardDeadline(oldest, nowTick))
        {
            return -1;
        }

        var nextOrdered = _frames.FindIndex(frame => frame.SequenceNumber >= 0);
        if (nextOrdered >= 0)
        {
            var skipped = Math.Max(1, _frames[nextOrdered].SequenceNumber - _nextPreviewSequence);
            RecordDroppedFrame(_nextPreviewSequence, "missing-sequence");
            _nextPreviewSequence = _frames[nextOrdered].SequenceNumber;
            Interlocked.Add(ref _deadlineDropCount, skipped);
            Interlocked.Add(ref _totalDropped, skipped);
            IncreaseTargetDepth(nowTick);
            return nextOrdered;
        }

        return oldestIndex;
    }

    private int GetOldestFrameIndex()
    {
        var oldestIndex = 0;
        for (var i = 1; i < _frames.Count; i++)
        {
            if (_frames[i].EnqueueTick < _frames[oldestIndex].EnqueueTick)
            {
                oldestIndex = i;
            }
        }

        return oldestIndex;
    }

    private void ClearQueue()
        => ClearQueue("cleared");

    private void ClearQueue(string reason)
    {
        lock (_sync)
        {
            foreach (var frame in _frames)
            {
                RecordDroppedFrame(frame.SequenceNumber, reason);
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
                Interlocked.Increment(ref _clearedDropCount);
            }

            _frames.Clear();
            _nextPreviewSequence = -1;
        }
    }

    private bool TryRecordResumeReprimeMiss(long nowTick)
    {
        while (true)
        {
            var budget = Volatile.Read(ref _resumeReprimeMissBudget);
            if (budget <= 0)
            {
                return false;
            }

            var startTick = Interlocked.Read(ref _resumeReprimeStartTick);
            var targetDepth = Math.Max(1, Volatile.Read(ref _targetDepth));
            var maxReprimeAgeTicks = _frameIntervalTicks * Math.Max(2, targetDepth + 2);
            if (startTick <= 0 ||
                (nowTick >= startTick && nowTick - startTick > maxReprimeAgeTicks))
            {
                Interlocked.Exchange(ref _resumeReprimeMissBudget, 0);
                return false;
            }

            if (Interlocked.CompareExchange(ref _resumeReprimeMissBudget, budget - 1, budget) == budget)
            {
                Interlocked.Increment(ref _resumeReprimeCount);
                Interlocked.Exchange(ref _lastUnderflowQpc, nowTick);
                Volatile.Write(ref _lastUnderflowQueueDepth, 0);
                Volatile.Write(ref _lastUnderflowReason, "resume-reprime");
                Interlocked.Exchange(ref _lastUnderflowInputAgeTicks, 0);
                Interlocked.Exchange(ref _lastUnderflowOutputAgeTicks, 0);
                return true;
            }
        }
    }
}

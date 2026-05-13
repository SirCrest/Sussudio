using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Capture;

internal sealed partial class MjpegPreviewJitterBuffer
{
    private void DropDeadlineExpiredFrames(long nowTick)
    {
        var droppedAny = false;

        lock (_sync)
        {
            while (_frames.Count > 0)
            {
                var oldestIndex = GetOldestFrameIndex();
                var frame = _frames[oldestIndex];
                if (!IsPastHardDeadline(frame, nowTick))
                {
                    break;
                }

                _frames.RemoveAt(oldestIndex);
                if (frame.SequenceNumber >= 0 && frame.SequenceNumber >= _nextPreviewSequence)
                {
                    _nextPreviewSequence = frame.SequenceNumber + 1;
                }

                RecordDroppedFrame(frame.SequenceNumber, "hard-deadline");
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
                Interlocked.Increment(ref _deadlineDropCount);
                droppedAny = true;
            }
        }

        if (droppedAny)
        {
            IncreaseTargetDepth(nowTick);
        }
    }

    private void DropLatencyOverflowFrames(long nowTick)
    {
        lock (_sync)
        {
            var targetDepth = Volatile.Read(ref _targetDepth);
            while (_frames.Count > Math.Max(1, targetDepth))
            {
                var oldestIndex = GetOldestFrameIndex();
                var frame = _frames[oldestIndex];
                if (!IsPastSoftDeadline(frame, nowTick))
                {
                    break;
                }

                _frames.RemoveAt(oldestIndex);
                if (frame.SequenceNumber >= 0 && frame.SequenceNumber >= _nextPreviewSequence)
                {
                    _nextPreviewSequence = frame.SequenceNumber + 1;
                }

                RecordDroppedFrame(frame.SequenceNumber, "soft-deadline");
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
                Interlocked.Increment(ref _deadlineDropCount);
            }
        }
    }

    private bool IsPastSoftDeadline(BufferedFrame frame, long nowTick)
    {
        var targetDepth = Volatile.Read(ref _targetDepth);
        var softDeadlineTicks = Math.Max(
            _frameIntervalTicks,
            _frameIntervalTicks * (targetDepth + SoftDeadlineExtraFrames));
        return nowTick - frame.EnqueueTick > softDeadlineTicks;
    }

    private bool IsPastHardDeadline(BufferedFrame frame, long nowTick)
    {
        var targetDepth = Volatile.Read(ref _targetDepth);
        var hardDeadlineTicks = Math.Max(
            _frameIntervalTicks,
            _frameIntervalTicks * (targetDepth + HardDeadlineExtraFrames));
        return nowTick - frame.EnqueueTick > hardDeadlineTicks;
    }

    private long GetAdjustedOutputIntervalTicks()
    {
        var depth = GetDepth();
        var targetDepth = Volatile.Read(ref _targetDepth);
        var surplus = depth - targetDepth;
        var adjustment = surplus >= AggressiveCatchUpSurplusFrames ? 0.985 :
                         surplus >= FastCatchUpSurplusFrames ? 0.99 :
                         surplus > 0 ? 0.995 :
                         surplus < 0 ? 1.005 :
                         1.0;
        return Math.Max(1, (long)Math.Round(_frameIntervalTicks * adjustment));
    }

    private void IncreaseTargetDepth(long nowTick)
    {
        while (true)
        {
            var current = Volatile.Read(ref _targetDepth);
            if (current >= _maxAdaptiveTargetDepth)
            {
                Interlocked.Exchange(ref _lastAdaptiveIssueTick, nowTick);
                return;
            }

            if (Interlocked.CompareExchange(ref _targetDepth, current + 1, current) == current)
            {
                Interlocked.Increment(ref _targetIncreaseCount);
                Interlocked.Exchange(ref _lastAdaptiveIssueTick, nowTick);
                Logger.Log($"MJPEG_PREVIEW_JITTER_TARGET_INCREASE target={current + 1}");
                return;
            }
        }
    }

    private void MaybeDecreaseTargetDepth(long nowTick)
    {
        if (HasLatencyPressure(nowTick))
        {
            Interlocked.Exchange(ref _lastAdaptiveIssueTick, nowTick);
            return;
        }

        var lastIssue = Interlocked.Read(ref _lastAdaptiveIssueTick);
        var lastDecrease = Interlocked.Read(ref _lastTargetDecreaseTick);
        var stableTicks = nowTick - lastIssue;
        var sinceDecreaseTicks = nowTick - lastDecrease;
        if (stableTicks < Stopwatch.Frequency * 15L ||
            sinceDecreaseTicks < Stopwatch.Frequency * 15L)
        {
            return;
        }

        while (true)
        {
            var current = Volatile.Read(ref _targetDepth);
            if (current <= _minAdaptiveTargetDepth)
            {
                Interlocked.Exchange(ref _lastTargetDecreaseTick, nowTick);
                return;
            }

            if (Interlocked.CompareExchange(ref _targetDepth, current - 1, current) == current)
            {
                Interlocked.Increment(ref _targetDecreaseCount);
                Interlocked.Exchange(ref _lastTargetDecreaseTick, nowTick);
                Logger.Log($"MJPEG_PREVIEW_JITTER_TARGET_DECREASE target={current - 1}");
                return;
            }
        }
    }

    private bool HasLatencyPressure(long nowTick)
    {
        lock (_sync)
        {
            if (_frames.Count == 0)
            {
                return false;
            }

            var targetDepth = Volatile.Read(ref _targetDepth);
            if (_frames.Count > targetDepth + 1)
            {
                return true;
            }

            return IsPastSoftDeadline(_frames[GetOldestFrameIndex()], nowTick);
        }
    }
}

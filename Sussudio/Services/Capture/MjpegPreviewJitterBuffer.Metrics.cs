using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class MjpegPreviewJitterBuffer
{
    public readonly record struct Metrics(
        bool Enabled,
        int TargetDepth,
        int MaxDepth,
        int QueueDepth,
        long TotalQueued,
        long TotalSubmitted,
        long TotalDropped,
        long UnderflowCount,
        long ResumeReprimeCount,
        int InputIntervalSampleCount,
        double InputIntervalAvgMs,
        double InputIntervalP95Ms,
        double InputIntervalMaxMs,
        int OutputIntervalSampleCount,
        double OutputIntervalAvgMs,
        double OutputIntervalP95Ms,
        double OutputIntervalMaxMs,
        int QueueLatencySampleCount,
        double QueueLatencyAvgMs,
        double QueueLatencyP95Ms,
        double QueueLatencyMaxMs,
        long DeadlineDropCount,
        long ClearedDropCount,
        long TargetIncreaseCount,
        long TargetDecreaseCount,
        long LastSelectedPreviewPresentId,
        long LastSelectedSourceSequenceNumber,
        long LastSelectedQpc,
        double LastSelectedSourceLatencyMs,
        long LastDroppedSourceSequenceNumber,
        long LastDropQpc,
        string LastDropReason,
        long LastUnderflowQpc,
        string LastUnderflowReason,
        int LastUnderflowQueueDepth,
        double LastUnderflowInputAgeMs,
        double LastUnderflowOutputAgeMs,
        double LastScheduleLateMs,
        double MaxScheduleLateMs,
        long ScheduleLateCount);

    public Metrics GetMetrics()
    {
        int depth;
        double[] input;
        double[] output;
        double[] latency;
        lock (_sync)
        {
            depth = _frames.Count;
            input = RingBufferHelpers.Copy(_inputIntervalsMs, _inputIntervalCount, _inputIntervalIndex);
            output = RingBufferHelpers.Copy(_outputIntervalsMs, _outputIntervalCount, _outputIntervalIndex);
            latency = RingBufferHelpers.Copy(_queueLatencyMs, _queueLatencyCount, _queueLatencyIndex);
        }

        var inputMetrics = ComputeTimingMetrics(input);
        var outputMetrics = ComputeTimingMetrics(output);
        var latencyMetrics = ComputeTimingMetrics(latency);
        return new Metrics(
            Enabled: true,
            TargetDepth: Volatile.Read(ref _targetDepth),
            MaxDepth: _maxDepth,
            QueueDepth: depth,
            TotalQueued: Interlocked.Read(ref _totalQueued),
            TotalSubmitted: Interlocked.Read(ref _totalSubmitted),
            TotalDropped: Interlocked.Read(ref _totalDropped),
            UnderflowCount: Interlocked.Read(ref _underflowCount),
            ResumeReprimeCount: Interlocked.Read(ref _resumeReprimeCount),
            InputIntervalSampleCount: inputMetrics.SampleCount,
            InputIntervalAvgMs: inputMetrics.AverageMs,
            InputIntervalP95Ms: inputMetrics.P95Ms,
            InputIntervalMaxMs: inputMetrics.MaxMs,
            OutputIntervalSampleCount: outputMetrics.SampleCount,
            OutputIntervalAvgMs: outputMetrics.AverageMs,
            OutputIntervalP95Ms: outputMetrics.P95Ms,
            OutputIntervalMaxMs: outputMetrics.MaxMs,
            QueueLatencySampleCount: latencyMetrics.SampleCount,
            QueueLatencyAvgMs: latencyMetrics.AverageMs,
            QueueLatencyP95Ms: latencyMetrics.P95Ms,
            QueueLatencyMaxMs: latencyMetrics.MaxMs,
            DeadlineDropCount: Interlocked.Read(ref _deadlineDropCount),
            ClearedDropCount: Interlocked.Read(ref _clearedDropCount),
            TargetIncreaseCount: Interlocked.Read(ref _targetIncreaseCount),
            TargetDecreaseCount: Interlocked.Read(ref _targetDecreaseCount),
            LastSelectedPreviewPresentId: Interlocked.Read(ref _lastSelectedPreviewPresentId),
            LastSelectedSourceSequenceNumber: Interlocked.Read(ref _lastSelectedSourceSequenceNumber),
            LastSelectedQpc: Interlocked.Read(ref _lastSelectedQpc),
            LastSelectedSourceLatencyMs: TicksToMs(Interlocked.Read(ref _lastSelectedSourceLatencyTicks)),
            LastDroppedSourceSequenceNumber: Interlocked.Read(ref _lastDroppedSourceSequenceNumber),
            LastDropQpc: Interlocked.Read(ref _lastDropQpc),
            LastDropReason: Volatile.Read(ref _lastDropReason),
            LastUnderflowQpc: Interlocked.Read(ref _lastUnderflowQpc),
            LastUnderflowReason: Volatile.Read(ref _lastUnderflowReason),
            LastUnderflowQueueDepth: Volatile.Read(ref _lastUnderflowQueueDepth),
            LastUnderflowInputAgeMs: TicksToMs(Interlocked.Read(ref _lastUnderflowInputAgeTicks)),
            LastUnderflowOutputAgeMs: TicksToMs(Interlocked.Read(ref _lastUnderflowOutputAgeTicks)),
            LastScheduleLateMs: TicksToMs(Interlocked.Read(ref _lastScheduleLateTicks)),
            MaxScheduleLateMs: TicksToMs(Interlocked.Read(ref _maxScheduleLateTicks)),
            ScheduleLateCount: Interlocked.Read(ref _scheduleLateCount));
    }

    private void RecordInputInterval(long nowTick)
    {
        var previous = Interlocked.Exchange(ref _lastInputTick, nowTick);
        if (previous > 0)
        {
            RecordTimingSample(_inputIntervalsMs, ref _inputIntervalCount, ref _inputIntervalIndex, ElapsedMs(previous, nowTick));
        }
    }

    private void RecordOutputInterval(long nowTick)
    {
        var previous = Interlocked.Exchange(ref _lastOutputTick, nowTick);
        if (previous > 0)
        {
            RecordTimingSample(_outputIntervalsMs, ref _outputIntervalCount, ref _outputIntervalIndex, ElapsedMs(previous, nowTick));
        }
    }

    private void RecordQueueLatency(long startTick, long endTick)
        => RecordTimingSample(_queueLatencyMs, ref _queueLatencyCount, ref _queueLatencyIndex, ElapsedMs(startTick, endTick));

    private void RecordSelectedFrame(BufferedFrame frame, long previewPresentId, long submitTick)
    {
        Interlocked.Exchange(ref _lastSelectedPreviewPresentId, previewPresentId);
        Interlocked.Exchange(ref _lastSelectedSourceSequenceNumber, frame.SequenceNumber);
        Interlocked.Exchange(ref _lastSelectedQpc, submitTick);
        var latencyTicks = frame.ArrivalTick > 0 && submitTick > frame.ArrivalTick
            ? submitTick - frame.ArrivalTick
            : 0;
        Interlocked.Exchange(ref _lastSelectedSourceLatencyTicks, latencyTicks);
    }

    private void RecordDroppedFrame(long sourceSequenceNumber, string reason)
    {
        Interlocked.Exchange(ref _lastDroppedSourceSequenceNumber, sourceSequenceNumber);
        Interlocked.Exchange(ref _lastDropQpc, Stopwatch.GetTimestamp());
        Volatile.Write(ref _lastDropReason, reason);
    }

    private void RecordUnderflow(long nowTick)
    {
        string reason;
        int depth;
        lock (_sync)
        {
            depth = _frames.Count;
            reason = depth == 0
                ? "empty-queue"
                : _nextPreviewSequence >= 0 &&
                  _frames.FindIndex(frame => frame.SequenceNumber == _nextPreviewSequence) < 0
                    ? "waiting-for-sequence"
                    : "selection-blocked";
        }

        Interlocked.Exchange(ref _lastUnderflowQpc, nowTick);
        Volatile.Write(ref _lastUnderflowQueueDepth, depth);
        Volatile.Write(ref _lastUnderflowReason, reason);

        var lastInputTick = Interlocked.Read(ref _lastInputTick);
        var inputAgeTicks = lastInputTick > 0 && nowTick > lastInputTick ? nowTick - lastInputTick : 0;
        Interlocked.Exchange(ref _lastUnderflowInputAgeTicks, inputAgeTicks);

        var lastOutputTick = Interlocked.Read(ref _lastOutputTick);
        var outputAgeTicks = lastOutputTick > 0 && nowTick > lastOutputTick ? nowTick - lastOutputTick : 0;
        Interlocked.Exchange(ref _lastUnderflowOutputAgeTicks, outputAgeTicks);
    }

    private void RecordScheduleLate(long scheduleLateTicks)
    {
        Interlocked.Exchange(ref _lastScheduleLateTicks, scheduleLateTicks);
        if (scheduleLateTicks <= _frameIntervalTicks / 2)
        {
            return;
        }

        Interlocked.Increment(ref _scheduleLateCount);
        while (true)
        {
            var current = Interlocked.Read(ref _maxScheduleLateTicks);
            if (scheduleLateTicks <= current ||
                Interlocked.CompareExchange(ref _maxScheduleLateTicks, scheduleLateTicks, current) == current)
            {
                return;
            }
        }
    }

    private void RecordTimingSample(double[] window, ref int count, ref int index, double valueMs)
    {
        if (valueMs <= 0 || valueMs > 5000)
        {
            return;
        }

        lock (_sync)
        {
            RingBufferHelpers.Add(window, ref count, ref index, valueMs);
        }
    }

    private static (int SampleCount, double AverageMs, double P95Ms, double MaxMs) ComputeTimingMetrics(double[] samples)
    {
        if (samples.Length == 0)
        {
            return (0, 0, 0, 0);
        }

        var sorted = (double[])samples.Clone();
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sorted.Length; i++)
        {
            sum += sorted[i];
            if (sorted[i] > max)
            {
                max = sorted[i];
            }
        }

        Array.Sort(sorted);
        var p95Index = Math.Min((int)(sorted.Length * 0.95), sorted.Length - 1);
        return (sorted.Length, sum / sorted.Length, sorted[p95Index], max);
    }

    private static double ElapsedMs(long startTick, long endTick)
        => (endTick - startTick) * 1000.0 / Stopwatch.Frequency;

    private static double TicksToMs(long ticks)
        => ticks <= 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency;

    private static long MsToTicks(double ms)
        => Math.Max(0, (long)Math.Round(ms * Stopwatch.Frequency / 1000.0));
}

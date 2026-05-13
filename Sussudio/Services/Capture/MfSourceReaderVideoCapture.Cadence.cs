using System;
using System.Threading;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private const int CadenceWindowSeconds = 20;
    private readonly object _cadenceLock = new();
    private double[] _sourceIntervalWindowMs = new double[1200];
    private int _sourceIntervalCount;
    private int _sourceIntervalIndex;
    private long _prevMfTimestamp100ns = -1;
    private double _expectedIntervalMs;

    public readonly record struct SourceCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double P99IntervalMs,
        double MaxIntervalMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentIntervalsMs,
        double JitterStdDevMs,
        long SevereGapCount,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

    public void SetExpectedFrameRate(double fps)
    {
        if (fps > 0)
        {
            _expectedIntervalMs = 1000.0 / fps;
            var targetSize = Math.Max(600, (int)Math.Ceiling(fps * CadenceWindowSeconds));
            lock (_cadenceLock)
            {
                if (_sourceIntervalWindowMs.Length != targetSize)
                {
                    _sourceIntervalWindowMs = new double[targetSize];
                    _sourceIntervalCount = 0;
                    _sourceIntervalIndex = 0;
                }
            }
        }
    }

    private void ResetSourceCadence()
    {
        Interlocked.Exchange(ref _prevMfTimestamp100ns, -1);
        lock (_cadenceLock)
        {
            Array.Clear(_sourceIntervalWindowMs, 0, _sourceIntervalWindowMs.Length);
            _sourceIntervalCount = 0;
            _sourceIntervalIndex = 0;
        }
    }

    public SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        double[] samples;
        double expectedIntervalMs;
        lock (_cadenceLock)
        {
            expectedIntervalMs = _expectedIntervalMs;
            if (_sourceIntervalCount <= 0)
            {
                return new SourceCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedIntervalMs,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    P99IntervalMs: 0,
                    MaxIntervalMs: 0,
                    OnePercentLowFps: 0,
                    FivePercentLowFps: 0,
                    SampleDurationMs: 0,
                    RecentIntervalsMs: Array.Empty<double>(),
                    JitterStdDevMs: 0,
                    SevereGapCount: 0,
                    EstimatedDroppedFrames: 0,
                    EstimatedDropPercent: 0);
            }

            samples = new double[_sourceIntervalCount];
            for (var i = 0; i < _sourceIntervalCount; i++)
            {
                var ringIndex = (_sourceIntervalIndex - _sourceIntervalCount + i + _sourceIntervalWindowMs.Length)
                    % _sourceIntervalWindowMs.Length;
                samples[i] = _sourceIntervalWindowMs[ringIndex];
            }
        }

        var sampleCount = samples.Length;
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var targetIntervalMs = expectedIntervalMs > 0 ? expectedIntervalMs : average;
        var severeGapThresholdMs = targetIntervalMs * 1.6;

        long severeGapCount = 0;
        long estimatedDroppedFrames = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var interval = samples[i];
            var delta = interval - average;
            varianceSum += delta * delta;
            if (interval >= severeGapThresholdMs)
            {
                severeGapCount++;
            }

            if (targetIntervalMs > double.Epsilon)
            {
                estimatedDroppedFrames += Math.Max(0, (int)Math.Round(interval / targetIntervalMs) - 1);
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p99Index = (int)Math.Ceiling((sorted.Length - 1) * 0.99);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var p99IntervalMs = sorted[Math.Clamp(p99Index, 0, sorted.Length - 1)];
        var onePercentLowFps = p99IntervalMs > double.Epsilon ? 1000.0 / p99IntervalMs : 0;
        var fivePercentLowFps = p95IntervalMs > double.Epsilon ? 1000.0 / p95IntervalMs : 0;
        var estimatedDropPercent = estimatedDroppedFrames * 100.0 / Math.Max(1, sampleCount + estimatedDroppedFrames);

        return new SourceCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            P99IntervalMs: p99IntervalMs,
            MaxIntervalMs: max,
            OnePercentLowFps: onePercentLowFps,
            FivePercentLowFps: fivePercentLowFps,
            SampleDurationMs: sum,
            RecentIntervalsMs: samples,
            JitterStdDevMs: jitterStdDevMs,
            SevereGapCount: severeGapCount,
            EstimatedDroppedFrames: estimatedDroppedFrames,
            EstimatedDropPercent: estimatedDropPercent);
    }

    private void TrackSourceCadence(long mfTimestamp100ns)
    {
        var previousTimestamp = Volatile.Read(ref _prevMfTimestamp100ns);
        if (previousTimestamp < 0)
        {
            Volatile.Write(ref _prevMfTimestamp100ns, mfTimestamp100ns);
            return;
        }

        var intervalMs = (mfTimestamp100ns - previousTimestamp) / 10_000.0;
        Volatile.Write(ref _prevMfTimestamp100ns, mfTimestamp100ns);
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        // Keep source cadence state coherent with diagnostics snapshots and frame-rate changes.
        lock (_cadenceLock)
        {
            var window = _sourceIntervalWindowMs;
            if (window.Length == 0)
            {
                return;
            }

            var idx = _sourceIntervalIndex;
            if (idx < 0 || idx >= window.Length)
            {
                idx = 0;
            }

            window[idx] = intervalMs;
            _sourceIntervalIndex = (idx + 1) % window.Length;
            if (_sourceIntervalCount < window.Length)
            {
                _sourceIntervalCount++;
            }
        }
    }
}

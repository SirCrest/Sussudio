using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly object _presentCadenceLock = new();
    private double[] _presentIntervalWindowMs = new double[1200];
    private int _presentIntervalCount;
    private int _presentIntervalIndex;
    private long _lastPresentTick;
    private int _presentCadenceBaselinePending;
    private readonly object _pipelineLatencyLock = new();
    private double[] _pipelineLatencyWindowMs = new double[1200];
    private int _pipelineLatencyCount;
    private int _pipelineLatencyIndex;
    private readonly object _renderCpuTimingLock = new();
    private double[] _inputUploadCpuTimingWindowMs = new double[1200];
    private double[] _renderSubmitCpuTimingWindowMs = new double[1200];
    private double[] _presentCallTimingWindowMs = new double[1200];
    private double[] _renderTotalCpuTimingWindowMs = new double[1200];
    private int _renderCpuTimingCount;
    private int _renderCpuTimingIndex;
    private readonly object _frameLatencyWaitTimingLock = new();
    private double[] _frameLatencyWaitTimingWindowMs = new double[1200];
    private int _frameLatencyWaitTimingCount;
    private int _frameLatencyWaitTimingIndex;
    private long _frameLatencyWaitCallCount;
    private long _frameLatencyWaitSignaledCount;
    private long _frameLatencyWaitTimeoutCount;
    private long _frameLatencyWaitUnexpectedResultCount;
    private long _frameLatencyWaitLastResult;
    private long _frameLatencyWaitLastTicks;
    private long _framesSubmitted;
    private long _framesRendered;
    private long _framesDropped;
    private long _lastSubmittedPreviewPresentId;
    private long _lastSubmittedSourceSequenceNumber = -1;
    private long _lastSubmittedSourcePtsTicks;
    private long _lastSubmittedQpc;
    private long _lastSubmittedUtcUnixMs;
    private long _lastRenderedPreviewPresentId;
    private long _lastRenderedSourceSequenceNumber = -1;
    private long _lastRenderedSourcePtsTicks;
    private long _lastRenderedQpc;
    private long _lastRenderedUtcUnixMs;
    private long _lastRenderedSchedulerToPresentTicks;
    private long _lastRenderedPipelineLatencyTicks;
    private long _lastDroppedPreviewPresentId;
    private long _lastDroppedSourceSequenceNumber = -1;
    private long _lastDroppedSourcePtsTicks;
    private long _lastDroppedQpc;
    private long _lastDroppedUtcUnixMs;
    private long _submissionGeneration;
    private string _lastDropReason = string.Empty;
    private string _submissionGenerationDropReason = "transition";

    public void SetExpectedFrameRate(double fps)
    {
        if (fps <= 0) return;
        _startupFps = fps;
        var targetSize = Math.Max(600, (int)Math.Ceiling(fps * CadenceWindowSeconds));
        lock (_presentCadenceLock)
        {
            if (_presentIntervalWindowMs.Length != targetSize)
            {
                _presentIntervalWindowMs = new double[targetSize];
                _presentIntervalCount = 0;
                _presentIntervalIndex = 0;
            }
        }

        lock (_pipelineLatencyLock)
        {
            if (_pipelineLatencyWindowMs.Length != targetSize)
            {
                _pipelineLatencyWindowMs = new double[targetSize];
                _pipelineLatencyCount = 0;
                _pipelineLatencyIndex = 0;
            }
        }

        lock (_renderCpuTimingLock)
        {
            if (_renderTotalCpuTimingWindowMs.Length != targetSize)
            {
                _inputUploadCpuTimingWindowMs = new double[targetSize];
                _renderSubmitCpuTimingWindowMs = new double[targetSize];
                _presentCallTimingWindowMs = new double[targetSize];
                _renderTotalCpuTimingWindowMs = new double[targetSize];
                _renderCpuTimingCount = 0;
                _renderCpuTimingIndex = 0;
            }
        }

        lock (_frameLatencyWaitTimingLock)
        {
            if (_frameLatencyWaitTimingWindowMs.Length != targetSize)
            {
                _frameLatencyWaitTimingWindowMs = new double[targetSize];
                _frameLatencyWaitTimingCount = 0;
                _frameLatencyWaitTimingIndex = 0;
            }
        }
    }

    public PresentCadenceMetrics GetPresentCadenceMetrics(double expectedIntervalMs)
    {
        double[] samples;
        lock (_presentCadenceLock)
        {
            if (_presentIntervalCount <= 0)
            {
                return new PresentCadenceMetrics(
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
                    SlowFrameCount: 0,
                    SlowFramePercent: 0);
            }

            samples = new double[_presentIntervalCount];
            for (var i = 0; i < _presentIntervalCount; i++)
            {
                var ringIndex = (_presentIntervalIndex - _presentIntervalCount + i + _presentIntervalWindowMs.Length)
                    % _presentIntervalWindowMs.Length;
                samples[i] = _presentIntervalWindowMs[ringIndex];
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
        var slowThresholdMs = targetIntervalMs * 1.6;

        long slowFrameCount = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var delta = samples[i] - average;
            varianceSum += delta * delta;
            if (samples[i] >= slowThresholdMs)
            {
                slowFrameCount++;
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var p99Index = (int)Math.Ceiling((sorted.Length - 1) * 0.99);
        var p99IntervalMs = sorted[Math.Clamp(p99Index, 0, sorted.Length - 1)];
        var onePercentLowFps = p99IntervalMs > double.Epsilon ? 1000.0 / p99IntervalMs : 0;
        var fivePercentLowFps = p95IntervalMs > double.Epsilon ? 1000.0 / p95IntervalMs : 0;
        var slowPercent = slowFrameCount <= 0
            ? 0
            : (double)slowFrameCount / Math.Max(1, sampleCount) * 100.0;

        return new PresentCadenceMetrics(
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
            SlowFrameCount: slowFrameCount,
            SlowFramePercent: slowPercent);
    }

    public double[] GetRecentPresentIntervalsMs(int maxSamples)
    {
        lock (_presentCadenceLock)
        {
            return CopyRecentRing(_presentIntervalWindowMs, _presentIntervalCount, _presentIntervalIndex, maxSamples);
        }
    }

    public double GetEstimatedPipelineLatencyMs()
    {
        lock (_pipelineLatencyLock)
        {
            if (_pipelineLatencyCount <= 0)
            {
                return 0;
            }

            var sum = 0.0;
            for (var i = 0; i < _pipelineLatencyCount; i++)
            {
                var idx = (_pipelineLatencyIndex - _pipelineLatencyCount + i + _pipelineLatencyWindowMs.Length)
                    % _pipelineLatencyWindowMs.Length;
                sum += _pipelineLatencyWindowMs[idx];
            }

            return sum / _pipelineLatencyCount;
        }
    }

    public PipelineLatencyMetrics GetPipelineLatencyMetrics()
    {
        lock (_pipelineLatencyLock)
        {
            if (_pipelineLatencyCount <= 0)
            {
                return default;
            }

            var samples = CopyRecentRing(_pipelineLatencyWindowMs, _pipelineLatencyCount, _pipelineLatencyIndex, _pipelineLatencyCount);
            var timing = SummarizeCpuStageTiming(samples);
            return new PipelineLatencyMetrics(
                timing.SampleCount,
                timing.AverageMs,
                timing.P95Ms,
                timing.P99Ms,
                timing.MaxMs);
        }
    }

    public double[] GetRecentPipelineLatencyMs(int maxSamples)
    {
        lock (_pipelineLatencyLock)
        {
            return CopyRecentRing(_pipelineLatencyWindowMs, _pipelineLatencyCount, _pipelineLatencyIndex, maxSamples);
        }
    }

    public RenderCpuTimingMetrics GetRenderCpuTimingMetrics()
    {
        double[] uploadSamples;
        double[] renderSamples;
        double[] presentSamples;
        double[] totalSamples;
        lock (_renderCpuTimingLock)
        {
            uploadSamples = CopyRecentRing(_inputUploadCpuTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
            renderSamples = CopyRecentRing(_renderSubmitCpuTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
            presentSamples = CopyRecentRing(_presentCallTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
            totalSamples = CopyRecentRing(_renderTotalCpuTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
        }

        return new RenderCpuTimingMetrics(
            SummarizeCpuStageTiming(uploadSamples),
            SummarizeCpuStageTiming(renderSamples),
            SummarizeCpuStageTiming(presentSamples),
            SummarizeCpuStageTiming(totalSamples));
    }

    public FrameLatencyWaitMetrics GetFrameLatencyWaitMetrics()
    {
        CpuStageTimingMetrics timing;
        lock (_frameLatencyWaitTimingLock)
        {
            timing = SummarizeCpuStageTiming(CopyRecentRing(
                _frameLatencyWaitTimingWindowMs,
                _frameLatencyWaitTimingCount,
                _frameLatencyWaitTimingIndex,
                _frameLatencyWaitTimingWindowMs.Length));
        }

        var lastTicks = Interlocked.Read(ref _frameLatencyWaitLastTicks);
        return new FrameLatencyWaitMetrics(
            Enabled: _waitableSwapChainEnabled,
            HandleActive: _frameLatencyWaitHandle != IntPtr.Zero,
            CallCount: Interlocked.Read(ref _frameLatencyWaitCallCount),
            SignaledCount: Interlocked.Read(ref _frameLatencyWaitSignaledCount),
            TimeoutCount: Interlocked.Read(ref _frameLatencyWaitTimeoutCount),
            UnexpectedResultCount: Interlocked.Read(ref _frameLatencyWaitUnexpectedResultCount),
            LastResult: unchecked((uint)Interlocked.Read(ref _frameLatencyWaitLastResult)),
            LastWaitMs: lastTicks > 0 ? TicksToMs(lastTicks) : 0,
            Timing: timing);
    }

    public FrameOwnershipMetrics GetFrameOwnershipMetrics()
    {
        var schedulerToPresentTicks = Interlocked.Read(ref _lastRenderedSchedulerToPresentTicks);
        var pipelineLatencyTicks = Interlocked.Read(ref _lastRenderedPipelineLatencyTicks);
        return new FrameOwnershipMetrics(
            LastSubmittedPreviewPresentId: Interlocked.Read(ref _lastSubmittedPreviewPresentId),
            LastSubmittedSourceSequenceNumber: Interlocked.Read(ref _lastSubmittedSourceSequenceNumber),
            LastSubmittedSourcePtsTicks: Interlocked.Read(ref _lastSubmittedSourcePtsTicks),
            LastSubmittedQpc: Interlocked.Read(ref _lastSubmittedQpc),
            LastSubmittedUtcUnixMs: Interlocked.Read(ref _lastSubmittedUtcUnixMs),
            LastRenderedPreviewPresentId: Interlocked.Read(ref _lastRenderedPreviewPresentId),
            LastRenderedSourceSequenceNumber: Interlocked.Read(ref _lastRenderedSourceSequenceNumber),
            LastRenderedSourcePtsTicks: Interlocked.Read(ref _lastRenderedSourcePtsTicks),
            LastRenderedQpc: Interlocked.Read(ref _lastRenderedQpc),
            LastRenderedUtcUnixMs: Interlocked.Read(ref _lastRenderedUtcUnixMs),
            LastRenderedSchedulerToPresentMs: schedulerToPresentTicks > 0 ? TicksToMs(schedulerToPresentTicks) : 0,
            LastRenderedPipelineLatencyMs: pipelineLatencyTicks > 0 ? TicksToMs(pipelineLatencyTicks) : 0,
            LastDroppedPreviewPresentId: Interlocked.Read(ref _lastDroppedPreviewPresentId),
            LastDroppedSourceSequenceNumber: Interlocked.Read(ref _lastDroppedSourceSequenceNumber),
            LastDroppedSourcePtsTicks: Interlocked.Read(ref _lastDroppedSourcePtsTicks),
            LastDroppedQpc: Interlocked.Read(ref _lastDroppedQpc),
            LastDroppedUtcUnixMs: Interlocked.Read(ref _lastDroppedUtcUnixMs),
            LastDropReason: Volatile.Read(ref _lastDropReason));
    }

    private void TrackFrameSubmitted(PendingFrame frame)
    {
        Interlocked.Exchange(ref _lastSubmittedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastSubmittedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastSubmittedSourcePtsTicks, frame.SourcePtsTicks);
        Interlocked.Exchange(ref _lastSubmittedQpc, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _lastSubmittedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void TrackFramePresented(PendingFrame frame, long presentReturnTick, long estimatedVisibleTick)
    {
        Interlocked.Exchange(ref _lastRenderedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastRenderedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);
        Interlocked.Exchange(ref _lastRenderedQpc, presentReturnTick);
        Interlocked.Exchange(ref _lastRenderedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var schedulerToPresentTicks = frame.SchedulerSubmitTick > 0 && presentReturnTick > frame.SchedulerSubmitTick
            ? presentReturnTick - frame.SchedulerSubmitTick
            : 0;
        var pipelineLatencyTicks = frame.ArrivalTick > 0 && estimatedVisibleTick > frame.ArrivalTick
            ? estimatedVisibleTick - frame.ArrivalTick
            : 0;
        Interlocked.Exchange(ref _lastRenderedSchedulerToPresentTicks, schedulerToPresentTicks);
        Interlocked.Exchange(ref _lastRenderedPipelineLatencyTicks, pipelineLatencyTicks);
    }

    private void TrackFrameDropped(PendingFrame frame, string reason)
    {
        Interlocked.Increment(ref _framesDropped);
        Interlocked.Exchange(ref _lastDroppedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastDroppedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastDroppedSourcePtsTicks, frame.SourcePtsTicks);
        Interlocked.Exchange(ref _lastDroppedQpc, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _lastDroppedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastDropReason, reason);
    }

    private double TrackPresentCadence(bool countSample)
    {
        var nowTick = Stopwatch.GetTimestamp();
        var previousTick = Interlocked.Exchange(ref _lastPresentTick, nowTick);
        if (!countSample)
        {
            Interlocked.Exchange(ref _presentCadenceBaselinePending, 1);
            return 0;
        }

        if (previousTick <= 0)
        {
            return 0;
        }

        if (Interlocked.Exchange(ref _presentCadenceBaselinePending, 0) != 0)
        {
            return 0;
        }

        var intervalMs = (nowTick - previousTick) * 1000.0 / Stopwatch.Frequency;
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return 0;
        }

        lock (_presentCadenceLock)
        {
            _presentIntervalWindowMs[_presentIntervalIndex] = intervalMs;
            _presentIntervalIndex = (_presentIntervalIndex + 1) % _presentIntervalWindowMs.Length;
            if (_presentIntervalCount < _presentIntervalWindowMs.Length)
            {
                _presentIntervalCount++;
            }
        }

        return intervalMs;
    }

    private void TrackPipelineLatency(long arrivalTick, long estimatedVisibleTick)
    {
        if (arrivalTick <= 0 || estimatedVisibleTick <= arrivalTick)
        {
            return;
        }

        var latencyMs = (estimatedVisibleTick - arrivalTick) * 1000.0 / Stopwatch.Frequency;
        if (latencyMs < 0 || latencyMs > 10000)
        {
            return;
        }

        lock (_pipelineLatencyLock)
        {
            _pipelineLatencyWindowMs[_pipelineLatencyIndex] = latencyMs;
            _pipelineLatencyIndex = (_pipelineLatencyIndex + 1) % _pipelineLatencyWindowMs.Length;
            if (_pipelineLatencyCount < _pipelineLatencyWindowMs.Length)
            {
                _pipelineLatencyCount++;
            }
        }
    }

    private void TrackRenderCpuTiming(long inputUploadTicks, long renderSubmitTicks, long presentCallTicks, long totalTicks)
    {
        if (totalTicks <= 0)
        {
            return;
        }

        var inputUploadMs = TicksToMs(inputUploadTicks);
        var renderSubmitMs = TicksToMs(renderSubmitTicks);
        var presentCallMs = TicksToMs(presentCallTicks);
        var totalMs = TicksToMs(totalTicks);
        if (!IsValidRenderCpuStageMs(totalMs))
        {
            return;
        }

        lock (_renderCpuTimingLock)
        {
            _inputUploadCpuTimingWindowMs[_renderCpuTimingIndex] = IsValidRenderCpuStageMs(inputUploadMs) ? inputUploadMs : 0;
            _renderSubmitCpuTimingWindowMs[_renderCpuTimingIndex] = IsValidRenderCpuStageMs(renderSubmitMs) ? renderSubmitMs : 0;
            _presentCallTimingWindowMs[_renderCpuTimingIndex] = IsValidRenderCpuStageMs(presentCallMs) ? presentCallMs : 0;
            _renderTotalCpuTimingWindowMs[_renderCpuTimingIndex] = totalMs;
            _renderCpuTimingIndex = (_renderCpuTimingIndex + 1) % _renderTotalCpuTimingWindowMs.Length;
            if (_renderCpuTimingCount < _renderTotalCpuTimingWindowMs.Length)
            {
                _renderCpuTimingCount++;
            }
        }
    }

    private void TrackFrameLatencyWait(uint result, long waitTicks)
    {
        Interlocked.Increment(ref _frameLatencyWaitCallCount);
        Interlocked.Exchange(ref _frameLatencyWaitLastResult, result);
        Interlocked.Exchange(ref _frameLatencyWaitLastTicks, waitTicks);
        if (result == WaitObject0)
        {
            Interlocked.Increment(ref _frameLatencyWaitSignaledCount);
        }
        else if (result == WaitTimeout)
        {
            Interlocked.Increment(ref _frameLatencyWaitTimeoutCount);
        }
        else
        {
            Interlocked.Increment(ref _frameLatencyWaitUnexpectedResultCount);
        }

        var waitMs = TicksToMs(waitTicks);
        if (!IsValidRenderCpuStageMs(waitMs))
        {
            return;
        }

        lock (_frameLatencyWaitTimingLock)
        {
            _frameLatencyWaitTimingWindowMs[_frameLatencyWaitTimingIndex] = waitMs;
            _frameLatencyWaitTimingIndex = (_frameLatencyWaitTimingIndex + 1) % _frameLatencyWaitTimingWindowMs.Length;
            if (_frameLatencyWaitTimingCount < _frameLatencyWaitTimingWindowMs.Length)
            {
                _frameLatencyWaitTimingCount++;
            }
        }
    }

    private void ResetPresentCadence()
    {
        Interlocked.Exchange(ref _lastPresentTick, 0);
        Interlocked.Exchange(ref _presentCadenceBaselinePending, 0);
        lock (_presentCadenceLock)
        {
            Array.Clear(_presentIntervalWindowMs, 0, _presentIntervalWindowMs.Length);
            _presentIntervalCount = 0;
            _presentIntervalIndex = 0;
        }

        lock (_pipelineLatencyLock)
        {
            Array.Clear(_pipelineLatencyWindowMs, 0, _pipelineLatencyWindowMs.Length);
            _pipelineLatencyCount = 0;
            _pipelineLatencyIndex = 0;
        }

        lock (_renderCpuTimingLock)
        {
            Array.Clear(_inputUploadCpuTimingWindowMs, 0, _inputUploadCpuTimingWindowMs.Length);
            Array.Clear(_renderSubmitCpuTimingWindowMs, 0, _renderSubmitCpuTimingWindowMs.Length);
            Array.Clear(_presentCallTimingWindowMs, 0, _presentCallTimingWindowMs.Length);
            Array.Clear(_renderTotalCpuTimingWindowMs, 0, _renderTotalCpuTimingWindowMs.Length);
            _renderCpuTimingCount = 0;
            _renderCpuTimingIndex = 0;
        }

        lock (_frameLatencyWaitTimingLock)
        {
            Array.Clear(_frameLatencyWaitTimingWindowMs, 0, _frameLatencyWaitTimingWindowMs.Length);
            _frameLatencyWaitTimingCount = 0;
            _frameLatencyWaitTimingIndex = 0;
        }

        Interlocked.Exchange(ref _frameLatencyWaitCallCount, 0);
        Interlocked.Exchange(ref _frameLatencyWaitSignaledCount, 0);
        Interlocked.Exchange(ref _frameLatencyWaitTimeoutCount, 0);
        Interlocked.Exchange(ref _frameLatencyWaitUnexpectedResultCount, 0);
        Interlocked.Exchange(ref _frameLatencyWaitLastResult, 0);
        Interlocked.Exchange(ref _frameLatencyWaitLastTicks, 0);

        lock (_slowFrameDiagnosticsLock)
        {
            Array.Clear(_slowFrameDiagnostics, 0, _slowFrameDiagnostics.Length);
            _slowFrameDiagnosticsCount = 0;
            _slowFrameDiagnosticsIndex = 0;
        }
    }
}

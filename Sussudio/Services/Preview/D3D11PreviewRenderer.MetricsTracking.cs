using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private long _lastPresentTick;
    private int _presentCadenceBaselinePending;

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

    private static double TicksToMs(long ticks)
        => ticks <= 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency;

    private static bool IsValidRenderCpuStageMs(double value)
        => value >= 0 && value <= 5000 && !double.IsNaN(value) && !double.IsInfinity(value);

}

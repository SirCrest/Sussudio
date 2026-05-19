using System;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
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

    private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)
    {
        var take = Math.Min(Math.Max(0, maxSamples), count);
        if (take <= 0)
        {
            return Array.Empty<double>();
        }

        var result = new double[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }

    private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)
    {
        if (samples.Length == 0)
        {
            return new CpuStageTimingMetrics(0, 0, 0, 0, 0);
        }

        Array.Sort(samples);
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var p95Index = (int)Math.Ceiling((samples.Length - 1) * 0.95);
        var p99Index = (int)Math.Ceiling((samples.Length - 1) * 0.99);
        return new CpuStageTimingMetrics(
            samples.Length,
            sum / samples.Length,
            samples[Math.Clamp(p95Index, 0, samples.Length - 1)],
            samples[Math.Clamp(p99Index, 0, samples.Length - 1)],
            max);
    }
}

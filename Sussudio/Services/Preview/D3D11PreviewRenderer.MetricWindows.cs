using System;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
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

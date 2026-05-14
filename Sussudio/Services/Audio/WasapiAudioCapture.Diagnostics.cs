using System;
using System.Threading;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioCapture
{
    public long AudioFramesArrived => Interlocked.Read(ref _audioFramesArrived);

    public long AudioFramesWrittenToSink => Interlocked.Read(ref _audioFramesWrittenToSink);

    public long CaptureCallbackCount => Interlocked.Read(ref _captureCallbackCount);

    // Snapshot getter — both fields read together drove the lock-and-walk twice per
    // diagnostics poll. Callers should prefer GetCaptureCallbackIntervalSnapshot()
    // when they need both values.
    public double CaptureCallbackAvgIntervalMs => GetCaptureCallbackIntervalMetrics().AverageIntervalMs;

    public double CaptureCallbackMaxIntervalMs => GetCaptureCallbackIntervalMetrics().MaxIntervalMs;

    public (double AvgIntervalMs, double MaxIntervalMs) GetCaptureCallbackIntervalSnapshot()
    {
        var metrics = GetCaptureCallbackIntervalMetrics();
        return (metrics.AverageIntervalMs, metrics.MaxIntervalMs);
    }

    public long CaptureCallbackSevereGapCount => Interlocked.Read(ref _captureCallbackSevereGapCount);

    public long AudioDataDiscontinuityCount => Interlocked.Read(ref _audioDataDiscontinuityCount);

    public long AudioTimestampErrorCount => Interlocked.Read(ref _audioTimestampErrorCount);

    public long AudioGlitchCount =>
        Interlocked.Read(ref _audioDataDiscontinuityCount) +
        Interlocked.Read(ref _audioTimestampErrorCount) +
        Interlocked.Read(ref _captureCallbackSevereGapCount);

    public int CaptureCallbackSilenceCount => Volatile.Read(ref _captureCallbackSilenceCount);

    public long LastCaptureCallbackTickMs => Interlocked.Read(ref _lastCaptureCallbackTickMs);

    public long AudioLevelEventsFired => Interlocked.Read(ref _audioLevelEventsFired);

    public long AudioLevelEventsLastFireTickMs => Interlocked.Read(ref _audioLevelEventsLastFireTickMs);

    private void TrackCaptureCallback(long callbackTickMs)
    {
        Interlocked.Increment(ref _captureCallbackCount);
        var previousTickMs = Interlocked.Exchange(ref _lastCaptureCallbackTickMs, callbackTickMs);
        if (previousTickMs <= 0 || callbackTickMs <= previousTickMs)
        {
            return;
        }

        var intervalMs = callbackTickMs - previousTickMs;
        var expectedIntervalMs = _captureFormat.SampleRate > 0
            ? Math.Max(1.0, (double)OutputSampleRate / _captureFormat.SampleRate)
            : 1.0;
        if (intervalMs > Math.Max(WaitTimeoutMs, expectedIntervalMs * SevereCallbackGapMultiplier))
        {
            Interlocked.Increment(ref _captureCallbackSevereGapCount);
        }

        lock (_captureCallbackIntervalLock)
        {
            _captureCallbackIntervalWindowMs[_captureCallbackIntervalIndex] = intervalMs;
            _captureCallbackIntervalIndex = (_captureCallbackIntervalIndex + 1) % _captureCallbackIntervalWindowMs.Length;
            if (_captureCallbackIntervalCount < _captureCallbackIntervalWindowMs.Length)
            {
                _captureCallbackIntervalCount++;
            }
        }
    }

    private CallbackIntervalMetrics GetCaptureCallbackIntervalMetrics()
    {
        double[] intervals;
        lock (_captureCallbackIntervalLock)
        {
            if (_captureCallbackIntervalCount <= 0)
            {
                return new CallbackIntervalMetrics(0, 0, 0);
            }

            intervals = new double[_captureCallbackIntervalCount];
            for (var i = 0; i < _captureCallbackIntervalCount; i++)
            {
                var ringIndex = (_captureCallbackIntervalIndex - _captureCallbackIntervalCount + i + _captureCallbackIntervalWindowMs.Length)
                    % _captureCallbackIntervalWindowMs.Length;
                intervals[i] = _captureCallbackIntervalWindowMs[ringIndex];
            }
        }

        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < intervals.Length; i++)
        {
            sum += intervals[i];
            if (intervals[i] > max)
            {
                max = intervals[i];
            }
        }

        return new CallbackIntervalMetrics(intervals.Length, sum / intervals.Length, max);
    }

    private void TrackCapturePacketFlags(uint flags)
    {
        if ((flags & WasapiComInterop.AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY) != 0)
        {
            Interlocked.Increment(ref _audioDataDiscontinuityCount);
        }

        if ((flags & WasapiComInterop.AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR) != 0)
        {
            Interlocked.Increment(ref _audioTimestampErrorCount);
        }
    }

    private readonly record struct CallbackIntervalMetrics(int SampleCount, double AverageIntervalMs, double MaxIntervalMs);
}

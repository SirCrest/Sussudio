using System;
using System.Diagnostics;

namespace Sussudio.Services.Capture;

// Samples luma from rendered frames to estimate what the viewer actually sees.
// This detects repeated visual output even when source frames keep arriving,
// which is why it complements source-reader and renderer-submit counters.
internal sealed partial class VisualCadenceTracker
{
    private const int DefaultSampleColumns = 640;
    private const int DefaultSampleRows = 360;
    private const int WindowSize = 720;

    private readonly object _sync = new();
    private readonly int _sampleColumns;
    private readonly int _sampleRows;
    private readonly int _sampleSize;
    private readonly double _cropLeft;
    private readonly double _cropTop;
    private readonly double _cropWidth;
    private readonly double _cropHeight;
    private byte[] _lastSample;
    private byte[] _currentSample;
    private readonly double[] _outputIntervalsMs = new double[WindowSize];
    private readonly double[] _changeIntervalsMs = new double[WindowSize];
    private readonly double[] _deltaWindow = new double[WindowSize];

    private int _outputIntervalCount;
    private int _outputIntervalIndex;
    private int _changeIntervalCount;
    private int _changeIntervalIndex;
    private int _deltaCount;
    private int _deltaIndex;
    private bool _hasLastSample;
    private long _lastOutputTick;
    private long _lastChangeTick;
    private long _sampleCount;
    private long _changedFrameCount;
    private long _repeatFrameCount;
    private long _currentRepeatRun;
    private long _longestRepeatRun;
    private double _lastDelta;
    private int _lastSampleLength;
    private int _lastBytesPerLuma;

    public VisualCadenceTracker(
        int sampleColumns = DefaultSampleColumns,
        int sampleRows = DefaultSampleRows,
        double cropLeft = 0,
        double cropTop = 0,
        double cropWidth = 1,
        double cropHeight = 1)
    {
        _sampleColumns = Math.Max(1, sampleColumns);
        _sampleRows = Math.Max(1, sampleRows);
        _sampleSize = _sampleColumns * _sampleRows;
        _cropLeft = Math.Clamp(cropLeft, 0, 0.999);
        _cropTop = Math.Clamp(cropTop, 0, 0.999);
        _cropWidth = Math.Clamp(cropWidth, 0.001, 1.0 - _cropLeft);
        _cropHeight = Math.Clamp(cropHeight, 0.001, 1.0 - _cropTop);
        _lastSample = new byte[_sampleSize * 2];
        _currentSample = new byte[_sampleSize * 2];
    }

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_lastSample, 0, _lastSample.Length);
            Array.Clear(_currentSample, 0, _currentSample.Length);
            Array.Clear(_outputIntervalsMs, 0, _outputIntervalsMs.Length);
            Array.Clear(_changeIntervalsMs, 0, _changeIntervalsMs.Length);
            Array.Clear(_deltaWindow, 0, _deltaWindow.Length);
            _outputIntervalCount = 0;
            _outputIntervalIndex = 0;
            _changeIntervalCount = 0;
            _changeIntervalIndex = 0;
            _deltaCount = 0;
            _deltaIndex = 0;
            _hasLastSample = false;
            _lastOutputTick = 0;
            _lastChangeTick = 0;
            _sampleCount = 0;
            _changedFrameCount = 0;
            _repeatFrameCount = 0;
            _currentRepeatRun = 0;
            _longestRepeatRun = 0;
            _lastDelta = 0;
            _lastSampleLength = 0;
            _lastBytesPerLuma = 0;
        }
    }

    public void RecordFrame(
        ReadOnlySpan<byte> frame,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        long timestampTick = 0)
    {
        if (frame.IsEmpty || width <= 0 || height <= 0)
        {
            return;
        }

        var bytesPerLuma = pixelFormat == PooledVideoPixelFormat.P010 ? 2 : 1;
        var requiredLumaBytes = width * height * bytesPerLuma;
        if (requiredLumaBytes <= 0 || frame.Length < requiredLumaBytes)
        {
            if (frame.Length >= width * height)
            {
                bytesPerLuma = 1;
                requiredLumaBytes = width * height;
            }
            else
            {
                return;
            }
        }

        var nowTick = timestampTick > 0 ? timestampTick : Stopwatch.GetTimestamp();
        lock (_sync)
        {
            var sample = SampleLumaAndCompare(frame, width, height, bytesPerLuma, _currentSample, _hasLastSample ? _lastSample : null);
            var sampleLength = sample.Length;
            if (sampleLength <= 0)
            {
                return;
            }

            if (_lastOutputTick > 0)
            {
                AddTimingSample(_outputIntervalsMs, ref _outputIntervalCount, ref _outputIntervalIndex, ElapsedMs(_lastOutputTick, nowTick));
            }

            _lastOutputTick = nowTick;
            _sampleCount++;

            if (!_hasLastSample)
            {
                PromoteCurrentSample(sampleLength, bytesPerLuma);
                _hasLastSample = true;
                _changedFrameCount++;
                _lastChangeTick = nowTick;
                return;
            }

            var sampleShapeChanged = sampleLength != _lastSampleLength || bytesPerLuma != _lastBytesPerLuma;
            var delta = sampleShapeChanged
                ? Math.Max(
                    sampleLength / Math.Max(1, bytesPerLuma),
                    _lastSampleLength / Math.Max(1, _lastBytesPerLuma))
                : sample.ChangedPixels;
            _lastDelta = delta;
            AddValueSample(_deltaWindow, ref _deltaCount, ref _deltaIndex, delta);

            if (delta > 0)
            {
                if (_lastChangeTick > 0)
                {
                    AddTimingSample(_changeIntervalsMs, ref _changeIntervalCount, ref _changeIntervalIndex, ElapsedMs(_lastChangeTick, nowTick));
                }

                _lastChangeTick = nowTick;
                _changedFrameCount++;
                _currentRepeatRun = 0;
                PromoteCurrentSample(sampleLength, bytesPerLuma);
                return;
            }

            _repeatFrameCount++;
            _currentRepeatRun++;
            if (_currentRepeatRun > _longestRepeatRun)
            {
                _longestRepeatRun = _currentRepeatRun;
            }
        }
    }

}

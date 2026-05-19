using System;
using System.Collections.Generic;
using System.Linq;

namespace Sussudio.ViewModels;

/// <summary>
/// Owns bounded byte-sample smoothing for recording and Flashback bitrate labels.
/// </summary>
internal sealed class BitrateSampleWindow
{
    private readonly long _windowMs;
    private readonly Queue<(long Tick, long Bytes)> _samples = new();

    public BitrateSampleWindow(long windowMs)
    {
        if (windowMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMs), "Bitrate sample window must be positive.");
        }

        _windowMs = windowMs;
    }

    public void Clear()
    {
        _samples.Clear();
    }

    public double? AddSampleAndCompute(long tick, long bytes)
    {
        _samples.Enqueue((tick, bytes));
        while (_samples.Count > 0 && tick - _samples.Peek().Tick > _windowMs)
        {
            _samples.Dequeue();
        }

        return ComputeAverageBitrate(_samples);
    }

    private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var first = samples.Peek();
        var last = samples.Last();
        var deltaMs = last.Tick - first.Tick;
        if (deltaMs <= 0)
        {
            return null;
        }

        var deltaBytes = Math.Max(0, last.Bytes - first.Bytes);
        return (deltaBytes * 8.0) / (deltaMs / 1000.0);
    }
}

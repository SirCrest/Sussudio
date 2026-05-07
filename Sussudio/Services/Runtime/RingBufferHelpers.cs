using System;

namespace Sussudio.Services.Runtime;

// Append-and-snapshot helpers for the simple ring buffers used by the cadence
// trackers and jitter trackers. Callers own their own validation and locking;
// these methods only handle the ring index/count bookkeeping and the bounded
// copy-out, both of which were duplicated across four classes before.
internal static class RingBufferHelpers
{
    public static void Add(double[] window, ref int count, ref int index, double value)
    {
        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    public static void Add(int[] window, ref int count, ref int index, int value)
    {
        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    // maxCount: optional cap on samples returned; defaults to "all available".
    public static double[] Copy(double[] window, int count, int index, int? maxCount = null)
    {
        var take = maxCount.HasValue
            ? Math.Min(Math.Max(0, maxCount.Value), count)
            : count;
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

    public static int[] Copy(int[] window, int count, int index, int? maxCount = null)
    {
        var take = maxCount.HasValue
            ? Math.Min(Math.Max(0, maxCount.Value), count)
            : count;
        if (take <= 0)
        {
            return Array.Empty<int>();
        }

        var result = new int[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }
}

using System;
using System.IO;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }

    private static long SubtractNonNegative(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left <= right ? 0 : left - right;
    }

    private long GetCompletedSegmentBytesSaturated()
    {
        // Must be called under _indexLock.
        long total = 0;
        foreach (var segment in _completedSegments)
        {
            total = AddNonNegativeSaturated(total, segment.SizeBytes);
        }

        return total;
    }

    private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)
    {
        if (latestTicks <= startTicks)
        {
            return 0;
        }

        if (startTicks < 0 && latestTicks > long.MaxValue + startTicks)
        {
            return long.MaxValue;
        }

        return latestTicks - startTicks;
    }

    private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)
        => endPts < startPts ? startPts : endPts;

    private static bool IsSameSegmentPath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static long ToNonNegativeLongSaturated(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 0;
        }

        return value >= long.MaxValue ? long.MaxValue : (long)value;
    }
}

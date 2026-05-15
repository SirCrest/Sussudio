using System;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }

    private static long ToAvTimeBaseTimestampOrMax(TimeSpan value)
        => value == TimeSpan.MaxValue ? long.MaxValue : ToAvTimeBaseTimestamp(value);

    private static long ToAvTimeBaseTimestamp(TimeSpan value)
        => ToMicrosecondsSaturated(value);

    private static long ToMicrosecondsSaturated(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return 0;
        }

        var microseconds = value.TotalMilliseconds * 1000.0;
        if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)microseconds;
    }

    private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)
    {
        if (left <= right)
        {
            return TimeSpan.Zero;
        }

        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(leftTicks - rightTicks);
    }
}

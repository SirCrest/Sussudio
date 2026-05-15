using System;

namespace Sussudio.Controllers;

internal static class FlashbackTimelineGeometry
{
    public static bool TryComputeFraction(double x, double width, out double fraction)
    {
        fraction = 0;
        if (!IsUsableTrackDimension(width) || !double.IsFinite(x))
        {
            return false;
        }

        fraction = Math.Clamp(x / width, 0, 1);
        return true;
    }

    public static bool TryComputePosition(double x, double width, TimeSpan bufferDuration, out TimeSpan position)
    {
        position = TimeSpan.Zero;
        if (!TryComputeFraction(x, width, out var fraction) || !IsUsableDuration(bufferDuration))
        {
            return false;
        }

        position = ComputePosition(fraction, bufferDuration);
        return true;
    }

    public static TimeSpan ComputePosition(double fraction, TimeSpan bufferDuration)
        => IsUsableDuration(bufferDuration)
            ? TimeSpan.FromSeconds(Math.Clamp(fraction, 0, 1) * bufferDuration.TotalSeconds)
            : TimeSpan.Zero;

    public static bool IsUsableTrackDimension(double value)
        => double.IsFinite(value) && value > 0;

    public static bool IsUsableDuration(TimeSpan value)
        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;
}

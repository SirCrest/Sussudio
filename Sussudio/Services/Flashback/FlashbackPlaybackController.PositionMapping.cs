using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    /// <summary>
    /// Returns true if the given path is the active fMP4 segment. The reopen-and-retry
    /// workaround only applies to fMP4 (fragment index goes stale); transport streams
    /// handle appended data via eof_reached reset and don't need reopening.
    /// </summary>
    private bool IsActiveFmp4Segment(string? path)
        => path != null
        && IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)
        && path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);

    private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);

    /// <summary>
    /// Clamp a scrub/seek position to the currently usable buffer range, optionally
    /// account for segment eviction that has happened since a scrub session captured
    /// its frozen reference. Without the eviction adjustment, a long-held scrub at
    /// position 0 maps via SaturatingAdd(pos, frozenValidStart) to a file PTS that
    /// has been evicted - EnsureFileOpen fails and the user gets a sudden snap-to-
    /// live instead of clamping to the new oldest available position.
    /// </summary>
    private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)
    {
        var bufferDuration = _bufferManager.BufferedDuration;
        var inTicks = Interlocked.Read(ref _inPointTicks);
        var min = inTicks == long.MinValue ? TimeSpan.Zero : TimeSpan.FromTicks(inTicks);
        var outTicks = Interlocked.Read(ref _outPointTicks);
        var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);
        if (max > bufferDuration) max = bufferDuration;
        if (frozenValidStart.HasValue)
        {
            // Eviction may have advanced ValidStartPts past the scrub session's
            // captured reference. Positions in the evicted gap (in scrub coords)
            // would resolve to file PTS values whose segments no longer exist.
            // Promote min so those positions clamp up to the new oldest valid
            // position rather than failing the file lookup downstream.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (currentValidStart > frozenValidStart.Value)
            {
                var evictedDelta = currentValidStart - frozenValidStart.Value;
                if (evictedDelta > min)
                {
                    min = evictedDelta;
                }
            }
        }
        if (min > max) min = max;
        if (position < min) return min;
        if (position > max) return max;
        return position;
    }

    private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)
    {
        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)
            return TimeSpan.MaxValue;
        if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)
            return TimeSpan.MinValue;
        return TimeSpan.FromTicks(leftTicks + rightTicks);
    }

    private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)
    {
        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)
            return TimeSpan.MaxValue;
        if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)
            return TimeSpan.MinValue;
        return TimeSpan.FromTicks(leftTicks - rightTicks);
    }

    private static bool IsSamePlaybackPath(string? left, string? right)
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
            Logger.Log($"FLASHBACK_PLAYBACK_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}

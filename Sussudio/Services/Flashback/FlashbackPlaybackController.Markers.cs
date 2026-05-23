using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- In/Out points ---

    private long _inPointTicks = long.MinValue;
    private long _outPointTicks = long.MinValue;
    private long _inPointFilePtsTicks = long.MinValue;
    private long _outPointFilePtsTicks = long.MinValue;

    public TimeSpan? InPoint
    {
        get
        {
            var t = Interlocked.Read(ref _inPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set
        {
            var normalized = value.HasValue ? NormalizeMarkerPosition(value.Value) : (TimeSpan?)null;
            Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);
            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);
        }
    }

    public TimeSpan? OutPoint
    {
        get
        {
            var t = Interlocked.Read(ref _outPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set
        {
            var normalized = value.HasValue ? NormalizeMarkerPosition(value.Value) : (TimeSpan?)null;
            Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);
            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);
        }
    }

    public TimeSpan? InPointFilePts
    {
        get
        {
            var t = Interlocked.Read(ref _inPointFilePtsTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
    }

    public TimeSpan? OutPointFilePts
    {
        get
        {
            var t = Interlocked.Read(ref _outPointFilePtsTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
    }

    public TimeSpan SetInPoint() => SetInPointAt(null);

    /// <summary>
    /// Pin the in-point at an explicit user-intended position rather than the
    /// controller's last decoded keyframe. The UI should pass the position the
    /// user is visually pointing at (its FlashbackPlaybackPosition), which during
    /// scrubbing is the user's drag target rather than the keyframe-snapped
    /// PlaybackPosition the controller publishes after each decode. Without this
    /// overload, mid-GOP "click In" landed on the prior keyframe and the marker
    /// could appear hundreds of milliseconds before where the playhead sat.
    /// </summary>
    public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);

    private TimeSpan SetInPointAt(TimeSpan? overridePosition)
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:SetInPoint");
            Logger.Log("FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
            return PlaybackPosition;
        }

        var pos = overridePosition.HasValue
            ? NormalizeMarkerPosition(overridePosition.Value)
            : PlaybackPosition;
        ClearLastCommandFailure();
        InPoint = pos;
        var outTicks = Interlocked.Read(ref _outPointTicks);
        if (outTicks != long.MinValue && outTicks <= pos.Ticks)
        {
            OutPoint = null;
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range");
        }

        Logger.Log($"FLASHBACK_PLAYBACK_SET_IN pos_ms={(long)pos.TotalMilliseconds} source={(overridePosition.HasValue ? "ui_override" : "playback")}");
        return pos;
    }

    public TimeSpan SetOutPoint() => SetOutPointAt(null);

    /// <summary>
    /// Pin the out-point at an explicit user-intended position. See
    /// <see cref="SetInPointAt(TimeSpan)"/> for the rationale: the UI's visual
    /// playhead and the controller's keyframe-snapped PlaybackPosition can
    /// differ by hundreds of milliseconds during scrubbing.
    /// </summary>
    public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);

    private TimeSpan SetOutPointAt(TimeSpan? overridePosition)
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:SetOutPoint");
            Logger.Log("FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
            return PlaybackPosition;
        }

        var pos = overridePosition.HasValue
            ? NormalizeMarkerPosition(overridePosition.Value)
            : PlaybackPosition;
        ClearLastCommandFailure();
        OutPoint = pos;
        var inTicks = Interlocked.Read(ref _inPointTicks);
        if (inTicks != long.MinValue && inTicks >= pos.Ticks)
        {
            InPoint = null;
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_IN invalid_range");
        }

        Logger.Log($"FLASHBACK_PLAYBACK_SET_OUT pos_ms={(long)pos.TotalMilliseconds} source={(overridePosition.HasValue ? "ui_override" : "playback")}");
        return pos;
    }

    public void ClearInOutPoints()
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:ClearInOutPoints");
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
            return;
        }

        InPoint = null;
        OutPoint = null;
        ClearLastCommandFailure();
        Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT");
    }

    public void RestoreInOutPoints(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts)
    {
        InPoint = inPoint;
        OutPoint = outPoint;

        if (inPoint.HasValue && inPointFilePts.HasValue && inPointFilePts.Value >= TimeSpan.Zero)
        {
            Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);
        }

        if (outPoint.HasValue && outPointFilePts.HasValue && outPointFilePts.Value >= TimeSpan.Zero)
        {
            Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);
        }
    }

    private bool CheckOutPoint(TimeSpan position, Stopwatch pacingStopwatch)
    {
        var outTicks = Interlocked.Read(ref _outPointTicks);
        if (outTicks != long.MinValue && position >= TimeSpan.FromTicks(outTicks))
        {
            Logger.Log($"FLASHBACK_PLAYBACK_HIT_OUTPOINT pos_ms={(long)position.TotalMilliseconds}");
            SafePauseRendering("out_point");
            pacingStopwatch.Stop();
            SetState(FlashbackPlaybackState.Paused);
            return true;
        }

        return false;
    }

    private TimeSpan NormalizeMarkerPosition(TimeSpan position)
    {
        if (position <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var bufferDuration = _bufferManager.BufferedDuration;
        return position > bufferDuration ? bufferDuration : position;
    }

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

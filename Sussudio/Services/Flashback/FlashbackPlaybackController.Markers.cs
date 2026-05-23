using System;
using System.Diagnostics;
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
}

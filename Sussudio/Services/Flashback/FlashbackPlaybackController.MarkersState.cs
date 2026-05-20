using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- In/Out point state ---

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
}

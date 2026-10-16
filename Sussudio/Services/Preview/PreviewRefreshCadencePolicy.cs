using System;

namespace Sussudio.Services.Preview;

/// <summary>
/// Turns one DXGI frame-statistics sample (presents and display refreshes elapsed between two displayed frames) into
/// the refreshes where a new source frame was due but the display repeated the previous one. The raw difference,
/// refreshes minus presents, counts every refresh beyond one per present, so a 60 fps source on a 120 Hz display
/// reports one "miss" per frame while pacing perfectly; this policy measures against the source cadence instead.
/// </summary>
internal static class PreviewRefreshCadencePolicy
{
    // A ratio this close to a whole number is that number: a measured 119.98 Hz must not turn a 60 fps source's exact
    // two-refresh span into slack that hides a real one-refresh miss. 59.94 fps on 120 Hz (2.002) also snaps, which
    // counts its natural extra repeat (about one every 8 s) as a miss.
    internal const double IntegerRatioTolerance = 0.01;

    /// <summary>
    /// Display refreshes per source frame, or 1 when either rate is unknown, which reduces the count to the raw one.
    /// </summary>
    internal static double RefreshesPerSourceFrame(double sourceFps, long refreshIntervalTicks, long ticksPerSecond)
    {
        if (!(sourceFps > 0) || !double.IsFinite(sourceFps) || refreshIntervalTicks <= 0 || ticksPerSecond <= 0)
        {
            return 1.0;
        }

        return ticksPerSecond / (sourceFps * refreshIntervalTicks);
    }

    /// <summary>
    /// On time, <paramref name="presentDelta"/> frames span at most ceil(presents x ratio) refreshes: floor or ceil of
    /// the exact span, depending on where the cadence phase starts. Each refresh beyond that showed a repeated frame
    /// while a new one was due. Below one refresh per frame (source faster than the display) every refresh is due a
    /// new frame, so the count is the raw one. Never exceeds the raw count.
    /// </summary>
    internal static long CountMissedRefreshes(long presentDelta, long presentRefreshDelta, double refreshesPerSourceFrame)
    {
        if (presentDelta <= 0 || presentRefreshDelta <= presentDelta)
        {
            return 0;
        }

        var ratio = double.IsFinite(refreshesPerSourceFrame) ? Math.Max(1.0, refreshesPerSourceFrame) : 1.0;
        var nearest = Math.Round(ratio);
        if (Math.Abs(ratio - nearest) <= nearest * IntegerRatioTolerance)
        {
            ratio = nearest;
        }

        var onTimeRefreshes = (long)Math.Ceiling(presentDelta * ratio);
        return Math.Max(0, presentRefreshDelta - onTimeRefreshes);
    }
}

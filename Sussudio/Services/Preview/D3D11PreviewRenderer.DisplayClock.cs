using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

internal readonly record struct PreviewDisplayClockSnapshot(
    long LastPresentTick,
    long FrameIntervalTicks,
    double ExpectedFrameIntervalMs,
    int SampleCount);

internal interface IPreviewDisplayClock
{
    bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot);
}

internal sealed partial class D3D11PreviewRenderer
{
    private long EstimateVisibleTick(long presentReturnTick)
    {
        var frameIntervalTicks = GetEstimatedDisplayFrameIntervalTicks();
        long displayClockTick;
        lock (_dxgiFrameStatisticsLock)
        {
            displayClockTick = _dxgiFrameStatisticsSyncQpcTime;
        }

        if (displayClockTick <= 0)
        {
            return presentReturnTick + frameIntervalTicks;
        }

        var visibleTick = displayClockTick;
        if (visibleTick <= presentReturnTick)
        {
            var intervalsBehind = ((presentReturnTick - visibleTick) / frameIntervalTicks) + 1;
            visibleTick += intervalsBehind * frameIntervalTicks;
        }

        var maxLeadTicks = frameIntervalTicks * Math.Max(1, Math.Min(3, _dxgiMaxFrameLatency + 1));
        if (visibleTick - presentReturnTick > maxLeadTicks)
        {
            return presentReturnTick + frameIntervalTicks;
        }

        return visibleTick;
    }

    private long GetEstimatedDisplayFrameIntervalTicks()
    {
        var fps = Math.Max(1.0, _startupFps);
        return Math.Max(1, (long)Math.Round(Stopwatch.Frequency / fps));
    }

    public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)
    {
        var fps = Math.Max(1.0, _startupFps);
        var intervalTicks = GetEstimatedDisplayFrameIntervalTicks();
        long lastPresentTick;
        int sampleCount;
        lock (_dxgiFrameStatisticsLock)
        {
            lastPresentTick = _dxgiFrameStatisticsSyncQpcTime > 0
                ? _dxgiFrameStatisticsSyncQpcTime
                : Interlocked.Read(ref _lastPresentTick);
            sampleCount = _dxgiFrameStatisticsSuccessCount > 0
                ? (int)Math.Min(int.MaxValue, _dxgiFrameStatisticsSuccessCount)
                : Volatile.Read(ref _presentIntervalCount);
        }

        snapshot = new PreviewDisplayClockSnapshot(
            LastPresentTick: lastPresentTick,
            FrameIntervalTicks: intervalTicks,
            ExpectedFrameIntervalMs: 1000.0 / fps,
            SampleCount: sampleCount);
        return lastPresentTick > 0;
    }
}

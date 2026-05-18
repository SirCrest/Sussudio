using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly object _dxgiFrameStatisticsLock = new();
    private long _dxgiFrameStatisticsSampleCount;
    private long _dxgiFrameStatisticsSuccessCount;
    private long _dxgiFrameStatisticsFailureCount;
    private string _dxgiFrameStatisticsLastError = string.Empty;
    private long _dxgiFrameStatisticsPresentCount = -1;
    private long _dxgiFrameStatisticsPresentRefreshCount = -1;
    private long _dxgiFrameStatisticsSyncRefreshCount = -1;
    private long _dxgiFrameStatisticsSyncQpcTime;
    private long _dxgiFrameStatisticsLastPresentDelta;
    private long _dxgiFrameStatisticsLastPresentRefreshDelta;
    private long _dxgiFrameStatisticsLastSyncRefreshDelta;
    private long _dxgiFrameStatisticsMissedRefreshCount;
    private long _dxgiFrameStatisticsFrameCounter;
    private long _dxgiFrameStatisticsLastSampleFrameCounter;
    private bool _dxgiFrameStatisticsHasBaseline;

    public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()
    {
        lock (_dxgiFrameStatisticsLock)
        {
            return new DxgiFrameStatisticsMetrics(
                SampleCount: _dxgiFrameStatisticsSampleCount,
                SuccessCount: _dxgiFrameStatisticsSuccessCount,
                FailureCount: _dxgiFrameStatisticsFailureCount,
                LastError: _dxgiFrameStatisticsLastError,
                PresentCount: _dxgiFrameStatisticsPresentCount,
                PresentRefreshCount: _dxgiFrameStatisticsPresentRefreshCount,
                SyncRefreshCount: _dxgiFrameStatisticsSyncRefreshCount,
                SyncQpcTime: _dxgiFrameStatisticsSyncQpcTime,
                LastPresentDelta: _dxgiFrameStatisticsLastPresentDelta,
                LastPresentRefreshDelta: _dxgiFrameStatisticsLastPresentRefreshDelta,
                LastSyncRefreshDelta: _dxgiFrameStatisticsLastSyncRefreshDelta,
                MissedRefreshCount: _dxgiFrameStatisticsMissedRefreshCount);
        }
    }

    private void TrackDxgiFrameStatistics()
    {
        if (!_dxgiFrameStatisticsEnabled || _swapChain == null)
        {
            return;
        }

        var frameCounter = Interlocked.Increment(ref _dxgiFrameStatisticsFrameCounter);
        if (_dxgiFrameStatisticsSampleIntervalFrames > 1 &&
            frameCounter % _dxgiFrameStatisticsSampleIntervalFrames != 0)
        {
            return;
        }

        try
        {
            if (_dxgiFrameStatisticsDwmFlushEnabled)
            {
                _ = DwmFlush();
            }

            var result = _swapChain.GetFrameStatistics(out var stats);
            lock (_dxgiFrameStatisticsLock)
            {
                _dxgiFrameStatisticsLastSampleFrameCounter = frameCounter;
                _dxgiFrameStatisticsSampleCount++;
                if (result.Failure)
                {
                    _dxgiFrameStatisticsFailureCount++;
                    _dxgiFrameStatisticsLastError = $"0x{result.Code:X8}";
                    return;
                }

                _dxgiFrameStatisticsSuccessCount++;
                _dxgiFrameStatisticsLastError = string.Empty;

                var presentCount = (long)stats.PresentCount;
                var presentRefreshCount = (long)stats.PresentRefreshCount;
                var syncRefreshCount = (long)stats.SyncRefreshCount;
                _dxgiFrameStatisticsSyncQpcTime = stats.SyncQPCTime;

                if (_dxgiFrameStatisticsHasBaseline &&
                    _dxgiFrameStatisticsPresentCount > 0 &&
                    _dxgiFrameStatisticsPresentRefreshCount > 0 &&
                    _dxgiFrameStatisticsSyncRefreshCount > 0)
                {
                    _dxgiFrameStatisticsLastPresentDelta = presentCount - _dxgiFrameStatisticsPresentCount;
                    _dxgiFrameStatisticsLastPresentRefreshDelta = presentRefreshCount - _dxgiFrameStatisticsPresentRefreshCount;
                    _dxgiFrameStatisticsLastSyncRefreshDelta = syncRefreshCount - _dxgiFrameStatisticsSyncRefreshCount;
                    if (_dxgiFrameStatisticsLastPresentDelta < 0 ||
                        _dxgiFrameStatisticsLastPresentRefreshDelta < 0 ||
                        _dxgiFrameStatisticsLastSyncRefreshDelta < 0 ||
                        _dxgiFrameStatisticsLastPresentDelta > 100 ||
                        _dxgiFrameStatisticsLastPresentRefreshDelta > 100 ||
                        _dxgiFrameStatisticsLastSyncRefreshDelta > 100)
                    {
                        _dxgiFrameStatisticsLastPresentDelta = 0;
                        _dxgiFrameStatisticsLastPresentRefreshDelta = 0;
                        _dxgiFrameStatisticsLastSyncRefreshDelta = 0;
                    }
                    else if (_dxgiFrameStatisticsLastPresentDelta > 0 &&
                             _dxgiFrameStatisticsLastPresentRefreshDelta > _dxgiFrameStatisticsLastPresentDelta)
                    {
                        _dxgiFrameStatisticsMissedRefreshCount +=
                            _dxgiFrameStatisticsLastPresentRefreshDelta - _dxgiFrameStatisticsLastPresentDelta;
                    }
                }
                else
                {
                    _dxgiFrameStatisticsLastPresentDelta = 0;
                    _dxgiFrameStatisticsLastPresentRefreshDelta = 0;
                    _dxgiFrameStatisticsLastSyncRefreshDelta = 0;
                }

                _dxgiFrameStatisticsPresentCount = presentCount;
                _dxgiFrameStatisticsPresentRefreshCount = presentRefreshCount;
                _dxgiFrameStatisticsSyncRefreshCount = syncRefreshCount;
                _dxgiFrameStatisticsHasBaseline =
                    presentCount > 0 &&
                    presentRefreshCount > 0 &&
                    syncRefreshCount > 0;
            }
        }
        catch (Exception ex)
        {
            lock (_dxgiFrameStatisticsLock)
            {
                _dxgiFrameStatisticsLastSampleFrameCounter = frameCounter;
                _dxgiFrameStatisticsSampleCount++;
                _dxgiFrameStatisticsFailureCount++;
                _dxgiFrameStatisticsLastError = $"{ex.GetType().Name}:0x{ex.HResult:X8}";
            }
        }
    }

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

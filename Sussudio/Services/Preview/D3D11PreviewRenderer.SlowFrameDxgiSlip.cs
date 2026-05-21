using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly record struct SlowFrameDxgiSlipSnapshot(
        long PresentDelta,
        long PresentRefreshDelta,
        long SyncRefreshDelta,
        long MissedRefreshCount,
        bool IsRefreshSlip);

    private SlowFrameDxgiSlipSnapshot CaptureSlowFrameDxgiSlipSnapshot()
    {
        var frameStatisticsFrameCounter = Interlocked.Read(ref _dxgiFrameStatisticsFrameCounter);
        long presentDelta;
        long presentRefreshDelta;
        long syncRefreshDelta;
        long missedRefreshCount;
        long frameStatisticsLastSampleFrameCounter;
        lock (_dxgiFrameStatisticsLock)
        {
            presentDelta = _dxgiFrameStatisticsLastPresentDelta;
            presentRefreshDelta = _dxgiFrameStatisticsLastPresentRefreshDelta;
            syncRefreshDelta = _dxgiFrameStatisticsLastSyncRefreshDelta;
            missedRefreshCount = _dxgiFrameStatisticsMissedRefreshCount;
            frameStatisticsLastSampleFrameCounter = _dxgiFrameStatisticsLastSampleFrameCounter;
        }

        var isRefreshSlip =
            frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter &&
            presentDelta > 0 &&
            presentRefreshDelta > presentDelta;

        return new SlowFrameDxgiSlipSnapshot(
            PresentDelta: presentDelta,
            PresentRefreshDelta: presentRefreshDelta,
            SyncRefreshDelta: syncRefreshDelta,
            MissedRefreshCount: missedRefreshCount,
            IsRefreshSlip: isRefreshSlip);
    }
}

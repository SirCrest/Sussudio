namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public long D3DFrameStatsSampleCount { get; private set; }
    public long D3DFrameStatsSuccessCount { get; private set; }
    public long D3DFrameStatsFailureCount { get; private set; }
    public string D3DFrameStatsLastError { get; private set; } = string.Empty;
    public long D3DFrameStatsPresentCount { get; private set; }
    public long D3DFrameStatsPresentRefreshCount { get; private set; }
    public long D3DFrameStatsSyncRefreshCount { get; private set; }
    public long D3DFrameStatsSyncQpcTime { get; private set; }
    public long D3DFrameStatsLastPresentDelta { get; private set; }
    public long D3DFrameStatsLastPresentRefreshDelta { get; private set; }
    public long D3DFrameStatsLastSyncRefreshDelta { get; private set; }
    public long D3DFrameStatsMissedRefreshCount { get; private set; }

    private void ApplyFrameStatistics(PreviewRuntimeD3DFrameStatistics frameStatistics)
    {
        D3DFrameStatsSampleCount = frameStatistics.SampleCount;
        D3DFrameStatsSuccessCount = frameStatistics.SuccessCount;
        D3DFrameStatsFailureCount = frameStatistics.FailureCount;
        D3DFrameStatsLastError = frameStatistics.LastError;
        D3DFrameStatsPresentCount = frameStatistics.PresentCount;
        D3DFrameStatsPresentRefreshCount = frameStatistics.PresentRefreshCount;
        D3DFrameStatsSyncRefreshCount = frameStatistics.SyncRefreshCount;
        D3DFrameStatsSyncQpcTime = frameStatistics.SyncQpcTime;
        D3DFrameStatsLastPresentDelta = frameStatistics.LastPresentDelta;
        D3DFrameStatsLastPresentRefreshDelta = frameStatistics.LastPresentRefreshDelta;
        D3DFrameStatsLastSyncRefreshDelta = frameStatistics.LastSyncRefreshDelta;
        D3DFrameStatsMissedRefreshCount = frameStatistics.MissedRefreshCount;
    }
}

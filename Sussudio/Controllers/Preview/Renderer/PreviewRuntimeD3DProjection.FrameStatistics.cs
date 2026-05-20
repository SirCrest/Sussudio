namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public long D3DFrameStatsSampleCount { get; init; }
    public long D3DFrameStatsSuccessCount { get; init; }
    public long D3DFrameStatsFailureCount { get; init; }
    public string D3DFrameStatsLastError { get; init; } = string.Empty;
    public long D3DFrameStatsPresentCount { get; init; }
    public long D3DFrameStatsPresentRefreshCount { get; init; }
    public long D3DFrameStatsSyncRefreshCount { get; init; }
    public long D3DFrameStatsSyncQpcTime { get; init; }
    public long D3DFrameStatsLastPresentDelta { get; init; }
    public long D3DFrameStatsLastPresentRefreshDelta { get; init; }
    public long D3DFrameStatsLastSyncRefreshDelta { get; init; }
    public long D3DFrameStatsMissedRefreshCount { get; init; }
}

using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DFrameStatistics(
    long SampleCount,
    long SuccessCount,
    long FailureCount,
    string LastError,
    long PresentCount,
    long PresentRefreshCount,
    long SyncRefreshCount,
    long SyncQpcTime,
    long LastPresentDelta,
    long LastPresentRefreshDelta,
    long LastSyncRefreshDelta,
    long MissedRefreshCount);

internal static class PreviewRuntimeD3DFrameStatisticsPolicy
{
    public static PreviewRuntimeD3DFrameStatistics Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameStats = d3d?.GetDxgiFrameStatisticsMetrics();

        return new PreviewRuntimeD3DFrameStatistics(
            SampleCount: frameStats?.SampleCount ?? 0,
            SuccessCount: frameStats?.SuccessCount ?? 0,
            FailureCount: frameStats?.FailureCount ?? 0,
            LastError: frameStats?.LastError ?? string.Empty,
            PresentCount: frameStats?.PresentCount ?? -1,
            PresentRefreshCount: frameStats?.PresentRefreshCount ?? -1,
            SyncRefreshCount: frameStats?.SyncRefreshCount ?? -1,
            SyncQpcTime: frameStats?.SyncQpcTime ?? 0,
            LastPresentDelta: frameStats?.LastPresentDelta ?? 0,
            LastPresentRefreshDelta: frameStats?.LastPresentRefreshDelta ?? 0,
            LastSyncRefreshDelta: frameStats?.LastSyncRefreshDelta ?? 0,
            MissedRefreshCount: frameStats?.MissedRefreshCount ?? 0);
    }
}

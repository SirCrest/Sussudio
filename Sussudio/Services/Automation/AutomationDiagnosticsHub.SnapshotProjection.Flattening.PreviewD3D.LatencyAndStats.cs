namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DLatencyAndStatsFlattenedProjection BuildPreviewD3DLatencyAndStatsFlattenedProjection(
        PreviewD3DPipelineLatencyProjection pipelineLatency,
        PreviewD3DFrameLatencyWaitProjection frameLatencyWait,
        PreviewD3DFrameStatsProjection frameStats)
        => new()
        {
            PipelineLatencySampleCount = pipelineLatency.SampleCount,
            PipelineLatencyAvgMs = pipelineLatency.AvgMs,
            PipelineLatencyP95Ms = pipelineLatency.P95Ms,
            PipelineLatencyP99Ms = pipelineLatency.P99Ms,
            PipelineLatencyMaxMs = pipelineLatency.MaxMs,
            FrameLatencyWaitEnabled = frameLatencyWait.Enabled,
            FrameLatencyWaitHandleActive = frameLatencyWait.HandleActive,
            FrameLatencyWaitCallCount = frameLatencyWait.CallCount,
            FrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount,
            FrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,
            FrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount,
            FrameLatencyWaitLastResult = frameLatencyWait.LastResult,
            FrameLatencyWaitLastMs = frameLatencyWait.LastMs,
            FrameLatencyWaitSampleCount = frameLatencyWait.SampleCount,
            FrameLatencyWaitAvgMs = frameLatencyWait.AvgMs,
            FrameLatencyWaitP95Ms = frameLatencyWait.P95Ms,
            FrameLatencyWaitP99Ms = frameLatencyWait.P99Ms,
            FrameLatencyWaitMaxMs = frameLatencyWait.MaxMs,
            FrameStatsSampleCount = frameStats.SampleCount,
            FrameStatsSuccessCount = frameStats.SuccessCount,
            FrameStatsFailureCount = frameStats.FailureCount,
            FrameStatsLastError = frameStats.LastError,
            FrameStatsPresentCount = frameStats.PresentCount,
            FrameStatsPresentRefreshCount = frameStats.PresentRefreshCount,
            FrameStatsSyncRefreshCount = frameStats.SyncRefreshCount,
            FrameStatsSyncQpcTime = frameStats.SyncQpcTime,
            FrameStatsLastPresentDelta = frameStats.LastPresentDelta,
            FrameStatsLastPresentRefreshDelta = frameStats.LastPresentRefreshDelta,
            FrameStatsLastSyncRefreshDelta = frameStats.LastSyncRefreshDelta,
            FrameStatsMissedRefreshCount = frameStats.MissedRefreshCount,
            FrameStatsRecentMissedRefreshCount = frameStats.RecentMissedRefreshCount,
            FrameStatsRecentFailureCount = frameStats.RecentFailureCount
        };

    private readonly record struct PreviewD3DLatencyAndStatsFlattenedProjection
    {
        public int PipelineLatencySampleCount { get; init; }
        public double PipelineLatencyAvgMs { get; init; }
        public double PipelineLatencyP95Ms { get; init; }
        public double PipelineLatencyP99Ms { get; init; }
        public double PipelineLatencyMaxMs { get; init; }
        public bool FrameLatencyWaitEnabled { get; init; }
        public bool FrameLatencyWaitHandleActive { get; init; }
        public long FrameLatencyWaitCallCount { get; init; }
        public long FrameLatencyWaitSignaledCount { get; init; }
        public long FrameLatencyWaitTimeoutCount { get; init; }
        public long FrameLatencyWaitUnexpectedResultCount { get; init; }
        public uint FrameLatencyWaitLastResult { get; init; }
        public double FrameLatencyWaitLastMs { get; init; }
        public int FrameLatencyWaitSampleCount { get; init; }
        public double FrameLatencyWaitAvgMs { get; init; }
        public double FrameLatencyWaitP95Ms { get; init; }
        public double FrameLatencyWaitP99Ms { get; init; }
        public double FrameLatencyWaitMaxMs { get; init; }
        public long FrameStatsSampleCount { get; init; }
        public long FrameStatsSuccessCount { get; init; }
        public long FrameStatsFailureCount { get; init; }
        public string FrameStatsLastError { get; init; }
        public long FrameStatsPresentCount { get; init; }
        public long FrameStatsPresentRefreshCount { get; init; }
        public long FrameStatsSyncRefreshCount { get; init; }
        public long FrameStatsSyncQpcTime { get; init; }
        public long FrameStatsLastPresentDelta { get; init; }
        public long FrameStatsLastPresentRefreshDelta { get; init; }
        public long FrameStatsLastSyncRefreshDelta { get; init; }
        public long FrameStatsMissedRefreshCount { get; init; }
        public long FrameStatsRecentMissedRefreshCount { get; init; }
        public long FrameStatsRecentFailureCount { get; init; }
    }
}

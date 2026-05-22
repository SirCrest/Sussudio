using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DProjection BuildPreviewD3DProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);
        var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);
        var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);
        var pipelineLatency = BuildPreviewD3DPipelineLatencyProjection(previewRuntime);
        var frameStats = BuildPreviewD3DFrameStatsProjection(
            previewRuntime,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        return new()
        {
            PresentSyncInterval = previewRuntime.D3DPresentSyncInterval,
            MaxFrameLatency = previewRuntime.D3DMaxFrameLatency,
            SwapChainBufferCount = previewRuntime.D3DSwapChainBufferCount,
            SwapChainAddress = previewRuntime.D3DSwapChainAddress,
            FramesSubmitted = previewRuntime.D3DFramesSubmitted,
            FramesRendered = previewRuntime.D3DFramesRendered,
            FramesDropped = previewRuntime.D3DFramesDropped,
            RenderThreadFailureCount = previewRuntime.D3DRenderThreadFailureCount,
            LastRenderThreadFailureType = previewRuntime.D3DLastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewRuntime.D3DLastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewRuntime.D3DLastRenderThreadFailureHResult,
            PendingFrameCount = previewRuntime.D3DPendingFrameCount,
            InputColorSpace = previewRuntime.D3DInputColorSpace,
            OutputColorSpace = previewRuntime.D3DOutputColorSpace,
            CpuTiming = cpuTiming,
            FrameLatencyWait = frameLatencyWait,
            PipelineLatency = pipelineLatency,
            FrameStats = frameStats,
            FrameFlow = frameFlow
        };
    }

    private readonly record struct PreviewD3DProjection
    {
        public int PresentSyncInterval { get; init; }
        public int MaxFrameLatency { get; init; }
        public int SwapChainBufferCount { get; init; }
        public string SwapChainAddress { get; init; }
        public long FramesSubmitted { get; init; }
        public long FramesRendered { get; init; }
        public long FramesDropped { get; init; }
        public long RenderThreadFailureCount { get; init; }
        public string LastRenderThreadFailureType { get; init; }
        public string LastRenderThreadFailureMessage { get; init; }
        public int LastRenderThreadFailureHResult { get; init; }
        public int PendingFrameCount { get; init; }
        public string InputColorSpace { get; init; }
        public string OutputColorSpace { get; init; }
        public PreviewD3DCpuTimingProjection CpuTiming { get; init; }
        public PreviewD3DFrameLatencyWaitProjection FrameLatencyWait { get; init; }
        public PreviewD3DPipelineLatencyProjection PipelineLatency { get; init; }
        public PreviewD3DFrameStatsProjection FrameStats { get; init; }
        public PreviewD3DFrameFlowProjection FrameFlow { get; init; }
    }

    private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(
        PreviewD3DProjection previewD3D)
        => new()
        {
            PresentSyncInterval = previewD3D.PresentSyncInterval,
            MaxFrameLatency = previewD3D.MaxFrameLatency,
            SwapChainBufferCount = previewD3D.SwapChainBufferCount,
            SwapChainAddress = previewD3D.SwapChainAddress,
            FramesSubmitted = previewD3D.FramesSubmitted,
            FramesRendered = previewD3D.FramesRendered,
            FramesDropped = previewD3D.FramesDropped,
            RenderThreadFailureCount = previewD3D.RenderThreadFailureCount,
            LastRenderThreadFailureType = previewD3D.LastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewD3D.LastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewD3D.LastRenderThreadFailureHResult,
            PendingFrameCount = previewD3D.PendingFrameCount,
            InputColorSpace = previewD3D.InputColorSpace,
            OutputColorSpace = previewD3D.OutputColorSpace,
            CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),
            LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(
                previewD3D.PipelineLatency,
                previewD3D.FrameLatencyWait,
                previewD3D.FrameStats),
            FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)
        };

    private readonly record struct PreviewD3DFlattenedProjection
    {
        public int PresentSyncInterval { get; init; }
        public int MaxFrameLatency { get; init; }
        public int SwapChainBufferCount { get; init; }
        public string SwapChainAddress { get; init; }
        public long FramesSubmitted { get; init; }
        public long FramesRendered { get; init; }
        public long FramesDropped { get; init; }
        public long RenderThreadFailureCount { get; init; }
        public string LastRenderThreadFailureType { get; init; }
        public string LastRenderThreadFailureMessage { get; init; }
        public int LastRenderThreadFailureHResult { get; init; }
        public int PendingFrameCount { get; init; }
        public string InputColorSpace { get; init; }
        public string OutputColorSpace { get; init; }
        public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }
        public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }
        public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }
    }

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

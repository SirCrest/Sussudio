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
            FrameStats = frameStats,
            FrameFlow = frameFlow
        };
    }

    private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            LastSubmittedPreviewPresentId = previewRuntime.D3DLastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = previewRuntime.D3DLastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = previewRuntime.D3DLastSubmittedSourcePtsTicks,
            LastSubmittedQpc = previewRuntime.D3DLastSubmittedQpc,
            LastSubmittedUtcUnixMs = previewRuntime.D3DLastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = previewRuntime.D3DLastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = previewRuntime.D3DLastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = previewRuntime.D3DLastRenderedSourcePtsTicks,
            LastRenderedQpc = previewRuntime.D3DLastRenderedQpc,
            LastRenderedUtcUnixMs = previewRuntime.D3DLastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = previewRuntime.D3DLastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = previewRuntime.D3DLastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = previewRuntime.D3DLastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = previewRuntime.D3DLastDroppedSourcePtsTicks,
            LastDroppedQpc = previewRuntime.D3DLastDroppedQpc,
            LastDroppedUtcUnixMs = previewRuntime.D3DLastDroppedUtcUnixMs,
            LastDropReason = previewRuntime.D3DLastDropReason,
            RecentSlowFrames = previewRuntime.D3DRecentSlowFrames
        };

    private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,
            HandleActive = previewRuntime.D3DFrameLatencyWaitHandleActive,
            CallCount = previewRuntime.D3DFrameLatencyWaitCallCount,
            SignaledCount = previewRuntime.D3DFrameLatencyWaitSignaledCount,
            TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
            UnexpectedResultCount = previewRuntime.D3DFrameLatencyWaitUnexpectedResultCount,
            LastResult = previewRuntime.D3DFrameLatencyWaitLastResult,
            LastMs = previewRuntime.D3DFrameLatencyWaitLastMs,
            SampleCount = previewRuntime.D3DFrameLatencyWaitSampleCount,
            AvgMs = previewRuntime.D3DFrameLatencyWaitAvgMs,
            P95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
            P99Ms = previewRuntime.D3DFrameLatencyWaitP99Ms,
            MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs
        };

    private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
        => new()
        {
            SampleCount = previewRuntime.D3DFrameStatsSampleCount,
            SuccessCount = previewRuntime.D3DFrameStatsSuccessCount,
            FailureCount = previewRuntime.D3DFrameStatsFailureCount,
            LastError = previewRuntime.D3DFrameStatsLastError,
            PresentCount = previewRuntime.D3DFrameStatsPresentCount,
            PresentRefreshCount = previewRuntime.D3DFrameStatsPresentRefreshCount,
            SyncRefreshCount = previewRuntime.D3DFrameStatsSyncRefreshCount,
            SyncQpcTime = previewRuntime.D3DFrameStatsSyncQpcTime,
            LastPresentDelta = previewRuntime.D3DFrameStatsLastPresentDelta,
            LastPresentRefreshDelta = previewRuntime.D3DFrameStatsLastPresentRefreshDelta,
            LastSyncRefreshDelta = previewRuntime.D3DFrameStatsLastSyncRefreshDelta,
            MissedRefreshCount = previewRuntime.D3DFrameStatsMissedRefreshCount,
            RecentMissedRefreshCount = recentD3DMissedRefreshes,
            RecentFailureCount = recentD3DStatsFailures
        };

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
        public PreviewD3DFrameStatsProjection FrameStats { get; init; }
        public PreviewD3DFrameFlowProjection FrameFlow { get; init; }
    }

    private readonly record struct PreviewD3DFrameFlowProjection
    {
        public long LastSubmittedPreviewPresentId { get; init; }
        public long LastSubmittedSourceSequenceNumber { get; init; }
        public long LastSubmittedSourcePtsTicks { get; init; }
        public long LastSubmittedQpc { get; init; }
        public long LastSubmittedUtcUnixMs { get; init; }
        public long LastRenderedPreviewPresentId { get; init; }
        public long LastRenderedSourceSequenceNumber { get; init; }
        public long LastRenderedSourcePtsTicks { get; init; }
        public long LastRenderedQpc { get; init; }
        public long LastRenderedUtcUnixMs { get; init; }
        public double LastRenderedSchedulerToPresentMs { get; init; }
        public double LastRenderedPipelineLatencyMs { get; init; }
        public long LastDroppedPreviewPresentId { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDroppedSourcePtsTicks { get; init; }
        public long LastDroppedQpc { get; init; }
        public long LastDroppedUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }
    }

    private readonly record struct PreviewD3DFrameLatencyWaitProjection
    {
        public bool Enabled { get; init; }
        public bool HandleActive { get; init; }
        public long CallCount { get; init; }
        public long SignaledCount { get; init; }
        public long TimeoutCount { get; init; }
        public long UnexpectedResultCount { get; init; }
        public uint LastResult { get; init; }
        public double LastMs { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }

    private readonly record struct PreviewD3DFrameStatsProjection
    {
        public long SampleCount { get; init; }
        public long SuccessCount { get; init; }
        public long FailureCount { get; init; }
        public string LastError { get; init; }
        public long PresentCount { get; init; }
        public long PresentRefreshCount { get; init; }
        public long SyncRefreshCount { get; init; }
        public long SyncQpcTime { get; init; }
        public long LastPresentDelta { get; init; }
        public long LastPresentRefreshDelta { get; init; }
        public long LastSyncRefreshDelta { get; init; }
        public long MissedRefreshCount { get; init; }
        public long RecentMissedRefreshCount { get; init; }
        public long RecentFailureCount { get; init; }
    }
}

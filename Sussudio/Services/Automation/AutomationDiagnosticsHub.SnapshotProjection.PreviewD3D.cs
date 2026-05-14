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
        public PreviewD3DFrameStatsProjection FrameStats { get; init; }
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
}

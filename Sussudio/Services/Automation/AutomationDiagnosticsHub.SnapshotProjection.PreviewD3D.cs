using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DProjection BuildPreviewD3DProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);

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
            CpuTimingSampleCount = previewRuntime.D3DCpuTimingSampleCount,
            InputUploadCpuAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            InputUploadCpuP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            InputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
            InputUploadCpuMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            RenderSubmitCpuAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            RenderSubmitCpuP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            RenderSubmitCpuP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
            RenderSubmitCpuMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
            PresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            TotalFrameCpuAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            TotalFrameCpuP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            TotalFrameCpuP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
            TotalFrameCpuMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs,
            PipelineLatencySampleCount = previewRuntime.D3DPipelineLatencySampleCount,
            PipelineLatencyAvgMs = previewRuntime.D3DPipelineLatencyAvgMs,
            PipelineLatencyP95Ms = previewRuntime.D3DPipelineLatencyP95Ms,
            PipelineLatencyP99Ms = previewRuntime.D3DPipelineLatencyP99Ms,
            PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,
            FrameLatencyWait = frameLatencyWait,
            FrameStatsSampleCount = previewRuntime.D3DFrameStatsSampleCount,
            FrameStatsSuccessCount = previewRuntime.D3DFrameStatsSuccessCount,
            FrameStatsFailureCount = previewRuntime.D3DFrameStatsFailureCount,
            FrameStatsLastError = previewRuntime.D3DFrameStatsLastError,
            FrameStatsPresentCount = previewRuntime.D3DFrameStatsPresentCount,
            FrameStatsPresentRefreshCount = previewRuntime.D3DFrameStatsPresentRefreshCount,
            FrameStatsSyncRefreshCount = previewRuntime.D3DFrameStatsSyncRefreshCount,
            FrameStatsSyncQpcTime = previewRuntime.D3DFrameStatsSyncQpcTime,
            FrameStatsLastPresentDelta = previewRuntime.D3DFrameStatsLastPresentDelta,
            FrameStatsLastPresentRefreshDelta = previewRuntime.D3DFrameStatsLastPresentRefreshDelta,
            FrameStatsLastSyncRefreshDelta = previewRuntime.D3DFrameStatsLastSyncRefreshDelta,
            FrameStatsMissedRefreshCount = previewRuntime.D3DFrameStatsMissedRefreshCount,
            FrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,
            FrameStatsRecentFailureCount = recentD3DStatsFailures,
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
        public int CpuTimingSampleCount { get; init; }
        public double InputUploadCpuAvgMs { get; init; }
        public double InputUploadCpuP95Ms { get; init; }
        public double InputUploadCpuP99Ms { get; init; }
        public double InputUploadCpuMaxMs { get; init; }
        public double RenderSubmitCpuAvgMs { get; init; }
        public double RenderSubmitCpuP95Ms { get; init; }
        public double RenderSubmitCpuP99Ms { get; init; }
        public double RenderSubmitCpuMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameCpuAvgMs { get; init; }
        public double TotalFrameCpuP95Ms { get; init; }
        public double TotalFrameCpuP99Ms { get; init; }
        public double TotalFrameCpuMaxMs { get; init; }
        public int PipelineLatencySampleCount { get; init; }
        public double PipelineLatencyAvgMs { get; init; }
        public double PipelineLatencyP95Ms { get; init; }
        public double PipelineLatencyP99Ms { get; init; }
        public double PipelineLatencyMaxMs { get; init; }
        public PreviewD3DFrameLatencyWaitProjection FrameLatencyWait { get; init; }
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

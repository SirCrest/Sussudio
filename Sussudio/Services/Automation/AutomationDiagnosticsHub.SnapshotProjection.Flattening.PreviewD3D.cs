using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
            CpuTimingSampleCount = previewD3D.CpuTiming.SampleCount,
            InputUploadCpuAvgMs = previewD3D.CpuTiming.InputUploadAvgMs,
            InputUploadCpuP95Ms = previewD3D.CpuTiming.InputUploadP95Ms,
            InputUploadCpuP99Ms = previewD3D.CpuTiming.InputUploadP99Ms,
            InputUploadCpuMaxMs = previewD3D.CpuTiming.InputUploadMaxMs,
            RenderSubmitCpuAvgMs = previewD3D.CpuTiming.RenderSubmitAvgMs,
            RenderSubmitCpuP95Ms = previewD3D.CpuTiming.RenderSubmitP95Ms,
            RenderSubmitCpuP99Ms = previewD3D.CpuTiming.RenderSubmitP99Ms,
            RenderSubmitCpuMaxMs = previewD3D.CpuTiming.RenderSubmitMaxMs,
            PresentCallAvgMs = previewD3D.CpuTiming.PresentCallAvgMs,
            PresentCallP95Ms = previewD3D.CpuTiming.PresentCallP95Ms,
            PresentCallP99Ms = previewD3D.CpuTiming.PresentCallP99Ms,
            PresentCallMaxMs = previewD3D.CpuTiming.PresentCallMaxMs,
            TotalFrameCpuAvgMs = previewD3D.CpuTiming.TotalFrameAvgMs,
            TotalFrameCpuP95Ms = previewD3D.CpuTiming.TotalFrameP95Ms,
            TotalFrameCpuP99Ms = previewD3D.CpuTiming.TotalFrameP99Ms,
            TotalFrameCpuMaxMs = previewD3D.CpuTiming.TotalFrameMaxMs,
            PipelineLatencySampleCount = previewD3D.PipelineLatency.SampleCount,
            PipelineLatencyAvgMs = previewD3D.PipelineLatency.AvgMs,
            PipelineLatencyP95Ms = previewD3D.PipelineLatency.P95Ms,
            PipelineLatencyP99Ms = previewD3D.PipelineLatency.P99Ms,
            PipelineLatencyMaxMs = previewD3D.PipelineLatency.MaxMs,
            FrameLatencyWaitEnabled = previewD3D.FrameLatencyWait.Enabled,
            FrameLatencyWaitHandleActive = previewD3D.FrameLatencyWait.HandleActive,
            FrameLatencyWaitCallCount = previewD3D.FrameLatencyWait.CallCount,
            FrameLatencyWaitSignaledCount = previewD3D.FrameLatencyWait.SignaledCount,
            FrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,
            FrameLatencyWaitUnexpectedResultCount = previewD3D.FrameLatencyWait.UnexpectedResultCount,
            FrameLatencyWaitLastResult = previewD3D.FrameLatencyWait.LastResult,
            FrameLatencyWaitLastMs = previewD3D.FrameLatencyWait.LastMs,
            FrameLatencyWaitSampleCount = previewD3D.FrameLatencyWait.SampleCount,
            FrameLatencyWaitAvgMs = previewD3D.FrameLatencyWait.AvgMs,
            FrameLatencyWaitP95Ms = previewD3D.FrameLatencyWait.P95Ms,
            FrameLatencyWaitP99Ms = previewD3D.FrameLatencyWait.P99Ms,
            FrameLatencyWaitMaxMs = previewD3D.FrameLatencyWait.MaxMs,
            FrameStatsSampleCount = previewD3D.FrameStats.SampleCount,
            FrameStatsSuccessCount = previewD3D.FrameStats.SuccessCount,
            FrameStatsFailureCount = previewD3D.FrameStats.FailureCount,
            FrameStatsLastError = previewD3D.FrameStats.LastError,
            FrameStatsPresentCount = previewD3D.FrameStats.PresentCount,
            FrameStatsPresentRefreshCount = previewD3D.FrameStats.PresentRefreshCount,
            FrameStatsSyncRefreshCount = previewD3D.FrameStats.SyncRefreshCount,
            FrameStatsSyncQpcTime = previewD3D.FrameStats.SyncQpcTime,
            FrameStatsLastPresentDelta = previewD3D.FrameStats.LastPresentDelta,
            FrameStatsLastPresentRefreshDelta = previewD3D.FrameStats.LastPresentRefreshDelta,
            FrameStatsLastSyncRefreshDelta = previewD3D.FrameStats.LastSyncRefreshDelta,
            FrameStatsMissedRefreshCount = previewD3D.FrameStats.MissedRefreshCount,
            FrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,
            FrameStatsRecentFailureCount = previewD3D.FrameStats.RecentFailureCount,
            LastSubmittedPreviewPresentId = previewD3D.FrameFlow.LastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = previewD3D.FrameFlow.LastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = previewD3D.FrameFlow.LastSubmittedSourcePtsTicks,
            LastSubmittedQpc = previewD3D.FrameFlow.LastSubmittedQpc,
            LastSubmittedUtcUnixMs = previewD3D.FrameFlow.LastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = previewD3D.FrameFlow.LastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = previewD3D.FrameFlow.LastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = previewD3D.FrameFlow.LastRenderedSourcePtsTicks,
            LastRenderedQpc = previewD3D.FrameFlow.LastRenderedQpc,
            LastRenderedUtcUnixMs = previewD3D.FrameFlow.LastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = previewD3D.FrameFlow.LastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = previewD3D.FrameFlow.LastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = previewD3D.FrameFlow.LastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = previewD3D.FrameFlow.LastDroppedSourcePtsTicks,
            LastDroppedQpc = previewD3D.FrameFlow.LastDroppedQpc,
            LastDroppedUtcUnixMs = previewD3D.FrameFlow.LastDroppedUtcUnixMs,
            LastDropReason = previewD3D.FrameFlow.LastDropReason,
            RecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames
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

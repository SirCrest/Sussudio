using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);
        var rendererCadence = d3d?.GetPresentCadenceMetrics(input.PreviewMinPresentationIntervalMs);
        var d3dRenderCpuTiming = d3d?.GetRenderCpuTimingMetrics();
        var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);
        var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);
        var d3dFrameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();
        var d3dPipelineLatency = d3d?.GetPipelineLatencyMetrics();

        return new PreviewRuntimeD3DProjection
        {
            GpuActive = frameCounters.GpuActive,
            RendererAttached = frameCounters.RendererAttached,
            FramesArrived = frameCounters.FramesArrived,
            FramesDisplayed = frameCounters.FramesDisplayed,
            FramesDropped = frameCounters.FramesDropped,
            RendererMode = d3d?.RendererMode ?? (input.IsPreviewing ? "CpuSoftwareBitmap" : "None"),
            DisplayCadenceSampleCount = rendererCadence?.SampleCount ?? 0,
            DisplayCadenceObservedFps = rendererCadence?.ObservedFps ?? 0,
            DisplayCadenceExpectedIntervalMs = rendererCadence?.ExpectedIntervalMs ?? 0,
            DisplayCadenceAverageIntervalMs = rendererCadence?.AverageIntervalMs ?? 0,
            DisplayCadenceP95IntervalMs = rendererCadence?.P95IntervalMs ?? 0,
            DisplayCadenceP99IntervalMs = rendererCadence?.P99IntervalMs ?? 0,
            DisplayCadenceMaxIntervalMs = rendererCadence?.MaxIntervalMs ?? 0,
            DisplayCadenceOnePercentLowFps = rendererCadence?.OnePercentLowFps ?? 0,
            DisplayCadenceFivePercentLowFps = rendererCadence?.FivePercentLowFps ?? 0,
            DisplayCadenceSampleDurationMs = rendererCadence?.SampleDurationMs ?? 0,
            DisplayCadenceRecentIntervalsMs = rendererCadence?.RecentIntervalsMs ?? Array.Empty<double>(),
            DisplayCadenceJitterStdDevMs = rendererCadence?.JitterStdDevMs ?? 0,
            DisplayCadenceSlowFrameCount = rendererCadence?.SlowFrameCount ?? 0,
            DisplayCadenceSlowFramePercent = rendererCadence?.SlowFramePercent ?? 0,
            D3DPresentSyncInterval = d3d?.PresentSyncInterval ?? 0,
            D3DMaxFrameLatency = d3d?.DxgiMaxFrameLatency ?? 0,
            D3DSwapChainBufferCount = d3d?.SwapChainBufferCount ?? 0,
            D3DSwapChainAddress = d3d?.SwapChainAddress ?? string.Empty,
            D3DFramesSubmitted = frameCounters.D3DFramesSubmitted,
            D3DFramesRendered = frameCounters.D3DFramesRendered,
            D3DFramesDropped = frameCounters.D3DFramesDropped,
            D3DRenderThreadFailureCount = d3d?.RenderThreadFailureCount ?? 0,
            D3DLastRenderThreadFailureType = d3d?.LastRenderThreadFailureType ?? string.Empty,
            D3DLastRenderThreadFailureMessage = d3d?.LastRenderThreadFailureMessage ?? string.Empty,
            D3DLastRenderThreadFailureHResult = d3d?.LastRenderThreadFailureHResult ?? 0,
            D3DPendingFrameCount = d3d?.PendingFrameCount ?? 0,
            D3DInputColorSpace = d3d?.InputColorSpaceLabel ?? "None",
            D3DOutputColorSpace = d3d?.OutputColorSpaceLabel ?? "None",
            D3DCpuTimingSampleCount = d3dRenderCpuTiming?.TotalFrame.SampleCount ?? 0,
            D3DInputUploadCpuAvgMs = d3dRenderCpuTiming?.InputUpload.AverageMs ?? 0,
            D3DInputUploadCpuP95Ms = d3dRenderCpuTiming?.InputUpload.P95Ms ?? 0,
            D3DInputUploadCpuP99Ms = d3dRenderCpuTiming?.InputUpload.P99Ms ?? 0,
            D3DInputUploadCpuMaxMs = d3dRenderCpuTiming?.InputUpload.MaxMs ?? 0,
            D3DRenderSubmitCpuAvgMs = d3dRenderCpuTiming?.RenderSubmit.AverageMs ?? 0,
            D3DRenderSubmitCpuP95Ms = d3dRenderCpuTiming?.RenderSubmit.P95Ms ?? 0,
            D3DRenderSubmitCpuP99Ms = d3dRenderCpuTiming?.RenderSubmit.P99Ms ?? 0,
            D3DRenderSubmitCpuMaxMs = d3dRenderCpuTiming?.RenderSubmit.MaxMs ?? 0,
            D3DPresentCallAvgMs = d3dRenderCpuTiming?.PresentCall.AverageMs ?? 0,
            D3DPresentCallP95Ms = d3dRenderCpuTiming?.PresentCall.P95Ms ?? 0,
            D3DPresentCallP99Ms = d3dRenderCpuTiming?.PresentCall.P99Ms ?? 0,
            D3DPresentCallMaxMs = d3dRenderCpuTiming?.PresentCall.MaxMs ?? 0,
            D3DTotalFrameCpuAvgMs = d3dRenderCpuTiming?.TotalFrame.AverageMs ?? 0,
            D3DTotalFrameCpuP95Ms = d3dRenderCpuTiming?.TotalFrame.P95Ms ?? 0,
            D3DTotalFrameCpuP99Ms = d3dRenderCpuTiming?.TotalFrame.P99Ms ?? 0,
            D3DTotalFrameCpuMaxMs = d3dRenderCpuTiming?.TotalFrame.MaxMs ?? 0,
            D3DPipelineLatencySampleCount = d3dPipelineLatency?.SampleCount ?? 0,
            D3DPipelineLatencyAvgMs = d3dPipelineLatency?.AverageMs ?? 0,
            D3DPipelineLatencyP95Ms = d3dPipelineLatency?.P95Ms ?? 0,
            D3DPipelineLatencyP99Ms = d3dPipelineLatency?.P99Ms ?? 0,
            D3DPipelineLatencyMaxMs = d3dPipelineLatency?.MaxMs ?? 0,
            D3DFrameLatencyWaitEnabled = d3dFrameLatencyWait?.Enabled ?? false,
            D3DFrameLatencyWaitHandleActive = d3dFrameLatencyWait?.HandleActive ?? false,
            D3DFrameLatencyWaitCallCount = d3dFrameLatencyWait?.CallCount ?? 0,
            D3DFrameLatencyWaitSignaledCount = d3dFrameLatencyWait?.SignaledCount ?? 0,
            D3DFrameLatencyWaitTimeoutCount = d3dFrameLatencyWait?.TimeoutCount ?? 0,
            D3DFrameLatencyWaitUnexpectedResultCount = d3dFrameLatencyWait?.UnexpectedResultCount ?? 0,
            D3DFrameLatencyWaitLastResult = d3dFrameLatencyWait?.LastResult ?? 0,
            D3DFrameLatencyWaitLastMs = d3dFrameLatencyWait?.LastWaitMs ?? 0,
            D3DFrameLatencyWaitSampleCount = d3dFrameLatencyWait?.Timing.SampleCount ?? 0,
            D3DFrameLatencyWaitAvgMs = d3dFrameLatencyWait?.Timing.AverageMs ?? 0,
            D3DFrameLatencyWaitP95Ms = d3dFrameLatencyWait?.Timing.P95Ms ?? 0,
            D3DFrameLatencyWaitP99Ms = d3dFrameLatencyWait?.Timing.P99Ms ?? 0,
            D3DFrameLatencyWaitMaxMs = d3dFrameLatencyWait?.Timing.MaxMs ?? 0,
            D3DFrameStatsSampleCount = frameStatistics.SampleCount,
            D3DFrameStatsSuccessCount = frameStatistics.SuccessCount,
            D3DFrameStatsFailureCount = frameStatistics.FailureCount,
            D3DFrameStatsLastError = frameStatistics.LastError,
            D3DFrameStatsPresentCount = frameStatistics.PresentCount,
            D3DFrameStatsPresentRefreshCount = frameStatistics.PresentRefreshCount,
            D3DFrameStatsSyncRefreshCount = frameStatistics.SyncRefreshCount,
            D3DFrameStatsSyncQpcTime = frameStatistics.SyncQpcTime,
            D3DFrameStatsLastPresentDelta = frameStatistics.LastPresentDelta,
            D3DFrameStatsLastPresentRefreshDelta = frameStatistics.LastPresentRefreshDelta,
            D3DFrameStatsLastSyncRefreshDelta = frameStatistics.LastSyncRefreshDelta,
            D3DFrameStatsMissedRefreshCount = frameStatistics.MissedRefreshCount,
            D3DLastSubmittedPreviewPresentId = frameOwnership.LastSubmittedPreviewPresentId,
            D3DLastSubmittedSourceSequenceNumber = frameOwnership.LastSubmittedSourceSequenceNumber,
            D3DLastSubmittedSourcePtsTicks = frameOwnership.LastSubmittedSourcePtsTicks,
            D3DLastSubmittedQpc = frameOwnership.LastSubmittedQpc,
            D3DLastSubmittedUtcUnixMs = frameOwnership.LastSubmittedUtcUnixMs,
            D3DLastRenderedPreviewPresentId = frameOwnership.LastRenderedPreviewPresentId,
            D3DLastRenderedSourceSequenceNumber = frameOwnership.LastRenderedSourceSequenceNumber,
            D3DLastRenderedSourcePtsTicks = frameOwnership.LastRenderedSourcePtsTicks,
            D3DLastRenderedQpc = frameOwnership.LastRenderedQpc,
            D3DLastRenderedUtcUnixMs = frameOwnership.LastRenderedUtcUnixMs,
            D3DLastRenderedSchedulerToPresentMs = frameOwnership.LastRenderedSchedulerToPresentMs,
            D3DLastRenderedPipelineLatencyMs = frameOwnership.LastRenderedPipelineLatencyMs,
            D3DLastDroppedPreviewPresentId = frameOwnership.LastDroppedPreviewPresentId,
            D3DLastDroppedSourceSequenceNumber = frameOwnership.LastDroppedSourceSequenceNumber,
            D3DLastDroppedSourcePtsTicks = frameOwnership.LastDroppedSourcePtsTicks,
            D3DLastDroppedQpc = frameOwnership.LastDroppedQpc,
            D3DLastDroppedUtcUnixMs = frameOwnership.LastDroppedUtcUnixMs,
            D3DLastDropReason = frameOwnership.LastDropReason,
            D3DRecentSlowFrames = d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),
            EstimatedPipelineLatencyMs = d3dPipelineLatency?.AverageMs ?? 0,
            GpuPlaybackState = d3d == null ? "None" : (d3d.IsRendering ? "Rendering" : "Idle"),
            GpuNaturalVideoWidth = d3d?.NaturalWidth ?? 0,
            GpuNaturalVideoHeight = d3d?.NaturalHeight ?? 0,
            GpuPositionMs = 0
        };
    }
}

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
        var d3dFrameOwnership = d3d?.GetFrameOwnershipMetrics();
        var d3dFrameStats = d3d?.GetDxgiFrameStatisticsMetrics();
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
            D3DFrameStatsSampleCount = d3dFrameStats?.SampleCount ?? 0,
            D3DFrameStatsSuccessCount = d3dFrameStats?.SuccessCount ?? 0,
            D3DFrameStatsFailureCount = d3dFrameStats?.FailureCount ?? 0,
            D3DFrameStatsLastError = d3dFrameStats?.LastError ?? string.Empty,
            D3DFrameStatsPresentCount = d3dFrameStats?.PresentCount ?? -1,
            D3DFrameStatsPresentRefreshCount = d3dFrameStats?.PresentRefreshCount ?? -1,
            D3DFrameStatsSyncRefreshCount = d3dFrameStats?.SyncRefreshCount ?? -1,
            D3DFrameStatsSyncQpcTime = d3dFrameStats?.SyncQpcTime ?? 0,
            D3DFrameStatsLastPresentDelta = d3dFrameStats?.LastPresentDelta ?? 0,
            D3DFrameStatsLastPresentRefreshDelta = d3dFrameStats?.LastPresentRefreshDelta ?? 0,
            D3DFrameStatsLastSyncRefreshDelta = d3dFrameStats?.LastSyncRefreshDelta ?? 0,
            D3DFrameStatsMissedRefreshCount = d3dFrameStats?.MissedRefreshCount ?? 0,
            D3DLastSubmittedPreviewPresentId = d3dFrameOwnership?.LastSubmittedPreviewPresentId ?? 0,
            D3DLastSubmittedSourceSequenceNumber = d3dFrameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,
            D3DLastSubmittedSourcePtsTicks = d3dFrameOwnership?.LastSubmittedSourcePtsTicks ?? 0,
            D3DLastSubmittedQpc = d3dFrameOwnership?.LastSubmittedQpc ?? 0,
            D3DLastSubmittedUtcUnixMs = d3dFrameOwnership?.LastSubmittedUtcUnixMs ?? 0,
            D3DLastRenderedPreviewPresentId = d3dFrameOwnership?.LastRenderedPreviewPresentId ?? 0,
            D3DLastRenderedSourceSequenceNumber = d3dFrameOwnership?.LastRenderedSourceSequenceNumber ?? -1,
            D3DLastRenderedSourcePtsTicks = d3dFrameOwnership?.LastRenderedSourcePtsTicks ?? 0,
            D3DLastRenderedQpc = d3dFrameOwnership?.LastRenderedQpc ?? 0,
            D3DLastRenderedUtcUnixMs = d3dFrameOwnership?.LastRenderedUtcUnixMs ?? 0,
            D3DLastRenderedSchedulerToPresentMs = d3dFrameOwnership?.LastRenderedSchedulerToPresentMs ?? 0,
            D3DLastRenderedPipelineLatencyMs = d3dFrameOwnership?.LastRenderedPipelineLatencyMs ?? 0,
            D3DLastDroppedPreviewPresentId = d3dFrameOwnership?.LastDroppedPreviewPresentId ?? 0,
            D3DLastDroppedSourceSequenceNumber = d3dFrameOwnership?.LastDroppedSourceSequenceNumber ?? -1,
            D3DLastDroppedSourcePtsTicks = d3dFrameOwnership?.LastDroppedSourcePtsTicks ?? 0,
            D3DLastDroppedQpc = d3dFrameOwnership?.LastDroppedQpc ?? 0,
            D3DLastDroppedUtcUnixMs = d3dFrameOwnership?.LastDroppedUtcUnixMs ?? 0,
            D3DLastDropReason = d3dFrameOwnership?.LastDropReason ?? string.Empty,
            D3DRecentSlowFrames = d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),
            EstimatedPipelineLatencyMs = d3dPipelineLatency?.AverageMs ?? 0,
            GpuPlaybackState = d3d == null ? "None" : (d3d.IsRendering ? "Rendering" : "Idle"),
            GpuNaturalVideoWidth = d3d?.NaturalWidth ?? 0,
            GpuNaturalVideoHeight = d3d?.NaturalHeight ?? 0,
            GpuPositionMs = 0
        };
    }
}

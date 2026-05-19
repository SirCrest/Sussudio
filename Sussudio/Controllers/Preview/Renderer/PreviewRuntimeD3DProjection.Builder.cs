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
        var displayCadence = PreviewRuntimeD3DDisplayCadencePolicy.Evaluate(d3d, input.PreviewMinPresentationIntervalMs);
        var renderCpuTiming = PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate(d3d);
        var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);
        var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);
        var frameLatencyWait = PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate(d3d);
        var pipelineLatency = PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate(d3d);

        return new PreviewRuntimeD3DProjection
        {
            GpuActive = frameCounters.GpuActive,
            RendererAttached = frameCounters.RendererAttached,
            FramesArrived = frameCounters.FramesArrived,
            FramesDisplayed = frameCounters.FramesDisplayed,
            FramesDropped = frameCounters.FramesDropped,
            RendererMode = d3d?.RendererMode ?? (input.IsPreviewing ? "CpuSoftwareBitmap" : "None"),
            DisplayCadenceSampleCount = displayCadence.SampleCount,
            DisplayCadenceObservedFps = displayCadence.ObservedFps,
            DisplayCadenceExpectedIntervalMs = displayCadence.ExpectedIntervalMs,
            DisplayCadenceAverageIntervalMs = displayCadence.AverageIntervalMs,
            DisplayCadenceP95IntervalMs = displayCadence.P95IntervalMs,
            DisplayCadenceP99IntervalMs = displayCadence.P99IntervalMs,
            DisplayCadenceMaxIntervalMs = displayCadence.MaxIntervalMs,
            DisplayCadenceOnePercentLowFps = displayCadence.OnePercentLowFps,
            DisplayCadenceFivePercentLowFps = displayCadence.FivePercentLowFps,
            DisplayCadenceSampleDurationMs = displayCadence.SampleDurationMs,
            DisplayCadenceRecentIntervalsMs = displayCadence.RecentIntervalsMs,
            DisplayCadenceJitterStdDevMs = displayCadence.JitterStdDevMs,
            DisplayCadenceSlowFrameCount = displayCadence.SlowFrameCount,
            DisplayCadenceSlowFramePercent = displayCadence.SlowFramePercent,
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
            D3DCpuTimingSampleCount = renderCpuTiming.SampleCount,
            D3DInputUploadCpuAvgMs = renderCpuTiming.InputUploadAverageMs,
            D3DInputUploadCpuP95Ms = renderCpuTiming.InputUploadP95Ms,
            D3DInputUploadCpuP99Ms = renderCpuTiming.InputUploadP99Ms,
            D3DInputUploadCpuMaxMs = renderCpuTiming.InputUploadMaxMs,
            D3DRenderSubmitCpuAvgMs = renderCpuTiming.RenderSubmitAverageMs,
            D3DRenderSubmitCpuP95Ms = renderCpuTiming.RenderSubmitP95Ms,
            D3DRenderSubmitCpuP99Ms = renderCpuTiming.RenderSubmitP99Ms,
            D3DRenderSubmitCpuMaxMs = renderCpuTiming.RenderSubmitMaxMs,
            D3DPresentCallAvgMs = renderCpuTiming.PresentCallAverageMs,
            D3DPresentCallP95Ms = renderCpuTiming.PresentCallP95Ms,
            D3DPresentCallP99Ms = renderCpuTiming.PresentCallP99Ms,
            D3DPresentCallMaxMs = renderCpuTiming.PresentCallMaxMs,
            D3DTotalFrameCpuAvgMs = renderCpuTiming.TotalFrameAverageMs,
            D3DTotalFrameCpuP95Ms = renderCpuTiming.TotalFrameP95Ms,
            D3DTotalFrameCpuP99Ms = renderCpuTiming.TotalFrameP99Ms,
            D3DTotalFrameCpuMaxMs = renderCpuTiming.TotalFrameMaxMs,
            D3DPipelineLatencySampleCount = pipelineLatency.SampleCount,
            D3DPipelineLatencyAvgMs = pipelineLatency.AverageMs,
            D3DPipelineLatencyP95Ms = pipelineLatency.P95Ms,
            D3DPipelineLatencyP99Ms = pipelineLatency.P99Ms,
            D3DPipelineLatencyMaxMs = pipelineLatency.MaxMs,
            D3DFrameLatencyWaitEnabled = frameLatencyWait.Enabled,
            D3DFrameLatencyWaitHandleActive = frameLatencyWait.HandleActive,
            D3DFrameLatencyWaitCallCount = frameLatencyWait.CallCount,
            D3DFrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount,
            D3DFrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,
            D3DFrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount,
            D3DFrameLatencyWaitLastResult = frameLatencyWait.LastResult,
            D3DFrameLatencyWaitLastMs = frameLatencyWait.LastWaitMs,
            D3DFrameLatencyWaitSampleCount = frameLatencyWait.SampleCount,
            D3DFrameLatencyWaitAvgMs = frameLatencyWait.AverageMs,
            D3DFrameLatencyWaitP95Ms = frameLatencyWait.P95Ms,
            D3DFrameLatencyWaitP99Ms = frameLatencyWait.P99Ms,
            D3DFrameLatencyWaitMaxMs = frameLatencyWait.MaxMs,
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
            EstimatedPipelineLatencyMs = pipelineLatency.EstimatedPipelineLatencyMs,
            GpuPlaybackState = d3d == null ? "None" : (d3d.IsRendering ? "Rendering" : "Idle"),
            GpuNaturalVideoWidth = d3d?.NaturalWidth ?? 0,
            GpuNaturalVideoHeight = d3d?.NaturalHeight ?? 0,
            GpuPositionMs = 0
        };
    }
}

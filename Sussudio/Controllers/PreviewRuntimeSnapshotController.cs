using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed class PreviewRuntimeSnapshotInput
{
    public D3D11PreviewRenderer? D3DRenderer { get; init; }
    public bool IsPreviewing { get; init; }
    public bool PreviewSourceAttached { get; init; }
    public bool GpuElementVisible { get; init; }
    public bool CpuElementVisible { get; init; }
    public bool PlaceholderVisible { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long FramesDropped { get; init; }
    public long LastPresentedTick { get; init; }
    public double PreviewMinPresentationIntervalMs { get; init; }
    public string StartupState { get; init; } = "Idle";
    public bool IsStartupWaitingForFirstVisual { get; init; }
    public string? StartupAttemptId { get; init; }
    public DateTimeOffset? StartupRequestedUtc { get; init; }
    public int StartupTimeoutMs { get; init; }
    public bool StartupGpuSignalMediaOpened { get; init; }
    public bool StartupGpuSignalFirstFrame { get; init; }
    public bool StartupGpuSignalPlaybackAdvancing { get; init; }
    public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }
    public PreviewStartupSignalFlags StartupReceivedSignals { get; init; }
    public PreviewStartupStrategy StartupStrategy { get; init; }
    public string? StartupMissingSignals { get; init; }
    public int StartupRecoveryAttemptCount { get; init; }
    public string? StartupLastFailureReason { get; init; }
    public bool FirstVisualConfirmed { get; init; }
    public long GpuPositionEventCount { get; init; }
}

internal static class PreviewRuntimeSnapshotController
{
    public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var nowTick = Environment.TickCount64;
        var framesArrived = input.FramesArrived;
        var framesDisplayed = input.FramesDisplayed;
        var framesDropped = input.FramesDropped;
        var gpuActive = d3d != null;
        var rendererAttached = d3d != null || input.PreviewSourceAttached;
        var previewPipelineActive = input.IsPreviewing && rendererAttached;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;
        var d3dRenderCpuTiming = d3d?.GetRenderCpuTimingMetrics();
        var d3dFrameOwnership = d3d?.GetFrameOwnershipMetrics();
        var d3dFrameStats = d3d?.GetDxgiFrameStatisticsMetrics();
        var d3dFrameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();
        var d3dPipelineLatency = d3d?.GetPipelineLatencyMetrics();
        var d3dSlowFrames = d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>();
        if (gpuActive)
        {
            framesArrived = d3dFramesSubmitted;
            framesDisplayed = d3dFramesRendered;
            framesDropped = d3dFramesDropped;
        }

        var rendererMode = d3d?.RendererMode
            ?? (input.IsPreviewing ? "CpuSoftwareBitmap" : "None");
        var gpuPlaybackState = "None";
        int gpuNaturalVideoWidth = 0, gpuNaturalVideoHeight = 0;
        double gpuPositionMs = 0;
        if (d3d != null)
        {
            gpuPlaybackState = d3d.IsRendering ? "Rendering" : "Idle";
            gpuNaturalVideoWidth = d3d.NaturalWidth;
            gpuNaturalVideoHeight = d3d.NaturalHeight;
            gpuPositionMs = 0;
        }

        var startupElapsedMs = input.StartupRequestedUtc.HasValue
            ? Math.Max(0, (DateTimeOffset.UtcNow - input.StartupRequestedUtc.Value).TotalMilliseconds)
            : (double?)null;
        var startupTimedOut = input.IsPreviewing &&
                              input.IsStartupWaitingForFirstVisual &&
                              startupElapsedMs.GetValueOrDefault() >= input.StartupTimeoutMs;
        var blankSuspected = !gpuActive && previewPipelineActive &&
                             framesArrived > 30 &&
                             framesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }

        var stallSuspected = !gpuActive && previewPipelineActive &&
                             input.LastPresentedTick > 0 &&
                             nowTick - input.LastPresentedTick > 3000;
        var rendererCadence = d3d?.GetPresentCadenceMetrics(input.PreviewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = input.IsPreviewing,
            GpuActive = gpuActive,
            PlaceholderVisible = input.PlaceholderVisible,
            GpuElementVisible = input.GpuElementVisible,
            CpuElementVisible = input.CpuElementVisible,
            RendererAttached = rendererAttached,
            StartupState = input.StartupState,
            StartupAttemptId = input.StartupAttemptId,
            StartupElapsedMs = startupElapsedMs,
            StartupTimeoutMs = input.StartupTimeoutMs,
            StartupGpuSignalMediaOpened = input.StartupGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = input.StartupGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = input.StartupGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = input.StartupRequiredSignals,
            StartupReceivedSignals = input.StartupReceivedSignals,
            StartupStrategy = input.StartupStrategy,
            StartupMissingSignals = input.StartupMissingSignals,
            StartupRecoveryAttemptCount = input.StartupRecoveryAttemptCount,
            StartupLastFailureReason = input.StartupLastFailureReason,
            FirstVisualConfirmed = input.FirstVisualConfirmed,
            FramesArrived = framesArrived,
            FramesDisplayed = framesDisplayed,
            FramesDropped = framesDropped,
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
            BlankSuspected = blankSuspected,
            StallSuspected = stallSuspected,
            RendererMode = rendererMode,
            D3DPresentSyncInterval = d3d?.PresentSyncInterval ?? 0,
            D3DMaxFrameLatency = d3d?.DxgiMaxFrameLatency ?? 0,
            D3DSwapChainBufferCount = d3d?.SwapChainBufferCount ?? 0,
            D3DSwapChainAddress = d3d?.SwapChainAddress ?? string.Empty,
            D3DFramesSubmitted = d3dFramesSubmitted,
            D3DFramesRendered = d3dFramesRendered,
            D3DFramesDropped = d3dFramesDropped,
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
            D3DRecentSlowFrames = d3dSlowFrames,
            EstimatedPipelineLatencyMs = d3dPipelineLatency?.AverageMs ?? 0,
            GpuPlaybackState = gpuPlaybackState,
            GpuNaturalVideoWidth = gpuNaturalVideoWidth,
            GpuNaturalVideoHeight = gpuNaturalVideoHeight,
            GpuPositionMs = gpuPositionMs,
            GpuPositionEventCount = input.GpuPositionEventCount
        };
    }
}

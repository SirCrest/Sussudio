using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed class PreviewRuntimeD3DProjection
{
    public bool GpuActive { get; private set; }
    public bool RendererAttached { get; private set; }
    public long FramesArrived { get; private set; }
    public long FramesDisplayed { get; private set; }
    public long FramesDropped { get; private set; }
    public long D3DFramesSubmitted { get; private set; }
    public long D3DFramesRendered { get; private set; }
    public long D3DFramesDropped { get; private set; }
    public string RendererMode { get; private set; } = "None";
    public int D3DPresentSyncInterval { get; private set; }
    public int D3DMaxFrameLatency { get; private set; }
    public int D3DSwapChainBufferCount { get; private set; }
    public string D3DSwapChainAddress { get; private set; } = string.Empty;
    public long D3DRenderThreadFailureCount { get; private set; }
    public string D3DLastRenderThreadFailureType { get; private set; } = string.Empty;
    public string D3DLastRenderThreadFailureMessage { get; private set; } = string.Empty;
    public int D3DLastRenderThreadFailureHResult { get; private set; }
    public int D3DPendingFrameCount { get; private set; }
    public string D3DInputColorSpace { get; private set; } = "None";
    public string D3DOutputColorSpace { get; private set; } = "None";
    public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; private set; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public string GpuPlaybackState { get; private set; } = "None";
    public int GpuNaturalVideoWidth { get; private set; }
    public int GpuNaturalVideoHeight { get; private set; }
    public double GpuPositionMs { get; private set; }
    public int DisplayCadenceSampleCount { get; private set; }
    public double DisplayCadenceObservedFps { get; private set; }
    public double DisplayCadenceExpectedIntervalMs { get; private set; }
    public double DisplayCadenceAverageIntervalMs { get; private set; }
    public double DisplayCadenceP95IntervalMs { get; private set; }
    public double DisplayCadenceP99IntervalMs { get; private set; }
    public double DisplayCadenceMaxIntervalMs { get; private set; }
    public double DisplayCadenceOnePercentLowFps { get; private set; }
    public double DisplayCadenceFivePercentLowFps { get; private set; }
    public double DisplayCadenceSampleDurationMs { get; private set; }
    public double[] DisplayCadenceRecentIntervalsMs { get; private set; } = Array.Empty<double>();
    public double DisplayCadenceJitterStdDevMs { get; private set; }
    public long DisplayCadenceSlowFrameCount { get; private set; }
    public double DisplayCadenceSlowFramePercent { get; private set; }
    public int D3DCpuTimingSampleCount { get; private set; }
    public double D3DInputUploadCpuAvgMs { get; private set; }
    public double D3DInputUploadCpuP95Ms { get; private set; }
    public double D3DInputUploadCpuP99Ms { get; private set; }
    public double D3DInputUploadCpuMaxMs { get; private set; }
    public double D3DRenderSubmitCpuAvgMs { get; private set; }
    public double D3DRenderSubmitCpuP95Ms { get; private set; }
    public double D3DRenderSubmitCpuP99Ms { get; private set; }
    public double D3DRenderSubmitCpuMaxMs { get; private set; }
    public double D3DPresentCallAvgMs { get; private set; }
    public double D3DPresentCallP95Ms { get; private set; }
    public double D3DPresentCallP99Ms { get; private set; }
    public double D3DPresentCallMaxMs { get; private set; }
    public double D3DTotalFrameCpuAvgMs { get; private set; }
    public double D3DTotalFrameCpuP95Ms { get; private set; }
    public double D3DTotalFrameCpuP99Ms { get; private set; }
    public double D3DTotalFrameCpuMaxMs { get; private set; }
    public int D3DPipelineLatencySampleCount { get; private set; }
    public double D3DPipelineLatencyAvgMs { get; private set; }
    public double D3DPipelineLatencyP95Ms { get; private set; }
    public double D3DPipelineLatencyP99Ms { get; private set; }
    public double D3DPipelineLatencyMaxMs { get; private set; }
    public double EstimatedPipelineLatencyMs { get; private set; }
    public long D3DLastSubmittedPreviewPresentId { get; private set; }
    public long D3DLastSubmittedSourceSequenceNumber { get; private set; }
    public long D3DLastSubmittedSourcePtsTicks { get; private set; }
    public long D3DLastSubmittedQpc { get; private set; }
    public long D3DLastSubmittedUtcUnixMs { get; private set; }
    public long D3DLastRenderedPreviewPresentId { get; private set; }
    public long D3DLastRenderedSourceSequenceNumber { get; private set; }
    public long D3DLastRenderedSourcePtsTicks { get; private set; }
    public long D3DLastRenderedQpc { get; private set; }
    public long D3DLastRenderedUtcUnixMs { get; private set; }
    public double D3DLastRenderedSchedulerToPresentMs { get; private set; }
    public double D3DLastRenderedPipelineLatencyMs { get; private set; }
    public long D3DLastDroppedPreviewPresentId { get; private set; }
    public long D3DLastDroppedSourceSequenceNumber { get; private set; }
    public long D3DLastDroppedSourcePtsTicks { get; private set; }
    public long D3DLastDroppedQpc { get; private set; }
    public long D3DLastDroppedUtcUnixMs { get; private set; }
    public string D3DLastDropReason { get; private set; } = string.Empty;
    public long D3DFrameStatsSampleCount { get; private set; }
    public long D3DFrameStatsSuccessCount { get; private set; }
    public long D3DFrameStatsFailureCount { get; private set; }
    public string D3DFrameStatsLastError { get; private set; } = string.Empty;
    public long D3DFrameStatsPresentCount { get; private set; }
    public long D3DFrameStatsPresentRefreshCount { get; private set; }
    public long D3DFrameStatsSyncRefreshCount { get; private set; }
    public long D3DFrameStatsSyncQpcTime { get; private set; }
    public long D3DFrameStatsLastPresentDelta { get; private set; }
    public long D3DFrameStatsLastPresentRefreshDelta { get; private set; }
    public long D3DFrameStatsLastSyncRefreshDelta { get; private set; }
    public long D3DFrameStatsMissedRefreshCount { get; private set; }
    public bool D3DFrameLatencyWaitEnabled { get; private set; }
    public bool D3DFrameLatencyWaitHandleActive { get; private set; }
    public long D3DFrameLatencyWaitCallCount { get; private set; }
    public long D3DFrameLatencyWaitSignaledCount { get; private set; }
    public long D3DFrameLatencyWaitTimeoutCount { get; private set; }
    public long D3DFrameLatencyWaitUnexpectedResultCount { get; private set; }
    public uint D3DFrameLatencyWaitLastResult { get; private set; }
    public double D3DFrameLatencyWaitLastMs { get; private set; }
    public int D3DFrameLatencyWaitSampleCount { get; private set; }
    public double D3DFrameLatencyWaitAvgMs { get; private set; }
    public double D3DFrameLatencyWaitP95Ms { get; private set; }
    public double D3DFrameLatencyWaitP99Ms { get; private set; }
    public double D3DFrameLatencyWaitMaxMs { get; private set; }

    public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);
        var rendererState = PreviewRuntimeD3DRendererStatePolicy.Evaluate(d3d, input.IsPreviewing);
        var displayCadence = PreviewRuntimeD3DDisplayCadencePolicy.Evaluate(d3d, input.PreviewMinPresentationIntervalMs);
        var renderCpuTiming = PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate(d3d);
        var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);
        var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);
        var frameLatencyWait = PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate(d3d);
        var pipelineLatency = PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate(d3d);

        var projection = new PreviewRuntimeD3DProjection();
        projection.ApplyFrameCounters(frameCounters);
        projection.ApplyRendererState(rendererState);
        projection.ApplyDisplayCadence(displayCadence);
        projection.ApplyRenderCpuTiming(renderCpuTiming);
        projection.ApplyPipelineLatency(pipelineLatency);
        projection.ApplyFrameLatencyWait(frameLatencyWait);
        projection.ApplyFrameStatistics(frameStatistics);
        projection.ApplyFrameOwnership(frameOwnership);
        return projection;
    }

    private void ApplyFrameCounters(PreviewRuntimeD3DFrameCounters frameCounters)
    {
        GpuActive = frameCounters.GpuActive;
        RendererAttached = frameCounters.RendererAttached;
        FramesArrived = frameCounters.FramesArrived;
        FramesDisplayed = frameCounters.FramesDisplayed;
        FramesDropped = frameCounters.FramesDropped;
        D3DFramesSubmitted = frameCounters.D3DFramesSubmitted;
        D3DFramesRendered = frameCounters.D3DFramesRendered;
        D3DFramesDropped = frameCounters.D3DFramesDropped;
    }

    private void ApplyRendererState(PreviewRuntimeD3DRendererState rendererState)
    {
        RendererMode = rendererState.RendererMode;
        D3DPresentSyncInterval = rendererState.PresentSyncInterval;
        D3DMaxFrameLatency = rendererState.MaxFrameLatency;
        D3DSwapChainBufferCount = rendererState.SwapChainBufferCount;
        D3DSwapChainAddress = rendererState.SwapChainAddress;
        D3DRenderThreadFailureCount = rendererState.RenderThreadFailureCount;
        D3DLastRenderThreadFailureType = rendererState.LastRenderThreadFailureType;
        D3DLastRenderThreadFailureMessage = rendererState.LastRenderThreadFailureMessage;
        D3DLastRenderThreadFailureHResult = rendererState.LastRenderThreadFailureHResult;
        D3DPendingFrameCount = rendererState.PendingFrameCount;
        D3DInputColorSpace = rendererState.InputColorSpace;
        D3DOutputColorSpace = rendererState.OutputColorSpace;
        D3DRecentSlowFrames = rendererState.RecentSlowFrames;
        GpuPlaybackState = rendererState.GpuPlaybackState;
        GpuNaturalVideoWidth = rendererState.NaturalVideoWidth;
        GpuNaturalVideoHeight = rendererState.NaturalVideoHeight;
        GpuPositionMs = rendererState.PositionMs;
    }

    private void ApplyDisplayCadence(PreviewRuntimeD3DDisplayCadence displayCadence)
    {
        DisplayCadenceSampleCount = displayCadence.SampleCount;
        DisplayCadenceObservedFps = displayCadence.ObservedFps;
        DisplayCadenceExpectedIntervalMs = displayCadence.ExpectedIntervalMs;
        DisplayCadenceAverageIntervalMs = displayCadence.AverageIntervalMs;
        DisplayCadenceP95IntervalMs = displayCadence.P95IntervalMs;
        DisplayCadenceP99IntervalMs = displayCadence.P99IntervalMs;
        DisplayCadenceMaxIntervalMs = displayCadence.MaxIntervalMs;
        DisplayCadenceOnePercentLowFps = displayCadence.OnePercentLowFps;
        DisplayCadenceFivePercentLowFps = displayCadence.FivePercentLowFps;
        DisplayCadenceSampleDurationMs = displayCadence.SampleDurationMs;
        DisplayCadenceRecentIntervalsMs = displayCadence.RecentIntervalsMs;
        DisplayCadenceJitterStdDevMs = displayCadence.JitterStdDevMs;
        DisplayCadenceSlowFrameCount = displayCadence.SlowFrameCount;
        DisplayCadenceSlowFramePercent = displayCadence.SlowFramePercent;
    }

    private void ApplyRenderCpuTiming(PreviewRuntimeD3DRenderCpuTiming renderCpuTiming)
    {
        D3DCpuTimingSampleCount = renderCpuTiming.SampleCount;
        D3DInputUploadCpuAvgMs = renderCpuTiming.InputUploadAverageMs;
        D3DInputUploadCpuP95Ms = renderCpuTiming.InputUploadP95Ms;
        D3DInputUploadCpuP99Ms = renderCpuTiming.InputUploadP99Ms;
        D3DInputUploadCpuMaxMs = renderCpuTiming.InputUploadMaxMs;
        D3DRenderSubmitCpuAvgMs = renderCpuTiming.RenderSubmitAverageMs;
        D3DRenderSubmitCpuP95Ms = renderCpuTiming.RenderSubmitP95Ms;
        D3DRenderSubmitCpuP99Ms = renderCpuTiming.RenderSubmitP99Ms;
        D3DRenderSubmitCpuMaxMs = renderCpuTiming.RenderSubmitMaxMs;
        D3DPresentCallAvgMs = renderCpuTiming.PresentCallAverageMs;
        D3DPresentCallP95Ms = renderCpuTiming.PresentCallP95Ms;
        D3DPresentCallP99Ms = renderCpuTiming.PresentCallP99Ms;
        D3DPresentCallMaxMs = renderCpuTiming.PresentCallMaxMs;
        D3DTotalFrameCpuAvgMs = renderCpuTiming.TotalFrameAverageMs;
        D3DTotalFrameCpuP95Ms = renderCpuTiming.TotalFrameP95Ms;
        D3DTotalFrameCpuP99Ms = renderCpuTiming.TotalFrameP99Ms;
        D3DTotalFrameCpuMaxMs = renderCpuTiming.TotalFrameMaxMs;
    }

    private void ApplyPipelineLatency(PreviewRuntimeD3DPipelineLatency pipelineLatency)
    {
        D3DPipelineLatencySampleCount = pipelineLatency.SampleCount;
        D3DPipelineLatencyAvgMs = pipelineLatency.AverageMs;
        D3DPipelineLatencyP95Ms = pipelineLatency.P95Ms;
        D3DPipelineLatencyP99Ms = pipelineLatency.P99Ms;
        D3DPipelineLatencyMaxMs = pipelineLatency.MaxMs;
        EstimatedPipelineLatencyMs = pipelineLatency.EstimatedPipelineLatencyMs;
    }

    private void ApplyFrameOwnership(PreviewRuntimeD3DFrameOwnership frameOwnership)
    {
        D3DLastSubmittedPreviewPresentId = frameOwnership.LastSubmittedPreviewPresentId;
        D3DLastSubmittedSourceSequenceNumber = frameOwnership.LastSubmittedSourceSequenceNumber;
        D3DLastSubmittedSourcePtsTicks = frameOwnership.LastSubmittedSourcePtsTicks;
        D3DLastSubmittedQpc = frameOwnership.LastSubmittedQpc;
        D3DLastSubmittedUtcUnixMs = frameOwnership.LastSubmittedUtcUnixMs;
        D3DLastRenderedPreviewPresentId = frameOwnership.LastRenderedPreviewPresentId;
        D3DLastRenderedSourceSequenceNumber = frameOwnership.LastRenderedSourceSequenceNumber;
        D3DLastRenderedSourcePtsTicks = frameOwnership.LastRenderedSourcePtsTicks;
        D3DLastRenderedQpc = frameOwnership.LastRenderedQpc;
        D3DLastRenderedUtcUnixMs = frameOwnership.LastRenderedUtcUnixMs;
        D3DLastRenderedSchedulerToPresentMs = frameOwnership.LastRenderedSchedulerToPresentMs;
        D3DLastRenderedPipelineLatencyMs = frameOwnership.LastRenderedPipelineLatencyMs;
        D3DLastDroppedPreviewPresentId = frameOwnership.LastDroppedPreviewPresentId;
        D3DLastDroppedSourceSequenceNumber = frameOwnership.LastDroppedSourceSequenceNumber;
        D3DLastDroppedSourcePtsTicks = frameOwnership.LastDroppedSourcePtsTicks;
        D3DLastDroppedQpc = frameOwnership.LastDroppedQpc;
        D3DLastDroppedUtcUnixMs = frameOwnership.LastDroppedUtcUnixMs;
        D3DLastDropReason = frameOwnership.LastDropReason;
    }

    private void ApplyFrameStatistics(PreviewRuntimeD3DFrameStatistics frameStatistics)
    {
        D3DFrameStatsSampleCount = frameStatistics.SampleCount;
        D3DFrameStatsSuccessCount = frameStatistics.SuccessCount;
        D3DFrameStatsFailureCount = frameStatistics.FailureCount;
        D3DFrameStatsLastError = frameStatistics.LastError;
        D3DFrameStatsPresentCount = frameStatistics.PresentCount;
        D3DFrameStatsPresentRefreshCount = frameStatistics.PresentRefreshCount;
        D3DFrameStatsSyncRefreshCount = frameStatistics.SyncRefreshCount;
        D3DFrameStatsSyncQpcTime = frameStatistics.SyncQpcTime;
        D3DFrameStatsLastPresentDelta = frameStatistics.LastPresentDelta;
        D3DFrameStatsLastPresentRefreshDelta = frameStatistics.LastPresentRefreshDelta;
        D3DFrameStatsLastSyncRefreshDelta = frameStatistics.LastSyncRefreshDelta;
        D3DFrameStatsMissedRefreshCount = frameStatistics.MissedRefreshCount;
    }

    private void ApplyFrameLatencyWait(PreviewRuntimeD3DFrameLatencyWait frameLatencyWait)
    {
        D3DFrameLatencyWaitEnabled = frameLatencyWait.Enabled;
        D3DFrameLatencyWaitHandleActive = frameLatencyWait.HandleActive;
        D3DFrameLatencyWaitCallCount = frameLatencyWait.CallCount;
        D3DFrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount;
        D3DFrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount;
        D3DFrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount;
        D3DFrameLatencyWaitLastResult = frameLatencyWait.LastResult;
        D3DFrameLatencyWaitLastMs = frameLatencyWait.LastWaitMs;
        D3DFrameLatencyWaitSampleCount = frameLatencyWait.SampleCount;
        D3DFrameLatencyWaitAvgMs = frameLatencyWait.AverageMs;
        D3DFrameLatencyWaitP95Ms = frameLatencyWait.P95Ms;
        D3DFrameLatencyWaitP99Ms = frameLatencyWait.P99Ms;
        D3DFrameLatencyWaitMaxMs = frameLatencyWait.MaxMs;
    }
}

internal readonly record struct PreviewRuntimeD3DFrameCounters(
    bool GpuActive,
    bool RendererAttached,
    long FramesArrived,
    long FramesDisplayed,
    long FramesDropped,
    long D3DFramesSubmitted,
    long D3DFramesRendered,
    long D3DFramesDropped);

internal static class PreviewRuntimeD3DFrameCounterPolicy
{
    public static PreviewRuntimeD3DFrameCounters Evaluate(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var gpuActive = d3d != null;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;

        return new PreviewRuntimeD3DFrameCounters(
            GpuActive: gpuActive,
            RendererAttached: d3d != null || input.PreviewSourceAttached,
            FramesArrived: gpuActive ? d3dFramesSubmitted : input.FramesArrived,
            FramesDisplayed: gpuActive ? d3dFramesRendered : input.FramesDisplayed,
            FramesDropped: gpuActive ? d3dFramesDropped : input.FramesDropped,
            D3DFramesSubmitted: d3dFramesSubmitted,
            D3DFramesRendered: d3dFramesRendered,
            D3DFramesDropped: d3dFramesDropped);
    }
}

internal readonly record struct PreviewRuntimeD3DRendererState(
    string RendererMode,
    int PresentSyncInterval,
    int MaxFrameLatency,
    int SwapChainBufferCount,
    string SwapChainAddress,
    long RenderThreadFailureCount,
    string LastRenderThreadFailureType,
    string LastRenderThreadFailureMessage,
    int LastRenderThreadFailureHResult,
    int PendingFrameCount,
    string InputColorSpace,
    string OutputColorSpace,
    PreviewSlowFrameDiagnostic[] RecentSlowFrames,
    string GpuPlaybackState,
    int NaturalVideoWidth,
    int NaturalVideoHeight,
    double PositionMs);

internal static class PreviewRuntimeD3DRendererStatePolicy
{
    public static PreviewRuntimeD3DRendererState Evaluate(D3D11PreviewRenderer? d3d, bool isPreviewing)
        => new(
            RendererMode: d3d?.RendererMode ?? (isPreviewing ? "CpuSoftwareBitmap" : "None"),
            PresentSyncInterval: d3d?.PresentSyncInterval ?? 0,
            MaxFrameLatency: d3d?.DxgiMaxFrameLatency ?? 0,
            SwapChainBufferCount: d3d?.SwapChainBufferCount ?? 0,
            SwapChainAddress: d3d?.SwapChainAddress ?? string.Empty,
            RenderThreadFailureCount: d3d?.RenderThreadFailureCount ?? 0,
            LastRenderThreadFailureType: d3d?.LastRenderThreadFailureType ?? string.Empty,
            LastRenderThreadFailureMessage: d3d?.LastRenderThreadFailureMessage ?? string.Empty,
            LastRenderThreadFailureHResult: d3d?.LastRenderThreadFailureHResult ?? 0,
            PendingFrameCount: d3d?.PendingFrameCount ?? 0,
            InputColorSpace: d3d?.InputColorSpaceLabel ?? "None",
            OutputColorSpace: d3d?.OutputColorSpaceLabel ?? "None",
            RecentSlowFrames: d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),
            GpuPlaybackState: d3d == null ? "None" : (d3d.IsRendering ? "Rendering" : "Idle"),
            NaturalVideoWidth: d3d?.NaturalWidth ?? 0,
            NaturalVideoHeight: d3d?.NaturalHeight ?? 0,
            PositionMs: 0);
}

internal readonly record struct PreviewRuntimeD3DDisplayCadence(
    int SampleCount,
    double ObservedFps,
    double ExpectedIntervalMs,
    double AverageIntervalMs,
    double P95IntervalMs,
    double P99IntervalMs,
    double MaxIntervalMs,
    double OnePercentLowFps,
    double FivePercentLowFps,
    double SampleDurationMs,
    double[] RecentIntervalsMs,
    double JitterStdDevMs,
    long SlowFrameCount,
    double SlowFramePercent);

internal static class PreviewRuntimeD3DDisplayCadencePolicy
{
    public static PreviewRuntimeD3DDisplayCadence Evaluate(
        D3D11PreviewRenderer? d3d,
        double previewMinPresentationIntervalMs)
    {
        var displayCadence = d3d?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);

        return new PreviewRuntimeD3DDisplayCadence(
            SampleCount: displayCadence?.SampleCount ?? 0,
            ObservedFps: displayCadence?.ObservedFps ?? 0,
            ExpectedIntervalMs: displayCadence?.ExpectedIntervalMs ?? 0,
            AverageIntervalMs: displayCadence?.AverageIntervalMs ?? 0,
            P95IntervalMs: displayCadence?.P95IntervalMs ?? 0,
            P99IntervalMs: displayCadence?.P99IntervalMs ?? 0,
            MaxIntervalMs: displayCadence?.MaxIntervalMs ?? 0,
            OnePercentLowFps: displayCadence?.OnePercentLowFps ?? 0,
            FivePercentLowFps: displayCadence?.FivePercentLowFps ?? 0,
            SampleDurationMs: displayCadence?.SampleDurationMs ?? 0,
            RecentIntervalsMs: displayCadence?.RecentIntervalsMs ?? Array.Empty<double>(),
            JitterStdDevMs: displayCadence?.JitterStdDevMs ?? 0,
            SlowFrameCount: displayCadence?.SlowFrameCount ?? 0,
            SlowFramePercent: displayCadence?.SlowFramePercent ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DRenderCpuTiming(
    int SampleCount,
    double InputUploadAverageMs,
    double InputUploadP95Ms,
    double InputUploadP99Ms,
    double InputUploadMaxMs,
    double RenderSubmitAverageMs,
    double RenderSubmitP95Ms,
    double RenderSubmitP99Ms,
    double RenderSubmitMaxMs,
    double PresentCallAverageMs,
    double PresentCallP95Ms,
    double PresentCallP99Ms,
    double PresentCallMaxMs,
    double TotalFrameAverageMs,
    double TotalFrameP95Ms,
    double TotalFrameP99Ms,
    double TotalFrameMaxMs);

internal static class PreviewRuntimeD3DRenderCpuTimingPolicy
{
    public static PreviewRuntimeD3DRenderCpuTiming Evaluate(D3D11PreviewRenderer? d3d)
    {
        var renderCpuTiming = d3d?.GetRenderCpuTimingMetrics();

        return new PreviewRuntimeD3DRenderCpuTiming(
            SampleCount: renderCpuTiming?.TotalFrame.SampleCount ?? 0,
            InputUploadAverageMs: renderCpuTiming?.InputUpload.AverageMs ?? 0,
            InputUploadP95Ms: renderCpuTiming?.InputUpload.P95Ms ?? 0,
            InputUploadP99Ms: renderCpuTiming?.InputUpload.P99Ms ?? 0,
            InputUploadMaxMs: renderCpuTiming?.InputUpload.MaxMs ?? 0,
            RenderSubmitAverageMs: renderCpuTiming?.RenderSubmit.AverageMs ?? 0,
            RenderSubmitP95Ms: renderCpuTiming?.RenderSubmit.P95Ms ?? 0,
            RenderSubmitP99Ms: renderCpuTiming?.RenderSubmit.P99Ms ?? 0,
            RenderSubmitMaxMs: renderCpuTiming?.RenderSubmit.MaxMs ?? 0,
            PresentCallAverageMs: renderCpuTiming?.PresentCall.AverageMs ?? 0,
            PresentCallP95Ms: renderCpuTiming?.PresentCall.P95Ms ?? 0,
            PresentCallP99Ms: renderCpuTiming?.PresentCall.P99Ms ?? 0,
            PresentCallMaxMs: renderCpuTiming?.PresentCall.MaxMs ?? 0,
            TotalFrameAverageMs: renderCpuTiming?.TotalFrame.AverageMs ?? 0,
            TotalFrameP95Ms: renderCpuTiming?.TotalFrame.P95Ms ?? 0,
            TotalFrameP99Ms: renderCpuTiming?.TotalFrame.P99Ms ?? 0,
            TotalFrameMaxMs: renderCpuTiming?.TotalFrame.MaxMs ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DPipelineLatency(
    int SampleCount,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    double EstimatedPipelineLatencyMs);

internal static class PreviewRuntimeD3DPipelineLatencyPolicy
{
    public static PreviewRuntimeD3DPipelineLatency Evaluate(D3D11PreviewRenderer? d3d)
    {
        var pipelineLatency = d3d?.GetPipelineLatencyMetrics();

        return new PreviewRuntimeD3DPipelineLatency(
            SampleCount: pipelineLatency?.SampleCount ?? 0,
            AverageMs: pipelineLatency?.AverageMs ?? 0,
            P95Ms: pipelineLatency?.P95Ms ?? 0,
            P99Ms: pipelineLatency?.P99Ms ?? 0,
            MaxMs: pipelineLatency?.MaxMs ?? 0,
            EstimatedPipelineLatencyMs: pipelineLatency?.AverageMs ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DFrameOwnership(
    long LastSubmittedPreviewPresentId,
    long LastSubmittedSourceSequenceNumber,
    long LastSubmittedSourcePtsTicks,
    long LastSubmittedQpc,
    long LastSubmittedUtcUnixMs,
    long LastRenderedPreviewPresentId,
    long LastRenderedSourceSequenceNumber,
    long LastRenderedSourcePtsTicks,
    long LastRenderedQpc,
    long LastRenderedUtcUnixMs,
    double LastRenderedSchedulerToPresentMs,
    double LastRenderedPipelineLatencyMs,
    long LastDroppedPreviewPresentId,
    long LastDroppedSourceSequenceNumber,
    long LastDroppedSourcePtsTicks,
    long LastDroppedQpc,
    long LastDroppedUtcUnixMs,
    string LastDropReason);

internal static class PreviewRuntimeD3DFrameOwnershipPolicy
{
    public static PreviewRuntimeD3DFrameOwnership Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameOwnership = d3d?.GetFrameOwnershipMetrics();

        return new PreviewRuntimeD3DFrameOwnership(
            LastSubmittedPreviewPresentId: frameOwnership?.LastSubmittedPreviewPresentId ?? 0,
            LastSubmittedSourceSequenceNumber: frameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,
            LastSubmittedSourcePtsTicks: frameOwnership?.LastSubmittedSourcePtsTicks ?? 0,
            LastSubmittedQpc: frameOwnership?.LastSubmittedQpc ?? 0,
            LastSubmittedUtcUnixMs: frameOwnership?.LastSubmittedUtcUnixMs ?? 0,
            LastRenderedPreviewPresentId: frameOwnership?.LastRenderedPreviewPresentId ?? 0,
            LastRenderedSourceSequenceNumber: frameOwnership?.LastRenderedSourceSequenceNumber ?? -1,
            LastRenderedSourcePtsTicks: frameOwnership?.LastRenderedSourcePtsTicks ?? 0,
            LastRenderedQpc: frameOwnership?.LastRenderedQpc ?? 0,
            LastRenderedUtcUnixMs: frameOwnership?.LastRenderedUtcUnixMs ?? 0,
            LastRenderedSchedulerToPresentMs: frameOwnership?.LastRenderedSchedulerToPresentMs ?? 0,
            LastRenderedPipelineLatencyMs: frameOwnership?.LastRenderedPipelineLatencyMs ?? 0,
            LastDroppedPreviewPresentId: frameOwnership?.LastDroppedPreviewPresentId ?? 0,
            LastDroppedSourceSequenceNumber: frameOwnership?.LastDroppedSourceSequenceNumber ?? -1,
            LastDroppedSourcePtsTicks: frameOwnership?.LastDroppedSourcePtsTicks ?? 0,
            LastDroppedQpc: frameOwnership?.LastDroppedQpc ?? 0,
            LastDroppedUtcUnixMs: frameOwnership?.LastDroppedUtcUnixMs ?? 0,
            LastDropReason: frameOwnership?.LastDropReason ?? string.Empty);
    }
}

internal readonly record struct PreviewRuntimeD3DFrameStatistics(
    long SampleCount,
    long SuccessCount,
    long FailureCount,
    string LastError,
    long PresentCount,
    long PresentRefreshCount,
    long SyncRefreshCount,
    long SyncQpcTime,
    long LastPresentDelta,
    long LastPresentRefreshDelta,
    long LastSyncRefreshDelta,
    long MissedRefreshCount);

internal static class PreviewRuntimeD3DFrameStatisticsPolicy
{
    public static PreviewRuntimeD3DFrameStatistics Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameStats = d3d?.GetDxgiFrameStatisticsMetrics();

        return new PreviewRuntimeD3DFrameStatistics(
            SampleCount: frameStats?.SampleCount ?? 0,
            SuccessCount: frameStats?.SuccessCount ?? 0,
            FailureCount: frameStats?.FailureCount ?? 0,
            LastError: frameStats?.LastError ?? string.Empty,
            PresentCount: frameStats?.PresentCount ?? -1,
            PresentRefreshCount: frameStats?.PresentRefreshCount ?? -1,
            SyncRefreshCount: frameStats?.SyncRefreshCount ?? -1,
            SyncQpcTime: frameStats?.SyncQpcTime ?? 0,
            LastPresentDelta: frameStats?.LastPresentDelta ?? 0,
            LastPresentRefreshDelta: frameStats?.LastPresentRefreshDelta ?? 0,
            LastSyncRefreshDelta: frameStats?.LastSyncRefreshDelta ?? 0,
            MissedRefreshCount: frameStats?.MissedRefreshCount ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DFrameLatencyWait(
    bool Enabled,
    bool HandleActive,
    long CallCount,
    long SignaledCount,
    long TimeoutCount,
    long UnexpectedResultCount,
    uint LastResult,
    double LastWaitMs,
    int SampleCount,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    double MaxMs);

internal static class PreviewRuntimeD3DFrameLatencyWaitPolicy
{
    public static PreviewRuntimeD3DFrameLatencyWait Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();

        return new PreviewRuntimeD3DFrameLatencyWait(
            Enabled: frameLatencyWait?.Enabled ?? false,
            HandleActive: frameLatencyWait?.HandleActive ?? false,
            CallCount: frameLatencyWait?.CallCount ?? 0,
            SignaledCount: frameLatencyWait?.SignaledCount ?? 0,
            TimeoutCount: frameLatencyWait?.TimeoutCount ?? 0,
            UnexpectedResultCount: frameLatencyWait?.UnexpectedResultCount ?? 0,
            LastResult: frameLatencyWait?.LastResult ?? 0,
            LastWaitMs: frameLatencyWait?.LastWaitMs ?? 0,
            SampleCount: frameLatencyWait?.Timing.SampleCount ?? 0,
            AverageMs: frameLatencyWait?.Timing.AverageMs ?? 0,
            P95Ms: frameLatencyWait?.Timing.P95Ms ?? 0,
            P99Ms: frameLatencyWait?.Timing.P99Ms ?? 0,
            MaxMs: frameLatencyWait?.Timing.MaxMs ?? 0);
    }
}

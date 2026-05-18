using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public bool GpuActive { get; init; }
    public bool RendererAttached { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long FramesDropped { get; init; }
    public string RendererMode { get; init; } = "None";
    public int DisplayCadenceSampleCount { get; init; }
    public double DisplayCadenceObservedFps { get; init; }
    public double DisplayCadenceExpectedIntervalMs { get; init; }
    public double DisplayCadenceAverageIntervalMs { get; init; }
    public double DisplayCadenceP95IntervalMs { get; init; }
    public double DisplayCadenceP99IntervalMs { get; init; }
    public double DisplayCadenceMaxIntervalMs { get; init; }
    public double DisplayCadenceOnePercentLowFps { get; init; }
    public double DisplayCadenceFivePercentLowFps { get; init; }
    public double DisplayCadenceSampleDurationMs { get; init; }
    public double[] DisplayCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double DisplayCadenceJitterStdDevMs { get; init; }
    public long DisplayCadenceSlowFrameCount { get; init; }
    public double DisplayCadenceSlowFramePercent { get; init; }
    public int D3DPresentSyncInterval { get; init; }
    public int D3DMaxFrameLatency { get; init; }
    public int D3DSwapChainBufferCount { get; init; }
    public string D3DSwapChainAddress { get; init; } = string.Empty;
    public long D3DFramesSubmitted { get; init; }
    public long D3DFramesRendered { get; init; }
    public long D3DFramesDropped { get; init; }
    public long D3DRenderThreadFailureCount { get; init; }
    public string D3DLastRenderThreadFailureType { get; init; } = string.Empty;
    public string D3DLastRenderThreadFailureMessage { get; init; } = string.Empty;
    public int D3DLastRenderThreadFailureHResult { get; init; }
    public int D3DPendingFrameCount { get; init; }
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
    public int D3DCpuTimingSampleCount { get; init; }
    public double D3DInputUploadCpuAvgMs { get; init; }
    public double D3DInputUploadCpuP95Ms { get; init; }
    public double D3DInputUploadCpuP99Ms { get; init; }
    public double D3DInputUploadCpuMaxMs { get; init; }
    public double D3DRenderSubmitCpuAvgMs { get; init; }
    public double D3DRenderSubmitCpuP95Ms { get; init; }
    public double D3DRenderSubmitCpuP99Ms { get; init; }
    public double D3DRenderSubmitCpuMaxMs { get; init; }
    public double D3DPresentCallAvgMs { get; init; }
    public double D3DPresentCallP95Ms { get; init; }
    public double D3DPresentCallP99Ms { get; init; }
    public double D3DPresentCallMaxMs { get; init; }
    public double D3DTotalFrameCpuAvgMs { get; init; }
    public double D3DTotalFrameCpuP95Ms { get; init; }
    public double D3DTotalFrameCpuP99Ms { get; init; }
    public double D3DTotalFrameCpuMaxMs { get; init; }
    public int D3DPipelineLatencySampleCount { get; init; }
    public double D3DPipelineLatencyAvgMs { get; init; }
    public double D3DPipelineLatencyP95Ms { get; init; }
    public double D3DPipelineLatencyP99Ms { get; init; }
    public double D3DPipelineLatencyMaxMs { get; init; }
    public bool D3DFrameLatencyWaitEnabled { get; init; }
    public bool D3DFrameLatencyWaitHandleActive { get; init; }
    public long D3DFrameLatencyWaitCallCount { get; init; }
    public long D3DFrameLatencyWaitSignaledCount { get; init; }
    public long D3DFrameLatencyWaitTimeoutCount { get; init; }
    public long D3DFrameLatencyWaitUnexpectedResultCount { get; init; }
    public uint D3DFrameLatencyWaitLastResult { get; init; }
    public double D3DFrameLatencyWaitLastMs { get; init; }
    public int D3DFrameLatencyWaitSampleCount { get; init; }
    public double D3DFrameLatencyWaitAvgMs { get; init; }
    public double D3DFrameLatencyWaitP95Ms { get; init; }
    public double D3DFrameLatencyWaitP99Ms { get; init; }
    public double D3DFrameLatencyWaitMaxMs { get; init; }
    public long D3DFrameStatsSampleCount { get; init; }
    public long D3DFrameStatsSuccessCount { get; init; }
    public long D3DFrameStatsFailureCount { get; init; }
    public string D3DFrameStatsLastError { get; init; } = string.Empty;
    public long D3DFrameStatsPresentCount { get; init; }
    public long D3DFrameStatsPresentRefreshCount { get; init; }
    public long D3DFrameStatsSyncRefreshCount { get; init; }
    public long D3DFrameStatsSyncQpcTime { get; init; }
    public long D3DFrameStatsLastPresentDelta { get; init; }
    public long D3DFrameStatsLastPresentRefreshDelta { get; init; }
    public long D3DFrameStatsLastSyncRefreshDelta { get; init; }
    public long D3DFrameStatsMissedRefreshCount { get; init; }
    public long D3DLastSubmittedPreviewPresentId { get; init; }
    public long D3DLastSubmittedSourceSequenceNumber { get; init; }
    public long D3DLastSubmittedSourcePtsTicks { get; init; }
    public long D3DLastSubmittedQpc { get; init; }
    public long D3DLastSubmittedUtcUnixMs { get; init; }
    public long D3DLastRenderedPreviewPresentId { get; init; }
    public long D3DLastRenderedSourceSequenceNumber { get; init; }
    public long D3DLastRenderedSourcePtsTicks { get; init; }
    public long D3DLastRenderedQpc { get; init; }
    public long D3DLastRenderedUtcUnixMs { get; init; }
    public double D3DLastRenderedSchedulerToPresentMs { get; init; }
    public double D3DLastRenderedPipelineLatencyMs { get; init; }
    public long D3DLastDroppedPreviewPresentId { get; init; }
    public long D3DLastDroppedSourceSequenceNumber { get; init; }
    public long D3DLastDroppedSourcePtsTicks { get; init; }
    public long D3DLastDroppedQpc { get; init; }
    public long D3DLastDroppedUtcUnixMs { get; init; }
    public string D3DLastDropReason { get; init; } = string.Empty;
    public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public double EstimatedPipelineLatencyMs { get; init; }
    public string GpuPlaybackState { get; init; } = "None";
    public int GpuNaturalVideoWidth { get; init; }
    public int GpuNaturalVideoHeight { get; init; }
    public double GpuPositionMs { get; init; }
}

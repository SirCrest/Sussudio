using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public long PreviewFramesArrived { get; init; }
    public long PreviewFramesDisplayed { get; init; }
    public long PreviewFramesDropped { get; init; }
    public int PreviewCadenceSampleCount { get; init; }
    public double PreviewCadenceObservedFps { get; init; }
    public double PreviewCadenceExpectedIntervalMs { get; init; }
    public double PreviewCadenceAverageIntervalMs { get; init; }
    public double PreviewCadenceP95IntervalMs { get; init; }
    public double PreviewCadenceP99IntervalMs { get; init; }
    public double PreviewCadenceMaxIntervalMs { get; init; }
    public double PreviewCadenceOnePercentLowFps { get; init; }
    public double PreviewCadenceFivePercentLowFps { get; init; }
    public double PreviewCadenceSampleDurationMs { get; init; }
    public double[] PreviewCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double PreviewCadenceJitterStdDevMs { get; init; }
    public long PreviewCadenceSlowFrameCount { get; init; }
    public double PreviewCadenceSlowFramePercent { get; init; }
    public bool PreviewGpuActive { get; init; }
    public bool PreviewPlaceholderVisible { get; init; }
    public bool PreviewGpuElementVisible { get; init; }
    public bool PreviewCpuElementVisible { get; init; }
    public bool PreviewRendererAttached { get; init; }
    public string PreviewStartupState { get; init; } = "Idle";
    public string? PreviewAttemptId { get; init; }
    public double? PreviewStartupElapsedMs { get; init; }
    public int PreviewStartupTimeoutMs { get; init; }
    public bool PreviewGpuSignalMediaOpened { get; init; }
    public bool PreviewGpuSignalFirstFrame { get; init; }
    public bool PreviewGpuSignalPlaybackAdvancing { get; init; }
    public PreviewStartupSignalFlags PreviewStartupRequiredSignals { get; init; }
    public PreviewStartupSignalFlags PreviewStartupReceivedSignals { get; init; }
    public string PreviewStartupStrategy { get; init; } = "None";
    public string? PreviewStartupMissingSignals { get; init; }
    public int PreviewRecoveryAttemptCount { get; init; }
    public string? PreviewLastFailureReason { get; init; }
    public bool PreviewFirstVisualConfirmed { get; init; }
    public bool PreviewBlankSuspected { get; init; }
    public bool PreviewStalled { get; init; }
    public string PreviewRendererMode { get; init; } = "None";
    public int PreviewD3DPresentSyncInterval { get; init; }
    public int PreviewD3DMaxFrameLatency { get; init; }
    public int PreviewD3DSwapChainBufferCount { get; init; }
    public string PreviewD3DSwapChainAddress { get; init; } = string.Empty;
    public long PreviewD3DFramesSubmitted { get; init; }
    public long PreviewD3DFramesRendered { get; init; }
    public long PreviewD3DFramesDropped { get; init; }
    public long PreviewD3DRenderThreadFailureCount { get; init; }
    public string PreviewD3DLastRenderThreadFailureType { get; init; } = string.Empty;
    public string PreviewD3DLastRenderThreadFailureMessage { get; init; } = string.Empty;
    public int PreviewD3DLastRenderThreadFailureHResult { get; init; }
    public int PreviewD3DPendingFrameCount { get; init; }
    public string PreviewD3DInputColorSpace { get; init; } = "None";
    public string PreviewD3DOutputColorSpace { get; init; } = "None";
    public int PreviewD3DCpuTimingSampleCount { get; init; }
    public double PreviewD3DInputUploadCpuAvgMs { get; init; }
    public double PreviewD3DInputUploadCpuP95Ms { get; init; }
    public double PreviewD3DInputUploadCpuP99Ms { get; init; }
    public double PreviewD3DInputUploadCpuMaxMs { get; init; }
    public double PreviewD3DRenderSubmitCpuAvgMs { get; init; }
    public double PreviewD3DRenderSubmitCpuP95Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuP99Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuMaxMs { get; init; }
    public double PreviewD3DPresentCallAvgMs { get; init; }
    public double PreviewD3DPresentCallP95Ms { get; init; }
    public double PreviewD3DPresentCallP99Ms { get; init; }
    public double PreviewD3DPresentCallMaxMs { get; init; }
    public double PreviewD3DTotalFrameCpuAvgMs { get; init; }
    public double PreviewD3DTotalFrameCpuP95Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP99Ms { get; init; }
    public double PreviewD3DTotalFrameCpuMaxMs { get; init; }
    public int PreviewD3DPipelineLatencySampleCount { get; init; }
    public double PreviewD3DPipelineLatencyAvgMs { get; init; }
    public double PreviewD3DPipelineLatencyP95Ms { get; init; }
    public double PreviewD3DPipelineLatencyP99Ms { get; init; }
    public double PreviewD3DPipelineLatencyMaxMs { get; init; }
    public bool PreviewD3DFrameLatencyWaitEnabled { get; init; }
    public bool PreviewD3DFrameLatencyWaitHandleActive { get; init; }
    public long PreviewD3DFrameLatencyWaitCallCount { get; init; }
    public long PreviewD3DFrameLatencyWaitSignaledCount { get; init; }
    public long PreviewD3DFrameLatencyWaitTimeoutCount { get; init; }
    public long PreviewD3DFrameLatencyWaitUnexpectedResultCount { get; init; }
    public uint PreviewD3DFrameLatencyWaitLastResult { get; init; }
    public double PreviewD3DFrameLatencyWaitLastMs { get; init; }
    public int PreviewD3DFrameLatencyWaitSampleCount { get; init; }
    public double PreviewD3DFrameLatencyWaitAvgMs { get; init; }
    public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitP99Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
    public long PreviewD3DFrameStatsSampleCount { get; init; }
    public long PreviewD3DFrameStatsSuccessCount { get; init; }
    public long PreviewD3DFrameStatsFailureCount { get; init; }
    public string PreviewD3DFrameStatsLastError { get; init; } = string.Empty;
    public long PreviewD3DFrameStatsPresentCount { get; init; }
    public long PreviewD3DFrameStatsPresentRefreshCount { get; init; }
    public long PreviewD3DFrameStatsSyncRefreshCount { get; init; }
    public long PreviewD3DFrameStatsSyncQpcTime { get; init; }
    public long PreviewD3DFrameStatsLastPresentDelta { get; init; }
    public long PreviewD3DFrameStatsLastPresentRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsLastSyncRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsMissedRefreshCount { get; init; }
    public long PreviewD3DFrameStatsRecentMissedRefreshCount { get; init; }
    public long PreviewD3DFrameStatsRecentFailureCount { get; init; }
    public long PreviewD3DLastSubmittedPreviewPresentId { get; init; }
    public long PreviewD3DLastSubmittedSourceSequenceNumber { get; init; }
    public long PreviewD3DLastSubmittedSourcePtsTicks { get; init; }
    public long PreviewD3DLastSubmittedQpc { get; init; }
    public long PreviewD3DLastSubmittedUtcUnixMs { get; init; }
    public long PreviewD3DLastRenderedPreviewPresentId { get; init; }
    public long PreviewD3DLastRenderedSourceSequenceNumber { get; init; }
    public long PreviewD3DLastRenderedSourcePtsTicks { get; init; }
    public long PreviewD3DLastRenderedQpc { get; init; }
    public long PreviewD3DLastRenderedUtcUnixMs { get; init; }
    public double PreviewD3DLastRenderedSchedulerToPresentMs { get; init; }
    public double PreviewD3DLastRenderedPipelineLatencyMs { get; init; }
    public long PreviewD3DLastDroppedPreviewPresentId { get; init; }
    public long PreviewD3DLastDroppedSourceSequenceNumber { get; init; }
    public long PreviewD3DLastDroppedSourcePtsTicks { get; init; }
    public long PreviewD3DLastDroppedQpc { get; init; }
    public long PreviewD3DLastDroppedUtcUnixMs { get; init; }
    public string PreviewD3DLastDropReason { get; init; } = string.Empty;
    public PreviewSlowFrameDiagnostic[] PreviewD3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public string PreviewGpuPlaybackState { get; init; } = "None";
    public int PreviewGpuNaturalVideoWidth { get; init; }
    public int PreviewGpuNaturalVideoHeight { get; init; }
    public double PreviewGpuPositionMs { get; init; }
    public long PreviewGpuPositionEventCount { get; init; }
    public bool PreviewHdrInputDetected { get; init; }
    public string PreviewToneMapMode { get; init; } = "Unknown";
    public string? PreviewColorContext { get; init; }
    public string PreviewAdapterColorMetadata { get; init; } = "None";
}

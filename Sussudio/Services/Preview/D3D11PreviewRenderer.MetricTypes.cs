namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    public readonly record struct PresentCadenceMetrics(
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

    public readonly record struct CpuStageTimingMetrics(
        int SampleCount,
        double AverageMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    public readonly record struct RenderCpuTimingMetrics(
        CpuStageTimingMetrics InputUpload,
        CpuStageTimingMetrics RenderSubmit,
        CpuStageTimingMetrics PresentCall,
        CpuStageTimingMetrics TotalFrame);

    public readonly record struct PipelineLatencyMetrics(
        int SampleCount,
        double AverageMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    public readonly record struct FrameLatencyWaitMetrics(
        bool Enabled,
        bool HandleActive,
        long CallCount,
        long SignaledCount,
        long TimeoutCount,
        long UnexpectedResultCount,
        uint LastResult,
        double LastWaitMs,
        CpuStageTimingMetrics Timing);

    public readonly record struct FrameOwnershipMetrics(
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

    public readonly record struct DxgiFrameStatisticsMetrics(
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
}

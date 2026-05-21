namespace Sussudio.Models;

public partial class CaptureDiagnosticsSnapshot
{
    public bool FlashbackActive { get; init; }
    public long FlashbackBufferedDurationMs { get; init; }
    public int FlashbackSegmentCount { get; init; }
    public long FlashbackDiskBytes { get; init; }
    public long FlashbackTotalBytesWritten { get; init; }
    public long FlashbackTempDriveFreeBytes { get; init; }
    public long FlashbackStartupCacheBudgetBytes { get; init; }
    public long FlashbackStartupCacheBytes { get; init; }
    public int FlashbackStartupCacheSessionCount { get; init; }
    public int FlashbackStartupCacheDeletedSessionCount { get; init; }
    public long FlashbackStartupCacheFreedBytes { get; init; }
    public bool FlashbackStartupCacheOverBudget { get; init; }

    public bool FlashbackEncodingFailed { get; init; }
    public string? FlashbackEncodingFailureType { get; init; }
    public string? FlashbackEncodingFailureMessage { get; init; }
    public bool FatalCleanupInProgress { get; init; }
    public bool FlashbackCleanupInProgress { get; init; }
    public bool FlashbackForceRotateActive { get; init; }
    public bool FlashbackForceRotateRequested { get; init; }
    public bool FlashbackForceRotateDraining { get; init; }
    public int FlashbackVideoQueueCapacity { get; init; }
    public int FlashbackVideoQueueMaxDepth { get; init; }
    public long FlashbackVideoFramesSubmittedToEncoder { get; init; }
    public long FlashbackVideoEncoderPts { get; init; }
    public long FlashbackVideoEncoderPacketsWritten { get; init; }
    public long FlashbackVideoEncoderDroppedFrames { get; init; }
    public long FlashbackVideoSequenceGaps { get; init; }
    public long FlashbackVideoQueueRejectedFrames { get; init; }
    public string FlashbackVideoQueueLastRejectReason { get; init; } = string.Empty;
    public long FlashbackVideoQueueOldestFrameAgeMs { get; init; }
    public long FlashbackVideoQueueLastLatencyMs { get; init; }
    public int FlashbackVideoQueueLatencySampleCount { get; init; }
    public double FlashbackVideoQueueLatencyAvgMs { get; init; }
    public double FlashbackVideoQueueLatencyP95Ms { get; init; }
    public double FlashbackVideoQueueLatencyP99Ms { get; init; }
    public double FlashbackVideoQueueLatencyMaxMs { get; init; }
    public long FlashbackVideoBackpressureWaitMs { get; init; }
    public long FlashbackVideoBackpressureEvents { get; init; }
    public long FlashbackVideoBackpressureLastWaitMs { get; init; }
    public long FlashbackVideoBackpressureMaxWaitMs { get; init; }
    public int FlashbackGpuQueueDepth { get; init; }
    public int FlashbackGpuQueueCapacity { get; init; }
    public int FlashbackGpuQueueMaxDepth { get; init; }
    public long FlashbackGpuFramesEnqueued { get; init; }
    public long FlashbackGpuFramesDropped { get; init; }
    public long FlashbackGpuQueueRejectedFrames { get; init; }
    public string FlashbackGpuQueueLastRejectReason { get; init; } = string.Empty;
}

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(
        FlashbackRecordingProjection flashbackRecording)
        => new()
        {
            EncodingFailed = flashbackRecording.EncodingFailed,
            EncodingFailureType = flashbackRecording.EncodingFailureType,
            EncodingFailureMessage = flashbackRecording.EncodingFailureMessage,
            FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress,
            CleanupInProgress = flashbackRecording.CleanupInProgress,
            ForceRotateActive = flashbackRecording.ForceRotateActive,
            ForceRotateRequested = flashbackRecording.ForceRotateRequested,
            ForceRotateDraining = flashbackRecording.ForceRotateDraining,
            TempDriveFreeBytes = flashbackRecording.StartupCache.TempDriveFreeBytes,
            StartupCacheBudgetBytes = flashbackRecording.StartupCache.BudgetBytes,
            StartupCacheBytes = flashbackRecording.StartupCache.Bytes,
            StartupCacheSessionCount = flashbackRecording.StartupCache.SessionCount,
            StartupCacheDeletedSessionCount = flashbackRecording.StartupCache.DeletedSessionCount,
            StartupCacheFreedBytes = flashbackRecording.StartupCache.FreedBytes,
            StartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,
            VideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,
            VideoQueueMaxDepth = flashbackRecording.Queues.VideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = flashbackRecording.Queues.VideoFramesSubmittedToEncoder,
            VideoEncoderPts = flashbackRecording.Queues.VideoEncoderPts,
            VideoEncoderPacketsWritten = flashbackRecording.Queues.VideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = flashbackRecording.Queues.VideoEncoderDroppedFrames,
            VideoSequenceGaps = flashbackRecording.Queues.VideoSequenceGaps,
            VideoQueueRejectedFrames = flashbackRecording.Queues.VideoQueueRejectedFrames,
            VideoQueueLastRejectReason = flashbackRecording.Queues.VideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = flashbackRecording.Queues.VideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = flashbackRecording.Queues.VideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = flashbackRecording.Queues.VideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = flashbackRecording.Queues.VideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = flashbackRecording.Queues.VideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = flashbackRecording.Queues.VideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = flashbackRecording.Queues.VideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = flashbackRecording.Queues.VideoBackpressureWaitMs,
            VideoBackpressureEvents = flashbackRecording.Queues.VideoBackpressureEvents,
            VideoBackpressureLastWaitMs = flashbackRecording.Queues.VideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = flashbackRecording.Queues.VideoBackpressureMaxWaitMs,
            GpuQueueDepth = flashbackRecording.Queues.GpuQueueDepth,
            GpuQueueCapacity = flashbackRecording.Queues.GpuQueueCapacity,
            GpuQueueMaxDepth = flashbackRecording.Queues.GpuQueueMaxDepth,
            GpuFramesEnqueued = flashbackRecording.Queues.GpuFramesEnqueued,
            GpuFramesDropped = flashbackRecording.Queues.GpuFramesDropped,
            GpuQueueRejectedFrames = flashbackRecording.Queues.GpuQueueRejectedFrames,
            GpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,
            Active = flashbackRecording.Active,
            BufferedDurationMs = flashbackRecording.BufferedDurationMs,
            DiskBytes = flashbackRecording.DiskBytes,
            TotalBytesWritten = flashbackRecording.TotalBytesWritten,
            OutputBytes = flashbackRecording.OutputBytes,
            FilePath = flashbackRecording.FilePath,
            EncodedFrames = flashbackRecording.EncodedFrames,
            DroppedFrames = flashbackRecording.DroppedFrames,
            GpuEncoding = flashbackRecording.GpuEncoding,
            BackendSettingsStale = flashbackRecording.BackendSettingsStale,
            BackendSettingsStaleReason = flashbackRecording.BackendSettingsStaleReason,
            BackendActiveFormat = flashbackRecording.BackendActiveFormat,
            BackendRequestedFormat = flashbackRecording.BackendRequestedFormat,
            BackendActivePreset = flashbackRecording.BackendActivePreset,
            BackendRequestedPreset = flashbackRecording.BackendRequestedPreset,
            ExportVerificationFormat = flashbackRecording.ExportVerificationFormat,
            CodecDowngradeReason = flashbackRecording.CodecDowngradeReason,
            EncoderCodecName = flashbackRecording.EncoderCodecName,
            EncoderTargetBitRate = flashbackRecording.EncoderTargetBitRate,
            EncoderWidth = flashbackRecording.EncoderWidth,
            EncoderHeight = flashbackRecording.EncoderHeight,
            EncoderFrameRate = flashbackRecording.EncoderFrameRate,
            EncoderFrameRateNumerator = flashbackRecording.EncoderFrameRateNumerator,
            EncoderFrameRateDenominator = flashbackRecording.EncoderFrameRateDenominator,
            VideoQueueDepth = flashbackRecording.Queues.VideoQueueDepth,
            AudioQueueDepth = flashbackRecording.Queues.AudioQueueDepth,
            AudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingFlattenedProjection
    {
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
        public bool FatalCleanupInProgress { get; init; }
        public bool CleanupInProgress { get; init; }
        public bool ForceRotateActive { get; init; }
        public bool ForceRotateRequested { get; init; }
        public bool ForceRotateDraining { get; init; }
        public long TempDriveFreeBytes { get; init; }
        public long StartupCacheBudgetBytes { get; init; }
        public long StartupCacheBytes { get; init; }
        public int StartupCacheSessionCount { get; init; }
        public int StartupCacheDeletedSessionCount { get; init; }
        public long StartupCacheFreedBytes { get; init; }
        public bool StartupCacheOverBudget { get; init; }
        public int VideoQueueCapacity { get; init; }
        public int VideoQueueMaxDepth { get; init; }
        public long VideoFramesSubmittedToEncoder { get; init; }
        public long VideoEncoderPts { get; init; }
        public long VideoEncoderPacketsWritten { get; init; }
        public long VideoEncoderDroppedFrames { get; init; }
        public long VideoSequenceGaps { get; init; }
        public long VideoQueueRejectedFrames { get; init; }
        public string VideoQueueLastRejectReason { get; init; }
        public long VideoQueueOldestFrameAgeMs { get; init; }
        public long VideoQueueLastLatencyMs { get; init; }
        public int VideoQueueLatencySampleCount { get; init; }
        public double VideoQueueLatencyAvgMs { get; init; }
        public double VideoQueueLatencyP95Ms { get; init; }
        public double VideoQueueLatencyP99Ms { get; init; }
        public double VideoQueueLatencyMaxMs { get; init; }
        public long VideoBackpressureWaitMs { get; init; }
        public long VideoBackpressureEvents { get; init; }
        public long VideoBackpressureLastWaitMs { get; init; }
        public long VideoBackpressureMaxWaitMs { get; init; }
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public long GpuQueueRejectedFrames { get; init; }
        public string GpuQueueLastRejectReason { get; init; }
        public bool Active { get; init; }
        public long BufferedDurationMs { get; init; }
        public long DiskBytes { get; init; }
        public long TotalBytesWritten { get; init; }
        public long OutputBytes { get; init; }
        public string? FilePath { get; init; }
        public long EncodedFrames { get; init; }
        public long DroppedFrames { get; init; }
        public bool GpuEncoding { get; init; }
        public bool BackendSettingsStale { get; init; }
        public string BackendSettingsStaleReason { get; init; }
        public string BackendActiveFormat { get; init; }
        public string BackendRequestedFormat { get; init; }
        public string BackendActivePreset { get; init; }
        public string BackendRequestedPreset { get; init; }
        public string? ExportVerificationFormat { get; init; }
        public string? CodecDowngradeReason { get; init; }
        public string? EncoderCodecName { get; init; }
        public uint EncoderTargetBitRate { get; init; }
        public int EncoderWidth { get; init; }
        public int EncoderHeight { get; init; }
        public double EncoderFrameRate { get; init; }
        public int? EncoderFrameRateNumerator { get; init; }
        public int? EncoderFrameRateDenominator { get; init; }
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }
}

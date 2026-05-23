using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
    {
        var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);
        var queues = BuildFlashbackRecordingQueuesProjection(health);
        var runtime = BuildFlashbackRecordingRuntimeProjection(health);
        var backend = BuildFlashbackRecordingBackendProjection(captureRuntime, health);
        var encoder = BuildFlashbackRecordingEncoderProjection(health);

        return new()
        {
            EncodingFailed = health.FlashbackEncodingFailed,
            EncodingFailureType = health.FlashbackEncodingFailureType,
            EncodingFailureMessage = health.FlashbackEncodingFailureMessage,
            FatalCleanupInProgress = health.FatalCleanupInProgress,
            CleanupInProgress = health.FlashbackCleanupInProgress,
            ForceRotateActive = health.FlashbackForceRotateActive,
            ForceRotateRequested = health.FlashbackForceRotateRequested,
            ForceRotateDraining = health.FlashbackForceRotateDraining,
            StartupCache = startupCache,
            Queues = queues,
            Runtime = runtime,
            Backend = backend,
            Encoder = encoder
        };
    }

    private readonly record struct FlashbackRecordingProjection
    {
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
        public bool FatalCleanupInProgress { get; init; }
        public bool CleanupInProgress { get; init; }
        public bool ForceRotateActive { get; init; }
        public bool ForceRotateRequested { get; init; }
        public bool ForceRotateDraining { get; init; }
        public FlashbackRecordingStartupCacheProjection StartupCache { get; init; }
        public FlashbackRecordingQueuesProjection Queues { get; init; }
        public FlashbackRecordingRuntimeProjection Runtime { get; init; }
        public FlashbackRecordingBackendProjection Backend { get; init; }
        public FlashbackRecordingEncoderProjection Encoder { get; init; }
    }

    private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,
            BudgetBytes = health.FlashbackStartupCacheBudgetBytes,
            Bytes = health.FlashbackStartupCacheBytes,
            SessionCount = health.FlashbackStartupCacheSessionCount,
            DeletedSessionCount = health.FlashbackStartupCacheDeletedSessionCount,
            FreedBytes = health.FlashbackStartupCacheFreedBytes,
            OverBudget = health.FlashbackStartupCacheOverBudget
        };

    private readonly record struct FlashbackRecordingStartupCacheProjection
    {
        public long TempDriveFreeBytes { get; init; }
        public long BudgetBytes { get; init; }
        public long Bytes { get; init; }
        public int SessionCount { get; init; }
        public int DeletedSessionCount { get; init; }
        public long FreedBytes { get; init; }
        public bool OverBudget { get; init; }
    }

    private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(
        FlashbackRecordingStartupCacheProjection startupCache)
        => new()
        {
            TempDriveFreeBytes = startupCache.TempDriveFreeBytes,
            BudgetBytes = startupCache.BudgetBytes,
            Bytes = startupCache.Bytes,
            SessionCount = startupCache.SessionCount,
            DeletedSessionCount = startupCache.DeletedSessionCount,
            FreedBytes = startupCache.FreedBytes,
            OverBudget = startupCache.OverBudget
        };

    private readonly record struct FlashbackRecordingStartupCacheFlattenedProjection
    {
        public long TempDriveFreeBytes { get; init; }
        public long BudgetBytes { get; init; }
        public long Bytes { get; init; }
        public int SessionCount { get; init; }
        public int DeletedSessionCount { get; init; }
        public long FreedBytes { get; init; }
        public bool OverBudget { get; init; }
    }

    private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            VideoQueueCapacity = health.FlashbackVideoQueueCapacity,
            VideoQueueMaxDepth = health.FlashbackVideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = health.FlashbackVideoFramesSubmittedToEncoder,
            VideoEncoderPts = health.FlashbackVideoEncoderPts,
            VideoEncoderPacketsWritten = health.FlashbackVideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = health.FlashbackVideoEncoderDroppedFrames,
            VideoSequenceGaps = health.FlashbackVideoSequenceGaps,
            VideoQueueRejectedFrames = health.FlashbackVideoQueueRejectedFrames,
            VideoQueueLastRejectReason = health.FlashbackVideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = health.FlashbackVideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = health.FlashbackVideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = health.FlashbackVideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = health.FlashbackVideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = health.FlashbackVideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = health.FlashbackVideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = health.FlashbackVideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = health.FlashbackVideoBackpressureWaitMs,
            VideoBackpressureEvents = health.FlashbackVideoBackpressureEvents,
            VideoBackpressureLastWaitMs = health.FlashbackVideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = health.FlashbackVideoBackpressureMaxWaitMs,
            GpuQueueDepth = health.FlashbackGpuQueueDepth,
            GpuQueueCapacity = health.FlashbackGpuQueueCapacity,
            GpuQueueMaxDepth = health.FlashbackGpuQueueMaxDepth,
            GpuFramesEnqueued = health.FlashbackGpuFramesEnqueued,
            GpuFramesDropped = health.FlashbackGpuFramesDropped,
            GpuQueueRejectedFrames = health.FlashbackGpuQueueRejectedFrames,
            GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,
            VideoQueueDepth = health.FlashbackVideoQueueDepth,
            AudioQueueDepth = health.FlashbackAudioQueueDepth,
            AudioQueueCapacity = health.FlashbackAudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingQueuesProjection
    {
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
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }

    private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(
        FlashbackRecordingQueuesProjection queues)
        => new()
        {
            VideoQueueCapacity = queues.VideoQueueCapacity,
            VideoQueueMaxDepth = queues.VideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = queues.VideoFramesSubmittedToEncoder,
            VideoEncoderPts = queues.VideoEncoderPts,
            VideoEncoderPacketsWritten = queues.VideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = queues.VideoEncoderDroppedFrames,
            VideoSequenceGaps = queues.VideoSequenceGaps,
            VideoQueueRejectedFrames = queues.VideoQueueRejectedFrames,
            VideoQueueLastRejectReason = queues.VideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = queues.VideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = queues.VideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = queues.VideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = queues.VideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = queues.VideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = queues.VideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = queues.VideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = queues.VideoBackpressureWaitMs,
            VideoBackpressureEvents = queues.VideoBackpressureEvents,
            VideoBackpressureLastWaitMs = queues.VideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = queues.VideoBackpressureMaxWaitMs,
            GpuQueueDepth = queues.GpuQueueDepth,
            GpuQueueCapacity = queues.GpuQueueCapacity,
            GpuQueueMaxDepth = queues.GpuQueueMaxDepth,
            GpuFramesEnqueued = queues.GpuFramesEnqueued,
            GpuFramesDropped = queues.GpuFramesDropped,
            GpuQueueRejectedFrames = queues.GpuQueueRejectedFrames,
            GpuQueueLastRejectReason = queues.GpuQueueLastRejectReason,
            VideoQueueDepth = queues.VideoQueueDepth,
            AudioQueueDepth = queues.AudioQueueDepth,
            AudioQueueCapacity = queues.AudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingQueuesFlattenedProjection
    {
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
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }

    private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Active = health.FlashbackActive,
            BufferedDurationMs = health.FlashbackBufferedDurationMs,
            DiskBytes = health.FlashbackDiskBytes,
            TotalBytesWritten = health.FlashbackTotalBytesWritten,
            OutputBytes = health.FlashbackOutputBytes,
            FilePath = health.FlashbackFilePath,
            EncodedFrames = health.FlashbackEncodedFrames,
            DroppedFrames = health.FlashbackDroppedFrames,
            GpuEncoding = health.FlashbackGpuEncoding
        };

    private readonly record struct FlashbackRecordingRuntimeProjection
    {
        public bool Active { get; init; }
        public long BufferedDurationMs { get; init; }
        public long DiskBytes { get; init; }
        public long TotalBytesWritten { get; init; }
        public long OutputBytes { get; init; }
        public string? FilePath { get; init; }
        public long EncodedFrames { get; init; }
        public long DroppedFrames { get; init; }
        public bool GpuEncoding { get; init; }
    }

    private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(
        FlashbackRecordingRuntimeProjection runtime)
        => new()
        {
            Active = runtime.Active,
            BufferedDurationMs = runtime.BufferedDurationMs,
            DiskBytes = runtime.DiskBytes,
            TotalBytesWritten = runtime.TotalBytesWritten,
            OutputBytes = runtime.OutputBytes,
            FilePath = runtime.FilePath,
            EncodedFrames = runtime.EncodedFrames,
            DroppedFrames = runtime.DroppedFrames,
            GpuEncoding = runtime.GpuEncoding
        };

    private readonly record struct FlashbackRecordingRuntimeFlattenedProjection
    {
        public bool Active { get; init; }
        public long BufferedDurationMs { get; init; }
        public long DiskBytes { get; init; }
        public long TotalBytesWritten { get; init; }
        public long OutputBytes { get; init; }
        public string? FilePath { get; init; }
        public long EncodedFrames { get; init; }
        public long DroppedFrames { get; init; }
        public bool GpuEncoding { get; init; }
    }

    private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
        => new()
        {
            SettingsStale = health.FlashbackBackendSettingsStale,
            SettingsStaleReason = health.FlashbackBackendSettingsStaleReason,
            ActiveFormat = health.FlashbackBackendActiveFormat,
            RequestedFormat = health.FlashbackBackendRequestedFormat,
            ActivePreset = health.FlashbackBackendActivePreset,
            RequestedPreset = health.FlashbackBackendRequestedPreset,
            ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,
            CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason
        };

    private readonly record struct FlashbackRecordingBackendProjection
    {
        public bool SettingsStale { get; init; }
        public string SettingsStaleReason { get; init; }
        public string ActiveFormat { get; init; }
        public string RequestedFormat { get; init; }
        public string ActivePreset { get; init; }
        public string RequestedPreset { get; init; }
        public string? ExportVerificationFormat { get; init; }
        public string? CodecDowngradeReason { get; init; }
    }

    private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(
        FlashbackRecordingBackendProjection backend)
        => new()
        {
            SettingsStale = backend.SettingsStale,
            SettingsStaleReason = backend.SettingsStaleReason,
            ActiveFormat = backend.ActiveFormat,
            RequestedFormat = backend.RequestedFormat,
            ActivePreset = backend.ActivePreset,
            RequestedPreset = backend.RequestedPreset,
            ExportVerificationFormat = backend.ExportVerificationFormat,
            CodecDowngradeReason = backend.CodecDowngradeReason,
        };

    private readonly record struct FlashbackRecordingBackendFlattenedProjection
    {
        public bool SettingsStale { get; init; }
        public string SettingsStaleReason { get; init; }
        public string ActiveFormat { get; init; }
        public string RequestedFormat { get; init; }
        public string ActivePreset { get; init; }
        public string RequestedPreset { get; init; }
        public string? ExportVerificationFormat { get; init; }
        public string? CodecDowngradeReason { get; init; }
    }

    private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            CodecName = health.EncoderCodecName,
            TargetBitRate = health.EncoderTargetBitRate,
            Width = health.EncoderWidth,
            Height = health.EncoderHeight,
            FrameRate = health.EncoderFrameRate,
            FrameRateNumerator = health.EncoderFrameRateNumerator,
            FrameRateDenominator = health.EncoderFrameRateDenominator
        };

    private readonly record struct FlashbackRecordingEncoderProjection
    {
        public string? CodecName { get; init; }
        public uint TargetBitRate { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public double FrameRate { get; init; }
        public int? FrameRateNumerator { get; init; }
        public int? FrameRateDenominator { get; init; }
    }

    private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(
        FlashbackRecordingEncoderProjection encoder)
        => new()
        {
            CodecName = encoder.CodecName,
            TargetBitRate = encoder.TargetBitRate,
            Width = encoder.Width,
            Height = encoder.Height,
            FrameRate = encoder.FrameRate,
            FrameRateNumerator = encoder.FrameRateNumerator,
            FrameRateDenominator = encoder.FrameRateDenominator
        };

    private readonly record struct FlashbackRecordingEncoderFlattenedProjection
    {
        public string? CodecName { get; init; }
        public uint TargetBitRate { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public double FrameRate { get; init; }
        public int? FrameRateNumerator { get; init; }
        public int? FrameRateDenominator { get; init; }
    }

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
            StartupCache = BuildFlashbackRecordingStartupCacheFlattenedProjection(flashbackRecording.StartupCache),
            Queues = BuildFlashbackRecordingQueuesFlattenedProjection(flashbackRecording.Queues),
            Runtime = BuildFlashbackRecordingRuntimeFlattenedProjection(flashbackRecording.Runtime),
            Backend = BuildFlashbackRecordingBackendFlattenedProjection(flashbackRecording.Backend),
            Encoder = BuildFlashbackRecordingEncoderFlattenedProjection(flashbackRecording.Encoder)
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
        public FlashbackRecordingStartupCacheFlattenedProjection StartupCache { get; init; }
        public FlashbackRecordingQueuesFlattenedProjection Queues { get; init; }
        public FlashbackRecordingRuntimeFlattenedProjection Runtime { get; init; }
        public FlashbackRecordingBackendFlattenedProjection Backend { get; init; }
        public FlashbackRecordingEncoderFlattenedProjection Encoder { get; init; }
    }
}

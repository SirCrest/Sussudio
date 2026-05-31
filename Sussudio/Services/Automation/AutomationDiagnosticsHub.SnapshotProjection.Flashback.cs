using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)
        => new()
        {
            Active = health.FlashbackExportActive,
            Id = health.FlashbackExportId,
            Status = health.FlashbackExportStatus,
            OutputPath = health.FlashbackExportOutputPath,
            StartedUtcUnixMs = health.FlashbackExportStartedUtcUnixMs,
            LastProgressUtcUnixMs = health.FlashbackExportLastProgressUtcUnixMs,
            CompletedUtcUnixMs = health.FlashbackExportCompletedUtcUnixMs,
            ElapsedMs = health.FlashbackExportElapsedMs,
            LastProgressAgeMs = health.FlashbackExportLastProgressAgeMs,
            OutputBytes = health.FlashbackExportOutputBytes,
            ThroughputBytesPerSec = health.FlashbackExportThroughputBytesPerSec,
            SegmentsProcessed = health.FlashbackExportSegmentsProcessed,
            TotalSegments = health.FlashbackExportTotalSegments,
            Percent = health.FlashbackExportPercent,
            InPointMs = health.FlashbackExportInPointMs,
            OutPointMs = health.FlashbackExportOutPointMs,
            Message = health.FlashbackExportMessage,
            FailureKind = health.FlashbackExportFailureKind,
            ForceRotateFallbacks = health.FlashbackExportForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs = health.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs = health.FlashbackExportLastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs = health.FlashbackExportLastForceRotateFallbackOutPointMs
        };

    private static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)
        => new()
        {
            LastExportId = health.LastExportId,
            LastExportPath = health.LastExportPath,
            LastExportSuccess = health.LastExportSuccess,
            LastExportMessage = health.LastExportMessage
        };

    private readonly record struct FlashbackExportProjection
    {
        public bool Active { get; init; }
        public long Id { get; init; }
        public string Status { get; init; }
        public string OutputPath { get; init; }
        public long StartedUtcUnixMs { get; init; }
        public long LastProgressUtcUnixMs { get; init; }
        public long CompletedUtcUnixMs { get; init; }
        public long ElapsedMs { get; init; }
        public long LastProgressAgeMs { get; init; }
        public long OutputBytes { get; init; }
        public double ThroughputBytesPerSec { get; init; }
        public int SegmentsProcessed { get; init; }
        public int TotalSegments { get; init; }
        public double Percent { get; init; }
        public long InPointMs { get; init; }
        public long OutPointMs { get; init; }
        public string Message { get; init; }
        public string FailureKind { get; init; }
        public long ForceRotateFallbacks { get; init; }
        public long LastForceRotateFallbackUtcUnixMs { get; init; }
        public int LastForceRotateFallbackSegments { get; init; }
        public long LastForceRotateFallbackInPointMs { get; init; }
        public long LastForceRotateFallbackOutPointMs { get; init; }
    }

    private readonly record struct FlashbackExportLastResultProjection
    {
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }

    private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(
        FlashbackExportProjection flashbackExport,
        FlashbackExportLastResultProjection lastResult)
        => new()
        {
            Active = flashbackExport.Active,
            Id = flashbackExport.Id,
            Status = flashbackExport.Status,
            OutputPath = flashbackExport.OutputPath,
            StartedUtcUnixMs = flashbackExport.StartedUtcUnixMs,
            LastProgressUtcUnixMs = flashbackExport.LastProgressUtcUnixMs,
            CompletedUtcUnixMs = flashbackExport.CompletedUtcUnixMs,
            ElapsedMs = flashbackExport.ElapsedMs,
            LastProgressAgeMs = flashbackExport.LastProgressAgeMs,
            OutputBytes = flashbackExport.OutputBytes,
            ThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,
            SegmentsProcessed = flashbackExport.SegmentsProcessed,
            TotalSegments = flashbackExport.TotalSegments,
            Percent = flashbackExport.Percent,
            InPointMs = flashbackExport.InPointMs,
            OutPointMs = flashbackExport.OutPointMs,
            Message = flashbackExport.Message,
            FailureKind = flashbackExport.FailureKind,
            ForceRotateFallbacks = flashbackExport.ForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs = flashbackExport.LastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs = flashbackExport.LastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs = flashbackExport.LastForceRotateFallbackOutPointMs,
            LastExportId = lastResult.LastExportId,
            LastExportPath = lastResult.LastExportPath,
            LastExportSuccess = lastResult.LastExportSuccess,
            LastExportMessage = lastResult.LastExportMessage
        };

    private readonly record struct FlashbackExportFlattenedProjection
    {
        public bool Active { get; init; }
        public long Id { get; init; }
        public string Status { get; init; }
        public string OutputPath { get; init; }
        public long StartedUtcUnixMs { get; init; }
        public long LastProgressUtcUnixMs { get; init; }
        public long CompletedUtcUnixMs { get; init; }
        public long ElapsedMs { get; init; }
        public long LastProgressAgeMs { get; init; }
        public long OutputBytes { get; init; }
        public double ThroughputBytesPerSec { get; init; }
        public int SegmentsProcessed { get; init; }
        public int TotalSegments { get; init; }
        public double Percent { get; init; }
        public long InPointMs { get; init; }
        public long OutPointMs { get; init; }
        public string Message { get; init; }
        public string FailureKind { get; init; }
        public long ForceRotateFallbacks { get; init; }
        public long LastForceRotateFallbackUtcUnixMs { get; init; }
        public int LastForceRotateFallbackSegments { get; init; }
        public long LastForceRotateFallbackInPointMs { get; init; }
        public long LastForceRotateFallbackOutPointMs { get; init; }
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }

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

private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)
    {
        var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);
        var timing = BuildFlashbackPlaybackTimingProjection(health);
        var decode = BuildFlashbackPlaybackDecodeProjection(health);
        var commands = BuildFlashbackPlaybackCommandProjection(health);

        return new()
        {
            State = health.FlashbackPlaybackState,
            PositionMs = health.FlashbackPlaybackPositionMs,
            DecoderHwAccel = health.FlashbackDecoderHwAccel,
            FrameCount = health.FlashbackPlaybackFrameCount,
            LateFrames = health.FlashbackPlaybackLateFrames,
            DroppedFrames = health.FlashbackPlaybackDroppedFrames,
            AudioMaster = audioMaster,
            Timing = timing,
            Decode = decode,
            Commands = commands
        };
    }

    private readonly record struct FlashbackPlaybackProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterProjection AudioMaster { get; init; }
        public FlashbackPlaybackTimingProjection Timing { get; init; }
        public FlashbackPlaybackDecodeProjection Decode { get; init; }
        public FlashbackPlaybackCommandProjection Commands { get; init; }
    }

    private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)
        => new()
        {
            DelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            LastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs
        };

    private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(
        FlashbackPlaybackAudioMasterProjection audioMaster)
        => new()
        {
            DelayDoubles = audioMaster.DelayDoubles,
            DelayShrinks = audioMaster.DelayShrinks,
            Fallbacks = audioMaster.Fallbacks,
            UnavailableFallbacks = audioMaster.UnavailableFallbacks,
            StaleFallbacks = audioMaster.StaleFallbacks,
            DriftOutlierFallbacks = audioMaster.DriftOutlierFallbacks,
            LastFallbackReason = audioMaster.LastFallbackReason,
            LastFallbackDriftMs = audioMaster.LastFallbackDriftMs,
            LastFallbackClockAgeMs = audioMaster.LastFallbackClockAgeMs
        };

    private readonly record struct FlashbackPlaybackAudioMasterProjection
    {
        public long DelayDoubles { get; init; }
        public long DelayShrinks { get; init; }
        public long Fallbacks { get; init; }
        public long UnavailableFallbacks { get; init; }
        public long StaleFallbacks { get; init; }
        public long DriftOutlierFallbacks { get; init; }
        public string LastFallbackReason { get; init; }
        public double LastFallbackDriftMs { get; init; }
        public double LastFallbackClockAgeMs { get; init; }
    }

    private readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection
    {
        public long DelayDoubles { get; init; }
        public long DelayShrinks { get; init; }
        public long Fallbacks { get; init; }
        public long UnavailableFallbacks { get; init; }
        public long StaleFallbacks { get; init; }
        public long DriftOutlierFallbacks { get; init; }
        public string LastFallbackReason { get; init; }
        public double LastFallbackDriftMs { get; init; }
        public double LastFallbackClockAgeMs { get; init; }
    }

    private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)
        => new()
        {
            SegmentSwitches = health.FlashbackPlaybackSegmentSwitches,
            Fmp4Reopens = health.FlashbackPlaybackFmp4Reopens,
            WriteHeadWaits = health.FlashbackPlaybackWriteHeadWaits,
            NearLiveSnaps = health.FlashbackPlaybackNearLiveSnaps,
            DecodeErrorSnaps = health.FlashbackPlaybackDecodeErrorSnaps,
            SubmitFailures = health.FlashbackPlaybackSubmitFailures,
            LastDropUtcUnixMs = health.FlashbackPlaybackLastDropUtcUnixMs,
            LastDropReason = health.FlashbackPlaybackLastDropReason,
            LastSubmitFailureUtcUnixMs = health.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            LastSubmitFailure = health.FlashbackPlaybackLastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = health.FlashbackPlaybackLastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = health.FlashbackPlaybackLastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = health.FlashbackPlaybackLastWriteHeadWaitGapMs,
            TargetFps = health.FlashbackPlaybackTargetFps,
            ObservedFps = health.FlashbackPlaybackObservedFps,
            AvgFrameMs = health.FlashbackPlaybackAvgFrameMs,
            CadenceSampleCount = health.FlashbackPlaybackCadenceSampleCount,
            P95FrameMs = health.FlashbackPlaybackP95FrameMs,
            P99FrameMs = health.FlashbackPlaybackP99FrameMs,
            MaxFrameMs = health.FlashbackPlaybackMaxFrameMs,
            SlowFrames = health.FlashbackPlaybackSlowFrames,
            SlowFramePercent = health.FlashbackPlaybackSlowFramePercent,
            OnePercentLowFps = health.FlashbackPlaybackOnePercentLowFps,
            FivePercentLowFps = health.FlashbackPlaybackFivePercentLowFps,
            SampleDurationMs = health.FlashbackPlaybackSampleDurationMs,
            RecentFrameIntervalsMs = health.FlashbackPlaybackRecentFrameIntervalsMs,
            PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = health.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = health.FlashbackPlaybackLastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = health.FlashbackPlaybackLastPtsCadenceExpectedMs,
            AvDriftMs = health.FlashbackAvDriftMs
        };

    private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(
        FlashbackPlaybackTimingProjection timing)
        => new()
        {
            SegmentSwitches = timing.SegmentSwitches,
            Fmp4Reopens = timing.Fmp4Reopens,
            WriteHeadWaits = timing.WriteHeadWaits,
            NearLiveSnaps = timing.NearLiveSnaps,
            DecodeErrorSnaps = timing.DecodeErrorSnaps,
            SubmitFailures = timing.SubmitFailures,
            LastDropUtcUnixMs = timing.LastDropUtcUnixMs,
            LastDropReason = timing.LastDropReason,
            LastSubmitFailureUtcUnixMs = timing.LastSubmitFailureUtcUnixMs,
            LastSubmitFailure = timing.LastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = timing.LastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = timing.LastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = timing.LastWriteHeadWaitGapMs,
            TargetFps = timing.TargetFps,
            ObservedFps = timing.ObservedFps,
            AvgFrameMs = timing.AvgFrameMs,
            CadenceSampleCount = timing.CadenceSampleCount,
            P95FrameMs = timing.P95FrameMs,
            P99FrameMs = timing.P99FrameMs,
            MaxFrameMs = timing.MaxFrameMs,
            SlowFrames = timing.SlowFrames,
            SlowFramePercent = timing.SlowFramePercent,
            OnePercentLowFps = timing.OnePercentLowFps,
            FivePercentLowFps = timing.FivePercentLowFps,
            SampleDurationMs = timing.SampleDurationMs,
            RecentFrameIntervalsMs = timing.RecentFrameIntervalsMs,
            PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = timing.LastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = timing.LastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = timing.LastPtsCadenceExpectedMs,
            AvDriftMs = timing.AvDriftMs
        };

    private readonly record struct FlashbackPlaybackTimingProjection
    {
        public long SegmentSwitches { get; init; }
        public long Fmp4Reopens { get; init; }
        public long WriteHeadWaits { get; init; }
        public long NearLiveSnaps { get; init; }
        public long DecodeErrorSnaps { get; init; }
        public long SubmitFailures { get; init; }
        public long LastDropUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public long LastSubmitFailureUtcUnixMs { get; init; }
        public string LastSubmitFailure { get; init; }
        public long LastSegmentSwitchUtcUnixMs { get; init; }
        public long LastFmp4ReopenUtcUnixMs { get; init; }
        public long LastWriteHeadWaitGapMs { get; init; }
        public double TargetFps { get; init; }
        public double ObservedFps { get; init; }
        public double AvgFrameMs { get; init; }
        public int CadenceSampleCount { get; init; }
        public double P95FrameMs { get; init; }
        public double P99FrameMs { get; init; }
        public double MaxFrameMs { get; init; }
        public long SlowFrames { get; init; }
        public double SlowFramePercent { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentFrameIntervalsMs { get; init; }
        public long PtsCadenceMismatchCount { get; init; }
        public long LastPtsCadenceMismatchUtcUnixMs { get; init; }
        public double LastPtsCadenceDeltaMs { get; init; }
        public double LastPtsCadenceExpectedMs { get; init; }
        public double AvDriftMs { get; init; }
    }

    private readonly record struct FlashbackPlaybackTimingFlattenedProjection
    {
        public long SegmentSwitches { get; init; }
        public long Fmp4Reopens { get; init; }
        public long WriteHeadWaits { get; init; }
        public long NearLiveSnaps { get; init; }
        public long DecodeErrorSnaps { get; init; }
        public long SubmitFailures { get; init; }
        public long LastDropUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public long LastSubmitFailureUtcUnixMs { get; init; }
        public string LastSubmitFailure { get; init; }
        public long LastSegmentSwitchUtcUnixMs { get; init; }
        public long LastFmp4ReopenUtcUnixMs { get; init; }
        public long LastWriteHeadWaitGapMs { get; init; }
        public double TargetFps { get; init; }
        public double ObservedFps { get; init; }
        public double AvgFrameMs { get; init; }
        public int CadenceSampleCount { get; init; }
        public double P95FrameMs { get; init; }
        public double P99FrameMs { get; init; }
        public double MaxFrameMs { get; init; }
        public long SlowFrames { get; init; }
        public double SlowFramePercent { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentFrameIntervalsMs { get; init; }
        public long PtsCadenceMismatchCount { get; init; }
        public long LastPtsCadenceMismatchUtcUnixMs { get; init; }
        public double LastPtsCadenceDeltaMs { get; init; }
        public double LastPtsCadenceExpectedMs { get; init; }
        public double AvDriftMs { get; init; }
    }

    private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)
        => new()
        {
            SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = health.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            SampleCount = health.FlashbackPlaybackDecodeSampleCount,
            AvgMs = health.FlashbackPlaybackDecodeAvgMs,
            P95Ms = health.FlashbackPlaybackDecodeP95Ms,
            P99Ms = health.FlashbackPlaybackDecodeP99Ms,
            MaxMs = health.FlashbackPlaybackDecodeMaxMs,
            MaxPhase = health.FlashbackPlaybackMaxDecodePhase,
            MaxReceiveMs = health.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxFeedMs = health.FlashbackPlaybackMaxDecodeFeedMs,
            MaxReadMs = health.FlashbackPlaybackMaxDecodeReadMs,
            MaxSendMs = health.FlashbackPlaybackMaxDecodeSendMs,
            MaxAudioMs = health.FlashbackPlaybackMaxDecodeAudioMs,
            MaxConvertMs = health.FlashbackPlaybackMaxDecodeConvertMs,
            MaxUtcUnixMs = health.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs
        };

    private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(
        FlashbackPlaybackDecodeProjection decode)
        => new()
        {
            SeekForwardDecodeCapHits = decode.SeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = decode.LastSeekHitForwardDecodeCap,
            SampleCount = decode.SampleCount,
            AvgMs = decode.AvgMs,
            P95Ms = decode.P95Ms,
            P99Ms = decode.P99Ms,
            MaxMs = decode.MaxMs,
            MaxPhase = decode.MaxPhase,
            MaxReceiveMs = decode.MaxReceiveMs,
            MaxFeedMs = decode.MaxFeedMs,
            MaxReadMs = decode.MaxReadMs,
            MaxSendMs = decode.MaxSendMs,
            MaxAudioMs = decode.MaxAudioMs,
            MaxConvertMs = decode.MaxConvertMs,
            MaxUtcUnixMs = decode.MaxUtcUnixMs,
            MaxPositionMs = decode.MaxPositionMs
        };

    private readonly record struct FlashbackPlaybackDecodeProjection
    {
        public long SeekForwardDecodeCapHits { get; init; }
        public bool LastSeekHitForwardDecodeCap { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
        public string MaxPhase { get; init; }
        public double MaxReceiveMs { get; init; }
        public double MaxFeedMs { get; init; }
        public double MaxReadMs { get; init; }
        public double MaxSendMs { get; init; }
        public double MaxAudioMs { get; init; }
        public double MaxConvertMs { get; init; }
        public long MaxUtcUnixMs { get; init; }
        public long MaxPositionMs { get; init; }
    }

    private readonly record struct FlashbackPlaybackDecodeFlattenedProjection
    {
        public long SeekForwardDecodeCapHits { get; init; }
        public bool LastSeekHitForwardDecodeCap { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
        public string MaxPhase { get; init; }
        public double MaxReceiveMs { get; init; }
        public double MaxFeedMs { get; init; }
        public double MaxReadMs { get; init; }
        public double MaxSendMs { get; init; }
        public double MaxAudioMs { get; init; }
        public double MaxConvertMs { get; init; }
        public long MaxUtcUnixMs { get; init; }
        public long MaxPositionMs { get; init; }
    }

    private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)
        => new()
        {
            ThreadAlive = health.FlashbackPlaybackThreadAlive,
            Enqueued = health.FlashbackPlaybackCommandsEnqueued,
            Processed = health.FlashbackPlaybackCommandsProcessed,
            Dropped = health.FlashbackPlaybackCommandsDropped,
            SkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            QueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            Pending = health.FlashbackPlaybackPendingCommands,
            MaxPending = health.FlashbackPlaybackMaxPendingCommands,
            LastQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            MaxQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastQueued = health.FlashbackPlaybackLastCommandQueued,
            LastProcessed = health.FlashbackPlaybackLastCommandProcessed,
            LastQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            LastFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastFailure = health.FlashbackPlaybackLastCommandFailure
        };

    private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(
        FlashbackPlaybackCommandProjection commands)
        => new()
        {
            ThreadAlive = commands.ThreadAlive,
            Enqueued = commands.Enqueued,
            Processed = commands.Processed,
            Dropped = commands.Dropped,
            SkippedNotReady = commands.SkippedNotReady,
            ScrubUpdatesCoalesced = commands.ScrubUpdatesCoalesced,
            SeekCommandsCoalesced = commands.SeekCommandsCoalesced,
            QueueCapacity = commands.QueueCapacity,
            Pending = commands.Pending,
            MaxPending = commands.MaxPending,
            LastQueueLatencyMs = commands.LastQueueLatencyMs,
            MaxQueueLatencyMs = commands.MaxQueueLatencyMs,
            MaxQueueLatencyCommand = commands.MaxQueueLatencyCommand,
            LastQueued = commands.LastQueued,
            LastProcessed = commands.LastProcessed,
            LastQueuedUtcUnixMs = commands.LastQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = commands.LastProcessedUtcUnixMs,
            LastFailureUtcUnixMs = commands.LastFailureUtcUnixMs,
            LastFailure = commands.LastFailure
        };

    private readonly record struct FlashbackPlaybackCommandProjection
    {
        public bool ThreadAlive { get; init; }
        public long Enqueued { get; init; }
        public long Processed { get; init; }
        public long Dropped { get; init; }
        public long SkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int QueueCapacity { get; init; }
        public int Pending { get; init; }
        public int MaxPending { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string MaxQueueLatencyCommand { get; init; }
        public string LastQueued { get; init; }
        public string LastProcessed { get; init; }
        public long LastQueuedUtcUnixMs { get; init; }
        public long LastProcessedUtcUnixMs { get; init; }
        public long LastFailureUtcUnixMs { get; init; }
        public string LastFailure { get; init; }
    }

    private readonly record struct FlashbackPlaybackCommandFlattenedProjection
    {
        public bool ThreadAlive { get; init; }
        public long Enqueued { get; init; }
        public long Processed { get; init; }
        public long Dropped { get; init; }
        public long SkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int QueueCapacity { get; init; }
        public int Pending { get; init; }
        public int MaxPending { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string MaxQueueLatencyCommand { get; init; }
        public string LastQueued { get; init; }
        public string LastProcessed { get; init; }
        public long LastQueuedUtcUnixMs { get; init; }
        public long LastProcessedUtcUnixMs { get; init; }
        public long LastFailureUtcUnixMs { get; init; }
        public string LastFailure { get; init; }
    }

    private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(
        FlashbackPlaybackProjection flashbackPlayback)
        => new()
        {
            State = flashbackPlayback.State,
            PositionMs = flashbackPlayback.PositionMs,
            DecoderHwAccel = flashbackPlayback.DecoderHwAccel,
            FrameCount = flashbackPlayback.FrameCount,
            LateFrames = flashbackPlayback.LateFrames,
            DroppedFrames = flashbackPlayback.DroppedFrames,
            AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),
            Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),
            Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),
            Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)
        };

    private readonly record struct FlashbackPlaybackFlattenedProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }
        public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }
        public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }
        public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }
    }
}

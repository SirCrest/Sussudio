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
            Active = health.FlashbackActive,
            BufferedDurationMs = health.FlashbackBufferedDurationMs,
            DiskBytes = health.FlashbackDiskBytes,
            TotalBytesWritten = health.FlashbackTotalBytesWritten,
            OutputBytes = health.FlashbackOutputBytes,
            FilePath = health.FlashbackFilePath,
            EncodedFrames = health.FlashbackEncodedFrames,
            DroppedFrames = health.FlashbackDroppedFrames,
            GpuEncoding = health.FlashbackGpuEncoding,
            BackendSettingsStale = health.FlashbackBackendSettingsStale,
            BackendSettingsStaleReason = health.FlashbackBackendSettingsStaleReason,
            BackendActiveFormat = health.FlashbackBackendActiveFormat,
            BackendRequestedFormat = health.FlashbackBackendRequestedFormat,
            BackendActivePreset = health.FlashbackBackendActivePreset,
            BackendRequestedPreset = health.FlashbackBackendRequestedPreset,
            ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,
            CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,
            EncoderCodecName = health.EncoderCodecName,
            EncoderTargetBitRate = health.EncoderTargetBitRate,
            EncoderWidth = health.EncoderWidth,
            EncoderHeight = health.EncoderHeight,
            EncoderFrameRate = health.EncoderFrameRate,
            EncoderFrameRateNumerator = health.EncoderFrameRateNumerator,
            EncoderFrameRateDenominator = health.EncoderFrameRateDenominator
        };
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
    }

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
}

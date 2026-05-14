using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(
        FlashbackEncoderSink? fbSink,
        FlashbackBufferManager? bufMgr,
        CaptureSettings? flashbackBackendSettings,
        CaptureSettings? currentSettings)
    {
        var backendSettingsStaleReason = fbSink == null
            ? string.Empty
            : ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings);

        return new FlashbackBufferHealthSnapshotFields(
            fbSink != null,
            (long)(bufMgr?.BufferedDuration.TotalMilliseconds ?? 0),
            bufMgr?.SegmentCount ?? 0,
            bufMgr?.TotalDiskBytes ?? 0,
            bufMgr?.TotalBytesWritten ?? 0,
            bufMgr?.TempDriveAvailableFreeBytes ?? 0,
            bufMgr?.StartupCacheBudgetBytes ?? 0,
            bufMgr?.StartupCacheBytes ?? 0,
            bufMgr?.StartupCacheSessionCount ?? 0,
            bufMgr?.StartupCacheDeletedSessionCount ?? 0,
            bufMgr?.StartupCacheFreedBytes ?? 0,
            bufMgr?.StartupCacheOverBudget ?? false,
            fbSink?.OutputBytes ?? 0,
            bufMgr?.ActiveFilePath,
            fbSink?.EncodedVideoFrames ?? 0,
            fbSink?.DroppedVideoFrames ?? 0,
            fbSink?.GpuEncodingEnabled ?? false,
            !string.IsNullOrEmpty(backendSettingsStaleReason),
            backendSettingsStaleReason,
            flashbackBackendSettings?.Format.ToString() ?? string.Empty,
            currentSettings?.Format.ToString() ?? string.Empty,
            flashbackBackendSettings?.NvencPreset.ToString() ?? string.Empty,
            currentSettings?.NvencPreset.ToString() ?? string.Empty,
            fbSink?.CodecName,
            fbSink?.TargetBitRate ?? 0,
            fbSink?.EncoderWidth ?? 0,
            fbSink?.EncoderHeight ?? 0,
            fbSink?.EncoderFrameRate ?? 0,
            fbSink?.EncoderFrameRateNumerator,
            fbSink?.EncoderFrameRateDenominator);
    }

    private readonly record struct FlashbackBufferHealthSnapshotFields(
        bool Active,
        long BufferedDurationMs,
        int SegmentCount,
        long DiskBytes,
        long TotalBytesWritten,
        long TempDriveFreeBytes,
        long StartupCacheBudgetBytes,
        long StartupCacheBytes,
        int StartupCacheSessionCount,
        int StartupCacheDeletedSessionCount,
        long StartupCacheFreedBytes,
        bool StartupCacheOverBudget,
        long OutputBytes,
        string? FilePath,
        long EncodedFrames,
        long DroppedFrames,
        bool GpuEncoding,
        bool BackendSettingsStale,
        string BackendSettingsStaleReason,
        string BackendActiveFormat,
        string BackendRequestedFormat,
        string BackendActivePreset,
        string BackendRequestedPreset,
        string? EncoderCodecName,
        uint EncoderTargetBitRate,
        int EncoderWidth,
        int EncoderHeight,
        double EncoderFrameRate,
        int? EncoderFrameRateNumerator,
        int? EncoderFrameRateDenominator);
}

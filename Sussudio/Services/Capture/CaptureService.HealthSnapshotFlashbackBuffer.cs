using System;
using System.Collections.Generic;
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

    private static string ResolveFlashbackBackendSettingsStaleReason(
        CaptureSettings? backendSettings,
        CaptureSettings? requestedSettings)
    {
        if (backendSettings == null || requestedSettings == null)
        {
            return string.Empty;
        }

        var reasons = new List<string>();
        if (backendSettings.Format != requestedSettings.Format)
        {
            reasons.Add($"format:{backendSettings.Format}->{requestedSettings.Format}");
        }

        if (backendSettings.Quality != requestedSettings.Quality)
        {
            reasons.Add($"quality:{backendSettings.Quality}->{requestedSettings.Quality}");
        }

        if (Math.Abs(backendSettings.CustomBitrateMbps - requestedSettings.CustomBitrateMbps) >= 0.01)
        {
            reasons.Add($"bitrate:{backendSettings.CustomBitrateMbps:0.##}->{requestedSettings.CustomBitrateMbps:0.##}");
        }

        if (backendSettings.NvencPreset != requestedSettings.NvencPreset)
        {
            reasons.Add($"preset:{backendSettings.NvencPreset}->{requestedSettings.NvencPreset}");
        }

        if (backendSettings.AudioEnabled != requestedSettings.AudioEnabled)
        {
            reasons.Add($"audio:{backendSettings.AudioEnabled}->{requestedSettings.AudioEnabled}");
        }

        if (backendSettings.MicrophoneEnabled != requestedSettings.MicrophoneEnabled)
        {
            reasons.Add($"microphone:{backendSettings.MicrophoneEnabled}->{requestedSettings.MicrophoneEnabled}");
        }

        if (backendSettings.FlashbackBufferMinutes != requestedSettings.FlashbackBufferMinutes)
        {
            reasons.Add($"bufferMinutes:{backendSettings.FlashbackBufferMinutes}->{requestedSettings.FlashbackBufferMinutes}");
        }

        if (backendSettings.FlashbackGpuDecode != requestedSettings.FlashbackGpuDecode)
        {
            reasons.Add($"gpuDecode:{backendSettings.FlashbackGpuDecode}->{requestedSettings.FlashbackGpuDecode}");
        }

        var backendHdr = HdrOutputPolicy.IsEnabled(backendSettings);
        var requestedHdr = HdrOutputPolicy.IsEnabled(requestedSettings);
        if (backendHdr != requestedHdr)
        {
            reasons.Add($"hdr:{backendHdr}->{requestedHdr}");
        }

        return reasons.Count == 0 ? string.Empty : string.Join(",", reasons);
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

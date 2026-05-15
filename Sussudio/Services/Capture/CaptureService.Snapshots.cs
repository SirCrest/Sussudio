using System;
using System.Collections.Generic;
using System.IO;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Snapshot projection for diagnostics, automation, and verification. These
// methods translate live service objects into immutable DTOs without mutating
// capture state, so they can be polled frequently by ssctl/MCP.
public partial class CaptureService
{
    private string ResolveRecordingBackendName()
    {
        if (IsFlashbackRecordingBackendOwnedByRecording())
            return "Flashback";
        return _isRecording && _libavSink != null ? "LibAv" : "None";
    }

    public RecordingStats GetRecordingStats()
    {
        try
        {
            if (_isRecording && _libavSink != null)
            {
                return new RecordingStats(_libavSink.OutputBytes, 0);
            }

            // Flashback recording: the output file doesn't exist until export-on-stop.
            // Report estimated size from the flashback buffer bytes written since recording start.
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                var bufferManager = _flashbackBufferManager;
                if (bufferManager != null)
                {
                    return new RecordingStats(bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes, 0, isFlashbackEstimate: true);
                }
            }

            var path = _recordingContext?.VideoOutputPath ?? _lastOutputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return new RecordingStats(0, 0);
            }

            try
            {
                return new RecordingStats(new FileInfo(path).Length, 0);
            }
            catch (FileNotFoundException)
            {
                return new RecordingStats(0, 0);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"GetRecordingStats failed: {ex.Message}");
            return new RecordingStats(0, 0, isFailure: true);
        }
    }

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        // CaptureHealthSnapshot inherits from CaptureDiagnosticsSnapshot,
        // so the full snapshot satisfies the diagnostics contract directly.
        return GetHealthSnapshot();
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

    private static long ComputeFlashbackExportElapsedMs(
        bool active,
        long startedUtcUnixMs,
        long completedUtcUnixMs,
        long nowUtcUnixMs)
    {
        if (startedUtcUnixMs <= 0)
        {
            return 0;
        }

        var endUtcUnixMs = active
            ? nowUtcUnixMs
            : completedUtcUnixMs > 0
                ? completedUtcUnixMs
                : nowUtcUnixMs;

        return Math.Max(0, endUtcUnixMs - startedUtcUnixMs);
    }

    private static long ComputeFlashbackExportLastProgressAgeMs(
        bool active,
        long startedUtcUnixMs,
        long lastProgressUtcUnixMs,
        long nowUtcUnixMs)
    {
        if (!active)
        {
            return 0;
        }

        var referenceUtcUnixMs = lastProgressUtcUnixMs > 0
            ? lastProgressUtcUnixMs
            : startedUtcUnixMs;

        return referenceUtcUnixMs > 0
            ? Math.Max(0, nowUtcUnixMs - referenceUtcUnixMs)
            : 0;
    }

    private static long GetFileLengthOrZero(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    private static long ComputeTickAge(long tick)
    {
        if (tick == 0) return -1;
        return Math.Max(0, Environment.TickCount64 - tick);
    }
}

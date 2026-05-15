using System;
using System.IO;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private FlashbackExportHealthSnapshotFields CaptureFlashbackExportHealthSnapshotFields(
        long snapshotUtcUnixMs)
    {
        FlashbackExportHealthSnapshotFields export;
        lock (_flashbackExportDiagnosticsLock)
        {
            export = new FlashbackExportHealthSnapshotFields(
                _flashbackExportActive,
                _flashbackExportId,
                _flashbackExportStatus,
                _flashbackExportOutputPath,
                _flashbackExportStartedUtcUnixMs,
                _flashbackExportLastProgressUtcUnixMs,
                _flashbackExportCompletedUtcUnixMs,
                _flashbackExportSegmentsProcessed,
                _flashbackExportTotalSegments,
                _flashbackExportPercent,
                _flashbackExportInPointMs,
                _flashbackExportOutPointMs,
                _flashbackExportMessage,
                _flashbackExportFailureKind,
                _flashbackExportForceRotateFallbacks,
                _flashbackExportLastForceRotateFallbackUtcUnixMs,
                _flashbackExportLastForceRotateFallbackSegments,
                _flashbackExportLastForceRotateFallbackInPointMs,
                _flashbackExportLastForceRotateFallbackOutPointMs,
                _lastFlashbackExportResultId,
                _lastExportResult,
                0,
                0,
                0,
                0);
        }

        var elapsedMs = ComputeFlashbackExportElapsedMs(
            export.Active,
            export.StartedUtcUnixMs,
            export.CompletedUtcUnixMs,
            snapshotUtcUnixMs);
        var lastProgressAgeMs = ComputeFlashbackExportLastProgressAgeMs(
            export.Active,
            export.StartedUtcUnixMs,
            export.LastProgressUtcUnixMs,
            snapshotUtcUnixMs);
        var outputBytes = GetFileLengthOrZero(
            !string.IsNullOrWhiteSpace(export.OutputPath)
                ? export.OutputPath
                : export.LastResult?.OutputPath);
        var throughputBytesPerSec = elapsedMs > 0
            ? outputBytes / (elapsedMs / 1000.0)
            : 0;

        return export with
        {
            ElapsedMs = elapsedMs,
            LastProgressAgeMs = lastProgressAgeMs,
            OutputBytes = outputBytes,
            ThroughputBytesPerSec = throughputBytesPerSec
        };
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

    private readonly record struct FlashbackExportHealthSnapshotFields(
        bool Active,
        long Id,
        string Status,
        string OutputPath,
        long StartedUtcUnixMs,
        long LastProgressUtcUnixMs,
        long CompletedUtcUnixMs,
        int SegmentsProcessed,
        int TotalSegments,
        double Percent,
        long InPointMs,
        long OutPointMs,
        string Message,
        string FailureKind,
        long ForceRotateFallbacks,
        long LastForceRotateFallbackUtcUnixMs,
        int LastForceRotateFallbackSegments,
        long LastForceRotateFallbackInPointMs,
        long LastForceRotateFallbackOutPointMs,
        long LastResultId,
        FinalizeResult? LastResult,
        long ElapsedMs,
        long LastProgressAgeMs,
        long OutputBytes,
        double ThroughputBytesPerSec);
}

using System;
using System.IO;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private void RecordLastFlashbackExportResult(long exportId, FinalizeResult result)
    {
        lock (_flashbackExportDiagnosticsLock)
        {
            _lastExportResult = result;
            Volatile.Write(ref _lastFlashbackExportResultId, exportId);
        }
    }

    private long BeginFlashbackExportDiagnostics(TimeSpan inPoint, TimeSpan outPoint, string outputPath)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_flashbackExportDiagnosticsLock)
        {
            var exportId = Interlocked.Increment(ref _flashbackExportId);
            _flashbackExportActive = true;
            _flashbackExportStatus = "Running";
            _flashbackExportOutputPath = outputPath;
            _flashbackExportStartedUtcUnixMs = now;
            _flashbackExportLastProgressUtcUnixMs = now;
            _flashbackExportCompletedUtcUnixMs = 0;
            _flashbackExportSegmentsProcessed = 0;
            _flashbackExportTotalSegments = 0;
            _flashbackExportPercent = 0;
            _flashbackExportInPointMs = (long)inPoint.TotalMilliseconds;
            _flashbackExportOutPointMs = outPoint == TimeSpan.MaxValue ? -1 : (long)outPoint.TotalMilliseconds;
            _flashbackExportMessage = string.Empty;
            _flashbackExportFailureKind = string.Empty;

            return exportId;
        }
    }

    private void RecordRejectedFlashbackExportDiagnostics(
        string outputPath,
        FinalizeResult result,
        TimeSpan? inPoint = null,
        TimeSpan? outPoint = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportActive)
            {
                _lastExportResult = result;
                Volatile.Write(ref _lastFlashbackExportResultId, 0);
                Logger.Log(
                    "FLASHBACK_EXPORT_REJECTED_DIAGNOSTICS_DEFERRED " +
                    $"active_id={_flashbackExportId} status='{_flashbackExportStatus}' " +
                    $"rejected_status='{result.StatusMessage}' output='{outputPath}'");
                return;
            }

            var exportId = Interlocked.Increment(ref _flashbackExportId);
            _flashbackExportId = exportId;
            _flashbackExportActive = false;
            _flashbackExportStatus = IsFlashbackExportCancelled(result.StatusMessage) ? "Cancelled" : "Failed";
            _flashbackExportOutputPath = outputPath;
            _flashbackExportStartedUtcUnixMs = now;
            _flashbackExportLastProgressUtcUnixMs = now;
            _flashbackExportCompletedUtcUnixMs = now;
            _flashbackExportSegmentsProcessed = 0;
            _flashbackExportTotalSegments = 0;
            _flashbackExportPercent = 0;
            _flashbackExportInPointMs = inPoint.HasValue ? (long)inPoint.Value.TotalMilliseconds : 0;
            _flashbackExportOutPointMs = outPoint.HasValue
                ? outPoint.Value == TimeSpan.MaxValue ? -1 : (long)outPoint.Value.TotalMilliseconds
                : 0;
            _flashbackExportMessage = result.StatusMessage;
            _flashbackExportFailureKind = ClassifyFlashbackExportFailureKind(result.StatusMessage);
            RecordLastFlashbackExportResult(exportId, result);
        }
    }

    private void CompleteFlashbackExportDiagnostics(long exportId, FinalizeResult result)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId)
            {
                return;
            }

            _flashbackExportActive = false;
            _flashbackExportStatus = result.Succeeded
                ? "Succeeded"
                : IsFlashbackExportCancelled(result.StatusMessage)
                    ? "Cancelled"
                    : "Failed";
            var completedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _flashbackExportCompletedUtcUnixMs = completedUtcUnixMs;
            _flashbackExportLastProgressUtcUnixMs = completedUtcUnixMs;
            _flashbackExportMessage = result.StatusMessage;
            _flashbackExportFailureKind = result.Succeeded
                ? string.Empty
                : ClassifyFlashbackExportFailureKind(result.StatusMessage);
            if (result.Succeeded && _flashbackExportPercent < 100)
            {
                _flashbackExportPercent = 100;
            }
        }
    }

    private IProgress<ExportProgress> CreateFlashbackExportProgressSink(
        long exportId,
        IProgress<ExportProgress>? innerProgress)
    {
        return new FlashbackExportProgressForwarder(progress =>
        {
            UpdateFlashbackExportProgress(exportId, progress);
            try
            {
                innerProgress?.Report(progress);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_PROGRESS_FORWARD_WARN id={exportId} type={ex.GetType().Name} msg='{ex.Message}'");
            }
        });
    }

    private void UpdateFlashbackExportProgress(long exportId, ExportProgress progress)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId || !_flashbackExportActive)
            {
                return;
            }

            var rawTotalSegments = progress.TotalSegments;
            var rawSegmentsProcessed = progress.SegmentsProcessed;
            var rawPercent = progress.Percent;
            var totalSegments = Math.Max(0, rawTotalSegments);
            var segmentsProcessed = Math.Max(0, rawSegmentsProcessed);
            if (totalSegments > 0 && segmentsProcessed > totalSegments)
            {
                segmentsProcessed = totalSegments;
            }

            var percent = double.IsFinite(rawPercent)
                ? Math.Clamp(rawPercent, 0.0, 100.0)
                : 0.0;
            if (rawTotalSegments != totalSegments ||
                rawSegmentsProcessed != segmentsProcessed ||
                !double.IsFinite(rawPercent) ||
                rawPercent != percent)
            {
                Logger.Log(
                    $"FLASHBACK_EXPORT_PROGRESS_NORMALIZED id={exportId} " +
                    $"raw_segments={rawSegmentsProcessed}/{rawTotalSegments} " +
                    $"segments={segmentsProcessed}/{totalSegments} " +
                    $"raw_percent={rawPercent:0.###} percent={percent:0.###}");
            }

            _flashbackExportSegmentsProcessed = segmentsProcessed;
            _flashbackExportTotalSegments = totalSegments;
            _flashbackExportPercent = percent;
            _flashbackExportLastProgressUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    private void RecordFlashbackExportForceRotateFallback(
        long exportId,
        int segmentCount,
        TimeSpan inPoint,
        TimeSpan outPoint)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId)
            {
                return;
            }

            _flashbackExportForceRotateFallbacks++;
            _flashbackExportLastForceRotateFallbackUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _flashbackExportLastForceRotateFallbackSegments = Math.Max(0, segmentCount);
            _flashbackExportLastForceRotateFallbackInPointMs = (long)inPoint.TotalMilliseconds;
            _flashbackExportLastForceRotateFallbackOutPointMs = outPoint == TimeSpan.MaxValue
                ? -1
                : (long)outPoint.TotalMilliseconds;
        }
    }

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

    private sealed class FlashbackExportProgressForwarder : IProgress<ExportProgress>
    {
        private readonly Action<ExportProgress> _onProgress;

        public FlashbackExportProgressForwarder(Action<ExportProgress> onProgress)
        {
            _onProgress = onProgress;
        }

        public void Report(ExportProgress value)
            => _onProgress(value);
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

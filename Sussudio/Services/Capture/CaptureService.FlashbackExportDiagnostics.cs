using System;
using System.Threading;
using Sussudio.Models;

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
}

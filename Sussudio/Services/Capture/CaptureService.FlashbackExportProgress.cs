using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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
}

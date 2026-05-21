using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Flashback export entry points: range export, last-N-seconds export, and
// operation-specific range resolution before the shared core pipeline runs.
public partial class CaptureService
{
    internal async Task<FinalizeResult> ExportFlashbackRangeAsync(
        TimeSpan? inPoint, TimeSpan? outPoint, string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken ct,
        TimeSpan? inPointFilePts = null,
        TimeSpan? outPointFilePts = null,
        bool force = false)
    {
        var snapshotResult = await SnapshotFlashbackExportBackendAsync(
                outputPath,
                operationName: "range",
                sessionReleaseOperation: "flashback_export_snapshot_session",
                ct)
            .ConfigureAwait(false);
        if (snapshotResult.Failure != null)
        {
            return snapshotResult.Failure;
        }

        var snapshot = snapshotResult.Snapshot;
        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: snapshot.Sink,
                snapshotBufferManager: snapshot.BufferManager,
                snapshotExporter: snapshot.Exporter,
                exportOperationLockAlreadyHeld: true,
                force: force,
                resolveRangeAfterEvictionPaused: manager =>
                {
                    var validStart = manager.ValidStartPts;
                    if (inPointFilePts.HasValue || outPointFilePts.HasValue)
                    {
                        var absoluteInPoint = inPointFilePts ?? validStart;
                        var absoluteOutPoint = outPointFilePts ?? TimeSpan.MaxValue;
                        if (absoluteInPoint < validStart)
                        {
                            return (false, absoluteInPoint, absoluteOutPoint, "Flashback export in point has been evicted from the buffer.");
                        }

                        if (absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= validStart)
                        {
                            return (false, absoluteInPoint, absoluteOutPoint, "Flashback export out point has been evicted from the buffer.");
                        }

                        return absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= absoluteInPoint
                            ? (false, absoluteInPoint, absoluteOutPoint, "Flashback export range is empty or invalid.")
                            : (true, absoluteInPoint, absoluteOutPoint, null);
                    }

                    var bufferedDuration = manager.BufferedDuration;
                    var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);
                    var bufferOutPoint = outPoint.HasValue
                        ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)
                        : TimeSpan.MaxValue;
                    var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);
                    var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);
                    return fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint
                        ? (false, fileInPoint, fileOutPoint, "Flashback export range is empty or invalid.")
                        : (true, fileInPoint, fileOutPoint, null);
                })
            .ConfigureAwait(false);
    }

    internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct,
        bool force = false)
    {
        if (ct.IsCancellationRequested)
        {
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }

        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return FailFlashbackExport(outputPath, "Flashback export duration must be finite, greater than zero, and within TimeSpan range.");
        }

        var snapshotResult = await SnapshotFlashbackExportBackendAsync(
                outputPath,
                operationName: "last_n",
                sessionReleaseOperation: "flashback_export_last_n_snapshot_session",
                ct)
            .ConfigureAwait(false);
        if (snapshotResult.Failure != null)
        {
            return snapshotResult.Failure;
        }

        var snapshot = snapshotResult.Snapshot;
        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: snapshot.Sink,
                snapshotBufferManager: snapshot.BufferManager,
                snapshotExporter: snapshot.Exporter,
                exportOperationLockAlreadyHeld: true,
                force: force,
                resolveRangeAfterEvictionPaused: manager =>
                {
                    var bufferedDuration = manager.BufferedDuration;
                    var validStart = manager.ValidStartPts;
                    var rangeStart = bufferedDuration.TotalSeconds > seconds
                        ? TimeSpan.FromSeconds(bufferedDuration.TotalSeconds - seconds)
                        : TimeSpan.Zero;
                    var fileInPoint = AddFlashbackPtsOffsetOrMax(rangeStart, validStart);
                    return (true, fileInPoint, TimeSpan.MaxValue, null);
                })
            .ConfigureAwait(false);
    }

}

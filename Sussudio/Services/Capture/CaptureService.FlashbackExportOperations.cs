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
                resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(
                    inPoint,
                    outPoint,
                    inPointFilePts,
                    outPointFilePts))
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
                resolveRangeAfterEvictionPaused: CreateFlashbackExportLastNRangeResolver(seconds))
            .ConfigureAwait(false);
    }

}

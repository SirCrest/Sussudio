using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Shared Flashback export pipeline: eviction pause, force-rotate, exporter request,
// diagnostics completion, and cleanup.
public partial class CaptureService
{
    private delegate (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        FlashbackExportRangeResolver(FlashbackBufferManager manager);

    private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts)
    {
        return manager => ResolveFlashbackExportRangeAfterEvictionPaused(
            manager,
            inPoint,
            outPoint,
            inPointFilePts,
            outPointFilePts);
    }

    private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)
        => manager => ResolveFlashbackExportLastNRangeAfterEvictionPaused(manager, seconds);

    private FinalizeResult FailFlashbackExport(
        string outputPath,
        string statusMessage,
        TimeSpan? inPoint = null,
        TimeSpan? outPoint = null)
    {
        var result = FinalizeResult.Failure(outputPath, statusMessage);
        Logger.Log($"FLASHBACK_EXPORT_REJECTED status='{statusMessage}' output='{outputPath}'");
        RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);
        return result;
    }

    // Called from two contexts:
    // (1) Export methods - pass snapshotSink/snapshotBufferManager captured under session lock.
    // (2) FinalizeFlashbackRecordingAsync - runs under session lock, omits snapshots (field reads safe).
    private async Task<FinalizeResult> ExportFlashbackCoreAsync(
        TimeSpan inPoint, TimeSpan outPoint, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct,
        FlashbackEncoderSink? snapshotSink = null,
        FlashbackBufferManager? snapshotBufferManager = null,
        FlashbackExporter? snapshotExporter = null,
        bool requireCompleteLiveEdge = false,
        bool exportOperationLockAlreadyHeld = false,
        bool throttleHighResolutionBaseline = true,
        bool force = false,
        FlashbackExportRangeResolver? resolveRangeAfterEvictionPaused = null)
    {
        var flashbackSink = snapshotSink ?? _flashbackBackend.Sink;
        var bufferManager = snapshotBufferManager ?? _flashbackBackend.BufferManager;

        var exportId = 0L;
        var evictionPaused = false;
        var exportOperationLockHeld = exportOperationLockAlreadyHeld;
        try
        {
            if (!exportOperationLockAlreadyHeld)
            {
                try
                {
                    await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);
                    exportOperationLockHeld = true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return FailFlashbackExport(outputPath, "Flashback export cancelled.", inPoint, outPoint);
                }
            }

            if (bufferManager == null)
            {
                return FailFlashbackExport(outputPath, "Flashback buffer not active", inPoint, outPoint);
            }

            var exporter = snapshotExporter;
            if (exporter == null)
            {
                exporter = _flashbackBackend.Exporter ??= new FlashbackExporter();
            }

            // Pause eviction so segments aren't deleted while the exporter reads them.
            // Range-based UI exports resolve relative buffer positions after this pause
            // so queued exports cannot use a stale valid-start snapshot.
            bufferManager.PauseEviction();
            evictionPaused = true;

            if (resolveRangeAfterEvictionPaused != null)
            {
                var resolvedRange = resolveRangeAfterEvictionPaused(bufferManager);
                inPoint = resolvedRange.InPoint;
                outPoint = resolvedRange.OutPoint;
                if (!resolvedRange.Succeeded)
                {
                    return FailFlashbackExport(
                        outputPath,
                        resolvedRange.FailureMessage ?? "Flashback export range is empty or invalid.",
                        inPoint,
                        outPoint);
                }
            }

            exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);
            var diagnosticProgress = CreateFlashbackExportProgressSink(exportId, progress);

            var preparedExport = PrepareFlashbackExportRequest(
                bufferManager,
                flashbackSink,
                exportId,
                inPoint,
                outPoint,
                outputPath,
                requireCompleteLiveEdge,
                force,
                throttleHighResolutionBaseline,
                ct);
            if (preparedExport.FailureResult is { } preparationFailure)
            {
                return preparationFailure;
            }

            var request = preparedExport.Request
                ?? throw new InvalidOperationException("Flashback export request preparation returned no request.");
            var result = await exporter.ExportAsync(request, diagnosticProgress, ct).ConfigureAwait(false);
            if (preparedExport.ForceRotateFallbackUsed && result.Succeeded)
            {
                result = FinalizeResult.Success(
                    result.OutputPath,
                    $"{result.StatusMessage} (live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames)");
            }

            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            return result;
        }
        catch (Exception ex)
        {
            var statusMessage = ex is OperationCanceledException && ct.IsCancellationRequested
                ? "Flashback export cancelled."
                : ex.Message;
            Logger.Log(
                $"FLASHBACK_EXPORT_CORE_FAIL id={exportId} type={ex.GetType().Name} " +
                $"cancelled={ct.IsCancellationRequested} msg='{statusMessage}'");
            var failure = FinalizeResult.Failure(outputPath, statusMessage);
            if (exportId != 0)
            {
                RecordLastFlashbackExportResult(exportId, failure);
                CompleteFlashbackExportDiagnostics(exportId, failure);
            }
            else
            {
                RecordRejectedFlashbackExportDiagnostics(outputPath, failure, inPoint, outPoint);
            }
            return failure;
        }
        finally
        {
            if (evictionPaused)
            {
                ResumeFlashbackEvictionBestEffort(bufferManager, "flashback_export");
            }
            if (exportOperationLockHeld)
            {
                ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            }
        }
    }

    private FlashbackExportPreparationResult PrepareFlashbackExportRequest(
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink? flashbackSink,
        long exportId,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool requireCompleteLiveEdge,
        bool force,
        bool throttleHighResolutionBaseline,
        CancellationToken ct)
    {
        var forceRotatePreparation = PrepareFlashbackExportForceRotateSegments(
            bufferManager,
            flashbackSink,
            exportId,
            inPoint,
            outPoint,
            outputPath,
            requireCompleteLiveEdge,
            ct);
        if (forceRotatePreparation.FailureResult is { } forceRotateFailure)
        {
            return FlashbackExportPreparationResult.Failure(forceRotateFailure);
        }

        var segmentPaths = forceRotatePreparation.SegmentPaths;
        var forceRotateFallbackUsed = forceRotatePreparation.ForceRotateFallbackUsed;
        string? tsPath = null;

        // Fallback: single-file export if no segments available.
        if (segmentPaths == null)
        {
            tsPath = bufferManager.ActiveFilePath;
            if (string.IsNullOrWhiteSpace(tsPath))
            {
                var result = FinalizeResult.Failure(outputPath, "Flashback buffer has no active file");
                RecordLastFlashbackExportResult(exportId, result);
                CompleteFlashbackExportDiagnostics(exportId, result);
                return FlashbackExportPreparationResult.Failure(result);
            }

            Logger.Log(
                "FLASHBACK_EXPORT_ACTIVE_FILE_FALLBACK " +
                $"path='{tsPath}' in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)}");
        }

        var request = new FlashbackExportRequest
        {
            Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths),
            SegmentPaths = segmentPaths,
            InputTsPath = tsPath,
            InPoint = inPoint,
            OutPoint = outPoint,
            OutputPath = outputPath,
            FastStart = false,
            Force = force,
            AdaptiveThrottleDelayMsProvider = CreateFlashbackExportThrottleDelayProvider(
                flashbackSink,
                throttleHighResolutionBaseline),
        };

        return FlashbackExportPreparationResult.Ready(request, forceRotateFallbackUsed);
    }

    private FlashbackExportForceRotatePreparation PrepareFlashbackExportForceRotateSegments(
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink? flashbackSink,
        long exportId,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool requireCompleteLiveEdge,
        CancellationToken ct)
    {
        if (flashbackSink == null)
        {
            return FlashbackExportForceRotatePreparation.Ready(null, forceRotateFallbackUsed: false);
        }

        var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);
        var segmentPaths = forceRotateResult.SegmentPaths;
        if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)
        {
            var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
            var result = FinalizeResult.Failure(
                outputPath,
                "Flashback export failed: live-edge segment rotation failed.",
                preservedArtifacts);
            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            Logger.Log(
                "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED " +
                $"preserved_segments={preservedArtifacts.Count} " +
                $"in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)}");
            return FlashbackExportForceRotatePreparation.Failure(result);
        }

        if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)
        {
            var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
            var result = FinalizeResult.Failure(
                outputPath,
                requireCompleteLiveEdge
                    ? "Flashback recording finalize failed: live-edge segment was not closed before timeout."
                    : "Flashback export failed: live-edge segment rotation committed but did not complete before timeout.",
                preservedArtifacts);
            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            Logger.Log(
                "FLASHBACK_EXPORT_FORCE_ROTATE_COMMITTED_PENDING_FAIL " +
                $"preserved_segments={preservedArtifacts.Count} " +
                $"in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)outPoint.TotalMilliseconds}");
            return FlashbackExportForceRotatePreparation.Failure(result);
        }

        var forceRotateFallbackUsed = false;
        if (segmentPaths.Count == 0)
        {
            if (requireCompleteLiveEdge)
            {
                var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                var result = FinalizeResult.Failure(
                    outputPath,
                    "Flashback recording finalize failed: live-edge segment was not closed before timeout.",
                    preservedArtifacts);
                RecordLastFlashbackExportResult(exportId, result);
                CompleteFlashbackExportDiagnostics(exportId, result);
                Logger.Log(
                    "FLASHBACK_RECORDING_EXPORT_INCOMPLETE_FAIL " +
                    $"preserved_segments={preservedArtifacts.Count} " +
                    $"in_ms={(long)inPoint.TotalMilliseconds} " +
                    $"out_ms={(long)outPoint.TotalMilliseconds}");
                return FlashbackExportForceRotatePreparation.Failure(result);
            }

            // ForceRotate timed out (AV1 encoder can be too slow to drain
            // within the 3-second window). Completed segments before the
            // active one are already finalized - query them directly.
            // NOTE: The encoding thread may still be completing the rotation.
            // This returns only already-completed segments - the live-edge
            // segment may be missed if it hasn't been finalized yet. This is
            // acceptable: the previous behavior returned a near-empty file.
            segmentPaths = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
            if (segmentPaths is { Count: > 0 })
            {
                forceRotateFallbackUsed = true;
                RecordFlashbackExportForceRotateFallback(exportId, segmentPaths.Count, inPoint, outPoint);
                Logger.Log($"FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout segments={segmentPaths.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)outPoint.TotalMilliseconds}");
            }
            else
            {
                segmentPaths = null;
            }
        }

        return FlashbackExportForceRotatePreparation.Ready(segmentPaths, forceRotateFallbackUsed);
    }

    private static (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        ResolveFlashbackExportRangeAfterEvictionPaused(
            FlashbackBufferManager manager,
            TimeSpan? inPoint,
            TimeSpan? outPoint,
            TimeSpan? inPointFilePts,
            TimeSpan? outPointFilePts)
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
    }

    private static (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        ResolveFlashbackExportLastNRangeAfterEvictionPaused(FlashbackBufferManager manager, double seconds)
    {
        var bufferedDuration = manager.BufferedDuration;
        var validStart = manager.ValidStartPts;
        var rangeStart = bufferedDuration.TotalSeconds > seconds
            ? TimeSpan.FromSeconds(bufferedDuration.TotalSeconds - seconds)
            : TimeSpan.Zero;
        var fileInPoint = AddFlashbackPtsOffsetOrMax(rangeStart, validStart);
        return (true, fileInPoint, TimeSpan.MaxValue, null);
    }

    private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)
    {
        if (bufferedDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return position > bufferedDuration ? bufferedDuration : position;
    }

    private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)
    {
        if (position == TimeSpan.MaxValue || offset == TimeSpan.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (offset <= TimeSpan.Zero)
        {
            return position;
        }

        return position > TimeSpan.MaxValue - offset
            ? TimeSpan.MaxValue
            : position + offset;
    }

    private sealed record FlashbackExportPreparationResult(
        FlashbackExportRequest? Request,
        FinalizeResult? FailureResult,
        bool ForceRotateFallbackUsed)
    {
        public static FlashbackExportPreparationResult Ready(
            FlashbackExportRequest request,
            bool forceRotateFallbackUsed) =>
            new(request, null, forceRotateFallbackUsed);

        public static FlashbackExportPreparationResult Failure(FinalizeResult result) =>
            new(null, result, false);
    }

    private sealed record FlashbackExportForceRotatePreparation(
        IReadOnlyList<string>? SegmentPaths,
        FinalizeResult? FailureResult,
        bool ForceRotateFallbackUsed)
    {
        public static FlashbackExportForceRotatePreparation Ready(
            IReadOnlyList<string>? segmentPaths,
            bool forceRotateFallbackUsed) =>
            new(segmentPaths, null, forceRotateFallbackUsed);

        public static FlashbackExportForceRotatePreparation Failure(FinalizeResult result) =>
            new(null, result, false);
    }

}

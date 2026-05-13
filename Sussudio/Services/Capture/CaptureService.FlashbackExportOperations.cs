using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Flashback export operations: range export, last-N-seconds export, core export
// pipeline, and all diagnostics/progress tracking for in-flight exports.
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
        // Snapshot buffer state under the session lock, then release it.
        // PauseEviction (inside ExportFlashbackCoreAsync) protects segment files
        // from deletion — the session lock only needs to be held long enough to
        // read consistent references, not for the entire FFmpeg export.
        FlashbackBufferManager? bufferManager;
        FlashbackEncoderSink? flashbackSink;
        FlashbackExporter? flashbackExporter;
        var sessionLockHeld = false;
        var backendLeaseHeld = false;
        var exportOperationLockHeld = false;
        try
        {
            await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
            sessionLockHeld = true;

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
                return FailFlashbackExport(outputPath, "Flashback export is unavailable while Flashback is the active recording backend.");
            }

            await _flashbackBackendLeaseLock.WaitAsync(ct).ConfigureAwait(false);
            backendLeaseHeld = true;
            bufferManager = _flashbackBufferManager;
            flashbackSink = _flashbackSink;
            flashbackExporter = bufferManager != null
                ? _flashbackExporter ??= new FlashbackExporter()
                : _flashbackExporter;

            await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);
            exportOperationLockHeld = true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_SNAPSHOT_FAIL op=range type={ex.GetType().Name} msg='{ex.Message}'");
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            throw;
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            if (sessionLockHeld)
            {
                ReleaseSemaphoreBestEffort(_sessionTransitionLock, "flashback_export_snapshot_session");
            }
        }

        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: flashbackSink,
                snapshotBufferManager: bufferManager,
                snapshotExporter: flashbackExporter,
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

        // Same pattern: snapshot under lock, export outside it.
        FlashbackBufferManager? bufferManager;
        FlashbackEncoderSink? flashbackSink;
        FlashbackExporter? flashbackExporter;
        var sessionLockHeld = false;
        var backendLeaseHeld = false;
        var exportOperationLockHeld = false;
        try
        {
            await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
            sessionLockHeld = true;

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
                return FailFlashbackExport(outputPath, "Flashback export is unavailable while Flashback is the active recording backend.");
            }

            await _flashbackBackendLeaseLock.WaitAsync(ct).ConfigureAwait(false);
            backendLeaseHeld = true;
            bufferManager = _flashbackBufferManager;
            flashbackSink = _flashbackSink;
            flashbackExporter = bufferManager != null
                ? _flashbackExporter ??= new FlashbackExporter()
                : _flashbackExporter;

            await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);
            exportOperationLockHeld = true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_SNAPSHOT_FAIL op=last_n type={ex.GetType().Name} msg='{ex.Message}'");
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            throw;
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            if (sessionLockHeld)
            {
                ReleaseSemaphoreBestEffort(_sessionTransitionLock, "flashback_export_last_n_snapshot_session");
            }
        }

        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: flashbackSink,
                snapshotBufferManager: bufferManager,
                snapshotExporter: flashbackExporter,
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
    // (1) Export methods — pass snapshotSink/snapshotBufferManager captured under session lock.
    // (2) FinalizeFlashbackRecordingAsync — runs under session lock, omits snapshots (field reads safe).
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
        Func<FlashbackBufferManager, (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)>? resolveRangeAfterEvictionPaused = null)
    {
        var flashbackSink = snapshotSink ?? _flashbackSink;
        var bufferManager = snapshotBufferManager ?? _flashbackBufferManager;

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
                exporter = _flashbackExporter ??= new FlashbackExporter();
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

            FinalizeResult result;
            IReadOnlyList<string>? segmentPaths = null;
            string? tsPath = null;
            var forceRotateFallbackUsed = false;

            if (flashbackSink != null)
            {
                var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);
                segmentPaths = forceRotateResult.SegmentPaths;
                if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)
                {
                    var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                    result = FinalizeResult.Failure(
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
                    return result;
                }

                if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)
                {
                    var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                    result = FinalizeResult.Failure(
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
                    return result;
                }

                if (segmentPaths.Count == 0)
                {
                    if (requireCompleteLiveEdge)
                    {
                        var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                        result = FinalizeResult.Failure(
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
                        return result;
                    }

                    // ForceRotate timed out (AV1 encoder can be too slow to drain
                    // within the 3-second window). Completed segments before the
                    // active one are already finalized — query them directly.
                    // NOTE: The encoding thread may still be completing the rotation.
                    // This returns only already-completed segments — the live-edge
                    // segment may be missed if it hasn't been finalized yet. This is
                    // acceptable: the previous behavior returned a near-empty file.
                    segmentPaths = bufferManager?.GetValidSegmentPaths(inPoint, outPoint);
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
            }

            // Fallback: single-file export if no segments available
            if (segmentPaths == null)
            {
                tsPath = bufferManager?.ActiveFilePath;
                if (string.IsNullOrWhiteSpace(tsPath))
                {
                    result = FinalizeResult.Failure(outputPath, "Flashback buffer has no active file");
                    RecordLastFlashbackExportResult(exportId, result);
                    CompleteFlashbackExportDiagnostics(exportId, result);
                    return result;
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
            result = await exporter.ExportAsync(request, diagnosticProgress, ct).ConfigureAwait(false);
            if (forceRotateFallbackUsed && result.Succeeded)
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

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        TimeSpan? outPointFilePts = null)
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
        IProgress<ExportProgress>? progress, CancellationToken ct)
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

    private static IReadOnlyList<FlashbackExportSegment>? BuildFlashbackExportSegments(
        FlashbackBufferManager? bufferManager,
        IReadOnlyList<string>? segmentPaths)
    {
        if (segmentPaths is not { Count: > 0 })
        {
            return null;
        }

        var segmentInfo = bufferManager?.GetSegmentInfoList()
            .Where(segment => !segment.IsActive)
            .Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Segment, StringComparer.OrdinalIgnoreCase);
        var segments = new List<FlashbackExportSegment>(segmentPaths.Count);
        foreach (var path in segmentPaths)
        {
            var pathKey = TryGetFullPath(path);
            if (segmentInfo != null &&
                pathKey != null &&
                segmentInfo.TryGetValue(pathKey, out var info))
            {
                var startPts = FromSegmentMilliseconds(info.StartPtsMs);
                var endPts = FromSegmentMilliseconds(info.EndPtsMs);
                if (endPts < startPts)
                {
                    endPts = startPts;
                }

                segments.Add(new FlashbackExportSegment
                {
                    Path = path,
                    StartPts = startPts,
                    EndPts = endPts
                });
            }
            else
            {
                segments.Add(new FlashbackExportSegment { Path = path });
            }
        }

        return segments;
    }

    private static Func<int>? CreateFlashbackExportThrottleDelayProvider(
        FlashbackEncoderSink? flashbackSink,
        bool throttleHighResolutionBaseline = true)
    {
        if (flashbackSink == null)
        {
            return null;
        }

        var lastLoggedTick = 0L;
        return () =>
        {
            var capacity = flashbackSink.VideoQueueCapacityFrames;
            if (capacity <= 0)
            {
                return 0;
            }

            var depth = flashbackSink.VideoQueueCount;
            var queueRatio = Math.Clamp(depth / (double)capacity, 0.0, 1.0);
            var oldestFrameAgeMs = flashbackSink.VideoQueueOldestFrameAgeMs;
            var delayMs = ResolveFlashbackExportThrottleDelayMs(
                queueRatio,
                oldestFrameAgeMs,
                throttleHighResolutionBaseline && IsHighResolutionFlashbackExport(flashbackSink));
            if (delayMs <= 0)
            {
                return 0;
            }

            var now = Environment.TickCount64;
            if (now - lastLoggedTick >= 1_000)
            {
                lastLoggedTick = now;
                Logger.Log(
                    "FLASHBACK_EXPORT_LIVE_THROTTLE " +
                    $"delay_ms={delayMs} queue={depth}/{capacity} " +
                    $"queue_ratio={queueRatio:0.00} oldest_ms={oldestFrameAgeMs}");
            }

            return delayMs;
        };
    }

    private static bool IsHighResolutionFlashbackExport(FlashbackEncoderSink flashbackSink)
        => flashbackSink.EncoderWidth >= 3840 || flashbackSink.EncoderHeight >= 2160;

    private static int ResolveFlashbackExportThrottleDelayMs(
        double queueRatio,
        long oldestFrameAgeMs,
        bool liveHighResolution = false)
    {
        if (queueRatio >= 0.85 || oldestFrameAgeMs >= 90)
        {
            return 25;
        }

        if (queueRatio >= 0.70 || oldestFrameAgeMs >= 50)
        {
            return 20;
        }

        if (liveHighResolution)
        {
            return 25;
        }

        if (queueRatio >= 0.50 || oldestFrameAgeMs >= 30)
        {
            return 16;
        }

        return 0;
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PATH_NORMALIZE_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return null;
        }
    }

    private static TimeSpan FromSegmentMilliseconds(long milliseconds)
    {
        if (milliseconds <= 0)
        {
            return TimeSpan.Zero;
        }

        return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(milliseconds);
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

    private static bool IsFlashbackExportCancelled(string? statusMessage)
        => statusMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;

    internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return string.Empty;
        }

        if (IsFlashbackExportCancelled(statusMessage))
        {
            return "Cancelled";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "request is required") ||
            ContainsFlashbackExportFailureText(statusMessage, "duration must be finite"))
        {
            return "InvalidRequest";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "active recording backend"))
        {
            return "UnavailableDuringRecording";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "buffer not active"))
        {
            return "BufferInactive";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "in point") ||
            ContainsFlashbackExportFailureText(statusMessage, "export range"))
        {
            return "InvalidRange";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "output path") ||
            ContainsFlashbackExportFailureText(statusMessage, "output directory") ||
            ContainsFlashbackExportFailureText(statusMessage, "overwrite source"))
        {
            return "InvalidOutputPath";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avio_open2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_alloc_output_context2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_new_stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avcodec_parameters_copy") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_dict_set") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_write_header") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_interleaved_write_frame") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_write_trailer") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file length unavailable") ||
            ContainsFlashbackExportFailureText(statusMessage, "temporary export file was not created") ||
            ContainsFlashbackExportFailureText(statusMessage, "access is denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "permission denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "sharing violation"))
        {
            return "OutputWriteFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "rotation failed"))
        {
            return "ForceRotateFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "live-edge segment"))
        {
            return "IncompleteLiveEdge";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no segment paths") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment path") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment files") ||
            ContainsFlashbackExportFailureText(statusMessage, "readable segment"))
        {
            return "SegmentUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input file not found") ||
            ContainsFlashbackExportFailureText(statusMessage, "buffer has no active file"))
        {
            return "InputUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_open_input") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_read_frame"))
        {
            return "InputReadFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input context") ||
            ContainsFlashbackExportFailureText(statusMessage, "input had no streams") ||
            ContainsFlashbackExportFailureText(statusMessage, "stream count"))
        {
            return "InvalidInputStream";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no usable video stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "no segment had complete video parameters") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file is empty") ||
            ContainsFlashbackExportFailureText(statusMessage, "no video packets") ||
            ContainsFlashbackExportFailureText(statusMessage, "no packets"))
        {
            return "NoMediaWritten";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "disposed"))
        {
            return "Disposed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "timeout") ||
            ContainsFlashbackExportFailureText(statusMessage, "timed out"))
        {
            return "Timeout";
        }

        return "Failed";
    }

    private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)
        => statusMessage.Contains(value, StringComparison.OrdinalIgnoreCase);

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

using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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
                return FlashbackExportPreparationResult.Failure(result);
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
                return FlashbackExportPreparationResult.Failure(result);
            }

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
                    return FlashbackExportPreparationResult.Failure(result);
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
        }

        // Fallback: single-file export if no segments available
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
}

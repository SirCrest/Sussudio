using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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

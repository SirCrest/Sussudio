using System;
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

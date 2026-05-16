using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Flashback recording finalization helpers: export the live-edge recording and
// preserve cancellation semantics.
public partial class CaptureService
{
    private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(
        RecordingContext? recordingContext,
        FlashbackRecordingBoundarySnapshot recordingBoundary,
        CancellationToken cancellationToken)
    {
        var outputPath = recordingContext?.FinalOutputPath ?? string.Empty;

        // H3: Pause eviction BEFORE EndRecordingAsync to close the window where
        // eviction could delete segments between EndRecording (which resumes eviction
        // internally) and ExportFlashbackCoreAsync (which pauses it again).
        // With ref-counted eviction, the nested Pause from ExportFlashbackCoreAsync is safe.
        var backendLeaseHeld = false;
        try
        {
            await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            backendLeaseHeld = true;

            return await _flashbackBackend.FinalizeRecordingAsync(
                    outputPath,
                    captureBoundarySnapshot: sink => CaptureFlashbackRecordingBoundarySnapshot(sink, recordingBoundary),
                    exportRecordingAsync: (startPts, endPts, exportOutputPath, ct) =>
                        ExportFlashbackCoreAsync(
                            startPts,
                            endPts,
                            exportOutputPath,
                            progress: null,
                            ct: ct,
                            requireCompleteLiveEdge: true,
                            throttleHighResolutionBaseline: false),
                    resumeEvictionBestEffort: ResumeFlashbackEvictionBestEffort,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
        }
    }

    private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)
        => !result.Succeeded &&
           (string.Equals(result.StatusMessage, "Flashback export cancelled.", StringComparison.Ordinal) ||
            string.Equals(result.StatusMessage, "Flashback recording finalize cancelled.", StringComparison.Ordinal));
}

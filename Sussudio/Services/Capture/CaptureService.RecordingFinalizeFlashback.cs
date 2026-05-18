using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

// Flashback recording finalization helpers: capture the live-edge accounting
// boundary exactly once, export the live-edge recording, and preserve
// cancellation semantics.
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

    // Flashback recording boundary capture: snapshot the final live edge exactly
    // once so export/finalize and fallback paths share the same accounting data.
    private sealed class FlashbackRecordingBoundarySnapshot
    {
        public bool Captured { get; set; }
        public long RecordingFramesDelivered { get; set; }
        public long RecordingFramesEnqueued { get; set; }
        public RecordingIntegrityCounterSnapshot? Counters { get; set; }
        public RecordingAudioIntegrityCounterSnapshot? AudioCounters { get; set; }
    }

    private void CaptureFlashbackRecordingBoundarySnapshot(
        FlashbackEncoderSink flashbackSink,
        FlashbackRecordingBoundarySnapshot recordingBoundary)
    {
        if (recordingBoundary.Captured)
        {
            return;
        }

        var flashbackVideoCapture = _unifiedVideoCapture;
        if (flashbackVideoCapture != null)
        {
            flashbackVideoCapture.EndFlashbackRecordingAccounting();
            _lastMfSourceReaderFramesDelivered = flashbackVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = flashbackVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = flashbackVideoCapture.NegotiatedFormat;
            recordingBoundary.RecordingFramesDelivered = flashbackVideoCapture.RecordingFramesDelivered;
            recordingBoundary.RecordingFramesEnqueued = flashbackVideoCapture.VideoFramesWrittenToSink;
            Logger.Log(
                "VIDEO_DIAG flashback_recording_pipeline " +
                $"source_frames_during_recording={recordingBoundary.RecordingFramesDelivered} " +
                $"frames_accepted_by_flashback={recordingBoundary.RecordingFramesEnqueued} " +
                $"pipeline_drops={recordingBoundary.RecordingFramesDelivered - recordingBoundary.RecordingFramesEnqueued}");
        }

        recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, flashbackVideoCapture);
        recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(
            CaptureRecordingAudioCounters(_wasapiAudioCapture, flashbackSink, _activeRecordingSettings));
        recordingBoundary.Captured = true;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Unified Flashback recording stop/finalize path: finish the live-edge export,
// preserve/cycle the DVR backend, publish outcome state, and restore mic monitor.
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

        var flashbackVideoCapture = _videoPipeline.Capture;
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
            CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, flashbackSink, _recordingBackend.SettingsSnapshot));
        recordingBoundary.Captured = true;
    }

    private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync(CancellationToken cancellationToken)
    {
        var flashbackSink = _flashbackBackend.Sink!;
        var fbRecordingContext = _recordingBackend.DetachFlashbackBackend();
        var fbOutputPath = fbRecordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);
        var recordingBoundary = new FlashbackRecordingBoundarySnapshot();

        Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 1);
        // Do not clear the backend sink here; it continues serving the buffer.

        FinalizeResult fbResult;
        OperationCanceledException? flashbackCancellationException = null;
        try
        {
            try
            {
                fbResult = await FinalizeFlashbackRecordingAsync(
                        fbRecordingContext,
                        recordingBoundary,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 0);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            flashbackCancellationException = new OperationCanceledException(cancellationToken);
            fbResult = FinalizeResult.Failure(fbOutputPath, "Flashback recording finalize cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            fbResult = FinalizeResult.Failure(fbOutputPath, $"Flashback recording finalize failed: {ex.Message}");
        }

        CaptureFlashbackRecordingBoundarySnapshot(flashbackSink, recordingBoundary);

        if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))
        {
            flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
        }

        _lastRecordingIntegrity = BuildRecordingIntegritySummary(
            backend: "Flashback",
            recordingActive: false,
            finalizeSucceeded: fbResult.Succeeded,
            finalizeStatus: fbResult.StatusMessage,
            completedUtc: DateTimeOffset.UtcNow,
            sourceFrames: recordingBoundary.RecordingFramesDelivered,
            acceptedFrames: recordingBoundary.RecordingFramesEnqueued,
            counters: recordingBoundary.Counters ?? CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, _videoPipeline.Capture),
            audioCounters: recordingBoundary.AudioCounters ?? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, flashbackSink, _recordingBackend.SettingsSnapshot)));
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        LogRecordingIntegritySummary(_lastRecordingIntegrity);

        flashbackCancellationException = await ReconcileFlashbackBackendAfterRecordingFinalizeAsync(
            fbResult,
            flashbackCancellationException,
            cancellationToken).ConfigureAwait(false);

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
        _recordingBackend.ClearContextAndSettings();
        PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);

        // Restart mic monitoring if preview is still active
        try
        {
            await RestartMicrophoneMonitorAfterRecordingAsync(
                new MicrophoneMonitorRestartOptions(
                    OnlyWhenMissing: true,
                    FlashbackAttachReason: null,
                    RestartLogEvent: null,
                    DisposeWarningEvent: "FLASHBACK_MIC_RESTART_DISPOSE_WARN"),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        }

        if (fbResult.Succeeded)
        {
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_OK output='{fbResult.OutputPath}'");
        }
        else
        {
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_FAIL output='{fbResult.OutputPath}'");
        }
        if (flashbackCancellationException != null)
        {
            throw flashbackCancellationException;
        }

        return fbResult;
    }

    private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(
        FinalizeResult fbResult,
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!fbResult.Succeeded)
            {
                var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;
                _pendingFlashbackSettingsChange = false;
                _flashbackBackend.PreserveRecoverySegments("recording_finalize_failed");
                Logger.Log(
                    "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED " +
                    $"reason=recording_finalize_failed pending_settings={hadPendingFlashbackSettingsChange}");
            }
            else if (_pendingFlashbackSettingsChange)
            {
                _pendingFlashbackSettingsChange = false;
                Logger.Log("FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
                await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
                var unifiedVideoCapture = _videoPipeline.Capture;
                var settings = _currentSettings;
                if (_flashbackEnabled && unifiedVideoCapture != null && settings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await CycleFlashbackBufferAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            RecordLastFlashbackFailure(ex);
            _flashbackBackend.PreserveRecoverySegments("buffer_cycle_failed");
            BeginFlashbackBackendCleanup(ex);
        }

        return cancellationException;
    }
}

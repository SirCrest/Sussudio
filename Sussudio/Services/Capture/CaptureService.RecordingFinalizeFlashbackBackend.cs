using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Unified Flashback recording stop/finalize path: finish the live-edge export,
// preserve/cycle the DVR backend, publish outcome state, and restore mic monitor.
public partial class CaptureService
{
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
                CaptureRecordingAudioCounters(_wasapiAudioCapture, flashbackSink, _recordingBackend.SettingsSnapshot)));
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

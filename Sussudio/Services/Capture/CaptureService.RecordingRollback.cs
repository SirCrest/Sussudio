using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task RollbackRecordingStartAsync(RecordingStartRollbackState rollback, Exception ex)
    {
        Logger.Log($"CAPTURE_RECORDING_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        RecordLastRecordingFailure(ex);

        if (rollback.FlashbackRecordingStartedSink != null)
        {
            try
            {
                rollback.FlashbackRecordingStartedSink.CancelRecordingStartRollback("start_recording_failed");
            }
            catch (Exception rollbackEx)
            {
                Logger.Log($"FLASHBACK_RECORDING_START_ROLLBACK_WARN type={rollbackEx.GetType().Name} error='{rollbackEx.Message}'");
            }

            _unifiedVideoCapture?.EndFlashbackRecordingAccounting();
            if (_recordingBackend.IsFlashbackBackend(rollback.FlashbackRecordingStartedSink))
            {
                _recordingBackend.ClearActiveBackend();
            }
        }

        Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
        if (rollback.FlashbackRecordingBackendLeaseHeld)
        {
            rollback.FlashbackRecordingBackendLeaseHeld = false;
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start_fail");
        }

        if (rollback.SinkAttachedForAudioOnly && _wasapiAudioCapture != null)
        {
            _wasapiAudioCapture.DetachRecordingSink();
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        if (rollback.OwnedUnifiedVideoCapture != null)
        {
            DetachUnifiedVideoCapture(rollback.OwnedUnifiedVideoCapture);
        }

        try
        {
            await _artifactManager.RollbackAsync(rollback.RecordingContext).ConfigureAwait(false);
        }
        catch (Exception rollbackEx)
        {
            Logger.Log($"Recording start rollback cleanup failed: {rollbackEx.Message}");
        }

        try
        {
            await DisposeTransientRecordingBackendAsync(
                rollback.RecordingSink,
                rollback.OwnedWasapiAudioCapture,
                rollback.OwnedUnifiedVideoCapture).ConfigureAwait(false);
        }
        catch (Exception disposeEx)
        {
            Logger.Log($"Transient recording backend cleanup failed during start rollback: {disposeEx.Message}");
        }

        if (rollback.OwnedWasapiAudioCapture != null && ReferenceEquals(_wasapiAudioCapture, rollback.OwnedWasapiAudioCapture))
        {
            _previewAudioGraph.DetachCapture(
                rollback.OwnedWasapiAudioCapture,
                OnWasapiAudioLevelUpdated,
                OnWasapiCaptureFailed,
                _flashbackBackend.PlaybackController);
            _wasapiAudioCapture = null;
        }

        if (rollback.OwnedUnifiedVideoCapture != null && ReferenceEquals(_unifiedVideoCapture, rollback.OwnedUnifiedVideoCapture))
        {
            CacheMjpegTimingMetrics(rollback.OwnedUnifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = rollback.OwnedUnifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = rollback.OwnedUnifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = rollback.OwnedUnifiedVideoCapture.NegotiatedFormat;
            _videoPipeline.ClearCapture();
        }

        _recordingBackend.ClearContextAndSettings();
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        _isRecording = false;
        _recordingStopwatch.Reset();
        _mfConvertersDisabled = false;
    }

    private async Task DisposeTransientRecordingBackendAsync(
        IRecordingSink? sink,
        WasapiAudioCapture? wasapiCapture,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video recording stop failed during rollback: {ex.Message}");
            }
        }

        if (sink != null)
        {
            try
            {
                await sink.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink stop failed during rollback: {ex.Message}");
            }

            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink dispose failed during rollback: {ex.Message}");
            }
        }

        if (unifiedVideoCapture != null)
        {
            if (sink is LibAvRecordingSink libAvSink)
            {
                var libAvDrainTask = libAvSink.EncodingCompletionTask;
                if (!libAvDrainTask.IsCompleted)
                {
                    _recordingBackend.PendingLibAvDrainTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                        libAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_start_rollback");
                    unifiedVideoCapture = null;
                }
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video stop failed during rollback: {ex.Message}");
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video dispose failed during rollback: {ex.Message}");
            }
        }

        if (wasapiCapture != null)
        {
            try
            {
                await wasapiCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient WASAPI capture dispose failed during rollback: {ex.Message}");
            }
        }

    }
}

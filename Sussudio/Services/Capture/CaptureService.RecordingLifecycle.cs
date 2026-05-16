using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Recording, async transitionToken =>
        {
            EnsureInitialized();
            if (_isRecording)
            {
                return;
            }

            if (_currentDevice == null)
            {
                throw new InvalidOperationException("No selected video device is available for recording.");
            }

            transitionToken.ThrowIfCancellationRequested();
            _currentSettings = settings;
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            var rollback = new RecordingStartRollbackState();
            Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
            ThrowIfPendingLibAvDrainTaskBlocksReentry();
            try
            {
                await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken).ConfigureAwait(false);

                if (_flashbackEnabled && _flashbackSink != null)
                {
                    await StartFlashbackRecordingAsync(settings, transitionToken, rollback).ConfigureAwait(false);
                    return;
                }

                await StartLibAvRecordingAsync(settings, transitionToken, rollback).ConfigureAwait(false);
            }
            catch (Exception ex)
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
                    if (ReferenceEquals(_recordingSink, rollback.FlashbackRecordingStartedSink))
                    {
                        _recordingSink = null;
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
                    DetachWasapiAudioCapture(rollback.OwnedWasapiAudioCapture);
                    _wasapiAudioCapture = null;
                }

                if (rollback.OwnedUnifiedVideoCapture != null && ReferenceEquals(_unifiedVideoCapture, rollback.OwnedUnifiedVideoCapture))
                {
                    CacheMjpegTimingMetrics(rollback.OwnedUnifiedVideoCapture);
                    _lastMfSourceReaderFramesDelivered = rollback.OwnedUnifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = rollback.OwnedUnifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = rollback.OwnedUnifiedVideoCapture.NegotiatedFormat;
                    _unifiedVideoCapture = null;
                }

                _recordingContext = null;
                _activeRecordingSettings = null;
                _recordingIntegrityCounterBaseline = null;
                _recordingIntegrityAudioBaseline = null;
                _isRecording = false;
                _recordingStopwatch.Reset();
                _mfConvertersDisabled = false;
                throw;
            }
        }, cancellationToken);
}

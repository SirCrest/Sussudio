using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task DisposeUnusableFlashbackRecordingBackendAsync(CancellationToken transitionToken)
    {
        var flashbackSink = _flashbackBackend.Sink;
        if (_flashbackEnabled &&
            flashbackSink != null &&
            !flashbackSink.CanBeginRecording)
        {
            Logger.Log(
                "FLASHBACK_RECORDING_BACKEND_UNUSABLE_FALLBACK " +
                $"failed={flashbackSink.EncodingFailed} type={flashbackSink.EncodingFailureType ?? "None"}");
            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
        }
    }

    private async Task StartFlashbackRecordingAsync(
        CaptureSettings settings,
        CancellationToken transitionToken,
        RecordingStartRollbackState rollback)
    {
        var flashbackSink = _flashbackBackend.Sink
            ?? throw new InvalidOperationException("Flashback backend is not available for recording.");

        // Guard: if the existing flashback sink's pixel format no longer matches the
        // negotiated UVC format, reject the reuse path so the slow path rebuilds correctly.
        if (flashbackSink.IsP010 is bool recSinkIsP010 &&
            _unifiedVideoCapture != null &&
            recSinkIsP010 != _unifiedVideoCapture.IsP010)
        {
            Logger.Log(
                $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                $"existing_p010={recSinkIsP010} requested_p010={_unifiedVideoCapture.IsP010}");
            throw new InvalidOperationException(
                $"Flashback recording fast path: pixel-format mismatch — sink was built for " +
                $"{(recSinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                $"{(_unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                "Rebuild the flashback backend with the correct format.");
        }

        var fbOutputFolder = await OpenRecordingOutputFolderAsync(settings).ConfigureAwait(false);

        transitionToken.ThrowIfCancellationRequested();

        var fbEffectiveFrameRate = _unifiedVideoCapture?.Fps > 0 ? _unifiedVideoCapture.Fps : settings.FrameRate;
        var fbRecordingContext = await CreateFlashbackRecordingContextAsync(
            settings,
            fbOutputFolder,
            fbEffectiveFrameRate).ConfigureAwait(false);
        rollback.RecordingContext = fbRecordingContext;

        // If flashback settings changed while preview was stopped, rebuild
        // before recording so the retained backend matches the requested file.
        var flashbackBackendSettingsChanged = _flashbackBackend.SettingsSnapshot == null ||
            !CanReuseFlashbackBackend(_flashbackBackend.SettingsSnapshot, settings);
        var flashbackAudioTopologyChanged =
            flashbackSink.AudioEnabled != settings.AudioEnabled ||
            flashbackSink.MicrophoneEnabled != settings.MicrophoneEnabled;
        if (flashbackAudioTopologyChanged)
        {
            Logger.Log($"FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT " +
                $"audio={settings.AudioEnabled} (was {flashbackSink.AudioEnabled}) " +
                $"mic={settings.MicrophoneEnabled} (was {flashbackSink.MicrophoneEnabled})");
            EnsureFlashbackRecordingTopologyMatches(
                flashbackSink,
                settings.AudioEnabled,
                settings.MicrophoneEnabled);
        }

        if (flashbackBackendSettingsChanged)
        {
            Logger.Log($"FLASHBACK_SETTINGS_MISMATCH_AUTO_RESTART " +
                $"settings_changed={flashbackBackendSettingsChanged} " +
                $"audio={settings.AudioEnabled} " +
                $"mic={settings.MicrophoneEnabled}");

            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);

            var uvc = _unifiedVideoCapture;
            if (uvc != null)
            {
                await EnsureFlashbackPreviewBackendAsync(uvc, settings, transitionToken).ConfigureAwait(false);
            }

            flashbackSink = _flashbackBackend.Sink
                ?? throw new InvalidOperationException("Failed to restart flashback backend for updated recording settings.");
        }

        await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "recording_flashback_start").ConfigureAwait(false);
        await _flashbackBackendLeaseLock.WaitAsync(transitionToken).ConfigureAwait(false);
        rollback.FlashbackRecordingBackendLeaseHeld = true;
        Volatile.Write(ref _flashbackRecordingStartInProgress, 1);
        try
        {
            var activeFlashbackSink = flashbackSink;
            if (!activeFlashbackSink.CanBeginRecording)
            {
                throw new InvalidOperationException("Flashback backend is not healthy enough to begin recording.");
            }

            if (!activeFlashbackSink.WaitForForceRotateIdle(TimeSpan.FromSeconds(10)))
            {
                throw new InvalidOperationException("Flashback backend export rotation did not quiesce before recording start.");
            }

            if (!activeFlashbackSink.CanBeginRecording)
            {
                throw new InvalidOperationException("Flashback backend became unavailable before recording start.");
            }

            rollback.FlashbackRecordingStartedSink = activeFlashbackSink;
            _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeFlashbackSink);
            _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
                _wasapiAudioCapture,
                activeFlashbackSink,
                settings);
            activeFlashbackSink.BeginRecording(fbRecordingContext.FinalOutputPath);
            if (activeFlashbackSink.EncodingFailed)
            {
                throw new InvalidOperationException(
                    $"Flashback backend failed while starting recording: {activeFlashbackSink.EncodingFailureMessage ?? "unknown error"}");
            }

            _unifiedVideoCapture?.BeginFlashbackRecordingAccounting();
            _recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);
            ClearLastRecordingFailure();
            _isRecording = true;
            _flashbackRecordingStartBytes = _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;
            PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);
            _recordingStopwatch.Restart();
            StatusChanged?.Invoke(this, "Recording");
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_START output='{fbRecordingContext.FinalOutputPath}'");
        }
        finally
        {
            Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
            if (rollback.FlashbackRecordingBackendLeaseHeld)
            {
                rollback.FlashbackRecordingBackendLeaseHeld = false;
                ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start");
            }
        }
    }
}

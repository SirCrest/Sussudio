using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Audio-preview start/stop lifecycle, preview volume/mute, and WASAPI
// audio-level/failure event projection for the capture session.
public partial class CaptureService
{
    public void SetPreviewVolume(float volume)
    {
        _previewAudioGraph.SetPreviewVolume(volume);
    }

    public void SetMonitoringMuted(bool muted)
    {
        _previewAudioGraph.SetMonitoringMuted(muted);
    }

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            transitionToken.ThrowIfCancellationRequested();

            var createdCaptureForAudioPreview = false;
            // Create WASAPI capture if it wasn't started with the preview (audio was disabled at start)
            if (_previewAudioGraph.ProgramCapture == null && _currentDevice != null)
            {
                var audioId = _audioDeviceId ?? _currentDevice.AudioDeviceId;
                if (!string.IsNullOrEmpty(audioId))
                {
                    Logger.Log($"Late-starting WASAPI audio capture for device {audioId}");
                    var wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(audioId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _previewAudioGraph.ProgramCapture = wasapiCapture;
                    createdCaptureForAudioPreview = true;
                    ResetAvSyncDriftBaseline();
                    _previewAudioGraph.ResetCaptureFault();
                }
                else
                {
                    Logger.Log("Audio preview requested but no audio capture device is available.");
                }
            }

            if (_previewAudioGraph.ProgramCapture == null)
            {
                _isAudioPreviewActive = false;
                StatusChanged?.Invoke(this, "Audio preview unavailable");
                return;
            }

            _isAudioPreviewActive = true;
            try
            {
                AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture, "audio_preview_start");
                await _previewAudioGraph.StartPlaybackAsync(
                    transitionToken,
                    _flashbackBackend.PlaybackController).ConfigureAwait(false);
            }
            catch
            {
                _isAudioPreviewActive = false;
                if (createdCaptureForAudioPreview)
                {
                    var capture = _previewAudioGraph.ProgramCapture;
                    _previewAudioGraph.ProgramCapture = null;
                    _previewAudioGraph.DetachCapture(
                        capture,
                        OnWasapiAudioLevelUpdated,
                        OnWasapiCaptureFailed,
                        _flashbackBackend.PlaybackController);
                    if (capture != null)
                    {
                        try
                        {
                            await capture.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Log($"AUDIO_PREVIEW_START_ROLLBACK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                        }
                    }
                }

                throw;
            }

            StatusChanged?.Invoke(this, "Audio preview started");
        }, cancellationToken);

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => StopAudioPreviewCoreAsync(teardownCapture: false, cancellationToken);

    public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => StopAudioPreviewCoreAsync(teardownCapture: true, cancellationToken);

    private Task StopAudioPreviewCoreAsync(bool teardownCapture, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            _isAudioPreviewActive = false;
            _previewAudioGraph.StopPlayback(_flashbackBackend.PlaybackController);

            if (teardownCapture && !_isRecording)
            {
                var capture = _previewAudioGraph.ProgramCapture;
                _previewAudioGraph.ProgramCapture = null;
                _previewAudioGraph.DetachCapture(
                    capture,
                    OnWasapiAudioLevelUpdated,
                    OnWasapiCaptureFailed,
                    _flashbackBackend.PlaybackController);
                if (capture != null)
                {
                    Logger.Log("Tearing down WASAPI audio capture (audio disabled)");
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
            }

            AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }, cancellationToken);

    private void OnWasapiAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        AudioLevelUpdated?.Invoke(this, e);
    }

    private void OnWasapiCaptureFailed(object? sender, Exception ex)
    {
        var source = _previewAudioGraph.ClassifyCaptureFailureSource(sender);

        if (_isRecording)
        {
            _previewAudioGraph.RecordCaptureFault(source, ex);
        }

        Logger.Log($"WASAPI_CAPTURE_FAILED source={source} type={ex.GetType().Name} hr=0x{ex.HResult:X8} message={ex.Message} recording={_isRecording}");
        var statusPrefix = source == "microphone" ? "Microphone capture error" : "Audio capture error";
        StatusChanged?.Invoke(this, $"{statusPrefix}: {ex.Message}");
        ErrorOccurred?.Invoke(this, ex);
    }
}

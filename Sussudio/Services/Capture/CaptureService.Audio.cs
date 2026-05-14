using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Audio preview, microphone monitoring, and WASAPI playback routing for the
// capture session. Recording/preview callers still serialize through the root
// transition lock; this partial owns the audio-specific resource order.
public partial class CaptureService
{
    public void SetPreviewVolume(float volume)
    {
        _previewVolume = Math.Clamp(volume, 0f, 1f);
        if (!_isMonitoringMuted)
        {
            var playback = _wasapiAudioPlayback;
            playback?.SetVolume(_previewVolume);
        }
    }

    public void SetMonitoringMuted(bool muted)
    {
        _isMonitoringMuted = muted;
        var playback = _wasapiAudioPlayback;
        playback?.SetVolume(muted ? 0f : _previewVolume);
    }

    private void OnWasapiAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        AudioLevelUpdated?.Invoke(this, e);
    }

    private void OnWasapiCaptureFailed(object? sender, Exception ex)
    {
        var source = ReferenceEquals(sender, _wasapiAudioCapture)
            ? "program"
            : ReferenceEquals(sender, _microphoneCapture)
                ? "microphone"
                : "unknown";

        if (_isRecording)
        {
            Volatile.Write(ref _wasapiAudioCaptureFaulted, true);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, $"{source}: {ex.Message}");
        }

        Logger.Log($"WASAPI_CAPTURE_FAILED source={source} type={ex.GetType().Name} hr=0x{ex.HResult:X8} message={ex.Message} recording={_isRecording}");
        var statusPrefix = source == "microphone" ? "Microphone capture error" : "Audio capture error";
        StatusChanged?.Invoke(this, $"{statusPrefix}: {ex.Message}");
        ErrorOccurred?.Invoke(this, ex);
    }

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            transitionToken.ThrowIfCancellationRequested();

            var createdCaptureForAudioPreview = false;
            // Create WASAPI capture if it wasn't started with the preview (audio was disabled at start)
            if (_wasapiAudioCapture == null && _currentDevice != null)
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
                    _wasapiAudioCapture = wasapiCapture;
                    createdCaptureForAudioPreview = true;
                    _avSyncBaselineDriftMs = double.NaN;
                    Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                    Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
                }
                else
                {
                    Logger.Log("Audio preview requested but no audio capture device is available.");
                }
            }

            if (_wasapiAudioCapture == null)
            {
                _isAudioPreviewActive = false;
                StatusChanged?.Invoke(this, "Audio preview unavailable");
                return;
            }

            _isAudioPreviewActive = true;
            try
            {
                AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "audio_preview_start");
                await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
            }
            catch
            {
                _isAudioPreviewActive = false;
                if (createdCaptureForAudioPreview)
                {
                    var capture = _wasapiAudioCapture;
                    _wasapiAudioCapture = null;
                    DetachWasapiAudioCapture(capture);
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
            StopWasapiPlayback();

            if (teardownCapture && !_isRecording)
            {
                var capture = _wasapiAudioCapture;
                _wasapiAudioCapture = null;
                DetachWasapiAudioCapture(capture);
                if (capture != null)
                {
                    Logger.Log("Tearing down WASAPI audio capture (audio disabled)");
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
            }

            AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }, cancellationToken);

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            var previousDeviceId = _audioDeviceId;
            var previousDeviceName = _audioDeviceName;

            if (string.Equals(previousDeviceId, audioDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                _audioDeviceName = audioDeviceName;
                return;
            }

            if (_wasapiAudioCapture == null)
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                return;
            }

            Logger.Log($"Live audio input switch: {audioDeviceName ?? "(card default)"}");

            var activeSink = _isRecording ? _recordingSink : null;
            var oldCapture = _wasapiAudioCapture;
            var committedSwitchToken = CancellationToken.None;

            var resolvedId = audioDeviceId ?? _currentDevice?.AudioDeviceId;
            if (!string.IsNullOrEmpty(resolvedId))
            {
                var newCapture = new WasapiAudioCapture();
                try
                {
                    await newCapture.InitializeAsync(resolvedId, committedSwitchToken).ConfigureAwait(false);
                    newCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    newCapture.CaptureFailed += OnWasapiCaptureFailed;
                    newCapture.Start();
                }
                catch
                {
                    _audioDeviceId = previousDeviceId;
                    _audioDeviceName = previousDeviceName;
                    try
                    {
                        newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        newCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        await newCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                    }

                    throw;
                }

                DetachWasapiAudioCapture(oldCapture);
                _wasapiAudioCapture = newCapture;
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);

                AttachFlashbackAudioIfSupported(newCapture, "audio_input_switch");

                if (activeSink != null && !ReferenceEquals(activeSink, _flashbackSink))
                {
                    newCapture.AttachRecordingSink(activeSink);
                }

                try
                {
                    if (_isAudioPreviewActive)
                    {
                        await StartWasapiPlaybackAsync(committedSwitchToken).ConfigureAwait(false);
                    }

                    Logger.Log($"Audio input switched to: {audioDeviceName ?? resolvedId}");
                }
                finally
                {
                    try
                    {
                        await oldCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                    }
                }
            }
            else
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                _isAudioPreviewActive = false;
                _wasapiAudioCapture = null;
                DetachWasapiAudioCapture(oldCapture);
                try
                {
                    await oldCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log("Audio input cleared - no device available");
                AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            }

            if (transitionToken.IsCancellationRequested)
            {
                Logger.Log("AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");
                transitionToken.ThrowIfCancellationRequested();
            }
        }, cancellationToken);
}

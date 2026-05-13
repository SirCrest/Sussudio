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

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }

    private async Task DisposeMicrophoneCaptureAsync()
    {
        var mic = _microphoneCapture;
        _microphoneCapture = null;
        if (mic != null)
        {
            try
            {
                try
                {
                    mic.SetAudioWriter(null);
                }
                catch (Exception detachEx)
                {
                    Logger.Log($"MIC_MONITOR_WRITER_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}");
                }

                mic.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                mic.CaptureFailed -= OnWasapiCaptureFailed;
                await mic.DisposeAsync().ConfigureAwait(false);
                Logger.Log("MIC_MONITOR_STOP");
            }
            catch (Exception ex)
            {
                Logger.Log("Microphone capture dispose failed: " + ex.Message);
            }
        }
    }

    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            var previousEnabled = _micMonitorEnabled;
            var previousDeviceId = _micMonitorDeviceId;
            var previousDeviceName = _micMonitorDeviceName;
            WasapiAudioCapture? nextMicCapture = null;
            try
            {
                transitionToken.ThrowIfCancellationRequested();
                if (_isRecording)
                {
                    _micMonitorEnabled = enabled;
                    _micMonitorDeviceId = deviceId;
                    _micMonitorDeviceName = deviceName;
                    Logger.Log("MIC_MONITOR_UPDATE_DEFERRED recording=true");
                    return;
                }

                if (enabled && !_isRecording && _isVideoPreviewActive && !string.IsNullOrWhiteSpace(deviceId))
                {
                    nextMicCapture = new WasapiAudioCapture();
                    await nextMicCapture.InitializeAsync(deviceId, transitionToken).ConfigureAwait(false);
                    nextMicCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                    nextMicCapture.CaptureFailed += OnWasapiCaptureFailed;
                    nextMicCapture.Start();
                    if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                    {
                        nextMicCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                    }
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                _micMonitorEnabled = enabled;
                _micMonitorDeviceId = deviceId;
                _micMonitorDeviceName = deviceName;
                _microphoneCapture = nextMicCapture;
                nextMicCapture = null;

                if (_microphoneCapture != null)
                {
                    Logger.Log("MIC_MONITOR_START device='" + (deviceName ?? "?") + "'");
                }
                else
                {
                    MicrophoneAudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
                }
            }
            catch
            {
                _micMonitorEnabled = previousEnabled;
                _micMonitorDeviceId = previousDeviceId;
                _micMonitorDeviceName = previousDeviceName;
                if (nextMicCapture != null)
                {
                    try
                    {
                        nextMicCapture.SetAudioWriter(null);
                        nextMicCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                        nextMicCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        await nextMicCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Microphone capture rollback dispose failed: " + ex.Message);
                    }
                }

                throw;
            }
        }, cancellationToken);

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

    private async Task StartWasapiPlaybackAsync(CancellationToken cancellationToken)
    {
        var capture = _wasapiAudioCapture;
        if (capture == null)
        {
            return;
        }

        var playback = _wasapiAudioPlayback;
        if (playback == null)
        {
            var newPlayback = new WasapiAudioPlayback();
            try
            {
                await newPlayback.InitializeAsync(cancellationToken).ConfigureAwait(false);
                newPlayback.SetVolume(0);
                newPlayback.Start();
                _wasapiAudioPlayback = newPlayback;
                Logger.Log("WASAPI audio playback started.");
                newPlayback.SetVolume(_isMonitoringMuted ? 0f : _previewVolume);
                playback = newPlayback;
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                if (ReferenceEquals(_wasapiAudioPlayback, newPlayback))
                {
                    _wasapiAudioPlayback = null;
                }
                StopWasapiPlaybackBestEffort(newPlayback, "start_fail");
                DisposeWasapiPlaybackBestEffort(newPlayback);
                throw;
            }
        }

        try
        {
            capture.SetPlayback(playback);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
            StopWasapiPlayback();
            throw;
        }

        // Update flashback controller with audio components (they weren't available
        // during flashback init because WASAPI starts after flashback).
        var controller = _flashbackPlaybackController;
        controller?.UpdateAudioComponents(playback, capture);
    }

    private void StopWasapiPlayback()
    {
        var fbController = _flashbackPlaybackController;
        fbController?.UpdateAudioComponents(null, null);
        var playback = _wasapiAudioPlayback;
        _wasapiAudioPlayback = null;
        SafeClearWasapiCapturePlayback(_wasapiAudioCapture, "stop_playback");
        if (playback != null)
        {
            StopWasapiPlaybackBestEffort(playback, "stop_playback");
            DisposeWasapiPlaybackBestEffort(playback);
        }
    }

    private void DetachWasapiAudioCapture(WasapiAudioCapture? capture)
    {
        if (capture == null)
        {
            StopWasapiPlayback();
            return;
        }

        capture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
        capture.CaptureFailed -= OnWasapiCaptureFailed;
        SafeClearWasapiCapturePlayback(capture, "detach_capture");
        StopWasapiPlayback();
    }

    private static void SafeClearWasapiCapturePlayback(WasapiAudioCapture? capture, string operation)
    {
        if (capture == null)
        {
            return;
        }

        try
        {
            capture.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback detach warning op={operation}: {ex.Message}");
        }
    }

    private static void DisposeWasapiPlaybackBestEffort(WasapiAudioPlayback playback)
    {
        try
        {
            playback.Dispose();
            Logger.Log("WASAPI audio playback disposed.");
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback dispose warning: {ex.Message}");
        }
    }

    private static void StopWasapiPlaybackBestEffort(WasapiAudioPlayback playback, string operation)
    {
        try
        {
            playback.Stop();
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback stop warning op={operation}: {ex.Message}");
        }
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

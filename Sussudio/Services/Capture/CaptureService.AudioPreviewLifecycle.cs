using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Capture audio lifecycle: audio-preview start/stop, live input switching,
// preview volume/mute, and WASAPI audio-level/failure event projection.
public partial class CaptureService
{
    private readonly record struct MicrophoneMonitorRestartOptions(
        bool OnlyWhenMissing,
        string? FlashbackAttachReason,
        string? RestartLogEvent,
        string DisposeWarningEvent);

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

    private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(
        CaptureSettings settings,
        string? audioDeviceId,
        CancellationToken transitionToken)
    {
        WasapiAudioCapture? wasapiCapture = null;
        try
        {
            if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))
            {
                wasapiCapture = new WasapiAudioCapture();
                await wasapiCapture.InitializeAsync(audioDeviceId, transitionToken).ConfigureAwait(false);
                wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                wasapiCapture.Start();
                _previewAudioGraph.ProgramCapture = wasapiCapture;
            }
            else if (settings.AudioEnabled)
            {
                Logger.Log("Audio preview requested but no audio capture device is available; continuing with video-only preview.");
            }

            if (_isAudioPreviewActive && _previewAudioGraph.ProgramCapture != null)
            {
                await _previewAudioGraph.StartPlaybackAsync(
                    transitionToken,
                    _flashbackBackend.PlaybackController).ConfigureAwait(false);
            }

            Logger.Log(
                _previewAudioGraph.ProgramCapture != null
                    ? "Preview backend active: IMFSourceReader video + WASAPI audio ingest."
                    : "Preview backend active: IMFSourceReader video only (no audio capture endpoint).");

            await StartPreviewMicrophoneMonitorAsync(transitionToken).ConfigureAwait(false);

            return wasapiCapture;
        }
        catch
        {
            await RollbackPreviewAudioCaptureStartupAsync(wasapiCapture).ConfigureAwait(false);
            throw;
        }
    }

    private async Task StartPreviewMicrophoneMonitorAsync(CancellationToken transitionToken)
    {
        // Start mic monitoring if enabled (metering only, no recording sink)
        if (!_micMonitorEnabled || string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            return;
        }

        WasapiAudioCapture? micCapture = null;
        try
        {
            micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(_micMonitorDeviceId, transitionToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            micCapture.Start();
            if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
            {
                micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_mic_monitor_start'");
            }

            _previewAudioGraph.MicrophoneCapture = micCapture;
            micCapture = null;
            Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
        }
        catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception micEx)
        {
            Logger.Log("Mic monitor start failed (non-fatal): " + micEx.Message);
        }
        finally
        {
            if (micCapture != null)
            {
                micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                try
                {
                    await micCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception disposeEx)
                {
                    Logger.Log($"MIC_MONITOR_PREVIEW_START_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                }
            }
        }
    }

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }

    private async Task DisposeMicrophoneCaptureAsync()
    {
        var mic = _previewAudioGraph.MicrophoneCapture;
        _previewAudioGraph.MicrophoneCapture = null;
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
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
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
                    if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
                    {
                        nextMicCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                    }
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                _micMonitorEnabled = enabled;
                _micMonitorDeviceId = deviceId;
                _micMonitorDeviceName = deviceName;
                _previewAudioGraph.MicrophoneCapture = nextMicCapture;
                nextMicCapture = null;

                if (_previewAudioGraph.MicrophoneCapture != null)
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

    private async Task RestartMicrophoneMonitorAfterRecordingAsync(
        MicrophoneMonitorRestartOptions options,
        CancellationToken cancellationToken)
    {
        if (!_isVideoPreviewActive || !_micMonitorEnabled || string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            return;
        }

        if (options.OnlyWhenMissing && _previewAudioGraph.MicrophoneCapture != null)
        {
            return;
        }

        WasapiAudioCapture? micCapture = null;
        try
        {
            micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            micCapture.Start();
            if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
            {
                micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                if (!string.IsNullOrWhiteSpace(options.FlashbackAttachReason))
                {
                    Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
                }
            }

            _previewAudioGraph.MicrophoneCapture = micCapture;
            micCapture = null;
            if (!string.IsNullOrWhiteSpace(options.RestartLogEvent))
            {
                Logger.Log($"{options.RestartLogEvent} device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
        }
        finally
        {
            if (micCapture != null)
            {
                micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                catch (Exception disposeEx) { Logger.Log($"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            }
        }
    }

    private async Task RollbackPreviewAudioCaptureStartupAsync(WasapiAudioCapture? wasapiCapture)
    {
        if (wasapiCapture != null)
        {
            wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
            wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
        }

        var capture = _previewAudioGraph.ProgramCapture ?? wasapiCapture;
        _previewAudioGraph.ProgramCapture = null;
        if (capture != null)
        {
            _previewAudioGraph.DetachCapture(
                capture,
                OnWasapiAudioLevelUpdated,
                OnWasapiCaptureFailed,
                _flashbackBackend.PlaybackController);
            try
            {
                await capture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
                Logger.Log($"WASAPI capture rollback dispose warning: {disposeEx.Message}");
            }
        }
    }

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

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            var previousDeviceId = _audioDeviceId;
            var previousDeviceName = _audioDeviceName;

            if (string.Equals(previousDeviceId, audioDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                _audioDeviceName = audioDeviceName;
                return;
            }

            if (_previewAudioGraph.ProgramCapture == null)
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                return;
            }

            Logger.Log($"Live audio input switch: {audioDeviceName ?? "(card default)"}");

            var activeSink = _isRecording ? _recordingBackend.Sink : null;
            var oldCapture = _previewAudioGraph.ProgramCapture;
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

                _previewAudioGraph.DetachCapture(
                    oldCapture,
                    OnWasapiAudioLevelUpdated,
                    OnWasapiCaptureFailed,
                    _flashbackBackend.PlaybackController);
                _previewAudioGraph.ProgramCapture = newCapture;
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                _previewAudioGraph.ResetCaptureFault();

                AttachFlashbackAudioIfSupported(newCapture, "audio_input_switch");

                if (activeSink != null && !ReferenceEquals(activeSink, _flashbackBackend.Sink))
                {
                    newCapture.AttachRecordingSink(activeSink);
                }

                try
                {
                    if (_isAudioPreviewActive)
                    {
                        await _previewAudioGraph.StartPlaybackAsync(
                            committedSwitchToken,
                            _flashbackBackend.PlaybackController).ConfigureAwait(false);
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
                _previewAudioGraph.ProgramCapture = null;
                _previewAudioGraph.DetachCapture(
                    oldCapture,
                    OnWasapiAudioLevelUpdated,
                    OnWasapiCaptureFailed,
                    _flashbackBackend.PlaybackController);
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio, microphone, and device-audio observable property change handlers.
/// </summary>
public partial class MainViewModel
{
    partial void OnIsMicrophoneEnabledChanged(bool value)
    {
        SaveSettings();
        if (_suppressMicrophoneMonitorUpdate)
        {
            return;
        }

        if (!IsRecording)
        {
            var device = SelectedMicrophoneDevice;
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(value, device?.Id, device?.Name),
                "mic monitor toggle");
        }
    }

    partial void OnIsCustomAudioInputEnabledChanged(bool value)
    {
        if (IsRecording)
        {
            Logger.Log("Custom audio input change ignored while recording");
            return;
        }

        if (value)
        {
            if (AudioInputDevices.Count == 0)
            {
                Logger.Log("Custom audio input enabled but no audio devices found");
                IsCustomAudioInputEnabled = false;
                return;
            }

            if (SelectedAudioInputDevice == null)
            {
                SelectedAudioInputDevice = AudioInputDevices[0];
            }
        }

        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio toggle"), "custom audio toggle");
        SaveSettings();
    }

    partial void OnSelectedAudioInputDeviceChanged(AudioInputDevice? value)
    {
        if (IsRecording)
        {
            return;
        }

        if (!IsCustomAudioInputEnabled || value == null)
        {
            return;
        }

        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio device change"), "custom audio device change");
        SaveSettings();
    }

    partial void OnSelectedMicrophoneDeviceChanged(AudioInputDevice? value)
    {
        if (value != null)
        {
            try
            {
                var pendingSavedVolume = _pendingSavedMicrophoneVolume;
                var pendingSavedVolumeDeviceId = _pendingSavedMicrophoneVolumeDeviceId;
                if (pendingSavedVolume.HasValue &&
                    string.Equals(value.Id, pendingSavedVolumeDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    _pendingSavedMicrophoneVolume = null;
                    _pendingSavedMicrophoneVolumeDeviceId = null;
                    var savedVolume = Math.Clamp(pendingSavedVolume.Value, 0.0, 100.0);
                    if (Math.Abs(MicrophoneVolume - savedVolume) > 0.5)
                    {
                        MicrophoneVolume = savedVolume;
                    }
                    else
                    {
                        SetMicrophoneEndpointVolume(savedVolume);
                    }
                }
                else
                {
                    _pendingSavedMicrophoneVolume = null;
                    _pendingSavedMicrophoneVolumeDeviceId = null;
                    var endpointVolume = GetMicrophoneEndpointVolume();
                    if (Math.Abs(MicrophoneVolume - endpointVolume) > 0.5)
                    {
                        MicrophoneVolume = endpointVolume;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainViewModel mic volume readback: {ex.Message}");
            }
        }

        SaveSettings();

        // Update mic monitoring when device changes
        if (IsMicrophoneEnabled && !IsRecording && value != null)
        {
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(true, value.Id, value.Name),
                "mic monitor device switch");
        }
    }

    partial void OnSelectedDeviceAudioModeChanged(string value)
    {
        if (_isLoadingSettings || _isRefreshingDeviceAudioControls || !IsDeviceAudioControlSupported)
        {
            return;
        }

        if (IsRecording)
        {
            Logger.Log("Device audio mode change ignored while recording");
            return;
        }
        var oldCts = _deviceAudioModeCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var targetDevice = SelectedDevice;
        _deviceAudioModeCts = cts;
        var enqueued = EnqueueUiOperation(async () =>
        {
            try
            {
                if (Volatile.Read(ref _disposeState) == 0)
                {
                    await ApplyDeviceAudioModeAsync("device audio mode change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Device audio mode change canceled because selected device changed");
            }
            finally
            {
                if (ReferenceEquals(_deviceAudioModeCts, cts))
                {
                    _deviceAudioModeCts = null;
                }

                cts.Dispose();
            }
        }, "device audio mode change", allowDuringDispose: true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioModeCts, cts))
            {
                _deviceAudioModeCts = null;
            }

            cts.Dispose();
        }
        SaveSettings();
    }

    partial void OnAnalogAudioGainPercentChanged(double value)
    {
        if (_isLoadingSettings || _isRefreshingDeviceAudioControls || !IsDeviceAudioControlSupported)
        {
            return;
        }

        if (IsRecording)
        {
            Logger.Log("Analog audio gain change ignored while recording");
            return;
        }

        if (!string.Equals(SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            SaveSettings();
            return;
        }

        // Debounce the XU write to avoid flooding the hardware with commands
        // while the user drags the slider (same hazard class as AT SET bricking).
        var targetDevice = SelectedDevice;
        if (targetDevice == null)
        {
            SaveSettings();
            return;
        }
        var oldCts = _gainXuDebounceCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _gainXuDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(200, token).ConfigureAwait(false);
                var enqueued = EnqueueUiOperation(async () =>
                {
                    try
                    {
                        if (Volatile.Read(ref _disposeState) == 0)
                        {
                            await ApplyAnalogAudioGainAsync("analog audio gain change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log("Analog audio gain change canceled because selected device changed");
                    }
                    finally
                    {
                        if (ReferenceEquals(_gainXuDebounceCts, cts))
                        {
                            _gainXuDebounceCts = null;
                        }

                        cts.Dispose();
                    }
                }, "analog audio gain change", allowDuringDispose: true);
                if (!enqueued)
                {
                    if (ReferenceEquals(_gainXuDebounceCts, cts))
                    {
                        _gainXuDebounceCts = null;
                    }

                    cts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(_gainXuDebounceCts, cts))
                {
                    _gainXuDebounceCts = null;
                }

                cts.Dispose();
            }
        });
        SaveSettings();
    }

    partial void OnIsAudioEnabledChanged(bool value)
    {
        Logger.Log($"Audio capture enabled: {value}");
        var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);

        if (value)
        {
            // Re-enable audio preview and start it if we're already previewing
            if (!IsAudioPreviewEnabled)
            {
                _suppressAudioPreviewEnabledChangeOperation = true;
                try
                {
                    IsAudioPreviewEnabled = true;
                }
                finally
                {
                    _suppressAudioPreviewEnabledChangeOperation = false;
                }
            }

            if (IsPreviewing && IsInitialized)
            {
                EnqueueUiOperation(async () =>
                {
                    if (changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled)
                    {
                        Logger.Log($"AUDIO_TOGGLE_SKIP op=enable stale_generation={changeGeneration}");
                        return;
                    }

                    // Cycle the flashback encoder so it reconnects its audio feed.
                    // Without this, the first recording after audio off->on produces
                    // an empty file because the flashback sink's audio path is stale.
                    var settings = BuildCaptureSettings();
                    await SetAudioMonitoringEnabledWithVolumeTransitionAsync(
                        true,
                        "audio_capture_enable",
                        teardownCapture: false,
                        afterMonitoringStarted: () => _sessionCoordinator.RestartFlashbackAsync(settings));
                }, "audio preview restart + flashback cycle");
            }
        }
        else
        {
            if (IsAudioPreviewEnabled)
            {
                _suppressAudioPreviewEnabledChangeOperation = true;
                try
                {
                    IsAudioPreviewEnabled = false;
                }
                finally
                {
                    _suppressAudioPreviewEnabledChangeOperation = false;
                }
            }

            EnqueueUiOperation(async () =>
            {
                if (changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled)
                {
                    Logger.Log($"AUDIO_TOGGLE_SKIP op=disable stale_generation={changeGeneration}");
                    return;
                }

                await SetAudioMonitoringEnabledWithVolumeTransitionAsync(false, "audio_capture_disable", teardownCapture: true);
            }, "audio capture teardown");

            ResetAudioMeter();
        }

        SaveSettings();
    }
}

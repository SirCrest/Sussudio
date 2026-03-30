using System;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;

namespace ElgatoCapture.ViewModels;

/// <summary>
/// Audio control state, persistence, and device-specific audio mode management.
/// </summary>
public partial class MainViewModel
{
    internal bool SuppressVolumeSave { get; set; }

    /// <summary>
    /// When non-null, SaveSettings writes this value for PreviewVolume instead of the
    /// current (animation-transient) property value. Set during the entrance volume
    /// fade-in to prevent intermediate 0 values from corrupting persisted settings.
    /// </summary>
    internal double? VolumeSaveOverride { get; set; }

    partial void OnPreviewVolumeChanged(double value)
    {
        _sessionCoordinator.SetPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));
    }

    partial void OnMicrophoneVolumeChanged(double value)
    {
        try
        {
            SetMicrophoneEndpointVolume(value);
        }
        catch (Exception ex)
        {
            Logger.Log($"OnMicrophoneVolumeChanged failed: {ex.Message}");
        }
    }

    internal void SavePreviewVolume() => SaveSettings();

    internal void SaveMicrophoneVolume() => SaveSettings();

    public void SetMicrophoneEndpointVolume(double volumePercent)
    {
        var deviceId = SelectedMicrophoneDevice?.Id;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        try
        {
            WasapiComInterop.SetEndpointVolume(deviceId, (float)(Math.Clamp(volumePercent, 0.0, 100.0) / 100.0));
        }
        catch (Exception ex)
        {
            Logger.Log($"SetMicrophoneEndpointVolume failed for device '{deviceId}': {ex.Message}");
        }
    }

    public double GetMicrophoneEndpointVolume()
    {
        var deviceId = SelectedMicrophoneDevice?.Id;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return 100.0;
        }

        try
        {
            return WasapiComInterop.GetEndpointVolume(deviceId) * 100.0;
        }
        catch (Exception ex)
        {
            Logger.Log($"GetMicrophoneEndpointVolume failed for device '{deviceId}': {ex.Message}");
            return 100.0;
        }
    }

    partial void OnIsAudioPreviewEnabledChanged(bool value)
    {
        if (value && !IsAudioEnabled)
        {
            Logger.Log("Audio preview requested but audio capture is disabled");
            IsAudioPreviewEnabled = false;
            return;
        }

        if (!value && !IsRecording)
        {
            ResetAudioMeter();
        }

        if (IsPreviewing && IsInitialized)
        {
            var description = value ? "audio monitoring enable" : "audio monitoring mute";
            EnqueueUiOperation(() => _sessionCoordinator.UpdateAudioMonitoringAsync(value), description);
        }

        SaveSettings();
    }

    private async Task ApplyAudioInputSelectionAsync(string reason)
    {
        if (!IsInitialized)
        {
            return;
        }

        string? audioDeviceId = null;
        string? audioDeviceName = null;

        if (IsCustomAudioInputEnabled)
        {
            audioDeviceId = SelectedAudioInputDevice?.Id;
            audioDeviceName = SelectedAudioInputDevice?.Name;
        }
        else
        {
            audioDeviceId = SelectedDevice?.AudioDeviceId;
            audioDeviceName = SelectedDevice?.AudioDeviceName;
        }

        Logger.Log($"=== Updating audio input ({reason}) ===");
        Logger.Log($"  Audio device: {audioDeviceName ?? "(none)"}");

        await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);
    }

    private async Task RefreshDeviceAudioControlsAsync(bool applySavedState)
    {
        var device = SelectedDevice;
        if (device == null)
        {
            WithAudioControlRefreshSuppressed(() =>
            {
                IsDeviceAudioControlSupported = false;
                SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;
                AnalogAudioGainPercent = 50;
            });

            return;
        }

        if (NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out _, out _))
        {
            WithAudioControlRefreshSuppressed(() => IsDeviceAudioControlSupported = true);
        }

        var state = await _deviceAudioControlService.ReadStateAsync(device).ConfigureAwait(false);
        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = state.IsSupported;
            if (state.IsSupported)
            {
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(state.Mode ?? _pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
                AnalogAudioGainPercent = Math.Clamp(
                    state.AnalogGainPercent ?? _pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent,
                    0.0,
                    100.0);
            }
            else
            {
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(_pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
                AnalogAudioGainPercent = Math.Clamp(_pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
            }
        });

        if (!applySavedState || !state.IsSupported)
        {
            return;
        }

        var desiredMode = NormalizeDeviceAudioMode(_pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
        var desiredGain = Math.Clamp(_pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        _pendingSavedDeviceAudioMode = null;
        _pendingSavedAnalogAudioGainPercent = null;

        Logger.Log($"NATIVEXU_AUDIO_RESTORE_READ_ONLY desired='{desiredMode}' device='{state.Mode}'");

        var refreshedState = await _deviceAudioControlService.ReadStateAsync(device).ConfigureAwait(false);
        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = refreshedState.IsSupported;
            SelectedDeviceAudioMode = NormalizeDeviceAudioMode(refreshedState.Mode ?? desiredMode);
            AnalogAudioGainPercent = Math.Clamp(refreshedState.AnalogGainPercent ?? desiredGain, 0.0, 100.0);
        });
    }

    private async Task ApplyDeviceAudioModeAsync(
        string reason,
        string? explicitMode = null,
        bool reapplyAnalogGain = true,
        bool persistSettings = true)
    {
        var device = SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return;
        }

        var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);
        Logger.Log($"=== Updating device audio mode ({reason}) ===");
        Logger.Log($"  Mode: {mode}");

        var isAnalog = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var gainByte = MapPercentToGainByte(AnalogAudioGainPercent);
        var applied = await NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte).ConfigureAwait(false);

        if (!applied)
        {
            await _deviceAudioControlService.ReadStateAsync(device).ConfigureAwait(false);
            var revertMode = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)
                ? DeviceAudioMode.Hdmi
                : DeviceAudioMode.Analog;
            WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = revertMode);

            StatusText = $"Device audio mode change failed ({mode})";
            return;
        }

        StatusText = $"Device audio mode set to {mode}";
        if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            await ApplyAnalogAudioGainAsync("analog gain after mode switch", AnalogAudioGainPercent, persistSettings: false).ConfigureAwait(false);
        }

        WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);

        if (persistSettings)
        {
            SaveSettings();
        }
    }

    private async Task ApplyAnalogAudioGainAsync(
        string reason,
        double? explicitPercent = null,
        bool persistSettings = true)
    {
        var device = SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return;
        }

        var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        var gainByte = MapPercentToGainByte(gainPercent);
        Logger.Log($"=== Updating analog audio gain ({reason}) ===");
        Logger.Log($"  GainPercent: {gainPercent:0} GainByte: 0x{gainByte:X2}");

        var applied = await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false).ConfigureAwait(false);

        if (!applied)
        {
            StatusText = $"Analog audio gain change failed ({gainPercent:0}%)";
            return;
        }

        StatusText = $"Analog audio gain set to {gainPercent:0}%";
        WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);

        _gainFlashDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _gainFlashDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, cts.Token).ConfigureAwait(false);
                if (!cts.Token.IsCancellationRequested)
                {
                    await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                /* Superseded by a newer gain change - expected */
            }
        }, cts.Token);

        if (persistSettings)
        {
            SaveSettings();
        }
    }

    private void WithAudioControlRefreshSuppressed(Action action)
    {
        _isRefreshingDeviceAudioControls = true;
        try
        {
            action();
        }
        finally
        {
            _isRefreshingDeviceAudioControls = false;
        }
    }

    private string NormalizeDeviceAudioMode(string? mode)
        => string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)
            ? DeviceAudioMode.Analog
            : DeviceAudioMode.Hdmi;

    private async Task<bool> TryApplyAtDeviceAudioModeAsync(CaptureDevice device, string mode)
    {
        var analogMode = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var desiredSource = analogMode ? 1 : 0;

        var currentSource = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, 0x35, "InputSourceCheck").ConfigureAwait(false);
        if (currentSource is { Length: >= 1 } && currentSource[0] == desiredSource)
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_AT_SKIP mode='{mode}' already={desiredSource}");
            return true;
        }

        var wasPreviewing = IsPreviewing;
        if (wasPreviewing)
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_AT_STOP_PREVIEW mode='{mode}'");
            try
            {
                await StopPreviewAsync(userInitiated: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"NATIVEXU_AUDIO_MODE_AT_STOP_PREVIEW_WARN error={ex.Message}");
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        var inputApplied = await NativeXuAtCommandProvider.SetInputSourceAsync(device, desiredSource).ConfigureAwait(false);
        Logger.Log($"NATIVEXU_AUDIO_MODE_AT mode='{mode}' inputApplied={inputApplied}");

        if (wasPreviewing)
        {
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var delayMs = attempt * 1000;
                Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_PREVIEW mode='{mode}' attempt={attempt} delayMs={delayMs}");
                await Task.Delay(delayMs).ConfigureAwait(false);
                try
                {
                    await RefreshDevicesAsync().ConfigureAwait(false);
                    await StartPreviewAsync(userInitiated: false).ConfigureAwait(false);
                    Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_OK attempt={attempt}");
                    break;
                }
                catch (Exception ex) when (attempt < 5)
                {
                    Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_RETRY attempt={attempt} error={ex.Message}");
                }
            }
        }

        return inputApplied;
    }

    private const double GainCurveK = 4.0;

    private static byte MapPercentToGainByte(double percent)
    {
        var x = Math.Clamp(percent / 100.0, 0.0, 1.0);
        var curved = Math.Log(1.0 + x * (Math.Exp(GainCurveK) - 1.0)) / GainCurveK;
        return (byte)Math.Clamp(Math.Round(curved * 255.0), 0, 255);
    }

    private static double MapGainByteToPercent(byte gainByte)
    {
        var y = gainByte / 255.0;
        var x = (Math.Exp(GainCurveK * y) - 1.0) / (Math.Exp(GainCurveK) - 1.0);
        return Math.Clamp(x * 100.0, 0.0, 100.0);
    }
}

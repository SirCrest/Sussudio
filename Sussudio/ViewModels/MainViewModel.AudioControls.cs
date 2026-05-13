using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Microphone endpoint volume and device-specific audio mode management.
/// </summary>
public partial class MainViewModel
{
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

    private async Task RefreshDeviceAudioControlsAsync(
        CaptureDevice? targetDevice,
        bool applySavedState,
        CancellationToken cancellationToken)
    {
        var device = targetDevice;
        if (device == null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SelectedDevice != null)
            {
                return;
            }

            WithAudioControlRefreshSuppressed(() =>
            {
                IsDeviceAudioControlSupported = false;
                SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;
                AnalogAudioGainPercent = 50;
            });

            return;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            return;
        }

        if (NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out _, out _))
        {
            WithAudioControlRefreshSuppressed(() => IsDeviceAudioControlSupported = true);
        }

        var state = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log("Device audio controls refresh ignored because selected device changed");
            return;
        }

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

        Logger.Log($"NATIVEXU_AUDIO_RESTORE_READ_ONLY desired='{desiredMode}' device='{state.Mode}'");

        var refreshedState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log("Device audio controls restore ignored because selected device changed");
            return;
        }

        _pendingSavedDeviceAudioMode = null;
        _pendingSavedAnalogAudioGainPercent = null;
        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = refreshedState.IsSupported;
            SelectedDeviceAudioMode = NormalizeDeviceAudioMode(refreshedState.Mode ?? desiredMode);
            AnalogAudioGainPercent = Math.Clamp(refreshedState.AnalogGainPercent ?? desiredGain, 0.0, 100.0);
        });
    }

    private async Task<bool> ApplyDeviceAudioModeAsync(
        string reason,
        string? explicitMode = null,
        bool reapplyAnalogGain = true,
        bool persistSettings = true,
        CaptureDevice? targetDevice = null,
        CancellationToken cancellationToken = default)
    {
        var device = targetDevice ?? SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return false;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Device audio mode skipped because selected device changed ({reason})");
            return false;
        }

        var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);
        Logger.Log($"=== Updating device audio mode ({reason}) ===");
        Logger.Log($"  Mode: {mode}");

        var isAnalog = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var gainByte = MapPercentToGainByte(AnalogAudioGainPercent);
        var applied = await NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte, cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentSelectedDevice(device))
            {
                Logger.Log($"Device audio mode failure ignored because selected device changed ({reason})");
                return false;
            }

            var failureState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentSelectedDevice(device))
            {
                Logger.Log($"Device audio mode failure readback ignored because selected device changed ({reason})");
                return false;
            }

            WithAudioControlRefreshSuppressed(() =>
            {
                IsDeviceAudioControlSupported = failureState.IsSupported;
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(failureState.Mode ?? SelectedDeviceAudioMode);
                if (failureState.AnalogGainPercent.HasValue)
                {
                    AnalogAudioGainPercent = Math.Clamp(failureState.AnalogGainPercent.Value, 0.0, 100.0);
                }
            });

            StatusText = $"Device audio mode change failed ({mode})";
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Device audio mode result ignored because selected device changed ({reason})");
            return false;
        }

        StatusText = $"Device audio mode set to {mode}";
        if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            var gainApplied = await ApplyAnalogAudioGainAsync(
                "analog gain after mode switch",
                AnalogAudioGainPercent,
                persistSettings: false,
                targetDevice: device,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!gainApplied)
            {
                return false;
            }
        }

        WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }

    private async Task<bool> ApplyAnalogAudioGainAsync(
        string reason,
        double? explicitPercent = null,
        bool persistSettings = true,
        CaptureDevice? targetDevice = null,
        CancellationToken cancellationToken = default)
    {
        var device = targetDevice ?? SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return false;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Analog audio gain skipped because selected device changed ({reason})");
            return false;
        }

        var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        var gainByte = MapPercentToGainByte(gainPercent);
        Logger.Log($"=== Updating analog audio gain ({reason}) ===");
        Logger.Log($"  GainPercent: {gainPercent:0} GainByte: 0x{gainByte:X2}");

        var applied = await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            StatusText = $"Analog audio gain change failed ({gainPercent:0}%)";
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Analog audio gain result ignored because selected device changed ({reason})");
            return false;
        }

        StatusText = $"Analog audio gain set to {gainPercent:0}%";
        WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);

        var oldCts = _gainFlashDebounceCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _gainFlashDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested && IsCurrentSelectedDevice(device))
                {
                    await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                /* Superseded by a newer gain change - expected */
            }
            finally
            {
                if (ReferenceEquals(_gainFlashDebounceCts, cts))
                {
                    _gainFlashDebounceCts = null;
                }

                cts.Dispose();
            }
        });

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }

    private bool IsCurrentSelectedDevice(CaptureDevice device)
    {
        var selected = SelectedDevice;
        if (selected == null)
        {
            return false;
        }

        return string.Equals(selected.Id, device.Id, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(selected.NativeXuInterfacePath, device.NativeXuInterfacePath, StringComparison.OrdinalIgnoreCase);
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
                // Native XU audio-mode change requires a full pipeline rebuild to pick
                // up the new input source — force teardown rather than the normal
                // preview-only detach.
                await StopPreviewAsync(userInitiated: false, teardownPipeline: true).ConfigureAwait(false);
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

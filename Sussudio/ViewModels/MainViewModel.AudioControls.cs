using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Device-specific audio mode and analog gain management.
/// </summary>
public partial class MainViewModel
{
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
        var gainByte = DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent);
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

}

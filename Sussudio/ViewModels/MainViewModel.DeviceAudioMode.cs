using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Device-native audio mode switching and failure readback.
/// </summary>
public partial class MainViewModel
{
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
}

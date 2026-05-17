using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Device-native analog gain application and deferred flash persistence.
/// </summary>
public partial class MainViewModel
{
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
        var gainByte = DeviceAudioGainMapper.PercentToGainByte(gainPercent);
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
        RequestAnalogGainFlashPersist(device, gainByte);

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }
}

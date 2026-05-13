using System;
using Sussudio.Services.Audio;

namespace Sussudio.ViewModels;

/// <summary>
/// Microphone endpoint volume synchronization and persistence hooks.
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
}

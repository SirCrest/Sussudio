using System;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Microphone observable property change handlers.
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

        // Update mic monitoring when device changes.
        if (IsMicrophoneEnabled && !IsRecording && value != null)
        {
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(true, value.Id, value.Name),
                "mic monitor device switch");
        }
    }
}

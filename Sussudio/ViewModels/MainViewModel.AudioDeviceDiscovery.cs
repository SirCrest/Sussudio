using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Watcher-driven audio endpoint refresh flow. Video device enumeration and
/// selected-device capability rebuilds stay in MainViewModel.DeviceManagement.cs.
/// </summary>
public partial class MainViewModel
{
    private void OnAudioDevicesChanged()
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            _ = RefreshAudioDeviceListAsync();
        }))
        {
            Logger.Log("AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        }
    }

    private List<AudioInputDevice> FilterOutCaptureCardAudio(List<AudioInputDevice> devices)
    {
        var excludeId = SelectedDevice?.AudioDeviceId;
        if (string.IsNullOrWhiteSpace(excludeId))
        {
            return devices;
        }

        return devices.Where(d => !string.Equals(d.Id, excludeId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task RefreshAudioDeviceListAsync()
    {
        try
        {
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousMicrophoneId = SelectedMicrophoneDevice?.Id;
            var audioDevices = FilterOutCaptureCardAudio(
                (await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync()).ToList());

            ReplaceCollection(AudioInputDevices, audioDevices);
            ReplaceCollection(MicrophoneDevices, audioDevices);
            var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
            SelectedAudioInputDevice =
                AudioInputDevices.FirstOrDefault(d => d.Id == previousAudioId)
                ?? AudioInputDevices.FirstOrDefault();
            SelectedMicrophoneDevice =
                MicrophoneDevices.FirstOrDefault(d => d.Id == previousMicrophoneId)
                ?? (!string.IsNullOrWhiteSpace(savedMicrophoneId) ? MicrophoneDevices.FirstOrDefault(d => d.Id == savedMicrophoneId) : null)
                ?? MicrophoneDevices.FirstOrDefault();

            Logger.Log($"Audio device list refreshed ({AudioInputDevices.Count} devices).");
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio device list refresh failed: {ex.Message}");
        }
    }
}

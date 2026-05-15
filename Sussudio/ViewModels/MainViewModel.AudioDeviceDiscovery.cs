using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio endpoint list projection for both startup scans and watcher-driven
/// refreshes. Video device enumeration and selected-device capability rebuilds
/// stay in MainViewModel.DeviceManagement.cs.
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

    private void ApplyStartupAudioDeviceScan(
        List<AudioInputDevice> audioDevices,
        IReadOnlyList<CaptureDevice> videoDevices,
        string? previousDeviceId,
        string? previousAudioId,
        string? previousMicrophoneId)
    {
        var captureCardAudioId = (videoDevices.FirstOrDefault(d => d.Id == previousDeviceId) ?? videoDevices.FirstOrDefault())?.AudioDeviceId;
        var filteredAudio = FilterOutCaptureCardAudio(audioDevices, captureCardAudioId);
        ReplaceCollection(AudioInputDevices, filteredAudio);
        ReplaceCollection(MicrophoneDevices, filteredAudio);

        var savedAudioId = _pendingSavedAudioDeviceId;
        _pendingSavedAudioDeviceId = null;
        var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
        _pendingSavedMicrophoneDeviceId = null;

        SelectedAudioInputDevice =
            AudioInputDevices.FirstOrDefault(d => d.Id == previousAudioId)
            ?? (!string.IsNullOrWhiteSpace(savedAudioId) ? AudioInputDevices.FirstOrDefault(d => d.Id == savedAudioId) : null)
            ?? AudioInputDevices.FirstOrDefault();
        SelectedMicrophoneDevice =
            MicrophoneDevices.FirstOrDefault(d => d.Id == previousMicrophoneId)
            ?? (!string.IsNullOrWhiteSpace(savedMicrophoneId) ? MicrophoneDevices.FirstOrDefault(d => d.Id == savedMicrophoneId) : null)
            ?? MicrophoneDevices.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(savedAudioId) && SelectedAudioInputDevice?.Id != savedAudioId)
        {
            Logger.Log($"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.");
        }

        if (!string.IsNullOrWhiteSpace(savedMicrophoneId) && SelectedMicrophoneDevice?.Id != savedMicrophoneId)
        {
            Logger.Log($"SETTINGS_RESTORE: saved microphone device '{savedMicrophoneId}' not found, using fallback.");
        }
    }

    private List<AudioInputDevice> FilterOutCaptureCardAudio(List<AudioInputDevice> devices)
        => FilterOutCaptureCardAudio(devices, SelectedDevice?.AudioDeviceId);

    private static List<AudioInputDevice> FilterOutCaptureCardAudio(List<AudioInputDevice> devices, string? excludeId)
    {
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

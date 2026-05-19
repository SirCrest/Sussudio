using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio endpoint list projection for both startup scans and watcher-driven
/// refreshes. Capture-device refresh orchestration lives in
/// MainViewModelDeviceRefreshController; selected-device capability rebuilds
/// stay in MainViewModel.DeviceSelection.cs.
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
        var savedAudioId = _pendingSavedAudioDeviceId;
        _pendingSavedAudioDeviceId = null;
        var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
        _pendingSavedMicrophoneDeviceId = null;
        var selection = AudioDeviceSelectionPolicy.SelectStartup(
            audioDevices,
            videoDevices,
            previousDeviceId,
            previousAudioId,
            savedAudioId,
            previousMicrophoneId,
            savedMicrophoneId);

        ReplaceCollection(AudioInputDevices, selection.AvailableDevices);
        ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);
        SelectedAudioInputDevice = selection.SelectedAudioInputDevice;
        SelectedMicrophoneDevice = selection.SelectedMicrophoneDevice;

        if (selection.ShouldLogSavedAudioFallback)
        {
            Logger.Log($"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.");
        }

        if (selection.ShouldLogSavedMicrophoneFallback)
        {
            Logger.Log($"SETTINGS_RESTORE: saved microphone device '{savedMicrophoneId}' not found, using fallback.");
        }
    }

    private async Task RefreshAudioDeviceListAsync()
    {
        try
        {
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousMicrophoneId = SelectedMicrophoneDevice?.Id;
            var audioDevices = (await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync()).ToList();
            var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
            var selection = AudioDeviceSelectionPolicy.SelectRefresh(
                audioDevices,
                SelectedDevice?.AudioDeviceId,
                previousAudioId,
                previousMicrophoneId,
                savedMicrophoneId);

            ReplaceCollection(AudioInputDevices, selection.AvailableDevices);
            ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);
            SelectedAudioInputDevice = selection.SelectedAudioInputDevice;
            SelectedMicrophoneDevice = selection.SelectedMicrophoneDevice;

            Logger.Log($"Audio device list refreshed ({AudioInputDevices.Count} devices).");
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio device list refresh failed: {ex.Message}");
        }
    }
}

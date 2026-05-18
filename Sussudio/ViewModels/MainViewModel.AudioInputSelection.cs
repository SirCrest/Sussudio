using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio input property-change retargeting and preview-monitoring ramp handoff.
/// </summary>
public partial class MainViewModel
{
    partial void OnIsCustomAudioInputEnabledChanged(bool value)
    {
        if (IsRecording)
        {
            Logger.Log("Custom audio input change ignored while recording");
            return;
        }

        if (value)
        {
            if (AudioInputDevices.Count == 0)
            {
                Logger.Log("Custom audio input enabled but no audio devices found");
                IsCustomAudioInputEnabled = false;
                return;
            }

            if (SelectedAudioInputDevice == null)
            {
                SelectedAudioInputDevice = AudioInputDevices[0];
            }
        }

        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio toggle"), "custom audio toggle");
        SaveSettings();
    }

    partial void OnSelectedAudioInputDeviceChanged(AudioInputDevice? value)
    {
        if (IsRecording)
        {
            return;
        }

        if (!IsCustomAudioInputEnabled || value == null)
        {
            return;
        }

        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio device change"), "custom audio device change");
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

        var shouldRampMonitoring = IsPreviewing && _captureService.IsAudioPreviewActive;
        var volumeTarget = _previewAudioVolumeTransitionController.PersistedVolumeTarget;
        var traceSessionId = shouldRampMonitoring
            ? BeginAudioRampTraceSession(reason, volumeTarget)
            : 0;
        try
        {
            if (shouldRampMonitoring)
            {
                await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);
            }

            await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);
            RecordAudioRampTracePoint("audio-input-updated", reason, volumeTarget);

            if (shouldRampMonitoring)
            {
                if (_captureService.IsAudioPreviewActive && IsAudioEnabled && IsAudioPreviewEnabled)
                {
                    await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);
                }
                else
                {
                    RestorePreviewVolumeAfterUnavailableAudio(volumeTarget, reason);
                }
            }
        }
        finally
        {
            if (shouldRampMonitoring)
            {
                CompleteAudioRampTraceSession(traceSessionId, reason);
            }
        }
    }
}

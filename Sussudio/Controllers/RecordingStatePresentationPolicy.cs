using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal static class RecordingStatePresentationPolicy
{
    internal static RecordingStatePresentationState Build(RecordingStatePresentationInput input)
    {
        var isIdle = !input.IsRecording;
        var isAnalogAudioMode = string.Equals(
            input.SelectedDeviceAudioMode,
            DeviceAudioMode.Analog,
            StringComparison.OrdinalIgnoreCase);

        return new RecordingStatePresentationState(
            AudioRecordToggleEnabled: isIdle,
            CustomAudioToggleEnabled: isIdle,
            MicrophoneToggleEnabled: isIdle,
            AudioInputComboBoxEnabled: input.IsCustomAudioInputEnabled && isIdle,
            MicrophoneComboBoxEnabled: input.IsMicrophoneEnabled && isIdle,
            DeviceAudioModeToggleEnabled: input.IsDeviceAudioControlSupported && isIdle,
            AnalogAudioGainSliderEnabled: input.IsDeviceAudioControlSupported && isAnalogAudioMode && isIdle,
            TransitionRecordButtonEnabled: !input.IsRecordingTransitioning,
            FfmpegRecordButtonEnabled: !input.IsFfmpegMissing && !input.IsRecordingTransitioning,
            TransitionStartingContentActive: input.IsRecordingTransitioning,
            SettledNormalContentVisible: !input.IsRecording,
            SettledRecordingContentVisible: input.IsRecording);
    }
}

internal readonly record struct RecordingStatePresentationInput(
    bool IsRecording,
    bool IsRecordingTransitioning,
    bool IsFfmpegMissing,
    bool IsCustomAudioInputEnabled,
    bool IsMicrophoneEnabled,
    bool IsDeviceAudioControlSupported,
    string? SelectedDeviceAudioMode);

internal readonly record struct RecordingStatePresentationState(
    bool AudioRecordToggleEnabled,
    bool CustomAudioToggleEnabled,
    bool MicrophoneToggleEnabled,
    bool AudioInputComboBoxEnabled,
    bool MicrophoneComboBoxEnabled,
    bool DeviceAudioModeToggleEnabled,
    bool AnalogAudioGainSliderEnabled,
    bool TransitionRecordButtonEnabled,
    bool FfmpegRecordButtonEnabled,
    bool TransitionStartingContentActive,
    bool SettledNormalContentVisible,
    bool SettledRecordingContentVisible);

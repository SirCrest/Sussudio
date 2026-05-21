using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

internal static partial class MainViewModelSettingsPersistenceProjection
{
    internal static UserSettings BuildSaveSettings(MainViewModelSettingsSaveInput input)
    {
        return new UserSettings
        {
            SelectedDeviceId = input.SelectedDeviceId,
            OutputPath = input.OutputPath,
            SelectedRecordingFormat = input.SelectedRecordingFormat,
            SelectedQuality = input.SelectedQuality,
            SelectedPreset = input.SelectedPreset,
            SelectedSplitEncodeMode = input.SelectedSplitEncodeMode,
            CustomBitrateMbps = input.CustomBitrateMbps,
            IsHdrEnabled = input.IsHdrEnabled,
            IsAudioEnabled = input.IsAudioEnabled,
            IsAudioPreviewEnabled = input.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = input.IsCustomAudioInputEnabled,
            SelectedAudioInputDeviceId = input.SelectedAudioInputDeviceId,
            IsMicrophoneEnabled = input.IsMicrophoneEnabled,
            SelectedMicrophoneDeviceId = input.SelectedMicrophoneDeviceId,
            MicrophoneVolume = input.MicrophoneVolume,
            PreviewVolume = input.PreviewVolume,
            ShowAllCaptureOptions = input.ShowAllCaptureOptions,
            IsStatsVisible = input.IsStatsVisible,
            SelectedDeviceAudioMode = input.SelectedDeviceAudioMode,
            AnalogAudioGainPercent = input.AnalogAudioGainPercent,
            FlashbackGpuDecode = input.FlashbackGpuDecode,
            FlashbackBufferMinutes = input.FlashbackBufferMinutes,
        };
    }
}

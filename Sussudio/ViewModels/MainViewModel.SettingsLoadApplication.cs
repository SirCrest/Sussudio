namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.OutputPath is not null)
        {
            OutputPath = loadPlan.OutputPath;
        }

        if (loadPlan.SelectedRecordingFormat is not null)
        {
            SelectedRecordingFormat = loadPlan.SelectedRecordingFormat;
        }

        if (loadPlan.SelectedQuality is not null)
        {
            SelectedQuality = loadPlan.SelectedQuality;
        }

        if (loadPlan.SelectedPreset is not null)
        {
            SelectedPreset = loadPlan.SelectedPreset;
        }

        if (loadPlan.SelectedSplitEncodeMode is not null)
        {
            SelectedSplitEncodeMode = loadPlan.SelectedSplitEncodeMode;
        }

        if (loadPlan.CustomBitrateMbps.HasValue)
        {
            CustomBitrateMbps = loadPlan.CustomBitrateMbps.Value;
        }

        if (loadPlan.IsHdrEnabled.HasValue)
        {
            IsHdrEnabled = loadPlan.IsHdrEnabled.Value;
        }

        if (loadPlan.IsAudioEnabled.HasValue)
        {
            IsAudioEnabled = loadPlan.IsAudioEnabled.Value;
        }

        if (loadPlan.IsAudioPreviewEnabled.HasValue)
        {
            IsAudioPreviewEnabled = loadPlan.IsAudioPreviewEnabled.Value;
        }

        if (loadPlan.IsCustomAudioInputEnabled.HasValue)
        {
            IsCustomAudioInputEnabled = loadPlan.IsCustomAudioInputEnabled.Value;
        }

        if (loadPlan.IsMicrophoneEnabled.HasValue)
        {
            IsMicrophoneEnabled = loadPlan.IsMicrophoneEnabled.Value;
        }

        if (loadPlan.MicrophoneVolume.HasValue)
        {
            MicrophoneVolume = loadPlan.MicrophoneVolume.Value;
            _pendingSavedMicrophoneVolume = loadPlan.MicrophoneVolume.Value;
            _pendingSavedMicrophoneVolumeDeviceId = loadPlan.PendingMicrophoneVolumeDeviceId;
        }

        if (loadPlan.PreviewVolume.HasValue)
        {
            PreviewVolume = loadPlan.PreviewVolume.Value;
        }

        if (loadPlan.ShowAllCaptureOptions.HasValue)
        {
            ShowAllCaptureOptions = loadPlan.ShowAllCaptureOptions.Value;
        }

        if (loadPlan.IsStatsVisible.HasValue)
        {
            IsStatsVisible = loadPlan.IsStatsVisible.Value;
        }

        if (loadPlan.SelectedDeviceAudioMode is not null)
        {
            SelectedDeviceAudioMode = loadPlan.SelectedDeviceAudioMode;
        }

        if (loadPlan.AnalogAudioGainPercent.HasValue)
        {
            AnalogAudioGainPercent = loadPlan.AnalogAudioGainPercent.Value;
        }

        if (loadPlan.FlashbackGpuDecode.HasValue)
        {
            FlashbackGpuDecode = loadPlan.FlashbackGpuDecode.Value;
        }

        if (loadPlan.FlashbackBufferMinutes.HasValue)
        {
            FlashbackBufferMinutes = loadPlan.FlashbackBufferMinutes.Value;
        }

        // Defer device selection until RefreshDevicesAsync populates the device list.
        _pendingSavedDeviceId = loadPlan.PendingDeviceId;
        _pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;
        _pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;
        _pendingSavedDeviceAudioMode = loadPlan.PendingDeviceAudioMode;
        _pendingSavedAnalogAudioGainPercent = loadPlan.PendingAnalogAudioGainPercent;
    }
}

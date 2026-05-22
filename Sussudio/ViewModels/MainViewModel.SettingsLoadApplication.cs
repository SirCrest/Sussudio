namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        ApplyRecordingSettingsLoadPlan(loadPlan);
        ApplyAudioSettingsLoadPlan(loadPlan);
        ApplyUiSettingsLoadPlan(loadPlan);
        ApplyDeviceAudioSettingsLoadPlan(loadPlan);
        ApplyFlashbackSettingsLoadPlan(loadPlan);
        StageDeferredDeviceSettingsLoadPlan(loadPlan);
    }

    private void ApplyRecordingSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
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
    }

    private void ApplyAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
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
    }

    private void ApplyUiSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.IsStatsVisible.HasValue)
        {
            IsStatsVisible = loadPlan.IsStatsVisible.Value;
        }
    }

    private void ApplyDeviceAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.SelectedDeviceAudioMode is not null)
        {
            SelectedDeviceAudioMode = loadPlan.SelectedDeviceAudioMode;
        }

        if (loadPlan.AnalogAudioGainPercent.HasValue)
        {
            AnalogAudioGainPercent = loadPlan.AnalogAudioGainPercent.Value;
        }
    }

    private void ApplyFlashbackSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.FlashbackGpuDecode.HasValue)
        {
            FlashbackGpuDecode = loadPlan.FlashbackGpuDecode.Value;
        }

        if (loadPlan.FlashbackBufferMinutes.HasValue)
        {
            FlashbackBufferMinutes = loadPlan.FlashbackBufferMinutes.Value;
        }
    }

    private void StageDeferredDeviceSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        // Defer device selection until RefreshDevicesAsync populates the device list.
        _pendingSavedDeviceId = loadPlan.PendingDeviceId;
        _pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;
        _pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;
        _pendingSavedDeviceAudioMode = loadPlan.PendingDeviceAudioMode;
        _pendingSavedAnalogAudioGainPercent = loadPlan.PendingAnalogAudioGainPercent;
    }
}

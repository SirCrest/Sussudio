using System;
using System.IO;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Settings initialization and load/save projection between persisted user settings and ViewModel state.
/// </summary>
public partial class MainViewModel
{
    public Task InitializeAsync()
    {
        LoadSettings();
        StartRecordingCapabilityRefresh();
        return Task.CompletedTask;
    }

    partial void OnOutputPathChanged(string value)
    {
        SaveSettings();
    }

    partial void OnIsStatsVisibleChanged(bool value)
    {
        SaveSettings();
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = SettingsService.Load();
            var loadPlan = MainViewModelSettingsPersistenceProjection.BuildLoadPlan(
                settings,
                new MainViewModelSettingsLoadInput(
                    AvailableRecordingFormats,
                    AvailableQualities,
                    AvailablePresets,
                    AvailableSplitEncodeModes,
                    AvailableDeviceAudioModes,
                    Directory.Exists));

            if (!string.IsNullOrWhiteSpace(loadPlan.UnavailableRecordingFormat))
            {
                Logger.Log($"SETTINGS_LOAD: saved format '{loadPlan.UnavailableRecordingFormat}' not available, using default.");
            }

            ApplySettingsLoadPlan(loadPlan);
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_LOAD: unexpected error: {ex.Message}");
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void SaveSettings()
    {
        if (_isLoadingSettings)
        {
            return;
        }

        try
        {
            var settings = MainViewModelSettingsPersistenceProjection.BuildSaveSettings(
                new MainViewModelSettingsSaveInput(
                    SelectedDevice?.Id,
                    OutputPath,
                    SelectedRecordingFormat,
                    SelectedQuality,
                    SelectedPreset,
                    SelectedSplitEncodeMode,
                    CustomBitrateMbps,
                    IsHdrEnabled,
                    IsAudioEnabled,
                    IsAudioPreviewEnabled,
                    IsCustomAudioInputEnabled,
                    SelectedAudioInputDevice?.Id,
                    IsMicrophoneEnabled,
                    SelectedMicrophoneDevice?.Id,
                    MicrophoneVolume,
                    VolumeSaveOverride ?? PreviewVolume,
                    IsStatsVisible,
                    SelectedDeviceAudioMode,
                    AnalogAudioGainPercent,
                    FlashbackGpuDecode,
                    FlashbackBufferMinutes));

            SettingsService.Save(settings);
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_SAVE: unexpected error: {ex.Message}");
        }
    }

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

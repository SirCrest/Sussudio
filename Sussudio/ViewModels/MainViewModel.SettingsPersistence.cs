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

}

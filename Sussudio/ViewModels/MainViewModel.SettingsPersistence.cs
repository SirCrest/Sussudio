using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

internal readonly record struct MainViewModelSettingsLoadInput(
    IReadOnlyCollection<string> AvailableRecordingFormats,
    IReadOnlyCollection<string> AvailableQualities,
    IReadOnlyCollection<string> AvailablePresets,
    IReadOnlyCollection<string> AvailableSplitEncodeModes,
    IReadOnlyCollection<string> AvailableDeviceAudioModes,
    Func<string, bool> OutputDirectoryExists);

internal readonly record struct MainViewModelSettingsLoadPlan(
    string? OutputPath,
    string? SelectedRecordingFormat,
    string? UnavailableRecordingFormat,
    string? SelectedQuality,
    string? SelectedPreset,
    string? SelectedSplitEncodeMode,
    double? CustomBitrateMbps,
    bool? IsHdrEnabled,
    bool? IsAudioEnabled,
    bool? IsAudioPreviewEnabled,
    bool? IsCustomAudioInputEnabled,
    bool? IsMicrophoneEnabled,
    double? MicrophoneVolume,
    string? PendingMicrophoneVolumeDeviceId,
    double? PreviewVolume,
    bool? IsStatsVisible,
    string? SelectedDeviceAudioMode,
    double? AnalogAudioGainPercent,
    bool? FlashbackGpuDecode,
    int? FlashbackBufferMinutes,
    string? PendingDeviceId,
    string? PendingAudioDeviceId,
    string? PendingMicrophoneDeviceId,
    string? PendingDeviceAudioMode,
    double? PendingAnalogAudioGainPercent);

internal readonly record struct MainViewModelSettingsSaveInput(
    string? SelectedDeviceId,
    string OutputPath,
    string SelectedRecordingFormat,
    string SelectedQuality,
    string SelectedPreset,
    string SelectedSplitEncodeMode,
    double CustomBitrateMbps,
    bool IsHdrEnabled,
    bool IsAudioEnabled,
    bool IsAudioPreviewEnabled,
    bool IsCustomAudioInputEnabled,
    string? SelectedAudioInputDeviceId,
    bool IsMicrophoneEnabled,
    string? SelectedMicrophoneDeviceId,
    double MicrophoneVolume,
    double PreviewVolume,
    bool IsStatsVisible,
    string SelectedDeviceAudioMode,
    double AnalogAudioGainPercent,
    bool FlashbackGpuDecode,
    int FlashbackBufferMinutes);

/// <summary>
/// Pure settings projection between persisted settings and MainViewModel load state.
/// </summary>
internal static class MainViewModelSettingsPersistenceProjection
{
    internal static MainViewModelSettingsLoadPlan BuildLoadPlan(
        UserSettings settings,
        MainViewModelSettingsLoadInput input)
    {
        var outputPath = !string.IsNullOrWhiteSpace(settings.OutputPath) &&
            input.OutputDirectoryExists(settings.OutputPath)
                ? settings.OutputPath
                : null;

        var selectedRecordingFormat = ResolveAvailableValue(
            settings.SelectedRecordingFormat,
            input.AvailableRecordingFormats,
            StringComparer.Ordinal);
        var unavailableRecordingFormat = selectedRecordingFormat is null &&
            !string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat)
                ? settings.SelectedRecordingFormat
                : null;

        var microphoneVolume = settings.MicrophoneVolume.HasValue
            ? Math.Clamp(settings.MicrophoneVolume.Value, 0.0, 100.0)
            : (double?)null;

        return new MainViewModelSettingsLoadPlan(
            OutputPath: outputPath,
            SelectedRecordingFormat: selectedRecordingFormat,
            UnavailableRecordingFormat: unavailableRecordingFormat,
            SelectedQuality: ResolveAvailableValue(settings.SelectedQuality, input.AvailableQualities, StringComparer.Ordinal),
            SelectedPreset: ResolveAvailableValue(settings.SelectedPreset, input.AvailablePresets, StringComparer.Ordinal),
            SelectedSplitEncodeMode: ResolveAvailableValue(settings.SelectedSplitEncodeMode, input.AvailableSplitEncodeModes, StringComparer.Ordinal),
            CustomBitrateMbps: settings.CustomBitrateMbps,
            IsHdrEnabled: settings.IsHdrEnabled,
            IsAudioEnabled: settings.IsAudioEnabled,
            IsAudioPreviewEnabled: settings.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled: settings.IsCustomAudioInputEnabled,
            IsMicrophoneEnabled: settings.IsMicrophoneEnabled,
            MicrophoneVolume: microphoneVolume,
            PendingMicrophoneVolumeDeviceId: microphoneVolume.HasValue ? settings.SelectedMicrophoneDeviceId : null,
            PreviewVolume: settings.PreviewVolume.HasValue ? Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0) : null,
            IsStatsVisible: settings.IsStatsVisible,
            SelectedDeviceAudioMode: ResolveAvailableValue(
                settings.SelectedDeviceAudioMode,
                input.AvailableDeviceAudioModes,
                StringComparer.OrdinalIgnoreCase),
            AnalogAudioGainPercent: settings.AnalogAudioGainPercent.HasValue
                ? Math.Clamp(settings.AnalogAudioGainPercent.Value, 0.0, 100.0)
                : null,
            FlashbackGpuDecode: settings.FlashbackGpuDecode,
            FlashbackBufferMinutes: settings.FlashbackBufferMinutes.HasValue
                ? Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)
                : null,
            PendingDeviceId: settings.SelectedDeviceId,
            PendingAudioDeviceId: settings.SelectedAudioInputDeviceId,
            PendingMicrophoneDeviceId: settings.SelectedMicrophoneDeviceId,
            PendingDeviceAudioMode: settings.SelectedDeviceAudioMode,
            PendingAnalogAudioGainPercent: settings.AnalogAudioGainPercent);
    }

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
            IsStatsVisible = input.IsStatsVisible,
            SelectedDeviceAudioMode = input.SelectedDeviceAudioMode,
            AnalogAudioGainPercent = input.AnalogAudioGainPercent,
            FlashbackGpuDecode = input.FlashbackGpuDecode,
            FlashbackBufferMinutes = input.FlashbackBufferMinutes,
        };
    }

    private static string? ResolveAvailableValue(
        string? savedValue,
        IEnumerable<string> availableValues,
        StringComparer comparer)
    {
        return !string.IsNullOrWhiteSpace(savedValue) &&
            availableValues.Contains(savedValue, comparer)
                ? savedValue
                : null;
    }
}

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

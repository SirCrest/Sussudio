using System;
using System.IO;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Settings load/save projection between persisted user settings and ViewModel state.
/// </summary>
public partial class MainViewModel
{
    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = SettingsService.Load();

            if (!string.IsNullOrWhiteSpace(settings.OutputPath) && Directory.Exists(settings.OutputPath))
            {
                OutputPath = settings.OutputPath;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat) &&
                AvailableRecordingFormats.Contains(settings.SelectedRecordingFormat))
            {
                SelectedRecordingFormat = settings.SelectedRecordingFormat;
            }
            else if (!string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat))
            {
                Logger.Log($"SETTINGS_LOAD: saved format '{settings.SelectedRecordingFormat}' not available, using default.");
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedQuality) &&
                AvailableQualities.Contains(settings.SelectedQuality))
            {
                SelectedQuality = settings.SelectedQuality;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedPreset) &&
                AvailablePresets.Contains(settings.SelectedPreset))
            {
                SelectedPreset = settings.SelectedPreset;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedSplitEncodeMode) &&
                AvailableSplitEncodeModes.Contains(settings.SelectedSplitEncodeMode))
            {
                SelectedSplitEncodeMode = settings.SelectedSplitEncodeMode;
            }

            if (settings.CustomBitrateMbps.HasValue)
            {
                CustomBitrateMbps = settings.CustomBitrateMbps.Value;
            }

            if (settings.IsHdrEnabled.HasValue)
            {
                IsHdrEnabled = settings.IsHdrEnabled.Value;
            }

            if (settings.IsAudioEnabled.HasValue)
            {
                IsAudioEnabled = settings.IsAudioEnabled.Value;
            }

            if (settings.IsAudioPreviewEnabled.HasValue)
            {
                IsAudioPreviewEnabled = settings.IsAudioPreviewEnabled.Value;
            }

            if (settings.IsCustomAudioInputEnabled.HasValue)
            {
                IsCustomAudioInputEnabled = settings.IsCustomAudioInputEnabled.Value;
            }

            if (settings.IsMicrophoneEnabled.HasValue)
            {
                IsMicrophoneEnabled = settings.IsMicrophoneEnabled.Value;
            }

            if (settings.MicrophoneVolume.HasValue)
            {
                var savedMicrophoneVolume = Math.Clamp(settings.MicrophoneVolume.Value, 0.0, 100.0);
                MicrophoneVolume = savedMicrophoneVolume;
                _pendingSavedMicrophoneVolume = savedMicrophoneVolume;
                _pendingSavedMicrophoneVolumeDeviceId = settings.SelectedMicrophoneDeviceId;
            }

            if (settings.PreviewVolume.HasValue)
            {
                PreviewVolume = Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0);
            }

            if (settings.ShowAllCaptureOptions.HasValue)
            {
                ShowAllCaptureOptions = settings.ShowAllCaptureOptions.Value;
            }

            if (settings.IsStatsVisible.HasValue)
            {
                IsStatsVisible = settings.IsStatsVisible.Value;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedDeviceAudioMode) &&
                AvailableDeviceAudioModes.Contains(settings.SelectedDeviceAudioMode, StringComparer.OrdinalIgnoreCase))
            {
                SelectedDeviceAudioMode = settings.SelectedDeviceAudioMode;
            }

            if (settings.AnalogAudioGainPercent.HasValue)
            {
                AnalogAudioGainPercent = Math.Clamp(settings.AnalogAudioGainPercent.Value, 0.0, 100.0);
            }

            if (settings.FlashbackGpuDecode.HasValue)
            {
                FlashbackGpuDecode = settings.FlashbackGpuDecode.Value;
            }

            if (settings.FlashbackBufferMinutes.HasValue)
            {
                FlashbackBufferMinutes = Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30);
            }

            // Defer device selection until RefreshDevicesAsync populates the device list
            _pendingSavedDeviceId = settings.SelectedDeviceId;
            _pendingSavedAudioDeviceId = settings.SelectedAudioInputDeviceId;
            _pendingSavedMicrophoneDeviceId = settings.SelectedMicrophoneDeviceId;
            _pendingSavedDeviceAudioMode = settings.SelectedDeviceAudioMode;
            _pendingSavedAnalogAudioGainPercent = settings.AnalogAudioGainPercent;
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
            var settings = new UserSettings
            {
                SelectedDeviceId = SelectedDevice?.Id,
                OutputPath = OutputPath,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                CustomBitrateMbps = CustomBitrateMbps,
                IsHdrEnabled = IsHdrEnabled,
                IsAudioEnabled = IsAudioEnabled,
                IsAudioPreviewEnabled = IsAudioPreviewEnabled,
                IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                IsMicrophoneEnabled = IsMicrophoneEnabled,
                SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,
                MicrophoneVolume = MicrophoneVolume,
                PreviewVolume = VolumeSaveOverride ?? PreviewVolume,
                ShowAllCaptureOptions = ShowAllCaptureOptions,
                IsStatsVisible = IsStatsVisible,
                SelectedDeviceAudioMode = SelectedDeviceAudioMode,
                AnalogAudioGainPercent = AnalogAudioGainPercent,
                FlashbackGpuDecode = FlashbackGpuDecode,
                FlashbackBufferMinutes = FlashbackBufferMinutes,
            };

            SettingsService.Save(settings);
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_SAVE: unexpected error: {ex.Message}");
        }
    }
}

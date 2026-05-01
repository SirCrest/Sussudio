using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.ViewModels;

public partial class MainViewModel
{
    public async Task InitializeAsync()
    {
        var formatsTask = RefreshRecordingFormatsAsync();
        var splitTask = RefreshSplitEncodeModesAsync();
        await Task.WhenAll(formatsTask, splitTask);
        LoadSettings();
    }

    partial void OnOutputPathChanged(string value)
    {
        SaveSettings();
    }

    partial void OnSelectedRecordingFormatChanged(string value)
    {
        SaveSettings();

        // Cycle the flashback encoder so the buffer uses the new codec.
        // Track the task so ReinitializeDeviceAsync can await it — otherwise
        // a rapid codec→resolution change sequence can race: the codec cycle
        // holds the session transition lock while the reinit tries to acquire it,
        // causing the reinit to read stale settings or fail silently.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false)
        {
            var format = value switch
            {
                "HEVC" => RecordingFormat.HevcMp4,
                "AV1" => RecordingFormat.Av1Mp4,
                _ => RecordingFormat.H264Mp4
            };
            TrackPendingFlashbackCycleTask(
                _sessionCoordinator.UpdateRecordingFormatAsync(format),
                "recording format");
        }
    }

    partial void OnCustomBitrateMbpsChanged(double value)
    {
        SaveSettings();

        // Cycle the flashback encoder so the buffer uses the new bitrate.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false && _suppressFlashbackEncoderSettingsCycle is false)
        {
            TrackFlashbackEncoderSettingsCycle("bitrate");
        }
    }

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

    private async Task RefreshRecordingFormatsAsync()
    {
        var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync();
        var formats = new List<string>();

        if (support.HasH264)
        {
            formats.Add("H.264");
        }

        if (support.HasHevc)
        {
            formats.Add("HEVC");
        }

        if (support.HasAv1)
        {
            formats.Add("AV1");
        }

        void ApplyFormats()
        {
            _detectedRecordingFormats = formats
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            IsFfmpegMissing = _detectedRecordingFormats.Count == 0;
            if (IsFfmpegMissing)
            {
                Logger.Log("FFMPEG_MISSING: encoder probe returned zero codecs. Recording unavailable.");
            }
            RebuildRecordingFormatOptions();
            Logger.Log($"Recording formats refreshed: {string.Join(", ", _detectedRecordingFormats)}");
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ApplyFormats();
        }
        else
        {
            if (!_dispatcherQueue.TryEnqueue(ApplyFormats))
            {
                Logger.Log($"RECORDING_FORMATS_UI_ENQUEUE_FAILED formats={formats.Count}");
            }
        }
    }

    private async Task RefreshSplitEncodeModesAsync()
    {
        var support = await FfmpegRuntimeLocator.GetSplitEncodeSupportAsync();
        var modes = new List<string> { "Auto", "Disabled", "2-way", "3-way" };

        void ApplyModes()
        {
            AvailableSplitEncodeModes.Clear();
            foreach (var mode in modes)
            {
                AvailableSplitEncodeModes.Add(mode);
            }
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ApplyModes();
        }
        else
        {
            if (!_dispatcherQueue.TryEnqueue(ApplyModes))
            {
                Logger.Log($"SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED modes={modes.Count}");
            }
        }
    }

    partial void OnShowAllCaptureOptionsChanged(bool value)
    {
        if (IsRecording)
        {
            _pendingModeOptionsRefresh = true;
            SaveSettings();
            return;
        }

        _pendingModeOptionsRefresh = false;
        RebuildResolutionOptions();
        SaveSettings();
    }

    partial void OnIsStatsVisibleChanged(bool value)
    {
        SaveSettings();
    }

    partial void OnFlashbackBufferMinutesChanged(int value)
    {
        SaveSettings();

        // Push into the active CaptureSettings so RestartFlashbackAsync sees the new value.
        var updateTask = _sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode);

        // Restart the flashback backend so the new duration takes effect immediately.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false)
        {
            var restartGeneration = Interlocked.Increment(ref _flashbackSettingsRestartGeneration);
            _ = RestartFlashbackAfterSettingsUpdateAsync(updateTask, restartGeneration);
        }
        else
        {
            TrackFlashbackCoordinatorTask(updateTask, "UpdateFlashbackSettings(buffer)");
        }
    }

    partial void OnFlashbackGpuDecodeChanged(bool value)
    {
        // Push into CaptureSettings so rebuilds (e.g., after buffer-duration restart
        // or format-change cycle) use the latest GPU decode preference.
        TrackFlashbackCoordinatorTask(
            _sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode),
            "UpdateFlashbackSettings(gpu)");
        SaveSettings();
    }

    private async Task RestartFlashbackAfterSettingsUpdateAsync(Task settingsUpdateTask, int restartGeneration)
    {
        try
        {
            await settingsUpdateTask.ConfigureAwait(false);
            if (restartGeneration != Volatile.Read(ref _flashbackSettingsRestartGeneration))
            {
                Logger.Log($"RestartFlashbackAfterSettingsUpdate skipped stale generation {restartGeneration}");
                return;
            }

            var shouldRestart = await InvokeOnUiThreadAsync(
                    () => IsPreviewing && !IsRecording && _isLoadingSettings is false,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (shouldRestart is false)
            {
                Logger.Log($"RestartFlashbackAfterSettingsUpdate skipped inactive generation {restartGeneration}");
                return;
            }

            await RestartFlashbackAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"RestartFlashbackAfterSettingsUpdate failed: {ex.Message}");
        }
    }

    private static void TrackFlashbackCoordinatorTask(Task task, string description)
    {
        _ = task.ContinueWith(
            t => Logger.Log($"{description} failed: {t.Exception!.InnerException?.Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private void TrackFlashbackEncoderSettingsCycle(string description)
    {
        var task = _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
            quality: ParseVideoQuality(SelectedQuality),
            customBitrateMbps: CustomBitrateMbps,
            nvencPreset: SelectedPreset);
        TrackPendingFlashbackCycleTask(task, description);
    }

    private void TrackPendingFlashbackCycleTask(Task task, string description)
    {
        _pendingFlashbackCycleTask = task;
        _ = task.ContinueWith(
            t =>
            {
                if (ReferenceEquals(_pendingFlashbackCycleTask, t))
                {
                    _pendingFlashbackCycleTask = null;
                }

                if (t.IsFaulted)
                {
                    Logger.Log($"CycleFlashbackEncoder({description}) failed: {t.Exception!.InnerException?.Message}");
                }
                else if (t.IsCanceled)
                {
                    Logger.Log($"CycleFlashbackEncoder({description}) canceled");
                }
            });
    }

    private static VideoQuality ParseVideoQuality(string value)
    {
        return value switch
        {
            "Auto" => VideoQuality.Auto,
            "Low" => VideoQuality.Low,
            "Medium" => VideoQuality.Medium,
            "High" => VideoQuality.High,
            "Super High" => VideoQuality.SuperHigh,
            "Custom" => VideoQuality.Custom,
            _ => VideoQuality.High
        };
    }


    partial void OnSelectedQualityChanged(string value)
    {
        IsCustomBitrateVisible = value == "Custom";
        SaveSettings();

        // Cycle the flashback encoder so the buffer uses the new quality level.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false && _suppressFlashbackEncoderSettingsCycle is false)
        {
            TrackFlashbackEncoderSettingsCycle("quality");
        }
    }

    partial void OnSelectedPresetChanged(string value)
    {
        SaveSettings();

        // Cycle the flashback encoder so the buffer uses the new preset.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false && _suppressFlashbackEncoderSettingsCycle is false)
        {
            TrackFlashbackEncoderSettingsCycle("preset");
        }
    }

    partial void OnSelectedSplitEncodeModeChanged(string value)
    {
        SaveSettings();
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.ViewModels;

/// <summary>
/// Live Flashback reactions to recording and buffer setting changes.
/// </summary>
public partial class MainViewModel
{
    partial void OnSelectedRecordingFormatChanged(string value)
    {
        SaveSettings();

        // Cycle the flashback encoder so the buffer uses the new codec.
        // Track the task so ReinitializeDeviceAsync can await it; otherwise
        // a rapid codec-to-resolution change sequence can race with reinit.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false && _suppressFlashbackFormatCycle is false)
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
        catch (OperationCanceledException ex)
        {
            Logger.Log($"RestartFlashbackAfterSettingsUpdate canceled: {ex.Message}");
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
            nvencPreset: SelectedPreset,
            splitEncodeMode: SelectedSplitEncodeMode);
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

        // Cycle the flashback encoder so the buffer uses the new split mode.
        if (IsPreviewing && !IsRecording && _isLoadingSettings is false && _suppressFlashbackEncoderSettingsCycle is false)
        {
            TrackFlashbackEncoderSettingsCycle("split encode");
        }
    }
}

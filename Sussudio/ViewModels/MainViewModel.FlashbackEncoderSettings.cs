using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Live Flashback encoder reactions to codec, quality, preset, split, and bitrate changes.
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
            var format = RecordingSettingsSelectionPolicy.ParseRecordingFormat(value);
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

    private void TrackFlashbackEncoderSettingsCycle(string description)
    {
        var task = _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
            quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality),
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

using System.Threading;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio capture and preview observable property change handlers.
/// </summary>
public partial class MainViewModel
{
    partial void OnIsAudioEnabledChanged(bool value)
    {
        Logger.Log($"Audio capture enabled: {value}");
        var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);

        if (value)
        {
            // Re-enable audio preview and start it if we're already previewing
            if (!IsAudioPreviewEnabled)
            {
                _suppressAudioPreviewEnabledChangeOperation = true;
                try
                {
                    IsAudioPreviewEnabled = true;
                }
                finally
                {
                    _suppressAudioPreviewEnabledChangeOperation = false;
                }
            }

            if (IsPreviewing && IsInitialized)
            {
                EnqueueUiOperation(async () =>
                {
                    if (changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled)
                    {
                        Logger.Log($"AUDIO_TOGGLE_SKIP op=enable stale_generation={changeGeneration}");
                        return;
                    }

                    // Cycle the flashback encoder so it reconnects its audio feed.
                    // Without this, the first recording after audio off->on produces
                    // an empty file because the flashback sink's audio path is stale.
                    var settings = BuildCaptureSettings();
                    await SetAudioMonitoringEnabledWithVolumeTransitionAsync(
                        true,
                        "audio_capture_enable",
                        teardownCapture: false,
                        afterMonitoringStarted: () => _sessionCoordinator.RestartFlashbackAsync(settings));
                }, "audio preview restart + flashback cycle");
            }
        }
        else
        {
            if (IsAudioPreviewEnabled)
            {
                _suppressAudioPreviewEnabledChangeOperation = true;
                try
                {
                    IsAudioPreviewEnabled = false;
                }
                finally
                {
                    _suppressAudioPreviewEnabledChangeOperation = false;
                }
            }

            EnqueueUiOperation(async () =>
            {
                if (changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled)
                {
                    Logger.Log($"AUDIO_TOGGLE_SKIP op=disable stale_generation={changeGeneration}");
                    return;
                }

                await SetAudioMonitoringEnabledWithVolumeTransitionAsync(false, "audio_capture_disable", teardownCapture: true);
            }, "audio capture teardown");

            ResetAudioMeter();
        }

        SaveSettings();
    }

    partial void OnIsAudioPreviewEnabledChanged(bool value)
    {
        if (value && !IsAudioEnabled)
        {
            Logger.Log("Audio preview requested but audio capture is disabled");
            IsAudioPreviewEnabled = false;
            return;
        }

        if (_suppressAudioPreviewEnabledChangeOperation)
        {
            SaveSettings();
            return;
        }

        if (!value && !IsRecording)
        {
            ResetAudioMeter();
        }

        if (IsPreviewing && IsInitialized)
        {
            var description = value ? "audio monitoring enable" : "audio monitoring mute";
            EnqueueUiOperation(
                () => SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false),
                description);
        }

        SaveSettings();
    }
}

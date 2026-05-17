using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Live Flashback reactions to buffer duration and GPU-decode setting changes.
/// </summary>
public partial class MainViewModel
{
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
}

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private readonly BitrateSampleWindow _flashbackBitrateSamples = new(BitrateWindowMs);
    private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;
    private bool _suppressFlashbackFormatCycle;
    private bool _suppressFlashbackEncoderSettingsCycle;
    private CancellationTokenSource? _exportCts;
    private int _flashbackExportOperationId;
    private int _flashbackSettingsRestartGeneration;
    private Task? _pendingFlashbackCycleTask;

    [ObservableProperty]
    public partial bool FlashbackGpuDecode { get; set; } = true;

    [ObservableProperty]
    public partial int FlashbackBufferMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial bool IsFlashbackEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFlashbackTimelineVisible { get; set; }

    [ObservableProperty]
    public partial FlashbackPlaybackState FlashbackState { get; set; } = FlashbackPlaybackState.Disabled;

    [ObservableProperty]
    public partial double FlashbackBufferFillPercent { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackBufferFilledDuration { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackPlaybackPosition { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackGapFromLive { get; set; }

    [ObservableProperty]
    public partial TimeSpan? FlashbackInPoint { get; set; }

    [ObservableProperty]
    public partial TimeSpan? FlashbackOutPoint { get; set; }

    [ObservableProperty]
    public partial long FlashbackBufferDiskBytes { get; set; }

    [ObservableProperty]
    public partial string FlashbackBitrateInfo { get; set; } = "";

    [ObservableProperty]
    public partial double FlashbackExportProgress { get; set; }

    [ObservableProperty]
    public partial bool IsFlashbackExporting { get; set; }

    [ObservableProperty]
    public partial bool IsDiskWarningActive { get; set; }

    partial void OnIsFlashbackEnabledChanged(bool value)
    {
        if (!value)
        {
            IsFlashbackTimelineVisible = false;
        }
    }

    // Live Flashback enablement, restart, buffer-duration, and GPU-decode setting changes.
    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken);

    public async Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken).ConfigureAwait(false);
        await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false);
        await InvokeOnUiThreadAsync(
            () =>
            {
                _flashbackBitrateSamples.Clear();
                return true;
            },
            cancellationToken).ConfigureAwait(false);
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

    // Live Flashback encoder reactions to codec, quality, preset, split, and bitrate changes.
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

    /// <summary>
    /// Updates flashback buffer status properties from the buffer manager.
    /// Called from a periodic timer on the UI thread.
    /// </summary>
    public void UpdateFlashbackBufferStatus()
    {
        var bufferStatus = _sessionCoordinator.GetFlashbackBufferStatus();
        if (!bufferStatus.IsActive)
        {
            if (FlashbackState != FlashbackPlaybackState.Disabled)
                FlashbackState = FlashbackPlaybackState.Disabled;
            FlashbackBufferFillPercent = 0;
            FlashbackBufferFilledDuration = TimeSpan.Zero;
            FlashbackBufferDiskBytes = 0;
            FlashbackBitrateInfo = "";
            IsDiskWarningActive = false;
            FlashbackInPoint = null;
            FlashbackOutPoint = null;
            _flashbackBitrateSamples.Clear();
            return;
        }

        FlashbackBufferFilledDuration = bufferStatus.FilledDuration;
        FlashbackBufferDiskBytes = bufferStatus.DiskBytes;
        FlashbackBufferFillPercent = bufferStatus.BufferDuration.TotalSeconds > 0
            ? Math.Clamp(bufferStatus.FilledDuration.TotalSeconds / bufferStatus.BufferDuration.TotalSeconds * 100, 0, 100)
            : 0;

        IsDiskWarningActive = bufferStatus.IsDiskWarningActive;

        UpdateFlashbackBitrate();

        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        if (playback.IsActive)
        {
            FlashbackState = playback.State;
            if (playback.State != FlashbackPlaybackState.Scrubbing)
                FlashbackPlaybackPosition = playback.PlaybackPosition;
            FlashbackGapFromLive = playback.GapFromLive;
            FlashbackInPoint = playback.InPoint;
            FlashbackOutPoint = playback.OutPoint;
        }
        else
        {
            if (FlashbackState != FlashbackPlaybackState.Live)
                FlashbackState = FlashbackPlaybackState.Live;
        }
    }

    private void UpdateFlashbackBitrate()
    {
        var diskBytes = _sessionCoordinator.FlashbackTotalBytesWritten;
        var now = Environment.TickCount64;
        var smoothed = _flashbackBitrateSamples.AddSampleAndCompute(now, diskBytes);
        FlashbackBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "";
    }
}

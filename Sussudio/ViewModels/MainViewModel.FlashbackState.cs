using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;

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
    private bool _suppressFlashbackSettingsUpdate;
    private static readonly int[] SupportedFlashbackBufferMinutes = { 1, 2, 5, 10, 15, 30 };

    // UI health surfacing (F1-UI/F8-UI). Reasons in this set are voluntary
    // transitions to Live and must not raise the involuntary snap-to-live
    // notice: "" (default/no-reason SetState calls), "user" (explicit
    // play/pause/seek/scrub/nudge user actions), "go_live"/"thread_stop"
    // (playback thread exiting cleanly via GoLive or a normal stop).
    private static readonly HashSet<string> FlashbackVoluntaryLiveReasons =
        new(StringComparer.Ordinal) { "", "user", "go_live", "thread_stop" };

    private const string FlashbackSnapToLiveHealthMessage = "Returned to live — playback error.";
    private const string FlashbackDeadBackendHealthMessage = "Flashback is not running — use Restart Flashback.";
    private static readonly TimeSpan FlashbackHealthMessageClearDelay = TimeSpan.FromSeconds(5);

    private FlashbackPlaybackController? _flashbackHealthSubscribedController;
    private FlashbackPlaybackController? _flashbackPreWarmedController;
    private DispatcherQueueTimer? _flashbackHealthClearTimer;

    [ObservableProperty]
    public partial string FlashbackHealthMessage { get; set; } = "";

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
            // A stale "not running" banner would be confusing once the user has
            // explicitly turned flashback off; the transient snap notice is left
            // alone here since it clears itself on its own timer.
            if (FlashbackHealthMessage == FlashbackDeadBackendHealthMessage)
            {
                FlashbackHealthMessage = "";
            }
        }
    }

    // Live Flashback enablement, restart, buffer-duration, and GPU-decode setting changes.
    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken);

    public async Task SetFlashbackBufferMinutesAsync(int minutes, CancellationToken cancellationToken = default)
    {
        if (Array.IndexOf(SupportedFlashbackBufferMinutes, minutes) < 0)
        {
            throw new InvalidOperationException("Flashback buffer minutes must be one of: 1, 2, 5, 10, 15, or 30.");
        }

        var state = await InvokeOnUiThreadAsync(
            () =>
            {
                if (FlashbackBufferMinutes == minutes)
                {
                    return (
                        Changed: false,
                        IsPreviewing,
                        IsRecording,
                        IsLoadingSettings: _isLoadingSettings,
                        GpuDecode: FlashbackGpuDecode);
                }

                _suppressFlashbackSettingsUpdate = true;
                try
                {
                    FlashbackBufferMinutes = minutes;
                }
                finally
                {
                    _suppressFlashbackSettingsUpdate = false;
                }

                SaveSettingsOrThrow();
                return (
                    Changed: true,
                    IsPreviewing,
                    IsRecording,
                    IsLoadingSettings: _isLoadingSettings,
                    GpuDecode: FlashbackGpuDecode);
            },
            cancellationToken).ConfigureAwait(false);

        if (!state.Changed)
        {
            return;
        }

        if (state.IsPreviewing && !state.IsRecording && !state.IsLoadingSettings)
        {
            await RestartFlashbackAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sessionCoordinator.UpdateFlashbackSettingsAsync(
            minutes,
            state.GpuDecode,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFlashbackGpuDecodeAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var state = await InvokeOnUiThreadAsync(
            () =>
            {
                if (FlashbackGpuDecode == enabled)
                {
                    return (Changed: false, BufferMinutes: FlashbackBufferMinutes);
                }

                _suppressFlashbackSettingsUpdate = true;
                try
                {
                    FlashbackGpuDecode = enabled;
                }
                finally
                {
                    _suppressFlashbackSettingsUpdate = false;
                }

                SaveSettingsOrThrow();
                return (Changed: true, BufferMinutes: FlashbackBufferMinutes);
            },
            cancellationToken).ConfigureAwait(false);

        if (!state.Changed)
        {
            return;
        }

        await _sessionCoordinator.UpdateFlashbackSettingsAsync(
            state.BufferMinutes,
            enabled,
            cancellationToken).ConfigureAwait(false);
    }

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
        if (_suppressFlashbackSettingsUpdate)
        {
            return;
        }

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
        if (_suppressFlashbackSettingsUpdate)
        {
            return;
        }

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

            // Dead-backend banner: the toggle says flashback should be running
            // but the buffer manager reports inactive (fatal error exhausted
            // auto-restart, or startup never brought the backend up). Persistent
            // until the backend comes back or the user disables the toggle.
            if (IsFlashbackEnabled)
            {
                FlashbackHealthMessage = FlashbackDeadBackendHealthMessage;
            }
            else if (FlashbackHealthMessage == FlashbackDeadBackendHealthMessage)
            {
                FlashbackHealthMessage = "";
            }

            DetachFlashbackStateChangedSubscription();
            return;
        }

        if (FlashbackHealthMessage == FlashbackDeadBackendHealthMessage)
        {
            FlashbackHealthMessage = "";
        }

        RefreshFlashbackStateChangedSubscription();
        // Re-attempt per poll tick: polling starts before the controller is
        // initialized (and the controller is rebuilt on backend cycles), so the
        // one-shot call in StartStatusPolling alone never lands the warm-up.
        PreWarmFlashbackPlayback();

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

    /// <summary>
    /// Re-attaches the involuntary snap-to-live subscription when the backend
    /// controller instance changes. Must be called from the same 250 ms poll
    /// that reads buffer status, since <see cref="FlashbackPlaybackController"/>
    /// is rebuilt on every backend cycle
    /// (<c>FlashbackBackendResources.CycleSinkOnlyAsync</c>).
    /// </summary>
    private void RefreshFlashbackStateChangedSubscription()
    {
        var current = _sessionCoordinator.FlashbackPlaybackControllerInstance;
        if (ReferenceEquals(current, _flashbackHealthSubscribedController))
        {
            return;
        }

        if (_flashbackHealthSubscribedController != null)
        {
            _flashbackHealthSubscribedController.StateChanged -= OnFlashbackPlaybackStateChanged;
        }

        _flashbackHealthSubscribedController = current;

        if (current != null)
        {
            current.StateChanged += OnFlashbackPlaybackStateChanged;
        }
    }

    private void DetachFlashbackStateChangedSubscription()
    {
        if (_flashbackHealthSubscribedController == null)
        {
            return;
        }

        _flashbackHealthSubscribedController.StateChanged -= OnFlashbackPlaybackStateChanged;
        _flashbackHealthSubscribedController = null;
    }

    /// <summary>
    /// Involuntary snap-to-live notice (F8-UI). Raised from the playback thread
    /// via <see cref="FlashbackPlaybackController.StateChanged"/> — marshal to
    /// the UI thread before touching any bound property.
    /// </summary>
    private void OnFlashbackPlaybackStateChanged(
        FlashbackPlaybackState oldState,
        FlashbackPlaybackState newState,
        string reason)
    {
        if (newState != FlashbackPlaybackState.Live || FlashbackVoluntaryLiveReasons.Contains(reason))
        {
            return;
        }

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            FlashbackHealthMessage = FlashbackSnapToLiveHealthMessage;
            ScheduleFlashbackHealthMessageClear();
        }))
        {
            Logger.Log($"FLASHBACK_HEALTH_UI_ENQUEUE_FAILED reason='{reason}'");
        }
    }

    private void ScheduleFlashbackHealthMessageClear()
    {
        _flashbackHealthClearTimer ??= _dispatcherQueue.CreateTimer();
        _flashbackHealthClearTimer.Stop();
        _flashbackHealthClearTimer.Tick -= FlashbackHealthClearTimer_Tick;
        _flashbackHealthClearTimer.Tick += FlashbackHealthClearTimer_Tick;
        _flashbackHealthClearTimer.Interval = FlashbackHealthMessageClearDelay;
        _flashbackHealthClearTimer.IsRepeating = false;
        _flashbackHealthClearTimer.Start();
    }

    private void FlashbackHealthClearTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        // Only clear the transient snap notice; a persistent dead-backend
        // banner set in the meantime must not be swallowed by this timer.
        if (FlashbackHealthMessage == FlashbackSnapToLiveHealthMessage)
        {
            FlashbackHealthMessage = "";
        }
    }

    /// <summary>
    /// Pre-warm hook: nudges the playback thread up once per controller
    /// instance so the first interaction after the flashback timeline opens
    /// isn't cold. No-op when there is no active controller or it has already
    /// been pre-warmed. Failures are swallowed and logged — pre-warming is a
    /// latency optimization, not a correctness requirement.
    /// </summary>
    public void PreWarmFlashbackPlayback()
    {
        var controller = _sessionCoordinator.FlashbackPlaybackControllerInstance;
        // IsInitialized gate: PreWarm() no-ops silently before Initialize() runs,
        // so latching an uninitialized controller would consume its only warm-up.
        if (controller == null || controller.IsDisposed || !controller.IsInitialized ||
            ReferenceEquals(controller, _flashbackPreWarmedController))
        {
            return;
        }

        _flashbackPreWarmedController = controller;
        try
        {
            controller.PreWarm();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREWARM_UI_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void UpdateFlashbackBitrate()
    {
        var diskBytes = _sessionCoordinator.FlashbackTotalBytesWritten;
        var now = Environment.TickCount64;
        var smoothed = _flashbackBitrateSamples.AddSampleAndCompute(now, diskBytes);
        FlashbackBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "";
    }

    public IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
        => _sessionCoordinator.GetFlashbackSegments();

    public Task<IReadOnlyList<FlashbackSegmentInfo>> GetFlashbackSegmentsAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);

    internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()
        => _sessionCoordinator.GetFlashbackPlaybackSnapshot();

    public void ReportFlashbackPlaybackRejection(string action, string logToken)
    {
        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        var lastFailure = string.IsNullOrWhiteSpace(playback.LastCommandFailure)
            ? "none"
            : playback.LastCommandFailure;
        var message =
            $"Flashback {action} rejected (state={playback.State}, " +
            $"threadAlive={playback.ThreadAlive}, pending={playback.PendingCommands}, " +
            $"lastFailure={lastFailure}).";

        Logger.Log(
            $"{logToken} state={playback.State} threadAlive={playback.ThreadAlive} " +
            $"pending={playback.PendingCommands} lastFailure='{lastFailure}' " +
            $"failureUtc={playback.LastCommandFailureUtcUnixMs}");
        StatusText = message;
    }

    /// <summary>
    /// Forwards a scrub-begin command to the active flashback playback controller.
    /// Returns true when the controller accepted the command (timeline was
    /// running and not stopped); false when flashback is disabled or the
    /// controller refused.
    /// </summary>
    public bool FlashbackBeginScrub(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackBeginScrub(position);
    }

    public bool FlashbackSeek(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackSeek(position);
    }

    public bool FlashbackUpdateScrub(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackUpdateScrub(position);
    }

    public bool FlashbackEndScrub()
    {
        return _sessionCoordinator.FlashbackEndScrub();
    }

    public bool FlashbackEndScrubAt(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackEndScrubAt(position);
    }

    public bool FlashbackPlay()
    {
        return _sessionCoordinator.FlashbackPlay();
    }

    public bool FlashbackPause()
    {
        return _sessionCoordinator.FlashbackPause();
    }

    public bool FlashbackGoLive()
    {
        return _sessionCoordinator.FlashbackGoLive();
    }

    public bool FlashbackNudge(TimeSpan delta)
    {
        return _sessionCoordinator.FlashbackNudge(delta);
    }

    public TimeSpan? FlashbackSetInPoint()
        => _sessionCoordinator.FlashbackSetInPoint();

    /// <summary>
    /// Pin the flashback in-point at an explicit user-intended position.
    /// The UI calls this with the visual playhead location so a marker placed
    /// during scrubbing lands where the user is pointing instead of at the
    /// keyframe-snapped <c>PlaybackPosition</c> the controller publishes after
    /// each decode (which can lag by hundreds of milliseconds mid-GOP).
    /// </summary>
    public TimeSpan? FlashbackSetInPointAt(TimeSpan position)
        => _sessionCoordinator.FlashbackSetInPointAt(position);

    public TimeSpan? FlashbackSetOutPoint()
        => _sessionCoordinator.FlashbackSetOutPoint();

    /// <summary>
    /// Pin the flashback out-point at an explicit user-intended position.
    /// See <see cref="FlashbackSetInPointAt"/> for rationale.
    /// </summary>
    public TimeSpan? FlashbackSetOutPointAt(TimeSpan position)
        => _sessionCoordinator.FlashbackSetOutPointAt(position);

    public bool FlashbackClearInOutPoints()
        => _sessionCoordinator.FlashbackClearInOutPoints();

    public Task<bool> ExecuteFlashbackActionAsync(
        AutomationFlashbackAction action,
        TimeSpan? position = null,
        CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken);

    private bool ExecuteFlashbackAction(AutomationFlashbackAction action, TimeSpan? position)
    {
        switch (action)
        {
            case AutomationFlashbackAction.Play:
                if (position.HasValue)
                {
                    if (!FlashbackSeek(position.Value))
                    {
                        return false;
                    }

                    return FlashbackPlay();
                }

                return FlashbackPlay();
            case AutomationFlashbackAction.Pause:
                return FlashbackPause();
            case AutomationFlashbackAction.GoLive:
                return FlashbackGoLive();
            case AutomationFlashbackAction.Seek:
                return FlashbackSeek(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.BeginScrub:
                return FlashbackBeginScrub(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.UpdateScrub:
                return FlashbackUpdateScrub(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.EndScrub:
                return position.HasValue
                    ? FlashbackEndScrubAt(position.Value)
                    : FlashbackEndScrub();
            case AutomationFlashbackAction.SetInPoint:
                return FlashbackSetInPoint().HasValue;
            case AutomationFlashbackAction.SetOutPoint:
                return FlashbackSetOutPoint().HasValue;
            case AutomationFlashbackAction.ClearInOutPoints:
                return FlashbackClearInOutPoints();
            default:
                throw new InvalidOperationException($"Unsupported flashback action '{action}'.");
        }
    }

    private abstract record ExportFlashbackOutcome
    {
        public sealed record Succeeded(FinalizeResult Result) : ExportFlashbackOutcome;
        public sealed record Failed(string ErrorMessage) : ExportFlashbackOutcome;
        public sealed record Stale : ExportFlashbackOutcome;
    }

    private bool IsCurrentFlashbackExport(int exportId, CancellationTokenSource exportCts)
        => Volatile.Read(ref _flashbackExportOperationId) == exportId && ReferenceEquals(_exportCts, exportCts);

    private static void CancelFlashbackExportCts(CancellationTokenSource? cts)
    {
        if (cts == null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // A previous automation export may have completed on a background
            // thread while its UI cleanup was still queued.
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CTS_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)
    {
        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static string FormatSuccessfulFlashbackExportStatus(
        string successPrefix,
        string exportPath,
        FinalizeResult result)
    {
        var statusMessage = result.StatusMessage?.Trim();
        return string.IsNullOrWhiteSpace(statusMessage)
            ? $"{successPrefix}: {exportPath}"
            : $"{successPrefix}: {exportPath} - {statusMessage}";
    }

    private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync(
        Func<IProgress<ExportProgress>, CancellationToken, Task<FinalizeResult>> exportAction)
    {
        // Export snapshots the flashback backend under CaptureService locks, then runs
        // outside the transition lock so long FFmpeg work does not block lifecycle commands.
        var exportId = Interlocked.Increment(ref _flashbackExportOperationId);
        var oldExportCts = _exportCts;
        CancelFlashbackExportCts(oldExportCts);
        _exportCts = new CancellationTokenSource();
        var exportCts = _exportCts;
        var ct = exportCts.Token;

        IsFlashbackExporting = true;
        FlashbackExportProgress = 0;
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                if (!_dispatcherQueue.TryEnqueue(() =>
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        FlashbackExportProgress = p.Percent;
                    }
                }))
                {
                    Logger.Log($"FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=ui percent={p.Percent:0.###}");
                }
            });

            var result = await exportAction(progress, ct);
            return IsCurrentFlashbackExport(exportId, exportCts)
                ? new ExportFlashbackOutcome.Succeeded(result)
                : new ExportFlashbackOutcome.Stale();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            return IsCurrentFlashbackExport(exportId, exportCts)
                ? new ExportFlashbackOutcome.Failed(ex.Message)
                : new ExportFlashbackOutcome.Stale();
        }
        finally
        {
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = false;
                FlashbackExportProgress = 0;
                _exportCts = null;
                DisposeFlashbackExportCtsBestEffort(exportCts, "ui_current");
            }
            else
            {
                DisposeFlashbackExportCtsBestEffort(exportCts, "ui_stale");
            }
        }
    }

    public async Task ExportFlashbackAsync()
    {
        if (!EnsureFlashbackActiveForExport("export"))
        {
            return;
        }

        var file = await PickFlashbackExportFileAsync($"Flashback_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        var inPoint = playback.InPoint;
        var outPoint = playback.OutPoint;
        var exportPath = ResolveUnusedFlashbackExportPath(file.Path);

        var outcome = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _sessionCoordinator.ExportFlashbackRangeAsync(
                inPoint,
                outPoint,
                exportPath,
                progress,
                ct,
                playback.InPointFilePts,
                playback.OutPointFilePts,
                force: false));
        switch (outcome)
        {
            case ExportFlashbackOutcome.Stale:
                return;
            case ExportFlashbackOutcome.Failed failed:
                StatusText = $"Export error: {failed.ErrorMessage}";
                break;
            case ExportFlashbackOutcome.Succeeded succeeded:
                StatusText = succeeded.Result.Succeeded
                    ? FormatSuccessfulFlashbackExportStatus("Export complete", exportPath, succeeded.Result)
                    : $"Export failed: {succeeded.Result.StatusMessage}";
                break;
        }
    }

    public async Task SaveFlashbackLast5mAsync()
    {
        if (!EnsureFlashbackActiveForExport("save_last_5m"))
        {
            return;
        }

        var file = await PickFlashbackExportFileAsync($"Flashback_Last5m_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var exportPath = ResolveUnusedFlashbackExportPath(file.Path);
        var outcome = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _sessionCoordinator.ExportFlashbackLastNSecondsAsync(300, exportPath, progress, ct, force: false));
        switch (outcome)
        {
            case ExportFlashbackOutcome.Stale:
                return;
            case ExportFlashbackOutcome.Failed failed:
                StatusText = $"Save error: {failed.ErrorMessage}";
                break;
            case ExportFlashbackOutcome.Succeeded succeeded:
                StatusText = succeeded.Result.Succeeded
                    ? FormatSuccessfulFlashbackExportStatus("Saved last 5 minutes", exportPath, succeeded.Result)
                    : $"Save failed: {succeeded.Result.StatusMessage}";
                break;
        }
    }

    private async Task<Windows.Storage.StorageFile?> PickFlashbackExportFileAsync(string suggestedFileName)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
        picker.FileTypeChoices.Add("MP4 Video", new[] { ".mp4" });
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandle);
        return await picker.PickSaveFileAsync();
    }

    private static string ResolveUnusedFlashbackExportPath(string selectedPath)
    {
        if (!File.Exists(selectedPath) && !Directory.Exists(selectedPath))
        {
            return selectedPath;
        }

        var directory = Path.GetDirectoryName(selectedPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return selectedPath;
        }

        var baseName = Path.GetFileNameWithoutExtension(selectedPath);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Flashback";
        }

        var extension = Path.GetExtension(selectedPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".mp4";
        }

        for (var suffix = 1; suffix <= 999; suffix++)
        {
            var candidate = Path.Combine(directory, $"{baseName} ({suffix}){extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{baseName}.{Guid.NewGuid():N}{extension}");
    }

    private bool EnsureFlashbackActiveForExport(string operation)
    {
        if (_sessionCoordinator.IsFlashbackActive)
        {
            return true;
        }

        Logger.Log($"FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        StatusText = "Flashback export unavailable: flashback is not active.";
        return false;
    }

    public async Task<FinalizeResult> ExportFlashbackAutomationAsync(
        double seconds, string outputPath, bool useSelectionRange, bool force, CancellationToken cancellationToken = default)
    {
        var exportId = Interlocked.Increment(ref _flashbackExportOperationId);
        var oldExportCts = _exportCts;
        CancelFlashbackExportCts(oldExportCts);
        _exportCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var exportCts = _exportCts;

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = true;
                FlashbackExportProgress = 0;
            }
        }))
        {
            Logger.Log("FLASHBACK_EXPORT_START_UI_ENQUEUE_FAILED source=automation");
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = true;
                FlashbackExportProgress = 0;
            }
        }
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                if (!_dispatcherQueue.TryEnqueue(() =>
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        FlashbackExportProgress = p.Percent;
                    }
                }))
                {
                    Logger.Log($"FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=automation percent={p.Percent:0.###}");
                }
            });

            if (useSelectionRange)
            {
                var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
                return await _sessionCoordinator.ExportFlashbackRangeAsync(
                    playback.InPoint,
                    playback.OutPoint,
                    outputPath,
                    progress,
                    exportCts.Token,
                    playback.InPointFilePts,
                    playback.OutPointFilePts,
                    force);
            }

            return await _sessionCoordinator.ExportFlashbackLastNSecondsAsync(
                seconds, outputPath, progress, exportCts.Token, force);
        }
        finally
        {
            if (!_dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        IsFlashbackExporting = false;
                        FlashbackExportProgress = 0;
                        _exportCts = null;
                    }
                }
                finally
                {
                    DisposeFlashbackExportCtsBestEffort(exportCts, "automation_dispatcher_cleanup");
                }
            }))
            {
                if (IsCurrentFlashbackExport(exportId, exportCts))
                {
                    IsFlashbackExporting = false;
                    FlashbackExportProgress = 0;
                    _exportCts = null;
                }
                DisposeFlashbackExportCtsBestEffort(exportCts, "automation_inline_cleanup");
            }
        }
    }
}

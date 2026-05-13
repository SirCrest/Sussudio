using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Preview audio monitoring state, volume persistence guards, and transition ramps.
/// </summary>
public partial class MainViewModel
{
    private const int PreviewAudioRampDownSteps = 18;
    private const int PreviewAudioRampDownDelayMs = 25;
    private const int PreviewAudioRampUpSteps = 30;
    private const int PreviewAudioRampUpDelayMs = 30;

    internal bool SuppressVolumeSave { get; set; }

    /// <summary>
    /// When non-null, SaveSettings writes this value for PreviewVolume instead of the
    /// current animation-transient property value. Set during preview volume
    /// fade-in/out to prevent intermediate 0 values from corrupting persisted settings.
    /// </summary>
    internal double? VolumeSaveOverride { get; set; }

    partial void OnPreviewVolumeChanged(double value)
    {
        if (!SuppressVolumeSave)
        {
            VolumeSaveOverride = null;
        }

        _sessionCoordinator.SetPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));
        RecordAudioRampTracePoint("volume-set");
    }

    private async Task RampPreviewVolumeDownForStopAsync(CancellationToken cancellationToken)
        => await RampPreviewVolumeDownForAudioTransitionAsync("preview_stop", cancellationToken);

    private async Task RampPreviewVolumeDownForAudioTransitionAsync(
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
    {
        var persistedVolume = Math.Clamp(VolumeSaveOverride ?? PreviewVolume, 0.0, 1.0);
        var startingVolume = Math.Clamp(PreviewVolume, 0.0, 1.0);
        var traceSessionId = traceSession ? BeginAudioRampTraceSession(reason, targetVolume: 0) : 0;
        if (persistedVolume > 0.001)
        {
            VolumeSaveOverride = persistedVolume;
        }

        try
        {
            if (startingVolume <= 0.001)
            {
                SuppressVolumeSave = true;
                try
                {
                    PreviewVolume = 0;
                }
                finally
                {
                    SuppressVolumeSave = false;
                }

                RecordAudioRampTracePoint("ramp-down-skipped", reason, targetVolume: 0, note: "already-zero");
                return;
            }

            SuppressVolumeSave = true;
            var startLog = string.Equals(reason, "preview_stop", StringComparison.Ordinal)
                ? $"PREVIEW_AUDIO_STOP_RAMP_STARTED fromPct={startingVolume * 100:0}"
                : $"PREVIEW_AUDIO_RAMP_DOWN_STARTED reason={reason} fromPct={startingVolume * 100:0}";
            Logger.Log(startLog);
            RecordAudioRampTracePoint("ramp-down-start", reason, targetVolume: 0);
            try
            {
                for (var step = 1; step <= PreviewAudioRampDownSteps; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var t = step / (double)PreviewAudioRampDownSteps;
                    var eased = Math.Pow(1.0 - t, 2.0);
                    PreviewVolume = startingVolume * eased;
                    await Task.Delay(PreviewAudioRampDownDelayMs, cancellationToken);
                }

                PreviewVolume = 0;
                RecordAudioRampTracePoint("ramp-down-complete", reason, targetVolume: 0);
                Logger.Log(
                    string.Equals(reason, "preview_stop", StringComparison.Ordinal)
                        ? "PREVIEW_AUDIO_STOP_RAMP_COMPLETED"
                        : $"PREVIEW_AUDIO_RAMP_DOWN_COMPLETED reason={reason}");
            }
            finally
            {
                SuppressVolumeSave = false;
            }
        }
        finally
        {
            if (traceSession)
            {
                CompleteAudioRampTraceSession(traceSessionId, reason);
            }
        }
    }

    private double PrimePreviewVolumeForAudioTransition(string reason)
    {
        var volumeTarget = Math.Clamp(VolumeSaveOverride ?? PreviewVolume, 0.0, 1.0);
        if (volumeTarget <= 0.001)
        {
            PreviewVolume = 0;
            VolumeSaveOverride = null;
            return 0;
        }

        VolumeSaveOverride = volumeTarget;
        SuppressVolumeSave = true;
        try
        {
            PreviewVolume = 0;
        }
        finally
        {
            SuppressVolumeSave = false;
        }

        Logger.Log($"PREVIEW_AUDIO_MONITOR_PRIMED reason={reason} targetPct={volumeTarget * 100:0}");
        RecordAudioRampTracePoint("primed", reason, volumeTarget);
        return volumeTarget;
    }

    private async Task RampPreviewVolumeUpForAudioTransitionAsync(
        double volumeTarget,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
    {
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        var traceSessionId = traceSession ? BeginAudioRampTraceSession(reason, volumeTarget) : 0;
        if (volumeTarget <= 0.001)
        {
            try
            {
                PreviewVolume = 0;
                VolumeSaveOverride = null;
                RecordAudioRampTracePoint("ramp-up-skipped", reason, volumeTarget, "target-zero");
            }
            finally
            {
                if (traceSession)
                {
                    CompleteAudioRampTraceSession(traceSessionId, reason);
                }
            }

            return;
        }

        VolumeSaveOverride = volumeTarget;
        SuppressVolumeSave = true;
        Logger.Log($"PREVIEW_AUDIO_RAMP_UP_STARTED reason={reason} targetPct={volumeTarget * 100:0}");
        RecordAudioRampTracePoint("ramp-up-start", reason, volumeTarget);
        try
        {
            for (var step = 1; step <= PreviewAudioRampUpSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var t = step / (double)PreviewAudioRampUpSteps;
                var eased = 1.0 - Math.Pow(1.0 - t, 3.0);
                PreviewVolume = volumeTarget * eased;
                await Task.Delay(PreviewAudioRampUpDelayMs, cancellationToken);
            }

            PreviewVolume = volumeTarget;
            RecordAudioRampTracePoint("ramp-up-complete", reason, volumeTarget);
            Logger.Log($"PREVIEW_AUDIO_RAMP_UP_COMPLETED reason={reason}");
        }
        finally
        {
            SuppressVolumeSave = false;
            VolumeSaveOverride = null;
            if (traceSession)
            {
                CompleteAudioRampTraceSession(traceSessionId, reason);
            }
        }
    }

    private void RestorePreviewVolumeAfterUnavailableAudio(double volumeTarget, string reason)
    {
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        SuppressVolumeSave = true;
        try
        {
            PreviewVolume = volumeTarget;
        }
        finally
        {
            SuppressVolumeSave = false;
            VolumeSaveOverride = null;
        }

        Logger.Log($"PREVIEW_AUDIO_MONITOR_RESTORE reason={reason} targetPct={volumeTarget * 100:0}");
        RecordAudioRampTracePoint("restore", reason, volumeTarget, "audio-preview-unavailable");
    }

    private async Task SetAudioMonitoringEnabledWithVolumeTransitionAsync(
        bool enabled,
        string reason,
        bool teardownCapture = false,
        Func<Task>? afterMonitoringStarted = null,
        CancellationToken cancellationToken = default)
    {
        var traceSessionId = BeginAudioRampTraceSession(
            reason,
            enabled ? Math.Clamp(VolumeSaveOverride ?? PreviewVolume, 0.0, 1.0) : 0);
        try
        {
            if (enabled)
            {
                var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);
                await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);
                RecordAudioRampTracePoint("monitoring-started", reason, volumeTarget);
                Exception? afterMonitoringStartedFailure = null;
                if (afterMonitoringStarted != null)
                {
                    try
                    {
                        await afterMonitoringStarted();
                    }
                    catch (Exception ex)
                    {
                        afterMonitoringStartedFailure = ex;
                        Logger.Log($"PREVIEW_AUDIO_MONITOR_PRIME_CALLBACK_FAIL reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
                    }
                }

                if (_captureService.IsAudioPreviewActive)
                {
                    await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);
                }
                else
                {
                    RestorePreviewVolumeAfterUnavailableAudio(volumeTarget, reason);
                }

                if (afterMonitoringStartedFailure != null)
                {
                    throw afterMonitoringStartedFailure;
                }

                return;
            }

            await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);
            if (teardownCapture)
            {
                await _sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);
                RecordAudioRampTracePoint("monitoring-stopped", reason, targetVolume: 0, note: "teardown");
            }
            else
            {
                await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);
                RecordAudioRampTracePoint("monitoring-stopped", reason, targetVolume: 0, note: "muted");
            }
        }
        finally
        {
            CompleteAudioRampTraceSession(traceSessionId, reason);
        }
    }

    internal void SavePreviewVolume() => SaveSettings();

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

    private async Task ApplyAudioInputSelectionAsync(string reason)
    {
        if (!IsInitialized)
        {
            return;
        }

        string? audioDeviceId = null;
        string? audioDeviceName = null;

        if (IsCustomAudioInputEnabled)
        {
            audioDeviceId = SelectedAudioInputDevice?.Id;
            audioDeviceName = SelectedAudioInputDevice?.Name;
        }
        else
        {
            audioDeviceId = SelectedDevice?.AudioDeviceId;
            audioDeviceName = SelectedDevice?.AudioDeviceName;
        }

        Logger.Log($"=== Updating audio input ({reason}) ===");
        Logger.Log($"  Audio device: {audioDeviceName ?? "(none)"}");

        var shouldRampMonitoring = IsPreviewing && _captureService.IsAudioPreviewActive;
        var volumeTarget = Math.Clamp(VolumeSaveOverride ?? PreviewVolume, 0.0, 1.0);
        var traceSessionId = shouldRampMonitoring
            ? BeginAudioRampTraceSession(reason, volumeTarget)
            : 0;
        try
        {
            if (shouldRampMonitoring)
            {
                await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);
            }

            await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);
            RecordAudioRampTracePoint("audio-input-updated", reason, volumeTarget);

            if (shouldRampMonitoring)
            {
                if (_captureService.IsAudioPreviewActive && IsAudioEnabled && IsAudioPreviewEnabled)
                {
                    await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);
                }
                else
                {
                    RestorePreviewVolumeAfterUnavailableAudio(volumeTarget, reason);
                }
            }
        }
        finally
        {
            if (shouldRampMonitoring)
            {
                CompleteAudioRampTraceSession(traceSessionId, reason);
            }
        }
    }
}

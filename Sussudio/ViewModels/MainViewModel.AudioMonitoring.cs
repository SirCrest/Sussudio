using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Preview audio monitoring orchestration, input retargeting, and coordinator sequencing.
/// </summary>
public partial class MainViewModel
{
    internal bool SuppressVolumeSave
    {
        get => _previewAudioVolumeTransitionController.SuppressVolumeSave;
        set => _previewAudioVolumeTransitionController.SuppressVolumeSave = value;
    }

    /// <summary>
    /// When non-null, SaveSettings writes this value for PreviewVolume instead of the
    /// current animation-transient property value. Set during preview volume
    /// fade-in/out to prevent intermediate 0 values from corrupting persisted settings.
    /// </summary>
    internal double? VolumeSaveOverride
    {
        get => _previewAudioVolumeTransitionController.VolumeSaveOverride;
        set => _previewAudioVolumeTransitionController.VolumeSaveOverride = value;
    }

    partial void OnPreviewVolumeChanged(double value)
        => _previewAudioVolumeTransitionController.HandlePreviewVolumeChanged(value);

    private async Task RampPreviewVolumeDownForStopAsync(CancellationToken cancellationToken)
        => await _previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken);

    private async Task RampPreviewVolumeDownForAudioTransitionAsync(
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
        => await _previewAudioVolumeTransitionController.RampDownForAudioTransitionAsync(
            reason,
            cancellationToken,
            traceSession);

    private double PrimePreviewVolumeForAudioTransition(string reason)
        => _previewAudioVolumeTransitionController.PrimeForAudioTransition(reason);

    private async Task RampPreviewVolumeUpForAudioTransitionAsync(
        double volumeTarget,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
        => await _previewAudioVolumeTransitionController.RampUpForAudioTransitionAsync(
            volumeTarget,
            reason,
            cancellationToken,
            traceSession);

    private void RestorePreviewVolumeAfterUnavailableAudio(double volumeTarget, string reason)
        => _previewAudioVolumeTransitionController.RestoreAfterUnavailableAudio(volumeTarget, reason);

    private async Task SetAudioMonitoringEnabledWithVolumeTransitionAsync(
        bool enabled,
        string reason,
        bool teardownCapture = false,
        Func<Task>? afterMonitoringStarted = null,
        CancellationToken cancellationToken = default)
    {
        var traceSessionId = BeginAudioRampTraceSession(
            reason,
            enabled ? _previewAudioVolumeTransitionController.PersistedVolumeTarget : 0);
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

}

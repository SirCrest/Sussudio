using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<AudioInputDevice> AudioInputDevices { get; set; } = new();

    [ObservableProperty]
    public partial AudioInputDevice? SelectedAudioInputDevice { get; set; }

    [ObservableProperty]
    public partial bool IsCustomAudioInputEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AudioInputDevice> MicrophoneDevices { get; set; } = new();

    [ObservableProperty]
    public partial bool IsMicrophoneEnabled { get; set; }

    [ObservableProperty]
    public partial AudioInputDevice? SelectedMicrophoneDevice { get; set; }

    private string? _pendingSavedAudioDeviceId;
    private string? _pendingSavedMicrophoneDeviceId;
    private double? _pendingSavedMicrophoneVolume;
    private string? _pendingSavedMicrophoneVolumeDeviceId;
    private int _audioEnabledChangeGeneration;
    private bool _suppressAudioPreviewEnabledChangeOperation;
    private bool _suppressMicrophoneMonitorUpdate;

    [ObservableProperty]
    public partial bool IsAudioEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAudioPreviewEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAudioPreviewActive { get; set; }

    [ObservableProperty]
    public partial double PreviewVolume { get; set; } = 1.0;

    [ObservableProperty]
    public partial double MicrophoneVolume { get; set; } = 100.0;

    [ObservableProperty]
    public partial double AudioPeak { get; set; }

    [ObservableProperty]
    public partial bool AudioClipping { get; set; }

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

    private AudioRampTraceRecorder CreateAudioRampTraceRecorder()
    {
        return new AudioRampTraceRecorder(
            new AudioRampTraceRecorderContext
            {
                GetRuntimeSnapshot = () => _captureService.GetRuntimeSnapshot(),
                GetPreviewVolume = () => PreviewVolume,
                GetIsAudioEnabled = () => IsAudioEnabled,
                GetIsAudioPreviewEnabled = () => IsAudioPreviewEnabled,
                GetAudioPeak = () => AudioPeak,
                Log = message => Logger.Log(message),
            });
    }

    private PreviewAudioVolumeTransitionController CreatePreviewAudioVolumeTransitionController()
    {
        return new PreviewAudioVolumeTransitionController(
            new PreviewAudioVolumeTransitionControllerContext
            {
                GetPreviewVolume = () => PreviewVolume,
                SetPreviewVolume = value => PreviewVolume = value,
                SetSessionPreviewVolume = volume => _sessionCoordinator.SetPreviewVolume(volume),
                BeginTraceSession = BeginAudioRampTraceSession,
                CompleteTraceSession = CompleteAudioRampTraceSession,
                RecordTracePoint = RecordAudioRampTracePoint,
                Log = (message, caller) => Logger.Log(message, caller),
            });
    }

    public AudioRampTraceSnapshot GetAudioRampTraceSnapshot(int maxEntries = 512)
        => _audioRampTraceRecorder.GetSnapshot(maxEntries);

    public Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync(
        int maxEntries = 512,
        CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(() => GetAudioRampTraceSnapshot(maxEntries), cancellationToken);

    private long BeginAudioRampTraceSession(string reason, double targetVolume)
        => _audioRampTraceRecorder.BeginSession(reason, targetVolume);

    private void CompleteAudioRampTraceSession(long sessionId, string reason)
        => _audioRampTraceRecorder.CompleteSession(sessionId, reason);

    private void RecordAudioRampTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
        => _audioRampTraceRecorder.RecordPoint(kind, reason, targetVolume, note, sessionId);

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

    internal void SavePreviewVolume() => SaveSettings();

    partial void OnMicrophoneVolumeChanged(double value)
    {
        try
        {
            SetMicrophoneEndpointVolume(value);
        }
        catch (Exception ex)
        {
            Logger.Log($"OnMicrophoneVolumeChanged failed: {ex.Message}");
        }
    }

    internal void SaveMicrophoneVolume() => SaveSettings();

    public void SetMicrophoneEndpointVolume(double volumePercent)
    {
        var deviceId = SelectedMicrophoneDevice?.Id;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        try
        {
            WasapiComInterop.SetEndpointVolume(deviceId, (float)(Math.Clamp(volumePercent, 0.0, 100.0) / 100.0));
        }
        catch (Exception ex)
        {
            Logger.Log($"SetMicrophoneEndpointVolume failed for device '{deviceId}': {ex.Message}");
        }
    }

    public double GetMicrophoneEndpointVolume()
    {
        var deviceId = SelectedMicrophoneDevice?.Id;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return 100.0;
        }

        try
        {
            return WasapiComInterop.GetEndpointVolume(deviceId) * 100.0;
        }
        catch (Exception ex)
        {
            Logger.Log($"GetMicrophoneEndpointVolume failed for device '{deviceId}': {ex.Message}");
            return 100.0;
        }
    }

    partial void OnIsMicrophoneEnabledChanged(bool value)
    {
        SaveSettings();
        if (_suppressMicrophoneMonitorUpdate)
        {
            return;
        }

        if (!IsRecording)
        {
            var device = SelectedMicrophoneDevice;
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(value, device?.Id, device?.Name),
                "mic monitor toggle");
        }
    }

    partial void OnSelectedMicrophoneDeviceChanged(AudioInputDevice? value)
    {
        if (value != null)
        {
            try
            {
                var pendingSavedVolume = _pendingSavedMicrophoneVolume;
                var pendingSavedVolumeDeviceId = _pendingSavedMicrophoneVolumeDeviceId;
                if (pendingSavedVolume.HasValue &&
                    string.Equals(value.Id, pendingSavedVolumeDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    _pendingSavedMicrophoneVolume = null;
                    _pendingSavedMicrophoneVolumeDeviceId = null;
                    var savedVolume = Math.Clamp(pendingSavedVolume.Value, 0.0, 100.0);
                    if (Math.Abs(MicrophoneVolume - savedVolume) > 0.5)
                    {
                        MicrophoneVolume = savedVolume;
                    }
                    else
                    {
                        SetMicrophoneEndpointVolume(savedVolume);
                    }
                }
                else
                {
                    _pendingSavedMicrophoneVolume = null;
                    _pendingSavedMicrophoneVolumeDeviceId = null;
                    var endpointVolume = GetMicrophoneEndpointVolume();
                    if (Math.Abs(MicrophoneVolume - endpointVolume) > 0.5)
                    {
                        MicrophoneVolume = endpointVolume;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainViewModel mic volume readback: {ex.Message}");
            }
        }

        SaveSettings();

        if (IsMicrophoneEnabled && !IsRecording && value != null)
        {
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(true, value.Id, value.Name),
                "mic monitor device switch");
        }
    }
}

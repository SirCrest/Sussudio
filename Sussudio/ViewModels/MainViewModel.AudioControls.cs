using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio control state, persistence, and device-specific audio mode management.
/// </summary>
public partial class MainViewModel
{
    private const int PreviewAudioRampDownSteps = 18;
    private const int PreviewAudioRampDownDelayMs = 25;
    private const int PreviewAudioRampUpSteps = 30;
    private const int PreviewAudioRampUpDelayMs = 30;
    private const int AudioRampTraceCapacity = 2048;
    private const int AudioRampTraceSampleIntervalMs = 10;
    private const int AudioRampTracePostCompleteSampleMs = 250;

    internal bool SuppressVolumeSave { get; set; }

    /// <summary>
    /// When non-null, SaveSettings writes this value for PreviewVolume instead of the
    /// current (animation-transient) property value. Set during preview volume
    /// fade-in/out to prevent intermediate 0 values from corrupting persisted settings.
    /// </summary>
    internal double? VolumeSaveOverride { get; set; }

    // Short-lived ring buffer for audible transition forensics. It captures the
    // control target and actual WASAPI render envelope so a reported pop, mute,
    // or stutter can be correlated with the playback thread rather than guessed.
    private readonly object _audioRampTraceLock = new();
    private readonly AudioRampTraceEntry[] _audioRampTraceBuffer = new AudioRampTraceEntry[AudioRampTraceCapacity];
    private CancellationTokenSource? _audioRampTraceSamplerCts;
    private int _audioRampTraceHead;
    private int _audioRampTraceCount;
    private long _audioRampTraceSequence;
    private long _audioRampTraceActiveSessionId;
    private long _audioRampTraceSessionStartTimestamp;
    private string _audioRampTraceActiveReason = string.Empty;
    private double _audioRampTraceTargetVolume = double.NaN;
    private bool _audioRampTraceSamplingActive;

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

    public AudioRampTraceSnapshot GetAudioRampTraceSnapshot(int maxEntries = 512)
    {
        lock (_audioRampTraceLock)
        {
            var count = Math.Min(_audioRampTraceCount, Math.Clamp(maxEntries, 0, AudioRampTraceCapacity));
            var entries = count == 0
                ? Array.Empty<AudioRampTraceEntry>()
                : new AudioRampTraceEntry[count];

            if (count > 0)
            {
                var oldest = (_audioRampTraceHead - _audioRampTraceCount + AudioRampTraceCapacity) % AudioRampTraceCapacity;
                var skip = _audioRampTraceCount - count;
                var readIndex = (oldest + skip) % AudioRampTraceCapacity;
                for (var i = 0; i < count; i++)
                {
                    entries[i] = _audioRampTraceBuffer[readIndex];
                    readIndex = (readIndex + 1) % AudioRampTraceCapacity;
                }
            }

            return new AudioRampTraceSnapshot
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                SampleIntervalMs = AudioRampTraceSampleIntervalMs,
                Capacity = AudioRampTraceCapacity,
                EntryCount = _audioRampTraceCount,
                IsSamplingActive = _audioRampTraceSamplingActive,
                ActiveSessionId = _audioRampTraceActiveSessionId,
                ActiveReason = _audioRampTraceActiveReason,
                Entries = entries
            };
        }
    }

    public Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync(
        int maxEntries = 512,
        CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(() => GetAudioRampTraceSnapshot(maxEntries), cancellationToken);

    private long BeginAudioRampTraceSession(string reason, double targetVolume)
    {
        CancellationTokenSource? previousCts = null;
        var cts = new CancellationTokenSource();
        long sessionId;
        lock (_audioRampTraceLock)
        {
            previousCts = _audioRampTraceSamplerCts;
            previousCts?.Cancel();
            _audioRampTraceSamplerCts = cts;
            sessionId = _audioRampTraceActiveSessionId + 1;
            _audioRampTraceActiveSessionId = sessionId;
            _audioRampTraceSessionStartTimestamp = Stopwatch.GetTimestamp();
            _audioRampTraceActiveReason = reason;
            _audioRampTraceTargetVolume = Math.Clamp(targetVolume, 0.0, 1.0);
            _audioRampTraceSamplingActive = true;
        }

        RecordAudioRampTracePoint("session-start", reason, targetVolume);
        _ = RunAudioRampTraceSamplerAsync(sessionId, cts);
        return sessionId;
    }

    private void CompleteAudioRampTraceSession(long sessionId, string reason)
    {
        if (sessionId <= 0)
        {
            return;
        }

        RecordAudioRampTracePoint("session-complete", reason);
        _ = StopAudioRampTraceSamplerAfterDelayAsync(sessionId, AudioRampTracePostCompleteSampleMs);
    }

    private async Task RunAudioRampTraceSamplerAsync(long sessionId, CancellationTokenSource cts)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                RecordAudioRampTracePoint("sample", sessionId: sessionId);
                await Task.Delay(AudioRampTraceSampleIntervalMs, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a trace session completes or is superseded.
        }
        catch (Exception ex)
        {
            Logger.Log($"AUDIO_RAMP_TRACE_SAMPLER_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task StopAudioRampTraceSamplerAfterDelayAsync(long sessionId, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
        }
        catch
        {
            return;
        }

        CancellationTokenSource? cts = null;
        lock (_audioRampTraceLock)
        {
            if (_audioRampTraceActiveSessionId != sessionId)
            {
                return;
            }

            cts = _audioRampTraceSamplerCts;
            _audioRampTraceSamplerCts = null;
            _audioRampTraceSamplingActive = false;
        }

        cts?.Cancel();
    }

    private void RecordAudioRampTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
    {
        long activeSessionId;
        long sessionStartTimestamp;
        string activeReason;
        double activeTargetVolume;
        bool shouldRecord;
        lock (_audioRampTraceLock)
        {
            activeSessionId = sessionId ?? _audioRampTraceActiveSessionId;
            sessionStartTimestamp = _audioRampTraceSessionStartTimestamp;
            activeReason = reason ?? _audioRampTraceActiveReason;
            activeTargetVolume = targetVolume ?? _audioRampTraceTargetVolume;
            shouldRecord = _audioRampTraceSamplingActive || !string.Equals(kind, "volume-set", StringComparison.Ordinal);
        }

        if (!shouldRecord)
        {
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var nowTimestamp = Stopwatch.GetTimestamp();
        var elapsedMs = sessionStartTimestamp > 0
            ? Stopwatch.GetElapsedTime(sessionStartTimestamp, nowTimestamp).TotalMilliseconds
            : 0;
        var runtime = _captureService.GetRuntimeSnapshot();
        var outputAgeMs = runtime.WasapiPlaybackOutputLevelLastTickMs > 0
            ? Math.Max(0, Environment.TickCount64 - runtime.WasapiPlaybackOutputLevelLastTickMs)
            : 0;

        var entry = new AudioRampTraceEntry
        {
            Sequence = Interlocked.Increment(ref _audioRampTraceSequence),
            SessionId = activeSessionId,
            Kind = kind,
            Reason = activeReason,
            Note = note ?? string.Empty,
            TimestampUtc = nowUtc,
            ElapsedMs = elapsedMs,
            PreviewVolumePercent = Math.Clamp(PreviewVolume, 0.0, 1.0) * 100.0,
            TargetVolumePercent = double.IsNaN(activeTargetVolume) ? 0 : Math.Clamp(activeTargetVolume, 0.0, 1.0) * 100.0,
            PlaybackTargetVolumePercent = runtime.WasapiPlaybackTargetVolumePercent,
            PlaybackCurrentVolumePercent = runtime.WasapiPlaybackCurrentVolumePercent,
            PlaybackOutputPeak = runtime.WasapiPlaybackOutputPeak,
            PlaybackOutputRms = runtime.WasapiPlaybackOutputRms,
            PlaybackOutputAgeMs = outputAgeMs,
            PlaybackRenderCallbackCount = runtime.WasapiPlaybackRenderCallbackCount,
            PlaybackQueueDepth = runtime.WasapiPlaybackQueueDepth,
            IsAudioEnabled = IsAudioEnabled,
            IsAudioPreviewEnabled = IsAudioPreviewEnabled,
            IsAudioPreviewActive = runtime.IsAudioPreviewActive,
            AudioReaderActive = runtime.AudioReaderActive,
            CaptureAudioPeak = AudioPeak,
            AudioFramesArrived = runtime.AudioFramesArrived
        };

        lock (_audioRampTraceLock)
        {
            _audioRampTraceBuffer[_audioRampTraceHead] = entry;
            _audioRampTraceHead = (_audioRampTraceHead + 1) % AudioRampTraceCapacity;
            if (_audioRampTraceCount < AudioRampTraceCapacity)
            {
                _audioRampTraceCount++;
            }
        }
    }

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

    internal void SavePreviewVolume() => SaveSettings();

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

    private async Task RefreshDeviceAudioControlsAsync(
        CaptureDevice? targetDevice,
        bool applySavedState,
        CancellationToken cancellationToken)
    {
        var device = targetDevice;
        if (device == null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SelectedDevice != null)
            {
                return;
            }

            WithAudioControlRefreshSuppressed(() =>
            {
                IsDeviceAudioControlSupported = false;
                SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;
                AnalogAudioGainPercent = 50;
            });

            return;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            return;
        }

        if (NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out _, out _))
        {
            WithAudioControlRefreshSuppressed(() => IsDeviceAudioControlSupported = true);
        }

        var state = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log("Device audio controls refresh ignored because selected device changed");
            return;
        }

        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = state.IsSupported;
            if (state.IsSupported)
            {
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(state.Mode ?? _pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
                AnalogAudioGainPercent = Math.Clamp(
                    state.AnalogGainPercent ?? _pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent,
                    0.0,
                    100.0);
            }
            else
            {
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(_pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
                AnalogAudioGainPercent = Math.Clamp(_pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
            }
        });

        if (!applySavedState || !state.IsSupported)
        {
            return;
        }

        var desiredMode = NormalizeDeviceAudioMode(_pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
        var desiredGain = Math.Clamp(_pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent, 0.0, 100.0);

        Logger.Log($"NATIVEXU_AUDIO_RESTORE_READ_ONLY desired='{desiredMode}' device='{state.Mode}'");

        var refreshedState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log("Device audio controls restore ignored because selected device changed");
            return;
        }

        _pendingSavedDeviceAudioMode = null;
        _pendingSavedAnalogAudioGainPercent = null;
        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = refreshedState.IsSupported;
            SelectedDeviceAudioMode = NormalizeDeviceAudioMode(refreshedState.Mode ?? desiredMode);
            AnalogAudioGainPercent = Math.Clamp(refreshedState.AnalogGainPercent ?? desiredGain, 0.0, 100.0);
        });
    }

    private async Task<bool> ApplyDeviceAudioModeAsync(
        string reason,
        string? explicitMode = null,
        bool reapplyAnalogGain = true,
        bool persistSettings = true,
        CaptureDevice? targetDevice = null,
        CancellationToken cancellationToken = default)
    {
        var device = targetDevice ?? SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return false;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Device audio mode skipped because selected device changed ({reason})");
            return false;
        }

        var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);
        Logger.Log($"=== Updating device audio mode ({reason}) ===");
        Logger.Log($"  Mode: {mode}");

        var isAnalog = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var gainByte = MapPercentToGainByte(AnalogAudioGainPercent);
        var applied = await NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte, cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentSelectedDevice(device))
            {
                Logger.Log($"Device audio mode failure ignored because selected device changed ({reason})");
                return false;
            }

            var failureState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentSelectedDevice(device))
            {
                Logger.Log($"Device audio mode failure readback ignored because selected device changed ({reason})");
                return false;
            }

            WithAudioControlRefreshSuppressed(() =>
            {
                IsDeviceAudioControlSupported = failureState.IsSupported;
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(failureState.Mode ?? SelectedDeviceAudioMode);
                if (failureState.AnalogGainPercent.HasValue)
                {
                    AnalogAudioGainPercent = Math.Clamp(failureState.AnalogGainPercent.Value, 0.0, 100.0);
                }
            });

            StatusText = $"Device audio mode change failed ({mode})";
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Device audio mode result ignored because selected device changed ({reason})");
            return false;
        }

        StatusText = $"Device audio mode set to {mode}";
        if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            var gainApplied = await ApplyAnalogAudioGainAsync(
                "analog gain after mode switch",
                AnalogAudioGainPercent,
                persistSettings: false,
                targetDevice: device,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!gainApplied)
            {
                return false;
            }
        }

        WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }

    private async Task<bool> ApplyAnalogAudioGainAsync(
        string reason,
        double? explicitPercent = null,
        bool persistSettings = true,
        CaptureDevice? targetDevice = null,
        CancellationToken cancellationToken = default)
    {
        var device = targetDevice ?? SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return false;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Analog audio gain skipped because selected device changed ({reason})");
            return false;
        }

        var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        var gainByte = MapPercentToGainByte(gainPercent);
        Logger.Log($"=== Updating analog audio gain ({reason}) ===");
        Logger.Log($"  GainPercent: {gainPercent:0} GainByte: 0x{gainByte:X2}");

        var applied = await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            StatusText = $"Analog audio gain change failed ({gainPercent:0}%)";
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Analog audio gain result ignored because selected device changed ({reason})");
            return false;
        }

        StatusText = $"Analog audio gain set to {gainPercent:0}%";
        WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);

        var oldCts = _gainFlashDebounceCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _gainFlashDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested && IsCurrentSelectedDevice(device))
                {
                    await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                /* Superseded by a newer gain change - expected */
            }
            finally
            {
                if (ReferenceEquals(_gainFlashDebounceCts, cts))
                {
                    _gainFlashDebounceCts = null;
                }

                cts.Dispose();
            }
        });

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }

    private bool IsCurrentSelectedDevice(CaptureDevice device)
    {
        var selected = SelectedDevice;
        if (selected == null)
        {
            return false;
        }

        return string.Equals(selected.Id, device.Id, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(selected.NativeXuInterfacePath, device.NativeXuInterfacePath, StringComparison.OrdinalIgnoreCase);
    }

    private void WithAudioControlRefreshSuppressed(Action action)
    {
        _isRefreshingDeviceAudioControls = true;
        try
        {
            action();
        }
        finally
        {
            _isRefreshingDeviceAudioControls = false;
        }
    }

    private string NormalizeDeviceAudioMode(string? mode)
        => string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)
            ? DeviceAudioMode.Analog
            : DeviceAudioMode.Hdmi;

    private async Task<bool> TryApplyAtDeviceAudioModeAsync(CaptureDevice device, string mode)
    {
        var analogMode = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var desiredSource = analogMode ? 1 : 0;

        var currentSource = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, 0x35, "InputSourceCheck").ConfigureAwait(false);
        if (currentSource is { Length: >= 1 } && currentSource[0] == desiredSource)
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_AT_SKIP mode='{mode}' already={desiredSource}");
            return true;
        }

        var wasPreviewing = IsPreviewing;
        if (wasPreviewing)
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_AT_STOP_PREVIEW mode='{mode}'");
            try
            {
                // Native XU audio-mode change requires a full pipeline rebuild to pick
                // up the new input source — force teardown rather than the normal
                // preview-only detach.
                await StopPreviewAsync(userInitiated: false, teardownPipeline: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"NATIVEXU_AUDIO_MODE_AT_STOP_PREVIEW_WARN error={ex.Message}");
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        var inputApplied = await NativeXuAtCommandProvider.SetInputSourceAsync(device, desiredSource).ConfigureAwait(false);
        Logger.Log($"NATIVEXU_AUDIO_MODE_AT mode='{mode}' inputApplied={inputApplied}");

        if (wasPreviewing)
        {
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var delayMs = attempt * 1000;
                Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_PREVIEW mode='{mode}' attempt={attempt} delayMs={delayMs}");
                await Task.Delay(delayMs).ConfigureAwait(false);
                try
                {
                    await RefreshDevicesAsync().ConfigureAwait(false);
                    await StartPreviewAsync(userInitiated: false).ConfigureAwait(false);
                    Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_OK attempt={attempt}");
                    break;
                }
                catch (Exception ex) when (attempt < 5)
                {
                    Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_RETRY attempt={attempt} error={ex.Message}");
                }
            }
        }

        return inputApplied;
    }

    private const double GainCurveK = 4.0;

    private static byte MapPercentToGainByte(double percent)
    {
        var x = Math.Clamp(percent / 100.0, 0.0, 1.0);
        var curved = Math.Log(1.0 + x * (Math.Exp(GainCurveK) - 1.0)) / GainCurveK;
        return (byte)Math.Clamp(Math.Round(curved * 255.0), 0, 255);
    }

    private static double MapGainByteToPercent(byte gainByte)
    {
        var y = gainByte / 255.0;
        var x = (Math.Exp(GainCurveK * y) - 1.0) / (Math.Exp(GainCurveK) - 1.0);
        return Math.Clamp(x * 100.0, 0.0, 100.0);
    }
}

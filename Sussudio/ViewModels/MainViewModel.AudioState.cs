using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;

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
    private int _audioInputSwitchGeneration;
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

    /// <summary>
    /// Written by WASAPI callback thread via Volatile.Write, read by UI timer.
    /// Bypasses PropertyChanged to avoid per-frame dispatch + 53-case switch overhead.
    /// </summary>
    public double AudioMeterTarget;
    public double MicrophoneMeterTarget;
    public bool MicrophoneClipping { get; set; }
    private int _audioMeterTimerNeeded;
    private int _microphoneMeterTimerNeeded;

    /// <summary>
    /// Fires once when audio transitions from silent to active, signaling MainWindow
    /// to start the audio meter animation timer. Reset when the timer stops itself.
    /// </summary>
    public event Action? AudioMeterActivated;
    public event Action? MicrophoneMeterActivated;

    private const double MeterFloorDb = -60.0;
    private const double MeterDecayDbPerSecond = 40.0 / 1.7; // OBS-like PPM decay
    private double _audioMeterDb = MeterFloorDb;
    private long _audioMeterLastTick;
    private double _micMeterDb = MeterFloorDb;
    private long _micMeterLastTick;

    private void OnAudioDevicesChanged()
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            _ = RefreshAudioDeviceListAsync();
        }))
        {
            Logger.Log("AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        }
    }

    private void ApplyStartupAudioDeviceScan(
        List<AudioInputDevice> audioDevices,
        IReadOnlyList<CaptureDevice> videoDevices,
        string? previousDeviceId,
        string? previousAudioId,
        string? previousMicrophoneId)
    {
        var savedAudioId = _pendingSavedAudioDeviceId;
        _pendingSavedAudioDeviceId = null;
        var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
        _pendingSavedMicrophoneDeviceId = null;
        var selection = AudioDeviceSelectionPolicy.SelectStartup(
            audioDevices,
            videoDevices,
            previousDeviceId,
            previousAudioId,
            savedAudioId,
            previousMicrophoneId,
            savedMicrophoneId);

        ReplaceCollection(AudioInputDevices, selection.AvailableDevices);
        ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);
        SelectedAudioInputDevice = selection.SelectedAudioInputDevice;
        SelectedMicrophoneDevice = selection.SelectedMicrophoneDevice;

        if (selection.ShouldLogSavedAudioFallback)
        {
            Logger.Log($"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.");
        }

        if (selection.ShouldLogSavedMicrophoneFallback)
        {
            Logger.Log($"SETTINGS_RESTORE: saved microphone device '{savedMicrophoneId}' not found, using fallback.");
        }
    }

    private async Task RefreshAudioDeviceListAsync()
    {
        try
        {
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousMicrophoneId = SelectedMicrophoneDevice?.Id;
            var audioDevices = (await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync()).ToList();
            var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
            var selection = AudioDeviceSelectionPolicy.SelectRefresh(
                audioDevices,
                SelectedDevice?.AudioDeviceId,
                previousAudioId,
                previousMicrophoneId,
                savedMicrophoneId);

            ReplaceCollection(AudioInputDevices, selection.AvailableDevices);
            ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);
            SelectedAudioInputDevice = selection.SelectedAudioInputDevice;
            SelectedMicrophoneDevice = selection.SelectedMicrophoneDevice;

            Logger.Log($"Audio device list refreshed ({AudioInputDevices.Count} devices).");
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio device list refresh failed: {ex.Message}");
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

    partial void OnIsCustomAudioInputEnabledChanged(bool value)
    {
        if (IsRecording)
        {
            Logger.Log("Custom audio input change ignored while recording");
            return;
        }

        if (value)
        {
            if (AudioInputDevices.Count == 0)
            {
                Logger.Log("Custom audio input enabled but no audio devices found");
                IsCustomAudioInputEnabled = false;
                return;
            }

            if (SelectedAudioInputDevice == null)
            {
                SelectedAudioInputDevice = AudioInputDevices[0];
            }
        }

        var audioInputSwitchGen = Interlocked.Increment(ref _audioInputSwitchGeneration);
        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio toggle", audioInputSwitchGen), "custom audio toggle");
        SaveSettings();
    }

    partial void OnSelectedAudioInputDeviceChanged(AudioInputDevice? value)
    {
        if (IsRecording)
        {
            return;
        }

        if (!IsCustomAudioInputEnabled || value == null)
        {
            return;
        }

        var audioInputSwitchGen = Interlocked.Increment(ref _audioInputSwitchGeneration);
        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio device change", audioInputSwitchGen), "custom audio device change");
        SaveSettings();
    }

    private async Task ApplyAudioInputSelectionAsync(string reason, int generation = 0)
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
        var volumeTarget = _previewAudioVolumeTransitionController.PersistedVolumeTarget;
        var traceSessionId = shouldRampMonitoring
            ? BeginAudioRampTraceSession(reason, volumeTarget)
            : 0;
        try
        {
            if (shouldRampMonitoring)
            {
                await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);
            }

            if (generation != 0 && generation != Volatile.Read(ref _audioInputSwitchGeneration))
            {
                Logger.Log($"AUDIO_INPUT_SWITCH_SKIP reason=stale_generation captured={generation} current={Volatile.Read(ref _audioInputSwitchGeneration)}");
                if (shouldRampMonitoring)
                {
                    RestorePreviewVolumeAfterUnavailableAudio(volumeTarget, reason);
                }

                return;
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

    partial void OnIsAudioEnabledChanged(bool value)
    {
        Logger.Log($"Audio capture enabled: {value}");
        var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);

        if (value)
        {
            // Re-enable audio preview and start it if we're already previewing.
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

    private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak, ref _audioMeterDb, ref _audioMeterLastTick);
        Volatile.Write(ref AudioMeterTarget, level);
        AudioPeak = e.Peak;

        if (level > 0 && Interlocked.CompareExchange(ref _audioMeterTimerNeeded, 1, 0) == 0)
        {
            _dispatcherQueue.TryEnqueue(() => AudioMeterActivated?.Invoke());
        }

        if (e.Clipped)
        {
            _dispatcherQueue.TryEnqueue(() => AudioClipping = true);
        }
    }

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak, ref _micMeterDb, ref _micMeterLastTick);
        Volatile.Write(ref MicrophoneMeterTarget, level);
        MicrophoneClipping = e.Clipped;

        if (level > 0 && Interlocked.CompareExchange(ref _microphoneMeterTimerNeeded, 1, 0) == 0)
        {
            _dispatcherQueue.TryEnqueue(() => MicrophoneMeterActivated?.Invoke());
        }
    }

    private void ResetAudioMeter()
    {
        _audioMeterDb = MeterFloorDb;
        _audioMeterLastTick = 0;
        _micMeterDb = MeterFloorDb;
        _micMeterLastTick = 0;
        AudioPeak = 0;
        Volatile.Write(ref AudioMeterTarget, 0.0);
        Volatile.Write(ref MicrophoneMeterTarget, 0.0);
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        Interlocked.Exchange(ref _microphoneMeterTimerNeeded, 0);
        AudioClipping = false;
        MicrophoneClipping = false;
    }

    public void ResetAudioMeterTimerFlag()
    {
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        Interlocked.Exchange(ref _microphoneMeterTimerNeeded, 0);
    }

    private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)
    {
        var targetDb = peak > 0 ? 20.0 * Math.Log10(peak) : MeterFloorDb;
        if (targetDb < MeterFloorDb) targetDb = MeterFloorDb;
        if (targetDb > 0) targetDb = 0;

        var nowTick = Environment.TickCount64;
        if (lastTick == 0)
        {
            meterDb = targetDb;
            lastTick = nowTick;
        }
        else
        {
            var dtSeconds = Math.Max(0, (nowTick - lastTick) / 1000.0);
            lastTick = nowTick;

            if (targetDb >= meterDb)
            {
                meterDb = targetDb;
            }
            else
            {
                var decay = MeterDecayDbPerSecond * dtSeconds;
                meterDb = Math.Max(targetDb, meterDb - decay);
            }
        }

        var level = (meterDb - MeterFloorDb) / -MeterFloorDb;
        return Math.Clamp(level, 0, 1);
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

        if (_suppressMicrophoneMonitorUpdate)
        {
            return;
        }

        if (IsMicrophoneEnabled && !IsRecording && value != null)
        {
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(true, value.Id, value.Name),
                "mic monitor device switch");
        }
    }

    private string? _pendingSavedDeviceAudioMode;
    private double? _pendingSavedAnalogAudioGainPercent;
    private bool _isRefreshingDeviceAudioControls;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableDeviceAudioModes { get; set; } = new()
    {
        DeviceAudioMode.Hdmi,
        DeviceAudioMode.Analog
    };

    [ObservableProperty]
    public partial bool IsDeviceAudioControlSupported { get; set; }

    [ObservableProperty]
    public partial string SelectedDeviceAudioMode { get; set; } = DeviceAudioMode.Hdmi;

    [ObservableProperty]
    public partial double AnalogAudioGainPercent { get; set; } = 50;

    partial void OnSelectedDeviceAudioModeChanged(string value)
        => _deviceAudioRequestController.HandleSelectedDeviceAudioModeChanged(value);

    partial void OnAnalogAudioGainPercentChanged(double value)
        => _deviceAudioRequestController.HandleAnalogAudioGainPercentChanged(value);

    private void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)
        => _deviceAudioRequestController.RequestDeviceAudioControlsRefresh(targetDevice);

    private void RequestAnalogGainFlashPersist(CaptureDevice device, byte gainByte)
        => _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);

    private void CancelPendingAudioControlWork()
        => _deviceAudioRequestController.CancelPendingAudioControlWork();

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

    // Device-native audio-control support probing and state readback.
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

        if (NativeXuDeviceSupport.TryGetSupported4kXIds(device, out _, out _))
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

    // Device-native audio mode switching and failure readback.
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
        var gainByte = DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent);
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
        var gainByte = DeviceAudioGainMapper.PercentToGainByte(gainPercent);
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
        RequestAnalogGainFlashPersist(device, gainByte);

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }
}

internal static class DeviceAudioGainMapper
{
    private const double GainCurveK = 4.0;

    internal static byte PercentToGainByte(double percent)
    {
        var x = Math.Clamp(percent / 100.0, 0.0, 1.0);
        var curved = Math.Log(1.0 + x * (Math.Exp(GainCurveK) - 1.0)) / GainCurveK;
        return (byte)Math.Clamp(Math.Round(curved * 255.0), 0, 255);
    }

    internal static double GainByteToPercent(byte gainByte)
    {
        var y = gainByte / 255.0;
        var x = (Math.Exp(GainCurveK * y) - 1.0) / (Math.Exp(GainCurveK) - 1.0);
        return Math.Clamp(x * 100.0, 0.0, 100.0);
    }
}

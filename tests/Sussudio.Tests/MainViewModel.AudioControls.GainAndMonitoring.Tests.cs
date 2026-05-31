using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelAudioControls_MapsAnalogGainCurveAndClamps()
    {
        var mapperType = RequireType("Sussudio.ViewModels.DeviceAudioGainMapper");
        var mapPercent = mapperType.GetMethod("PercentToGainByte", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.PercentToGainByte was not found.");
        var mapByte = mapperType.GetMethod("GainByteToPercent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.GainByteToPercent was not found.");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceAudioModeText, "DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent)");
        AssertContains(deviceAudioStateText, "DeviceAudioGainMapper.PercentToGainByte(gainPercent)");
        AssertContains(deviceAudioStateText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertDoesNotContain(deviceAudioStateText, "private static byte MapPercentToGainByte");
        AssertDoesNotContain(deviceAudioStateText, "private static double MapGainByteToPercent");
        AssertContains(deviceAudioStateText, "internal static class DeviceAudioGainMapper");
        AssertContains(deviceAudioStateText, "private const double GainCurveK = 4.0;");
        AssertContains(deviceAudioStateText, "internal static byte PercentToGainByte");
        AssertContains(deviceAudioStateText, "internal static double GainByteToPercent");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "DeviceAudioGainMapper.cs")), "DeviceAudioGainMapper folded into MainViewModel.DeviceAudioState.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")), "analog gain XU writes folded into MainViewModel.DeviceAudioState.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs")), "device audio mode folded into MainViewModel.DeviceAudioState.cs");

        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { -25d })!, "PercentToGainByte clamps below zero");
        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { 0d })!, "PercentToGainByte zero");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 100d })!, "PercentToGainByte one hundred");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 150d })!, "PercentToGainByte clamps above one hundred");

        var gain25 = (byte)mapPercent.Invoke(null, new object[] { 25d })!;
        var gain50 = (byte)mapPercent.Invoke(null, new object[] { 50d })!;
        var gain75 = (byte)mapPercent.Invoke(null, new object[] { 75d })!;
        AssertEqual(true, gain25 > 0 && gain25 < gain50 && gain50 < gain75 && gain75 < 255, "PercentToGainByte monotonic curve");

        AssertNear(0d, (double)mapByte.Invoke(null, new object[] { (byte)0 })!, 0.0001d, "GainByteToPercent zero");
        AssertNear(100d, (double)mapByte.Invoke(null, new object[] { (byte)255 })!, 0.0001d, "GainByteToPercent max");
        AssertNear(50d, (double)mapByte.Invoke(null, new object[] { gain50 })!, 1.0d, "GainByteToPercent round-trip midpoint");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetProperty("SuppressVolumeSave", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SuppressVolumeSave");
        AssertNotNull(viewModelType.GetProperty("VolumeSaveOverride", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.VolumeSaveOverride");
        AssertNotNull(viewModelType.GetMethod("SavePreviewVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SavePreviewVolume");

        var audioStateCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var transitionCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs");
        var previewChanged = ExtractMemberCode(audioStateCode, "OnPreviewVolumeChanged");
        var handlePreviewChanged = ExtractMemberCode(transitionCode, "HandlePreviewVolumeChanged");
        var rampDown = ExtractMemberCode(transitionCode, "RampDownForAudioTransitionAsync");
        var rampUp = ExtractMemberCode(transitionCode, "RampUpForAudioTransitionAsync");
        var primeTransition = ExtractMemberCode(transitionCode, "PrimeForAudioTransition");
        var restoreTransition = ExtractMemberCode(transitionCode, "RestoreAfterUnavailableAudio");
        var monitoringTransition = ExtractMemberCode(audioStateCode, "SetAudioMonitoringEnabledWithVolumeTransitionAsync");
        var audioPreviewChanged = ExtractMemberCode(audioStateCode, "OnIsAudioPreviewEnabledChanged");
        var applyAudioInputSelection = ExtractMemberCode(audioStateCode, "ApplyAudioInputSelectionAsync");

        AssertContains(audioStateCode, "get => _previewAudioVolumeTransitionController.SuppressVolumeSave;");
        AssertContains(audioStateCode, "set => _previewAudioVolumeTransitionController.SuppressVolumeSave = value;");
        AssertContains(audioStateCode, "get => _previewAudioVolumeTransitionController.VolumeSaveOverride;");
        AssertContains(audioStateCode, "set => _previewAudioVolumeTransitionController.VolumeSaveOverride = value;");
        AssertContains(previewChanged, "_previewAudioVolumeTransitionController.HandlePreviewVolumeChanged(value);");
        AssertContains(audioStateCode, "internal void SavePreviewVolume() => SaveSettings();");
        AssertContains(audioStateCode, "private async Task RampPreviewVolumeDownForStopAsync(CancellationToken cancellationToken)");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewVolumeTransitions.cs")), "MainViewModel.PreviewVolumeTransitions.cs folded into audio state");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioMonitoring.cs")), "MainViewModel.AudioMonitoring.cs folded into audio state");
        AssertDoesNotContain(audioStateCode, "private const int PreviewAudioRampDownSteps");
        AssertContains(transitionCode, "internal sealed class PreviewAudioVolumeTransitionController");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "PreviewAudioVolumeTransitionController.Ramps.cs")), "PreviewAudioVolumeTransitionController.Ramps.cs folded into PreviewAudioVolumeTransitionController.cs");
        AssertContains(transitionCode, "private const int RampDownSteps = 18;");
        AssertContains(transitionCode, "private const int RampDownDelayMs = 25;");
        AssertContains(transitionCode, "private const int RampUpSteps = 30;");
        AssertContains(transitionCode, "private const int RampUpDelayMs = 30;");

        AssertContains(handlePreviewChanged, "if (!SuppressVolumeSave)");
        AssertContains(handlePreviewChanged, "VolumeSaveOverride = null;");
        AssertContains(handlePreviewChanged, "_context.SetSessionPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));");
        AssertOccursBefore(handlePreviewChanged, "VolumeSaveOverride = null;", "_context.SetSessionPreviewVolume");

        AssertContains(rampDown, "var persistedVolume = PersistedVolumeTarget;");
        AssertContains(rampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(rampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(rampDown, "_context.SetPreviewVolume(0);");
        AssertContains(rampUp, "VolumeSaveOverride = volumeTarget;");
        AssertContains(rampUp, "_context.SetPreviewVolume(volumeTarget * eased);");
        AssertContains(rampUp, "VolumeSaveOverride = null;");
        AssertContains(primeTransition, "var volumeTarget = PersistedVolumeTarget;");
        AssertContains(primeTransition, "_context.SetPreviewVolume(0);");
        AssertContains(restoreTransition, "_context.SetPreviewVolume(volumeTarget);");

        AssertContains(monitoringTransition, "var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);");
        AssertContains(monitoringTransition, "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);");
        AssertContains(monitoringTransition, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);");
        AssertOccursBefore(monitoringTransition, "var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);", "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);");
        AssertOccursBefore(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);", "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);");
        AssertOccursBefore(monitoringTransition, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);", "await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);");

        AssertContains(audioPreviewChanged, "if (value && !IsAudioEnabled)");
        AssertContains(audioPreviewChanged, "if (_suppressAudioPreviewEnabledChangeOperation)");
        AssertContains(audioPreviewChanged, "if (!value && !IsRecording)");
        AssertContains(audioPreviewChanged, "if (IsPreviewing && IsInitialized)");
        AssertContains(audioPreviewChanged, "SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false)");
        AssertContains(audioStateCode, "private async Task ApplyAudioInputSelectionAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioInputSelection.cs")), "MainViewModel.AudioInputSelection.cs folded into audio state");
        AssertOccursBefore(audioPreviewChanged, "if (value && !IsAudioEnabled)", "IsAudioPreviewEnabled = false;");
        AssertOccursBefore(audioPreviewChanged, "if (_suppressAudioPreviewEnabledChangeOperation)", "if (!value && !IsRecording)");
        AssertOccursBefore(audioPreviewChanged, "if (!value && !IsRecording)", "ResetAudioMeter();");
        AssertOccursBefore(audioPreviewChanged, "if (IsPreviewing && IsInitialized)", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false)");

        AssertContains(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedAudioInputDevice?.Id;");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedDevice?.AudioDeviceId;");
        AssertContains(applyAudioInputSelection, "var shouldRampMonitoring = IsPreviewing && _captureService.IsAudioPreviewActive;");
        AssertContains(applyAudioInputSelection, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);");
        AssertContains(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertContains(applyAudioInputSelection, "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);");
        AssertOccursBefore(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);", "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAudioMeters_OwnCallbackMeterState()
    {
        var baseText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs")
            .Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioStateText, "public double AudioMeterTarget;");
        AssertContains(audioStateText, "public double MicrophoneMeterTarget;");
        AssertContains(audioStateText, "public event Action? AudioMeterActivated;");
        AssertContains(audioStateText, "public event Action? MicrophoneMeterActivated;");
        AssertContains(audioStateText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(audioStateText, "private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(audioStateText, "private void ResetAudioMeter()");
        AssertContains(audioStateText, "public void ResetAudioMeterTimerFlag()");
        AssertContains(audioStateText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioMeters.cs")),
            "MainViewModel.AudioMeters.cs folded into MainViewModel.AudioState.cs");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertDoesNotContain(baseText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "private const double MeterFloorDb");
        AssertDoesNotContain(baseText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertDoesNotContain(baseText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");

        return Task.CompletedTask;
    }

    internal static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioMeterText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var audioMeterControllerRootText = ReadRepoFile("Sussudio/Controllers/Audio/Meter/AudioMeterController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();

        AssertContains(mainViewModelStateText, "IsAudioPreviewActive");
        AssertContains(propertyChangedText, "TryHandleAudio = TryHandleAudioPropertyChanged,");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(audioControlPresentationControllerText, "HandleAudioPreviewActiveChanged();");
        AssertContains(audioControlPresentationControllerText, "_context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);");
        AssertContains(audioMeterText, "private AudioMeterController _audioMeterController = null!;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AudioMeter.cs")),
            "Audio meter adapter folded into MainWindow.ControlBindings.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.MicrophoneControls.cs")),
            "Microphone controls adapter folded into MainWindow.ControlBindings.cs");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerRootText, "internal sealed class AudioMeterController");
        AssertContains(audioMeterControllerRootText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerRootText, "internal sealed class AudioMeterControllerContext");
        AssertContains(audioMeterControllerRootText, "public required MainViewModel ViewModel { get; init; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "Audio",
                "Meter",
                "AudioMeterController.Context.cs")),
            "AudioMeterController context partial");
        AssertContains(audioMeterControllerRootText, "public void AnimateTick()");
        AssertContains(audioMeterControllerRootText, "public void ResetVisuals()");
        AssertContains(audioMeterControllerRootText, "public void ResetMicrophoneVisuals()");
        AssertContains(audioMeterControllerRootText, "public void SetAudioMeterTargetLevel(double targetLevel)");
        AssertContains(audioMeterControllerRootText, "public void EnsureTimerRunning()");
        AssertContains(audioMeterControllerRootText, "public void StopTimer()");
        AssertContains(audioMeterControllerRootText, "public static double TranslateMarker(double trackWidth, double level, double markerWidth)");
        AssertContains(audioMeterControllerRootText, "public void SetMonitoringState(bool isMonitoring)");
        AssertContains(audioMeterControllerRootText, "_audioMeterMonitoringStoryboard?.Stop();");
        AssertContains(audioMeterControllerRootText, "public void AnimateDisabled(bool isDisabled)");
        AssertContains(audioMeterControllerRootText, "private static void SetupRoundedContentClip(FrameworkElement element, float cornerRadius)");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3");
        AssertContains(audioMeterControllerRootText, "private static void AddOpacityAnimation(");
        AssertDoesNotContain(audioMeterControllerRootText, "partial class AudioMeterController");

        return Task.CompletedTask;
    }

    internal static Task AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry()
    {
        var traceModelsText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs")
            .Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderRootText = ReadRepoFile("Sussudio/ViewModels/AudioRampTraceRecorder.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderText = audioRampTraceRecorderRootText;
        var playbackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs").Replace("\r\n", "\n");
        var playbackRenderText = playbackText;
        var playbackVolumeText = playbackText;
        var runtimeContractsText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Models/Automation/AutomationRuntimeModels.cs"))
            .Replace("\r\n", "\n");
        var runtimeSnapshotText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"))
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText().Replace("\r\n", "\n");
        var automationInterfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs").Replace("\r\n", "\n");

        AssertContains(traceModelsText, "public sealed class AudioRampTraceSnapshot");
        AssertContains(traceModelsText, "public sealed class AudioRampTraceEntry");
        AssertContains(traceModelsText, "public double PlaybackOutputPeak { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackOutputRms { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackCurrentVolumePercent { get; init; }");
        AssertContains(traceModelsText, "public long PlaybackOutputAgeMs { get; init; }");

        AssertContains(audioRampTraceRecorderRootText, "internal sealed class AudioRampTraceRecorder");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTraceCapacity = 2048;");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTraceSampleIntervalMs = 10;");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTracePostCompleteSampleMs = 250;");
        AssertContains(audioRampTraceRecorderText, "public long BeginSession(string reason, double targetVolume)");
        AssertContains(audioRampTraceRecorderText, "public void CompleteSession(long sessionId, string reason)");
        AssertContains(audioRampTraceRecorderText, "public void RecordPoint(");
        AssertContains(audioRampTraceRecorderText, "RunSamplerAsync");
        AssertContains(audioRampTraceRecorderText, "Task.Delay(AudioRampTraceSampleIntervalMs");
        AssertContains(audioRampTraceRecorderText, "AUDIO_RAMP_TRACE_SAMPLER_FAIL");
        AssertDoesNotContain(audioRampTraceRecorderRootText, "internal sealed partial class AudioRampTraceRecorder");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.GetSnapshot(maxEntries);");
        AssertContains(audioRampTraceText, "private AudioRampTraceRecorder CreateAudioRampTraceRecorder()");
        AssertContains(audioRampTraceText, "new AudioRampTraceRecorderContext");
        AssertContains(audioRampTraceText, "GetRuntimeSnapshot = () => _captureService.GetRuntimeSnapshot(),");
        AssertContains(audioRampTraceText, "GetPreviewVolume = () => PreviewVolume,");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.BeginSession(reason, targetVolume);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.CompleteSession(sessionId, reason);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.RecordPoint(kind, reason, targetVolume, note, sessionId);");
        AssertContains(audioRampTraceText, "private PreviewAudioVolumeTransitionController CreatePreviewAudioVolumeTransitionController()");
        AssertContains(audioRampTraceText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertContains(audioRampTraceText, "SetSessionPreviewVolume = volume => _sessionCoordinator.SetPreviewVolume(volume),");
        AssertContains(audioRampTraceText, "BeginTraceSession = BeginAudioRampTraceSession,");
        AssertDoesNotContain(audioRampTraceText, "private readonly AudioRampTraceEntry[]");
        AssertDoesNotContain(audioRampTraceText, "RunAudioRampTraceSamplerAsync");
        AssertDoesNotContain(rootText, "new AudioRampTraceRecorderContext");
        AssertDoesNotContain(rootText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertContains(audioVolumeTransitionText, "BeginTraceSession(");
        AssertContains(audioVolumeTransitionText, "RecordTracePoint(\"volume-set\")");
        AssertContains(audioVolumeTransitionText, "RecordTracePoint(\"primed\"");
        AssertContains(audioVolumeTransitionText, "public Task RampDownForStopAsync(CancellationToken cancellationToken)");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-started\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-stopped\"");
        AssertContains(audioRampTraceText, "GetAudioRampTraceSnapshotAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioRampTrace.cs")),
            "MainViewModel.AudioRampTrace.cs folded into MainViewModel.AudioState.cs");

        AssertContains(playbackRenderText, "UpdateOutputLevel(destinationSpan);");
        AssertContains(playbackRenderText, "private unsafe void RenderAvailableFrames()");
        AssertContains(playbackVolumeText, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(playbackVolumeText, "public float TargetVolume => _targetVolume;");
        AssertContains(playbackVolumeText, "public float CurrentVolume => _currentVolume;");
        AssertContains(playbackVolumeText, "public float LastOutputPeak => _lastOutputPeak;");
        AssertContains(playbackVolumeText, "public float LastOutputRms => _lastOutputRms;");
        AssertContains(playbackVolumeText, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackVolumeText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Volume.cs")),
            "WASAPI playback render-side volume telemetry folded into the playback lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.RenderThread.cs")),
            "WASAPI playback render thread folded into the playback lifecycle root");

        AssertContains(runtimeContractsText, "public double WasapiPlaybackTargetVolumePercent { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackCurrentVolumePercent { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackOutputPeak { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackOutputRms { get; init; }");
        AssertContains(runtimeSnapshotText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0,");
        AssertContains(runtimeSnapshotText, "WasapiPlaybackOutputPeak = wasapiPlayback?.LastOutputPeak ?? 0,");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetAudioRampTrace:");
        AssertContains(automationInterfaceText, "Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync");

        return Task.CompletedTask;
    }

    private static void AssertNear(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected {expected:0.###} +/- {tolerance:0.###}, actual {actual:0.###}.");
        }
    }
}

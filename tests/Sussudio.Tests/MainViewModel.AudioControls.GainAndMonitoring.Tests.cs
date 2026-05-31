using System.Collections;
using System.IO;
using System.Linq;
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
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
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
        var audioMeterControllerRootText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
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
                "AudioMeterController.Context.cs")),
            "AudioMeterController context partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "Audio",
                "Meter",
                "AudioMeterController.cs")),
            "audio meter controller folded into AudioControlBindingController.cs");
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

static partial class Program
{
    internal static Task MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetMethod("SaveMicrophoneVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SaveMicrophoneVolume");

        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs");
        var deviceAudioRefreshText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioRefreshCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs");
        var deviceAudioRequestControllerCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs");
        var deviceAudioStateCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs");
        var microphoneVolumeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var audioCode = deviceAudioModeCode + "\n" + deviceAudioRefreshCode + "\n" + deviceAudioRequestControllerCode + "\n" + deviceAudioStateCode + "\n" + microphoneVolumeCode;
        var setMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "SetMicrophoneEndpointVolume");
        var getMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "GetMicrophoneEndpointVolume");
        var refreshDeviceAudioControls = ExtractMemberCode(audioCode, "RefreshDeviceAudioControlsAsync");
        var applyDeviceAudioMode = ExtractMemberCode(audioCode, "ApplyDeviceAudioModeAsync");
        var applyAnalogAudioGain = ExtractMemberCode(audioCode, "ApplyAnalogAudioGainAsync");
        var isCurrentSelectedDevice = ExtractMemberCode(audioCode, "IsCurrentSelectedDevice");
        var suppressedRefresh = ExtractMemberCode(audioCode, "WithAudioControlRefreshSuppressed");
        var normalizeDeviceAudioMode = ExtractMemberCode(audioCode, "NormalizeDeviceAudioMode");

        AssertContains(microphoneVolumeCode, "internal void SaveMicrophoneVolume() => SaveSettings();");
        AssertContains(microphoneVolumeCode, "partial void OnMicrophoneVolumeChanged(double value)");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.MicrophoneVolume.cs")), "MainViewModel.MicrophoneVolume.cs folded into audio state");
        AssertDoesNotContain(deviceAudioStateCode, "SetMicrophoneEndpointVolume");
        AssertDoesNotContain(deviceAudioStateCode, "GetMicrophoneEndpointVolume");
        AssertContains(deviceAudioStateCode, "private async Task RefreshDeviceAudioControlsAsync");
        AssertContains(deviceAudioRefreshText, "Device-native audio-control support probing and state readback.");
        AssertContains(deviceAudioStateCode, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeText, "Device-native audio mode switching and failure readback.");
        AssertContains(deviceAudioStateCode, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioRefresh.cs")), "MainViewModel.DeviceAudioRefresh.cs folded into device audio state");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs")), "MainViewModel.DeviceAudioMode.cs folded into device audio state");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioStateCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertDoesNotContain(deviceAudioStateCode, "TryApplyAtDeviceAudioModeAsync");
        AssertDoesNotContain(deviceAudioStateCode, "SetInputSourceAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")), "MainViewModel analog gain writes folded into device audio state");

        AssertContains(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)");
        AssertContains(setMicrophoneEndpointVolume, "WasapiComInterop.SetEndpointVolume(deviceId, (float)(Math.Clamp(volumePercent, 0.0, 100.0) / 100.0));");
        AssertOccursBefore(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.SetEndpointVolume");

        AssertContains(getMicrophoneEndpointVolume, "return 100.0;");
        AssertContains(getMicrophoneEndpointVolume, "return WasapiComInterop.GetEndpointVolume(deviceId) * 100.0;");
        AssertOccursBefore(getMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.GetEndpointVolume");

        AssertContains(refreshDeviceAudioControls, "IsDeviceAudioControlSupported = false;");
        AssertContains(refreshDeviceAudioControls, "SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;");
        AssertContains(refreshDeviceAudioControls, "AnalogAudioGainPercent = 50;");
        AssertContains(refreshDeviceAudioControls, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out _, out _)");
        AssertContains(refreshDeviceAudioControls, "await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedAnalogAudioGainPercent = null;");
        AssertOccursBefore(refreshDeviceAudioControls, "if (device == null)", "IsDeviceAudioControlSupported = false;");
        AssertOccursBefore(refreshDeviceAudioControls, "NativeXuDeviceSupport.TryGetSupported4kXIds", "var state = await _deviceAudioControlService.ReadStateAsync");
        var refreshInitialReadback = ExtractTextBetween(
            refreshDeviceAudioControls,
            "var state = await _deviceAudioControlService.ReadStateAsync",
            "WithAudioControlRefreshSuppressed(() =>");
        AssertContains(refreshInitialReadback, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(refreshInitialReadback, "if (!IsCurrentSelectedDevice(device))");
        var refreshRestoreReadback = ExtractTextBetween(
            refreshDeviceAudioControls,
            "var refreshedState = await _deviceAudioControlService.ReadStateAsync",
            "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshRestoreReadback, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(refreshRestoreReadback, "if (!IsCurrentSelectedDevice(device))");

        AssertContains(applyDeviceAudioMode, "if (device == null || !IsDeviceAudioControlSupported)");
        AssertContains(applyDeviceAudioMode, "if (!IsCurrentSelectedDevice(device))");
        AssertContains(applyDeviceAudioMode, "var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);");
        AssertContains(applyDeviceAudioMode, "var gainByte = DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent);");
        AssertContains(applyDeviceAudioMode, "NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte, cancellationToken)");
        AssertContains(applyDeviceAudioMode, "var failureState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(applyDeviceAudioMode, "StatusText =");
        AssertContains(applyDeviceAudioMode, "if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))");
        AssertContains(applyDeviceAudioMode, "ApplyAnalogAudioGainAsync(");
        AssertContains(applyDeviceAudioMode, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);");
        AssertContains(applyDeviceAudioMode, "if (persistSettings)");
        AssertOccursBefore(applyDeviceAudioMode, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SwitchAudioInputAsync");
        AssertOccursBefore(applyDeviceAudioMode, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SwitchAudioInputAsync");
        AssertOccursBefore(applyDeviceAudioMode, "NativeXuAtCommandProvider.SwitchAudioInputAsync", "var failureState = await _deviceAudioControlService.ReadStateAsync");
        AssertOccursBefore(applyDeviceAudioMode, "var failureState = await _deviceAudioControlService.ReadStateAsync", "StatusText =");
        AssertOccursBefore(applyDeviceAudioMode, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);", "if (persistSettings)");

        AssertContains(applyAnalogAudioGain, "var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);");
        AssertContains(applyAnalogAudioGain, "var gainByte = DeviceAudioGainMapper.PercentToGainByte(gainPercent);");
        AssertContains(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)");
        AssertContains(applyAnalogAudioGain, "StatusText =");
        AssertContains(applyAnalogAudioGain, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertContains(applyAnalogAudioGain, "SaveSettings();");
        AssertOccursBefore(applyAnalogAudioGain, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)", "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertOccursBefore(applyAnalogAudioGain, "if (persistSettings)", "SaveSettings();");

        AssertContains(isCurrentSelectedDevice, "string.Equals(selected.Id, device.Id, StringComparison.OrdinalIgnoreCase)");
        AssertContains(isCurrentSelectedDevice, "string.Equals(selected.NativeXuInterfacePath, device.NativeXuInterfacePath, StringComparison.OrdinalIgnoreCase)");
        AssertContains(suppressedRefresh, "_isRefreshingDeviceAudioControls = true;");
        AssertContains(suppressedRefresh, "finally");
        AssertContains(suppressedRefresh, "_isRefreshingDeviceAudioControls = false;");
        AssertOccursBefore(suppressedRefresh, "_isRefreshingDeviceAudioControls = true;", "try");
        AssertOccursBefore(suppressedRefresh, "finally", "_isRefreshingDeviceAudioControls = false;");
        AssertContains(normalizeDeviceAudioMode, "string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)");
        AssertContains(normalizeDeviceAudioMode, "? DeviceAudioMode.Analog");
        AssertContains(normalizeDeviceAudioMode, ": DeviceAudioMode.Hdmi;");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime()
    {
        var deviceAudioRequestControllerCode = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs")
            .Replace("\r\n", "\n");
        var deviceAudioStateCode = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var controllerStart = deviceAudioRequestControllerCode.IndexOf(
            "internal sealed class MainViewModelDeviceAudioRequestController",
            StringComparison.Ordinal);
        AssertEqual(true, controllerStart >= 0, "device audio request controller class marker");
        var controllerBody = deviceAudioRequestControllerCode[controllerStart..];
        var handleModeChange = ExtractMemberCode(controllerBody, "HandleSelectedDeviceAudioModeChanged");
        var refreshControls = ExtractMemberCode(controllerBody, "RequestDeviceAudioControlsRefresh");
        var cancelWork = ExtractMemberCode(controllerBody, "CancelPendingAudioControlWork");
        var handleGainChange = ExtractMemberCode(controllerBody, "HandleAnalogAudioGainPercentChanged");
        var flashPersist = ExtractMemberCode(controllerBody, "ScheduleAnalogGainFlashPersist");

        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainFlashDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainXuDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioModeCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioRefreshCts;");
        AssertContains(deviceAudioStateCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioStateCode, "=> _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioRequestControllerCode, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerCode, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");

        AssertContains(refreshControls, "_deviceAudioRefreshCts = refreshCts;");
        AssertContains(refreshControls, "_context.RefreshDeviceAudioControlsAsync(targetDevice, true, refreshToken)");
        AssertContains(refreshControls, "\"device audio controls refresh\", true");
        AssertContains(refreshControls, "if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))");
        AssertContains(refreshControls, "refreshCts.Dispose();");

        AssertContains(handleModeChange, "oldCts?.Cancel();");
        AssertContains(handleModeChange, "_deviceAudioModeCts = cts;");
        AssertContains(handleModeChange, "_context.ApplyDeviceAudioModeAsync(\"device audio mode change\", targetDevice, token)");
        AssertContains(handleModeChange, "if (ReferenceEquals(_deviceAudioModeCts, cts))");
        AssertContains(handleModeChange, "cts.Dispose();");
        AssertContains(handleModeChange, "_context.SaveSettings();");

        AssertContains(handleGainChange, "oldCts?.Cancel();");
        AssertContains(handleGainChange, "_gainXuDebounceCts = cts;");
        AssertContains(handleGainChange, "await Task.Delay(200, token).ConfigureAwait(false);");
        AssertContains(handleGainChange, "_context.ApplyAnalogAudioGainAsync(\"analog audio gain change\", targetDevice, token)");
        AssertContains(handleGainChange, "if (ReferenceEquals(_gainXuDebounceCts, cts))");
        AssertContains(handleGainChange, "cts.Dispose();");
        AssertContains(handleGainChange, "_context.SaveSettings();");

        AssertContains(flashPersist, "oldCts?.Cancel();");
        AssertContains(flashPersist, "_gainFlashDebounceCts = cts;");
        AssertContains(flashPersist, "await Task.Delay(300, token).ConfigureAwait(false);");
        AssertContains(flashPersist, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertContains(flashPersist, "if (ReferenceEquals(_gainFlashDebounceCts, cts))");
        AssertContains(flashPersist, "cts.Dispose();");
        AssertOccursBefore(flashPersist, "await Task.Delay(300, token).ConfigureAwait(false);", "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertOccursBefore(flashPersist, "if (ReferenceEquals(_gainFlashDebounceCts, cts))", "cts.Dispose();");

        AssertContains(cancelWork, "var flashCts = _gainFlashDebounceCts;");
        AssertContains(cancelWork, "_gainFlashDebounceCts = null;");
        AssertContains(cancelWork, "flashCts?.Cancel();");
        AssertContains(cancelWork, "var xuCts = _gainXuDebounceCts;");
        AssertContains(cancelWork, "_gainXuDebounceCts = null;");
        AssertContains(cancelWork, "xuCts?.Cancel();");
        AssertContains(cancelWork, "var modeCts = _deviceAudioModeCts;");
        AssertContains(cancelWork, "_deviceAudioModeCts = null;");
        AssertContains(cancelWork, "modeCts?.Cancel();");
        AssertContains(cancelWork, "var refreshCts = _deviceAudioRefreshCts;");
        AssertContains(cancelWork, "_deviceAudioRefreshCts = null;");
        AssertContains(cancelWork, "refreshCts?.Cancel();");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAudioControlService_LivesInCohesiveServiceFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "internal sealed class NativeXuAudioControlService");
        AssertDoesNotContain(rootText, "partial class NativeXuAudioControlService");
        AssertContains(rootText, "public async Task<DeviceAudioControlState> ReadStateAsync(");
        AssertContains(rootText, "public async Task<bool> SetAudioModeAsync(");
        AssertContains(rootText, "public async Task<bool> SetAnalogGainPercentAsync(");
        AssertContains(rootText, "internal sealed record DeviceAudioControlState(");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static readonly int[] InputByteIndexes");
        AssertContains(rootText, "private static readonly int[] DynamicByteIndexes");
        AssertContains(rootText, "private static readonly byte[] HdmiReference = ParseHex(");
        AssertContains(rootText, "private static readonly byte[] AnalogReference = ParseHex(");
        AssertContains(rootText, "private static bool TryGetTargetInputReference(string? mode, out byte[] reference)");
        AssertContains(rootText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertContains(rootText, "private static AnalogGainDecision DecodeGain(byte[] payload)");
        AssertContains(rootText, "private static byte[] ParseHex(string hex)");
        AssertContains(rootText, "private async Task<bool> UpdatePayloadAsync(");
        AssertContains(rootText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertContains(rootText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(rootText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(rootText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");
        AssertContains(rootText, "private static bool TryReadRawPayload(");
        AssertContains(rootText, "private static bool TryWriteRawPayload(");
        AssertContains(rootText, "private static byte[] NormalizePayload(byte[] rawPayload)");
        AssertContains(rootText, "private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)");
        AssertContains(rootText, "private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)");
        AssertContains(rootText, "NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath)");
        AssertContains(rootText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertContains(rootText, "private readonly record struct GainProfile");
        AssertContains(rootText, "private readonly record struct RawControlCandidate");
        AssertContains(rootText, "private readonly record struct RawPayloadSnapshot");
        AssertDoesNotContain(rootText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfacePath(");
        AssertContains(deviceSupportText, "public static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)");
        AssertContains(probeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuDeviceSupport.cs");
        foreach (var removedFile in new[]
        {
            "NativeXuAudioControlService.Profiles.cs",
            "NativeXuAudioControlService.Transport.cs",
            "NativeXuAudioControlService.RawTransport.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_LivesInFocusedHelper()
    {
        var adapterText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioDeviceDiscovery.cs")), "audio device discovery adapter stays folded into AudioState");
        AssertContains(policyText, "internal static class AudioDeviceSelectionPolicy");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectStartup(");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectRefresh(");
        AssertContains(policyText, "internal static IReadOnlyList<AudioInputDevice> FilterOutCaptureCardAudio(");
        AssertContains(policyText, "SelectByPreviousSavedOrFirst(availableDevices, previousAudioId, savedAudioId)");
        AssertContains(policyText, "SelectByPreviousOrFirst(availableDevices, previousAudioId)");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectStartup(");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectRefresh(");
        AssertContains(adapterText, "ReplaceCollection(AudioInputDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "Logger.Log($\"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.\");");
        AssertContains(adapterText, "Logger.Log($\"Audio device list refreshed ({AudioInputDevices.Count} devices).\");");
        AssertDoesNotContain(policyText, "ReplaceCollection(");
        AssertDoesNotContain(policyText, "Logger.Log(");
        AssertDoesNotContain(policyText, "_pendingSaved");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("CAPTURE-AUDIO"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.CaptureDevice",
            CreateAudioDeviceSelectionPolicyCapture("video-first", "other-capture"),
            CreateAudioDeviceSelectionPolicyCapture("video-previous", "capture-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "video-previous",
            "missing-audio",
            "saved-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Startup audio list filters the capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Startup first filtered audio id");
        AssertEqual("saved-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup saved audio fallback");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup saved audio found");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup saved microphone found");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");

        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup preserves previous audio");
        AssertEqual("previous-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup preserves previous microphone");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup keeps existing saved-audio fallback log decision");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup keeps existing saved-microphone fallback log decision");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("capture-audio"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            "CAPTURE-AUDIO",
            "previous-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Refresh audio list filters selected capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Refresh first filtered audio id");
        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Refresh preserves previous audio");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Refresh saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Refresh does not log saved audio fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Refresh does not log saved microphone fallback");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.AudioInputDevice");
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var startupSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(startupSelection).Length, "Startup empty audio list");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedAudioInputDevice"), "Startup empty audio selection");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedMicrophoneDevice"), "Startup empty microphone selection");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedAudioFallback"), "Startup empty saved audio fallback log decision");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedMicrophoneFallback"), "Startup empty saved microphone fallback log decision");

        var refreshSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            null,
            "previous-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(refreshSelection).Length, "Refresh empty audio list");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedAudioInputDevice"), "Refresh empty audio selection");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedMicrophoneDevice"), "Refresh empty microphone selection");
        AssertEqual(false, GetBoolProperty(refreshSelection, "ShouldLogSavedMicrophoneFallback"), "Refresh empty saved microphone log decision");

        return Task.CompletedTask;
    }

    private static object InvokeAudioDeviceSelectionPolicy(string methodName, params object?[] arguments)
    {
        var policyType = RequireType("Sussudio.ViewModels.AudioDeviceSelectionPolicy");
        var method = policyType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing AudioDeviceSelectionPolicy.{methodName}.");
        return method.Invoke(null, arguments)
               ?? throw new InvalidOperationException($"AudioDeviceSelectionPolicy.{methodName} returned null.");
    }

    private static object CreateAudioDeviceSelectionPolicyAudio(string id)
    {
        var audioType = RequireType("Sussudio.Models.AudioInputDevice");
        var audio = Activator.CreateInstance(audioType)
            ?? throw new InvalidOperationException("Failed to create AudioInputDevice.");
        SetPropertyOrBackingField(audio, "Id", id);
        SetPropertyOrBackingField(audio, "Name", id);
        return audio;
    }

    private static object CreateAudioDeviceSelectionPolicyCapture(string id, string? audioDeviceId)
    {
        var captureType = RequireType("Sussudio.Models.CaptureDevice");
        var capture = Activator.CreateInstance(captureType)
            ?? throw new InvalidOperationException("Failed to create CaptureDevice.");
        SetPropertyOrBackingField(capture, "Id", id);
        SetPropertyOrBackingField(capture, "Name", id);
        SetPropertyOrBackingField(capture, "AudioDeviceId", audioDeviceId);
        return capture;
    }

    private static object CreateAudioDeviceSelectionPolicyList(string elementTypeName, params object[] items)
    {
        var elementType = RequireType(elementTypeName);
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
            ?? throw new InvalidOperationException($"Failed to create list for {elementTypeName}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static string? GetAudioDeviceSelectionId(object selection, string propertyName)
    {
        var device = GetPropertyValue(selection, propertyName);
        return device != null ? GetStringProperty(device, "Id") : null;
    }

    private static string[] GetAudioDeviceSelectionAvailableIds(object selection)
    {
        var devices = (IEnumerable)(GetPropertyValue(selection, "AvailableDevices")
            ?? throw new InvalidOperationException("AudioDeviceSelection.AvailableDevices was null."));
        return devices.Cast<object>().Select(device => GetStringProperty(device, "Id")).ToArray();
    }
}

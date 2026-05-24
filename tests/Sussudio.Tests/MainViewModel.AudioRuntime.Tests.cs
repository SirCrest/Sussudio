using System.Threading.Tasks;

static partial class Program
{
    internal static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlPresentationController.cs").Replace("\r\n", "\n");
        var audioMeterText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs").Replace("\r\n", "\n");
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
            "Audio meter adapter folded into MainWindow.AudioBindings.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.MicrophoneControls.cs")),
            "Microphone controls adapter folded into MainWindow.AudioBindings.cs");
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
        var traceModelsText = ReadRepoFile("Sussudio/Models/Audio/AudioModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var audioVolumeTransitionText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs"),
            ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.Ramps.cs"))
            .Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderRootText = ReadRepoFile("Sussudio/ViewModels/AudioRampTraceRecorder.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderText = audioRampTraceRecorderRootText;
        var playbackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs").Replace("\r\n", "\n");
        var playbackRenderText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs").Replace("\r\n", "\n");
        var playbackVolumeText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.Volume.cs").Replace("\r\n", "\n");
        var runtimeContractsText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.cs"))
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
        AssertContains(playbackVolumeText, "internal sealed partial class WasapiAudioPlayback");
        AssertContains(playbackVolumeText, "public float TargetVolume => _targetVolume;");
        AssertContains(playbackVolumeText, "public float CurrentVolume => _currentVolume;");
        AssertContains(playbackVolumeText, "public float LastOutputPeak => _lastOutputPeak;");
        AssertContains(playbackVolumeText, "public float LastOutputRms => _lastOutputRms;");
        AssertContains(playbackVolumeText, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackVolumeText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");
        AssertDoesNotContain(playbackText, "private void ApplyVolume(Span<byte> buffer)");
        AssertDoesNotContain(playbackText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");

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
}

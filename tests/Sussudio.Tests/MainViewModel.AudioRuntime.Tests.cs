using System.Threading.Tasks;

static partial class Program
{
    private static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlPresentationController.cs").Replace("\r\n", "\n");
        var audioMeterText = ReadRepoFile("Sussudio/MainWindow.AudioMeter.cs").Replace("\r\n", "\n");
        var audioMeterControllerRootText = ReadRepoFile("Sussudio/Controllers/Audio/Meter/AudioMeterController.cs").Replace("\r\n", "\n");
        var audioMeterStateText = ReadRepoFile("Sussudio/Controllers/Audio/Meter/AudioMeterController.MeterState.cs").Replace("\r\n", "\n");
        var audioMeterAnimationsText = ReadRepoFile("Sussudio/Controllers/Audio/Meter/AudioMeterController.PresentationAnimations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelStateText, "IsAudioPreviewActive");
        AssertContains(propertyChangedText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(audioPropertyChangedText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(audioPropertyChangedText, "HandleAudioPreviewActiveChanged();");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.HandleAudioPreviewActiveChanged();");
        AssertContains(audioControlPresentationControllerText, "_context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);");
        AssertContains(audioMeterText, "private AudioMeterController _audioMeterController = null!;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerRootText, "internal sealed partial class AudioMeterController");
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
        AssertContains(audioMeterStateText, "public void AnimateTick()");
        AssertContains(audioMeterStateText, "public void ResetVisuals()");
        AssertContains(audioMeterStateText, "public void ResetMicrophoneVisuals()");
        AssertContains(audioMeterStateText, "public void SetAudioMeterTargetLevel(double targetLevel)");
        AssertContains(audioMeterStateText, "public void EnsureTimerRunning()");
        AssertContains(audioMeterStateText, "public void StopTimer()");
        AssertContains(audioMeterStateText, "public static double TranslateMarker(double trackWidth, double level, double markerWidth)");
        AssertContains(audioMeterAnimationsText, "public void SetMonitoringState(bool isMonitoring)");
        AssertContains(audioMeterAnimationsText, "_audioMeterMonitoringStoryboard?.Stop();");
        AssertContains(audioMeterAnimationsText, "public void AnimateDisabled(bool isDisabled)");
        AssertContains(audioMeterAnimationsText, "private static void SetupRoundedContentClip(FrameworkElement element, float cornerRadius)");
        AssertContains(audioMeterAnimationsText, "AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0");
        AssertContains(audioMeterAnimationsText, "AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4");
        AssertContains(audioMeterAnimationsText, "AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2");
        AssertContains(audioMeterAnimationsText, "AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3");
        AssertContains(audioMeterAnimationsText, "private static void AddOpacityAnimation(");
        AssertDoesNotContain(audioMeterControllerRootText, "public void AnimateTick()");
        AssertDoesNotContain(audioMeterControllerRootText, "public void SetMonitoringState(bool isMonitoring)");
        AssertDoesNotContain(audioMeterControllerRootText, "private static void AddOpacityAnimation(");

        return Task.CompletedTask;
    }

    private static Task AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry()
    {
        var traceModelsText = ReadRepoFile("Sussudio/Models/Audio/AudioRampTraceModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs").Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs").Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioRampTrace.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderText = ReadRepoFile("Sussudio/ViewModels/AudioRampTraceRecorder.cs").Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs").Replace("\r\n", "\n");
        var playbackRenderText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs").Replace("\r\n", "\n");
        var playbackVolumeText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.Volume.cs").Replace("\r\n", "\n");
        var runtimeContractsText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var runtimeSnapshotText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs"))
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText().Replace("\r\n", "\n");
        var automationInterfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs").Replace("\r\n", "\n");

        AssertContains(traceModelsText, "public sealed class AudioRampTraceSnapshot");
        AssertContains(traceModelsText, "public sealed class AudioRampTraceEntry");
        AssertContains(traceModelsText, "public double PlaybackOutputPeak { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackOutputRms { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackCurrentVolumePercent { get; init; }");
        AssertContains(traceModelsText, "public long PlaybackOutputAgeMs { get; init; }");

        AssertContains(audioRampTraceRecorderText, "internal sealed class AudioRampTraceRecorder");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTraceCapacity = 2048;");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTraceSampleIntervalMs = 10;");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTracePostCompleteSampleMs = 250;");
        AssertContains(audioRampTraceRecorderText, "public long BeginSession(string reason, double targetVolume)");
        AssertContains(audioRampTraceRecorderText, "public void CompleteSession(long sessionId, string reason)");
        AssertContains(audioRampTraceRecorderText, "public void RecordPoint(");
        AssertContains(audioRampTraceRecorderText, "RunSamplerAsync");
        AssertContains(audioRampTraceRecorderText, "Task.Delay(AudioRampTraceSampleIntervalMs");
        AssertContains(audioRampTraceRecorderText, "AUDIO_RAMP_TRACE_SAMPLER_FAIL");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.GetSnapshot(maxEntries);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.BeginSession(reason, targetVolume);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.CompleteSession(sessionId, reason);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.RecordPoint(kind, reason, targetVolume, note, sessionId);");
        AssertDoesNotContain(audioRampTraceText, "private readonly AudioRampTraceEntry[]");
        AssertDoesNotContain(audioRampTraceText, "RunAudioRampTraceSamplerAsync");
        AssertContains(audioVolumeTransitionText, "BeginTraceSession(");
        AssertContains(audioVolumeTransitionText, "RecordTracePoint(\"volume-set\")");
        AssertContains(audioVolumeTransitionText, "RecordTracePoint(\"primed\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-started\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-stopped\"");
        AssertContains(audioRampTraceText, "GetAudioRampTraceSnapshotAsync");

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

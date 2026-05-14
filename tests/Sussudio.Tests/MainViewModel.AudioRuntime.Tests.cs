using System.Threading.Tasks;

static partial class Program
{
    private static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var audioMeterText = ReadRepoFile("Sussudio/MainWindow.AudioMeter.cs").Replace("\r\n", "\n");
        var audioMeterControllerText = ReadRepoFile("Sussudio/Controllers/AudioMeterController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelStateText, "IsAudioPreviewActive");
        AssertContains(propertyChangedText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(propertyChangedText, "HandleAudioPreviewActiveChanged();");
        AssertContains(audioPropertyChangedText, "SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);");
        AssertContains(audioMeterText, "private AudioMeterController _audioMeterController = null!;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerText, "_audioMeterMonitoringStoryboard?.Stop();");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3");
        AssertContains(audioMeterControllerText, "private static void AddOpacityAnimation(");

        return Task.CompletedTask;
    }

    private static Task AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry()
    {
        var traceModelsText = ReadRepoFile("Sussudio/Models/Audio/AudioRampTraceModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs").Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioRampTrace.cs").Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs").Replace("\r\n", "\n");
        var playbackRenderText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs").Replace("\r\n", "\n");
        var playbackVolumeText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.Volume.cs").Replace("\r\n", "\n");
        var runtimeContractsText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var runtimeSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs").Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText().Replace("\r\n", "\n");
        var automationInterfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs").Replace("\r\n", "\n");

        AssertContains(traceModelsText, "public sealed class AudioRampTraceSnapshot");
        AssertContains(traceModelsText, "public sealed class AudioRampTraceEntry");
        AssertContains(traceModelsText, "public double PlaybackOutputPeak { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackOutputRms { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackCurrentVolumePercent { get; init; }");
        AssertContains(traceModelsText, "public long PlaybackOutputAgeMs { get; init; }");

        AssertContains(audioRampTraceText, "private const int AudioRampTraceSampleIntervalMs = 10;");
        AssertContains(audioMonitoringText, "BeginAudioRampTraceSession(");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"volume-set\")");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"primed\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-started\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-stopped\"");
        AssertContains(audioRampTraceText, "RunAudioRampTraceSamplerAsync");
        AssertContains(audioRampTraceText, "Task.Delay(AudioRampTraceSampleIntervalMs");
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

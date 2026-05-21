using System.IO;
using System.Linq;
using System.Threading.Tasks;

static partial class Program
{
    private static readonly string[] CaptureServiceAudioFiles =
    {
        "Sussudio/Services/Capture/CaptureService.Audio.cs",
        "Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs",
        "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs",
        "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Update.cs",
        "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Disposal.cs",
        "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Restart.cs",
        "Sussudio/Services/Capture/PreviewAudioGraphResources.cs"
    };

    private static string ReadCaptureServiceAudioSource()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    internal static Task CaptureService_AudioOwnershipLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");
        var audioText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs");
        var audioPreviewText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs");
        var audioInputSwitchingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs");
        var microphoneText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs");
        var microphoneUpdateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Update.cs");
        var microphoneDisposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Disposal.cs");
        var microphoneRestartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Restart.cs");
        var resourceText = ReadRepoFile("Sussudio/Services/Capture/PreviewAudioGraphResources.cs");

        AssertContains(rootText, "private readonly PreviewAudioGraphResources _previewAudioGraph = new();");
        AssertDoesNotContain(rootText, "private sealed class PreviewAudioGraphResources");
        AssertContains(resourceText, "internal sealed class PreviewAudioGraphResources");
        AssertContains(resourceText, "public WasapiAudioCapture? ProgramCapture;");
        AssertContains(resourceText, "public WasapiAudioCapture? MicrophoneCapture;");
        AssertContains(resourceText, "public WasapiAudioPlayback? Playback;");
        AssertContains(resourceText, "public float PreviewVolume = 1.0f;");
        AssertContains(resourceText, "private bool _captureFaulted;");
        AssertContains(resourceText, "private string? _captureFaultMessage;");
        AssertContains(resourceText, "public void RecordCaptureFault(");
        AssertContains(resourceText, "public PreviewAudioCaptureFaultSnapshot ConsumeCaptureFault()");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.ProgramCapture;");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.MicrophoneCapture;");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.Playback;");
        AssertDoesNotContain(rootText, "private WasapiAudioCapture? _wasapiAudioCapture");
        AssertDoesNotContain(rootText, "private WasapiAudioCapture? _microphoneCapture");
        AssertDoesNotContain(rootText, "private WasapiAudioPlayback? _wasapiAudioPlayback");
        AssertDoesNotContain(rootText, "private float _previewVolume");
        AssertDoesNotContain(rootText, "private bool _isMonitoringMuted");
        AssertDoesNotContain(rootText, "private bool _wasapiAudioCaptureFaulted;");
        AssertDoesNotContain(rootText, "private string? _wasapiAudioCaptureFaultMessage;");
        AssertContains(audioText, "public void SetPreviewVolume(");
        AssertContains(audioText, "public void SetMonitoringMuted(");
        AssertContains(audioText, "private void OnWasapiAudioLevelUpdated(");
        AssertContains(audioText, "private void OnWasapiCaptureFailed(");
        AssertDoesNotContain(audioText, "public Task StartAudioPreviewAsync(");
        AssertDoesNotContain(audioText, "public Task UpdateAudioInputAsync(");
        AssertDoesNotContain(audioText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioText, "private async Task StartWasapiPlaybackAsync(");

        AssertContains(audioPreviewText, "public Task StartAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewWithTeardownAsync(");
        AssertContains(audioPreviewText, "private Task StopAudioPreviewCoreAsync(");
        AssertDoesNotContain(audioPreviewText, "public Task UpdateAudioInputAsync(");
        AssertDoesNotContain(audioPreviewText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioPreviewText, "private async Task StartWasapiPlaybackAsync(");

        AssertContains(audioInputSwitchingText, "public Task UpdateAudioInputAsync(");
        AssertContains(audioInputSwitchingText, "Logger.Log($\"Live audio input switch:");
        AssertContains(audioInputSwitchingText, "Logger.Log(\"AUDIO_INPUT_SWITCH_CANCEL_DEFERRED\");");
        AssertDoesNotContain(audioInputSwitchingText, "public Task StartAudioPreviewAsync(");
        AssertDoesNotContain(audioInputSwitchingText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioInputSwitchingText, "private async Task StartWasapiPlaybackAsync(");

        AssertContains(microphoneUpdateText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertContains(microphoneUpdateText, "RunTransitionAsync(CurrentSessionState,");
        AssertContains(microphoneDisposalText, "private async Task DisposeMicrophoneCaptureAsync()");
        AssertContains(microphoneText, "private void OnMicrophoneAudioLevelUpdated(");
        AssertContains(microphoneRestartText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertDoesNotContain(microphoneText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(microphoneText, "private async Task DisposeMicrophoneCaptureAsync()");
        AssertDoesNotContain(microphoneText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");

        AssertContains(resourceText, "public async Task StartPlaybackAsync(");
        AssertContains(resourceText, "public void StopPlayback(");
        AssertContains(resourceText, "public void DetachCapture(");
        AssertContains(resourceText, "private static void SafeClearCapturePlayback(");
        AssertContains(resourceText, "private static void DisposePlaybackBestEffort(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.WasapiPlayback.cs")),
            "old WASAPI playback partial removed after PreviewAudioGraphResources promotion");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_MicrophoneRestartAfterRecordingLivesInMicrophoneMonitorPartial()
    {
        var finalizationText = ReadCaptureServiceRecordingFinalizationSource()
            .Replace("\r\n", "\n");
        var microphoneRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs")
            .Replace("\r\n", "\n");
        var microphoneText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.Restart.cs")
            .Replace("\r\n", "\n");

        AssertContains(microphoneRootText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertContains(microphoneText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneText, "new WasapiAudioCapture()");
        AssertContains(microphoneText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");
        AssertContains(microphoneText, "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(microphoneText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
        AssertContains(microphoneText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_previewAudioGraph.MicrophoneCapture = micCapture;");

        AssertContains(finalizationText, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(finalizationText, "OnlyWhenMissing: true,");
        AssertContains(finalizationText, "DisposeWarningEvent: \"FLASHBACK_MIC_RESTART_DISPOSE_WARN\"");
        AssertContains(finalizationText, "OnlyWhenMissing: false,");
        AssertContains(finalizationText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(finalizationText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(finalizationText, "DisposeWarningEvent: \"MIC_MONITOR_RESTART_DISPOSE_WARN\"");
        AssertDoesNotContain(finalizationText, "WasapiAudioCapture? micCapture = null;");
        AssertDoesNotContain(finalizationText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(finalizationText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");

        return Task.CompletedTask;
    }
}

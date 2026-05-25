using System.IO;
using System.Linq;
using System.Threading.Tasks;

static partial class Program
{
    private static readonly string[] CaptureServiceAudioFiles =
    {
        "Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs",
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
        var audioPreviewText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs");
        var microphoneText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs");
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
        AssertContains(audioPreviewText, "public void SetPreviewVolume(");
        AssertContains(audioPreviewText, "public void SetMonitoringMuted(");
        AssertContains(audioPreviewText, "private void OnWasapiAudioLevelUpdated(");
        AssertContains(audioPreviewText, "private void OnWasapiCaptureFailed(");
        AssertContains(audioPreviewText, "public Task StartAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewWithTeardownAsync(");
        AssertContains(audioPreviewText, "private Task StopAudioPreviewCoreAsync(");
        AssertContains(audioPreviewText, "public Task UpdateAudioInputAsync(");
        AssertContains(audioPreviewText, "Logger.Log($\"Live audio input switch:");
        AssertContains(audioPreviewText, "Logger.Log(\"AUDIO_INPUT_SWITCH_CANCEL_DEFERRED\");");
        AssertDoesNotContain(audioPreviewText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioPreviewText, "private async Task StartWasapiPlaybackAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Audio.cs")),
            "old audio event projection partial removed after audio preview lifecycle consolidation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.AudioInputSwitching.cs")),
            "live audio input switching folded into CaptureService.AudioPreviewLifecycle.cs");

        AssertContains(microphoneText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertContains(microphoneText, "RunTransitionAsync(CurrentSessionState,");
        AssertContains(microphoneText, "private async Task DisposeMicrophoneCaptureAsync()");
        AssertContains(microphoneText, "private void OnMicrophoneAudioLevelUpdated(");
        AssertContains(microphoneText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.Update.cs")),
            "old microphone monitor update partial removed after monitor consolidation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.Restart.cs")),
            "old microphone monitor restart partial removed after monitor consolidation");

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

        AssertContains(microphoneRootText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertContains(microphoneRootText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneRootText, "new WasapiAudioCapture()");
        AssertContains(microphoneRootText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneRootText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");
        AssertContains(microphoneRootText, "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(microphoneRootText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneRootText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
        AssertContains(microphoneRootText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneRootText,
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

using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelCaptureSettings_OwnsSettingsProjection()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var captureSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSettings.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureSettingsText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureSettingsText, "RequestedPixelFormat = ResolveRequestedPixelFormat()");
        AssertContains(captureSettingsText, "ForceMjpegDecode = ShouldForceMjpegDecode()");
        AssertContains(captureSettingsText, "settings.UseCustomAudioInput = IsCustomAudioInputEnabled;");
        AssertContains(captureSettingsText, "settings.MicrophoneEnabled = IsMicrophoneEnabled;");
        AssertContains(captureSettingsText, "private string? ResolveRequestedPixelFormat()");
        AssertContains(captureSettingsText, "private bool ShouldForceMjpegDecode()");
        AssertDoesNotContain(captureText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureText, "await _sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken)");
        AssertContains(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");

        return Task.CompletedTask;
    }
}

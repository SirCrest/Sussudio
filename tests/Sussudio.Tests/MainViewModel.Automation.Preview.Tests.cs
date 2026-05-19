using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_PreviewEnablementLivesInPreviewLifecycleController()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Probes.cs")
                .Replace("\r\n", "\n");

        AssertContains(previewStateText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);");
        AssertContains(previewStateText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "=> _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertDoesNotContain(mainViewModelText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(mainViewModelText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "CancelPendingPreviewRestart();");
        AssertContains(previewLifecycleControllerText, "if (enabled == _viewModel.IsPreviewing)");
        AssertContains(previewLifecycleControllerText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(previewLifecycleControllerText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertContains(captureServiceText, "private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertContains(captureServiceText, "await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationPreview.cs")),
            "MainViewModel preview automation partial");

        return Task.CompletedTask;
    }
}

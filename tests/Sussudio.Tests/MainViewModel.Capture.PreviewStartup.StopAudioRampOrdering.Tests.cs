using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStop_RampsAudioDownBeforePreviewTeardown()
    {
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs")
            .Replace("\r\n", "\n");
        var audioVolumeTransitionText = string.Join(
                "\n",
                ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs"),
                ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.Ramps.cs"))
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");

        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonActionController.cs")
            .Replace("\r\n", "\n");
        var previewButtonClick = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(previewButtonClick, "var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();");
        AssertContains(previewButtonClick, "var previewFadeOutTask = _context.AnimatePreviewOutAsync();");
        AssertContains(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertOccursBefore(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);", "await viewModel.StopPreviewAsync(userInitiated: true);");

        var uiFadeOut = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeOutAsync");
        AssertContains(uiFadeOut, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(uiFadeOut, "To = 0,");
        AssertContains(uiFadeOut, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(uiFadeOut, "PREVIEW_AUDIO_FADE_OUT_STARTED");

        var vmStopRamp = ExtractMemberCode(audioMonitoringText, "RampPreviewVolumeDownForStopAsync");
        AssertContains(vmStopRamp, "_previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken)");

        var vmRampDown = ExtractMemberCode(audioVolumeTransitionText, "RampDownForAudioTransitionAsync");
        AssertContains(vmRampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(0);");

        var stopPreview = ExtractTextBetween(
            previewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "_context.RaisePreviewStopRequested();");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "await _context.SessionCoordinator.StopAudioPreviewAsync(cancellationToken);");

        AssertDoesNotContain(previewPropertyChangedText, "private Task ViewModel_PreviewRendererStopRequested()");
        var previewReinitStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(previewReinitStop, "=> _previewRendererHostController.StopRendererForReinitTeardownAsync();");
        AssertDoesNotContain(previewReinitStop, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }
}

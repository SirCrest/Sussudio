using System.Threading.Tasks;

static partial class Program
{
    private static Task MainWindowPropertyChangedRouting_LivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var previewText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewReinit.cs").Replace("\r\n", "\n");
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs").Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var outputText = ReadRepoFile("Sussudio/MainWindow.OutputPath.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedCaptureOptions.cs").Replace("\r\n", "\n");
        var audioText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var shellText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedShell.cs").Replace("\r\n", "\n");
        var liveSignalText = ReadRepoFile("Sussudio/MainWindow.LiveSignalInfo.cs").Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "var propertyName = e.PropertyName ?? string.Empty;");
        AssertContains(rootText, "TryHandleCaptureSelectionPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleStatusStripPropertyChanged(propertyName)");
        AssertContains(rootText, "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertContains(rootText, "TryHandleRecordingPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleOutputPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleShellPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleLiveSignalPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleFlashbackPropertyChanged(propertyName)");

        AssertOccursBefore(rootText, "TryHandleCaptureSelectionPropertyChanged(propertyName)", "TryHandleStatusStripPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleStatusStripPropertyChanged(propertyName)", "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertOccursBefore(rootText, "await TryHandlePreviewPropertyChangedAsync(propertyName)", "TryHandleRecordingPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleRecordingPropertyChanged(propertyName)", "TryHandleOutputPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleOutputPropertyChanged(propertyName)", "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleCaptureOptionPropertyChanged(propertyName)", "TryHandleAudioPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleAudioPropertyChanged(propertyName)", "TryHandleShellPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleShellPropertyChanged(propertyName)", "TryHandleLiveSignalPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleLiveSignalPropertyChanged(propertyName)", "TryHandleFlashbackPropertyChanged(propertyName)");

        AssertDoesNotContain(rootText, "case nameof(MainViewModel.");
        AssertDoesNotContain(rootText, "HandlePreviewingChangedAsync();");
        AssertDoesNotContain(rootText, "HandleRecordingChanged();");
        AssertDoesNotContain(rootText, "HandleFlashbackTimelineVisibleChanged();");
        AssertDoesNotContain(rootText, "HandleAudioPreviewActiveChanged();");

        AssertContains(previewText, "private PreviewLifecycleEventController _previewLifecycleEventController = null!;");
        AssertContains(previewText, "private void InitializePreviewLifecycleEventController()");
        AssertContains(previewText, "=> _previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewText, "=> _previewLifecycleEventController.HandlePreviewStartRequested();");
        AssertContains(previewText, "=> _previewLifecycleEventController.HandlePreviewStopRequested();");
        AssertContains(previewText, "private void ViewModel_PreviewStartRequested(object? sender, System.EventArgs e)");
        AssertContains(previewText, "private void ViewModel_PreviewStopRequested(object? sender, System.EventArgs e)");
        AssertContains(previewLifecycleControllerText, "internal sealed class PreviewLifecycleEventController");
        AssertContains(previewLifecycleControllerText, "private bool _stopRequestedByUser;");
        AssertContains(previewLifecycleControllerText, "public bool StopRequestedByUser => _stopRequestedByUser;");
        AssertContains(previewLifecycleControllerText, "public void SetStopRequestedByUser(bool value)");
        AssertContains(previewLifecycleControllerText, "case nameof(MainViewModel.IsPreviewing):");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(previewLifecycleControllerText, "case nameof(MainViewModel.IsPreviewReinitializing):");
        AssertContains(previewLifecycleControllerText, "_context.HandlePreviewReinitializingChanged();");
        AssertContains(previewLifecycleControllerText, "public void HandlePreviewStartRequested()");
        AssertContains(previewLifecycleControllerText, "public void HandlePreviewStopRequested()");
        AssertContains(previewLifecycleControllerText, "private async Task HandlePreviewingChangedAsync()");
        AssertDoesNotContain(previewText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(previewText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertDoesNotContain(previewText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertDoesNotContain(previewText, "private void HandlePreviewReinitializingChanged()");
        AssertDoesNotContain(previewText, "case nameof(MainViewModel.IsPreviewing):");
        AssertDoesNotContain(previewText, "await HandlePreviewingChangedAsync();");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertContains(previewReinitTransitionControllerText, "internal enum PreviewReinitCompletionPresentation");
        AssertContains(previewReinitTransitionControllerText, "D3D11_RENDERER_REINIT_FLAG flag=true");
        AssertContains(previewReinitTransitionControllerText, "PREVIEW_REINIT_ANIMATE_OUT");
        AssertContains(previewReinitTransitionControllerText, "PREVIEW_REINIT_ANIMATE_IN");
        AssertContains(previewReinitTransitionControllerText, "PREVIEW_REINIT_ANIMATE_RESET");
        AssertContains(previewReinitTransitionControllerText, "D3D11_RENDERER_REINIT_FLAG flag=false");
        AssertDoesNotContain(previewReinitText, "private bool _isPreviewReinitAnimating;");
        var rendererStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(rendererStop, "DisposeD3DPreviewRendererForReinit();");
        AssertContains(rendererStop, "catch (TimeoutException ex)");
        AssertContains(rendererStop, "PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
        AssertDoesNotContain(rendererStop, "renderer.StopRenderThread();");
        AssertContains(recordingText, "private bool TryHandleRecordingPropertyChanged(string propertyName)");
        AssertContains(recordingText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(outputText, "private bool TryHandleOutputPropertyChanged(string propertyName)");
        AssertContains(outputText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(captureOptionText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionText, "case nameof(MainViewModel.IsHdrEnabled):");
        AssertContains(audioText, "private bool TryHandleAudioPropertyChanged(string propertyName)");
        AssertContains(audioText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(shellText, "private bool TryHandleShellPropertyChanged(string propertyName)");
        AssertContains(shellText, "case nameof(MainViewModel.IsStatsVisible):");
        AssertContains(shellText, "=> _statsOverlayController.SyncStatsVisibility(ViewModel.IsStatsVisible);");
        AssertDoesNotContain(shellText, "StatsToggle.IsChecked = ViewModel.IsStatsVisible;");
        AssertDoesNotContain(shellText, "ApplyStatsVisibility(ViewModel.IsStatsVisible);");
        AssertContains(liveSignalText, "private bool TryHandleLiveSignalPropertyChanged(string propertyName)");
        AssertContains(liveSignalText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(flashbackText, "private bool TryHandleFlashbackPropertyChanged(string propertyName)");
        AssertContains(flashbackText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):");

        return Task.CompletedTask;
    }
}

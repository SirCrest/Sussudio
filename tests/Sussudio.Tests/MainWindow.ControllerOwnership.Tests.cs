using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainWindowPropertyChangedRouting_DelegatesToFocusedControllers()
    {
        var rootText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedRouterText = rootText;
        var previewText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewText, "TryHandlePreviewPropertyChangedAsync");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");
        var previewRendererHostControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var recordingStatePresentationControllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        var outputText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var outputPathControllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var captureOptionBindingControllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var audioText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var shellText = ReadMainWindowShellChromeAdapterSource();
        var statsOverlayCompositionControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var shellChromeControllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");
        var settingsShelfControllerText = shellChromeControllerText;
        var liveSignalText = shellText;
        var liveSignalControllerText = ReadRepoFile("Sussudio/Controllers/Shell/LiveSignalInfoController.cs").Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "private MainWindowPropertyChangedRouter _propertyChangedRouter = null!;");
        AssertContains(rootText, "private void InitializeMainWindowPropertyChangedRouter()");
        AssertContains(mainWindowText, "InitializeMainWindowPropertyChangedRouter();");
        AssertContains(rootText, "=> _propertyChangedRouter.RouteAsync(e.PropertyName);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PropertyChanged.cs")),
            "property-change router composition lives in the MainWindow root composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ControllerInitialization.cs")),
            "controller initialization partial folded into MainWindow root composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "MainWindowPropertyChangedRouter.cs")),
            "property-name route order lives in the MainWindow root composition");
        AssertContains(rootText, "TryHandleCaptureSelection = TryHandleCaptureSelectionPropertyChanged,");
        AssertContains(rootText, "TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,");
        AssertContains(rootText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(rootText, "TryHandleRecording = TryHandleRecordingPropertyChanged,");
        AssertContains(rootText, "TryHandleOutput = TryHandleOutputPropertyChanged,");
        AssertContains(rootText, "TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,");
        AssertContains(rootText, "TryHandleAudio = TryHandleAudioPropertyChanged,");
        AssertContains(rootText, "TryHandleShell = TryHandleShellPropertyChanged,");
        AssertContains(rootText, "TryHandleLiveSignal = TryHandleLiveSignalPropertyChanged,");
        AssertContains(rootText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");

        AssertContains(propertyChangedRouterText, "internal sealed class MainWindowPropertyChangedRouterContext");
        AssertContains(propertyChangedRouterText, "internal sealed class MainWindowPropertyChangedRouter");
        AssertContains(propertyChangedRouterText, "var propertyName = propertyNameValue ?? string.Empty;");
        AssertContains(propertyChangedRouterText, "_context.TryHandleCaptureSelection(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleStatusStrip(propertyName)");
        AssertContains(propertyChangedRouterText, "await _context.TryHandlePreviewAsync(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleRecording(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleOutput(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleCaptureOption(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleAudio(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleShell(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleLiveSignal(propertyName)");
        AssertContains(propertyChangedRouterText, "_context.TryHandleFlashback(propertyName)");

        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleCaptureSelection(propertyName)", "_context.TryHandleStatusStrip(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleStatusStrip(propertyName)", "await _context.TryHandlePreviewAsync(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "await _context.TryHandlePreviewAsync(propertyName)", "_context.TryHandleRecording(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleRecording(propertyName)", "_context.TryHandleOutput(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleOutput(propertyName)", "_context.TryHandleCaptureOption(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleCaptureOption(propertyName)", "_context.TryHandleAudio(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleAudio(propertyName)", "_context.TryHandleShell(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleShell(propertyName)", "_context.TryHandleLiveSignal(propertyName)");
        AssertOccursBefore(propertyChangedRouterText, "_context.TryHandleLiveSignal(propertyName)", "_context.TryHandleFlashback(propertyName)");

        AssertDoesNotContain(rootText, "case nameof(MainViewModel.");
        AssertDoesNotContain(rootText, "var propertyName = e.PropertyName ?? string.Empty;");
        AssertDoesNotContain(propertyChangedRouterText, "case nameof(MainViewModel.");
        AssertDoesNotContain(rootText, "HandlePreviewingChangedAsync();");
        AssertDoesNotContain(rootText, "HandleRecordingChanged();");
        AssertDoesNotContain(rootText, "HandleFlashbackTimelineVisibleChanged();");
        AssertDoesNotContain(rootText, "HandleAudioPreviewActiveChanged();");

        AssertContains(previewText, "private PreviewLifecycleEventController _previewLifecycleEventController = null!;");
        AssertContains(previewText, "private void InitializePreviewLifecycleEventController()");
        AssertContains(previewText, "=> _previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewText, "=> _previewLifecycleEventController.HandlePreviewStartRequested();");
        AssertContains(previewText, "=> _previewLifecycleEventController.HandlePreviewStopRequested();");
        AssertContains(previewText, "private void ViewModel_PreviewStartRequested(object? sender, EventArgs e)");
        AssertContains(previewText, "private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)");
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
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewReinitRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "HandlePreviewReinitializingChanged(");
        AssertDoesNotContain(previewPropertyChangedHandler, "case nameof(MainViewModel.IsPreviewing):");
        AssertDoesNotContain(previewPropertyChangedHandler, "await HandlePreviewingChangedAsync();");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewLifecycle.Composition.cs")),
            "preview reinit adapter lives in the preview transitions composition partial");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.HandleReinitializingChanged(");
        AssertContains(previewReinitText, "new PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,");
        AssertContains(previewReinitText, "RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,");
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertContains(previewReinitTransitionControllerText, "internal enum PreviewReinitCompletionPresentation");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitTransitionControllerText, "public void HandleReinitializingChanged(PreviewReinitCompletionPresentationContext context)");
        AssertContains(previewReinitTransitionControllerText, "D3D11_RENDERER_REINIT_FLAG flag=true");
        AssertContains(previewReinitTransitionControllerText, "PREVIEW_REINIT_ANIMATE_OUT");
        AssertContains(previewReinitTransitionControllerText, "PREVIEW_REINIT_ANIMATE_IN");
        AssertContains(previewReinitTransitionControllerText, "PREVIEW_REINIT_ANIMATE_RESET");
        AssertContains(previewReinitTransitionControllerText, "D3D11_RENDERER_REINIT_FLAG flag=false");
        AssertDoesNotContain(previewReinitText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(previewReinitText, "case PreviewReinitCompletionPresentation.");
        AssertDoesNotContain(previewReinitText, "GetCompletionPresentation(");
        var rendererStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(rendererStop, "=> _previewRendererHostController.StopRendererForReinitTeardownAsync();");
        AssertContains(previewRendererHostControllerText, "public Task StopRendererForReinitTeardownAsync()");
        AssertContains(previewRendererHostControllerText, "DisposeD3DPreviewRendererForReinit();");
        AssertContains(previewRendererHostControllerText, "catch (TimeoutException ex)");
        AssertContains(previewRendererHostControllerText, "PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
        AssertDoesNotContain(rendererStop, "renderer.StopRenderThread();");
        AssertContains(recordingText, "private bool TryHandleRecordingPropertyChanged(string propertyName)");
        AssertContains(recordingText, "=> _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(recordingText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(recordingStatePresentationControllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(recordingStatePresentationControllerText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(outputText, "private bool TryHandleOutputPropertyChanged(string propertyName)");
        AssertContains(outputText, "=> _outputPathController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(outputText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(outputPathControllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(outputPathControllerText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(captureOptionBindingsText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertContains(captureOptionBindingControllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(captureOptionBindingControllerText, "case nameof(MainViewModel.IsHdrEnabled):");
        AssertContains(audioText, "private bool TryHandleAudioPropertyChanged(string propertyName)");
        AssertContains(audioText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(audioText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(shellText, "private ShellPropertyChangedController _shellPropertyChangedController = null!;");
        AssertContains(shellText, "private void InitializeShellPropertyChangedController()");
        AssertContains(mainWindowText, "InitializeShellPropertyChangedController();");
        AssertContains(shellText, "private bool TryHandleShellPropertyChanged(string propertyName)");
        AssertContains(shellText, "=> _shellPropertyChangedController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(shellText, "_statsOverlayCompositionController.TryHandlePropertyChanged(propertyName, ViewModel.IsStatsVisible)");
        AssertDoesNotContain(shellText, "_settingsShelfController.TryHandlePropertyChanged(propertyName, ViewModel.IsSettingsVisible)");
        AssertDoesNotContain(shellText, "case nameof(MainViewModel.IsStatsVisible):");
        AssertDoesNotContain(shellText, "case nameof(MainViewModel.IsSettingsVisible):");
        AssertContains(shellChromeControllerText, "internal sealed class ShellPropertyChangedController");
        AssertContains(shellChromeControllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(shellChromeControllerText, "_context.StatsOverlayComposition.TryHandlePropertyChanged(propertyName, _context.IsStatsVisible())");
        AssertContains(shellChromeControllerText, "_context.SettingsShelf.TryHandlePropertyChanged(propertyName, _context.IsSettingsVisible())");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ShellPropertyChangedController.cs")),
            "shell property-change routing lives with shell chrome controller concerns");
        AssertContains(statsOverlayCompositionControllerText, "case nameof(MainViewModel.IsStatsVisible):");
        AssertContains(settingsShelfControllerText, "case nameof(MainViewModel.IsSettingsVisible):");
        AssertDoesNotContain(shellText, "StatsToggle.IsChecked = ViewModel.IsStatsVisible;");
        AssertDoesNotContain(shellText, "_statsOverlayController.SyncStatsVisibility(ViewModel.IsStatsVisible);");
        AssertContains(liveSignalText, "private bool TryHandleLiveSignalPropertyChanged(string propertyName)");
        AssertContains(liveSignalText, "=> _liveSignalInfoController.TryHandlePropertyChanged(");
        AssertDoesNotContain(liveSignalText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(liveSignalControllerText, "public bool TryHandlePropertyChanged(string propertyName, string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(liveSignalControllerText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(flashbackText, "private bool TryHandleFlashbackPropertyChanged(string propertyName)");
        AssertContains(flashbackText, "=> _flashbackPropertyChangedController.TryHandlePropertyChanged(propertyName);");
        AssertContains(flashbackControllerText, "internal sealed class FlashbackPropertyChangedController");
        AssertContains(flashbackControllerText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):");

        return Task.CompletedTask;
    }
}

using System.Linq;
using System.Reflection;
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


    internal static Task ControlBarHoverAnimations_LiveInController()
    {
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarAnimationController _controlBarAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeControlBarAnimationController()");
        AssertContains(adapterText, "SettingsToggleButton,");
        AssertContains(adapterText, "FrameTimeOverlayToggle,");
        AssertContains(adapterText, "=> _controlBarAnimationController.AttachHoverAnimations();");
        AssertContains(adapterText, "=> _controlBarAnimationController.EntranceButtons;");
        AssertContains(mainWindowText, "InitializeControlBarAnimationController();");
        AssertContains(mainWindowText, "SetupButtonHoverAnimations();");
        AssertContains(launchEntranceShellText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(controllerText, "internal sealed class ControlBarAnimationController");
        AssertContains(controllerText, "public IReadOnlyList<FrameworkElement> EntranceButtons");
        AssertContains(controllerText, "public void AttachHoverAnimations()");
        AssertContains(controllerText, "private static void AnimateScale(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ControlBarAnimationController.cs")),
            "control-bar hover animation lives with shell chrome controller concerns");
        AssertDoesNotContain(adapterText, "private FrameworkElement[] GetControlBarButtons()");

        return Task.CompletedTask;
    }

    internal static Task ShellElevationSetup_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ShellElevationController _shellElevationController = null!;");
        AssertContains(adapterText, "private void InitializeShellElevationController()");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "SettingsOverlayPanel = SettingsOverlayPanel,");
        AssertContains(adapterText, "RecordButton = RecordButton,");
        AssertContains(adapterText, "private void ApplyShellElevation()");
        AssertContains(adapterText, "=> _shellElevationController.Apply();");
        AssertContains(mainWindowText, "InitializeShellElevationController();");
        AssertContains(mainWindowText, "ApplyShellElevation();");
        AssertContains(controllerText, "internal sealed class ShellElevationController");
        AssertContains(controllerText, "var controlBarShadow = new ThemeShadow();");
        AssertContains(controllerText, "controlBarShadow.Receivers.Add(_context.SettingsOverlayPanel);");
        AssertContains(controllerText, "_context.ControlBarBorder.Translation = new Vector3(0, 0, 32);");
        AssertContains(controllerText, "var recordButtonShadow = new ThemeShadow();");
        AssertContains(controllerText, "_context.RecordButton.Translation = new Vector3(0, 0, 16);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ShellElevationController.cs")),
            "shell elevation setup lives with shell chrome controller concerns");
        AssertDoesNotContain(mainWindowText, "new Microsoft.UI.Xaml.Media.ThemeShadow()");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Translation = new System.Numerics.Vector3(0, 0, 32);");
        AssertDoesNotContain(mainWindowText, "RecordButton.Translation = new System.Numerics.Vector3(0, 0, 16);");

        return Task.CompletedTask;
    }

    internal static Task PreviewTransitionAnimations_LiveInController()
    {
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;");
        AssertContains(adapterText, "private void InitializePreviewTransitionAnimationController()");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewContentGrid = PreviewContentGrid,");
        AssertContains(adapterText, "StopPreviewFadeInTimer = StopPreviewFadeInTimer,");
        AssertContains(adapterText, "FadeOutVideoFrameShadow = FadeOutVideoFrameShadow,");
        AssertContains(adapterText, "FadeInVideoFrameShadow = FadeInVideoFrameShadow,");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.ResetPreviewContentTransform();");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AnimatePreviewOutAsync();");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AnimatePreviewInAsync();");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.PrepareStartupPresentation();");
        AssertContains(mainWindowText, "InitializePreviewTransitionAnimationController();");
        AssertContains(launchEntranceShellText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(controllerText, "internal sealed class PreviewTransitionAnimationController");
        AssertContains(controllerText, "public void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)");
        AssertContains(controllerText, "public Task AnimatePreviewOutAsync()");
        AssertContains(controllerText, "public Task AnimatePreviewInAsync()");
        AssertContains(controllerText, "_context.FadeOutVideoFrameShadow(150);");
        AssertContains(controllerText, "_context.FadeInVideoFrameShadow(0, 400);");
        AssertContains(controllerText, "public void PrepareStartupPresentation()");
        AssertContains(controllerText, "public void RevealUnavailablePlaceholder()");
        AssertContains(controllerText, "public static void FadeOutElement(UIElement element)");
        AssertContains(controllerText, "private Task AnimatePreviewTransitionAsync(");
        AssertContains(controllerText, "private static Task BeginStoryboardAsync(");
        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(previewSurfaceControllerText, "=> FadeIn(_videoShadowVisual, delayMs, durationMs);");
        AssertContains(previewSurfaceControllerText, "=> FadeOut(_videoShadowVisual, durationMs);");
        AssertContains(previewSurfaceControllerText, "=> FadeIn(_controlBarShadowVisual, delayMs, durationMs);");
        AssertContains(previewSurfaceControllerText, "private static void FadeIn(SpriteVisual? visual, int delayMs, int durationMs)");
        AssertContains(previewSurfaceControllerText, "private static void FadeOut(SpriteVisual? visual, int durationMs)");
        AssertContains(previewSurfaceControllerText, "if (visual == null)");
        AssertContains(previewSurfaceControllerText, "CreateScalarKeyFrameAnimation()");
        AssertContains(previewSurfaceControllerText, "animation.InsertKeyFrame(0f, 0f);");
        AssertContains(previewSurfaceControllerText, "compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f))");
        AssertContains(previewSurfaceControllerText, "animation.DelayTime = TimeSpan.FromMilliseconds(delayMs);");
        AssertContains(previewSurfaceControllerText, "visual.StartAnimation(\"Opacity\", animation);");
        AssertDoesNotContain(controllerText, "PreviewShadowFadeAnimator.");
        AssertDoesNotContain(previewSurfaceControllerText, "PreviewShadowFadeAnimator.");
        AssertDoesNotContain(adapterText, "FadeOutVideoFrameShadow(durationMs: 150);");
        AssertDoesNotContain(adapterText, "FadeInVideoFrameShadow(delayMs: 0, durationMs: 400);");
        AssertDoesNotContain(adapterText, "private Task AnimatePreviewTransitionAsync(");
        AssertDoesNotContain(adapterText, "private static Task BeginStoryboardAsync(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupOverlay_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewStartupOverlayController _previewStartupOverlayController = null!;");
        AssertContains(adapterText, "private void InitializePreviewStartupOverlayController()");
        AssertContains(adapterText, "PreviewLoadingOverlay = PreviewLoadingOverlay,");
        AssertContains(adapterText, "=> _previewStartupOverlayController.Start();");
        AssertContains(adapterText, "=> _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);");
        AssertContains(mainWindowText, "InitializePreviewStartupOverlayController();");
        AssertContains(controllerText, "internal sealed class PreviewStartupOverlayController");
        AssertContains(controllerText, "public void Start()");
        AssertContains(controllerText, "public void Stop(bool isPreviewReinitAnimating)");
        AssertContains(controllerText, "var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];");
        AssertContains(controllerText, "ring.IsActive = true;");
        AssertContains(controllerText, "ring.IsActive = false;");
        AssertContains(controllerText, "_context.PreviewLoadingOverlay.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.PreviewLoadingOverlay.Opacity = 1.0;");
        AssertContains(controllerText, "PreviewTransitionAnimationController.FadeInElement(_context.PreviewLoadingOverlay);");
        AssertContains(controllerText, "PreviewTransitionAnimationController.FadeOutElement(_context.PreviewLoadingOverlay);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupOverlayController.cs")),
            "preview startup overlay lives with preview transition presentation concerns");
        AssertDoesNotContain(adapterText, "FadeInElement = FadeInElement,");
        AssertDoesNotContain(adapterText, "FadeOutElement = FadeOutElement,");
        AssertDoesNotContain(adapterText, "var ring = (ProgressRing)");
        AssertDoesNotContain(adapterText, "PreviewLoadingOverlay.Visibility = Visibility.Collapsed;");

        return Task.CompletedTask;
    }

    internal static Task PreviewFadeInReveal_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewTransitionText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(adapterText, "private void InitializePreviewFadeInController()");
        AssertContains(adapterText, "DispatcherQueue = _dispatcherQueue,");
        AssertContains(adapterText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(adapterText, "AnimatePreviewInAsync = AnimatePreviewInAsync,");
        AssertContains(adapterText, "StartPreviewAudioFadeIn = () => StartPreviewAudioFadeIn(),");
        AssertContains(adapterText, "=> _previewFadeInController.Schedule();");
        AssertContains(adapterText, "=> _previewFadeInController.Stop();");
        AssertContains(mainWindowText, "InitializePreviewFadeInController();");
        AssertContains(previewTransitionText, "StopPreviewFadeInTimer = StopPreviewFadeInTimer,");
        AssertContains(controllerText, "internal sealed class PreviewFadeInController");
        AssertContains(controllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(controllerText, "_timer.Interval = TimeSpan.FromMilliseconds(50);");
        AssertContains(controllerText, "_timer.Interval = TimeSpan.FromMilliseconds(16);");
        AssertContains(controllerText, "var baselineFrames = renderer.FramesRendered;");
        AssertContains(controllerText, "current == null || current != renderer");
        AssertContains(controllerText, "PREVIEW_FADE_IN_READY framesRendered={rendered} baseline={baselineFrames}");
        AssertOccursBefore(controllerText, "_ = _context.AnimatePreviewInAsync();", "_context.StartPreviewAudioFadeIn();");
        AssertDoesNotContain(adapterText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertDoesNotContain(adapterText, "private DispatcherQueueTimer? _previewFadeInTimer;");
        AssertDoesNotContain(adapterText, "PREVIEW_FADE_IN_READY");

        return Task.CompletedTask;
    }


    internal static Task RecordingButtonChrome_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var recordingPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Recording", "Button", "RecordingButtonChromeController.cs")),
            "recording button chrome controller folded into RecordingControlsControllers.cs");
        var presentationStart = controllerText.IndexOf("internal sealed class RecordingStatePresentationControllerContext", System.StringComparison.Ordinal);
        if (presentationStart < 0)
        {
            throw new System.InvalidOperationException("RecordingStatePresentationControllerContext was not found in RecordingControlsControllers.cs.");
        }
        var recordingPresentationText = controllerText[presentationStart..];

        AssertContains(recordingPropertyChangedText, "private RecordingButtonChromeController _recordingButtonChromeController = null!;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PropertyChangedRecording.cs")),
            "recording property-changed adapter folded into MainWindow.ControlBindings.cs");
        AssertContains(recordingPropertyChangedText, "private void InitializeRecordingButtonChromeController()");
        AssertContains(recordingPropertyChangedText, "RecordingGlowBorder = RecordingGlowBorder,");
        AssertContains(recordingPropertyChangedText, "RecordingGlowPulseStoryboard = RecordingGlowPulseStoryboard,");
        AssertContains(recordingPropertyChangedText, "RecPulseStoryboard = RecPulseStoryboard,");
        AssertContains(recordingPropertyChangedText, "RecordButton = RecordButton,");
        AssertContains(recordingPropertyChangedText, "RecordButtonNormalContent = RecordButtonNormalContent,");
        AssertContains(recordingPropertyChangedText, "RecordButtonStartingContent = RecordButtonStartingContent,");
        AssertContains(recordingPropertyChangedText, "RecordButtonRecordingContent = RecordButtonRecordingContent,");
        AssertContains(mainWindowText, "InitializeRecordingButtonChromeController();");
        AssertContains(propertyChangedText, "TryHandleRecording = TryHandleRecordingPropertyChanged,");
        AssertContains(recordingPropertyChangedText, "=> _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(recordingPropertyChangedText, "RecordingButtonChrome = _recordingButtonChromeController,");
        AssertContains(recordingPresentationText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(recordingPresentationText, "HandleRecordingChanged();");
        AssertContains(recordingPresentationText, "public required RecordingButtonChromeController RecordingButtonChrome { get; init; }");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyRecordingGlow(isRecording);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyRecordingButtonState(isRecording);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyRecordingPulse(isRecording);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyTransitioningState(viewModel.IsRecording, state);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyFfmpegMissingState(state);");
        AssertContains(controllerText, "internal sealed class RecordingButtonChromeController");
        AssertContains(controllerText, "private const double CollapsedRecordButtonWidth = 36;");
        AssertContains(controllerText, "public void ApplyRecordingGlow(bool isRecording)");
        AssertContains(controllerText, "_context.RecordingGlowBorder.Opacity = 1.0;");
        AssertContains(controllerText, "_context.RecordingGlowPulseStoryboard.Begin();");
        AssertContains(controllerText, "_context.RecordingGlowPulseStoryboard.Stop();");
        AssertContains(controllerText, "_context.RecordingGlowBorder.Opacity = 0;");
        AssertContains(controllerText, "public void ApplyRecordingButtonState(bool isRecording)");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.IsActive = false;");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.RecordButtonNormalContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.RecordButtonRecordingContent.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.RecordButton.Padding = new Thickness(12, 0, 12, 0);");
        AssertContains(controllerText, "_context.RecordButton.Width = double.NaN;");
        AssertContains(controllerText, "_context.RecordButton.UpdateLayout();");
        AssertContains(controllerText, "var targetWidth = _context.RecordButton.ActualWidth;");
        AssertContains(controllerText, "_context.RecordButton.Width = CollapsedRecordButtonWidth;");
        AssertContains(controllerText, "AnimateWidth(CollapsedRecordButtonWidth, targetWidth, null);");
        AssertContains(controllerText, "var currentWidth = _context.RecordButton.ActualWidth;");
        AssertContains(controllerText, "AnimateWidth(currentWidth, CollapsedRecordButtonWidth, () =>");
        AssertContains(controllerText, "_context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.RecordButtonNormalContent.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.RecordButton.Padding = new Thickness(0);");
        AssertContains(controllerText, "public void ApplyRecordingPulse(bool isRecording)");
        AssertContains(controllerText, "_context.RecPulseStoryboard.Begin();");
        AssertContains(controllerText, "_context.RecPulseStoryboard.Stop();");
        AssertContains(controllerText, "public void ApplyTransitioningState(bool isRecording, RecordingStatePresentationState state)");
        AssertContains(controllerText, "_context.RecordButton.IsEnabled = state.TransitionRecordButtonEnabled;");
        AssertContains(controllerText, "_context.RecordButton.Width = _context.RecordButton.ActualWidth;");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.IsActive = state.TransitionStartingContentActive;");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "public void ApplyFfmpegMissingState(RecordingStatePresentationState state)");
        AssertContains(controllerText, "_context.RecordButton.IsEnabled = state.FfmpegRecordButtonEnabled;");
        AssertContains(controllerText, "private void AnimateWidth(double from, double to, Action? onCompleted = null)");
        AssertContains(controllerText, "Duration = new Duration(TimeSpan.FromMilliseconds(200)),");
        AssertContains(controllerText, "EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "Storyboard.SetTarget(anim, _context.RecordButton);");
        AssertContains(controllerText, "Storyboard.SetTargetProperty(anim, \"Width\");");
        AssertContains(controllerText, "_context.RecordButton.Width = to == CollapsedRecordButtonWidth ? CollapsedRecordButtonWidth : double.NaN;");
        AssertOccursBefore(controllerText, "_context.RecordButton.UpdateLayout();", "var targetWidth = _context.RecordButton.ActualWidth;");
        AssertDoesNotContain(recordingPresentationText, "public required Action<double, double, Action?> AnimateRecordButtonWidth { get; init; }");
        AssertDoesNotContain(recordingPresentationText, "_context.AnimateRecordButtonWidth(");
        AssertDoesNotContain(recordingPresentationText, "_context.RecordButton.");
        AssertDoesNotContain(recordingPresentationText, "_context.RecordButtonStartingContent.");
        AssertDoesNotContain(recordingPresentationText, "_context.RecordingGlowPulseStoryboard.");
        AssertDoesNotContain(recordingPresentationText, "_context.RecPulseStoryboard.");
        AssertDoesNotContain(recordingPropertyChangedText, "Storyboard.SetTarget(anim, RecordButton);");

        return Task.CompletedTask;
    }

    internal static Task RecordingStatePresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Recording", "RecordingStatePresentationController.cs")),
            "recording state presentation lives with recording button chrome instead of returning as a tiny adjacent file");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        const string presentationMarker = "internal sealed class RecordingStatePresentationControllerContext";
        var presentationStart = controllerText.IndexOf(presentationMarker, System.StringComparison.Ordinal);
        if (presentationStart < 0)
        {
            throw new System.InvalidOperationException("RecordingStatePresentationControllerContext was not found in RecordingControlsControllers.cs.");
        }

        const string policyMarker = "internal static class RecordingStatePresentationPolicy";
        var policyStart = controllerText.IndexOf(policyMarker, System.StringComparison.Ordinal);
        if (policyStart < 0)
        {
            throw new System.InvalidOperationException("RecordingStatePresentationPolicy was not found in RecordingControlsControllers.cs.");
        }

        var presentationText = controllerText[presentationStart..policyStart];
        var policyText = controllerText[policyStart..];

        AssertContains(adapterText, "private RecordingStatePresentationController _recordingStatePresentationController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingStatePresentationController()");
        AssertContains(adapterText, "RecordingButtonChrome = _recordingButtonChromeController,");
        AssertContains(adapterText, "AudioRecordToggle = AudioRecordToggle,");
        AssertContains(adapterText, "AnalogAudioGainSlider = AnalogAudioGainSlider,");
        AssertContains(adapterText, "ApplyWindowTitle = ApplyWindowTitle,");
        AssertContains(adapterText, "=> _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(adapterText, "private void ApplyInitialRecordingStatePresentation()");
        AssertContains(adapterText, "=> _recordingStatePresentationController.HandleFfmpegMissingChanged();");
        AssertContains(bindingsText, "ApplyInitialRecordingStatePresentation();");
        AssertContains(mainWindowText, "InitializeRecordingStatePresentationController();");
        AssertContains(controllerText, "internal sealed class RecordingStatePresentationController");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsRecordingTransitioning):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsFfmpegMissing):");
        AssertContains(controllerText, "public void HandleRecordingChanged()");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyRecordingGlow(isRecording);");
        AssertContains(controllerText, "_context.ResetAudioMeterVisuals();");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyRecordingButtonState(isRecording);");
        AssertContains(controllerText, "RecordingStatePresentationPolicy.Build(new RecordingStatePresentationInput(");
        AssertContains(controllerText, "_context.AudioInputComboBox.IsEnabled = state.AudioInputComboBoxEnabled;");
        AssertContains(controllerText, "_context.AnalogAudioGainSlider.IsEnabled = state.AnalogAudioGainSliderEnabled;");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyRecordingPulse(isRecording);");
        AssertContains(controllerText, "_context.ApplyWindowTitle();");
        AssertContains(controllerText, "public void HandleRecordingTransitioningChanged()");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyTransitioningState(viewModel.IsRecording, state);");
        AssertContains(controllerText, "public void HandleFfmpegMissingChanged()");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyFfmpegMissingState(state);");
        AssertContains(policyText, "internal static class RecordingStatePresentationPolicy");
        AssertContains(policyText, "internal static RecordingStatePresentationState Build(RecordingStatePresentationInput input)");
        AssertContains(policyText, "internal readonly record struct RecordingStatePresentationInput(");
        AssertContains(policyText, "internal readonly record struct RecordingStatePresentationState(");
        AssertContains(policyText, "DeviceAudioMode.Analog");
        AssertContains(policyText, "StringComparison.OrdinalIgnoreCase");
        AssertDoesNotContain(policyText, "Microsoft.UI.Xaml");
        AssertDoesNotContain(policyText, "Storyboard");
        AssertDoesNotContain(adapterText, "RecordingGlowPulseStoryboard.Begin();");
        AssertDoesNotContain(adapterText, "RecordButtonStartingContent.IsActive = false;");
        AssertDoesNotContain(adapterText, "AnimateRecordButtonWidth = AnimateRecordButtonWidth,");
        AssertDoesNotContain(adapterText, "AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled");
        AssertDoesNotContain(adapterText, "RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.");
        AssertDoesNotContain(adapterText, "=> _recordingStatePresentationController.HandleRecordingChanged();");
        AssertDoesNotContain(bindingsText, "RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing");
        AssertDoesNotContain(presentationText, "_context.RecordButton.");
        AssertDoesNotContain(presentationText, "_context.RecordButtonStartingContent.");
        AssertDoesNotContain(presentationText, "_context.RecordingGlowPulseStoryboard.");
        AssertDoesNotContain(presentationText, "string.Equals(viewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog");
        AssertDoesNotContain(presentationText, "_context.ViewModel.IsFfmpegMissing &&");

        return Task.CompletedTask;
    }

    internal static Task RecordingStatePresentationPolicy_PreservesLockoutRules()
    {
        var policyType = RequireType("Sussudio.Controllers.RecordingStatePresentationPolicy");
        var inputType = RequireType("Sussudio.Controllers.RecordingStatePresentationInput");
        var build = policyType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingStatePresentationPolicy.Build was not found.");
        var constructor = inputType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 7);

        object Build(
            bool isRecording = false,
            bool isRecordingTransitioning = false,
            bool isFfmpegMissing = false,
            bool isCustomAudioInputEnabled = true,
            bool isMicrophoneEnabled = true,
            bool isDeviceAudioControlSupported = true,
            string? selectedDeviceAudioMode = "Analog")
        {
            var input = constructor.Invoke(new object?[]
            {
                isRecording,
                isRecordingTransitioning,
                isFfmpegMissing,
                isCustomAudioInputEnabled,
                isMicrophoneEnabled,
                isDeviceAudioControlSupported,
                selectedDeviceAudioMode
            });

            return build.Invoke(null, new[] { input })
                ?? throw new InvalidOperationException("RecordingStatePresentationPolicy.Build returned null.");
        }

        var idleAnalog = Build();
        AssertEqual(true, GetBoolProperty(idleAnalog, "AudioRecordToggleEnabled"), "idle enables audio record toggle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "CustomAudioToggleEnabled"), "idle enables custom audio toggle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "MicrophoneToggleEnabled"), "idle enables microphone toggle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "AudioInputComboBoxEnabled"), "custom audio enables input combo while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "MicrophoneComboBoxEnabled"), "microphone enables combo while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "DeviceAudioModeToggleEnabled"), "device audio controls enable mode toggle while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "AnalogAudioGainSliderEnabled"), "analog device audio enables gain while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "TransitionRecordButtonEnabled"), "idle transition state enables record button");
        AssertEqual(true, GetBoolProperty(idleAnalog, "FfmpegRecordButtonEnabled"), "available FFmpeg enables record button");
        AssertEqual(false, GetBoolProperty(idleAnalog, "TransitionStartingContentActive"), "idle transition hides starting content");
        AssertEqual(true, GetBoolProperty(idleAnalog, "SettledNormalContentVisible"), "idle settled content shows normal record button");
        AssertEqual(false, GetBoolProperty(idleAnalog, "SettledRecordingContentVisible"), "idle settled content hides recording button");

        var recording = Build(isRecording: true);
        AssertEqual(false, GetBoolProperty(recording, "AudioRecordToggleEnabled"), "recording locks audio record toggle");
        AssertEqual(false, GetBoolProperty(recording, "CustomAudioToggleEnabled"), "recording locks custom audio toggle");
        AssertEqual(false, GetBoolProperty(recording, "MicrophoneToggleEnabled"), "recording locks microphone toggle");
        AssertEqual(false, GetBoolProperty(recording, "AudioInputComboBoxEnabled"), "recording locks audio input combo");
        AssertEqual(false, GetBoolProperty(recording, "MicrophoneComboBoxEnabled"), "recording locks microphone combo");
        AssertEqual(false, GetBoolProperty(recording, "DeviceAudioModeToggleEnabled"), "recording locks device audio mode");
        AssertEqual(false, GetBoolProperty(recording, "AnalogAudioGainSliderEnabled"), "recording locks analog gain");
        AssertEqual(false, GetBoolProperty(recording, "SettledNormalContentVisible"), "recording hides normal content");
        AssertEqual(true, GetBoolProperty(recording, "SettledRecordingContentVisible"), "recording shows recording content");

        var unsupportedAnalog = Build(isDeviceAudioControlSupported: false);
        AssertEqual(false, GetBoolProperty(unsupportedAnalog, "DeviceAudioModeToggleEnabled"), "unsupported device audio disables mode");
        AssertEqual(false, GetBoolProperty(unsupportedAnalog, "AnalogAudioGainSliderEnabled"), "unsupported device audio disables gain");

        var hdmiMode = Build(selectedDeviceAudioMode: "HDMI");
        AssertEqual(false, GetBoolProperty(hdmiMode, "AnalogAudioGainSliderEnabled"), "non-analog device audio disables gain");

        var transition = Build(isRecordingTransitioning: true);
        AssertEqual(false, GetBoolProperty(transition, "TransitionRecordButtonEnabled"), "transition disables record button through transition handler");
        AssertEqual(false, GetBoolProperty(transition, "FfmpegRecordButtonEnabled"), "transition disables record button through FFmpeg handler");
        AssertEqual(true, GetBoolProperty(transition, "TransitionStartingContentActive"), "transition activates starting content");

        var ffmpegMissing = Build(isFfmpegMissing: true);
        AssertEqual(true, GetBoolProperty(ffmpegMissing, "TransitionRecordButtonEnabled"), "FFmpeg missing does not affect transition handler enablement");
        AssertEqual(false, GetBoolProperty(ffmpegMissing, "FfmpegRecordButtonEnabled"), "FFmpeg missing disables record button through FFmpeg handler");

        var inactiveInputs = Build(isCustomAudioInputEnabled: false, isMicrophoneEnabled: false);
        AssertEqual(false, GetBoolProperty(inactiveInputs, "AudioInputComboBoxEnabled"), "custom audio disabled locks input combo");
        AssertEqual(false, GetBoolProperty(inactiveInputs, "MicrophoneComboBoxEnabled"), "microphone disabled locks microphone combo");

        return Task.CompletedTask;
    }
}

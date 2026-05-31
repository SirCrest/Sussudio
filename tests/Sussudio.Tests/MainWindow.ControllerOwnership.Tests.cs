using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

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
        var recordingText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var recordingStatePresentationControllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        var outputText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var outputPathControllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionBindingControllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var audioText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var shellText = ReadMainWindowShellChromeAdapterSource();
        var statsOverlayCompositionControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var shellChromeControllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");
        var settingsShelfControllerText = shellChromeControllerText;
        var liveSignalText = shellText;
        var liveSignalControllerText = shellChromeControllerText;
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
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ControlBindings.cs")),
            "XAML control binding adapter folded into MainWindow root code-behind");
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
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfacePresentationController.cs")),
            "preview surface presentation folded into PreviewTransitionAnimationController.cs");
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
        var recordingPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
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
            "recording property-changed adapter folded into MainWindow.xaml.cs");
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
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
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

internal static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarLabelVisibilityController _controlBarLabelVisibilityController = null!;");
        AssertContains(adapterText, "private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;");
        AssertContains(adapterText, "private void InitializeResponsiveShellLayoutController()");
        AssertContains(adapterText, "var controlBarLabels = new UIElement[]");
        AssertContains(adapterText, "CaptureSettingsGrid = CaptureSettingsGrid,");
        AssertContains(adapterText, "FlashbackToggleLabel,");
        AssertContains(adapterText, "_controlBarLabelVisibilityController = new ControlBarLabelVisibilityController(new ControlBarLabelVisibilityControllerContext");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "ControlBarLabels = controlBarLabels,");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()");
        AssertContains(adapterText, "_controlBarLabelVisibilityController.Attach();");
        AssertContains(adapterText, "_responsiveShellLayoutController.Attach();");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()\n    {\n        _controlBarLabelVisibilityController.Attach();\n        _responsiveShellLayoutController.Attach();\n    }");
        AssertContains(xamlText, "x:Name=\"FlashbackToggleLabel\"");
        AssertContains(mainWindowText, "InitializeResponsiveShellLayoutController();");
        AssertContains(bindingsText, "SetupResponsiveShellLayoutBindings();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Bindings.cs")),
            "root startup binding sequence lives with the MainWindow composition root");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(controllerText, "internal sealed class ControlBarLabelVisibilityController");
        AssertContains(controllerText, "public required UIElement[] ControlBarLabels { get; init; }");
        AssertContains(controllerText, "internal static class ResponsiveShellLayoutPolicy");
        AssertContains(controllerText, "public const double ControlBarLabelThreshold = 900.0;");
        AssertContains(controllerText, "public const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(controllerText, "internal readonly record struct ResponsiveCaptureSettingsPlacement");
        AssertContains(controllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);");
        AssertContains(controllerText, "foreach (var label in _context.ControlBarLabels)");
        AssertContains(controllerText, "label.Visibility = visibility;");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind(width);");
        AssertContains(controllerText, "private void ApplyCaptureSettingsLayout(ResponsiveCaptureSettingsPlacement placement)");
        AssertContains(controllerText, "private static void ApplyGridSlot(FrameworkElement element, ResponsiveGridSlot slot)");
        AssertContains(agentMapText, "complete control-bar label set");
        AssertContains(cleanupPlanText, "complete control-bar label set");
        AssertDoesNotContain(mainWindowText, "private bool _toggleLabelsVisible;");
        AssertDoesNotContain(mainWindowText, "private bool _captureSettingsNarrow;");
        AssertDoesNotContain(mainWindowText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double CaptureSettingsNarrowWidth = 700.0;");
        AssertDoesNotContain(controllerText, "_context.HdrToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(controllerText, "_context.FrameTimeOverlayToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(adapterText, "FlashbackToggleLabel = FlashbackToggleLabel,");
        AssertDoesNotContain(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertDoesNotContain(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ResponsiveShellLayout.cs")),
            "responsive shell layout adapter lives with shell chrome composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ControlBarLabelVisibilityController.cs")),
            "control-bar label visibility lives with responsive shell layout application");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ResponsiveShellLayoutPolicy.cs")),
            "responsive shell layout policy lives with responsive shell layout application");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ResponsiveShellLayoutController.cs")),
            "responsive shell layout application lives with shell chrome");

        return Task.CompletedTask;
    }

    internal static Task ResponsiveShellLayoutPolicy_PreservesBreakpointsAndPlacements()
    {
        var policyType = RequireType("Sussudio.Controllers.ResponsiveShellLayoutPolicy");
        var shouldShowLabels = policyType.GetMethod(
            "ShouldShowControlBarLabels",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels not found.");
        var getLayoutKind = policyType.GetMethod(
            "GetCaptureSettingsLayoutKind",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind not found.");
        var getPlacement = policyType.GetMethod(
            "GetCaptureSettingsPlacement",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.GetCaptureSettingsPlacement not found.");

        AssertEqual(false, (bool)shouldShowLabels.Invoke(null, new object[] { 899.99 })!, "control bar labels below 900");
        AssertEqual(true, (bool)shouldShowLabels.Invoke(null, new object[] { 900.0 })!, "control bar labels at 900");

        var narrowKind = getLayoutKind.Invoke(null, new object[] { 699.99 })
            ?? throw new InvalidOperationException("Narrow responsive shell layout kind was null.");
        var wideKind = getLayoutKind.Invoke(null, new object[] { 700.0 })
            ?? throw new InvalidOperationException("Wide responsive shell layout kind was null.");
        AssertEqual("Narrow", narrowKind.ToString()!, "capture settings below 700");
        AssertEqual("Wide", wideKind.ToString()!, "capture settings at 700");

        var narrowPlacement = getPlacement.Invoke(null, new[] { narrowKind })
            ?? throw new InvalidOperationException("Narrow responsive shell placement was null.");
        AssertEqual(true, GetBoolProperty(narrowPlacement, "CollapseCaptureOptionColumns"), "narrow columns collapse");
        AssertGridSlot(narrowPlacement, "VideoFormat", 1, 1);
        AssertGridSlot(narrowPlacement, "Preset", 1, 2);
        AssertGridSlot(narrowPlacement, "Split", 1, 3);
        AssertGridSlot(narrowPlacement, "CustomBitrate", 1, 2);

        var widePlacement = getPlacement.Invoke(null, new[] { wideKind })
            ?? throw new InvalidOperationException("Wide responsive shell placement was null.");
        AssertEqual(false, GetBoolProperty(widePlacement, "CollapseCaptureOptionColumns"), "wide columns stay flexible");
        AssertGridSlot(widePlacement, "VideoFormat", 0, 0);
        AssertGridSlot(widePlacement, "Preset", 0, 5);
        AssertGridSlot(widePlacement, "Split", 0, 6);
        AssertGridSlot(widePlacement, "CustomBitrate", 0, 5);

        return Task.CompletedTask;
    }

    private static void AssertGridSlot(object placement, string propertyName, int expectedRow, int expectedColumn)
    {
        var slot = GetPropertyValue(placement, propertyName)
            ?? throw new InvalidOperationException($"Responsive grid slot '{propertyName}' was null.");
        AssertEqual(expectedRow, GetIntProperty(slot, "Row"), $"{propertyName} row");
        AssertEqual(expectedColumn, GetIntProperty(slot, "Column"), $"{propertyName} column");
    }
    internal static Task OutputPathDisplay_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var setupBindingsText = ExtractMemberCode(bindingsText, "SetupBindings");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Recording", "Output", "OutputPathController.cs")),
            "output path controller folded into RecordingControlsControllers.cs");
        const string formatterMarker = "internal static class OutputPathDisplayTextFormatter";
        var formatterStart = controllerText.IndexOf(formatterMarker, System.StringComparison.Ordinal);
        if (formatterStart < 0)
        {
            throw new System.InvalidOperationException("OutputPathDisplayTextFormatter was not found in RecordingControlsControllers.cs.");
        }

        var formatterText = controllerText[formatterStart..];

        AssertContains(adapterText, "private OutputPathController _outputPathController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathController()");
        AssertContains(adapterText, "OutputPathTextBox = OutputPathTextBox,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "private void AttachOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathController.AttachDisplay();");
        AssertContains(adapterText, "private void UpdateOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathController.UpdateDisplay();");
        AssertContains(mainWindowText, "InitializeOutputPathController();");
        AssertContains(bindingsText, "AttachOutputPathDisplay();");
        AssertContains(propertyChangedText, "TryHandleOutput = TryHandleOutputPropertyChanged,");
        AssertContains(adapterText, "private bool TryHandleOutputPropertyChanged(string propertyName)");
        AssertContains(adapterText, "=> _outputPathController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(controllerText, "internal sealed class OutputPathController");
        AssertContains(controllerText, "public void AttachDisplay()");
        AssertContains(controllerText, "public void UpdateDisplay()");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(controllerText, "UpdateDisplay();");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.OutputPathTextBox, path);");
        AssertContains(controllerText, "OutputPathDisplayTextFormatter.Format(path, availableWidth);");
        AssertContains(formatterText, "internal static class OutputPathDisplayTextFormatter");
        AssertContains(formatterText, "public static string Format(string path, double availableWidth)");
        AssertContains(formatterText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertContains(formatterText, "var parts = path.Split('\\\\', '/');");
        AssertContains(formatterText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(adapterText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertDoesNotContain(adapterText, "var parts = path.Split('\\\\', '/');");
        AssertDoesNotContain(adapterText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(setupBindingsText, "OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();");
        AssertDoesNotContain(setupBindingsText, "private void UpdateOutputPathDisplay()");
        AssertDoesNotContain(setupBindingsText, "ToolTipService.SetToolTip(OutputPathTextBox, path);");

        return Task.CompletedTask;
    }

    internal static Task OutputPathDisplayTextFormatter_PreservesTruncationPolicy()
    {
        var formatterType = RequireType("Sussudio.Controllers.OutputPathDisplayTextFormatter");
        var format = formatterType.GetMethod("Format", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("OutputPathDisplayTextFormatter.Format was not found.");

        string Format(string path, double availableWidth)
        {
            return format.Invoke(null, new object[] { path, availableWidth })?.ToString()
                ?? throw new InvalidOperationException("OutputPathDisplayTextFormatter.Format returned null.");
        }

        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", 240),
            "Full output path fits when width has enough characters");
        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", 0),
            "Zero output path width preserves full path");
        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", -10),
            "Negative output path width preserves full path");
        AssertEqual(
            "clip-with-a-very-long-name.mp4",
            Format("clip-with-a-very-long-name.mp4", 40),
            "Simple path without folder segments stays unchanged");
        AssertEqual(
            "C:\\...\\session\\captures\\clip.mp4",
            Format("C:\\users\\crest\\videos\\session\\captures\\clip.mp4", 250),
            "Deep output path keeps root and fitting tail segments");
        AssertEqual(
            "C:\\...\\clip.mp4",
            Format("C:\\users\\crest\\videos\\session\\captures\\clip.mp4", 80),
            "Deep output path falls back to root and filename");

        return Task.CompletedTask;
    }

    internal static Task OutputPathButtonActions_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Recording", "Output", "OutputPathController.cs")),
            "output path button actions folded into RecordingControlsControllers.cs");

        AssertContains(adapterText, "private OutputPathController _outputPathController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathController()");
        AssertContains(adapterText, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "SetOutputPath = path => ViewModel.OutputPath = path,");
        AssertContains(adapterText, "SetStatusText = text => ViewModel.StatusText = text,");
        AssertContains(adapterText, "OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()");
        AssertContains(adapterText, "private Task BrowseOutputPathFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathController.BrowseAsync();");
        AssertContains(adapterText, "private Task OpenRecordingsFolderFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathController.OpenRecordingsFolderIfAvailableAsync();");
        AssertContains(adapterText, "private void BrowseButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));");
        AssertContains(adapterText, "private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));");
        AssertContains(mainWindowText, "InitializeOutputPathController();");
        AssertContains(controllerText, "internal sealed class OutputPathController");
        AssertContains(controllerText, "public async Task BrowseAsync()");
        AssertContains(controllerText, "var picker = new FolderPicker();");
        AssertContains(controllerText, "picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;");
        AssertContains(controllerText, "picker.FileTypeFilter.Add(\"*\");");
        AssertContains(controllerText, "WinRT.Interop.InitializeWithWindow.Initialize(picker, _context.GetWindowHandle());");
        AssertContains(controllerText, "await picker.PickSingleFolderAsync();");
        AssertContains(controllerText, "_context.SetOutputPath(folder.Path);");
        AssertContains(controllerText, "_context.SetStatusText($\"Error selecting folder: {ex.Message}\");");
        AssertContains(controllerText, "public Task OpenRecordingsFolderIfAvailableAsync()");
        AssertContains(controllerText, "var path = _context.GetOutputPath();");
        AssertContains(controllerText, "string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)");
        AssertContains(controllerText, "return _context.OpenRecordingsFolderAsync();");
        AssertDoesNotContain(adapterText, "ViewModel.BrowseOutputPathAsync()");
        AssertDoesNotContain(adapterText, "System.IO.Directory.Exists(path)");
        AssertContains(controllerText, "case nameof(MainViewModel.OutputPath):");

        return Task.CompletedTask;
    }


    internal static Task MainViewModelOutputPathSelection_LivesInFocusedPartial()
    {
        var mainViewModelFiles = ReadMainViewModelCodeFiles();
        var mainViewModelText = string.Join("\n", mainViewModelFiles.Values);
        var outputPathSelectionPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "ViewModels",
            "MainViewModel.OutputPathSelection.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertEqual(false, File.Exists(outputPathSelectionPath), "MainViewModel output picker partial retired");
        AssertDoesNotContain(mainViewModelText, "BrowseOutputPathAsync");
        AssertDoesNotContain(mainViewModelText, "FolderPicker");
        AssertDoesNotContain(mainViewModelText, "FileTypeFilter");
        AssertContains(agentMapText, "`Sussudio/Controllers/Recording/RecordingControlsControllers.cs` owns recording output-");
        AssertContains(agentMapText, "`MainViewModel.cs` owns the stable recording facade:");
        AssertContains(agentMapText, "bridge, recording option selections, output path, counters, and observable");
        AssertContains(cleanupPlanText, "Recording output-path textbox, tooltip, resize-event updates, browse, and");
        AssertDoesNotContain(agentMapText, "`MainViewModel.OutputPathSelection.cs` owns output folder picker and path assignment.");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.OutputPathSelection.cs`");

        return Task.CompletedTask;
    }

    internal static Task OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty()
    {
        var builderType = RequireType("Sussudio.ViewModels.OutputDriveSpacePresentationBuilder");
        var buildMethod = builderType.GetMethod(
            "Build",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OutputDriveSpacePresentationBuilder.Build was not found.");

        AssertEqual(
            "",
            buildMethod.Invoke(null, new object?[] { "\0" }),
            "Output drive space invalid path fallback");

        return Task.CompletedTask;
    }

    internal static Task OutputDriveSpacePresentationBuilder_LivesInFocusedHelper()
    {
        var bridgeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(bridgeText, "private void UpdateDiskSpace()");
        AssertContains(bridgeText, "DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);");
        AssertDoesNotContain(bridgeText, "new DriveInfo(");
        AssertDoesNotContain(bridgeText, "Path.GetPathRoot(");
        AssertDoesNotContain(bridgeText, "Trace.TraceWarning(");
        AssertDoesNotContain(bridgeText, "Free: {freeGb:F1} GB");

        AssertContains(builderText, "internal static class OutputDriveSpacePresentationBuilder");
        AssertContains(builderText, "internal static string Build(string outputPath)");
        AssertContains(builderText, "new DriveInfo(Path.GetPathRoot(outputPath) ?? \"C:\");");
        AssertContains(builderText, "return $\"Free: {freeGb:F1} GB\";");
        AssertContains(builderText, "Trace.TraceWarning($\"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}\");");
        AssertContains(builderText, "return \"\";");
        AssertDoesNotContain(builderText, "DiskSpaceInfo =");

        AssertContains(agentMapText, "`MainViewModel.cs` owns recording-runtime counters and the DiskSpaceInfo assignment bridge");
        AssertContains(agentMapText, "`Sussudio/ViewModels/ViewModelBuilders.cs` owns output drive probing");
        AssertContains(cleanupPlanText, "`ViewModelBuilders.cs`");

        return Task.CompletedTask;
    }

internal static Task PreviewScreenshotButtonWorkflow_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/ScreenshotControllers.cs").Replace("\r\n", "\n");
        const string policyMarker = "internal static class PreviewScreenshotPlanPolicy";
        var policyStart = controllerText.IndexOf(policyMarker, StringComparison.Ordinal);
        if (policyStart < 0)
        {
            throw new InvalidOperationException("PreviewScreenshotPlanPolicy was not found in ScreenshotControllers.cs.");
        }

        var policyEnd = controllerText.IndexOf("internal sealed class WindowScreenshotController", policyStart, StringComparison.Ordinal);
        if (policyEnd < 0)
        {
            throw new InvalidOperationException("WindowScreenshotController was not found after PreviewScreenshotPlanPolicy.");
        }

        var policyText = controllerText[policyStart..policyEnd];
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewScreenshotController _previewScreenshotController = null!;");
        AssertContains(adapterText, "private void InitializePreviewScreenshotController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "ScreenshotButton = ScreenshotButton,");
        AssertContains(adapterText, "private Task CapturePreviewScreenshotAsync()");
        AssertContains(adapterText, "=> _previewScreenshotController.CaptureAsync();");
        AssertContains(adapterText, "private void ScreenshotButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));");
        AssertContains(mainWindowText, "InitializePreviewScreenshotController();");
        AssertContains(controllerText, "internal sealed class PreviewScreenshotController");
        AssertContains(controllerText, "public async Task CaptureAsync()");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.PreviewRequiredStatusText");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.Create(");
        AssertContains(controllerText, "Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)");
        AssertContains(controllerText, "DateTime.Now");
        AssertContains(controllerText, "Directory.CreateDirectory(plan.OutputDirectory);");
        AssertContains(controllerText, "CapturePreviewFrameAsync(plan.FilePath)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatSavedStatus(plan.FilePath)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatSavedLog(plan.FilePath, result.CapturedWidth, result.CapturedHeight)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatFailedStatus(result.Message)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatFailedLog(result.Message)");
        AssertContains(policyText, "internal static class PreviewScreenshotPlanPolicy");
        AssertContains(policyText, "PreviewRequiredStatusText = \"Start preview before capturing a screenshot\"");
        AssertContains(policyText, "Path.Combine(picturesFolder, DefaultOutputFolderName)");
        AssertContains(policyText, "$\"Screenshot_{timestamp.ToString(TimestampFormat)}.png\"");
        AssertContains(policyText, "=> $\"Screenshot saved: {Path.GetFileName(filePath)}\";");
        AssertContains(policyText, "=> $\"Screenshot failed: {message}\";");
        AssertContains(policyText, "=> $\"SCREENSHOT_SAVED path={filePath} width={capturedWidth} height={capturedHeight}\";");
        AssertContains(policyText, "=> $\"SCREENSHOT_FAILED reason={message}\";");
        AssertContains(policyText, "internal readonly record struct PreviewScreenshotPlan(string OutputDirectory, string FilePath);");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = false;");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = true;");
        AssertContains(agentMapText, "`Sussudio/Controllers/Screenshot/ScreenshotControllers.cs` owns");
        AssertContains(agentMapText, "pure preview-frame screenshot output-directory fallback");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/Screenshot/ScreenshotControllers.cs` owns");
        AssertContains(cleanupPlanText, "owns the pure output\ndirectory fallback");
        AssertDoesNotContain(adapterText, "Directory.CreateDirectory(outputDir);");
        AssertDoesNotContain(adapterText, "CapturePreviewFrameAsync(");
        AssertDoesNotContain(controllerText, "Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), \"Sussudio\")");
        AssertDoesNotContain(adapterText, "Screenshot saved: {Path.GetFileName(filePath)}");
        AssertDoesNotContain(policyText, "Button");
        AssertDoesNotContain(policyText, "CapturePreviewFrameAsync");
        AssertDoesNotContain(policyText, "Directory.CreateDirectory");
        AssertDoesNotContain(policyText, "Logger.Log");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Screenshot.cs")),
            "preview screenshot button adapter lives with MainWindow button actions");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Screenshot", "Preview", "PreviewScreenshotController.cs")),
            "preview screenshot workflow lives with the screenshot controller owner");

        return Task.CompletedTask;
    }

    internal static Task PreviewScreenshotPlanPolicy_PreservesPathAndTextContracts()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewScreenshotPlanPolicy");
        var create = policyType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create was not found.");
        var savedStatus = policyType.GetMethod("FormatSavedStatus", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatSavedStatus was not found.");
        var failedStatus = policyType.GetMethod("FormatFailedStatus", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatFailedStatus was not found.");
        var savedLog = policyType.GetMethod("FormatSavedLog", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatSavedLog was not found.");
        var failedLog = policyType.GetMethod("FormatFailedLog", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatFailedLog was not found.");
        var previewRequired = policyType.GetField("PreviewRequiredStatusText", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.PreviewRequiredStatusText was not found.");

        var timestamp = new System.DateTime(2026, 5, 16, 14, 3, 4);
        var fallbackPlan = create.Invoke(null, new object?[] { "   ", "C:\\Users\\crest\\Pictures", timestamp })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var configuredPlan = create.Invoke(null, new object?[] { "D:\\Captures", "C:\\Users\\crest\\Pictures", timestamp })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var fallbackPath = GetStringProperty(fallbackPlan, "FilePath");
        var configuredPath = GetStringProperty(configuredPlan, "FilePath");

        AssertEqual(
            "Start preview before capturing a screenshot",
            previewRequired.GetValue(null)?.ToString(),
            "preview screenshot not-previewing status");
        AssertEqual(
            "C:\\Users\\crest\\Pictures\\Sussudio",
            GetStringProperty(fallbackPlan, "OutputDirectory"),
            "preview screenshot fallback output directory");
        AssertEqual(
            "C:\\Users\\crest\\Pictures\\Sussudio\\Screenshot_2026-05-16_14-03-04.png",
            fallbackPath,
            "preview screenshot fallback path");
        AssertEqual(
            "D:\\Captures",
            GetStringProperty(configuredPlan, "OutputDirectory"),
            "preview screenshot configured output directory");
        AssertEqual(
            "D:\\Captures\\Screenshot_2026-05-16_14-03-04.png",
            configuredPath,
            "preview screenshot configured path");
        AssertEqual(
            "Screenshot saved: Screenshot_2026-05-16_14-03-04.png",
            savedStatus.Invoke(null, new object[] { configuredPath })?.ToString(),
            "preview screenshot saved status");
        AssertEqual(
            "SCREENSHOT_SAVED path=D:\\Captures\\Screenshot_2026-05-16_14-03-04.png width=1280 height=720",
            savedLog.Invoke(null, new object[] { configuredPath, 1280, 720 })?.ToString(),
            "preview screenshot saved log");
        AssertEqual(
            "Screenshot failed: renderer unavailable",
            failedStatus.Invoke(null, new object[] { "renderer unavailable" })?.ToString(),
            "preview screenshot failed status");
        AssertEqual(
            "SCREENSHOT_FAILED reason=renderer unavailable",
            failedLog.Invoke(null, new object[] { "renderer unavailable" })?.ToString(),
            "preview screenshot failed log");

        return Task.CompletedTask;
    }

    internal static Task MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation()
    {
        var windowText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/ScreenshotControllers.cs")
            .Replace("\r\n", "\n");
        var method = ExtractTextBetween(
            controllerText,
            "public Task<WindowScreenshotResult> CaptureAsync",
            "    private WindowScreenshotResult CaptureCore");

        AssertContains(windowText, "public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync");
        AssertContains(windowText, "=> _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);");
        AssertContains(method, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(method, "Message = \"Screenshot canceled.\"");
        AssertContains(method, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(method, "cancellationToken.Register(() =>");
        AssertContains(method, "_ = completion.Task.ContinueWith(");
        AssertContains(method, "cancellationRegistration.Dispose()");
        AssertContains(method, "if (!_dispatcherQueue.TryEnqueue(() =>");
        AssertContains(method, "Message = \"Failed to enqueue screenshot capture on the UI thread.\"");
        AssertContains(controllerText, "=> CaptureNative(_windowHandleProvider(), outputPath);");

        return Task.CompletedTask;
    }

    internal static Task WindowScreenshotNativeCapture_LivesWithWindowScreenshotController()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/ScreenshotControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(controllerText, "=> CaptureNative(_windowHandleProvider(), outputPath);");
        AssertContains(controllerText, "private static WindowScreenshotResult CaptureNative(IntPtr hwnd, string outputPath)");
        AssertContains(controllerText, "Message = \"Window handle not available.\"");
        AssertContains(controllerText, "Message = \"PrintWindow failed.\"");
        AssertContains(controllerText, "var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);");
        AssertContains(controllerText, "GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);");
        AssertContains(controllerText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertContains(controllerText, "Message = $\"Window screenshot saved: {width}x{height}\"");
        AssertContains(controllerText, "[DllImport(\"user32.dll\")]");
        AssertContains(controllerText, "private struct BITMAPINFOHEADER");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Screenshot", "Window", "WindowScreenshotNativeCapture.cs")),
            "native whole-window capture stays with WindowScreenshotController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Screenshot", "Window", "WindowScreenshotController.cs")),
            "whole-window screenshot capture lives with the screenshot controller owner");

        return Task.CompletedTask;
    }

    internal static Task WindowScreenshotImageEncoding_LivesInFocusedHelper()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/ScreenshotControllers.cs")
            .Replace("\r\n", "\n");
        var encoderText = controllerText;

        AssertContains(controllerText, "private static void SaveHBitmapAsImage(");
        AssertContains(controllerText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertContains(controllerText, "internal static void WritePngToStream");
        AssertContains(controllerText, "internal static void WriteBmpToStream");
        AssertContains(encoderText, "internal static class WindowScreenshotImageEncoder");
        AssertContains(encoderText, "internal static void WritePngToStream");
        AssertContains(encoderText, "internal static void WriteBmpToStream");
        AssertContains(encoderText, "internal static uint[] InitCrc32Table()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Screenshot", "Window", "WindowScreenshotImageEncoder.cs")),
            "whole-window screenshot image encoder folded into WindowScreenshotController");

        var encoderType = RequireType("Sussudio.Controllers.WindowScreenshotImageEncoder");
        var writePng = encoderType.GetMethod("WritePngToStream", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowScreenshotImageEncoder.WritePngToStream missing.");
        var writeBmp = encoderType.GetMethod("WriteBmpToStream", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowScreenshotImageEncoder.WriteBmpToStream missing.");
        var bgra = new byte[] { 0, 0, 255, 255 };

        using var pngStream = new MemoryStream();
        writePng.Invoke(null, new object[] { pngStream, 1, 1, bgra });
        var pngBytes = pngStream.ToArray();
        AssertSequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, pngBytes.Take(8).ToArray(), "PNG signature");
        AssertEqual((byte)73, pngBytes[12], "PNG IHDR I");
        AssertEqual((byte)72, pngBytes[13], "PNG IHDR H");
        AssertEqual((byte)68, pngBytes[14], "PNG IHDR D");
        AssertEqual((byte)82, pngBytes[15], "PNG IHDR R");

        using var bmpStream = new MemoryStream();
        writeBmp.Invoke(null, new object[] { bmpStream, 1, 1, bgra });
        var bmpBytes = bmpStream.ToArray();
        AssertEqual((byte)0x42, bmpBytes[0], "BMP signature B");
        AssertEqual((byte)0x4D, bmpBytes[1], "BMP signature M");
        AssertEqual(58, bmpBytes.Length, "BMP byte length");
        AssertEqual(1, BitConverter.ToInt32(bmpBytes, 18), "BMP width");
        AssertEqual(-1, BitConverter.ToInt32(bmpBytes, 22), "BMP top-down height");

        return Task.CompletedTask;
    }

    internal static Task SettingsShelfLifecycle_LivesInController()
    {
        var fullScreenText = ReadMainWindowShellChromeAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var settingsShelfText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(settingsShelfText, "private SettingsShelfController _settingsShelfController = null!;");
        AssertContains(settingsShelfText, "private void InitializeSettingsShelfController()");
        AssertContains(settingsShelfText, "=> _settingsShelfController.Toggle();");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ApplyVisibility(visible);");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ShellChrome.Composition.cs")),
            "settings shelf adapter lives in the shell chrome composition partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.FullScreen.Composition.cs")),
            "fullscreen adapter folded into the shell chrome composition partial");
        AssertContains(mainWindowText, "InitializeSettingsShelfController();");
        AssertContains(fullScreenText, "ResetSettingsShelfAnimation = _settingsShelfController.ResetAnimationState,");
        AssertDoesNotContain(settingsShelfText, "ResetSettingsShelfAnimationForFullScreen");
        AssertContains(controllerText, "internal sealed class SettingsShelfController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "SettingsShelfController.cs")),
            "settings shelf lives with shell chrome ownership");
        AssertContains(controllerText, "private bool _isAnimating;");
        AssertContains(controllerText, "public bool IsAnimating => _isAnimating;");
        AssertContains(controllerText, "public void Toggle()");
        AssertContains(controllerText, "public void ApplyVisibility(bool visible)");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName, bool isSettingsVisible)");
        AssertContains(controllerText, "case nameof(MainViewModel.IsSettingsVisible):");
        AssertContains(controllerText, "ApplyVisibility(isSettingsVisible);");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.UpdateLayout();");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;");
        AssertDoesNotContain(mainWindowText, "private bool _isSettingsShelfAnimating;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.EventHandlers.cs")),
            "generic MainWindow event-handler partial removed");

        return Task.CompletedTask;
    }

    internal static Task MainWindowTitlePresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var statusStripText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(statusStripText, "private WindowTitleController _windowTitleController = null!;");
        AssertContains(statusStripText, "private void InitializeWindowTitleController()");
        AssertContains(statusStripText, "private void ApplyWindowTitle()");
        AssertContains(statusStripText, "=> _windowTitleController = new WindowTitleController();");
        AssertContains(statusStripText, "=> Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);");
        AssertContains(controllerText, "internal sealed class WindowTitleController");
        AssertContains(controllerText, "private const string DefaultTitle = \"Simple Sussudio\";");
        AssertContains(controllerText, "public string BuildTitle(bool isRecording, string recordingTime)");
        AssertContains(controllerText, "internal static string BuildWindowTitleBase()");
        AssertContains(controllerText, "Environment.ProcessPath");
        AssertContains(controllerText, "File.GetLastWriteTime(exePath)");
        AssertContains(controllerText, "internal static string FormatBuildTitle(DateTime buildTime)");
        AssertContains(controllerText, "CultureInfo.InvariantCulture");
        AssertContains(controllerText, "internal static string FormatTitle(string baseTitle, bool isRecording, string recordingTime)");
        AssertContains(controllerText, "=> isRecording ? $\"{baseTitle} - REC {recordingTime}\" : baseTitle;");
        AssertContains(mainWindowText, "InitializeWindowTitleController();");
        AssertContains(mainWindowText, "ApplyWindowTitle();");
        AssertContains(propertyChangedText, "TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,");
        AssertContains(statusStripText, "ApplyWindowTitle);");
        AssertDoesNotContain(mainWindowText, "private static string BuildWindowTitleBase()");
        AssertDoesNotContain(mainWindowText, "private void ApplyWindowTitle()");
        AssertDoesNotContain(mainWindowText, "CultureInfo.InvariantCulture");
        AssertDoesNotContain(statusStripText, "Environment.ProcessPath");
        AssertDoesNotContain(statusStripText, "File.GetLastWriteTime(");
        AssertDoesNotContain(statusStripText, "CultureInfo.InvariantCulture");

        return Task.CompletedTask;
    }

    internal static Task WindowTitleController_FormatsBuildStampAndRecordingSuffix()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var controllerType = RequireType("Sussudio.Controllers.WindowTitleController");
            var formatBuildTitle = controllerType.GetMethod("FormatBuildTitle", BindingFlags.Static | BindingFlags.NonPublic)
                                   ?? throw new InvalidOperationException("WindowTitleController.FormatBuildTitle not found.");
            var formatTitle = controllerType.GetMethod("FormatTitle", BindingFlags.Static | BindingFlags.NonPublic)
                              ?? throw new InvalidOperationException("WindowTitleController.FormatTitle not found.");

            var buildTime = new DateTime(2026, 5, 14, 22, 30, 45, DateTimeKind.Local);
            var buildTitle = formatBuildTitle.Invoke(null, new object?[] { buildTime });
            AssertEqual("Simple Sussudio (build 2026-05-14 22:30:45)", buildTitle, "invariant build title");
            AssertEqual("Simple Sussudio", formatBuildTitle.Invoke(null, new object?[] { DateTime.MinValue }), "missing build-time title");

            AssertEqual("Simple Sussudio", formatTitle.Invoke(null, new object?[] { "Simple Sussudio", false, "00:01:02" }), "idle title");
            AssertEqual(
                "Simple Sussudio - REC 00:01:02",
                formatTitle.Invoke(null, new object?[] { "Simple Sussudio", true, "00:01:02" }),
                "recording title");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        return Task.CompletedTask;
    }

    internal static Task LiveSignalInfoPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var liveSignalAdapterText = ReadMainWindowShellChromeAdapterSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(liveSignalAdapterText, "private LiveSignalInfoController _liveSignalInfoController = null!;");
        AssertContains(liveSignalAdapterText, "private void InitializeLiveSignalInfoController()");
        AssertContains(liveSignalAdapterText, "LiveResolutionTextBlock = LiveResolutionTextBlock,");
        AssertContains(liveSignalAdapterText, "LiveFrameRateTextBlock = LiveFrameRateTextBlock,");
        AssertContains(liveSignalAdapterText, "LivePixelFormatTextBlock = LivePixelFormatTextBlock,");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.Update(");
        AssertContains(liveSignalAdapterText, "ViewModel.LiveResolution,");
        AssertContains(liveSignalAdapterText, "private void StopLiveSignalInfoTimers()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.StopTimers();");
        AssertContains(liveSignalAdapterText, "private bool TryHandleLiveSignalPropertyChanged(string propertyName)");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.TryHandlePropertyChanged(");
        AssertDoesNotContain(liveSignalAdapterText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(mainWindowText, "InitializeLiveSignalInfoController();");
        AssertContains(bindingsText, "UpdateLiveSignalInfoVisibility();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(controllerText, "internal sealed class LiveSignalInfoController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _showDebounceTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _hideDebounceTimer;");
        AssertContains(controllerText, "public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName, string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(controllerText, "case nameof(MainViewModel.LiveFrameRate):");
        AssertContains(controllerText, "case nameof(MainViewModel.LivePixelFormat):");
        AssertContains(controllerText, "Update(liveResolution, liveFrameRate, livePixelFormat);");
        AssertContains(controllerText, "_context.LiveResolutionTextBlock.Text = liveResolution;");
        AssertContains(controllerText, "_context.LiveFrameRateTextBlock.Text = liveFrameRate;");
        AssertContains(controllerText, "_context.LivePixelFormatTextBlock.Text = livePixelFormat;");
        AssertContains(controllerText, "private bool HasCompleteLiveSignal()");
        AssertContains(controllerText, "private void AnimateIn()");
        AssertContains(controllerText, "private void AnimateOut()");
        AssertDoesNotContain(propertyChangedText, "LiveResolutionTextBlock.Text = ViewModel.LiveResolution;");
        AssertDoesNotContain(propertyChangedText, "LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;");
        AssertDoesNotContain(propertyChangedText, "LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;");
        AssertDoesNotContain(mainWindowText, "private bool _liveSignalInfoVisible;");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _liveSignalDebounceTimer;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "LiveSignalInfoController.cs")),
            "live signal info presentation lives with shell chrome");

        return Task.CompletedTask;
    }

    internal static Task StatusStripPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private StatusStripPresentationController _statusStripPresentationController = null!;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.StatusStripPresentation.cs")),
            "status strip adapter lives with the shell chrome composition partial");
        AssertContains(adapterText, "private void InitializeStatusStripPresentationController()");
        AssertContains(adapterText, "DiskWarningInfoBar = DiskWarningInfoBar,");
        AssertContains(adapterText, "StatusTextBlock = StatusTextBlock,");
        AssertContains(adapterText, "RecordingTimeTextBlock = RecordingTimeTextBlock,");
        AssertContains(adapterText, "DiskSpaceTextBlock = DiskSpaceTextBlock,");
        AssertContains(adapterText, "RecordingSizeTextBlock = RecordingSizeTextBlock,");
        AssertContains(adapterText, "RecordingBitrateTextBlock = RecordingBitrateTextBlock,");
        AssertContains(adapterText, "private void ApplyInitialStatusStripPresentation()");
        AssertContains(adapterText, "private StatusStripPresentationSnapshot BuildStatusStripPresentationSnapshot()");
        AssertContains(adapterText, "private void UpdateStatusTextPresentation()");
        AssertContains(adapterText, "private void UpdateRecordingTimePresentation()");
        AssertContains(adapterText, "private void UpdateDiskSpacePresentation()");
        AssertContains(adapterText, "private void UpdateRecordingSizePresentation()");
        AssertContains(adapterText, "private void UpdateRecordingBitratePresentation()");
        AssertDoesNotContain(adapterText, "private void UpdateFlashbackBitratePresentation()");
        AssertContains(adapterText, "private void UpdateDiskWarningPresentation()");
        AssertContains(adapterText, "private bool TryHandleStatusStripPropertyChanged(string? propertyName)");
        AssertContains(adapterText, "_statusStripPresentationController.TryHandlePropertyChanged(");
        AssertContains(adapterText, "BuildStatusStripPresentationSnapshot(),");
        AssertContains(adapterText, "ApplyWindowTitle);");
        AssertContains(mainWindowText, "InitializeStatusStripPresentationController();");
        AssertContains(bindingsText, "ApplyInitialStatusStripPresentation();");
        AssertContains(propertyChangedText, "TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,");
        AssertDoesNotContain(flashbackPropertyChangedText, "UpdateBitrate = UpdateFlashbackBitratePresentation,");
        AssertDoesNotContain(flashbackPropertyChangedControllerText, "_context.UpdateBitrate();");
        AssertDoesNotContain(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBitrateInfo):");
        AssertContains(controllerText, "internal readonly record struct StatusStripPresentationSnapshot");
        AssertContains(controllerText, "internal sealed class StatusStripPresentationController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "StatusStripPresentationController.cs")),
            "status strip presentation lives with shell chrome ownership");
        AssertContains(controllerText, "public void ApplyInitial(StatusStripPresentationSnapshot snapshot)");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(");
        AssertContains(controllerText, "case nameof(MainViewModel.StatusText):");
        AssertContains(controllerText, "case nameof(MainViewModel.RecordingTime):");
        AssertContains(controllerText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertContains(controllerText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertContains(controllerText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertContains(controllerText, "case nameof(MainViewModel.FlashbackBitrateInfo):");
        AssertContains(controllerText, "UpdateFlashbackBitrate(snapshot.FlashbackBitrateInfo, snapshot.IsRecording, snapshot.IsFlashbackEnabled);");
        AssertContains(controllerText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertContains(controllerText, "if (snapshot.IsRecording)");
        AssertContains(controllerText, "applyWindowTitle();");
        AssertContains(controllerText, "_context.StatusTextBlock.Text = statusText;");
        AssertContains(controllerText, "_context.RecordingTimeTextBlock.Text = recordingTime;");
        AssertContains(controllerText, "_context.DiskSpaceTextBlock.Text = diskSpaceInfo;");
        AssertContains(controllerText, "_context.RecordingSizeTextBlock.Text = recordingSizeInfo;");
        AssertContains(controllerText, "_context.RecordingBitrateTextBlock.Text = recordingBitrateInfo;");
        AssertContains(controllerText, "if (!isRecording && isFlashbackEnabled)");
        AssertContains(controllerText, "_context.RecordingBitrateTextBlock.Text = flashbackBitrateInfo;");
        AssertContains(controllerText, "_context.DiskWarningInfoBar.IsOpen = isDiskWarningActive;");
        AssertDoesNotContain(bindingsText, "DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;");
        AssertDoesNotContain(bindingsText, "RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;");
        AssertDoesNotContain(bindingsText, "RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;");
        AssertDoesNotContain(bindingsText, "LiveResolutionTextBlock.Text = ViewModel.LiveResolution;");
        AssertDoesNotContain(bindingsText, "LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;");
        AssertDoesNotContain(bindingsText, "LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;");
        AssertDoesNotContain(propertyChangedText, "StatusTextBlock.Text = ViewModel.StatusText;");
        AssertDoesNotContain(propertyChangedText, "RecordingTimeTextBlock.Text = ViewModel.RecordingTime;");
        AssertDoesNotContain(propertyChangedText, "DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;");
        AssertDoesNotContain(propertyChangedText, "RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;");
        AssertDoesNotContain(propertyChangedText, "RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;");
        AssertDoesNotContain(propertyChangedText, "DiskWarningInfoBar.IsOpen = ViewModel.IsDiskWarningActive;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.StatusText):");
        AssertDoesNotContain(propertyChangedText, "UpdateStatusTextPresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingTime):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingTimePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateDiskSpacePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingSizePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingBitratePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertDoesNotContain(propertyChangedText, "UpdateDiskWarningPresentation();");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.StatusText):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.RecordingTime):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertDoesNotContain(adapterText, "if (ViewModel.IsRecording)");
        AssertDoesNotContain(flashbackPropertyChangedText, "RecordingBitrateTextBlock.Text = ViewModel.FlashbackBitrateInfo;");

        return Task.CompletedTask;
    }

    internal static Task RecordingButtonAction_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingControlsControllers.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordingButtonActionController _recordingButtonActionController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingButtonActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "GetPreviewActivitySnapshot = () => new RecordingPreviewActivitySnapshot(");
        AssertContains(adapterText, "_previewRendererHostController.HasD3DRenderer && PreviewSwapChainPanel.Visibility == Visibility.Visible");
        AssertContains(adapterText, "_previewRendererHostController.IsCpuPreviewSourceAttached && PreviewImage.Visibility == Visibility.Visible");
        AssertContains(adapterText, "NoDevicePlaceholder.Visibility == Visibility.Visible");
        AssertContains(adapterText, "private Task ToggleRecordingFromButtonAsync()");
        AssertContains(adapterText, "=> _recordingButtonActionController.ToggleRecordingAsync();");
        AssertContains(adapterText, "private void RecordButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));");
        AssertContains(mainWindowText, "InitializeRecordingButtonActionController();");
        AssertContains(controllerText, "internal readonly record struct RecordingPreviewActivitySnapshot");
        AssertContains(controllerText, "public bool RendererActive => GpuActive || CpuActive;");
        AssertContains(controllerText, "public async Task ToggleRecordingAsync()");
        AssertContains(controllerText, "await _context.ViewModel.ToggleRecordingAsync();");
        AssertContains(controllerText, "if (!_context.ViewModel.IsRecording)");
        AssertContains(controllerText, "PreviewStateDuringRecording: rendererActive={snapshot.RendererActive}");
        AssertContains(controllerText, "WARNING: preview renderer appears inactive while recording.");
        AssertDoesNotContain(adapterText, "ViewModel.ToggleRecordingAsync();");
        AssertDoesNotContain(adapterText, "PreviewStateDuringRecording");
        AssertDoesNotContain(adapterText, "WARNING: preview renderer appears inactive while recording.");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.RecordingActions.cs")),
            "recording button adapter folded into MainWindow.xaml.cs");

        return Task.CompletedTask;
    }

    internal static Task PreviewAudioFadeState_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioControlBindingFamilyText = audioControlBindingControllerText;
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");

        AssertContains(audioBindingsText, "private AudioControlBindingController _audioControlBindingController = null!;");
        AssertContains(audioBindingsText, "private void InitializeAudioControlBindingController()");
        AssertContains(audioBindingsText, "PreviewVolumeSlider = PreviewVolumeSlider,");
        AssertContains(audioBindingsText, "IsPreviewAudioFadeInActive = () => IsPreviewAudioFadeInActive,");
        AssertContains(audioBindingsText, "CancelPreviewAudioFadeInForUser = CancelPreviewAudioFadeInForUser,");
        AssertContains(adapterText, "private PreviewAudioFadeController _previewAudioFadeController = null!;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;");
        AssertContains(adapterText, "private void InitializePreviewAudioFadeController()");
        AssertContains(adapterText, "=> _previewAudioFadeController.PrimeFadeIn();");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeIn(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeOutAsync(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.CancelFadeInForUser();");
        AssertContains(mainWindowText, "InitializePreviewAudioFadeController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewLifecycle.Composition.cs")),
            "preview audio fade adapter lives in the preview transitions composition partial");
        AssertContains(mainWindowText, "InitializeAudioControlBindingController();");
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioControlBindingControllerText, "internal sealed class AudioControlBindingControllerContext");
        AssertContains(audioControlBindingControllerText, "internal sealed class AudioControlBindingController");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioMeterActivationBindings()");
        AssertContains(audioControlBindingControllerText, "public void ApplyInitialAudioControlBindings()");
        AssertContains(audioControlBindingControllerText, "_context.IsPreviewAudioFadeInActive() || _context.IsPreviewAudioFadeAnimationActive()");
        AssertContains(audioControlBindingControllerText, "_context.PreviewVolumeSlider.ValueChanged +=");
        AssertContains(audioControlBindingControllerText, "_context.CancelPreviewAudioFadeInForUser();");
        AssertContains(audioControlBindingControllerText, "public void ApplyInitialAudioMeterPresentation()");
        AssertContains(audioControlBindingControllerText, "public void EnsureAudioControlSelections()");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioSelectionBindings()");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioRecordPreviewToggleBindings()");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioInputToggleBindings()");
        AssertContains(audioControlBindingControllerText, "public void AttachDeviceAudioGainAndMeterBindings()");
        AssertContains(audioControlBindingFamilyText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(propertyChangedText, "TryHandleAudio = TryHandleAudioPropertyChanged,");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.PreviewVolume):");
        AssertContains(audioControlPresentationControllerText, "HandlePreviewVolumeChanged();");
        AssertContains(audioControlPresentationControllerText, "if (_context.IsPreviewAudioFadeInActive())");
        AssertContains(previewLifecycleControllerText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(controllerText, "internal sealed class PreviewAudioFadeController");
        AssertContains(controllerText, "private double _savedPreviewVolume;");
        AssertContains(controllerText, "private Storyboard? _volumeFadeStoryboard;");
        AssertContains(controllerText, "public void PrimeFadeIn()");
        AssertContains(controllerText, "public async Task StartFadeOutAsync(int durationMs = 450)");
        AssertContains(controllerText, "Sussudio.Logger.Log(\"PREVIEW_AUDIO_FADE_OUT_COMPLETED\");");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewAudioFadeController.cs")),
            "preview audio fade folded into PreviewLifecycleEventController.cs");
        AssertDoesNotContain(mainWindowText, "private double _savedPreviewVolume;");
        AssertDoesNotContain(mainWindowText, "private bool _isVolumeFadingIn;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _previewVolumeFadeStoryboard;");
        AssertDoesNotContain(bindingsText, "PreviewVolumeSlider.ValueChanged +=");
        AssertDoesNotContain(audioBindingsText, "PreviewVolumeSlider.ValueChanged +=");

        return Task.CompletedTask;
    }

    internal static Task PreviewButtonPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewActionsText = ReadMainWindowPreviewTransitionsAdapterSource();
        var propertyChangedPreviewText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var actionControllerText = previewLifecycleControllerText;
        var controllerText = actionControllerText;
        var reinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(propertyChangedPreviewText, "private PreviewButtonPresentationController _previewButtonPresentationController = null!;");
        AssertContains(propertyChangedPreviewText, "private void InitializePreviewButtonPresentationController()");
        AssertContains(propertyChangedPreviewText, "PreviewButton = PreviewButton,");
        AssertContains(propertyChangedPreviewText, "PreviewButtonIcon = PreviewButtonIcon,");
        AssertContains(propertyChangedPreviewText, "private void ShowStopPreviewButtonPresentation()");
        AssertContains(propertyChangedPreviewText, "=> _previewButtonPresentationController.ShowStopPreview();");
        AssertContains(propertyChangedPreviewText, "private void ShowStartPreviewButtonPresentation()");
        AssertContains(propertyChangedPreviewText, "=> _previewButtonPresentationController.ShowStartPreview();");
        AssertContains(mainWindowText, "InitializePreviewButtonPresentationController();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStopPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStartPreviewButtonPresentation();");
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,");
        AssertContains(reinitTransitionControllerText, "context.ShowStartPreviewButtonPresentation();");
        AssertContains(controllerText, "internal sealed class PreviewButtonPresentationController");
        AssertContains(controllerText, "private const string StopPreviewGlyph = \"\\uE71A\";");
        AssertContains(controllerText, "private const string StartPreviewGlyph = \"\\uE768\";");
        AssertContains(controllerText, "_context.PreviewButtonIcon.Glyph = StopPreviewGlyph;");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.PreviewButton, \"Stop Preview\");");
        AssertContains(controllerText, "_context.PreviewButtonIcon.Glyph = StartPreviewGlyph;");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.PreviewButton, \"Start Preview\");");
        AssertContains(previewActionsText, "private PreviewButtonActionController _previewButtonActionController = null!;");
        AssertContains(previewActionsText, "private void InitializePreviewButtonActionController()");
        AssertContains(previewActionsText, "private Task TogglePreviewFromButtonAsync()");
        AssertContains(previewActionsText, "=> _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));");
        AssertContains(previewActionsText, "_ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));");
        AssertContains(mainWindowText, "InitializePreviewButtonActionController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewLifecycle.Composition.cs")),
            "preview button action adapter lives in the preview transitions composition partial");
        AssertContains(actionControllerText, "internal sealed class PreviewButtonActionController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewButtonActionController.cs")),
            "preview button action, presentation, and fade-in controllers live with preview lifecycle events");
        AssertContains(actionControllerText, "public async Task TogglePreviewAsync(string operationName)");
        AssertContains(actionControllerText, "viewModel.CancelPendingPreviewRestart();");
        AssertContains(actionControllerText, "Logger.Log($\"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_context.GetPreviewStartupAttemptId() ?? \"none\"}\", operationName);");
        AssertContains(previewActionsText, "_previewReinitTransitionController.Clear(operationName, operationName: operationName);");
        AssertContains(reinitTransitionControllerText, "Logger.Log(message, operationName);");
        AssertContains(actionControllerText, "var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();");
        AssertContains(actionControllerText, "var previewFadeOutTask = _context.AnimatePreviewOutAsync();");
        AssertContains(actionControllerText, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertContains(actionControllerText, "await viewModel.StopPreviewAsync(userInitiated: true);");
        AssertContains(actionControllerText, "_context.ClearPreviewReinitAnimation(operationName);");
        AssertContains(actionControllerText, "await viewModel.StartPreviewAsync(userInitiated: true);");
        AssertDoesNotContain(previewActionsText, "var audioFadeOutTask = StartPreviewAudioFadeOutAsync();");
        AssertDoesNotContain(previewActionsText, "await ViewModel.StopPreviewAsync(userInitiated: true);");
        AssertDoesNotContain(propertyChangedPreviewText, "PreviewButtonIcon.Glyph = \"\\uE71A\";");
        AssertDoesNotContain(propertyChangedPreviewText, "PreviewButtonIcon.Glyph = \"\\uE768\";");
        AssertDoesNotContain(propertyChangedPreviewText, "ToolTipService.SetToolTip(PreviewButton, \"Stop Preview\");");
        AssertDoesNotContain(propertyChangedPreviewText, "ToolTipService.SetToolTip(PreviewButton, \"Start Preview\");");
        AssertDoesNotContain(previewReinitText, "PreviewButtonIcon.Glyph = \"\\uE71A\";");
        AssertDoesNotContain(previewReinitText, "PreviewButtonIcon.Glyph = \"\\uE768\";");
        AssertDoesNotContain(previewReinitText, "ToolTipService.SetToolTip(PreviewButton, \"Stop Preview\");");
        AssertDoesNotContain(previewReinitText, "ToolTipService.SetToolTip(PreviewButton, \"Start Preview\");");

        return Task.CompletedTask;
    }

    internal static Task AudioControlPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");

        AssertContains(audioPropertyChangedText, "private AudioControlPresentationController _audioControlPresentationController = null!;");
        AssertContains(audioPropertyChangedText, "private void InitializeAudioControlPresentationController()");
        AssertContains(audioPropertyChangedText, "CustomAudioToggle = CustomAudioToggle,");
        AssertContains(audioPropertyChangedText, "AudioInputComboBox = AudioInputComboBox,");
        AssertContains(audioPropertyChangedText, "MicrophoneToggle = MicrophoneToggle,");
        AssertContains(audioPropertyChangedText, "MicrophoneComboBox = MicrophoneComboBox,");
        AssertContains(audioPropertyChangedText, "AudioRecordToggle = AudioRecordToggle,");
        AssertContains(audioPropertyChangedText, "AudioPreviewToggle = AudioPreviewToggle,");
        AssertContains(audioPropertyChangedText, "PreviewVolumeSlider = PreviewVolumeSlider,");
        AssertContains(audioPropertyChangedText, "PreviewVolumeLabel = PreviewVolumeLabel,");
        AssertContains(audioPropertyChangedText, "IsPreviewAudioFadeInActive = () => IsPreviewAudioFadeInActive,");
        AssertContains(audioPropertyChangedText, "SetAudioMeterMonitoringState = SetAudioMeterMonitoringState,");
        AssertContains(audioPropertyChangedText, "AnimateAudioMeterDisabled = AnimateAudioMeterDisabled,");
        AssertContains(audioPropertyChangedText, "UpdateMicrophoneControlsVisibility = UpdateMicrophoneControlsVisibility,");
        AssertContains(audioPropertyChangedText, "SyncMicrophoneVolumeControls = SyncMicrophoneVolumeControls");
        AssertContains(mainWindowText, "InitializeAudioControlPresentationController();");

        AssertContains(controllerText, "internal sealed class AudioControlPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class AudioControlPresentationController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Audio", "AudioControlPresentationController.cs")),
            "audio control presentation lives with audio control binding ownership");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerText, "case nameof(MainViewModel.IsCustomAudioInputEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsMicrophoneEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsAudioEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsAudioPreviewEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(controllerText, "case nameof(MainViewModel.PreviewVolume):");
        AssertContains(controllerText, "case nameof(MainViewModel.MicrophoneVolume):");
        AssertContains(controllerText, "public void HandleCustomAudioInputEnabledChanged()");
        AssertContains(controllerText, "_context.AudioInputComboBox.IsEnabled = _context.ViewModel.IsCustomAudioInputEnabled && !_context.ViewModel.IsRecording;");
        AssertContains(controllerText, "public void HandleMicrophoneEnabledChanged()");
        AssertContains(controllerText, "_context.MicrophoneComboBox.IsEnabled = _context.ViewModel.IsMicrophoneEnabled && !_context.ViewModel.IsRecording;");
        AssertContains(controllerText, "_context.UpdateMicrophoneControlsVisibility();");
        AssertContains(controllerText, "public void HandleAudioEnabledChanged()");
        AssertContains(controllerText, "_context.AudioPreviewToggle.IsEnabled = _context.ViewModel.IsAudioEnabled;");
        AssertContains(controllerText, "_context.AudioPreviewToggle.IsChecked = false;");
        AssertContains(controllerText, "_context.AnimateAudioMeterDisabled(!_context.ViewModel.IsAudioEnabled);");
        AssertContains(controllerText, "public void HandleAudioPreviewActiveChanged()");
        AssertContains(controllerText, "_context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);");
        AssertContains(controllerText, "public void HandlePreviewVolumeChanged()");
        AssertContains(controllerText, "if (_context.IsPreviewAudioFadeInActive())");
        AssertContains(controllerText, "_context.PreviewVolumeLabel.Text = $\"{(int)volumePct}%\";");
        AssertContains(controllerText, "public void HandleMicrophoneVolumeChanged()");
        AssertContains(controllerText, "_context.SyncMicrophoneVolumeControls(_context.ViewModel.MicrophoneVolume);");

        AssertDoesNotContain(audioPropertyChangedText, "AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled");
        AssertDoesNotContain(audioPropertyChangedText, "AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled");
        AssertDoesNotContain(audioPropertyChangedText, "PreviewVolumeLabel.Text = $\"{(int)volumePct}%\";");
        AssertDoesNotContain(audioPropertyChangedText, "case nameof(MainViewModel.");
        AssertDoesNotContain(audioPropertyChangedText, "=> _audioControlPresentationController.HandlePreviewVolumeChanged();");

        return Task.CompletedTask;
    }

    internal static Task MicrophoneControls_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var setupBindingsText = ExtractMemberCode(bindingsText, "SetupBindings");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private MicrophoneControlsController _microphoneControlsController = null!;");
        AssertContains(adapterText, "private void InitializeMicrophoneControlsController()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.MicrophoneControls.cs")),
            "microphone controls adapter folded into MainWindow.xaml.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Audio", "MicrophoneControlsController.cs")),
            "microphone controls folded into AudioControlBindingController.cs");
        AssertContains(adapterText, "=> _microphoneControlsController.AttachVolumeBindings();");
        AssertContains(adapterText, "=> _microphoneControlsController.SyncVolumeControls(volumePercent);");
        AssertContains(adapterText, "=> _microphoneControlsController.ApplyInitialVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.UpdateVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.StopRowAnimation();");
        AssertContains(mainWindowText, "InitializeMicrophoneControlsController();");
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioBindingsText, "SetupMicrophoneVolumeBindings = SetupMicrophoneVolumeBindings,");
        AssertContains(audioBindingsText, "ApplyInitialMicrophoneControlsVisibility = ApplyInitialMicrophoneControlsVisibility,");
        AssertContains(audioControlBindingControllerText, "_context.SetupMicrophoneVolumeBindings();");
        AssertContains(audioControlBindingControllerText, "_context.ApplyInitialMicrophoneControlsVisibility();");
        AssertContains(propertyChangedText, "TryHandleAudio = TryHandleAudioPropertyChanged,");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.IsMicrophoneEnabled):");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.MicrophoneVolume):");
        AssertContains(audioControlPresentationControllerText, "HandleMicrophoneEnabledChanged();");
        AssertContains(audioControlPresentationControllerText, "HandleMicrophoneVolumeChanged();");
        AssertContains(audioControlPresentationControllerText, "_context.UpdateMicrophoneControlsVisibility();");
        AssertContains(audioControlPresentationControllerText, "_context.SyncMicrophoneVolumeControls(_context.ViewModel.MicrophoneVolume);");
        AssertContains(shutdownCleanupControllerText, "_context.StopRecordingVisuals();");
        AssertContains(controllerText, "internal sealed class MicrophoneControlsController");
        AssertContains(controllerText, "private bool _syncingVolumeControls;");
        AssertContains(controllerText, "private Storyboard? _activeRowStoryboard;");
        AssertContains(controllerText, "public void AttachVolumeBindings()");
        AssertContains(controllerText, "public void SyncVolumeControls(double volumePercent)");
        AssertContains(controllerText, "public void ApplyInitialVisibility()");
        AssertContains(controllerText, "public void UpdateVisibility()");
        AssertContains(controllerText, "public void StopRowAnimation()");
        AssertContains(controllerText, "private Storyboard CreateRowStoryboard(bool showing)");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _micMeterRowStoryboard;");
        AssertDoesNotContain(mainWindowText, "private bool _syncingMicrophoneVolumeControls;");
        AssertDoesNotContain(mainWindowText, "private const double MicMeterRowHeight = 14;");
        AssertDoesNotContain(setupBindingsText, "MicVolumeSlider.ValueChanged +=");
        AssertDoesNotContain(setupBindingsText, "SetupMicrophoneVolumeBindings();");
        AssertDoesNotContain(setupBindingsText, "private Storyboard CreateMicMeterRowStoryboard(bool showing)");

        return Task.CompletedTask;
    }
}

namespace Sussudio.Tests
{
    public sealed class WindowSnapRegionLayoutPolicyTests
    {
        private const string PolicyTypeName = "Sussudio.Controllers.WindowSnapRegionLayoutPolicy";
        private const string ActionTypeName = "Sussudio.Models.AutomationWindowAction";

        [Theory]
        [InlineData("SnapLeft", 10, 20, 50, 55)]
        [InlineData("SnapRight", 60, 20, 51, 55)]
        [InlineData("SnapTopLeft", 10, 20, 50, 27)]
        [InlineData("SnapTopRight", 60, 20, 51, 27)]
        [InlineData("SnapBottomLeft", 10, 47, 50, 28)]
        [InlineData("SnapBottomRight", 60, 47, 51, 28)]
        [InlineData("Center", 44, 40, 33, 15)]
        public void ResolveTargetBounds_PreservesExistingSnapGeometry(string actionName, int x, int y, int width, int height)
        {
            var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
            var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
            var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
            var parameterTypes = method.GetParameters();
            var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
            var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
            var action = Enum.Parse(actionType, actionName);

            var result = method.Invoke(null, new[] { action, workArea, currentSize });

            Assert.NotNull(result);
            AssertRect(result!, x, y, width, height);
        }

        [Theory]
        [InlineData("Restore")]
        [InlineData(null)]
        public void ResolveTargetBounds_ReturnsNullForNonSnapActions(string? actionName)
        {
            var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
            var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
            var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
            var parameterTypes = method.GetParameters();
            var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
            var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
            var action = actionName is null
                ? Enum.ToObject(actionType, 999)
                : Enum.Parse(actionType, actionName);

            var result = method.Invoke(null, new[] { action, workArea, currentSize });

            Assert.Null(result);
        }

        private static object CreateStruct(Type type, params int[] args)
            => Activator.CreateInstance(type, args.Cast<object>().ToArray())!;

        private static void AssertRect(object rect, int x, int y, int width, int height)
        {
            Assert.Equal(x, ReadIntProperty(rect, "X"));
            Assert.Equal(y, ReadIntProperty(rect, "Y"));
            Assert.Equal(width, ReadIntProperty(rect, "Width"));
            Assert.Equal(height, ReadIntProperty(rect, "Height"));
        }

        private static int ReadIntProperty(object instance, string propertyName)
        {
            var type = instance.GetType();
            var property = type.GetProperty(propertyName, ReflectionFlags.Instance);
            if (property != null)
            {
                return (int)property.GetValue(instance)!;
            }

            return (int)type.GetField(propertyName, ReflectionFlags.Instance)!.GetValue(instance)!;
        }
    }
}

using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task SettingsShelfLifecycle_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var fullScreenText = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var settingsShelfText = ReadRepoFile("Sussudio/MainWindow.SettingsShelf.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/SettingsShelfController.cs").Replace("\r\n", "\n");

        AssertContains(settingsShelfText, "private SettingsShelfController _settingsShelfController = null!;");
        AssertContains(settingsShelfText, "private void InitializeSettingsShelfController()");
        AssertContains(settingsShelfText, "=> _settingsShelfController.Toggle();");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ApplyVisibility(visible);");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ResetAnimationState();");
        AssertContains(mainWindowText, "InitializeSettingsShelfController();");
        AssertContains(fullScreenText, "ResetSettingsShelfAnimation = ResetSettingsShelfAnimationForFullScreen,");
        AssertContains(controllerText, "internal sealed class SettingsShelfController");
        AssertContains(controllerText, "private bool _isAnimating;");
        AssertContains(controllerText, "public bool IsAnimating => _isAnimating;");
        AssertContains(controllerText, "public void Toggle()");
        AssertContains(controllerText, "public void ApplyVisibility(bool visible)");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.UpdateLayout();");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;");
        AssertDoesNotContain(mainWindowText, "private bool _isSettingsShelfAnimating;");
        AssertDoesNotContain(animationsText, "private void AnimateSettingsShelf(");
        AssertDoesNotContain(eventHandlersText, "private void SettingsToggleButton_Click(");

        return Task.CompletedTask;
    }

    private static Task SplashLoadingPhrases_LiveInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var splashAdapterText = ReadRepoFile("Sussudio/MainWindow.SplashLoading.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/SplashLoadingPhraseController.cs").Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio/Controllers/SplashLoadingPhraseCatalog.cs").Replace("\r\n", "\n");

        AssertContains(splashAdapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(splashAdapterText, "private void InitializeSplashLoadingPhraseController()");
        AssertContains(splashAdapterText, "SplashLoadingTextA = SplashLoadingTextA,");
        AssertContains(splashAdapterText, "SplashLoadingTransformB = SplashLoadingTransformB,");
        AssertContains(splashAdapterText, "=> _splashLoadingPhraseController.Start();");
        AssertContains(splashAdapterText, "=> _splashLoadingPhraseController.Stop();");
        AssertContains(mainWindowText, "InitializeSplashLoadingPhraseController();");
        AssertContains(launchEntranceControllerText, "_context.StartSplashLoadingPhrases();");
        AssertContains(launchEntranceControllerText, "_context.StopSplashLoadingPhrases();");
        AssertContains(controllerText, "internal sealed class SplashLoadingPhraseController");
        AssertContains(controllerText, "private DispatcherTimer? _splashPhraseTimer;");
        AssertContains(controllerText, "SplashLoadingPhraseCatalog.Load()");
        AssertContains(controllerText, "private TimeSpan NextSplashPhraseInterval()");
        AssertContains(controllerText, "private void CyclePhrase()");
        AssertContains(controllerText, "storyboard.Begin();");
        AssertContains(catalogText, "internal static class SplashLoadingPhraseCatalog");
        AssertContains(catalogText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertContains(catalogText, "public static string[] Load()");
        AssertContains(catalogText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");
        AssertContains(catalogText, "if (line.StartsWith(\"##\"))");
        AssertContains(catalogText, "if (line.StartsWith('#')) continue;");
        AssertContains(catalogText, "if (line.StartsWith(\"<!--\")) continue;");
        AssertContains(catalogText, "line = line[2..].Trim();");
        AssertContains(catalogText, "while (line.EndsWith('.'))");
        AssertContains(catalogText, "_cachedSplashPhrases = DefaultSplashLoadingPhrases;");
        AssertDoesNotContain(animationsText, "private DispatcherTimer? _splashPhraseTimer;");
        AssertDoesNotContain(animationsText, "private static string[] LoadSplashPhrases()");
        AssertDoesNotContain(animationsText, "private void CycleSplashPhrase()");
        AssertDoesNotContain(controllerText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertDoesNotContain(controllerText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");

        return Task.CompletedTask;
    }

    private static Task LaunchEntranceAnimation_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.LaunchEntrance.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeLaunchEntranceAnimationController()");
        AssertContains(adapterText, "SplashContent = SplashContent,");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewBorderScale = PreviewBorderScale,");
        AssertContains(adapterText, "GetEntranceButtons = GetEntranceButtons,");
        AssertContains(adapterText, "IsPreviewFirstVisualConfirmed = () => _previewFirstVisualConfirmed,");
        AssertContains(adapterText, "FadeInControlBarShadow = () => FadeInShadow(_controlBarShadowVisual, delayMs: 400, durationMs: 500),");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PrepareInitialState();");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PlaySplashAndEntrance();");
        AssertContains(mainWindowText, "InitializeLaunchEntranceAnimationController();");
        AssertContains(mainWindowText, "PrepareLaunchEntranceInitialState();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(controllerText, "internal sealed class LaunchEntranceAnimationController");
        AssertContains(controllerText, "private bool _played;");
        AssertContains(controllerText, "private Storyboard? _activeStoryboard;");
        AssertContains(controllerText, "public void PrepareInitialState()");
        AssertContains(controllerText, "_context.ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };");
        AssertContains(controllerText, "_context.PreviewBorderScale.ScaleX = 0.97;");
        AssertContains(controllerText, "foreach (var button in _context.GetEntranceButtons())");
        AssertContains(controllerText, "public void PlaySplashAndEntrance()");
        AssertContains(controllerText, "private void PlayEntranceAnimation()");
        AssertContains(controllerText, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(controllerText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertDoesNotContain(mainWindowText, "private bool _entranceAnimationPlayed;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _entranceStoryboard;");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Opacity = 0;");
        AssertDoesNotContain(mainWindowText, "var entranceButtons = GetEntranceButtons();");
        AssertDoesNotContain(animationsText, "private void PlaySplashAndEntrance()");
        AssertDoesNotContain(animationsText, "private void PlayEntranceAnimation()");

        return Task.CompletedTask;
    }

    private static Task MainWindowStartupHosting_LivesInStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(startupText, "private int _automationServicesStarted;");
        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += uncloakOnFirstFrame;");
        AssertContains(startupText, "DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakFalse, sizeof(int));");
        AssertContains(startupText, "await ViewModel.InitializeAsync();");
        AssertContains(startupText, "PrimePreviewAudioFadeIn();");
        AssertContains(startupText, "await ViewModel.RefreshDevicesAsync();");
        AssertContains(startupText, "RevealPreviewUnavailablePlaceholder();");
        AssertContains(startupText, "StartAutomationServices();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(startupText, "private void StartAutomationServices()");
        AssertContains(startupText, "_automationDiagnosticsHub.Start();");
        AssertContains(startupText, "Automation control ready on pipe");
        AssertContains(startupText, "Automation control disabled on pipe");
        AssertContains(mainWindowText, "mainContent.Loaded += MainWindow_Loaded;");
        AssertDoesNotContain(mainWindowText, "private int _automationServicesStarted;");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_Loaded(");
        AssertDoesNotContain(closeLifecycleText, "private void StartAutomationServices()");
        AssertDoesNotContain(closeLifecycleText, "_automationServicesStarted");

        return Task.CompletedTask;
    }

    private static Task MainWindowShellResizeTelemetry_LivesInSizingPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var windowSizingText = ReadRepoFile("Sussudio/MainWindow.WindowSizing.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");

        AssertContains(windowSizingText, "private long _previewLastResizeLogTick;");
        AssertContains(windowSizingText, "private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(windowSizingText, "if (!ViewModel.IsPreviewing ||");
        AssertContains(windowSizingText, "_d3dRenderer == null ||");
        AssertContains(windowSizingText, "PreviewSwapChainPanel.Visibility != Visibility.Visible");
        AssertContains(windowSizingText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertContains(windowSizingText, "Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick)");
        AssertContains(windowSizingText, "Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        AssertContains(mainWindowText, "mainContent.SizeChanged += MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupText, "mainContent.SizeChanged -= MainWindow_SizeChanged;");
        AssertContains(previewRendererText, "_previewLastResizeLogTick = 0;");
        AssertDoesNotContain(mainWindowText, "private long _previewLastResizeLogTick;");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_SizeChanged(");
        AssertDoesNotContain(closeLifecycleText, "_previewLastResizeLogTick");

        return Task.CompletedTask;
    }

    private static Task PreviewRendererRuntimeState_LivesInRendererPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");
        var previewSurfaceText = ReadRepoFile("Sussudio/MainWindow.PreviewSurface.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/MainWindow.StatsSnapshot.cs").Replace("\r\n", "\n");

        AssertContains(previewRendererText, "private SoftwareBitmapSource? _previewSource;");
        AssertContains(previewRendererText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertContains(previewSurfaceText, "Preview surface presentation");
        AssertContains(previewSurfaceText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewSurfaceText, "private void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceText, "private void SetupControlBarShadow()");
        AssertContains(previewRendererText, "private long _previewFramesArrived;");
        AssertContains(previewRendererText, "private long _previewFramesDisplayed;");
        AssertContains(previewRendererText, "private long _previewFramesDropped;");
        AssertContains(previewRendererText, "private long _previewLastPresentedTick;");
        AssertContains(previewRendererText, "private long _lastRendererStopTick;");
        AssertContains(previewRendererText, "private long _rendererReinitUnsafeWindows;");
        AssertContains(previewRendererText, "private double _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertContains(previewRendererText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "return GetPreviewRuntimeSnapshot();");
        AssertContains(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertContains(previewRuntimeSnapshotText, "var d3d = _d3dRenderer;");
        AssertContains(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertContains(previewRendererText, "return Math.Max(1.0, 1000.0 / sourceFps);");
        AssertContains(previewRendererText, "_previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();");
        AssertContains(statsSnapshotText, "GetPresentCadenceMetrics(_previewMinPresentationIntervalMs)");
        AssertDoesNotContain(mainWindowText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesArrived;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesDisplayed;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesDropped;");
        AssertDoesNotContain(mainWindowText, "private long _previewLastPresentedTick;");
        AssertDoesNotContain(mainWindowText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(mainWindowText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(mainWindowText, "private double _previewMinPresentationIntervalMs;");
        AssertDoesNotContain(mainWindowText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertDoesNotContain(mainWindowText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertDoesNotContain(mainWindowText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertDoesNotContain(mainWindowText, "private static bool IsHdrSubtype");

        return Task.CompletedTask;
    }

    private static Task MainWindowTitlePresentation_LivesInTitlePartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var titleText = ReadRepoFile("Sussudio/MainWindow.WindowTitle.cs").Replace("\r\n", "\n");

        AssertContains(titleText, "private readonly string _windowTitleBase;");
        AssertContains(titleText, "private static string BuildWindowTitleBase()");
        AssertContains(titleText, "Environment.ProcessPath");
        AssertContains(titleText, "File.GetLastWriteTime(exePath)");
        AssertContains(titleText, "CultureInfo.InvariantCulture");
        AssertContains(titleText, "private void ApplyWindowTitle()");
        AssertContains(titleText, "Title = $\"{_windowTitleBase} - REC {ViewModel.RecordingTime}\";");
        AssertContains(mainWindowText, "_windowTitleBase = BuildWindowTitleBase();");
        AssertContains(mainWindowText, "ApplyWindowTitle();");
        AssertContains(propertyChangedText, "ApplyWindowTitle();");
        AssertDoesNotContain(mainWindowText, "private static string BuildWindowTitleBase()");
        AssertDoesNotContain(mainWindowText, "private void ApplyWindowTitle()");
        AssertDoesNotContain(mainWindowText, "CultureInfo.InvariantCulture");

        return Task.CompletedTask;
    }

    private static Task MainWindowCloseLifecycleAndNativeHelpers_AreSplit()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var nativeWindowText = ReadRepoFile("Sussudio/MainWindow.NativeWindow.cs").Replace("\r\n", "\n");
        var oldWindowManagementPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Sussudio",
            "MainWindow.WindowManagement.cs");

        if (File.Exists(oldWindowManagementPath))
        {
            throw new InvalidOperationException("MainWindow.WindowManagement.cs should not return as a catch-all partial.");
        }

        AssertContains(closeLifecycleText, "private int _windowCloseRequested;");
        AssertContains(closeLifecycleText, "private int _windowCloseCleanupStarted;");
        AssertContains(closeLifecycleText, "private TaskCompletionSource<object?>? _windowCloseCompletion;");
        AssertContains(closeLifecycleText, "private bool _isWindowClosing;");
        AssertContains(closeLifecycleText, "private void RegisterCloseLifecycle(Microsoft.UI.Windowing.AppWindow appWindow)");
        AssertContains(closeLifecycleText, "=> appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "private async void MainWindow_Closing(");
        AssertContains(closeLifecycleText, "private async Task<bool> TryStopRecordingBeforeCloseAsync()");
        AssertContains(shutdownCleanupText, "Post-close shutdown cleanup");
        AssertContains(shutdownCleanupText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertContains(closeLifecycleText, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertContains(closeLifecycleText, "private Task GetWindowCloseCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleText, "private void RequestWindowClose()");
        AssertContains(closeLifecycleText, "private static bool IsCloseAlreadyInProgressException(Exception ex)");
        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(nativeWindowText, "private const int MinWindowWidth = 900;");
        AssertContains(nativeWindowText, "private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;");
        AssertContains(nativeWindowText, "private IntPtr _hwnd;");
        AssertContains(nativeWindowText, "private Microsoft.UI.Windowing.AppWindow InitializeNativeShellWindow()");
        AssertContains(nativeWindowText, "ViewModel.SetWindowHandle(_hwnd);");
        AssertContains(nativeWindowText, "MinSizeWindowSubclass.Install(_hwnd, MinWindowWidth, MinWindowHeight);");
        AssertContains(nativeWindowText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertContains(nativeWindowText, "appWindow.SetIcon(\"Assets\\\\AppIcon.ico\");");
        AssertContains(nativeWindowText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertContains(nativeWindowText, "private static extern int DwmSetWindowAttribute(");
        AssertContains(nativeWindowText, "private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;");
        AssertContains(nativeWindowText, "private const int DWMWA_CLOAK = 13;");
        AssertContains(mainWindowText, "var appWindow = InitializeNativeShellWindow();");
        AssertContains(mainWindowText, "RegisterCloseLifecycle(appWindow);");
        AssertContains(mainWindowText, "InitializeShellControllers();");
        AssertContains(mainWindowText, "Closed += MainWindow_Closed;");
        AssertDoesNotContain(mainWindowText, "WindowNative.GetWindowHandle(this);");
        AssertDoesNotContain(mainWindowText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(mainWindowText, "MinSizeWindowSubclass.Install(");
        AssertDoesNotContain(mainWindowText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertDoesNotContain(mainWindowText, "appWindow.Closing += MainWindow_Closing;");
        AssertDoesNotContain(mainWindowText, "private int _windowCloseRequested;");
        AssertDoesNotContain(mainWindowText, "private bool _isWindowClosing;");
        AssertDoesNotContain(mainWindowText, "private IntPtr _hwnd;");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(closeLifecycleText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");

        return Task.CompletedTask;
    }
}

using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task LaunchEntranceAnimation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/MainWindow.ControllerInitialization.cs").Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var splashText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs").Replace("\r\n", "\n");
        var shellText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;");
        AssertContains(adapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(adapterText, "private void InitializeLaunchEntranceAnimationController()");
        AssertContains(adapterText, "SplashContent = SplashContent,");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewBorderScale = PreviewBorderScale,");
        AssertContains(adapterText, "GetEntranceButtons = GetEntranceButtons,");
        AssertContains(adapterText, "IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,");
        AssertContains(adapterText, "FadeInControlBarShadow = () => FadeInControlBarShadow(delayMs: 400, durationMs: 500),");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PrepareInitialState();");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PlaySplashAndEntrance();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ShellChrome.Composition.cs")),
            "launch entrance adapter lives in the shell chrome composition partial");
        AssertContains(controllerInitializationText, "InitializeLaunchEntranceAnimationController();");
        AssertContains(mainWindowText, "PrepareLaunchEntranceInitialState();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(controllerText, "internal sealed partial class LaunchEntranceAnimationController");
        AssertContains(splashText, "internal sealed partial class LaunchEntranceAnimationController");
        AssertContains(shellText, "internal sealed partial class LaunchEntranceAnimationController");
        AssertContains(splashText, "private bool _played;");
        AssertContains(shellText, "private Storyboard? _activeStoryboard;");
        AssertContains(controllerText, "public void PrepareInitialState()");
        AssertContains(controllerText, "_context.ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };");
        AssertContains(controllerText, "_context.PreviewBorderScale.ScaleX = 0.97;");
        AssertContains(controllerText, "foreach (var button in _context.GetEntranceButtons())");
        AssertContains(splashText, "public void PlaySplashAndEntrance()");
        AssertContains(splashText, "BeginTime = TimeSpan.FromMilliseconds(180)");
        AssertContains(splashText, "BeginTime = TimeSpan.FromMilliseconds(3000)");
        AssertContains(splashText, "_context.StopSplashLoadingPhrases();");
        AssertContains(splashText, "PlayEntranceAnimation();");
        AssertOccursBefore(splashText, "_context.StartSplashLoadingPhrases();", "splashStoryboard.Begin();");
        AssertOccursBefore(splashText, "_context.StopSplashLoadingPhrases();", "PlayEntranceAnimation();");
        AssertContains(shellText, "private void PlayEntranceAnimation()");
        AssertContains(shellText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(shellText, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(shellText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(shellText, "_context.FadeInControlBarShadow();");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs");
        AssertDoesNotContain(mainWindowText, "private bool _entranceAnimationPlayed;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _entranceStoryboard;");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Opacity = 0;");
        AssertDoesNotContain(mainWindowText, "var entranceButtons = GetEntranceButtons();");
        AssertDoesNotContain(controllerText, "public void PlaySplashAndEntrance()");
        AssertDoesNotContain(controllerText, "private void PlayEntranceAnimation()");
        AssertDoesNotContain(splashText, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertDoesNotContain(shellText, "_context.StartSplashLoadingPhrases();");

        return Task.CompletedTask;
    }

    internal static Task MainWindowStartupHosting_LivesInStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/MainWindow.ControllerInitialization.cs").Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs").Replace("\r\n", "\n");
        var launchStartupControllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchStartupController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(startupText, "private LaunchStartupController _launchStartupController = null!;");
        AssertContains(startupText, "private void InitializeLaunchStartupController()");
        AssertContains(startupText, "new LaunchStartupControllerContext");
        AssertContains(startupText, "MainContent = (FrameworkElement)Content,");
        AssertContains(startupText, "LoadedHandler = MainWindow_Loaded,");
        AssertContains(startupText, "ScheduleNativeShellRevealAfterFirstFrame = ScheduleNativeShellRevealAfterFirstFrame,");
        AssertContains(startupText, "RunUiEventHandlerAsync = RunUiEventHandlerAsync,");
        AssertContains(startupText, "InitializeViewModelAsync = ViewModel.InitializeAsync,");
        AssertContains(startupText, "PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,");
        AssertContains(startupText, "RefreshDevicesAsync = () => ViewModel.RefreshDevicesAsync(),");
        AssertContains(startupText, "StartAutomationHost = _automationHostLifecycleController.Start,");
        AssertContains(startupText, "PlaySplashAndEntrance = PlaySplashAndEntrance,");
        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "=> _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));");
        AssertContains(launchStartupControllerText, "internal sealed class LaunchStartupControllerContext");
        AssertContains(launchStartupControllerText, "internal sealed class LaunchStartupController");
        AssertContains(launchStartupControllerText, "public void HandleLoaded(string operationName)");
        AssertContains(launchStartupControllerText, "_context.MainContent.Loaded -= _context.LoadedHandler;");
        AssertContains(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();");
        AssertContains(launchStartupControllerText, "_ = _context.RunUiEventHandlerAsync(async () =>");
        AssertContains(launchStartupControllerText, "await _context.InitializeViewModelAsync();");
        AssertContains(launchStartupControllerText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(launchStartupControllerText, "await _context.RefreshDevicesAsync();");
        AssertContains(launchStartupControllerText, "_context.RevealPreviewUnavailablePlaceholder();");
        AssertContains(launchStartupControllerText, "_context.StartAutomationHost();");
        AssertContains(launchStartupControllerText, "_context.PlaySplashAndEntrance();");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchStartupController.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchStartupController.cs");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ShellChrome.Composition.cs")),
            "startup adapter lives in the shell chrome composition partial");
        AssertContains(mainWindowText, "private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");
        AssertContains(automationHostControllerText, "private int _started;");
        AssertContains(automationHostControllerText, "Interlocked.Exchange(ref _started, 1)");
        AssertContains(automationHostControllerText, "if (_pipeServer.Start())\n        {\n            _diagnosticsHub.Start();");
        AssertContains(automationHostControllerText, "Automation control ready on pipe");
        AssertContains(automationHostControllerText, "Automation control disabled on pipe");
        AssertContains(mainWindowText, "InitializeShellControllers();");
        AssertDoesNotContain(mainWindowText, "private void InitializePreviewControllers()");
        AssertContains(controllerInitializationText, "private void InitializeShellControllers()");
        AssertContains(controllerInitializationText, "private void InitializeWindowShellControllers()");
        AssertContains(controllerInitializationText, "private void InitializeFlashbackControllers()");
        AssertContains(controllerInitializationText, "private void InitializeShellPresentationControllers()");
        AssertContains(controllerInitializationText, "private void InitializePreviewControllers()");
        AssertContains(controllerInitializationText, "private void InitializeRecordingControllers()");
        AssertContains(controllerInitializationText, "private void InitializeLaunchAndStatusControllers()");
        AssertContains(controllerInitializationText, "InitializeLaunchStartupController();");
        AssertContains(controllerInitializationText, "private void InitializePreviewActionControllers()");
        AssertContains(controllerInitializationText, "private void InitializeAudioControllers()");
        AssertContains(controllerInitializationText, "private void InitializeCaptureControllers()");
        AssertContains(controllerInitializationText, "private void InitializeOutputControllers()");
        AssertOccursBefore(controllerInitializationText, "InitializeWindowShellControllers();", "InitializeFlashbackControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeFlashbackControllers();", "InitializeShellPresentationControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeShellPresentationControllers();", "InitializePreviewControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializePreviewControllers();", "InitializeRecordingControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeRecordingControllers();", "InitializeLaunchAndStatusControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeLaunchAndStatusControllers();", "InitializePreviewActionControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializePreviewActionControllers();", "InitializeAudioControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeAudioControllers();", "InitializeResponsiveShellLayoutController();");
        AssertOccursBefore(controllerInitializationText, "InitializeResponsiveShellLayoutController();", "InitializeCaptureControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeCaptureControllers();", "InitializeOutputControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeOutputControllers();", "InitializePreviewScreenshotController();");
        AssertOccursBefore(controllerInitializationText, "private void InitializeWindowShellControllers()", "private void InitializeFlashbackControllers()");
        AssertOccursBefore(controllerInitializationText, "private void InitializeFlashbackControllers()", "private void InitializeShellPresentationControllers()");
        AssertOccursBefore(controllerInitializationText, "private void InitializeShellPresentationControllers()", "private void InitializePreviewControllers()");
        AssertOccursBefore(controllerInitializationText, "private void InitializePreviewControllers()", "private void InitializeRecordingControllers()");
        AssertOccursBefore(launchStartupControllerText, "await _context.InitializeViewModelAsync();", "_context.StartAutomationHost();");
        AssertOccursBefore(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();", "_ = _context.RunUiEventHandlerAsync(async () =>");
        AssertOccursBefore(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();", "await _context.InitializeViewModelAsync();");
        AssertOccursBefore(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();", "_context.PlaySplashAndEntrance();");
        AssertContains(mainWindowText, "mainContent.Loaded += MainWindow_Loaded;");
        AssertDoesNotContain(startupText, "await ViewModel.InitializeAsync();");
        AssertDoesNotContain(startupText, "await ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(startupText, "_automationHostLifecycleController.Start();");
        AssertDoesNotContain(mainWindowText, "private int _automationServicesStarted;");
        AssertDoesNotContain(startupText, "private int _automationServicesStarted;");
        AssertDoesNotContain(startupText, "Interlocked.Exchange(ref _automationServicesStarted");
        AssertDoesNotContain(startupText, "_automationDiagnosticsHub.Start();");
        AssertDoesNotContain(startupText, "CompositionTarget.Rendering");
        AssertDoesNotContain(startupText, "UncloakNativeShellWindow();");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_Loaded(");
        AssertDoesNotContain(closeLifecycleText, "private void StartAutomationServices()");
        AssertDoesNotContain(closeLifecycleText, "_automationServicesStarted");

        return Task.CompletedTask;
    }
}

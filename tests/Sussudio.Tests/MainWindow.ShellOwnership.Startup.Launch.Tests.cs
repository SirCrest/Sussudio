using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task LaunchEntranceAnimation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.LaunchEntrance.cs").Replace("\r\n", "\n");
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
        AssertContains(mainWindowText, "InitializeLaunchEntranceAnimationController();");
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

    private static Task MainWindowStartupHosting_LivesInStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "ScheduleNativeShellRevealAfterFirstFrame();");
        AssertContains(startupText, "await ViewModel.InitializeAsync();");
        AssertContains(startupText, "PrimePreviewAudioFadeIn();");
        AssertContains(startupText, "await ViewModel.RefreshDevicesAsync();");
        AssertContains(startupText, "RevealPreviewUnavailablePlaceholder();");
        AssertContains(startupText, "_automationHostLifecycleController.Start();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
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
        AssertContains(mainWindowText, "private void InitializeShellControllers()");
        AssertContains(mainWindowText, "private void InitializeWindowShellControllers()");
        AssertContains(mainWindowText, "private void InitializeFlashbackControllers()");
        AssertContains(mainWindowText, "private void InitializeShellPresentationControllers()");
        AssertContains(mainWindowText, "private void InitializePreviewControllers()");
        AssertContains(mainWindowText, "private void InitializeRecordingControllers()");
        AssertContains(mainWindowText, "private void InitializeLaunchAndStatusControllers()");
        AssertContains(mainWindowText, "private void InitializePreviewActionControllers()");
        AssertContains(mainWindowText, "private void InitializeAudioControllers()");
        AssertContains(mainWindowText, "private void InitializeCaptureControllers()");
        AssertContains(mainWindowText, "private void InitializeOutputControllers()");
        AssertOccursBefore(mainWindowText, "InitializeWindowShellControllers();", "InitializeFlashbackControllers();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackControllers();", "InitializeShellPresentationControllers();");
        AssertOccursBefore(mainWindowText, "InitializeShellPresentationControllers();", "InitializePreviewControllers();");
        AssertOccursBefore(mainWindowText, "InitializePreviewControllers();", "InitializeRecordingControllers();");
        AssertOccursBefore(mainWindowText, "InitializeRecordingControllers();", "InitializeLaunchAndStatusControllers();");
        AssertOccursBefore(mainWindowText, "InitializeLaunchAndStatusControllers();", "InitializePreviewActionControllers();");
        AssertOccursBefore(mainWindowText, "InitializePreviewActionControllers();", "InitializeAudioControllers();");
        AssertOccursBefore(mainWindowText, "InitializeAudioControllers();", "InitializeResponsiveShellLayoutController();");
        AssertOccursBefore(mainWindowText, "InitializeResponsiveShellLayoutController();", "InitializeCaptureControllers();");
        AssertOccursBefore(mainWindowText, "InitializeCaptureControllers();", "InitializeOutputControllers();");
        AssertOccursBefore(mainWindowText, "InitializeOutputControllers();", "InitializePreviewScreenshotController();");
        AssertOccursBefore(mainWindowText, "private void InitializeWindowShellControllers()", "private void InitializeFlashbackControllers()");
        AssertOccursBefore(mainWindowText, "private void InitializeFlashbackControllers()", "private void InitializeShellPresentationControllers()");
        AssertOccursBefore(mainWindowText, "private void InitializeShellPresentationControllers()", "private void InitializePreviewControllers()");
        AssertOccursBefore(mainWindowText, "private void InitializePreviewControllers()", "private void InitializeRecordingControllers()");
        AssertOccursBefore(startupText, "await ViewModel.InitializeAsync();", "_automationHostLifecycleController.Start();");
        AssertOccursBefore(startupText, "ScheduleNativeShellRevealAfterFirstFrame();", "_ = RunUiEventHandlerAsync(async () =>");
        AssertOccursBefore(startupText, "ScheduleNativeShellRevealAfterFirstFrame();", "await ViewModel.InitializeAsync();");
        AssertOccursBefore(startupText, "ScheduleNativeShellRevealAfterFirstFrame();", "PlaySplashAndEntrance();");
        AssertContains(mainWindowText, "mainContent.Loaded += MainWindow_Loaded;");
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

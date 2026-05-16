using System.Threading.Tasks;

static partial class Program
{
    private static Task SplashLoadingPhrases_LiveInController()
    {
        var launchEntranceSplashText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Splash.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var splashAdapterText = ReadRepoFile("Sussudio/MainWindow.SplashLoading.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/SplashLoadingPhraseController.cs").Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio/Controllers/Launch/SplashLoadingPhraseCatalog.cs").Replace("\r\n", "\n");

        AssertContains(splashAdapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(splashAdapterText, "private void InitializeSplashLoadingPhraseController()");
        AssertContains(splashAdapterText, "SplashLoadingTextA = SplashLoadingTextA,");
        AssertContains(splashAdapterText, "SplashLoadingTransformB = SplashLoadingTransformB,");
        AssertContains(splashAdapterText, "=> _splashLoadingPhraseController.Start();");
        AssertContains(splashAdapterText, "=> _splashLoadingPhraseController.Stop();");
        AssertContains(mainWindowText, "InitializeSplashLoadingPhraseController();");
        AssertContains(launchEntranceSplashText, "_context.StartSplashLoadingPhrases();");
        AssertContains(launchEntranceSplashText, "_context.StopSplashLoadingPhrases();");
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
        AssertDoesNotContain(controllerText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertDoesNotContain(controllerText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");

        return Task.CompletedTask;
    }

    private static Task LaunchEntranceAnimation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.LaunchEntrance.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var splashText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Splash.cs").Replace("\r\n", "\n");
        var shellText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Shell.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;");
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
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Splash.cs");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Shell.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Splash.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchEntranceAnimationController.Shell.cs");
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
        var automationHostAdapterText = ReadRepoFile("Sussudio/MainWindow.AutomationHost.cs").Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/WindowAutomationHostLifecycleController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += uncloakOnFirstFrame;");
        AssertContains(startupText, "UncloakNativeShellWindow();");
        AssertContains(startupText, "await ViewModel.InitializeAsync();");
        AssertContains(startupText, "PrimePreviewAudioFadeIn();");
        AssertContains(startupText, "await ViewModel.RefreshDevicesAsync();");
        AssertContains(startupText, "RevealPreviewUnavailablePlaceholder();");
        AssertContains(startupText, "StartAutomationServices();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(automationHostAdapterText, "private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;");
        AssertContains(automationHostAdapterText, "private void StartAutomationServices()");
        AssertContains(automationHostAdapterText, "=> _automationHostLifecycleController.Start();");
        AssertContains(automationHostControllerText, "private int _started;");
        AssertContains(automationHostControllerText, "Interlocked.Exchange(ref _started, 1)");
        AssertContains(automationHostControllerText, "if (_pipeServer.Start())\n        {\n            _diagnosticsHub.Start();");
        AssertContains(automationHostControllerText, "Automation control ready on pipe");
        AssertContains(automationHostControllerText, "Automation control disabled on pipe");
        AssertOccursBefore(startupText, "await ViewModel.InitializeAsync();", "StartAutomationServices();");
        AssertContains(mainWindowText, "mainContent.Loaded += MainWindow_Loaded;");
        AssertDoesNotContain(mainWindowText, "private int _automationServicesStarted;");
        AssertDoesNotContain(startupText, "private int _automationServicesStarted;");
        AssertDoesNotContain(startupText, "Interlocked.Exchange(ref _automationServicesStarted");
        AssertDoesNotContain(startupText, "_automationDiagnosticsHub.Start();");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_Loaded(");
        AssertDoesNotContain(closeLifecycleText, "private void StartAutomationServices()");
        AssertDoesNotContain(closeLifecycleText, "_automationServicesStarted");

        return Task.CompletedTask;
    }
}

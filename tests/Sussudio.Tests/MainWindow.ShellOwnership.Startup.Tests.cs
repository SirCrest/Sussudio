using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task SplashLoadingPhrases_LiveInController()
    {
        var launchEntranceSplashText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var splashAdapterText = ReadRepoFile("Sussudio/MainWindow.SplashLoading.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs").Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseCatalog.cs").Replace("\r\n", "\n");
        var pacingPolicyText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhrasePacingPolicy.cs").Replace("\r\n", "\n");

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
        AssertContains(controllerText, "private readonly SplashLoadingPhrasePacingPolicy _pacingPolicy = new();");
        AssertContains(controllerText, "_pacingPolicy.Reset();");
        AssertContains(controllerText, "Interval = _pacingPolicy.NextInterval()");
        AssertContains(controllerText, "private void CyclePhrase()");
        AssertContains(controllerText, "storyboard.Begin();");
        AssertContains(pacingPolicyText, "internal sealed class SplashLoadingPhrasePacingPolicy");
        AssertContains(pacingPolicyText, "internal enum SplashLoadingPhrasePaceMode");
        AssertContains(pacingPolicyText, "public TimeSpan NextInterval()");
        AssertContains(pacingPolicyText, "internal TimeSpan NextInterval(Func<double> nextDouble, Func<int, int, int> nextInt)");
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
        AssertDoesNotContain(controllerText, "private TimeSpan NextSplashPhraseInterval()");
        AssertDoesNotContain(controllerText, "Random.Shared.NextDouble()");
        AssertDoesNotContain(controllerText, "SplashLoadingPhrasePaceMode");
        AssertDoesNotContain(controllerText, "280, 420");
        AssertDoesNotContain(controllerText, "380, 900");
        AssertDoesNotContain(controllerText, "900, 1500");
        AssertDoesNotContain(controllerText, "1500, 2500");
        AssertDoesNotContain(controllerText, "nextInt(280, 420)");
        AssertDoesNotContain(controllerText, "nextInt(380, 900)");
        AssertDoesNotContain(controllerText, "nextInt(900, 1500)");
        AssertDoesNotContain(controllerText, "nextInt(1500, 2500)");

        return Task.CompletedTask;
    }

    private static Task SplashLoadingPhrasePacingPolicy_PreservesIntervalBands()
    {
        var policyType = RequireType("Sussudio.Controllers.SplashLoadingPhrasePacingPolicy");
        var policy = Activator.CreateInstance(policyType, nonPublic: true)
            ?? throw new InvalidOperationException("Failed to create SplashLoadingPhrasePacingPolicy.");
        var nextInterval = policyType.GetMethod(
                "NextInterval",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Func<double>), typeof(Func<int, int, int>) },
                modifiers: null)
            ?? throw new InvalidOperationException("SplashLoadingPhrasePacingPolicy.NextInterval test seam was not found.");
        var reset = policyType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("SplashLoadingPhrasePacingPolicy.Reset was not found.");

        AssertEqual(
            TimeSpan.FromMilliseconds(319),
            InvokePolicy(policy, nextInterval, new[] { 0.10d }, (2, 6, 2), (280, 420, 319)),
            "burst first interval uses burst tick and interval ranges");
        AssertEqual(
            TimeSpan.FromMilliseconds(318),
            InvokePolicy(policy, nextInterval, Array.Empty<double>(), (280, 420, 318)),
            "burst keeps current mode while tick budget remains");
        AssertEqual(
            TimeSpan.FromMilliseconds(700),
            InvokePolicy(policy, nextInterval, new[] { 0.20d }, (1, 4, 1), (380, 900, 700)),
            "normal lower boundary uses normal ranges");
        AssertEqual(
            TimeSpan.FromMilliseconds(1200),
            InvokePolicy(policy, nextInterval, new[] { 0.70d }, (900, 1500, 1200)),
            "stuck lower boundary uses stuck interval range");
        AssertEqual(
            TimeSpan.FromMilliseconds(2000),
            InvokePolicy(policy, nextInterval, new[] { 0.90d }, (1500, 2500, 2000)),
            "long-stuck lower boundary uses long-stuck interval range");

        _ = InvokePolicy(policy, nextInterval, new[] { 0.05d }, (2, 6, 5), (280, 420, 300));
        reset.Invoke(policy, null);
        AssertEqual(
            TimeSpan.FromMilliseconds(1800),
            InvokePolicy(policy, nextInterval, new[] { 0.95d }, (1500, 2500, 1800)),
            "reset forces the next interval to choose a fresh mode");

        return Task.CompletedTask;
    }

    private static TimeSpan InvokePolicy(
        object policy,
        MethodInfo nextInterval,
        double[] rolls,
        params (int Min, int Max, int Value)[] integerResponses)
    {
        var rollQueue = new Queue<double>(rolls);
        var integerQueue = new Queue<(int Min, int Max, int Value)>(integerResponses);

        Func<double> nextDouble = () =>
        {
            if (rollQueue.Count == 0)
            {
                throw new InvalidOperationException("Policy requested an unexpected random roll.");
            }

            return rollQueue.Dequeue();
        };
        Func<int, int, int> nextInt = (min, max) =>
        {
            if (integerQueue.Count == 0)
            {
                throw new InvalidOperationException($"Policy requested unexpected integer range {min}..{max}.");
            }

            var expected = integerQueue.Dequeue();
            AssertEqual(expected.Min, min, "policy integer range minimum");
            AssertEqual(expected.Max, max, "policy integer range maximum");
            return expected.Value;
        };

        var result = (TimeSpan)(nextInterval.Invoke(policy, new object[] { nextDouble, nextInt })
                                ?? throw new InvalidOperationException("Policy returned null interval."));
        AssertEqual(0, rollQueue.Count, "unused policy random rolls");
        AssertEqual(0, integerQueue.Count, "unused policy integer responses");
        return result;
    }

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
        var automationHostAdapterText = ReadRepoFile("Sussudio/MainWindow.AutomationHost.cs").Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "ScheduleNativeShellRevealAfterFirstFrame();");
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
        AssertOccursBefore(startupText, "await ViewModel.InitializeAsync();", "StartAutomationServices();");
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

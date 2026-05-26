using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task SplashLoadingPhrases_LiveInController()
    {
        var launchEntranceText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var launchAdapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs").Replace("\r\n", "\n");
        var catalogText = controllerText;
        var pacingPolicyText = controllerText;

        AssertContains(launchAdapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(launchAdapterText, "private void InitializeSplashLoadingPhraseController()");
        AssertContains(launchAdapterText, "SplashLoadingTextA = SplashLoadingTextA,");
        AssertContains(launchAdapterText, "SplashLoadingTransformB = SplashLoadingTransformB,");
        AssertContains(launchAdapterText, "=> _splashLoadingPhraseController.Start();");
        AssertContains(launchAdapterText, "=> _splashLoadingPhraseController.Stop();");
        AssertContains(mainWindowText, "InitializeSplashLoadingPhraseController();");
        AssertContains(launchEntranceText, "_context.StartSplashLoadingPhrases();");
        AssertContains(launchEntranceText, "_context.StopSplashLoadingPhrases();");
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
        AssertDoesNotContain(controllerText, "private TimeSpan NextSplashPhraseInterval()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhraseCatalog.cs")),
            "splash phrase catalog folded into SplashLoadingPhraseController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhrasePacingPolicy.cs")),
            "splash phrase pacing policy folded into SplashLoadingPhraseController.cs");

        return Task.CompletedTask;
    }

    internal static Task SplashLoadingPhrasePacingPolicy_PreservesIntervalBands()
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

    internal static Task LaunchEntranceAnimation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
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
        AssertContains(controllerText, "internal sealed class LaunchEntranceAnimationController");
        AssertContains(controllerText, "private bool _played;");
        AssertContains(controllerText, "private Storyboard? _activeStoryboard;");
        AssertContains(controllerText, "public void PrepareInitialState()");
        AssertContains(controllerText, "_context.ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };");
        AssertContains(controllerText, "_context.PreviewBorderScale.ScaleX = 0.97;");
        AssertContains(controllerText, "foreach (var button in _context.GetEntranceButtons())");
        AssertContains(controllerText, "public void PlaySplashAndEntrance()");
        AssertContains(controllerText, "BeginTime = TimeSpan.FromMilliseconds(180)");
        AssertContains(controllerText, "BeginTime = TimeSpan.FromMilliseconds(3000)");
        AssertContains(controllerText, "_context.StopSplashLoadingPhrases();");
        AssertContains(controllerText, "PlayEntranceAnimation();");
        AssertOccursBefore(controllerText, "_context.StartSplashLoadingPhrases();", "splashStoryboard.Begin();");
        AssertOccursBefore(controllerText, "_context.StopSplashLoadingPhrases();", "PlayEntranceAnimation();");
        AssertContains(controllerText, "private void PlayEntranceAnimation()");
        AssertContains(controllerText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(controllerText, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(controllerText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(controllerText, "_context.FadeInControlBarShadow();");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertDoesNotContain(agentMapText, "LaunchEntranceAnimationController.Splash.cs");
        AssertDoesNotContain(agentMapText, "LaunchEntranceAnimationController.Shell.cs");
        AssertDoesNotContain(cleanupPlanText, "LaunchEntranceAnimationController.Splash.cs");
        AssertDoesNotContain(cleanupPlanText, "LaunchEntranceAnimationController.Shell.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Entrance", "LaunchEntranceAnimationController.Splash.cs")),
            "launch entrance splash phase is consolidated into the root controller");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Entrance", "LaunchEntranceAnimationController.Shell.cs")),
            "launch entrance shell phase is consolidated into the root controller");
        AssertDoesNotContain(mainWindowText, "private bool _entranceAnimationPlayed;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _entranceStoryboard;");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Opacity = 0;");
        AssertDoesNotContain(mainWindowText, "var entranceButtons = GetEntranceButtons();");

        return Task.CompletedTask;
    }

    internal static Task MainWindowStartupHosting_LivesInStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationController.cs").Replace("\r\n", "\n");
        var launchStartupControllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs").Replace("\r\n", "\n");
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
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "LaunchStartupController.cs")),
            "launch startup choreography lives with the launch flow owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Entrance", "LaunchEntranceAnimationController.cs")),
            "launch entrance choreography lives with the launch flow owner");
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
        AssertContains(controllerInitializationText, "private void InitializeShellControllers()");
        AssertContains(controllerInitializationText, "private void InitializeWindowAutomationControllers()");
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
        AssertOccursBefore(controllerInitializationText, "InitializeWindowAutomationControllers();", "InitializeFlashbackControllers();");
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
        AssertOccursBefore(controllerInitializationText, "private void InitializeWindowAutomationControllers()", "private void InitializeFlashbackControllers()");
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
        AssertDoesNotContain(closeLifecycleText, "private void StartAutomationServices()");
        AssertDoesNotContain(closeLifecycleText, "_automationServicesStarted");

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
}

using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainWindowCloseLifecycleAndNativeHelpers_AreSplit()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var nativeWindowText = ReadRepoFile("Sussudio/MainWindow.NativeWindow.cs").Replace("\r\n", "\n");
        var nativeWindowControllerText = ReadRepoFile("Sussudio/Controllers/Window/NativeWindowBootstrapController.cs").Replace("\r\n", "\n");
        var automationHostAdapterText = ReadRepoFile("Sussudio/MainWindow.AutomationHost.cs").Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs").Replace("\r\n", "\n");
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var closeRequestControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseRequestController.cs").Replace("\r\n", "\n");
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var stopBeforeCloseMethodOffset = closeRecordingFinalizationControllerText.IndexOf("public async Task<bool> StopBeforeCloseAsync(");
        var stopAfterClosedMethodOffset = closeRecordingFinalizationControllerText.IndexOf("public async Task StopAfterClosedBestEffortAsync(");
        var waitForStopMethodOffset = closeRecordingFinalizationControllerText.IndexOf("private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(");
        var oldWindowManagementPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "MainWindow.WindowManagement.cs");

        if (stopBeforeCloseMethodOffset < 0)
        {
            throw new InvalidOperationException("Window close recording finalization controller must expose pre-close recording stop.");
        }

        if (stopAfterClosedMethodOffset < 0)
        {
            throw new InvalidOperationException("Window close recording finalization controller must expose post-close best-effort stop.");
        }

        if (waitForStopMethodOffset < 0)
        {
            throw new InvalidOperationException("Window close recording finalization controller must keep recording-stop wait mechanics in a helper.");
        }

        var stopBeforeCloseMethodText = closeRecordingFinalizationControllerText.Substring(
            stopBeforeCloseMethodOffset,
            stopAfterClosedMethodOffset - stopBeforeCloseMethodOffset);
        var stopAfterClosedMethodText = closeRecordingFinalizationControllerText.Substring(
            stopAfterClosedMethodOffset,
            waitForStopMethodOffset - stopAfterClosedMethodOffset);
        var waitForStopMethodText = closeRecordingFinalizationControllerText.Substring(waitForStopMethodOffset);
        var documentedOwners = new[]
        {
            "Sussudio/Controllers/Window/WindowCloseLifecycleController.cs",
            "Sussudio/Controllers/Window/WindowCloseRequestController.cs",
            "Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs",
            "Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs",
            "Sussudio/MainWindow.CloseLifecycle.cs",
            "Sussudio/MainWindow.ShutdownCleanup.cs",
            "Sussudio/Controllers/Window/NativeWindowBootstrapController.cs",
        };

        foreach (var owner in documentedOwners)
        {
            AssertContains(agentMapText, owner);
            AssertContains(cleanupPlanText, owner);
        }

        if (File.Exists(oldWindowManagementPath))
        {
            throw new InvalidOperationException("MainWindow.WindowManagement.cs should not return as a catch-all partial.");
        }

        AssertContains(closeLifecycleText, "private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();");
        AssertContains(closeLifecycleText, "private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();");
        AssertContains(closeLifecycleText, "private WindowCloseRequestController _windowCloseRequestController = null!;");
        AssertContains(closeLifecycleText, "private bool _isWindowClosing => _windowCloseLifecycleController.IsClosing;");
        AssertContains(closeLifecycleText, "private void InitializeWindowCloseRequestController()");
        AssertContains(closeLifecycleText, "CloseWindow = Close,");
        AssertContains(closeLifecycleText, "ExitApplication = () => Application.Current.Exit(),");
        AssertContains(closeLifecycleText, "IsRecording = () => ViewModel.IsRecording,");
        AssertContains(closeLifecycleText, "IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning");
        AssertContains(closeLifecycleText, "private void RegisterCloseLifecycle(Microsoft.UI.Windowing.AppWindow appWindow)");
        AssertContains(closeLifecycleText, "=> appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "private async void MainWindow_Closing(");
        AssertContains(closeLifecycleText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "if (!ViewModel.IsRecording && !ViewModel.IsRecordingTransitioning)");
        AssertContains(closeLifecycleText, "private Task<bool> TryStopRecordingBeforeCloseAsync()");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));");
        AssertContains(closeLifecycleText, "RequestWindowClose();");
        AssertContains(shutdownCleanupText, "Post-close shutdown cleanup");
        AssertContains(shutdownCleanupText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertContains(shutdownCleanupText, "await _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(");
        AssertContains(shutdownCleanupText, "await DisposeAutomationHostAsync();");
        AssertContains(automationHostAdapterText, "private ValueTask DisposeAutomationHostAsync()");
        AssertContains(automationHostAdapterText, "=> _automationHostLifecycleController.DisposeAsync();");
        AssertContains(automationHostControllerText, "public async ValueTask DisposeAsync()");
        AssertContains(automationHostControllerText, "await _pipeServer.DisposeAsync();");
        AssertContains(automationHostControllerText, "await _diagnosticsHub.DisposeAsync();");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation diagnostics shutdown cleanup failed: {ex.Message}\");");
        AssertOccursBefore(automationHostControllerText, "await _pipeServer.DisposeAsync();", "await _diagnosticsHub.DisposeAsync();");
        AssertOccursBefore(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");", "await _diagnosticsHub.DisposeAsync();");
        AssertOccursBefore(shutdownCleanupText, "CompleteWindowCloseRequest();", "_windowCloseLifecycleController.MarkClosing();");
        AssertContains(closeLifecycleText, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertContains(closeLifecycleText, "=> _windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken);");
        AssertContains(closeLifecycleText, "private void RequestWindowClose()");
        AssertContains(closeLifecycleText, "=> _windowCloseRequestController.RequestClose();");
        AssertContains(closeLifecycleControllerText, "internal sealed class WindowCloseLifecycleController");
        AssertContains(closeLifecycleControllerText, "private int _closeRequested;");
        AssertContains(closeLifecycleControllerText, "private int _cleanupStarted;");
        AssertContains(closeLifecycleControllerText, "private int _recordingStopInProgress;");
        AssertContains(closeLifecycleControllerText, "private int _allowedAfterRecordingStop;");
        AssertContains(closeLifecycleControllerText, "private TaskCompletionSource<object?>? _completion;");
        AssertContains(closeLifecycleControllerText, "public bool TryBeginCleanup()");
        AssertContains(closeLifecycleControllerText, "public void MarkClosing()");
        AssertContains(closeLifecycleControllerText, "public Task CloseAsync(");
        AssertContains(closeLifecycleControllerText, "private Task GetCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleControllerText, "public static bool IsCloseAlreadyInProgressException(Exception ex)");
        AssertContains(closeRequestControllerText, "internal sealed class WindowCloseRequestControllerContext");
        AssertContains(closeRequestControllerText, "public required WindowCloseLifecycleController LifecycleController { get; init; }");
        AssertContains(closeRequestControllerText, "public required Action CloseWindow { get; init; }");
        AssertContains(closeRequestControllerText, "public required Action ExitApplication { get; init; }");
        AssertContains(closeRequestControllerText, "public required Func<bool> IsRecording { get; init; }");
        AssertContains(closeRequestControllerText, "internal sealed class WindowCloseRequestController");
        AssertContains(closeRequestControllerText, "public void RequestClose()");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.TryMarkRequested()");
        AssertContains(closeRequestControllerText, "_context.CloseWindow();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.IsRecordingStopInProgress");
        AssertContains(closeRequestControllerText, "!_context.IsRecording()");
        AssertContains(closeRequestControllerText, "!_context.IsRecordingTransitioning()");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeRequestControllerText, "WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex)");
        AssertContains(closeRequestControllerText, "catch (COMException ex)");
        AssertContains(closeRequestControllerText, "_context.ExitApplication();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.ResetRequestedAfterFailure();");
        AssertContains(closeRecordingFinalizationControllerText, "internal sealed class WindowCloseRecordingFinalizationController");
        AssertContains(closeRecordingFinalizationControllerText, "private const int StopBudgetMs = 120_000;");
        AssertContains(closeRecordingFinalizationControllerText, "public async Task<bool> StopBeforeCloseAsync(");
        AssertContains(closeRecordingFinalizationControllerText, "public async Task StopAfterClosedBestEffortAsync(");
        AssertContains(closeRecordingFinalizationControllerText, "private enum RecordingStopWaitResult");
        AssertContains(closeRecordingFinalizationControllerText, "private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(MainViewModel viewModel)");
        AssertContains(stopBeforeCloseMethodText, "var stopResult = await WaitForRecordingStopAsync(viewModel);");
        AssertContains(stopAfterClosedMethodText, "var stopResult = await WaitForRecordingStopAsync(viewModel);");
        AssertDoesNotContain(stopBeforeCloseMethodText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertDoesNotContain(stopAfterClosedMethodText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(waitForStopMethodText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(waitForStopMethodText, "var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.IsHitTestVisible = false;");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.Opacity = 0.5;");
        AssertContains(closeRecordingFinalizationControllerText, "if (shutdownContent != null &&");
        AssertContains(closeRecordingFinalizationControllerText, "!isAllowedAfterRecordingStop())");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.IsHitTestVisible = true;");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.Opacity = 1;");
        AssertDoesNotContain(stopAfterClosedMethodText, "shutdownContent.IsHitTestVisible = true;");
        AssertDoesNotContain(stopAfterClosedMethodText, "shutdownContent.Opacity = 1;");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_TIMEOUT ");
        AssertContains(closeRecordingFinalizationControllerText, "close cancelled to protect recording.");
        AssertContains(closeRecordingFinalizationControllerText, "Still saving recording. Close cancelled.");
        AssertContains(closeRecordingFinalizationControllerText, "window already closed; continuing shutdown cleanup.");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_FAILED_AFTER_CLOSE ");
        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupText, "CancelNativeShellRevealAfterFirstFrame();");
        AssertOccursBefore(
            shutdownCleanupText,
            "CancelNativeShellRevealAfterFirstFrame();",
            "if (!_windowCloseLifecycleController.TryBeginCleanup())");
        AssertContains(nativeWindowText, "private readonly NativeWindowBootstrapController _nativeWindowBootstrapController = new();");
        AssertContains(nativeWindowText, "private IntPtr _hwnd;");
        AssertContains(nativeWindowText, "private AppWindow InitializeNativeShellWindow()");
        AssertContains(nativeWindowText, "var result = _nativeWindowBootstrapController.Initialize(this, ViewModel.SetWindowHandle);");
        AssertContains(nativeWindowText, "_hwnd = result.Hwnd;");
        AssertContains(nativeWindowText, "return result.AppWindow;");
        AssertContains(nativeWindowText, "private AppWindow GetAppWindow()");
        AssertContains(nativeWindowText, "=> _nativeWindowBootstrapController.GetAppWindow(this);");
        AssertContains(nativeWindowText, "private void ScheduleNativeShellRevealAfterFirstFrame()");
        AssertContains(nativeWindowText, "=> _nativeWindowBootstrapController.ScheduleRevealAfterFirstComposedFrame(_hwnd);");
        AssertContains(nativeWindowText, "private void CancelNativeShellRevealAfterFirstFrame()");
        AssertContains(nativeWindowText, "=> _nativeWindowBootstrapController.CancelPendingFirstFrameReveal();");
        AssertContains(nativeWindowControllerText, "internal readonly record struct NativeWindowBootstrapResult(IntPtr Hwnd, AppWindow AppWindow);");
        AssertContains(nativeWindowControllerText, "internal sealed class NativeWindowBootstrapController");
        AssertContains(nativeWindowControllerText, "private const int MinWindowWidth = 900;");
        AssertContains(nativeWindowControllerText, "private const int MinWindowHeight = 500;");
        AssertContains(nativeWindowControllerText, "private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;");
        AssertContains(nativeWindowControllerText, "private const int DWMWA_CLOAK = 13;");
        AssertContains(nativeWindowControllerText, "private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;");
        AssertContains(nativeWindowControllerText, "private EventHandler<object>? _pendingFirstFrameReveal;");
        AssertContains(nativeWindowControllerText, "public NativeWindowBootstrapResult Initialize(Window window, Action<IntPtr> setWindowHandle)");
        AssertContains(nativeWindowControllerText, "var hwnd = WindowNative.GetWindowHandle(window);");
        AssertContains(nativeWindowControllerText, "setWindowHandle(hwnd);");
        AssertContains(nativeWindowControllerText, "SetCloaked(hwnd, cloaked: true);");
        AssertContains(nativeWindowControllerText, "SetDarkMode(hwnd, enabled: true);");
        AssertContains(nativeWindowControllerText, "MinSizeWindowSubclass.Install(hwnd, MinWindowWidth, MinWindowHeight);");
        AssertContains(nativeWindowControllerText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertContains(nativeWindowControllerText, "appWindow.SetIcon(\"Assets\\\\AppIcon.ico\");");
        AssertContains(nativeWindowControllerText, "public AppWindow GetAppWindow(Window window)");
        AssertContains(nativeWindowControllerText, "public void SetCloaked(IntPtr hwnd, bool cloaked)");
        AssertContains(nativeWindowControllerText, "public void ScheduleRevealAfterFirstComposedFrame(IntPtr hwnd)");
        AssertContains(nativeWindowControllerText, "CancelPendingFirstFrameReveal();");
        AssertContains(nativeWindowControllerText, "EventHandler<object>? revealOnFirstFrame = null;");
        AssertContains(nativeWindowControllerText, "_pendingFirstFrameReveal = revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "SetCloaked(hwnd, cloaked: false);");
        AssertContains(nativeWindowControllerText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "public void CancelPendingFirstFrameReveal()");
        AssertContains(nativeWindowControllerText, "var pending = _pendingFirstFrameReveal;");
        AssertContains(nativeWindowControllerText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= pending;");
        AssertContains(nativeWindowControllerText, "_pendingFirstFrameReveal = null;");
        AssertOccursBefore(
            nativeWindowControllerText,
            "CancelPendingFirstFrameReveal();",
            "SetCloaked(hwnd, cloaked: false);");
        AssertOccursBefore(
            nativeWindowControllerText,
            "_pendingFirstFrameReveal = revealOnFirstFrame;",
            "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "private static extern int DwmSetWindowAttribute(");
        AssertContains(mainWindowText, "ViewModel = new MainViewModel();");
        AssertContains(mainWindowText, "InitializeWindowCloseRequestController();");
        AssertOccursBefore(mainWindowText, "ViewModel = new MainViewModel();", "InitializeWindowCloseRequestController();");
        AssertOccursBefore(mainWindowText, "InitializeWindowCloseRequestController();", "_automationHostLifecycleController = new WindowAutomationHostLifecycleController(");
        AssertContains(mainWindowText, "var appWindow = InitializeNativeShellWindow();");
        AssertContains(mainWindowText, "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "InitializeWindowCloseRequestController();", "RegisterCloseLifecycle(appWindow);");
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
        AssertDoesNotContain(closeLifecycleText, "private int _windowCloseRequested;");
        AssertDoesNotContain(closeLifecycleText, "private TaskCompletionSource<object?>? _windowCloseCompletion;");
        AssertDoesNotContain(closeLifecycleText, "private Task GetWindowCloseCompletionTask(CancellationToken cancellationToken)");
        AssertDoesNotContain(closeLifecycleText, "private static bool IsCloseAlreadyInProgressException(Exception ex)");
        AssertDoesNotContain(closeLifecycleText, "WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex)");
        AssertDoesNotContain(closeLifecycleText, "catch (System.Runtime.InteropServices.COMException ex)");
        AssertDoesNotContain(closeLifecycleText, "Window.Close COMException");
        AssertDoesNotContain(closeLifecycleText, "ResetRequestedAfterFailure();");
        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopBudgetMs");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(shutdownCleanupText, "Task.WhenAny(");
        AssertDoesNotContain(shutdownCleanupText, "StopBudgetMs");
        AssertDoesNotContain(shutdownCleanupText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(shutdownCleanupText, "_automationPipeServer.DisposeAsync()");
        AssertDoesNotContain(shutdownCleanupText, "_automationDiagnosticsHub.DisposeAsync()");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(closeLifecycleText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertDoesNotContain(nativeWindowText, "private static extern int DwmSetWindowAttribute(");
        AssertDoesNotContain(nativeWindowText, "MinSizeWindowSubclass.Install(");

        return Task.CompletedTask;
    }
}

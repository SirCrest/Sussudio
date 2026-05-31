using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainWindowNativeBootstrap_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var nativeWindowText = ReadMainWindowShellChromeAdapterSource();
        var nativeWindowControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.Composition.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var nativeBootstrapOwner = "Sussudio/Controllers/Window/WindowCloseLifecycleController.cs";

        AssertContains(agentMapText, nativeBootstrapOwner);
        AssertContains(cleanupPlanText, nativeBootstrapOwner);
        AssertContains(agentMapText, "Sussudio/MainWindow.Composition.cs");
        AssertContains(cleanupPlanText, "Sussudio/MainWindow.Composition.cs");
        AssertContains(agentMapText, "owns native window");
        AssertContains(cleanupPlanText, "DWM cloak/dark-mode setup");
        AssertContains(agentMapText, "first-composed-frame");
        AssertContains(cleanupPlanText, "first-composed-frame shell reveal");
        AssertContains(mainWindowText, "InitializeShellControllers();");
        AssertContains(mainWindowText, "var appWindow = InitializeNativeShellWindow();");
        AssertContains(mainWindowText, "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "var appWindow = InitializeNativeShellWindow();", "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "RegisterCloseLifecycle(appWindow);", "InitializeShellControllers();");
        AssertDoesNotContain(mainWindowText, "WindowNative.GetWindowHandle(this);");
        AssertDoesNotContain(mainWindowText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(mainWindowText, "MinSizeWindowSubclass.Install(");
        AssertDoesNotContain(mainWindowText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");

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
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "native window adapter lives in the shell chrome composition partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "NativeWindowBootstrapController.cs")),
            "native window bootstrap lives with the window lifecycle controller");
        AssertDoesNotContain(nativeWindowText, "private static extern int DwmSetWindowAttribute(");
        AssertDoesNotContain(nativeWindowText, "MinSizeWindowSubclass.Install(");

        AssertContains(nativeWindowControllerText, "internal readonly record struct NativeWindowBootstrapResult(IntPtr Hwnd, AppWindow AppWindow);");
        AssertContains(nativeWindowControllerText, "internal sealed class NativeWindowBootstrapController");
        AssertContains(nativeWindowControllerText, "private const int MinWindowWidth = 1500;");
        AssertContains(nativeWindowControllerText, "private const int MinWindowHeight = 900;");
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
        AssertContains(nativeWindowControllerText, "if (appWindow.Presenter is OverlappedPresenter presenter)");
        AssertContains(nativeWindowControllerText, "presenter.IsResizable = true;");
        AssertContains(nativeWindowControllerText, "presenter.IsMaximizable = true;");
        AssertContains(nativeWindowControllerText, "presenter.IsMinimizable = true;");
        AssertContains(nativeWindowControllerText, "presenter.Restore();");
        AssertContains(nativeWindowControllerText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertContains(nativeWindowControllerText, "appWindow.SetIcon(\"Assets\\\\AppIcon.ico\");");
        AssertContains(nativeWindowControllerText, "public AppWindow GetAppWindow(Window window)");
        AssertContains(nativeWindowControllerText, "var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);");
        AssertContains(nativeWindowControllerText, "return AppWindow.GetFromWindowId(windowId);");
        AssertContains(nativeWindowControllerText, "public void SetCloaked(IntPtr hwnd, bool cloaked)");
        AssertContains(nativeWindowControllerText, "DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));");
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
        AssertContains(nativeWindowControllerText, "private static void SetDarkMode(IntPtr hwnd, bool enabled)");
        AssertContains(nativeWindowControllerText, "DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));");
        AssertOccursBefore(
            nativeWindowControllerText,
            "CancelPendingFirstFrameReveal();",
            "SetCloaked(hwnd, cloaked: false);");
        AssertOccursBefore(
            nativeWindowControllerText,
            "_pendingFirstFrameReveal = revealOnFirstFrame;",
            "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "private static extern int DwmSetWindowAttribute(");

        return Task.CompletedTask;
    }

    internal static Task MainWindowCloseLifecycleAndShutdownCleanup_AreSplit()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var oldWindowManagementPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "MainWindow.WindowManagement.cs");
        var documentedOwners = new[]
        {
            "Sussudio/Controllers/Window/WindowCloseLifecycleController.cs",
            "Sussudio/Controllers/Window/WindowAutomationController.cs",
            "Sussudio/MainWindow.Composition.cs",
            "Sussudio/MainWindow.xaml.cs",
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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ShutdownCleanup.Composition.cs")),
            "shutdown cleanup adapter folded into MainWindow root composition");

        AssertContains(mainWindowText, "ViewModel = new MainViewModel();");
        AssertContains(mainWindowText, "InitializeWindowCloseRequestController();");
        AssertOccursBefore(mainWindowText, "ViewModel = new MainViewModel();", "InitializeWindowCloseRequestController();");
        AssertContains(mainWindowText, "InitializeWindowShutdownCleanupController();");
        AssertOccursBefore(mainWindowText, "InitializeWindowCloseRequestController();", "_automationHostLifecycleController = new WindowAutomationHostLifecycleController(");
        AssertContains(mainWindowText, "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "InitializeWindowShutdownCleanupController();", "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "InitializeWindowCloseRequestController();", "RegisterCloseLifecycle(appWindow);");
        AssertContains(mainWindowText, "Closed += MainWindow_Closed;");
        AssertDoesNotContain(mainWindowText, "WindowNative.GetWindowHandle(this);");
        AssertDoesNotContain(mainWindowText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(mainWindowText, "MinSizeWindowSubclass.Install(");
        AssertDoesNotContain(mainWindowText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertDoesNotContain(mainWindowText, "appWindow.Closing += MainWindow_Closing;");
        AssertDoesNotContain(mainWindowText, "private int _windowCloseRequested;");
        AssertDoesNotContain(mainWindowText, "private bool _isWindowClosing;");
        AssertDoesNotContain(mainWindowText, "private IntPtr _hwnd;");

        return Task.CompletedTask;
    }

    internal static Task MainWindowCloseLifecycleControllers_OwnCloseRequestAndAppClosing()
    {
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.Composition.cs").Replace("\r\n", "\n");
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var appClosingControllerText = closeLifecycleControllerText;
        var closeRequestControllerText = closeLifecycleControllerText;

        AssertContains(closeLifecycleText, "private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();");
        AssertContains(closeLifecycleText, "private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();");
        AssertContains(closeLifecycleText, "private WindowCloseRequestController _windowCloseRequestController = null!;");
        AssertContains(closeLifecycleText, "private WindowAppClosingController _windowAppClosingController = null!;");
        AssertContains(closeLifecycleText, "private bool _isWindowClosing => _windowCloseLifecycleController.IsClosing;");
        AssertContains(closeLifecycleText, "private void InitializeWindowCloseRequestController()");
        AssertContains(closeLifecycleText, "_windowAppClosingController = new WindowAppClosingController(new WindowAppClosingControllerContext");
        AssertContains(closeLifecycleText, "CloseWindow = Close,");
        AssertContains(closeLifecycleText, "ExitApplication = () => Application.Current.Exit(),");
        AssertContains(closeLifecycleText, "IsRecording = () => ViewModel.IsRecording,");
        AssertContains(closeLifecycleText, "IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning");
        AssertContains(closeLifecycleText, "GetStatusText = () => ViewModel.StatusText,");
        AssertContains(closeLifecycleText, "StopRecordingBeforeCloseAsync = TryStopRecordingBeforeCloseAsync,");
        AssertContains(closeLifecycleText, "RequestWindowClose = RequestWindowClose");
        AssertContains(closeLifecycleText, "private void RegisterCloseLifecycle(AppWindow appWindow)");
        AssertContains(closeLifecycleText, "=> appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "private async void MainWindow_Closing(");
        AssertContains(closeLifecycleText, "=> await _windowAppClosingController.HandleClosingAsync(args);");
        AssertContains(closeLifecycleText, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertContains(closeLifecycleText, "=> _windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken);");
        AssertContains(closeLifecycleText, "private void RequestWindowClose()");
        AssertContains(closeLifecycleText, "=> _windowCloseRequestController.RequestClose();");

        AssertContains(appClosingControllerText, "internal sealed class WindowAppClosingControllerContext");
        AssertContains(appClosingControllerText, "internal sealed class WindowAppClosingController");
        AssertContains(appClosingControllerText, "public async Task HandleClosingAsync(AppWindowClosingEventArgs args)");
        AssertContains(appClosingControllerText, "LogWindowClosingTrigger();");
        AssertContains(appClosingControllerText, "if (!_context.IsRecording() && !_context.IsRecordingTransitioning())");
        AssertContains(appClosingControllerText, "args.Cancel = true;");
        AssertContains(appClosingControllerText, "_context.LifecycleController.ClearRequested();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.TryBeginRecordingStop()");
        AssertContains(appClosingControllerText, "var stopped = await _context.StopRecordingBeforeCloseAsync();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()))");
        AssertContains(appClosingControllerText, "_context.LifecycleController.AllowAfterRecordingStop();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(appClosingControllerText, "_context.RequestWindowClose();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.EndRecordingStop();");
        AssertContains(appClosingControllerText, "WINDOW_CLOSING_TRIGGER ");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowCloseRequestController.cs")),
            "close request execution lives with close lifecycle policy");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowAppClosingController.cs")),
            "app closing choreography lives with close lifecycle policy");

        AssertDoesNotContain(closeLifecycleText, "args.Cancel = true;");
        AssertDoesNotContain(closeLifecycleText, "if (!ViewModel.IsRecording && !ViewModel.IsRecordingTransitioning)");
        AssertDoesNotContain(closeLifecycleText, "CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));");
        AssertDoesNotContain(closeLifecycleText, "private int _windowCloseRequested;");
        AssertDoesNotContain(closeLifecycleText, "private TaskCompletionSource<object?>? _windowCloseCompletion;");
        AssertDoesNotContain(closeLifecycleText, "private Task GetWindowCloseCompletionTask(CancellationToken cancellationToken)");
        AssertDoesNotContain(closeLifecycleText, "private static bool IsCloseAlreadyInProgressException(Exception ex)");
        AssertDoesNotContain(closeLifecycleText, "WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex)");
        AssertDoesNotContain(closeLifecycleText, "catch (System.Runtime.InteropServices.COMException ex)");
        AssertDoesNotContain(closeLifecycleText, "Window.Close COMException");
        AssertDoesNotContain(closeLifecycleText, "ResetRequestedAfterFailure();");
        AssertDoesNotContain(closeLifecycleText, "WINDOW_CLOSING_TRIGGER ");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(closeLifecycleText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");

        return Task.CompletedTask;
    }

    internal static Task MainWindowClose_CancelsCloseUntilRecordingStopCompletes()
    {
        var windowCtorText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.Composition.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs")
            .Replace("\r\n", "\n");
        var appClosingControllerText = closeLifecycleControllerText;
        var closeRequestControllerText = closeLifecycleControllerText;
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "RegisterCloseLifecycle(appWindow);");
        AssertContains(closeLifecycleText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "_windowAppClosingController.HandleClosingAsync(args)");
        AssertContains(appClosingControllerText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(appClosingControllerText, "if (!_context.IsRecording() && !_context.IsRecordingTransitioning())");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(appClosingControllerText, "_context.RequestWindowClose();");
        AssertContains(closeLifecycleText, "_windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken)");
        AssertContains(closeLifecycleText, "=> _windowCloseRequestController.RequestClose();");
        AssertContains(closeRequestControllerText, "_context.CloseWindow();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeRequestControllerText, "_context.ExitApplication();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()))");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeLifecycleControllerText, "private Task GetCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleControllerText, "var enqueueFailure = new InvalidOperationException(\"Failed to enqueue window close action on the UI thread.\");");
        AssertContains(closeRecordingFinalizationControllerText, "private const int StopBudgetMs = 120_000;");
        AssertContains(closeRecordingFinalizationControllerText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(closeRecordingFinalizationControllerText, "var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));");
        AssertContains(closeRecordingFinalizationControllerText, "close cancelled to protect recording");
        AssertContains(closeRecordingFinalizationControllerText, "Still saving recording. Close cancelled.");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_FAILED_AFTER_CLOSE ");
        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(closeLifecycleText, "args.Cancel = true;");
        AssertDoesNotContain(closeLifecycleText, "MP4 may be truncated.");

        return Task.CompletedTask;
    }

    internal static Task MainWindowCloseRecordingFinalization_OwnsRecordingStopPolicy()
    {
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.Composition.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var stopBeforeCloseMethodOffset = closeRecordingFinalizationControllerText.IndexOf("public async Task<bool> StopBeforeCloseAsync(");
        var stopAfterClosedMethodOffset = closeRecordingFinalizationControllerText.IndexOf("public async Task StopAfterClosedBestEffortAsync(");
        var waitForStopMethodOffset = closeRecordingFinalizationControllerText.IndexOf("private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(");

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

        AssertContains(closeLifecycleText, "private Task<bool> TryStopRecordingBeforeCloseAsync()");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(shutdownCleanupText, "StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(");
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

        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopBudgetMs");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(shutdownCleanupText, "Task.WhenAny(");
        AssertDoesNotContain(shutdownCleanupText, "StopBudgetMs");
        AssertDoesNotContain(shutdownCleanupText, "StopRecordingAndWaitAsync");

        return Task.CompletedTask;
    }

    internal static Task MainWindowShutdownCleanup_OwnsPostCloseCleanupOrder()
    {
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationController.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");

        AssertContains(shutdownCleanupText, "private WindowShutdownCleanupController _windowShutdownCleanupController = null!;");
        AssertContains(shutdownCleanupText, "private WindowShutdownCleanupController _windowShutdownCleanupController = null!;");
        AssertContains(shutdownCleanupText, "private void InitializeWindowShutdownCleanupController()");
        AssertContains(shutdownCleanupText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertContains(shutdownCleanupText, "=> await _windowShutdownCleanupController.RunAsync();");
        AssertContains(shutdownCleanupText, "StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(");
        AssertContains(shutdownCleanupText, "DisposeAutomationHostAsync = () => _automationHostLifecycleController.DisposeAsync(),");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");

        AssertContains(automationHostControllerText, "public async ValueTask DisposeAsync()");
        AssertContains(automationHostControllerText, "await _pipeServer.DisposeAsync();");
        AssertContains(automationHostControllerText, "await _diagnosticsHub.DisposeAsync();");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation diagnostics shutdown cleanup failed: {ex.Message}\");");
        AssertOccursBefore(automationHostControllerText, "await _pipeServer.DisposeAsync();", "await _diagnosticsHub.DisposeAsync();");
        AssertOccursBefore(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");", "await _diagnosticsHub.DisposeAsync();");

        AssertContains(shutdownCleanupControllerText, "internal sealed class WindowShutdownCleanupControllerContext");
        AssertContains(shutdownCleanupControllerText, "internal sealed class WindowShutdownCleanupController");
        AssertContains(shutdownCleanupControllerText, "public async Task RunAsync()");
        AssertContains(shutdownCleanupControllerText, "_context.CancelNativeShellRevealAfterFirstFrame();");
        AssertContains(shutdownCleanupControllerText, "if (!_context.LifecycleController.TryBeginCleanup())");
        AssertContains(shutdownCleanupControllerText, "LogWindowClosedTrigger();");
        AssertContains(shutdownCleanupControllerText, "_context.CompleteWindowCloseRequest();");
        AssertContains(shutdownCleanupControllerText, "_context.LifecycleController.MarkClosing();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMeterActivationHandlers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopStatsOverlay();");
        AssertContains(shutdownCleanupControllerText, "_context.StopRecordingVisuals();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMainContentSizeChanged();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachViewModelEventHandlers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopPreviewForShutdown();");
        AssertContains(shutdownCleanupControllerText, "_context.ResetPreviewStartupTracking();");
        AssertContains(shutdownCleanupControllerText, "await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);");
        AssertContains(shutdownCleanupControllerText, "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);");
        AssertContains(shutdownCleanupControllerText, "_context.DisposeNvmlMonitor();");
        AssertContains(shutdownCleanupControllerText, "await _context.DisposeViewModelAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.CancelNativeShellRevealAfterFirstFrame();", "if (!_context.LifecycleController.TryBeginCleanup())");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.CompleteWindowCloseRequest();", "_context.LifecycleController.MarkClosing();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.LifecycleController.MarkClosing();", "_context.DetachMeterActivationHandlers();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.DetachViewModelEventHandlers();", "_context.StopPreviewForShutdown();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.ResetPreviewStartupTracking();", "await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);", "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);", "_context.DisposeNvmlMonitor();");

        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupText, "CancelNativeShellRevealAfterFirstFrame = CancelNativeShellRevealAfterFirstFrame,");
        AssertDoesNotContain(shutdownCleanupText, "WINDOW_CLOSED_TRIGGER ");
        AssertDoesNotContain(shutdownCleanupText, "_automationPipeServer.DisposeAsync()");
        AssertDoesNotContain(shutdownCleanupText, "_automationDiagnosticsHub.DisposeAsync()");

        return Task.CompletedTask;
    }

    internal static Task SplashLoadingPhrases_LiveInController()
    {
        var launchEntranceText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var launchAdapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
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
            "splash phrase catalog folded into LaunchFlowController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhrasePacingPolicy.cs")),
            "splash phrase pacing policy folded into LaunchFlowController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhraseController.cs")),
            "splash phrase controller folded into LaunchFlowController.cs");

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
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
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
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.Composition.cs").Replace("\r\n", "\n");
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
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
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

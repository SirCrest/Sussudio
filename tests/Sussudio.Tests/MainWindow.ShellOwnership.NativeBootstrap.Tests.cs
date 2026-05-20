using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainWindowNativeBootstrap_LivesInFocusedController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var nativeWindowText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.cs").Replace("\r\n", "\n");
        var nativeWindowControllerText = ReadRepoFile("Sussudio/Controllers/Window/NativeWindowBootstrapController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var nativeBootstrapOwner = "Sussudio/Controllers/Window/NativeWindowBootstrapController.cs";

        AssertContains(agentMapText, nativeBootstrapOwner);
        AssertContains(cleanupPlanText, nativeBootstrapOwner);
        AssertContains(agentMapText, "Sussudio/MainWindow.ShellChrome.cs");
        AssertContains(cleanupPlanText, "Sussudio/MainWindow.ShellChrome.cs");
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
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.NativeWindow.cs")),
            "native window adapter is consolidated into the shell chrome adapter");
        AssertDoesNotContain(nativeWindowText, "private static extern int DwmSetWindowAttribute(");
        AssertDoesNotContain(nativeWindowText, "MinSizeWindowSubclass.Install(");

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
}

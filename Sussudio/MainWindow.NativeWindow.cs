using System;
using System.Runtime.InteropServices;
using Sussudio.Services.Runtime;
using WinRT.Interop;

namespace Sussudio;

// Native AppWindow and DWM helpers shared by startup, shell construction, and
// window automation controllers.
public sealed partial class MainWindow
{
    private const int MinWindowWidth = 900;
    private const int MinWindowHeight = 500;
    private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;
    private IntPtr _hwnd;

    private Microsoft.UI.Windowing.AppWindow InitializeNativeShellWindow()
    {
        // Set window handle for folder picker and automation adapters.
        _hwnd = WindowNative.GetWindowHandle(this);
        ViewModel.SetWindowHandle(_hwnd);

        // Cloak the window to prevent white flash before XAML renders.
        int cloakTrue = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakTrue, sizeof(int));
        int darkMode = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Enforce minimum window size via WM_GETMINMAXINFO.
        _minSizeHandle = MinSizeWindowSubclass.Install(_hwnd, MinWindowWidth, MinWindowHeight);

        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));

        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.Restore();
        }

        // Accommodates a 1920x1080 preview, controls, spacing, and titlebar.
        appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));
        appWindow.SetIcon("Assets\\AppIcon.ico");

        return appWindow;
    }

    private Microsoft.UI.Windowing.AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CLOAK = 13;
}

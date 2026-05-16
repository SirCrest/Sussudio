using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sussudio.Services.Runtime;
using WinRT.Interop;

namespace Sussudio.Controllers;

internal readonly record struct NativeWindowBootstrapResult(IntPtr Hwnd, AppWindow AppWindow);

internal sealed class NativeWindowBootstrapController
{
    private const int MinWindowWidth = 900;
    private const int MinWindowHeight = 500;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CLOAK = 13;

    private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;
    private EventHandler<object>? _pendingFirstFrameReveal;

    public NativeWindowBootstrapResult Initialize(Window window, Action<IntPtr> setWindowHandle)
    {
        // Set window handle for folder picker and automation adapters.
        var hwnd = WindowNative.GetWindowHandle(window);
        setWindowHandle(hwnd);

        // Cloak the window to prevent white flash before XAML renders.
        SetCloaked(hwnd, cloaked: true);
        SetDarkMode(hwnd, enabled: true);

        // Enforce minimum window size via WM_GETMINMAXINFO.
        _minSizeHandle = MinSizeWindowSubclass.Install(hwnd, MinWindowWidth, MinWindowHeight);

        var appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.Restore();
        }

        // Accommodates a 1920x1080 preview, controls, spacing, and titlebar.
        appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));
        appWindow.SetIcon("Assets\\AppIcon.ico");

        return new NativeWindowBootstrapResult(hwnd, appWindow);
    }

    public AppWindow GetAppWindow(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    public void SetCloaked(IntPtr hwnd, bool cloaked)
    {
        var value = cloaked ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));
    }

    public void ScheduleRevealAfterFirstComposedFrame(IntPtr hwnd)
    {
        // Loaded fires after layout but before the first paint; wait for the
        // first composed frame so the cloaked shell never exposes a black frame.
        CancelPendingFirstFrameReveal();
        EventHandler<object>? revealOnFirstFrame = null;
        revealOnFirstFrame = (_, _) =>
        {
            CancelPendingFirstFrameReveal();
            SetCloaked(hwnd, cloaked: false);
        };
        _pendingFirstFrameReveal = revealOnFirstFrame;
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;
    }

    public void CancelPendingFirstFrameReveal()
    {
        var pending = _pendingFirstFrameReveal;
        if (pending == null)
        {
            return;
        }

        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= pending;
        _pendingFirstFrameReveal = null;
    }

    private static void SetDarkMode(IntPtr hwnd, bool enabled)
    {
        var value = enabled ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}

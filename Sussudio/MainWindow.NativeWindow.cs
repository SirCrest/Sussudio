using System;
using Microsoft.UI.Windowing;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for native AppWindow bootstrap and DWM helpers shared by
// startup, shell construction, and window automation controllers.
public sealed partial class MainWindow
{
    private readonly NativeWindowBootstrapController _nativeWindowBootstrapController = new();
    private IntPtr _hwnd;

    private AppWindow InitializeNativeShellWindow()
    {
        var result = _nativeWindowBootstrapController.Initialize(this, ViewModel.SetWindowHandle);
        _hwnd = result.Hwnd;
        return result.AppWindow;
    }

    private AppWindow GetAppWindow()
        => _nativeWindowBootstrapController.GetAppWindow(this);

    private void ScheduleNativeShellRevealAfterFirstFrame()
        => _nativeWindowBootstrapController.ScheduleRevealAfterFirstComposedFrame(_hwnd);

    private void CancelNativeShellRevealAfterFirstFrame()
        => _nativeWindowBootstrapController.CancelPendingFirstFrameReveal();
}

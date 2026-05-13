using System;
using System.Threading;
using Microsoft.UI.Xaml;

namespace Sussudio;

// Top-level window resize telemetry for the shell. Preview surface sizing stays
// with MainWindow.PreviewRenderer.cs; close/finalize handling stays in
// MainWindow.CloseLifecycle.cs.
public sealed partial class MainWindow
{
    private long _previewLastResizeLogTick;

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var nowTick = Environment.TickCount64;
        if (!ViewModel.IsPreviewing ||
            _d3dRenderer == null ||
            PreviewSwapChainPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var lastLogTick = Interlocked.Read(ref _previewLastResizeLogTick);
        if (nowTick - lastLogTick < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick) == lastLogTick)
        {
            Logger.Log("Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        }
    }
}

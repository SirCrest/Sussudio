using System;
using System.Threading;
using Microsoft.UI.Xaml;

namespace Sussudio.Controllers;

internal sealed class PreviewResizeTelemetryController
{
    private long _previewLastResizeLogTick;

    public void HandleSizeChanged(bool isPreviewing, bool hasD3dRenderer, Visibility previewVisibility)
    {
        var nowTick = Environment.TickCount64;
        if (!isPreviewing ||
            !hasD3dRenderer ||
            previewVisibility != Visibility.Visible)
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
            Sussudio.Logger.Log("Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _previewLastResizeLogTick, 0);
    }
}

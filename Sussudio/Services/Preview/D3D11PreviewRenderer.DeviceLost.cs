using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpGen.Runtime;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private void HandleDeviceLost(Exception ex)
    {
        Logger.Log($"D3D11 preview device lost ({ex.GetType().Name}); recreating device.");

        // If Stop() is pending, bail. Stop() will unbind the swap chain from
        // the panel while D3D resources are still alive, then the finally block
        // will clean up. Proceeding here would dispose the swap chain while
        // Stop() may be concurrently calling SetSwapChain(null) on the panel -
        // the native call would hit freed memory and trigger an
        // AccessViolationException that .NET 8 cannot catch.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        CleanupD3DResources();
        while (TryDequeuePendingFrame(out var stalePending))
        {
            TrackFrameDropped(stalePending, "device-lost");
            stalePending.Dispose();
        }

        // Re-check: Stop() may have been called during cleanup. Proceeding
        // into InitializeD3D->BindSwapChainToPanel would dispatch to the UI
        // thread, which may be blocked on Join - a 5-second deadlock.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        InitializeD3D();
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
    }

    private static bool IsDeviceLostException(Exception ex)
    {
        if (ex is SharpGenException sharpGenException)
        {
            return sharpGenException.ResultCode == ResultCode.DeviceRemoved ||
                   sharpGenException.ResultCode == ResultCode.DeviceReset;
        }

        if (ex is COMException comException)
        {
            return comException.HResult == unchecked((int)0x887A0005) ||
                   comException.HResult == unchecked((int)0x887A0007);
        }

        return false;
    }
}

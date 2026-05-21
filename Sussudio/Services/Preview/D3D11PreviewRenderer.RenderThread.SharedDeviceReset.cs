using System;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private void HandlePendingSharedDeviceResetOnRenderThread()
    {
        while (TryDequeuePendingFrame(out var stale))
        {
            TrackFrameDropped(stale, "shared-device-reset");
            stale.Dispose();
        }

        try
        {
            // The capture backend can hand us its shared D3D device after the
            // render thread has already created a startup swap chain. Rebuilding
            // directly would leave the first chain attached to SwapChainPanel
            // while the fields point at the second chain, corrupting WinUI's
            // native panel state. Unbind before rebuilding the shared-device chain.
            if (Interlocked.CompareExchange(ref _swapChainBound, 0, 1) == 1)
            {
                Interlocked.Exchange(ref _swapChainAddress, 0);
                UnbindSwapChainFromPanel();
            }

            CleanupD3DResources();
            InitializeD3D();
            Interlocked.Exchange(ref _compositionTransformDirty, 1);
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview shared device rebind failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            CleanupD3DResources();
        }
    }
}

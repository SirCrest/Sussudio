using System;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private bool TryApplyPendingCompositionTransformOnRenderThread(out bool skipFrameDispatch)
    {
        skipFrameDispatch = false;

        // Re-check stop flag: Stop() may have unbound the swap chain between
        // the top-of-loop check and here. Accessing an unbound chain causes
        // native stack corruption (BEX64 / 0xc0000409).
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            return false;
        }

        try
        {
            var swapChain = _swapChain;
            if (swapChain != null && Volatile.Read(ref _swapChainBound) == 1)
            {
                ApplyCompositionScaleTransform(swapChain);
            }
        }
        catch (Exception ex)
        {
            if (IsDeviceLostException(ex))
            {
                HandleDeviceLost(ex);
                skipFrameDispatch = true;
                return true;
            }

            Logger.Log($"D3D11 preview composition transform update failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }

        return true;
    }
}

using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private void CleanupRenderThreadExit()
    {
        while (TryDequeuePendingFrame(out var stale))
        {
            TrackFrameDropped(stale, "renderer-exit");
            stale.Dispose();
        }

        FailPendingFrameCapture("Render thread exited before frame capture completed.");
        CleanupD3DResources();
        Interlocked.Exchange(ref _isRendering, 0);
        Volatile.Write(ref _rendererMode, RendererModeNone);
    }
}

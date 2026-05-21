using System;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private string _lastRenderThreadFailureType = string.Empty;
    private string _lastRenderThreadFailureMessage = string.Empty;
    private int _lastRenderThreadFailureHResult;
    private long _renderThreadFailureCount;

    private void NotifyRenderThreadFailed(Exception ex)
    {
        Interlocked.Increment(ref _renderThreadFailureCount);
        Volatile.Write(ref _lastRenderThreadFailureType, ex.GetType().Name);
        Volatile.Write(ref _lastRenderThreadFailureMessage, ex.Message);
        Volatile.Write(ref _lastRenderThreadFailureHResult, ex.HResult);

        var reason = $"{ex.GetType().Name}: {ex.Message}";
        if (!_dispatcherQueue.TryEnqueue(() => RenderThreadFailed?.Invoke(reason)))
        {
            Logger.Log("D3D_RENDER_THREAD_FAILURE_UI_ENQUEUE_FAILED");
        }
    }
}

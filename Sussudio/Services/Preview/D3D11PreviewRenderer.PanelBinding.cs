using System;
using System.Threading;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private int _swapChainBound; // 0=unbound, 1=bound; use Interlocked.CompareExchange to claim unbind

    private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)
    {
        // ISwapChainPanelNative.SetSwapChain must be called on the UI thread
        // because _panel is a XAML element. Marshal from the render thread.
        //
        // Reinit deadlock guard: if the UI thread is blocked in StopRenderThread().Join()
        // waiting for this render thread to exit, dispatching here would deadlock until
        // the Join times out. Two safeguards: (a) the wait below polls _stopRequested in
        // short chunks so it can bail early, (b) the queued lambda re-checks both
        // _stopRequested and that _swapChain still equals the chain we are trying to
        // bind. If either has changed, the renderer has been stopped or the chain
        // superseded, and SetSwapChain on a stale chain would AV.
        var done = new ManualResetEventSlim(false);
        Exception? uiError = null;
        var aborted = false;

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (Volatile.Read(ref _stopRequested) != 0 ||
                    !ReferenceEquals(_swapChain, swapChain))
                {
                    uiError = new OperationCanceledException(
                        "Swap chain binding superseded before reaching the UI thread.");
                    return;
                }

                if (_panel?.XamlRoot == null)
                {
                    uiError = new InvalidOperationException(
                        "Panel is no longer in the visual tree; swap chain binding skipped.");
                    return;
                }

                var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                panelNative.SetSwapChain(swapChain.NativePointer);
                Interlocked.Exchange(ref _swapChainAddress, swapChain.NativePointer.ToInt64());
                Interlocked.Exchange(ref _swapChainBound, 1);
            }
            catch (Exception ex)
            {
                uiError = ex;
            }
            finally
            {
                try { done.Set(); }
                catch { /* race with dispose if we aborted; safe to ignore */ }
            }
        });

        if (!enqueued)
        {
            done.Dispose();
            throw new InvalidOperationException("Failed to enqueue swap chain binding to UI thread.");
        }

        const int waitChunkMs = 50;
        const int maxWaitMs = 5000;
        var elapsedMs = 0;
        var completed = false;
        while (elapsedMs < maxWaitMs)
        {
            if (done.Wait(waitChunkMs))
            {
                completed = true;
                break;
            }

            elapsedMs += waitChunkMs;
            if (Volatile.Read(ref _stopRequested) != 0)
            {
                aborted = true;
                Logger.Log($"D3D11 preview swap-chain binding aborted at {elapsedMs}ms: stop requested during UI dispatcher wait.");
                break;
            }
        }

        if (!completed)
        {
            // Leave done undisposed: the queued lambda may still run later and
            // call done.Set(). Disposing now would race with that and risk an
            // ObjectDisposedException on the UI thread. The lambda's stale-chain
            // guard above prevents it from binding a disposed swap chain.
            if (aborted)
            {
                return;
            }

            throw new TimeoutException("Swap chain binding to UI thread timed out.");
        }

        done.Dispose();
        if (uiError != null)
        {
            if (uiError is OperationCanceledException)
            {
                Logger.Log("D3D11 preview swap-chain binding cancelled on UI thread; renderer shutting down.");
                return;
            }

            throw new InvalidOperationException("Swap chain binding failed on UI thread.", uiError);
        }
    }

    private void UnbindSwapChainFromPanel()
    {
        // Must run on UI thread since _panel is a XAML element.
        // Called from render thread during cleanup, so marshal via dispatcher.
        try
        {
            using var done = new ManualResetEventSlim(false);
            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Guard: if the panel is no longer in the visual tree, its native
                    // COM backing may be released. AccessViolationException from a stale
                    // vtable pointer is a corrupted-state exception that .NET Core cannot
                    // catch; it terminates the process. Skip the call entirely.
                    if (_panel?.XamlRoot == null)
                    {
                        done.Set();
                        return;
                    }

                    var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                    panelNative.SetSwapChain(IntPtr.Zero);
                }
                catch
                {
                    // Best-effort: panel may already be torn down during app shutdown.
                    Logger.Log("D3D11 preview swap chain unbind skipped: UI callback failed during cleanup.");
                }
                finally
                {
                    done.Set();
                }
            });

            if (enqueued)
            {
                if (!done.Wait(TimeSpan.FromSeconds(2)))
                {
                    Logger.Log("D3D11 preview swap chain unbind timed out on UI thread during cleanup.");
                }
            }
            else
            {
                Logger.Log("D3D11 preview swap chain unbind enqueue failed during cleanup.");
            }
        }
        catch (Exception ex)
        {
            // Dispatcher may be shut down; safe to ignore during cleanup.
            Logger.Log($"D3D11 preview swap chain unbind ignored during cleanup: {ex.GetType().Name}: {ex.Message}");
        }
    }

}

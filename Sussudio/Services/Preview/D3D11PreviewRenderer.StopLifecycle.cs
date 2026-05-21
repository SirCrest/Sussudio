using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private int _stopRequested;
    private int _inNativeCall; // 1 while render thread is between guard-check and Present return

    /// <summary>
    /// Stops the render thread before capture pipeline teardown.
    /// </summary>
    public void StopRenderThread()
    {
        // Reinit has two native lifetime constraints that have to be ordered:
        // stop rendering before UnifiedVideoCapture tears down the shared D3D
        // device, and detach SwapChainPanel while the old swap chain is still
        // alive. HDR<->SDR or resolution changes can alter swap-chain format
        // and size; replacing a still-attached stale chain later lets WinUI call
        // through a released native reference during SetSwapChain(newPtr).
        Stop();
        Logger.Log("D3D11 preview renderer render thread stopped for reinit.");
    }

    public void Stop()
    {
        Thread? renderThread;
        lock (_lifecycleLock)
        {
            renderThread = _renderThread;
            if (renderThread == null)
            {
                FailPendingFrameCapture("Preview renderer is not running.");
                Volatile.Write(ref _rendererMode, RendererModeNone);
                ResetPresentCadence();
                return;
            }

            Interlocked.Exchange(ref _stopRequested, 1);
        }

        // Wait for any in-flight native render call (VideoProcessorBlt / Present)
        // to complete before we unbind the swap chain. The render thread sets
        // _inNativeCall=1 before entering the native call block and clears it after.
        // Without this gate, the CAS unbind below can yank the swap chain while
        // the render thread is inside a native D3D call, causing an unrecoverable
        // AccessViolationException (.NET 8 cannot catch corrupted-state exceptions).
        WaitForNativeCallToDrainOrThrow("stop");

        // Unbind swap chain from panel BEFORE joining the render thread.
        // The render thread releases the swap chain and D3D device during cleanup.
        // If we unbind after that, the panel holds a stale DXGI reference and
        // SetSwapChain (either null or new chain) hits an AccessViolationException,
        // a corrupted-state exception .NET Core cannot catch.
        // Unbinding first, while D3D resources are still alive, avoids this.
        //
        // CAS(1->0) ensures exactly one thread performs the unbind. The render
        // thread's CleanupD3DResources also CAS's this flag before disposing the
        // swap chain; whoever loses the race skips the native call entirely.
        if (Interlocked.CompareExchange(ref _swapChainBound, 0, 1) == 1)
        {
            Interlocked.Exchange(ref _swapChainAddress, 0);
            try
            {
                if (_dispatcherQueue.HasThreadAccess)
                {
                    if (_panel?.XamlRoot != null)
                    {
                        var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                        panelNative.SetSwapChain(IntPtr.Zero);
                    }
                }
                else
                {
                    UnbindSwapChainFromPanel();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"D3D11 preview swap chain unbind failed: {ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Wake the render thread AFTER the swap chain is safely unbound so it
        // sees _stopRequested and exits without attempting to Present.
        SignalFrameReady("stop");
        if (Thread.CurrentThread != renderThread)
        {
            if (!renderThread.Join(TimeSpan.FromMilliseconds(_renderThreadStopTimeoutMs)))
            {
                Logger.Log($"D3D11 preview renderer stop timed out after {_renderThreadStopTimeoutMs}ms; leaving renderer owned by the stop path to avoid blocking UI indefinitely.");
                throw new TimeoutException("D3D11 preview render thread did not stop before timeout.");
            }
        }

        lock (_lifecycleLock)
        {
            if (ReferenceEquals(_renderThread, renderThread))
            {
                _renderThread = null;
            }
        }

        while (TryDequeuePendingFrame(out var stale))
        {
            TrackFrameDropped(stale, "renderer-stop");
            stale.Dispose();
        }

        FailPendingFrameCapture("Preview renderer stopped before frame capture completed.");
        Volatile.Write(ref _rendererMode, RendererModeNone);
        ResetPresentCadence();
        Logger.Log("D3D11 preview renderer stop completed.");
    }

    private void WaitForNativeCallToDrainOrThrow(string operation)
    {
        if (Volatile.Read(ref _inNativeCall) == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var spinner = new SpinWait();
        while (Volatile.Read(ref _inNativeCall) != 0)
        {
            if (stopwatch.ElapsedMilliseconds >= _nativeStopFenceTimeoutMs)
            {
                Logger.Log($"D3D11 preview renderer {operation} timed out waiting for native render call to return after {_nativeStopFenceTimeoutMs}ms.");
                throw new TimeoutException("D3D11 preview native render call did not return before timeout.");
            }

            spinner.SpinOnce();
            if (spinner.NextSpinWillYield)
            {
                Thread.Sleep(1);
            }
        }
    }

    private bool TryEnterNativeRenderCall()
    {
        if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
        {
            return false;
        }

        Interlocked.Exchange(ref _inNativeCall, 1);
        if (Volatile.Read(ref _stopRequested) == 0 && Volatile.Read(ref _swapChainBound) != 0)
        {
            return true;
        }

        ExitNativeRenderCall();
        return false;
    }

    private void ExitNativeRenderCall()
        => Interlocked.Exchange(ref _inNativeCall, 0);
}

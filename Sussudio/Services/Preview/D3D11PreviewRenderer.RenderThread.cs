using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Runtime;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;
    private IntPtr _frameLatencyWaitHandle;

    private void RenderThreadMain()
    {
        Interlocked.Exchange(ref _isRendering, 1);
        using var mmcss = MmcssThreadRegistration.TryRegister(_renderMmcssTask, _renderMmcssPriority, message => Logger.Log(message));
        try
        {
            InitializeD3D();
            while (Volatile.Read(ref _stopRequested) == 0)
            {
                _frameReadyEvent.Wait(TimeSpan.FromMilliseconds(200));
                if (Volatile.Read(ref _stopRequested) != 0) break;

                if (Interlocked.CompareExchange(ref _sharedDeviceResetPending, 0, 1) == 1)
                {
                    HandlePendingSharedDeviceResetOnRenderThread();
                }

                if (Interlocked.CompareExchange(ref _compositionTransformDirty, 0, 1) == 1)
                {
                    if (!TryApplyPendingCompositionTransformOnRenderThread(out var skipFrameDispatch))
                    {
                        break;
                    }

                    if (skipFrameDispatch)
                    {
                        continue;
                    }
                }

                if (!ProcessRenderThreadFrameOrIdle())
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview renderer thread failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            NotifyRenderThreadFailed(ex);
        }
        finally
        {
            CleanupRenderThreadExit();
        }
    }

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

    private bool ProcessRenderThreadFrameOrIdle()
    {
        if (!TryDequeuePendingFrame(out var frame))
        {
            ResetFrameReady("render_loop_idle");
            if (!_pendingFrames.IsEmpty ||
                Volatile.Read(ref _compositionTransformDirty) != 0 ||
                Volatile.Read(ref _sharedDeviceResetPending) != 0)
            {
                SignalFrameReady("render_loop_race");
            }

            return true;
        }

        if (Volatile.Read(ref _stopRequested) != 0)
        {
            TrackFrameDropped(frame, "renderer-stopped");
            frame.Dispose();
            return false;
        }

        try
        {
            if (frame.SubmissionGeneration != Interlocked.Read(ref _submissionGeneration))
            {
                var reason = Volatile.Read(ref _submissionGenerationDropReason);
                TrackFrameDropped(frame, string.IsNullOrWhiteSpace(reason) ? "stale-generation" : $"{reason}:stale");
                return true;
            }

            WaitForFrameLatencySignal();
            var framesRenderedBefore = Interlocked.Read(ref _framesRendered);
            RenderFrame(frame);
            if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)
            {
                TrackFrameDropped(frame, "render-skipped");
            }

            // Keep the event set while more frames are queued so the
            // render thread drains the elastic buffer without waiting.
            if (!_pendingFrames.IsEmpty)
            {
                SignalFrameReady("render_loop_drain");
            }
        }
        catch (Exception ex)
        {
            if (IsDeviceLostException(ex))
            {
                HandleDeviceLost(ex);
            }
            else
            {
                Logger.Log($"D3D11 preview render failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            }

            TrackFrameDropped(frame, "render-failed");
        }
        finally
        {
            frame.Dispose();
        }

        if (_pendingFrames.IsEmpty &&
            Volatile.Read(ref _compositionTransformDirty) == 0 &&
            Volatile.Read(ref _sharedDeviceResetPending) == 0)
        {
            ResetFrameReady("render_loop_empty_after_failure");
        }

        return true;
    }

    private void ConfigureFrameLatencyWaitableObject()
    {
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain2?.Dispose();
        _swapChain2 = null;

        if (!_waitableSwapChainEnabled || _swapChain == null)
        {
            return;
        }

        _swapChain2 = _swapChain.QueryInterfaceOrNull<IDXGISwapChain2>();
        if (_swapChain2 == null)
        {
            Logger.Log("D3D11 preview waitable swap chain unavailable: IDXGISwapChain2 not supported.");
            return;
        }

        _swapChain2.MaximumFrameLatency = (uint)_dxgiMaxFrameLatency;
        _frameLatencyWaitHandle = _swapChain2.FrameLatencyWaitableObject;
        Logger.Log($"D3D11 preview waitable swap chain configured handle=0x{_frameLatencyWaitHandle.ToInt64():X} latency={_dxgiMaxFrameLatency}.");
    }

    private void WaitForFrameLatencySignal()
    {
        if (!_waitableSwapChainEnabled || _frameLatencyWaitHandle == IntPtr.Zero)
        {
            return;
        }

        var waitStart = Stopwatch.GetTimestamp();
        var result = WaitForSingleObject(_frameLatencyWaitHandle, 8);
        TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);
        if (result != WaitObject0 && result != WaitTimeout)
        {
            Logger.Log($"D3D11 preview waitable swap chain wait returned {result}.");
        }
    }

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

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}

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

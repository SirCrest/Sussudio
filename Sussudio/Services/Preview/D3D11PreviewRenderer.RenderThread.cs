using System;
using System.Threading;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
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

using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Runtime;
using Microsoft.UI.Dispatching;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    // Reused at every shader bind in the per-frame render path; the LINQ-friendly
    // overloads on Vortice's device context allocate an IReadOnlyList wrapper from
    // Array.Empty<T>() each call, which adds up at 60-120 fps.
    private static readonly ID3D11ClassInstance[] EmptyClassInstances = System.Array.Empty<ID3D11ClassInstance>();

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
                    while (TryDequeuePendingFrame(out var stale))
                    {
                        TrackFrameDropped(stale, "shared-device-reset");
                        stale.Dispose();
                    }

                    try
                    {
                        // The capture backend can hand us its shared D3D device
                        // after the render thread has already created a startup
                        // swap chain. Rebuilding directly would leave the first
                        // chain attached to SwapChainPanel while the fields point
                        // at the second chain, which later corrupts WinUI's native
                        // panel state. Unbind while the old chain is still alive,
                        // then dispose it before InitializeD3D builds the shared
                        // device-backed chain.
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

                if (Interlocked.CompareExchange(ref _compositionTransformDirty, 0, 1) == 1)
                {
                    // Re-check stop flag: Stop() may have unbound the swap chain between
                    // the top-of-loop check and here. Accessing an unbound chain causes
                    // native stack corruption (BEX64 / 0xc0000409).
                    if (Volatile.Read(ref _stopRequested) != 0) break;
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
                            continue;
                        }

                        Logger.Log($"D3D11 preview composition transform update failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
                    }
                }

                if (!TryDequeuePendingFrame(out var frame))
                {
                    ResetFrameReady("render_loop_idle");
                    if (!_pendingFrames.IsEmpty ||
                        Volatile.Read(ref _compositionTransformDirty) != 0 ||
                        Volatile.Read(ref _sharedDeviceResetPending) != 0)
                    {
                        SignalFrameReady("render_loop_race");
                    }
                    continue;
                }

                if (Volatile.Read(ref _stopRequested) != 0)
                {
                    TrackFrameDropped(frame, "renderer-stopped");
                    frame.Dispose();
                    break;
                }

                try
                {
                    if (frame.SubmissionGeneration != Interlocked.Read(ref _submissionGeneration))
                    {
                        var reason = Volatile.Read(ref _submissionGenerationDropReason);
                        TrackFrameDropped(frame, string.IsNullOrWhiteSpace(reason) ? "stale-generation" : $"{reason}:stale");
                        continue;
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
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview renderer thread failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            NotifyRenderThreadFailed(ex);
        }
        finally
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

    private void RenderFrame(PendingFrame frame)
    {
        ApplySwapChainColorSpaceIfDirty();

        if (frame.D3DTextureY != IntPtr.Zero && frame.D3DTextureUV != IntPtr.Zero && _nv12PS != null)
        {
            Volatile.Write(ref _rendererMode, PreviewShaderSources.RendererModeNv12);
            RenderNv12WithShader(frame);
            return;
        }

        if (frame.IsHdr && _fullscreenVS != null && !_hdrPlaneViewsUnavailable)
        {
            var usePassthrough = Volatile.Read(ref _hdrPassthroughEnabled) != 0 &&
                                 _hdrCapableSwapChain &&
                                 _hdrPassthroughPS != null;

            if (usePassthrough)
            {
                Volatile.Write(ref _rendererMode, RendererModeHdrPassthrough);
                RenderHdrFrameWithShader(frame, _hdrPassthroughPS!);
                return;
            }

            if (_hdrTonemapPS != null)
            {
                Volatile.Write(ref _rendererMode, PreviewShaderSources.RendererModeHdr);
                RenderHdrFrameWithShader(frame, _hdrTonemapPS);
                return;
            }
        }

        if (frame.IsHdr && !_loggedHdrShaderFallback)
        {
            _loggedHdrShaderFallback = true;
            var reason = _fullscreenVS == null ? "fullscreen-VS-null"
                : _hdrPlaneViewsUnavailable ? "hdr-plane-views-unavailable"
                : _hdrTonemapPS == null && _hdrPassthroughPS == null ? "both-hdr-shaders-null"
                : "hdr-shader-conditions-not-met";
            Logger.Log($"D3D11_PREVIEW_HDR_SHADER_FALLBACK reason={reason} hdrPassthroughEnabled={Volatile.Read(ref _hdrPassthroughEnabled) != 0} hdrCapableSwapChain={_hdrCapableSwapChain}");
        }

        Volatile.Write(ref _rendererMode, RendererModeVideoProcessor);
        RenderFrameWithVideoProcessor(frame);
    }

    private void ApplySwapChainColorSpaceIfDirty()
    {
        if (Interlocked.CompareExchange(ref _swapChainColorSpaceDirty, 0, 1) != 1)
        {
            return;
        }

        if (_swapChain3 == null || !_hdrCapableSwapChain)
        {
            return;
        }

        var wantHdr = Volatile.Read(ref _hdrPassthroughEnabled) != 0;
        var targetColorSpace = wantHdr
            ? ColorSpaceType.RgbFullG2084NoneP2020
            : ColorSpaceType.RgbFullG22NoneP709;

        _swapChain3.SetColorSpace1(targetColorSpace);

        var label = wantHdr ? "HDR10-PQ (BT.2020)" : "sRGB (BT.709)";
        _outputColorSpaceLabel = label;
        Logger.Log($"D3D11 preview swap chain color space set to {targetColorSpace} ({label}).");
    }

    private void RenderFrameWithVideoProcessor(PendingFrame frame)
    {
        var totalStart = Stopwatch.GetTimestamp();
        long inputUploadTicks = 0;
        long renderTicks = 0;
        long presentTicks = 0;

        // Fence: Stop() spins on _inNativeCall before unbinding the swap chain.
        // The fence covers the entire render method (EnsurePipeline, input view
        // resolution, Blt, Present) so Stop() cannot yank D3D resources while
        // any of those operations are in flight.
        if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
        {
            return;
        }
        Interlocked.Exchange(ref _inNativeCall, 1);
        try
        {
            if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
            {
                return;
            }

            var useExternalTexture = frame.D3DTexture != null;
            EnsurePipeline(frame.Width, frame.Height, frame.IsHdr, useExternalTexture);

            var inputStart = Stopwatch.GetTimestamp();
            if (!TryResolveInputView(frame, out var inputView, out var disposeInputView))
            {
                return;
            }
            inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;

            try
            {
                if (_videoContext == null || _videoProcessor == null || _outputView == null || inputView == null || _swapChain == null)
                {
                    return;
                }

                _vpStreamArray[0] = new VideoProcessorStream { Enable = true, InputSurface = inputView };
                var renderStart = Stopwatch.GetTimestamp();
                var bltResult = _videoContext.VideoProcessorBlt(_videoProcessor, _outputView, _outputFrameIndex++, 1, _vpStreamArray);
                renderTicks += Stopwatch.GetTimestamp() - renderStart;
                if (bltResult.Failure)
                {
                    throw new InvalidOperationException($"VideoProcessorBlt failed: 0x{bltResult.Code:X8}.");
                }

                PresentAndTrackFrame(
                    frame,
                    "VideoProcessor",
                    "D3D11 preview first frame rendered.",
                    totalStart,
                    inputUploadTicks,
                    renderTicks,
                    ref presentTicks);
            }
            finally
            {
                if (disposeInputView)
                {
                    inputView?.Dispose();
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _inNativeCall, 0);
        }
    }

    private void RenderNv12WithShader(PendingFrame frame)
    {
        var totalStart = Stopwatch.GetTimestamp();
        long inputUploadTicks = 0;
        long renderTicks = 0;
        long presentTicks = 0;

        if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
        {
            return;
        }
        Interlocked.Exchange(ref _inNativeCall, 1);
        try
        {
            if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
            {
                return;
            }

            if (_device == null || _deviceContext == null || _swapChain == null)
            {
                return;
            }

            if (_fullscreenVS == null ||
                _nv12PS == null ||
                _linearSampler == null ||
                frame.D3DTextureYObject == null ||
                frame.D3DTextureUVObject == null)
            {
                return;
            }

            var inputStart = Stopwatch.GetTimestamp();
            if (!TryEnsureNv12ShaderResources(frame))
            {
                return;
            }
            inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;

            EnsureSwapChainRTV();
            if (_swapChainRTV == null || _nv12YSRV == null || _nv12UVSRV == null)
            {
                return;
            }

            var viewport = ComputeLetterboxViewport(frame.Width, frame.Height);

            _rtvArray[0] = _swapChainRTV;
            _deviceContext.OMSetRenderTargets(1, _rtvArray, null);
            _deviceContext.ClearRenderTargetView(_swapChainRTV, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
            _viewportArray[0] = viewport;
            _deviceContext.RSSetViewports(1, _viewportArray);
            _deviceContext.IASetInputLayout(null);
            _deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _deviceContext.VSSetShader(_fullscreenVS, EmptyClassInstances, 0);
            _deviceContext.PSSetShader(_nv12PS, EmptyClassInstances, 0);
            _samplerArray[0] = _linearSampler!;
            _deviceContext.PSSetSamplers(0, 1, _samplerArray);
            _srvArray2[0] = _nv12YSRV;
            _srvArray2[1] = _nv12UVSRV;
            _deviceContext.PSSetShaderResources(0, 2, _srvArray2);
            UpdateViewportConstantBuffer(viewport);

            var renderStart = Stopwatch.GetTimestamp();
            _deviceContext.Draw(3, 0);
            renderTicks += Stopwatch.GetTimestamp() - renderStart;
            _deviceContext.PSSetShaderResources(0, 2, _srvNullArray2);

            PresentAndTrackFrame(
                frame,
                PreviewShaderSources.RendererModeNv12,
                "D3D11 preview first SDR frame rendered via NV12 shader.",
                totalStart,
                inputUploadTicks,
                renderTicks,
                ref presentTicks);
        }
        finally
        {
            Interlocked.Exchange(ref _inNativeCall, 0);
        }
    }

    private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)
    {
        var totalStart = Stopwatch.GetTimestamp();
        long inputUploadTicks = 0;
        long renderTicks = 0;
        long presentTicks = 0;

        if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
        {
            return;
        }
        Interlocked.Exchange(ref _inNativeCall, 1);
        try
        {
            if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
            {
                return;
            }

            if (_device == null || _deviceContext == null || _swapChain == null)
            {
                return;
            }

            if (_fullscreenVS == null || pixelShader == null || _linearSampler == null)
            {
                return;
            }

            var inputStart = Stopwatch.GetTimestamp();
            EnsureHdrInputResources(frame.Width, frame.Height);
            if (_hdrInputTexture == null ||
                _hdrStagingTexture == null ||
                _hdrYPlaneSRV == null ||
                _hdrUVPlaneSRV == null)
            {
                return;
            }
            inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;

            if (frame.D3DTexture != null)
            {
                inputStart = Stopwatch.GetTimestamp();
                var srcDesc = frame.D3DTexture.Description;
                var planeOffset = (int)(srcDesc.ArraySize * Math.Max(1, srcDesc.MipLevels));

                _deviceContext.CopySubresourceRegion(_hdrInputTexture, 0, 0, 0, 0,
                    frame.D3DTexture, (uint)frame.D3DSubresourceIndex);

                _deviceContext.CopySubresourceRegion(_hdrInputTexture, 1, 0, 0, 0,
                    frame.D3DTexture, (uint)(frame.D3DSubresourceIndex + planeOffset));
                inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;
            }
            else if (frame.RawData != null)
            {
                inputStart = Stopwatch.GetTimestamp();
                if (!UploadRawFrameToTexture(frame.RawData, frame.RawDataLength, frame.Width, frame.Height, true, _hdrStagingTexture!, _hdrInputTexture!))
                {
                    return;
                }
                inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;
            }
            else if (frame.FrameLease != null)
            {
                inputStart = Stopwatch.GetTimestamp();
                if (!UploadRawFrameToTexture(frame.FrameLease.Memory.Span, frame.Width, frame.Height, true, _hdrStagingTexture!, _hdrInputTexture!))
                {
                    return;
                }
                inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;
            }
            else
            {
                return;
            }

            EnsureSwapChainRTV();
            if (_swapChainRTV == null)
            {
                return;
            }

            var viewport = ComputeLetterboxViewport(frame.Width, frame.Height);

            _rtvArray[0] = _swapChainRTV;
            _deviceContext.OMSetRenderTargets(1, _rtvArray, null);
            _deviceContext.ClearRenderTargetView(_swapChainRTV, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
            _viewportArray[0] = viewport;
            _deviceContext.RSSetViewports(1, _viewportArray);
            _deviceContext.IASetInputLayout(null);
            _deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _deviceContext.VSSetShader(_fullscreenVS, EmptyClassInstances, 0);
            _deviceContext.PSSetShader(pixelShader, EmptyClassInstances, 0);
            _samplerArray[0] = _linearSampler!;
            _deviceContext.PSSetSamplers(0, 1, _samplerArray);
            _srvArray2[0] = _hdrYPlaneSRV!;
            _srvArray2[1] = _hdrUVPlaneSRV!;
            _deviceContext.PSSetShaderResources(0, 2, _srvArray2);

            UpdateViewportConstantBuffer(viewport);

            var renderStart = Stopwatch.GetTimestamp();
            _deviceContext.Draw(3, 0);
            renderTicks += Stopwatch.GetTimestamp() - renderStart;
            _deviceContext.PSSetShaderResources(0, 2, _srvNullArray2);

            var rendererMode = ReferenceEquals(pixelShader, _hdrPassthroughPS)
                ? RendererModeHdrPassthrough
                : PreviewShaderSources.RendererModeHdr;
            var mode = ReferenceEquals(pixelShader, _hdrPassthroughPS)
                ? "passthrough" : "tonemapping";
            PresentAndTrackFrame(
                frame,
                rendererMode,
                $"D3D11 preview first HDR frame rendered via {mode} shader.",
                totalStart,
                inputUploadTicks,
                renderTicks,
                ref presentTicks);
        }
        finally
        {
            Interlocked.Exchange(ref _inNativeCall, 0);
        }
    }

    private void PresentAndTrackFrame(
        PendingFrame frame,
        string rendererMode,
        string firstFrameMessage,
        long totalStart,
        long inputUploadTicks,
        long renderTicks,
        ref long presentTicks)
    {
        var swapChain = _swapChain ?? throw new InvalidOperationException("Swap chain is not initialized.");

        TryCaptureFrameBeforePresent(rendererMode);
        var presentStart = Stopwatch.GetTimestamp();
        var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);
        var presentEnd = Stopwatch.GetTimestamp();
        presentTicks += presentEnd - presentStart;
        if (presentResult.Failure)
        {
            throw new InvalidOperationException($"SwapChain.Present failed: 0x{presentResult.Code:X8}.");
        }

        if (Interlocked.Exchange(ref _firstFrameRaised, 1) == 0)
        {
            Logger.Log(firstFrameMessage);
            if (!_dispatcherQueue.TryEnqueue(() => FirstFrameRendered?.Invoke()))
            {
                Logger.Log("D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
            }
        }

        Interlocked.Increment(ref _framesRendered);
        var presentIntervalMs = TrackPresentCadence(frame.CountForPresentCadence);
        TrackDxgiFrameStatistics();
        var estimatedVisibleTick = EstimateVisibleTick(presentEnd);
        TrackFramePresented(frame, presentEnd, estimatedVisibleTick);
        TrackPipelineLatency(frame.ArrivalTick, estimatedVisibleTick);
        var totalTicks = Stopwatch.GetTimestamp() - totalStart;
        TrackRenderCpuTiming(inputUploadTicks, renderTicks, presentTicks, totalTicks);
        RecordSlowFrameDiagnostic(frame, presentIntervalMs, inputUploadTicks, renderTicks, presentTicks, totalTicks, presentEnd, estimatedVisibleTick);
    }

    private bool TryEnsureNv12ShaderResources(PendingFrame frame)
    {
        if (_device == null)
        {
            return false;
        }

        if (frame.D3DTextureY == _nv12LastYPtr &&
            frame.D3DTextureUV == _nv12LastUVPtr &&
            _nv12YSRV != null &&
            _nv12UVSRV != null)
        {
            return true;
        }

        _nv12YSRV?.Dispose();
        _nv12YSRV = null;
        _nv12UVSRV?.Dispose();
        _nv12UVSRV = null;
        _nv12LastYPtr = IntPtr.Zero;
        _nv12LastUVPtr = IntPtr.Zero;

        try
        {
            var yTexture = frame.D3DTextureYObject;
            var uvTexture = frame.D3DTextureUVObject;
            if (yTexture == null || uvTexture == null)
            {
                return false;
            }

            _nv12YSRV = _device.CreateShaderResourceView(
                yTexture,
                new ShaderResourceViewDescription(yTexture, ShaderResourceViewDimension.Texture2D, Format.R8_UNorm, 0, 1));
            _nv12UVSRV = _device.CreateShaderResourceView(
                uvTexture,
                new ShaderResourceViewDescription(uvTexture, ShaderResourceViewDimension.Texture2D, Format.R8G8_UNorm, 0, 1));

            _nv12LastYPtr = frame.D3DTextureY;
            _nv12LastUVPtr = frame.D3DTextureUV;
            return true;
        }
        catch (Exception ex)
        {
            _nv12YSRV?.Dispose();
            _nv12YSRV = null;
            _nv12UVSRV?.Dispose();
            _nv12UVSRV = null;
            _nv12LastYPtr = IntPtr.Zero;
            _nv12LastUVPtr = IntPtr.Zero;
            Logger.Log($"D3D11 preview NV12 SRV creation failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            return false;
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(D3D11PreviewRenderer));
        }
    }
}

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Microsoft.UI.Dispatching;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;

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
            Volatile.Write(ref _rendererMode, RendererModeNv12Shader);
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
                Volatile.Write(ref _rendererMode, RendererModeHdrShader);
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
            _deviceContext.VSSetShader(_fullscreenVS, Array.Empty<ID3D11ClassInstance>(), 0);
            _deviceContext.PSSetShader(_nv12PS, Array.Empty<ID3D11ClassInstance>(), 0);
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
                RendererModeNv12Shader,
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
            _deviceContext.VSSetShader(_fullscreenVS, Array.Empty<ID3D11ClassInstance>(), 0);
            _deviceContext.PSSetShader(pixelShader, Array.Empty<ID3D11ClassInstance>(), 0);
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
                : RendererModeHdrShader;
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

    private Viewport ComputeLetterboxViewport(int sourceWidth, int sourceHeight)
    {
        var outputWidth = _configuredOutputWidth > 0
            ? _configuredOutputWidth
            : Math.Max(1, Volatile.Read(ref _startupWidth));
        var outputHeight = _configuredOutputHeight > 0
            ? _configuredOutputHeight
            : Math.Max(1, Volatile.Read(ref _startupHeight));
        var destinationRect = ComputeLetterboxRect(sourceWidth, sourceHeight, outputWidth, outputHeight);
        return new Viewport(
            destinationRect.Left,
            destinationRect.Top,
            Math.Max(1, destinationRect.Right - destinationRect.Left),
            Math.Max(1, destinationRect.Bottom - destinationRect.Top),
            0.0f,
            1.0f);
    }

    private void UpdateViewportConstantBuffer(Viewport viewport)
    {
        if (_viewportCB == null || _deviceContext == null)
        {
            return;
        }

        var mapped = _deviceContext.Map(_viewportCB, 0, MapMode.WriteDiscard);
        unsafe
        {
            var data = (float*)mapped.DataPointer;
            data[0] = viewport.X;
            data[1] = viewport.Y;
            data[2] = viewport.Width;
            data[3] = viewport.Height;
        }

        _deviceContext.Unmap(_viewportCB, 0);
        _cbArray[0] = _viewportCB;
        _deviceContext.PSSetConstantBuffers(0, 1, _cbArray);
    }

    private void TryCaptureFrameBeforePresent(string rendererMode)
    {
        var request = Interlocked.Exchange(ref _frameCaptureRequest, null);
        if (request == null)
        {
            return;
        }

        var requestedOutputPath = request.Task.AsyncState as string;
        Interlocked.Exchange(ref _frameCaptureOutputPath, null);
        var outputPath = string.IsNullOrWhiteSpace(requestedOutputPath)
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}.bmp")
            : requestedOutputPath;

        try
        {
            if (_device == null || _deviceContext == null || _swapChain == null)
            {
                request.TrySetResult(CreateFrameCaptureError("Renderer device state is unavailable.", rendererMode));
                return;
            }

            var fullOutputPath = Path.GetFullPath(outputPath);
            var isPng = fullOutputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            if (isPng && Interlocked.CompareExchange(ref _frameCaptureEncodeInProgress, 1, 0) != 0)
            {
                request.TrySetResult(CreateFrameCaptureError("A preview frame capture is already pending.", rendererMode));
                return;
            }

            ID3D11Texture2D? backBuffer = _swapChainBackBuffer;
            var disposeBackBuffer = false;
            if (backBuffer == null)
            {
                backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
                disposeBackBuffer = true;
            }

            try
            {
                if (backBuffer == null)
                {
                    throw new InvalidOperationException("Swap chain back buffer is unavailable.");
                }

                var backBufferDescription = backBuffer.Description;
                var width = checked((int)backBufferDescription.Width);
                var height = checked((int)backBufferDescription.Height);
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Swap chain back buffer has invalid dimensions.");
                }

                if (_captureStagingTexture == null ||
                    _captureStagingWidth != width ||
                    _captureStagingHeight != height)
                {
                    _captureStagingTexture?.Dispose();
                    _captureStagingTexture = _device.CreateTexture2D(new Texture2DDescription(
                        backBufferDescription.Format,
                        (uint)width,
                        (uint)height,
                        1,
                        1,
                        BindFlags.None,
                        ResourceUsage.Staging,
                        CpuAccessFlags.Read,
                        1,
                        0,
                        ResourceOptionFlags.None));
                    _captureStagingWidth = width;
                    _captureStagingHeight = height;
                }

                var stagingTexture = _captureStagingTexture;
                _deviceContext.CopyResource(stagingTexture, backBuffer);

                _deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None, out var mapped);
                PreviewFrameCaptureResult captureResult;
                byte[]? pngFrameBuffer = null;
                var pngSourceRowBytes = checked(width * 4);
                try
                {
                    if (isPng)
                    {
                        pngFrameBuffer = CopyMappedFrameToBuffer(mapped, height, pngSourceRowBytes);
                        captureResult = default!;
                    }
                    else
                    {
                        captureResult = CaptureMappedFrameToBmp(
                            mapped,
                            width,
                            height,
                            fullOutputPath,
                            rendererMode,
                            backBufferDescription.Format);
                    }
                }
                finally
                {
                    _deviceContext.Unmap(stagingTexture, 0);
                }

                if (isPng)
                {
                    var pngBuffer = pngFrameBuffer!;
                    _ = Task.Run(
                        () =>
                        {
                            try
                            {
                                var pngCaptureResult = CaptureFrameBufferTo16BitPng(
                                    pngBuffer,
                                    pngSourceRowBytes,
                                    width,
                                    height,
                                    fullOutputPath,
                                    rendererMode,
                                    backBufferDescription.Format);
                                request.TrySetResult(pngCaptureResult);
                                Logger.Log(
                                    $"PREVIEW_FRAME_CAPTURE_RESULT ok={pngCaptureResult.Succeeded} renderer={pngCaptureResult.RendererMode} path={pngCaptureResult.FilePath ?? "n/a"} width={pngCaptureResult.CapturedWidth} height={pngCaptureResult.CapturedHeight} avgLum={pngCaptureResult.AverageLuminance:0.00} pureBlackPct={pngCaptureResult.PureBlackPercent:0.00}");
                            }
                            catch (Exception ex)
                            {
                                request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
                                Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
                            }
                            finally
                            {
                                Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);
                            }
                        });
                    return;
                }

                request.TrySetResult(captureResult);
                Logger.Log(
                    $"PREVIEW_FRAME_CAPTURE_RESULT ok={captureResult.Succeeded} renderer={captureResult.RendererMode} path={captureResult.FilePath ?? "n/a"} width={captureResult.CapturedWidth} height={captureResult.CapturedHeight} avgLum={captureResult.AverageLuminance:0.00} pureBlackPct={captureResult.PureBlackPercent:0.00}");
            }
            finally
            {
                if (disposeBackBuffer)
                {
                    backBuffer?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);
            request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
            Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(
        MappedSubresource mapped,
        int width,
        int height,
        string outputPath,
        string rendererMode,
        Format backBufferFormat = Format.B8G8R8A8_UNorm)
    {
        const int bitmapFileHeaderSize = 14;
        const int bitmapInfoHeaderSize = 40;
        const int bitmapColorMaskSize = 12;
        const int bytesPerPixel = 4;

        var rowBytes = checked(width * bytesPerPixel);
        var imageSize = checked(rowBytes * height);
        var pixelDataOffset = bitmapFileHeaderSize + bitmapInfoHeaderSize + bitmapColorMaskSize;
        var fileSize = checked(pixelDataOffset + imageSize);

        var histogram = new int[16];
        var rowAllBlack = new bool[height];
        var columnAllBlack = new bool[width];
        Array.Fill(rowAllBlack, true);
        Array.Fill(columnAllBlack, true);

        long sumR = 0;
        long sumG = 0;
        long sumB = 0;
        double sumLuminance = 0;
        double minLuminance = 255;
        double maxLuminance = 0;
        long nearBlackCount = 0;
        long nearWhiteCount = 0;
        long pureBlackCount = 0;
        var totalPixels = (long)width * height;

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowBytes);
        try
        {
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new BinaryWriter(fileStream, System.Text.Encoding.ASCII, leaveOpen: false);
            WriteBitmapHeaders(writer, fileSize, pixelDataOffset, width, height, imageSize);

            for (var y = 0; y < height; y++)
            {
                var sourceRow = IntPtr.Add(mapped.DataPointer, checked(y * (int)mapped.RowPitch));
                Marshal.Copy(sourceRow, rowBuffer, 0, rowBytes);

                if (backBufferFormat == Format.R10G10B10A2_UNorm)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var offset = x * bytesPerPixel;
                        var pixel = (uint)(rowBuffer[offset] |
                                           (rowBuffer[offset + 1] << 8) |
                                           (rowBuffer[offset + 2] << 16) |
                                           (rowBuffer[offset + 3] << 24));
                        rowBuffer[offset] = (byte)(((pixel >> 20) & 0x3FFu) >> 2);
                        rowBuffer[offset + 1] = (byte)(((pixel >> 10) & 0x3FFu) >> 2);
                        rowBuffer[offset + 2] = (byte)((pixel & 0x3FFu) >> 2);
                        rowBuffer[offset + 3] = 255;
                    }
                }

                writer.Write(rowBuffer, 0, rowBytes);

                var isRowPureBlack = true;
                for (var x = 0; x < width; x++)
                {
                    var offset = x * bytesPerPixel;
                    var b = rowBuffer[offset];
                    var g = rowBuffer[offset + 1];
                    var r = rowBuffer[offset + 2];

                    sumR += r;
                    sumG += g;
                    sumB += b;

                    var luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
                    sumLuminance += luminance;
                    if (luminance < minLuminance)
                    {
                        minLuminance = luminance;
                    }
                    if (luminance > maxLuminance)
                    {
                        maxLuminance = luminance;
                    }

                    if (luminance < 16.0)
                    {
                        nearBlackCount++;
                    }
                    if (luminance > 240.0)
                    {
                        nearWhiteCount++;
                    }

                    var isPureBlack = r == 0 && g == 0 && b == 0;
                    if (isPureBlack)
                    {
                        pureBlackCount++;
                    }
                    else
                    {
                        isRowPureBlack = false;
                        columnAllBlack[x] = false;
                    }

                    var histogramIndex = (int)(luminance / 16.0);
                    if (histogramIndex > 15)
                    {
                        histogramIndex = 15;
                    }
                    histogram[histogramIndex]++;
                }

                rowAllBlack[y] = isRowPureBlack;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }

        var letterboxTopRows = CountLeadingBlackEdges(rowAllBlack);
        var letterboxBottomRows = letterboxTopRows == height ? 0 : CountTrailingBlackEdges(rowAllBlack);
        var pillarboxLeftCols = CountLeadingBlackEdges(columnAllBlack);
        var pillarboxRightCols = pillarboxLeftCols == width ? 0 : CountTrailingBlackEdges(columnAllBlack);

        var contentWidth = Math.Max(0, width - pillarboxLeftCols - pillarboxRightCols);
        var contentHeight = Math.Max(0, height - letterboxTopRows - letterboxBottomRows);
        var contentAspectRatio = contentHeight > 0
            ? (double)contentWidth / contentHeight
            : 0.0;

        var averageR = totalPixels > 0 ? (double)sumR / totalPixels : 0.0;
        var averageG = totalPixels > 0 ? (double)sumG / totalPixels : 0.0;
        var averageB = totalPixels > 0 ? (double)sumB / totalPixels : 0.0;
        var averageLuminance = totalPixels > 0 ? sumLuminance / totalPixels : 0.0;
        var nearBlackPercent = totalPixels > 0 ? (nearBlackCount * 100.0) / totalPixels : 0.0;
        var nearWhitePercent = totalPixels > 0 ? (nearWhiteCount * 100.0) / totalPixels : 0.0;
        var pureBlackPercent = totalPixels > 0 ? (pureBlackCount * 100.0) / totalPixels : 0.0;

        return new PreviewFrameCaptureResult
        {
            Succeeded = true,
            Message = "Preview frame captured.",
            FilePath = outputPath,
            CapturedWidth = width,
            CapturedHeight = height,
            RendererMode = rendererMode,
            AverageR = averageR,
            AverageG = averageG,
            AverageB = averageB,
            AverageLuminance = averageLuminance,
            MinLuminance = minLuminance,
            MaxLuminance = maxLuminance,
            NearBlackPercent = nearBlackPercent,
            NearWhitePercent = nearWhitePercent,
            PureBlackPercent = pureBlackPercent,
            LetterboxTopRows = letterboxTopRows,
            LetterboxBottomRows = letterboxBottomRows,
            PillarboxLeftCols = pillarboxLeftCols,
            PillarboxRightCols = pillarboxRightCols,
            ContentWidth = contentWidth,
            ContentHeight = contentHeight,
            ContentAspectRatio = contentAspectRatio,
            LuminanceHistogram = histogram,
            TotalPixels = totalPixels
        };
    }

    private static byte[] CopyMappedFrameToBuffer(MappedSubresource mapped, int height, int sourceRowBytes)
    {
        var sourceBuffer = new byte[checked(sourceRowBytes * height)];
        for (var y = 0; y < height; y++)
        {
            var sourceRow = IntPtr.Add(mapped.DataPointer, checked(y * (int)mapped.RowPitch));
            Marshal.Copy(sourceRow, sourceBuffer, checked(y * sourceRowBytes), sourceRowBytes);
        }

        return sourceBuffer;
    }

    private static PreviewFrameCaptureResult CaptureFrameBufferTo16BitPng(
        byte[] sourceBuffer,
        int sourceRowBytes,
        int width,
        int height,
        string outputPath,
        string rendererMode,
        Format backBufferFormat)
    {
        const int sourceBytesPerPixel = 4;
        const int pngBytesPerPixel = 6;
        var pngRowBytes = checked(1 + (width * pngBytesPerPixel));

        var histogram = new int[16];
        var rowAllBlack = new bool[height];
        var columnAllBlack = new bool[width];
        Array.Fill(rowAllBlack, true);
        Array.Fill(columnAllBlack, true);

        long sumR = 0;
        long sumG = 0;
        long sumB = 0;
        double sumLuminance = 0;
        double minLuminance = 255;
        double maxLuminance = 0;
        long nearBlackCount = 0;
        long nearWhiteCount = 0;
        long pureBlackCount = 0;
        var totalPixels = (long)width * height;

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sourceRowBuffer = ArrayPool<byte>.Shared.Rent(sourceRowBytes);
        var pngRowBuffer = ArrayPool<byte>.Shared.Rent(pngRowBytes);
        try
        {
            using (var compressedDataStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedDataStream, CompressionLevel.Fastest, leaveOpen: true))
                {
                    for (var y = 0; y < height; y++)
                    {
                        var sourceRowOffset = checked(y * sourceRowBytes);
                        Buffer.BlockCopy(sourceBuffer, sourceRowOffset, sourceRowBuffer, 0, sourceRowBytes);

                        pngRowBuffer[0] = 0;
                        var pngOffset = 1;
                        var isRowPureBlack = true;

                        for (var x = 0; x < width; x++)
                        {
                            var offset = x * sourceBytesPerPixel;
                            byte r8;
                            byte g8;
                            byte b8;
                            ushort r16;
                            ushort g16;
                            ushort b16;

                            if (backBufferFormat == Format.R10G10B10A2_UNorm)
                            {
                                var pixel = (uint)(sourceRowBuffer[offset] |
                                                   (sourceRowBuffer[offset + 1] << 8) |
                                                   (sourceRowBuffer[offset + 2] << 16) |
                                                   (sourceRowBuffer[offset + 3] << 24));
                                var r10 = pixel & 0x3FFu;
                                var g10 = (pixel >> 10) & 0x3FFu;
                                var b10 = (pixel >> 20) & 0x3FFu;

                                r8 = (byte)(r10 >> 2);
                                g8 = (byte)(g10 >> 2);
                                b8 = (byte)(b10 >> 2);
                                r16 = (ushort)((r10 << 6) | (r10 >> 4));
                                g16 = (ushort)((g10 << 6) | (g10 >> 4));
                                b16 = (ushort)((b10 << 6) | (b10 >> 4));
                            }
                            else if (backBufferFormat == Format.B8G8R8A8_UNorm)
                            {
                                b8 = sourceRowBuffer[offset];
                                g8 = sourceRowBuffer[offset + 1];
                                r8 = sourceRowBuffer[offset + 2];
                                b16 = (ushort)((b8 << 8) | b8);
                                g16 = (ushort)((g8 << 8) | g8);
                                r16 = (ushort)((r8 << 8) | r8);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Preview PNG capture does not support back buffer format {backBufferFormat}.");
                            }

                            pngRowBuffer[pngOffset++] = (byte)(r16 >> 8);
                            pngRowBuffer[pngOffset++] = (byte)r16;
                            pngRowBuffer[pngOffset++] = (byte)(g16 >> 8);
                            pngRowBuffer[pngOffset++] = (byte)g16;
                            pngRowBuffer[pngOffset++] = (byte)(b16 >> 8);
                            pngRowBuffer[pngOffset++] = (byte)b16;

                            sumR += r8;
                            sumG += g8;
                            sumB += b8;

                            var luminance = (0.299 * r8) + (0.587 * g8) + (0.114 * b8);
                            sumLuminance += luminance;
                            if (luminance < minLuminance)
                            {
                                minLuminance = luminance;
                            }
                            if (luminance > maxLuminance)
                            {
                                maxLuminance = luminance;
                            }

                            if (luminance < 16.0)
                            {
                                nearBlackCount++;
                            }
                            if (luminance > 240.0)
                            {
                                nearWhiteCount++;
                            }

                            var isPureBlack = r8 == 0 && g8 == 0 && b8 == 0;
                            if (isPureBlack)
                            {
                                pureBlackCount++;
                            }
                            else
                            {
                                isRowPureBlack = false;
                                columnAllBlack[x] = false;
                            }

                            var histogramIndex = (int)(luminance / 16.0);
                            if (histogramIndex > 15)
                            {
                                histogramIndex = 15;
                            }

                            histogram[histogramIndex]++;
                        }

                        rowAllBlack[y] = isRowPureBlack;
                        zlibStream.Write(pngRowBuffer, 0, pngRowBytes);
                    }
                }

                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new BinaryWriter(fileStream, System.Text.Encoding.ASCII, leaveOpen: false);

                writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

                var ihdrData = new byte[13];
                BinaryPrimitives.WriteUInt32BigEndian(ihdrData.AsSpan(0, 4), checked((uint)width));
                BinaryPrimitives.WriteUInt32BigEndian(ihdrData.AsSpan(4, 4), checked((uint)height));
                ihdrData[8] = 16;
                ihdrData[9] = 2;
                ihdrData[10] = 0;
                ihdrData[11] = 0;
                ihdrData[12] = 0;

                WritePngChunk(writer, new byte[] { (byte)'I', (byte)'H', (byte)'D', (byte)'R' }, ihdrData);
                if (compressedDataStream.TryGetBuffer(out var compressedData))
                {
                    WritePngChunk(
                        writer,
                        new byte[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' },
                        compressedData.Array!,
                        compressedData.Offset,
                        checked((int)compressedDataStream.Length));
                }
                else
                {
                    WritePngChunk(writer, new byte[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' }, compressedDataStream.ToArray());
                }

                WritePngChunk(writer, new byte[] { (byte)'I', (byte)'E', (byte)'N', (byte)'D' }, Array.Empty<byte>());
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pngRowBuffer);
            ArrayPool<byte>.Shared.Return(sourceRowBuffer);
        }

        var letterboxTopRows = CountLeadingBlackEdges(rowAllBlack);
        var letterboxBottomRows = letterboxTopRows == height ? 0 : CountTrailingBlackEdges(rowAllBlack);
        var pillarboxLeftCols = CountLeadingBlackEdges(columnAllBlack);
        var pillarboxRightCols = pillarboxLeftCols == width ? 0 : CountTrailingBlackEdges(columnAllBlack);

        var contentWidth = Math.Max(0, width - pillarboxLeftCols - pillarboxRightCols);
        var contentHeight = Math.Max(0, height - letterboxTopRows - letterboxBottomRows);
        var contentAspectRatio = contentHeight > 0
            ? (double)contentWidth / contentHeight
            : 0.0;

        var averageR = totalPixels > 0 ? (double)sumR / totalPixels : 0.0;
        var averageG = totalPixels > 0 ? (double)sumG / totalPixels : 0.0;
        var averageB = totalPixels > 0 ? (double)sumB / totalPixels : 0.0;
        var averageLuminance = totalPixels > 0 ? sumLuminance / totalPixels : 0.0;
        var nearBlackPercent = totalPixels > 0 ? (nearBlackCount * 100.0) / totalPixels : 0.0;
        var nearWhitePercent = totalPixels > 0 ? (nearWhiteCount * 100.0) / totalPixels : 0.0;
        var pureBlackPercent = totalPixels > 0 ? (pureBlackCount * 100.0) / totalPixels : 0.0;

        return new PreviewFrameCaptureResult
        {
            Succeeded = true,
            Message = "Preview frame captured.",
            FilePath = outputPath,
            CapturedWidth = width,
            CapturedHeight = height,
            RendererMode = rendererMode,
            AverageR = averageR,
            AverageG = averageG,
            AverageB = averageB,
            AverageLuminance = averageLuminance,
            MinLuminance = minLuminance,
            MaxLuminance = maxLuminance,
            NearBlackPercent = nearBlackPercent,
            NearWhitePercent = nearWhitePercent,
            PureBlackPercent = pureBlackPercent,
            LetterboxTopRows = letterboxTopRows,
            LetterboxBottomRows = letterboxBottomRows,
            PillarboxLeftCols = pillarboxLeftCols,
            PillarboxRightCols = pillarboxRightCols,
            ContentWidth = contentWidth,
            ContentHeight = contentHeight,
            ContentAspectRatio = contentAspectRatio,
            LuminanceHistogram = histogram,
            TotalPixels = totalPixels
        };
    }

    private static void WriteBitmapHeaders(
        BinaryWriter writer,
        int fileSize,
        int pixelDataOffset,
        int width,
        int height,
        int imageSize)
    {
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write(pixelDataOffset);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(3);
        writer.Write(imageSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        writer.Write(unchecked((int)0x00FF0000));
        writer.Write(unchecked((int)0x0000FF00));
        writer.Write(unchecked((int)0x000000FF));
    }

    private static uint[] InitPngCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static uint UpdatePngCrc32(uint crc, byte[] buffer, int offset, int length)
    {
        for (var i = offset; i < offset + length; i++)
        {
            crc = PngCrc32Table[(crc ^ buffer[i]) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static void WritePngChunk(BinaryWriter writer, byte[] chunkType, byte[] data)
    {
        WritePngChunk(writer, chunkType, data, 0, data.Length);
    }

    private static void WritePngChunk(BinaryWriter writer, byte[] chunkType, byte[] data, int dataOffset, int dataLength)
    {
        writer.Write(BinaryPrimitives.ReverseEndianness(checked((uint)dataLength)));
        writer.Write(chunkType);
        if (dataLength > 0)
        {
            writer.Write(data, dataOffset, dataLength);
        }

        var crc = 0xFFFFFFFFu;
        crc = UpdatePngCrc32(crc, chunkType, 0, chunkType.Length);
        if (dataLength > 0)
        {
            crc = UpdatePngCrc32(crc, data, dataOffset, dataLength);
        }

        writer.Write(BinaryPrimitives.ReverseEndianness(crc ^ 0xFFFFFFFFu));
    }

    private static int CountLeadingBlackEdges(bool[] values)
    {
        var count = 0;
        while (count < values.Length && values[count])
        {
            count++;
        }

        return count;
    }

    private static int CountTrailingBlackEdges(bool[] values)
    {
        var count = 0;
        while (count < values.Length && values[values.Length - 1 - count])
        {
            count++;
        }

        return count;
    }

    private static PreviewFrameCaptureResult CreateFrameCaptureError(string message, string rendererMode = "Unknown")
    {
        return new PreviewFrameCaptureResult
        {
            Succeeded = false,
            Message = message,
            RendererMode = rendererMode,
            LuminanceHistogram = new int[16]
        };
    }

    private void FailPendingFrameCapture(string message)
    {
        var request = Interlocked.Exchange(ref _frameCaptureRequest, null);
        Interlocked.Exchange(ref _frameCaptureOutputPath, null);
        if (request == null)
        {
            return;
        }

        request.TrySetResult(CreateFrameCaptureError(message));
        Logger.Log($"PREVIEW_FRAME_CAPTURE_ABORTED reason={message}");
    }

    private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)
    {
        inputView = null;
        disposeInputView = false;

        if (frame.D3DTexture != null)
        {
            inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);
            disposeInputView = true;
            return true;
        }

        if (_deviceContext == null || _inputTexture == null || _stagingTexture == null)
        {
            return false;
        }

        if (frame.RawData != null)
        {
            if (!UploadRawFrameToTexture(frame.RawData, frame.RawDataLength, frame.Width, frame.Height, frame.IsHdr, _stagingTexture, _inputTexture))
            {
                return false;
            }
        }
        else if (frame.FrameLease != null)
        {
            if (!UploadRawFrameToTexture(frame.FrameLease.Memory.Span, frame.Width, frame.Height, frame.IsHdr, _stagingTexture, _inputTexture))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        inputView = _inputView;
        return inputView != null;
    }

    private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)
    {
        if (_videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 preview pipeline is not ready for external texture input.");
        }

        var textureDescription = texture.Description;
        var mipLevels = Math.Max(1, (int)textureDescription.MipLevels);
        var arraySize = Math.Max(1, (int)textureDescription.ArraySize);
        var safeSubresource = Math.Max(0, subresourceIndex);
        var arraySlice = Math.Clamp(safeSubresource / mipLevels, 0, arraySize - 1);
        var mipSlice = Math.Clamp(safeSubresource % mipLevels, 0, mipLevels - 1);
        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView
            {
                MipSlice = (uint)mipSlice,
                ArraySlice = (uint)arraySlice
            }
        };

        return _videoDevice.CreateVideoProcessorInputView(texture, _videoProcessorEnumerator, inputViewDescription);
    }

    private unsafe bool UploadRawFrameToTexture(
        byte[] data, int dataLength, int width, int height, bool isHdr,
        ID3D11Texture2D stagingTexture, ID3D11Texture2D inputTexture)
        => UploadRawFrameToTexture(data.AsSpan(0, Math.Min(dataLength, data.Length)), width, height, isHdr, stagingTexture, inputTexture);

    private unsafe bool UploadRawFrameToTexture(
        ReadOnlySpan<byte> data, int width, int height, bool isHdr,
        ID3D11Texture2D stagingTexture, ID3D11Texture2D inputTexture)
    {
        if (_deviceContext == null)
        {
            return false;
        }

        var rowBytes = isHdr ? width * 2 : width;
        var uvRows = height / 2;
        var expectedBytes = (rowBytes * height) + (rowBytes * uvRows);
        if (data.Length < expectedBytes)
        {
            Logger.Log(
                $"D3D11 preview raw frame too small: expected={expectedBytes} actual={data.Length} hdr={isHdr}.");
            return false;
        }

        if (TryUpdateRawFrameTexture(data, inputTexture, rowBytes, expectedBytes))
        {
            return true;
        }

        return UploadRawFrameViaStaging(data, width, height, rowBytes, uvRows, stagingTexture, inputTexture);
    }

    private unsafe bool TryUpdateRawFrameTexture(
        ReadOnlySpan<byte> data,
        ID3D11Texture2D inputTexture,
        int rowBytes,
        int expectedBytes)
    {
        if (_deviceContext == null)
        {
            return false;
        }

        try
        {
            fixed (byte* srcStart = data)
            {
                _deviceContext.UpdateSubresource(
                    inputTexture,
                    0,
                    null,
                    (IntPtr)srcStart,
                    (uint)rowBytes,
                    (uint)expectedBytes);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!_loggedDirectUploadFallback)
            {
                _loggedDirectUploadFallback = true;
                Logger.Log($"D3D11 preview direct texture update failed; falling back to staging upload. type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            }

            return false;
        }
    }

    private unsafe bool UploadRawFrameViaStaging(
        ReadOnlySpan<byte> data,
        int width,
        int height,
        int rowBytes,
        int uvRows,
        ID3D11Texture2D stagingTexture,
        ID3D11Texture2D inputTexture)
    {
        if (_deviceContext == null)
        {
            return false;
        }

        fixed (byte* srcStart = data)
        {
            var srcY = srcStart;
            var srcUv = srcStart + (rowBytes * height);

            _deviceContext.Map(stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None, out var mapped);
            try
            {
                var dstY = (byte*)mapped.DataPointer;
                var dstUv = dstY + (mapped.RowPitch * height);

                for (var row = 0; row < height; row++)
                {
                    Buffer.MemoryCopy(
                        srcY + (row * rowBytes),
                        dstY + (row * mapped.RowPitch),
                        mapped.RowPitch,
                        rowBytes);
                }

                for (var row = 0; row < uvRows; row++)
                {
                    Buffer.MemoryCopy(
                        srcUv + (row * rowBytes),
                        dstUv + (row * mapped.RowPitch),
                        mapped.RowPitch,
                        rowBytes);
                }
            }
            finally
            {
                _deviceContext.Unmap(stagingTexture, 0);
            }
        }

        _deviceContext.CopyResource(inputTexture, stagingTexture);
        return true;
    }

    private void InitializeD3D()
    {
        CleanupD3DResources();

        var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);
        if (!sharedDeviceActive)
        {
            CreateRendererOwnedDevice(out featureLevel);
        }

        if (_device == null || _deviceContext == null)
        {
            throw new InvalidOperationException("D3D11 device initialization did not produce a valid device/context.");
        }

        var device = _device;
        var deviceContext = _deviceContext;
        _device3?.Dispose();
        _device3 = device.QueryInterfaceOrNull<ID3D11Device3>();
        Interlocked.Exchange(ref _sharedDeviceActive, sharedDeviceActive ? 1 : 0);

        _multithread = device.QueryInterfaceOrNull<ID3D11Multithread>();
        _multithread?.SetMultithreadProtected(true);

        // Keep the compositor queue shallow. This defaults to 2 for latency,
        // but is env-tunable while we measure DWM pacing behavior.
        using var dxgiDevice1 = device.QueryInterfaceOrNull<IDXGIDevice1>();
        dxgiDevice1?.SetMaximumFrameLatency((uint)_dxgiMaxFrameLatency);

        _videoDevice = device.QueryInterfaceOrNull<ID3D11VideoDevice>();
        _videoContext = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext>();
        _videoContext1 = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext1>();
        if (_videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 video interfaces are unavailable.");
        }

        var factoryResult = DXGI.CreateDXGIFactory2(false, out _factory);
        if (factoryResult.Failure || _factory == null)
        {
            throw new InvalidOperationException($"CreateDXGIFactory2 failed: 0x{factoryResult.Code:X8}.");
        }

        var pixelWidth = Math.Max(1, Volatile.Read(ref _startupWidth));
        var pixelHeight = Math.Max(1, Volatile.Read(ref _startupHeight));

        var swapChainFormat = Format.B8G8R8A8_UNorm;
        _hdrCapableSwapChain = false;
        _swapChain3?.Dispose();
        _swapChain3 = null;

        if (_configuredHdr)
        {
            swapChainFormat = Format.R10G10B10A2_UNorm;
        }

        var swapChainFlags = _waitableSwapChainEnabled
            ? SwapChainFlags.FrameLatencyWaitableObject
            : SwapChainFlags.None;

        var swapChainDescription = new SwapChainDescription1(
            (uint)pixelWidth,
            (uint)pixelHeight,
            swapChainFormat,
            false,
            Usage.RenderTargetOutput,
            (uint)_swapChainBufferCount,
            Scaling.Stretch,
            SwapEffect.FlipSequential,
            AlphaMode.Ignore,
            swapChainFlags);

        _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
        ConfigureFrameLatencyWaitableObject();
        if (_configuredHdr)
        {
            _swapChain3 = _swapChain.QueryInterfaceOrNull<IDXGISwapChain3>();
            if (_swapChain3 != null)
            {
                var srgbSupport = _swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG22NoneP709);
                var hdr10Support = _swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020);

                var srgbOk = (srgbSupport & SwapChainColorSpaceSupportFlags.Present) != 0;
                var hdr10Ok = (hdr10Support & SwapChainColorSpaceSupportFlags.Present) != 0;

                if (srgbOk && hdr10Ok)
                {
                    _hdrCapableSwapChain = true;
                    var wantHdr = Volatile.Read(ref _hdrPassthroughEnabled) != 0;
                    var initialColorSpace = wantHdr
                        ? ColorSpaceType.RgbFullG2084NoneP2020
                        : ColorSpaceType.RgbFullG22NoneP709;
                    _swapChain3.SetColorSpace1(initialColorSpace);
                    _outputColorSpaceLabel = wantHdr ? "HDR10-PQ (BT.2020)" : "sRGB (BT.709)";
                    Interlocked.Exchange(ref _swapChainColorSpaceDirty, 0);
                    Logger.Log($"D3D11 preview HDR-capable swap chain: srgb={srgbOk} hdr10={hdr10Ok} initial={initialColorSpace}.");
                }
                else
                {
                    Logger.Log($"D3D11 preview HDR color space check: srgb={srgbOk}({srgbSupport}) hdr10={hdr10Ok}({hdr10Support}). Falling back to B8G8R8A8.");
                    _swapChain3.Dispose();
                    _swapChain3 = null;
                    _swapChain2?.Dispose();
                    _swapChain2 = null;
                    _frameLatencyWaitHandle = IntPtr.Zero;
                    _swapChain.Dispose();
                    swapChainDescription = new SwapChainDescription1(
                        (uint)pixelWidth,
                        (uint)pixelHeight,
                        Format.B8G8R8A8_UNorm,
                        false,
                        Usage.RenderTargetOutput,
                        (uint)_swapChainBufferCount,
                        Scaling.Stretch,
                        SwapEffect.FlipSequential,
                        AlphaMode.Ignore,
                        swapChainFlags);
                    _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
                    ConfigureFrameLatencyWaitableObject();
                }
            }
            else
            {
                Logger.Log("D3D11 preview IDXGISwapChain3 unavailable - HDR passthrough not supported.");
                _swapChain2?.Dispose();
                _swapChain2 = null;
                _frameLatencyWaitHandle = IntPtr.Zero;
                _swapChain.Dispose();
                swapChainDescription = new SwapChainDescription1(
                    (uint)pixelWidth,
                    (uint)pixelHeight,
                    Format.B8G8R8A8_UNorm,
                    false,
                    Usage.RenderTargetOutput,
                    (uint)_swapChainBufferCount,
                    Scaling.Stretch,
                    SwapEffect.FlipSequential,
                    AlphaMode.Ignore,
                    swapChainFlags);
                _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
                ConfigureFrameLatencyWaitableObject();
            }
        }

        _configuredOutputWidth = pixelWidth;
        _configuredOutputHeight = pixelHeight;
        ConfigureMediaPresentDuration();
        ApplyCompositionScaleTransform(_swapChain);
        BindSwapChainToPanel(_swapChain);
        CompileTonemapShaders();

        Logger.Log($"D3D11 preview device created featureLevel={featureLevel} shared={sharedDeviceActive}.");
        Logger.Log($"D3D11 preview swap chain created width={pixelWidth} height={pixelHeight} buffers={_swapChainBufferCount} renderQueue={_maxPendingFrames} sync={_presentSyncInterval} latency={_dxgiMaxFrameLatency} waitable={_waitableSwapChainEnabled}.");
    }

    private void ConfigureMediaPresentDuration()
    {
        if (!_mediaPresentDurationEnabled || _swapChain == null)
        {
            return;
        }

        using var mediaSwapChain = _swapChain.QueryInterfaceOrNull<IDXGISwapChainMedia>();
        if (mediaSwapChain == null)
        {
            Logger.Log("D3D11 preview media present duration unavailable: IDXGISwapChainMedia not supported.");
            return;
        }

        var fps = Math.Max(1.0, _startupFps);
        var desiredDuration = (uint)Math.Max(1, (int)Math.Round(10_000_000.0 / fps));
        try
        {
            mediaSwapChain.CheckPresentDurationSupport(
                desiredDuration,
                out var closestSmaller,
                out var closestLarger);
            Logger.Log(
                $"D3D11 preview media present duration support desired={desiredDuration} " +
                $"smaller={closestSmaller} larger={closestLarger}");

            mediaSwapChain.SetPresentDuration(desiredDuration);
            Logger.Log($"D3D11 preview media present duration set desired={desiredDuration} fps={fps:0.###}");
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview media present duration failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
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

    private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)
    {
        featureLevel = FeatureLevel.Level_11_0;

        ID3D11Device? sharedDevice = null;
        lock (_lifecycleLock)
        {
            if (_sharedDevice == null || _sharedDevice.NativePointer == IntPtr.Zero)
            {
                return false;
            }

            Marshal.AddRef(_sharedDevice.NativePointer);
            sharedDevice = new ID3D11Device(_sharedDevice.NativePointer);
        }

        try
        {
            _device = sharedDevice;
            sharedDevice = null;
            _deviceContext = _device.ImmediateContext;
            if (_deviceContext == null)
            {
                throw new InvalidOperationException("Shared D3D11 device returned a null immediate context.");
            }

            featureLevel = _device.FeatureLevel;
            return true;
        }
        catch (Exception ex)
        {
            sharedDevice?.Dispose();
            _deviceContext?.Dispose();
            _deviceContext = null;
            _device?.Dispose();
            _device = null;
            Logger.Log($"D3D11 shared device init failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}; falling back to renderer-owned device.");
            return false;
        }
    }

    private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)
    {
        var featureLevels = new[] { FeatureLevel.Level_11_0 };
        var flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;

        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            flags,
            featureLevels,
            out _device,
            out featureLevel,
            out _deviceContext);

        if (result.Failure)
        {
            Logger.Log($"D3D11 hardware device creation failed: 0x{result.Code:X8}. Falling back to WARP.");
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                flags,
                featureLevels,
                out _device,
                out featureLevel,
                out _deviceContext);
        }

        if (result.Failure || _device == null || _deviceContext == null)
        {
            throw new InvalidOperationException($"D3D11CreateDevice failed: 0x{result.Code:X8}.");
        }
    }

    private unsafe void CompileTonemapShaders()
    {
        _fullscreenVS?.Dispose();
        _fullscreenVS = null;
        _nv12PS?.Dispose();
        _nv12PS = null;
        _hdrTonemapPS?.Dispose();
        _hdrTonemapPS = null;
        _hdrPassthroughPS?.Dispose();
        _hdrPassthroughPS = null;
        _linearSampler?.Dispose();
        _linearSampler = null;
        _viewportCB?.Dispose();
        _viewportCB = null;

        if (_device == null)
        {
            return;
        }

        try
        {
            var vertexShaderBytecode = CompileShader(FullscreenVertexShaderSource, "main", "vs_5_0");
            var pixelShaderBytecode = CompileShader(HdrTonemapPixelShaderSource, "main", "ps_5_0");
            var passthroughBytecode = CompileShader(HdrPassthroughPixelShaderSource, "main", "ps_5_0");

            fixed (byte* vertexShaderPtr = vertexShaderBytecode)
            {
                _fullscreenVS = _device.CreateVertexShader(vertexShaderPtr, (nuint)vertexShaderBytecode.Length, null);
            }

            fixed (byte* pixelShaderPtr = pixelShaderBytecode)
            {
                _hdrTonemapPS = _device.CreatePixelShader(pixelShaderPtr, (nuint)pixelShaderBytecode.Length, null);
            }

            fixed (byte* passthroughPtr = passthroughBytecode)
            {
                _hdrPassthroughPS = _device.CreatePixelShader(passthroughPtr, (nuint)passthroughBytecode.Length, null);
            }

            try
            {
                var nv12Bytecode = CompileShader(Nv12PixelShaderSource, "main", "ps_5_0");
                fixed (byte* nv12Ptr = nv12Bytecode)
                {
                    _nv12PS = _device.CreatePixelShader(nv12Ptr, (nuint)nv12Bytecode.Length, null);
                }
            }
            catch (Exception ex)
            {
                _nv12PS?.Dispose();
                _nv12PS = null;
                Logger.Log($"D3D11 preview NV12 shader compile failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            }

            var samplerDescription = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MipLODBias = 0.0f,
                MaxAnisotropy = 1,
                ComparisonFunc = ComparisonFunction.Never,
                BorderColor = default,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue
            };

            _linearSampler = _device.CreateSamplerState(samplerDescription);

            _viewportCB = _device.CreateBuffer(new BufferDescription(
                16, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

            Logger.Log(
                $"D3D11 HDR shaders compiled (VS={vertexShaderBytecode.Length}b TonemapPS={pixelShaderBytecode.Length}b PassthroughPS={passthroughBytecode.Length}b Nv12PS={(_nv12PS != null ? "ok" : "unavailable")}).");
        }
        catch (Exception ex)
        {
            _fullscreenVS?.Dispose();
            _fullscreenVS = null;
            _nv12PS?.Dispose();
            _nv12PS = null;
            _hdrTonemapPS?.Dispose();
            _hdrTonemapPS = null;
            _hdrPassthroughPS?.Dispose();
            _hdrPassthroughPS = null;
            _linearSampler?.Dispose();
            _linearSampler = null;
            _viewportCB?.Dispose();
            _viewportCB = null;
            Logger.Log($"D3D11 HDR tonemap shader compile failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)
    {
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(hlslSource);
        var shaderBlob = IntPtr.Zero;
        var errorBlob = IntPtr.Zero;
        try
        {
            var hr = D3DCompileNative(
                sourceBytes,
                (IntPtr)sourceBytes.Length,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                entryPoint,
                profile,
                0,
                0,
                out shaderBlob,
                out errorBlob);

            if (hr < 0)
            {
                var errors = ReadBlobString(errorBlob);
                throw new InvalidOperationException(
                    $"D3DCompile failed entry={entryPoint} target={profile} hr=0x{hr:X8} errors={errors}");
            }

            if (shaderBlob == IntPtr.Zero)
            {
                throw new InvalidOperationException($"D3DCompile returned an empty blob for entry={entryPoint} target={profile}.");
            }

            return ReadBlobBytes(shaderBlob);
        }
        finally
        {
            if (shaderBlob != IntPtr.Zero)
            {
                Marshal.Release(shaderBlob);
            }

            if (errorBlob != IntPtr.Zero)
            {
                Marshal.Release(errorBlob);
            }
        }
    }

    private static byte[] ReadBlobBytes(IntPtr blobPtr)
    {
        if (blobPtr == IntPtr.Zero)
        {
            return Array.Empty<byte>();
        }

        ID3DBlob? blob = null;
        try
        {
            blob = (ID3DBlob)Marshal.GetObjectForIUnknown(blobPtr);
            var length = checked((int)blob.GetBufferSize().ToInt64());
            if (length <= 0)
            {
                return Array.Empty<byte>();
            }

            var bytes = new byte[length];
            Marshal.Copy(blob.GetBufferPointer(), bytes, 0, length);
            return bytes;
        }
        finally
        {
            if (blob != null)
            {
                Marshal.ReleaseComObject(blob);
            }
        }
    }

    private static string ReadBlobString(IntPtr blobPtr)
    {
        if (blobPtr == IntPtr.Zero)
        {
            return string.Empty;
        }

        ID3DBlob? blob = null;
        try
        {
            blob = (ID3DBlob)Marshal.GetObjectForIUnknown(blobPtr);
            var length = checked((int)blob.GetBufferSize().ToInt64());
            if (length <= 0)
            {
                return string.Empty;
            }

            var bytes = new byte[length];
            Marshal.Copy(blob.GetBufferPointer(), bytes, 0, length);
            return System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0', '\r', '\n');
        }
        finally
        {
            if (blob != null)
            {
                Marshal.ReleaseComObject(blob);
            }
        }
    }

    private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)
    {
        if (_swapChain == null || _videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 preview pipeline is not initialized.");
        }

        // Keep the internal render target aligned with the source-sized swap chain.
        var outputWidth = _configuredOutputWidth > 0
            ? _configuredOutputWidth
            : Math.Max(1, Volatile.Read(ref _startupWidth));
        var outputHeight = _configuredOutputHeight > 0
            ? _configuredOutputHeight
            : Math.Max(1, Volatile.Read(ref _startupHeight));
        var needRecreate = _videoProcessor == null ||
                           _videoProcessorEnumerator == null ||
                           _configuredInputWidth != width ||
                           _configuredInputHeight != height ||
                           _configuredHdr != isHdr;

        if (needRecreate)
        {
            DisposeProcessorResources();

            var fps = Math.Max(1.0, _startupFps);
            var fpsNum = (uint)Math.Max(1, (int)Math.Round(fps * 1000.0));
            var frameRate = new Rational(fpsNum, 1000u);
            var contentDescription = new VideoProcessorContentDescription
            {
                InputFrameFormat = VideoFrameFormat.Progressive,
                InputFrameRate = frameRate,
                InputWidth = (uint)width,
                InputHeight = (uint)height,
                OutputFrameRate = frameRate,
                OutputWidth = (uint)outputWidth,
                OutputHeight = (uint)outputHeight,
                Usage = VideoUsage.PlaybackNormal
            };

            _videoProcessorEnumerator = _videoDevice.CreateVideoProcessorEnumerator(contentDescription);
            _videoProcessor = _videoDevice.CreateVideoProcessor(_videoProcessorEnumerator, 0);
            RecreateOutputView();

            var sourceRect = new Vortice.RawRect(0, 0, width, height);
            var destinationRect = ComputeLetterboxRect(width, height, outputWidth, outputHeight);
            var outputTargetRect = new Vortice.RawRect(0, 0, outputWidth, outputHeight);
            _videoContext.VideoProcessorSetStreamFrameFormat(_videoProcessor, 0, VideoFrameFormat.Progressive);
            _videoContext.VideoProcessorSetStreamAutoProcessingMode(_videoProcessor, 0, false);
            _videoContext.VideoProcessorSetStreamSourceRect(_videoProcessor, 0, true, sourceRect);
            _videoContext.VideoProcessorSetStreamDestRect(_videoProcessor, 0, true, destinationRect);
            _videoContext.VideoProcessorSetOutputTargetRect(_videoProcessor, true, outputTargetRect);
            _videoContext.VideoProcessorSetOutputBackgroundColor(_videoProcessor, false, new VideoColor());
            ApplyColorSpaces(isHdr);

            _configuredInputWidth = width;
            _configuredInputHeight = height;
            _configuredHdr = isHdr;

            Logger.Log($"D3D11 video processor created input={width}x{height} output={outputWidth}x{outputHeight} hdr={isHdr}.");
        }

        if (!useExternalTexture)
        {
            EnsureInputResources(width, height, isHdr);
        }
    }

    private void EnsureInputResources(int width, int height, bool isHdr)
    {
        if (_device == null || _videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for input texture creation.");
        }

        var targetFormat = isHdr ? Format.P010 : Format.NV12;
        if (_inputTexture != null &&
            _stagingTexture != null &&
            _inputView != null &&
            _configuredInputWidth == width &&
            _configuredInputHeight == height &&
            _configuredInputFormat == targetFormat)
        {
            return;
        }

        _inputView?.Dispose();
        _inputView = null;
        _inputTexture?.Dispose();
        _inputTexture = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        var inputDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _inputTexture = _device.CreateTexture2D(inputDescription);
        _stagingTexture = _device.CreateTexture2D(stagingDescription);

        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView { MipSlice = 0, ArraySlice = 0 }
        };

        _inputView = _videoDevice.CreateVideoProcessorInputView(_inputTexture, _videoProcessorEnumerator, inputViewDescription);
        _configuredInputFormat = targetFormat;
    }

    private void EnsureHdrInputResources(int width, int height)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for HDR shader input texture creation.");
        }

        if (_hdrPlaneViewsUnavailable)
        {
            return;
        }

        if (_hdrInputTexture != null &&
            _hdrStagingTexture != null &&
            _hdrYPlaneSRV != null &&
            _hdrUVPlaneSRV != null &&
            _hdrInputConfiguredWidth == width &&
            _hdrInputConfiguredHeight == height)
        {
            return;
        }

        _hdrYPlaneSRV?.Dispose();
        _hdrYPlaneSRV = null;
        _hdrUVPlaneSRV?.Dispose();
        _hdrUVPlaneSRV = null;
        _hdrInputTexture?.Dispose();
        _hdrInputTexture = null;
        _hdrStagingTexture?.Dispose();
        _hdrStagingTexture = null;

        var inputDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.ShaderResource,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _hdrInputTexture = _device.CreateTexture2D(inputDescription);
        _hdrStagingTexture = _device.CreateTexture2D(stagingDescription);

        _hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);
        _hdrUVPlaneSRV = CreateHdrPlaneView(Format.R16G16_UNorm, planeSlice: 1);

        if (_hdrYPlaneSRV == null && _hdrUVPlaneSRV == null)
        {
            _hdrInputTexture.Dispose();
            _hdrInputTexture = null;
            _hdrStagingTexture.Dispose();
            _hdrStagingTexture = null;
            _hdrPlaneViewsUnavailable = true;
            return;
        }

        _hdrInputConfiguredWidth = width;
        _hdrInputConfiguredHeight = height;
    }

    private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)
    {
        if (_device == null || _hdrInputTexture == null)
        {
            throw new InvalidOperationException("HDR shader input texture has not been created.");
        }

        if (_device3 != null)
        {
            var srvDesc = new ShaderResourceViewDescription1(
                _hdrInputTexture,
                ShaderResourceViewDimension.Texture2D,
                format,
                0,
                1,
                0,
                1,
                planeSlice);

            return _device3.CreateShaderResourceView1(_hdrInputTexture, srvDesc);
        }

        Logger.Log("D3D11_RENDERER_WARN Device3 not available for P010 plane views — HDR shader path disabled, falling back to VideoProcessor");
        return null;
    }

    private void EnsureSwapChainRTV()
    {
        if (_device == null || _swapChain == null)
        {
            throw new InvalidOperationException("D3D11 preview swap chain render target state is unavailable.");
        }

        if (_swapChainBackBuffer == null)
        {
            _swapChainBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        }

        if (_swapChainRTV == null)
        {
            _swapChainRTV = _device.CreateRenderTargetView(_swapChainBackBuffer, null);
        }
    }

    private void RecreateOutputView()
    {
        if (_swapChain == null || _videoDevice == null || _videoProcessorEnumerator == null || _device == null)
        {
            throw new InvalidOperationException("D3D11 output view recreation requires swap chain and video enumerator.");
        }

        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;

        _swapChainBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        EnsureSwapChainRTV();
        var outputViewDescription = new VideoProcessorOutputViewDescription
        {
            ViewDimension = VideoProcessorOutputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorOutputView { MipSlice = 0 }
        };

        _outputView = _videoDevice.CreateVideoProcessorOutputView(_swapChainBackBuffer, _videoProcessorEnumerator, outputViewDescription);
    }

    private void ApplyColorSpaces(bool isHdr)
    {
        if (_videoContext1 == null || _videoProcessor == null) return;

        var fullRange = Volatile.Read(ref _fullRangeInput);
        var inputColorSpace = isHdr
            ? ColorSpaceType.YcbcrStudioG2084LeftP2020
            : fullRange
                ? ColorSpaceType.YcbcrFullG22LeftP709
                : ColorSpaceType.YcbcrStudioG22LeftP709;
        var outputColorSpace = ColorSpaceType.RgbFullG22NoneP709;

        _videoContext1.VideoProcessorSetStreamColorSpace1(_videoProcessor, 0, inputColorSpace);
        _videoContext1.VideoProcessorSetOutputColorSpace1(_videoProcessor, outputColorSpace);

        _inputColorSpaceLabel = inputColorSpace.ToString();
        _outputColorSpaceLabel = outputColorSpace.ToString();
        Logger.Log($"D3D11 preview color space input={_inputColorSpaceLabel} output={_outputColorSpaceLabel} mode=VideoProcessor.");
    }

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
        // bind — if either has changed, the renderer has been stopped or the chain
        // superseded, and SetSwapChain on a stale (possibly disposed) chain would AV.
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
            // Leave `done` undisposed — the queued lambda may still run later and
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
                    // catch — it terminates the process. Skip the call entirely.
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
            // Dispatcher may be shut down — safe to ignore during cleanup.
            Logger.Log($"D3D11 preview swap chain unbind ignored during cleanup: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)
    {
        using var swapChain2 = swapChain.QueryInterfaceOrNull<IDXGISwapChain2>();
        if (swapChain2 == null)
        {
            return;
        }

        var panelLogicalW = Volatile.Read(ref _panelLogicalWidth);
        var panelLogicalH = Volatile.Read(ref _panelLogicalHeight);
        var swapW = (double)Math.Max(1, _configuredOutputWidth);
        var swapH = (double)Math.Max(1, _configuredOutputHeight);

        if (panelLogicalW <= 0 || panelLogicalH <= 0)
        {
            swapChain2.MatrixTransform = System.Numerics.Matrix3x2.Identity;
            return;
        }

        var uniformScale = (float)Math.Min(panelLogicalW / swapW, panelLogicalH / swapH);
        var offsetX = (float)((panelLogicalW - swapW * uniformScale) * 0.5);
        var offsetY = (float)((panelLogicalH - swapH * uniformScale) * 0.5);

        swapChain2.MatrixTransform = new System.Numerics.Matrix3x2(
            uniformScale, 0,
            0, uniformScale,
            offsetX, offsetY);

        Logger.Log($"D3D11 preview composition transform set scale={uniformScale:F4} offset=({offsetX:F1},{offsetY:F1}) panel={panelLogicalW:F0}x{panelLogicalH:F0} swap={swapW}x{swapH}.");
    }

    private void HandleDeviceLost(Exception ex)
    {
        Logger.Log($"D3D11 preview device lost ({ex.GetType().Name}); recreating device.");

        // If Stop() is pending, bail. Stop() will unbind the swap chain from
        // the panel while D3D resources are still alive, then the finally block
        // will clean up. Proceeding here would dispose the swap chain while
        // Stop() may be concurrently calling SetSwapChain(null) on the panel —
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
        // into InitializeD3D→BindSwapChainToPanel would dispatch to the UI
        // thread, which may be blocked on Join — a 5-second deadlock.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        InitializeD3D();
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
    }

    private void DisposeProcessorResources()
    {
        _inputView?.Dispose();
        _inputView = null;
        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;
        _hdrYPlaneSRV?.Dispose();
        _hdrYPlaneSRV = null;
        _hdrUVPlaneSRV?.Dispose();
        _hdrUVPlaneSRV = null;
        _nv12YSRV?.Dispose();
        _nv12YSRV = null;
        _nv12UVSRV?.Dispose();
        _nv12UVSRV = null;
        _nv12LastYPtr = IntPtr.Zero;
        _nv12LastUVPtr = IntPtr.Zero;
        _hdrStagingTexture?.Dispose();
        _hdrStagingTexture = null;
        _hdrInputTexture?.Dispose();
        _hdrInputTexture = null;
        _hdrInputConfiguredWidth = 0;
        _hdrInputConfiguredHeight = 0;
        _hdrPlaneViewsUnavailable = false;
        _videoProcessor?.Dispose();
        _videoProcessor = null;
        _videoProcessorEnumerator?.Dispose();
        _videoProcessorEnumerator = null;
    }

    private void CleanupD3DResources()
    {
        DisposeProcessorResources();

        _captureStagingTexture?.Dispose();
        _captureStagingTexture = null;
        _captureStagingWidth = 0;
        _captureStagingHeight = 0;

        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _inputTexture?.Dispose();
        _inputTexture = null;

        // Stop() unbinds the panel before waking the render thread, while the
        // swap chain is still alive. Cleanup can then release the DXGI objects
        // without leaving SwapChainPanel holding a stale native reference.
        Interlocked.CompareExchange(ref _swapChainBound, 0, 1);
        Interlocked.Exchange(ref _swapChainAddress, 0);

        _swapChain3?.Dispose();
        _swapChain3 = null;
        _swapChain2?.Dispose();
        _swapChain2 = null;
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain?.Dispose();
        _swapChain = null;
        _factory?.Dispose();
        _factory = null;
        _videoContext1?.Dispose();
        _videoContext1 = null;
        _videoContext?.Dispose();
        _videoContext = null;
        _videoDevice?.Dispose();
        _videoDevice = null;
        _linearSampler?.Dispose();
        _linearSampler = null;
        _viewportCB?.Dispose();
        _viewportCB = null;
        _nv12PS?.Dispose();
        _nv12PS = null;
        _hdrTonemapPS?.Dispose();
        _hdrTonemapPS = null;
        _hdrPassthroughPS?.Dispose();
        _hdrPassthroughPS = null;
        _fullscreenVS?.Dispose();
        _fullscreenVS = null;
        _multithread?.Dispose();
        _multithread = null;
        _device3?.Dispose();
        _device3 = null;
        _deviceContext?.Dispose();
        _deviceContext = null;
        _device?.Dispose();
        _device = null;

        _configuredInputWidth = 0;
        _configuredInputHeight = 0;
        _configuredOutputWidth = 0;
        _configuredOutputHeight = 0;
        _configuredInputFormat = Format.Unknown;
        _hdrCapableSwapChain = false;
        Interlocked.Exchange(ref _sharedDeviceActive, 0);
    }

    private static Vortice.RawRect ComputeLetterboxRect(int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        if (srcWidth <= 0 || srcHeight <= 0 || dstWidth <= 0 || dstHeight <= 0)
        {
            return new Vortice.RawRect(0, 0, dstWidth, dstHeight);
        }

        var srcAspect = (double)srcWidth / srcHeight;
        var dstAspect = (double)dstWidth / dstHeight;

        int fitWidth, fitHeight;
        if (srcAspect > dstAspect)
        {
            // Source is wider - letterbox (bars top/bottom)
            fitWidth = dstWidth;
            fitHeight = (int)(dstWidth / srcAspect);
        }
        else
        {
            // Source is taller - pillarbox (bars left/right)
            fitHeight = dstHeight;
            fitWidth = (int)(dstHeight * srcAspect);
        }

        var x = (dstWidth - fitWidth) / 2;
        var y = (dstHeight - fitHeight) / 2;
        return new Vortice.RawRect(x, y, x + fitWidth, y + fitHeight);
    }

    private static bool IsDeviceLostException(Exception ex)
    {
        if (ex is SharpGen.Runtime.SharpGenException sharpGenException)
        {
            return sharpGenException.ResultCode == Vortice.DXGI.ResultCode.DeviceRemoved ||
                   sharpGenException.ResultCode == Vortice.DXGI.ResultCode.DeviceReset;
        }

        if (ex is COMException comException)
        {
            return comException.HResult == unchecked((int)0x887A0005) ||
                   comException.HResult == unchecked((int)0x887A0007);
        }

        return false;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(D3D11PreviewRenderer));
        }
    }

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}

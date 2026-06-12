using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private const int FrameCaptureTimeoutMs = 5000;

    private bool _loggedHdrShaderFallback;
    private bool _loggedDirectUploadFallback;
    private TaskCompletionSource<PreviewFrameCaptureResult>? _frameCaptureRequest;
    private string? _frameCaptureOutputPath;
    private ID3D11Texture2D? _captureStagingTexture;
    private int _frameCaptureEncodeInProgress;
    private int _captureStagingWidth;
    private int _captureStagingHeight;

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

        if (_deviceContext == null || _inputTextures.Length == 0 || _inputViews.Length != _inputTextures.Length)
        {
            return false;
        }

        // Advance the upload ring so this write never lands on the texture the
        // previous frame's VideoProcessorBlt may still be consuming.
        var ringIndex = _inputTextureRingIndex;
        _inputTextureRingIndex = (ringIndex + 1) % _inputTextures.Length;
        var inputTexture = _inputTextures[ringIndex];
        var stagingTexture = ringIndex < _stagingTextures.Length ? _stagingTextures[ringIndex] : null;
        var ringInputView = _inputViews[ringIndex];
        if (inputTexture == null || stagingTexture == null || ringInputView == null)
        {
            return false;
        }

        if (frame.RawData != null)
        {
            if (!UploadRawFrameToTexture(frame.RawData, frame.RawDataLength, frame.Width, frame.Height, frame.IsHdr, stagingTexture, inputTexture))
            {
                return false;
            }
        }
        else if (frame.FrameLease != null)
        {
            if (!UploadRawFrameToTexture(frame.FrameLease.Memory.Span, frame.Width, frame.Height, frame.IsHdr, stagingTexture, inputTexture))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        inputView = ringInputView;
        return true;
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

    private void RenderFrameWithVideoProcessor(PendingFrame frame)
    {
        var totalStart = Stopwatch.GetTimestamp();
        long inputUploadTicks = 0;
        long renderTicks = 0;
        long presentTicks = 0;

        if (!TryEnterNativeRenderCall())
        {
            return;
        }

        try
        {
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
            ExitNativeRenderCall();
        }
    }

    private void RenderNv12WithShader(PendingFrame frame)
    {
        var totalStart = Stopwatch.GetTimestamp();
        long inputUploadTicks = 0;
        long renderTicks = 0;
        long presentTicks = 0;

        if (!TryEnterNativeRenderCall())
        {
            return;
        }

        try
        {
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
            ExitNativeRenderCall();
        }
    }

    private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)
    {
        var totalStart = Stopwatch.GetTimestamp();
        long inputUploadTicks = 0;
        long renderTicks = 0;
        long presentTicks = 0;

        if (!TryEnterNativeRenderCall())
        {
            return;
        }

        try
        {
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
            if (_hdrInputTextures.Length == 0 || _hdrInputTextures[0] == null)
            {
                return;
            }

            var ringIndex = _hdrInputRingIndex;
            _hdrInputRingIndex = (ringIndex + 1) % _hdrInputTextures.Length;
            var hdrInputTexture = _hdrInputTextures[ringIndex]!;
            var hdrStagingTexture = _hdrStagingTextures[ringIndex]!;
            var hdrYPlaneSRV = _hdrYPlaneSRVs[ringIndex]!;
            var hdrUVPlaneSRV = _hdrUVPlaneSRVs[ringIndex]!;
            inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;

            if (frame.D3DTexture != null)
            {
                inputStart = Stopwatch.GetTimestamp();
                var srcDesc = frame.D3DTexture.Description;
                var planeOffset = (int)(srcDesc.ArraySize * Math.Max(1, srcDesc.MipLevels));

                _deviceContext.CopySubresourceRegion(hdrInputTexture, 0, 0, 0, 0,
                    frame.D3DTexture, (uint)frame.D3DSubresourceIndex);

                _deviceContext.CopySubresourceRegion(hdrInputTexture, 1, 0, 0, 0,
                    frame.D3DTexture, (uint)(frame.D3DSubresourceIndex + planeOffset));
                inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;
            }
            else if (frame.RawData != null)
            {
                inputStart = Stopwatch.GetTimestamp();
                if (!UploadRawFrameToTexture(frame.RawData, frame.RawDataLength, frame.Width, frame.Height, true, hdrStagingTexture, hdrInputTexture))
                {
                    return;
                }
                inputUploadTicks += Stopwatch.GetTimestamp() - inputStart;
            }
            else if (frame.FrameLease != null)
            {
                inputStart = Stopwatch.GetTimestamp();
                if (!UploadRawFrameToTexture(frame.FrameLease.Memory.Span, frame.Width, frame.Height, true, hdrStagingTexture, hdrInputTexture))
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
            _srvArray2[0] = hdrYPlaneSRV;
            _srvArray2[1] = hdrUVPlaneSRV;
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
            ExitNativeRenderCall();
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

        NotifyFirstFrameRendered(firstFrameMessage);

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

    public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath)
        => CaptureNextFrameAsync(outputPath, CancellationToken.None);

    public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(CreateFrameCaptureError("Preview frame capture canceled."));
        }

        if (!IsRendering || _device == null || _swapChain == null || Volatile.Read(ref _stopRequested) != 0)
        {
            return Task.FromResult(CreateFrameCaptureError("No active preview renderer."));
        }

        if (IsPngFrameCaptureCompletionInProgress())
        {
            return Task.FromResult(CreateFrameCaptureError("A preview frame capture is already pending."));
        }

        var resolvedOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.bmp")
            : outputPath;

        var request = new TaskCompletionSource<PreviewFrameCaptureResult>(
            state: resolvedOutputPath,
            creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _frameCaptureRequest, request, null) != null)
        {
            return Task.FromResult(CreateFrameCaptureError("A preview frame capture is already pending."));
        }

        CancellationTokenRegistration cancellationRegistration = default;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    var (renderer, request) = ((D3D11PreviewRenderer Renderer, TaskCompletionSource<PreviewFrameCaptureResult> Request))state!;
                    var pending = Interlocked.CompareExchange(ref renderer._frameCaptureRequest, null, request);
                    if (!ReferenceEquals(pending, request))
                    {
                        return;
                    }

                    Interlocked.Exchange(ref renderer._frameCaptureOutputPath, null);
                    request.TrySetResult(CreateFrameCaptureError("Preview frame capture canceled."));
                    Logger.Log("PREVIEW_FRAME_CAPTURE_CANCELED");
                },
                (this, request));
            _ = request.Task.ContinueWith(
                _ => cancellationRegistration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        Volatile.Write(ref _frameCaptureOutputPath, resolvedOutputPath);
        _ = Task.Delay(FrameCaptureTimeoutMs).ContinueWith(
            _ =>
            {
                var pending = Interlocked.CompareExchange(ref _frameCaptureRequest, null, request);
                if (!ReferenceEquals(pending, request))
                {
                    return;
                }

                Interlocked.Exchange(ref _frameCaptureOutputPath, null);
                request.TrySetResult(CreateFrameCaptureError("Timed out waiting for the next rendered preview frame."));
                Logger.Log("PREVIEW_FRAME_CAPTURE_TIMEOUT missing=RenderedFrame");
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return request.Task;
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
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.bmp")
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
            if (isPng && !TryBeginPngFrameCaptureCompletion())
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

                var stagingTexture = EnsureFrameCaptureStagingTexture(backBufferDescription, width, height);
                _deviceContext.CopyResource(stagingTexture, backBuffer);

                _deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None, out var mapped);
                PreviewFrameCaptureResult captureResult;
                byte[]? pngFrameBuffer = null;
                var pngSourceRowBytes = checked(width * 4);
                try
                {
                    if (isPng)
                    {
                        pngFrameBuffer = PreviewScreenshotCapture.CopyMappedFrameToBuffer(mapped, height, pngSourceRowBytes);
                        captureResult = default!;
                    }
                    else
                    {
                        captureResult = PreviewScreenshotCapture.CaptureMappedFrameToBmp(
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
                    BeginPngFrameCaptureCompletion(
                        request,
                        pngFrameBuffer!,
                        pngSourceRowBytes,
                        width,
                        height,
                        fullOutputPath,
                        rendererMode,
                        backBufferDescription.Format);
                    return;
                }

                request.TrySetResult(captureResult);
                LogFrameCaptureResult(captureResult);
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
            EndPngFrameCaptureCompletion();
            request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
            LogFrameCaptureFailure(ex, rendererMode);
        }
    }

    private bool IsPngFrameCaptureCompletionInProgress()
        => Volatile.Read(ref _frameCaptureEncodeInProgress) != 0;

    private bool TryBeginPngFrameCaptureCompletion()
        => Interlocked.CompareExchange(ref _frameCaptureEncodeInProgress, 1, 0) == 0;

    private void EndPngFrameCaptureCompletion()
        => Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);

    private void BeginPngFrameCaptureCompletion(
        TaskCompletionSource<PreviewFrameCaptureResult> request,
        byte[] frameBuffer,
        int sourceRowBytes,
        int width,
        int height,
        string outputPath,
        string rendererMode,
        Format backBufferFormat)
    {
        _ = Task.Run(
            () =>
            {
                try
                {
                    var captureResult = PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(
                        frameBuffer,
                        sourceRowBytes,
                        width,
                        height,
                        outputPath,
                        rendererMode,
                        backBufferFormat);
                    request.TrySetResult(captureResult);
                    LogFrameCaptureResult(captureResult);
                }
                catch (Exception ex)
                {
                    request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
                    LogFrameCaptureFailure(ex, rendererMode);
                }
                finally
                {
                    EndPngFrameCaptureCompletion();
                }
            });
    }

    private ID3D11Texture2D EnsureFrameCaptureStagingTexture(Texture2DDescription backBufferDescription, int width, int height)
    {
        if (_captureStagingTexture == null ||
            _captureStagingWidth != width ||
            _captureStagingHeight != height)
        {
            _captureStagingTexture?.Dispose();
            _captureStagingTexture = _device!.CreateTexture2D(new Texture2DDescription(
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

        return _captureStagingTexture!;
    }

    private void DisposeFrameCaptureStagingResources()
    {
        _captureStagingTexture?.Dispose();
        _captureStagingTexture = null;
        _captureStagingWidth = 0;
        _captureStagingHeight = 0;
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

    private static void LogFrameCaptureResult(PreviewFrameCaptureResult captureResult)
    {
        Logger.Log(
            $"PREVIEW_FRAME_CAPTURE_RESULT ok={captureResult.Succeeded} renderer={captureResult.RendererMode} path={captureResult.FilePath ?? "n/a"} width={captureResult.CapturedWidth} height={captureResult.CapturedHeight} avgLum={captureResult.AverageLuminance:0.00} pureBlackPct={captureResult.PureBlackPercent:0.00}");
    }

    private static void LogFrameCaptureFailure(Exception ex, string rendererMode)
    {
        Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
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
            fitWidth = dstWidth;
            fitHeight = (int)(dstWidth / srcAspect);
        }
        else
        {
            fitHeight = dstHeight;
            fitWidth = (int)(dstHeight * srcAspect);
        }

        var x = (dstWidth - fitWidth) / 2;
        var y = (dstHeight - fitHeight) / 2;
        return new Vortice.RawRect(x, y, x + fitWidth, y + fitHeight);
    }

}

using System;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private bool _loggedHdrShaderFallback;

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
            ExitNativeRenderCall();
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Runtime;
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
}

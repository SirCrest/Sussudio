using System;
using Sussudio.Services.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private ID3D11VertexShader? _fullscreenVS;
    private ID3D11PixelShader? _nv12PS;
    private ID3D11ShaderResourceView? _nv12YSRV;
    private ID3D11ShaderResourceView? _nv12UVSRV;
    private IntPtr _nv12LastYPtr;
    private IntPtr _nv12LastUVPtr;
    private ID3D11PixelShader? _hdrTonemapPS;
    private ID3D11PixelShader? _hdrPassthroughPS;
    private ID3D11SamplerState? _linearSampler;
    private ID3D11Buffer? _viewportCB;

    // Pre-allocated arrays to avoid per-frame GC pressure (720+ allocs/s at 120fps)
    private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];
    private readonly ID3D11RenderTargetView[] _rtvArray = new ID3D11RenderTargetView[1];
    private readonly Viewport[] _viewportArray = new Viewport[1];
    private readonly ID3D11SamplerState[] _samplerArray = new ID3D11SamplerState[1];
    private readonly ID3D11ShaderResourceView[] _srvArray2 = new ID3D11ShaderResourceView[2];
    private readonly ID3D11ShaderResourceView[] _srvNullArray2 = { null!, null! };
    private readonly ID3D11Buffer[] _cbArray = new ID3D11Buffer[1];

    // Reused at every shader bind in the per-frame render path; the LINQ-friendly
    // overloads on Vortice's device context allocate an IReadOnlyList wrapper from
    // Array.Empty<T>() each call, which adds up at 60-120 fps.
    private static readonly ID3D11ClassInstance[] EmptyClassInstances = System.Array.Empty<ID3D11ClassInstance>();

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

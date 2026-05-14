using System;
using D3D11DeviceContext = Vortice.Direct3D11.ID3D11DeviceContext;
using D3D11Multithread = Vortice.Direct3D11.ID3D11Multithread;
using D3D11Texture2D = Vortice.Direct3D11.ID3D11Texture2D;

namespace Sussudio.Services.Gpu;

/// <summary>
/// Copies NVDEC CUDA NV12 surfaces to a D3D11 NV12 texture for preview rendering.
/// Prefers zero-copy GPU-to-GPU path via two CUDA-registered helper textures and falls back
/// to a staging texture (cuMemcpy2D device->host + CopyResource) when CUDA-D3D11 interop setup fails.
/// </summary>
internal sealed unsafe partial class CudaD3D11InteropBridge : IDisposable
{
    // D3D11 immediate context is NOT thread-safe. cuGraphicsMapResources uses it
    // internally, so concurrent calls from two decoder threads race and cause error 999.
    private static readonly object D3D11InteropLock = new();

    private readonly IntPtr _cudaCtx;
    private readonly int _cuDevice;
    private readonly int _width;
    private readonly int _height;
    private readonly D3D11DeviceContext _deviceContext;
    private readonly D3D11Multithread _multithread;

    // Zero-copy path: two helper textures registered with CUDA
    private IntPtr _registeredResourceY;
    private IntPtr _registeredResourceUV;
    private D3D11Texture2D? _helperTextureY;
    private D3D11Texture2D? _helperTextureUV;
    private bool _zeroCopyAvailable;

    // Staging fallback (only created if zero-copy fails)
    private D3D11Texture2D? _stagingTexture;

    // Shared: NV12 default texture for preview renderer
    private D3D11Texture2D? _defaultTexture;
    private bool _initialized;
    private int _disposed;
    private int _diagDone;
    private int _ctxDiagDone;

    public IntPtr TextureNativePointer
        => (_defaultTexture ?? throw new ObjectDisposedException(nameof(CudaD3D11InteropBridge))).NativePointer;

    public bool ZeroCopyAvailable => _zeroCopyAvailable;

    public IntPtr HelperYTextureNativePointer
        => _zeroCopyAvailable ? (_helperTextureY?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero;

    public IntPtr HelperUVTextureNativePointer
        => _zeroCopyAvailable ? (_helperTextureUV?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero;

}

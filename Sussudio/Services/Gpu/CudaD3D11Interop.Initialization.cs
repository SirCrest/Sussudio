using System;
using System.Threading;
using Vortice.Direct3D11;
using Vortice.DXGI;
using D3D11Device = Vortice.Direct3D11.ID3D11Device;
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

    public CudaD3D11InteropBridge(IntPtr cudaCtx, D3D11Device d3dDevice, int width, int height)
    {
        if (cudaCtx == IntPtr.Zero)
            throw new ArgumentException("CUDA context pointer is null.", nameof(cudaCtx));

        ArgumentNullException.ThrowIfNull(d3dDevice);
        if (d3dDevice.NativePointer == IntPtr.Zero)
            throw new ArgumentException("D3D11 device pointer is null.", nameof(d3dDevice));

        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if ((width & 1) != 0) throw new ArgumentOutOfRangeException(nameof(width), "NV12 requires even width.");
        if ((height & 1) != 0) throw new ArgumentOutOfRangeException(nameof(height), "NV12 requires even height.");

        // Use the CUDA primary context for all bridge operations.
        // Primary contexts are thread-safe (concurrent use from multiple threads is allowed).
        // FFmpeg creates a non-primary context for decoding — that's fine, device pointers
        // are valid across contexts on the same GPU.
        ThrowOnCudaError(cuDeviceGet(out var cuDevice, 0), nameof(cuDeviceGet));
        ThrowOnCudaError(cuDevicePrimaryCtxRetain(out var primaryCtx, cuDevice), nameof(cuDevicePrimaryCtxRetain));
        Logger.Log(
            $"CUDA_D3D11_INTEROP_CTX_INIT ffmpeg=0x{cudaCtx.ToInt64():X} primary=0x{primaryCtx.ToInt64():X} " +
            $"width={width} height={height}");

        _cudaCtx = primaryCtx;
        _cuDevice = cuDevice;
        _width = width;
        _height = height;

        _deviceContext = d3dDevice.ImmediateContext
            ?? throw new InvalidOperationException("D3D11 device returned a null immediate context.");
        _multithread = d3dDevice.QueryInterfaceOrNull<D3D11Multithread>()
            ?? throw new InvalidOperationException("D3D11 multithread protection unavailable.");

        D3D11Texture2D? defaultTex = null;
        D3D11Texture2D? helperTextureY = null;
        D3D11Texture2D? helperTextureUV = null;
        D3D11Texture2D? staging = null;
        IntPtr registeredResourceY = IntPtr.Zero;
        IntPtr registeredResourceUV = IntPtr.Zero;

        try
        {
            _multithread.SetMultithreadProtected(true);

            defaultTex = d3dDevice.CreateTexture2D(new Texture2DDescription(
                Format.NV12, (uint)width, (uint)height, 1, 1,
                BindFlags.None, ResourceUsage.Default, CpuAccessFlags.None,
                1, 0, ResourceOptionFlags.None));

            if (TryInitializeZeroCopyResources(
                d3dDevice,
                out helperTextureY,
                out helperTextureUV,
                out registeredResourceY,
                out registeredResourceUV))
            {
                _helperTextureY = helperTextureY;
                _helperTextureUV = helperTextureUV;
                _registeredResourceY = registeredResourceY;
                _registeredResourceUV = registeredResourceUV;
                _zeroCopyAvailable = true;
            }
            else
            {
                helperTextureY?.Dispose();
                helperTextureY = null;
                helperTextureUV?.Dispose();
                helperTextureUV = null;

                staging = d3dDevice.CreateTexture2D(new Texture2DDescription(
                    Format.NV12, (uint)width, (uint)height, 1, 1,
                    BindFlags.None, ResourceUsage.Staging, CpuAccessFlags.Write,
                    1, 0, ResourceOptionFlags.None));

                _stagingTexture = staging;
                _zeroCopyAvailable = false;
            }

            _defaultTexture = defaultTex;
            _initialized = true;
        }
        catch
        {
            if (registeredResourceY != IntPtr.Zero)
            {
                TryUnregisterResource(registeredResourceY);
            }

            if (registeredResourceUV != IntPtr.Zero)
            {
                TryUnregisterResource(registeredResourceUV);
            }

            cuDevicePrimaryCtxRelease(_cuDevice);
            defaultTex?.Dispose();
            helperTextureY?.Dispose();
            helperTextureUV?.Dispose();
            staging?.Dispose();
            throw;
        }
    }

    private bool TryInitializeZeroCopyResources(
        D3D11Device d3dDevice,
        out D3D11Texture2D? helperTextureY,
        out D3D11Texture2D? helperTextureUV,
        out IntPtr registeredResourceY,
        out IntPtr registeredResourceUV)
    {
        helperTextureY = null;
        helperTextureUV = null;
        registeredResourceY = IntPtr.Zero;
        registeredResourceUV = IntPtr.Zero;

        try
        {
            helperTextureY = d3dDevice.CreateTexture2D(new Texture2DDescription(
                Format.R8_UNorm, (uint)_width, (uint)_height, 1, 1,
                BindFlags.ShaderResource, ResourceUsage.Default, CpuAccessFlags.None,
                1, 0, ResourceOptionFlags.None));

            helperTextureUV = d3dDevice.CreateTexture2D(new Texture2DDescription(
                Format.R8G8_UNorm, (uint)(_width / 2), (uint)(_height / 2), 1, 1,
                BindFlags.ShaderResource, ResourceUsage.Default, CpuAccessFlags.None,
                1, 0, ResourceOptionFlags.None));

            ThrowOnCudaError(cuCtxSetCurrent(_cudaCtx), nameof(cuCtxSetCurrent));

            var registerYResult = cuGraphicsD3D11RegisterResource(
                out registeredResourceY,
                helperTextureY.NativePointer,
                CU_GRAPHICS_REGISTER_FLAGS_NONE);
            if (registerYResult != CUDA_SUCCESS)
            {
                Logger.Log(
                    $"CUDA_D3D11_ZEROCOPY_REGISTER_FAIL plane=Y cuda_error={registerYResult} " +
                    $"width={_width} height={_height} fallback=staging");
                return false;
            }

            var registerUVResult = cuGraphicsD3D11RegisterResource(
                out registeredResourceUV,
                helperTextureUV.NativePointer,
                CU_GRAPHICS_REGISTER_FLAGS_NONE);
            if (registerUVResult != CUDA_SUCCESS)
            {
                TryUnregisterResource(registeredResourceY);
                registeredResourceY = IntPtr.Zero;
                Logger.Log(
                    $"CUDA_D3D11_ZEROCOPY_REGISTER_FAIL plane=UV cuda_error={registerUVResult} " +
                    $"width={_width} height={_height} fallback=staging");
                return false;
            }

            Logger.Log($"CUDA_D3D11_ZEROCOPY_REGISTER_OK width={_width} height={_height}");
            return true;
        }
        catch (Exception ex)
        {
            if (registeredResourceY != IntPtr.Zero)
            {
                TryUnregisterResource(registeredResourceY);
                registeredResourceY = IntPtr.Zero;
            }

            if (registeredResourceUV != IntPtr.Zero)
            {
                TryUnregisterResource(registeredResourceUV);
                registeredResourceUV = IntPtr.Zero;
            }

            helperTextureY?.Dispose();
            helperTextureY = null;
            helperTextureUV?.Dispose();
            helperTextureUV = null;

            Logger.Log($"CUDA_D3D11_ZEROCOPY_REGISTER_EXCEPTION type={ex.GetType().Name} msg={ex.Message} fallback=staging");
            return false;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        // 1. Unregister CUDA resources while context is still alive
        if (_registeredResourceY != IntPtr.Zero)
        {
            TryUnregisterResource(_registeredResourceY);
            _registeredResourceY = IntPtr.Zero;
        }

        if (_registeredResourceUV != IntPtr.Zero)
        {
            TryUnregisterResource(_registeredResourceUV);
            _registeredResourceUV = IntPtr.Zero;
        }

        // 2. Dispose D3D11 textures (CUDA no longer references them)
        _helperTextureY?.Dispose();
        _helperTextureY = null;
        _helperTextureUV?.Dispose();
        _helperTextureUV = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _defaultTexture?.Dispose();
        _defaultTexture = null;

        // 3. Release CUDA primary context after all CUDA and D3D11 resources are freed
        cuDevicePrimaryCtxRelease(_cuDevice);

        // 4. Release COM references obtained in the constructor. ImmediateContext and
        // QueryInterfaceOrNull both AddRef, so we must Dispose (Release) to balance.
        // Each consumer holds its own refcounted reference - releasing ours does not
        // affect other consumers (preview renderer, encoder) of the same device.
        _multithread?.Dispose();
        _deviceContext?.Dispose();

        _initialized = false;
        Logger.Log($"CUDA_D3D11_INTEROP_DISPOSED zero_copy={_zeroCopyAvailable}");
    }

    private void TryUnregisterResource(IntPtr resource)
    {
        try
        {
            cuCtxSetCurrent(_cudaCtx);
            cuGraphicsUnregisterResource(resource);
        }
        catch
        {
            // Best-effort: CUDA context or resource may already be destroyed during teardown - non-fatal
        }
    }
}

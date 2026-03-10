using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Vortice.Direct3D11;
using Vortice.DXGI;
using D3D11Device = Vortice.Direct3D11.ID3D11Device;
using D3D11DeviceContext = Vortice.Direct3D11.ID3D11DeviceContext;
using D3D11Multithread = Vortice.Direct3D11.ID3D11Multithread;
using D3D11Texture2D = Vortice.Direct3D11.ID3D11Texture2D;

namespace ElgatoCapture.Services;

/// <summary>
/// Copies NVDEC CUDA NV12 surfaces to a D3D11 NV12 texture for preview rendering.
/// Prefers zero-copy GPU-to-GPU path via two CUDA-registered helper textures and falls back
/// to a staging texture (cuMemcpy2D device->host + CopyResource) when CUDA-D3D11 interop setup fails.
/// </summary>
internal sealed unsafe class CudaD3D11InteropBridge : IDisposable
{
    private const uint CU_MEMORYTYPE_HOST = 1;
    private const uint CU_MEMORYTYPE_DEVICE = 2;
    private const uint CU_MEMORYTYPE_ARRAY = 3;
    private const uint CU_GRAPHICS_REGISTER_FLAGS_NONE = 0;
    private const int CUDA_SUCCESS = 0;

    private readonly IntPtr _cudaCtx;
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

        _cudaCtx = cudaCtx;
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

            defaultTex?.Dispose();
            helperTextureY?.Dispose();
            helperTextureUV?.Dispose();
            staging?.Dispose();
            throw;
        }
    }

    public IntPtr TextureNativePointer
        => (_defaultTexture ?? throw new ObjectDisposedException(nameof(CudaD3D11InteropBridge))).NativePointer;

    public void CopyFrameToTexture(AVFrame* cudaFrame)
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(CudaD3D11InteropBridge));

        if (!_initialized ||
            _defaultTexture == null ||
            (_zeroCopyAvailable
                ? _registeredResourceY == IntPtr.Zero || _registeredResourceUV == IntPtr.Zero || _helperTextureY == null || _helperTextureUV == null
                : _stagingTexture == null))
            throw new InvalidOperationException("Bridge not initialized.");

        if (cudaFrame == null)
            throw new ArgumentNullException(nameof(cudaFrame));

        if (cudaFrame->data[0] == null || cudaFrame->data[1] == null)
            throw new InvalidOperationException("CUDA frame missing NV12 plane pointers.");

        if (cudaFrame->linesize[0] <= 0 || cudaFrame->linesize[1] <= 0)
            throw new InvalidOperationException("CUDA frame missing valid NV12 plane pitches.");

        if (_zeroCopyAvailable)
            CopyFrameZeroCopy(cudaFrame);
        else
            CopyFrameStaging(cudaFrame);
    }

    private void CopyFrameZeroCopy(AVFrame* cudaFrame)
    {
        var helperTextureY = _helperTextureY ?? throw new InvalidOperationException("Y helper texture is unavailable.");
        var helperTextureUV = _helperTextureUV ?? throw new InvalidOperationException("UV helper texture is unavailable.");
        var defaultTexture = _defaultTexture ?? throw new InvalidOperationException("Default interop texture is unavailable.");
        if (_registeredResourceY == IntPtr.Zero || _registeredResourceUV == IntPtr.Zero)
            throw new InvalidOperationException("Zero-copy interop resource is unavailable.");

        var ctxPushed = false;
        var yMapped = false;
        var uvMapped = false;
        try
        {
            ThrowOnCudaError(cuCtxPushCurrent(_cudaCtx), nameof(cuCtxPushCurrent));
            ctxPushed = true;

            var resourceY = _registeredResourceY;
            ThrowOnCudaError(
                cuGraphicsMapResources(1, &resourceY, IntPtr.Zero),
                "cuGraphicsMapResources[Y]");
            yMapped = true;

            var resourceUV = _registeredResourceUV;
            ThrowOnCudaError(
                cuGraphicsMapResources(1, &resourceUV, IntPtr.Zero),
                "cuGraphicsMapResources[UV]");
            uvMapped = true;

            ThrowOnCudaError(
                cuGraphicsSubResourceGetMappedArray(out var yArray, resourceY, 0, 0),
                "cuGraphicsSubResourceGetMappedArray[Y]");

            var yCopy = new CUDA_MEMCPY2D
            {
                srcMemoryType = CU_MEMORYTYPE_DEVICE,
                srcDevice = (ulong)cudaFrame->data[0],
                srcPitch = (ulong)cudaFrame->linesize[0],
                dstMemoryType = CU_MEMORYTYPE_ARRAY,
                dstArray = yArray,
                WidthInBytes = (ulong)_width,
                Height = (ulong)_height
            };
            ThrowOnCudaError(cuMemcpy2D_v2(&yCopy), "cuMemcpy2D[Y]");

            ThrowOnCudaError(
                cuGraphicsSubResourceGetMappedArray(out var uvArray, resourceUV, 0, 0),
                "cuGraphicsSubResourceGetMappedArray[UV]");

            var uvCopy = new CUDA_MEMCPY2D
            {
                srcMemoryType = CU_MEMORYTYPE_DEVICE,
                srcDevice = (ulong)cudaFrame->data[1],
                srcPitch = (ulong)cudaFrame->linesize[1],
                dstMemoryType = CU_MEMORYTYPE_ARRAY,
                dstArray = uvArray,
                WidthInBytes = (ulong)_width,
                Height = (ulong)(_height / 2)
            };
            ThrowOnCudaError(cuMemcpy2D_v2(&uvCopy), "cuMemcpy2D[UV]");
        }
        finally
        {
            if (uvMapped)
            {
                var resourceUV = _registeredResourceUV;
                cuGraphicsUnmapResources(1, &resourceUV, IntPtr.Zero);
            }

            if (yMapped)
            {
                var resourceY = _registeredResourceY;
                cuGraphicsUnmapResources(1, &resourceY, IntPtr.Zero);
            }

            if (ctxPushed)
                cuCtxPopCurrent(out _);
        }

        _multithread.Enter();
        try
        {
            _deviceContext.CopySubresourceRegion(defaultTexture, 0, 0, 0, 0, helperTextureY, 0u);
            _deviceContext.CopySubresourceRegion(defaultTexture, 1, 0, 0, 0, helperTextureUV, 0u);
        }
        finally
        {
            _multithread.Leave();
        }

        if (Interlocked.Exchange(ref _diagDone, 1) == 0)
        {
            Logger.Log(
                "CUDA_D3D11_ZEROCOPY_DIAG " +
                $"y_src=0x{(ulong)cudaFrame->data[0]:X} uv_src=0x{(ulong)cudaFrame->data[1]:X} " +
                $"y_pitch={cudaFrame->linesize[0]} uv_pitch={cudaFrame->linesize[1]} " +
                $"width={_width} height={_height}");
        }
    }

    private void CopyFrameStaging(AVFrame* cudaFrame)
    {
        var stagingTexture = _stagingTexture ?? throw new InvalidOperationException("Staging interop resources are unavailable.");
        var defaultTexture = _defaultTexture ?? throw new InvalidOperationException("Default interop texture is unavailable.");

        // Map the staging NV12 texture (subresource 0 covers both Y and UV planes).
        // NV12 layout: Y plane at offset 0 (rowPitch x height), UV at rowPitch x height.
        _multithread.Enter();
        MappedSubresource mapped;
        try
        {
            mapped = _deviceContext.Map(stagingTexture, 0, MapMode.Write);
        }
        catch
        {
            _multithread.Leave();
            throw;
        }

        var yHostPtr = mapped.DataPointer;
        var uvHostPtr = mapped.DataPointer + (nint)(mapped.RowPitch * _height);

        // CUDA DtoH copy: device planes -> staging CPU memory
        var ctxPushed = false;
        try
        {
            ThrowOnCudaError(cuCtxPushCurrent(_cudaCtx), nameof(cuCtxPushCurrent));
            ctxPushed = true;

            // Y plane: width bytes x height rows
            var yCopy = new CUDA_MEMCPY2D
            {
                srcMemoryType = CU_MEMORYTYPE_DEVICE,
                srcDevice = (ulong)cudaFrame->data[0],
                srcPitch = (ulong)cudaFrame->linesize[0],
                dstMemoryType = CU_MEMORYTYPE_HOST,
                dstHost = yHostPtr,
                dstPitch = (ulong)mapped.RowPitch,
                WidthInBytes = (ulong)_width,
                Height = (ulong)_height
            };
            ThrowOnCudaError(cuMemcpy2D_v2(&yCopy), "cuMemcpy2D[Y]");

            // UV plane: width bytes x height/2 rows (interleaved U,V pairs)
            var uvCopy = new CUDA_MEMCPY2D
            {
                srcMemoryType = CU_MEMORYTYPE_DEVICE,
                srcDevice = (ulong)cudaFrame->data[1],
                srcPitch = (ulong)cudaFrame->linesize[1],
                dstMemoryType = CU_MEMORYTYPE_HOST,
                dstHost = uvHostPtr,
                dstPitch = (ulong)mapped.RowPitch,
                WidthInBytes = (ulong)_width,
                Height = (ulong)(_height / 2)
            };
            ThrowOnCudaError(cuMemcpy2D_v2(&uvCopy), "cuMemcpy2D[UV]");
        }
        finally
        {
            if (ctxPushed)
                cuCtxPopCurrent(out _);

            _deviceContext.Unmap(stagingTexture, 0);
            _multithread.Leave();
        }

        // GPU copy: staging NV12 -> default NV12
        _multithread.Enter();
        try
        {
            _deviceContext.CopyResource(defaultTexture, stagingTexture);
        }
        finally
        {
            _multithread.Leave();
        }

        if (Interlocked.Exchange(ref _diagDone, 1) == 0)
        {
            Logger.Log(
                "CUDA_D3D11_STAGING_COPY_DIAG " +
                $"y_src=0x{(ulong)cudaFrame->data[0]:X} uv_src=0x{(ulong)cudaFrame->data[1]:X} " +
                $"y_pitch={cudaFrame->linesize[0]} uv_pitch={cudaFrame->linesize[1]} " +
                $"staging_pitch={mapped.RowPitch} uv_offset={mapped.RowPitch * _height} " +
                $"width={_width} height={_height}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

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

        _defaultTexture?.Dispose();
        _defaultTexture = null;
        _helperTextureY?.Dispose();
        _helperTextureY = null;
        _helperTextureUV?.Dispose();
        _helperTextureUV = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _multithread?.Dispose();
        _deviceContext?.Dispose();
        _initialized = false;
        Logger.Log($"CUDA_D3D11_INTEROP_DISPOSED zero_copy={_zeroCopyAvailable}");
    }

    private void TryUnregisterResource(IntPtr resource)
    {
        var ctxPushed = false;
        try
        {
            ThrowOnCudaError(cuCtxPushCurrent(_cudaCtx), nameof(cuCtxPushCurrent));
            ctxPushed = true;
            cuGraphicsUnregisterResource(resource);
        }
        catch
        {
        }
        finally
        {
            if (ctxPushed)
                cuCtxPopCurrent(out _);
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
                BindFlags.None, ResourceUsage.Default, CpuAccessFlags.None,
                1, 0, ResourceOptionFlags.None));

            helperTextureUV = d3dDevice.CreateTexture2D(new Texture2DDescription(
                Format.R8G8_UNorm, (uint)(_width / 2), (uint)(_height / 2), 1, 1,
                BindFlags.None, ResourceUsage.Default, CpuAccessFlags.None,
                1, 0, ResourceOptionFlags.None));

            var ctxPushed = false;
            try
            {
                ThrowOnCudaError(cuCtxPushCurrent(_cudaCtx), nameof(cuCtxPushCurrent));
                ctxPushed = true;

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
            }
            finally
            {
                if (ctxPushed)
                    cuCtxPopCurrent(out _);
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

    private static void ThrowOnCudaError(int result, string api)
    {
        if (result != CUDA_SUCCESS)
            throw new InvalidOperationException($"{api} failed with CUDA error {result}.");
    }

    [DllImport("nvcuda.dll")]
    private static extern int cuCtxPushCurrent(IntPtr ctx);

    [DllImport("nvcuda.dll")]
    private static extern int cuCtxPopCurrent(out IntPtr ctx);

    [DllImport("nvcuda.dll")]
    private static extern int cuMemcpy2D_v2(CUDA_MEMCPY2D* pCopy);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsD3D11RegisterResource(
        out IntPtr pCudaResource,
        IntPtr pD3DResource,
        uint flags);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsUnregisterResource(IntPtr resource);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsMapResources(
        uint count,
        IntPtr* resources,
        IntPtr hStream);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsUnmapResources(
        uint count,
        IntPtr* resources,
        IntPtr hStream);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsSubResourceGetMappedArray(
        out IntPtr pArray,
        IntPtr resource,
        uint arrayIndex,
        uint mipLevel);

    [StructLayout(LayoutKind.Sequential)]
    private struct CUDA_MEMCPY2D
    {
        public ulong srcXInBytes;
        public ulong srcY;
        public uint srcMemoryType;
        public IntPtr srcHost;
        public ulong srcDevice;
        public IntPtr srcArray;
        public ulong srcPitch;
        public ulong dstXInBytes;
        public ulong dstY;
        public uint dstMemoryType;
        public IntPtr dstHost;
        public ulong dstDevice;
        public IntPtr dstArray;
        public ulong dstPitch;
        public ulong WidthInBytes;
        public ulong Height;
    }
}

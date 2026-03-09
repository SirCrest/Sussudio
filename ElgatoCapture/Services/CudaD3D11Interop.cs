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
/// Uses cuMemcpy2D (device→host) into a D3D11 staging texture, then CopyResource to default.
/// CUDA-D3D11 graphics interop cannot map NV12 textures to CUDA arrays, so we route
/// through a staging texture's mapped CPU memory instead. Cost: ~1ms PCIe per 4K frame.
/// </summary>
internal sealed unsafe class CudaD3D11InteropBridge : IDisposable
{
    private const uint CU_MEMORYTYPE_DEVICE = 2;
    private const uint CU_MEMORYTYPE_HOST = 1;
    private const int CUDA_SUCCESS = 0;

    private readonly IntPtr _cudaCtx;
    private readonly int _width;
    private readonly int _height;
    private readonly D3D11DeviceContext _deviceContext;
    private readonly D3D11Multithread _multithread;
    private D3D11Texture2D? _stagingTexture;
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

        D3D11Texture2D? staging = null;
        D3D11Texture2D? defaultTex = null;

        try
        {
            _multithread.SetMultithreadProtected(true);

            staging = d3dDevice.CreateTexture2D(new Texture2DDescription(
                Format.NV12, (uint)width, (uint)height, 1, 1,
                BindFlags.None, ResourceUsage.Staging, CpuAccessFlags.Write,
                1, 0, ResourceOptionFlags.None));

            defaultTex = d3dDevice.CreateTexture2D(new Texture2DDescription(
                Format.NV12, (uint)width, (uint)height, 1, 1,
                BindFlags.None, ResourceUsage.Default, CpuAccessFlags.None,
                1, 0, ResourceOptionFlags.None));

            _stagingTexture = staging;
            _defaultTexture = defaultTex;
            _initialized = true;
        }
        catch
        {
            defaultTex?.Dispose();
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

        if (!_initialized || _stagingTexture == null || _defaultTexture == null)
            throw new InvalidOperationException("Bridge not initialized.");

        if (cudaFrame == null)
            throw new ArgumentNullException(nameof(cudaFrame));

        if (cudaFrame->data[0] == null || cudaFrame->data[1] == null)
            throw new InvalidOperationException("CUDA frame missing NV12 plane pointers.");

        if (cudaFrame->linesize[0] <= 0 || cudaFrame->linesize[1] <= 0)
            throw new InvalidOperationException("CUDA frame missing valid NV12 plane pitches.");

        // Map the staging NV12 texture (subresource 0 covers both Y and UV planes).
        // NV12 layout: Y plane at offset 0 (rowPitch × height), UV at rowPitch × height.
        _multithread.Enter();
        MappedSubresource mapped;
        try
        {
            mapped = _deviceContext.Map(_stagingTexture, 0, MapMode.Write);
        }
        catch
        {
            _multithread.Leave();
            throw;
        }

        var yHostPtr = mapped.DataPointer;
        var uvHostPtr = mapped.DataPointer + (nint)(mapped.RowPitch * _height);

        // CUDA DtoH copy: device planes → staging CPU memory
        var ctxPushed = false;
        try
        {
            ThrowOnCudaError(cuCtxPushCurrent(_cudaCtx), nameof(cuCtxPushCurrent));
            ctxPushed = true;

            // Y plane: width bytes × height rows
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

            // UV plane: width bytes × height/2 rows (interleaved U,V pairs)
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

            _deviceContext.Unmap(_stagingTexture, 0);
            _multithread.Leave();
        }

        // GPU copy: staging NV12 → default NV12
        _multithread.Enter();
        try
        {
            _deviceContext.CopyResource(_defaultTexture, _stagingTexture);
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

        _defaultTexture?.Dispose();
        _defaultTexture = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _multithread?.Dispose();
        _deviceContext?.Dispose();
        _initialized = false;
        Logger.Log("CUDA_D3D11_INTEROP_DISPOSED");
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

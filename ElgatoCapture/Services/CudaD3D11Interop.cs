using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Vortice.DXGI;
using D3D11Device = Vortice.Direct3D11.ID3D11Device;
using D3D11Texture2D = Vortice.Direct3D11.ID3D11Texture2D;
using Vortice.Direct3D11;

namespace ElgatoCapture.Services;

internal sealed unsafe class CudaD3D11InteropBridge : IDisposable
{
    private const uint CU_MEMORYTYPE_DEVICE = 2;
    private const uint CU_MEMORYTYPE_ARRAY = 3;
    private const uint CU_GRAPHICS_REGISTER_FLAGS_NONE = 0;
    private const int CUDA_SUCCESS = 0;

    private readonly IntPtr _cudaCtx;
    private readonly int _width;
    private readonly int _height;
    private D3D11Texture2D? _texture;
    private IntPtr _registeredResource;
    private bool _initialized;
    private int _disposed;
    private int _diagDone;

    public CudaD3D11InteropBridge(IntPtr cudaCtx, D3D11Device d3dDevice, int width, int height)
    {
        if (cudaCtx == IntPtr.Zero)
        {
            throw new ArgumentException("CUDA context pointer is null.", nameof(cudaCtx));
        }

        ArgumentNullException.ThrowIfNull(d3dDevice);
        if (d3dDevice.NativePointer == IntPtr.Zero)
        {
            throw new ArgumentException("D3D11 device pointer is null.", nameof(d3dDevice));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        _cudaCtx = cudaCtx;
        _width = width;
        _height = height;

        IntPtr registeredResource = IntPtr.Zero;
        D3D11Texture2D? texture = null;
        var ctxPushed = false;

        try
        {
            var textureDescription = new Texture2DDescription(
                Format.NV12,
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

            texture = d3dDevice.CreateTexture2D(textureDescription);

            ThrowOnCudaError(cuCtxPushCurrent(cudaCtx), nameof(cuCtxPushCurrent));
            ctxPushed = true;

            ThrowOnCudaError(
                cuGraphicsD3D11RegisterResource(
                    out registeredResource,
                    texture.NativePointer,
                    CU_GRAPHICS_REGISTER_FLAGS_NONE),
                nameof(cuGraphicsD3D11RegisterResource));

            ThrowOnCudaError(cuCtxPopCurrent(out _), nameof(cuCtxPopCurrent));
            ctxPushed = false;

            _texture = texture;
            _registeredResource = registeredResource;
            _initialized = true;
        }
        catch (Exception ex)
        {
            Exception? cleanupEx = null;
            if (registeredResource != IntPtr.Zero)
            {
                if (!ctxPushed)
                {
                    var pushResult = cuCtxPushCurrent(cudaCtx);
                    if (pushResult != CUDA_SUCCESS)
                    {
                        cleanupEx = new InvalidOperationException(
                            $"{nameof(cuCtxPushCurrent)} failed with CUDA error {pushResult} during constructor cleanup.");
                    }
                    else
                    {
                        ctxPushed = true;
                    }
                }

                if (ctxPushed)
                {
                    var unregisterResult = cuGraphicsUnregisterResource(registeredResource);
                    if (unregisterResult != CUDA_SUCCESS)
                    {
                        cleanupEx ??= new InvalidOperationException(
                            $"{nameof(cuGraphicsUnregisterResource)} failed with CUDA error {unregisterResult} during constructor cleanup.");
                    }
                    else
                    {
                        registeredResource = IntPtr.Zero;
                    }
                }
            }

            if (ctxPushed)
            {
                var popResult = cuCtxPopCurrent(out _);
                if (popResult != CUDA_SUCCESS)
                {
                    cleanupEx ??= new InvalidOperationException(
                        $"{nameof(cuCtxPopCurrent)} failed with CUDA error {popResult} during constructor cleanup.");
                }
            }

            texture?.Dispose();
            if (cleanupEx != null)
            {
                throw new AggregateException(ex, cleanupEx);
            }

            throw;
        }
    }

    public IntPtr TextureNativePointer
        => (_texture ?? throw new ObjectDisposedException(nameof(CudaD3D11InteropBridge))).NativePointer;

    public void CopyFrameToTexture(AVFrame* cudaFrame)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(CudaD3D11InteropBridge));
        }

        if (!_initialized || _texture == null || _registeredResource == IntPtr.Zero)
        {
            throw new InvalidOperationException("CUDA-D3D11 interop bridge is not initialized.");
        }

        if (cudaFrame == null)
        {
            throw new ArgumentNullException(nameof(cudaFrame));
        }

        var resource = _registeredResource;
        var ctxPushed = false;
        var resourcesMapped = false;
        Exception? failure = null;

        try
        {
            ThrowOnCudaError(cuCtxPushCurrent(_cudaCtx), nameof(cuCtxPushCurrent));
            ctxPushed = true;

            ThrowOnCudaError(cuGraphicsMapResources(1, &resource, IntPtr.Zero), nameof(cuGraphicsMapResources));
            resourcesMapped = true;

            ThrowOnCudaError(
                cuGraphicsSubResourceGetMappedArray(out var yArray, resource, 0, 0),
                nameof(cuGraphicsSubResourceGetMappedArray));

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

            ThrowOnCudaError(cuMemcpy2D_v2(&yCopy), nameof(cuMemcpy2D_v2));

            ThrowOnCudaError(
                cuGraphicsSubResourceGetMappedArray(out var uvArray, resource, 1, 0),
                nameof(cuGraphicsSubResourceGetMappedArray));

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

            ThrowOnCudaError(cuMemcpy2D_v2(&uvCopy), nameof(cuMemcpy2D_v2));

            if (Interlocked.Exchange(ref _diagDone, 1) == 0)
            {
                Logger.Log(
                    "CUDA_D3D11_COPY_DIAG " +
                    $"y_src=0x{(ulong)cudaFrame->data[0]:X} uv_src=0x{(ulong)cudaFrame->data[1]:X} " +
                    $"y_pitch={cudaFrame->linesize[0]} uv_pitch={cudaFrame->linesize[1]} " +
                    $"width={_width} height={_height}");
            }
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        TryCleanup(resource, ref resourcesMapped, ref ctxPushed, out var cleanupEx);
        if (failure != null && cleanupEx != null)
        {
            throw new AggregateException(failure, cleanupEx);
        }

        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        if (cleanupEx != null)
        {
            throw cleanupEx;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_initialized && _registeredResource != IntPtr.Zero)
        {
            var pushResult = cuCtxPushCurrent(_cudaCtx);
            if (pushResult == CUDA_SUCCESS)
            {
                var unregisterResult = cuGraphicsUnregisterResource(_registeredResource);
                if (unregisterResult != CUDA_SUCCESS)
                {
                    Logger.Log($"CUDA_D3D11_INTEROP_DISPOSE_WARN stage=unregister code={unregisterResult}");
                }

                var popResult = cuCtxPopCurrent(out _);
                if (popResult != CUDA_SUCCESS)
                {
                    Logger.Log($"CUDA_D3D11_INTEROP_DISPOSE_WARN stage=pop code={popResult}");
                }
            }
            else
            {
                Logger.Log($"CUDA_D3D11_INTEROP_DISPOSE_WARN stage=push code={pushResult}");
            }

            _registeredResource = IntPtr.Zero;
        }

        _texture?.Dispose();
        _texture = null;
        _initialized = false;
        Logger.Log("CUDA_D3D11_INTEROP_DISPOSED");
    }

    private static void ThrowOnCudaError(int result, string api)
    {
        if (result != CUDA_SUCCESS)
        {
            throw new InvalidOperationException($"{api} failed with CUDA error {result}.");
        }
    }

    private static void TryCleanup(
        IntPtr resource,
        ref bool resourcesMapped,
        ref bool ctxPushed,
        out Exception? cleanupEx)
    {
        cleanupEx = null;

        if (resourcesMapped)
        {
            var unmapResult = cuGraphicsUnmapResources(1, &resource, IntPtr.Zero);
            if (unmapResult != CUDA_SUCCESS)
            {
                cleanupEx = new InvalidOperationException(
                    $"{nameof(cuGraphicsUnmapResources)} failed with CUDA error {unmapResult} during cleanup.");
            }

            resourcesMapped = false;
        }

        if (ctxPushed)
        {
            var popResult = cuCtxPopCurrent(out _);
            if (popResult != CUDA_SUCCESS)
            {
                cleanupEx ??= new InvalidOperationException(
                    $"{nameof(cuCtxPopCurrent)} failed with CUDA error {popResult} during cleanup.");
            }

            ctxPushed = false;
        }
    }

    [DllImport("nvcuda.dll")]
    private static extern int cuCtxPushCurrent(IntPtr ctx);

    [DllImport("nvcuda.dll")]
    private static extern int cuCtxPopCurrent(out IntPtr ctx);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsD3D11RegisterResource(out IntPtr resource, IntPtr d3d11Resource, uint flags);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsUnregisterResource(IntPtr resource);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsMapResources(uint count, IntPtr* resources, IntPtr stream);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsUnmapResources(uint count, IntPtr* resources, IntPtr stream);

    [DllImport("nvcuda.dll")]
    private static extern int cuGraphicsSubResourceGetMappedArray(out IntPtr array, IntPtr resource, uint arrayIndex, uint mipLevel);

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

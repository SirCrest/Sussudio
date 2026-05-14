using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class CudaD3D11InteropBridge
{
    private const uint CU_MEMORYTYPE_HOST = 1;
    private const uint CU_MEMORYTYPE_DEVICE = 2;
    private const uint CU_MEMORYTYPE_ARRAY = 3;
    private const uint CU_GRAPHICS_REGISTER_FLAGS_NONE = 0;
    private const int CUDA_SUCCESS = 0;

    private static void ThrowOnCudaError(int result, string api)
    {
        if (result != CUDA_SUCCESS)
            throw new InvalidOperationException($"{api} failed with CUDA error {result}.");
    }

    [DllImport("nvcuda.dll")]
    private static extern int cuDeviceGet(out int device, int ordinal);

    [DllImport("nvcuda.dll")]
    private static extern int cuDevicePrimaryCtxRetain(out IntPtr pctx, int dev);

    [DllImport("nvcuda.dll")]
    private static extern int cuDevicePrimaryCtxRelease(int dev);

    [DllImport("nvcuda.dll")]
    private static extern int cuCtxSetCurrent(IntPtr ctx);

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

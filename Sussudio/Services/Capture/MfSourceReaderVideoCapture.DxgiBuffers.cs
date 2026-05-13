using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private static readonly Guid ID3D11Texture2DIid = new(
        0x6F15AAF2, 0xD208, 0x4E89, 0x9A, 0xB4, 0x48, 0x95, 0x35, 0xD3, 0x4F, 0x9C);

    private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)
    {
        gpuTexture = IntPtr.Zero;
        gpuSubresource = 0;
        if (!Volatile.Read(ref _sourceReaderD3DEnabled) || _dxgiDeviceManagerPtr == IntPtr.Zero)
        {
            return false;
        }

        if (buffer is not IMFDXGIBuffer dxgiBuffer)
        {
            if (Interlocked.CompareExchange(ref _dxgiBufferProbeDone, 1, 0) == 0)
            {
                Log(
                    "MF_SOURCE_READER_D3D_BUFFER_MISS " +
                    $"buffer_type={buffer.GetType().Name} fallback=cpu");
            }

            return false;
        }

        var textureIid = ID3D11Texture2DIid;
        var getResourceHr = dxgiBuffer.GetResource(ref textureIid, out gpuTexture);
        if (getResourceHr < 0 || gpuTexture == IntPtr.Zero)
        {
            var failureCount = Interlocked.Increment(ref _dxgiResourceFailureCount);
            if (failureCount <= 3)
            {
                Log(
                    "MF_SOURCE_READER_D3D_RESOURCE_FAIL " +
                    $"stage=GetResource hr=0x{getResourceHr:X8} fallback=cpu");
            }

            gpuTexture = IntPtr.Zero;
            return false;
        }

        var subresourceHr = dxgiBuffer.GetSubresourceIndex(out var subresource);
        if (subresourceHr < 0)
        {
            var failureCount = Interlocked.Increment(ref _dxgiResourceFailureCount);
            if (failureCount <= 3)
            {
                Log(
                    "MF_SOURCE_READER_D3D_RESOURCE_FAIL " +
                    $"stage=GetSubresourceIndex hr=0x{subresourceHr:X8} fallback=cpu");
            }

            Marshal.Release(gpuTexture);
            gpuTexture = IntPtr.Zero;
            return false;
        }

        gpuSubresource = unchecked((int)subresource);
        return true;
    }
}

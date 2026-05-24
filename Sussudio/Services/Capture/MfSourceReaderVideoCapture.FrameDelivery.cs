using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private static readonly Guid ID3D11Texture2DIid = new(
        0x6F15AAF2, 0xD208, 0x4E89, 0x9A, 0xB4, 0x48, 0x95, 0x35, 0xD3, 0x4F, 0x9C);

    private unsafe void DeliverFrame(
        IMFSample sample,
        RawFrameCallback? onFrame,
        DualFrameCallback? onDualFrame,
        long arrivalTick)
    {
#if DEBUG
        // One-shot vtable diagnostic — runs on the very first sample to compare
        // raw vtable dispatch vs managed COM interop dispatch. This definitively
        // reveals whether .NET's vtable slot calculation for IMFSample is correct.
        if (Interlocked.CompareExchange(ref _vtableDiagDone, 1, 0) == 0)
        {
            DiagnoseVtable(sample);
        }
#endif

        IMFMediaBuffer? buffer = null;
        try
        {
            if (Volatile.Read(ref _isCompressedMjpgOutput) && onDualFrame == null && onFrame != null)
            {
                var getBufferCountHr = sample.GetBufferCount(out var bufferCount);
                if (getBufferCountHr >= 0 && bufferCount == 1)
                {
                    var getBufferHr = sample.GetBufferByIndex(0, out buffer);
                    if (getBufferHr >= 0 && buffer != null)
                    {
                        DeliverRawFrameFromBuffer(buffer, onFrame!, arrivalTick);
                        return;
                    }
                }
            }

            var ctcbHr = sample.ConvertToContiguousBuffer(out buffer);
            if (ctcbHr < 0 || buffer == null)
            {
                var probeCount = Interlocked.Increment(ref _framesDropped);
                if (probeCount <= 3)
                {
                    Log($"MF_SOURCE_READER_BUFFER_PROBE ctcb_hr=0x{ctcbHr:X8} sample_type={sample.GetType().Name}");
                }
                return;
            }

            if (onDualFrame != null)
            {
                DeliverDualFrameFromBuffer(buffer, onDualFrame, onFrame, arrivalTick);
                return;
            }

            if (onFrame != null)
            {
                DeliverRawFrameFromBuffer(buffer, onFrame, arrivalTick);
            }
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref buffer);
        }
    }

    private unsafe void DeliverDualFrameFromBuffer(
        IMFMediaBuffer buffer,
        DualFrameCallback onDualFrame,
        RawFrameCallback? fallbackRawFrame,
        long arrivalTick)
    {
        var hasTexture = TryGetDxgiTexture(buffer, out var gpuTexture, out var gpuSubresource);
        if (!hasTexture && Volatile.Read(ref _strictTextureOutputRequired))
        {
            throw new InvalidOperationException("4K120 MJPG mode requires D3D11 texture delivery for preview.");
        }

        if (!hasTexture && fallbackRawFrame != null)
        {
            DeliverRawFrameFromBuffer(buffer, fallbackRawFrame, arrivalTick);
            return;
        }

        try
        {
            if (hasTexture && Volatile.Read(ref _skipCpuReadback))
            {
                try
                {
                    onDualFrame(gpuTexture, gpuSubresource, ReadOnlySpan<byte>.Empty, _width, _height, arrivalTick);
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Log($"MF_SOURCE_READER_GPU_ONLY_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (TryDeliverDualFrameFrom2DBuffer(buffer, gpuTexture, gpuSubresource, onDualFrame, arrivalTick))
            {
                return;
            }

            MfInteropHelpers.ThrowIfFailed(
                buffer.Lock(out var dataPtr, out _, out var curLen),
                "IMFMediaBuffer.Lock");
            try
            {
                if (dataPtr == IntPtr.Zero || curLen <= 0)
                {
                    Interlocked.Increment(ref _framesDropped);
                    return;
                }

                var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
                if (packedFrameBytes <= 0)
                {
                    throw new InvalidOperationException("Invalid frame dimensions.");
                }

                if (curLen < packedFrameBytes)
                {
                    throw new InvalidOperationException(
                        $"Media buffer length ({curLen}) is smaller than expected frame size ({packedFrameBytes}).");
                }

                var expectedStride = GetRowBytes(_width, _isP010);
                var inferredStride = InferPackedStride(curLen, _height);
                if (inferredStride > expectedStride)
                {
                    var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
                    try
                    {
                        var packedSpan = packed.AsSpan(0, packedFrameBytes);
                        CopyYuvWithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height, _isP010);

                        onDualFrame(gpuTexture, gpuSubresource, packedSpan, _width, _height, arrivalTick);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(packed);
                    }
                }
                else
                {
                    onDualFrame(gpuTexture, gpuSubresource, new ReadOnlySpan<byte>((void*)dataPtr, packedFrameBytes), _width, _height, arrivalTick);
                }
            }
            finally
            {
                _ = buffer.Unlock();
            }
        }
        finally
        {
            if (gpuTexture != IntPtr.Zero)
            {
                Marshal.Release(gpuTexture);
            }
        }
    }

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

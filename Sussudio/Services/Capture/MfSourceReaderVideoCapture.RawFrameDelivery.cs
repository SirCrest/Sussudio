using System;
using System.Buffers;
using System.Threading;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)
    {
        if (Volatile.Read(ref _isCompressedMjpgOutput))
        {
            MfInteropHelpers.ThrowIfFailed(
                buffer.Lock(out var compressedDataPtr, out _, out var compressedLength),
                "IMFMediaBuffer.Lock");

            byte[]? rentedBuffer = null;
            int jpegLength = 0;
            try
            {
                if (compressedDataPtr == IntPtr.Zero || compressedLength <= 0)
                {
                    Interlocked.Increment(ref _framesDropped);
                    return;
                }

                // Copy MJPG bytes out so we release the source reader's USB buffer slot
                // before running the decode + preview + recording pipeline.
                rentedBuffer = ArrayPool<byte>.Shared.Rent(compressedLength);
                jpegLength = compressedLength;
                new ReadOnlySpan<byte>((void*)compressedDataPtr, compressedLength)
                    .CopyTo(rentedBuffer);
            }
            finally
            {
                _ = buffer.Unlock();
            }

            try
            {
                onFrame(new ReadOnlySpan<byte>(rentedBuffer, 0, jpegLength), _width, _height, arrivalTick);
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            return;
        }

        if (TryDeliverFrameFrom2DBuffer(buffer, onFrame, arrivalTick))
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

                    onFrame(packedSpan, _width, _height, arrivalTick);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packed);
                }
            }
            else
            {
                onFrame(new ReadOnlySpan<byte>((void*)dataPtr, packedFrameBytes), _width, _height, arrivalTick);
            }
        }
        finally
        {
            _ = buffer.Unlock();
        }
    }

    private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)
    {
        if (buffer is not IMF2DBuffer buffer2D)
        {
            return false;
        }

        MfInteropHelpers.ThrowIfFailed(
            buffer2D.Lock2D(out var scanlinePtr, out var pitch),
            "IMF2DBuffer.Lock2D");
        try
        {
            if (scanlinePtr == IntPtr.Zero)
            {
                Interlocked.Increment(ref _framesDropped);
                return true;
            }

            var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
            if (packedFrameBytes <= 0)
            {
                throw new InvalidOperationException("Invalid frame dimensions.");
            }

            var expectedStride = GetRowBytes(_width, _isP010);
            if (pitch == expectedStride)
            {
                onFrame(new ReadOnlySpan<byte>((void*)scanlinePtr, packedFrameBytes), _width, _height, arrivalTick);
                return true;
            }

            var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
            try
            {
                var packedSpan = packed.AsSpan(0, packedFrameBytes);
                CopyYuvWithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height, _isP010);

                onFrame(packedSpan, _width, _height, arrivalTick);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packed);
            }

            return true;
        }
        finally
        {
            _ = buffer2D.Unlock2D();
        }
    }

    private unsafe bool TryDeliverDualFrameFrom2DBuffer(
        IMFMediaBuffer buffer,
        IntPtr gpuTexture,
        int gpuSubresource,
        DualFrameCallback onFrame,
        long arrivalTick)
    {
        if (buffer is not IMF2DBuffer buffer2D)
        {
            return false;
        }

        MfInteropHelpers.ThrowIfFailed(
            buffer2D.Lock2D(out var scanlinePtr, out var pitch),
            "IMF2DBuffer.Lock2D");
        try
        {
            if (scanlinePtr == IntPtr.Zero)
            {
                Interlocked.Increment(ref _framesDropped);
                return true;
            }

            var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
            if (packedFrameBytes <= 0)
            {
                throw new InvalidOperationException("Invalid frame dimensions.");
            }

            var expectedStride = GetRowBytes(_width, _isP010);
            if (pitch == expectedStride)
            {
                onFrame(gpuTexture, gpuSubresource, new ReadOnlySpan<byte>((void*)scanlinePtr, packedFrameBytes), _width, _height, arrivalTick);
                return true;
            }

            var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
            try
            {
                var packedSpan = packed.AsSpan(0, packedFrameBytes);
                CopyYuvWithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height, _isP010);

                onFrame(gpuTexture, gpuSubresource, packedSpan, _width, _height, arrivalTick);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packed);
            }

            return true;
        }
        finally
        {
            _ = buffer2D.Unlock2D();
        }
    }
}

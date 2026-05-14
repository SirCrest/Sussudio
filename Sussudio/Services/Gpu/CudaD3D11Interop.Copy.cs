using System;
using System.Threading;
using FFmpeg.AutoGen;
using Vortice.Direct3D11;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class CudaD3D11InteropBridge
{
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
        _ = _helperTextureY ?? throw new InvalidOperationException("Y helper texture is unavailable.");
        _ = _helperTextureUV ?? throw new InvalidOperationException("UV helper texture is unavailable.");
        if (_registeredResourceY == IntPtr.Zero || _registeredResourceUV == IntPtr.Zero)
            throw new InvalidOperationException("Zero-copy interop resource is unavailable.");

        if (Interlocked.Exchange(ref _ctxDiagDone, 1) == 0)
        {
            Logger.Log($"CUDA_D3D11_CTX_PRE_SET ctx=0x{(long)_cudaCtx:X} thread={Environment.CurrentManagedThreadId}");
        }

        // Acquire the D3D11 device's multithread critical section.
        // cuGraphicsMapResources internally touches the D3D11 immediate context,
        // which races with UI-thread rendering (swap chain present, shader draw).
        // _multithread.Enter/Leave uses the same CS that D3D11 runtime uses,
        // so the UI thread's rendering is properly serialized against us.
        _multithread.Enter();
        var yMapped = false;
        var uvMapped = false;
        try
        {
            ThrowOnCudaError(cuCtxSetCurrent(_cudaCtx), nameof(cuCtxSetCurrent));

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

        // Hold the D3D11 multithread lock across Map, CUDA copy, Unmap, and CopyResource
        // to prevent another thread from using the device context in between.
        _multithread.Enter();
        try
        {
            // Map the staging NV12 texture (subresource 0 covers both Y and UV planes).
            // NV12 layout: Y plane at offset 0 (rowPitch x height), UV at rowPitch x height.
            var mapped = _deviceContext.Map(stagingTexture, 0, MapMode.Write);

            var yHostPtr = mapped.DataPointer;
            var uvHostPtr = mapped.DataPointer + (nint)(mapped.RowPitch * _height);

            // CUDA DtoH copy: device planes -> staging CPU memory
            try
            {
                if (Interlocked.Exchange(ref _ctxDiagDone, 1) == 0)
                {
                    Logger.Log($"CUDA_D3D11_CTX_PRE_SET ctx=0x{(long)_cudaCtx:X} thread={Environment.CurrentManagedThreadId}");
                }

                ThrowOnCudaError(cuCtxSetCurrent(_cudaCtx), nameof(cuCtxSetCurrent));

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
                _deviceContext.Unmap(stagingTexture, 0);
            }

            // GPU copy: staging NV12 -> default NV12 (still under lock)
            _deviceContext.CopyResource(defaultTexture, stagingTexture);

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
        finally
        {
            _multithread.Leave();
        }
    }
}

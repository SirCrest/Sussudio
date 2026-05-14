using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class NvdecMjpegDecoder
{
    /// <summary>
    /// Download a CUDA frame to packed CPU NV12 bytes for preview rendering.
    /// </summary>
    public bool TryDownloadToCpu(AVFrame* cudaFrame, out IntPtr nv12Data, out int nv12Size)
    {
        nv12Data = IntPtr.Zero;
        nv12Size = 0;

        if (cudaFrame == null || _cpuFrame == null)
        {
            return false;
        }

        var writableResult = ffmpeg.av_frame_make_writable(_cpuFrame);
        if (writableResult < 0)
        {
            Logger.Log($"NVDEC_CPU_MAKE_WRITABLE_FAIL code={writableResult} msg='{GetErrorString(writableResult)}'");
            return false;
        }

        var transferResult = ffmpeg.av_hwframe_transfer_data(_cpuFrame, cudaFrame, 0);
        if (transferResult < 0)
        {
            Logger.Log($"NVDEC_CPU_TRANSFER_FAIL code={transferResult} msg='{GetErrorString(transferResult)}'");
            return false;
        }

        // One-shot diagnostic: log frame formats and strides after first download
        if (Interlocked.Exchange(ref _downloadDiagDone, 1) == 0)
        {
            Logger.Log(
                $"NVDEC_CPU_DOWNLOAD_DIAG cuda_fmt={(AVPixelFormat)cudaFrame->format} cuda_w={cudaFrame->width} cuda_h={cudaFrame->height} " +
                $"cpu_fmt={(AVPixelFormat)_cpuFrame->format} cpu_w={_cpuFrame->width} cpu_h={_cpuFrame->height} " +
                $"y_stride={_cpuFrame->linesize[0]} uv_stride={_cpuFrame->linesize[1]} " +
                $"y_data=0x{(long)_cpuFrame->data[0]:X} uv_data=0x{(long)_cpuFrame->data[1]:X} " +
                $"y_uv_gap={(_cpuFrame->data[1] - _cpuFrame->data[0])} expected_y_size={_cpuFrame->linesize[0] * _cpuFrame->height}");
        }

        var packedSize = checked((_width * _height * 3) / 2);
        var yStride = _cpuFrame->linesize[0];
        var uvStride = _cpuFrame->linesize[1];
        var yPlaneSize = yStride * _height;
        var uvPlaneExpectedStart = _cpuFrame->data[0] + yPlaneSize;

        // Fast path: only when stride matches width AND planes are truly contiguous
        // (av_frame_get_buffer may add alignment padding between Y and UV planes)
        if (yStride == _width && uvStride == _width && _cpuFrame->data[1] == uvPlaneExpectedStart)
        {
            nv12Data = (IntPtr)_cpuFrame->data[0];
            nv12Size = packedSize;
            return true;
        }

        // Slow path: copy both planes to a packed contiguous buffer
        EnsurePackedBufferCapacity(packedSize);

        var destination = (byte*)_packedCpuBuffer;
        CopyPlane(_cpuFrame->data[0], yStride, destination, _width, _width, _height);
        CopyPlane(
            _cpuFrame->data[1],
            uvStride,
            destination + (_width * _height),
            _width,
            _width,
            _height / 2);

        nv12Data = _packedCpuBuffer;
        nv12Size = packedSize;
        return true;
    }

    private void EnsurePackedBufferCapacity(int requiredSize)
    {
        if (_packedCpuBufferSize >= requiredSize && _packedCpuBuffer != IntPtr.Zero)
        {
            return;
        }

        if (_packedCpuBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_packedCpuBuffer);
        }

        _packedCpuBuffer = Marshal.AllocHGlobal(requiredSize);
        _packedCpuBufferSize = requiredSize;
    }

    private static void CopyPlane(byte* source, int sourceStride, byte* destination, int destinationStride, int rowBytes, int rowCount)
    {
        for (var row = 0; row < rowCount; row++)
        {
            Buffer.MemoryCopy(
                source + (row * sourceStride),
                destination + (row * destinationStride),
                rowBytes,
                rowBytes);
        }
    }

}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using FFmpeg.AutoGen;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        Exception? overloadFailure = null;
        lock (_videoQueueSync)
        {
            if (!_started ||
                _cts?.IsCancellationRequested == true ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                ReturnVideoPacket(packet);
                return VideoEnqueueResult.Rejected;
            }

            if (TryWriteVideoPacket(queue, packet))
            {
                _videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick);
                Interlocked.Increment(ref _videoFramesEnqueued);
                SignalWork("video_enqueue");
                return VideoEnqueueResult.Accepted;
            }

            if (!_started ||
                _cts?.IsCancellationRequested == true ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                ReturnVideoPacket(packet);
                return VideoEnqueueResult.Rejected;
            }

            Interlocked.Increment(ref _droppedVideoFrames);
            overloadFailure = new InvalidOperationException(
                $"LibAv recording video queue overloaded: capacity={VideoQueueCapacity} depth={Volatile.Read(ref _videoQueueDepth)}");
            ReturnVideoPacket(packet);
        }

        FailEncoding(overloadFailure);
        return VideoEnqueueResult.Overloaded;
    }

    private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            Marshal.Release(packet.Texture);
            return VideoEnqueueResult.Rejected;
        }

        if (TryWriteGpuPacket(queue, packet))
        {
            Interlocked.Increment(ref _gpuFramesEnqueued);
            SignalWork("gpu_enqueue");
            return VideoEnqueueResult.Accepted;
        }

        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            Marshal.Release(packet.Texture);
            return VideoEnqueueResult.Rejected;
        }

        Marshal.Release(packet.Texture);
        FailEncoding(new InvalidOperationException(
            $"LibAv GPU recording queue overloaded: capacity={GpuQueueCapacity} depth={Volatile.Read(ref _gpuQueueDepth)}"));
        return VideoEnqueueResult.Overloaded;
    }

    private unsafe VideoEnqueueResult TryEnqueueCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)
    {
        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            var rejectedFrame = (AVFrame*)packet.Frame;
            if (rejectedFrame != null)
            {
                ffmpeg.av_frame_free(&rejectedFrame);
            }

            return VideoEnqueueResult.Rejected;
        }

        if (TryWriteCudaPacket(queue, packet))
        {
            Interlocked.Increment(ref _cudaFramesEnqueued);
            SignalWork("cuda_enqueue");
            return VideoEnqueueResult.Accepted;
        }

        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            var frame = (AVFrame*)packet.Frame;
            if (frame != null)
            {
                ffmpeg.av_frame_free(&frame);
            }

            return VideoEnqueueResult.Rejected;
        }

        var overloadedFrame = (AVFrame*)packet.Frame;
        if (overloadedFrame != null)
        {
            ffmpeg.av_frame_free(&overloadedFrame);
        }

        FailEncoding(new InvalidOperationException(
            $"LibAv CUDA recording queue overloaded: capacity={CudaQueueCapacity} depth={Volatile.Read(ref _cudaQueueDepth)}"));
        return VideoEnqueueResult.Overloaded;
    }

    private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        var depth = Interlocked.Increment(ref _videoQueueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref _videoQueueMaxDepth, depth);
            return true;
        }

        DecrementQueueDepth(ref _videoQueueDepth, "video_write_failed");
        return false;
    }

    private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        var depth = Interlocked.Increment(ref _gpuQueueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref _gpuQueueMaxDepth, depth);
            return true;
        }

        DecrementQueueDepth(ref _gpuQueueDepth, "gpu_write_failed");
        return false;
    }

    private bool TryWriteCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)
    {
        var depth = Interlocked.Increment(ref _cudaQueueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref _cudaQueueMaxDepth, depth);
            return true;
        }

        DecrementQueueDepth(ref _cudaQueueDepth, "cuda_write_failed");
        return false;
    }

    private readonly record struct VideoFramePacket(byte[]? Buffer, PooledVideoFrameLease? Lease, int Length, long EnqueueTick, long? SequenceNumber)
    {
        public static VideoFramePacket Frame(byte[] buffer, int length, long enqueueTick) => new(buffer, null, length, enqueueTick, null);
        public static VideoFramePacket Frame(PooledVideoFrameLease lease, long enqueueTick) => new(null, lease, lease.Length, enqueueTick, lease.SequenceNumber);
    }

    private enum VideoEnqueueResult
    {
        Accepted,
        Rejected,
        Overloaded
    }

    private readonly record struct GpuFramePacket(IntPtr Texture, int Subresource);
    private readonly record struct CudaFramePacket(IntPtr Frame);
}

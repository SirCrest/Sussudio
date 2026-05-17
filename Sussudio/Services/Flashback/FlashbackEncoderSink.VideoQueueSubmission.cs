using System;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        lock (_videoQueueSync)
        {
            var rejectReason = GetVideoEnqueueRejectReason(isGpu: false);
            if (rejectReason != null)
            {
                ReturnVideoPacket(packet);
                TrackVideoQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            if (TryWriteVideoPacket(queue, packet))
            {
                _videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick);
                Interlocked.Increment(ref _videoFramesEnqueued);
                SignalWork("video_enqueue");
                return VideoEnqueueResult.Accepted;
            }

            rejectReason = GetVideoEnqueueRejectReason(isGpu: false);
            if (rejectReason != null)
            {
                ReturnVideoPacket(packet);
                TrackVideoQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            Interlocked.Increment(ref _droppedVideoFrames);
            ReturnVideoPacket(packet);
            TrackVideoQueueRejected("queue_full");
            return VideoEnqueueResult.Overloaded;
        }
    }

    private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        lock (_videoQueueSync)
        {
            var rejectReason = GetVideoEnqueueRejectReason(isGpu: true);
            if (rejectReason != null)
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
                TrackGpuQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            if (TryWriteGpuPacket(queue, packet))
            {
                Interlocked.Increment(ref _gpuFramesEnqueued);
                SignalWork("gpu_enqueue");
                return VideoEnqueueResult.Accepted;
            }

            rejectReason = GetVideoEnqueueRejectReason(isGpu: true);
            if (rejectReason != null)
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
                TrackGpuQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            ReleaseGpuTextureBestEffort(packet.Texture);
            TrackGpuQueueRejected("queue_full");
            return VideoEnqueueResult.Overloaded;
        }
    }

    private string? GetVideoEnqueueRejectReason(bool isGpu)
    {
        if (_disposed)
        {
            return "disposed";
        }

        if (!_started)
        {
            return "not_started";
        }

        if (_cts?.IsCancellationRequested == true)
        {
            return "cancelled";
        }

        if (Volatile.Read(ref _forceRotateDraining))
        {
            return "force_rotate_draining";
        }

        var failure = Volatile.Read(ref _encodingFailure);
        return failure != null
            ? $"encoding_failed:{failure.GetType().Name}"
            : null;
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

    private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)
    {
        var lifecycleReason = GetVideoEnqueueRejectReason(isGpu: false);
        if (lifecycleReason != null)
        {
            return lifecycleReason;
        }

        if (queue == null)
        {
            return "queue_null";
        }

        if (expectedSize <= 0)
        {
            return "invalid_expected_size";
        }

        return dataIsEmpty ? "data_empty" : null;
    }

    private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)
    {
        var lifecycleReason = GetVideoEnqueueRejectReason(isGpu: true);
        if (lifecycleReason != null)
        {
            return lifecycleReason;
        }

        if (queue == null)
        {
            return "queue_null";
        }

        return texture == IntPtr.Zero ? "null_texture" : null;
    }

    private void TrackVideoQueueRejected(string reason)
    {
        Volatile.Write(ref _lastVideoQueueRejectReason, reason);
        var total = Interlocked.Increment(ref _videoQueueRejectedFrames);
        if (total == 1 || total % 30 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_VIDEO_QUEUE_REJECT reason={reason} total={total} depth={Volatile.Read(ref _videoQueueDepth)} capacity={VideoQueueCapacityFrames}");
        }
    }

    private void TrackGpuQueueRejected(string reason)
    {
        Volatile.Write(ref _lastGpuQueueRejectReason, reason);
        var total = Interlocked.Increment(ref _gpuQueueRejectedFrames);
        if (total == 1 || total % 30 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_GPU_QUEUE_REJECT reason={reason} total={total} depth={Volatile.Read(ref _gpuQueueDepth)} capacity={GpuQueueCapacity}");
        }
    }
}

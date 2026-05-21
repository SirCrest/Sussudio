using System.Threading;
using System.Threading.Channels;

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
}

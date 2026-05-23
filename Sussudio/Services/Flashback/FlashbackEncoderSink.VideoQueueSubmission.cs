using System;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio);

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

    private bool TryEnqueueAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        ref long backlogEvictions)
    {
        lock (_videoQueueSync)
        {
            if (_disposed ||
                !_started ||
                _cts?.IsCancellationRequested == true ||
                (Volatile.Read(ref _forceRotateDraining) &&
                 IsForceRotateQueueGuarded(Volatile.Read(ref queueDepth), AudioQueueCapacity)) ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                ReturnBuffer(packet.Buffer);
                return false;
            }

            if (TryWriteAudioPacket(queue, packet, ref queueDepth, "audio"))
            {
                SignalWork("audio_enqueue");
                return true;
            }

            if (queue.Reader.TryRead(out var evictedPacket))
            {
                DecrementQueueDepth(ref queueDepth, "audio_evict");
                Interlocked.Increment(ref backlogEvictions);
                // Track dropped audio samples for A/V drift diagnostics (analogous to SkipVideoFrame for video)
                var evictedSamples = GetSampleCount(evictedPacket.Length);
                var totalDropped = Interlocked.Add(ref _droppedAudioSamplesCount, evictedSamples);
                if (totalDropped == evictedSamples || totalDropped % 48_000 < evictedSamples)
                {
                    Logger.Log(
                        $"FLASHBACK_SINK_AUDIO_EVICT_PTS samples={evictedSamples} total_dropped_samples={totalDropped} " +
                        $"drift_ms={totalDropped * 1000.0 / 48_000:F1}");
                }

                ReturnBuffer(evictedPacket.Buffer);
                if (TryWriteAudioPacket(queue, packet, ref queueDepth, "audio_after_evict"))
                {
                    SignalWork("audio_after_evict");
                    return true;
                }
            }

            // Total saturation: both eviction and re-enqueue failed.
            var saturatedSamples = GetSampleCount(packet.Length);
            Interlocked.Add(ref _droppedAudioSamplesCount, saturatedSamples);
            ReturnBuffer(packet.Buffer);
            return false;
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

    private static bool TryWriteAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        string queueName)
    {
        Interlocked.Increment(ref queueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            return true;
        }

        DecrementQueueDepth(ref queueDepth, $"{queueName}_write_failed");
        return false;
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

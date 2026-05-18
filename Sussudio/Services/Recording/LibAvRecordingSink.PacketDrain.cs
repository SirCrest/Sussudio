using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets)
        {
            VideoFramePacket packet;
            lock (_videoQueueSync)
            {
                if (!reader.TryRead(out packet))
                {
                    break;
                }

                _videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick);
                DecrementQueueDepth(ref _videoQueueDepth, "video");
            }

            _videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber);
            try
            {
                var frameData = packet.Lease != null
                    ? packet.Lease.Memory.Span
                    : packet.Buffer!.AsSpan(0, packet.Length);
                _encoder.SendVideoFrame(frameData, _width, _height);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            finally
            {
                ReturnVideoPacket(packet);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _gpuQueueDepth, "gpu");
            try
            {
                _encoder.SendGpuVideoFrame(packet.Texture, packet.Subresource);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            finally
            {
                Marshal.Release(packet.Texture);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _cudaQueueDepth, "cuda");
            var frame = (AVFrame*)packet.Frame;
            try
            {
                _encoder.SendCudaVideoFrame(frame);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"LIBAV_SINK_CUDA_DRAIN_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                if (frame != null)
                {
                    ffmpeg.av_frame_free(&frame);
                }
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _audioQueueDepth, "audio");
            try
            {
                _encoder.SendAudioSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _microphoneQueueDepth, "microphone");
            try
            {
                _encoder.SendMicrophoneSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }
}

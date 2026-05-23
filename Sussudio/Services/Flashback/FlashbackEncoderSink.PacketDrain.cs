using System;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var w = _width;
        var h = _height;
        if (w <= 0 || h <= 0)
        {
            return false;
        }

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
                var expectedFrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(w, h, packet.IsP010);
                // Defense-in-depth: if a stale frame from a previous resolution
                // leaks through during a reinit cycle, drop it rather than sending
                // mismatched dimensions to the encoder, which could crash in native code.
                if (expectedFrameSize > 0 && packet.Length != expectedFrameSize)
                {
                    Interlocked.Increment(ref _droppedVideoFrames);
                    Logger.Log($"FLASHBACK_SINK_FRAME_SIZE_MISMATCH expected={expectedFrameSize} actual={packet.Length} w={w} h={h} p010={packet.IsP010}");
                    continue;
                }

                var frameData = packet.Lease != null
                    ? packet.Lease.Memory.Span
                    : packet.Buffer!.AsSpan(0, packet.Length);
                _encoder.SendVideoFrame(frameData, w, h);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                OnVideoFrameEncoded();
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
                OnVideoFrameEncoded();
            }
            finally
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
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
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
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
            drainedCount++;
        }

        return drainedAny;
    }
}

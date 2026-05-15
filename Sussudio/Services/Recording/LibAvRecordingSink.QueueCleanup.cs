using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private void ReturnRemainingVideoBuffers(Channel<VideoFramePacket>? queue)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReturnVideoPacket(packet);
        }

        lock (_videoQueueSync)
        {
            _videoLatencyTracker.ClearEnqueueTicksUnderLock();
        }

        Interlocked.Exchange(ref _videoQueueDepth, 0);
    }

    private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            Marshal.Release(packet.Texture);
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private static unsafe void ReturnRemainingCudaFrames(Channel<CudaFramePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            var frame = (AVFrame*)packet.Frame;
            if (frame != null)
            {
                ffmpeg.av_frame_free(&frame);
            }
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private static byte[] GetBuffer(int size)
    {
        return ArrayPool<byte>.Shared.Rent(size);
    }

    private static void ReturnBuffer(byte[] buffer)
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    private static void ReturnVideoPacket(VideoFramePacket packet)
    {
        if (packet.Buffer != null)
        {
            ReturnBuffer(packet.Buffer);
        }

        packet.Lease?.Dispose();
    }
}

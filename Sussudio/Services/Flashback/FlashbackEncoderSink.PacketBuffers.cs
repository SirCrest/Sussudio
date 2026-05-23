using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private readonly record struct VideoFramePacket(byte[]? Buffer, PooledVideoFrameLease? Lease, int Length, long EnqueueTick, long? SequenceNumber, bool IsP010)
    {
        public static VideoFramePacket Frame(byte[] buffer, int length, long enqueueTick, bool isP010) => new(buffer, null, length, enqueueTick, null, isP010);
        public static VideoFramePacket Frame(PooledVideoFrameLease lease, long enqueueTick) => new(null, lease, lease.Length, enqueueTick, lease.SequenceNumber, lease.PixelFormat == PooledVideoPixelFormat.P010);
    }

    private enum VideoEnqueueResult
    {
        Accepted,
        Rejected,
        Overloaded
    }

    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
    private readonly record struct GpuFramePacket(IntPtr Texture, int Subresource);

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

    private static void ReturnVideoPacketBestEffort(VideoFramePacket packet)
    {
        try
        {
            ReturnVideoPacket(packet);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static void ReleaseGpuTextureBestEffort(IntPtr texture)
    {
        if (texture == IntPtr.Zero)
        {
            return;
        }

        try
        {
            Marshal.Release(texture);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }
}

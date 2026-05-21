using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
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

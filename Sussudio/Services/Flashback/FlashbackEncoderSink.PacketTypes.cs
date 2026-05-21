using System;
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
}

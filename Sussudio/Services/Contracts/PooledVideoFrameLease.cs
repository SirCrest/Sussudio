using System;
using System.Threading;

namespace Sussudio.Services.Contracts;

// Pixel formats carried by pooled decoded frames. The enum is deliberately
// narrow because the pooled-frame path currently transports luma/chroma capture
// buffers, not arbitrary RGB render targets.
internal enum PooledVideoPixelFormat
{
    Unknown = 0,
    Nv12 = 1,
    P010 = 2
}

// Read-only consumer lease over a PooledVideoFrame. Lease metadata is copied at
// creation so diagnostics can still identify the frame after Dispose releases
// the underlying pooled bytes.
internal sealed class PooledVideoFrameLease : IDisposable
{
    private PooledVideoFrame? _frame;

    internal PooledVideoFrameLease(PooledVideoFrame frame)
    {
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        SequenceNumber = frame.SequenceNumber;
        ArrivalTick = frame.ArrivalTick;
        DecodedTick = frame.DecodedTick;
        Width = frame.Width;
        Height = frame.Height;
        PixelFormat = frame.PixelFormat;
        Length = frame.Length;
    }

    public long SequenceNumber { get; }
    public long ArrivalTick { get; }
    public long DecodedTick { get; }
    public int Width { get; }
    public int Height { get; }
    public PooledVideoPixelFormat PixelFormat { get; }
    public int Length { get; }
    public ReadOnlyMemory<byte> Memory =>
        (Volatile.Read(ref _frame) ?? throw new ObjectDisposedException(nameof(PooledVideoFrameLease)))
        .GetReadOnlyMemoryForLease();

    public void Dispose()
    {
        var frame = Interlocked.Exchange(ref _frame, null);
        frame?.ReleaseLease();
    }
}

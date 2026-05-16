using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Capture;

internal sealed partial class MjpegPreviewJitterBuffer
{
    private sealed class BufferedFrame : IDisposable
    {
        public BufferedFrame(byte[] buffer, int length, int width, int height, long arrivalTick, long enqueueTick)
        {
            Buffer = buffer;
            SequenceNumber = -1;
            Length = length;
            Width = width;
            Height = height;
            PixelFormat = PooledVideoPixelFormat.Nv12;
            ArrivalTick = arrivalTick;
            EnqueueTick = enqueueTick;
        }

        public BufferedFrame(PooledVideoFrameLease lease, long enqueueTick)
        {
            Lease = lease ?? throw new ArgumentNullException(nameof(lease));
            Buffer = Array.Empty<byte>();
            SequenceNumber = lease.SequenceNumber;
            Length = lease.Length;
            Width = lease.Width;
            Height = lease.Height;
            PixelFormat = lease.PixelFormat;
            ArrivalTick = lease.ArrivalTick;
            EnqueueTick = enqueueTick;
        }

        public byte[] Buffer { get; private set; }
        public PooledVideoFrameLease? Lease { get; set; }
        public long SequenceNumber { get; }
        public int Length { get; }
        public int Width { get; }
        public int Height { get; }
        public PooledVideoPixelFormat PixelFormat { get; }
        public long ArrivalTick { get; }
        public long EnqueueTick { get; }

        public void Dispose()
        {
            var lease = Lease;
            if (lease != null)
            {
                Lease = null;
                lease.Dispose();
            }

            var buffer = Buffer;
            if (buffer.Length != 0)
            {
                Buffer = Array.Empty<byte>();
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)
    {
        if (Volatile.Read(ref _disposed) != 0 ||
            Volatile.Read(ref _previewSubmissionSuppressed) != 0 ||
            nv12Data.IsEmpty ||
            width <= 0 ||
            height <= 0)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        RecordInputInterval(now);

        var buffer = ArrayPool<byte>.Shared.Rent(nv12Data.Length);
        nv12Data.CopyTo(buffer);
        var frame = new BufferedFrame(buffer, nv12Data.Length, width, height, arrivalTick, now);
        EnqueueBufferedFrame(frame);
    }

    public void Enqueue(PooledVideoFrameLease frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0 ||
            Volatile.Read(ref _previewSubmissionSuppressed) != 0 ||
            frame.Length <= 0 ||
            frame.Width <= 0 ||
            frame.Height <= 0)
        {
            frame.Dispose();
            return;
        }

        var now = Stopwatch.GetTimestamp();
        RecordInputInterval(now);
        EnqueueBufferedFrame(new BufferedFrame(frame, now));
    }

    private void EnqueueBufferedFrame(BufferedFrame frame)
    {
        var shouldSignal = false;

        lock (_sync)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                frame.Dispose();
                return;
            }

            while (_frames.Count >= _maxDepth)
            {
                var dropped = RemoveOldestFrame();
                RecordDroppedFrame(dropped.SequenceNumber, "queue-full");
                dropped.Dispose();
                Interlocked.Increment(ref _totalDropped);
            }

            if (AddFrameInOrder(frame))
            {
                Interlocked.Increment(ref _totalQueued);
                shouldSignal = true;
            }
        }

        if (shouldSignal && Volatile.Read(ref _disposed) == 0)
        {
            try
            {
                _signal.Set();
            }
            catch (ObjectDisposedException)
            {
                // Dispose won the race after the frame was queued; Dispose drains the queue.
            }
        }
    }
}

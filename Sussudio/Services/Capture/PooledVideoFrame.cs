using System;
using System.Buffers;
using System.Threading;

namespace Sussudio.Services.Capture;

// ArrayPool-backed decoded frame with reference-counted leases. The owner can
// dispose immediately after fan-out; the buffer returns to the pool only after
// every preview/recording consumer releases its lease.
internal sealed class PooledVideoFrame : IDisposable
{
    private readonly object _leaseSync = new();
    private readonly ArrayPool<byte> _pool;
    private readonly byte[] _buffer;
    private int _leaseCount = 1;
    private int _ownerReleased;
    private int _returned;

    private PooledVideoFrame(
        long sequenceNumber,
        long arrivalTick,
        long decodedTick,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        int length,
        ArrayPool<byte> pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

        SequenceNumber = sequenceNumber;
        ArrivalTick = arrivalTick;
        DecodedTick = decodedTick;
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Length = length;
        _pool = pool;
        _buffer = pool.Rent(length);
    }

    public long SequenceNumber { get; }
    public long ArrivalTick { get; }
    public long DecodedTick { get; internal set; }
    public int Width { get; }
    public int Height { get; }
    public PooledVideoPixelFormat PixelFormat { get; }
    public int Length { get; }
    public int LeaseCount
    {
        get
        {
            lock (_leaseSync)
            {
                return _leaseCount;
            }
        }
    }

    public bool IsReturned
    {
        get
        {
            lock (_leaseSync)
            {
                return _returned != 0;
            }
        }
    }
    public Memory<byte> Memory
    {
        get
        {
            ThrowIfOwnerAccessClosed();
            return new Memory<byte>(_buffer, 0, Length);
        }
    }

    public Span<byte> Span
    {
        get
        {
            ThrowIfOwnerAccessClosed();
            return _buffer.AsSpan(0, Length);
        }
    }

    public static PooledVideoFrame Rent(
        long sequenceNumber,
        long arrivalTick,
        long decodedTick,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        int length)
        => new(sequenceNumber, arrivalTick, decodedTick, width, height, pixelFormat, length, ArrayPool<byte>.Shared);

    public PooledVideoFrameLease AddLease()
    {
        if (TryAddLease(out var lease))
        {
            return lease!;
        }

        throw new ObjectDisposedException(nameof(PooledVideoFrame));
    }

    public bool TryAddLease(out PooledVideoFrameLease? lease)
    {
        lock (_leaseSync)
        {
            if (_leaseCount <= 0 || _ownerReleased != 0 || _returned != 0)
            {
                lease = default;
                return false;
            }

            _leaseCount++;
            lease = new PooledVideoFrameLease(this);
            return true;
        }
    }

    public void Dispose()
    {
        lock (_leaseSync)
        {
            if (_ownerReleased != 0)
            {
                return;
            }

            _ownerReleased = 1;
            ReleaseLeaseCore();
        }
    }

    internal void ReleaseLease()
    {
        lock (_leaseSync)
        {
            ReleaseLeaseCore();
        }
    }

    internal ReadOnlyMemory<byte> GetReadOnlyMemoryForLease()
    {
        lock (_leaseSync)
        {
            if (_returned != 0)
            {
                throw new ObjectDisposedException(nameof(PooledVideoFrame));
            }
        }

        return new ReadOnlyMemory<byte>(_buffer, 0, Length);
    }

    private void ReleaseLeaseCore()
    {
        var remaining = --_leaseCount;
        if (remaining < 0)
        {
            throw new InvalidOperationException("Pooled video frame lease count went negative.");
        }

        if (remaining == 0 && _returned == 0)
        {
            _returned = 1;
            _pool.Return(_buffer);
        }
    }

    private void ThrowIfOwnerAccessClosed()
    {
        lock (_leaseSync)
        {
            if (_ownerReleased != 0 || _returned != 0)
            {
                throw new ObjectDisposedException(nameof(PooledVideoFrame));
            }
        }
    }
}

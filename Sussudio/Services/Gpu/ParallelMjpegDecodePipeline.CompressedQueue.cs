using System;
using System.Buffers;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private const int WorkQueueItemCapacityPerDecoder = 8;
    private const long DefaultCompressedQueueByteBudget = 512L * 1024 * 1024;

    private readonly Channel<MjpegWorkItem> _workQueue;
    private readonly FrameFingerprintCadenceTracker _packetHashTracker = new();
    private long _nextDispatchSeq;
    private long _compressedFramesQueued;
    private long _compressedFramesDequeued;
    private long _compressedDropsQueueFull;
    private long _compressedDropsByteBudget;
    private long _compressedDropsDisposed;
    private long _startupInvalidCompressedDrops;
    private int _compressedQueueDepth;
    private long _compressedQueueBytes;
    private readonly long _compressedQueueByteBudget = DefaultCompressedQueueByteBudget;

    private readonly record struct MjpegWorkItem(
        byte[] JpegBuffer,
        int JpegLength,
        int Width,
        int Height,
        long ArrivalTick,
        long SeqNo);

    public bool EnqueueFrame(ReadOnlySpan<byte> jpegData, int width, int height, long arrivalTick)
    {
        if (_stopped || jpegData.IsEmpty)
        {
            return false;
        }

        if (Volatile.Read(ref _compressedFramesQueued) == 0 &&
            Volatile.Read(ref _compressedFramesDequeued) == 0 &&
            !HasJpegStartOfImage(jpegData))
        {
            var dropped = Interlocked.Increment(ref _totalFramesDropped);
            var startupDrops = Interlocked.Increment(ref _startupInvalidCompressedDrops);
            Interlocked.Increment(ref _compressedDropsDisposed);
            if (startupDrops <= 8 || startupDrops % 30 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_STARTUP_DROP reason=missing_soi drops={startupDrops} " +
                    $"totalDropped={dropped} bytes={jpegData.Length}");
            }

            return false;
        }

        var seq = Interlocked.Increment(ref _nextDispatchSeq) - 1;
        var buffer = ArrayPool<byte>.Shared.Rent(jpegData.Length);
        jpegData.CopyTo(buffer);

        var queuedBytes = Interlocked.Add(ref _compressedQueueBytes, jpegData.Length);
        if (queuedBytes > _compressedQueueByteBudget)
        {
            Interlocked.Add(ref _compressedQueueBytes, -jpegData.Length);
            ArrayPool<byte>.Shared.Return(buffer);
            var dropped = Interlocked.Increment(ref _totalFramesDropped);
            var byteBudgetDrops = Interlocked.Increment(ref _compressedDropsByteBudget);
            MarkKnownMissing(seq, "compressed_byte_budget");
            if (dropped == 1 || dropped % 30 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_COMPRESSED_DROP reason=byte_budget seq={seq} " +
                    $"drops={byteBudgetDrops} totalDropped={dropped} queuedBytes={queuedBytes} " +
                    $"budget={_compressedQueueByteBudget}");
            }

            return false;
        }

        Interlocked.Increment(ref _compressedFramesQueued);
        Interlocked.Increment(ref _compressedQueueDepth);

        if (!_workQueue.Writer.TryWrite(
                new MjpegWorkItem(buffer, jpegData.Length, width, height, arrivalTick, seq)))
        {
            Interlocked.Add(ref _compressedQueueBytes, -jpegData.Length);
            DecrementCompressedQueueDepth("write_failed");
            Interlocked.Decrement(ref _compressedFramesQueued);
            ArrayPool<byte>.Shared.Return(buffer);
            var dropped = Interlocked.Increment(ref _totalFramesDropped);
            var fullDrops = Interlocked.Increment(ref _compressedDropsQueueFull);
            MarkKnownMissing(seq, "compressed_queue_full");
            if (dropped == 1 || dropped % 30 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_COMPRESSED_DROP reason=queue_full seq={seq} " +
                    $"drops={fullDrops} totalDropped={dropped} depth={Volatile.Read(ref _compressedQueueDepth)}");
            }

            return false;
        }

        var packetHash = FrameFingerprintCadenceTracker.ComputeHash(jpegData);
        _packetHashTracker.RecordFrame(packetHash, arrivalTick);
        return true;
    }

    private static bool HasJpegStartOfImage(ReadOnlySpan<byte> data)
        => data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;

    private void DecrementCompressedQueueDepth(string operation)
    {
        while (true)
        {
            var current = Volatile.Read(ref _compressedQueueDepth);
            if (current <= 0)
            {
                Logger.Log($"MJPEG_PIPELINE_COMPRESSED_DEPTH_UNDERFLOW op={operation}");
                return;
            }

            if (Interlocked.CompareExchange(ref _compressedQueueDepth, current - 1, current) == current)
            {
                return;
            }
        }
    }
}

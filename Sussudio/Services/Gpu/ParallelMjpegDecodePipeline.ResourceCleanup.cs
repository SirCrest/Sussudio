using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private void CleanupResources()
    {
        if (Interlocked.Exchange(ref _resourcesDisposed, 1) != 0)
        {
            return;
        }

        foreach (var decoder in _decoders)
        {
            decoder?.Dispose();
        }

        ReturnRemainingWorkItems();

        List<DecodedFrame> remaining;
        lock (_reorderLock)
        {
            remaining = _reorderFrames.Values.ToList();
            _reorderFrames.Clear();
            _knownMissingSequences.Clear();
            Volatile.Write(ref _reorderBufferDepth, 0);
            Monitor.PulseAll(_reorderLock);
        }

        foreach (var frame in remaining)
        {
            frame.Frame.Dispose();
        }

        _emitSignal.Dispose();

        Logger.Log(
            $"PARALLEL_MJPEG_PIPELINE_DISPOSED decoded={_totalFramesDecoded} emitted={_totalFramesEmitted} " +
            $"dropped={_totalFramesDropped} compressedQueued={_compressedFramesQueued} " +
            $"compressedDequeued={_compressedFramesDequeued} queueFullDrops={_compressedDropsQueueFull} " +
            $"byteBudgetDrops={_compressedDropsByteBudget} disposedDrops={_compressedDropsDisposed} decodeFailures={_decodeFailures} " +
            $"reorderCollisions={_reorderCollisions} emitFailures={_emitFailures} skips={_reorderSkips}");
    }

    private void DiscardRemainingReorderFrames(string reason)
    {
        List<DecodedFrame> remaining;
        lock (_reorderLock)
        {
            remaining = _reorderFrames.Values.ToList();
            _reorderFrames.Clear();
            _knownMissingSequences.Clear();
            Volatile.Write(ref _reorderBufferDepth, 0);
            Monitor.PulseAll(_reorderLock);
        }

        foreach (var frame in remaining)
        {
            frame.Frame.Dispose();
            Interlocked.Increment(ref _totalFramesDropped);
            Logger.Log($"MJPEG_REORDER_DISCARD reason={reason} seq={frame.SeqNo}");
        }
    }

    private void ReturnRemainingWorkItems()
    {
        var disposedCount = 0L;
        var disposedBytes = 0L;
        while (_workQueue.Reader.TryRead(out var item))
        {
            disposedCount++;
            disposedBytes += item.JpegLength;
            ArrayPool<byte>.Shared.Return(item.JpegBuffer);
        }

        if (disposedCount > 0)
        {
            Interlocked.Add(ref _compressedDropsDisposed, disposedCount);
            Interlocked.Add(ref _totalFramesDropped, disposedCount);
        }

        if (disposedBytes > 0)
        {
            Interlocked.Add(ref _compressedQueueBytes, -disposedBytes);
        }

        Interlocked.Add(ref _compressedQueueDepth, -(int)Math.Min(int.MaxValue, disposedCount));
        if (Volatile.Read(ref _compressedQueueDepth) < 0)
        {
            Volatile.Write(ref _compressedQueueDepth, 0);
        }

        if (Interlocked.Read(ref _compressedQueueBytes) < 0)
        {
            Interlocked.Exchange(ref _compressedQueueBytes, 0);
        }
    }
}

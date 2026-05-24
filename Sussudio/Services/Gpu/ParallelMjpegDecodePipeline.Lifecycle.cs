using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private Thread? _emitThread;
    private readonly AutoResetEvent _emitSignal = new(false);

    public void Dispose()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        BeginStop();
        if (!TryWaitForShutdown(TimeSpan.FromSeconds(5), out var failureReason))
        {
            Logger.Log(
                $"PARALLEL_MJPEG_PIPELINE_DISPOSE_TIMEOUT reason='{failureReason ?? "unknown"}' " +
                $"decoded={_totalFramesDecoded} emitted={_totalFramesEmitted} dropped={_totalFramesDropped} skips={_reorderSkips}");
            return;
        }

        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        CleanupResources();
    }

    public bool TryStop(TimeSpan timeout, out string? failureReason)
    {
        failureReason = null;

        if (Volatile.Read(ref _threadsStopped) != 0)
        {
            return true;
        }

        BeginStop();
        return TryWaitForShutdown(timeout, out failureReason);
    }

    private void BeginStop()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            SignalEmitter("stop_already_requested");
            return;
        }

        _stopped = true;
        _workQueue.Writer.TryComplete();

        lock (_reorderLock)
        {
            Monitor.PulseAll(_reorderLock);
        }

        SignalEmitter("stop_requested");
    }

    private void StartEmitter()
    {
        _emitThread = new Thread(EmitLoop)
        {
            IsBackground = true,
            Name = "MjpegEmitter"
        };
        _emitThread.Start();
    }

    private void SignalEmitter(string operation)
    {
        try
        {
            _emitSignal.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"MJPEG_PIPELINE_EMIT_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private bool TryWaitForShutdown(TimeSpan timeout, out string? failureReason)
    {
        failureReason = null;

        if (Volatile.Read(ref _threadsStopped) != 0)
        {
            return true;
        }

        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        for (var i = 0; i < _workers.Length; i++)
        {
            var worker = _workers[i];
            if (worker == null || !worker.IsAlive)
            {
                continue;
            }

            if (ReferenceEquals(Thread.CurrentThread, worker))
            {
                failureReason = $"worker_self_join index={i}";
                return false;
            }

            var remaining = GetRemainingTimeout(deadline);
            if (remaining <= TimeSpan.Zero || !worker.Join(remaining))
            {
                failureReason = $"worker_timeout index={i}";
                return false;
            }
        }

        SignalEmitter("wait_for_shutdown");
        if (_emitThread is { IsAlive: true } emitThread)
        {
            if (ReferenceEquals(Thread.CurrentThread, emitThread))
            {
                failureReason = "emitter_self_join";
                return false;
            }

            var remaining = GetRemainingTimeout(deadline);
            if (remaining <= TimeSpan.Zero || !emitThread.Join(remaining))
            {
                failureReason = "emitter_timeout";
                return false;
            }
        }

        Interlocked.Exchange(ref _threadsStopped, 1);
        return true;
    }

    private void SignalFatalError(Exception ex)
    {
        BeginStop();

        if (Interlocked.Exchange(ref _fatalErrorSignaled, 1) != 0)
        {
            return;
        }

        if (_fatalErrorCallback == null)
        {
            return;
        }

        ThreadPool.UnsafeQueueUserWorkItem(
            static state =>
            {
                var (callback, exception) = ((Action<Exception>, Exception))state!;
                try
                {
                    callback(exception);
                }
                catch (Exception callbackEx)
                {
                    Logger.Log($"MJPEG_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
                }
            },
            (_fatalErrorCallback, ex));
    }

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

    private static TimeSpan GetRemainingTimeout(long deadlineTimestamp)
    {
        var remainingTicks = deadlineTimestamp - Stopwatch.GetTimestamp();
        if (remainingTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(remainingTicks / (double)Stopwatch.Frequency);
    }
}

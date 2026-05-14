using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Gpu;

// CPU-side MJPEG decode pipeline used when the source reader delivers
// compressed MJPG samples. It parallelizes decode, restores source sequence
// order in the emitter, and returns decoded NV12/P010 frames through pooled
// leases so preview and recording can share bytes safely.
internal sealed partial class ParallelMjpegDecodePipeline : IDisposable
{
    private const int WorkQueueItemCapacityPerDecoder = 8;
    private const long DefaultCompressedQueueByteBudget = 512L * 1024 * 1024;
    private const long DefaultDecodedReorderByteBudget = 1024L * 1024 * 1024;
    private const int MinDecodedReorderCapacity = 32;
    private const int MaxDecodedReorderCapacity = 240;

    public delegate void EmitFrameCallback(PooledVideoFrame frame);
    public delegate void PreviewFrameCallback(PooledVideoFrameLease frame);

    private readonly record struct MjpegWorkItem(
        byte[] JpegBuffer,
        int JpegLength,
        int Width,
        int Height,
        long ArrivalTick,
        long SeqNo);

    private readonly record struct DecodedFrame(
        long SeqNo,
        PooledVideoFrame Frame,
        long DecodedTick);

    private readonly SoftwareMjpegDecoder[] _decoders;
    private readonly Thread[] _workers;
    private readonly Channel<MjpegWorkItem> _workQueue;

    // Workers complete out of order, so decoded frames wait here until the
    // emitter can advance _nextEmitSeq. Missing-sequence tracking is explicit
    // so a dropped compressed packet does not permanently stall the pipeline.
    private readonly SortedDictionary<long, DecodedFrame> _reorderFrames = new();
    private readonly SortedSet<long> _knownMissingSequences = new();
    private readonly object _reorderLock = new();
    private readonly int _decodedReorderCapacity;
    private int _reorderBufferDepth;
    private long _nextDispatchSeq;
    private long _nextEmitSeq;
    private Thread? _emitThread;
    private readonly AutoResetEvent _emitSignal = new(false);
    private readonly EmitFrameCallback _emitCallback;
    private readonly PreviewFrameCallback? _previewCallback;
    private readonly Action<Exception>? _fatalErrorCallback;
    private readonly FrameFingerprintCadenceTracker _packetHashTracker = new();
    private volatile bool _stopped;
    private readonly int _decoderCount;
    private readonly int _width;
    private readonly int _height;
    private readonly double[][] _perDecoderDecodeTimeMs;
    private readonly int[] _perDecoderDecodeTimeCount;
    private readonly int[] _perDecoderDecodeTimeIndex;
    private readonly double[] _reorderLatencyMs = new double[300];
    private int _reorderLatencyCount;
    private int _reorderLatencyIndex;
    private readonly double[] _pipelineLatencyMs = new double[300];
    private int _pipelineLatencyCount;
    private int _pipelineLatencyIndex;
    private readonly object _timingLock = new();
    private readonly object[] _perDecoderTimingLocks;
    private long _totalFramesDecoded;
    private long _totalFramesEmitted;
    private long _totalFramesDropped;
    private long _compressedFramesQueued;
    private long _compressedFramesDequeued;
    private long _compressedDropsQueueFull;
    private long _compressedDropsByteBudget;
    private long _compressedDropsDisposed;
    private long _startupInvalidCompressedDrops;
    private long _decodeFailures;
    private long _reorderCollisions;
    private long _emitFailures;
    private int _compressedQueueDepth;
    private long _compressedQueueBytes;
    private readonly long _compressedQueueByteBudget = DefaultCompressedQueueByteBudget;
    private long _reorderSkips = 0;
    private long _missingSeqSinceTickMs = -1;
    private int _stopRequested;
    private int _threadsStopped;
    private int _fatalErrorSignaled;
    private int _resourcesDisposed;
    private int _disposed;

    public int DecoderCount => _decoderCount;

    public ParallelMjpegDecodePipeline(
        int decoderCount,
        int width,
        int height,
        EmitFrameCallback emitCallback,
        Action<Exception>? fatalErrorCallback = null,
        PreviewFrameCallback? previewCallback = null)
    {
        ArgumentNullException.ThrowIfNull(emitCallback);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
        }

        _decoderCount = Math.Clamp(decoderCount, 1, 8);
        _width = width;
        _height = height;
        _emitCallback = emitCallback;
        _previewCallback = previewCallback;
        _fatalErrorCallback = fatalErrorCallback;
        _decodedReorderCapacity = ResolveDecodedReorderCapacity(width, height);
        _decoders = new SoftwareMjpegDecoder[_decoderCount];
        _workers = new Thread[_decoderCount];
        _workQueue = Channel.CreateBounded<MjpegWorkItem>(new BoundedChannelOptions(
            Math.Max(32, _decoderCount * WorkQueueItemCapacityPerDecoder))
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _perDecoderDecodeTimeMs = new double[_decoderCount][];
        _perDecoderDecodeTimeCount = new int[_decoderCount];
        _perDecoderDecodeTimeIndex = new int[_decoderCount];
        _perDecoderTimingLocks = new object[_decoderCount];
        for (var j = 0; j < _decoderCount; j++)
        {
            _perDecoderTimingLocks[j] = new object();
        }

        try
        {
            for (var i = 0; i < _decoderCount; i++)
            {
                _decoders[i] = new SoftwareMjpegDecoder();
                _decoders[i].Initialize(width, height);
                _perDecoderDecodeTimeMs[i] = new double[300];

                var workerIndex = i;
                _workers[i] = new Thread(() => WorkerLoop(workerIndex))
                {
                    IsBackground = true,
                    Name = $"MjpegWorker-{i}"
                };
                _workers[i].Start();
            }

            _emitThread = new Thread(EmitLoop)
            {
                IsBackground = true,
                Name = "MjpegEmitter"
            };
            _emitThread.Start();

            Logger.Log(
                $"PARALLEL_MJPEG_PIPELINE_INIT decoders={_decoderCount} width={width} height={height} " +
                $"compressed_queue_capacity={Math.Max(32, _decoderCount * WorkQueueItemCapacityPerDecoder)} " +
                $"compressed_byte_budget={_compressedQueueByteBudget} decoded_reorder_capacity={_decodedReorderCapacity}");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

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

    private void WorkerLoop(int workerIndex)
    {
        var decoder = _decoders[workerIndex];
        var reader = _workQueue.Reader;

        try
        {
            while (true)
            {
                if (!reader.TryRead(out var item))
                {
                    if (!reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
                    {
                        break;
                    }

                    continue;
                }

                DecrementCompressedQueueDepth("dequeue");
                Interlocked.Add(ref _compressedQueueBytes, -item.JpegLength);
                Interlocked.Increment(ref _compressedFramesDequeued);

                var decodeStart = Stopwatch.GetTimestamp();
                var decodeSucceeded = false;
                PooledVideoFrame? pooledFrame = null;
                var frameOwned = false; // track ownership so we always return on exception

                try
                {
                    // Pre-rent the output frame so the decoder writes directly
                    // into pooled NV12 storage shared by downstream leases.
                    var nv12Size = decoder.Nv12Size;
                    pooledFrame = PooledVideoFrame.Rent(
                        item.SeqNo,
                        item.ArrivalTick,
                        decodedTick: 0,
                        item.Width,
                        item.Height,
                        PooledVideoPixelFormat.Nv12,
                        nv12Size);
                    frameOwned = true;

                    decodeSucceeded = decoder.DecodeToNv12(
                        item.JpegBuffer.AsSpan(0, item.JpegLength),
                        pooledFrame.Span);
                    pooledFrame.DecodedTick = Stopwatch.GetTimestamp();
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));

                    if (!decodeSucceeded)
                    {
                        Interlocked.Increment(ref _decodeFailures);
                        Interlocked.Increment(ref _totalFramesDropped);
                        MarkKnownMissing(item.SeqNo, "decode_failed");
                        continue;
                    }

                    if (!TryAddDecodedFrame(item.SeqNo, pooledFrame, pooledFrame.DecodedTick))
                    {
                        continue;
                    }

                    frameOwned = false; // ownership transferred to reorder ring
                    SignalEmitter("decoded_frame");
                    Interlocked.Increment(ref _totalFramesDecoded);
                }
                catch (SoftwareMjpegDecoderPermanentException ex)
                {
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));
                    Interlocked.Increment(ref _decodeFailures);
                    Interlocked.Increment(ref _totalFramesDropped);
                    MarkKnownMissing(item.SeqNo, "decode_fatal");
                    Logger.Log($"MJPEG_WORKER_FATAL worker={workerIndex} type={ex.GetType().Name} msg={ex.Message}");
                    SignalFatalError(ex);
                    break;
                }
                catch (Exception ex)
                {
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));
                    Interlocked.Increment(ref _decodeFailures);
                    Interlocked.Increment(ref _totalFramesDropped);
                    MarkKnownMissing(item.SeqNo, "decode_exception");
                    Logger.Log($"MJPEG_WORKER_FAIL worker={workerIndex} type={ex.GetType().Name} msg={ex.Message}");
                }
                finally
                {
                    if (frameOwned)
                    {
                        pooledFrame?.Dispose();
                    }

                    ArrayPool<byte>.Shared.Return(item.JpegBuffer);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"MJPEG_WORKER_FAIL worker={workerIndex} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EmitLoop()
    {
        while (!_stopped || HasAliveWorkers() || Volatile.Read(ref _reorderBufferDepth) > 0)
        {
            _emitSignal.WaitOne(8);

            var emittedAny = DrainReadyFrames();
            if (Volatile.Read(ref _fatalErrorSignaled) != 0)
            {
                break;
            }

            DetectAndResetStall(emittedAny);
        }

        if (Volatile.Read(ref _fatalErrorSignaled) == 0)
        {
            DrainReadyFrames();
            DrainRemainingFramesInOrder();
        }
        else
        {
            DiscardRemainingReorderFrames("fatal_stop");
        }
    }

    private bool DrainReadyFrames()
    {
        var emittedAny = false;
        while (true)
        {
            DiscardStaleReorderFrames();
            emittedAny = ConsumeKnownMissingFrames() || emittedAny;
            if (!TryTakeNextDecodedFrame(out var frame))
            {
                break;
            }

            emittedAny = true;

            RecordTimingSample(_reorderLatencyMs, ref _reorderLatencyCount, ref _reorderLatencyIndex, GetElapsedMilliseconds(frame.DecodedTick, Stopwatch.GetTimestamp()));
            RecordTimingSample(_pipelineLatencyMs, ref _pipelineLatencyCount, ref _pipelineLatencyIndex, GetElapsedMilliseconds(frame.Frame.ArrivalTick, Stopwatch.GetTimestamp()));
            var emitted = false;

            try
            {
                NotifyPreviewFrameDecoded(frame.Frame);
                _emitCallback(frame.Frame);
                emitted = true;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _emitFailures);
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                frame.Frame.Dispose();
            }

            _nextEmitSeq++;
            if (emitted)
            {
                Interlocked.Increment(ref _totalFramesEmitted);
            }
        }

        if (emittedAny)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, -1);
        }

        return emittedAny;
    }

    private void DetectAndResetStall(bool emittedAny)
    {
        var depth = Volatile.Read(ref _reorderBufferDepth);
        if (emittedAny)
        {
            return;
        }

        if (depth <= 0)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, -1);
            return;
        }

        var nowTickMs = Environment.TickCount64;
        var missingSince = Interlocked.Read(ref _missingSeqSinceTickMs);
        if (missingSince < 0)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
            return;
        }

        // Strict shared path: record the stall, but do not synthesize skips for
        // recording/Flashback. Preview-only skipping is handled downstream.
        var thresholdMs = 1000;

        if (nowTickMs - missingSince <= thresholdMs)
        {
            return;
        }

        Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
        Logger.Log(
            $"MJPEG_REORDER_STRICT_WAIT nextEmit={_nextEmitSeq} " +
            $"depth={Volatile.Read(ref _reorderBufferDepth)} waited_ms={nowTickMs - missingSince}");
    }

    private void MarkKnownMissing(long seqNo, string reason)
    {
        lock (_reorderLock)
        {
            if (seqNo < _nextEmitSeq)
            {
                return;
            }

            _knownMissingSequences.Add(seqNo);
            Monitor.PulseAll(_reorderLock);
        }

        Logger.Log($"MJPEG_PIPELINE_KNOWN_MISSING seq={seqNo} reason={reason}");
        SignalEmitter("known_missing");
    }

    private bool ConsumeKnownMissingFrames()
    {
        var consumedAny = false;
        while (true)
        {
            long skippedSeq;
            lock (_reorderLock)
            {
                if (!_knownMissingSequences.Remove(_nextEmitSeq))
                {
                    return consumedAny;
                }

                skippedSeq = _nextEmitSeq++;
                Monitor.PulseAll(_reorderLock);
            }

            consumedAny = true;
            var skips = Interlocked.Increment(ref _reorderSkips);
            if (skips == 1 || skips % 30 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_KNOWN_MISSING_SKIP seq={skippedSeq} " +
                    $"nextEmit={Volatile.Read(ref _nextEmitSeq)} totalSkips={skips}");
            }
        }
    }

    private void NotifyPreviewFrameDecoded(PooledVideoFrame frame)
    {
        var callback = _previewCallback;
        if (callback == null ||
            !frame.TryAddLease(out var lease))
        {
            return;
        }

        try
        {
            callback(lease!);
            lease = null;
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"MJPEG_PREVIEW_CALLBACK_FAIL seq={frame.SequenceNumber} " +
                $"type={ex.GetType().Name} msg={ex.Message}");
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private int GetNextSequenceState()
    {
        lock (_reorderLock)
        {
            if (_reorderFrames.TryGetValue(_nextEmitSeq, out _))
            {
                return 1;
            }

            if (_reorderFrames.Count == 0)
            {
                return 0;
            }

            var seqNo = PeekFirstSequenceUnderLock();
            return seqNo < _nextEmitSeq ? -1 : 2;
        }
    }

    // SortedDictionary keys are sorted, but Keys.First() goes through Enumerable.First
    // which boxes the struct enumerator. The struct GetEnumerator on the dictionary
    // itself avoids that, and we only need the smallest key.
    private long PeekFirstSequenceUnderLock()
    {
        using var e = _reorderFrames.GetEnumerator();
        e.MoveNext();
        return e.Current.Key;
    }

    private bool TryTakeNextDecodedFrame(out DecodedFrame frame)
    {
        lock (_reorderLock)
        {
            if (!_reorderFrames.TryGetValue(_nextEmitSeq, out frame))
            {
                return false;
            }

            _reorderFrames.Remove(_nextEmitSeq);
            Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);
            Monitor.PulseAll(_reorderLock);
            return true;
        }
    }

    private bool TryAddDecodedFrame(long seqNo, PooledVideoFrame frame, long decodedTick)
    {
        lock (_reorderLock)
        {
            while (!_stopped &&
                   Volatile.Read(ref _fatalErrorSignaled) == 0 &&
                   _reorderFrames.Count >= _decodedReorderCapacity &&
                   seqNo != _nextEmitSeq)
            {
                Monitor.Wait(_reorderLock, TimeSpan.FromMilliseconds(8));
            }

            if (_stopped || Volatile.Read(ref _fatalErrorSignaled) != 0)
            {
                return false;
            }

            if (_reorderFrames.ContainsKey(seqNo))
            {
                Interlocked.Increment(ref _reorderCollisions);
                var collisions = Interlocked.Increment(ref _totalFramesDropped);
                if (collisions == 1 || collisions % 120 == 0)
                {
                    Logger.Log(
                        $"MJPEG_REORDER_DUPLICATE seq={seqNo} " +
                        $"depth={Volatile.Read(ref _reorderBufferDepth)} " +
                        $"nextEmit={_nextEmitSeq} totalDropped={collisions}");
                }

                return false;
            }

            _reorderFrames.Add(seqNo, new DecodedFrame(seqNo, frame, decodedTick));
            Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);
            Monitor.PulseAll(_reorderLock);
            return true;
        }
    }

    private void DiscardStaleReorderFrames()
    {
        while (true)
        {
            DecodedFrame frame;
            lock (_reorderLock)
            {
                if (_reorderFrames.Count == 0)
                {
                    return;
                }

                var seqNo = PeekFirstSequenceUnderLock();
                if (seqNo >= _nextEmitSeq)
                {
                    return;
                }

                frame = _reorderFrames[seqNo];
                _reorderFrames.Remove(seqNo);
                Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);
                Monitor.PulseAll(_reorderLock);
            }

            Interlocked.Increment(ref _totalFramesDropped);
            frame.Frame.Dispose();
            Logger.Log(
                $"MJPEG_REORDER_DISCARD reason=stale seq={frame.SeqNo} nextEmit={_nextEmitSeq} " +
                $"depth={Volatile.Read(ref _reorderBufferDepth)}");
        }
    }

    private void DrainRemainingFramesInOrder()
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

        remaining.Sort((a, b) => a.SeqNo.CompareTo(b.SeqNo));

        foreach (var frame in remaining)
        {
            try
            {
                NotifyPreviewFrameDecoded(frame.Frame);
                _emitCallback(frame.Frame);
                Interlocked.Increment(ref _totalFramesEmitted);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _emitFailures);
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                frame.Frame.Dispose();
            }
        }
    }

    private bool HasAliveWorkers()
    {
        foreach (var worker in _workers)
        {
            if (worker.IsAlive)
            {
                return true;
            }
        }

        return false;
    }

    private static int ResolveDecodedReorderCapacity(int width, int height)
    {
        var nv12Bytes = Math.Max(1L, (long)width * height * 3 / 2);
        var budgetedFrames = DefaultDecodedReorderByteBudget / nv12Bytes;
        return (int)Math.Clamp(budgetedFrames, MinDecodedReorderCapacity, MaxDecodedReorderCapacity);
    }

}

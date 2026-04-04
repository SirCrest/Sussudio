using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;

namespace ElgatoCapture.Services;

internal sealed class ParallelMjpegDecodePipeline : IDisposable
{
    public delegate void EmitFrameCallback(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick);

    private readonly record struct MjpegWorkItem(
        byte[] JpegBuffer,
        int JpegLength,
        int Width,
        int Height,
        long ArrivalTick,
        long SeqNo);

    private readonly record struct DecodedFrame(
        long SeqNo,
        byte[] Nv12Buffer,
        int Nv12Size,
        int Width,
        int Height,
        long ArrivalTick,
        long DecodedTick);

    public readonly record struct PipelineTimingMetrics(
        int DecoderCount,
        int DecodeSampleCount,
        double DecodeAvgMs,
        double DecodeP95Ms,
        double DecodeMaxMs,
        int ReorderSampleCount,
        double ReorderAvgMs,
        double ReorderP95Ms,
        double ReorderMaxMs,
        int PipelineSampleCount,
        double PipelineAvgMs,
        double PipelineP95Ms,
        double PipelineMaxMs,
        long TotalDecoded,
        long TotalEmitted,
        long TotalDropped,
        long ReorderSkips,
        int ReorderBufferDepth,
        PerDecoderMetrics[] PerDecoder);

    public readonly record struct PerDecoderMetrics(
        int WorkerIndex,
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double MaxMs);

    private readonly SoftwareMjpegDecoder[] _decoders;
    private readonly Thread[] _workers;
    private readonly Channel<MjpegWorkItem>[] _workerQueues;
    private readonly DecodedFrame[] _reorderRing;
    private readonly int[] _reorderFlags;
    private readonly int _reorderCapacity;
    private int _reorderBufferDepth;
    private long _nextDispatchSeq;
    private long _nextEmitSeq;
    private Thread? _emitThread;
    private readonly AutoResetEvent _emitSignal = new(false);
    private readonly EmitFrameCallback _emitCallback;
    private readonly Action<Exception>? _fatalErrorCallback;
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
    private long _reorderSkips;
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
        Action<Exception>? fatalErrorCallback = null)
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
        _fatalErrorCallback = fatalErrorCallback;
        _reorderCapacity = Math.Max(32, _decoderCount * 8);
        _reorderRing = new DecodedFrame[_reorderCapacity];
        _reorderFlags = new int[_reorderCapacity];
        _decoders = new SoftwareMjpegDecoder[_decoderCount];
        _workers = new Thread[_decoderCount];
        _workerQueues = new Channel<MjpegWorkItem>[_decoderCount];
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
                _workerQueues[i] = Channel.CreateBounded<MjpegWorkItem>(new BoundedChannelOptions(4)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropWrite
                });

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

            Logger.Log($"PARALLEL_MJPEG_PIPELINE_INIT decoders={_decoderCount} width={width} height={height}");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void EnqueueFrame(ReadOnlySpan<byte> jpegData, int width, int height, long arrivalTick)
    {
        if (_stopped || jpegData.IsEmpty)
        {
            return;
        }

        var seq = Interlocked.Increment(ref _nextDispatchSeq) - 1;
        var buffer = ArrayPool<byte>.Shared.Rent(jpegData.Length);
        jpegData.CopyTo(buffer);

        var workerIndex = (int)(seq % (uint)_decoderCount);
        if (!_workerQueues[workerIndex].Writer.TryWrite(
                new MjpegWorkItem(buffer, jpegData.Length, width, height, arrivalTick, seq)))
        {
            ArrayPool<byte>.Shared.Return(buffer);
            var dropped = Interlocked.Increment(ref _totalFramesDropped);
            if (dropped == 1 || dropped % 30 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_DROP worker={workerIndex} dropped={dropped} queue_capacity=4 seq={seq}");
            }
        }
    }

    public PipelineTimingMetrics GetTimingMetrics()
    {
        double[][] decoderSamples;
        int[] decoderCounts;
        int[] decoderIndexes;
        double[] reorderSamples;
        int reorderCount;
        int reorderIndex;
        double[] pipelineSamples;
        int pipelineCount;
        int pipelineIndex;

        // Snapshot per-decoder timing under individual locks to avoid contention.
        decoderSamples = new double[_decoderCount][];
        decoderCounts = new int[_decoderCount];
        decoderIndexes = new int[_decoderCount];
        for (var i = 0; i < _decoderCount; i++)
        {
            lock (_perDecoderTimingLocks[i])
            {
                decoderSamples[i] = (double[])_perDecoderDecodeTimeMs[i].Clone();
                decoderCounts[i] = _perDecoderDecodeTimeCount[i];
                decoderIndexes[i] = _perDecoderDecodeTimeIndex[i];
            }
        }

        // Reorder and pipeline latency are written by emit thread only — shared lock is fine.
        lock (_timingLock)
        {
            reorderSamples = (double[])_reorderLatencyMs.Clone();
            reorderCount = _reorderLatencyCount;
            reorderIndex = _reorderLatencyIndex;
            pipelineSamples = (double[])_pipelineLatencyMs.Clone();
            pipelineCount = _pipelineLatencyCount;
            pipelineIndex = _pipelineLatencyIndex;
        }

        var perDecoder = new PerDecoderMetrics[_decoderCount];
        var allDecodeSamples = new double[decoderCounts.Sum()];
        var decodeOffset = 0;
        for (var i = 0; i < _decoderCount; i++)
        {
            var copied = CopyRing(decoderSamples[i], decoderCounts[i], decoderIndexes[i]);
            Array.Copy(copied, 0, allDecodeSamples, decodeOffset, copied.Length);
            decodeOffset += copied.Length;

            var decoderMetrics = ComputeTimingMetrics(copied);
            perDecoder[i] = new PerDecoderMetrics(
                WorkerIndex: i,
                SampleCount: decoderMetrics.SampleCount,
                AvgMs: decoderMetrics.AverageMs,
                P95Ms: decoderMetrics.P95Ms,
                MaxMs: decoderMetrics.MaxMs);
        }

        var aggregateDecode = ComputeTimingMetrics(allDecodeSamples);
        var reorderMetrics = ComputeTimingMetrics(CopyRing(reorderSamples, reorderCount, reorderIndex));
        var pipelineMetrics = ComputeTimingMetrics(CopyRing(pipelineSamples, pipelineCount, pipelineIndex));

        return new PipelineTimingMetrics(
            DecoderCount: _decoderCount,
            DecodeSampleCount: aggregateDecode.SampleCount,
            DecodeAvgMs: aggregateDecode.AverageMs,
            DecodeP95Ms: aggregateDecode.P95Ms,
            DecodeMaxMs: aggregateDecode.MaxMs,
            ReorderSampleCount: reorderMetrics.SampleCount,
            ReorderAvgMs: reorderMetrics.AverageMs,
            ReorderP95Ms: reorderMetrics.P95Ms,
            ReorderMaxMs: reorderMetrics.MaxMs,
            PipelineSampleCount: pipelineMetrics.SampleCount,
            PipelineAvgMs: pipelineMetrics.AverageMs,
            PipelineP95Ms: pipelineMetrics.P95Ms,
            PipelineMaxMs: pipelineMetrics.MaxMs,
            TotalDecoded: Interlocked.Read(ref _totalFramesDecoded),
            TotalEmitted: Interlocked.Read(ref _totalFramesEmitted),
            TotalDropped: Interlocked.Read(ref _totalFramesDropped),
            ReorderSkips: Interlocked.Read(ref _reorderSkips),
            ReorderBufferDepth: Volatile.Read(ref _reorderBufferDepth),
            PerDecoder: perDecoder);
    }

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

    private void WorkerLoop(int workerIndex)
    {
        var decoder = _decoders[workerIndex];
        var reader = _workerQueues[workerIndex].Reader;

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

                var decodeStart = Stopwatch.GetTimestamp();
                var decodeSucceeded = false;

                // Pre-rent the output buffer so the decoder writes directly
                // into it, eliminating a 10MB copy per frame.
                var nv12Size = decoder.Nv12Size;
                var nv12Buffer = ArrayPool<byte>.Shared.Rent(nv12Size);
                var nv12Owned = true; // track ownership so we always return on exception

                try
                {
                    decodeSucceeded = decoder.DecodeToNv12(
                        item.JpegBuffer.AsSpan(0, item.JpegLength),
                        nv12Buffer.AsSpan(0, nv12Size));
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));

                    if (!decodeSucceeded)
                    {
                        Interlocked.Increment(ref _totalFramesDropped);
                        continue;
                    }

                    var slot = (int)(item.SeqNo % (uint)_reorderCapacity);

                    // Guard against ring slot collision: if the emitter hasn't
                    // consumed this slot yet, drop the new frame rather than
                    // overwriting and leaking the old buffer.
                    if (Volatile.Read(ref _reorderFlags[slot]) != 0)
                    {
                        var collisions = Interlocked.Increment(ref _totalFramesDropped);
                        if (collisions == 1 || collisions % 120 == 0)
                        {
                            Logger.Log(
                                $"MJPEG_REORDER_SLOT_COLLISION seq={item.SeqNo} slot={slot} " +
                                $"depth={Volatile.Read(ref _reorderBufferDepth)} " +
                                $"nextEmit={_nextEmitSeq} totalDropped={collisions}");
                        }

                        continue;
                    }

                    _reorderRing[slot] = new DecodedFrame(
                        item.SeqNo,
                        nv12Buffer,
                        nv12Size,
                        item.Width,
                        item.Height,
                        item.ArrivalTick,
                        Stopwatch.GetTimestamp());
                    Volatile.Write(ref _reorderFlags[slot], 1);
                    nv12Owned = false; // ownership transferred to reorder ring
                    Interlocked.Increment(ref _reorderBufferDepth);
                    _emitSignal.Set();
                    Interlocked.Increment(ref _totalFramesDecoded);
                }
                catch (SoftwareMjpegDecoderPermanentException ex)
                {
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));
                    Interlocked.Increment(ref _totalFramesDropped);
                    Logger.Log($"MJPEG_WORKER_FATAL worker={workerIndex} type={ex.GetType().Name} msg={ex.Message}");
                    SignalFatalError(ex);
                    break;
                }
                catch (Exception ex)
                {
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));
                    Interlocked.Increment(ref _totalFramesDropped);
                    Logger.Log($"MJPEG_WORKER_FAIL worker={workerIndex} type={ex.GetType().Name} msg={ex.Message}");
                }
                finally
                {
                    if (nv12Owned)
                    {
                        ArrayPool<byte>.Shared.Return(nv12Buffer);
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
            DetectAndResetStall(emittedAny);
        }

        DrainReadyFrames();
        DrainRemainingFramesInOrder();
    }

    private bool DrainReadyFrames()
    {
        var emittedAny = false;
        while (true)
        {
            var slot = (int)(_nextEmitSeq % (uint)_reorderCapacity);
            if (Volatile.Read(ref _reorderFlags[slot]) == 0)
            {
                break;
            }

            var frame = _reorderRing[slot];
            Volatile.Write(ref _reorderFlags[slot], 0);
            Interlocked.Decrement(ref _reorderBufferDepth);
            emittedAny = true;

            RecordTimingSample(_reorderLatencyMs, ref _reorderLatencyCount, ref _reorderLatencyIndex, GetElapsedMilliseconds(frame.DecodedTick, Stopwatch.GetTimestamp()));
            RecordTimingSample(_pipelineLatencyMs, ref _pipelineLatencyCount, ref _pipelineLatencyIndex, GetElapsedMilliseconds(frame.ArrivalTick, Stopwatch.GetTimestamp()));
            var emitted = false;

            try
            {
                _emitCallback(new ReadOnlySpan<byte>(frame.Nv12Buffer, 0, frame.Nv12Size), frame.Width, frame.Height, frame.ArrivalTick);
                emitted = true;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(frame.Nv12Buffer);
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
        if (emittedAny || depth <= _decoderCount * 2)
        {
            if (depth == 0)
            {
                Interlocked.Exchange(ref _missingSeqSinceTickMs, -1);
            }

            return;
        }

        var nowTickMs = Environment.TickCount64;
        var missingSince = Interlocked.Read(ref _missingSeqSinceTickMs);
        if (missingSince < 0)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
            return;
        }

        // At high buffer depth (near capacity), skip immediately — the frame
        // is clearly lost and waiting only compounds lag. Otherwise use a
        // short 17ms threshold (~2 frame periods at 120fps).
        var nearFull = depth >= _reorderCapacity - 2;
        var thresholdMs = nearFull ? 0 : 17;

        if (nowTickMs - missingSince <= thresholdMs)
        {
            return;
        }

        // Batch-skip all consecutive missing seqs in one pass so we don't
        // pay the stall threshold N times for N consecutive drops.
        var skippedCount = 0;
        while (true)
        {
            var slot = (int)(_nextEmitSeq % (uint)_reorderCapacity);
            if (Volatile.Read(ref _reorderFlags[slot]) != 0)
            {
                break; // next frame is ready — stop skipping
            }

            _nextEmitSeq++;
            skippedCount++;
            Interlocked.Increment(ref _reorderSkips);

            // Don't skip past all buffered frames — leave at least one to emit.
            if (Volatile.Read(ref _reorderBufferDepth) <= 1)
            {
                break;
            }
        }

        Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
        if (skippedCount > 0)
        {
            Logger.Log(
                $"MJPEG_REORDER_SKIP count={skippedCount} nextEmit={_nextEmitSeq} " +
                $"depth={Volatile.Read(ref _reorderBufferDepth)} threshold_ms={thresholdMs}");
        }
    }

    private void DrainRemainingFramesInOrder()
    {
        // Collect remaining frames from the ring buffer.
        var remaining = new List<DecodedFrame>();
        for (var i = 0; i < _reorderCapacity; i++)
        {
            if (Volatile.Read(ref _reorderFlags[i]) != 0)
            {
                remaining.Add(_reorderRing[i]);
                Volatile.Write(ref _reorderFlags[i], 0);
            }
        }

        remaining.Sort((a, b) => a.SeqNo.CompareTo(b.SeqNo));
        Volatile.Write(ref _reorderBufferDepth, 0);

        foreach (var frame in remaining)
        {
            try
            {
                _emitCallback(new ReadOnlySpan<byte>(frame.Nv12Buffer, 0, frame.Nv12Size), frame.Width, frame.Height, frame.ArrivalTick);
                Interlocked.Increment(ref _totalFramesEmitted);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(frame.Nv12Buffer);
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

    private void RecordPerDecoderTiming(int workerIndex, double valueMs)
    {
        lock (_perDecoderTimingLocks[workerIndex])
        {
            var window = _perDecoderDecodeTimeMs[workerIndex];
            var index = _perDecoderDecodeTimeIndex[workerIndex];
            window[index] = valueMs;
            _perDecoderDecodeTimeIndex[workerIndex] = (index + 1) % window.Length;
            if (_perDecoderDecodeTimeCount[workerIndex] < window.Length)
            {
                _perDecoderDecodeTimeCount[workerIndex]++;
            }
        }
    }

    private void RecordTimingSample(double[] window, ref int count, ref int index, double valueMs)
    {
        lock (_timingLock)
        {
            window[index] = valueMs;
            index = (index + 1) % window.Length;
            if (count < window.Length)
            {
                count++;
            }
        }
    }

    private static double[] CopyRing(double[] window, int count, int index)
    {
        var samples = new double[count];
        for (var i = 0; i < count; i++)
        {
            var ringIndex = (index - count + i + window.Length) % window.Length;
            samples[i] = window[ringIndex];
        }

        return samples;
    }

    private static (int SampleCount, double AverageMs, double P95Ms, double MaxMs) ComputeTimingMetrics(double[] samples)
    {
        var sampleCount = samples.Length;
        if (sampleCount == 0)
        {
            return (0, 0, 0, 0);
        }

        var sorted = (double[])samples.Clone();
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sorted.Length; i++)
        {
            sum += sorted[i];
            if (sorted[i] > max)
            {
                max = sorted[i];
            }
        }

        Array.Sort(sorted);
        var p95Index = Math.Min((int)(sampleCount * 0.95), sampleCount - 1);
        return (sampleCount, sum / sampleCount, sorted[p95Index], max);
    }

    private static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
        => (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;

    private void BeginStop()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            _emitSignal.Set();
            return;
        }

        _stopped = true;
        foreach (var queue in _workerQueues)
        {
            queue?.Writer.TryComplete();
        }

        _emitSignal.Set();
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

        _emitSignal.Set();
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

        for (var i = 0; i < _reorderCapacity; i++)
        {
            if (Volatile.Read(ref _reorderFlags[i]) != 0)
            {
                ArrayPool<byte>.Shared.Return(_reorderRing[i].Nv12Buffer);
                Volatile.Write(ref _reorderFlags[i], 0);
            }
        }

        Volatile.Write(ref _reorderBufferDepth, 0);
        _emitSignal.Dispose();

        Logger.Log(
            $"PARALLEL_MJPEG_PIPELINE_DISPOSED decoded={_totalFramesDecoded} emitted={_totalFramesEmitted} dropped={_totalFramesDropped} skips={_reorderSkips}");
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

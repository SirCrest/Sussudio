using System;
using System.Buffers;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<long, DecodedFrame> _reorderBuffer = new();
    private long _nextDispatchSeq;
    private long _nextEmitSeq;
    private Thread? _emitThread;
    private readonly ManualResetEventSlim _emitSignal = new(false);
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
        _decoders = new SoftwareMjpegDecoder[_decoderCount];
        _workers = new Thread[_decoderCount];
        _workerQueues = new Channel<MjpegWorkItem>[_decoderCount];
        _perDecoderDecodeTimeMs = new double[_decoderCount][];
        _perDecoderDecodeTimeCount = new int[_decoderCount];
        _perDecoderDecodeTimeIndex = new int[_decoderCount];

        try
        {
            for (var i = 0; i < _decoderCount; i++)
            {
                _decoders[i] = new SoftwareMjpegDecoder();
                _decoders[i].Initialize(width, height);
                _perDecoderDecodeTimeMs[i] = new double[300];
                _workerQueues[i] = Channel.CreateBounded<MjpegWorkItem>(new BoundedChannelOptions(2)
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
            if (dropped % 120 == 0)
            {
                Logger.Log(
                    $"MJPEG_PIPELINE_DROP worker={workerIndex} dropped={dropped} queue_capacity=2 seq={seq}");
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

        lock (_timingLock)
        {
            decoderSamples = new double[_decoderCount][];
            decoderCounts = (int[])_perDecoderDecodeTimeCount.Clone();
            decoderIndexes = (int[])_perDecoderDecodeTimeIndex.Clone();
            for (var i = 0; i < _decoderCount; i++)
            {
                decoderSamples[i] = (double[])_perDecoderDecodeTimeMs[i].Clone();
            }

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
            ReorderBufferDepth: _reorderBuffer.Count,
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

                try
                {
                    decodeSucceeded = decoder.DecodeToNv12(
                        item.JpegBuffer.AsSpan(0, item.JpegLength),
                        out var nv12Data);
                    RecordPerDecoderTiming(workerIndex, GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));

                    if (!decodeSucceeded || nv12Data.IsEmpty)
                    {
                        Interlocked.Increment(ref _totalFramesDropped);
                        continue;
                    }

                    var nv12Copy = ArrayPool<byte>.Shared.Rent(nv12Data.Length);
                    nv12Data.CopyTo(nv12Copy);

                    _reorderBuffer[item.SeqNo] = new DecodedFrame(
                        item.SeqNo,
                        nv12Copy,
                        nv12Data.Length,
                        item.Width,
                        item.Height,
                        item.ArrivalTick,
                        Stopwatch.GetTimestamp());
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
        while (!_stopped || HasAliveWorkers() || _reorderBuffer.Count > 0)
        {
            try
            {
                _emitSignal.Wait(50);
                _emitSignal.Reset();
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var emittedAny = DrainReadyFrames();
            DetectAndHandleStall(emittedAny);
        }

        DrainReadyFrames();
        DrainRemainingFramesInOrder();
    }

    private bool DrainReadyFrames()
    {
        var emittedAny = false;
        while (_reorderBuffer.TryRemove(_nextEmitSeq, out var frame))
        {
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

    private void DetectAndHandleStall(bool emittedAny)
    {
        if (emittedAny || _reorderBuffer.Count <= _decoderCount * 2)
        {
            if (_reorderBuffer.Count == 0)
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

        if (nowTickMs - missingSince <= 50)
        {
            return;
        }

        var skippedSeq = _nextEmitSeq;
        _nextEmitSeq++;
        Interlocked.Increment(ref _reorderSkips);
        Interlocked.Exchange(ref _missingSeqSinceTickMs, nowTickMs);
        Logger.Log($"MJPEG_REORDER_SKIP seq={skippedSeq} depth={_reorderBuffer.Count} threshold_ms=50");
    }

    private void DrainRemainingFramesInOrder()
    {
        foreach (var frame in _reorderBuffer.Values.OrderBy(frame => frame.SeqNo))
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

        _reorderBuffer.Clear();
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
        lock (_timingLock)
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

        foreach (var entry in _reorderBuffer)
        {
            ArrayPool<byte>.Shared.Return(entry.Value.Nv12Buffer);
        }

        _reorderBuffer.Clear();
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

using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Gpu;

// CPU-side MJPEG decode pipeline used when the source reader delivers
// compressed MJPG samples. It parallelizes decode, restores source sequence
// order in the emitter, and returns decoded NV12/P010 frames through pooled
// leases so preview and recording can share bytes safely.
internal sealed partial class ParallelMjpegDecodePipeline : IDisposable
{
    public delegate void EmitFrameCallback(PooledVideoFrame frame);
    public delegate void PreviewFrameCallback(PooledVideoFrameLease frame);

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
        long CompressedFramesQueued,
        long CompressedFramesDequeued,
        long CompressedDropsQueueFull,
        long CompressedDropsByteBudget,
        long CompressedDropsDisposed,
        long DecodeFailures,
        long ReorderCollisions,
        long EmitFailures,
        int CompressedQueueDepth,
        long CompressedQueueBytes,
        long CompressedQueueByteBudget,
        long ReorderSkips,
        int ReorderBufferDepth,
        PerDecoderMetrics[] PerDecoder);

    public readonly record struct PerDecoderMetrics(
        int WorkerIndex,
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double MaxMs);

    private const int WorkQueueItemCapacityPerDecoder = 8;
    private const long DefaultCompressedQueueByteBudget = 512L * 1024 * 1024;

    private readonly EmitFrameCallback _emitCallback;
    private readonly PreviewFrameCallback? _previewCallback;
    private readonly Action<Exception>? _fatalErrorCallback;
    private readonly Channel<MjpegWorkItem> _workQueue;
    private readonly FrameFingerprintCadenceTracker _packetHashTracker = new();
    private readonly SoftwareMjpegDecoder[] _decoders;
    private readonly Thread[] _workers;
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
    private long _decodeFailures;
    private long _reorderCollisions;
    private long _emitFailures;
    private int _stopRequested;
    private int _threadsStopped;
    private int _fatalErrorSignaled;
    private int _resourcesDisposed;
    private int _disposed;
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

    public int DecoderCount => _decoderCount;

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

    private void StartDecodeWorkers(int width, int height)
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

        // Reorder and pipeline latency are written by emit thread only; shared lock is fine.
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
        var totalDecodeSamples = 0;
        for (var i = 0; i < decoderCounts.Length; i++)
        {
            totalDecodeSamples += decoderCounts[i];
        }

        var allDecodeSamples = new double[totalDecodeSamples];
        var decodeOffset = 0;
        for (var i = 0; i < _decoderCount; i++)
        {
            var copied = RingBufferHelpers.Copy(decoderSamples[i], decoderCounts[i], decoderIndexes[i]);
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
        var reorderMetrics = ComputeTimingMetrics(RingBufferHelpers.Copy(reorderSamples, reorderCount, reorderIndex));
        var pipelineMetrics = ComputeTimingMetrics(RingBufferHelpers.Copy(pipelineSamples, pipelineCount, pipelineIndex));

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
            CompressedFramesQueued: Interlocked.Read(ref _compressedFramesQueued),
            CompressedFramesDequeued: Interlocked.Read(ref _compressedFramesDequeued),
            CompressedDropsQueueFull: Interlocked.Read(ref _compressedDropsQueueFull),
            CompressedDropsByteBudget: Interlocked.Read(ref _compressedDropsByteBudget),
            CompressedDropsDisposed: Interlocked.Read(ref _compressedDropsDisposed),
            DecodeFailures: Interlocked.Read(ref _decodeFailures),
            ReorderCollisions: Interlocked.Read(ref _reorderCollisions),
            EmitFailures: Interlocked.Read(ref _emitFailures),
            CompressedQueueDepth: Volatile.Read(ref _compressedQueueDepth),
            CompressedQueueBytes: Interlocked.Read(ref _compressedQueueBytes),
            CompressedQueueByteBudget: _compressedQueueByteBudget,
            ReorderSkips: Interlocked.Read(ref _reorderSkips),
            ReorderBufferDepth: Volatile.Read(ref _reorderBufferDepth),
            PerDecoder: perDecoder);
    }

    public FrameFingerprintCadenceTracker.Metrics GetPacketHashMetrics()
    {
        return _packetHashTracker.GetMetrics();
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
            RingBufferHelpers.Add(window, ref count, ref index, valueMs);
        }
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
            StartDecodeWorkers(width, height);
            StartEmitter();

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

}

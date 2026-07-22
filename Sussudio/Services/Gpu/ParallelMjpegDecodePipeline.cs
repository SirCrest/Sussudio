using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using System.Threading.Channels;
using FFmpeg.AutoGen;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Gpu;

// CPU-side MJPEG decode pipeline used when the source reader delivers
// compressed MJPG samples. It parallelizes decode, restores source sequence
// order in the emitter, and returns decoded NV12/P010 frames through pooled
// leases so preview and recording can share bytes safely.
internal sealed class ParallelMjpegDecodePipeline : IDisposable
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
        long LoadShedDrops,
        long DecodeFailures,
        long ReorderCollisions,
        long EmitFailures,
        int CompressedQueueDepth,
        long CompressedQueueBytes,
        long CompressedQueueByteBudget,
        long ReorderSkips,
        int ReorderBufferDepth,
        int PeakReorderDepth,
        long PeakCompressedQueueBytes,
        long ReorderRingForceDrops,
        PerDecoderMetrics[] PerDecoder);

    public readonly record struct PerDecoderMetrics(
        int WorkerIndex,
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double MaxMs);

    private const int WorkQueueItemCapacityPerDecoder = 8;
    private const long DefaultCompressedQueueByteBudget = 64L * 1024 * 1024;

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
    private Thread? _emitThread;
    private readonly AutoResetEvent _emitSignal = new(false);
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
    private long _loadShedDrops;
    private int _peakReorderDepth;
    private long _peakCompressedQueueBytes;
    private long _reorderForceDrops;
    private readonly long _compressedQueueByteBudget = DefaultCompressedQueueByteBudget;
    private readonly long _shedAgeTicks;
    private readonly Func<bool>? _strictFrameConsumerActive;
    private readonly bool _earlyPreviewForkEnabled =
        EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_MJPEG_PREVIEW_EARLY_FORK", 1, 0, 1) != 0;
    private readonly bool _loadSheddingEnabled =
        EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_MJPEG_LOAD_SHED", 1, 0, 1) != 0;
    private readonly string _decodeMmcssTask =
        Environment.GetEnvironmentVariable("SUSSUDIO_MJPEG_DECODE_MMCSS_TASK") ?? "Playback";
    private readonly int _decodeMmcssPriority =
        EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_MJPEG_DECODE_MMCSS_PRIORITY", 0, -2, 2);

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

        var currentPeak = Interlocked.Read(ref _peakCompressedQueueBytes);
        if (queuedBytes > currentPeak)
        {
            Interlocked.CompareExchange(ref _peakCompressedQueueBytes, queuedBytes, currentPeak);
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

    private bool ShouldShedStaleCompressedFrame(long arrivalTick)
    {
        if (!_loadSheddingEnabled || _shedAgeTicks <= 0)
        {
            return false;
        }

        // Only shed under real backlog: a transient single-frame burst should
        // still be decoded so preview cadence stays intact.
        if (Volatile.Read(ref _compressedQueueDepth) <= _decoderCount)
        {
            return false;
        }

        if (Stopwatch.GetTimestamp() - arrivalTick <= _shedAgeTicks)
        {
            return false;
        }

        // Recording/Flashback require every frame; with no gate installed,
        // assume a strict consumer exists and never shed.
        var strictFrameConsumerActive = _strictFrameConsumerActive;
        return strictFrameConsumerActive != null && !strictFrameConsumerActive();
    }

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
        // Decode workers are the heaviest CPU consumers in the app and run
        // alongside games; without MMCSS they are the first threads starved,
        // which is exactly the stutter the jitter buffer then has to hide.
        using var mmcss = MmcssThreadRegistration.TryRegister(_decodeMmcssTask, _decodeMmcssPriority, message => Logger.Log(message));

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

                if (ShouldShedStaleCompressedFrame(item.ArrivalTick))
                {
                    ArrayPool<byte>.Shared.Return(item.JpegBuffer);
                    Interlocked.Increment(ref _totalFramesDropped);
                    var shedDrops = Interlocked.Increment(ref _loadShedDrops);
                    MarkKnownMissing(item.SeqNo, "load_shed_stale");
                    if (shedDrops == 1 || shedDrops % 60 == 0)
                    {
                        Logger.Log(
                            $"MJPEG_PIPELINE_LOAD_SHED seq={item.SeqNo} sheds={shedDrops} " +
                            $"ageMs={GetElapsedMilliseconds(item.ArrivalTick, Stopwatch.GetTimestamp()):0.##} " +
                            $"depth={Volatile.Read(ref _compressedQueueDepth)}");
                    }

                    continue;
                }

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

                    if (_earlyPreviewForkEnabled)
                    {
                        // Fork to preview before the strict reorder ring so a slow
                        // neighboring decode cannot delay this frame's submission.
                        // The jitter buffer re-orders by sequence number and
                        // deadline-skips gaps on its own; only recording/Flashback
                        // need the strict in-order emit below.
                        NotifyPreviewFrameDecoded(pooledFrame);
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
            LoadShedDrops: Interlocked.Read(ref _loadShedDrops),
            DecodeFailures: Interlocked.Read(ref _decodeFailures),
            ReorderCollisions: Interlocked.Read(ref _reorderCollisions),
            EmitFailures: Interlocked.Read(ref _emitFailures),
            CompressedQueueDepth: Volatile.Read(ref _compressedQueueDepth),
            CompressedQueueBytes: Interlocked.Read(ref _compressedQueueBytes),
            CompressedQueueByteBudget: _compressedQueueByteBudget,
            ReorderSkips: Interlocked.Read(ref _reorderSkips),
            ReorderBufferDepth: Volatile.Read(ref _reorderBufferDepth),
            PeakReorderDepth: Volatile.Read(ref _peakReorderDepth),
            PeakCompressedQueueBytes: Interlocked.Read(ref _peakCompressedQueueBytes),
            ReorderRingForceDrops: Interlocked.Read(ref _reorderForceDrops),
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
        return (sampleCount, sum / sampleCount, PercentileHelpers.FromSorted(sorted, 0.95), max);
    }

    private static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
        => (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;

    public ParallelMjpegDecodePipeline(
        int decoderCount,
        int width,
        int height,
        EmitFrameCallback emitCallback,
        Action<Exception>? fatalErrorCallback = null,
        PreviewFrameCallback? previewCallback = null,
        double fps = 0,
        Func<bool>? strictFrameConsumerActive = null)
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
        _strictFrameConsumerActive = strictFrameConsumerActive;
        // Every MJPEG frame is a keyframe, so a backlogged compressed frame can
        // be skipped without corrupting anything downstream — but only when no
        // strict consumer (recording/Flashback) needs the full sequence.
        _shedAgeTicks = fps > 0
            ? (long)Math.Round(Stopwatch.Frequency * 3.0 / fps)
            : (long)Math.Round(Stopwatch.Frequency * 0.025);
        _decodedReorderCapacity = ResolveDecodedReorderCapacity(width, height);
        var overrideMb = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_MJPEG_COMPRESSED_BUDGET_MB", 0, 0, 4096);
        if (overrideMb > 0)
        {
            _compressedQueueByteBudget = (long)overrideMb * 1024 * 1024;
        }

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
                $"compressed_byte_budget={_compressedQueueByteBudget} decoded_reorder_capacity={_decodedReorderCapacity} " +
                $"early_preview_fork={_earlyPreviewForkEnabled} load_shedding={_loadSheddingEnabled} " +
                $"shed_age_ms={_shedAgeTicks * 1000.0 / Stopwatch.Frequency:0.##} " +
                $"strict_consumer_gate={(_strictFrameConsumerActive != null).ToString().ToLowerInvariant()}");
        }
        catch
        {
            Dispose();
            throw;
        }
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
                // Workers have exited but the emitter is wedged (waiting on a gap in the
                // ring). Drain the ring so the emit-loop exit condition becomes satisfiable
                // (_stopped && !HasAliveWorkers() && depth==0), then retry the join with
                // a short timeout.
                DiscardRemainingReorderFrames("emitter_drain_on_stop");
                SignalEmitter("emitter_drain_on_stop");
                if (!emitThread.Join(TimeSpan.FromMilliseconds(500)))
                {
                    failureReason = "emitter_timeout";
                    return false;
                }
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


    private const long DefaultDecodedReorderByteBudget = 1024L * 1024 * 1024;
    private const int MinDecodedReorderCapacity = 32;
    private const int MaxDecodedReorderCapacity = 240;

    private readonly record struct DecodedFrame(
        long SeqNo,
        PooledVideoFrame Frame,
        long DecodedTick);

    // Workers complete out of order, so decoded frames wait here until the
    // emitter can advance _nextEmitSeq. Missing-sequence tracking is explicit
    // so a dropped compressed packet does not permanently stall the pipeline.
    private readonly SortedDictionary<long, DecodedFrame> _reorderFrames = new();
    private readonly SortedSet<long> _knownMissingSequences = new();
    private readonly object _reorderLock = new();
    private readonly int _decodedReorderCapacity;
    private int _reorderBufferDepth;
    private long _nextEmitSeq;
    private long _reorderSkips = 0;
    private long _missingSeqSinceTickMs = -1;

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

        // Backstop: if the ring has frames but the lowest available sequence is above
        // _nextEmitSeq, the emitter is waiting on a sequence that will never arrive.
        // Register every gap in [_nextEmitSeq, lowestAvailable) so the emitter unblocks
        // within ~1 s even if Part (a) ever misses a case.
        lock (_reorderLock)
        {
            if (_reorderFrames.Count > 0)
            {
                var lowestAvailable = PeekFirstSequenceUnderLock();
                if (lowestAvailable > _nextEmitSeq)
                {
                    var from = _nextEmitSeq;
                    for (var gap = _nextEmitSeq; gap < lowestAvailable; gap++)
                    {
                        _knownMissingSequences.Add(gap);
                    }

                    Logger.Log(
                        $"MJPEG_REORDER_STRICT_ADVANCE from={from} to={lowestAvailable} gap={lowestAvailable - from}");
                    Monitor.PulseAll(_reorderLock);
                }
            }
        }

        SignalEmitter("strict_advance");
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
            var waitCount = 0;
            while (!_stopped &&
                   Volatile.Read(ref _fatalErrorSignaled) == 0 &&
                   _reorderFrames.Count >= _decodedReorderCapacity &&
                   seqNo != _nextEmitSeq)
            {
                Monitor.Wait(_reorderLock, TimeSpan.FromMilliseconds(8));
                waitCount++;
                if (waitCount >= 2 &&
                    _reorderFrames.Count >= _decodedReorderCapacity &&
                    !_stopped &&
                    Volatile.Read(ref _fatalErrorSignaled) == 0)
                {
                    // Ring still full after bounded wait — force-drop oldest to
                    // keep workers flowing. A stalled worker would block the
                    // entire compressed queue and stall recording/Flashback.
                    ForceDropOldestReorderFrameUnderLock();
                    waitCount = 0;
                }
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
            var depth = _reorderFrames.Count;
            Volatile.Write(ref _reorderBufferDepth, depth);
            if (depth > Volatile.Read(ref _peakReorderDepth))
            {
                Volatile.Write(ref _peakReorderDepth, depth);
            }

            Monitor.PulseAll(_reorderLock);
            return true;
        }
    }

    // Called under _reorderLock only. Removes the oldest entry from the reorder
    // ring to unblock a worker when the ring is full beyond the bounded wait.
    private void ForceDropOldestReorderFrameUnderLock()
    {
        if (_reorderFrames.Count == 0)
        {
            return;
        }

        var seqNo = PeekFirstSequenceUnderLock();
        var oldest = _reorderFrames[seqNo];
        _reorderFrames.Remove(seqNo);
        Volatile.Write(ref _reorderBufferDepth, _reorderFrames.Count);

        // Force-drop must register the gap so the emitter never waits on a destroyed frame.
        if (seqNo >= _nextEmitSeq)
        {
            _knownMissingSequences.Add(seqNo);
            if (seqNo == _nextEmitSeq)
            {
                _nextEmitSeq++;
            }
        }

        Monitor.PulseAll(_reorderLock);

        var drops = Interlocked.Increment(ref _reorderForceDrops);
        Interlocked.Increment(ref _totalFramesDropped);
        if (drops == 1 || drops % 30 == 0)
        {
            Logger.Log(
                $"MJPEG_REORDER_RING_FULL_DROP seq={oldest.SeqNo} drops={drops} " +
                $"capacity={_decodedReorderCapacity} depth={Volatile.Read(ref _reorderBufferDepth)}");
        }

        oldest.Frame.Dispose();
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
                if (!_earlyPreviewForkEnabled)
                {
                    NotifyPreviewFrameDecoded(frame.Frame);
                }

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
                if (!_earlyPreviewForkEnabled)
                {
                    NotifyPreviewFrameDecoded(frame.Frame);
                }

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

    private static int ResolveDecodedReorderCapacity(int width, int height)
    {
        // Out-of-order window is bounded by decoder count; empirically ≤6 frames at 4K120.
        // Formula: decoderCount * 2 + 4 for 6 workers = 16 slots ≈ 190 MB at 4K NV12.
        // Byte-budget constant is retained for diagnostics compatibility.
        _ = width;
        _ = height;
        var overrideSlots = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_MJPEG_REORDER_SLOTS", 0, 0, 240);
        return overrideSlots > 0 ? overrideSlots : 16;
    }
}

internal sealed class SoftwareMjpegDecoderPermanentException : InvalidOperationException
{
    public SoftwareMjpegDecoderPermanentException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// FFmpeg software MJPEG decoder with YUVJ420P->NV12 conversion.
/// Each instance owns its own AVCodecContext and is NOT thread-safe.
/// </summary>
internal sealed unsafe class SoftwareMjpegDecoder : IDisposable
{
    private AVCodecContext* _decoderCtx;
    private AVFrame* _decodedFrame;
    private AVFrame* _drainFrame;
    private AVPacket* _packet;
    private int _nv12Size;
    private int _width;
    private int _height;
    private bool _initialized;
    private bool _disposed;
    private int _diagDone;

    public int Width => _width;
    public int Height => _height;
    public int Nv12Size => _nv12Size;

    /// <summary>
    /// Decodes MJPEG data and writes NV12 output directly into the caller's buffer,
    /// avoiding an intermediate copy through the decoder's own storage.
    /// </summary>
    public bool DecodeToNv12(ReadOnlySpan<byte> jpegData, Span<byte> nv12Destination)
    {
        if (!_initialized || _decoderCtx == null || _packet == null || _decodedFrame == null)
        {
            throw new InvalidOperationException("Decoder is not initialized.");
        }

        if (jpegData.IsEmpty)
        {
            return false;
        }

        if (nv12Destination.Length < _nv12Size)
        {
            throw new ArgumentException(
                $"NV12 destination too small: {nv12Destination.Length} < {_nv12Size}", nameof(nv12Destination));
        }

        fixed (byte* dataPtr = jpegData)
        {
            ffmpeg.av_packet_unref(_packet);
            _packet->data = dataPtr;
            _packet->size = jpegData.Length;

            var sendResult = ffmpeg.avcodec_send_packet(_decoderCtx, _packet);
            if (sendResult < 0)
            {
                Logger.Log($"SW_MJPEG_SEND_PACKET_FAIL code={sendResult} msg='{GetErrorString(sendResult)}'");
                return false;
            }

            ffmpeg.av_frame_unref(_decodedFrame);
            var receiveResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _decodedFrame);
            if (receiveResult < 0)
            {
                if (receiveResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"SW_MJPEG_RECV_FRAME_FAIL code={receiveResult} msg='{GetErrorString(receiveResult)}'");
                }

                return false;
            }

            while (_drainFrame != null)
            {
                ffmpeg.av_frame_unref(_drainFrame);
                var drainResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _drainFrame);
                if (drainResult < 0)
                {
                    break;
                }

                ffmpeg.av_frame_unref(_decodedFrame);
                ffmpeg.av_frame_move_ref(_decodedFrame, _drainFrame);
            }

            var format = (AVPixelFormat)_decodedFrame->format;
            if (format != AVPixelFormat.AV_PIX_FMT_YUVJ420P &&
                format != AVPixelFormat.AV_PIX_FMT_YUV420P)
            {
                var message = $"SW_MJPEG_UNSUPPORTED_FMT fmt={format} w={_decodedFrame->width} h={_decodedFrame->height}";
                Logger.Log(message);
                throw new SoftwareMjpegDecoderPermanentException(message);
            }

            if (_decodedFrame->width != _width || _decodedFrame->height != _height)
            {
                var message =
                    $"SW_MJPEG_DIM_MISMATCH expected={_width}x{_height} actual={_decodedFrame->width}x{_decodedFrame->height}";
                Logger.Log(message);
                throw new SoftwareMjpegDecoderPermanentException(message);
            }

            if (Interlocked.Exchange(ref _diagDone, 1) == 0)
            {
                Logger.Log(
                    $"SW_MJPEG_DECODE_DIAG fmt={format} w={_decodedFrame->width} h={_decodedFrame->height} " +
                    $"y_stride={_decodedFrame->linesize[0]} u_stride={_decodedFrame->linesize[1]} v_stride={_decodedFrame->linesize[2]}");
            }

            fixed (byte* nv12Ptr = nv12Destination)
            {
                var yBytes = _width * _height;
                if (_decodedFrame->linesize[0] == _width)
                {
                    Buffer.MemoryCopy(_decodedFrame->data[0], nv12Ptr, yBytes, yBytes);
                }
                else
                {
                    for (var row = 0; row < _height; row++)
                    {
                        Buffer.MemoryCopy(
                            _decodedFrame->data[0] + (row * _decodedFrame->linesize[0]),
                            nv12Ptr + (row * _width),
                            _width,
                            _width);
                    }
                }

                var uvDestination = nv12Ptr + yBytes;
                var uvWidth = _width / 2;
                for (var row = 0; row < _height / 2; row++)
                {
                    var uRow = _decodedFrame->data[1] + (row * _decodedFrame->linesize[1]);
                    var vRow = _decodedFrame->data[2] + (row * _decodedFrame->linesize[2]);
                    var destRow = uvDestination + (row * _width);
                    InterleaveUvRow(uRow, vRow, destRow, uvWidth);
                }
            }
        }

        return true;
    }

    // Interleaves planar U/V rows into NV12's UV plane. The scalar loop is the
    // largest per-frame CPU cost after the JPEG decode itself at 4K120, so use
    // 128-bit vectors when available: widening U/V to ushort lanes and OR-ing
    // V into the high byte produces the interleaved little-endian byte pairs.
    private static void InterleaveUvRow(byte* uRow, byte* vRow, byte* destRow, int uvWidth)
    {
        var column = 0;
        if (Vector128.IsHardwareAccelerated && uvWidth >= Vector128<byte>.Count)
        {
            var vectorEnd = uvWidth - (uvWidth % Vector128<byte>.Count);
            for (; column < vectorEnd; column += Vector128<byte>.Count)
            {
                var u = Vector128.Load(uRow + column);
                var v = Vector128.Load(vRow + column);
                var (uLow, uHigh) = Vector128.Widen(u);
                var (vLow, vHigh) = Vector128.Widen(v);
                (uLow | Vector128.ShiftLeft(vLow, 8)).AsByte().Store(destRow + (column * 2));
                (uHigh | Vector128.ShiftLeft(vHigh, 8)).AsByte().Store(destRow + (column * 2) + Vector128<byte>.Count);
            }
        }

        for (; column < uvWidth; column++)
        {
            destRow[column * 2] = uRow[column];
            destRow[(column * 2) + 1] = vRow[column];
        }
    }

    public void Initialize(int width, int height)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("SoftwareMjpegDecoder is already initialized.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
        }

        FfmpegRuntimeInit.EnsureInitialized(requireNativeRuntime: true);

        var codec = ffmpeg.avcodec_find_decoder_by_name("mjpeg");
        if (codec == null)
        {
            throw new InvalidOperationException("mjpeg decoder not found.");
        }

        var decoderCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (decoderCtx == null)
        {
            throw new InvalidOperationException("Failed to allocate mjpeg decoder context.");
        }

        try
        {
            decoderCtx->width = width;
            decoderCtx->height = height;
            decoderCtx->thread_count = 1;

            var openResult = ffmpeg.avcodec_open2(decoderCtx, codec, null);
            if (openResult < 0)
            {
                throw new InvalidOperationException(
                    $"avcodec_open2(mjpeg) failed: code={openResult} msg='{GetErrorString(openResult)}'");
            }

            _decodedFrame = ffmpeg.av_frame_alloc();
            if (_decodedFrame == null)
            {
                throw new InvalidOperationException("Failed to allocate decoded frame.");
            }

            _drainFrame = ffmpeg.av_frame_alloc();
            if (_drainFrame == null)
            {
                throw new InvalidOperationException("Failed to allocate drain frame.");
            }

            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                throw new InvalidOperationException("Failed to allocate packet.");
            }

            _decoderCtx = decoderCtx;
            _width = width;
            _height = height;
            _nv12Size = checked(width * height * 3 / 2);
            _initialized = true;

            decoderCtx = null;

            Logger.Log($"SW_MJPEG_DECODER_INIT width={width} height={height} codec=mjpeg");
        }
        catch
        {
            if (decoderCtx != null)
            {
                ffmpeg.avcodec_free_context(&decoderCtx);
            }

            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_packet != null)
        {
            var packet = _packet;
            ffmpeg.av_packet_free(&packet);
            _packet = null;
        }

        if (_decodedFrame != null)
        {
            var decodedFrame = _decodedFrame;
            ffmpeg.av_frame_free(&decodedFrame);
            _decodedFrame = null;
        }

        if (_drainFrame != null)
        {
            var drainFrame = _drainFrame;
            ffmpeg.av_frame_free(&drainFrame);
            _drainFrame = null;
        }

        if (_decoderCtx != null)
        {
            var decoderCtx = _decoderCtx;
            ffmpeg.avcodec_free_context(&decoderCtx);
            _decoderCtx = null;
        }

        _initialized = false;
        Logger.Log("SW_MJPEG_DECODER_DISPOSED");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }
}

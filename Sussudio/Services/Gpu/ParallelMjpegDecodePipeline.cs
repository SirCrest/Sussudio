using System;
using System.Buffers;
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
    public delegate void EmitFrameCallback(PooledVideoFrame frame);
    public delegate void PreviewFrameCallback(PooledVideoFrameLease frame);

    private const int WorkQueueItemCapacityPerDecoder = 8;
    private const long DefaultCompressedQueueByteBudget = 512L * 1024 * 1024;

    private readonly EmitFrameCallback _emitCallback;
    private readonly PreviewFrameCallback? _previewCallback;
    private readonly Action<Exception>? _fatalErrorCallback;
    private readonly Channel<MjpegWorkItem> _workQueue;
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

using System;
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

    private readonly EmitFrameCallback _emitCallback;
    private readonly PreviewFrameCallback? _previewCallback;
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
    private long _decodeFailures;
    private long _reorderCollisions;
    private long _emitFailures;
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

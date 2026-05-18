using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private readonly SoftwareMjpegDecoder[] _decoders;
    private readonly Thread[] _workers;

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
}

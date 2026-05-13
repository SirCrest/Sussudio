using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
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
}

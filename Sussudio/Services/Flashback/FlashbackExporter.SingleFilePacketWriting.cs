using System;
using System.Collections.Generic;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(
        int streamCount,
        int videoStreamIndex,
        int[] streamMap,
        TimeSpan outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var timestampBasesUs = new long[streamCount]; // per-stream base in microseconds
        var hasTimestampBase = new bool[streamCount];
        long? globalMinBaseUs = null; // global minimum base in microseconds
        var packetCounts = new long[streamCount];
        long totalPackets = 0;
        var outPtsLimit = ToAvTimeBaseTimestampOrMax(outPoint);
        var usTimeBase = new AVRational { num = 1, den = 1_000_000 };

        // Read and remux packets — two-phase approach:
        // Phase 1: buffer packets until all stream timestamp bases are discovered (globalMinBaseUs is final)
        // Phase 2: flush buffer then process remaining packets inline with stable base
        var bufferedPackets = new List<IntPtr>();
        var bufferedStreamIndices = new List<int>();
        var allBasesDiscovered = false;
        var lastProgressHeartbeatTick = 0L;

        var packet = ffmpeg.av_packet_alloc();
        if (packet == null)
        {
            throw new InvalidOperationException("Failed to allocate AVPacket.");
        }

        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);
                if (readResult == ffmpeg.AVERROR_EOF)
                {
                    break;
                }
                ThrowIfError(readResult, "av_read_frame");
                if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))
                {
                    ReportProgress(progress, new ExportProgress(0, 1, 0), "single_heartbeat");
                }

                try
                {
                    var streamIndex = packet->stream_index;
                    if (streamIndex < 0 || streamIndex >= streamCount)
                    {
                        continue;
                    }

                    // Skip streams that were filtered out (invalid codec params)
                    var outputIndex = streamMap[streamIndex];
                    if (outputIndex < 0)
                        continue;

                    var inStream = _activeInputContext->streams[streamIndex];
                    var outStream = _activeOutputContext->streams[outputIndex];

                    // Check if we've passed outPoint (applies to all streams)
                    if (packet->pts != ffmpeg.AV_NOPTS_VALUE && outPtsLimit < long.MaxValue)
                    {
                        var ptsUs = ffmpeg.av_rescale_q(packet->pts, inStream->time_base,
                            new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE });
                        if (ptsUs > outPtsLimit)
                        {
                            if (streamIndex == videoStreamIndex)
                                break; // Video past out-point — stop reading entirely
                            continue; // Non-video past out-point — skip but keep reading for video
                        }
                    }

                    // Rescale timestamps
                    ffmpeg.av_packet_rescale_ts(packet, inStream->time_base, outStream->time_base);

                    // Discover per-stream timestamp base and track global minimum (in microseconds)
                    if (!hasTimestampBase[streamIndex])
                    {
                        if (TryResolveTimestampBase(packet, out var tsBase))
                        {
                            var baseUs = ffmpeg.av_rescale_q(tsBase, outStream->time_base, usTimeBase);
                            timestampBasesUs[streamIndex] = baseUs;
                            hasTimestampBase[streamIndex] = true;

                            if (globalMinBaseUs == null || baseUs < globalMinBaseUs.Value)
                            {
                                globalMinBaseUs = baseUs;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // Phase 1: buffer until all active stream bases are known so globalMinBaseUs is final.
                    // Cap buffered packets to avoid unbounded memory if a configured stream never
                    // produces packets (e.g., microphone enabled but silent).
                    const int MaxBufferedPackets = 600;
                    if (!allBasesDiscovered)
                    {
                        var clone = ClonePacketOrThrow(packet, "single_buffer");
                        bufferedPackets.Add((IntPtr)clone);
                        bufferedStreamIndices.Add(streamIndex);

                        // Check if all mapped streams have bases discovered,
                        // OR we've buffered enough packets that missing streams are assumed empty
                        allBasesDiscovered = true;
                        for (int i = 0; i < streamCount; i++)
                        {
                            if (streamMap[i] >= 0 && !hasTimestampBase[i]) { allBasesDiscovered = false; break; }
                        }
                        if (!allBasesDiscovered && bufferedPackets.Count >= MaxBufferedPackets)
                        {
                            allBasesDiscovered = true;
                            globalMinBaseUs ??= 0; // Silent streams never set a base — default to 0
                            var discoveredCount = 0;
                            for (var i = 0; i < streamCount; i++) { if (hasTimestampBase[i]) discoveredCount++; }
                            Logger.Log($"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH buffered={bufferedPackets.Count} streams_discovered={discoveredCount}/{streamCount}");
                        }

                        if (allBasesDiscovered)
                        {
                            totalPackets += FlushBufferedPackets(
                                bufferedPackets, bufferedStreamIndices, streamMap,
                                globalMinBaseUs!.Value, usTimeBase, packetCounts);
                        }
                        continue;
                    }

                    // Phase 2: inline write with stable globalMinBaseUs
                    var baseTs = ffmpeg.av_rescale_q(globalMinBaseUs!.Value, usTimeBase, outStream->time_base);
                    if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                        packet->pts -= baseTs;
                    if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                        packet->dts -= baseTs;
                    NormalizePacketTimestampsBeforeWrite(packet);

                    packet->pos = -1;
                    packet->stream_index = outStream->index;

                    ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
                    packetCounts[streamIndex]++;
                    totalPackets++;
                    ThrottleExportWriterIfNeeded(totalPackets);
                }
                finally
                {
                    ffmpeg.av_packet_unref(packet);
                }
            }

            // Phase 1 EOF: flush buffered packets with best-known base if not all streams discovered
            if (!allBasesDiscovered && bufferedPackets.Count > 0)
            {
                globalMinBaseUs ??= 0; // No stream ever produced a base — default to 0
            }
            if (!allBasesDiscovered && globalMinBaseUs.HasValue && bufferedPackets.Count > 0)
            {
                var discoveredCount = 0;
                for (int i = 0; i < streamCount; i++) { if (hasTimestampBase[i]) discoveredCount++; }
                Logger.Log($"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH streams_discovered={discoveredCount}/{streamCount} buffered={bufferedPackets.Count}");
                totalPackets += FlushBufferedPackets(
                    bufferedPackets, bufferedStreamIndices, streamMap,
                    globalMinBaseUs!.Value, usTimeBase, packetCounts);
            }
        }
        finally
        {
            // Free any buffered packets not yet flushed (early EOF, error, or cancellation)
            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);
            var packetToFree = packet;
            ffmpeg.av_packet_free(&packetToFree);
        }

        // Log per-stream base drift warning if bases differ by more than 100ms
        LogTimestampBaseDrift(timestampBasesUs, hasTimestampBase);

        // Validate per-stream packet counts
        if (videoStreamIndex >= 0 && videoStreamIndex < streamCount && packetCounts[videoStreamIndex] == 0)
        {
            const string message = "Flashback export failed: no video packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), totalPackets);
        }

        for (var i = 0; i < streamCount; i++)
        {
            if (i != videoStreamIndex && hasTimestampBase[i] && packetCounts[i] == 0)
            {
                Logger.Log($"FLASHBACK_EXPORT_WARN stream={i} reason='no_packets_written' (non-video stream)");
            }
        }

        if (totalPackets == 0)
        {
            const string message = "Flashback export failed: no packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), totalPackets);
        }

        return new SingleFilePacketWriteResult(null, totalPackets);
    }

    private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);
}

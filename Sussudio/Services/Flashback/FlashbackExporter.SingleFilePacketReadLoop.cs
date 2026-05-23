using System;
using System.Collections.Generic;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Flashback;

// Single-file packet read loop: read frames from the active input, filter mapped
// streams, and delegate stateful timestamp/write decisions to focused owners.
internal sealed unsafe partial class FlashbackExporter
{
    private static readonly AVRational SingleFilePacketUsTimeBase = new() { num = 1, den = 1_000_000 };
    private const int SingleFileMaxBufferedPackets = 600;

    private static SingleFilePacketWriteState CreateSingleFilePacketWriteState(int streamCount)
        => new(
            new long[streamCount],
            new bool[streamCount],
            new long[streamCount],
            new List<IntPtr>(),
            new List<int>());

    private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(
        int streamCount,
        int videoStreamIndex,
        int[] streamMap,
        TimeSpan outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var packetState = CreateSingleFilePacketWriteState(streamCount);
        WriteSingleFilePacketReadLoop(
            streamCount,
            videoStreamIndex,
            streamMap,
            ToAvTimeBaseTimestampOrMax(outPoint),
            progress,
            ct,
            ref packetState);

        LogTimestampBaseDrift(packetState.TimestampBasesUs, packetState.HasTimestampBase);

        if (videoStreamIndex >= 0 && videoStreamIndex < streamCount && packetState.PacketCounts[videoStreamIndex] == 0)
        {
            const string message = "Flashback export failed: no video packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), packetState.TotalPackets);
        }

        for (var i = 0; i < streamCount; i++)
        {
            if (i != videoStreamIndex && packetState.HasTimestampBase[i] && packetState.PacketCounts[i] == 0)
            {
                Logger.Log($"FLASHBACK_EXPORT_WARN stream={i} reason='no_packets_written' (non-video stream)");
            }
        }

        if (packetState.TotalPackets == 0)
        {
            const string message = "Flashback export failed: no packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), packetState.TotalPackets);
        }

        return new SingleFilePacketWriteResult(null, packetState.TotalPackets);
    }

    private void WriteSingleFilePacketReadLoop(
        int streamCount,
        int videoStreamIndex,
        int[] streamMap,
        long outPtsLimit,
        IProgress<ExportProgress>? progress,
        CancellationToken ct,
        ref SingleFilePacketWriteState packetState)
    {
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
                ReportSingleFileProgressHeartbeat(progress, ref packetState);

                try
                {
                    var streamIndex = packet->stream_index;
                    if (streamIndex < 0 || streamIndex >= streamCount)
                    {
                        continue;
                    }

                    var outputIndex = streamMap[streamIndex];
                    if (outputIndex < 0)
                    {
                        continue;
                    }

                    var inputStream = _activeInputContext->streams[streamIndex];
                    var outputStream = _activeOutputContext->streams[outputIndex];
                    var pastOutPoint = PacketPtsExceedsSingleFileOutPoint(packet, inputStream, outPtsLimit);
                    if (pastOutPoint && streamIndex == videoStreamIndex)
                    {
                        break;
                    }

                    if (pastOutPoint)
                    {
                        continue;
                    }

                    ffmpeg.av_packet_rescale_ts(packet, inputStream->time_base, outputStream->time_base);

                    if (!packetState.HasTimestampBase[streamIndex] &&
                        !TryRecordSingleFileTimestampBase(ref packetState, packet, streamIndex, outputStream))
                    {
                        continue;
                    }

                    if (!packetState.AllBasesDiscovered)
                    {
                        BufferSingleFilePacketOrFlushReady(streamCount, streamMap, streamIndex, ref packetState, packet);
                        continue;
                    }

                    WriteSingleFilePacket(packet, streamIndex, outputStream, ref packetState);
                }
                finally
                {
                    ffmpeg.av_packet_unref(packet);
                }
            }

            FlushSingleFileBufferedPacketsAtEof(streamCount, streamMap, ref packetState);
        }
        finally
        {
            FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);
            var packetToFree = packet;
            ffmpeg.av_packet_free(&packetToFree);
        }
    }

    private static bool TryRecordSingleFileTimestampBase(
        ref SingleFilePacketWriteState state,
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream)
    {
        if (!TryResolveTimestampBase(packet, out var timestampBase))
        {
            return false;
        }

        var baseUs = ffmpeg.av_rescale_q(timestampBase, outputStream->time_base, SingleFilePacketUsTimeBase);
        state.TimestampBasesUs[streamIndex] = baseUs;
        state.HasTimestampBase[streamIndex] = true;
        if (state.GlobalMinBaseUs == null || baseUs < state.GlobalMinBaseUs.Value)
        {
            state.GlobalMinBaseUs = baseUs;
        }

        return true;
    }

    private void BufferSingleFilePacketOrFlushReady(
        int streamCount,
        int[] streamMap,
        int streamIndex,
        ref SingleFilePacketWriteState state,
        AVPacket* packet)
    {
        var clone = ClonePacketOrThrow(packet, "single_buffer");
        state.BufferedPackets.Add((IntPtr)clone);
        state.BufferedStreamIndices.Add(streamIndex);

        state.AllBasesDiscovered = HasDiscoveredAllMappedSingleFileBases(in state, streamCount, streamMap);
        if (!state.AllBasesDiscovered && state.BufferedPackets.Count >= SingleFileMaxBufferedPackets)
        {
            state.AllBasesDiscovered = true;
            state.GlobalMinBaseUs ??= 0;
            Logger.Log(
                $"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH buffered={state.BufferedPackets.Count} streams_discovered={CountDiscoveredSingleFileBases(in state, streamCount)}/{streamCount}");
        }

        if (state.AllBasesDiscovered)
        {
            state.TotalPackets += FlushBufferedPackets(
                state.BufferedPackets,
                state.BufferedStreamIndices,
                streamMap,
                state.GlobalMinBaseUs!.Value,
                SingleFilePacketUsTimeBase,
                state.PacketCounts);
        }
    }

    private void FlushSingleFileBufferedPacketsAtEof(
        int streamCount,
        int[] streamMap,
        ref SingleFilePacketWriteState state)
    {
        if (state.AllBasesDiscovered || state.BufferedPackets.Count == 0)
        {
            return;
        }

        state.GlobalMinBaseUs ??= 0;
        Logger.Log(
            $"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH streams_discovered={CountDiscoveredSingleFileBases(in state, streamCount)}/{streamCount} buffered={state.BufferedPackets.Count}");
        state.TotalPackets += FlushBufferedPackets(
            state.BufferedPackets,
            state.BufferedStreamIndices,
            streamMap,
            state.GlobalMinBaseUs!.Value,
            SingleFilePacketUsTimeBase,
            state.PacketCounts);
    }

    private void WriteSingleFilePacket(
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream,
        ref SingleFilePacketWriteState state)
    {
        var baseTs = ffmpeg.av_rescale_q(state.GlobalMinBaseUs!.Value, SingleFilePacketUsTimeBase, outputStream->time_base);
        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->pts -= baseTs;
        }

        if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->dts -= baseTs;
        }

        NormalizePacketTimestampsBeforeWrite(packet);
        packet->pos = -1;
        packet->stream_index = outputStream->index;

        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
        state.PacketCounts[streamIndex]++;
        state.TotalPackets++;
        ThrottleExportWriterIfNeeded(state.TotalPackets);
    }

    private static bool PacketPtsExceedsSingleFileOutPoint(
        AVPacket* packet,
        AVStream* inputStream,
        long outPtsLimit)
    {
        if (packet->pts == ffmpeg.AV_NOPTS_VALUE || outPtsLimit >= long.MaxValue)
        {
            return false;
        }

        var ptsUs = ffmpeg.av_rescale_q(packet->pts, inputStream->time_base, SingleFilePacketUsTimeBase);
        return ptsUs > outPtsLimit;
    }

    private static void ReportSingleFileProgressHeartbeat(
        IProgress<ExportProgress>? progress,
        ref SingleFilePacketWriteState state)
    {
        if (ShouldReportProgressHeartbeat(ref state.LastProgressHeartbeatTick))
        {
            ReportProgress(progress, new ExportProgress(0, 1, 0), "single_heartbeat");
        }
    }

    private static bool HasDiscoveredAllMappedSingleFileBases(
        in SingleFilePacketWriteState state,
        int streamCount,
        int[] streamMap)
    {
        for (var i = 0; i < streamCount; i++)
        {
            if (streamMap[i] >= 0 && !state.HasTimestampBase[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int CountDiscoveredSingleFileBases(in SingleFilePacketWriteState state, int streamCount)
    {
        var discoveredCount = 0;
        for (var i = 0; i < streamCount; i++)
        {
            if (state.HasTimestampBase[i])
            {
                discoveredCount++;
            }
        }

        return discoveredCount;
    }

    private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);

    private struct SingleFilePacketWriteState
    {
        public SingleFilePacketWriteState(
            long[] timestampBasesUs,
            bool[] hasTimestampBase,
            long[] packetCounts,
            List<IntPtr> bufferedPackets,
            List<int> bufferedStreamIndices)
        {
            TimestampBasesUs = timestampBasesUs;
            HasTimestampBase = hasTimestampBase;
            PacketCounts = packetCounts;
            BufferedPackets = bufferedPackets;
            BufferedStreamIndices = bufferedStreamIndices;
        }

        public long[] TimestampBasesUs { get; }
        public bool[] HasTimestampBase { get; }
        public long[] PacketCounts { get; }
        public List<IntPtr> BufferedPackets { get; }
        public List<int> BufferedStreamIndices { get; }
        public long? GlobalMinBaseUs { get; set; }
        public bool AllBasesDiscovered { get; set; }
        public long TotalPackets { get; set; }
        public long LastProgressHeartbeatTick;
    }
}

using System;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Flashback;

// Single-file packet read loop: read frames from the active input, filter mapped
// streams, and delegate stateful timestamp/write decisions to focused owners.
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

    private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);
}

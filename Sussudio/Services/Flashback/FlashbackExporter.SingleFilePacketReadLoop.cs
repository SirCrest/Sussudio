using System;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

// Single-file packet read loop: read frames from the active input, filter mapped
// streams, and delegate stateful timestamp/write decisions to focused owners.
internal sealed unsafe partial class FlashbackExporter
{
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

    private static void ReportSingleFileProgressHeartbeat(
        IProgress<ExportProgress>? progress,
        ref SingleFilePacketWriteState state)
    {
        if (ShouldReportProgressHeartbeat(ref state.LastProgressHeartbeatTick))
        {
            ReportProgress(progress, new ExportProgress(0, 1, 0), "single_heartbeat");
        }
    }
}

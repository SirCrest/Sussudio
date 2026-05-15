using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    /// <summary>
    /// Flushes all buffered packets by subtracting <paramref name="globalMinBaseUs"/> from PTS/DTS,
    /// clamping negative values to zero, and writing each packet to the active output context.
    /// Frees all packet clones and clears both lists. Returns the number of packets written.
    /// </summary>
    private long FlushBufferedPackets(
        List<IntPtr> bufferedPackets,
        List<int> bufferedStreamIndices,
        int[] streamMap,
        long globalMinBaseUs,
        AVRational usTimeBase,
        long[] packetCounts)
    {
        long flushed = 0;
        try
        {
            for (int bi = 0; bi < bufferedPackets.Count; bi++)
            {
                var buffPkt = (AVPacket*)bufferedPackets[bi];
                var si = bufferedStreamIndices[bi];
                var oi = streamMap[si];
                var outStr = _activeOutputContext->streams[oi];
                var bTs = ffmpeg.av_rescale_q(globalMinBaseUs, usTimeBase, outStr->time_base);
                if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                    buffPkt->pts -= bTs;
                if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                    buffPkt->dts -= bTs;
                NormalizePacketTimestampsBeforeWrite(buffPkt);
                buffPkt->pos = -1;
                buffPkt->stream_index = oi;
                ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                packetCounts[si]++;
                flushed++;
                ffmpeg.av_packet_free(&buffPkt);
                bufferedPackets[bi] = IntPtr.Zero;
            }
        }
        finally
        {
            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);
        }

        return flushed;
    }

    private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)
    {
        foreach (var pktPtr in bufferedPackets)
        {
            if (pktPtr != IntPtr.Zero)
            {
                var p = (AVPacket*)pktPtr;
                ffmpeg.av_packet_free(&p);
            }
        }

        bufferedPackets.Clear();
        bufferedStreamIndices?.Clear();
    }

    private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)
    {
        var clone = ffmpeg.av_packet_clone(packet);
        if (clone != null)
        {
            return clone;
        }

        Logger.Log($"FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
        throw new InvalidOperationException($"FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
    }
}

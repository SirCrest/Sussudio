using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private static long ResolveFrameDurationUs(AVStream* videoStream)
    {
        if (videoStream != null && IsValidPositiveRational(videoStream->avg_frame_rate))
        {
            return Math.Max(1, 1_000_000L * videoStream->avg_frame_rate.den / videoStream->avg_frame_rate.num);
        }

        if (videoStream != null && IsValidPositiveRational(videoStream->r_frame_rate))
        {
            return Math.Max(1, 1_000_000L * videoStream->r_frame_rate.den / videoStream->r_frame_rate.num);
        }

        return 33333; // fallback ~30fps
    }

    private static bool IsValidPositiveRational(AVRational value)
        => value.num > 0 && value.den > 0;

    private static long ResolveSegmentBoundaryTimestampRepairUs(
        long ptsUs,
        long outputPtsOffsetUs,
        long frameDurUs,
        int segmentVideoPacketsSeen,
        long existingRepairUs)
    {
        if (outputPtsOffsetUs <= 0 ||
            frameDurUs <= 0 ||
            segmentVideoPacketsSeen <= 0 ||
            segmentVideoPacketsSeen > 12)
        {
            return 0;
        }

        var expectedPtsUs = outputPtsOffsetUs + segmentVideoPacketsSeen * frameDurUs;
        var repairedPtsUs = ptsUs - existingRepairUs;
        var gapUs = repairedPtsUs - expectedPtsUs;
        var thresholdUs = frameDurUs + frameDurUs / 2;
        if (gapUs <= thresholdUs)
        {
            return 0;
        }

        return gapUs;
    }

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

    private static bool TryResolveTimestampBase(AVPacket* packet, out long timestampBase)
    {
        timestampBase = 0;

        var hasPts = packet->pts != ffmpeg.AV_NOPTS_VALUE;
        var hasDts = packet->dts != ffmpeg.AV_NOPTS_VALUE;
        if (!hasPts && !hasDts)
        {
            return false;
        }

        if (hasPts && hasDts)
        {
            timestampBase = Math.Min(packet->pts, packet->dts);
            return true;
        }

        timestampBase = hasPts ? packet->pts : packet->dts;
        return true;
    }

    private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet)
    {
        if (packet == null)
        {
            return;
        }

        if (packet->pts != ffmpeg.AV_NOPTS_VALUE && packet->pts < 0)
        {
            packet->pts = 0;
        }

        if (packet->dts != ffmpeg.AV_NOPTS_VALUE && packet->dts < 0)
        {
            packet->dts = 0;
        }

        if (packet->pts != ffmpeg.AV_NOPTS_VALUE &&
            packet->dts != ffmpeg.AV_NOPTS_VALUE &&
            packet->pts < packet->dts)
        {
            packet->pts = packet->dts;
        }
    }
}

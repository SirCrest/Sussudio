using System;
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

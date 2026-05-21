using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

// Single-file packet timestamp clipping and rebasing before native remux writes.
internal sealed unsafe partial class FlashbackExporter
{
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
}

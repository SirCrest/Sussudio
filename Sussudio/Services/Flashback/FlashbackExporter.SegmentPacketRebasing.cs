using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(
        ref SegmentPacketWriteState state,
        AVPacket* packet,
        int sourceStreamIndex,
        int outputStreamIndex,
        AVStream* outputStream,
        long[] lastDtsPerOutputStream)
    {
        // Check outPoint against absolute PTS before remapping. At this point
        // packet->pts is in the output time base but still absolute encoder PTS.
        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
        {
            var absolutePtsUs = ffmpeg.av_rescale_q(packet->pts, outputStream->time_base, SegmentPacketUsTimeBase);
            var comparePtsUs = state.UseSegmentTimeline
                ? absolutePtsUs - state.MinBaseUs!.Value
                : absolutePtsUs;
            if (sourceStreamIndex == state.VideoStreamIndex && absolutePtsUs > state.AbsMaxPtsUs)
            {
                state.AbsMaxPtsUs = absolutePtsUs;
            }

            if (state.UseSegmentTimeline && comparePtsUs < state.SegmentInOffsetUs)
            {
                return SegmentPacketWriteOutcome.Skipped;
            }

            if (state.SegmentOutOffsetUs < long.MaxValue && comparePtsUs > state.SegmentOutOffsetUs)
            {
                return sourceStreamIndex == state.VideoStreamIndex
                    ? SegmentPacketWriteOutcome.StopAtVideoOutPoint
                    : SegmentPacketWriteOutcome.Skipped;
            }
        }

        // Remap: subtract segment base, add cross-segment output offset.
        var segmentBaseTs = ffmpeg.av_rescale_q(
            state.MinBaseUs!.Value,
            SegmentPacketUsTimeBase,
            outputStream->time_base);
        var offsetTs = ffmpeg.av_rescale_q(
            state.OutputPtsOffsetUs,
            SegmentPacketUsTimeBase,
            outputStream->time_base);

        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->pts = packet->pts - segmentBaseTs + offsetTs;
            var ptsUs = ffmpeg.av_rescale_q(packet->pts, outputStream->time_base, SegmentPacketUsTimeBase);
            if (state.UseSegmentTimeline && sourceStreamIndex == state.VideoStreamIndex)
            {
                var repairUs = ResolveSegmentBoundaryTimestampRepairUs(
                    ptsUs,
                    state.OutputPtsOffsetUs,
                    state.VideoFrameDurationUs,
                    state.VideoPacketsSeen,
                    state.VideoTimestampRepairUs);
                if (repairUs > 0)
                {
                    state.VideoTimestampRepairUs += repairUs;
                    Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR seg={state.SegmentIndex} stream={sourceStreamIndex} repair_us={repairUs} total_repair_us={state.VideoTimestampRepairUs}");
                }

                if (state.VideoTimestampRepairUs > 0)
                {
                    var repairTs = ffmpeg.av_rescale_q(
                        state.VideoTimestampRepairUs,
                        SegmentPacketUsTimeBase,
                        outputStream->time_base);
                    packet->pts -= repairTs;
                    ptsUs = ffmpeg.av_rescale_q(packet->pts, outputStream->time_base, SegmentPacketUsTimeBase);
                }

                state.VideoPacketsSeen++;
            }

            if (ptsUs > state.MaxPtsUs)
            {
                state.MaxPtsUs = ptsUs;
            }
        }

        if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->dts = packet->dts - segmentBaseTs + offsetTs;
            if (state.UseSegmentTimeline &&
                sourceStreamIndex == state.VideoStreamIndex &&
                state.VideoTimestampRepairUs > 0)
            {
                var repairTs = ffmpeg.av_rescale_q(
                    state.VideoTimestampRepairUs,
                    SegmentPacketUsTimeBase,
                    outputStream->time_base);
                packet->dts -= repairTs;
            }

            // mp4 muxing rejects non-monotonic DTS; preserve the existing per-output-stream clamp.
            if (outputStreamIndex < lastDtsPerOutputStream.Length &&
                lastDtsPerOutputStream[outputStreamIndex] != long.MinValue &&
                packet->dts <= lastDtsPerOutputStream[outputStreamIndex])
            {
                packet->dts = lastDtsPerOutputStream[outputStreamIndex] + 1;
            }
        }

        if (outputStreamIndex < lastDtsPerOutputStream.Length &&
            packet->dts != ffmpeg.AV_NOPTS_VALUE)
        {
            lastDtsPerOutputStream[outputStreamIndex] = packet->dts;
        }

        NormalizePacketTimestampsBeforeWrite(packet);
        packet->pos = -1;
        packet->stream_index = outputStreamIndex;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
        return SegmentPacketWriteOutcome.Written;
    }
}

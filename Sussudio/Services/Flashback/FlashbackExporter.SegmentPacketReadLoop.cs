using System;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

// Per-segment packet read loop: read frames from the active input, discover
// timestamp bases, buffer early packets, and write rebased packets.
internal sealed unsafe partial class FlashbackExporter
{
    private void WriteSegmentPacketReadLoop(
        int segIdx,
        int segmentCount,
        int streamCount,
        int videoStreamIndex,
        int currentStreamCount,
        int[] streamMap,
        long[] lastDtsPerStream,
        long totalEstimatedBytes,
        long bytesProcessed,
        long outputPtsOffsetUs,
        bool useSegmentTimeline,
        long segmentInOffsetUs,
        long segmentOutOffsetUs,
        AVPacket* packet,
        IProgress<ExportProgress>? progress,
        CancellationToken ct,
        ref long totalPackets,
        out SegmentPacketWriteState segmentPacketState)
    {
        var lastProgressHeartbeatTick = 0L;
        var segmentVideoFrameDurUs = 33333L;
        if (useSegmentTimeline &&
            videoStreamIndex >= 0 &&
            videoStreamIndex < currentStreamCount)
        {
            segmentVideoFrameDurUs = ResolveFrameDurationUs(_activeInputContext->streams[videoStreamIndex]);
        }
        segmentPacketState = CreateSegmentPacketWriteState(
            segIdx,
            streamCount,
            useSegmentTimeline,
            segmentInOffsetUs,
            segmentOutOffsetUs,
            outputPtsOffsetUs,
            videoStreamIndex,
            segmentVideoFrameDurUs);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);
            if (readResult == ffmpeg.AVERROR_EOF)
                break;
            ThrowIfError(readResult, "av_read_frame");
            if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))
            {
                ReportProgress(
                    progress,
                    new ExportProgress(
                        segIdx,
                        segmentCount,
                        totalEstimatedBytes > 0
                            ? 100.0 * bytesProcessed / totalEstimatedBytes
                            : 100.0 * segIdx / segmentCount),
                    "segment_heartbeat");
            }

            try
            {
                var streamIndex = packet->stream_index;
                if (streamIndex < 0 || streamIndex >= streamCount)
                    continue;

                // Skip streams filtered out by CopyTemplateStreams
                var mappedIndex = streamMap[streamIndex];
                if (mappedIndex < 0)
                    continue;

                var inStream = _activeInputContext->streams[streamIndex];
                var outStream = _activeOutputContext->streams[mappedIndex];

                // Rescale to output time base
                ffmpeg.av_packet_rescale_ts(packet, inStream->time_base, outStream->time_base);

                // Discover per-stream base
                if (!segmentPacketState.HasTimestampBase[streamIndex])
                {
                    if (!TryRecordSegmentTimestampBase(ref segmentPacketState, packet, streamIndex, outStream))
                    {
                        continue;
                    }
                }

                // Phase 1: buffer until all bases known
                const int MaxBufferedPackets = 600;
                if (!segmentPacketState.AllBasesDiscovered)
                {
                    var clone = ClonePacketOrThrow(packet, "segment_buffer");
                    segmentPacketState.BufferedPackets.Add((IntPtr)clone);
                    segmentPacketState.BufferedStreamIndices.Add(streamIndex);

                    segmentPacketState.AllBasesDiscovered = HasDiscoveredAllMappedSegmentBases(
                        in segmentPacketState,
                        streamCount,
                        streamMap);
                    if (!segmentPacketState.AllBasesDiscovered &&
                        segmentPacketState.BufferedPackets.Count >= MaxBufferedPackets)
                    {
                        segmentPacketState.MinBaseUs ??= 0; // Silent streams never set a base - default to 0
                        segmentPacketState.AllBasesDiscovered = true;
                    }

                    if (segmentPacketState.AllBasesDiscovered)
                    {
                        totalPackets += FlushSegmentBufferedPackets(
                            ref segmentPacketState,
                            streamMap,
                            lastDtsPerStream,
                            out var stopFlushing);
                        if (stopFlushing)
                            break;
                    }
                    continue;
                }

                var writeOutcome = WriteRebasedSegmentPacket(
                    ref segmentPacketState,
                    packet,
                    streamIndex,
                    mappedIndex,
                    outStream,
                    lastDtsPerStream);
                if (writeOutcome == SegmentPacketWriteOutcome.StopAtVideoOutPoint)
                {
                    break;
                }
                if (writeOutcome == SegmentPacketWriteOutcome.Written)
                {
                    totalPackets++;
                    ThrottleExportWriterIfNeeded(totalPackets);
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(packet);
            }
        }

        // EOF: if Phase 1 never completed (some configured stream, typically a
        // silent mic, never produced packets and the buffer never reached the
        // 600-packet cap), flush whatever we have using a fallback base of 0.
        // Without this, every video packet in a short segment would be silently
        // discarded by the FreeBufferedPackets path that used to live here.
        if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)
        {
            segmentPacketState.MinBaseUs ??= 0;
            segmentPacketState.AllBasesDiscovered = true;
            var discoveredCount = 0;
            for (var i = 0; i < streamCount; i++) { if (segmentPacketState.HasTimestampBase[i]) discoveredCount++; }
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx} buffered={segmentPacketState.BufferedPackets.Count} streams_discovered={discoveredCount}/{streamCount}");
            totalPackets += FlushSegmentBufferedPackets(
                ref segmentPacketState,
                streamMap,
                lastDtsPerStream,
                out _);
        }
        else
        {
            // Either Phase 1 completed inline (nothing to flush) or buffer is empty.
            // FreeBufferedPackets is a no-op on an empty list; safe in both cases.
            FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);
        }
    }
}

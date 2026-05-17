using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

// Segment export packet remuxing: template initialization, per-segment loop
// orchestration, and skip validation.
internal sealed unsafe partial class FlashbackExporter
{
    private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(
        IReadOnlyList<FlashbackExportSegment> segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string tmpPath,
        string outputPath,
        bool fastStart,
        long totalEstimatedBytes,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);

        // Output state - initialized from first segment
        int streamCount = 0;
        int videoStreamIndex = -1;
        int[] streamMap = Array.Empty<int>();
        long totalPackets = 0;
        long bytesProcessed = 0;
        var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);

        // Cross-segment PTS tracking (in microseconds)
        long outputPtsOffsetUs = 0; // accumulated offset for output continuity

        // Per-stream last DTS tracking for monotonicity enforcement
        var lastDtsPerStream = new long[64]; // indexed by OUTPUT stream index
        for (int i = 0; i < lastDtsPerStream.Length; i++) lastDtsPerStream[i] = long.MinValue;

        if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{templateFailure}'");
            return new SegmentPacketWriteResult(FinalizeResult.Failure(outputPath, templateFailure), 0);
        }

        var packet = ffmpeg.av_packet_alloc();
        if (packet == null)
            throw new InvalidOperationException("Failed to allocate AVPacket.");

        try
        {
            for (var segIdx = 0; segIdx < segments.Count; segIdx++)
            {
                ct.ThrowIfCancellationRequested();
                var segment = segments[segIdx];
                var segPath = segment.Path;
                var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);
                if (segmentExportWindow.SkipBecauseEmpty)
                {
                    continue;
                }
                var useSegmentTimeline = segmentExportWindow.UseSegmentTimeline;
                var segmentInOffsetUs = segmentExportWindow.SegmentInOffsetUs;
                var segmentOutOffsetUs = segmentExportWindow.SegmentOutOffsetUs;

                if (!TryOpenSegmentInputForExport(
                        segment,
                        segPath,
                        streamCount,
                        streamMap,
                        ref requestedSegmentSkips,
                        out var currentStreamCount))
                {
                    continue;
                }

                // Seek to inPoint in first segment
                if (segIdx == 0 && inPoint > TimeSpan.Zero && !useSegmentTimeline)
                {
                    var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);
                    var seekResult = ffmpeg.av_seek_frame(_activeInputContext, -1, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                    if (seekResult < 0)
                        Logger.Log($"FLASHBACK_EXPORT_SEEK_WARN code={seekResult} target_ms={(long)inPoint.TotalMilliseconds}");
                }

                var lastProgressHeartbeatTick = 0L;
                var segmentVideoFrameDurUs = 33333L;
                if (useSegmentTimeline &&
                    videoStreamIndex >= 0 &&
                    videoStreamIndex < currentStreamCount)
                {
                    segmentVideoFrameDurUs = ResolveFrameDurationUs(_activeInputContext->streams[videoStreamIndex]);
                }
                var segmentPacketState = CreateSegmentPacketWriteState(
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
                                segments.Count,
                                totalEstimatedBytes > 0
                                    ? 100.0 * bytesProcessed / totalEstimatedBytes
                                    : 100.0 * segIdx / segments.Count),
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

                // Update cross-segment offset: next segment's PTS starts after this segment's max + one frame
                if (segmentPacketState.MaxPtsUs > outputPtsOffsetUs)
                {
                    var videoStream = videoStreamIndex >= 0 ? _activeInputContext->streams[videoStreamIndex] : null;
                    long frameDurUs = ResolveFrameDurationUs(videoStream);
                    outputPtsOffsetUs = segmentPacketState.MaxPtsUs + frameDurUs;
                }

                // Track bytes for progress
                try { if (File.Exists(segPath)) bytesProcessed = AddNonNegativeSaturated(bytesProcessed, new FileInfo(segPath).Length); }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_EXPORT_PROGRESS_UPDATE_WARN path='{segPath}' type={ex.GetType().Name} msg='{ex.Message}'");
                }

                // Close this segment's input
                CloseActiveInput();

                ReportProgress(
                    progress,
                    new ExportProgress(
                        segIdx + 1,
                        segments.Count,
                        totalEstimatedBytes > 0 ? 100.0 * bytesProcessed / totalEstimatedBytes : 100.0 * (segIdx + 1) / segments.Count),
                    "segment_complete");

                Logger.Log($"FLASHBACK_EXPORT_SEGMENT_OK seg={segIdx}/{segments.Count} path='{Path.GetFileName(segPath)}' packets={totalPackets} seg_max_pts_us={segmentPacketState.MaxPtsUs} seg_abs_max_pts_us={segmentPacketState.AbsMaxPtsUs} local_in_us={segmentInOffsetUs} local_out_us={segmentOutOffsetUs} bases_discovered={segmentPacketState.AllBasesDiscovered}");

                // If outPoint was hit, stop processing more segments
                // Use absolute PTS (not rebased) since outPtsLimitUs is in absolute encoder time
                if (outPtsLimitUs < long.MaxValue && segmentPacketState.AbsMaxPtsUs >= outPtsLimitUs)
                    break;
            }
        }
        finally
        {
            var packetToFree = packet;
            ffmpeg.av_packet_free(&packetToFree);
        }

        if (requestedSegmentSkips.TryCreateFailureMessage(out var skippedSegmentFailureMessage))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{skippedSegmentFailureMessage}'");
            return new SegmentPacketWriteResult(FinalizeResult.Failure(outputPath, skippedSegmentFailureMessage), 0);
        }

        if (totalPackets == 0)
        {
            const string message = "Flashback export failed: no packets were written from any segment.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SegmentPacketWriteResult(FinalizeResult.Failure(outputPath, message), 0);
        }

        return new SegmentPacketWriteResult(null, totalPackets);
    }

    private readonly record struct SegmentPacketWriteResult(FinalizeResult? Failure, long TotalPackets);
}

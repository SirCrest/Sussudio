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

                WriteSegmentPacketReadLoop(
                    segIdx,
                    segments.Count,
                    streamCount,
                    videoStreamIndex,
                    currentStreamCount,
                    streamMap,
                    lastDtsPerStream,
                    totalEstimatedBytes,
                    bytesProcessed,
                    outputPtsOffsetUs,
                    useSegmentTimeline,
                    segmentInOffsetUs,
                    segmentOutOffsetUs,
                    packet,
                    progress,
                    ct,
                    ref totalPackets,
                    out var segmentPacketState);

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

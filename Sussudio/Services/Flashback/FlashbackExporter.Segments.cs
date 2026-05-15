using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private FinalizeResult ExportSegmentsCore(
        IReadOnlyList<FlashbackExportSegment> segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        bool allowOverwrite,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return CreateCancelledExportResult(outputPath);
        }

        if (!TryValidateSegmentExportInputs(
                segments,
                inPoint,
                outPoint,
                outputPath,
                out var normalizedOutputPath,
                out var validationFailure))
        {
            return validationFailure!;
        }
        outputPath = normalizedOutputPath;

        var tmpPath = outputPath + ".tmp";

        if (!TryEstimateSegmentExportReadableBytes(
                segments,
                outputPath,
                out var totalEstimatedBytes,
                out var estimateFailure))
        {
            return estimateFailure!;
        }

        if (!TryWaitForExportLock(outputPath, ct, out var cancellationResult))
        {
            return cancellationResult;
        }

        try
        {
        _activeTempPath = tmpPath;

        try
        {
            if (!TryPrepareTempOutputFile(tmpPath, outputPath, out var tempOutputFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{tempOutputFailure}'");
                return FinalizeResult.Failure(outputPath, tempOutputFailure);
            }

            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_START segments={segments.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");
            ReportProgress(progress, new ExportProgress(0, segments.Count, 0), "segments_start");

            var usTimeBase = new AVRational { num = 1, den = 1_000_000 };
            var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);

            // Output state — initialized from first segment
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
                return FinalizeResult.Failure(outputPath, templateFailure);
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
                    var useSegmentTimeline = segment.StartPts.HasValue;
                    var segmentInOffsetUs = useSegmentTimeline
                        ? ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value))
                        : 0;
                    var segmentOutDelta = useSegmentTimeline
                        ? SaturatingSubtract(
                            (segment.EndPts.HasValue && segment.EndPts.Value < outPoint) ? segment.EndPts.Value : outPoint,
                            segment.StartPts!.Value)
                        : TimeSpan.Zero;
                    var segmentOutOffsetUs = useSegmentTimeline
                        ? ToMicrosecondsSaturated(segmentOutDelta)
                        : outPtsLimitUs;
                    if (useSegmentTimeline && segmentOutDelta <= TimeSpan.Zero)
                    {
                        continue;
                    }

                    if (!File.Exists(segPath))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='not_found'");
                        requestedSegmentSkips.Track(segment, "not_found");
                        continue;
                    }

                    // Open this segment
                    OpenInput(segPath);
                    ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
                    if (!TryGetInputStreamCount(_activeInputContext, "segment_export", out var currentStreamCount, out var streamCountFailure))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
                        requestedSegmentSkips.Track(segment, "invalid_stream_count");
                        CloseActiveInput();
                        continue;
                    }

                    // Validate that this segment's stream layout matches the selected template.
                    // Mismatched layouts (e.g. microphone toggled mid-capture) would cause
                    // packet->stream_index to map incorrectly, producing corrupt output.
                    var segNbStreams = currentStreamCount;
                    if (segNbStreams != streamCount)
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='stream_count_mismatch' expected={streamCount} actual={segNbStreams}");
                        requestedSegmentSkips.Track(segment, "stream_count_mismatch");
                        CloseActiveInput();
                        continue;
                    }

                    var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(
                        _activeInputContext,
                        _activeOutputContext,
                        streamMap,
                        segNbStreams);
                    if (streamLayoutMismatch != null)
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
                        requestedSegmentSkips.Track(segment, "stream_layout_mismatch");
                        CloseActiveInput();
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

                    // Per-segment timestamp discovery (two-phase like single-file export)
                    var segTimestampBasesUs = new long[streamCount];
                    var segHasTimestampBase = new bool[streamCount];
                    long? segMinBaseUs = null;
                    var segBufferedPackets = new List<IntPtr>();
                    var segBufferedStreamIndices = new List<int>();
                    var segAllBasesDiscovered = false;
                    var lastProgressHeartbeatTick = 0L;
                    long segMaxPtsUs = 0; // track highest rebased PTS in this segment for offset calculation
                    long segAbsMaxPtsUs = 0; // track highest absolute PTS for outPoint check
                    long segmentVideoTimestampRepairUs = 0;
                    var segmentVideoPacketsSeen = 0;
                    var segmentVideoFrameDurUs = 33333L;
                    if (useSegmentTimeline &&
                        videoStreamIndex >= 0 &&
                        videoStreamIndex < currentStreamCount)
                    {
                        segmentVideoFrameDurUs = ResolveFrameDurationUs(_activeInputContext->streams[videoStreamIndex]);
                    }

                    // Flush segment-buffered packets (Phase 1 → Phase 2 transition or EOF rescue).
                    // Captures per-iteration locals via closure so both the mid-loop trigger and
                    // the EOF rescue path go through the same code.
                    int FlushSegmentBufferedPackets(out bool stopFlushing)
                    {
                        int written = 0;
                        stopFlushing = false;
                        try
                        {
                            for (int bi = 0; bi < segBufferedPackets.Count; bi++)
                            {
                                var buffPkt = (AVPacket*)segBufferedPackets[bi];
                                var si = segBufferedStreamIndices[bi];
                                var oi = streamMap[si];
                                var outStr = _activeOutputContext->streams[oi];

                                // Check outPoint against absolute PTS BEFORE remapping
                                // At this point buffPkt->pts is in outStr->time_base but still absolute encoder PTS
                                if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                                {
                                    var absPtsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                    var comparePtsUs = useSegmentTimeline
                                        ? absPtsUs - segMinBaseUs!.Value
                                        : absPtsUs;
                                    if (si == videoStreamIndex && absPtsUs > segAbsMaxPtsUs)
                                        segAbsMaxPtsUs = absPtsUs;
                                    if (useSegmentTimeline && comparePtsUs < segmentInOffsetUs)
                                    {
                                        ffmpeg.av_packet_free(&buffPkt);
                                        segBufferedPackets[bi] = IntPtr.Zero;
                                        continue;
                                    }

                                    if (segmentOutOffsetUs < long.MaxValue && comparePtsUs > segmentOutOffsetUs)
                                    {
                                        ffmpeg.av_packet_free(&buffPkt);
                                        segBufferedPackets[bi] = IntPtr.Zero;
                                        if (si == videoStreamIndex)
                                            stopFlushing = true;
                                        continue;
                                    }
                                }

                                // Remap: subtract segment base, add cross-segment offset
                                var segBaseTs = ffmpeg.av_rescale_q(segMinBaseUs!.Value, usTimeBase, outStr->time_base);
                                var offsetTs = ffmpeg.av_rescale_q(outputPtsOffsetUs, usTimeBase, outStr->time_base);

                                if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                                {
                                    buffPkt->pts = buffPkt->pts - segBaseTs + offsetTs;
                                    var ptsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                    if (useSegmentTimeline && si == videoStreamIndex)
                                    {
                                        var repairUs = ResolveSegmentBoundaryTimestampRepairUs(
                                            ptsUs,
                                            outputPtsOffsetUs,
                                            segmentVideoFrameDurUs,
                                            segmentVideoPacketsSeen,
                                            segmentVideoTimestampRepairUs);
                                        if (repairUs > 0)
                                        {
                                            segmentVideoTimestampRepairUs += repairUs;
                                            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR seg={segIdx} stream={si} repair_us={repairUs} total_repair_us={segmentVideoTimestampRepairUs}");
                                        }

                                        if (segmentVideoTimestampRepairUs > 0)
                                        {
                                            var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStr->time_base);
                                            buffPkt->pts -= repairTs;
                                            ptsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                        }

                                        segmentVideoPacketsSeen++;
                                    }

                                    // Track max PTS for offset calculation
                                    if (ptsUs > segMaxPtsUs) segMaxPtsUs = ptsUs;
                                }
                                if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                {
                                    buffPkt->dts = buffPkt->dts - segBaseTs + offsetTs;
                                    if (useSegmentTimeline && si == videoStreamIndex && segmentVideoTimestampRepairUs > 0)
                                    {
                                        var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStr->time_base);
                                        buffPkt->dts -= repairTs;
                                    }
                                    if (oi < lastDtsPerStream.Length && lastDtsPerStream[oi] != long.MinValue && buffPkt->dts <= lastDtsPerStream[oi])
                                        buffPkt->dts = lastDtsPerStream[oi] + 1;
                                }
                                if (oi < lastDtsPerStream.Length && buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                    lastDtsPerStream[oi] = buffPkt->dts;

                                NormalizePacketTimestampsBeforeWrite(buffPkt);
                                buffPkt->pos = -1;
                                buffPkt->stream_index = oi;
                                ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                                written++;
                                ThrottleExportWriterIfNeeded(written);
                                ffmpeg.av_packet_free(&buffPkt);
                                segBufferedPackets[bi] = IntPtr.Zero;
                            }
                        }
                        finally
                        {
                            FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);
                        }

                        return written;
                    }

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
                            if (!segHasTimestampBase[streamIndex])
                            {
                                if (TryResolveTimestampBase(packet, out var tsBase))
                                {
                                    var baseUs = ffmpeg.av_rescale_q(tsBase, outStream->time_base, usTimeBase);
                                    segTimestampBasesUs[streamIndex] = baseUs;
                                    segHasTimestampBase[streamIndex] = true;
                                    if (segMinBaseUs == null || baseUs < segMinBaseUs.Value)
                                        segMinBaseUs = baseUs;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            // Phase 1: buffer until all bases known
                            const int MaxBufferedPackets = 600;
                            if (!segAllBasesDiscovered)
                            {
                                var clone = ClonePacketOrThrow(packet, "segment_buffer");
                                segBufferedPackets.Add((IntPtr)clone);
                                segBufferedStreamIndices.Add(streamIndex);

                                segAllBasesDiscovered = true;
                                for (int i = 0; i < streamCount; i++)
                                {
                                    if (streamMap[i] >= 0 && !segHasTimestampBase[i]) { segAllBasesDiscovered = false; break; }
                                }
                                if (!segAllBasesDiscovered && segBufferedPackets.Count >= MaxBufferedPackets)
                                {
                                    segMinBaseUs ??= 0; // Silent streams never set a base — default to 0
                                    segAllBasesDiscovered = true;
                                }

                                if (segAllBasesDiscovered)
                                {
                                    totalPackets += FlushSegmentBufferedPackets(out var stopFlushing);
                                    if (stopFlushing)
                                        break;
                                }
                                continue;
                            }

                            // Phase 2: inline write
                            var outStream2 = _activeOutputContext->streams[mappedIndex];

                            // Check outPoint against absolute PTS BEFORE remapping
                            // At this point packet->pts is in outStream2->time_base but still absolute encoder PTS
                            if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                var absPtsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                var comparePtsUs = useSegmentTimeline
                                    ? absPtsUs - segMinBaseUs!.Value
                                    : absPtsUs;
                                if (streamIndex == videoStreamIndex && absPtsUs > segAbsMaxPtsUs)
                                    segAbsMaxPtsUs = absPtsUs;
                                if (useSegmentTimeline && comparePtsUs < segmentInOffsetUs)
                                    continue;
                                if (segmentOutOffsetUs < long.MaxValue && comparePtsUs > segmentOutOffsetUs)
                                {
                                    if (streamIndex == videoStreamIndex)
                                        break;

                                    continue;
                                }
                            }

                            var segBase = ffmpeg.av_rescale_q(segMinBaseUs!.Value, usTimeBase, outStream2->time_base);
                            var offset = ffmpeg.av_rescale_q(outputPtsOffsetUs, usTimeBase, outStream2->time_base);

                            if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                packet->pts = packet->pts - segBase + offset;
                                var ptsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                if (useSegmentTimeline && streamIndex == videoStreamIndex)
                                {
                                    var repairUs = ResolveSegmentBoundaryTimestampRepairUs(
                                        ptsUs,
                                        outputPtsOffsetUs,
                                        segmentVideoFrameDurUs,
                                        segmentVideoPacketsSeen,
                                        segmentVideoTimestampRepairUs);
                                    if (repairUs > 0)
                                    {
                                        segmentVideoTimestampRepairUs += repairUs;
                                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR seg={segIdx} stream={streamIndex} repair_us={repairUs} total_repair_us={segmentVideoTimestampRepairUs}");
                                    }

                                    if (segmentVideoTimestampRepairUs > 0)
                                    {
                                        var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStream2->time_base);
                                        packet->pts -= repairTs;
                                        ptsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                    }

                                    segmentVideoPacketsSeen++;
                                }

                                if (ptsUs > segMaxPtsUs) segMaxPtsUs = ptsUs;
                            }
                            if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                packet->dts = packet->dts - segBase + offset;
                                if (useSegmentTimeline && streamIndex == videoStreamIndex && segmentVideoTimestampRepairUs > 0)
                                {
                                    var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStream2->time_base);
                                    packet->dts -= repairTs;
                                }
                                // Enforce DTS monotonicity — mp4 muxer rejects non-monotonic DTS
                                if (mappedIndex < lastDtsPerStream.Length && lastDtsPerStream[mappedIndex] != long.MinValue && packet->dts <= lastDtsPerStream[mappedIndex])
                                    packet->dts = lastDtsPerStream[mappedIndex] + 1;
                            }
                            if (mappedIndex < lastDtsPerStream.Length && packet->dts != ffmpeg.AV_NOPTS_VALUE)
                                lastDtsPerStream[mappedIndex] = packet->dts;

                            NormalizePacketTimestampsBeforeWrite(packet);
                            packet->pos = -1;
                            packet->stream_index = mappedIndex;
                            ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
                            totalPackets++;
                            ThrottleExportWriterIfNeeded(totalPackets);
                        }
                        finally
                        {
                            ffmpeg.av_packet_unref(packet);
                        }
                    }

                    // EOF: if Phase 1 never completed (some configured stream — typically a
                    // silent mic — never produced packets and the buffer never reached the
                    // 600-packet cap), flush whatever we have using a fallback base of 0.
                    // Without this, every video packet in a short segment would be silently
                    // discarded by the FreeBufferedPackets path that used to live here.
                    if (!segAllBasesDiscovered && segBufferedPackets.Count > 0)
                    {
                        segMinBaseUs ??= 0;
                        segAllBasesDiscovered = true;
                        var discoveredCount = 0;
                        for (var i = 0; i < streamCount; i++) { if (segHasTimestampBase[i]) discoveredCount++; }
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx} buffered={segBufferedPackets.Count} streams_discovered={discoveredCount}/{streamCount}");
                        totalPackets += FlushSegmentBufferedPackets(out _);
                    }
                    else
                    {
                        // Either Phase 1 completed inline (nothing to flush) or buffer is empty.
                        // FreeBufferedPackets is a no-op on an empty list; safe in both cases.
                        FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);
                    }

                    // Update cross-segment offset: next segment's PTS starts after this segment's max + one frame
                    if (segMaxPtsUs > outputPtsOffsetUs)
                    {
                        var videoStream = videoStreamIndex >= 0 ? _activeInputContext->streams[videoStreamIndex] : null;
                        long frameDurUs = ResolveFrameDurationUs(videoStream);
                        outputPtsOffsetUs = segMaxPtsUs + frameDurUs;
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

                    Logger.Log($"FLASHBACK_EXPORT_SEGMENT_OK seg={segIdx}/{segments.Count} path='{Path.GetFileName(segPath)}' packets={totalPackets} seg_max_pts_us={segMaxPtsUs} seg_abs_max_pts_us={segAbsMaxPtsUs} local_in_us={segmentInOffsetUs} local_out_us={segmentOutOffsetUs} bases_discovered={segAllBasesDiscovered}");

                    // If outPoint was hit, stop processing more segments
                    // Use absolute PTS (not rebased) since outPtsLimitUs is in absolute encoder time
                    if (outPtsLimitUs < long.MaxValue && segAbsMaxPtsUs >= outPtsLimitUs)
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
                return FinalizeResult.Failure(outputPath, skippedSegmentFailureMessage);
            }

            if (totalPackets == 0)
            {
                const string message = "Flashback export failed: no packets were written from any segment.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }

            ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), "av_write_trailer");
            CloseOutputIo();

            if (!TryFinalizeTempOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{outputFailure}'");
                return FinalizeResult.Failure(outputPath, outputFailure);
            }
            _activeTempPath = null;

            Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_OK output='{outputPath}' segments={segments.Count} packets={totalPackets} bytes={outputBytes}");
            ReportProgress(progress, new ExportProgress(segments.Count, segments.Count, 100.0), "segments_complete");
            return FinalizeResult.Success(outputPath, $"Exported {totalPackets} packets from {segments.Count} segments");
        }
        catch (OperationCanceledException)
        {
            const string message = "Flashback export cancelled.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }
        catch (Exception ex)
        {
            var message = $"Flashback export failed: {ex.Message}";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }
        finally
        {
            CleanupNativeState();
            DeleteTempFileIfPresent(tmpPath);
            _activeTempPath = null;
        }
        }
        finally
        {
            ReleaseExportLockBestEffort("segment_export");
        }
    }
}

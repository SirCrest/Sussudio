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
    private struct RequestedSegmentSkipTracker
    {
        private readonly TimeSpan _inPoint;
        private readonly TimeSpan _outPoint;
        private int _count;
        private string? _firstReason;

        public RequestedSegmentSkipTracker(TimeSpan inPoint, TimeSpan outPoint)
        {
            _inPoint = inPoint;
            _outPoint = outPoint;
            _count = 0;
            _firstReason = null;
        }

        public void Track(FlashbackExportSegment segment, string reason)
        {
            if (!SegmentOverlapsExportRange(segment, _inPoint, _outPoint))
            {
                return;
            }

            _count++;
            _firstReason ??= reason;
        }

        public bool TryCreateFailureMessage(out string message)
        {
            if (_count <= 0)
            {
                message = string.Empty;
                return false;
            }

            message = $"Flashback export failed: {_count} requested segment(s) were skipped; first reason: {_firstReason}.";
            return true;
        }
    }

    private readonly record struct SegmentExportWindow(
        bool UseSegmentTimeline,
        long SegmentInOffsetUs,
        long SegmentOutOffsetUs,
        bool SkipBecauseEmpty);

    private static SegmentExportWindow ProjectSegmentExportWindow(
        FlashbackExportSegment segment,
        TimeSpan inPoint,
        TimeSpan outPoint,
        long outPtsLimitUs)
    {
        var useSegmentTimeline = segment.StartPts.HasValue;
        if (!useSegmentTimeline)
        {
            return new SegmentExportWindow(
                UseSegmentTimeline: false,
                SegmentInOffsetUs: 0,
                SegmentOutOffsetUs: outPtsLimitUs,
                SkipBecauseEmpty: false);
        }

        var segmentInOffsetUs = ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value));
        var segmentOutDelta = SaturatingSubtract(
            (segment.EndPts.HasValue && segment.EndPts.Value < outPoint) ? segment.EndPts.Value : outPoint,
            segment.StartPts!.Value);
        var segmentOutOffsetUs = ToMicrosecondsSaturated(segmentOutDelta);

        return new SegmentExportWindow(
            UseSegmentTimeline: true,
            SegmentInOffsetUs: segmentInOffsetUs,
            SegmentOutOffsetUs: segmentOutOffsetUs,
            SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero);
    }

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

// Per-segment packet read loop: read frames from the active input, discover
// timestamp bases, buffer early packets, and write rebased packets.
internal sealed unsafe partial class FlashbackExporter
{
    private static readonly AVRational SegmentPacketUsTimeBase = new() { num = 1, den = 1_000_000 };

    private static SegmentPacketWriteState CreateSegmentPacketWriteState(
        int segmentIndex,
        int streamCount,
        bool useSegmentTimeline,
        long segmentInOffsetUs,
        long segmentOutOffsetUs,
        long outputPtsOffsetUs,
        int videoStreamIndex,
        long videoFrameDurationUs)
        => new(
            segmentIndex,
            useSegmentTimeline,
            segmentInOffsetUs,
            segmentOutOffsetUs,
            outputPtsOffsetUs,
            videoStreamIndex,
            videoFrameDurationUs,
            new long[streamCount],
            new bool[streamCount],
            new List<IntPtr>(),
            new List<int>());

    private static bool TryRecordSegmentTimestampBase(
        ref SegmentPacketWriteState state,
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream)
    {
        if (!TryResolveTimestampBase(packet, out var timestampBase))
        {
            return false;
        }

        var baseUs = ffmpeg.av_rescale_q(timestampBase, outputStream->time_base, SegmentPacketUsTimeBase);
        state.TimestampBasesUs[streamIndex] = baseUs;
        state.HasTimestampBase[streamIndex] = true;
        if (state.MinBaseUs == null || baseUs < state.MinBaseUs.Value)
        {
            state.MinBaseUs = baseUs;
        }

        return true;
    }

    private static bool HasDiscoveredAllMappedSegmentBases(
        in SegmentPacketWriteState state,
        int streamCount,
        int[] streamMap)
    {
        for (var i = 0; i < streamCount; i++)
        {
            if (streamMap[i] >= 0 && !state.HasTimestampBase[i])
            {
                return false;
            }
        }

        return true;
    }

    private int FlushSegmentBufferedPackets(
        ref SegmentPacketWriteState state,
        int[] streamMap,
        long[] lastDtsPerOutputStream,
        out bool stopFlushing)
    {
        var written = 0;
        stopFlushing = false;
        try
        {
            for (var bufferedIndex = 0; bufferedIndex < state.BufferedPackets.Count; bufferedIndex++)
            {
                var bufferedPacket = (AVPacket*)state.BufferedPackets[bufferedIndex];
                var sourceStreamIndex = state.BufferedStreamIndices[bufferedIndex];
                var outputStreamIndex = streamMap[sourceStreamIndex];
                var outputStream = _activeOutputContext->streams[outputStreamIndex];
                var writeOutcome = WriteRebasedSegmentPacket(
                    ref state,
                    bufferedPacket,
                    sourceStreamIndex,
                    outputStreamIndex,
                    outputStream,
                    lastDtsPerOutputStream);
                if (writeOutcome == SegmentPacketWriteOutcome.StopAtVideoOutPoint)
                {
                    stopFlushing = true;
                }
                else if (writeOutcome == SegmentPacketWriteOutcome.Written)
                {
                    written++;
                    ThrottleExportWriterIfNeeded(written);
                }

                ffmpeg.av_packet_free(&bufferedPacket);
                state.BufferedPackets[bufferedIndex] = IntPtr.Zero;
            }
        }
        finally
        {
            FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);
        }

        return written;
    }

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

    private enum SegmentPacketWriteOutcome
    {
        Skipped,
        Written,
        StopAtVideoOutPoint,
    }

    private struct SegmentPacketWriteState
    {
        public SegmentPacketWriteState(
            int segmentIndex,
            bool useSegmentTimeline,
            long segmentInOffsetUs,
            long segmentOutOffsetUs,
            long outputPtsOffsetUs,
            int videoStreamIndex,
            long videoFrameDurationUs,
            long[] timestampBasesUs,
            bool[] hasTimestampBase,
            List<IntPtr> bufferedPackets,
            List<int> bufferedStreamIndices)
        {
            SegmentIndex = segmentIndex;
            UseSegmentTimeline = useSegmentTimeline;
            SegmentInOffsetUs = segmentInOffsetUs;
            SegmentOutOffsetUs = segmentOutOffsetUs;
            OutputPtsOffsetUs = outputPtsOffsetUs;
            VideoStreamIndex = videoStreamIndex;
            VideoFrameDurationUs = videoFrameDurationUs;
            TimestampBasesUs = timestampBasesUs;
            HasTimestampBase = hasTimestampBase;
            BufferedPackets = bufferedPackets;
            BufferedStreamIndices = bufferedStreamIndices;
        }

        public int SegmentIndex { get; }
        public bool UseSegmentTimeline { get; }
        public long SegmentInOffsetUs { get; }
        public long SegmentOutOffsetUs { get; }
        public long OutputPtsOffsetUs { get; }
        public int VideoStreamIndex { get; }
        public long VideoFrameDurationUs { get; }
        public long[] TimestampBasesUs { get; }
        public bool[] HasTimestampBase { get; }
        public List<IntPtr> BufferedPackets { get; }
        public List<int> BufferedStreamIndices { get; }
        public long? MinBaseUs { get; set; }
        public bool AllBasesDiscovered { get; set; }
        public long MaxPtsUs { get; set; }
        public long AbsMaxPtsUs { get; set; }
        public long VideoTimestampRepairUs { get; set; }
        public int VideoPacketsSeen { get; set; }
    }
}

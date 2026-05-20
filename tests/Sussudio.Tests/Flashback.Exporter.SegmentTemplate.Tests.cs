using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream()
    {
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.StreamTemplates.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentInputPreflight.cs")
            .Replace("\r\n", "\n");

        var templateSelectionBlock = ExtractTextBetween(
            sourceText,
            "private bool TryInitializeSegmentOutputTemplate(",
            "    private static bool TryValidateSegmentExportInputs");
        var incompleteVideoParamsBlock = ExtractTextBetween(
            sourceText,
            "var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];",
            "CreateOutputContext(tmpPath, fastStart);");

        AssertDoesNotContain(templateSelectionBlock, "TrackSkippedRequestedSegment(segment, \"video_stream_missing\");");
        AssertDoesNotContain(templateSelectionBlock, "TrackSkippedRequestedSegment(segment, \"video_params_incomplete\");");
        AssertContains(templateSelectionBlock, "var candidateVideoStreamIndex = FindVideoStreamIndex(_activeInputContext);");
        AssertContains(templateSelectionBlock, "LogInputStreams(_activeInputContext, candidateStreamCount);");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_stream_missing'");
        AssertContains(templateSelectionBlock, "no usable video stream was found in any segment");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(incompleteVideoParamsBlock, "var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];");
        AssertContains(incompleteVideoParamsBlock, "var videoHasValidParams = videoWidth > 0 && videoHeight > 0;");
        AssertContains(incompleteVideoParamsBlock, "no segment had complete video parameters");
        AssertContains(segmentInputPreflightText, "var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(");
        AssertContains(segmentInputPreflightText, "reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
        AssertContains(streamTemplatesText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(streamTemplatesText, "inputCodec->codec_type != templateCodec->codec_type");
        AssertContains(streamTemplatesText, "inputCodec->codec_id != templateCodec->codec_id");
        AssertContains(streamTemplatesText, "private static bool VideoDimensionsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(streamTemplatesText, "return !inputHasCompleteDimensions && templateHasCompleteDimensions;");
        AssertContains(streamTemplatesText, "inputCodec->sample_rate != templateCodec->sample_rate");
        AssertContains(streamTemplatesText, "inputCodec->ch_layout.nb_channels != templateCodec->ch_layout.nb_channels");
        AssertContains(streamTemplatesText, "inputCodec->format != templateCodec->format");
        AssertDoesNotContain(streamsText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertDoesNotContain(streamsText, "private static bool VideoDimensionsMatchOrCanUseTemplate(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped()
    {
        var segmentExportCore = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var skipTrackingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentSkipTracking.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentInputPreflight.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentExportCore, "WriteSegmentPacketsToActiveOutput(");
        AssertDoesNotContain(segmentExportCore, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentPacketWritingText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertDoesNotContain(segmentPacketWritingText, "void TrackSkippedRequestedSegment(FlashbackExportSegment segment, string reason)");
        AssertContains(skipTrackingText, "private struct RequestedSegmentSkipTracker");
        AssertContains(skipTrackingText, "public void Track(FlashbackExportSegment segment, string reason)");
        AssertContains(skipTrackingText, "SegmentOverlapsExportRange(segment, _inPoint, _outPoint)");
        AssertContains(skipTrackingText, "public bool TryCreateFailureMessage(out string message)");
        AssertContains(segmentPacketWritingText, "ref requestedSegmentSkips,");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"not_found\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"invalid_stream_count\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_count_mismatch\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_layout_mismatch\");");
        AssertDoesNotContain(segmentPacketWritingText, "requestedSegmentSkips.Track(segment, \"video_stream_missing\");");
        AssertDoesNotContain(segmentPacketWritingText, "requestedSegmentSkips.Track(segment, \"video_params_incomplete\");");
        AssertContains(segmentPacketWritingText, "if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))");
        AssertOccursBefore(segmentPacketWritingText, "if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))", "for (var segIdx = 0; segIdx < segments.Count; segIdx++)");
        AssertContains(skipTrackingText, "requested segment(s) were skipped");
        AssertOccursBefore(segmentPacketWritingText, "if (requestedSegmentSkips.TryCreateFailureMessage(out var skippedSegmentFailureMessage))", "if (totalPackets == 0)");

        return Task.CompletedTask;
    }
}

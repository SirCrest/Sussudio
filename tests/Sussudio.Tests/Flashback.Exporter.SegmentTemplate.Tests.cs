using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream()
    {
        var sourceText = ReadFlashbackExporterSource();

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
        AssertContains(sourceText, "var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(");
        AssertContains(sourceText, "reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
        AssertContains(sourceText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(sourceText, "inputCodec->codec_type != templateCodec->codec_type");
        AssertContains(sourceText, "inputCodec->codec_id != templateCodec->codec_id");
        AssertContains(sourceText, "private static bool VideoDimensionsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(sourceText, "return !inputHasCompleteDimensions && templateHasCompleteDimensions;");
        AssertContains(sourceText, "inputCodec->sample_rate != templateCodec->sample_rate");
        AssertContains(sourceText, "inputCodec->ch_layout.nb_channels != templateCodec->ch_layout.nb_channels");
        AssertContains(sourceText, "inputCodec->format != templateCodec->format");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped()
    {
        var sourceText = ReadFlashbackExporterSource();
        var segmentExportCore = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportSegmentsCore",
            "    private static long ResolveFrameDurationUs");

        AssertContains(segmentExportCore, "var skippedRequestedSegmentCount = 0;");
        AssertContains(segmentExportCore, "void TrackSkippedRequestedSegment(FlashbackExportSegment segment, string reason)");
        AssertContains(segmentExportCore, "SegmentOverlapsExportRange(segment, inPoint, outPoint)");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"not_found\");");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"invalid_stream_count\");");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"stream_count_mismatch\");");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"stream_layout_mismatch\");");
        AssertDoesNotContain(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"video_stream_missing\");");
        AssertDoesNotContain(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"video_params_incomplete\");");
        AssertContains(segmentExportCore, "if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))");
        AssertOccursBefore(segmentExportCore, "if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))", "for (var segIdx = 0; segIdx < segments.Count; segIdx++)");
        AssertContains(segmentExportCore, "requested segment(s) were skipped");
        AssertOccursBefore(segmentExportCore, "if (skippedRequestedSegmentCount > 0)", "if (totalPackets == 0)");

        return Task.CompletedTask;
    }
}

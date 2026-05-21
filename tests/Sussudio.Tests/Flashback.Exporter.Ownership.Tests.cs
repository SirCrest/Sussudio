using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var requestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Requests.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifetime.cs")
            .Replace("\r\n", "\n");
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketWriting.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketWriteStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketWriteState.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketRebasingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketRebasing.cs")
            .Replace("\r\n", "\n");
        var segmentsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWriteStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriteState.cs")
            .Replace("\r\n", "\n");
        var segmentPacketRebasingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketRebasing.cs")
            .Replace("\r\n", "\n");
        var segmentRangeProjectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentRangeProjection.cs")
            .Replace("\r\n", "\n");
        var segmentSkipTrackingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentSkipTracking.cs")
            .Replace("\r\n", "\n");
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentInputPreflight.cs")
            .Replace("\r\n", "\n");
        var segmentValidationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentValidation.cs")
            .Replace("\r\n", "\n");
        var progressText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Progress.cs")
            .Replace("\r\n", "\n");
        var writerPacingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.WriterPacing.cs")
            .Replace("\r\n", "\n");
        var tempFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.TempFiles.cs")
            .Replace("\r\n", "\n");
        var outputFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs")
            .Replace("\r\n", "\n");
        var exportLockText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.ExportLock.cs")
            .Replace("\r\n", "\n");
        var outputValidationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.OutputValidation.cs")
            .Replace("\r\n", "\n");
        var pathValidationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PathValidation.cs")
            .Replace("\r\n", "\n");
        var segmentSelectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentSelection.cs")
            .Replace("\r\n", "\n");
        var nativeStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.NativeState.cs")
            .Replace("\r\n", "\n");
        var cancellationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Cancellation.cs")
            .Replace("\r\n", "\n");
        var libAvErrorsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.LibAvErrors.cs")
            .Replace("\r\n", "\n");
        var packetTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs")
            .Replace("\r\n", "\n");
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketBuffers.cs")
            .Replace("\r\n", "\n");
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.StreamTemplates.cs")
            .Replace("\r\n", "\n");
        var timeMathText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.TimeMath.cs")
            .Replace("\r\n", "\n");

        AssertContains(requestsText, "public Task<FinalizeResult> ExportAsync(");
        AssertContains(requestsText, "request.SegmentPaths.Select(path => new FlashbackExportSegment");
        AssertContains(lifetimeText, "public void Dispose()");
        AssertContains(lifetimeText, "FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
        AssertContains(singleFileText, "private FinalizeResult ExportCore(");
        AssertContains(singleFileText, "WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFileText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(singleFilePacketWritingText, "private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFilePacketWritingText, "private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);");
        AssertContains(singleFilePacketWritingText, "WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketWritingText, "LogTimestampBaseDrift(packetState.TimestampBasesUs, packetState.HasTimestampBase);");
        AssertContains(singleFilePacketWritingText, "Flashback export failed: no video packets were written.");
        AssertDoesNotContain(singleFilePacketWritingText, "var packet = ffmpeg.av_packet_alloc();");
        AssertDoesNotContain(singleFilePacketWritingText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(singleFilePacketReadLoopText, "private void WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketReadLoopText, "var packet = ffmpeg.av_packet_alloc();");
        AssertContains(singleFilePacketReadLoopText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(singleFilePacketReadLoopText, "FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);");
        AssertDoesNotContain(singleFilePacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertDoesNotContain(singleFilePacketReadLoopText, "private struct SingleFilePacketWriteState");
        AssertContains(singleFilePacketWriteStateText, "private struct SingleFilePacketWriteState");
        AssertContains(singleFilePacketWriteStateText, "private static readonly AVRational SingleFilePacketUsTimeBase");
        AssertContains(singleFilePacketWriteStateText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(singleFilePacketWriteStateText, "private void FlushSingleFileBufferedPacketsAtEof(");
        AssertContains(singleFilePacketRebasingText, "private void WriteSingleFilePacket(");
        AssertContains(singleFilePacketRebasingText, "private static bool PacketPtsExceedsSingleFileOutPoint(");
        AssertContains(singleFilePacketRebasingText, "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), \"av_interleaved_write_frame\");");
        AssertDoesNotContain(singleFileText, "var timestampBasesUs = new long[streamCount];");
        AssertDoesNotContain(singleFileText, "var packet = ffmpeg.av_packet_alloc();");
        AssertDoesNotContain(singleFileText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertDoesNotContain(singleFileText, "LogTimestampBaseDrift(timestampBasesUs, hasTimestampBase);");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "TryValidateSegmentExportInputs(");
        AssertContains(segmentsText, "TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentsText, "WriteSegmentPacketsToActiveOutput(");
        AssertDoesNotContain(segmentsText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertDoesNotContain(segmentsText, "var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);");
        AssertContains(segmentPacketWritingText, "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentPacketWritingText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentPacketWritingText, "var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);");
        AssertContains(segmentPacketWritingText, "WriteSegmentPacketReadLoop(");
        AssertDoesNotContain(segmentPacketWritingText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(segmentPacketReadLoopText, "private void WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(segmentPacketReadLoopText, "ffmpeg.av_packet_unref(packet);");
        AssertContains(segmentPacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");
        AssertContains(segmentPacketReadLoopText, "FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH");
        AssertContains(segmentPacketReadLoopText, "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");
        AssertContains(segmentPacketWriteStateText, "private struct SegmentPacketWriteState");
        AssertContains(segmentPacketWriteStateText, "private int FlushSegmentBufferedPackets(");
        AssertDoesNotContain(segmentPacketWriteStateText, "private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(");
        AssertContains(segmentPacketRebasingText, "private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(");
        AssertContains(segmentPacketRebasingText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(segmentPacketRebasingText, "packet->dts = lastDtsPerOutputStream[outputStreamIndex] + 1;");
        AssertContains(segmentPacketRebasingText, "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), \"av_interleaved_write_frame\");");
        AssertContains(segmentPacketWriteStateText, "private enum SegmentPacketWriteOutcome");
        AssertContains(segmentPacketWriteStateText, "public List<IntPtr> BufferedPackets { get; }");
        AssertContains(segmentPacketWriteStateText, "public long VideoTimestampRepairUs { get; set; }");
        AssertDoesNotContain(segmentsText, "var segmentOutDelta =");
        AssertDoesNotContain(segmentsText, "SaturatingSubtract(\n                            (segment.EndPts.HasValue && segment.EndPts.Value < outPoint) ? segment.EndPts.Value : outPoint,");
        AssertContains(segmentRangeProjectionText, "private readonly record struct SegmentExportWindow(");
        AssertContains(segmentRangeProjectionText, "private static SegmentExportWindow ProjectSegmentExportWindow(");
        AssertContains(segmentRangeProjectionText, "SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero");
        AssertContains(segmentPacketWritingText, "TryOpenSegmentInputForExport(");
        AssertDoesNotContain(segmentsText, "avformat_find_stream_info(_activeInputContext, null)");
        AssertContains(segmentSkipTrackingText, "private struct RequestedSegmentSkipTracker");
        AssertContains(segmentSkipTrackingText, "public void Track(FlashbackExportSegment segment, string reason)");
        AssertContains(segmentSkipTrackingText, "public bool TryCreateFailureMessage(out string message)");
        AssertContains(segmentsText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(segmentInputPreflightText, "private bool TryOpenSegmentInputForExport(");
        AssertContains(segmentInputPreflightText, "ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), \"avformat_find_stream_info\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"not_found\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"invalid_stream_count\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_count_mismatch\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_layout_mismatch\");");
        AssertContains(segmentTemplateText, "private bool TryInitializeSegmentOutputTemplate(");
        AssertContains(segmentTemplateText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertDoesNotContain(segmentsText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(segmentValidationText, "private static bool TryValidateSegmentExportInputs(");
        AssertContains(segmentValidationText, "private static bool TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentValidationText, "private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(segmentValidationText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertDoesNotContain(segmentsText, "FindDuplicateSegmentPathIndex(segments)");
        AssertDoesNotContain(segmentsText, "FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN");
        AssertContains(progressText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertContains(progressText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertDoesNotContain(progressText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(writerPacingText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(writerPacingText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(writerPacingText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(writerPacingText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(writerPacingText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(writerPacingText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(writerPacingText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(writerPacingText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(writerPacingText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(writerPacingText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertDoesNotContain(rootText, "ExportWriterYieldPacketInterval");
        AssertDoesNotContain(rootText, "_adaptiveThrottleSync");
        AssertContains(tempFilesText, "private static void DeleteTempFileIfPresent(string tmpPath)");
        AssertContains(tempFilesText, "private static bool TryPrepareTempOutputFile(string tmpPath, string outputPath, out string failureMessage)");
        AssertContains(tempFilesText, "internal static void CleanupOrphanedTempFiles(string directory)");
        AssertContains(outputFilesText, "private bool TryFinalizeActiveOutputFile(");
        AssertContains(outputFilesText, "ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), \"av_write_trailer\");");
        AssertContains(outputFilesText, "CloseOutputIo();");
        AssertContains(outputFilesText, "TryFinalizeTempOutputFile(tmpPath, outputPath, allowOverwrite, out outputBytes, out failureMessage)");
        AssertContains(outputFilesText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{failureMessage}'\");");
        AssertContains(outputFilesText, "_activeTempPath = null;");
        AssertDoesNotContain(singleFileText, "av_write_trailer(_activeOutputContext)");
        AssertDoesNotContain(singleFileText, "CloseOutputIo();\n\n            if (!TryFinalizeTempOutputFile");
        AssertContains(singleFileText, "if (!TryFinalizeActiveOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))");
        AssertDoesNotContain(segmentsText, "av_write_trailer(_activeOutputContext)");
        AssertDoesNotContain(segmentsText, "CloseOutputIo();\n\n            if (!TryFinalizeTempOutputFile");
        AssertContains(segmentsText, "if (!TryFinalizeActiveOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))");
        AssertDoesNotContain(segmentPacketWritingText, "TryFinalizeActiveOutputFile(");
        AssertContains(exportLockText, "private bool TryWaitForExportLock(string outputPath, CancellationToken ct, out FinalizeResult cancellationResult)");
        AssertContains(exportLockText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(exportLockText, "private void DisposeExportLockBestEffort()");
        AssertContains(cancellationText, "private static FinalizeResult CreateCancelledExportResult(string outputPath)");
        AssertContains(cancellationText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(outputValidationText, "private static long GetFileLengthBestEffort(string path)");
        AssertContains(outputValidationText, "private static bool TryValidateCompletedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(pathValidationText, "private static bool IsSamePath(string? left, string? right)");
        AssertContains(pathValidationText, "private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)");
        AssertContains(segmentSelectionText, "private static bool SegmentOverlapsExportRange(");
        AssertContains(segmentSelectionText, "private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)");
        AssertContains(nativeStateText, "private void CloseActiveInput()");
        AssertContains(nativeStateText, "private void CloseOutputIo()");
        AssertContains(nativeStateText, "private void CleanupNativeState()");
        AssertContains(cancellationText, "private CancellationTokenSource CreateExportCancellationSource(CancellationToken ct)");
        AssertContains(cancellationText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(cancellationText, "private void ClearDisposeCtsReference(CancellationTokenSource? disposeCts)");
        AssertContains(cancellationText, "private void EnsureNotDisposed()");
        AssertContains(libAvErrorsText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(libAvErrorsText, "private static string GetErrorString(int errorCode)");
        AssertContains(packetTimingText, "private static long ResolveFrameDurationUs(AVStream* videoStream)");
        AssertContains(packetTimingText, "private static long ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(packetTimingText, "private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet)");
        AssertDoesNotContain(packetTimingText, "private long FlushBufferedPackets(");
        AssertDoesNotContain(packetTimingText, "private static void FreeBufferedPackets(");
        AssertDoesNotContain(packetTimingText, "private static AVPacket* ClonePacketOrThrow(");
        AssertContains(packetBuffersText, "private long FlushBufferedPackets(");
        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertContains(streamsText, "private void OpenInput(string inputPath)");
        AssertContains(streamsText, "private void CreateOutputContext(string tmpPath, bool fastStart)");
        AssertContains(streamsText, "private static void OpenOutputIoAndWriteHeader(AVFormatContext* outputContext, string tmpPath, bool fastStart)");
        AssertDoesNotContain(streamsText, "private static int[] CopyTemplateStreams(");
        AssertDoesNotContain(streamsText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(streamTemplatesText, "private static int[] CopyTemplateStreams(");
        AssertContains(streamTemplatesText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(streamTemplatesText, "private static bool VideoDimensionsMatchOrCanUseTemplate(");
        AssertContains(timeMathText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(timeMathText, "private static long ToAvTimeBaseTimestampOrMax(TimeSpan value)");
        AssertContains(timeMathText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(timeMathText, "private static long ToMicrosecondsSaturated(TimeSpan value)");
        AssertContains(timeMathText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> ExportAsync(");
        AssertDoesNotContain(rootText, "public void Dispose()");
        AssertDoesNotContain(rootText, "private FinalizeResult ExportCore(");
        AssertDoesNotContain(rootText, "private FinalizeResult ExportSegmentsCore(");

        return Task.CompletedTask;
    }
}

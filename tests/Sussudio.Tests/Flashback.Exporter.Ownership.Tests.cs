using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials()
    {
        var requestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketWritingText = singleFilePacketReadLoopText;
        var singleFilePacketWriteStateText = singleFilePacketReadLoopText;
        var singleFilePacketRebasingText = singleFilePacketReadLoopText;
        var segmentsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWriteStateText = segmentPacketReadLoopText;
        var segmentPacketRebasingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketRebasing.cs")
            .Replace("\r\n", "\n");
        var segmentRangeProjectionText = segmentPacketWritingText;
        var segmentSkipTrackingText = segmentPacketWritingText;
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;
        var segmentValidationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentValidation.cs")
            .Replace("\r\n", "\n");
        var runtimePolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.RuntimePolicy.cs")
            .Replace("\r\n", "\n");
        var outputFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Validation.cs")
            .Replace("\r\n", "\n");
        var libAvErrorsText = lifecycleText;
        var packetTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs")
            .Replace("\r\n", "\n");
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketBuffers.cs")
            .Replace("\r\n", "\n");
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.StreamTemplates.cs")
            .Replace("\r\n", "\n");
        var timeMathText = packetTimingText;

        AssertContains(requestsText, "public Task<FinalizeResult> ExportAsync(");
        AssertContains(requestsText, "request.SegmentPaths.Select(path => new FlashbackExportSegment");
        AssertContains(lifecycleText, "internal sealed unsafe partial class FlashbackExporter : IDisposable");
        AssertContains(lifecycleText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(lifecycleText, "private readonly SemaphoreSlim _exportLock = new(1, 1);");
        AssertContains(lifecycleText, "private AVFormatContext* _activeInputContext;");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
        AssertContains(singleFileText, "private FinalizeResult ExportCore(");
        AssertContains(singleFileText, "WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFileText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(singleFilePacketWritingText, "private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFilePacketWritingText, "private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);");
        AssertContains(singleFilePacketWritingText, "WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketWritingText, "LogTimestampBaseDrift(packetState.TimestampBasesUs, packetState.HasTimestampBase);");
        AssertContains(singleFilePacketWritingText, "Flashback export failed: no video packets were written.");
        AssertContains(singleFilePacketReadLoopText, "private void WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketReadLoopText, "var packet = ffmpeg.av_packet_alloc();");
        AssertContains(singleFilePacketReadLoopText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(singleFilePacketReadLoopText, "FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);");
        AssertContains(singleFilePacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(singleFilePacketReadLoopText, "private struct SingleFilePacketWriteState");
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
        AssertDoesNotContain(singleFileText, "private void WriteSingleFilePacket(");
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
        AssertContains(runtimePolicyText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertContains(runtimePolicyText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(runtimePolicyText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(runtimePolicyText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(runtimePolicyText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(runtimePolicyText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(runtimePolicyText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(runtimePolicyText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(runtimePolicyText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(runtimePolicyText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(runtimePolicyText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(runtimePolicyText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertDoesNotContain(lifecycleText, "ExportWriterYieldPacketInterval");
        AssertDoesNotContain(lifecycleText, "_adaptiveThrottleSync");
        AssertContains(outputFilesText, "private static void DeleteTempFileIfPresent(string tmpPath)");
        AssertContains(outputFilesText, "private static bool TryPrepareTempOutputFile(string tmpPath, string outputPath, out string failureMessage)");
        AssertContains(outputFilesText, "internal static void CleanupOrphanedTempFiles(string directory)");
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
        AssertContains(lifecycleText, "private bool TryWaitForExportLock(string outputPath, CancellationToken ct, out FinalizeResult cancellationResult)");
        AssertContains(lifecycleText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(lifecycleText, "private void DisposeExportLockBestEffort()");
        AssertContains(lifecycleText, "private static FinalizeResult CreateCancelledExportResult(string outputPath)");
        AssertContains(lifecycleText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(validationText, "private static long GetFileLengthBestEffort(string path)");
        AssertContains(validationText, "private static bool TryValidateCompletedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(validationText, "private static bool IsSamePath(string? left, string? right)");
        AssertContains(validationText, "private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)");
        AssertContains(validationText, "private static bool SegmentOverlapsExportRange(");
        AssertContains(validationText, "private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)");
        AssertContains(lifecycleText, "private void CloseActiveInput()");
        AssertContains(lifecycleText, "private void CloseOutputIo()");
        AssertContains(lifecycleText, "private void CleanupNativeState()");
        AssertContains(lifecycleText, "private CancellationTokenSource CreateExportCancellationSource(CancellationToken ct)");
        AssertContains(lifecycleText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(lifecycleText, "private void ClearDisposeCtsReference(CancellationTokenSource? disposeCts)");
        AssertContains(lifecycleText, "private void EnsureNotDisposed()");
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
        AssertDoesNotContain(lifecycleText, "public Task<FinalizeResult> ExportAsync(");
        AssertDoesNotContain(lifecycleText, "private FinalizeResult ExportCore(");
        AssertDoesNotContain(lifecycleText, "private FinalizeResult ExportSegmentsCore(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.cs")),
            "FlashbackExporter state-only partial folded into FlashbackExporter.Lifecycle.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.Lifetime.cs",
            "FlashbackExporter.ExportLock.cs",
            "FlashbackExporter.NativeState.cs",
            "FlashbackExporter.Cancellation.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.Lifecycle.cs");
        }
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.OutputValidation.cs",
            "FlashbackExporter.PathValidation.cs",
            "FlashbackExporter.SegmentSelection.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.Validation.cs");
        }
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.Progress.cs",
            "FlashbackExporter.WriterPacing.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.RuntimePolicy.cs");
        }

        return Task.CompletedTask;
    }
}

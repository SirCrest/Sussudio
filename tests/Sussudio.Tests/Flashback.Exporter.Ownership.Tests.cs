using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials()
    {
        var requestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs")
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
        var segmentPacketRebasingText = segmentPacketReadLoopText;
        var segmentRangeProjectionText = segmentPacketWritingText;
        var segmentSkipTrackingText = segmentPacketWritingText;
        var segmentTemplateText = segmentsText;
        var segmentInputPreflightText = segmentTemplateText;
        var executionPolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var singleFileText = executionPolicyText;
        var outputFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Validation.cs")
            .Replace("\r\n", "\n");
        var segmentValidationText = validationText;
        var libAvErrorsText = lifecycleText;
        var packetTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs")
            .Replace("\r\n", "\n");
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var timeMathText = packetTimingText;
        var packetBuffersText = packetTimingText;

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
        AssertContains(segmentsText, "avformat_find_stream_info(_activeInputContext, null)");
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
        AssertContains(segmentsText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(segmentValidationText, "private static bool TryValidateSegmentExportInputs(");
        AssertContains(segmentValidationText, "private static bool TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentValidationText, "private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(segmentValidationText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertDoesNotContain(segmentsText, "FindDuplicateSegmentPathIndex(segments)");
        AssertDoesNotContain(segmentsText, "FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN");
        AssertContains(executionPolicyText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertContains(executionPolicyText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(executionPolicyText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(executionPolicyText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(executionPolicyText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(executionPolicyText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(executionPolicyText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(executionPolicyText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(executionPolicyText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(executionPolicyText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(executionPolicyText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(executionPolicyText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
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
        AssertContains(packetBuffersText, "private long FlushBufferedPackets(");
        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.PacketBuffers.cs")),
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.PacketTiming.cs");
        AssertContains(streamsText, "private void OpenInput(string inputPath)");
        AssertContains(streamsText, "private void CreateOutputContext(string tmpPath, bool fastStart)");
        AssertContains(streamsText, "private static void OpenOutputIoAndWriteHeader(AVFormatContext* outputContext, string tmpPath, bool fastStart)");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentValidation.cs")),
            "FlashbackExporter.SegmentValidation.cs folded into FlashbackExporter.Validation.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.Progress.cs",
            "FlashbackExporter.WriterPacing.cs",
            "FlashbackExporter.RuntimePolicy.cs",
            "FlashbackExporter.SingleFile.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.Execution.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentPacketRebasing.cs")),
            "FlashbackExporter.SegmentPacketRebasing.cs folded into FlashbackExporter.SegmentPacketReadLoop.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentTemplate.cs")),
            "FlashbackExporter.SegmentTemplate.cs folded into FlashbackExporter.Segments.cs");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation()
    {
        var sourceText = ReadFlashbackExporterSource();
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private readonly object _lifetimeSync = new();");
        AssertContains(sourceText, "return Task.FromResult(CreateDisposedExportResult(request.OutputPath));");
        AssertEqual(2, sourceText.Split("return Task.FromResult(CreateDisposedExportResult(outputPath));", StringSplitOptions.None).Length - 1, "Single and segment wrappers return disposed result");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            cancellationResult = CreateDisposedExportResult(outputPath);\n            return false;\n        }");
        AssertContains(sourceText, "linkedCts = CreateExportCancellationSource(ct);");
        AssertContains(sourceText, "var segmentSnapshot = SnapshotSegments(segments);");
        AssertContains(sourceText, "private static IReadOnlyList<FlashbackExportSegment> SnapshotSegments(IReadOnlyList<FlashbackExportSegment>? segments)");
        AssertContains(sourceText, "snapshot[i] = segment == null\n                ? new FlashbackExportSegment { Path = string.Empty }\n                : segment with { };");
        AssertContains(sourceText, "CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposed, this);");
        AssertContains(sourceText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(sourceText, "const string message = \"Flashback exporter is disposed.\";");
        AssertContains(sourceText, "private const int ExportLockWaitTimeoutSeconds = 30;");
        AssertContains(executionText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(executionText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(executionText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(executionText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(executionText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(sourceText, "_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"single_export\"));");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"segment_export\"));");
        AssertContains(sourceText, "thread.Priority = ThreadPriority.BelowNormal;");
        AssertContains(sourceText, "thread.Priority = previousPriority;");
        AssertContains(sourceText, "Func<int>? adaptiveThrottleDelayMsProvider");
        AssertContains(executionText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(executionText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(executionText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(executionText, "[ThreadStatic]\n    private static Func<int>? s_adaptiveThrottleDelayMsProvider;");
        AssertContains(executionText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(executionText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(executionText, "packetsWritten % ExportWriterAdaptiveThrottlePacketInterval == 0");
        AssertContains(executionText, "ExportWriterMaxAdaptiveThrottleSleepMs");
        AssertContains(executionText, "Thread.Sleep(ExportWriterThrottleSleepMs);");
        AssertContains(executionText, "Thread.Yield();");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(totalPackets);");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(written);");
        AssertContains(sourceText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN");
        AssertContains(packetBuffersText, "private long FlushBufferedPackets(");
        AssertContains(packetBuffersText, "NormalizePacketTimestampsBeforeWrite(buffPkt);");
        AssertContains(packetBuffersText, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(packetBuffersText, "ffmpeg.av_packet_free(&p);");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "var clone = ffmpeg.av_packet_clone(packet);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.PacketBuffers.cs")),
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.PacketTiming.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.RuntimePolicy.cs")),
            "FlashbackExporter.RuntimePolicy.cs folded into FlashbackExporter.Execution.cs");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(sourceText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_RELEASE_WARN");
        AssertDoesNotContain(sourceText, "catch (ObjectDisposedException) { }");
        AssertDoesNotContain(sourceText, "}, linkedCts.Token);");
        AssertDoesNotContain(sourceText, "_disposeCts!.Token");

        return Task.CompletedTask;
    }

}

static partial class Program
{
    internal static Task FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState()
    {
        var sourceText = ReadFlashbackExporterSource();

        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "    private FinalizeResult ExportCore");
        AssertContains(disposeBlock, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n        }");
        AssertOccursBefore(disposeBlock, "FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN", "var lockAcquired = _exportLock.Wait(TimeSpan.FromSeconds(10));");
        AssertContains(disposeBlock, "ReleaseExportLockBestEffort(\"dispose\");");
        AssertContains(disposeBlock, "DisposeExportLockBestEffort();");
        AssertContains(disposeBlock, "DisposeLinkedCtsBestEffort(disposeCts, \"dispose\");");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_DISPOSE_WARN");

        var timeoutBlock = ExtractTextBetween(
            sourceText,
            "if (!lockAcquired)",
            "        try\n        {\n            CleanupNativeState();");

        AssertContains(timeoutBlock, "FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock");
        AssertContains(timeoutBlock, "DisposeLinkedCtsBestEffort(disposeCts, \"dispose_timeout\");");
        AssertContains(timeoutBlock, "ClearDisposeCtsReference(disposeCts);");
        AssertContains(timeoutBlock, "return;");
        AssertDoesNotContain(timeoutBlock, "CleanupNativeState()");
        AssertDoesNotContain(timeoutBlock, "_exportLock.Dispose()");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task FlashbackExporter_InputStreamCountsAreBounded()
    {
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(streamsText, "private static bool TryGetInputStreamCount(");
        AssertContains(streamsText, "if (nativeStreamCount == 0)");
        AssertContains(streamsText, "if (nativeStreamCount > MaxSupportedInputStreams)");
        AssertContains(streamsText, "streamCount = (int)nativeStreamCount;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"single_export\", out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'\");");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_template\", out var candidateStreamCount, out var streamCountFailure))");
        AssertContains(segmentInputPreflightText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_export\", out currentStreamCount, out var streamCountFailure))");
        AssertContains(segmentInputPreflightText, "FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='invalid_stream_count'");
        AssertContains(singleFileText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount)");
        AssertContains(segmentTemplateText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount)");
        AssertContains(streamTemplatesText, "private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }
}

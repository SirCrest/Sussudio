using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var requestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Requests.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifetime.cs")
            .Replace("\r\n", "\n");
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs")
            .Replace("\r\n", "\n");
        var segmentsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs")
            .Replace("\r\n", "\n");
        var segmentSkipTrackingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentSkipTracking.cs")
            .Replace("\r\n", "\n");
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs")
            .Replace("\r\n", "\n");
        var segmentValidationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentValidation.cs")
            .Replace("\r\n", "\n");
        var progressText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Progress.cs")
            .Replace("\r\n", "\n");
        var tempFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.TempFiles.cs")
            .Replace("\r\n", "\n");
        var exportLockText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.ExportLock.cs")
            .Replace("\r\n", "\n");
        var resultsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Results.cs")
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
        AssertContains(singleFileText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "TryValidateSegmentExportInputs(");
        AssertContains(segmentsText, "TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentsText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentSkipTrackingText, "private struct RequestedSegmentSkipTracker");
        AssertContains(segmentSkipTrackingText, "public void Track(FlashbackExportSegment segment, string reason)");
        AssertContains(segmentSkipTrackingText, "public bool TryCreateFailureMessage(out string message)");
        AssertContains(segmentsText, "ReleaseExportLockBestEffort(\"segment_export\");");
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
        AssertContains(progressText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(tempFilesText, "private static void DeleteTempFileIfPresent(string tmpPath)");
        AssertContains(tempFilesText, "private static bool TryPrepareTempOutputFile(string tmpPath, string outputPath, out string failureMessage)");
        AssertContains(tempFilesText, "internal static void CleanupOrphanedTempFiles(string directory)");
        AssertContains(exportLockText, "private bool TryWaitForExportLock(string outputPath, CancellationToken ct, out FinalizeResult cancellationResult)");
        AssertContains(exportLockText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(exportLockText, "private void DisposeExportLockBestEffort()");
        AssertContains(resultsText, "private static FinalizeResult CreateCancelledExportResult(string outputPath)");
        AssertContains(resultsText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
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

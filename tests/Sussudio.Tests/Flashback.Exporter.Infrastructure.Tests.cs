using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation()
    {
        var sourceText = ReadFlashbackExporterSource();

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
        AssertContains(sourceText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(sourceText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(sourceText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(sourceText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(sourceText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(sourceText, "_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"single_export\"));");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"segment_export\"));");
        AssertContains(sourceText, "thread.Priority = ThreadPriority.BelowNormal;");
        AssertContains(sourceText, "thread.Priority = previousPriority;");
        AssertContains(sourceText, "Func<int>? adaptiveThrottleDelayMsProvider");
        AssertContains(sourceText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(sourceText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(sourceText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(sourceText, "[ThreadStatic]\n    private static Func<int>? s_adaptiveThrottleDelayMsProvider;");
        AssertContains(sourceText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(sourceText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(sourceText, "packetsWritten % ExportWriterAdaptiveThrottlePacketInterval == 0");
        AssertContains(sourceText, "ExportWriterMaxAdaptiveThrottleSleepMs");
        AssertContains(sourceText, "Thread.Sleep(ExportWriterThrottleSleepMs);");
        AssertContains(sourceText, "Thread.Yield();");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(totalPackets);");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(written);");
        AssertContains(sourceText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(sourceText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_RELEASE_WARN");
        AssertDoesNotContain(sourceText, "catch (ObjectDisposedException) { }");
        AssertDoesNotContain(sourceText, "}, linkedCts.Token);");
        AssertDoesNotContain(sourceText, "_disposeCts!.Token");

        return Task.CompletedTask;
    }

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
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs")
            .Replace("\r\n", "\n");
        var segmentValidationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentValidation.cs")
            .Replace("\r\n", "\n");
        var progressText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Progress.cs")
            .Replace("\r\n", "\n");
        var tempFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.TempFiles.cs")
            .Replace("\r\n", "\n");
        var infrastructureText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Infrastructure.cs")
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
        AssertContains(segmentsText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(segmentTemplateText, "private bool TryInitializeSegmentOutputTemplate(");
        AssertContains(segmentTemplateText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertDoesNotContain(segmentsText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(segmentValidationText, "private static bool TryValidateSegmentExportInputs(");
        AssertContains(segmentValidationText, "private static bool TryEstimateSegmentExportReadableBytes(");
        AssertDoesNotContain(segmentsText, "FindDuplicateSegmentPathIndex(segments)");
        AssertDoesNotContain(segmentsText, "FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN");
        AssertContains(progressText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertContains(progressText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(progressText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertDoesNotContain(infrastructureText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertDoesNotContain(infrastructureText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(tempFilesText, "private static void DeleteTempFileIfPresent(string tmpPath)");
        AssertContains(tempFilesText, "private static bool TryPrepareTempOutputFile(string tmpPath, string outputPath, out string failureMessage)");
        AssertContains(tempFilesText, "internal static void CleanupOrphanedTempFiles(string directory)");
        AssertDoesNotContain(infrastructureText, "internal static void CleanupOrphanedTempFiles(string directory)");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> ExportAsync(");
        AssertDoesNotContain(rootText, "public void Dispose()");
        AssertDoesNotContain(rootText, "private FinalizeResult ExportCore(");
        AssertDoesNotContain(rootText, "private FinalizeResult ExportSegmentsCore(");

        return Task.CompletedTask;
    }
}

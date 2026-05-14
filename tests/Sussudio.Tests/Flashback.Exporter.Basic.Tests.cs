using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var resolve = serviceType.GetMethod("ResolveFlashbackExportThrottleDelayMs", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFlashbackExportThrottleDelayMs not found.");
        var exportOperationsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            .Replace("\r\n", "\n");
        var exportPlanningText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs")
            .Replace("\r\n", "\n");
        var sourceText = exportOperationsText
            + "\n" + exportPlanningText
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
                .Replace("\r\n", "\n");

        AssertEqual(0, (int)resolve.Invoke(null, new object[] { 0.49, 29L, false })!, "Flashback export throttle idle");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.49, 0L, true })!, "Flashback export throttle high-resolution live baseline");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.50, 0L, false })!, "Flashback export throttle queue half full");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.0, 30L, false })!, "Flashback export throttle oldest frame mild pressure");
        AssertEqual(20, (int)resolve.Invoke(null, new object[] { 0.70, 0L, false })!, "Flashback export throttle medium queue pressure");
        AssertEqual(20, (int)resolve.Invoke(null, new object[] { 0.0, 50L, false })!, "Flashback export throttle medium frame age");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.85, 0L, false })!, "Flashback export throttle severe queue pressure");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.0, 90L, false })!, "Flashback export throttle severe frame age");
        AssertContains(sourceText, "throttleHighResolutionBaseline && IsHighResolutionFlashbackExport(flashbackSink)");
        AssertContains(sourceText, "FastStart = false");
        AssertContains(sourceText, "AdaptiveThrottleDelayMsProvider = CreateFlashbackExportThrottleDelayProvider(\n                    flashbackSink,\n                    throttleHighResolutionBaseline)");
        AssertContains(sourceText, "ct: ct,");
        AssertContains(sourceText, "requireCompleteLiveEdge: true");
        AssertContains(sourceText, "throttleHighResolutionBaseline: false");
        AssertOccursBefore(sourceText, "ct: ct,", "requireCompleteLiveEdge: true");
        AssertOccursBefore(sourceText, "requireCompleteLiveEdge: true", "throttleHighResolutionBaseline: false");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LIVE_THROTTLE");
        AssertDoesNotContain(exportOperationsText, "private static int ResolveFlashbackExportThrottleDelayMs(");
        AssertContains(exportPlanningText, "private static int ResolveFlashbackExportThrottleDelayMs(");
        AssertContains(exportPlanningText, "private static IReadOnlyList<FlashbackExportSegment>? BuildFlashbackExportSegments(");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        // Non-existent directory should not throw
        cleanup.Invoke(null, new object[] { Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}") });

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cleanup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var orphan1 = Path.Combine(tempDir, "clip_a.mp4.tmp");
            var orphan2 = Path.Combine(tempDir, "clip_b.mp4.tmp");
            var recentTemp = Path.Combine(tempDir, "clip_recent.mp4.tmp");
            var lockedTemp = Path.Combine(tempDir, "clip_locked.mp4.tmp");
            var unrelated = Path.Combine(tempDir, "unrelated.mp4");
            var legacyTemp = Path.Combine(tempDir, "fb_export_temp_001.ts");

            File.WriteAllText(orphan1, "data");
            File.WriteAllText(orphan2, "data");
            File.WriteAllText(recentTemp, "keep");
            File.WriteAllText(lockedTemp, "keep");
            File.WriteAllText(unrelated, "keep");
            File.WriteAllText(legacyTemp, "keep");
            var oldEnough = DateTime.UtcNow - TimeSpan.FromMinutes(30);
            File.SetLastWriteTimeUtc(orphan1, oldEnough);
            File.SetLastWriteTimeUtc(orphan2, oldEnough);
            File.SetLastWriteTimeUtc(lockedTemp, oldEnough);

            using var lockedStream = new FileStream(lockedTemp, FileMode.Open, FileAccess.Read, FileShare.None);

            cleanup.Invoke(null, new object[] { tempDir });

            AssertEqual(false, File.Exists(orphan1), "First mp4 temp deleted");
            AssertEqual(false, File.Exists(orphan2), "Second mp4 temp deleted");
            AssertEqual(true, File.Exists(recentTemp), "Recent mp4 temp preserved");
            AssertEqual(true, File.Exists(lockedTemp), "Locked mp4 temp preserved");
            AssertEqual(true, File.Exists(unrelated), "Unrelated file preserved");
            AssertEqual(true, File.Exists(legacyTemp), "Legacy TS temp preserved by mp4 cleanup");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertDoesNotContain(sourceText, "private static void CleanupOrphanedTempFilesNearOutput(string outputPath)");
        AssertDoesNotContain(sourceText, "FLASHBACK_EXPORT_ORPHAN_OUTPUT_SCAN_FAIL");

        var singleExportBlock = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportCore(",
            "    private FinalizeResult ExportSegmentsCore(");
        AssertContains(singleExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(singleExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");
        AssertContains(singleExportBlock, "TryPrepareTempOutputFile(tmpPath, outputPath, out var tempOutputFailure)");

        var segmentExportBlock = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportSegmentsCore(",
            "    private static long ResolveFrameDurationUs");
        AssertContains(segmentExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(segmentExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");
        AssertContains(segmentExportBlock, "TryPrepareTempOutputFile(tmpPath, outputPath, out var tempOutputFailure)");

        return Task.CompletedTask;
    }

    private static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportAsync not found.");

        var nonexistentInput = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.ts");
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");
        var request = Activator.CreateInstance(requestType)!;
        SetPropertyBackingField(request, "InputTsPath", nonexistentInput);
        SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
        SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
        SetPropertyBackingField(request, "OutputPath", outputPath);
        SetPropertyBackingField(request, "FastStart", true);

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            request,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        var succeeded = GetBoolProperty(result, "Succeeded");
        AssertEqual(false, succeeded, "Export fails when input file not found");
    }

    private static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        // Create a real temp file so input validation passes
        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
            File.WriteAllBytes(tempInput, new byte[] { 0x47 }); // MPEG-TS sync byte
        try
        {
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputTsPath", tempInput);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
            SetPropertyBackingField(request, "OutputPath", "");
            SetPropertyBackingField(request, "FastStart", true);

            var task = exportMethod.Invoke(exporter, new object?[]
            {
                request,
                null,
                CancellationToken.None
            }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export fails when output path empty");
        }
        finally
        {
            try { File.Delete(tempInput); } catch { }
        }
    }

    private static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"fb_export_dir_{Guid.NewGuid():N}");
        File.WriteAllBytes(tempInput, new byte[] { 0x47 });
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputTsPath", tempInput);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
            SetPropertyBackingField(request, "OutputPath", outputDirectory);
            SetPropertyBackingField(request, "FastStart", true);

            var task = exportMethod.Invoke(exporter, new object?[]
            {
                request,
                null,
                CancellationToken.None
            }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export fails when output path is a directory");
            AssertContains(GetStringProperty(result, "StatusMessage"), "output path is a directory");
            AssertEqual(false, File.Exists(outputDirectory + ".tmp"), "Directory-target export does not create temp output");
        }
        finally
        {
            try { File.Delete(tempInput); } catch { }
            try { Directory.Delete(outputDirectory, recursive: true); } catch { }
        }
    }

    private static async Task FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportSegmentsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportSegmentsAsync not found.");

        var emptySegments = Array.CreateInstance(segmentType, 0);
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            emptySegments,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            outputPath,
            true,
            false,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportSegmentsAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export segments fails when no segments");
    }

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
        var progressText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Progress.cs")
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
        AssertContains(segmentsText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(progressText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertContains(progressText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(progressText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertDoesNotContain(infrastructureText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertDoesNotContain(infrastructureText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> ExportAsync(");
        AssertDoesNotContain(rootText, "public void Dispose()");
        AssertDoesNotContain(rootText, "private FinalizeResult ExportCore(");
        AssertDoesNotContain(rootText, "private FinalizeResult ExportSegmentsCore(");

        return Task.CompletedTask;
    }

    private static async Task FlashbackExporter_RejectsNullRequests()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", new[] { requestType, typeof(IProgress<>).MakeGenericType(RequireType("Sussudio.Models.ExportProgress")), typeof(CancellationToken) })
            ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync(request) not found.");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            null,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Null export request reports failure");
        AssertContains(GetStringProperty(result, "StatusMessage"), "request is required");
    }

    private static Task FlashbackExporter_OutputPathValidation_ReturnsFailure()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "if (!TryValidateOutputPath(outputPath, out var normalizedOutputPath, out var outputPathFailure))\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'\");\n            return FinalizeResult.Failure(outputPath, outputPathFailure);\n        }\n        outputPath = normalizedOutputPath;");
        AssertContains(sourceText, "if (!TryValidateExportRange(inPoint, outPoint, out var rangeFailure))");
        AssertContains(sourceText, "private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: in point must not be negative.\";");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: export range is empty or invalid.\";");
        AssertContains(sourceText, "var invalidSegmentIndex = FindInvalidSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: segment path at index {invalidSegmentIndex} is empty.");
        AssertContains(sourceText, "private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(sourceText, "private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)");
        AssertContains(sourceText, "fullOutputPath = Path.GetFullPath(outputPath);");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: output path is required.\";");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";\n            Logger.Log($\"FLASHBACK_EXPORT_PATH_VALIDATE_WARN path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            return false;\n        }");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output directory does not exist for '{outputPath}'.\";");
        AssertContains(sourceText, "if (Directory.Exists(fullOutputPath))\n        {\n            failureMessage = $\"Flashback export failed: output path is a directory '{outputPath}'.\";\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PATH_COMPARE_WARN");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN");

        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var validateOutputPath = exporterType.GetMethod("TryValidateOutputPath", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryValidateOutputPath not found.");
        var args = new object?[] { ".\\flashback-relative-output.mp4", null, null };
        var isValid = (bool)validateOutputPath.Invoke(null, args)!;
        AssertEqual(true, isValid, "Relative output path validates when current directory exists");
        AssertEqual(
            Path.GetFullPath(".\\flashback-relative-output.mp4"),
            (string)args[1]!,
            "Relative output path is normalized to full path");
        AssertEqual(string.Empty, (string)args[2]!, "Valid output path has no failure message");

        return Task.CompletedTask;
    }

    private static Task FlashbackExportFailureClassifier_MapsCommandFailures()
    {
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = captureServiceType.GetMethod(
            "ClassifyFlashbackExportFailureKind",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureService.ClassifyFlashbackExportFailureKind was not found.");
        var exportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            .Replace("\r\n", "\n");
        var classifierText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(exportText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(classifierText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(classifierText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(classifierText, "private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)");

        AssertEqual(
            "BufferInactive",
            method.Invoke(null, new object?[] { "Flashback buffer not active" })?.ToString(),
            "inactive buffer export rejection is classified");
        AssertEqual(
            "InvalidRequest",
            method.Invoke(null, new object?[] { "Flashback export duration must be finite, greater than zero, and within TimeSpan range." })?.ToString(),
            "invalid duration export rejection is classified");
        AssertEqual(
            "InvalidRange",
            method.Invoke(null, new object?[] { "Flashback export range is empty or invalid." })?.ToString(),
            "invalid export range is classified");
        AssertEqual(
            "UnavailableDuringRecording",
            method.Invoke(null, new object?[] { "Cannot export while Flashback is the active recording backend." })?.ToString(),
            "recording backend export rejection is classified");
        AssertEqual(
            "InvalidOutputPath",
            method.Invoke(null, new object?[] { "Flashback export failed: output path is a directory." })?.ToString(),
            "output path export rejection is classified");
        AssertEqual(
            "InputUnavailable",
            method.Invoke(null, new object?[] { "Flashback buffer has no active file" })?.ToString(),
            "missing active file export rejection is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=avio_open2 code=-13 msg='Permission denied'" })?.ToString(),
            "output open failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=av_interleaved_write_frame code=-5 msg='I/O error'" })?.ToString(),
            "output packet write failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_ERROR operation=avformat_alloc_output_context2 msg='Output context allocation failed.'" })?.ToString(),
            "output context allocation failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_ERROR operation=avformat_new_stream msg='Stream allocation returned null.'" })?.ToString(),
            "output stream allocation failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=avcodec_parameters_copy code=-22 msg='Invalid argument'" })?.ToString(),
            "output stream parameter copy failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=av_dict_set(movflags) code=-12 msg='Cannot allocate memory'" })?.ToString(),
            "output muxer option failure is classified");
        AssertEqual(
            "InputReadFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=av_read_frame code=-5 msg='I/O error'" })?.ToString(),
            "input read failure is classified");
        AssertEqual(
            "NoMediaWritten",
            method.Invoke(null, new object?[] { "Flashback export wrote no packets." })?.ToString(),
            "empty media export failure is classified");
        AssertEqual(
            "NoMediaWritten",
            method.Invoke(null, new object?[] { "Flashback export failed: output file is empty 'clip.mp4'." })?.ToString(),
            "empty completed output export failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "Flashback export failed: output file length unavailable 'clip.mp4'." })?.ToString(),
            "unreadable completed output export failure is classified");
        AssertEqual(
            "IncompleteLiveEdge",
            method.Invoke(null, new object?[] { "Flashback export skipped a live-edge segment." })?.ToString(),
            "live-edge segment export failure is classified");
        AssertEqual(
            "ForceRotateFailed",
            method.Invoke(null, new object?[] { "Flashback export failed: live-edge segment rotation failed." })?.ToString(),
            "live-edge force-rotate failure is classified");
        AssertEqual(
            "ForceRotateFailed",
            method.Invoke(null, new object?[] { "Flashback export failed: rotation failed." })?.ToString(),
            "generic rotation failure is classified");
        AssertEqual(
            "SegmentUnavailable",
            method.Invoke(null, new object?[] { "Flashback export failed: no segment paths were readable." })?.ToString(),
            "missing segment export failure is classified");
        AssertEqual(
            "InvalidInputStream",
            method.Invoke(null, new object?[] { "Flashback export failed: input had no streams." })?.ToString(),
            "invalid input stream export failure is classified");
        AssertEqual(
            "Disposed",
            method.Invoke(null, new object?[] { "Flashback exporter is disposed." })?.ToString(),
            "disposed exporter failure is classified");
        AssertEqual(
            "Cancelled",
            method.Invoke(null, new object?[] { "Flashback export cancelled." })?.ToString(),
            "cancelled export failure is classified");
        AssertEqual(
            "Timeout",
            method.Invoke(null, new object?[] { "Flashback export lock timed out after 30s." })?.ToString(),
            "export timeout failure is classified");

        return Task.CompletedTask;
    }

}

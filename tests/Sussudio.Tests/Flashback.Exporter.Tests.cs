using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var resolve = serviceType.GetMethod("ResolveFlashbackExportThrottleDelayMs", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFlashbackExportThrottleDelayMs not found.");
        var exportOperationsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var exportCoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var exportPlanningText = exportCoreText;
        var sourceText = exportOperationsText
            + "\n" + exportCoreText
            + "\n" + exportPlanningText
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();

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
        AssertContains(sourceText, "AdaptiveThrottleDelayMsProvider = CreateFlashbackExportThrottleDelayProvider(");
        AssertContains(sourceText, "flashbackSink,\n                throttleHighResolutionBaseline)");
        AssertContains(sourceText, "ct: ct,");
        AssertContains(sourceText, "requireCompleteLiveEdge: true");
        AssertContains(sourceText, "throttleHighResolutionBaseline: false");
        AssertOccursBefore(sourceText, "ct: ct,", "requireCompleteLiveEdge: true");
        AssertOccursBefore(sourceText, "requireCompleteLiveEdge: true", "throttleHighResolutionBaseline: false");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LIVE_THROTTLE");
        AssertContains(exportPlanningText, "private static int ResolveFlashbackExportThrottleDelayMs(");
        AssertContains(exportPlanningText, "private static IReadOnlyList<FlashbackExportSegment>? BuildFlashbackExportSegments(");
        AssertEqual(
            1,
            exportCoreText.Split("public partial class CaptureService", StringSplitOptions.None).Length - 1,
            "CaptureService.FlashbackExportCore.cs stays one in-file CaptureService body");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportPlanning.cs")),
            "CaptureService.FlashbackExportPlanning.cs folded into CaptureService.FlashbackExportCore.cs");

        return Task.CompletedTask;
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound()
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

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty()
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

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory()
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

    internal static async Task FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments()
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

    internal static async Task FlashbackExporter_RejectsNullRequests()
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

    internal static Task FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var segment = Activator.CreateInstance(segmentType)!;
            SetPropertyBackingField(segment, "Path", Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.mp4"));
            var segments = Array.CreateInstance(segmentType, 1);
            segments.SetValue(segment, 0);
            var outputPath = Path.Combine(Path.GetTempPath(), $"fb_cancelled_{Guid.NewGuid():N}.mp4");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

            var result = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                segments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                outputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Cancelled export reports failure result");
            AssertContains(GetStringProperty(result, "StatusMessage"), "cancelled");
            AssertEqual(false, File.Exists(outputPath), "Cancelled export does not create output");
            AssertEqual(false, File.Exists(outputPath + ".tmp"), "Cancelled export does not leave temp output");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_CancellationWinsBeforeValidation()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
            var singleOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_single_{Guid.NewGuid():N}.mp4");
            var singleResult = exportCore.Invoke(exporter, new object?[]
            {
                Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.ts"),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                singleOutputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportCore returned null.");

            AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Cancelled single-file export reports failure");
            AssertContains(GetStringProperty(singleResult, "StatusMessage"), "cancelled");
            AssertDoesNotContain(GetStringProperty(singleResult, "StatusMessage"), "not found");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
            var emptySegments = Array.CreateInstance(segmentType, 0);
            var segmentOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_segments_{Guid.NewGuid():N}.mp4");
            var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                emptySegments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                segmentOutputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Cancelled segment export reports failure");
            AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "cancelled");
            AssertDoesNotContain(GetStringProperty(segmentResult, "StatusMessage"), "no segment paths");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportFailureClassifier_MapsCommandFailures()
    {
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = captureServiceType.GetMethod(
            "ClassifyFlashbackExportFailureKind",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureService.ClassifyFlashbackExportFailureKind was not found.");
        var exportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(diagnosticsText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(diagnosticsText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(diagnosticsText, "private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)");

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

    internal static Task FlashbackExporter_RejectsInvalidExportRanges()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_invalid_range_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.ts");
        File.WriteAllBytes(inputPath, new byte[] { 0x47 });

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleOutputPath = Path.Combine(tempDir, "single-invalid.mp4");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    inputPath,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5),
                    singleOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Empty single-file export range reports failure");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "export range is empty or invalid");
                AssertEqual(false, File.Exists(singleOutputPath), "Invalid single-file range does not create output");
                AssertEqual(false, File.Exists(singleOutputPath + ".tmp"), "Invalid single-file range does not leave temp output");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentOutputPath = Path.Combine(tempDir, "segment-invalid.mp4");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.FromSeconds(-1),
                    TimeSpan.FromSeconds(1),
                    segmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Negative segment export in point reports failure");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "in point must not be negative");
                AssertEqual(false, File.Exists(segmentOutputPath), "Invalid segment range does not create output");
                AssertEqual(false, File.Exists(segmentOutputPath + ".tmp"), "Invalid segment range does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportRejectedDiagnostics_PreserveAttemptedRange()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
                .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(");
        AssertContains(captureServiceText, "ResolveFlashbackExportRangeAfterEvictionPaused(");
        AssertContains(captureServiceText, "if (inPointFilePts.HasValue || outPointFilePts.HasValue)");
        AssertContains(captureServiceText, "var absoluteInPoint = inPointFilePts ?? validStart;");
        AssertContains(captureServiceText, "var absoluteOutPoint = outPointFilePts ?? TimeSpan.MaxValue;");
        AssertContains(captureServiceText, "\"Flashback export in point has been evicted from the buffer.\"");
        AssertContains(captureServiceText, "\"Flashback export out point has been evicted from the buffer.\"");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\", inPoint, outPoint);");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_SNAPSHOT_FAIL op={operationName} type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceText, "_flashbackExportInPointMs = inPoint.HasValue ? (long)inPoint.Value.TotalMilliseconds : 0;");
        AssertContains(captureServiceText, "outPoint.Value == TimeSpan.MaxValue ? -1 : (long)outPoint.Value.TotalMilliseconds");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures()
    {
        var sourceText = ReadFlashbackExporterSource();
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketReadLoopText = singleFileText;
        var singleFilePacketWritingText = singleFilePacketReadLoopText;
        var singleFilePacketWriteStateText = singleFilePacketReadLoopText;
        var segmentsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWriteStateText = segmentPacketReadLoopText;
        var segmentPacketRebasingText = segmentPacketReadLoopText;
        var packetBuffersText = segmentPacketWritingText;

        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(sourceText, "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");
        AssertContains(sourceText, "FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);");
        AssertContains(sourceText, "FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);");
        AssertContains(packetBuffersText, "bufferedStreamIndices?.Clear();");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.PacketBuffers.cs")),
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.SegmentPacketWriting.cs");
        AssertContains(singleFileText, "WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFilePacketWritingText, "WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketReadLoopText, "FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);");
        AssertContains(singleFilePacketWriteStateText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(segmentsText, "WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");
        AssertContains(segmentPacketWritingText, "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentPacketWritingText, "WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "private void WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");

        var segmentLoopBlock = ExtractTextBetween(
            segmentPacketReadLoopText,
            "segmentPacketState = CreateSegmentPacketWriteState(",
            "    }\n}");
        AssertContains(segmentPacketWriteStateText, "private int FlushSegmentBufferedPackets(");
        AssertContains(segmentLoopBlock, "totalPackets += FlushSegmentBufferedPackets(");
        AssertOccursBefore(
            segmentLoopBlock,
            "if (segmentPacketState.AllBasesDiscovered)",
            "if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)");
        AssertOccursBefore(
            segmentLoopBlock,
            "if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)",
            "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");
        var segmentFlushBlock = ExtractTextBetween(
            segmentPacketWriteStateText,
            "private int FlushSegmentBufferedPackets(",
            "private enum SegmentPacketWriteOutcome");
        var segmentWriteBlock = ExtractTextBetween(
            segmentPacketRebasingText,
            "private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(",
            "    }\n}");
        AssertContains(segmentFlushBlock, "finally\n        {\n            FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);\n        }");
        AssertContains(segmentFlushBlock, "WriteRebasedSegmentPacket(");
        AssertContains(segmentWriteBlock, "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), \"av_interleaved_write_frame\");");
        AssertOccursBefore(
            segmentFlushBlock,
            "WriteRebasedSegmentPacket(",
            "finally\n        {\n            FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);\n        }");
        AssertContains(segmentLoopBlock, "if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)");
        AssertContains(segmentLoopBlock, "segmentPacketState.MinBaseUs ??= 0;");
        AssertContains(segmentLoopBlock, "FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx}");
        AssertContains(segmentLoopBlock, "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");

        var sharedFlushBlock = ExtractTextBetween(
            packetBuffersText,
            "private long FlushBufferedPackets(",
            "private static void FreeBufferedPackets(");
        AssertContains(sharedFlushBlock, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertOccursBefore(
            sharedFlushBlock,
            "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), \"av_interleaved_write_frame\");",
            "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ProgressCallbacksAreBestEffort()
    {
        var sourceText = ReadFlashbackExporterSource();
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "progress?.Report(new ExportProgress");
        AssertContains(sourceText, "using System.Diagnostics;");
        AssertContains(sourceText, "private const int ProgressHeartbeatIntervalMs = 1_000;");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_start\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, segments.Count, 0), \"segments_start\");");
        AssertContains(sourceText, "if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_heartbeat\");");
        var segmentExportLoopBlock = ExtractTextBetween(
            segmentPacketReadLoopText,
            "var segmentVideoFrameDurUs = 33333L;",
            "// EOF: if Phase 1 never completed");
        AssertContains(segmentExportLoopBlock, "ReportProgress(");
        AssertContains(segmentExportLoopBlock, "\"segment_heartbeat\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(1, 1, 100.0), \"single_complete\")");
        AssertContains(sourceText, "if (!TryFinalizeActiveOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{failureMessage}'\");");
        AssertContains(sourceText, "ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), \"av_write_trailer\");");
        AssertContains(sourceText, "CloseOutputIo();");
        AssertContains(sourceText, "return FinalizeResult.Failure(outputPath, outputFailure);");
        AssertContains(sourceText, "ReportProgress(\n                    progress,\n                    new ExportProgress(\n                        segIdx + 1,\n                        segments.Count,");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(segments.Count, segments.Count, 100.0), \"segments_complete\")");
        AssertContains(sourceText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)\n    {\n        value = NormalizeExportProgress(value, stage);");
        AssertContains(sourceText, "private static ExportProgress NormalizeExportProgress(ExportProgress value, string stage)");
        AssertContains(sourceText, "if (totalSegments > 0 && segmentsProcessed > totalSegments)");
        AssertContains(sourceText, "var percent = double.IsFinite(value.Percent)\n            ? Math.Clamp(value.Percent, 0.0, 100.0)\n            : 0.0;");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_NORMALIZED stage={stage}");
        AssertContains(sourceText, "return new ExportProgress(segmentsProcessed, totalSegments, percent);");
        AssertContains(sourceText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(sourceText, "(now - last) * 1000.0 / Stopwatch.Frequency < ProgressHeartbeatIntervalMs");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_PROGRESS_WARN stage={stage} type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "private static long GetFileLengthBestEffort(string path)\n    {\n        try\n        {\n            return new FileInfo(path).Length;\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_WARN reason='output_length_unavailable' path='{path}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            return -1;\n        }\n    }");
        AssertContains(sourceText, "private static bool TryValidateCompletedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(sourceText, "outputBytes > 0");
        AssertContains(sourceText, "Flashback export failed: output file is empty");
        AssertContains(sourceText, "Flashback export failed: output file length unavailable");
        AssertContains(sourceText, "private static bool TryFinalizeTempOutputFile(");
        AssertContains(sourceText, "private bool TryFinalizeActiveOutputFile(");
        AssertContains(sourceText, "Flashback export failed: temporary output file is empty before replacing");
        AssertContains(sourceText, "AtomicMoveTempFile(tmpPath, outputPath, allowOverwrite);");
        AssertContains(sourceText, "FLASHBACK_EXPORT_REFUSED_DESTINATION_EXISTS");
        AssertContains(sourceText, "FLASHBACK_EXPORT_FINAL_OUTPUT_VALIDATE_WARN");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed' path='{tmpPath}' type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_ORPHAN_CLEANUP_FAIL path='{Path.GetFileName(tmpFile)}' type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_ORPHAN_SCAN_FAIL dir='{directory}' type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "FLASHBACK_EXPORT_CLEANUP_WARN op=close_input");
        AssertContains(sourceText, "FLASHBACK_EXPORT_CLEANUP_WARN op=close_output_io");
        AssertContains(sourceText, "FLASHBACK_EXPORT_CLEANUP_WARN op=free_output_context");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_UPDATE_WARN");
        AssertDoesNotContain(sourceText, "catch { /* Best-effort: segment may be deleted mid-export; progress tracking is non-critical */ }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream()
    {
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        var templateSelectionBlock = ExtractTextBetween(
            sourceText,
            "private bool TryInitializeSegmentOutputTemplate(",
            "    private bool TryOpenSegmentInputForExport");
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

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped()
    {
        var segmentExportCore = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var skipTrackingText = segmentPacketWritingText;
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        AssertContains(segmentExportCore, "WriteSegmentPacketsToActiveOutput(");
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

    internal static Task FlashbackExporter_TimestampConversionsAreSaturating()
    {
        var sourceText = ReadFlashbackExporterSource();
        var packetTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "TotalSeconds * ffmpeg.AV_TIME_BASE");
        AssertDoesNotContain(sourceText, "TotalMilliseconds * 1000)");
        AssertContains(sourceText, "var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);");
        AssertContains(sourceText, "ToAvTimeBaseTimestampOrMax(outPoint),");
        AssertContains(sourceText, "var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);");
        AssertContains(sourceText, "var segmentInOffsetUs = ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value));");
        AssertContains(sourceText, "var segmentOutDelta = SaturatingSubtract(");
        AssertContains(sourceText, "var segmentOutOffsetUs = ToMicrosecondsSaturated(segmentOutDelta);");
        AssertContains(sourceText, "SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero");
        AssertContains(sourceText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(sourceText, "var segmentLength = new FileInfo(segment.Path).Length;\n                    readableSegmentCount++;\n                    totalEstimatedBytes = AddNonNegativeSaturated(totalEstimatedBytes, segmentLength);");
        AssertContains(sourceText, "bytesProcessed = AddNonNegativeSaturated(bytesProcessed, new FileInfo(segPath).Length);");
        AssertDoesNotContain(sourceText, "inPoint - segment.StartPts!.Value");
        AssertDoesNotContain(sourceText, " - segment.StartPts!.Value\n                        : TimeSpan.Zero;");
        AssertDoesNotContain(sourceText, "totalEstimatedBytes += new FileInfo(segment.Path).Length");
        AssertDoesNotContain(sourceText, "bytesProcessed += new FileInfo(segPath).Length");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestampOrMax(TimeSpan value)\n        => value == TimeSpan.MaxValue ? long.MaxValue : ToAvTimeBaseTimestamp(value);");
        AssertContains(sourceText, "private static long ToMicrosecondsSaturated(TimeSpan value)");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "if (videoStream != null && IsValidPositiveRational(videoStream->avg_frame_rate))");
        AssertContains(sourceText, "if (videoStream != null && IsValidPositiveRational(videoStream->r_frame_rate))");
        AssertContains(sourceText, "private static bool IsValidPositiveRational(AVRational value)\n        => value.num > 0 && value.den > 0;");
        AssertDoesNotContain(sourceText, "videoStream->avg_frame_rate.num > 0)");
        AssertDoesNotContain(sourceText, "videoStream->r_frame_rate.num > 0)");
        AssertContains(packetTimingText, "private static long ResolveFrameDurationUs(AVStream* videoStream)");
        AssertContains(packetTimingText, "private static long ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(packetTimingText, "private static bool TryResolveTimestampBase(AVPacket* packet, out long timestampBase)");
        AssertContains(packetTimingText, "private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet)");
        AssertContains(packetTimingText, "if (packet->pts != ffmpeg.AV_NOPTS_VALUE && packet->pts < 0)");
        AssertContains(packetTimingText, "if (packet->dts != ffmpeg.AV_NOPTS_VALUE && packet->dts < 0)");
        AssertContains(packetTimingText, "packet->pts != ffmpeg.AV_NOPTS_VALUE &&\n            packet->dts != ffmpeg.AV_NOPTS_VALUE &&\n            packet->pts < packet->dts");
        AssertContains(packetTimingText, "private long FlushBufferedPackets(");
        AssertContains(packetTimingText, "private static void FreeBufferedPackets(");
        AssertContains(packetTimingText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertEqual(3, sourceText.Split("NormalizePacketTimestampsBeforeWrite(", StringSplitOptions.None).Length - 2, "All export packet write paths normalize timestamps");
        AssertDoesNotContain(sourceText, "if (packet->pts < 0) packet->pts = 0;");
        AssertDoesNotContain(sourceText, "if (packet->dts < 0) packet->dts = 0;");
        AssertDoesNotContain(sourceText, "if (buffPkt->pts < 0) buffPkt->pts = 0;");
        AssertDoesNotContain(sourceText, "if (buffPkt->dts < 0) buffPkt->dts = 0;");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials()
    {
        var requestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketReadLoopText = requestsText;
        var singleFilePacketWritingText = singleFilePacketReadLoopText;
        var singleFilePacketWriteStateText = singleFilePacketReadLoopText;
        var singleFilePacketRebasingText = singleFilePacketReadLoopText;
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
            .Replace("\r\n", "\n");
        var segmentsText = segmentPacketWritingText;
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
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
        var outputFilesText = executionPolicyText;
        var validationText = executionPolicyText;
        var segmentValidationText = validationText;
        var libAvErrorsText = lifecycleText;
        var packetTimingText = segmentPacketWritingText;
        var streamsText = lifecycleText;
        var streamTemplatesText = streamsText;
        var timeMathText = packetTimingText;
        var packetBuffersText = packetTimingText;

        AssertEqual(
            1,
            segmentPacketWritingText.Split("internal sealed unsafe partial class FlashbackExporter", StringSplitOptions.None).Length - 1,
            "FlashbackExporter.SegmentPacketWriting.cs stays one in-file FlashbackExporter body");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SingleFilePacketReadLoop.cs")),
            "single-file packet pump folded into FlashbackExporter.Execution.cs");
        AssertDoesNotContain(singleFileText, "var timestampBasesUs = new long[streamCount];");
        AssertDoesNotContain(singleFileText, "LogTimestampBaseDrift(timestampBasesUs, hasTimestampBase);");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "TryValidateSegmentExportInputs(");
        AssertContains(segmentsText, "TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentsText, "WriteSegmentPacketsToActiveOutput(");
        AssertOccursBefore(segmentsText, "private FinalizeResult ExportSegmentsCore(", "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentsText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentsText, "var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);");
        AssertContains(segmentPacketWritingText, "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentPacketWritingText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentPacketWritingText, "var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);");
        AssertContains(segmentPacketWritingText, "WriteSegmentPacketReadLoop(");
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
        AssertContains(singleFileText, "av_write_trailer(_activeOutputContext)");
        AssertContains(singleFileText, "CloseOutputIo();\n\n        if (!TryFinalizeTempOutputFile");
        AssertContains(singleFileText, "if (!TryFinalizeActiveOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))");
        AssertDoesNotContain(segmentsText, "av_write_trailer(_activeOutputContext)");
        AssertDoesNotContain(segmentsText, "CloseOutputIo();\n\n            if (!TryFinalizeTempOutputFile");
        AssertContains(segmentsText, "if (!TryFinalizeActiveOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))");
        AssertDoesNotContain(segmentPacketWritingText, "private bool TryFinalizeActiveOutputFile(");
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
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.SegmentPacketWriting.cs");
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
            "FlashbackExporter.SegmentSelection.cs",
            "FlashbackExporter.Validation.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.Execution.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentValidation.cs")),
            "FlashbackExporter.SegmentValidation.cs folded into FlashbackExporter.Execution.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.Progress.cs",
            "FlashbackExporter.WriterPacing.cs",
            "FlashbackExporter.RuntimePolicy.cs",
            "FlashbackExporter.SingleFile.cs",
            "FlashbackExporter.OutputFiles.cs"
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
            "FlashbackExporter.SegmentPacketRebasing.cs folded into FlashbackExporter.SegmentPacketWriting.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentTemplate.cs")),
            "FlashbackExporter.SegmentTemplate.cs folded into FlashbackExporter.SegmentPacketWriting.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.Segments.cs")),
            "FlashbackExporter.Segments.cs folded into FlashbackExporter.SegmentPacketWriting.cs");

        return Task.CompletedTask;
    }
}
static partial class Program
{
    internal static Task FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation()
    {
        var sourceText = ReadFlashbackExporterSource();
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
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
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.SegmentPacketWriting.cs");
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
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs")
            .Replace("\r\n", "\n");
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs")
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.Streams.cs")),
            "FlashbackExporter.Streams.cs folded into FlashbackExporter.Lifecycle.cs");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_RejectsEmptySegmentPaths()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_empty_segment_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", " ");
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "empty-segment-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Empty segment path export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "segment path at index 0 is empty");
                AssertEqual(false, File.Exists(outputPath), "Empty segment path export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Empty segment path export does not leave temp output");

                var nullSegments = Array.CreateInstance(segmentType, 1);
                var nullSegmentOutputPath = Path.Combine(tempDir, "null-segment-export.mp4");
                var nullSegmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    nullSegments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    nullSegmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null for null segment.");

                AssertEqual(false, GetBoolProperty(nullSegmentResult, "Succeeded"), "Null segment export reports failure");
                AssertContains(GetStringProperty(nullSegmentResult, "StatusMessage"), "segment path at index 0 is empty");
                AssertEqual(false, File.Exists(nullSegmentOutputPath), "Null segment export does not create output");
                AssertEqual(false, File.Exists(nullSegmentOutputPath + ".tmp"), "Null segment export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsDuplicateSegmentPaths()
    {
        var sourceText = ReadFlashbackExporterSource();
        AssertContains(sourceText, "var duplicateSegmentIndex = FindDuplicateSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: duplicate segment path at index {duplicateSegmentIndex}.");
        AssertContains(sourceText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");

        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_duplicate_segment_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segmentPath = Path.Combine(tempDir, "segment-0.ts");
                File.WriteAllText(segmentPath, "segment");

                var firstSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(firstSegment, "Path", segmentPath);
                var duplicateSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(duplicateSegment, "Path", Path.Combine(tempDir, ".", "segment-0.ts"));

                var segments = Array.CreateInstance(segmentType, 2);
                segments.SetValue(firstSegment, 0);
                segments.SetValue(duplicateSegment, 1);
                var outputPath = Path.Combine(tempDir, "duplicate-segment-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Duplicate segment path export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "duplicate segment path at index 1");
                AssertEqual(false, File.Exists(outputPath), "Duplicate segment path export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Duplicate segment path export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_missing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", Path.Combine(tempDir, "missing-segment.ts"));
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "missing-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Missing segment export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "no readable segment files");
                AssertEqual(false, File.Exists(outputPath), "Missing segment export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Missing segment export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_OutputPathValidation_ReturnsFailure()
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

    internal static Task FlashbackExporter_RejectsOutputPathThatOverwritesSource()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_paths_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "fb_source_0001.mp4");
            File.WriteAllBytes(sourcePath, new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 });

            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    sourcePath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Single-file export rejects source overwrite");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Single-file rejection preserves source bytes");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", sourcePath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Segment export rejects source overwrite");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Segment rejection preserves source bytes");

                var outputPath = Path.Combine(tempDir, "fb_output.mp4");
                var tempSourcePath = outputPath + ".tmp";
                File.WriteAllBytes(tempSourcePath, new byte[] { 0x01, 0x02, 0x03, 0x04 });

                var tempSingleResult = exportCore.Invoke(exporter, new object?[]
                {
                    tempSourcePath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(tempSingleResult, "Succeeded"), "Single-file export rejects temp source overwrite");
                AssertContains(GetStringProperty(tempSingleResult, "StatusMessage"), "temporary output path must not overwrite source segment");
                AssertEqual(4L, new FileInfo(tempSourcePath).Length, "Single-file temp rejection preserves source bytes");

                var tempSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(tempSegment, "Path", tempSourcePath);
                var tempSegments = Array.CreateInstance(segmentType, 1);
                tempSegments.SetValue(tempSegment, 0);
                var tempSegmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    tempSegments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(tempSegmentResult, "Succeeded"), "Segment export rejects temp source overwrite");
                AssertContains(GetStringProperty(tempSegmentResult, "StatusMessage"), "temporary output path must not overwrite source segment");
                AssertEqual(4L, new FileInfo(tempSourcePath).Length, "Segment temp rejection preserves source bytes");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_temp_blocked_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "input.ts");
            File.WriteAllBytes(inputPath, new byte[] { 0x47 });

            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleOutputPath = Path.Combine(tempDir, "single-blocked.mp4");
                Directory.CreateDirectory(singleOutputPath + ".tmp");

                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    inputPath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    singleOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Single export rejects blocked temp output");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "temporary output path is a directory");
                AssertEqual(false, File.Exists(singleOutputPath), "Single blocked temp export does not create output");
                AssertEqual(true, Directory.Exists(singleOutputPath + ".tmp"), "Single blocked temp directory is preserved");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentOutputPath = Path.Combine(tempDir, "segment-blocked.mp4");
                Directory.CreateDirectory(segmentOutputPath + ".tmp");

                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    segmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Segment export rejects blocked temp output");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "temporary output path is a directory");
                AssertEqual(false, File.Exists(segmentOutputPath), "Segment blocked temp export does not create output");
                AssertEqual(true, Directory.Exists(segmentOutputPath + ".tmp"), "Segment blocked temp directory is preserved");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        cleanup.Invoke(null, new object[] { Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}") });

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles()
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

    internal static Task FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans()
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
            "    private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(segmentExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");
        AssertContains(segmentExportBlock, "TryPrepareTempOutputFile(tmpPath, outputPath, out var tempOutputFailure)");
        AssertContains(segmentExportBlock, "WriteSegmentPacketsToActiveOutput(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeTemp = exporterType.GetMethod("TryFinalizeTempOutputFile", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFile not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_finalize_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-export.mp4");
            var tmpPath = outputPath + ".tmp";
            var existingBytes = new byte[] { 0x65, 0x78, 0x70, 0x6f, 0x72, 0x74 };
            File.WriteAllBytes(outputPath, existingBytes);
            File.WriteAllBytes(tmpPath, Array.Empty<byte>());

            // Pass allowOverwrite=true so we exercise the empty-temp guard rather than
            // the destination-exists guard: the existing export must still be preserved
            // when the temp file itself is invalid.
            var args = new object?[] { tmpPath, outputPath, true, 0L, string.Empty };
            var finalized = (bool)(finalizeTemp.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFile returned null."));

            AssertEqual(false, finalized, "Invalid temp output is rejected");
            AssertContains((string)args[4]!, "temporary output file is empty before replacing");
            AssertEqual(true, File.Exists(outputPath), "Existing export remains present");
            AssertEqual(existingBytes.Length, new FileInfo(outputPath).Length, "Existing export length is preserved");
            AssertEqual(false, File.Exists(tmpPath), "Invalid temp output is deleted");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeTemp = exporterType.GetMethod("TryFinalizeTempOutputFile", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFile not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_refuse_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-take.mp4");
            var tmpPath = outputPath + ".tmp";
            var existingBytes = new byte[] { 0x66, 0x69, 0x72, 0x73, 0x74 };
            var freshTempBytes = new byte[] { 0x6e, 0x65, 0x77 };
            File.WriteAllBytes(outputPath, existingBytes);
            File.WriteAllBytes(tmpPath, freshTempBytes);

            // allowOverwrite=false means destination must be preserved, tmp must be deleted,
            // and a structured refusal message must surface in the out failureMessage.
            var args = new object?[] { tmpPath, outputPath, false, 0L, string.Empty };
            var finalized = (bool)(finalizeTemp.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFile returned null."));

            AssertEqual(false, finalized, "Refuse-on-collision rejects the overwrite");
            AssertContains((string)args[4]!, "destination file already exists");
            AssertEqual(true, File.Exists(outputPath), "Existing take is preserved on refusal");
            AssertEqual(existingBytes.Length, new FileInfo(outputPath).Length, "Existing take bytes are preserved on refusal");
            AssertEqual(false, File.Exists(tmpPath), "Temporary export is cleaned up on refusal");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_OverwritesWhenForceTrue()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeTemp = exporterType.GetMethod("TryFinalizeTempOutputFile", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFile not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_force_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-take.mp4");
            var tmpPath = outputPath + ".tmp";
            File.WriteAllBytes(outputPath, new byte[] { 0x6f, 0x6c, 0x64 });
            var freshTempBytes = new byte[] { 0x6e, 0x65, 0x77, 0x65, 0x72 };
            File.WriteAllBytes(tmpPath, freshTempBytes);

            var args = new object?[] { tmpPath, outputPath, true, 0L, string.Empty };
            var finalized = (bool)(finalizeTemp.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFile returned null."));

            AssertEqual(true, finalized, "Force=true overwrites the destination");
            AssertEqual(true, File.Exists(outputPath), "Destination remains present after overwrite");
            AssertEqual(freshTempBytes.Length, new FileInfo(outputPath).Length, "Destination contains the fresh export bytes");
            AssertEqual(false, File.Exists(tmpPath), "Temporary export was moved into place");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_FinalValidationFailureDeletesMovedOutput()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeCore = exporterType.GetMethod("TryFinalizeTempOutputFileCore", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFileCore not found.");
        var validatorType = exporterType.GetNestedType("CompletedOutputValidator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CompletedOutputValidator not found.");
        var validatorMethod = typeof(Program).GetMethod(
            nameof(ValidateFinalOutputFailureAfterMove),
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateFinalOutputFailureAfterMove not found.");
        var validator = Delegate.CreateDelegate(validatorType, validatorMethod);

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_final_validate_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            var tmpPath = outputPath + ".tmp";
            File.WriteAllBytes(tmpPath, new byte[] { 0x66, 0x69, 0x6e, 0x61, 0x6c });

            var args = new object?[] { tmpPath, outputPath, true, 0L, string.Empty, validator };
            var finalized = (bool)(finalizeCore.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFileCore returned null."));

            AssertEqual(false, finalized, "Final validation failure is rejected");
            AssertContains((string)args[4]!, "forced final validation failure");
            AssertEqual(false, File.Exists(tmpPath), "Temporary output was moved before final validation");
            AssertEqual(false, File.Exists(outputPath), "Invalid moved final output is deleted");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }
}

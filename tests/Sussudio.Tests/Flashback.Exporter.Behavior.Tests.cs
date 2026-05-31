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
}

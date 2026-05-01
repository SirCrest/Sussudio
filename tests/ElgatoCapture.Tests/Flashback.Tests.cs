using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    // ── FlashbackBufferOptions ──

    private static Task FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration()
    {
        var optionsType = RequireType("ElgatoCapture.Models.FlashbackBufferOptions");

        // 57 MB/s safety rate = 57 * 1024 * 1024 = 59768832 bytes/sec
        const long safetyBytesPerSecond = 57L * 1024 * 1024;

        var options = RuntimeHelpers.GetUninitializedObject(optionsType);

        // 5 minutes
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
        var maxBytes = (long)GetPropertyValue(options, "MaxDiskBytes")!;
        AssertEqual((long)(300.0 * safetyBytesPerSecond), maxBytes, "MaxDiskBytes for 5 minutes");

        // 1 minute
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(1));
        var oneMinBytes = (long)GetPropertyValue(options, "MaxDiskBytes")!;
        AssertEqual((long)(60.0 * safetyBytesPerSecond), oneMinBytes, "MaxDiskBytes for 1 minute");

        // Linear scaling: 5 min = 5 × 1 min
        AssertEqual(maxBytes, oneMinBytes * 5, "MaxDiskBytes linear scaling");

        SetPropertyBackingField(options, "BufferDuration", TimeSpan.Zero);
        AssertEqual(0L, (long)GetPropertyValue(options, "MaxDiskBytes")!, "MaxDiskBytes for zero duration");

        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromTicks(-1));
        AssertEqual(0L, (long)GetPropertyValue(options, "MaxDiskBytes")!, "MaxDiskBytes for negative duration");

        SetPropertyBackingField(options, "BufferDuration", TimeSpan.MaxValue);
        AssertEqual(long.MaxValue, (long)GetPropertyValue(options, "MaxDiskBytes")!, "MaxDiskBytes saturates huge duration");

        return Task.CompletedTask;
    }

    // ── FlashbackEncoderSink pure logic ──

    private static Task FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates()
    {
        var sinkType = RequireType("ElgatoCapture.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("ResolveFrameRateParts", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFrameRateParts not found.");

        // "60000/1001" → (60000, 1001)
        var result1 = method.Invoke(null, new object[] { "60000/1001" });
        var (num1, den1) = GetTupleValues(result1!);
        AssertEqual(60000, num1, "60000/1001 numerator");
        AssertEqual(1001, den1, "60000/1001 denominator");

        // "30/1" → (30, 1)
        var result2 = method.Invoke(null, new object[] { "30/1" });
        var (num2, den2) = GetTupleValues(result2!);
        AssertEqual(30, num2, "30/1 numerator");
        AssertEqual(1, den2, "30/1 denominator");

        // null → (null, null)
        var result3 = method.Invoke(null, new object?[] { null });
        var (num3, den3) = GetNullableTupleValues(result3!);
        if (num3 != null)
            throw new InvalidOperationException($"Expected null numerator for null input, got {num3}");

        // Empty string → (null, null)
        var result4 = method.Invoke(null, new object[] { "" });
        var (num4, den4) = GetNullableTupleValues(result4!);
        if (num4 != null)
            throw new InvalidOperationException($"Expected null numerator for empty input, got {num4}");

        return Task.CompletedTask;
    }

    private static (int, int) GetTupleValues(object tuple)
    {
        var item1 = tuple.GetType().GetField("Item1")?.GetValue(tuple);
        var item2 = tuple.GetType().GetField("Item2")?.GetValue(tuple);
        return (Convert.ToInt32(item1), Convert.ToInt32(item2));
    }

    private static (int?, int?) GetNullableTupleValues(object tuple)
    {
        var item1 = tuple.GetType().GetField("Item1")?.GetValue(tuple);
        var item2 = tuple.GetType().GetField("Item2")?.GetValue(tuple);
        return (item1 == null ? null : Convert.ToInt32(item1), item2 == null ? null : Convert.ToInt32(item2));
    }

    private static Task FlashbackEncoderSink_MapCodecName_MapsFormats()
    {
        var sinkType = RequireType("ElgatoCapture.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("MapCodecName", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapCodecName not found.");

        var formatType = RequireType("ElgatoCapture.Models.RecordingFormat");

        var hevc = method.Invoke(null, new[] { Enum.Parse(formatType, "HevcMp4") })?.ToString();
        AssertContains(hevc ?? "", "hevc");

        var h264 = method.Invoke(null, new[] { Enum.Parse(formatType, "H264Mp4") })?.ToString();
        AssertContains(h264 ?? "", "264");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_CountersDefaultToZero()
    {
        var sinkType = RequireType("ElgatoCapture.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("ElgatoCapture.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;

        AssertEqual(0L, GetLongProperty(sink, "DroppedVideoFrames"), "DroppedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "EncodedVideoFrames"), "EncodedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "AudioSamplesReceived"), "AudioSamplesReceived");

        return Task.CompletedTask;
    }

    // ── FlashbackExporter ──

    private static Task FlashbackEncoderSink_StartFailureRollsBackStartedState()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        var startCatchBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            /* Cleanup must not throw",
            "            throw;\n        }");

        AssertContains(sourceText, "ValidateSessionContext(context);");
        AssertContains(sourceText, "if (ptsBaseOffset < TimeSpan.Zero)\n        {\n            throw new ArgumentOutOfRangeException(nameof(ptsBaseOffset), \"PTS base offset must not be negative.\");\n        }");
        AssertOccursBefore(sourceText, "ValidateSessionContext(context);", "_started = true;");
        AssertOccursBefore(sourceText, "PTS base offset must not be negative.", "_started = true;");
        AssertContains(sourceText, "private static void ValidateSessionContext(FlashbackSessionContext context)");
        AssertContains(sourceText, "Flashback session width must be positive.");
        AssertContains(sourceText, "Flashback session height must be positive.");
        AssertContains(sourceText, "Flashback session codec name is required.");
        AssertContains(sourceText, "if (_started || _encodingTask is { IsCompleted: false })");
        AssertContains(startCatchBlock, "Logger.Log($\"FLASHBACK_SINK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(startCatchBlock, "lock (_sync)\n            {\n                _started = false;\n            }");
        AssertEqual(1, startCatchBlock.Split("_started = false;", StringSplitOptions.None).Length - 1, "Start failure rollback clears started state once");
        AssertOccursBefore(startCatchBlock, "_started = false;", "throw;");
        AssertContains(startCatchBlock, "_tsFilePath = null;\n            _recordingOutputPath = string.Empty;\n            _segmentStartPts = TimeSpan.Zero;\n            _segmentDuration = TimeSpan.Zero;\n            _ptsBaseOffset = TimeSpan.Zero;\n            Interlocked.Exchange(ref _segmentStartBytes, 0);");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        // Non-existent directory should not throw
        cleanup.Invoke(null, new object[] { Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}") });

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cleanup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var orphan1 = Path.Combine(tempDir, "clip_a.mp4.tmp");
            var orphan2 = Path.Combine(tempDir, "clip_b.mp4.tmp");
            var unrelated = Path.Combine(tempDir, "unrelated.mp4");
            var legacyTemp = Path.Combine(tempDir, "fb_export_temp_001.ts");

            File.WriteAllText(orphan1, "data");
            File.WriteAllText(orphan2, "data");
            File.WriteAllText(unrelated, "keep");
            File.WriteAllText(legacyTemp, "keep");

            cleanup.Invoke(null, new object[] { tempDir });

            AssertEqual(false, File.Exists(orphan1), "First mp4 temp deleted");
            AssertEqual(false, File.Exists(orphan2), "Second mp4 temp deleted");
            AssertEqual(true, File.Exists(unrelated), "Unrelated file preserved");
            AssertEqual(true, File.Exists(legacyTemp), "Legacy TS temp preserved by mp4 cleanup");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    // ── FlashbackPlaybackController ──

    private static Task FlashbackPlaybackController_InitialState_IsLive()
    {
        var bufferManagerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");

        var controller = ctor.Invoke(new[] { bufferManager });

        // State should be Live before Initialize
        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "Initial state is Live");

        // PlaybackPosition should be zero
        var position = (TimeSpan)GetPropertyValue(controller, "PlaybackPosition")!;
        AssertEqual(TimeSpan.Zero, position, "Initial PlaybackPosition");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_CommandsNoOpBeforeInitialize()
    {
        var bufferManagerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        // These should all no-op without throwing (IsReady is false)
        var playMethod = controllerType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);
        var pauseMethod = controllerType.GetMethod("Pause", BindingFlags.Public | BindingFlags.Instance);
        var goLiveMethod = controllerType.GetMethod("GoLive", BindingFlags.Public | BindingFlags.Instance);

        playMethod?.Invoke(controller, null);
        pauseMethod?.Invoke(controller, null);
        goLiveMethod?.Invoke(controller, null);

        // State should still be Live (commands were no-ops)
        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "State unchanged after no-op commands");

        return Task.CompletedTask;
    }

    // ── FlashbackExporter: early-exit error paths ──

    private static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportAsync not found.");

        var nonexistentInput = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.ts");
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            nonexistentInput,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            outputPath,
            true,
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
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("ElgatoCapture.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        // Create a real temp file so input validation passes
        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
        File.WriteAllBytes(tempInput, new byte[] { 0x47 }); // MPEG-TS sync byte
        try
        {
            var task = exportMethod.Invoke(exporter, new object?[]
            {
                tempInput,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                "",  // empty output path
                true,
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
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("ElgatoCapture.Models.FlashbackExportRequest");
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
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportSegmentsAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportSegmentsAsync not found.");

        var emptySegments = Array.Empty<string>();
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            emptySegments,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            outputPath,
            true,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportSegmentsAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export segments fails when no segments");
    }

    private static Task FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private readonly object _lifetimeSync = new();");
        AssertContains(sourceText, "return Task.FromResult(CreateDisposedExportResult(request.OutputPath));");
        AssertEqual(2, sourceText.Split("return Task.FromResult(CreateDisposedExportResult(outputPath));", StringSplitOptions.None).Length - 1, "Single and segment wrappers return disposed result");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            cancellationResult = CreateDisposedExportResult(outputPath);\n            return false;\n        }");
        AssertContains(sourceText, "linkedCts = CreateExportCancellationSource(ct);");
        AssertContains(sourceText, "var segmentSnapshot = SnapshotSegments(segments);");
        AssertContains(sourceText, "return ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, progress, linkedCts.Token);");
        AssertContains(sourceText, "private static IReadOnlyList<FlashbackExportSegment> SnapshotSegments(IReadOnlyList<FlashbackExportSegment>? segments)");
        AssertContains(sourceText, "snapshot[i] = segment == null\n                ? new FlashbackExportSegment { Path = string.Empty }\n                : segment with { };");
        AssertContains(sourceText, "CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposed, this);");
        AssertContains(sourceText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(sourceText, "const string message = \"Flashback exporter is disposed.\";");
        AssertContains(sourceText, "finally\n            {\n                DisposeLinkedCtsBestEffort(linkedCts, \"single_export\");\n            }\n        });");
        AssertContains(sourceText, "finally\n            {\n                DisposeLinkedCtsBestEffort(linkedCts, \"segment_export\");\n            }\n        });");
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

    private static async Task FlashbackExporter_RejectsNullRequests()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("ElgatoCapture.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", new[] { requestType, typeof(IProgress<>).MakeGenericType(RequireType("ElgatoCapture.Models.ExportProgress")), typeof(CancellationToken) })
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
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

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

        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
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

    private static Task FlashbackExporter_RejectsInvalidExportRanges()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackExporter_RejectsEmptySegmentPaths()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackExporter_RejectsDuplicateSegmentPaths()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        AssertContains(sourceText, "var duplicateSegmentIndex = FindDuplicateSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: duplicate segment path at index {duplicateSegmentIndex}.");
        AssertContains(sourceText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");

        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackExporter_ProgressCallbacksAreBestEffort()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "progress?.Report(new ExportProgress");
        AssertContains(sourceText, "using System.Diagnostics;");
        AssertContains(sourceText, "private const int ProgressHeartbeatIntervalMs = 1_000;");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_start\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, segments.Count, 0), \"segments_start\");");
        AssertContains(sourceText, "if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_heartbeat\");");
        AssertContains(sourceText, "ReportProgress(\n                                progress,\n                                new ExportProgress(\n                                    segIdx,\n                                    segments.Count,");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(1, 1, 100.0), \"single_complete\")");
        AssertContains(sourceText, "var outputBytes = GetFileLengthBestEffort(outputPath);");
        AssertContains(sourceText, "ReportProgress(\n                        progress,\n                        new ExportProgress(\n                            segIdx + 1,\n                            segments.Count,");
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

    private static Task FlashbackExporter_TimestampConversionsAreSaturating()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "TotalSeconds * ffmpeg.AV_TIME_BASE");
        AssertDoesNotContain(sourceText, "TotalMilliseconds * 1000)");
        AssertContains(sourceText, "var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);");
        AssertContains(sourceText, "var outPtsLimit = ToAvTimeBaseTimestampOrMax(outPoint);");
        AssertContains(sourceText, "var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);");
        AssertContains(sourceText, "? ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value))");
        AssertContains(sourceText, "var segmentOutDelta = useSegmentTimeline");
        AssertContains(sourceText, "? ToMicrosecondsSaturated(segmentOutDelta)");
        AssertContains(sourceText, "if (useSegmentTimeline && segmentOutDelta <= TimeSpan.Zero)");
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
        AssertContains(sourceText, "private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet)");
        AssertContains(sourceText, "if (packet->pts != ffmpeg.AV_NOPTS_VALUE && packet->pts < 0)");
        AssertContains(sourceText, "if (packet->dts != ffmpeg.AV_NOPTS_VALUE && packet->dts < 0)");
        AssertContains(sourceText, "packet->pts != ffmpeg.AV_NOPTS_VALUE &&\n            packet->dts != ffmpeg.AV_NOPTS_VALUE &&\n            packet->pts < packet->dts");
        AssertEqual(4, sourceText.Split("NormalizePacketTimestampsBeforeWrite(", StringSplitOptions.None).Length - 2, "All export packet write paths normalize timestamps");
        AssertDoesNotContain(sourceText, "if (packet->pts < 0) packet->pts = 0;");
        AssertDoesNotContain(sourceText, "if (packet->dts < 0) packet->dts = 0;");
        AssertDoesNotContain(sourceText, "if (buffPkt->pts < 0) buffPkt->pts = 0;");
        AssertDoesNotContain(sourceText, "if (buffPkt->dts < 0) buffPkt->dts = 0;");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_InputStreamCountsAreBounded()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private static bool TryGetInputStreamCount(");
        AssertContains(sourceText, "if (nativeStreamCount == 0)");
        AssertContains(sourceText, "if (nativeStreamCount > MaxSupportedInputStreams)");
        AssertContains(sourceText, "streamCount = (int)nativeStreamCount;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"single_export\", out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'\");");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_export\", out var currentStreamCount, out var streamCountFailure))");
        AssertContains(sourceText, "FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='invalid_stream_count'");
        AssertContains(sourceText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount)");
        AssertContains(sourceText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, inputNbStreams)");
        AssertContains(sourceText, "private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        var missingVideoBlock = ExtractTextBetween(
            sourceText,
            "if (videoStreamIndex < 0)",
            "                        var videoStream = _activeInputContext->streams[videoStreamIndex];");
        var incompleteVideoParamsBlock = ExtractTextBetween(
            sourceText,
            "var videoStream = _activeInputContext->streams[videoStreamIndex];",
            "                        CreateOutputContext(tmpPath, fastStart);");

        AssertDoesNotContain(missingVideoBlock, "streams[videoStreamIndex]");
        AssertContains(missingVideoBlock, "FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_stream_missing'");
        AssertContains(missingVideoBlock, "no usable video stream was found in any segment");
        AssertContains(incompleteVideoParamsBlock, "var videoStream = _activeInputContext->streams[videoStreamIndex];");
        AssertContains(incompleteVideoParamsBlock, "var videoHasValidParams = videoWidth > 0 && videoHeight > 0;");
        AssertContains(incompleteVideoParamsBlock, "no segment had complete video parameters");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackExporter_CancellationWinsBeforeValidation()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

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

    private static Task FlashbackExporter_RejectsOutputPathThatOverwritesSource()
    {
        var exporterType = RequireType("ElgatoCapture.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("ElgatoCapture.Models.FlashbackExportSegment");
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

    private static Task FlashbackPlaybackController_InOutPoints_DefaultToUnset()
    {
        var bufferManagerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        // InPoint and OutPoint properties
        var inPointProp = controllerType.GetProperty("InPoint", BindingFlags.Public | BindingFlags.Instance);
        var outPointProp = controllerType.GetProperty("OutPoint", BindingFlags.Public | BindingFlags.Instance);

        AssertNotNull(inPointProp, "FlashbackPlaybackController.InPoint");
        AssertNotNull(outPointProp, "FlashbackPlaybackController.OutPoint");
        foreach (var propertyName in new[]
                 {
                     "CommandsEnqueued",
                     "CommandsProcessed",
                     "CommandsDropped",
                     "CommandsSkippedNotReady",
                     "ScrubUpdatesCoalesced",
                     "PendingCommands",
                     "MaxPendingCommands",
                     "LastCommandQueueLatencyMs",
                     "MaxCommandQueueLatencyMs",
                     "LastCommandQueued",
                     "LastCommandProcessed",
                     "LastCommandQueuedUtcUnixMs",
                     "LastCommandProcessedUtcUnixMs",
                     "LastCommandFailure",
                     "PlaybackThreadAlive"
                 })
        {
            AssertNotNull(
                controllerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance),
                $"FlashbackPlaybackController.{propertyName}");
        }

        // ClearInOutPoints should not throw on a fresh controller
        var clearMethod = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(clearMethod, "FlashbackPlaybackController.ClearInOutPoints");
        clearMethod!.Invoke(controller, null);

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "var outTicks = Interlocked.Read(ref _outPointTicks);\n        if (outTicks != long.MinValue && outTicks <= pos.Ticks)\n        {\n            OutPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range\");\n        }");
        AssertContains(sourceText, "var inTicks = Interlocked.Read(ref _inPointTicks);\n        if (inTicks != long.MinValue && inTicks >= pos.Ticks)\n        {\n            InPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_IN invalid_range\");\n        }");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_InOutPointSettersNormalizeMarkers()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "set => Interlocked.Exchange(ref _inPointTicks, value.HasValue ? NormalizeMarkerPosition(value.Value).Ticks : long.MinValue);");
        AssertContains(sourceText, "set => Interlocked.Exchange(ref _outPointTicks, value.HasValue ? NormalizeMarkerPosition(value.Value).Ticks : long.MinValue);");
        AssertContains(sourceText, "private TimeSpan NormalizeMarkerPosition(TimeSpan position)\n    {\n        if (position <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }\n\n        var bufferDuration = _bufferManager.BufferedDuration;\n        return position > bufferDuration ? bufferDuration : position;\n    }");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_InOutPointChangesStopAfterDispose()
    {
        var bufferManagerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("ElgatoCapture.Services.Flashback.FlashbackPlaybackController");
        using var controller = (IDisposable)Activator.CreateInstance(controllerType, new[] { bufferManager })!;

        var setInPoint = controllerType.GetMethod("SetInPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetInPoint not found.");
        var setOutPoint = controllerType.GetMethod("SetOutPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetOutPoint not found.");
        var clearInOut = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.ClearInOutPoints not found.");

        setInPoint.Invoke(controller, null);
        controller.Dispose();
        clearInOut.Invoke(controller, null);
        setOutPoint.Invoke(controller, null);

        AssertEqual(TimeSpan.Zero, (TimeSpan?)GetPropertyValue(controller, "InPoint"), "Disposed clear should preserve existing in point");
        AssertEqual(null, GetPropertyValue(controller, "OutPoint"), "Disposed set out should not create a marker");

        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "var bufferDuration = _bufferManager.BufferedDuration;\n        var inTicks = Interlocked.Read(ref _inPointTicks);");
        AssertContains(sourceText, "var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);\n        if (max > bufferDuration) max = bufferDuration;\n        if (min > max) min = max;");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        const string clampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position) };\n                        decoder ??= CreateDecoder();\n                        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));";

        AssertEqual(3, sourceText.Split(clampBeforeOpen, StringSplitOptions.None).Length - 1, "Seek, BeginScrub, and UpdateScrub clamp before file lookup");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_TimestampArithmeticIsSaturating()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)");
        AssertDoesNotContain(sourceText, "cmd.Position + frozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + frozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + cmd.Delta");
        AssertDoesNotContain(sourceText, "bufferPosition + validStartPts");
        AssertDoesNotContain(sourceText, "pos + frozenValidStart");
        AssertDoesNotContain(sourceText, "nudgeFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "frame.Pts - validStartPts");
        AssertDoesNotContain(sourceText, "videoFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "latestAbsPts - lastFrameAbsPts");
        AssertDoesNotContain(sourceText, "absoluteLatestPts - absoluteFramePts");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "return HandleEndOfSegment(decoder, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);");
        AssertContains(sourceText, "TimeSpan frozenValidStart,\n        ref bool fileOpen,\n        CancellationToken cancellationToken)");
        AssertContains(sourceText, "if (cancellationToken.WaitHandle.WaitOne(50))\n        {\n            return false;\n        }");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'\");\n                    SnapToLiveOnError(decoder, ex, ref fileOpen);\n                    return false;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n                    SnapToLiveOnError(decoder, ex, ref fileOpen);\n                    return false;");
        AssertContains(sourceText, "if (nextFile != null && !IsSamePlaybackPath(nextFile, currentOpenFilePath))");
        AssertContains(sourceText, "_currentOpenFilePath = nextFile;\n                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";");
        AssertContains(sourceText, "decoder.OpenFile(currentOpenFilePath);\n                    fileOpen = true;\n                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var nearLiveBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP",
            "SetState(FlashbackPlaybackState.Live);");
        AssertContains(nearLiveBlock, "CloseDecoderFileBestEffort(decoder, \"near_live\");");
        AssertContains(nearLiveBlock, "fileOpen = false;\n            _currentOpenFilePath = null;\n            _decoderHwAccel = \"N/A\";");
        AssertContains(nearLiveBlock, "ReleasePlaybackFrameForLive(\"near_live\");\n            RestoreLiveAudio();");

        var decodeErrorBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK",
            "SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEEK_ERROR type={ex.GetType().Name} error='{ex.Message}'\");");
        AssertContains(decodeErrorBlock, "CloseDecoderFileBestEffort(decoder, \"decode_error\");");
        AssertContains(decodeErrorBlock, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(decodeErrorBlock, "ReleasePlaybackFrameForLive(\"decode_error\");\n        RestoreLiveAudio();");
        AssertContains(sourceText, "private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)\n    {\n        try\n        {\n            if (decoder.IsOpen) decoder.CloseFile();\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'\");\n        }\n    }");
        var ensureFileOpenBlock = ExtractTextBetween(
            sourceText,
            "private void EnsureFileOpen",
            "private void CleanupDecoder");
        AssertContains(ensureFileOpenBlock, "CloseDecoderFileBestEffort(decoder, \"ensure_file_open\");\n                fileOpen = false;\n                _currentOpenFilePath = null;\n                _decoderHwAccel = \"N/A\";");
        AssertContains(ensureFileOpenBlock, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n        {\n            SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n        }");
        AssertContains(sourceText, "case CommandKind.Stop:\n                        isPlaying = false;\n                        isScrubbing = false;\n                        CleanupDecoder(ref decoder, ref fileOpen);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);\n                        RestoreLiveAudio();\n                        SafeResumePreviewSubmission(\"thread_stop\");\n                        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive) return true;\n        if (!EnsurePlaybackThread(CommandKind.GoLive)) return false;\n        return SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });");
        AssertContains(sourceText, "private bool EnsurePlaybackThread(CommandKind commandKind)");
        AssertContains(sourceText, "private readonly object _playbackThreadSync = new();");
        AssertContains(sourceText, "lock (_playbackThreadSync)");
        AssertContains(sourceText, "if (_disposedFlag != 0) return RejectCommand(commandKind, \"disposed\", \"disposed\", false);");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_UPDATE_SKIP reason=disposed");
        AssertContains(sourceText, "private const int CommandQueueCapacity = 256;");
        AssertContains(sourceText, "public int CommandQueueCapacityCommands => CommandQueueCapacity;");
        AssertContains(sourceText, "private Channel<PlaybackCommand> _commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "private static Channel<PlaybackCommand> CreateCommandChannel()");
        AssertContains(sourceText, "Channel.CreateBounded<PlaybackCommand>");
        AssertContains(sourceText, "new BoundedChannelOptions(CommandQueueCapacity)");
        AssertContains(sourceText, "FullMode = BoundedChannelFullMode.Wait");
        AssertDoesNotContain(sourceText, "Channel.CreateUnbounded<PlaybackCommand>");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "DisposePlaybackCtsBestEffort(_playCts, \"thread_start_fail\");");
        AssertContains(sourceText, "_playbackThread = null;\n            Interlocked.Exchange(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "return RejectCommand(\n                commandKind,\n                $\"thread_start_failed:{ex.GetType().Name}:{ex.Message}\",\n                $\"thread_start_failed type={ex.GetType().Name}\",\n                false);");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n                        return;");
        AssertContains(sourceText, "var canRead = _commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            isScrubbing = false;\n                            CleanupDecoder(ref decoder, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"channel_closed\");\n                            SetState(FlashbackPlaybackState.Live);\n                            return;\n                        }");
        AssertContains(sourceText, "if (_disposedFlag != 0)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");\n                            isScrubbing = false;\n                            CleanupDecoder(ref decoder, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"thread_disposed\");\n                            SetState(FlashbackPlaybackState.Live);\n                            return;\n                        }");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n        }");
        AssertContains(sourceText, "finally\n        {\n            timeEndPeriod(1);");
        AssertContains(sourceText, "var threadExited = true;");
        AssertContains(sourceText, "if (ReferenceEquals(Thread.CurrentThread, thread))\n            {\n                Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self\");\n                _lastCommandFailure = \"thread_join_skipped:self\";\n                threadExited = false;\n            }");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT\");\n                _lastCommandFailure = \"thread_join_timeout\";\n                threadExited = false;");
        AssertContains(sourceText, "_lastCommandFailure = \"thread_join_skipped:self\";");
        AssertContains(sourceText, "_lastCommandFailure = \"thread_join_timeout\";");
        AssertContains(sourceText, "if (threadExited)\n        {\n            DisposePlaybackCtsBestEffort(_playCts, \"stop_thread\");");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);\n            Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);\n            Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "if (cts.IsCancellationRequested)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");\n                            CleanupDecoder(ref decoder, ref fileOpen);\n                            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n                            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n                            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);");
        AssertContains(sourceText, "PaceAndDecodeFrame(decoder, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token)");
        AssertContains(sourceText, "CancellationToken cancellationToken)\n    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)\n                    {\n                        cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"thread_cancelled\");");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");\n            CleanupDecoder(ref decoder, ref fileOpen);\n            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'\");\n            CleanupDecoder(ref decoder, ref fileOpen);\n            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);");
        AssertContains(sourceText, "var decoderToDispose = decoder;\n            decoder = null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose");
        AssertContains(sourceText, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(sourceText, "DrainAbandonedCommandsOnThreadExit();");
        AssertContains(sourceText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(sourceText, "if (string.IsNullOrEmpty(_lastCommandFailure))\n            {\n                _lastCommandFailure = $\"abandoned_on_exit:{abandoned}\";\n            }");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertContains(sourceText, "ReferenceEquals(Thread.CurrentThread, _playbackThread)");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "StopPlaybackThread();\n        _initialized = false;\n        Logger.Log(\"FLASHBACK_PLAYBACK_DISPOSED\");");
        AssertContains(sourceText, "if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)\n        {\n            return RejectCommand(command.Kind, \"disposed\", \"disposed\", false);\n        }");
        AssertContains(sourceText, "if (ReferenceEquals(cts, _playCts))\n            {\n                _playCts = null;\n            }\n            DisposePlaybackCtsBestEffort(cts, \"thread_exit\");");
        AssertContains(sourceText, "private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "_lastCommandQueued = command.Kind.ToString();\n        _lastCommandFailure = string.Empty;\n        return true;");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_DisposeResetsGpuQueueDepth()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(sourceText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(sourceText, "Interlocked.Exchange(ref queueDepth, 0);");
        AssertContains(sourceText, "var timeoutFailure = new TimeoutException(\"Flashback encode drain timed out while stopping.\");");
        AssertContains(sourceText, "_encodingFailure ??= timeoutFailure;");
        AssertContains(sourceText, "_encodingFailure ??= ex;");
        AssertContains(sourceText, "CancelEncodingCts(\"dispose\");");
        AssertContains(sourceText, "CancelEncodingCts(\"stop_timeout\");");
        AssertContains(sourceText, "private void CancelEncodingCts(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_CANCEL_WARN");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"start_fail\");");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"finalize_dispose\");");
        AssertContains(sourceText, "DisposeWorkAvailableBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"encoding_loop_fatal\");");
        AssertContains(sourceText, "FLASHBACK_SINK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_DISPOSE_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_ENCODER_DISPOSE_WARN");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'\");");
        AssertContains(sourceText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(sourceText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(sourceText, "FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN");
        AssertContains(sourceText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertContains(sourceText, "var current = Volatile.Read(ref target);");
        AssertContains(sourceText, "if (current <= 0)");
        AssertContains(sourceText, "if (Interlocked.CompareExchange(ref target, current - 1, current) == current)");
        AssertContains(sourceText, "FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _audioQueueDepth, \"audio\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _microphoneQueueDepth, \"microphone\");");
        AssertContains(sourceText, "private bool WaitForBackpressureRetryCancellation()");
        AssertContains(sourceText, "=> WaitForCancellation(TimeSpan.FromMilliseconds(1));");
        AssertContains(sourceText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(sourceText, "return cts.Token.WaitHandle.WaitOne(timeout);");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            return true;\n        }");
        AssertEqual(2, sourceText.Split("if (WaitForBackpressureRetryCancellation())", StringSplitOptions.None).Length - 1, "Video and GPU enqueue backpressure waits are cancellation-aware");
        AssertContains(sourceText, "if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))\n            {\n                return false;\n            }");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Decrement(ref target);");
        AssertDoesNotContain(sourceText, "Interlocked.Exchange(ref target, 0);\n        Logger.Log($\"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(sourceText, "Marshal.Release(packet.Texture);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private const int AudioInputBlockAlignBytes = 2 * sizeof(float);");
        AssertContains(sourceText, "private const int MaxAudioPacketBytes = 4 * 1024 * 1024;");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"audio\"))");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"microphone\"))");
        AssertContains(sourceText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertContains(sourceText, "if (byteLength <= 0 || byteLength > MaxAudioPacketBytes)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=size");
        AssertContains(sourceText, "if (byteLength % AudioInputBlockAlignBytes != 0)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=alignment");
        AssertContains(sourceText, "return byteLength > 0 ? byteLength / AudioInputBlockAlignBytes : 0;");
        AssertDoesNotContain(sourceText, "const int inputBlockAlign = 2 * sizeof(float);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(_encoder.NextVideoPts / frameRate)");
        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(_encoder.NextVideoPts / finalFrameRate)");
        AssertDoesNotContain(sourceText, "ptsBaseOffset.TotalSeconds * context.FrameRate");
        AssertContains(sourceText, "var sessionFrameRate = ResolveSessionFrameRate(context.FrameRate);");
        AssertContains(sourceText, "var sessionContext = context with { FrameRate = sessionFrameRate };");
        AssertContains(sourceText, "_encoder.Initialize(CreateOptions(sessionContext, tsPath));");
        AssertContains(sourceText, "_bufferManager.EncodeFrameRate = sessionFrameRate;");
        AssertContains(sourceText, "private const double FallbackSessionFrameRate = 30.0;");
        AssertContains(sourceText, "private const double MaxSessionFrameRate = 1000.0;");
        AssertContains(sourceText, "var currentPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var finalPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var crashPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var pts = ResolveEncoderPts();");
        AssertContains(sourceText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(sourceText, "var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(frameRate) || frameRate <= 0)\n        {\n            return FallbackSessionFrameRate;\n        }");
        AssertContains(sourceText, "return Math.Min(frameRate, MaxSessionFrameRate);");
        AssertContains(sourceText, "private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0 || fps > MaxSessionFrameRate)");
        AssertContains(sourceText, "FrameRateNumerator = frameRateNumerator,");
        AssertContains(sourceText, "FrameRateDenominator = frameRateDenominator,");
        AssertContains(sourceText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(sourceText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(sourceText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(sourceText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(\n        FlashbackBufferManager bufferManager,\n        string operation)");
        AssertContains(sourceText, "return bufferManager.ResumeEviction();");
        AssertContains(sourceText, "FLASHBACK_SINK_EVICTION_RESUME_WARN");
        AssertContains(sourceText, "return (bufferManager.RecordingStartPts, bufferManager.RecordingEndPts);");
        AssertContains(sourceText, "var finalSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(sourceText, "var crashSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(sourceText, "var segmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, Interlocked.Read(ref _segmentStartBytes));");
        AssertDoesNotContain(sourceText, "_encoder.TotalBytesWritten - Interlocked.Read(ref _segmentStartBytes)");
        AssertDoesNotContain(sourceText, "result.PreviousTotalBytes - Interlocked.Read(ref _segmentStartBytes)");
        AssertDoesNotContain(sourceText, "LastRecordingEndPts - LastRecordingStartPts");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PauseFromLive_DoesNotBlockOnExactSeek()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var pauseFromLiveBlock = ExtractTextBetween(
            sourceText,
            "else if (State == FlashbackPlaybackState.Live)",
            "                        break;\n\n                    case CommandKind.GoLive:");

        AssertContains(pauseFromLiveBlock, "SafeSuppressPreviewSubmission(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "SafePauseRendering(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "PlaybackPosition = pausePos;");
        AssertContains(pauseFromLiveBlock, "SetState(FlashbackPlaybackState.Paused);");
        AssertContains(pauseFromLiveBlock, "frozen_preview=true");
        AssertDoesNotContain(pauseFromLiveBlock, "EnsureFileOpen");
        AssertDoesNotContain(pauseFromLiveBlock, "SeekAndDisplayExactFrame");
        AssertDoesNotContain(sourceText, "private void SeekAndDisplayExactFrame");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0))");
        AssertContains(sourceText, "frameDuration = ResolveFrameDuration(decoder);");
        AssertContains(sourceText, "private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = decoder.FrameRate;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = FallbackPlaybackFrameRate;\n        }");
        AssertContains(sourceText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(sourceText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(sourceText, "fps = Math.Min(fps, MaxPlaybackFrameRate);");
        AssertContains(sourceText, "return TimeSpan.FromSeconds(1.0 / fps);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var nudgeBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.Nudge:",
            "                        break;\n                }");

        AssertContains(nudgeBlock, "decoder ??= CreateDecoder();");
        AssertContains(nudgeBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));");
        AssertContains(nudgeBlock, "if (!decoder.IsOpen)");
        AssertContains(nudgeBlock, "FLASHBACK_PLAYBACK_NUDGE_NO_FILE");
        AssertContains(nudgeBlock, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "RestoreLiveAudio();");
        AssertContains(nudgeBlock, "SafeResumePreviewSubmission(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SetState(FlashbackPlaybackState.Live);");
        AssertContains(nudgeBlock, "SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart);");
        AssertDoesNotContain(nudgeBlock, "if (decoder != null)");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "if (!TryValidatePreviewFrame(frame, out var skipReason))");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_{skipReason}\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
        AssertContains(sourceText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(sourceText, "reason = \"invalid_dimensions\";");
        AssertContains(sourceText, "reason = \"null_texture\";");
        AssertContains(sourceText, "reason = \"null_data\";");
        AssertContains(sourceText, "reason = \"invalid_data_length\";");
        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_submit_fail\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_previousHeldFrame, \"previous_frame\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip\");");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"seek_no_file\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_no_file\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_update_no_file\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"play_no_file\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"near_live\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"decode_error\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_FAIL");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(nudgeFrame, \"nudge\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(frame, \"seek\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(videoFrame, \"playback\")");
        AssertContains(sourceText, "if (!TrySubmitAndHoldFrame(videoFrame, \"playback\"))\n            {\n                SetState(FlashbackPlaybackState.Paused);\n                Logger.Log($\"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}\");\n                return false;\n            }");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n        try\n        {\n            SubmitFrame(frame);");
        AssertContains(sourceText, "SubmitFrame(frame);\n            ReleasePreviousHeldFrame();");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n            SubmitFrame(videoFrame);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeek(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan seekTarget, string reason)");
        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeekKeyframe(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan seekTarget, string reason)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_ERROR");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR");
        AssertContains(sourceText, "private static bool IsSamePlaybackPath(string? left, string? right)");
        AssertContains(sourceText, "Path.GetFullPath(left)");
        AssertContains(sourceText, "Path.GetFullPath(right)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PATH_COMPARE_WARN");
        AssertContains(sourceText, "&& IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)");
        AssertContains(sourceText, "if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))\n            return;");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Paused && IsSamePlaybackPath(prevFile, _currentOpenFilePath))");
        AssertContains(sourceText, "fileOpen = false;\n            _currentOpenFilePath = null;\n            return false;");
        AssertContains(sourceText, "_currentOpenFilePath = currentPath;\n            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";\n            return decoder.SeekTo(seekTarget);");
        AssertContains(sourceText, "_currentOpenFilePath = currentPath;\n            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";\n            return decoder.SeekToKeyframe(seekTarget);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeek(decoder, ref fileOpen, coalescedSeekTarget, \"seek\")");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeek(decoder, ref fileOpen, endScrubTarget, \"end_scrub\")");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, \"play\")");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, \"seek_keyframe\")");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath!)");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var seekBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.Seek:",
            "                    case CommandKind.BeginScrub:");

        AssertContains(seekBlock, "_commandChannel.Reader.TryPeek(out var newerSeek) &&\n                               newerSeek.Kind == CommandKind.Seek");
        AssertContains(seekBlock, "TrackCommandDequeued(newerSeek);");
        AssertContains(seekBlock, "FLASHBACK_PLAYBACK_SEEK");

        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.UpdateScrub:",
            "                    case CommandKind.EndScrub:");
        var updateScrubMethod = ExtractTextBetween(
            sourceText,
            "public bool UpdateScrub(TimeSpan position)",
            "    public bool EndScrub()");
        var drainAbandonedCommands = ExtractTextBetween(
            sourceText,
            "private void DrainAbandonedCommandsOnThreadExit()",
            "    // --- Decode helpers ---");

        AssertContains(sourceText, "private long _latestScrubUpdateTicks;");
        AssertContains(sourceText, "private int _scrubUpdateCommandQueued;");
        AssertContains(sourceText, "private long _scrubUpdatesCoalesced;");
        AssertContains(updateScrubMethod, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(updateScrubMethod, "if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, \"thread_not_running\", \"thread_not_running\", false);");
        AssertContains(updateScrubMethod, "Interlocked.CompareExchange(ref _scrubUpdateCommandQueued, 1, 0) != 0");
        AssertContains(updateScrubMethod, "TrackCoalescedScrubUpdate();");
        AssertContains(updateScrubMethod, "return true;");
        AssertContains(updateScrubMethod, "Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);");
        AssertContains(updateScrubMethod, "return false;");
        AssertContains(updateScrubBlock, "Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);");
        AssertContains(updateScrubBlock, "TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks))");
        AssertContains(updateScrubBlock, "_commandChannel.Reader.TryPeek(out var newer) &&\n                               newer.Kind == CommandKind.UpdateScrub");
        AssertContains(updateScrubBlock, "if (!_commandChannel.Reader.TryRead(out newer))");
        AssertContains(updateScrubBlock, "TrackCommandDequeued(newer);");
        AssertContains(updateScrubBlock, "cmd = newer;");
        AssertContains(updateScrubBlock, "FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE");
        AssertContains(updateScrubBlock, "SafeResumePreviewSubmission(\"scrub_update_no_file\")");
        AssertContains(updateScrubBlock, "SetState(FlashbackPlaybackState.Live)");
        AssertContains(drainAbandonedCommands, "Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive) return true;\n        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.EndScrub, \"thread_not_running\", \"thread_not_running\", false);");
        AssertContains(sourceText, "private bool RejectCommand(CommandKind kind, string failure, string reason, bool returnValue)\n    {\n        Interlocked.Increment(ref _commandsSkippedNotReady);\n        _lastCommandFailure = $\"{failure}:{kind}\";\n        Logger.Log($\"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}\");\n        return returnValue;\n    }");
        AssertContains(sourceText, "private void TrackCoalescedScrubUpdate()");
        AssertContains(sourceText, "Interlocked.Increment(ref _scrubUpdatesCoalesced);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SCRUB_COALESCED");
        AssertDoesNotContain(updateScrubBlock, "SendCommand(newer)");
        AssertDoesNotContain(updateScrubBlock, "Non-scrub command consumed");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private void SafeSuppressPreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafeResumePreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafePauseRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumeRendering(string operation)");
        AssertContains(sourceText, "private void SafeFlushPlayback(string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"scrub_no_file\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"go_live\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"decode_error\")");
        AssertContains(sourceText, "SafeFlushPlayback(\"restore_live_audio\")");
        AssertContains(sourceText, "SafeResumeRendering(\"play_no_file\")");
        AssertContains(sourceText, "if (_audioPlayback == null)\n        {\n            decoder.AudioChunkCallback = null;\n            return;\n        }");
        AssertDoesNotContain(sourceText, "_videoCapture?.SuppressPreviewSubmission();\n                        SuppressLiveAudio();\n                        _audioPlayback?.PauseRendering();");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ResetClearsDecodeMetrics()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var resetMetricsBlock = ExtractTextBetween(
            sourceText,
            "private void ResetPlaybackMetrics()",
            "private void RestoreAudioCallback");
        AssertContains(resetMetricsBlock, "lock (_playbackDecodeLock)");
        AssertContains(resetMetricsBlock, "Array.Clear(_playbackDecodeDurationsMs);");
        AssertContains(resetMetricsBlock, "_playbackDecodeDurationHead = 0;");
        AssertContains(resetMetricsBlock, "_playbackDecodeDurationCount = 0;");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_DiscardedAudioFramesAreUnreffed()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        var audioDecodeBlock = ExtractTextBetween(
            sourceText,
            "private void DecodeAndDeliverAudioPacket",
            "// ── Private: Frame Conversion");
        AssertContains(audioDecodeBlock, "if (callback == null)\n            {\n                ffmpeg.av_frame_unref(_audioFrame);\n                continue; // Codec advanced, but no delivery during seek/scrub\n            }");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_PtsConversionRejectsInvalidTimestamps()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(_videoFrame->pts, _videoTimeBase);");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(_audioFrame->pts, _audioTimeBase);");
        AssertContains(sourceText, "var timestampUs = ToAvTimeBaseTimestamp(target);");
        AssertContains(sourceText, "private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)");
        AssertContains(sourceText, "if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertDoesNotContain(sourceText, "(long)(target.TotalSeconds * ffmpeg.AV_TIME_BASE)");
        AssertDoesNotContain(sourceText, "var seconds = (double)_videoFrame->pts * _videoTimeBase.num / _videoTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "var seconds = (double)_audioFrame->pts * _audioTimeBase.num / _audioTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_InputStreamsAndFrameSizesAreBounded()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private const int MaxDecodedVideoDimension = 8192;");
        AssertContains(sourceText, "private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_formatCtx, out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "if (!IsValidStreamIndex(_videoStreamIndex, streamCount))");
        AssertContains(sourceText, "if (_audioStreamIndex >= 0 && !IsValidStreamIndex(_audioStreamIndex, streamCount))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_stream_index");
        AssertContains(sourceText, "ValidateVideoDimensions(_videoWidth, _videoHeight);");
        AssertContains(sourceText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(sourceText, "width > MaxDecodedVideoDimension");
        AssertContains(sourceText, "height > MaxDecodedVideoDimension");
        AssertContains(sourceText, "(width & 1) != 0");
        AssertContains(sourceText, "var pixels = (long)width * height;");
        AssertContains(sourceText, "if (bytes <= 0 || bytes > MaxDecodedVideoFrameBytes || bytes > int.MaxValue)");
        AssertDoesNotContain(sourceText, "return width * height * 2 + width * (height / 2) * 2;");
        AssertDoesNotContain(sourceText, "return width * height + width * (height / 2);");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_AudioOutputBuffersAreBounded()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;");
        AssertContains(sourceText, "if (inputSamples <= 0)\n        {\n            ffmpeg.av_frame_unref(_audioFrame);");
        AssertContains(sourceText, "maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size");
        AssertContains(sourceText, "private static int ToBoundedAudioSampleCount(long sampleCount)");
        AssertContains(sourceText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(sourceText, "var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var outputBytesNeeded = maxOutputSamples * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var validBytes = outputSamplesProduced * OutputAudioChannels * sizeof(float);");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_SoftwareFramePlanesAreValidated()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)");
        AssertContains(sourceText, "if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))");
        AssertContains(sourceText, "FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame");
        AssertContains(sourceText, "ffmpeg.av_frame_unref(_videoFrame);\n            return default;");
        AssertContains(sourceText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(sourceText, "width_mismatch frame={frame->width} expected={width}");
        AssertContains(sourceText, "height_mismatch frame={frame->height} expected={height}");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE");
        AssertContains(sourceText, "failure = $\"unsupported_format:{format}\";");
        AssertContains(sourceText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(sourceText, "var plane = (uint)planeIndex;");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_null\";");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_linesize:{frame->linesize[plane]}<{minLineSize}\";");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_HeldFrameCleanupIsBestEffort()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"seek_keyframe_pending\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_replace_best\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_superseded\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"close_pending\");");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_RejectsInitializeAfterDispose()
    {
        var decoderType = RequireType("ElgatoCapture.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var initialize = decoderType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.Initialize not found.");

        decoder.Dispose();

        try
        {
            initialize.Invoke(decoder, new object[] { IntPtr.Zero, IntPtr.Zero });
            throw new InvalidOperationException("Expected disposed decoder initialization to be rejected.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
        }

        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var initializeBlock = ExtractTextBetween(
            sourceText,
            "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)",
            "    /// <summary>\n    /// Opens a .ts or .mp4 file for decoding.");
        AssertContains(initializeBlock, "ThrowIfDisposed();");
        AssertOccursBefore(initializeBlock, "ThrowIfDisposed();", "if (_initialized)");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_ClearsAudioCallbackOnDispose()
    {
        var decoderType = RequireType("ElgatoCapture.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var callbackProperty = decoderType.GetProperty("AudioChunkCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.AudioChunkCallback not found.");

        var callbackType = callbackProperty.PropertyType;
        var callbackParameter = Expression.Parameter(callbackType.GetGenericArguments()[0], "chunk");
        var callback = Expression.Lambda(callbackType, Expression.Empty(), callbackParameter).Compile();
        callbackProperty.SetValue(decoder, callback);

        decoder.Dispose();

        AssertEqual(null, callbackProperty.GetValue(decoder), "Disposed decoder clears audio callback");

        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "    // ── Private: Initialization");
        AssertContains(disposeBlock, "AudioChunkCallback = null;");
        AssertOccursBefore(disposeBlock, "AudioChunkCallback = null;", "CloseFileCore();");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_RotateFailureRestoresActiveSegment()
    {
        var sinkText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var bufferText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");

        var rotateBlock = ExtractTextBetween(
            sinkText,
            "private bool RotateSegment(TimeSpan currentPts)",
            "    public IReadOnlyList<string> ForceRotateForExport");
        AssertContains(rotateBlock, "string? completedPath = null;");
        AssertContains(rotateBlock, "string? newPath = null;");
        AssertContains(rotateBlock, "completedPath = _tsFilePath;");
        AssertContains(rotateBlock, "newPath = _bufferManager.GenerateSegmentPath();");
        AssertContains(rotateBlock, "if (newPath != null)\n            {\n                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);\n            }");

        var abandonBlock = ExtractTextBetween(
            bufferText,
            "public void AbandonGeneratedSegmentPath",
            "    private static void CleanupStaleSessionDirectories");
        AssertContains(abandonBlock, "if (IsSameSegmentPath(_activeSegmentPath, generatedPath))");
        AssertContains(abandonBlock, "_activeSegmentPath = restoreActivePath;");
        AssertContains(abandonBlock, "_nextSegmentIndex--;");
        AssertContains(abandonBlock, "if (!IsSameSegmentPath(generatedPath, restoreActivePath))");
        AssertContains(abandonBlock, "TryDeleteFile(generatedPath);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateRejectsFailedEncoder()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public IReadOnlyList<string> ForceRotateForExport",
            "    private bool TryCancelPendingForceRotate");
        AssertContains(forceRotateBlock, "if (inPoint < TimeSpan.Zero || outPoint <= inPoint)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE", "var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);");
        AssertContains(forceRotateBlock, "if (_encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED");
        AssertContains(forceRotateBlock, "return Array.Empty<string>();");
        AssertContains(forceRotateBlock, "var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);");
        AssertContains(forceRotateBlock, "if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK", "_forceRotateTcs = tcs;");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        var loopBlock = ExtractTextBetween(
            sourceText,
            "if (Volatile.Read(ref _forceRotateRequested))",
            "                    madeProgress = true;\n                }");

        AssertContains(loopBlock, "localTcs = _forceRotateTcs;\n                        _forceRotateTcs = null;");
        AssertContains(loopBlock, "if (localTcs == null)\n                        {\n                            Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request\");\n                            madeProgress = true;\n                            continue;\n                        }");
        AssertOccursBefore(loopBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request", "while (DrainAudioPackets(audioQueue.Reader))");
        AssertContains(loopBlock, "if (localTcs.Task.IsCompleted)\n                        {\n                            Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed\");\n                            madeProgress = true;\n                            continue;\n                        }");
        AssertOccursBefore(loopBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed", "while (DrainAudioPackets(audioQueue.Reader))");
        AssertContains(loopBlock, "finally\n                    {\n                        lock (_videoQueueSync)\n                        {\n                            Volatile.Write(ref _forceRotateDraining, false);\n                        }\n                    }");

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public IReadOnlyList<string> ForceRotateForExport",
            "    private bool TryCancelPendingForceRotate");
        AssertContains(forceRotateBlock, "var clearedPending = TryCancelPendingForceRotate(tcs);\n            tcs.TrySetResult(Array.Empty<string>());");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        var fatalBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL",
            "            ReturnAllRemainingQueuedBuffers();");
        AssertContains(fatalBlock, "catch (Exception segmentEx)");
        AssertContains(fatalBlock, "FLASHBACK_SINK_FATAL_SEGMENT_REGISTER_FAIL");
        AssertContains(fatalBlock, "Preserve the original fatal error.");

        return Task.CompletedTask;
    }

    private static Task FlashbackSuppressedExceptionsUseAppLogs()
    {
        var decoderText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var sinkText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        var openFileBlock = ExtractTextBetween(
            decoderText,
            "public void OpenFile(string filePath)",
            "    /// <summary>\n    /// Closes the currently open file");
        AssertContains(openFileBlock, "FLASHBACK_DECODER_OPEN_WARN");
        AssertContains(openFileBlock, "CloseFileCore();\n            throw;");
        AssertContains(decoderText, "var closedPath = _currentFilePath;\n        CloseFileCore();\n        Logger.Log($\"FLASHBACK_DECODER_CLOSE path='{closedPath}'\");");
        AssertContains(decoderText, "_currentPosition = TimeSpan.Zero;\n        _currentFilePath = null;\n        _needsConvert = false;");
        AssertDoesNotContain(openFileBlock, "System.Diagnostics.Trace.TraceWarning");
        AssertContains(decoderText, "FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");

        var fileSizeBlock = ExtractTextBetween(
            sinkText,
            "private static long GetFileSize(string path)",
            "    private static string CreateSessionId()");
        AssertContains(fileSizeBlock, "FLASHBACK_SINK_FILE_SIZE_WARN");
        AssertContains(fileSizeBlock, "return 0;");
        AssertDoesNotContain(fileSizeBlock, "System.Diagnostics.Trace.TraceWarning");

        return Task.CompletedTask;
    }
}

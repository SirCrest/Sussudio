using System.Reflection;
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
        AssertContains(sourceText, "var linkedCts = CreateExportCancellationSource(ct);");
        AssertContains(sourceText, "CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposed, this);");
        AssertContains(sourceText, "finally\n            {\n                linkedCts.Dispose();\n            }\n        });");
        AssertDoesNotContain(sourceText, "}, linkedCts.Token);");
        AssertDoesNotContain(sourceText, "_disposeCts!.Token");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_OutputPathValidation_ReturnsFailure()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "if (!TryValidateOutputDirectory(outputPath, out var outputPathFailure))\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'\");\n            return FinalizeResult.Failure(outputPath, outputPathFailure);\n        }");
        AssertContains(sourceText, "private static bool TryValidateOutputDirectory(string outputPath, out string failureMessage)");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: output path is required.\";");
        AssertContains(sourceText, "catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output directory does not exist for '{outputPath}'.\";");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_ProgressCallbacksAreBestEffort()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "progress?.Report(new ExportProgress");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(1, 1, 100.0), \"single_complete\")");
        AssertContains(sourceText, "var outputBytes = GetFileLengthBestEffort(outputPath);");
        AssertContains(sourceText, "ReportProgress(\n                        progress,\n                        new ExportProgress(\n                            segIdx + 1,\n                            segments.Count,");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(segments.Count, segments.Count, 100.0), \"segments_complete\")");
        AssertContains(sourceText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)\n    {\n        try\n        {\n            progress?.Report(value);\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_PROGRESS_WARN stage={stage} type={ex.GetType().Name} msg='{ex.Message}'\");\n        }\n    }");
        AssertContains(sourceText, "private static long GetFileLengthBestEffort(string path)\n    {\n        try\n        {\n            return new FileInfo(path).Length;\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_WARN reason='output_length_unavailable' path='{path}' msg='{ex.Message}'\");\n            return -1;\n        }\n    }");

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

        var timeoutBlock = ExtractTextBetween(
            sourceText,
            "if (!lockAcquired)",
            "        try\n        {\n            CleanupNativeState();");

        AssertContains(timeoutBlock, "FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock");
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
        const string clampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position) };\n                        decoder ??= CreateDecoder();\n                        EnsureFileOpen(decoder, ref fileOpen, cmd.Position + frozenValidStart);";

        AssertEqual(3, sourceText.Split(clampBeforeOpen, StringSplitOptions.None).Length - 1, "Seek, BeginScrub, and UpdateScrub clamp before file lookup");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "return HandleEndOfSegment(decoder, pacingStopwatch, frozenValidStart, ref fileOpen);");
        AssertContains(sourceText, "TimeSpan frozenValidStart,\n        ref bool fileOpen)");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'\");\n                    SnapToLiveOnError(decoder, ex, ref fileOpen);\n                    return false;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n                    SnapToLiveOnError(decoder, ex, ref fileOpen);\n                    return false;");

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

        var decodeErrorBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK",
            "SetState(FlashbackPlaybackState.Live);");
        AssertContains(decodeErrorBlock, "CloseDecoderFileBestEffort(decoder, \"decode_error\");");
        AssertContains(decodeErrorBlock, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(sourceText, "private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)\n    {\n        try\n        {\n            if (decoder.IsOpen) decoder.CloseFile();\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'\");\n        }\n    }");
        var ensureFileOpenBlock = ExtractTextBetween(
            sourceText,
            "private void EnsureFileOpen",
            "private void CleanupDecoder");
        AssertContains(ensureFileOpenBlock, "CloseDecoderFileBestEffort(decoder, \"ensure_file_open\");\n                fileOpen = false;\n                _currentOpenFilePath = null;\n                _decoderHwAccel = \"N/A\";");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n        {\n            SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n        }");
        AssertContains(sourceText, "case CommandKind.Stop:\n                        isPlaying = false;\n                        isScrubbing = false;\n                        CleanupDecoder(ref decoder, ref fileOpen);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);\n                        RestoreLiveAudio();\n                        SafeResumePreviewSubmission(\"thread_stop\");\n                        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive) return true;\n        if (!EnsurePlaybackThread()) return false;\n        return SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });");
        AssertContains(sourceText, "private bool EnsurePlaybackThread()");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            _lastCommandFailure = $\"thread_start_failed:{ex.GetType().Name}:{ex.Message}\";\n            Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "_playbackThread = null;\n            Interlocked.Exchange(ref _playbackThreadStarted, 0);\n            return false;");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n                        return;");
        AssertContains(sourceText, "var canRead = _commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            isScrubbing = false;\n                            CleanupDecoder(ref decoder, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"channel_closed\");\n                            SetState(FlashbackPlaybackState.Live);\n                            return;\n                        }");
        AssertContains(sourceText, "if (_disposedFlag != 0)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");\n                            isScrubbing = false;\n                            CleanupDecoder(ref decoder, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"thread_disposed\");\n                            SetState(FlashbackPlaybackState.Live);\n                            return;\n                        }");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "finally\n        {\n            timeEndPeriod(1);");
        AssertContains(sourceText, "var threadExited = true;");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT\");\n                threadExited = false;");
        AssertContains(sourceText, "if (threadExited)\n        {\n            _playCts?.Dispose();");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);\n            Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);\n            Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "if (cts.IsCancellationRequested)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");\n                            CleanupDecoder(ref decoder, ref fileOpen);\n                            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n                            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n                            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);");
        AssertContains(sourceText, "PaceAndDecodeFrame(decoder, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token)");
        AssertContains(sourceText, "CancellationToken cancellationToken)\n    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)\n                    {\n                        cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"thread_cancelled\");");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");\n            CleanupDecoder(ref decoder, ref fileOpen);\n            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FATAL error='{ex.Message}'\");\n            CleanupDecoder(ref decoder, ref fileOpen);\n            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);");
        AssertContains(sourceText, "var decoderToDispose = decoder;\n            decoder = null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose");
        AssertContains(sourceText, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(sourceText, "DrainAbandonedCommandsOnThreadExit();");
        AssertContains(sourceText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(sourceText, "_lastCommandFailure = $\"abandoned_on_exit:{abandoned}\";");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertContains(sourceText, "ReferenceEquals(Thread.CurrentThread, _playbackThread)");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "if (ReferenceEquals(cts, _playCts))\n            {\n                _playCts = null;\n            }\n            cts.Dispose();");
        AssertContains(sourceText, "Volatile.Write(ref _playbackThreadStarted, 0);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_DisposeResetsGpuQueueDepth()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(sourceText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(sourceText, "Interlocked.Exchange(ref queueDepth, 0);");

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

    private static Task FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var nudgeBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.Nudge:",
            "                        break;\n                }");

        AssertContains(nudgeBlock, "decoder ??= CreateDecoder();");
        AssertContains(nudgeBlock, "EnsureFileOpen(decoder, ref fileOpen, nudgedPos + frozenValidStart);");
        AssertContains(nudgeBlock, "if (!decoder.IsOpen)");
        AssertContains(nudgeBlock, "FLASHBACK_PLAYBACK_NUDGE_NO_FILE");
        AssertContains(nudgeBlock, "RestoreLiveAudio();");
        AssertContains(nudgeBlock, "SafeResumePreviewSubmission(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SetState(FlashbackPlaybackState.Live);");
        AssertContains(nudgeBlock, "SeekAndDisplayKeyframe(decoder, nudgedPos, frozenValidStart);");
        AssertDoesNotContain(nudgeBlock, "if (decoder != null)");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FlashbackDecoder.ReleaseHeldFrame(frame);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_FAIL");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(nudgeFrame, \"nudge\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(frame, \"seek\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(videoFrame, \"playback\")");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n                SubmitFrame(frame);");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n            SubmitFrame(videoFrame);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeek(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan seekTarget, string reason)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_ERROR");
        AssertContains(sourceText, "fileOpen = false;\n            _currentOpenFilePath = null;\n            return false;");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeek(decoder, ref fileOpen, coalescedSeekTarget, \"seek\")");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeek(decoder, ref fileOpen, endScrubTarget, \"end_scrub\")");
        AssertContains(sourceText, "TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, \"play\")");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath!)");

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
        AssertContains(updateScrubMethod, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(updateScrubMethod, "if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, \"thread_not_running\", \"thread_not_running\", false);");
        AssertContains(updateScrubMethod, "Interlocked.CompareExchange(ref _scrubUpdateCommandQueued, 1, 0) != 0");
        AssertContains(updateScrubMethod, "Interlocked.Increment(ref _commandsDropped);");
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
        AssertContains(sourceText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"scrub_no_file\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"go_live\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"decode_error\")");
        AssertContains(sourceText, "SafeFlushPlayback(\"restore_live_audio\")");
        AssertContains(sourceText, "SafeResumeRendering(\"play_no_file\")");
        AssertDoesNotContain(sourceText, "_videoCapture?.SuppressPreviewSubmission();\n                        SuppressLiveAudio();\n                        _audioPlayback?.PauseRendering();");

        return Task.CompletedTask;
    }
}

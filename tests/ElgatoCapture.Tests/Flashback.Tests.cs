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

        AssertContains(sourceText, "var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts!.Token);");
        AssertContains(sourceText, "finally\n            {\n                linkedCts.Dispose();\n            }\n        });");
        AssertDoesNotContain(sourceText, "}, linkedCts.Token);");

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

    private static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n        {\n            SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n        }");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive) return;\n        EnsurePlaybackThread();\n        SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n                        return;");
        AssertContains(sourceText, "var canRead = _commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            return;\n                        }");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "finally\n        {\n            timeEndPeriod(1);");
        AssertContains(sourceText, "DrainAbandonedCommandsOnThreadExit();");
        AssertContains(sourceText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(sourceText, "_lastCommandFailure = $\"abandoned_on_exit:{abandoned}\";");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertContains(sourceText, "ReferenceEquals(Thread.CurrentThread, _playbackThread)");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "Volatile.Write(ref _playbackThreadStarted, 0);");

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

        AssertContains(pauseFromLiveBlock, "_videoCapture?.SuppressPreviewSubmission();");
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
        AssertContains(nudgeBlock, "SeekAndDisplayKeyframe(decoder, nudgedPos, frozenValidStart);");
        AssertDoesNotContain(nudgeBlock, "if (decoder != null)");

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

        AssertContains(updateScrubBlock, "_commandChannel.Reader.TryPeek(out var newer) &&\n                               newer.Kind == CommandKind.UpdateScrub");
        AssertContains(updateScrubBlock, "if (!_commandChannel.Reader.TryRead(out newer))");
        AssertContains(updateScrubBlock, "TrackCommandDequeued(newer);");
        AssertContains(updateScrubBlock, "cmd = newer;");
        AssertDoesNotContain(updateScrubBlock, "SendCommand(newer)");
        AssertDoesNotContain(updateScrubBlock, "Non-scrub command consumed");

        return Task.CompletedTask;
    }
}

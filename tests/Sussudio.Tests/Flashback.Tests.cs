using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static string ReadFlashbackPlaybackControllerSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Lifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PositionMapping.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.MetricsCollection.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrames.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.SeekDisplay.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackTiming.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Commands.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Thread.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCleanup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadTimer.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioPrebuffer.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackEncoderSinkSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.SegmentRotation.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Lifetime.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Options.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackExporterSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Requests.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifetime.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Infrastructure.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackDecoderSource()
    {
        var parts = new[]
        {
            // Keep audio first so source-shape checks still see audio delivery
            // before the root file's frame-conversion section marker.
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Timestamps.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Validation.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Diagnostics.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Guards.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.OutputTypes.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackBufferManagerSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    // ── FlashbackBufferOptions ──

    private static Task FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration()
    {
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");

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

    private static Task FlashbackBufferManager_InitializeClearsRecordingPts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_init_pts_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            var manager = Activator.CreateInstance(managerType, new[] { options })
                ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
            using var disposableManager = manager as IDisposable;

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-a" });
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(10) });
            managerType.GetMethod("PauseEviction")!.Invoke(manager, null);
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) });
            managerType.GetMethod("ResumeEviction")!.Invoke(manager, null);

            AssertEqual(TimeSpan.FromSeconds(10), (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts before reinitialize");
            AssertEqual(TimeSpan.FromSeconds(20), (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts before reinitialize");

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-b" });
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts resets on Initialize");
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts resets on Initialize");

            var activePath = (string)managerType.GetMethod("AcquireSegmentPath", Type.EmptyTypes)!.Invoke(manager, null)!;
            File.WriteAllBytes(activePath, new byte[] { 1, 2, 3, 4 });
            var segmentInfo = (System.Collections.IEnumerable)managerType.GetMethod("GetSegmentInfoList")!.Invoke(manager, null)!;
            var activeInfo = segmentInfo.Cast<object>().Single(info => (bool)GetPropertyValue(info, "IsActive")!);
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "StartPtsMs")!, "Active segment start PTS resets on Initialize");
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "EndPtsMs")!, "Active segment end PTS resets on Initialize");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
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
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("MapCodecName", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapCodecName not found.");

        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        var hevc = method.Invoke(null, new[] { Enum.Parse(formatType, "HevcMp4") })?.ToString();
        AssertContains(hevc ?? "", "hevc");

        var h264 = method.Invoke(null, new[] { Enum.Parse(formatType, "H264Mp4") })?.ToString();
        AssertContains(h264 ?? "", "264");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_CountersDefaultToZero()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;

        AssertEqual(0L, GetLongProperty(sink, "DroppedVideoFrames"), "DroppedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "EncodedVideoFrames"), "EncodedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "AudioSamplesReceived"), "AudioSamplesReceived");

        return Task.CompletedTask;
    }

    // ── FlashbackExporter ──

    private static Task FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var contextType = RequireType("Sussudio.Models.FlashbackSessionContext");
        var resolve = sinkType.GetMethod("ResolveVideoQueueCapacity", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveVideoQueueCapacity not found.");

        var fourKContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(fourKContext, "Width", 3840);
        SetPropertyBackingField(fourKContext, "Height", 2160);
        var normalContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(normalContext, "Width", 1920);
        SetPropertyBackingField(normalContext, "Height", 1080);

        AssertEqual(128, (int)resolve.Invoke(null, new[] { fourKContext, false })!, "4K CPU Flashback queue capacity");
        AssertEqual(180, (int)resolve.Invoke(null, new[] { fourKContext, true })!, "4K GPU Flashback queue capacity");
        AssertEqual(180, (int)resolve.Invoke(null, new[] { normalContext, false })!, "1080p CPU Flashback queue capacity");

        return Task.CompletedTask;
    }

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

    private static Task FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;
        var rejectReason = sinkType.GetMethod("GetVideoEnqueueRejectReason", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoEnqueueRejectReason not found.");

        try
        {
            SetPrivateField(sink, "_started", true);
            SetPrivateField(sink, "_forceRotateDraining", true);

            AssertEqual("force_rotate_draining", rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining rejects CPU video");
            AssertEqual("force_rotate_draining", rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining rejects GPU video");

            SetPrivateField(sink, "_forceRotateDraining", false);
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "CPU video accepted after force-rotate drain clears");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "GPU video accepted after force-rotate drain clears");
        }
        finally
        {
            (sink as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_StartFailureRollsBackStartedState()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

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
        AssertOccursBefore(sourceText, "_started = false;", "    public bool IsForceRotateActive =>");
        AssertContains(startCatchBlock, "_tsFilePath = null;\n            _recordingOutputPath = string.Empty;\n            _segmentStartPts = TimeSpan.Zero;\n            _segmentDuration = TimeSpan.Zero;\n            _ptsBaseOffset = TimeSpan.Zero;\n            Interlocked.Exchange(ref _segmentStartBytes, 0);");
        AssertContains(sourceText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(sourceText, "startupGeneratedSegmentPath = tsPath;");
        AssertContains(startCatchBlock, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(startCatchBlock, "else if (startupGeneratedSegmentPath != null)\n            {\n                _bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);\n            }");
        AssertOccursBefore(startCatchBlock, "DisposeEncoderBestEffort(\"start_fail\");", "_bufferManager.PurgeAllSegments();");
        AssertOccursBefore(startCatchBlock, "DisposeEncoderBestEffort(\"start_fail\");", "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");

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

    // ── FlashbackPlaybackController ──

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

    private static Task FlashbackPlaybackController_InitialState_IsLive()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
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
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
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

    private static Task FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        SetPrivateField(controller, "_initialized", true);

        try
        {
            SeedCommandFailure(controller, "old_failure:EndScrub");
            AssertEqual(false, (bool)controllerType.GetMethod("EndScrub")!.Invoke(controller, null)!, "EndScrub live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "EndScrub no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "EndScrub no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:GoLive");
            AssertEqual(false, (bool)controllerType.GetMethod("GoLive")!.Invoke(controller, null)!, "GoLive live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "GoLive no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "GoLive no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:Nudge");
            AssertEqual(false, (bool)controllerType.GetMethod("NudgePosition")!.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(8.33) })!, "Nudge live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Nudge no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Nudge no-op clears stale failure UTC");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");

        try
        {
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(1) })!, "Initial seek enqueues");
            var initialSeekQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:Seek");
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(2) })!, "Coalesced seek succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced seek clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced seek clears stale failure UTC");
            AssertEqual("Seek", GetStringProperty(controller, "LastCommandQueued"), "Coalesced seek keeps physical queued-command name");
            AssertEqual(initialSeekQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced seek does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "SeekCommandsCoalesced"), "Coalesced seek counter");

            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(3) })!, "Initial scrub update enqueues");
            var initialScrubQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:UpdateScrub");
            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(4) })!, "Coalesced scrub update succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced scrub update clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced scrub update clears stale failure UTC");
            AssertEqual("UpdateScrub", GetStringProperty(controller, "LastCommandQueued"), "Coalesced scrub update keeps physical queued-command name");
            AssertEqual(initialScrubQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced scrub update does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "ScrubUpdatesCoalesced"), "Coalesced scrub update counter");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static void SeedCommandFailure(object controller, string failure)
        => InvokeNonPublicInstanceMethod(controller, "SetLastCommandFailure", new object[] { failure });

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

        AssertContains(requestsText, "public Task<FinalizeResult> ExportAsync(");
        AssertContains(requestsText, "request.SegmentPaths.Select(path => new FlashbackExportSegment");
        AssertContains(lifetimeText, "public void Dispose()");
        AssertContains(lifetimeText, "FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
        AssertContains(singleFileText, "private FinalizeResult ExportCore(");
        AssertContains(singleFileText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "ReleaseExportLockBestEffort(\"segment_export\");");
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

    private static Task FlashbackExporter_RejectsInvalidExportRanges()
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

    private static Task FlashbackExportRejectedDiagnostics_PreserveAttemptedRange()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
                .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(captureServiceText, "if (inPointFilePts.HasValue || outPointFilePts.HasValue)");
        AssertContains(captureServiceText, "var absoluteInPoint = inPointFilePts ?? validStart;");
        AssertContains(captureServiceText, "var absoluteOutPoint = outPointFilePts ?? TimeSpan.MaxValue;");
        AssertContains(captureServiceText, "\"Flashback export in point has been evicted from the buffer.\"");
        AssertContains(captureServiceText, "\"Flashback export out point has been evicted from the buffer.\"");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\", inPoint, outPoint);");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_SNAPSHOT_FAIL op=range type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_SNAPSHOT_FAIL op=last_n type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceText, "_flashbackExportInPointMs = inPoint.HasValue ? (long)inPoint.Value.TotalMilliseconds : 0;");
        AssertContains(captureServiceText, "outPoint.Value == TimeSpan.MaxValue ? -1 : (long)outPoint.Value.TotalMilliseconds");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_RejectsEmptySegmentPaths()
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

    private static Task FlashbackExporter_RejectsDuplicateSegmentPaths()
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

    private static Task FlashbackExporter_ProgressCallbacksAreBestEffort()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertDoesNotContain(sourceText, "progress?.Report(new ExportProgress");
        AssertContains(sourceText, "using System.Diagnostics;");
        AssertContains(sourceText, "private const int ProgressHeartbeatIntervalMs = 1_000;");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_start\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, segments.Count, 0), \"segments_start\");");
        AssertContains(sourceText, "if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_heartbeat\");");
        var segmentExportLoopBlock = ExtractTextBetween(
            sourceText,
            "var segmentVideoFrameDurUs = 33333L;",
            "// Update cross-segment offset:");
        AssertContains(segmentExportLoopBlock, "ReportProgress(");
        AssertContains(segmentExportLoopBlock, "\"segment_heartbeat\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(1, 1, 100.0), \"single_complete\")");
        AssertContains(sourceText, "if (!TryFinalizeTempOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{outputFailure}'\");");
        AssertContains(sourceText, "return FinalizeResult.Failure(outputPath, outputFailure);");
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
        AssertContains(sourceText, "private static bool TryValidateCompletedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(sourceText, "outputBytes > 0");
        AssertContains(sourceText, "Flashback export failed: output file is empty");
        AssertContains(sourceText, "Flashback export failed: output file length unavailable");
        AssertContains(sourceText, "private static bool TryFinalizeTempOutputFile(");
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

    private static Task FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(sourceText, "FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);");
        AssertContains(sourceText, "FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);");
        AssertContains(sourceText, "bufferedStreamIndices?.Clear();");
        AssertContains(sourceText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
        AssertContains(sourceText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(sourceText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");

        var segmentLoopBlock = ExtractTextBetween(
            sourceText,
            "var segmentVideoFrameDurUs = 33333L;",
            "// Update cross-segment offset:");
        // The inline flush body was extracted into a local function FlushSegmentBufferedPackets
        // so the EOF rescue path can call it too. Both call sites must exist.
        AssertContains(segmentLoopBlock, "int FlushSegmentBufferedPackets(out bool stopFlushing)");
        AssertContains(segmentLoopBlock, "totalPackets += FlushSegmentBufferedPackets(out var stopFlushing);");
        AssertContains(segmentLoopBlock, "totalPackets += FlushSegmentBufferedPackets(out _);");
        // The local function's finally block must release buffered packets.
        AssertContains(segmentLoopBlock, "finally\n                        {\n                            FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);\n                        }");
        AssertOccursBefore(
            segmentLoopBlock,
            "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), \"av_interleaved_write_frame\");",
            "finally\n                        {\n                            FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);\n                        }");
        // EOF rescue: when Phase 1 never completed because some configured stream never
        // produced packets, flush whatever is buffered using a fallback base of 0 so we
        // do not silently discard video. (Was: bare FreeBufferedPackets that dropped video.)
        AssertContains(segmentLoopBlock, "if (!segAllBasesDiscovered && segBufferedPackets.Count > 0)");
        AssertContains(segmentLoopBlock, "segMinBaseUs ??= 0;");
        AssertContains(segmentLoopBlock, "FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx}");
        // The else branch still calls FreeBufferedPackets for the empty-buffer case.
        AssertContains(segmentLoopBlock, "FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);");

        var sharedFlushBlock = ExtractTextBetween(
            sourceText,
            "private long FlushBufferedPackets(",
            "private static void FreeBufferedPackets(");
        AssertContains(sharedFlushBlock, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertOccursBefore(
            sharedFlushBlock,
            "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), \"av_interleaved_write_frame\");",
            "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_TimestampConversionsAreSaturating()
    {
        var sourceText = ReadFlashbackExporterSource();

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
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private static bool TryGetInputStreamCount(");
        AssertContains(sourceText, "if (nativeStreamCount == 0)");
        AssertContains(sourceText, "if (nativeStreamCount > MaxSupportedInputStreams)");
        AssertContains(sourceText, "streamCount = (int)nativeStreamCount;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"single_export\", out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'\");");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_template\", out var candidateStreamCount, out var streamCountFailure))");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_export\", out var currentStreamCount, out var streamCountFailure))");
        AssertContains(sourceText, "FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='invalid_stream_count'");
        AssertContains(sourceText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount)");
        AssertContains(sourceText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount)");
        AssertContains(sourceText, "private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream()
    {
        var sourceText = ReadFlashbackExporterSource();

        var templateSelectionBlock = ExtractTextBetween(
            sourceText,
            "bool TryInitializeSegmentOutputTemplate(",
            "            if (!TryInitializeSegmentOutputTemplate");
        var incompleteVideoParamsBlock = ExtractTextBetween(
            sourceText,
            "var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];",
            "                        CreateOutputContext(tmpPath, fastStart);");

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
        AssertContains(sourceText, "var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(");
        AssertContains(sourceText, "reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
        AssertContains(sourceText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(sourceText, "inputCodec->codec_type != templateCodec->codec_type");
        AssertContains(sourceText, "inputCodec->codec_id != templateCodec->codec_id");
        AssertContains(sourceText, "private static bool VideoDimensionsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(sourceText, "return !inputHasCompleteDimensions && templateHasCompleteDimensions;");
        AssertContains(sourceText, "inputCodec->sample_rate != templateCodec->sample_rate");
        AssertContains(sourceText, "inputCodec->ch_layout.nb_channels != templateCodec->ch_layout.nb_channels");
        AssertContains(sourceText, "inputCodec->format != templateCodec->format");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped()
    {
        var sourceText = ReadFlashbackExporterSource();
        var segmentExportCore = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportSegmentsCore",
            "    private static long ResolveFrameDurationUs");

        AssertContains(segmentExportCore, "var skippedRequestedSegmentCount = 0;");
        AssertContains(segmentExportCore, "void TrackSkippedRequestedSegment(FlashbackExportSegment segment, string reason)");
        AssertContains(segmentExportCore, "SegmentOverlapsExportRange(segment, inPoint, outPoint)");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"not_found\");");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"invalid_stream_count\");");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"stream_count_mismatch\");");
        AssertContains(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"stream_layout_mismatch\");");
        AssertDoesNotContain(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"video_stream_missing\");");
        AssertDoesNotContain(segmentExportCore, "TrackSkippedRequestedSegment(segment, \"video_params_incomplete\");");
        AssertContains(segmentExportCore, "if (!TryInitializeSegmentOutputTemplate(out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))");
        AssertOccursBefore(segmentExportCore, "if (!TryInitializeSegmentOutputTemplate(out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))", "for (var segIdx = 0; segIdx < segments.Count; segIdx++)");
        AssertContains(segmentExportCore, "requested segment(s) were skipped");
        AssertOccursBefore(segmentExportCore, "if (skippedRequestedSegmentCount > 0)", "if (totalPackets == 0)");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled()
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

    private static Task FlashbackExporter_CancellationWinsBeforeValidation()
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

    private static Task FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone()
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

    private static Task FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState()
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

    private static Task FlashbackExporter_RejectsOutputPathThatOverwritesSource()
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

    private static Task FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport()
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

    private static Task FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse()
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

            // allowOverwrite=false → destination must be preserved, tmp must be deleted,
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

    private static Task FlashbackExporter_OverwritesWhenForceTrue()
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

    private static Task FlashbackExporter_FinalValidationFailureDeletesMovedOutput()
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

    private static bool ValidateFinalOutputFailureAfterMove(string outputPath, out long outputBytes, out string failureMessage)
    {
        if (outputPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            outputBytes = new FileInfo(outputPath).Length;
            failureMessage = string.Empty;
            return outputBytes > 0;
        }

        outputBytes = -1;
        failureMessage = $"forced final validation failure for '{outputPath}'";
        return false;
    }

    private static Task FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport()
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

    private static Task FlashbackPlaybackController_InOutPoints_DefaultToUnset()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
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
                     "LastCommandFailureUtcUnixMs",
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

        var sourceText = ReadFlashbackPlaybackControllerSource();
        AssertContains(
            sourceText,
            "var pending = Interlocked.Increment(ref _pendingCommands);\n        var droppedOldest = false;\n        var droppedCommand = default(PlaybackCommand);\n        if (!_commandChannel.Writer.TryWrite(queuedCommand) &&\n            (!IsCommandChannelOpenForDropRetry() ||\n             !TryDropOldestQueuedCommandForNewCommand(out droppedCommand) ||\n             !(droppedOldest = _commandChannel.Writer.TryWrite(queuedCommand))))\n        {\n            DecrementPendingCommands();");
        AssertContains(sourceText, "if (droppedOldest)\n        {\n            TrackDroppedQueuedCommand(droppedCommand, queuedCommand.Kind);\n        }");
        AssertContains(sourceText, "UpdateMaxPendingCommands(pending);");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "var outTicks = Interlocked.Read(ref _outPointTicks);\n        if (outTicks != long.MinValue && outTicks <= pos.Ticks)\n        {\n            OutPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range\");\n        }");
        AssertContains(sourceText, "var inTicks = Interlocked.Read(ref _inPointTicks);\n        if (inTicks != long.MinValue && inTicks >= pos.Ticks)\n        {\n            InPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_IN invalid_range\");\n        }");
        // SetInPointAt/SetOutPointAt accept an explicit user-intended position so
        // mid-GOP scrub clicks don't snap markers to the prior keyframe. Both
        // paths still default to PlaybackPosition when called without an override.
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        InPoint = pos;");
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        OutPoint = pos;");
        AssertContains(sourceText, "public TimeSpan SetInPoint() => SetInPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "public TimeSpan SetOutPoint() => SetOutPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "InPoint = null;\n        OutPoint = null;\n        ClearLastCommandFailure();");

        // UI must call the explicit-position overload so the marker matches the
        // visual playhead, not the controller's keyframe-snapped PlaybackPosition.
        var mainWindowFlashback = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");
        AssertContains(mainWindowFlashback, "ViewModel.FlashbackSetInPointAt(ViewModel.FlashbackPlaybackPosition)");
        AssertContains(mainWindowFlashback, "ViewModel.FlashbackSetOutPointAt(ViewModel.FlashbackPlaybackPosition)");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_InOutPointSettersNormalizeMarkers()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "private long _inPointFilePtsTicks = long.MinValue;");
        AssertContains(sourceText, "private long _outPointFilePtsTicks = long.MinValue;");
        AssertContains(sourceText, "Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(sourceText, "public TimeSpan? InPointFilePts");
        AssertContains(sourceText, "public TimeSpan? OutPointFilePts");
        AssertContains(sourceText, "public void RestoreInOutPoints(\n        TimeSpan? inPoint,\n        TimeSpan? outPoint,\n        TimeSpan? inPointFilePts,\n        TimeSpan? outPointFilePts)");
        AssertContains(sourceText, "Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);");
        AssertContains(sourceText, "private TimeSpan NormalizeMarkerPosition(TimeSpan position)\n    {\n        if (position <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }\n\n        var bufferDuration = _bufferManager.BufferedDuration;\n        return position > bufferDuration ? bufferDuration : position;\n    }");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_InOutPointChangesStopAfterDispose()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
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

        var sourceText = ReadFlashbackPlaybackControllerSource();
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetInPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetOutPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:ClearInOutPoints\");");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "var bufferDuration = _bufferManager.BufferedDuration;\n        var inTicks = Interlocked.Read(ref _inPointTicks);");
        AssertContains(sourceText, "var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);\n        if (max > bufferDuration) max = bufferDuration;");
        // Eviction-aware scrub clamp: ClampPosition(position, frozenValidStart) must
        // promote min to currentValidStart - frozenValidStart so a scrub-frozen
        // position 0 doesn't resolve to an evicted file PTS and snap-to-live.
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);");
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)");
        AssertContains(sourceText, "var currentValidStart = _bufferManager.ValidStartPts;");
        AssertContains(sourceText, "var evictedDelta = currentValidStart - frozenValidStart.Value;");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();
        // All three scrub-related command paths must clamp via the eviction-aware
        // overload so a long-held scrub doesn't resolve to evicted file PTS.
        const string seekClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n                        var seekResumeTarget = SaturatingAdd(cmd.Position, frozenValidStart);";
        const string scrubClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n                        decoder ??= CreateDecoder();\n                        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));";

        AssertContains(sourceText, seekClampBeforeOpen);
        AssertContains(sourceText, "if (ShouldYieldSeekToQueuedPlay(commandChannel))\n                        {\n                            PlaybackPosition = cmd.Position;\n                            pendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "decoder ??= CreateDecoder();\n                        EnsureFileOpen(decoder, ref fileOpen, seekResumeTarget);");
        AssertEqual(1, sourceText.Split(scrubClampBeforeOpen, StringSplitOptions.None).Length - 1, "BeginScrub clamps before file lookup with frozen reference");
        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.UpdateScrub:",
            "                    case CommandKind.EndScrub:");
        AssertContains(updateScrubBlock, "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n                        if (ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "decoder ??= CreateDecoder();\n                        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_TimestampArithmeticIsSaturating()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

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
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);");
        AssertContains(sourceText, "TimeSpan frozenValidStart,\n        ref bool fileOpen,\n        CancellationToken cancellationToken)");
        AssertContains(sourceText, "if (cancellationToken.WaitHandle.WaitOne(50))\n        {\n            return false;\n        }");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'\");\n                    SnapToLiveOnError(decoder, ex, ref fileOpen);\n                    return false;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n                    SnapToLiveOnError(decoder, ex, ref fileOpen);\n                    return false;");
        AssertContains(sourceText, "if (nextFile != null && !IsSamePlaybackPath(nextFile, currentOpenFilePath))");
        AssertContains(sourceText, "_currentOpenFilePath = nextFile;\n                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";");
        AssertContains(sourceText, "decoder.OpenFile(currentOpenFilePath);\n                    fileOpen = true;\n                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";");
        AssertContains(sourceText, "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)");
        AssertOccursBefore(
            sourceText,
            "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)",
            "if (gapFromLive > 2000)");
        AssertOccursBefore(
            sourceText,
            "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)",
            "FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertContains(sourceText, "private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);");
        AssertContains(sourceText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(sourceText, "CheckNearLiveEdge(decoder, videoFrame.Pts, newPosition, ref fileOpen)");
        AssertContains(sourceText, "var snapThreshold = requireFrameWarmup\n            ? ResolveContinuousPlaybackNearLiveSnapThreshold()\n            : RecoveryNearLiveSnapThreshold;");
        AssertContains(sourceText, "gapFromLive <= snapThreshold");
        AssertContains(sourceText, "private TimeSpan ResolveContinuousPlaybackNearLiveSnapThreshold()");
        AssertContains(sourceText, "ContinuousPlaybackNearLiveSnapFrames / Math.Min(fps, MaxPlaybackFrameRate)");
        AssertContains(sourceText, "threshold_ms={(long)snapThreshold.TotalMilliseconds}");
        AssertDoesNotContain(sourceText, "gapFromLive <= TimeSpan.FromMilliseconds(2000)");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

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
        AssertContains(sourceText, "SetLastCommandFailure($\"decode_error:{ex.GetType().Name}{FormatCommandDetail(position: pos)}\");");
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
        AssertContains(ensureFileOpenBlock, "if (string.IsNullOrWhiteSpace(filePath))\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_NO_FILE\");\n            if (decoder.IsOpen)\n            {\n                CloseDecoderFileBestEffort(decoder, \"ensure_file_open_no_file\");\n            }\n\n            fileOpen = false;\n            _currentOpenFilePath = null;\n            _decoderHwAccel = \"N/A\";\n            return;\n        }");
        AssertContains(ensureFileOpenBlock, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");\n            if (decoder.IsOpen)\n            {\n                CloseDecoderFileBestEffort(decoder, \"ensure_file_open_error\");\n            }\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(ensureFileOpenBlock, "private static bool IsDecoderFileReady(FlashbackDecoder decoder, bool fileOpen)\n        => fileOpen && decoder.IsOpen;");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(PlaybackPosition, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertEqual(6, sourceText.Split("if (!IsDecoderFileReady(decoder, fileOpen))", StringSplitOptions.None).Length - 1, "All EnsureFileOpen callers gate on fileOpen and decoder.IsOpen");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n            {\n                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n            }");
        AssertContains(sourceText, "case CommandKind.Stop:\n                            isPlaying = false;\n                            isScrubbing = false;\n                            pendingExactResumeTarget = null;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_stop\");");
        AssertContains(sourceText, "private void RestoreLiveForPlaybackThreadExit(");
        AssertContains(sourceText, "Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(sourceText, "_suppressAudioUntilPtsTicks");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.GoLive, \"live_thread_not_running\");\n            return false;\n        }");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.Nudge, \"live_thread_not_running\", delta: delta);\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
        AssertContains(sourceText, "private bool EnsurePlaybackThread(CommandKind commandKind)");
        AssertContains(sourceText, "private readonly object _playbackThreadSync = new();");
        AssertContains(sourceText, "lock (_playbackThreadSync)");
        AssertContains(sourceText, "if (_disposedFlag != 0) return RejectCommand(commandKind, \"disposed\", \"disposed\", false);");
        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0)\n        {\n            if (_playbackThread is { IsAlive: true })");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped\");\n            DrainAbandonedCommandsOnThreadExit(_commandChannel);");
        AssertContains(sourceText, "DisposePlaybackCtsBestEffort(_playCts, \"recover_stale_thread\");");
        AssertContains(sourceText, "Volatile.Write(ref _playbackThreadStarted, 0);\n        }\n\n        if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_UPDATE_SKIP reason=disposed");
        AssertContains(sourceText, "private const int CommandQueueCapacity = 256;");
        AssertContains(sourceText, "public int CommandQueueCapacityCommands => CommandQueueCapacity;");
        AssertContains(sourceText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "private Channel<PlaybackCommand> CreateCommandChannel()");
        AssertContains(sourceText, "Channel.CreateBounded<PlaybackCommand>");
        AssertContains(sourceText, "new BoundedChannelOptions(CommandQueueCapacity)");
        AssertContains(sourceText, "FullMode = BoundedChannelFullMode.Wait");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{detail} new_kind={newCommandKind} reason=channel_full");
        AssertContains(sourceText, "private void ClearQueuedCommandSlotForDroppedCommand(PlaybackCommand command)");
        AssertDoesNotContain(sourceText, "Channel.CreateUnbounded<PlaybackCommand>");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "DisposePlaybackCtsBestEffort(_playCts, \"thread_start_fail\");");
        AssertContains(sourceText, "_playbackThread = null;\n            Interlocked.Exchange(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "return RejectCommand(\n                commandKind,\n                $\"thread_start_failed:{ex.GetType().Name}:{ex.Message}\",\n                $\"thread_start_failed type={ex.GetType().Name}\",\n                false);");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n                        break;");
        AssertContains(sourceText, "var commandChannel = _commandChannel;");
        AssertContains(sourceText, "_playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))");
        AssertContains(sourceText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY");
        AssertContains(sourceText, "using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));");
        AssertContains(sourceText, "var canRead = commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"channel_closed\");");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");\n                            return;\n                        }");
        AssertContains(sourceText, "if (_disposedFlag != 0)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");
        AssertContains(sourceText, "finally\n        {\n            ClearPrebufferedFrames(prebufferedFrames, \"thread_exit\");\n            timeEndPeriod(1);");
        AssertContains(sourceText, "var threadExited = true;");
        AssertContains(sourceText, "if (ReferenceEquals(Thread.CurrentThread, thread))\n                {\n                    Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self\");\n                    SetLastCommandFailure(\"thread_join_skipped:self\");\n                    threadExited = false;\n                }");
        AssertContains(sourceText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(sourceText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT op={operation} timeout_ms={timeout.TotalMilliseconds:0}\");\n                    SetLastCommandFailure($\"thread_join_timeout:{operation}\");\n                    threadExited = false;");
        AssertContains(sourceText, "SetLastCommandFailure(\"thread_join_skipped:self\");");
        AssertContains(sourceText, "SetLastCommandFailure($\"thread_join_timeout:{operation}\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE op={operation} duration_ms=");
        AssertContains(sourceText, "thread_was_alive={threadWasAlive} thread_exited={threadExited}");
        AssertContains(sourceText, "active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###}");
        AssertContains(sourceText, "if (threadExited)\n            {\n                ApplyDeferredPreviewAttachAfterStopTimeout();\n                DisposePlaybackCtsBestEffort(_playCts, \"stop_thread\");");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);\n                ClearQueuedCommandSlotsBarrier();\n                Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_COMPLETE kind={cmd.Kind} duration_ms={commandElapsedMs:0.###}");
        AssertContains(sourceText, "private static string FormatActiveCommandKind(int rawKind)");
        AssertContains(sourceText, "private double GetActiveCommandElapsedMs(long nowTimestamp)");
        AssertContains(sourceText, "if (cts.IsCancellationRequested)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "PaceAndDecodeFrame(decoder, prebufferedFrames, commandChannel, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token)");
        AssertContains(sourceText, "TryDecodeNextVideoFrameWithMetrics(decoder, out var nudgeFrame, cts.Token)");
        AssertContains(sourceText, "CancellationToken cancellationToken)\n    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)\n                {\n                    cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "if (commandChannel.Reader.TryPeek(out _))\n                    {\n                        ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip_command_pending\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped}");
        AssertContains(sourceText, "const double FrameSkipThresholdMs = 500.0;");
        // Frame-skip catch-up loop must re-sync the audio clock each iteration so a
        // long catch-up burst does not extrapolate from a stale wall-time anchor.
        AssertContains(sourceText, "private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)");
        AssertContains(sourceText, "if (TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) &&\n                driftMs < -FrameSkipThresholdMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out driftMs))\n                    {\n                        break;\n                    }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH");
        AssertContains(sourceText, "nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250)");
        AssertContains(sourceText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            throw;\n        }\n        catch (Exception ex)\n        {\n            SnapToLiveOnError(decoder, ex, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_fatal\");");
        AssertContains(sourceText, "var decoderToDispose = decoder;\n            decoder = null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen}");
        AssertContains(sourceText, "release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
        AssertContains(sourceText, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(sourceText, "CompleteCommandChannelForThreadExit(commandChannel);\n            DrainAbandonedCommandsOnThreadExit(commandChannel);");
        AssertContains(sourceText, "private static void CompleteCommandChannelForThreadExit(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "commandChannel.Writer.TryComplete();");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN");
        AssertContains(sourceText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(sourceText, "if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))\n            {\n                SetLastCommandFailure($\"abandoned_on_exit:{abandoned}\");\n            }");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertContains(sourceText, "var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);");
        AssertContains(sourceText, "var ownsCts = ReferenceEquals(cts, _playCts);");
        AssertContains(sourceText, "if (ownsPlaybackThread)\n            {\n                _playbackThread = null;\n            }");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "StopPlaybackThread(PlaybackThreadStopTimeout, \"dispose\");\n        _initialized = false;\n        Logger.Log(\"FLASHBACK_PLAYBACK_DISPOSED\");");
        AssertContains(sourceText, "if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)\n        {\n            return RejectCommand(command.Kind, \"disposed\", \"disposed\", false);\n        }");
        AssertContains(sourceText, "if (ownsCts)\n            {\n                _playCts = null;\n            }\n            DisposePlaybackCtsBestEffort(cts, \"thread_exit\");");
        AssertContains(sourceText, "private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "if (ownsPlaybackThread || ownsCts)\n            {\n                Volatile.Write(ref _playbackThreadStarted, 0);\n            }");
        AssertContains(sourceText, "Interlocked.Increment(ref _commandsEnqueued);\n        UpdateMaxPendingCommands(pending);\n        MarkCommandQueued(command.Kind);\n        return true;");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");
        var queueCapacityProperty = controllerType.GetProperty("CommandQueueCapacityCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandQueueCapacityCommands not found.");
        var commandsDroppedProperty = controllerType.GetProperty("CommandsDropped", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandsDropped not found.");
        var pendingCommandsProperty = controllerType.GetProperty("PendingCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("PendingCommands not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var playKind = Enum.Parse(commandKindType, "Play");
        var goLiveKind = Enum.Parse(commandKindType, "GoLive");
        var capacity = (int)queueCapacityProperty.GetValue(controller)!;

        for (var i = 0; i < capacity; i++)
        {
            var playCommand = Activator.CreateInstance(commandType)
                ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
            SetPropertyOrBackingField(playCommand, "Kind", playKind);
            AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { playCommand })!, $"Play command {i} enqueues");
        }

        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Queue starts full");

        var goLiveCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand GoLive construction failed.");
        SetPropertyOrBackingField(goLiveCommand, "Kind", goLiveKind);
        AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { goLiveCommand })!, "Newest GoLive command is accepted when queue is full");
        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Drop-oldest accounting keeps pending bounded at capacity");

        var channel = commandChannelField.GetValue(controller)
            ?? throw new InvalidOperationException("Command channel missing.");
        var sawGoLive = false;
        while (TryReadQueuedPlaybackCommand(channel, commandType, out var command) && command != null)
        {
            if (GetPropertyValue(command, "Kind")?.ToString() == "GoLive")
            {
                sawGoLive = true;
            }
        }

        AssertEqual(true, sawGoLive, "Full command queue preserves the newest GoLive command");
        AssertEqual(true, (long)commandsDroppedProperty.GetValue(controller)! > 0, "Dropped-command diagnostics record the evicted older command");

        return Task.CompletedTask;

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }
    }

    private static Task FlashbackEncoderSink_DisposeResetsGpuQueueDepth()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

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
        AssertContains(sourceText, "private void SignalWork(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_SKIPPED");
        AssertContains(sourceText, "SignalWork(\"force_rotate_idle\");");
        AssertContains(sourceText, "SignalWork(\"force_rotate_request\");");
        AssertEqual(1, sourceText.Split("_workAvailable.Set();", StringSplitOptions.None).Length - 1, "All work-signal wakeups go through SignalWork");
        AssertContains(sourceText, "FLASHBACK_SINK_ENCODER_DISPOSE_WARN");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'\");");
        AssertContains(sourceText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(sourceText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(sourceText, "FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN");
        AssertContains(sourceText, "public long VideoQueueRejectedFrames => Interlocked.Read(ref _videoQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastVideoQueueRejectReason => Volatile.Read(ref _lastVideoQueueRejectReason);");
        AssertContains(sourceText, "public long GpuQueueRejectedFrames => Interlocked.Read(ref _gpuQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastGpuQueueRejectReason => Volatile.Read(ref _lastGpuQueueRejectReason);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _videoQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastVideoQueueRejectReason, null);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _gpuQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastGpuQueueRejectReason, null);");
        AssertContains(sourceText, "private const double ForceRotateQueueGuardRatio = 0.65;");
        AssertContains(sourceText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(sourceText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(sourceText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(sourceText, "return \"force_rotate_draining\";");
        AssertDoesNotContain(sourceText, "return \"force_rotate_queue_guard\";");
        AssertContains(sourceText, "private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)");
        AssertContains(sourceText, "queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio)");
        AssertContains(sourceText, "return \"cancelled\";");
        AssertContains(sourceText, "return \"disposed\";");
        AssertContains(sourceText, "return \"not_started\";");
        AssertContains(sourceText, "return \"queue_null\";");
        AssertContains(sourceText, "return \"invalid_expected_size\";");
        AssertContains(sourceText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(sourceText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"invalid_subresource\");");
        AssertContains(sourceText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(sourceText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(sourceText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(sourceText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(sourceText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(sourceText, "total == 1 || total % 30 == 0");
        AssertContains(sourceText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(sourceText, "var depth = Interlocked.Increment(ref _videoQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(sourceText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(sourceText, "var depth = Interlocked.Increment(ref _gpuQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(sourceText, "private static bool TryWriteAudioPacket(");
        AssertContains(sourceText, "Interlocked.Increment(ref queueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(sourceText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertContains(sourceText, "var current = Volatile.Read(ref target);");
        AssertContains(sourceText, "if (current <= 0)");
        AssertContains(sourceText, "if (Interlocked.CompareExchange(ref target, current - 1, current) == current)");
        AssertContains(sourceText, "FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _audioQueueDepth, \"audio\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _microphoneQueueDepth, \"microphone\");");
        AssertDoesNotContain(sourceText, "private bool WaitForBackpressureRetryCancellation()");
        AssertDoesNotContain(sourceText, "=> WaitForCancellation(TimeSpan.FromMilliseconds(1));");
        AssertContains(sourceText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(sourceText, "return cts.Token.WaitHandle.WaitOne(timeout);");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            return true;\n        }");
        AssertDoesNotContain(sourceText, "if (WaitForBackpressureRetryCancellation())");
        AssertContains(sourceText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_VIDEO_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_GPU_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FailEncoding(overloadFailure);");
        AssertDoesNotContain(sourceText, "Flashback recording video queue overloaded after");
        AssertDoesNotContain(sourceText, "Flashback GPU recording queue overloaded after");
        AssertContains(sourceText, "if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))\n            {\n                return false;\n            }");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Decrement(ref target);");
        AssertDoesNotContain(sourceText, "Interlocked.Exchange(ref target, 0);\n        Logger.Log($\"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth))");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth))");
        AssertDoesNotContain(sourceText, "queue.Writer.TryWrite(packet))\n        {\n            Interlocked.Increment(ref queueDepth);");
        AssertDoesNotContain(sourceText, "Marshal.Release(packet.Texture);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

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

    private static Task FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int VideoDrainBatchLimit = 24;");
        AssertContains(sourceText, "private const int GpuDrainBatchLimit = 16;");
        AssertContains(sourceText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(sourceText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(sourceText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets)");
        AssertContains(sourceText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");

        var loopBlock = ExtractTextBetween(
            sourceText,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "    private bool DrainVideoPackets");
        AssertOccursBefore(loopBlock, "DrainAudioPackets(audioQueue.Reader)", "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertOccursBefore(loopBlock, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)", "// Audio AGAIN");
        var secondAudioDrainBlock = ExtractTextBetween(
            loopBlock,
            "// Audio AGAIN",
            "// Handle force-rotate requests");
        AssertContains(secondAudioDrainBlock, "DrainAudioPackets(audioQueue.Reader)");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

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

    private static Task FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();
        var publicPauseBlock = ExtractTextBetween(
            sourceText,
            "public bool Pause()",
            "    public bool GoLive()");

        var pauseFromLiveBlock = ExtractTextBetween(
            sourceText,
            "else if (State == FlashbackPlaybackState.Live)",
            "                        break;\n\n                    case CommandKind.GoLive:");

        AssertContains(publicPauseBlock, "return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });");
        AssertDoesNotContain(publicPauseBlock, "SeekAndDisplay");
        AssertContains(pauseFromLiveBlock, "SafeSuppressPreviewSubmission(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "SafePauseRendering(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "var pauseTarget = ResolvePauseFromLiveTarget(frozenValidStart);");
        AssertContains(pauseFromLiveBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(pausePos, frozenValidStart));");
        AssertContains(pauseFromLiveBlock, "if (!IsDecoderFileReady(decoder, fileOpen))");
        AssertContains(pauseFromLiveBlock, "SetNoFileFailure(CommandKind.Pause, pausePos);");
        AssertContains(pauseFromLiveBlock, "if (ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(pauseFromLiveBlock, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(pauseFromLiveBlock, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, pausePos, frozenValidStart, CommandKind.Pause, cts.Token))");
        AssertContains(pauseFromLiveBlock, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"pause_from_live_display_failed\");");
        AssertContains(pauseFromLiveBlock, "pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(pauseFromLiveBlock, "SetState(FlashbackPlaybackState.Paused);");
        AssertContains(pauseFromLiveBlock, "frozen_frame=true");
        AssertContains(sourceText, "private TimeSpan ResolvePauseFromLiveTarget(TimeSpan frozenValidStart)");
        AssertContains(sourceText, "var backoff = TimeSpan.FromSeconds(1.0 / fps);");
        AssertContains(sourceText, "return latestPts - backoff;");
        AssertDoesNotContain(pauseFromLiveBlock, "SeekAndDisplayExactFrame");
        AssertDoesNotContain(sourceText, "private void SeekAndDisplayExactFrame");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0))");
        AssertContains(sourceText, "frameDuration = ResolveFrameDuration(decoder);");
        AssertContains(sourceText, "private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = decoder.FrameRate;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = FallbackPlaybackFrameRate;\n        }");
        AssertContains(sourceText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(sourceText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(sourceText, "fps = Math.Min(fps, MaxPlaybackFrameRate);");
        AssertContains(sourceText, "_playbackTargetFps = fps;");
        AssertContains(sourceText, "public double PlaybackTargetFps => _playbackTargetFps;");
        AssertContains(sourceText, "return TimeSpan.FromSeconds(1.0 / fps);");
        AssertContains(sourceText, "TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);");
        AssertContains(sourceText, "private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH");
        AssertContains(sourceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackPtsCadenceMismatchCount, 0);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var track = controllerType.GetMethod("TrackDecodedPtsCadence", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TrackDecodedPtsCadence not found.");
        var reset = controllerType.GetMethod("ResetPlaybackMetrics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResetPlaybackMetrics not found.");
        var expected = TimeSpan.FromMilliseconds(1000.0 / 120.0);

        try
        {
            track.Invoke(controller, new object[] { expected, expected });
            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 60.0), expected });
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "matching decoded PTS cadence count");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(1L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "slow decoded PTS cadence count");
            AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "slow decoded PTS cadence delta");
            AssertNearlyEqual(expected.TotalMilliseconds, GetDoubleProperty(controller, "LastPlaybackPtsCadenceExpectedMs"), 0.1, "decoded PTS expected cadence");
            if (GetLongProperty(controller, "LastPlaybackPtsCadenceMismatchUtcUnixMs") <= 0)
            {
                throw new InvalidOperationException("Expected decoded PTS cadence mismatch timestamp to be populated.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(2L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "duplicate decoded PTS cadence count");
            AssertNearlyEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "duplicate decoded PTS cadence delta");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(25.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "backward decoded PTS cadence count");
            if (GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs") >= 0)
            {
                throw new InvalidOperationException("Expected backward decoded PTS cadence delta to be negative.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 24.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "valid cadence after backward PTS remains clean");

            reset.Invoke(controller, null);
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "decoded PTS cadence reset count");
            AssertEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), "decoded PTS cadence reset delta");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        var nudgeBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.Nudge:",
            "                        break;\n                    }\n                }\n                finally");

        AssertContains(nudgeBlock, "decoder ??= CreateDecoder();");
        AssertContains(nudgeBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));");
        AssertContains(nudgeBlock, "if (!IsDecoderFileReady(decoder, fileOpen))");
        AssertContains(nudgeBlock, "FLASHBACK_PLAYBACK_NUDGE_NO_FILE");
        AssertContains(nudgeBlock, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "RestoreLiveAudio();");
        AssertContains(nudgeBlock, "SafeResumePreviewSubmission(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SafeResumeRendering(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SetState(FlashbackPlaybackState.Live);");
        AssertContains(nudgeBlock, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token))");
        AssertContains(nudgeBlock, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"nudge_display_failed\");");
        AssertDoesNotContain(nudgeBlock, "if (decoder != null)");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "if (!TryValidatePreviewFrame(frame, out var skipReason))");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackSubmitFailures);");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:{skipReason}\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_{skipReason}\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
        AssertContains(sourceText, "public long PlaybackSubmitFailures => Interlocked.Read(ref _playbackSubmitFailures);");
        AssertContains(sourceText, "public long LastSubmitFailureUtcUnixMs => Interlocked.Read(ref _lastSubmitFailureUtcUnixMs);");
        AssertContains(sourceText, "public string LastSubmitFailure => Volatile.Read(ref _lastSubmitFailure);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackSubmitFailures, 0);");
        AssertContains(sourceText, "ClearLastSubmitFailure();");
        AssertContains(sourceText, "public void UpdatePreviewComponents(IPreviewFrameSink? previewSink, ILiveVideoSource? videoCapture)");
        AssertContains(sourceText, "TryDeferPreviewAttachAfterStopTimeoutUnsafe(previewSink, videoCapture, \"update\")");
        AssertContains(sourceText, "_initialized = previewSink != null && videoCapture != null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_UPDATE sink={previewSink != null} capture={videoCapture != null}");
        AssertContains(sourceText, "ApplyPreviewRoutingForState(\"preview_update\");");
        AssertContains(sourceText, "public void PrepareForPreviewDetach()");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_DETACH state={_state} thread_alive={PlaybackThreadAlive}");
        AssertContains(sourceText, "if (!StopPlaybackThread(PreviewDetachThreadStopTimeout, \"preview_detach\"))\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_PREVIEW_DETACH_ABORT reason=thread_stop_failed\");\n            RestoreLiveAudio();\n            SafeResumePreviewSubmission(\"preview_detach_timeout\");\n            DetachPreviewComponentsAfterStopTimeout();\n            return;\n        }\n\n        ReleasePlaybackFrameForLive(\"preview_detach\");");
        AssertOccursBefore(sourceText, "SafeResumePreviewSubmission(\"preview_detach_timeout\");", "DetachPreviewComponentsAfterStopTimeout();\n            return;");
        AssertOccursBefore(sourceText, "DetachPreviewComponentsAfterStopTimeout();\n            return;", "ReleasePlaybackFrameForLive(\"preview_detach\");");
        AssertContains(sourceText, "RestoreLiveAudio();\n        SafeResumePreviewSubmission(\"preview_detach\");\n        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "private void DetachPreviewComponentsAfterStopTimeout()");
        AssertContains(sourceText, "Volatile.Write(ref _previewDetachStopTimeoutActive, 1);");
        AssertContains(sourceText, "_pendingPreviewSinkAfterDetachTimeout = null;\n            _pendingVideoCaptureAfterDetachTimeout = null;");
        AssertContains(sourceText, "_previewSink = null;\n            _videoCapture = null;\n            _initialized = false;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_DETACH_DEFER_OWNED_CLEANUP reason=thread_alive");
        AssertContains(sourceText, "private bool TryDeferPreviewAttachAfterStopTimeoutUnsafe(");
        AssertContains(sourceText, "_pendingPreviewSinkAfterDetachTimeout = previewSink;\n        _pendingVideoCaptureAfterDetachTimeout = videoCapture;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER op={operation} reason=thread_alive_after_detach_timeout");
        AssertContains(sourceText, "private void ApplyDeferredPreviewAttachAfterStopTimeout()");
        AssertContains(sourceText, "Monitor.TryEnter(_playbackThreadSync, 0, ref lockTaken);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLY_SKIP reason=lock_busy");
        AssertContains(sourceText, "ScheduleDeferredPreviewAttachApplyRetry();");
        AssertContains(sourceText, "private void ScheduleDeferredPreviewAttachApplyRetry()");
        AssertContains(sourceText, "Interlocked.CompareExchange(ref _deferredPreviewAttachApplyRetryScheduled, 1, 0)");
        AssertContains(sourceText, "await Task.Delay(25).ConfigureAwait(false);");
        AssertContains(sourceText, "if (Volatile.Read(ref _previewDetachStopTimeoutActive) != 0)");
        AssertContains(sourceText, "Volatile.Write(ref _previewDetachStopTimeoutActive, 0);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _deferredPreviewAttachApplyRetryScheduled, 0);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLIED reason=thread_exit");
        AssertContains(sourceText, "ApplyPreviewRoutingForState(\"deferred_preview_attach\");");
        AssertContains(sourceText, "private void ApplyPreviewRoutingForState(string operation)");
        AssertContains(sourceText, "var previewSink = Volatile.Read(ref _previewSink);");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:missing_preview_sink\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_missing_preview_sink\");");
        AssertContains(sourceText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(sourceText, "reason = \"invalid_dimensions\";");
        AssertContains(sourceText, "reason = \"null_texture\";");
        AssertContains(sourceText, "reason = \"invalid_subresource\";");
        AssertContains(sourceText, "reason = \"null_data\";");
        AssertContains(sourceText, "reason = \"invalid_data_length\";");
        AssertContains(sourceText, "reason = \"short_data_length\";");
        AssertContains(sourceText, "private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)");
        AssertContains(sourceText, "var calculated = isHdr\n            ? pixels * 3\n            : pixels + width * (long)(height / 2);");
        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:submit_fail:{ex.GetType().Name}\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_submit_fail\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_previousHeldFrame, \"previous_frame\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip\");");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)\n    {\n        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"seek_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Seek, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.BeginScrub, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_update_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.UpdateScrub, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"play_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Play, PlaybackPosition);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Nudge, nudgedPos);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"near_live\");");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"decode_error\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_FAIL");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(nudgeFrame, \"nudge\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(frame, \"seek\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(videoFrame, \"playback\")");
        AssertContains(sourceText, "var countForPresentCadence = string.Equals(operation, \"playback\", StringComparison.Ordinal);");
        AssertContains(sourceText, "var submitTick = Stopwatch.GetTimestamp();");
        AssertContains(sourceText, "var previewPresentId = Interlocked.Increment(ref _playbackPreviewPresentId);");
        AssertContains(sourceText, "SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);");
        AssertContains(sourceText, "sourceSequenceNumber: -1");
        AssertContains(sourceText, "previewPresentId: previewPresentId");
        AssertContains(sourceText, "sourcePtsTicks: frame.Pts.Ticks");
        AssertContains(sourceText, "countForPresentCadence: countForPresentCadence");
        AssertContains(sourceText, "arrivalTick: submitTick");
        AssertContains(sourceText, "schedulerSubmitTick: submitTick");
        AssertDoesNotContain(sourceText, "frame.Width, frame.Height, frame.IsHdr, arrivalTick: 0");
        AssertContains(sourceText, "if (!TrySubmitAndHoldFrame(videoFrame, \"playback\"))\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}\");\n                RestoreLiveAfterPlaybackSubmitFailure(decoder, ref fileOpen, \"playback_submit_failed\");\n                return false;\n            }");
        AssertContains(sourceText, "private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SafeResumeRendering(operation);\n        SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n        try\n        {\n            SubmitFrame(frame);");
        AssertContains(sourceText, "SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);\n            ReleasePreviousHeldFrame();");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n            SubmitFrame(videoFrame);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeek(");
        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeekKeyframe(");
        AssertContains(sourceText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(sourceText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(sourceText, "private bool ShouldSkipActiveFmp4ReopenNearLive(TimeSpan seekTarget, string reason)");
        AssertContains(sourceText, "var latestPts = _bufferManager.LatestPts;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_ERROR");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR");
        AssertContains(sourceText, "private bool TrySeekAdjacentSegmentStart(");
        AssertContains(sourceText, "var nextPath = _bufferManager.GetNextSegmentFile(currentPath);");
        AssertContains(sourceText, "var nextStart = _bufferManager.GetSegmentStartPts(nextPath);");
        AssertContains(sourceText, "if (targetGap > AdjacentSegmentSeekFallbackWindow)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR");
        AssertContains(sourceText, "private static bool IsSamePlaybackPath(string? left, string? right)");
        AssertContains(sourceText, "Path.GetFullPath(left)");
        AssertContains(sourceText, "Path.GetFullPath(right)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PATH_COMPARE_WARN");
        AssertContains(sourceText, "&& IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)");
        AssertContains(sourceText, "if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))\n            return;");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Paused &&\n                            IsSamePlaybackPath(prevFile, _currentOpenFilePath) &&\n                            !requireExactResumeSeek)");
        AssertContains(sourceText, "fileOpen = false;\n            _currentOpenFilePath = null;\n            return false;");
        AssertContains(sourceText, "private bool TrySeekWithActiveFmp4Reopen(");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n        {\n            return true;\n        }");
        AssertContains(sourceText, "private bool SeekToWithCapTelemetry(");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackSeekForwardDecodeCapHits)");
        AssertContains(sourceText, "if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))\n            {\n                SetReopenFailure(reason, \"near_live\", seekTarget);\n                return false;\n            }\n\n            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);");
        AssertContains(sourceText, "if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))\n        {\n            return true;\n        }\n\n        SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "if (decoder.SeekToKeyframe(seekTarget, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"keyframe_seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(sourceText, "SetReopenFailure(reason, \"no_current_file\", seekTarget);");
        AssertContains(sourceText, "SetReopenFailure(reason, ex.GetType().Name, seekTarget);");
        AssertContains(sourceText, "private void SetReopenFailure(string reason, string detail, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"reopen_failed:{reason}:{detail}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token))");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token))");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token))");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"no_file\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"seek_failed\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"submit_failed\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"no_frame\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, ex.GetType().Name, bufferPosition);");
        AssertContains(sourceText, "private bool SeekAndDisplayKeyframe(");
        var seekDisplayBlock = ExtractTextBetween(
            sourceText,
            "private bool SeekAndDisplayKeyframe(",
            "    private void RecordSeekDisplayDecodeFailure");
        AssertContains(seekDisplayBlock, "CancellationToken cancellationToken");
        AssertContains(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(seekDisplayBlock, "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertContains(seekDisplayBlock, "TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken)");
        AssertContains(seekDisplayBlock, "var frameOwned = gotFrame;");
        AssertContains(seekDisplayBlock, "frameOwned = false;");
        AssertContains(seekDisplayBlock, "ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\")");
        AssertContains(seekDisplayBlock, "if (frameOwned)\n                {\n                    ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\");\n                }");
        AssertContains(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(seekDisplayBlock, "throw;");
        AssertOccursBefore(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();", "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertOccursBefore(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", "catch (Exception ex)");
        AssertContains(seekDisplayBlock, "TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $\"seek_display:{kind}\", out var adjacentFilePts, cancellationToken)");
        AssertContains(seekDisplayBlock, "RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);");
        AssertContains(sourceText, "private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)");
        AssertContains(sourceText, "RecordPlaybackDroppedFrame(\"seek_display_no_frame\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE");
        AssertContains(sourceText, "return gotFrame;");
        AssertContains(sourceText, "private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "CloseDecoderFileBestEffort(decoder, operation);\n        fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";\n        ReleasePlaybackFrameForLive(operation);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SafeResumeRendering(operation);\n        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"seek_display_failed\");");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"begin_scrub_display_failed\");");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"scrub_update_display_failed\");");
        AssertContains(sourceText, "private void SetSeekDisplayFailure(CommandKind kind, string detail, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"seek_display_failed:{kind}:{detail}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "TimeSpan? pendingExactResumeTarget = null;");
        AssertContains(sourceText, "var seekResumeTarget = SaturatingAdd(cmd.Position, frozenValidStart);");
        AssertContains(sourceText, "var coalescedSeekTarget = seekResumeTarget;");
        AssertContains(sourceText, "pendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "var pendingPlayTarget = pendingExactResumeTarget ?? SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(sourceText, "var requireExactResumeSeek = pendingExactResumeTarget.HasValue;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RESUME_EXACT_SEEK");
        AssertContains(sourceText, "if (ShouldYieldSeekToQueuedPlay(commandChannel))");
        AssertContains(sourceText, "MarkCommandNoOp(CommandKind.Seek, \"superseded_by_play\", cmd.Position);");
        AssertContains(sourceText, "if (ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token))");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token))");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token))");
        AssertContains(sourceText, "if (!ShouldSkipActiveFmp4ReopenNearLive(filePts, \"seek_keyframe\"))\n                    {\n                        Logger.Log($\"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}\");\n                        if (TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, \"seek_keyframe\", cancellationToken))\n                            goto seekSuccess;\n                    }");
        AssertContains(sourceText, "SetReopenFailure(\"segment_switch\", \"seek_failed\", segSwitchTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"segment_switch_seek_failed\");");
        AssertContains(sourceText, "SetReopenFailure(\"fmp4_reopen\", \"seek_failed\", resumeTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"fmp4_reopen_seek_failed\");");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath!)");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        var seekBlock = ExtractTextBetween(
            sourceText,
            "case CommandKind.Seek:",
            "                    case CommandKind.BeginScrub:");

        AssertContains(seekBlock, "commandChannel.Reader.TryPeek(out var newerSeek) &&\n                               newerSeek.Kind == CommandKind.Seek");
        AssertContains(seekBlock, "TrackCommandDequeued(newerSeek);");
        AssertContains(seekBlock, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertContains(seekBlock, "newerSeek = ResolveSeekCommandPosition(newerSeek);");
        AssertContains(seekBlock, "FLASHBACK_PLAYBACK_SEEK");

        var beginScrubMethod = ExtractTextBetween(
            sourceText,
            "public bool BeginScrub(TimeSpan position)",
            "    public bool Seek(TimeSpan position)");
        var seekMethod = ExtractTextBetween(
            sourceText,
            "public bool Seek(TimeSpan position)",
            "    private bool SendUpdateScrubCommand");
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
            "private void DrainAbandonedCommandsOnThreadExit(Channel<PlaybackCommand> commandChannel)",
            "    private static void CompleteCommandChannelForThreadExit");

        AssertContains(sourceText, "private long _latestScrubUpdateTicks;");
        AssertContains(sourceText, "private sealed class SeekIntentSlot");
        AssertContains(sourceText, "private sealed class ScrubUpdateIntentSlot");
        AssertContains(sourceText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(sourceText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertContains(sourceText, "private readonly object _seekSlotSync = new();");
        AssertContains(sourceText, "private SeekIntentSlot? _queuedSeekSlot;");
        AssertContains(sourceText, "private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;");
        AssertContains(sourceText, "private long _scrubUpdatesCoalesced;");
        AssertContains(sourceText, "private long _seekCommandsCoalesced;");
        AssertContains(sourceText, "public long SeekCommandsCoalesced => Interlocked.Read(ref _seekCommandsCoalesced);");
        AssertContains(sourceText, "public bool HasPositionOverride { get; init; }");
        AssertContains(sourceText, "public bool EndScrub() => EndScrubAt(null);");
        AssertContains(sourceText, "public bool EndScrubAt(TimeSpan position) => EndScrubAt((TimeSpan?)position);");
        AssertContains(sourceText, "private bool EndScrubAt(TimeSpan? position)");
        AssertContains(sourceText, "return SendEndScrubCommand(position);");
        AssertContains(sourceText, "private bool SendEndScrubCommand(TimeSpan? position)");
        AssertContains(sourceText, "var commandTicks = position?.Ticks ??");
        AssertContains(sourceText, "_queuedScrubUpdateSlot?.LatestTicks ??");
        AssertContains(sourceText, "var commandPosition = TimeSpan.FromTicks(commandTicks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Value.Ticks);");
        AssertContains(sourceText, "HasPositionOverride = position.HasValue");
        AssertContains(sourceText, "HasPositionOverride = command.HasPositionOverride");
        AssertContains(sourceText, "SeekSlot = command.SeekSlot");
        AssertContains(sourceText, "ScrubUpdateSlot = command.ScrubUpdateSlot");
        AssertContains(beginScrubMethod, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(seekMethod, "lock (_seekSlotSync)");
        AssertContains(seekMethod, "_queuedScrubUpdateSlot = null;");
        AssertContains(seekMethod, "if (_queuedSeekSlot is { } queuedSlot)");
        AssertContains(seekMethod, "queuedSlot.LatestTicks = position.Ticks;");
        AssertContains(seekMethod, "TrackCoalescedSeekCommand();");
        AssertContains(seekMethod, "ClearLastCommandFailure();");
        AssertContains(seekMethod, "return true;");
        AssertContains(seekMethod, "var slot = new SeekIntentSlot(position.Ticks);");
        AssertContains(seekMethod, "_queuedSeekSlot = slot;");
        AssertContains(seekMethod, "SeekSlot = slot");
        AssertContains(seekMethod, "ClearQueuedSeekSlotUnsafe(slot);");
        AssertContains(seekMethod, "return false;");
        AssertContains(sourceText, "private bool SendCommand(PlaybackCommand command)\n    {\n        lock (_seekSlotSync)\n        {\n            if (!SendCommandCore(command))\n            {\n                return false;\n            }\n\n            if (command.Kind != CommandKind.Seek)\n            {\n                _queuedSeekSlot = null;\n            }\n\n            if (command.Kind != CommandKind.UpdateScrub)\n            {\n                _queuedScrubUpdateSlot = null;\n            }\n\n            return true;\n        }\n    }");
        AssertContains(updateScrubMethod, "return SendUpdateScrubCommand(position);");
        AssertContains(sourceText, "private bool SendUpdateScrubCommand(TimeSpan position)");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(sourceText, "if (_queuedScrubUpdateSlot is { } queuedSlot)");
        AssertContains(sourceText, "queuedSlot.LatestTicks = position.Ticks;");
        AssertContains(sourceText, "ClearLastCommandFailure();");
        AssertContains(sourceText, "var slot = new ScrubUpdateIntentSlot(position.Ticks);");
        AssertContains(sourceText, "_queuedScrubUpdateSlot = slot;");
        AssertContains(sourceText, "ScrubUpdateSlot = slot");
        AssertContains(sourceText, "ClearQueuedScrubUpdateSlotUnsafe(slot);");
        AssertContains(updateScrubMethod, "if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, \"thread_not_running\", \"thread_not_running\", false, position);");
        AssertContains(sourceText, "TrackCoalescedScrubUpdate();");
        AssertContains(updateScrubBlock, "cmd = ResolveScrubUpdateCommandPosition(cmd);");
        AssertContains(updateScrubBlock, "commandChannel.Reader.TryPeek(out var newer) &&\n                               newer.Kind == CommandKind.UpdateScrub");
        AssertContains(updateScrubBlock, "if (!commandChannel.Reader.TryRead(out newer))");
        AssertContains(updateScrubBlock, "TrackCommandDequeued(newer);");
        AssertContains(updateScrubBlock, "newer = ResolveScrubUpdateCommandPosition(newer);");
        AssertContains(updateScrubBlock, "cmd = newer;");
        AssertContains(updateScrubBlock, "if (ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "PlaybackPosition = cmd.Position;");
        AssertContains(updateScrubBlock, "MarkCommandNoOp(CommandKind.UpdateScrub, \"superseded_by_control\", cmd.Position);");
        AssertContains(updateScrubBlock, "FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE");
        AssertContains(updateScrubBlock, "SafeResumePreviewSubmission(\"scrub_update_no_file\")");
        AssertContains(updateScrubBlock, "SetState(FlashbackPlaybackState.Live)");
        AssertContains(sourceText, "private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(sourceText, "private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(sourceText, "private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(drainAbandonedCommands, "ClearQueuedCommandSlotsBarrier();");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.EndScrub, \"live_thread_not_running\", position);\n            return false;\n        }");
        var endScrubBlock = ExtractTextBetween(
            sourceText,
            "                    case CommandKind.EndScrub:",
            "                    case CommandKind.Play:");
        AssertContains(endScrubBlock, "var endScrubPosition = ClampPosition(cmd.Position, frozenValidStart);");
        AssertContains(endScrubBlock, "PlaybackPosition = endScrubPosition;");
        AssertDoesNotContain(endScrubBlock, "TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks))");
        AssertContains(endScrubBlock, "var endScrubTarget = SaturatingAdd(endScrubPosition, frozenValidStart);");
        AssertDoesNotContain(endScrubBlock, "var endScrubTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(sourceText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(sourceText, "SetLastCommandFailure($\"{failure}:{kind}{detail}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}{detail}\");");
        AssertContains(sourceText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"no_file:{kind}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(sourceText, "return $\" pos_ms={position.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "return $\" delta_ms={delta.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "private void SetLastCommandFailure(string failure)\n    {\n        Volatile.Write(ref _lastCommandFailure, failure);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());\n    }");
        AssertContains(sourceText, "private void MarkCommandQueued(CommandKind kind)");
        AssertContains(sourceText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(sourceText, "private void ClearLastCommandFailure()\n    {\n        Volatile.Write(ref _lastCommandFailure, string.Empty);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);\n    }");
        AssertContains(sourceText, "private void TrackCoalescedScrubUpdate()");
        AssertContains(sourceText, "Interlocked.Increment(ref _scrubUpdatesCoalesced);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SCRUB_COALESCED");
        var coalescedSeekMethod = ExtractTextBetween(
            sourceText,
            "private void TrackCoalescedSeekCommand()",
            "    private void TrackCommandDequeued");
        AssertContains(coalescedSeekMethod, "Interlocked.Increment(ref _seekCommandsCoalesced);");
        AssertContains(coalescedSeekMethod, "FLASHBACK_PLAYBACK_SEEK_COALESCED");
        AssertDoesNotContain(coalescedSeekMethod, "_commandsDropped");
        AssertContains(sourceText, "private PlaybackCommand ResolveSeekCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedSeekSlot, slot))\n            {\n                _queuedSeekSlot = null;\n            }");
        AssertContains(sourceText, "private PlaybackCommand ResolveScrubUpdateCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedScrubUpdateSlot, slot))\n            {\n                _queuedScrubUpdateSlot = null;\n            }");
        AssertContains(sourceText, "private void ClearQueuedCommandSlotsBarrier()");
        AssertDoesNotContain(updateScrubBlock, "SendCommand(newer)");
        AssertDoesNotContain(updateScrubBlock, "Non-scrub command consumed");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var seekSlotType = controllerType.GetNestedType("SeekIntentSlot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SeekIntentSlot not found.");
        var scrubUpdateSlotType = controllerType.GetNestedType("ScrubUpdateIntentSlot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot not found.");
        var resolve = controllerType.GetMethod("ResolveSeekCommandPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSeekCommandPosition not found.");
        var resolveScrub = controllerType.GetMethod("ResolveScrubUpdateCommandPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveScrubUpdateCommandPosition not found.");
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");
        var sendEndScrub = controllerType.GetMethod("SendEndScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendEndScrubCommand not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var latestTicksField = seekSlotType.GetField("LatestTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SeekIntentSlot.LatestTicks not found.");
        var scrubLatestTicksField = scrubUpdateSlotType.GetField("LatestTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot.LatestTicks not found.");
        var queuedSeekSlotField = controllerType.GetField("_queuedSeekSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedSeekSlot not found.");
        var queuedScrubSlotField = controllerType.GetField("_queuedScrubUpdateSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedScrubUpdateSlot not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var seekKind = Enum.Parse(commandKindType, "Seek");
        var updateScrubKind = Enum.Parse(commandKindType, "UpdateScrub");
        var playKind = Enum.Parse(commandKindType, "Play");
        var oneSecond = TimeSpan.FromSeconds(1);
        var twoSeconds = TimeSpan.FromSeconds(2);
        var threeSeconds = TimeSpan.FromSeconds(3);
        var fourSeconds = TimeSpan.FromSeconds(4);

        var slotA = Activator.CreateInstance(
                seekSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { oneSecond.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("SeekIntentSlot A construction failed.");
        var commandA = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand construction failed.");
        SetPropertyOrBackingField(commandA, "Kind", seekKind);
        SetPropertyOrBackingField(commandA, "Position", oneSecond);
        SetPropertyOrBackingField(commandA, "SeekSlot", slotA);

        queuedSeekSlotField.SetValue(controller, slotA);
        latestTicksField.SetValue(slotA, twoSeconds.Ticks);
        var resolvedCoalesced = resolve.Invoke(controller, new[] { commandA })
            ?? throw new InvalidOperationException("Resolve coalesced seek returned null.");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedCoalesced, "Position")!, "Coalesced seek slot resolves latest position");
        AssertEqual(null, queuedSeekSlotField.GetValue(controller), "Resolved active seek slot is cleared");

        latestTicksField.SetValue(slotA, oneSecond.Ticks);
        var slotB = Activator.CreateInstance(
                seekSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { threeSeconds.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("SeekIntentSlot B construction failed.");
        queuedSeekSlotField.SetValue(controller, slotB);
        var resolvedBarrier = resolve.Invoke(controller, new[] { commandA })
            ?? throw new InvalidOperationException("Resolve barrier seek returned null.");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedBarrier, "Position")!, "Older seek slot does not consume later barrier-separated target");
        if (!ReferenceEquals(slotB, queuedSeekSlotField.GetValue(controller)))
        {
            throw new InvalidOperationException("Later seek slot should remain queued after resolving older barrier-separated seek.");
        }

        var scrubSlotA = Activator.CreateInstance(
                scrubUpdateSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { oneSecond.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot A construction failed.");
        var updateCommandA = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand update construction failed.");
        SetPropertyOrBackingField(updateCommandA, "Kind", updateScrubKind);
        SetPropertyOrBackingField(updateCommandA, "Position", oneSecond);
        SetPropertyOrBackingField(updateCommandA, "ScrubUpdateSlot", scrubSlotA);

        queuedScrubSlotField.SetValue(controller, scrubSlotA);
        scrubLatestTicksField.SetValue(scrubSlotA, twoSeconds.Ticks);
        var resolvedScrubCoalesced = resolveScrub.Invoke(controller, new[] { updateCommandA })
            ?? throw new InvalidOperationException("Resolve coalesced scrub update returned null.");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedScrubCoalesced, "Position")!, "Coalesced scrub slot resolves latest position");
        AssertEqual(null, queuedScrubSlotField.GetValue(controller), "Resolved active scrub slot is cleared");

        scrubLatestTicksField.SetValue(scrubSlotA, oneSecond.Ticks);
        var scrubSlotB = Activator.CreateInstance(
                scrubUpdateSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { threeSeconds.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot B construction failed.");
        queuedScrubSlotField.SetValue(controller, scrubSlotB);
        var resolvedScrubBarrier = resolveScrub.Invoke(controller, new[] { updateCommandA })
            ?? throw new InvalidOperationException("Resolve barrier scrub update returned null.");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedScrubBarrier, "Position")!, "Older scrub slot does not consume later barrier-separated target");
        if (!ReferenceEquals(scrubSlotB, queuedScrubSlotField.GetValue(controller)))
        {
            throw new InvalidOperationException("Later scrub slot should remain queued after resolving older barrier-separated scrub update.");
        }

        var producerController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Producer FlashbackPlaybackController construction failed.");
        using var disposableProducerController = producerController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { oneSecond })!, "First producer seek enqueues");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { twoSeconds })!, "Adjacent producer seek coalesces");
        var playCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
        SetPropertyOrBackingField(playCommand, "Kind", playKind);
        AssertEqual(true, (bool)sendCommand.Invoke(producerController, new[] { playCommand })!, "Producer play barrier enqueues");
        AssertEqual(null, queuedSeekSlotField.GetValue(producerController), "Accepted non-seek barrier closes active seek slot before later seeks");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { threeSeconds })!, "Post-barrier producer seek enqueues new slot");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(producerController, new object[] { oneSecond })!, "Producer scrub update barrier enqueues");
        AssertEqual(null, queuedSeekSlotField.GetValue(producerController), "Accepted scrub update barrier closes active seek slot before later seeks");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { fourSeconds })!, "Post-scrub-barrier producer seek enqueues new slot");

        var channel = commandChannelField.GetValue(producerController)
            ?? throw new InvalidOperationException("Producer command channel missing.");
        var firstQueued = ReadQueuedPlaybackCommand(channel, commandType, "first queued command");
        var resolvedFirstQueued = resolve.Invoke(producerController, new[] { firstQueued })
            ?? throw new InvalidOperationException("Resolve first producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedFirstQueued, "Kind")?.ToString(), "First queued producer command kind");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedFirstQueued, "Position")!, "Adjacent producer seeks resolve to latest pre-barrier position");

        var secondQueued = ReadQueuedPlaybackCommand(channel, commandType, "second queued command");
        AssertEqual("Play", GetPropertyValue(secondQueued, "Kind")?.ToString(), "Second queued producer command is the barrier");

        var thirdQueued = ReadQueuedPlaybackCommand(channel, commandType, "third queued command");
        var resolvedThirdQueued = resolve.Invoke(producerController, new[] { thirdQueued })
            ?? throw new InvalidOperationException("Resolve third producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedThirdQueued, "Kind")?.ToString(), "Third queued producer command kind");
        AssertEqual(threeSeconds, (TimeSpan)GetPropertyValue(resolvedThirdQueued, "Position")!, "Post-barrier producer seek keeps its own position");

        var fourthQueued = ReadQueuedPlaybackCommand(channel, commandType, "fourth queued command");
        var resolvedFourthQueued = resolveScrub.Invoke(producerController, new[] { fourthQueued })
            ?? throw new InvalidOperationException("Resolve fourth producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedFourthQueued, "Kind")?.ToString(), "Fourth queued producer command is the scrub barrier");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedFourthQueued, "Position")!, "Scrub barrier command keeps its own position");

        var fifthQueued = ReadQueuedPlaybackCommand(channel, commandType, "fifth queued command");
        var resolvedFifthQueued = resolve.Invoke(producerController, new[] { fifthQueued })
            ?? throw new InvalidOperationException("Resolve fifth producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedFifthQueued, "Kind")?.ToString(), "Fifth queued producer command kind");
        AssertEqual(fourSeconds, (TimeSpan)GetPropertyValue(resolvedFifthQueued, "Position")!, "Post-scrub-barrier producer seek keeps its own position");
        AssertEqual(false, TryReadQueuedPlaybackCommand(channel, commandType, out _), "No extra producer commands are queued");

        var scrubProducerController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Scrub producer FlashbackPlaybackController construction failed.");
        using var disposableScrubProducerController = scrubProducerController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { oneSecond })!, "First producer scrub update enqueues");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { twoSeconds })!, "Adjacent producer scrub update coalesces");
        AssertEqual(true, (bool)sendEndScrub.Invoke(scrubProducerController, new object?[] { null })!, "Producer end scrub barrier enqueues");
        AssertEqual(null, queuedScrubSlotField.GetValue(scrubProducerController), "EndScrub closes active scrub slot before later updates");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { threeSeconds })!, "Post-barrier producer scrub update enqueues new slot");

        var scrubChannel = commandChannelField.GetValue(scrubProducerController)
            ?? throw new InvalidOperationException("Scrub producer command channel missing.");
        var firstScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "first queued scrub command");
        var resolvedFirstScrubQueued = resolveScrub.Invoke(scrubProducerController, new[] { firstScrubQueued })
            ?? throw new InvalidOperationException("Resolve first producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedFirstScrubQueued, "Kind")?.ToString(), "First queued producer scrub command kind");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedFirstScrubQueued, "Position")!, "Adjacent producer scrub updates resolve to latest pre-barrier position");

        var secondScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "second queued scrub command");
        AssertEqual("EndScrub", GetPropertyValue(secondScrubQueued, "Kind")?.ToString(), "Second queued producer scrub command is the barrier");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(secondScrubQueued, "Position")!, "EndScrub snapshots the latest pre-barrier scrub target");

        var thirdScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "third queued scrub command");
        var resolvedThirdScrubQueued = resolveScrub.Invoke(scrubProducerController, new[] { thirdScrubQueued })
            ?? throw new InvalidOperationException("Resolve third producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedThirdScrubQueued, "Kind")?.ToString(), "Third queued producer scrub command kind");
        AssertEqual(threeSeconds, (TimeSpan)GetPropertyValue(resolvedThirdScrubQueued, "Position")!, "Post-barrier producer scrub update keeps its own position");
        AssertEqual(false, TryReadQueuedPlaybackCommand(scrubChannel, commandType, out _), "No extra producer scrub commands are queued");

        var failedBarrierController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed barrier FlashbackPlaybackController construction failed.");
        using var disposableFailedBarrierController = failedBarrierController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(failedBarrierController, new object[] { oneSecond })!, "Failed-barrier setup seek enqueues");
        var failedBarrierSlot = queuedSeekSlotField.GetValue(failedBarrierController)
            ?? throw new InvalidOperationException("Failed-barrier seek slot missing.");
        var failedBarrierChannel = commandChannelField.GetValue(failedBarrierController)
            ?? throw new InvalidOperationException("Failed-barrier command channel missing.");
        CompleteQueuedPlaybackCommands(failedBarrierChannel);
        AssertEqual(false, (bool)sendCommand.Invoke(failedBarrierController, new[] { playCommand })!, "Rejected play barrier reports failure");
        if (!ReferenceEquals(failedBarrierSlot, queuedSeekSlotField.GetValue(failedBarrierController)))
        {
            throw new InvalidOperationException("Rejected play barrier should preserve the active seek slot.");
        }

        var failedSeekController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed seek FlashbackPlaybackController construction failed.");
        using var disposableFailedSeekController = failedSeekController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedSeekController, new object[] { oneSecond })!, "Failed-seek setup scrub update enqueues");
        var failedSeekScrubSlot = queuedScrubSlotField.GetValue(failedSeekController)
            ?? throw new InvalidOperationException("Failed-seek scrub slot missing.");
        var failedSeekChannel = commandChannelField.GetValue(failedSeekController)
            ?? throw new InvalidOperationException("Failed-seek command channel missing.");
        CompleteQueuedPlaybackCommands(failedSeekChannel);
        AssertEqual(false, (bool)sendSeek.Invoke(failedSeekController, new object[] { twoSeconds })!, "Rejected seek barrier reports failure");
        if (!ReferenceEquals(failedSeekScrubSlot, queuedScrubSlotField.GetValue(failedSeekController)))
        {
            throw new InvalidOperationException("Rejected seek should preserve the active scrub slot.");
        }
        AssertEqual(null, queuedSeekSlotField.GetValue(failedSeekController), "Rejected seek clears only its own newly-created seek slot");

        var failedScrubUpdateController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed scrub update FlashbackPlaybackController construction failed.");
        using var disposableFailedScrubUpdateController = failedScrubUpdateController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(failedScrubUpdateController, new object[] { oneSecond })!, "Failed-scrub-update setup seek enqueues");
        var failedScrubUpdateSeekSlot = queuedSeekSlotField.GetValue(failedScrubUpdateController)
            ?? throw new InvalidOperationException("Failed-scrub-update seek slot missing.");
        var failedScrubUpdateChannel = commandChannelField.GetValue(failedScrubUpdateController)
            ?? throw new InvalidOperationException("Failed-scrub-update command channel missing.");
        CompleteQueuedPlaybackCommands(failedScrubUpdateChannel);
        AssertEqual(false, (bool)sendUpdateScrub.Invoke(failedScrubUpdateController, new object[] { twoSeconds })!, "Rejected scrub update barrier reports failure");
        if (!ReferenceEquals(failedScrubUpdateSeekSlot, queuedSeekSlotField.GetValue(failedScrubUpdateController)))
        {
            throw new InvalidOperationException("Rejected scrub update should preserve the active seek slot.");
        }
        AssertEqual(null, queuedScrubSlotField.GetValue(failedScrubUpdateController), "Rejected scrub update clears only its own newly-created scrub slot");

        var failedEndScrubController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed end scrub FlashbackPlaybackController construction failed.");
        using var disposableFailedEndScrubController = failedEndScrubController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedEndScrubController, new object[] { oneSecond })!, "Failed-end-scrub setup update enqueues");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedEndScrubController, new object[] { twoSeconds })!, "Failed-end-scrub setup update coalesces");
        var failedEndScrubSlot = queuedScrubSlotField.GetValue(failedEndScrubController)
            ?? throw new InvalidOperationException("Failed-end-scrub slot missing.");
        var failedEndScrubChannel = commandChannelField.GetValue(failedEndScrubController)
            ?? throw new InvalidOperationException("Failed-end-scrub command channel missing.");
        CompleteQueuedPlaybackCommands(failedEndScrubChannel);
        AssertEqual(false, (bool)sendEndScrub.Invoke(failedEndScrubController, new object?[] { null })!, "Rejected end scrub barrier reports failure");
        if (!ReferenceEquals(failedEndScrubSlot, queuedScrubSlotField.GetValue(failedEndScrubController)))
        {
            throw new InvalidOperationException("Rejected end scrub should preserve the active scrub slot.");
        }

        return Task.CompletedTask;

        static object ReadQueuedPlaybackCommand(object channel, Type commandType, string label)
        {
            if (!TryReadQueuedPlaybackCommand(channel, commandType, out var command) || command is null)
            {
                throw new InvalidOperationException($"Expected {label}.");
            }

            return command;
        }

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }

        static void CompleteQueuedPlaybackCommands(object channel)
        {
            var writer = channel.GetType().GetProperty("Writer")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel writer missing.");
            var tryComplete = writer.GetType().GetMethod(
                    "TryComplete",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(Exception) },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryComplete not found.");
            _ = (bool)tryComplete.Invoke(writer, new object?[] { null })!;
        }
    }

    private static Task FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();
        var wasapiPlaybackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private void SafeSuppressPreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafeResumePreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafePauseRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumeRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumePlaybackRendering(string operation)");
        AssertContains(sourceText, "private void SafeFlushPlayback(string operation)");
        AssertContains(sourceText, "private const double PlaybackAudioPrebufferTargetMs = 180.0;");
        AssertContains(sourceText, "private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferTimeoutMs = 1000;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferRetryDelayMs = 20;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;");
        AssertContains(sourceText, "var prebufferedFrames = new Queue<DecodedVideoFrame>();");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"command_{cmd.Kind}\");");
        AssertContains(sourceText, "private void PrimePlaybackAudioBuffer(");
        AssertContains(sourceText, "TimeSpan resumeTarget,");
        AssertContains(sourceText, "while (decodedFrames < PlaybackAudioPrebufferDecodeFrameBudget)");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"audio_prebuffer_{operation}\");");
        AssertContains(sourceText, "released_frames={prebufferReleasedFrames}");
        AssertDoesNotContain(sourceText, "prebufferedFrames.Enqueue(frame);");
        AssertContains(sourceText, "cancellationToken.WaitHandle.WaitOne(waitMs)");
        AssertContains(sourceText, "bufferedMs > PlaybackAudioPrebufferDiscardThresholdMs");
        AssertContains(sourceText, "rewound = TryRewindPlaybackAudioPrebuffer(decoder, ref fileOpen, resumeTarget, operation, cancellationToken);");
        AssertContains(sourceText, "private bool TryRewindPlaybackAudioPrebuffer(");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, resumeTarget, $\"prebuffer_discard_{operation}\", cancellationToken)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND operation={operation}");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"prebuffer_discard_{operation}\");");
        AssertContains(sourceText, "eof_retries={eofRetries}");
        AssertContains(sourceText, "rewound={rewound}");
        AssertDoesNotContain(sourceText, "if ((reachedEnd && decodedFrames > 0) ||");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_PREBUFFER operation={operation}");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"seek_resume\")");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"end_scrub_resume\")");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"play\")");
        AssertContains(sourceText, "ApplyAudioRoutingForState(\"audio_update\");");
        AssertContains(sourceText, "private void ApplyAudioRoutingForState(string operation)");
        AssertContains(sourceText, "case FlashbackPlaybackState.Live:\n                RestoreLiveAudio();");
        AssertContains(sourceText, "case FlashbackPlaybackState.Playing:\n                SuppressLiveAudio();\n                SafeResumeRendering(operation);");
        AssertContains(sourceText, "case FlashbackPlaybackState.Paused:\n            case FlashbackPlaybackState.Scrubbing:\n                SuppressLiveAudio();\n                SafePauseRendering(operation);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"scrub_no_file\")");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"go_live\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"decode_error\")");
        AssertContains(sourceText, "SafeFlushPlayback(\"restore_live_audio\")");
        AssertContains(sourceText, "SafeResumeRendering(\"play_no_file\")");
        AssertContains(sourceText, "SafeResumeRendering(\"nudge_no_file\")");
        AssertContains(sourceText, "if (_audioPlayback == null)\n        {\n            decoder.AudioChunkCallback = null;\n            return;\n        }");
        AssertContains(sourceText, "if (!TryValidatePlaybackAudioChunk(chunk, out var invalidReason))");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_DROP reason={invalidReason}");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, $\"playback_audio_{invalidReason}\");");
        AssertContains(sourceText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(sourceText, "reason = \"length_exceeds_buffer\";");
        AssertContains(sourceText, "reason = \"unaligned_length\";");
        AssertContains(sourceText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_RETURN_WARN");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, \"playback_audio_non_monotonic_pts\");");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, \"playback_audio_before_gate\");");
        AssertContains(sourceText, "pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);");
        AssertContains(sourceText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertContains(sourceText, "private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "private bool ShouldSnapLiveForSoftwarePlaybackBudget(");
        AssertContains(sourceText, "GpuDecodeEnabled &&\n               !decoder.IsD3D11HwAccelerated &&\n               pixelRate > MaxContinuousSoftwarePlaybackPixelRate");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SOFTWARE_DECODE_SNAP_TO_LIVE");
        AssertContains(sourceText, "SetLastCommandFailure($\"software_decode_over_budget:{operation}{FormatCommandDetail(position: pos)}\");");
        AssertContains(sourceText, "TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, \"play\")");
        AssertContains(sourceText, "SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, \"playback_decode\");");
        AssertContains(sourceText, "private void UpdateDecoderHwAccel(FlashbackDecoder decoder)");
        AssertContains(sourceText, "const double MaxAudioMasterCorrectionMs = 250.0;");
        AssertContains(sourceText, "const double syncThresholdMs = 100.0;");
        AssertContains(sourceText, "private string _pendingAudioMasterFallbackReason = string.Empty;");
        AssertContains(sourceText, "private static bool IsTransientAudioMasterFallbackCandidate(string reason)");
        AssertContains(sourceText, "string.Equals(reason, \"unavailable\", StringComparison.Ordinal)");
        AssertContains(sourceText, "string.Equals(reason, \"stale-clock\", StringComparison.Ordinal)");
        AssertContains(sourceText, "string.Equals(reason, \"drift-outlier\", StringComparison.Ordinal)");
        AssertContains(sourceText, "ClearPendingAudioMasterFallback();");
        AssertContains(sourceText, "CommitPendingAudioMasterFallback();");
        AssertContains(sourceText, "CommitAudioMasterFallback(");
        AssertContains(sourceText, "if (Math.Abs(diffMs) > MaxAudioMasterCorrectionMs)\n            {\n                // WASAPI render PTS can lag decoded video by the endpoint buffer/device");
        AssertContains(sourceText, "WallClockPace(pacingStopwatch, frameDuration);\n                return;");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"seek_resume\");");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, endScrubTarget, \"end_scrub_resume\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"end_scrub_resume\");");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, seekTarget, \"play\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"play\");");
        AssertContains(sourceText, "private void ResetPlaybackPtsCadenceBaseline()");
        AssertContains(sourceText, "ResetPlaybackPtsCadenceBaseline();\n                    pacingStopwatch.Restart();\n                    return true;");
        AssertContains(sourceText, "if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))");
        AssertContains(sourceText, "_pendingAudioMasterFallbackReason = reason;");
        AssertContains(sourceText, "CommitAudioMasterFallback(");
        AssertContains(sourceText, "_pendingAudioMasterFallbackReason,");
        AssertContains(sourceText, "var correctionMs = Math.Min(diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));");
        AssertContains(sourceText, "adjustedDelayMs = nominalDelayMs + Math.Max(0, correctionMs);");
        AssertContains(sourceText, "var correctionMs = Math.Min(-diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));");
        AssertContains(sourceText, "adjustedDelayMs = Math.Max(0, nominalDelayMs - Math.Max(0, correctionMs));");
        AssertDoesNotContain(sourceText, "adjustedDelayMs = nominalDelayMs * 2;");
        AssertDoesNotContain(sourceText, "adjustedDelayMs = Math.Max(0, nominalDelayMs + diffMs);");
        AssertContains(wasapiPlaybackText, "if (Volatile.Read(ref _renderingPaused) != 0 && !_resumeRequested) return;");
        AssertContains(wasapiPlaybackText, "_resumeRequested = false;\n        _pauseRequested = true;");
        AssertContains(wasapiPlaybackText, "if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;");
        AssertContains(wasapiPlaybackText, "public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)");
        AssertContains(wasapiPlaybackText, "Volatile.Write(ref _resumePrebufferFrames, Math.Max(0, prebufferFrames));");
        AssertContains(wasapiPlaybackText, "_resumeRequested = true;\n        _renderEvent?.Set();");
        AssertContains(wasapiPlaybackText, "if (!_resumeRequested)\n                {\n                    continue;\n                }");
        AssertContains(wasapiPlaybackText, "WASAPI_PLAYBACK_RENDER_RESUME_CANCELED_PENDING_PAUSE");
        AssertContains(wasapiPlaybackText, "WaitForResumePrebuffer();");
        AssertContains(wasapiPlaybackText, "WASAPI_PLAYBACK_RENDER_PREBUFFER target_ms={FramesToMilliseconds(targetFrames):F1}");
        AssertContains(wasapiPlaybackText, "private int PlaybackBufferedFramesForResume()");
        AssertDoesNotContain(wasapiPlaybackText, "public void ResumeRendering()\n    {\n        if (Volatile.Read(ref _started) == 0) return;\n        if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;\n\n        _pauseRequested = false;");
        AssertDoesNotContain(wasapiPlaybackText, "GetCurrentPadding(pre-fill)");
        AssertDoesNotContain(wasapiPlaybackText, "IAudioRenderClient.GetBuffer(pre-fill)");
        AssertDoesNotContain(wasapiPlaybackText, "AUDCLNT_BUFFERFLAGS_SILENT");
        AssertDoesNotContain(wasapiPlaybackText, "WASAPI_PREFILL_WARN");
        AssertContains(wasapiPlaybackText, "private int _playbackQueueDepth;");
        AssertContains(wasapiPlaybackText, "public int PlaybackQueueDepth => Math.Max(0, Volatile.Read(ref _playbackQueueDepth));");
        AssertContains(wasapiPlaybackText, "if (TryWriteChunk(chunk)) return;");
        AssertContains(wasapiPlaybackText, "private bool TryWriteChunk(PlaybackChunk chunk)");
        AssertContains(wasapiPlaybackText, "Interlocked.Increment(ref _playbackQueueDepth);\n        if (_sampleQueue.Writer.TryWrite(chunk))");
        AssertContains(wasapiPlaybackText, "DecrementPlaybackQueueDepth();\n        return false;");
        AssertContains(wasapiPlaybackText, "private bool TryDequeueChunk(out PlaybackChunk chunk)");
        AssertContains(wasapiPlaybackText, "DecrementPlaybackQueueDepth();");
        AssertContains(wasapiPlaybackText, "private const int OutputSampleRate = 48000;");
        AssertContains(wasapiPlaybackText, "private const uint MaxRenderWriteFrames = OutputSampleRate / 50; // 20ms");
        AssertContains(wasapiPlaybackText, "var framesToWrite = Math.Min(_bufferFrameCount - paddingFrames, MaxRenderWriteFrames);");
        AssertDoesNotContain(wasapiPlaybackText, "var framesToWrite = _bufferFrameCount - paddingFrames;");
        AssertContains(wasapiPlaybackText, "UpdateRenderingPtsForActiveChunk();");
        AssertContains(wasapiPlaybackText, "var frameOffset = Math.Max(0, _activeChunkOffset) / OutputBlockAlign;");
        AssertContains(wasapiPlaybackText, "var offsetTicks = frameOffset * TimeSpan.TicksPerSecond / OutputSampleRate;");
        AssertDoesNotContain(wasapiPlaybackText, "_sampleQueue.Reader.Count");
        AssertDoesNotContain(wasapiPlaybackText, "_sampleQueue.Writer.TryWrite(chunk))\n        {\n            Interlocked.Increment(ref _playbackQueueDepth);");
        AssertDoesNotContain(sourceText, "_videoCapture?.SuppressPreviewSubmission();\n                        SuppressLiveAudio();\n                        _audioPlayback?.PauseRendering();");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ResetClearsDecodeMetrics()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        var resetMetricsBlock = ExtractTextBetween(
            sourceText,
            "private void ResetPlaybackMetrics()",
            "private void RestoreAudioCallback");
        AssertContains(resetMetricsBlock, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(resetMetricsBlock, "lock (_playbackDecodeLock)");
        AssertContains(resetMetricsBlock, "Array.Clear(_playbackDecodeDurationsMs);");
        AssertContains(resetMetricsBlock, "_playbackDecodeDurationHead = 0;");
        AssertContains(resetMetricsBlock, "_playbackDecodeDurationCount = 0;");
        AssertContains(sourceText, "if (phaseTimings.FeedMs > max) { phase = \"feed\"; max = phaseTimings.FeedMs; }");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_DiscardedAudioFramesAreUnreffed()
    {
        var sourceText = ReadFlashbackDecoderSource();

        var audioDecodeBlock = ExtractTextBetween(
            sourceText,
            "private void DecodeAndDeliverAudioPacket",
            "// ── Private: Frame Conversion");
        AssertContains(audioDecodeBlock, "if (callback == null)\n            {\n                ffmpeg.av_frame_unref(_audioFrame);\n                continue; // Codec advanced, but no delivery during seek/scrub\n            }");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode()
    {
        var sourceText = ReadFlashbackDecoderSource();

        var softwareInitBlock = ExtractTextBetween(
            sourceText,
            "// Software fallback",
            "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");
        AssertContains(softwareInitBlock, "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)\n        {\n            _videoCodecCtx->thread_count = 1;\n        }");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_PtsConversionRejectsInvalidTimestamps()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_videoFrame), _videoTimeBase);");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);");
        AssertContains(sourceText, "var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);");
        AssertContains(sourceText, "_formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "var timestampUs = ToAvTimeBaseTimestamp(target);");
        AssertContains(sourceText, "_formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(sourceText, "private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)");
        AssertContains(sourceText, "private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)");
        AssertContains(sourceText, "frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE");
        AssertContains(sourceText, "return frame->best_effort_timestamp;");
        AssertContains(sourceText, "return frame->pts;");
        AssertContains(sourceText, "if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(sourceText, "private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)");
        AssertContains(sourceText, "if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "private bool _suppressRecoverableSeekLogsForNextVideoFrame;");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = true;");
        AssertContains(sourceText, "using var recoverableSeekLogScope = BeginRecoverableSeekLogSuppressionIfNeeded();");
        AssertContains(sourceText, "private IDisposable? BeginRecoverableSeekLogSuppressionIfNeeded()");
        AssertContains(sourceText, "return LibAvEncoder.SuppressRecoverableSeekFfmpegLogs();");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = false;");
        AssertDoesNotContain(sourceText, "(long)(target.TotalSeconds * ffmpeg.AV_TIME_BASE)");
        AssertDoesNotContain(sourceText, "var seconds = (double)_videoFrame->pts * _videoTimeBase.num / _videoTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "var seconds = (double)_audioFrame->pts * _audioTimeBase.num / _audioTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_videoFrame->pts, _videoTimeBase)");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_audioFrame->pts, _audioTimeBase)");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_InputStreamsAndFrameSizesAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private const int MaxDecodedVideoDimension = 8192;");
        AssertContains(sourceText, "private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000;");
        AssertContains(sourceText, "_formatCtx->probesize = MaxMpegTsProbeSizeBytes;");
        AssertContains(sourceText, "_formatCtx->max_analyze_duration = MaxMpegTsAnalyzeDurationUs;");
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
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;");
        AssertContains(sourceText, "byte[]? result = null;\n        var returnResultToPool = true;");
        AssertContains(sourceText, "if (inputSamples <= 0)\n            {\n                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };\n            }");
        AssertContains(sourceText, "maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size");
        AssertContains(sourceText, "returnResultToPool = false;");
        AssertContains(sourceText, "finally\n        {\n            ffmpeg.av_frame_unref(_audioFrame);\n            if (returnResultToPool && result is { Length: > 0 })");
        AssertContains(sourceText, "ArrayPool<byte>.Shared.Return(result);");
        AssertContains(sourceText, "private static int ToBoundedAudioSampleCount(long sampleCount)");
        AssertContains(sourceText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(sourceText, "var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var outputBytesNeeded = maxOutputSamples * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var validBytes = outputSamplesProduced * OutputAudioChannels * sizeof(float);");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_AudioSetupLivesInAudioOutputPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var audioOutputText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private void InitializeAudioDecoder()");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler()");
        AssertContains(audioOutputText, "private void InitializeAudioDecoder()");
        AssertContains(audioOutputText, "private void InitializeAudioResampler()");
        AssertContains(audioOutputText, "FLASHBACK_DECODER_AUDIO codec=");
        AssertContains(audioOutputText, "swr_alloc_set_opts2");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_SoftwareFramePlanesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)");
        AssertContains(sourceText, "if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))");
        AssertContains(sourceText, "FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame");
        AssertContains(sourceText, "ffmpeg.av_frame_unref(_videoFrame);\n            return default;");
        var softwareOutputBlock = ExtractTextBetween(
            sourceText,
            "var outputSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);",
            "    private void CopyFramePlanesToBuffer");
        AssertContains(softwareOutputBlock, "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertOccursBefore(softwareOutputBlock, "CopyFramePlanesToBuffer((byte*)dataPtr, outputSize);", "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
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

    private static Task FlashbackDecoder_D3D11FramesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (!TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight, out var d3dFrameFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_d3d11_frame detail='{d3dFrameFailure}' w={_videoWidth} h={_videoHeight}\");\n                ffmpeg.av_frame_free(&clonedFrame);\n                return default;");
        AssertContains(sourceText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(sourceText, "failure = \"texture_null\";");
        AssertContains(sourceText, "failure = $\"subresource_out_of_range:{subresource}\";");
        AssertOccursBefore(sourceText, "TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight", "var texturePtr = (IntPtr)clonedFrame->data[0];");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_HeldFrameCleanupIsBestEffort()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"seek_keyframe_pending\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_replace_best\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_superseded\");");
        AssertContains(sourceText, "var bestFrameTransferred = false;");
        AssertContains(sourceText, "bestFrameTransferred = true;\n                        return true;");
        AssertContains(sourceText, "finally\n        {\n            if (!bestFrameTransferred && bestFrame != null)\n            {\n                ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_abandoned\");\n            }\n        }");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"close_pending\");");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_DecodeLoopsObserveCancellation()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "if (!SeekToKeyframe(target, cancellationToken))");
        AssertContains(sourceText, "if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))");
        AssertContains(sourceText, "if (!FeedNextVideoPacket(cancellationToken))");
        AssertContains(sourceText, "cancellationToken.ThrowIfCancellationRequested();");

        var seekToBlock = ExtractTextBetween(
            sourceText,
            "public bool SeekTo(TimeSpan target",
            "    /// <summary>\n    /// Decodes the next video frame.");
        AssertContains(seekToBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertOccursBefore(seekToBlock, "cancellationToken.ThrowIfCancellationRequested();\n                if (!TryDecodeNextVideoFrame", "if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))");

        var decodeBlock = ExtractTextBetween(
            sourceText,
            "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame",
            "    public void Dispose()");
        AssertContains(decodeBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(decodeBlock, "if (!FeedNextVideoPacket(cancellationToken))");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_RejectsInitializeAfterDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
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

        var sourceText = ReadFlashbackDecoderSource();
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
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var callbackProperty = decoderType.GetProperty("AudioChunkCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.AudioChunkCallback not found.");

        var callbackType = callbackProperty.PropertyType;
        var callbackParameter = Expression.Parameter(callbackType.GetGenericArguments()[0], "chunk");
        var callback = Expression.Lambda(callbackType, Expression.Empty(), callbackParameter).Compile();
        callbackProperty.SetValue(decoder, callback);

        decoder.Dispose();

        AssertEqual(null, callbackProperty.GetValue(decoder), "Disposed decoder clears audio callback");

        var sourceText = ReadFlashbackDecoderSource();
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
        var sinkText = ReadFlashbackEncoderSinkSource();
        var bufferText = ReadFlashbackBufferManagerSource();

        var rotateBlock = ExtractTextBetween(
            sinkText,
            "private bool RotateSegment(TimeSpan currentPts)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateBlock, "string? completedPath = null;");
        AssertContains(rotateBlock, "string? newPath = null;");
        AssertContains(rotateBlock, "var encoderRotated = false;");
        AssertContains(rotateBlock, "completedPath = _tsFilePath;");
        AssertContains(rotateBlock, "var completedStartPts = _segmentStartPts;");
        AssertContains(rotateBlock, "newPath = _bufferManager.GenerateSegmentPath();");
        AssertContains(rotateBlock, "encoderRotated = true;");
        AssertOccursBefore(rotateBlock, "encoderRotated = true;", "_tsFilePath = newPath;");
        AssertOccursBefore(rotateBlock, "_tsFilePath = newPath;", "_bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);");
        AssertContains(rotateBlock, "if (newPath != null && !encoderRotated)\n            {\n                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);\n            }");

        var abandonBlock = ExtractTextBetween(
            bufferText,
            "public void AbandonGeneratedSegmentPath",
            "    public void OnSegmentCompleted");
        AssertContains(abandonBlock, "if (IsSameSegmentPath(_activeSegmentPath, generatedPath))");
        AssertContains(abandonBlock, "_activeSegmentPath = restoreActivePath;");
        AssertContains(abandonBlock, "_nextSegmentIndex--;");
        AssertContains(abandonBlock, "if (!IsSameSegmentPath(generatedPath, restoreActivePath))");
        AssertContains(abandonBlock, "TryDeleteFile(generatedPath);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var cancelBlock = ExtractTextBetween(
            sourceText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL");
        AssertContains(cancelBlock, "Logger.Log(\"FLASHBACK_SINK_ENCODING_LOOP_CANCELLED\");");
        AssertContains(cancelBlock, "CompletePendingForceRotateWithEmptyResult();");
        AssertContains(cancelBlock, "var cancelPts = ResolveEncoderPts();");
        AssertContains(cancelBlock, "if (cancelPts > _segmentStartPts)");
        AssertContains(cancelBlock, "var cancelSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);");
        AssertContains(cancelBlock, "FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED");
        AssertContains(cancelBlock, "FLASHBACK_SINK_CANCELLED_SEGMENT_REGISTER_FAIL");
        AssertContains(cancelBlock, "ReturnAllRemainingQueuedBuffers();");
        AssertOccursBefore(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);", "ReturnAllRemainingQueuedBuffers();");

        var rotateFailureBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            if (newPath != null && !encoderRotated)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateFailureBlock, "Interlocked.Increment(ref _segmentRotationFailures);");
        AssertContains(rotateFailureBlock, "var failPts = ResolveEncoderPts();");
        AssertContains(rotateFailureBlock, "if (failPts > _segmentStartPts)");
        AssertContains(rotateFailureBlock, "var failSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL");
        AssertContains(rotateFailureBlock, "_segmentStartPts = currentPts;");
        AssertOccursBefore(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);", "_segmentStartPts = currentPts;");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateRejectsFailedEncoder()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "CancellationToken cancellationToken = default");
        AssertContains(forceRotateBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(forceRotateBlock, "if (inPoint < TimeSpan.Zero || outPoint <= inPoint)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE", "var request = new ForceRotateRequest();");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE");
        AssertContains(forceRotateBlock, "if (_encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Failed();");
        AssertContains(forceRotateBlock, "var request = new ForceRotateRequest();");
        AssertContains(forceRotateBlock, "if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK", "_forceRotateRequest = request;");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var loopBlock = ExtractTextBetween(
            sourceText,
            "if (Volatile.Read(ref _forceRotateRequested))",
            "                    madeProgress = true;\n                }");

        AssertContains(sourceText, "private sealed class ForceRotateRequest");
        AssertContains(sourceText, "public bool TryBeginCommit()\n            => Interlocked.CompareExchange(ref _state, StateCommitting, StatePending) == StatePending;");
        AssertContains(sourceText, "public bool TryCancel()");
        AssertContains(sourceText, "public void Complete(IReadOnlyList<string> paths)");
        AssertContains(loopBlock, "localRequest = _forceRotateRequest;\n                        _forceRotateRequest = null;");
        AssertContains(loopBlock, "if (localRequest == null)\n                        {\n                            Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request\");\n                            madeProgress = true;\n                            continue;\n                        }");
        AssertOccursBefore(loopBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request", "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(loopBlock, "if (localRequest.IsCompleted)\n                        {\n                            Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed\");\n                            madeProgress = true;\n                            continue;\n                        }");
        AssertOccursBefore(loopBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed", "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(loopBlock, "var forceRotateDrainAborted = ShouldAbortForceRotateDrain(localRequest, \"before_drain\", inFlightCount);");
        AssertContains(sourceText, "private const int AudioDrainBatchLimit = 128;");
        AssertContains(loopBlock, "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(loopBlock, "while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(loopBlock, "while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))");
        AssertContains(loopBlock, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))");
        AssertDoesNotContain(loopBlock, "while (DrainGpuPackets(gpuQueue.Reader))");
        AssertDoesNotContain(loopBlock, "while (DrainVideoPackets(videoQueue.Reader))");
        AssertContains(loopBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"audio\", inFlightCount))");
        AssertContains(loopBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"microphone\", inFlightCount))");
        AssertContains(loopBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"gpu\", inFlightCount))");
        AssertContains(loopBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"video\", inFlightCount))");
        AssertContains(loopBlock, "if (forceRotateDrainAborted)\n                        {\n                            madeProgress = true;\n                            continue;\n                        }");
        AssertOccursBefore(loopBlock, "if (forceRotateDrainAborted)\n                        {\n                            madeProgress = true;\n                            continue;\n                        }", "var currentPts = ResolveEncoderPts();");
        AssertContains(loopBlock, "if (localRequest.IsCompleted)\n                        {\n                            Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain\");\n                            madeProgress = true;\n                            continue;\n                        }");
        AssertOccursBefore(loopBlock, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))", "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain");
        AssertOccursBefore(loopBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain", "var currentPts = ResolveEncoderPts();");
        AssertContains(loopBlock, "if (!localRequest.TryBeginCommit())\n                            {\n                                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate\");\n                                madeProgress = true;\n                                continue;\n                            }");
        AssertOccursBefore(loopBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate", "if (!RotateSegment(currentPts))");
        AssertContains(sourceText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(sourceText, "if (!request.IsCompleted)");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_FORCE_ROTATE_ABORT_DRAIN phase={phase} in_flight_rounds={inFlightRounds}\");");
        AssertContains(sourceText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");
        AssertContains(loopBlock, "catch (Exception ex)\n                    {\n                        Logger.Log($\"FLASHBACK_SINK_FORCE_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}\");\n                        localRequest?.CompleteEmpty();\n                        throw;\n                    }");
        AssertOccursBefore(loopBlock, "localRequest?.CompleteEmpty();\n                        throw;", "finally\n                    {\n                        lock (_videoQueueSync)");
        AssertContains(loopBlock, "finally\n                    {\n                        lock (_videoQueueSync)\n                        {\n                            Volatile.Write(ref _forceRotateDraining, false);\n                        }\n                    }");

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "if (!request.Task.Wait(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken))");
        AssertContains(forceRotateBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED");
        AssertContains(forceRotateBlock, "var cancelled = TryCancelForceRotate(request);");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED");
        AssertContains(sourceText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateBlock, "if (request.Task.Wait(TimeSpan.FromMilliseconds(ForceRotateCommittedGraceMs)))\n                    {\n                        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());\n                    }");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED_PENDING");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.CommittedPending();");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.CanceledBeforeCommit();");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());");
        AssertDoesNotContain(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED\");\n                _ = request.Task.GetAwaiter().GetResult();");
        AssertDoesNotContain(forceRotateBlock, "return request.Task.Result;");
        AssertDoesNotContain(sourceText, "_forceRotateTcs");
        AssertDoesNotContain(sourceText, "localTcs.Task.IsCompleted");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

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
        var decoderText = ReadFlashbackDecoderSource();
        var sinkText = ReadFlashbackEncoderSinkSource();

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
        AssertContains(decoderText, "var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder");
        AssertContains(decoderText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(decoderText, "ffmpeg.avcodec_find_decoder_by_name(preferredName)");
        AssertContains(decoderText, "AVCodecID.AV_CODEC_ID_AV1 => \"av1\"");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        AssertContains(decoderText, "private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)");
        AssertContains(decoderText, "ffmpeg.avcodec_get_hw_config(codec, i)");
        AssertContains(decoderText, "pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11");
        AssertContains(decoderText, "deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA");
        AssertContains(decoderText, "AvCodecHwConfigMethodHwDeviceCtx");
        AssertContains(decoderText, "private static string FormatHardwareConfigMethods(int methods)");
        AssertContains(decoderText, "private static string GetPixelFormatName(AVPixelFormat pixelFormat)");
        AssertContains(decoderText, "private static string GetHardwareDeviceName(AVHWDeviceType deviceType)");
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

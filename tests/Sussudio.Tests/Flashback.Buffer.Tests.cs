using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
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

    // ── FlashbackBufferManager tests ──

    private static object CreateInitializedBufferManager(string tempDir)
    {
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
        SetPropertyBackingField(options, "TempDirectory", tempDir);
        SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

        var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var manager = RuntimeHelpers.GetUninitializedObject(managerType);
        SetPrivateField(manager, "_options", options);
        SetPrivateField(manager, "_indexLock", new object());
        SetPrivateField(manager, "_sessionId", "test-session");
        SetPrivateField(manager, "_sessionDirectory", tempDir);
        SetPrivateField(manager, "_activeSegmentPath", Path.Combine(tempDir, "fb_test_0003.ts"));
        SetPrivateField(manager, "_activeSegmentStartPtsTicks", -1L);
        SetPrivateField(manager, "_nextSegmentIndex", 4);

        // Initialize the completed segments list via reflection
        var listType = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listType.GetValue(manager);
        if (list == null)
        {
            // GetUninitializedObject skips ctor — create the list
            var csType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
            var listGenericType = typeof(List<>).MakeGenericType(csType);
            list = Activator.CreateInstance(listGenericType)!;
            listType.SetValue(manager, list);
        }

        return manager;
    }

    private static void AddCompletedSegment(object manager, string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)
    {
        var managerType = manager.GetType();
        var csType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
        var listField = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listField.GetValue(manager)!;
        var addMethod = list.GetType().GetMethod("Add")!;

        var countProp = list.GetType().GetProperty("Count")!;
        var seqNum = (int)countProp.GetValue(list)!;

        var segment = Activator.CreateInstance(csType, path, seqNum, startPts, endPts, sizeBytes)!;
        addMethod.Invoke(list, new[] { segment });
    }

    private static Task FlashbackBufferManager_MathHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var mathText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Math.cs")
            .Replace("\r\n", "\n");

        AssertContains(mathText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(mathText, "private static long SubtractNonNegative(long left, long right)");
        AssertContains(mathText, "private long GetCompletedSegmentBytesSaturated()");
        AssertContains(mathText, "private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)");
        AssertContains(mathText, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertContains(mathText, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(mathText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertDoesNotContain(rootText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertDoesNotContain(rootText, "private long GetCompletedSegmentBytesSaturated()");
        AssertDoesNotContain(rootText, "private static bool IsSameSegmentPath(string? left, string? right)");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentQueriesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var queryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.SegmentQueries.cs")
            .Replace("\r\n", "\n");

        AssertContains(queryText, "public int SegmentCount");
        AssertContains(queryText, "public string? ActiveFilePath");
        AssertContains(queryText, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)");
        AssertContains(queryText, "public string? GetValidSegmentFileForPosition(TimeSpan absolutePts)");
        AssertContains(queryText, "private string? GetOldestExistingSegmentPath()");
        AssertContains(queryText, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(queryText, "public string? GetNextSegmentFile(string currentPath)");
        AssertContains(queryText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(queryText, "public IReadOnlyList<string> GetValidSegmentPaths(TimeSpan inPoint, TimeSpan outPoint)");
        AssertContains(queryText, "private TimeSpan GetActiveSegmentStartPts()");
        AssertContains(queryText, "private TimeSpan GetDefaultActiveSegmentStartPts()");
        AssertContains(queryText, "public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()");
        AssertDoesNotContain(rootText, "public int SegmentCount");
        AssertDoesNotContain(rootText, "public string? ActiveFilePath");
        AssertDoesNotContain(rootText, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)");
        AssertDoesNotContain(rootText, "public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_LifecycleHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public bool IsSessionPreservedForRecovery");
        AssertContains(lifecycleText, "public void MarkSessionPreservedForRecovery()");
        AssertContains(lifecycleText, "public void SetSegmentExtension(string extension)");
        AssertContains(lifecycleText, "public void Initialize(string sessionId)");
        AssertContains(lifecycleText, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "private void ThrowIfDisposed()");
        AssertDoesNotContain(rootText, "public void MarkSessionPreservedForRecovery()");
        AssertDoesNotContain(rootText, "public void SetSegmentExtension(string extension)");
        AssertDoesNotContain(rootText, "public void Initialize(string sessionId)");
        AssertDoesNotContain(rootText, "public void Dispose()");
        AssertDoesNotContain(rootText, "private void ThrowIfDisposed()");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)\n        => GetValidSegmentFileForPosition(absolutePts);");

        // Add 3 segments: 0-5s, 5-10s, 10-15s
        var seg0 = Path.Combine(tempDir, "seg0.ts");
        var seg1 = Path.Combine(tempDir, "seg1.ts");
        var seg2 = Path.Combine(tempDir, "seg2.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(seg0, "segment");
        File.WriteAllText(seg1, "segment");
        File.WriteAllText(seg2, "segment");
        File.WriteAllText(active, "active");
        AddCompletedSegment(manager, seg0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 1000);
        AddCompletedSegment(manager, seg1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1000);
        AddCompletedSegment(manager, seg2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 1000);

        var method = manager.GetType().GetMethod("GetSegmentFileForPosition")!;

        // Position 7s → segment 1 (5-10s)
        var result1 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(7) }) as string;
        AssertEqual(seg1, result1!, "Position 7s");

        // Position 0s → segment 0 (0-5s)
        var result2 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(0) }) as string;
        AssertEqual(seg0, result2!, "Position 0s");

        // Position 20s → not in any completed segment → falls back to active
        var result3 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) }) as string;
        AssertContains(result3!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "if (string.IsNullOrWhiteSpace(path))\n        {\n            Logger.Log(\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=empty_path\");\n            return;\n        }");
        AssertContains(source, "if (endPts <= startPts)\n        {\n            Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=invalid_range path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds}\");\n            return;\n        }");
        AssertContains(source, "if (!IsPathInSessionDirectory(path))\n            {\n                Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=outside_session path='{Path.GetFileName(path)}'\");\n                return;\n            }");
        AssertContains(source, "if (!File.Exists(path))\n            {\n                Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=missing_file path='{Path.GetFileName(path)}'\");\n                return;\n            }");
        AssertContains(source, "var existingIndex = _completedSegments.FindIndex(seg => IsSameSegmentPath(seg.Path, path));");
        AssertContains(source, "if (existingIndex >= 0)\n            {\n                if (!TryExtendCompletedSegment(existingIndex, path, startPts, endPts, safeSizeBytes, pathIsActiveSegment))");
        AssertContains(source, "private bool TryExtendCompletedSegment(");
        AssertContains(source, "if (!pathIsActiveSegment && !existing.AllowSamePathExtension)");
        AssertContains(source, "AllowSamePathExtension = pathIsActiveSegment");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertContains(source, "if (_completedSegments.Count > 0 && startPts < _completedSegments[^1].EndPts)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=non_monotonic");
        AssertContains(source, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_PATH_WARN");
        AssertContains(source, "var safeSizeBytes = Math.Max(0, sizeBytes);");
        AssertContains(source, "private int _completedSegmentSequence;");
        AssertContains(source, "var sequenceNumber = _completedSegmentSequence++;");
        AssertContains(source, "_completedSegments.Add(new CompletedSegment(path, sequenceNumber, startPts, endPts, safeSizeBytes)\n            {\n                AllowSamePathExtension = pathIsActiveSegment\n            });");
        AssertContains(source, "_completedSegmentBytes = AddNonNegativeSaturated(_completedSegmentBytes, safeSizeBytes);");
        AssertContains(source, "_previousActiveSegmentBytes = pathIsActiveSegment ? safeSizeBytes : 0;");
        AssertContains(source, "_completedSegmentSequence = 0;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var missingSegmentPath = Path.Combine(tempDir, "segment-missing.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                missingSegmentPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });

            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Missing segment should not allocate sequence");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Missing segment should not update bytes");

            var segment0Path = Path.Combine(tempDir, "segment-0.ts");
            File.WriteAllBytes(segment0Path, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                segment0Path,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });
            var overlappingSegmentPath = Path.Combine(tempDir, "segment-overlap.ts");
            File.WriteAllBytes(overlappingSegmentPath, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(6),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Duplicate segment path should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Duplicate segment path should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                1500L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Non-active duplicate segment growth should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Non-active duplicate segment growth should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                overlappingSegmentPath,
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(7),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Overlapping segment should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Overlapping segment should not update bytes");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbtest_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                outsidePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                1200L
            });

            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Outside segment path should not update bytes");
            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Outside segment path should not allocate sequence");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            try { Directory.Delete(outsideDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbdelete_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var tryDeleteFile = manager.GetType().GetMethod("TryDeleteFile", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.TryDeleteFile not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            File.WriteAllText(outsidePath, "keep");

            var result = (bool)tryDeleteFile.Invoke(manager, new object[] { outsidePath })!;
            AssertEqual(false, result, "Outside delete should be rejected");
            AssertEqual(true, File.Exists(outsidePath), "Outside delete should preserve file");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session");
            AssertOccursBefore(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session", "File.Delete(filePath);");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            try { Directory.Delete(outsideDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "var activeEndPts = TimeSpan.FromTicks(Math.Max(activeStartPts.Ticks, Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var activeSizeBytes = Math.Max(0, _totalDiskBytes - _completedSegmentBytes);");
        AssertContains(source, "EndPtsMs = (long)activeEndPts.TotalMilliseconds,");
        AssertContains(source, "SizeBytes = activeSizeBytes,");
        AssertContains(source, "var safeActiveSegmentBytes = Math.Max(0, activeSegmentBytes);");
        AssertContains(source, "var accountedActiveSegmentBytes = safeActiveSegmentBytes;");
        AssertContains(source, "accountedActiveSegmentBytes = SubtractNonNegative(safeActiveSegmentBytes, _completedSegments[^1].SizeBytes);");
        AssertContains(source, "_totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, accountedActiveSegmentBytes);");
        AssertContains(source, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
        AssertContains(source, "private long GetCompletedSegmentBytesSaturated()");
        AssertContains(source, "_totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, retainedActiveBytes);");
        AssertContains(source, "freedBytes = AddNonNegativeSaturated(freedBytes, _completedSegments[i].SizeBytes);");
        AssertContains(source, "FLASHBACK_BUFFER_DELETE_WARN path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration()
    {
        var source = ReadFlashbackBufferManagerSource();
        var cleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");

        AssertContains(source, "var maxTicks = Math.Max(0, _options.BufferDuration.Ticks);");
        AssertContains(source, "var duration = NonNegativeDeltaTicks(ptsTicks, startTicks);");
        AssertContains(source, "var newStartTicks = Math.Max(0, ptsTicks - maxTicks);");
        AssertContains(source, "Interlocked.CompareExchange(ref _validStartPtsTicks, newStartTicks, startTicks);");
        AssertContains(source, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(source, "private static long SubtractNonNegative(long left, long right)");
        AssertContains(source, "private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)");
        AssertContains(source, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(source, "var totalDuration = NonNegativeDeltaTicks(latestTicks, startTicks);");
        AssertContains(source, "var evictTicks = ToNonNegativeLongSaturated(excessBytes / bytesPerTick);");
        AssertContains(source, "var newStart = AddNonNegativeSaturated(Math.Max(0, startTicks), evictTicks);");
        AssertContains(cleanupSource, "directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);");
        AssertContains(cleanupSource, "totalCacheBytes = AddNonNegativeSaturated(totalCacheBytes, directoryBytes);");
        AssertContains(cleanupSource, "totalCacheBytes = SubtractNonNegative(totalCacheBytes, candidate.SizeBytes);");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        updateDiskBytes.Invoke(manager, new object[] { 1000L });
        var completedPath = Path.Combine(tempDir, "completed-0.ts");
        File.WriteAllBytes(completedPath, new byte[] { 0x47 });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            completedPath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });
        AssertEqual(1200L, GetLongProperty(manager, "TotalBytesWritten"), "Final segment bytes counted at rotation");

        updateDiskBytes.Invoke(manager, new object[] { 100L });
        AssertEqual(1300L, GetLongProperty(manager, "TotalBytesWritten"), "First bytes from next segment counted after rotation");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SamePathCompletionExtendsLatestSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var getValidSegmentPaths = manager.GetType().GetMethod("GetValidSegmentPaths")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetValidSegmentPaths not found.");
            var getSegmentInfoList = manager.GetType().GetMethod("GetSegmentInfoList")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetSegmentInfoList not found.");

            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[] { 0x47 });

            updateDiskBytes.Invoke(manager, new object[] { 1000L });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                activePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                1000L
            });
            AssertEqual(1000L, GetLongProperty(manager, "TotalDiskBytes"), "Initial same-path completion tracks one physical active file");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Initial same-path completion does not double count active bytes");

            updateDiskBytes.Invoke(manager, new object[] { 1500L });
            AssertEqual(1500L, GetLongProperty(manager, "TotalDiskBytes"), "Same active file growth is counted as a delta after completion");
            AssertEqual(1500L, GetLongProperty(manager, "TotalBytesWritten"), "Same active file growth advances monotonic bytes by delta");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", Path.GetFileName(activePath)),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                2000L
            });

            var paths = ((IEnumerable<string>)getValidSegmentPaths.Invoke(manager, new object[]
            {
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(19)
            })!).ToArray();
            AssertEqual(1, paths.Length, "Extended same-path segment remains exportable for tail range");
            AssertEqual(activePath, paths[0], "Extended same-path segment export path");
            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Same-path extension keeps original segment sequence");
            AssertEqual(2000L, GetLongProperty(manager, "TotalDiskBytes"), "Extended same-path completion updates completed disk bytes");
            AssertEqual(2000L, GetLongProperty(manager, "TotalBytesWritten"), "Extended same-path completion advances monotonic bytes by growth delta");

            var infos = ((System.Collections.IEnumerable)getSegmentInfoList.Invoke(manager, Array.Empty<object>())!)
                .Cast<object>()
                .ToArray();
            var completedInfo = infos.First(info => GetPropertyValue(info, "IsActive") is false);
            AssertEqual(0L, (long)GetPropertyValue(completedInfo, "StartPtsMs")!, "Extended segment keeps original start");
            AssertEqual(20_000L, (long)GetPropertyValue(completedInfo, "EndPtsMs")!, "Extended segment updates end");
            AssertEqual(2000L, (long)GetPropertyValue(completedInfo, "SizeBytes")!, "Extended segment updates size");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_IgnoresUpdatesAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var updateLatestPts = manager.GetType().GetMethod("UpdateLatestPts")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateLatestPts not found.");
        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        ((IDisposable)manager).Dispose();

        updateLatestPts.Invoke(manager, new object[] { TimeSpan.FromSeconds(5) });
        updateDiskBytes.Invoke(manager, new object[] { 4096L });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            Path.Combine(tempDir, "completed-after-dispose.ts"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });

        AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "LatestPts")!, "Disposed manager ignores latest PTS updates");
        AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Disposed manager ignores disk and segment byte updates");
        AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Disposed manager does not allocate segment sequence");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private volatile bool _disposed;");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=disposed");
        AssertContains(source, "public void UpdateLatestPts(TimeSpan pts)\n    {\n        if (_disposed)\n        {\n            return;\n        }");
        AssertContains(source, "public void UpdateDiskBytes(long activeSegmentBytes)\n    {\n        if (_disposed)\n        {\n            return;\n        }");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");
        var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
            ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");
        var finalizeCycle = manager.GetType().GetMethod("FinalizeActiveSegmentForCycle")
            ?? throw new InvalidOperationException("FlashbackBufferManager.FinalizeActiveSegmentForCycle not found.");

        ((IDisposable)manager).Dispose();

        purgeCompleted.Invoke(manager, null);
        purgeAll.Invoke(manager, null);
        abandonGenerated.Invoke(manager, new object?[] { activePath, null });
        finalizeCycle.Invoke(manager, null);

        AssertEqual(false, File.Exists(completedPath), "Dispose purges completed segment before post-dispose purge attempts");
        AssertEqual(false, File.Exists(activePath), "Dispose purges active segment before post-dispose purge attempts");
        AssertEqual(0, GetIntProperty(manager, "SegmentCount"), "Disposed destructive operations keep the disposed empty index stable");
        AssertEqual(string.Empty, GetStringProperty(manager, "ActiveFilePath"), "Disposed destructive operations keep active path cleared");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_PURGE_SKIP reason=disposed");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=disposed");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PreservesMarkedRecoverySessions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_recovery_preserve_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var markPreserved = manager.GetType().GetMethod("MarkSessionPreservedForRecovery")
            ?? throw new InvalidOperationException("FlashbackBufferManager.MarkSessionPreservedForRecovery not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

        markPreserved.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "IsSessionPreservedForRecovery"), "Recovery-preserved manager exposes preserved state");
        SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(2).Ticks);
        InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives normal eviction");

        purgeAll.Invoke(manager, null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives explicit purge");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives explicit purge");

        ((IDisposable)manager).Dispose();

        AssertEqual(true, Directory.Exists(tempDir), "Recovery-preserved session directory survives dispose");
        AssertEqual(true, File.Exists(Path.Combine(tempDir, ".flashback-recovery-preserve")), "Recovery marker survives dispose");
        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives dispose");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives dispose");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private bool _preserveSessionForRecovery;");
        AssertContains(source, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_EVICT_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingOldest = Path.Combine(tempDir, "missing-oldest.ts");
        var existingFallback = Path.Combine(tempDir, "existing-fallback.ts");
        File.WriteAllText(existingFallback, "segment");

        AddCompletedSegment(manager, missingOldest, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingFallback, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentFileForPosition")!;

        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(existingFallback, fallback!, "Missing target should fall back to first existing completed segment");

        File.Delete(existingFallback);
        var missingAll = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(null, missingAll, "Missing completed and active segments should return null");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var oldest = Path.Combine(tempDir, "oldest.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(oldest, "oldest");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, oldest, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentFileForPosition")!;
        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(1) }) as string;

        AssertEqual(oldest, fallback!, "Position before first segment should use oldest existing segment, not active");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetNextSegmentFile_WalksForward()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        var c = Path.Combine(tempDir, "c.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");
        File.WriteAllText(c, "c");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, c, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;

        var nextA = method.Invoke(manager, new object[] { a }) as string;
        AssertEqual(b, nextA!, "a to b");

        var nextB = method.Invoke(manager, new object[] { b }) as string;
        AssertEqual(c, nextB!, "b to c");

        var nextC = method.Invoke(manager, new object[] { c }) as string;
        AssertContains(nextC!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(source, "Path.GetFullPath(left)");
        AssertContains(source, "Path.GetFullPath(right)");
        AssertContains(source, "FLASHBACK_BUFFER_PATH_COMPARE_WARN");
        AssertContains(source, "if (IsSameSegmentPath(_completedSegments[i].Path, currentPath))");
        AssertContains(source, "if (IsSameSegmentPath(seg.Path, path) && File.Exists(seg.Path))");
        AssertContains(source, "if (IsSameSegmentPath(_activeSegmentPath, path) &&");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var equivalentA = Path.Combine(tempDir, ".", "a.ts");
        var nextMethod = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = nextMethod.Invoke(manager, new object[] { equivalentA }) as string;
        AssertEqual(b, next!, "Equivalent completed segment path should walk to next segment");

        var startMethod = manager.GetType().GetMethod("GetSegmentStartPts")!;
        var start = (TimeSpan?)startMethod.Invoke(manager, new object[] { equivalentA });
        AssertEqual(TimeSpan.Zero, start!.Value, "Equivalent completed segment path should resolve start PTS");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentStartPts")!;

        var missingStart = (TimeSpan?)method.Invoke(manager, new object[] { missingCompleted });
        AssertEqual(null, missingStart, "Missing completed segment should not expose start PTS");

        var existingStart = (TimeSpan?)method.Invoke(manager, new object[] { existingCompleted });
        AssertEqual(TimeSpan.FromSeconds(5), existingStart!.Value, "Existing completed segment should expose start PTS");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var activeStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(TimeSpan.FromSeconds(12), activeStart!.Value, "Active segment should expose marked encoder start PTS");

        File.Delete(active);
        var missingActiveStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(null, missingActiveStart, "Missing active segment should not expose start PTS");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var current = Path.Combine(tempDir, "current.ts");
        var missingNext = Path.Combine(tempDir, "missing-next.ts");
        var existingNext = Path.Combine(tempDir, "existing-next.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(current, "current");
        File.WriteAllText(existingNext, "next");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, current, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, missingNext, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, existingNext, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = method.Invoke(manager, new object[] { current }) as string;

        AssertEqual(existingNext, next!, "Next segment lookup should skip missing indexed segment");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var s0 = Path.Combine(tempDir, "s0.ts");
        var s1 = Path.Combine(tempDir, "s1.ts");
        var s2 = Path.Combine(tempDir, "s2.ts");
        var s3 = Path.Combine(tempDir, "s3.ts");
        File.WriteAllText(s0, "segment");
        File.WriteAllText(s1, "segment");
        File.WriteAllText(s2, "segment");
        File.WriteAllText(s3, "segment");

        AddCompletedSegment(manager, s0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, s1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, s2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);
        AddCompletedSegment(manager, s3, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentPaths")!;

        // Range 3s-12s should include s0 (0-5 overlaps), s1 (5-10), s2 (10-15 overlaps)
        var result = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(12) })!;
        var count = GetCountProperty(result);
        AssertEqual(3, count, "3s-12s should span 3 segments");

        // Range 5s-5.5s should include only s1 (5-10)
        var narrow = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(1, GetCountProperty(narrow), "5s-5.5s should be 1 segment");

        File.Delete(s1);
        var missing = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(0, GetCountProperty(missing), "Missing overlapping file should not be returned");

        var emptyRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(8) })!;
        AssertEqual(0, GetCountProperty(emptyRange), "Empty range should not return segments");

        var invertedRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(3) })!;
        AssertEqual(0, GetCountProperty(invertedRange), "Inverted range should not return segments");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "if (!File.Exists(seg.Path))\n                {\n                    continue;\n                }");
        AssertContains(source, "if (_activeSegmentPath != null && File.Exists(_activeSegmentPath))");
        AssertContains(source, "SequenceNumber = Math.Max(0, _nextSegmentIndex - 1),");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentInfoList")!;
        var result = method.Invoke(manager, null)!;

        AssertEqual(2, GetCountProperty(result), "Segment info should include existing completed plus active");
        var infos = ((System.Collections.IEnumerable)result).Cast<object>().ToArray();
        var activeInfo = infos.Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(3, GetIntProperty(activeInfo, "SequenceNumber"), "Active segment sequence should match current generated segment index");
        AssertEqual(10_000L, GetLongProperty(activeInfo, "StartPtsMs"), "Unmarked active segment start should fall back to completed end");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var markedResult = method.Invoke(manager, null)!;
        var markedActiveInfo = ((System.Collections.IEnumerable)markedResult)
            .Cast<object>()
            .Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(12_000L, GetLongProperty(markedActiveInfo, "StartPtsMs"), "Marked active segment start should follow encoder PTS");

        File.Delete(active);
        var withoutActive = method.Invoke(manager, null)!;

        AssertEqual(1, GetCountProperty(withoutActive), "Segment info should omit missing active file");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_ActiveFilePath_RequiresExistingFile()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return _activeSegmentPath != null && File.Exists(_activeSegmentPath)\n                    ? _activeSegmentPath\n                    : null;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        AssertEqual(null, GetPropertyValue(manager, "ActiveFilePath"), "Missing active file should not be exposed");

        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(active, "active");

        AssertEqual(active, (string)GetPropertyValue(manager, "ActiveFilePath")!, "Existing active file should be exposed");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentCount_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return _completedSegments.Count(seg => File.Exists(seg.Path)) +\n                    (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? 1 : 0);");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        AssertEqual(2, GetIntProperty(manager, "SegmentCount"), "Segment count should include existing completed plus active");

        File.Delete(active);

        AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count should omit missing active file");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_bytes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed and active bytes");

            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            var deleteDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (File.Exists(firstSegment) && DateTime.UtcNow < deleteDeadline)
            {
                Thread.Sleep(25);
            }

            AssertEqual(false, File.Exists(firstSegment), "Eviction should delete the expired completed segment");
            AssertEqual(true, File.Exists(secondSegment), "Eviction should retain overlapping completed segment");
            AssertEqual(250L, GetLongProperty(manager, "TotalDiskBytes"), "Eviction subtracts deleted completed segment bytes");
            AssertEqual(200L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache matches retained segment");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_locked_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            SetPrivateField(manager, "_sessionDirectory", Path.Combine(tempDir, "different-session"));
            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            AssertEqual(true, File.Exists(firstSegment), "Rejected expired segment remains on disk");
            AssertEqual(true, File.Exists(secondSegment), "Later segment is not evicted past a rejected predecessor");
            AssertEqual(3, GetIntProperty(manager, "SegmentCount"), "Rejected completed segments remain tracked with active segment");
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Rejected segment bytes stay in disk accounting");
            AssertEqual(300L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache retains rejected segment");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "if (DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"valid_window\"))");
            AssertContains(source, "private static bool DeleteEvictedFile");
            AssertContains(source, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictionPauseResume_Balanced()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var pauseMethod = manager.GetType().GetMethod("PauseEviction")!;
        var resumeMethod = manager.GetType().GetMethod("ResumeEviction")!;

        // Initially not paused
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "Initial EvictionPaused");

        // Pause → paused
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 pause");

        // Double-pause → still paused (count-based)
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 2 pauses");

        // Resume once → still paused (count = 1)
        resumeMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 resume (count=1)");

        // Resume again → unpaused (count = 0)
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After 2 resumes (count=0)");

        // Extra resume → remains unpaused and must not underflow the pause counter.
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After unbalanced resume");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(source, "var unbalancedEndPts = ClampEndPtsToStart(_recordingStartPts, _recordingEndPts);");
        AssertContains(source, "_recordingEndPts = ClampEndPtsToStart(\n                    _recordingStartPts,\n                    TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var rangeSeconds = TimeSpan.FromTicks(NonNegativeDeltaTicks(_recordingEndPts.Ticks, _recordingStartPts.Ticks)).TotalSeconds;");
        AssertContains(source, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertDoesNotContain(source, "range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_startup_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            SetPrivateField(manager, "_activeSegmentPath", null);
            var startingIndex = (int)GetPrivateField(manager, "_nextSegmentIndex")!;
            var getFilePath = manager.GetType().GetMethod("AcquireSegmentPath", new[] { typeof(bool).MakeByRefType() })
                ?? throw new InvalidOperationException("FlashbackBufferManager.AcquireSegmentPath(out bool) not found.");
            var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
                ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");

            object?[] args = { false };
            var generatedPath = (string)getFilePath.Invoke(manager, args)!;
            AssertEqual(true, (bool)args[0]!, "Fresh AcquireSegmentPath reports generated path");
            AssertEqual(generatedPath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Generated path becomes raw active segment");
            AssertEqual(startingIndex + 1, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Generated path advances segment index");

            File.WriteAllBytes(generatedPath, new byte[17]);
            abandonGenerated.Invoke(manager, new object?[] { generatedPath, null });

            AssertEqual<string?>(null, (string?)GetPrivateField(manager, "_activeSegmentPath"), "Abandon clears startup-generated active path");
            AssertEqual(false, File.Exists(generatedPath), "Abandon deletes partial startup segment file");
            AssertEqual(startingIndex, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Abandon rolls back generated segment index");

            object?[] retryArgs = { false };
            var retryPath = (string)getFilePath.Invoke(manager, retryArgs)!;
            AssertEqual(true, (bool)retryArgs[0]!, "Retry after abandon generates a fresh path");
            AssertEqual(generatedPath, retryPath, "Retry reuses the rolled-back segment slot");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_RemovesStaleLegacyRootSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_legacy_cleanup_{Guid.NewGuid():N}");
        object? manager = null;
        Directory.CreateDirectory(tempDir);

        try
        {
            var staleRootSegment = Path.Combine(tempDir, "fb_legacy_0001.ts");
            var recentRootSegment = Path.Combine(tempDir, "fb_recent_0001.ts");
            var unrelatedFile = Path.Combine(tempDir, "unrelated.ts");
            File.WriteAllText(staleRootSegment, "stale");
            File.WriteAllText(recentRootSegment, "recent");
            File.WriteAllText(unrelatedFile, "keep");

            File.SetLastWriteTimeUtc(staleRootSegment, DateTime.UtcNow - TimeSpan.FromHours(13));
            File.SetLastWriteTimeUtc(recentRootSegment, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(unrelatedFile, DateTime.UtcNow - TimeSpan.FromHours(13));

            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            manager = Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");
            initialize.Invoke(manager, new object[] { "current-session" });

            AssertEqual(false, File.Exists(staleRootSegment), "Stale root fb_* segment removed");
            AssertEqual(true, File.Exists(recentRootSegment), "Recent root fb_* segment preserved");
            AssertEqual(true, File.Exists(unrelatedFile), "Unrelated root file preserved");
            AssertEqual(true, Directory.Exists(Path.Combine(tempDir, "current-session")), "Current session directory created");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PurgesRetainLockedActivePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_locked_active_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        string? activePath = null;

        try
        {
            activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[50]);
            File.SetAttributes(activePath, File.GetAttributes(activePath) | FileAttributes.ReadOnly);

            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
            var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Setup tracks active bytes");
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Setup tracks active bytes written");

            purgeCompleted.Invoke(manager, null);

            AssertEqual(true, File.Exists(activePath), "Read-only active file remains on disk");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Read-only active path remains tracked");
            AssertEqual(activePath, GetStringProperty(manager, "ActiveFilePath"), "ActiveFilePath still reports read-only active segment");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Read-only active bytes remain in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Read-only active byte baseline is preserved");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Same active bytes are not double-counted after failed purge");

            purgeAll.Invoke(manager, null);
            AssertEqual(true, File.Exists(activePath), "Read-only active file remains after full purge attempt");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Full purge keeps read-only active path tracked");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Full purge segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge keeps read-only active bytes in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Full purge keeps read-only active byte baseline");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(activePath) && File.Exists(activePath))
            {
                try { File.SetAttributes(activePath, FileAttributes.Normal); } catch { }
            }

            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_full_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var completedPath = Path.Combine(tempDir, "completed.ts");
            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(completedPath, new byte[300]);
            File.WriteAllBytes(activePath, new byte[50]);
            AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 300L);
            SetPrivateField(manager, "_completedSegmentBytes", 300L);
            SetPrivateField(manager, "_totalDiskBytes", 350L);

            var purgeCore = manager.GetType().GetMethod("PurgeAllSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegmentsCore not found.");
            var result = purgeCore.Invoke(manager, null)!;
            var segments = Convert.ToInt32(result.GetType().GetField("Item1")!.GetValue(result));
            var freedBytes = Convert.ToInt64(result.GetType().GetField("Item2")!.GetValue(result));

            AssertEqual(2, segments, "Full purge reports completed plus active segment");
            AssertEqual(350L, freedBytes, "Full purge reports completed plus active bytes exactly once");
            AssertEqual(false, File.Exists(completedPath), "Full purge deletes completed segment");
            AssertEqual(false, File.Exists(activePath), "Full purge deletes active segment");
            AssertEqual(0L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge resets total disk bytes");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Full purge resets monotonic bytes for a new buffer session");

            var source = ReadFlashbackBufferManagerSource();
            var purgeCoreBlock = ExtractTextBetween(
                source,
                "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()",
                "    private void EvictOldestSegments()");
            AssertOccursBefore(purgeCoreBlock, "var activeBytes = _activeSegmentPath != null", "if (_activeSegmentPath != null)");
            AssertContains(purgeCoreBlock, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
            AssertContains(purgeCoreBlock, "var retainedActiveBytes = _activeSegmentPath != null ? activeBytes : 0;");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_partial_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        FileStream? lockedCompleted = null;

        try
        {
            var completedPath = Path.Combine(tempDir, "completed-locked.ts");
            var deletableCompletedPath = Path.Combine(tempDir, "completed-deletable.ts");
            var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
            File.WriteAllBytes(completedPath, new byte[100]);
            File.WriteAllBytes(deletableCompletedPath, new byte[200]);
            File.WriteAllBytes(activePath, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                completedPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                deletableCompletedPath,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed plus active bytes");

            lockedCompleted = new FileStream(completedPath, FileMode.Open, FileAccess.Read, FileShare.None);
            purgeCompleted.Invoke(manager, null);

            AssertEqual(false, File.Exists(activePath), "Partial purge should still delete stale active segment");
            AssertEqual(false, File.Exists(deletableCompletedPath), "Partial purge deletes unlocked completed segments");
            AssertEqual(true, File.Exists(completedPath), "Partial purge retains locked completed segments");
            AssertEqual(100L, GetLongProperty(manager, "TotalDiskBytes"), "Partial purge subtracts deleted completed and active bytes");
            AssertEqual(100L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Partial purge preserves retained completed byte accounting");
            AssertEqual(0L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Partial purge resets active byte baseline");

            updateDiskBytes.Invoke(manager, new object[] { 25L });
            AssertEqual(125L, GetLongProperty(manager, "TotalDiskBytes"), "Next active bytes are added to retained completed bytes");
            AssertEqual(375L, GetLongProperty(manager, "TotalBytesWritten"), "Next active segment bytes are counted after purge baseline reset");
        }
        finally
        {
            lockedCompleted?.Dispose();
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }


    private static Task FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_stale_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var staleFlashbackSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var unrelatedEmptyDirectory = Path.Combine(tempDir, "empty-but-not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(staleFlashbackSession);
            Directory.CreateDirectory(unrelatedEmptyDirectory);

            var staleTime = DateTime.UtcNow - TimeSpan.FromHours(13);
            Directory.SetLastWriteTimeUtc(staleFlashbackSession, staleTime);
            Directory.SetLastWriteTimeUtc(unrelatedEmptyDirectory, staleTime);

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupStaleSessionDirectories", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupStaleSessionDirectories not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession });

            AssertEqual(true, Directory.Exists(currentSession), "Current empty session directory preserved");
            AssertEqual(false, Directory.Exists(staleFlashbackSession), "Plausible stale empty flashback session removed");
            AssertEqual(true, Directory.Exists(unrelatedEmptyDirectory), "Unrelated stale empty directory preserved");

            var cleanupSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Sussudio", "Services", "Flashback", "FlashbackStartupCacheCleanup.cs"))
                .Replace("\r\n", "\n");
            var scannerSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Sussudio", "Services", "Flashback", "FlashbackSessionRecoveryScanner.cs"))
                .Replace("\r\n", "\n");
            AssertContains(cleanupSource, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
            AssertContains(scannerSource, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");
            AssertContains(scannerSource, "internal static bool IsLowerHexString(ReadOnlySpan<char> value)");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_TrimsStartupSessionCacheBudget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cache_budget_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, "current-session");
            var oldSession = Path.Combine(tempDir, "old-session");
            var recentSession = Path.Combine(tempDir, "recent-session");
            var preservedSession = Path.Combine(tempDir, "preserved-session");
            var nonFlashbackDirectory = Path.Combine(tempDir, "not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(oldSession);
            Directory.CreateDirectory(recentSession);
            Directory.CreateDirectory(preservedSession);
            Directory.CreateDirectory(nonFlashbackDirectory);

            WriteSizedFile(Path.Combine(currentSession, "fb_current_0001.ts"), 1);
            WriteSizedFile(Path.Combine(oldSession, "fb_old_0001.ts"), 20);
            WriteSizedFile(Path.Combine(recentSession, "fb_recent_0001.ts"), 10);
            WriteSizedFile(Path.Combine(preservedSession, "fb_preserved_0001.ts"), 100);
            File.WriteAllText(Path.Combine(preservedSession, ".flashback-recovery-preserve"), "keep");
            File.WriteAllText(Path.Combine(nonFlashbackDirectory, "notes.txt"), "keep");

            var now = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(Path.Combine(oldSession, "fb_old_0001.ts"), now - TimeSpan.FromHours(2));
            File.SetLastWriteTimeUtc(Path.Combine(recentSession, "fb_recent_0001.ts"), now - TimeSpan.FromMinutes(5));

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupSessionCacheBudget", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupSessionCacheBudget not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession, 25L });

            AssertEqual(true, Directory.Exists(currentSession), "Current session preserved");
            AssertEqual(false, Directory.Exists(oldSession), "Oldest session removed to satisfy budget");
            AssertEqual(true, Directory.Exists(recentSession), "Recent session preserved once budget is satisfied");
            AssertEqual(true, Directory.Exists(preservedSession), "Recovery-preserved session skipped");
            AssertEqual(true, Directory.Exists(nonFlashbackDirectory), "Non-flashback directory preserved");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_RejectsUnsafeSessionIds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_session_id_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");

            try
            {
                initialize.Invoke(manager, new object[] { "..\\outside-session" });
                throw new InvalidOperationException("Expected unsafe session id to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
            }

            AssertEqual(false, Directory.Exists(Path.Combine(Directory.GetParent(tempDir)!.FullName, "outside-session")), "Unsafe session id must not create outside directory");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_ValidatesSegmentExtensions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_segment_ext_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "safe-session" });

            var setExtension = managerType.GetMethod("SetSegmentExtension")
                ?? throw new InvalidOperationException("SetSegmentExtension not found.");
            var generatePath = managerType.GetMethod("GenerateSegmentPath")
                ?? throw new InvalidOperationException("GenerateSegmentPath not found.");

            setExtension.Invoke(manager, new object[] { ".TS" });
            var transportPath = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, transportPath.EndsWith(".ts", StringComparison.Ordinal), "Transport stream extension normalized");

            setExtension.Invoke(manager, new object[] { ".Mp4" });
            var mp4Path = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, mp4Path.EndsWith(".mp4", StringComparison.Ordinal), "MP4 extension normalized");

            try
            {
                setExtension.Invoke(manager, new object[] { "..\\escape.ts" });
                throw new InvalidOperationException("Expected unsafe segment extension to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static void WriteSizedFile(string path, int byteCount)
    {
        File.WriteAllBytes(path, Enumerable.Repeat((byte)0x47, byteCount).ToArray());
    }
}

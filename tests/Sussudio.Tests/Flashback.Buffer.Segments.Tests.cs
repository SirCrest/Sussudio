using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
}

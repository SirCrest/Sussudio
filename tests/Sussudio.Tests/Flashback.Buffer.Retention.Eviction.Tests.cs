using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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

        // Pause â†’ paused
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 pause");

        // Double-pause â†’ still paused (count-based)
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 2 pauses");

        // Resume once â†’ still paused (count = 1)
        resumeMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 resume (count=1)");

        // Resume again â†’ unpaused (count = 0)
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After 2 resumes (count=0)");

        // Extra resume â†’ remains unpaused and must not underflow the pause counter.
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After unbalanced resume");

        var source = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.EvictionPause.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Math.cs").Replace("\r\n", "\n"));
        AssertContains(source, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(source, "var unbalancedEndPts = ClampEndPtsToStart(_recordingStartPts, _recordingEndPts);");
        AssertContains(source, "_recordingEndPts = ClampEndPtsToStart(\n                    _recordingStartPts,\n                    TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var rangeSeconds = TimeSpan.FromTicks(NonNegativeDeltaTicks(_recordingEndPts.Ticks, _recordingStartPts.Ticks)).TotalSeconds;");
        AssertContains(source, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertDoesNotContain(source, "range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");

        return Task.CompletedTask;
    }
}

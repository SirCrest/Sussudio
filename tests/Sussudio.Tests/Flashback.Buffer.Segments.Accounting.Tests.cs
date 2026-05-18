using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
        var budgetSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupSessionCacheBudget.cs")
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
        AssertContains(budgetSource, "directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);");
        AssertContains(budgetSource, "totalCacheBytes = AddNonNegativeSaturated(totalCacheBytes, directoryBytes);");
        AssertContains(budgetSource, "totalCacheBytes = SubtractNonNegative(totalCacheBytes, candidate.SizeBytes);");

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
}

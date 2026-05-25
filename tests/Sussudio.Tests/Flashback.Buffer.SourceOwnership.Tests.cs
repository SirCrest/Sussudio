using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackBufferManager_SegmentMutationLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var segmentsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentsText, "public string AcquireSegmentPath(out bool generated)");
        AssertContains(segmentsText, "public string GenerateSegmentPath()");
        AssertContains(segmentsText, "public void MarkActiveSegmentStart(string path, TimeSpan startPts)");
        AssertContains(segmentsText, "public void AbandonGeneratedSegmentPath(string generatedPath, string? restoreActivePath)");
        AssertContains(segmentsText, "public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)");
        AssertContains(segmentsText, "private bool TryExtendCompletedSegment(");
        AssertContains(segmentsText, "FLASHBACK_BUFFER_SEGMENT_COMPLETE");
        AssertContains(segmentsText, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertDoesNotContain(rootText, "public string AcquireSegmentPath(out bool generated)");
        AssertDoesNotContain(rootText, "public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)");
        AssertDoesNotContain(rootText, "private bool TryExtendCompletedSegment(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_LiveAccountingLivesWithRootState()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public void ResetLatestPts()");
        AssertContains(rootText, "public void FinalizeActiveSegmentForCycle()");
        AssertContains(rootText, "public double EncodeFrameRate { get; set; }");
        AssertContains(rootText, "public void UpdateLatestPts(TimeSpan pts)");
        AssertContains(rootText, "public void UpdateDiskBytes(long activeSegmentBytes)");
        AssertContains(rootText, "FLASHBACK_BUFFER_DISK_EVICT");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.LiveAccounting.cs")),
            "FlashbackBufferManager.LiveAccounting.cs folded into FlashbackBufferManager.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_MathHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var mathText = rootText;

        AssertContains(mathText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(mathText, "private static long SubtractNonNegative(long left, long right)");
        AssertContains(mathText, "private long GetCompletedSegmentBytesSaturated()");
        AssertContains(mathText, "private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)");
        AssertContains(mathText, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertContains(mathText, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(mathText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Math.cs")),
            "FlashbackBufferManager.Math.cs folded into FlashbackBufferManager.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentQueriesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var queryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs")
            .Replace("\r\n", "\n");
        var pathSafetyText = queryText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(queryText, "public int SegmentCount");
        AssertContains(queryText, "public string? ActiveFilePath");
        AssertContains(queryText, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)");
        AssertContains(queryText, "public string? GetValidSegmentFileForPosition(TimeSpan absolutePts)");
        AssertContains(queryText, "private string? GetOldestExistingSegmentPath()");
        AssertContains(queryText, "public string? GetNextSegmentFile(string currentPath)");
        AssertContains(queryText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(queryText, "public IReadOnlyList<string> GetValidSegmentPaths(TimeSpan inPoint, TimeSpan outPoint)");
        AssertContains(pathSafetyText, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(pathSafetyText, "FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator");
        AssertContains(pathSafetyText, "FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, sessionRoot)");
        AssertContains(pathSafetyText, "FLASHBACK_BUFFER_SEGMENT_PATH_WARN");

        AssertContains(queryText, "private TimeSpan GetActiveSegmentStartPts()");
        AssertContains(queryText, "private TimeSpan GetDefaultActiveSegmentStartPts()");
        AssertContains(queryText, "public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()");
        AssertDoesNotContain(rootText, "public int SegmentCount");
        AssertDoesNotContain(rootText, "public string? ActiveFilePath");
        AssertDoesNotContain(rootText, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)");
        AssertDoesNotContain(rootText, "public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()");
        AssertContains(docsText, "FlashbackBufferManager.Segments.cs");
        AssertContains(docsText, "session-directory path safety");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_LifecycleHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var recoveryPreserveText = lifecycleText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public void SetSegmentExtension(string extension)");
        AssertContains(lifecycleText, "public void Initialize(string sessionId)");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "private void ThrowIfDisposed()");
        AssertContains(recoveryPreserveText, "public bool IsSessionPreservedForRecovery");
        AssertContains(recoveryPreserveText, "public void MarkSessionPreservedForRecovery()");
        AssertContains(recoveryPreserveText, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(recoveryPreserveText, "RecoveryPreserveMarkerFileName");
        AssertContains(recoveryPreserveText, "FLASHBACK_RECOVERY_PRESERVE_MARKER");
        AssertContains(recoveryPreserveText, "FLASHBACK_RECOVERY_PRESERVE_MARKER_CHECK_WARN");
        AssertDoesNotContain(rootText, "public void MarkSessionPreservedForRecovery()");
        AssertDoesNotContain(rootText, "public void SetSegmentExtension(string extension)");
        AssertDoesNotContain(rootText, "public void Initialize(string sessionId)");
        AssertDoesNotContain(rootText, "public void Dispose()");
        AssertDoesNotContain(rootText, "private void ThrowIfDisposed()");
        AssertContains(docsText, "FlashbackBufferManager.Segments.cs");
        AssertContains(docsText, "FlashbackBufferManager.Lifecycle.cs");
        AssertContains(docsText, "recovery-preserve state");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PurgeLivesWithLifecycleCleanup()
    {
        var retentionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public void PurgeCompletedSegments()");
        AssertContains(lifecycleText, "public void PurgeAllSegments()");
        AssertContains(lifecycleText, "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()");
        AssertContains(lifecycleText, "private bool TryDeleteFile(string filePath)");
        AssertContains(lifecycleText, "FLASHBACK_PURGE_PARTIAL");
        AssertContains(lifecycleText, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(lifecycleText, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session");
        AssertContains(lifecycleText, "var (purgedSegments, purgedBytes) = PurgeAllSegmentsCore();");
        AssertContains(retentionText, "public void PauseEviction()");
        AssertContains(retentionText, "public (TimeSpan StartPts, TimeSpan EndPts) ResumeEviction()");
        AssertContains(retentionText, "public bool IsDiskWarningActive");
        AssertContains(retentionText, "public TimeSpan RecordingStartPts");
        AssertContains(retentionText, "public TimeSpan RecordingEndPts");
        AssertContains(retentionText, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(retentionText, "private void EvictOldestSegments()");
        AssertContains(retentionText, "private bool DeleteFileForEviction(string filePath, long sizeBytes, string reason)");
        AssertContains(retentionText, "private static bool DeleteEvictedFile(string fullPath, string sessionRoot, long sizeBytes, string reason)");
        AssertDoesNotContain(retentionText, "public void PurgeCompletedSegments()");
        AssertDoesNotContain(retentionText, "public void PurgeAllSegments()");
        AssertDoesNotContain(retentionText, "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Purge.cs")),
            "FlashbackBufferManager.Purge.cs folded into FlashbackBufferManager.Lifecycle.cs");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackBufferManager_SegmentMutationLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var segmentMutationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.SegmentMutation.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentMutationText, "public string AcquireSegmentPath(out bool generated)");
        AssertContains(segmentMutationText, "public string GenerateSegmentPath()");
        AssertContains(segmentMutationText, "public void MarkActiveSegmentStart(string path, TimeSpan startPts)");
        AssertContains(segmentMutationText, "public void AbandonGeneratedSegmentPath(string generatedPath, string? restoreActivePath)");
        AssertContains(segmentMutationText, "public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)");
        AssertContains(segmentMutationText, "private bool TryExtendCompletedSegment(");
        AssertContains(segmentMutationText, "FLASHBACK_BUFFER_SEGMENT_COMPLETE");
        AssertContains(segmentMutationText, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertDoesNotContain(rootText, "public string AcquireSegmentPath(out bool generated)");
        AssertDoesNotContain(rootText, "public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)");
        AssertDoesNotContain(rootText, "private bool TryExtendCompletedSegment(");

        return Task.CompletedTask;
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
}

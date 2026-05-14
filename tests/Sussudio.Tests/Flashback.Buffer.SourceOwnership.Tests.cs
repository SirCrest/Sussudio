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
}

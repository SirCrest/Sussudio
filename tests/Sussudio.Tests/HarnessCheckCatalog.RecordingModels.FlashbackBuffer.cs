using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelFlashbackBufferChecksAsync(List<CheckResult> results)
    {
        // --- FlashbackBufferManager ---
        await AddCheckAsync(results,
            "FlashbackBufferManager Initialize clears recording PTS",
            FlashbackBufferManager_InitializeClearsRecordingPts);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment lookup returns correct file for position",
            FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment completion rejects invalid metadata",
            FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment completion rejects outside paths",
            FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths);
        await AddCheckAsync(results,
            "FlashbackBufferManager delete helper rejects outside paths",
            FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment diagnostics clamp active counters",
            FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters);
        await AddCheckAsync(results,
            "FlashbackBufferManager math helpers live in focused partial",
            FlashbackBufferManager_MathHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment query helpers live in focused partial",
            FlashbackBufferManager_SegmentQueriesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment mutation lives in focused partial",
            FlashbackBufferManager_SegmentMutationLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager lifecycle helpers live in focused partial",
            FlashbackBufferManager_LifecycleHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager purge helpers live in focused partial",
            FlashbackBufferManager_PurgeLivesInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager latest PTS clamps invalid buffer duration",
            FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment rotation keeps total bytes written monotonic",
            FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic);
        await AddCheckAsync(results,
            "FlashbackBufferManager same-path completion extends latest segment",
            FlashbackBufferManager_SamePathCompletionExtendsLatestSegment);
        await AddCheckAsync(results,
            "FlashbackBufferManager ignores updates after dispose",
            FlashbackBufferManager_IgnoresUpdatesAfterDispose);
        await AddCheckAsync(results,
            "FlashbackBufferManager ignores destructive operations after dispose",
            FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose);
        await AddCheckAsync(results,
            "FlashbackBufferManager valid segment lookup skips missing files",
            FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager stale left-edge lookup uses oldest segment",
            FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest);
        await AddCheckAsync(results,
            "FlashbackBufferManager GetNextSegmentFile walks forward through segments",
            FlashbackBufferManager_GetNextSegmentFile_WalksForward);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment path lookups normalize equivalent paths",
            FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment start PTS skips missing files",
            FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager GetNextSegmentFile skips missing indexed segments",
            FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments);
        await AddCheckAsync(results,
            "FlashbackBufferManager GetValidSegmentPaths returns overlapping segments",
            FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment info skips missing files",
            FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager active file path requires existing file",
            FlashbackBufferManager_ActiveFilePath_RequiresExistingFile);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment count skips missing files",
            FlashbackBufferManager_SegmentCount_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager eviction updates disk byte totals",
            FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes);
        await AddCheckAsync(results,
            "FlashbackBufferManager eviction keeps rejected segments accounted",
            FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted);
        await AddCheckAsync(results,
            "FlashbackBufferManager eviction pause and resume are balanced",
            FlashbackBufferManager_EvictionPauseResume_Balanced);
        await AddCheckAsync(results,
            "FlashbackBufferManager abandons startup-generated segment paths",
            FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath);
        await AddCheckAsync(results,
            "FlashbackBufferManager purges retain locked active segment path",
            FlashbackBufferManager_PurgesRetainLockedActivePath);
        await AddCheckAsync(results,
            "FlashbackBufferManager partial purge accounts for deleted active segment",
            FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge);
        await AddCheckAsync(results,
            "FlashbackBufferManager full purge reports active bytes once",
            FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce);
        await AddCheckAsync(results,
            "FlashbackBufferManager removes stale legacy root segments",
            FlashbackBufferManager_RemovesStaleLegacyRootSegments);
        await AddCheckAsync(results,
            "FlashbackBufferManager preserves unrelated empty temp directories",
            FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories);
        await AddCheckAsync(results,
            "FlashbackBufferManager trims startup session cache budget",
            FlashbackBufferManager_TrimsStartupSessionCacheBudget);
        await AddCheckAsync(results,
            "FlashbackBufferManager rejects unsafe session ids",
            FlashbackBufferManager_RejectsUnsafeSessionIds);
        await AddCheckAsync(results,
            "FlashbackBufferManager validates segment extensions",
            FlashbackBufferManager_ValidatesSegmentExtensions);
    }
}

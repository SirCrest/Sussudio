using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelRecordingContractChecksAsync(List<CheckResult> results)
    {
        // --- RecordingContracts ---
        await AddCheckAsync(results,
            "FinalizeResult.Success produces empty preserved list",
            FinalizeResult_Success_ProducesEmptyPreservedList);
        await AddCheckAsync(results,
            "FinalizeResult.Failure deduplicates and filters preserved artifacts",
            FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts);

        // --- RecordingArtifactManager ---
        await AddCheckAsync(results,
            "FinalizeContext returns success when post-mux audio disabled",
            ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled);
        await AddCheckAsync(results,
            "FinalizeContext preserves temp artifacts when mux fails",
            ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails);
        await AddCheckAsync(results,
            "FinalizeContext rejects invalid final output",
            ArtifactManager_FinalizeContext_RejectsInvalidFinalOutput);
        await AddCheckAsync(results,
            "RollbackAsync deletes all artifacts when post-mux enabled",
            ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled);
        await AddCheckAsync(results,
            "RollbackAsync is safe with null context",
            ArtifactManager_RollbackAsync_SafeWithNullContext);

        // --- RecordingStats ---
        await AddCheckAsync(results,
            "RecordingStats computes totals and preserves estimate flag",
            RecordingStats_ComputesTotalsAndPreservesEstimateFlag);
    }
}

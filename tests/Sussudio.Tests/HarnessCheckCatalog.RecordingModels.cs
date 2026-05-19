using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelChecksAsync(List<CheckResult> results)
    {
        await AddRecordingModelLibAvSinkChecksAsync(results);
        await AddRecordingModelCaptureRuntimeChecksAsync(results);
        await AddRecordingModelRecordingContractChecksAsync(results);
        await AddRecordingModelFlashbackBufferChecksAsync(results);
        await AddRecordingModelSnapshotChecksAsync(results);
    }

    private static async Task AddRecordingModelLibAvSinkChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "LibAv recording drain loop interleaves audio with bounded video batches",
            LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches);
        await AddCheckAsync(results,
            "LibAv recording encoding loop and packet drains live in focused partials",
            LibAvRecordingSink_EncodingLoopAndPacketDrainsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "LibAv recording audio queues live in focused partial",
            LibAvRecordingSink_AudioQueuesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording video queue submission lives in focused partial",
            LibAvRecordingSink_VideoQueueSubmissionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording startup and stop lifecycle live in focused partials",
            LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials);
    }

    private static async Task AddRecordingModelCaptureRuntimeChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Strict HFR fatal handler clears active session state",
            CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState);
        await AddCheckAsync(results,
            "Capture errors refresh ViewModel runtime flags",
            CaptureErrors_RefreshViewModelRuntimeFlags);
    }

    private static async Task AddRecordingModelRecordingContractChecksAsync(List<CheckResult> results)
    {
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
        await AddCheckAsync(results,
            "RecordingStats computes totals and preserves estimate flag",
            RecordingStats_ComputesTotalsAndPreservesEstimateFlag);
    }

    private static async Task AddRecordingModelSnapshotChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "CaptureDiagnosticsSnapshot preserves diagnostics telemetry contract and MJPEG ownership",
            CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry);
        await AddCheckAsync(results,
            "CaptureHealthSnapshot extends diagnostics with health telemetry",
            CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync);
    }

}

using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackPlaybackStartupChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback playback initial state is live",
            FlashbackPlaybackController_InitialState_IsLive);
        await AddCheckAsync(results,
            "Flashback playback commands no-op before initialize",
            FlashbackPlaybackController_CommandsNoOpBeforeInitialize);
        await AddCheckAsync(results,
            "Flashback playback successful no-ops clear stale failures",
            FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure);
        await AddCheckAsync(results,
            "Flashback playback coalesced commands clear stale failures",
            FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure);
        await AddCheckAsync(results,
            "Flashback playback worker exit rearms future commands",
            FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart);
        await AddCheckAsync(results,
            "Flashback playback command queue accepts newest control when full",
            FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull);
    }

    private static async Task AddFlashbackPlaybackTimelineChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback in/out points default to unset",
            FlashbackPlaybackController_InOutPoints_DefaultToUnset);
        await AddCheckAsync(results,
            "Flashback in/out points clear invalid counterpart",
            FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart);
        await AddCheckAsync(results,
            "Flashback in/out point setters normalize markers",
            FlashbackPlaybackController_InOutPointSettersNormalizeMarkers);
        await AddCheckAsync(results,
            "Flashback in/out point changes stop after dispose",
            FlashbackPlaybackController_InOutPointChangesStopAfterDispose);
        await AddCheckAsync(results,
            "Flashback clamp bounds stale markers to buffered duration",
            FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration);
        await AddCheckAsync(results,
            "Flashback command positions clamp before file lookup",
            FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup);
        await AddCheckAsync(results,
            "Flashback playback timestamp arithmetic is saturating",
            FlashbackPlaybackController_TimestampArithmeticIsSaturating);
        await AddCheckAsync(results,
            "Flashback end-of-segment open failures snap live",
            FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive);
        await AddCheckAsync(results,
            "Flashback normal playback uses tight near-live snap",
            FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap);
        await AddCheckAsync(results,
            "Flashback snap-live clears open file identity",
            FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity);
        await AddCheckAsync(results,
            "Flashback pause from live displays a buffered frame before paused",
            FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused);
        await AddCheckAsync(results,
            "Flashback playback guards invalid decoder frame rates",
            FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps);
        await AddCheckAsync(results,
            "Flashback playback PTS cadence telemetry tracks mismatches",
            FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches);
        await AddCheckAsync(results,
            "Flashback nudge opens decoder after pause from live",
            FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused);
        await AddCheckAsync(results,
            "Flashback playback releases decoded frames after submit failures",
            FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames);
        await AddCheckAsync(results,
            "Flashback playback guards fMP4 reopen retries",
            FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded);
        await AddCheckAsync(results,
            "Flashback scrub coalescing does not requeue control commands",
            FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands);
        await AddCheckAsync(results,
            "Flashback seek slots preserve control command barriers",
            FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers);
        await AddCheckAsync(results,
            "Flashback seek slots preserve slot state after rejected barriers",
            FlashbackPlaybackController_SeekSlots_PreserveSlotStateAfterRejectedBarriers);
        await AddCheckAsync(results,
            "Flashback playback transitions use best-effort audio preview guards",
            FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards);
        await AddCheckAsync(results,
            "Flashback playback metric reset clears decode timings",
            FlashbackPlaybackController_ResetClearsDecodeMetrics);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackEncoderSinkCoreChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback encoder resolves fractional frame rates",
            FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates);
        await AddCheckAsync(results,
            "Flashback encoder maps codec names",
            FlashbackEncoderSink_MapCodecName_MapsFormats);
        await AddCheckAsync(results,
            "Flashback encoder counters default to zero",
            FlashbackEncoderSink_CountersDefaultToZero);
        await AddCheckAsync(results,
            "Flashback encoder bounds high-resolution CPU queue capacity",
            FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded);
        await AddCheckAsync(results,
            "Flashback export throttle responds to live queue pressure",
            CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure);
        await AddCheckAsync(results,
            "Flashback encoder force-rotate drain rejects video enqueues",
            FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues);
        await AddCheckAsync(results,
            "Flashback encoder start failure rolls back started state",
            FlashbackEncoderSink_StartFailureRollsBackStartedState);
        await AddCheckAsync(results,
            "Flashback encoder dispose resets GPU queue depth",
            FlashbackEncoderSink_DisposeResetsGpuQueueDepth);
        await AddCheckAsync(results,
            "Flashback encoder PTS guards invalid frame rates",
            FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate);
    }

    private static async Task AddFlashbackEncoderSinkDrainChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback encoder sink restores active segment after rotation failure",
            FlashbackEncoderSink_RotateFailureRestoresActiveSegment);
        await AddCheckAsync(results,
            "Flashback encoder sink registers segments on cancellation and rotation failure",
            FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure);
        await AddCheckAsync(results,
            "Flashback encoder sink rejects force rotate after encoder failure",
            FlashbackEncoderSink_ForceRotateRejectsFailedEncoder);
        await AddCheckAsync(results,
            "Flashback encoder sink skips completed force rotate requests",
            FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest);
        await AddCheckAsync(results,
            "Flashback encoder sink logs fatal segment registration failures",
            FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged);
        await AddCheckAsync(results,
            "Flashback encoder sink validates audio packets before rent",
            FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent);
        await AddCheckAsync(results,
            "Flashback encoder sink interleaves audio with bounded video batches",
            FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches);
        await AddCheckAsync(results,
            "Flashback encoder sink packet drains live in focused partial",
            FlashbackEncoderSink_PacketDrainLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback encoder sink queue cleanup lives in focused partial",
            FlashbackEncoderSink_QueueCleanupLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback encoder sink startup lives in focused partial",
            FlashbackEncoderSink_StartupLivesInFocusedPartial);
    }
}

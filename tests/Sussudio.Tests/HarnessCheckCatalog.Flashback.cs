using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackChecksAsync(List<CheckResult> results)
    {
        // --- FlashbackPlaybackState enum ---
        await AddCheckAsync(results,
            "Flashback models preserve buffer session and export contracts",
            FlashbackModels_PreserveBufferSessionExportContracts);
        await AddCheckAsync(results,
            "Flashback buffer options max disk bytes scales with duration",
            FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration);
        await AddCheckAsync(results,
            "FlashbackPlaybackState enum has all expected states",
            FlashbackPlaybackState_HasAllExpectedStates);
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
            "Flashback playback transitions use best-effort audio preview guards",
            FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards);
        await AddCheckAsync(results,
            "Flashback playback metric reset clears decode timings",
            FlashbackPlaybackController_ResetClearsDecodeMetrics);
        await AddCheckAsync(results,
            "Flashback decoder calculates NV12 frame buffer sizes",
            FlashbackDecoder_CalculateFrameBufferSize_Nv12);
        await AddCheckAsync(results,
            "Flashback decoder calculates P010 frame buffer sizes",
            FlashbackDecoder_CalculateFrameBufferSize_P010);
        await AddCheckAsync(results,
            "Flashback decoder validation helpers live in focused partial",
            FlashbackDecoder_ValidationHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder lifetime cleanup lives in focused partial",
            FlashbackDecoder_LifetimeCleanupLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder diagnostics and guards live in focused partials",
            FlashbackDecoder_DiagnosticsAndGuardsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "Flashback decoder output types live in focused file",
            FlashbackDecoder_OutputTypesLiveInFocusedFile);
        await AddCheckAsync(results,
            "Flashback decoder video setup lives in focused partial",
            FlashbackDecoder_VideoSetupLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder seeking lives in focused partial",
            FlashbackDecoder_SeekingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder defaults to closed state",
            FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized);
        await AddCheckAsync(results,
            "Flashback decoder dispose before initialize is safe",
            FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow);
        await AddCheckAsync(results,
            "Flashback decoder unreferences discarded audio frames",
            FlashbackDecoder_DiscardedAudioFramesAreUnreffed);
        await AddCheckAsync(results,
            "Flashback decoder MJPEG playback uses low-latency single-thread decode",
            FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode);
        await AddCheckAsync(results,
            "Flashback decoder rejects invalid timestamps",
            FlashbackDecoder_PtsConversionRejectsInvalidTimestamps);
        await AddCheckAsync(results,
            "Flashback decoder input streams and frame sizes are bounded",
            FlashbackDecoder_InputStreamsAndFrameSizesAreBounded);
        await AddCheckAsync(results,
            "Flashback decoder audio output buffers are bounded",
            FlashbackDecoder_AudioOutputBuffersAreBounded);
        await AddCheckAsync(results,
            "Flashback decoder audio setup lives in audio output partial",
            FlashbackDecoder_AudioSetupLivesInAudioOutputPartial);
        await AddCheckAsync(results,
            "Flashback decoder software frame planes are validated",
            FlashbackDecoder_SoftwareFramePlanesAreValidated);
        await AddCheckAsync(results,
            "Flashback decoder D3D11 frames are validated",
            FlashbackDecoder_D3D11FramesAreValidated);
        await AddCheckAsync(results,
            "Flashback decoder held-frame cleanup is best effort",
            FlashbackDecoder_HeldFrameCleanupIsBestEffort);
        await AddCheckAsync(results,
            "Flashback decoder decode loops observe cancellation",
            FlashbackDecoder_DecodeLoopsObserveCancellation);
        await AddCheckAsync(results,
            "Flashback decoder rejects initialize after dispose",
            FlashbackDecoder_RejectsInitializeAfterDispose);
        await AddCheckAsync(results,
            "Flashback decoder clears audio callback on dispose",
            FlashbackDecoder_ClearsAudioCallbackOnDispose);
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
            "Flashback encoder sink startup lives in focused partial",
            FlashbackEncoderSink_StartupLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback suppressed exceptions use app logs",
            FlashbackSuppressedExceptionsUseAppLogs);
        await AddCheckAsync(results,
            "Flashback exporter cleanup ignores nonexistent directories",
            FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory);
        await AddCheckAsync(results,
            "Flashback exporter cleanup deletes orphaned temp files",
            FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles);
        await AddCheckAsync(results,
            "Flashback exporter does not scan user output directory for orphans",
            FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans);
        await AddCheckAsync(results,
            "Flashback exporter task wrappers dispose linked cancellation",
            FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation);
        await AddCheckAsync(results,
            "Flashback exporter ownership is split across focused partials",
            FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials);
        await AddCheckAsync(results,
            "Flashback exporter rejects null requests",
            FlashbackExporter_RejectsNullRequests);
        await AddCheckAsync(results,
            "Flashback exporter fails when input file is missing",
            FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound);
        await AddCheckAsync(results,
            "Flashback exporter fails when output path is empty",
            FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty);
        await AddCheckAsync(results,
            "Flashback exporter fails when no segment paths are provided",
            FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments);
        await AddCheckAsync(results,
            "Flashback exporter output path validation returns failure",
            FlashbackExporter_OutputPathValidation_ReturnsFailure);
        await AddCheckAsync(results,
            "Flashback export failure classifier maps command failures",
            FlashbackExportFailureClassifier_MapsCommandFailures);
        await AddCheckAsync(results,
            "Flashback exporter rejects directory output paths",
            FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory);
        await AddCheckAsync(results,
            "Flashback exporter rejects invalid export ranges",
            FlashbackExporter_RejectsInvalidExportRanges);
        await AddCheckAsync(results,
            "Flashback rejected export diagnostics preserve attempted range",
            FlashbackExportRejectedDiagnostics_PreserveAttemptedRange);
        await AddCheckAsync(results,
            "Flashback exporter rejects empty segment paths",
            FlashbackExporter_RejectsEmptySegmentPaths);
        await AddCheckAsync(results,
            "Flashback exporter rejects duplicate segment paths",
            FlashbackExporter_RejectsDuplicateSegmentPaths);
        await AddCheckAsync(results,
            "Flashback exporter progress callbacks are best effort",
            FlashbackExporter_ProgressCallbacksAreBestEffort);
        await AddCheckAsync(results,
            "Flashback exporter releases buffered segment packets on failures",
            FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures);
        await AddCheckAsync(results,
            "Flashback exporter timestamp conversions are saturating",
            FlashbackExporter_TimestampConversionsAreSaturating);
        await AddCheckAsync(results,
            "Flashback exporter input stream counts are bounded",
            FlashbackExporter_InputStreamCountsAreBounded);
        await AddCheckAsync(results,
            "Flashback exporter segment template validation guards missing video streams",
            FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream);
        await AddCheckAsync(results,
            "Flashback exporter fails when requested segments are skipped",
            FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped);
        await AddCheckAsync(results,
            "Flashback exporter returns cancellation result while waiting for export lock",
            FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled);
        await AddCheckAsync(results,
            "Flashback exporter cancellation wins before validation",
            FlashbackExporter_CancellationWinsBeforeValidation);
        await AddCheckAsync(results,
            "Flashback exporter fails fast when segment files are gone",
            FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone);
        await AddCheckAsync(results,
            "Flashback exporter dispose timeout does not tear down active native state",
            FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState);
        await AddCheckAsync(results,
            "Flashback exporter rejects output paths that overwrite source segments",
            FlashbackExporter_RejectsOutputPathThatOverwritesSource);
        await AddCheckAsync(results,
            "Flashback exporter invalid temp output preserves existing exports",
            FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport);
        await AddCheckAsync(results,
            "Flashback exporter refuses to overwrite existing destination when force is false",
            FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse);
        await AddCheckAsync(results,
            "Flashback exporter overwrites existing destination when force is true",
            FlashbackExporter_OverwritesWhenForceTrue);
        await AddCheckAsync(results,
            "Flashback exporter deletes invalid moved final outputs",
            FlashbackExporter_FinalValidationFailureDeletesMovedOutput);
        await AddCheckAsync(results,
            "Flashback exporter rejects blocked temp output paths before native export",
            FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport);

        // --- RecordingPipelineOptions ---
    }
}

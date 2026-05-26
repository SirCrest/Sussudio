using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackDecoderContractsTests
{
    public FlashbackDecoderContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackDecoderCalculatesNv12FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_Nv12();

    [Fact]
    public Task FlashbackDecoderCalculatesP010FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_P010();

    [Fact]
    public Task FlashbackDecoderValidationHelpersLiveWithRootLifecycle()
        => global::Program.FlashbackDecoder_ValidationHelpersLiveWithRootLifecycle();

    [Fact]
    public Task FlashbackDecoderLifetimeCleanupLivesWithRootLifecycle()
        => global::Program.FlashbackDecoder_LifetimeCleanupLivesWithRootLifecycle();

    [Fact]
    public Task FlashbackDecoderStateGuardsAndTimingLiveWithOwners()
        => global::Program.FlashbackDecoder_StateGuardsAndTimingLiveWithOwners();

    [Fact]
    public Task FlashbackDecoderOutputTypesLiveWithDecoderRoot()
        => global::Program.FlashbackDecoder_OutputTypesLiveWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderVideoSetupOwnsHardwareAndSoftwareSetup()
        => global::Program.FlashbackDecoder_VideoSetupOwnsHardwareAndSoftwareSetup();

    [Fact]
    public Task FlashbackDecoderSeekingLivesInFocusedPartial()
        => global::Program.FlashbackDecoder_SeekingLivesInFocusedPartial();

    [Fact]
    public Task FlashbackDecoderDecodeLoopLivesInFocusedPartial()
        => global::Program.FlashbackDecoder_DecodeLoopLivesInFocusedPartial();

    [Fact]
    public Task FlashbackDecoderDefaultsToClosedState()
        => global::Program.FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized();

    [Fact]
    public Task FlashbackDecoderDisposeBeforeInitializeIsSafe()
        => global::Program.FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow();

    [Fact]
    public Task FlashbackDecoderUnreferencesDiscardedAudioFrames()
        => global::Program.FlashbackDecoder_DiscardedAudioFramesAreUnreffed();

    [Fact]
    public Task FlashbackDecoderMjpegPlaybackUsesLowLatencySingleThreadDecode()
        => global::Program.FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode();

    [Fact]
    public Task FlashbackDecoderRejectsInvalidTimestamps()
        => global::Program.FlashbackDecoder_PtsConversionRejectsInvalidTimestamps();

    [Fact]
    public Task FlashbackDecoderInputStreamsAndFrameSizesAreBounded()
        => global::Program.FlashbackDecoder_InputStreamsAndFrameSizesAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioOutputBuffersAreBounded()
        => global::Program.FlashbackDecoder_AudioOutputBuffersAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioSetupLivesInAudioOutputPartial()
        => global::Program.FlashbackDecoder_AudioSetupLivesInAudioOutputPartial();

    [Fact]
    public Task FlashbackDecoderSoftwareFramePlanesAreValidated()
        => global::Program.FlashbackDecoder_SoftwareFramePlanesAreValidated();

    [Fact]
    public Task FlashbackDecoderD3D11FramesAreValidated()
        => global::Program.FlashbackDecoder_D3D11FramesAreValidated();

    [Fact]
    public Task FlashbackDecoderHeldFrameCleanupIsBestEffort()
        => global::Program.FlashbackDecoder_HeldFrameCleanupIsBestEffort();

    [Fact]
    public Task FlashbackDecoderDecodeLoopsObserveCancellation()
        => global::Program.FlashbackDecoder_DecodeLoopsObserveCancellation();

    [Fact]
    public Task FlashbackDecoderRejectsInitializeAfterDispose()
        => global::Program.FlashbackDecoder_RejectsInitializeAfterDispose();

    [Fact]
    public Task FlashbackDecoderClearsAudioCallbackOnDispose()
        => global::Program.FlashbackDecoder_ClearsAudioCallbackOnDispose();
}

public sealed class FlashbackEncoderSinkContractsTests
{
    public FlashbackEncoderSinkContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackEncoderResolvesFractionalFrameRates()
        => global::Program.FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates();

    [Fact]
    public Task FlashbackEncoderMapsCodecNames()
        => global::Program.FlashbackEncoderSink_MapCodecName_MapsFormats();

    [Fact]
    public Task FlashbackEncoderCountersDefaultToZero()
        => global::Program.FlashbackEncoderSink_CountersDefaultToZero();

    [Fact]
    public Task FlashbackEncoderBoundsHighResolutionCpuQueueCapacity()
        => global::Program.FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded();

    [Fact]
    public Task FlashbackExportThrottleRespondsToLiveQueuePressure()
        => global::Program.CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure();

    [Fact]
    public Task FlashbackEncoderForceRotateDrainRejectsVideoEnqueues()
        => global::Program.FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues();

    [Fact]
    public Task FlashbackEncoderStartFailureRollsBackStartedState()
        => global::Program.FlashbackEncoderSink_StartFailureRollsBackStartedState();

    [Fact]
    public Task FlashbackEncoderDisposeResetsGpuQueueDepth()
        => global::Program.FlashbackEncoderSink_DisposeResetsGpuQueueDepth();

    [Fact]
    public Task FlashbackEncoderPtsGuardsInvalidFrameRates()
        => global::Program.FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate();

    [Fact]
    public Task FlashbackEncoderSinkRestoresActiveSegmentAfterRotationFailure()
        => global::Program.FlashbackEncoderSink_RotateFailureRestoresActiveSegment();

    [Fact]
    public Task FlashbackEncoderSinkRegistersSegmentsOnCancellationAndRotationFailure()
        => global::Program.FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure();

    [Fact]
    public Task FlashbackEncoderSinkRejectsForceRotateAfterEncoderFailure()
        => global::Program.FlashbackEncoderSink_ForceRotateRejectsFailedEncoder();

    [Fact]
    public Task FlashbackEncoderSinkSkipsCompletedForceRotateRequests()
        => global::Program.FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest();

    [Fact]
    public Task FlashbackEncoderSinkLogsFatalSegmentRegistrationFailures()
        => global::Program.FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged();

    [Fact]
    public Task FlashbackEncoderSinkValidatesAudioPacketsBeforeRent()
        => global::Program.FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent();

    [Fact]
    public Task FlashbackEncoderSinkInterleavesAudioWithBoundedVideoBatches()
        => global::Program.FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches();

    [Fact]
    public Task FlashbackEncoderSinkEncodingThreadWorkLivesInEncodingLoop()
        => global::Program.FlashbackEncoderSink_EncodingThreadWorkLivesInEncodingLoop();

    [Fact]
    public Task FlashbackEncoderSinkQueueCleanupLivesInFocusedPartial()
        => global::Program.FlashbackEncoderSink_QueueCleanupLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkStartupLivesInFocusedPartial()
        => global::Program.FlashbackEncoderSink_StartupLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkRootOwnsConstructionAndRuntimeSurface()
        => global::Program.FlashbackEncoderSink_RootOwnsConstructionAndRuntimeSurface();

    [Fact]
    public Task FlashbackEncoderSinkForceRotateLivesInFocusedPartial()
        => global::Program.FlashbackEncoderSink_ForceRotateLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkStopAndDisposeLifecyclesShareShutdownOwner()
        => global::Program.FlashbackEncoderSink_StopAndDisposeLifecyclesShareShutdownOwner();

    [Fact]
    public Task FlashbackEncoderSinkProducerInputsLiveInCohesivePartial()
        => global::Program.FlashbackEncoderSink_ProducerInputsLiveInCohesivePartial();

    [Fact]
    public Task FlashbackEncoderSinkRuntimeStateLivesWithRoot()
        => global::Program.FlashbackEncoderSink_RuntimeStateLivesWithRoot();

    [Fact]
    public Task FlashbackEncoderSinkRecordingLifecycleLivesWithRootRuntimeSurface()
        => global::Program.FlashbackEncoderSink_RecordingLifecycleLivesWithRootRuntimeSurface();

    [Fact]
    public Task FlashbackEncoderSinkOptionsHelpersLiveWithStartup()
        => global::Program.FlashbackEncoderSink_OptionsHelpersLiveWithStartup();
}

public sealed class FlashbackExporterContractsTests
{
    public FlashbackExporterContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackSuppressedExceptionsUseAppLogs()
        => global::Program.FlashbackSuppressedExceptionsUseAppLogs();

    [Fact]
    public Task FlashbackExporterCleanupIgnoresNonexistentDirectories()
        => global::Program.FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory();

    [Fact]
    public Task FlashbackExporterCleanupDeletesOrphanedTempFiles()
        => global::Program.FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles();

    [Fact]
    public Task FlashbackExporterDoesNotScanUserOutputDirectoryForOrphans()
        => global::Program.FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans();

    [Fact]
    public Task FlashbackExporterTaskWrappersDisposeLinkedCancellation()
        => global::Program.FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation();

    [Fact]
    public Task FlashbackExporterOwnershipIsSplitAcrossFocusedPartials()
        => global::Program.FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials();

    [Fact]
    public Task FlashbackExporterRejectsNullRequests()
        => global::Program.FlashbackExporter_RejectsNullRequests();

    [Fact]
    public Task FlashbackExporterFailsWhenInputFileIsMissing()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound();

    [Fact]
    public Task FlashbackExporterFailsWhenOutputPathIsEmpty()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty();

    [Fact]
    public Task FlashbackExporterFailsWhenNoSegmentPathsAreProvided()
        => global::Program.FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments();

    [Fact]
    public Task FlashbackExporterOutputPathValidationReturnsFailure()
        => global::Program.FlashbackExporter_OutputPathValidation_ReturnsFailure();

    [Fact]
    public Task FlashbackExportFailureClassifierMapsCommandFailures()
        => global::Program.FlashbackExportFailureClassifier_MapsCommandFailures();

    [Fact]
    public Task FlashbackExporterRejectsDirectoryOutputPaths()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory();

    [Fact]
    public Task FlashbackExporterRejectsInvalidExportRanges()
        => global::Program.FlashbackExporter_RejectsInvalidExportRanges();

    [Fact]
    public Task FlashbackRejectedExportDiagnosticsPreserveAttemptedRange()
        => global::Program.FlashbackExportRejectedDiagnostics_PreserveAttemptedRange();

    [Fact]
    public Task FlashbackExporterRejectsEmptySegmentPaths()
        => global::Program.FlashbackExporter_RejectsEmptySegmentPaths();

    [Fact]
    public Task FlashbackExporterRejectsDuplicateSegmentPaths()
        => global::Program.FlashbackExporter_RejectsDuplicateSegmentPaths();

    [Fact]
    public Task FlashbackExporterProgressCallbacksAreBestEffort()
        => global::Program.FlashbackExporter_ProgressCallbacksAreBestEffort();

    [Fact]
    public Task FlashbackExporterReleasesBufferedSegmentPacketsOnFailures()
        => global::Program.FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures();

    [Fact]
    public Task FlashbackExporterTimestampConversionsAreSaturating()
        => global::Program.FlashbackExporter_TimestampConversionsAreSaturating();

    [Fact]
    public Task FlashbackExporterInputStreamCountsAreBounded()
        => global::Program.FlashbackExporter_InputStreamCountsAreBounded();

    [Fact]
    public Task FlashbackExporterSegmentTemplateValidationGuardsMissingVideoStreams()
        => global::Program.FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream();

    [Fact]
    public Task FlashbackExporterFailsWhenRequestedSegmentsAreSkipped()
        => global::Program.FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped();

    [Fact]
    public Task FlashbackExporterReturnsCancellationResultWhileWaitingForExportLock()
        => global::Program.FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled();

    [Fact]
    public Task FlashbackExporterCancellationWinsBeforeValidation()
        => global::Program.FlashbackExporter_CancellationWinsBeforeValidation();

    [Fact]
    public Task FlashbackExporterFailsFastWhenSegmentFilesAreGone()
        => global::Program.FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone();

    [Fact]
    public Task FlashbackExporterDisposeTimeoutDoesNotTearDownActiveNativeState()
        => global::Program.FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState();

    [Fact]
    public Task FlashbackExporterRejectsOutputPathsThatOverwriteSourceSegments()
        => global::Program.FlashbackExporter_RejectsOutputPathThatOverwritesSource();

    [Fact]
    public Task FlashbackExporterInvalidTempOutputPreservesExistingExports()
        => global::Program.FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationWhenForceIsFalse()
        => global::Program.FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse();

    [Fact]
    public Task FlashbackExporterOverwritesExistingDestinationWhenForceIsTrue()
        => global::Program.FlashbackExporter_OverwritesWhenForceTrue();

    [Fact]
    public Task FlashbackExporterDeletesInvalidMovedFinalOutputs()
        => global::Program.FlashbackExporter_FinalValidationFailureDeletesMovedOutput();

    [Fact]
    public Task FlashbackExporterRejectsBlockedTempOutputPathsBeforeNativeExport()
        => global::Program.FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport();
}

public sealed class FlashbackPlaybackContractsTests
{
    public FlashbackPlaybackContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackPlaybackInitialStateIsLive()
        => global::Program.FlashbackPlaybackController_InitialState_IsLive();

    [Fact]
    public Task FlashbackPlaybackCommandsNoOpBeforeInitialize()
        => global::Program.FlashbackPlaybackController_CommandsNoOpBeforeInitialize();

    [Fact]
    public Task FlashbackPlaybackSuccessfulNoOpsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackCoalescedCommandsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackWorkerExitRearmsFutureCommands()
        => global::Program.FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart();

    [Fact]
    public Task FlashbackPlaybackCommandQueueAcceptsNewestControlWhenFull()
        => global::Program.FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull();

    [Fact]
    public Task FlashbackCommandPositionsClampBeforeFileLookup()
        => global::Program.FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup();

    [Fact]
    public Task FlashbackPlaybackTimestampArithmeticIsSaturating()
        => global::Program.FlashbackPlaybackController_TimestampArithmeticIsSaturating();

    [Fact]
    public Task FlashbackEndOfSegmentOpenFailuresSnapLive()
        => global::Program.FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive();

    [Fact]
    public Task FlashbackNormalPlaybackUsesTightNearLiveSnap()
        => global::Program.FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap();

    [Fact]
    public Task FlashbackSnapLiveClearsOpenFileIdentity()
        => global::Program.FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity();

    [Fact]
    public Task FlashbackPauseFromLiveDisplaysBufferedFrameBeforePaused()
        => global::Program.FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused();

    [Fact]
    public Task FlashbackPlaybackGuardsInvalidDecoderFrameRates()
        => global::Program.FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps();

    [Fact]
    public Task FlashbackPlaybackPtsCadenceTelemetryTracksMismatches()
        => global::Program.FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches();

    [Fact]
    public Task FlashbackNudgeOpensDecoderAfterPauseFromLive()
        => global::Program.FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused();

    [Fact]
    public Task FlashbackPlaybackReleasesDecodedFramesAfterSubmitFailures()
        => global::Program.FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames();

    [Fact]
    public Task FlashbackPlaybackGuardsFmp4ReopenRetries()
        => global::Program.FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded();

    [Fact]
    public Task FlashbackScrubCoalescingDoesNotRequeueControlCommands()
        => global::Program.FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands();

    [Fact]
    public Task FlashbackSeekSlotsPreserveControlCommandBarriers()
        => global::Program.FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers();

    [Fact]
    public Task FlashbackSeekSlotsPreserveSlotStateAfterRejectedBarriers()
        => global::Program.FlashbackPlaybackController_SeekSlots_PreserveSlotStateAfterRejectedBarriers();

    [Fact]
    public Task FlashbackPlaybackTransitionsUseBestEffortAudioPreviewGuards()
        => global::Program.FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards();

    [Fact]
    public Task FlashbackPlaybackMetricResetClearsDecodeTimings()
        => global::Program.FlashbackPlaybackController_ResetClearsDecodeMetrics();
}

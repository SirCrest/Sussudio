using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class RecordingPipelineContractsTests
{
    public RecordingPipelineContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingVideoQueuesFailExplicitlyInsteadOfEvictingFrames()
        => global::Program.RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames();

    [Fact]
    public Task RecordingBackendFinalizeAndCleanupPreservesFlashbackBoundaries()
        => global::Program.RecordingBackendFinalizeAndCleanup_PreservesFlashbackBoundaries();

    [Fact]
    public Task RecordingBackendFlashbackBufferCyclePreservesCommittedPolicies()
        => global::Program.RecordingBackendFlashbackBufferCycle_PreservesPolicies();

    [Fact]
    public Task CaptureServiceRecordingLifecycleAndBackendResourcesHaveFocusedOwners()
        => global::Program.CaptureService_RecordingLifecycleAndBackendResourcesHaveFocusedOwners();

    [Fact]
    public Task CaptureServiceRecordingRollbackLivesInFocusedPartial()
        => global::Program.CaptureService_RecordingRollbackLivesInFocusedPartial();

    [Fact]
    public Task CaptureServiceRecordingOutcomeStateLivesWithRecordingLifecycle()
        => global::Program.CaptureService_RecordingOutcomeStateLivesWithRecordingLifecycle();

    [Fact]
    public Task CaptureServiceAudioOwnershipLivesInFocusedPartials()
        => global::Program.CaptureService_AudioOwnershipLivesInFocusedPartials();

    [Fact]
    public Task CaptureServiceMicrophoneRestartAfterRecordingLivesInAudioPreviewLifecyclePartial()
        => global::Program.CaptureService_MicrophoneRestartAfterRecordingLivesInAudioPreviewLifecyclePartial();

    [Fact]
    public Task LibAvRecordingSinkStopValidatesFinalOutput()
        => global::Program.LibAvRecordingSink_StopValidatesFinalOutput();

    [Fact]
    public Task RecordingVideoTryEnqueuePathsDoNotBlockCaptureCallbacks()
        => global::Program.RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks();

    [Fact]
    public Task UnifiedVideoCaptureSinkFanoutOwnsRecordingAndFlashbackFanout()
        => global::Program.UnifiedVideoCapture_SinkFanoutOwnsRecordingAndFlashbackFanout();

    [Fact]
    public Task UnifiedVideoCaptureFrameIngressLivesWithSourceSessionRoot()
        => global::Program.UnifiedVideoCapture_FrameIngressLivesWithSourceSessionRoot();

    [Fact]
    public Task UnifiedVideoCaptureLifecycleLivesWithRootState()
        => global::Program.UnifiedVideoCapture_LifecycleLivesWithRootState();

    [Fact]
    public Task WasapiAudioCaptureRejectsIncompleteHotAudioWrites()
        => global::Program.WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks();

    [Fact]
    public Task WasapiAudioCaptureConversionLivesWithLifecycleRoot()
        => global::Program.WasapiAudioCapture_ConversionLivesWithLifecycleRoot();

    [Fact]
    public Task WasapiAudioCaptureInitializationLivesWithLifecycleRoot()
        => global::Program.WasapiAudioCapture_InitializationLivesWithLifecycleRoot();

    [Fact]
    public Task WasapiAudioPlaybackInitializationLivesWithLifecycleRoot()
        => global::Program.WasapiAudioPlayback_InitializationLivesWithLifecycleRoot();

    [Fact]
    public Task WasapiAudioCaptureDiagnosticsLivesWithLifecycleRoot()
        => global::Program.WasapiAudioCapture_DiagnosticsLivesWithLifecycleRoot();

    [Fact]
    public Task WasapiComInteropContractsLiveWithInteropOwner()
        => global::Program.WasapiComInterop_ContractsLiveWithInteropOwner();

    [Fact]
    public Task WasapiAudioCaptureStopUsesBoundedThreadJoin()
        => global::Program.WasapiAudioCapture_StopUsesBoundedThreadJoin();

    [Fact]
    public Task CaptureServiceFlashbackBackendOwnershipUsesResourceAggregate()
        => global::Program.CaptureService_FlashbackBackendOwnershipUsesResourceAggregate();

    [Fact]
    public Task CaptureServiceFlashbackOrchestrationLivesInFocusedPartials()
        => global::Program.CaptureService_FlashbackOrchestrationLivesInFocusedPartials();

    [Fact]
    public Task CaptureServiceRecordingFinalizationLivesInFocusedPartials()
        => global::Program.CaptureService_RecordingFinalizationLivesInFocusedPartials();
}

public sealed class CoreRuntimeRecordingContractsTests
{
    public CoreRuntimeRecordingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingVerifierFailsWhenOutputFileIsMissing()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist();

    [Fact]
    public Task RecordingVerifierFailsWhenOutputFileIsEmpty()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenFileIsEmpty();

    [Fact]
    public Task RecordingVerifierFailsWhenOutputPathIsNull()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull();

    [Fact]
    public Task RecordingVerifierImplementsVerificationInterface()
        => global::Program.RecordingVerifier_ImplementsIRecordingVerifier();

    [Fact]
    public Task RecordingVerifierCadenceAnalysisLivesWithVerifier()
        => global::Program.RecordingVerifier_CadenceAnalysisLivesWithVerifier();

    [Fact]
    public Task RecordingVerifierProbeValidationAndResultShapingOwnership()
        => global::Program.RecordingVerifier_ProbeValidationAndResultShapingOwnership();

    [Fact]
    public Task RecordingVerificationResultExposesExpectedProperties()
        => global::Program.RecordingVerificationResult_HasExpectedProperties();

    [Fact]
    public Task RecordingVerifierFailsWhenFfprobeIsUnavailable()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable();

    [Fact]
    public Task RecordingVerifierRunsFfprobeBelowNormalPriority()
        => global::Program.RecordingVerifier_RunsFfprobeBelowNormalPriority();

    [Fact]
    public Task RecordingVerifierPassesHevcWhenAllFieldsMatch()
        => global::Program.RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc();

    [Fact]
    public Task RecordingVerifierDetectsH264CodecWhenHevcIsExpected()
        => global::Program.RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc();

    [Fact]
    public Task RecordingVerifierUsesFlashbackExportVerificationFormat()
        => global::Program.RecordingVerifier_UsesFlashbackExportVerificationFormat();

    [Fact]
    public Task RecordingVerifierUsesFlashbackRecordingVerificationFormat()
        => global::Program.RecordingVerifier_UsesFlashbackRecordingVerificationFormat();

    [Fact]
    public Task RecordingVerifierDetectsResolutionMismatch()
        => global::Program.RecordingVerifier_DetectsResolutionMismatch();

    [Fact]
    public Task RecordingVerifierDetectsFrameRateMismatch()
        => global::Program.RecordingVerifier_DetectsFrameRateMismatch();

    [Fact]
    public Task RecordingVerifierPassesHdrValidationWhenMetadataIsPresent()
        => global::Program.RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent();

    [Fact]
    public Task RecordingVerifierDetectsHdrColorimetryMismatch()
        => global::Program.RecordingVerifier_DetectsHdrColorimetryMismatch();

    [Fact]
    public Task RecordingVerifierPassesH264Format()
        => global::Program.RecordingVerifier_PassesVerification_ForH264Format();

    [Fact]
    public Task RecordingVerifierToleratesNtscFrameRateDrift()
        => global::Program.RecordingVerifier_PassesNtscFrameRateWithinTolerance();

    [Fact]
    public Task RecordingVerifierFailsWhenFfprobeExitsNonzero()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero();

    [Fact]
    public Task LibAvEncoderHdrBitstreamFiltersMapCodecs()
        => global::Program.LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs();

    [Fact]
    public Task LibAvEncoderChainsHdrAndMpegTsBitstreamFilters()
        => global::Program.LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters();

    [Fact]
    public Task LibAvEncoderExpectedFrameSizesMatchPixelFormats()
        => global::Program.LibAvEncoder_GetExpectedFrameSizeBytes_CalculatesCorrectly();

    [Fact]
    public Task LibAvEncoderNvencPresetsMapCorrectly()
        => global::Program.LibAvEncoder_MapNvencPreset_MapsCorrectly();

    [Fact]
    public Task LibAvEncoderThrowsOnNegativeNativeErrors()
        => global::Program.LibAvEncoder_ThrowIfError_ThrowsOnNegative();

    [Fact]
    public Task LibAvEncoderRationalInversionSwapsNumeratorAndDenominator()
        => global::Program.LibAvEncoder_Invert_SwapsNumeratorDenominator();

    [Fact]
    public Task LibAvEncoderHdrRationalsParseCorrectly()
        => global::Program.LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly();

    [Fact]
    public Task LibAvEncoderAcceptsValidOptions()
        => global::Program.LibAvEncoder_ValidateOptions_AcceptsValidOptions();

    [Fact]
    public Task LibAvEncoderRejectsEmptyOutputPath()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath();

    [Fact]
    public Task LibAvEncoderRejectsZeroDimensions()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsZeroDimensions();

    [Fact]
    public Task LibAvEncoderRejectsHdrWithH264()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsHdrWithH264();

    [Fact]
    public Task LibAvEncoderRejectsHdrWithoutP010()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010();

    [Fact]
    public Task LibAvEncoderRejectsMismatchedFrameRateParts()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts();

    [Fact]
    public Task LibAvEncoderFragmentsMp4TightlyForFlashbackPlayback()
        => global::Program.LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback();

    [Fact]
    public Task LibAvEncoderDumpsMpegTsHeadersForRotatedFlashbackSegments()
        => global::Program.LibAvEncoder_MpegTsNvencDumpsHeadersForRotatedSegments();

    [Fact]
    public Task LibAvEncoderPacketWritingLivesWithVideoSubmission()
        => global::Program.LibAvEncoder_PacketWritingLivesWithVideoSubmission();

    [Fact]
    public Task LibAvEncoderFrameCopyLivesWithVideoSubmission()
        => global::Program.LibAvEncoder_FrameCopyLivesWithVideoSubmission();

    [Fact]
    public Task LibAvEncoderVideoSubmissionLivesInFocusedPartial()
        => global::Program.LibAvEncoder_VideoSubmissionLivesInFocusedPartial();

    [Fact]
    public Task LibAvEncoderInitializationLivesInFocusedPartial()
        => global::Program.LibAvEncoder_InitializationLivesInFocusedPartial();

    [Fact]
    public Task LibAvEncoderDiagnosticsHelpersLiveWithCoreState()
        => global::Program.LibAvEncoder_DiagnosticsHelpersLiveWithCoreState();

    [Fact]
    public Task LibAvEncoderSetupAndModelsLiveInFocusedPartials()
        => global::Program.LibAvEncoder_SetupAndModelsLiveInFocusedPartials();

    [Fact]
    public Task LibAvEncoderOutputLifecycleLivesInFocusedOwner()
        => global::Program.LibAvEncoder_OutputLifecycleLivesInFocusedOwner();

    [Fact]
    public Task FlashbackIntegrityUsesRecordingScopedSequenceGaps()
        => global::Program.FlashbackRecordingIntegrity_UsesRecordingScopedSequenceGaps();

    [Fact]
    public Task SharedFormatterRendersRecordingIntegrity()
        => global::Program.SharedFormatter_RendersRecordingIntegrity();

    [Fact]
    public Task DedicatedLibAvVerificationScriptUsesFlashbackOffStrictWorkflow()
        => global::Program.DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification();
}

public sealed class RecordingModelContractsTests
{
    public RecordingModelContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task LibAvRecordingDrainLoopInterleavesAudioWithBoundedVideoBatches()
        => global::Program.LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches();

    [Fact]
    public Task LibAvRecordingEncodingLoopAndPacketDrainsLiveWithSinkRoot()
        => global::Program.LibAvRecordingSink_EncodingLoopAndPacketDrainsLiveWithSinkRoot();

    [Fact]
    public Task LibAvRecordingQueueingOwnsProducerAdmissionAndCleanup()
        => global::Program.LibAvRecordingSink_QueueingOwnsProducerAdmissionAndCleanup();

    [Fact]
    public Task LibAvRecordingLifecycleHelpersLiveWithTheirOwners()
        => global::Program.LibAvRecordingSink_LifecycleHelpersLiveWithTheirOwners();

    [Fact]
    public Task StrictHfrFatalHandlerClearsActiveSessionState()
        => global::Program.CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState();

    [Fact]
    public Task CaptureErrorsRefreshViewModelRuntimeFlags()
        => global::Program.CaptureErrors_RefreshViewModelRuntimeFlags();

    [Fact]
    public Task FlashbackBufferManagerInitializeClearsRecordingPts()
        => global::Program.FlashbackBufferManager_InitializeClearsRecordingPts();

    [Fact]
    public Task FlashbackBufferManagerSegmentLookupReturnsCorrectFileForPosition()
        => global::Program.FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment();

    [Fact]
    public Task FlashbackBufferManagerSegmentCompletionRejectsInvalidMetadata()
        => global::Program.FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata();

    [Fact]
    public Task FlashbackBufferManagerSegmentCompletionRejectsOutsidePaths()
        => global::Program.FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths();

    [Fact]
    public Task FlashbackBufferManagerDeleteHelperRejectsOutsidePaths()
        => global::Program.FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths();

    [Fact]
    public Task FlashbackBufferManagerSegmentDiagnosticsClampActiveCounters()
        => global::Program.FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters();

    [Fact]
    public Task FlashbackBufferManagerMathHelpersLiveInFocusedPartial()
        => global::Program.FlashbackBufferManager_MathHelpersLiveInFocusedPartial();

    [Fact]
    public Task FlashbackBufferManagerSegmentQueryHelpersLiveInFocusedPartial()
        => global::Program.FlashbackBufferManager_SegmentQueriesLiveInFocusedPartial();

    [Fact]
    public Task FlashbackBufferManagerSegmentMutationLivesInFocusedPartial()
        => global::Program.FlashbackBufferManager_SegmentMutationLiveInFocusedPartial();

    [Fact]
    public Task FlashbackBufferManagerLiveAccountingLivesWithRootState()
        => global::Program.FlashbackBufferManager_LiveAccountingLivesWithRootState();

    [Fact]
    public Task FlashbackBufferManagerLifecycleHelpersLiveWithRootState()
        => global::Program.FlashbackBufferManager_LifecycleHelpersLiveWithRootState();

    [Fact]
    public Task FlashbackBufferManagerPurgeHelpersLiveWithLifecycleCleanup()
        => global::Program.FlashbackBufferManager_PurgeLivesWithLifecycleCleanup();

    [Fact]
    public Task FlashbackBufferManagerLatestPtsClampsInvalidBufferDuration()
        => global::Program.FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration();

    [Fact]
    public Task FlashbackBufferManagerSegmentRotationKeepsTotalBytesWrittenMonotonic()
        => global::Program.FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic();

    [Fact]
    public Task FlashbackBufferManagerSamePathCompletionExtendsLatestSegment()
        => global::Program.FlashbackBufferManager_SamePathCompletionExtendsLatestSegment();

    [Fact]
    public Task FlashbackBufferManagerIgnoresUpdatesAfterDispose()
        => global::Program.FlashbackBufferManager_IgnoresUpdatesAfterDispose();

    [Fact]
    public Task FlashbackBufferManagerIgnoresDestructiveOperationsAfterDispose()
        => global::Program.FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose();

    [Fact]
    public Task FlashbackBufferManagerValidSegmentLookupSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerStaleLeftEdgeLookupUsesOldestSegment()
        => global::Program.FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest();

    [Fact]
    public Task FlashbackBufferManagerGetNextSegmentFileWalksForwardThroughSegments()
        => global::Program.FlashbackBufferManager_GetNextSegmentFile_WalksForward();

    [Fact]
    public Task FlashbackBufferManagerSegmentPathLookupsNormalizeEquivalentPaths()
        => global::Program.FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths();

    [Fact]
    public Task FlashbackBufferManagerSegmentStartPtsSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerGetNextSegmentFileSkipsMissingIndexedSegments()
        => global::Program.FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments();

    [Fact]
    public Task FlashbackBufferManagerGetValidSegmentPathsReturnsOverlappingSegments()
        => global::Program.FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping();

    [Fact]
    public Task FlashbackBufferManagerSegmentInfoSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerActiveFilePathRequiresExistingFile()
        => global::Program.FlashbackBufferManager_ActiveFilePath_RequiresExistingFile();

    [Fact]
    public Task FlashbackBufferManagerSegmentCountSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_SegmentCount_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerEvictionUpdatesDiskByteTotals()
        => global::Program.FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes();

    [Fact]
    public Task FlashbackBufferManagerEvictionKeepsRejectedSegmentsAccounted()
        => global::Program.FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted();

    [Fact]
    public Task FlashbackBufferManagerEvictionPauseAndResumeAreBalanced()
        => global::Program.FlashbackBufferManager_EvictionPauseResume_Balanced();

    [Fact]
    public Task FlashbackBufferManagerAbandonsStartupGeneratedSegmentPaths()
        => global::Program.FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath();

    [Fact]
    public Task FlashbackBufferManagerPurgesRetainLockedActiveSegmentPath()
        => global::Program.FlashbackBufferManager_PurgesRetainLockedActivePath();

    [Fact]
    public Task FlashbackBufferManagerPartialPurgeAccountsForDeletedActiveSegment()
        => global::Program.FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge();

    [Fact]
    public Task FlashbackBufferManagerFullPurgeReportsActiveBytesOnce()
        => global::Program.FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce();

    [Fact]
    public Task FlashbackBufferManagerRemovesStaleLegacyRootSegments()
        => global::Program.FlashbackBufferManager_RemovesStaleLegacyRootSegments();

    [Fact]
    public Task FlashbackBufferManagerPreservesUnrelatedEmptyTempDirectories()
        => global::Program.FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories();

    [Fact]
    public Task FlashbackBufferManagerTrimsStartupSessionCacheBudget()
        => global::Program.FlashbackBufferManager_TrimsStartupSessionCacheBudget();

    [Fact]
    public Task FlashbackBufferManagerRejectsUnsafeSessionIds()
        => global::Program.FlashbackBufferManager_RejectsUnsafeSessionIds();

    [Fact]
    public Task FlashbackBufferManagerValidatesSegmentExtensions()
        => global::Program.FlashbackBufferManager_ValidatesSegmentExtensions();
}

public class RecordingArtifactManagerTests
{
    [Fact]
    public void ArtifactManager_OwnsContextCreationAndFinalization()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/RecordingArtifactManager.cs");

        AssertContains(rootText, "public sealed class RecordingArtifactManager");
        AssertDoesNotContain(rootText, "partial class RecordingArtifactManager");
        AssertContains(rootText, "public async Task<RecordingContext> CreateContextAsync(");
        AssertContains(rootText, "private static RecordingContext BuildContext(");
        AssertContains(rootText, "public FinalizeResult FinalizeContext(");
        AssertContains(rootText, "public Task RollbackAsync(");
        AssertContains(rootText, "private static bool TryValidateFinalOutput(");
        AssertContains(rootText, "private static IReadOnlyList<string> GetExistingTempArtifacts(");
    }

    [Fact]
    public void ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var finalPath = Path.Combine(tempDir, "video.mp4");
            File.WriteAllText(finalPath, "video-data");

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(usePostMuxAudio: false, finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, true, null })!;

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual(finalPath, GetStringProperty(result, "OutputPath"), "OutputPath");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(finalPath, Array.Empty<byte>());

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, false, "encoder error" })!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            var preserved = GetPropertyValue(result, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

            if (File.Exists(finalPath))
            {
                throw new InvalidOperationException("Expected empty final file to be deleted");
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void ArtifactManager_FinalizeContext_RejectsInvalidFinalOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var emptyFinalPath = Path.Combine(tempDir, "empty-final.mp4");
            var missingFinalPath = Path.Combine(tempDir, "missing-final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(emptyFinalPath, Array.Empty<byte>());

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");

            var directContext = BuildRecordingContext(usePostMuxAudio: false, finalPath: emptyFinalPath);
            var directResult = finalizeMethod.Invoke(manager, new object?[] { directContext, true, null })!;
            AssertEqual(false, GetBoolProperty(directResult, "Succeeded"), "Direct empty output finalize fails");
            AssertContains(GetStringProperty(directResult, "StatusMessage"), "final output invalid");
            AssertContains(GetStringProperty(directResult, "StatusMessage"), "output file is empty");

            var muxContext = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: missingFinalPath);
            var muxResult = finalizeMethod.Invoke(manager, new object?[] { muxContext, true, null })!;
            AssertEqual(false, GetBoolProperty(muxResult, "Succeeded"), "Mux success with missing final output fails");
            AssertContains(GetStringProperty(muxResult, "StatusMessage"), "output file is missing");
            var preserved = GetPropertyValue(muxResult, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "Invalid mux final preserves temp artifacts");
            AssertEqual(true, File.Exists(videoPath), "Invalid mux final preserves video temp");
            AssertEqual(true, File.Exists(audioPath), "Invalid mux final preserves audio temp");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "v");
            File.WriteAllText(audioPath, "a");
            File.WriteAllText(finalPath, "f");

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
                ?? throw new InvalidOperationException("RollbackAsync not found");
            var task = rollbackMethod.Invoke(manager, new object?[] { context, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("RollbackAsync did not return Task");
            await task;

            if (File.Exists(videoPath))
            {
                throw new InvalidOperationException("Expected video temp to be deleted");
            }

            if (File.Exists(audioPath))
            {
                throw new InvalidOperationException("Expected audio temp to be deleted");
            }

            if (File.Exists(finalPath))
            {
                throw new InvalidOperationException("Expected final output to be deleted");
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task ArtifactManager_RollbackAsync_SafeWithNullContext()
    {
        var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
        var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
            ?? throw new InvalidOperationException("RollbackAsync not found");

        _ = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var task = rollbackMethod.Invoke(manager, new object?[] { null, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("RollbackAsync did not return Task");
        await task;
    }

    private static object BuildRecordingContext(
        bool usePostMuxAudio,
        string? videoPath = null,
        string? audioTempPath = null,
        string? finalPath = null)
    {
        var settings = BuildSettings();
        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var context = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(context, "Settings", settings);
        SetPropertyBackingField(context, "UsePostMuxAudio", usePostMuxAudio);
        SetPropertyBackingField(context, "EffectiveFrameRate", 60.0);
        SetPropertyBackingField(context, "FrameRateArg", "60");
        SetPropertyBackingField(context, "EffectiveWidth", 1920u);
        SetPropertyBackingField(context, "EffectiveHeight", 1080u);
        SetPropertyBackingField(context, "VideoInputPixelFormat", "nv12");
        SetPropertyBackingField(context, "VideoOutputPath", videoPath ?? "/tmp/video.mp4");
        SetPropertyBackingField(context, "FinalOutputPath", finalPath ?? "/tmp/final.mp4");
        SetPropertyBackingField(context, "AudioTempPath", audioTempPath);
        SetPropertyBackingField(context, "HdrPipelineActive", false);
        return context;
    }

    private static object BuildSettings()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "RequestedFrameRateArg", "60/1");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", 60u);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", 1u);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "NV12");
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));
        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", "Hdr10Pq"));
        SetPropertyOrBackingField(settings, "AudioEnabled", true);
        SetPropertyOrBackingField(settings, "OutputPath", Path.GetTempPath());
        return settings;
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateInstance(string typeName)
        => Activator.CreateInstance(RequireType(typeName))
           ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string name, object? value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        SetPropertyBackingField(instance, name, value);
    }

    private static void SetPropertyBackingField(object instance, string name, object? value)
    {
        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static bool GetBoolProperty(object instance, string name)
        => (bool)GetPropertyValue(instance, name)!;

    private static string GetStringProperty(object instance, string name)
        => (string)GetPropertyValue(instance, name)!;

    private static int GetCountProperty(object? value)
        => value is ICollection collection
            ? collection.Count
            : throw new InvalidOperationException("Expected collection value.");

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertEqual<T>(T expected, T actual, string _)
        => Assert.Equal(expected, actual);
}

// Representative xUnit slice ported from the legacy Program.cs runner.
//
// The test project targets net8.0 while Sussudio targets
// net8.0-windows10.0.19041.0, so a ProjectReference would force a Windows
// target onto the test rig and pull WinUI deps into discovery. xUnit tests
// therefore reach the assembly the same way the legacy runner does:
// Assembly.LoadFrom against the staged Sussudio.dll. The
// [assembly: InternalsVisibleTo("Sussudio.Tests")] attributes on Sussudio
// and ssctl mean reflection no longer needs to crack open private members,
// just resolve the type via its public/internal name.
//
// SussudioAssembly.Path is set by the test entry point (Program.Main today;
// later: a custom xUnit fixture) before any [Fact] runs.
public class RecordingContractsTests
{
    [Fact]
    public void GpuPipelineHandles_None_IsZeroed()
    {
        var asm = SussudioAssembly.Load();
        var handlesType = asm.GetType("Sussudio.Services.Contracts.GpuPipelineHandles", throwOnError: true)!;

        var none = handlesType.GetProperty("None", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("D3D11DevicePtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("D3D11DeviceContextPtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("CudaHwDeviceCtxPtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("CudaHwFramesCtxPtr")!.GetValue(none)!);
    }

    [Fact]
    public void FinalizeResult_Success_HasEmptyPreservedArtifacts()
    {
        var asm = SussudioAssembly.Load();
        var resultType = asm.GetType("Sussudio.Services.Contracts.FinalizeResult", throwOnError: true)!;

        var success = resultType.GetMethod("Success", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { "/tmp/out.mp4", "Stopped" })!;

        Assert.True((bool)resultType.GetProperty("Succeeded")!.GetValue(success)!);
        Assert.Equal("/tmp/out.mp4", (string)resultType.GetProperty("OutputPath")!.GetValue(success)!);
        Assert.Equal("Stopped", (string)resultType.GetProperty("StatusMessage")!.GetValue(success)!);

        var artifacts = (System.Collections.IEnumerable)resultType.GetProperty("PreservedArtifacts")!.GetValue(success)!;
        Assert.Empty(artifacts.Cast<object>());
    }

    [Fact]
    public void RecordingContextRequest_Defaults_MatchRecordingContextDefaults()
    {
        var asm = SussudioAssembly.Load();
        var requestType = asm.GetType("Sussudio.Services.Contracts.RecordingContextRequest", throwOnError: true)!;

        var request = Activator.CreateInstance(requestType)!;

        Assert.Equal("30", (string)requestType.GetProperty("FrameRateArg")!.GetValue(request)!);
        Assert.Equal("nv12", (string)requestType.GetProperty("VideoInputPixelFormat")!.GetValue(request)!);
        Assert.False((bool)requestType.GetProperty("IsFullRangeInput")!.GetValue(request)!);
        Assert.False((bool)requestType.GetProperty("UsePostMuxAudio")!.GetValue(request)!);
    }

    [Fact]
    public void RecordingStats_ComputesTotalsAndPreservesEstimateFlag()
    {
        var asm = SussudioAssembly.Load();
        var statsType = asm.GetType("Sussudio.Models.RecordingStats", throwOnError: true)!;

        Assert.True(statsType.IsValueType);
        Assert.True(statsType.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), inherit: false));

        foreach (var propertyName in new[] { "VideoBytes", "AudioBytes", "TotalBytes", "IsFlashbackEstimate", "IsFailure" })
        {
            var property = statsType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            Assert.Null(property!.SetMethod);
        }

        var ctor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool), typeof(bool) });
        Assert.NotNull(ctor);

        var finalStats = ctor!.Invoke(new object[] { 123L, 456L, false, false });
        Assert.Equal(123L, GetLongProperty(finalStats, "VideoBytes"));
        Assert.Equal(456L, GetLongProperty(finalStats, "AudioBytes"));
        Assert.Equal(579L, GetLongProperty(finalStats, "TotalBytes"));
        Assert.False(GetBoolProperty(finalStats, "IsFlashbackEstimate"));
        Assert.False(GetBoolProperty(finalStats, "IsFailure"));

        var flashbackStats = ctor.Invoke(new object[] { 10L, 5L, true, false });
        Assert.Equal(15L, GetLongProperty(flashbackStats, "TotalBytes"));
        Assert.True(GetBoolProperty(flashbackStats, "IsFlashbackEstimate"));

        var failureStats = ctor.Invoke(new object[] { 0L, 0L, false, true });
        Assert.True(GetBoolProperty(failureStats, "IsFailure"));

        var negativeCorrection = ctor.Invoke(new object[] { 100L, -20L, false, false });
        Assert.Equal(80L, GetLongProperty(negativeCorrection, "TotalBytes"));
    }

    [Fact]
    public void FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts()
    {
        var asm = SussudioAssembly.Load();
        var resultType = asm.GetType("Sussudio.Services.Contracts.FinalizeResult", throwOnError: true)!;

        var artifacts = new List<string> { "/path/a.mp4", "/path/A.mp4", null!, string.Empty, " ", "/path/b.m4a" };
        var failure = resultType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object?[] { "/output.mp4", "mux failed", artifacts })!;

        Assert.False((bool)resultType.GetProperty("Succeeded")!.GetValue(failure)!);
        Assert.Equal("/output.mp4", (string)resultType.GetProperty("OutputPath")!.GetValue(failure)!);
        Assert.Equal("mux failed", (string)resultType.GetProperty("StatusMessage")!.GetValue(failure)!);

        var preserved = (System.Collections.IEnumerable)resultType.GetProperty("PreservedArtifacts")!.GetValue(failure)!;
        Assert.Equal(new[] { "/path/a.mp4", "/path/b.m4a" }, preserved.Cast<object>().Select(value => (string)value).ToArray());
    }

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.GetValue(instance)!;

    private static long GetLongProperty(object instance, string propertyName)
        => (long)instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.GetValue(instance)!;
}

// Resolves the staged Sussudio.dll the same way the legacy runner does.
// Lives next to the xUnit slice rather than in Program.cs so a future cleanup
// can lift this into an IClassFixture without touching the legacy runner.
internal static class SussudioAssembly
{
    private static Assembly? _cached;

    public static Assembly Load()
    {
        if (_cached != null) return _cached;
        var path = System.Environment.GetEnvironmentVariable("SUSSUDIO_TEST_ASSEMBLY")
            ?? "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll";
        if (!System.IO.File.Exists(path))
        {
            var repoRoot = FindRepoRoot();
            var rooted = System.IO.Path.Combine(repoRoot, path.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(rooted)) path = rooted;
        }
        _cached = Assembly.LoadFrom(path);
        return _cached;
    }

    private static string FindRepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory);
        while (dir != null)
        {
            var gitPath = System.IO.Path.Combine(dir.FullName, ".git");
            if (System.IO.Directory.Exists(gitPath) || System.IO.File.Exists(gitPath))
            {
                break;
            }
            dir = dir.Parent;
        }
        return dir?.FullName ?? System.Environment.CurrentDirectory;
    }
}

internal static class LinqShim
{
    public static System.Collections.Generic.IEnumerable<T> Cast<T>(this System.Collections.IEnumerable source)
    {
        foreach (var item in source) yield return (T)item;
    }
}

internal static class ReflectionFlags
{
    public const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    public const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}

internal static class EnvVarScope
{
    public static IDisposable Push(string name, string? value)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return new Restore(name, previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;
        public Restore(string name, string? previous) { _name = name; _previous = previous; }
        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
    }
}

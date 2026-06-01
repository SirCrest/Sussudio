using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{

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
    public Task CaptureServiceAudioOwnershipLivesWithPreviewLifecycleOwner()
        => global::Program.CaptureService_AudioOwnershipLivesWithPreviewLifecycleOwner();

    [Fact]
    public Task CaptureServiceMicrophoneRestartAfterRecordingLivesInPreviewLifecycleOwner()
        => global::Program.CaptureService_MicrophoneRestartAfterRecordingLivesInPreviewLifecycleOwner();

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
    public Task LibAvEncoderOutputLifecycleLivesWithEncoderRoot()
        => global::Program.LibAvEncoder_OutputLifecycleLivesWithEncoderRoot();

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
    public Task FlashbackBufferManagerMathHelpersLiveWithRootState()
        => global::Program.FlashbackBufferManager_MathHelpersLiveWithRootState();

    [Fact]
    public Task FlashbackBufferManagerSegmentQueryHelpersLiveWithRootState()
        => global::Program.FlashbackBufferManager_SegmentQueriesLiveWithRootState();

    [Fact]
    public Task FlashbackBufferManagerSegmentMutationLivesWithRootState()
        => global::Program.FlashbackBufferManager_SegmentMutationLivesWithRootState();

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

// Representative xUnit slice ported from the legacy Program runner.
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
}

static partial class Program
{
    internal static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"))
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "FinalizeResult.Failure(fallbackOutputPath, $\"Unified video recording stop failed: {ex.Message}\");");
        AssertContains(captureServiceText, "StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(captureServiceText, "StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(captureServiceText, "DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(captureServiceText, "FoldLibAvAudioFaultIntoFinalizeResult(result, cancellationException);");
        AssertContains(captureServiceText, "PublishLibAvRecordingIntegrity(");
        // Fix #12: sink dispatch became a ternary so the emergency flag can route to libAvSink.StopAsync(emergency, ct).
        AssertContains(captureServiceText, "var sinkResult = libAvSink != null");
        AssertContains(captureServiceText, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(captureServiceText, ": await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n            {\n                result = sinkResult;");
        AssertContains(captureServiceText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(captureServiceText, "_previewAudioGraph.DetachCapture(");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingLifecycleAndBackendResourcesHaveFocusedOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var recordingBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = lifecycleText;
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var libAvFinalizeText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"))
            .Replace("\r\n", "\n");
        var flashbackFinalizeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackStartText = ExtractTextBetween(
            flashbackRecordingText,
            "private async Task DisposeUnusableFlashbackRecordingBackendAsync(",
            "private bool IsFlashbackRecordingBackendActive()");

        AssertDoesNotContain(rootText, "public Task StartRecordingAsync(");
        AssertDoesNotContain(rootText, "public Task StopRecordingAsync(");
        AssertContains(rootText, "private readonly CaptureRecordingBackendResources _recordingBackend = new();");
        AssertDoesNotContain(rootText, "private CaptureSettings? _activeRecordingSettings;");
        AssertDoesNotContain(rootText, "private LibAvRecordingSink? _libavSink;");
        AssertDoesNotContain(rootText, "private IRecordingSink? _recordingSink;");
        AssertDoesNotContain(rootText, "private Task? _pendingLibAvDrainTask");
        AssertDoesNotContain(rootText, "private RecordingContext? _recordingContext;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.SettingsSnapshot;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.LibAvSink;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.Sink;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.Context;");
        AssertContains(recordingBackendText, "internal sealed class CaptureRecordingBackendResources");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureRecordingBackendResources.cs")),
            "recording backend resources folded into CaptureService.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CapturePipelineResources.cs")),
            "capture pipeline resources folded into CaptureService.cs");
        AssertContains(recordingBackendText, "public LibAvRecordingSink? LibAvSink { get; set; }");
        AssertContains(recordingBackendText, "public IRecordingSink? Sink { get; set; }");
        AssertContains(recordingBackendText, "public RecordingContext? Context { get; set; }");
        AssertContains(recordingBackendText, "public CaptureSettings? SettingsSnapshot { get; set; }");
        AssertContains(recordingBackendText, "public Task? PendingLibAvDrainTask { get; set; }");
        AssertContains(recordingBackendText, "public bool IsFlashbackBackend(FlashbackEncoderSink? flashbackSink)");
        AssertContains(recordingBackendText, "public void InstallLibAv(");
        AssertContains(recordingBackendText, "public void InstallFlashback(");
        AssertContains(recordingBackendText, "public ActiveRecordingBackend DetachLibAvBackend()");
        AssertContains(recordingBackendText, "public RecordingContext? DetachFlashbackBackend()");
        AssertContains(recordingBackendText, "public void ClearPendingLibAvDrainIfCompletedSuccessfully()");
        AssertContains(recordingBackendText, "public void ThrowIfPendingLibAvDrainBlocksReentry()");
        AssertContains(recordingBackendText, "Previous recording backend failed to finalize cleanly. Check the logs and retry.");
        AssertContains(recordingBackendText, "Previous recording backend cleanup was canceled. Check the logs and retry.");
        AssertContains(recordingBackendText, "Previous recording backend is still finalizing. Please wait a moment and try again.");
        AssertContains(lifecycleText, "public Task StartRecordingAsync(");
        AssertContains(lifecycleText, "RunTransitionAsync(CaptureSessionState.Recording");
        AssertContains(lifecycleText, "_recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();");
        AssertContains(lifecycleText, "var rollback = new RecordingStartRollbackState();");
        AssertContains(lifecycleText, "await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken)");
        AssertContains(lifecycleText, "await StartFlashbackRecordingAsync(settings, transitionToken, rollback)");
        AssertContains(lifecycleText, "await StartLibAvRecordingAsync(settings, transitionToken, rollback)");
        AssertContains(lifecycleText, "await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);");
        AssertContains(lifecycleText, "await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);\n                throw;");
        AssertDoesNotContain(lifecycleText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(lifecycleText, "HDR_NEGOTIATION");
        AssertContains(lifecycleText, "private sealed class RecordingStartRollbackState");
        AssertContains(lifecycleText, "public RecordingContext? RecordingContext { get; set; }");
        AssertContains(lifecycleText, "public FlashbackEncoderSink? FlashbackRecordingStartedSink { get; set; }");
        AssertContains(lifecycleText, "private static async Task<StorageFolder> OpenRecordingOutputFolderAsync(");
        AssertContains(lifecycleText, "Output folder is unavailable: {settings.OutputPath}");
        AssertContains(lifecycleText, "private async Task<RecordingContext> CreateLibAvRecordingContextAsync(");
        AssertContains(lifecycleText, "private async Task<RecordingContext> CreateFlashbackRecordingContextAsync(");
        AssertContains(lifecycleText, "new RecordingContextRequest");
        AssertContains(lifecycleText, "GpuHandles = new GpuPipelineHandles(");
        AssertContains(lifecycleText, "GpuHandles = GpuPipelineHandles.None");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartContext.cs")),
            "old recording start context partial removed");
        AssertContains(flashbackStartText, "private async Task DisposeUnusableFlashbackRecordingBackendAsync(");
        AssertContains(flashbackStartText, "private async Task StartFlashbackRecordingAsync(");
        AssertContains(flashbackStartText, "await OpenRecordingOutputFolderAsync(settings)");
        AssertContains(flashbackStartText, "await CreateFlashbackRecordingContextAsync(");
        AssertContains(flashbackStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(flashbackStartText, "_recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);");
        AssertContains(flashbackStartText, "FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT");
        AssertContains(flashbackStartText, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(flashbackStartText, "videoCapture?.BeginFlashbackRecordingAccounting();");
        AssertDoesNotContain(flashbackStartText, "StorageFolder.GetFolderFromPathAsync");
        AssertDoesNotContain(flashbackStartText, "new RecordingContextRequest");
        AssertDoesNotContain(flashbackStartText, "HDR_NEGOTIATION");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartFlashback.cs")),
            "Flashback recording start folded into Flashback recording owner");
        AssertContains(libAvStartText, "private async Task StartLibAvRecordingAsync(");
        AssertContains(libAvStartText, "_recordingBackend.InstallLibAv(");
        AssertContains(libAvStartText, "await OpenRecordingOutputFolderAsync(settings)");
        AssertContains(libAvStartText, "await CreateLibAvRecordingContextAsync(");
        AssertContains(libAvStartText, "await RefreshSourceTelemetryAsync(transitionToken)");
        AssertContains(libAvStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)");
        AssertContains(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(");
        AssertContains(libAvStartText, "await PrepareLibAvRecordingVideoCaptureAsync(");
        AssertOccursBefore(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)", "await StartLibAvRecordingAudioInputsAsync(");
        AssertOccursBefore(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(", "_recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(");
        AssertOccursBefore(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(", "await unifiedVideoCapture.StartRecordingAsync(");
        AssertContains(libAvStartText, "private async Task<UnifiedVideoCapture> PrepareLibAvRecordingVideoCaptureAsync(");
        AssertContains(libAvStartText, "rollback.OwnedUnifiedVideoCapture = new UnifiedVideoCapture();");
        AssertContains(libAvStartText, "AttachUnifiedVideoCapture(rollback.OwnedUnifiedVideoCapture);");
        AssertContains(libAvStartText, "_videoPipeline.InstallCapture(rollback.OwnedUnifiedVideoCapture);");
        AssertContains(libAvStartText, "TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _videoPipeline.PreviewFrameSink : null);");
        AssertContains(libAvStartText, "Recording requires {(requireP010 ? \"P010\" : \"NV12\")}, but the active source-reader session negotiated");
        AssertContains(libAvStartText, "Recording requested mjpeg_hfr={useMjpegHighFrameRateMode}, but the active preview session is mjpeg_hfr=");
        AssertContains(libAvStartText, "private async Task StartLibAvRecordingAudioInputsAsync(");
        AssertContains(libAvStartText, "rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();");
        AssertContains(libAvStartText, "_previewAudioGraph.ProgramCapture.AttachRecordingSink(recordingSink);");
        AssertContains(libAvStartText, "rollback.SinkAttachedForAudioOnly = true;");
        AssertContains(libAvStartText, "await _previewAudioGraph.StartPlaybackAsync(");
        AssertContains(libAvStartText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);");
        AssertContains(libAvStartText, "micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(libAvStartText, "MICROPHONE_CAPTURE_START");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartLibAv.cs")),
            "LibAv recording startup folded into recording lifecycle owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartLibAv.VideoCapture.cs")),
            "old LibAv recording video-capture startup partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartLibAv.AudioInputs.cs")),
            "old LibAv recording audio-input startup partial removed");
        AssertDoesNotContain(libAvStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(lifecycleText, "public Task StopRecordingAsync(");
        AssertContains(lifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(lifecycleText, "await StopAndDisposeRecordingBackendAsync(\"Stopped\", emergency, transitionToken)");
        AssertContains(lifecycleText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertEqual(false, System.IO.File.Exists(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.RecordingStopLifecycle.cs")),
            "old recording stop lifecycle partial removed");
        AssertContains(libAvFinalizeText, "var detachedBackend = _recordingBackend.DetachLibAvBackend();");
        AssertContains(libAvFinalizeText, "private async Task<LibAvVideoBoundaryStopResult> StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(libAvFinalizeText, "private async Task DetachLibAvRecordingAudioBeforeSinkStopAsync()");
        AssertContains(libAvFinalizeText, "private async Task<LibAvFinalizeStepResult> StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(libAvFinalizeText, "private async Task<LibAvFinalizeStepResult> DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(libAvFinalizeText, "private readonly record struct LibAvFinalizeStepResult(");
        AssertContains(libAvFinalizeText, "private readonly record struct LibAvVideoBoundaryStopResult(");
        AssertContains(libAvFinalizeText, "VIDEO_DIAG mf_source_reader ");
        AssertContains(libAvFinalizeText, "VIDEO_DIAG recording_pipeline ");
        AssertContains(libAvFinalizeText, "var libAvDrainTask = libAvSink.EncodingCompletionTask;");
        AssertContains(libAvFinalizeText, "reason: \"recording_stop_deferred_drain\"");
        AssertContains(libAvFinalizeText, "_previewAudioGraph.DetachCapture(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvResources.cs")),
            "old broad LibAv resource finalization partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvVideoBoundary.cs")),
            "old LibAv video-boundary finalization partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvSink.cs")),
            "old LibAv sink finalization partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvIdlePreview.cs")),
            "old LibAv idle-preview finalization partial removed");
        AssertContains(flashbackFinalizeText, "var fbRecordingContext = _recordingBackend.DetachFlashbackBackend();");
        AssertContains(flashbackRecordingText, "_recordingBackend.IsFlashbackBackend(_flashbackBackend.Sink)");
        AssertContains(flashbackRecordingText, "private FlashbackSessionContext CreateFlashbackSessionContext(");
        AssertContains(flashbackRecordingText, "var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);");
        AssertContains(flashbackRecordingText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(");
        AssertContains(flashbackRecordingText, "private static readonly (int Numerator, int Denominator)[] CommonFlashbackFrameRateParts");
        AssertContains(flashbackRecordingText, "private static string? ResolveFlashbackExportVerificationFormat(");
        AssertContains(flashbackRecordingText, "private static string? ResolveFlashbackCodecDowngradeReason(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackRecording.SessionContext.cs")),
            "old Flashback recording session-context partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackRecording.FrameRate.cs")),
            "old Flashback recording frame-rate partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingRollbackLivesInFocusedPartial()
    {
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            }).Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var rollbackText = lifecycleText;

        AssertContains(rollbackText, "private async Task RollbackRecordingStartAsync(");
        AssertContains(rollbackText, "CAPTURE_RECORDING_START_FAIL");
        AssertContains(rollbackText, "RecordLastRecordingFailure(ex);");
        AssertContains(rollbackText, "CancelRecordingStartRollback(\"start_recording_failed\")");
        AssertContains(rollbackText, "FLASHBACK_RECORDING_START_ROLLBACK_WARN");
        AssertContains(rollbackText, "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertContains(rollbackText, "Recording start rollback cleanup failed");
        AssertContains(rollbackText, "Transient recording backend cleanup failed during start rollback");
        AssertContains(rollbackText, "_recordingStopwatch.Reset();");
        AssertContains(rollbackText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "Transient recording sink stop failed during rollback");
        AssertContains(rollbackText, "Transient unified video dispose failed during rollback");
        AssertContains(rollbackText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(rollbackText, "reason: \"recording_start_rollback\"");
        AssertOccursBefore(rollbackText, "CAPTURE_RECORDING_START_FAIL", "RecordLastRecordingFailure(ex);");
        AssertOccursBefore(rollbackText, "RecordLastRecordingFailure(ex);", "await _artifactManager.RollbackAsync(rollback.RecordingContext)");
        AssertOccursBefore(rollbackText, "rollback.FlashbackRecordingBackendLeaseHeld = false;", "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertOccursBefore(rollbackText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);", "await DisposeTransientRecordingBackendAsync(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingRollback.cs")),
            "recording start rollback lives with recording lifecycle ownership");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingOutcomeStateLivesWithRecordingLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var flashbackFinalizeText = ExtractMemberCode(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs").Replace("\r\n", "\n"),
            "StopAndDisposeFlashbackRecordingBackendAsync");
        var libAvFinalizeText = ExtractMemberCode(
            lifecycleText,
            "StopAndDisposeLibAvRecordingBackendAsync");
        var routerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private string? _lastOutputPath;");
        AssertDoesNotContain(rootText, "private string _lastFinalizeStatus = \"None\";");
        AssertDoesNotContain(rootText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertDoesNotContain(rootText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(lifecycleText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(lifecycleText, "private string? _lastOutputPath;");
        AssertContains(lifecycleText, "private string _lastFinalizeStatus = \"None\";");
        AssertContains(lifecycleText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertContains(lifecycleText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(lifecycleText, "_lastOutputPath = finalOutputPath;");
        AssertContains(lifecycleText, "_lastFinalizeStatus = \"Recording\";");
        AssertContains(lifecycleText, "_lastFinalizeUtc = null;");
        AssertContains(lifecycleText, "_lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(lifecycleText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(lifecycleText, "if (updateOutputPath)");
        AssertContains(lifecycleText, "_lastOutputPath = result.OutputPath;");
        AssertContains(lifecycleText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertContains(lifecycleText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertContains(lifecycleText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = lifecycleText;

        AssertContains(flashbackStartText, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(libAvStartText, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = fbRecordingContext.FinalOutputPath;");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = recordingContext.FinalOutputPath;");

        AssertContains(flashbackFinalizeText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(libAvFinalizeText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(routerText, "_lastFinalizeStatus = fbResult.StatusMessage;");
        AssertDoesNotContain(flashbackFinalizeText, "_lastOutputPath = result.OutputPath;");
        AssertDoesNotContain(flashbackFinalizeText, "_lastFinalizeStatus = fbResult.StatusMessage;");
        AssertDoesNotContain(libAvFinalizeText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertDoesNotContain(flashbackFinalizeText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertDoesNotContain(libAvFinalizeText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertDoesNotContain(flashbackFinalizeText, "_lastPreservedArtifacts = fbResult.PreservedArtifacts;");
        AssertDoesNotContain(libAvFinalizeText, "_lastPreservedArtifacts = result.PreservedArtifacts;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingOutcomeState.cs")),
            "old recording outcome-state partial removed");

        return Task.CompletedTask;
    }
}

// LibAv encoder contract implementations live with the recording xUnit wrappers.
static partial class Program
{
    private static string ReadLibAvEncoderSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static object CreateValidEncoderOptions()
    {
        var optionsType = RequireType("Sussudio.Services.Recording.LibAvEncoderOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "OutputPath", "/output/test.mp4");
        SetPropertyBackingField(options, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(options, "Width", 1920);
        SetPropertyBackingField(options, "Height", 1080);
        SetPropertyBackingField(options, "FrameRate", 60.0);
        SetPropertyBackingField(options, "BitRate", (uint)50_000_000);
        SetPropertyBackingField(options, "AudioEnabled", false);
        SetPropertyBackingField(options, "HdrEnabled", false);
        return options;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetVideoBitstreamFilterSpec",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoBitstreamFilterSpec not found.");

        var hdrHevcTs = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrHevcTs, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(hdrHevcTs, "ContainerFormat", "mpegts");
        SetPropertyBackingField(hdrHevcTs, "HdrEnabled", true);
        SetPropertyBackingField(hdrHevcTs, "IsP010", true);
        AssertEqual(
            "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9,dump_extra",
            method.Invoke(null, new[] { hdrHevcTs })?.ToString(),
            "HDR HEVC MPEG-TS chains HDR metadata and parameter-set filters");

        var sdrHevcTs = CreateValidEncoderOptions();
        SetPropertyBackingField(sdrHevcTs, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(sdrHevcTs, "ContainerFormat", "mpegts");
        SetPropertyBackingField(sdrHevcTs, "HdrEnabled", false);
        AssertEqual("dump_extra", method.Invoke(null, new[] { sdrHevcTs })?.ToString(), "SDR HEVC MPEG-TS dumps parameter sets");

        var hdrHevcMp4 = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrHevcMp4, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(hdrHevcMp4, "ContainerFormat", "mp4");
        SetPropertyBackingField(hdrHevcMp4, "HdrEnabled", true);
        SetPropertyBackingField(hdrHevcMp4, "IsP010", true);
        AssertEqual(
            "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            method.Invoke(null, new[] { hdrHevcMp4 })?.ToString(),
            "HDR HEVC MP4 keeps HDR metadata filter");

        var hdrAv1Mp4 = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrAv1Mp4, "CodecName", "av1_nvenc");
        SetPropertyBackingField(hdrAv1Mp4, "ContainerFormat", "mp4");
        SetPropertyBackingField(hdrAv1Mp4, "HdrEnabled", true);
        SetPropertyBackingField(hdrAv1Mp4, "IsP010", true);
        AssertEqual(
            "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            method.Invoke(null, new[] { hdrAv1Mp4 })?.ToString(),
            "HDR AV1 MP4 keeps AV1 metadata filter");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_MapNvencPreset_MapsCorrectly()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("MapNvencPreset",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapNvencPreset not found.");

        AssertEqual("p4", method.Invoke(null, new object?[] { null })!.ToString(), "null → p4");
        AssertEqual("p4", method.Invoke(null, new object[] { "Auto" })!.ToString(), "Auto → p4");
        AssertEqual("p1", method.Invoke(null, new object[] { "Fast" })!.ToString(), "Fast → p1");
        AssertEqual("p7", method.Invoke(null, new object[] { "Slow" })!.ToString(), "Slow → p7");
        AssertEqual("custom", method.Invoke(null, new object[] { "custom" })!.ToString(), "custom passthrough");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_MpegTsNvencDumpsHeadersForRotatedSegments()
    {
        var sourceText = ReadLibAvEncoderSource();

        AssertContains(sourceText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");
        AssertContains(sourceText, "GetVideoBitstreamFilterSpec(options)");
        AssertContains(sourceText, "ffmpeg.av_bsf_list_parse_str(filterSpec, &bsfCtx)");
        AssertContains(sourceText, "string.Join(\",\", filters)");
        AssertContains(sourceText, "filters.Add(hdrFilter)");
        AssertContains(sourceText, "filters.Add(parameterSetFilter)");
        AssertContains(sourceText, "IsMpegTsParameterSetFilterCandidate(options) ? \"dump_extra\" : null");
        AssertContains(sourceText, "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9");
        AssertContains(sourceText, "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9");
        AssertContains(sourceText, "string.Equals(options.ContainerFormat, \"mpegts\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(sourceText, "options.CodecName.Contains(\"h264\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(sourceText, "options.CodecName.Contains(\"hevc\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(sourceText, "ffmpeg.av_opt_set_int(codecContext->priv_data, \"forced-idr\", 1, 0)");
        AssertContains(sourceText, "av_opt_set_int(forced-idr)");
        AssertContains(sourceText, "TryMapSplitEncodeMode(options.SplitEncodeMode, out var splitEncodeMode)");
        AssertContains(sourceText, "ffmpeg.av_opt_set_int(codecContext->priv_data, \"split_encode_mode\", splitEncodeMode, 0)");
        AssertContains(sourceText, "splitEncodeMode is 2 or 3");
        AssertContains(sourceText, "public string SplitEncodeMode { get; init; } = \"Auto\";");
        AssertDoesNotContain(sourceText, "\"repeat_headers\"");
        // Suppression forwarder stays on LibAvEncoder for caller compatibility.
        AssertContains(sourceText, "internal static IDisposable SuppressRecoverableSeekFfmpegLogs()");
        AssertContains(sourceText, "FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs()");

        // Suppression implementation lives with FFmpeg runtime resolution and log callback routing.
        var suppressionText = ReadRepoFile("Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        AssertContains(suppressionText, "internal static bool ShouldSuppressRecoverableSeekFfmpegLog(string message)");
        AssertContains(suppressionText, "[ThreadStatic]\n    private static int _recoverableSeekLogSuppressionDepth;");
        AssertContains(suppressionText, "message.Contains(\"Could not find ref with POC\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "message.Contains(\"Error constructing the frame RPS\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "message.Contains(\"First slice in a frame missing\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "message.Contains(\"PPS id out of range\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "FFMPEG_LOG_RECOVERABLE_SEEK_SUPPRESSED");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_GetExpectedFrameSizeBytes_CalculatesCorrectly()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetExpectedFrameSizeBytes",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetExpectedFrameSizeBytes not found.");

        // NV12: width * height * 3 / 2
        var nv12_1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 * 3 / 2, nv12_1080, "NV12 1080p");

        // P010: width * height * 3
        var p010_1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 3, p010_1080, "P010 1080p");

        // P010 is exactly 2x NV12
        AssertEqual(nv12_1080 * 2, p010_1080, "P010 is 2x NV12");

        // 4K
        var nv12_4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 * 3 / 2, nv12_4k, "NV12 4K");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ThrowIfError_ThrowsOnNegative()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ThrowIfError",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ThrowIfError not found.");

        // Non-negative should not throw
        method.Invoke(null, new object[] { 0, "test" });
        method.Invoke(null, new object[] { 1, "test" });

        // Negative should throw (may throw InvalidOperationException or
        // DllNotFoundException if FFmpeg runtime isn't loaded for GetErrorString)
        var threw = false;
        try
        {
            method.Invoke(null, new object[] { -1, "test operation" });
        }
        catch (TargetInvocationException)
        {
            threw = true;
        }
        AssertEqual(true, threw, "ThrowIfError throws on negative error code");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_DiagnosticsHelpersLiveWithCoreState()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Diagnostics.cs")),
            "LibAvEncoder diagnostics helpers live with core encoder state, not a standalone partial");
        AssertContains(rootText, "private void EnsureOpen()");
        AssertContains(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(rootText, "private static string GetErrorString(int errorCode)");
        AssertContains(rootText, "private static InvalidOperationException CreateLibAvException(string message)");
        AssertContains(rootText, "private static void CheckDeviceRemoved(IntPtr d3d11Device)");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetHdrBitstreamFilterName",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetHdrBitstreamFilterName not found.");

        // HEVC variants → "hevc_metadata"
        var hevc1 = method.Invoke(null, new object[] { "hevc_nvenc" })?.ToString();
        AssertEqual("hevc_metadata", hevc1!, "hevc_nvenc → hevc_metadata");

        var hevc2 = method.Invoke(null, new object[] { "libx265" })?.ToString();
        // libx265 doesn't contain "hevc" so should return null
        AssertEqual(true, hevc2 == null, "libx265 → null (no hevc substring)");

        // AV1 → "av1_metadata"
        var av1 = method.Invoke(null, new object[] { "av1_nvenc" })?.ToString();
        AssertEqual("av1_metadata", av1!, "av1_nvenc → av1_metadata");

        // H264 → null (no HDR bitstream filter)
        var h264 = method.Invoke(null, new object?[] { "h264_nvenc" });
        AssertEqual(true, h264 == null, "h264 → null");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_Invert_SwapsNumeratorDenominator()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("Invert",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Invert not found.");

        // The method takes AVRational which is a struct from FFmpeg.AutoGen
        // AVRational has fields: int num, int den
        var avRationalType = method.GetParameters()[0].ParameterType;
        var input = Activator.CreateInstance(avRationalType)!;
        avRationalType.GetField("num")!.SetValue(input, 60);
        avRationalType.GetField("den")!.SetValue(input, 1);

        var result = method.Invoke(null, new[] { input })!;
        var resultNum = (int)avRationalType.GetField("num")!.GetValue(result)!;
        var resultDen = (int)avRationalType.GetField("den")!.GetValue(result)!;

        AssertEqual(1, resultNum, "Inverted numerator");
        AssertEqual(60, resultDen, "Inverted denominator");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly()
    {
        var hdrType = RequireType("Sussudio.Services.Recording.HdrMasterDisplayMetadata");

        var chromaMethod = hdrType.GetMethod("ToChromaticityRational",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToChromaticityRational not found.");
        var lumaMethod = hdrType.GetMethod("ToLuminanceRational",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToLuminanceRational not found.");

        var avRationalType = chromaMethod.ReturnType;

        // ToChromaticityRational: int.Parse(value) / 50000
        var chromaResult = chromaMethod.Invoke(null, new object[] { "13250" })!;
        var chromaNum = (int)avRationalType.GetField("num")!.GetValue(chromaResult)!;
        var chromaDen = (int)avRationalType.GetField("den")!.GetValue(chromaResult)!;
        AssertEqual(13250, chromaNum, "Chromaticity numerator");
        AssertEqual(50000, chromaDen, "Chromaticity denominator");

        // ToLuminanceRational: int.Parse(value) / 10000
        var lumaResult = lumaMethod.Invoke(null, new object[] { "10000" })!;
        var lumaNum = (int)avRationalType.GetField("num")!.GetValue(lumaResult)!;
        var lumaDen = (int)avRationalType.GetField("den")!.GetValue(lumaResult)!;
        AssertEqual(10000, lumaNum, "Luminance numerator");
        AssertEqual(10000, lumaDen, "Luminance denominator");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    // LibAvEncoder: ValidateOptions

    internal static Task LibAvEncoder_ValidateOptions_AcceptsValidOptions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateOptions not found.");
        var options = CreateValidEncoderOptions();
        method.Invoke(null, new[] { options });
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "OutputPath", "");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { threw = true; }
        AssertEqual(true, threw, "Empty OutputPath throws ArgumentException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsZeroDimensions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "Width", 0);
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException) { threw = true; }
        AssertEqual(true, threw, "Width=0 throws ArgumentOutOfRangeException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsHdrWithH264()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "HdrEnabled", true);
        SetPropertyBackingField(options, "IsP010", true);
        SetPropertyBackingField(options, "CodecName", "h264_nvenc");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { threw = true; }
        AssertEqual(true, threw, "HDR with H264 throws InvalidOperationException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "HdrEnabled", true);
        SetPropertyBackingField(options, "IsP010", false);
        SetPropertyBackingField(options, "CodecName", "hevc_nvenc");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { threw = true; }
        AssertEqual(true, threw, "HDR without P010 throws InvalidOperationException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "FrameRateNumerator", (int?)60000);
        SetPropertyBackingField(options, "FrameRateDenominator", (int?)null);
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { threw = true; }
        AssertEqual(true, threw, "Mismatched FrameRate parts throws ArgumentException");
        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_PacketWritingLivesWithVideoSubmission()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.PacketWriting.cs")),
            "video packet drain/write helpers stay folded into video submission");
        AssertContains(videoSubmissionText, "private void DrainEncoderPackets()");
        AssertContains(videoSubmissionText, "private void WriteFilteredPackets()");
        AssertContains(videoSubmissionText, "private void DrainBsfPackets()");
        AssertContains(videoSubmissionText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");
        AssertDoesNotContain(rootText, "private void DrainEncoderPackets()");
        AssertDoesNotContain(rootText, "private void WriteFilteredPackets()");
        AssertDoesNotContain(rootText, "private void DrainBsfPackets()");
        AssertDoesNotContain(rootText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_FrameCopyLivesWithVideoSubmission()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.FrameCopy.cs")),
            "CPU packed-frame copy is part of video submission, not a standalone partial");
        AssertContains(videoSubmissionText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");
        AssertContains(videoSubmissionText, "Buffer.MemoryCopy(");
        AssertDoesNotContain(rootText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_VideoSubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");
        var hardwareFramesText = videoSubmissionText;

        AssertContains(videoSubmissionText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertContains(videoSubmissionText, "CopyPackedFrameToVideoFrame(frameData[..expectedSize], options);");
        AssertContains(videoSubmissionText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(videoSubmissionText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareFramesText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "CopySubresourceRegion");
        AssertContains(hardwareFramesText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSubmission.cs")),
            "CPU video submission folded into LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.cs")),
            "hardware frame setup/submission folded into LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareSubmission.cs")),
            "hardware frame submission lives with LibAvEncoder.VideoFrames.cs");
        AssertDoesNotContain(rootText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertDoesNotContain(rootText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertDoesNotContain(rootText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_InitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = rootText;

        AssertContains(initializationText, "public static void InitializeFFmpeg(bool requireNativeRuntime = false)");
        AssertContains(initializationText, "public void Initialize(LibAvEncoderOptions options)");
        AssertContains(initializationText, "ThrowIfError(ffmpeg.avcodec_open2(_videoCodecCtx, codec, null), \"avcodec_open2\");");
        AssertContains(initializationText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(initializationText, "CleanupResources(writeTrailer: false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Initialization.cs")),
            "LibAvEncoder initialization folded into the encoder root");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_SetupAndModelsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = rootText;
        var audioText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");
        var modelsText = rootText;
        var hardwareFramesText = videoSubmissionText;

        AssertContains(audioText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "private unsafe struct AudioStreamState");
        AssertContains(audioText, "private void DrainStreamEncoderPackets(ref AudioStreamState s)");
        AssertContains(audioText, "private void WriteStreamPacket(ref AudioStreamState s, AVPacket* packet)");
        AssertContains(audioText, "private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,");
        AssertContains(audioText, "private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)");
        AssertContains(audioText, "private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,");
        AssertContains(audioText, "private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)");
        AssertContains(audioText, "private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void InitializeAudioIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioText, "ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC)");
        AssertContains(audioText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertContains(audioText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void AllocateAudioFrame()");
        AssertContains(audioText, "private void AllocateAudioAccumulator(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void AllocateAudioSampleQueue(LibAvEncoderOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioSetup.cs")),
            "Audio setup helpers live with audio stream initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioSubmission.cs")),
            "Audio sample submission folded into LibAvEncoder.Audio.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioQueue.cs")),
            "Audio queue and A/V sync helpers folded into LibAvEncoder.Audio.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioInitialization.cs")),
            "Audio stream initialization folded into LibAvEncoder.Audio.cs");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame)");
        AssertContains(videoSubmissionText, "ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HdrSideData.cs")),
            "HDR side-data helpers live with LibAvEncoder.VideoFrames.cs");
        AssertContains(modelsText, "internal sealed record LibAvEncoderOptions");
        AssertContains(modelsText, "internal readonly record struct RotateOutputResult");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Models.cs")),
            "LibAvEncoder option/result models live with the encoder root");
        AssertContains(initializationText, "private void ConfigureVideoCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(initializationText, "private void ApplyEncoderPrivateOptions(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(initializationText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static string? GetVideoBitstreamFilterSpec(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static string MapNvencPreset(string? preset)");
        AssertContains(initializationText, "private static bool TryMapSplitEncodeMode(string? splitEncodeMode, out long value)");
        AssertContains(initializationText, "private static AVRational ResolveFrameRate(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static bool IsSampleFormatSupported(AVCodec* codec, AVSampleFormat sampleFormat)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSetup.cs")),
            "Video codec setup helpers live with encoder initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.CodecPolicy.cs")),
            "LibAvEncoder codec/filter/rational policy lives with encoder initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Initialization.cs")),
            "LibAvEncoder initialization lives with the encoder root");
        AssertContains(hardwareFramesText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertContains(hardwareFramesText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "framesCtx->initial_pool_size = 0;");
        AssertContains(hardwareFramesText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "_useCudaHardwareFrames = true;");
        AssertContains(hardwareFramesText, "AVPixelFormat.AV_PIX_FMT_CUDA");
        AssertContains(hardwareFramesText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareFramesText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "CopySubresourceRegion");
        AssertContains(hardwareFramesText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.Cuda.cs")),
            "CUDA hardware frame adoption lives with the hardware frame initializer");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareSubmission.cs")),
            "hardware frame submission lives with LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSubmission.cs")),
            "CPU video submission folded into LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.cs")),
            "hardware frame setup/submission folded into LibAvEncoder.VideoFrames.cs");
        AssertContains(initializationText, "private static void ValidateOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateRequiredVideoOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateAudioOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateHdrOptions(LibAvEncoderOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OptionsValidation.cs")),
            "LibAvEncoder option validation folded into encoder initialization");
        AssertDoesNotContain(rootText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void AllocateAudioFrame()");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(initializationText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertDoesNotContain(initializationText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(initializationText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertDoesNotContain(hardwareFramesText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_OutputLifecycleLivesWithEncoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var outputLifecycleText = rootText;

        AssertContains(outputLifecycleText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertContains(outputLifecycleText, "private void CloseCurrentOutputIo()");
        AssertContains(outputLifecycleText, "private void ReinitializeOutputContext(string outputPath)");
        AssertContains(outputLifecycleText, "private void ReinitializeVideoStream()");
        AssertContains(outputLifecycleText, "private void ResetSegmentRuntimeState()");
        AssertContains(outputLifecycleText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(outputLifecycleText, "frag_keyframe+empty_moov");
        AssertContains(outputLifecycleText, "public void FlushAndClose()");
        AssertContains(outputLifecycleText, "public void Dispose()");
        AssertContains(outputLifecycleText, "private void CleanupResources(bool writeTrailer)");
        AssertContains(outputLifecycleText, "var finalMicSamplesReceived = ReleaseNativeResources(useCudaHardwareFrames);");
        AssertContains(outputLifecycleText, "ffmpeg.av_write_trailer(_formatCtx)");
        AssertContains(outputLifecycleText, "private long ReleaseNativeResources(bool useCudaHardwareFrames)");
        AssertContains(outputLifecycleText, "ffmpeg.avio_closep(&_formatCtx->pb)");
        AssertContains(outputLifecycleText, "Marshal.Release(_hwPoolTextures[i]);");
        AssertContains(outputLifecycleText, "ffmpeg.avcodec_free_context(&videoCodecCtx)");
        AssertContains(outputLifecycleText, "_isOpen = false;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OutputLifecycle.cs")),
            "output lifecycle folded into LibAvEncoder.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OutputRotation.cs")),
            "output rotation folded into LibAvEncoder.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.ResourceCleanup.cs")),
            "resource cleanup folded into LibAvEncoder.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.NativeResourceRelease.cs")),
            "Native resource release folded into LibAvEncoder.cs");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback()
    {
        var sourceText = ReadLibAvEncoderSource();

        AssertContains(sourceText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(sourceText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(sourceText, "ApplyMp4MuxerOptions(containerFormat, _options?.FragmentedMp4 ?? false, &muxerOptions, \"rotate\");");
        AssertContains(sourceText, "frag_keyframe+empty_moov");
        AssertContains(sourceText, "ffmpeg.av_dict_set(muxerOptions, \"frag_duration\", \"100000\", 0)");
        AssertContains(sourceText, "ffmpeg.av_dict_set(muxerOptions, \"flush_packets\", \"1\", 0)");
        AssertDoesNotContain(sourceText, "var movflags = options.FragmentedMp4\n                        ? \"frag_keyframe+empty_moov\"");
        AssertDoesNotContain(sourceText, "var movflags = (_options?.FragmentedMp4 ?? false)\n                    ? \"frag_keyframe+empty_moov\"");

        return Task.CompletedTask;
    }
}

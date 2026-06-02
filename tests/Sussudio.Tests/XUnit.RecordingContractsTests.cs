using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using System.Text.Json;

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
        AssertContains(rootText, "internal static class RecordingFinalizationRecoveryArtifacts");
        AssertContains(rootText, "private const string UnresolvedMarkerSuffix = \".recording-finalization-unresolved.txt\";");
        AssertContains(rootText, "AddExistingFile(preserved, outputPath);");
        AssertContains(rootText, "string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)");
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

    [Fact]
    public void RecordingFinalizationRecoveryArtifacts_PreservesFallbackOutputWithoutContext()
    {
        var asm = SussudioAssembly.Load();
        var helperType = asm.GetType("Sussudio.Services.Recording.RecordingFinalizationRecoveryArtifacts", throwOnError: true)!;
        var preserveUnresolved = helperType.GetMethod("PreserveUnresolved", BindingFlags.Public | BindingFlags.Static)!;

        var tempDirectory = Path.Combine(Path.GetTempPath(), "sussudio-recording-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var outputPath = Path.Combine(tempDirectory, "capture.mp4");
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });

            var result = (IEnumerable)preserveUnresolved.Invoke(
                null,
                new object?[] { null, outputPath, "timeout" })!;
            var artifacts = result.Cast<object>().Select(value => (string)value).ToArray();
            var markerPath = outputPath + ".recording-finalization-unresolved.txt";

            Assert.Contains(outputPath, artifacts);
            Assert.Contains(markerPath, artifacts);
            Assert.True(File.Exists(markerPath));
            Assert.Contains("reason=timeout", File.ReadAllText(markerPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
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
    private static string ReadLibAvRecordingSinkSource()
        => ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

    private static string ReadUnifiedVideoCaptureSource()
        => ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");

    private static string ExtractSourceBlock(string source, string startToken, string endToken)
    {
        var normalizedSource = NormalizeLineEndings(source);
        var normalizedStartToken = NormalizeLineEndings(startToken);
        var normalizedEndToken = NormalizeLineEndings(endToken);
        var start = normalizedSource.IndexOf(normalizedStartToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{startToken}'.");
        }

        var end = normalizedSource.IndexOf(normalizedEndToken, start + normalizedStartToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source after '{startToken}' to contain '{endToken}'.");
        }

        return normalizedSource[start..end];
    }

    internal static Task RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var libAvVideoEnqueue = ExtractSourceBlock(
            libAvSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var libAvGpuEnqueue = ExtractSourceBlock(
            libAvSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private unsafe VideoEnqueueResult TryEnqueueCudaPacket");
        var libAvCudaEnqueue = ExtractSourceBlock(
            libAvSource,
            "private unsafe VideoEnqueueResult TryEnqueueCudaPacket",
            "private bool TryWriteVideoPacket");
        var flashbackVideoEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var flashbackGpuEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private string? GetVideoEnqueueRejectReason");

        AssertDoesNotContain(libAvVideoEnqueue, "while (true)");
        AssertDoesNotContain(libAvGpuEnqueue, "while (true)");
        AssertDoesNotContain(libAvCudaEnqueue, "while (true)");
        AssertDoesNotContain(flashbackVideoEnqueue, "while (true)");
        AssertDoesNotContain(flashbackGpuEnqueue, "while (true)");
        AssertDoesNotContain(libAvSource, "Thread.Sleep(");
        AssertDoesNotContain(libAvSource, "backpressure_retry");
        AssertDoesNotContain(flashbackSource, "WaitForBackpressureRetryCancellation");
        AssertDoesNotContain(flashbackVideoEnqueue, "TimeSpan.FromMilliseconds(1)");
        AssertDoesNotContain(flashbackGpuEnqueue, "TimeSpan.FromMilliseconds(1)");

        AssertContains(libAvVideoEnqueue, "FailEncoding(overloadFailure);");
        AssertContains(libAvVideoEnqueue, "ReturnVideoPacket(packet);");
        AssertContains(libAvGpuEnqueue, "FailEncoding(new InvalidOperationException(");
        AssertContains(libAvGpuEnqueue, "Marshal.Release(packet.Texture);");
        AssertContains(libAvCudaEnqueue, "FailEncoding(new InvalidOperationException(");
        AssertContains(libAvCudaEnqueue, "ffmpeg.av_frame_free(&overloadedFrame);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(\"queue_full\");");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_QueueingOwnsProducerAdmissionAndCleanup()
    {
        var rootText = ReadLibAvRecordingSinkSource();
        var queueText = rootText;
        var videoSubmissionText = rootText;

        AssertContains(queueText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(queueText, "public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(queueText, "private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertContains(queueText, "private bool TryEnqueueMicrophonePacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertContains(queueText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queueText, "private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.AudioQueues.cs")),
            "LibAvRecordingSink audio queue surface folded into shared queue owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.QueueCleanup.cs")),
            "LibAvRecordingSink queue cleanup lives with video queue submission and packet ownership");
        AssertContains(videoSubmissionText, "private void ReturnRemainingVideoBuffers(Channel<VideoFramePacket>? queue)");
        AssertContains(videoSubmissionText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(videoSubmissionText, "private static unsafe void ReturnRemainingCudaFrames(Channel<CudaFramePacket>? queue, ref int queueDepth)");
        AssertContains(videoSubmissionText, "private static void ReturnVideoPacket(VideoFramePacket packet)");
        AssertContains(queueText, "public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)");
        AssertContains(queueText, "public unsafe void EnqueueCudaVideoFrame(AVFrame* cudaFrame)");
        AssertContains(queueText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(queueText, "bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)");
        AssertContains(videoSubmissionText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoSubmissionText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoSubmissionText, "private unsafe VideoEnqueueResult TryEnqueueCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertContains(videoSubmissionText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoSubmissionText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoSubmissionText, "private bool TryWriteCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertContains(videoSubmissionText, "private readonly record struct VideoFramePacket");
        AssertContains(videoSubmissionText, "private enum VideoEnqueueResult");
        AssertContains(videoSubmissionText, "private readonly record struct GpuFramePacket");
        AssertContains(videoSubmissionText, "private readonly record struct CudaFramePacket");
        AssertContains(videoSubmissionText, "internal sealed class VideoQueueLatencyTracker");
        AssertContains(videoSubmissionText, "public void TrackEnqueueUnderLock(long enqueueTick)");
        AssertContains(videoSubmissionText, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) GetMetrics()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Queues.cs")),
            "LibAvRecordingSink.Queues.cs folded into LibAvRecordingSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.VideoQueueSubmission.cs")),
            "LibAvRecordingSink.VideoQueueSubmission.cs folded into LibAvRecordingSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "VideoQueueLatencyTracker.cs")),
            "shared video queue latency tracker folded into the recording queueing owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Queueing.cs")),
            "LibAvRecordingSink queueing sidecar folded into the sink root");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_StopValidatesFinalOutput()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();

        AssertContains(libAvSource, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(libAvSource, "if (!TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure))\n        {\n            Logger.Log($\"LIBAV_SINK_STOP_OUTPUT_INVALID output='{outputPath}' reason='{outputFailure}'\");\n            return FinalizeResult.Failure(outputPath, $\"Stopped (output file invalid: {outputFailure})\");\n        }");
        AssertOccursBefore(libAvSource, "TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure)", "if (context?.HdrPipelineActive == true)");
        AssertContains(libAvSource, "failureMessage = \"output file is missing\";");
        AssertContains(libAvSource, "failureMessage = \"output file is empty\";");
        AssertContains(libAvSource, "LIBAV_SINK_STOP_OUTPUT_VALIDATE_WARN");
        AssertContains(libAvSource, "LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes}");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var encodingLoopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(libAvSource, "private const int VideoDrainBatchLimit = 24;");
        AssertContains(libAvSource, "private const int AudioDrainBatchLimit = 128;");
        AssertContains(libAvSource, "private const int GpuDrainBatchLimit = 16;");
        AssertContains(libAvSource, "private const int CudaDrainBatchLimit = 16;");
        AssertContains(libAvSource, "DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit)");
        AssertContains(libAvSource, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(libAvSource, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(libAvSource, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(libAvSource, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(libAvSource, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");

        var loopBlock = ExtractSourceBlock(
            encodingLoopText,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "            _encoder.FlushAndClose();");
        AssertOccursBefore(loopBlock, "DrainAudioPackets(audioQueue.Reader)", "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertOccursBefore(loopBlock, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)", "// Audio again catches samples");

        var secondAudioDrainBlock = ExtractSourceBlock(
            loopBlock,
            "// Audio again catches samples",
            "if (videoQueue.Reader.Completion.IsCompleted");
        AssertContains(secondAudioDrainBlock, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(secondAudioDrainBlock, "DrainMicrophonePackets(microphoneQueue.Reader)");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_EncodingLoopAndPacketDrainsLiveWithSinkRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(rootText, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(rootText, "DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit)");
        AssertContains(rootText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(rootText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertEqual(
            1,
            rootText.Split("public sealed class LibAvRecordingSink", StringSplitOptions.None).Length - 1,
            "LibAvRecordingSink.cs stays one in-file sink body");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.EncodingLoop.cs")),
            "LibAvRecordingSink encoding loop stays folded into the sink root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.PacketDrain.cs")),
            "LibAvRecordingSink packet drains stay folded into the sink root with the encoding loop");
        AssertContains(rootText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(rootText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(rootText, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(rootText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(rootText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(rootText, "Marshal.Release(packet.Texture);");
        AssertContains(rootText, "ffmpeg.av_frame_free(&frame);");
        AssertContains(rootText, "ReturnVideoPacket(packet);");
        AssertContains(rootText, "ReturnBuffer(packet.Buffer);");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_LifecycleHelpersLiveWithTheirOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");
        var stopText = rootText;

        AssertContains(rootText, "public long DroppedVideoFrames =>");
        AssertContains(rootText, "public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)");
        AssertContains(rootText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(rootText, "InitializeVideoSessionQueues();");
        AssertContains(rootText, "ResetVideoSessionState(context);");
        AssertContains(rootText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(rootText, "TaskCreationOptions.LongRunning");
        AssertContains(rootText, "LIBAV_SINK_START output='{context.FinalOutputPath}'");
        AssertContains(rootText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertContains(rootText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");
        AssertContains(rootText, "private void InitializeVideoSessionQueues()");
        AssertContains(rootText, "_cudaQueue = Channel.CreateBounded<CudaFramePacket>");
        AssertContains(rootText, "_gpuQueue = Channel.CreateBounded<GpuFramePacket>");
        AssertContains(rootText, "_videoQueue = Channel.CreateBounded<VideoFramePacket>");
        AssertContains(rootText, "LIBAV_SINK_CUDA_QUEUE_INIT capacity=");
        AssertContains(rootText, "LIBAV_SINK_GPU_QUEUE_INIT capacity=");
        AssertContains(rootText, "private void ResetVideoSessionState(RecordingContext context)");
        AssertContains(rootText, "_width = checked((int)context.EffectiveWidth);");
        AssertContains(rootText, "_height = checked((int)context.EffectiveHeight);");
        AssertContains(rootText, "private void ResetVideoSessionMetrics()");
        AssertContains(rootText, "Interlocked.Exchange(ref _videoFramesEnqueued, 0);");
        AssertContains(rootText, "Interlocked.Exchange(ref _gpuFramesEnqueued, 0);");
        AssertContains(rootText, "Interlocked.Exchange(ref _cudaFramesEnqueued, 0);");
        AssertContains(rootText, "Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);");
        AssertContains(rootText, "ResetVideoDiagnostics();");
        AssertContains(rootText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Diagnostics.cs")),
            "LibAvRecordingSink diagnostics surface lives with the sink root state");
        AssertContains(stopText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "=> StopCoreAsync(emergency: false, cancellationToken);");
        AssertContains(stopText, "internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)");
        AssertContains(stopText, "=> StopCoreAsync(emergency, cancellationToken);");
        AssertContains(stopText, "private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)");
        AssertContains(stopText, "var drainTimeoutMs = emergency ? EmergencyStopTimeoutMs : StopTimeoutMs;");
        AssertContains(stopText, "_cts?.Cancel();");
        AssertContains(stopText, "LIBAV_SINK_STOP_DRAIN_FLUSH_SKIPPED reason=encoder_task_still_running");
        AssertContains(stopText, "const string timeoutStatus = \"Stopped (libav encode drain timed out; recovery artifacts preserved)\";");
        AssertContains(stopText, "RecordingFinalizationRecoveryArtifacts.PreserveUnresolved(");
        AssertContains(stopText, "return FinalizeResult.Failure(outputPath, timeoutStatus, preservedArtifacts);");
        AssertContains(stopText, "TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure)");
        AssertContains(stopText, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(stopText, "if (context?.HdrPipelineActive == true)");
        AssertContains(stopText, "LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes}");
        AssertContains(rootText, "public async ValueTask DisposeAsync()");
        AssertContains(rootText, "private void ScheduleDeferredDisposeCleanup(Task encodingTask)");
        AssertContains(rootText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(rootText, "SignalWork(\"complete_writer\");");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.StopLifecycle.cs")),
            "LibAvRecordingSink stop/finalize lifecycle lives with the sink root lifecycle");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.OutputValidation.cs")),
            "LibAvRecordingSink.OutputValidation.cs folded into the sink root lifecycle");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Options.cs")),
            "LibAvRecordingSink.Options.cs folded into the sink startup owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.VideoSession.cs")),
            "LibAvRecordingSink video session startup helpers folded into the sink root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Startup.cs")),
            "LibAvRecordingSink startup shell folded into the sink root with encoding-loop lifecycle");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Lifetime.cs")),
            "LibAvRecordingSink dispose/deferred cleanup lives with the sink root");

        return Task.CompletedTask;
    }

    internal static Task RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var libAvSource = sources.LibAvSource;
        var flashbackSource = sources.FlashbackSource;
        var flashbackBackendSource = sources.FlashbackBackendSource;
        var flashbackBufferSource = sources.FlashbackBufferSource;
        var flashbackCleanupSource = sources.FlashbackCleanupSource;
        var captureServiceSource = sources.CaptureServiceSource;
        var captureHealthSnapshotRootSource = sources.CaptureHealthSnapshotRootSource;
        var captureSnapshotsSource = sources.CaptureSnapshotsSource;
        var unifiedVideoCaptureSource = sources.UnifiedVideoCaptureSource;
        var recordingContractsSource = sources.RecordingContractsSource;

        AssertDoesNotContain(libAvSource, "LIBAV_SINK_BURST_EVICT");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_BURST_EVICT");
        AssertDoesNotContain(libAvSource, "LIBAV_SINK_VIDEO_DROP");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_VIDEO_DROP");
        AssertDoesNotContain(libAvSource, "_videoSkipsBeforeNextPacket");
        AssertDoesNotContain(flashbackSource, "_videoSkipsBeforeNextPacket");
        AssertDoesNotContain(libAvSource, "SkipRawVideoFrame");
        AssertDoesNotContain(flashbackSource, "SkipRawVideoFrame");
        AssertDoesNotContain(libAvSource, "VideoFramePacket.Skip");
        AssertDoesNotContain(flashbackSource, "VideoFramePacket.Skip");
        AssertDoesNotContain(libAvSource, "_encoder.SkipVideoFrame");
        AssertDoesNotContain(flashbackSource, "_encoder.SkipVideoFrame");
        AssertDoesNotContain(libAvSource, "Interlocked.Add(ref _videoDropsBacklogEviction");
        AssertDoesNotContain(flashbackSource, "Interlocked.Add(ref _videoDropsBacklogEviction");
        AssertContains(captureServiceSource, "FLASHBACK_ENCODER_SUPPORT_PROBE_WARN");
        AssertDoesNotContain(captureServiceSource, "catch { /* Assume unavailable");

        AssertLibAvRecordingQueueOverloadPolicy(libAvSource, recordingContractsSource);
        AssertFlashbackRecordingQueueOverloadPolicy(flashbackSource);
        AssertFlashbackBufferRecoveryPolicy(flashbackSource, flashbackBufferSource, flashbackCleanupSource);
        AssertContains(captureServiceSource, "libAvSink.OnEncodingFailed = OnRecordingBackendFatalError");
        AssertContains(captureServiceSource, "OnFlashbackBackendFatalError,");
        AssertContains(flashbackBackendSource, "flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback)");
        AssertContains(flashbackBackendSource, "newSink.SetFatalErrorCallback(request.FatalErrorCallback)");
        AssertContains(captureServiceSource, "if (sink == null && controller is { IsDisposed: false, IsInitialized: true })");
        AssertContains(captureServiceSource, "controller.PrepareForPreviewDetach();");
        AssertOccursBefore(captureServiceSource, "controller.PrepareForPreviewDetach();", "_videoPipeline.SetPreviewFrameSink(sink);");
        AssertContains(captureServiceSource, "controller.UpdatePreviewComponents(sink, unifiedVideoCapture);");
        AssertContains(captureServiceSource, "FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        AssertContains(captureServiceSource, "private void OnFlashbackBackendFatalError");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_BACKEND_UNUSABLE_FALLBACK");
        AssertContains(captureServiceSource, "FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
        AssertContains(captureServiceSource, "Flashback export is unavailable while Flashback is the active recording backend.");
        AssertContains(captureServiceSource, "FLASHBACK_DISABLE_BLOCKED reason=recording_active");
        AssertContains(captureServiceSource, "Cannot disable Flashback while Flashback recording is active.");
        AssertContains(captureServiceSource, "FLASHBACK_RESTART_BLOCKED reason=recording_active");
        AssertContains(captureServiceSource, "Cannot restart Flashback while Flashback recording is active.");
        var restartFlashbackWithSettings = ExtractSourceBlock(
            captureServiceSource,
            "public Task RestartFlashbackAsync(CaptureSettings settings",
            "private async Task RestartFlashbackCoreAsync");
        AssertOccursBefore(
            restartFlashbackWithSettings,
            "if (_isRecording && IsFlashbackRecordingBackendActive())",
            "UpdateEncodingSettings(settings);");
        var restartFlashbackCore = ExtractSourceBlock(
            captureServiceSource,
            "private async Task RestartFlashbackCoreAsync",
            "    private async Task EnsureFlashbackAudioInputsAsync");
        AssertContains(restartFlashbackCore, "var committedRestartToken = CancellationToken.None;");
        AssertContains(restartFlashbackCore, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, committedRestartToken).ConfigureAwait(false);");
        AssertContains(restartFlashbackCore, "Logger.Log(\"FLASHBACK_RESTART_OK\");\n        cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(captureServiceSource, "_flashbackBackend.PreserveRecoverySegments");
        AssertContains(flashbackBackendSource, "MarkSessionPreservedForRecovery");
        AssertContains(flashbackBackendSource, "FLASHBACK_RECOVERY_PRESERVE");
        AssertContains(flashbackBackendSource, "ClearRecoveryPreserve();");
        AssertContains(flashbackBackendSource, "FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN");
        AssertContains(flashbackBackendSource, "flashbackSink.FrameEncoded -= request.FrameEncodedHandler;");
        AssertContains(flashbackBackendSource, "FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN");
        AssertContains(captureServiceSource, "_flashbackBackend.ResolveSegmentPurge");
        AssertContains(flashbackBackendSource, "FLASHBACK_SEGMENT_PURGE_BLOCKED");
        AssertContains(captureServiceSource, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(captureServiceSource, "Flashback backend export rotation did not quiesce before recording start.");
        var flashbackRecordingStartMismatch = ExtractSourceBlock(
            captureServiceSource,
            "var flashbackBackendSettingsChanged = _flashbackBackend.SettingsSnapshot == null",
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken, \"recording_flashback_start\")");
        AssertContains(flashbackRecordingStartMismatch, "FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT");
        AssertContains(flashbackRecordingStartMismatch, "EnsureFlashbackRecordingTopologyMatches(");
        AssertOccursBefore(
            flashbackRecordingStartMismatch,
            "EnsureFlashbackRecordingTopologyMatches(",
            "await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true)");
        AssertContains(captureServiceSource, "bool requireCompleteLiveEdge = false");
        AssertContains(captureServiceSource, "requireCompleteLiveEdge: true");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_EXPORT_INCOMPLETE_FAIL");
        AssertContains(captureServiceSource, "live-edge segment was not closed before timeout");
        AssertContains(flashbackBackendSource, "PreserveEndArtifactsOnFailure(exportResult, endResult);");
        AssertContains(flashbackBackendSource, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertContains(flashbackBackendSource, "exportResult.PreservedArtifacts.Concat(endResult.PreservedArtifacts)");
        AssertOccursBefore(flashbackBackendSource, "PreserveEndArtifactsOnFailure(exportResult, endResult);", "FLASHBACK_RECORDING_EXPORT_OK");
        AssertContains(captureServiceSource, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        var flashbackFailedFinalizeSettingsBranch = ExtractTextBetween(
            captureServiceSource,
            "if (!fbResult.Succeeded)",
            "else if (_pendingFlashbackSettingsChange)");
        AssertContains(flashbackFailedFinalizeSettingsBranch, "var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;");
        AssertContains(flashbackFailedFinalizeSettingsBranch, "_pendingFlashbackSettingsChange = false;");
        AssertContains(flashbackFailedFinalizeSettingsBranch, "pending_settings={hadPendingFlashbackSettingsChange}");
        AssertOccursBefore(captureServiceSource, "if (!fbResult.Succeeded)", "else if (_pendingFlashbackSettingsChange)");
        AssertContains(captureServiceSource, "preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_PRESERVE_SEGMENTS");
        AssertContains(captureServiceSource, "purgeSegments: !preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_UNIFIED_VIDEO_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_WASAPI_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "SafeClearCapturePlayback(ProgramCapture, \"stop_playback\")");
        AssertContains(captureServiceSource, "SafeClearCapturePlayback(capture, \"detach_capture\")");
        AssertContains(captureServiceSource, "private static void DisposePlaybackBestEffort(WasapiAudioPlayback playback)");
        AssertContains(captureServiceSource, "StopPlaybackBestEffort(newPlayback, \"start_fail\")");
        AssertContains(captureServiceSource, "WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "StopPlayback(flashbackPlaybackController);\n            throw;");
        AssertContains(captureServiceSource, "if (ReferenceEquals(Playback, newPlayback))");
        AssertContains(captureServiceSource, "private static void StopPlaybackBestEffort(WasapiAudioPlayback playback, string operation)");
        AssertContains(captureServiceSource, "WASAPI audio playback dispose warning");
        AssertDoesNotContain(captureServiceSource, "_previewAudioGraph.ProgramCapture?.SetPlayback(null);");
        AssertDoesNotContain(captureServiceSource, "capture.SetPlayback(null);\n        StopWasapiPlayback();");
        AssertContains(captureServiceSource, "CAPTURE_RECORDING_START_FAIL");
        var startRecordingFailure = ExtractSourceBlock(
            captureServiceSource,
            "private async Task RollbackRecordingStartAsync",
            "private async Task DisposeTransientRecordingBackendAsync");
        AssertContains(startRecordingFailure, "RecordLastRecordingFailure(ex);");
        AssertContains(startRecordingFailure, "Recording start rollback cleanup failed");
        AssertContains(startRecordingFailure, "Transient recording backend cleanup failed during start rollback");
        AssertOccursBefore(
            startRecordingFailure,
            "RecordLastRecordingFailure(ex);",
            "await _artifactManager.RollbackAsync(rollback.RecordingContext)");
        AssertDoesNotContain(captureServiceSource, "System.Diagnostics.Trace.TraceWarning($\"Suppressed exception in CaptureService.StartRecordingAsync");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild");
        AssertContains(captureServiceSource, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(flashbackBackendSource, "FLASHBACK_BUFFER_CLEANUP_PURGE_WARN");
        AssertDoesNotContain(captureServiceSource, "FLASHBACK_BUFFER_DEFERRED_PURGE_SKIP");
        AssertContains(captureServiceSource, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n            {\n                throw;\n            }");
        return Task.CompletedTask;
    }

    internal static Task RecordingBackendFlashbackBufferCycle_PreservesPolicies()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(bufferCycleText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertDoesNotContain(bufferCycleText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertFlashbackBufferCyclePolicies(
            sources.CaptureServiceSource,
            sources.FlashbackBackendSource);

        return Task.CompletedTask;
    }

    private readonly record struct RecordingQueueOverloadPolicySources(
        string LibAvSource,
        string FlashbackSource,
        string FlashbackBackendSource,
        string FlashbackBufferSource,
        string FlashbackCleanupSource,
        string CaptureServiceSource,
        string CaptureHealthSnapshotRootSource,
        string CaptureSnapshotsSource,
        string UnifiedVideoCaptureSource,
        string RecordingContractsSource);

    private static RecordingQueueOverloadPolicySources ReadRecordingQueueOverloadPolicySources()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();
        var flashbackBackendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var flashbackBufferSource = ReadFlashbackBufferManagerSource();
        var flashbackCleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs");
        var captureServiceSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var captureHealthSnapshotRootSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs");
        var captureSnapshotsSource = captureHealthSnapshotRootSource;
        var unifiedVideoCaptureSource = ReadUnifiedVideoCaptureSource();
        var recordingContractsSource = ReadRepoFile("Sussudio/Services/Contracts/ServiceContracts.cs");

        return new RecordingQueueOverloadPolicySources(
            libAvSource,
            flashbackSource,
            flashbackBackendSource,
            flashbackBufferSource,
            flashbackCleanupSource,
            captureServiceSource,
            captureHealthSnapshotRootSource,
            captureSnapshotsSource,
            unifiedVideoCaptureSource,
            recordingContractsSource);
    }

    private static void AssertLibAvRecordingQueueOverloadPolicy(string libAvSource, string recordingContractsSource)
    {
        AssertContains(libAvSource, "LibAv recording video queue overloaded");
        AssertDoesNotContain(libAvSource, "QueueBackpressureTimeoutMs");
        AssertDoesNotContain(libAvSource, "Thread.Sleep(");
        AssertDoesNotContain(libAvSource, "backpressure_retry");
        AssertContains(libAvSource, "LIBAV_SINK_VIDEO_OVERLOAD");
        AssertContains(libAvSource, "LIBAV_SINK_FATAL");
        AssertContains(libAvSource, "OnEncodingFailed?.Invoke");
        AssertContains(libAvSource, "public bool EncodingFailed");
        AssertContains(libAvSource, "public string? EncodingFailureMessage");
        AssertContains(libAvSource, "public int VideoQueueMaxDepth");
        AssertContains(libAvSource, "public long VideoFramesSubmittedToEncoder");
        AssertContains(libAvSource, "public long VideoEncoderPacketsWritten");
        AssertContains(libAvSource, "public long VideoSequenceGaps");
        AssertContains(libAvSource, "public long VideoQueueOldestFrameAgeMs");
        AssertContains(libAvSource, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(libAvSource, "public double VideoQueueLatencyP95Ms");
        AssertContains(libAvSource, "public double VideoQueueLatencyP99Ms");
        AssertContains(libAvSource, "public long VideoBackpressureWaitMs");
        AssertContains(libAvSource, "public long VideoBackpressureEvents");
        AssertDoesNotContain(libAvSource, "_videoLatencyTracker.RecordBackpressure(backpressureStartTick");
        AssertContains(libAvSource, "_videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick)");
        AssertContains(libAvSource, "_videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick)");
        AssertContains(libAvSource, "_videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber)");
        AssertContains(libAvSource, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(libAvSource, "var depth = Interlocked.Increment(ref _videoQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(libAvSource, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(libAvSource, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(libAvSource, "public int GpuQueueMaxDepth");
        AssertContains(libAvSource, "public int CudaQueueMaxDepth");
        AssertContains(libAvSource, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(libAvSource, "var depth = Interlocked.Increment(ref _gpuQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(libAvSource, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(libAvSource, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(libAvSource, "private bool TryWriteCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertContains(libAvSource, "var depth = Interlocked.Increment(ref _cudaQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(libAvSource, "AtomicMax.Update(ref _cudaQueueMaxDepth, depth);");
        AssertContains(libAvSource, "DecrementQueueDepth(ref _cudaQueueDepth, \"cuda_write_failed\");");
        AssertContains(libAvSource, "private static bool TryWriteAudioPacket(");
        AssertContains(libAvSource, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");
        AssertContains(libAvSource, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertContains(libAvSource, "LIBAV_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(libAvSource, "private void SignalWork(string operation)");
        AssertContains(libAvSource, "LIBAV_SINK_WORK_SIGNAL_SKIPPED");
        AssertContains(libAvSource, "SignalWork(\"complete_writer\");");
        AssertEqual(1, libAvSource.Split("_workAvailable.Release();", StringSplitOptions.None).Length - 1, "All LibAv work-signal wakeups go through SignalWork");
        AssertContains(libAvSource, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(libAvSource, "ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);");
        AssertDoesNotContain(libAvSource, "AtomicMax.Update(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth))");
        AssertDoesNotContain(libAvSource, "AtomicMax.Update(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth))");
        AssertDoesNotContain(libAvSource, "AtomicMax.Update(ref _cudaQueueMaxDepth, Interlocked.Increment(ref _cudaQueueDepth))");
        AssertDoesNotContain(libAvSource, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(libAvSource, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(libAvSource, "Interlocked.Decrement(ref _cudaQueueDepth)");
        AssertContains(recordingContractsSource, "IRawVideoFrameTryEncoder");
        AssertContains(recordingContractsSource, "IGpuVideoFrameTryEncoder");
        AssertContains(libAvSource, "IRawVideoFrameTryEncoder");
        AssertContains(libAvSource, "IGpuVideoFrameTryEncoder");
        AssertContains(libAvSource, "public bool TryEnqueueRawVideoFrame");
        AssertContains(libAvSource, "public bool TryEnqueueGpuVideoFrame");
        AssertContains(libAvSource, "VideoEnqueueResult.Rejected");
        AssertContains(libAvSource, "TryEnqueueGpuPacket");
        AssertContains(libAvSource, "TryEnqueueCudaPacket");
        AssertContains(libAvSource, "LibAv GPU recording queue overloaded");
        AssertContains(libAvSource, "LibAv CUDA recording queue overloaded");
        AssertContains(libAvSource, "if (!_started");
        AssertContains(libAvSource, "Volatile.Read(ref _encodingFailure) != null");
    }

    private static void AssertFlashbackRecordingQueueOverloadPolicy(string flashbackSource)
    {
        AssertDoesNotContain(flashbackSource, "QueueBackpressureTimeoutMs");
        AssertDoesNotContain(flashbackSource, "WaitForBackpressureRetryCancellation");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_VIDEO_BACKPRESSURE_DROP");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_GPU_BACKPRESSURE_DROP");
        AssertContains(flashbackSource, "var p010FrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(_width, _height, isP010: true)");
        AssertContains(flashbackSource, "VideoFramePacket.Frame(buffer, expectedSize, enqueueTick, isP010)");
        AssertContains(flashbackSource, "MfSourceReaderVideoCapture.GetFrameSizeBytes(w, h, packet.IsP010)");
        AssertContains(flashbackSource, "lease.PixelFormat == PooledVideoPixelFormat.P010");
        AssertContains(flashbackSource, "FLASHBACK_SINK_VIDEO_OVERLOAD");
        AssertContains(flashbackSource, "FLASHBACK_SINK_GPU_OVERLOAD");
        AssertContains(flashbackSource, "_onFatalError?.Invoke");
        AssertDoesNotContain(flashbackSource, "catch { /* Callback must not mask the original error */ }");
        AssertContains(flashbackSource, "Logger.Log($\"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}\");");
        AssertContains(flashbackSource, "private void OnVideoFrameEncoded()\n    {\n        if (_disposed)\n        {\n            return;\n        }");
        AssertContains(flashbackSource, "if (!_disposed && Volatile.Read(ref _recordingActive) == 1)");
        AssertContains(flashbackSource, "public bool EncodingFailed");
        AssertContains(flashbackSource, "public string? EncodingFailureMessage");
        AssertContains(flashbackSource, "public bool CanBeginRecording");
        AssertContains(flashbackSource, "public bool IsRecordingActive");
        AssertContains(flashbackSource, "Volatile.Read(ref _recordingActive) == 0");
        AssertContains(flashbackSource, "!_bufferManager.IsSessionPreservedForRecovery");
        AssertContains(flashbackSource, "Cannot begin recording: flashback recording is already active.");
        AssertContains(flashbackSource, "Cannot begin recording: flashback session is preserved for recovery.");
        AssertOccursBefore(flashbackSource, "Cannot begin recording: flashback recording is already active.", "_bufferManager.PauseEviction();");
        AssertOccursBefore(flashbackSource, "Cannot begin recording: flashback session is preserved for recovery.", "_bufferManager.PauseEviction();");
        AssertOccursBefore(flashbackSource, "_bufferManager.PauseEviction();", "Volatile.Write(ref _recordingActive, 1);");
        AssertContains(flashbackSource, "public bool IsForceRotateActive");
        AssertContains(flashbackSource, "public bool IsForceRotateRequested");
        AssertContains(flashbackSource, "public bool IsForceRotateDraining");
        AssertContains(flashbackSource, "WaitForForceRotateIdle");
        AssertContains(flashbackSource, "CompletePendingForceRotateWithEmptyResult");
        AssertContains(flashbackSource, "ForceRotateRequest? supersededRequest;");
        AssertContains(flashbackSource, "supersededRequest = _forceRotateRequest;");
        AssertContains(flashbackSource, "FLASHBACK_SINK_FORCE_ROTATE_SUPERSEDED");
        AssertContains(flashbackSource, "supersededRequest.TryCancel();");
        AssertContains(flashbackSource, "if (!RotateSegment(currentPts))\n                {\n                    localRequest.CompleteEmpty();\n                    return true;\n                }");
        AssertContains(flashbackSource, "private bool RotateSegment(TimeSpan currentPts)");
        AssertContains(flashbackSource, "return true;\n        }\n        catch (Exception ex)");
        AssertContains(flashbackSource, "Logger.Log($\"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}\");\n            return false;");
        AssertContains(flashbackSource, "TryCancelForceRotate(request)");
        AssertContains(flashbackSource, "ReferenceEquals(_forceRotateRequest, request)");
        AssertContains(flashbackSource, "cancelled={cancelled}");
        AssertContains(flashbackSource, "_forceRotateRequested = false;");
        AssertContains(flashbackSource, "Volatile.Write(ref _forceRotateDraining, false);");
        AssertContains(flashbackSource, "CancelEncodingCts(\"stop_timeout\");\n                CompletePendingForceRotateWithEmptyResult();\n                Logger.Log(\"FLASHBACK_SINK_STOP_DRAIN_TIMEOUT\");");
        AssertContains(flashbackSource, "Cannot begin recording: flashback export rotation is still draining.");
        AssertContains(flashbackSource, "if (_ownsBufferManager)");
        AssertOccursBefore(flashbackSource, "if (_ownsBufferManager)\n        {\n            _bufferManager.PurgeAllSegments();", "_encoder.Dispose();");
        AssertContains(flashbackSource, "CancelRecordingStartRollback");
        AssertContains(flashbackSource, "var wasRecording = Interlocked.Exchange(ref _recordingActive, 0) != 0");
        AssertContains(flashbackSource, "if (!wasRecording)\n        {\n            const string message = \"Flashback recording was not active.\";");
        AssertContains(flashbackSource, "FLASHBACK_RECORDING_END_REJECTED");
        AssertContains(flashbackSource, "finally");
        AssertContains(flashbackSource, "ResumeEvictionBestEffort(_bufferManager, \"recording_end\")");
        AssertContains(flashbackSource, "ResumeEvictionBestEffort(_bufferManager, \"recording_start_rollback\")");
        AssertContains(flashbackSource, "if (Interlocked.Exchange(ref _recordingActive, 0) != 0)\n        {\n            ResumeEvictionBestEffort(_bufferManager, \"dispose\");\n        }");
        AssertContains(flashbackSource, "_gpuEncodingEnabled = false;\n        _audioEnabled = false;\n        _microphoneEnabled = false;\n        _sessionContext = null;\n        _width = 0;\n        _height = 0;\n        _tsFilePath = null;\n        _recordingOutputPath = string.Empty;\n        _segmentStartPts = TimeSpan.Zero;\n        _segmentDuration = TimeSpan.Zero;\n        _ptsBaseOffset = TimeSpan.Zero;\n        Interlocked.Exchange(ref _segmentStartBytes, 0);");
        AssertContains(flashbackSource, "FLASHBACK_SINK_EVICTION_RESUME_WARN");
        AssertContains(flashbackSource, "if (LastRecordingEndPts < LastRecordingStartPts)\n                {\n                    LastRecordingEndPts = _bufferManager.LatestPts;\n                    if (LastRecordingEndPts < LastRecordingStartPts)\n                    {\n                        LastRecordingEndPts = LastRecordingStartPts;\n                    }\n                }");
        AssertContains(flashbackSource, "Cannot begin recording: flashback encoder is not running.");
        AssertContains(flashbackSource, "public int VideoQueueMaxDepth");
        AssertContains(flashbackSource, "public long VideoFramesSubmittedToEncoder");
        AssertContains(flashbackSource, "public long VideoEncoderPacketsWritten");
        AssertContains(flashbackSource, "public long VideoSequenceGaps");
        AssertContains(flashbackSource, "public long VideoQueueOldestFrameAgeMs");
        AssertContains(flashbackSource, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(flashbackSource, "public double VideoQueueLatencyP95Ms");
        AssertContains(flashbackSource, "public double VideoQueueLatencyP99Ms");
        AssertContains(flashbackSource, "public long VideoBackpressureWaitMs");
        AssertContains(flashbackSource, "public long VideoBackpressureEvents");
        AssertDoesNotContain(flashbackSource, "_videoLatencyTracker.RecordBackpressure(backpressureStartTick");
        AssertContains(flashbackSource, "_videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick)");
        AssertContains(flashbackSource, "_videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick)");
        AssertContains(flashbackSource, "_videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber)");
        AssertContains(flashbackSource, "public int GpuQueueMaxDepth");
        AssertContains(flashbackSource, "IRawVideoFrameTryEncoder");
        AssertContains(flashbackSource, "IGpuVideoFrameTryEncoder");
        AssertContains(flashbackSource, "public bool TryEnqueueRawVideoFrame");
        AssertContains(flashbackSource, "public bool TryEnqueueGpuVideoFrame");
        AssertContains(flashbackSource, "VideoEnqueueResult.Rejected");
        AssertContains(flashbackSource, "TryEnqueueGpuPacket");
        AssertContains(flashbackSource, "Volatile.Read(ref _forceRotateDraining)");
        AssertContains(flashbackSource, "Volatile.Read(ref _encodingFailure) != null");
        AssertContains(flashbackSource, "var maxFrameSize = Math.Max(nv12FrameSize, p010FrameSize);");
        AssertContains(flashbackSource, "var matchesConfiguredFrameSize =\n            expectedSize == nv12FrameSize ||\n            (p010FrameSize > 0 && expectedSize == p010FrameSize);");
        AssertContains(flashbackSource, "if (maxFrameSize <= 0 || !matchesConfiguredFrameSize)");
        AssertContains(flashbackSource, "FLASHBACK_SINK_VIDEO_FRAME_INVALID_SIZE expected={expectedSize} max={maxFrameSize}");
        AssertContains(flashbackSource, "if (expectedSize <= 0)\n        {\n            Logger.Log($\"FLASHBACK_SINK_VIDEO_FRAME_INVALID_SIZE expected={expectedSize} actual={frame.Width}x{frame.Height}\");\n            frame.Dispose();\n            return false;\n        }");
        AssertContains(flashbackSource, "if (subresourceIndex < 0)\n        {\n            TrackGpuQueueRejected(\"invalid_subresource\");\n            Logger.Log($\"FLASHBACK_SINK_GPU_FRAME_INVALID_SUBRESOURCE subresource={subresourceIndex}\");\n            return false;\n        }");
        AssertOccursBefore(flashbackSource, "FLASHBACK_SINK_GPU_FRAME_INVALID_SUBRESOURCE", "Marshal.AddRef(d3d11Texture2D);");
    }

    private static void AssertFlashbackBufferRecoveryPolicy(
        string flashbackSource,
        string flashbackBufferSource,
        string flashbackCleanupSource)
    {
        var flashbackBufferDispose = ExtractSourceBlock(
            flashbackBufferSource,
            "public void Dispose()",
            "private void ThrowIfDisposed()");
        AssertDoesNotContain(flashbackBufferDispose, "PurgeAllSegments()");
        AssertContains(flashbackBufferSource, "RecoveryPreserveMarkerFileName");
        AssertContains(flashbackBufferSource, "MarkSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "public bool IsSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "private bool _preserveSessionForRecovery;");
        AssertContains(flashbackBufferSource, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");
        AssertContains(flashbackCleanupSource, "FLASHBACK_STALE_SESSION_PRESERVE_SKIP");
        AssertContains(flashbackCleanupSource, "File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName))");
        AssertContains(flashbackBufferSource, "DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"valid_window\")");
        AssertContains(flashbackBufferSource, "DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"disk_budget\")");
        AssertContains(flashbackBufferSource, "private static bool DeleteEvictedFile");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_SEGMENT_EVICT_DELETED");
        AssertContains(flashbackBufferSource, "public void MarkActiveSegmentStart(string path, TimeSpan startPts)");
        AssertContains(flashbackSource, "_bufferManager.MarkActiveSegmentStart(tsPath, _segmentStartPts);");
        AssertContains(flashbackSource, "_bufferManager.MarkActiveSegmentStart(newPath, _segmentStartPts);");

        var flashbackVideoEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var flashbackGpuEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private void FailEncoding");
        var flashbackAudioEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private bool TryEnqueueAudioPacket",
            "private static bool TryWriteAudioPacket");
        AssertOccursBefore(flashbackVideoEnqueue, "GetVideoEnqueueRejectReason(isGpu: false)", "TryWriteVideoPacket(queue, packet)");
        AssertOccursBefore(flashbackGpuEnqueue, "GetVideoEnqueueRejectReason(isGpu: true)", "TryWriteGpuPacket(queue, packet)");
        AssertOccursBefore(flashbackAudioEnqueue, "Volatile.Read(ref _forceRotateDraining)", "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(flashbackVideoEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: false);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(flashbackGpuEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: true);");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(rejectReason);");
        AssertContains(flashbackAudioEnqueue, "if (_disposed ||");
        AssertContains(flashbackAudioEnqueue, "!_started ||");
        AssertContains(flashbackGpuEnqueue, "lock (_videoQueueSync)");
        AssertContains(flashbackAudioEnqueue, "lock (_videoQueueSync)");
    }

    private static void AssertRecordingQueueHealthSnapshotTelemetry(
        string captureServiceSource,
        string captureHealthSnapshotRootSource,
        string captureSnapshotsSource,
        string unifiedVideoCaptureSource)
    {
        AssertContains(unifiedVideoCaptureSource, "encoder is IRawVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "leaseEncoder is IRawVideoFrameLeaseTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "encoder is IGpuVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "BeginFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "RecordFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "sink.IsRecordingActive");
        AssertContains(unifiedVideoCaptureSource, "if (accepted)");
        AssertContains(unifiedVideoCaptureSource, "public MjpegPipelineTimingSnapshot GetMjpegPipelineTimingSnapshot()");
        AssertContains(unifiedVideoCaptureSource, "private static MjpegPipelineTimingMetrics CreateMjpegPipelineTimingSummary");
        AssertContains(captureServiceSource, "var timingSnapshot = capture?.GetMjpegPipelineTimingSnapshot();");
        AssertContains(captureServiceSource, "RecordLastRecordingFailure");
        AssertContains(captureServiceSource, "RecordLastFlashbackFailure");
        AssertContains(captureServiceSource, "ClearLastRecordingFailure");
        AssertContains(captureServiceSource, "ClearLastFlashbackFailure");
        AssertContains(captureSnapshotsSource, "GetLastFailureTelemetry");
        AssertContains(captureSnapshotsSource, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(captureHealthSnapshotRootSource, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(captureSnapshotsSource, "var timingSnapshot = _videoPipeline.GetMjpegTimingSnapshot(unifiedVideoCapture);");
        AssertContains(captureSnapshotsSource, "private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetMjpegPipelineTimingMetrics()");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics()");
        AssertContains(captureSnapshotsSource, "var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics");
        AssertContains(captureSnapshotsSource, "sink?.VideoQueueLatencyMetrics ??");
        AssertDoesNotContain(captureSnapshotsSource, "var flashbackIsRecordingBackend = _isRecording && IsFlashbackRecordingBackendActive()");
        AssertContains(captureSnapshotsSource, "RecordingEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "RecordingVideoFramesSubmittedToEncoder = recordingHealth.VideoFramesSubmitted");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP99Ms = recordingHealth.VideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueOldestFrameAgeMs = recordingHealth.VideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "RecordingVideoBackpressureWaitMs = recordingHealth.VideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "RecordingCudaQueueDepth = recordingHealth.CudaQueueDepth");
        AssertContains(captureSnapshotsSource, "RecordingCudaFramesDropped = recordingHealth.CudaFramesDropped");
        AssertContains(captureSnapshotsSource, "sink?.CudaQueueCount ?? 0");
        AssertContains(captureSnapshotsSource, "sink?.CudaFramesDropped ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoEncoderPacketsWritten ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoSequenceGaps ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(captureSnapshotsSource, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoBackpressureWaitMs ?? 0");
        AssertContains(captureSnapshotsSource, "FatalCleanupInProgress = fatalCleanupInProgress");
        AssertContains(captureSnapshotsSource, "FlashbackCleanupInProgress = flashbackCleanupInProgress");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateRequested ?? false");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateDraining ?? false");
        AssertContains(captureSnapshotsSource, "FlashbackEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "FlashbackStartupCacheBytes = flashbackBuffer.StartupCacheBytes");
        AssertContains(captureSnapshotsSource, "bufMgr?.StartupCacheBytes ?? 0");
        AssertContains(captureSnapshotsSource, "FlashbackTempDriveFreeBytes = flashbackBuffer.TempDriveFreeBytes");
        AssertContains(captureSnapshotsSource, "bufMgr?.TempDriveAvailableFreeBytes ?? 0");

        var sharedFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadAutomationSnapshotFormatterSource();
        var ssctlFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadSsctlSnapshotFormatterSource();
        var mcpAppStateSource = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        AssertContains(sharedFormatterSource, "FlashbackEncodingFailed");
        AssertContains(sharedFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(sharedFormatterSource, "FlashbackCleanupInProgress");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateActive");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateRequested");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateDraining");
        AssertContains(ssctlFormatterSource, "FlashbackEncodingFailed");
        AssertContains(ssctlFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(ssctlFormatterSource, "FlashbackCleanupInProgress");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateActive");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateRequested");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateDraining");
        AssertContains(mcpAppStateSource, "FormatSnapshot(response, includeFlashback: true)");
        AssertOccursBefore(
            sharedFormatterSource,
            "var flashbackFailed = Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");
        AssertOccursBefore(
            ssctlFormatterSource,
            "var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");
    }

    private static void AssertFlashbackBufferCyclePolicies(string captureServiceSource, string flashbackBackendSource)
    {
        var cycleFlashbackBuffer = ExtractSourceBlock(
            captureServiceSource,
            "private async Task CycleFlashbackBufferAsync",
            "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync");
        var backendCycleFlashbackBuffer = ExtractSourceBlock(
            flashbackBackendSource,
            "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync",
            "private async Task RollBackPreviewBackendStartAsync");
        AssertContains(cycleFlashbackBuffer, "var committedCycleToken = CancellationToken.None;");
        AssertContains(backendCycleFlashbackBuffer, "FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED");
        AssertContains(backendCycleFlashbackBuffer, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertDoesNotContain(cycleFlashbackBuffer, "cancellationToken: cancellationToken");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "StopAndDisposeOldSinkForBufferCycleAsync(",
            "ClearSinkAndSettings();");
        AssertContains(backendCycleFlashbackBuffer, "await oldSink.DisposeAsync().ConfigureAwait(false);");
        AssertContains(backendCycleFlashbackBuffer, "var oldPlaybackController = TakePlaybackController();");
        AssertContains(backendCycleFlashbackBuffer, "oldPlaybackController.GoLive();");
        AssertContains(backendCycleFlashbackBuffer, "oldPlaybackController.Dispose();");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "DisposePlaybackForBufferCycle(",
            "bufferManager.PurgeCompletedSegments();");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "DisposePlaybackForBufferCycle(",
            "DetachOldSinkProducersForBufferCycle(");
        AssertContains(backendCycleFlashbackBuffer, "DetachProducers(");
        AssertContains(backendCycleFlashbackBuffer, "\"FLASHBACK_CYCLE_DETACH_WARN\"");
        var cycleNewSinkStart = backendCycleFlashbackBuffer;
        AssertContains(cycleNewSinkStart, "committedCycleToken,");
        AssertContains(cycleNewSinkStart, "AttachProducers(");
        AssertContains(cycleNewSinkStart, "new FlashbackProducerAttachRequest(");
        AssertContains(cycleNewSinkStart, "\"buffer_cycle\"");
        AssertContains(cycleNewSinkStart, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertContains(cycleNewSinkStart, "newSink.FrameEncoded -= request.FrameEncodedHandler;");
        AssertContains(cycleNewSinkStart, "request.VideoCapture.SetFlashbackSink(null);");
        AssertContains(cycleNewSinkStart, "request.AudioCapture?.DetachFlashbackSink();");
        AssertContains(cycleNewSinkStart, "request.MicrophoneCapture?.SetAudioWriter(null);");
        AssertContains(cycleNewSinkStart, "new FlashbackPlaybackController(bufferManager)");
        AssertContains(cycleNewSinkStart, "GpuDecodeEnabled = request.Settings.FlashbackGpuDecode");
        AssertContains(cycleNewSinkStart, "request.PreviewFrameSink");
        AssertContains(cycleNewSinkStart, "PlaybackController = playbackController;");
        AssertContains(cycleNewSinkStart, "FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(cycleNewSinkStart, "FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN");
        AssertContains(flashbackBackendSource, "request.PurgeSegments");
        AssertContains(captureServiceSource, "new FlashbackPreviewBackendDisposalRequest(");
        AssertContains(flashbackBackendSource, "new FlashbackBackendArtifactCleanupRequest(");
        AssertContains(captureServiceSource, "effectivePurgeSegments,");
        AssertContains(captureServiceSource, "!activeFlashbackSink.CanBeginRecording");
        AssertContains(captureServiceSource, "_flashbackRecordingStartInProgress");
        AssertContains(captureServiceSource, "_flashbackRecordingFinalizeInProgress");
        AssertContains(captureServiceSource, "IsFlashbackRecordingBackendOwnedByRecording");
        AssertContains(captureServiceSource, "Volatile.Write(ref _flashbackRecordingStartInProgress, 1)");
        AssertContains(captureServiceSource, "Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 1)");
        AssertContains(captureServiceSource, "Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 0)");
        AssertContains(captureServiceSource, "await _flashbackBackendLeaseLock.WaitAsync(transitionToken)");
        AssertContains(captureServiceSource, "BeginFlashbackRecordingAccounting");
        AssertContains(captureServiceSource, "EndFlashbackRecordingAccounting");
        AssertContains(captureServiceSource, "CancelRecordingStartRollback");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_START_ROLLBACK_WARN type={rollbackEx.GetType().Name} error='{rollbackEx.Message}'");
        AssertContains(captureServiceSource, "var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_INIT_CANCELLED");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_INIT_FAIL");
        AssertContains(captureServiceSource, "Logger.Log($\"{failureToken} type={ex.GetType().Name} error='{ex.Message}'\")");
        AssertContains(flashbackBackendSource, "new FlashbackProducerDetachRequest(");
        AssertContains(flashbackBackendSource, "\"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN\"");
        AssertContains(flashbackBackendSource, "Logger.Log($\"{request.WarningToken} target=video");
        AssertContains(flashbackBackendSource, "Logger.Log($\"{request.WarningToken} target=audio");
        AssertContains(flashbackBackendSource, "Logger.Log($\"{request.WarningToken} target=microphone");
        AssertContains(captureServiceSource, "MIC_MONITOR_WRITER_DETACH_WARN");
        AssertOccursBefore(captureServiceSource, "MIC_MONITOR_WRITER_DETACH_WARN", "await mic.DisposeAsync().ConfigureAwait(false);");
        AssertContains(captureServiceSource, "VIDEO_DIAG flashback_recording_pipeline");
        AssertContains(captureServiceSource, "BeginFlashbackBackendCleanup");
        AssertContains(captureServiceSource, "detachMicrophoneWriter: !preserveDedicatedRecordingMic");
        AssertContains(captureServiceSource, "recordingContext = fbRecordingContext");
        AssertDoesNotContain(captureServiceSource, "SetFatalErrorCallback(OnRecordingBackendFatalError)");
    }

    internal static Task RecordingBackendFinalizeAndCleanup_PreservesFlashbackBoundaries()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var flashbackBackendSource = sources.FlashbackBackendSource;
        var captureServiceSource = sources.CaptureServiceSource;
        var captureHealthSnapshotRootSource = sources.CaptureHealthSnapshotRootSource;
        var captureSnapshotsSource = sources.CaptureSnapshotsSource;
        var unifiedVideoCaptureSource = sources.UnifiedVideoCaptureSource;
        var microphoneMonitorText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n");
        var stopRecordingBackendRouter = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync",
            "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync");
        var flashbackStopRecordingBackend = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync",
            "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync");
        var libAvStopRecordingBackend = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync",
            "private readonly record struct LibAvFinalizeStepResult");

        AssertContains(stopRecordingBackendRouter, "IsFlashbackRecordingBackendActive()");
        AssertContains(stopRecordingBackendRouter, "StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken)");
        AssertContains(stopRecordingBackendRouter, "StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken)");
        AssertDoesNotContain(stopRecordingBackendRouter, "OperationCanceledException? flashbackCancellationException = null;");
        AssertDoesNotContain(stopRecordingBackendRouter, "var sink = _recordingSink;");
        AssertContains(flashbackStopRecordingBackend, "OperationCanceledException? flashbackCancellationException = null;");
        AssertContains(flashbackStopRecordingBackend, "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");");
        AssertContains(flashbackStopRecordingBackend, "if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))");
        AssertContains(flashbackStopRecordingBackend, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(flashbackStopRecordingBackend, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackStopRecordingBackend, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackStopRecordingBackend, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackStopRecordingBackend, "RecordLastFlashbackFailure(ex);");
        AssertContains(flashbackStopRecordingBackend, "_flashbackBackend.PreserveRecoverySegments(\"buffer_cycle_failed\");");
        AssertContains(flashbackStopRecordingBackend, "BeginFlashbackBackendCleanup(ex);");
        AssertContains(flashbackStopRecordingBackend, "FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        AssertDoesNotContain(flashbackStopRecordingBackend, "libAvSink.StopAsync(emergency, cancellationToken)");
        AssertContains(libAvStopRecordingBackend, "var detachedBackend = _recordingBackend.DetachLibAvBackend();");
        AssertContains(libAvStopRecordingBackend, "await unifiedVideoCapture.StopRecordingAsync()");
        AssertContains(libAvStopRecordingBackend, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(libAvStopRecordingBackend, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvStopRecordingBackend, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(libAvStopRecordingBackend, "FinalizeFlashbackRecordingAsync(");
        AssertOccursBefore(
            libAvStopRecordingBackend,
            "RestoreLibAvPreviewFeaturesAfterRecordingAsync(",
            "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))",
            "_lastRecordingIntegrity = BuildRecordingIntegritySummary(");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");",
            "_recordingStopwatch.Stop();");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);",
            "throw flashbackCancellationException;");
        var postFinalizeCycle = ExtractSourceBlock(
            flashbackStopRecordingBackend,
            "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync",
            "        return cancellationException;");
        AssertContains(postFinalizeCycle, "cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertOccursBefore(
            postFinalizeCycle,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_BUFFER_CYCLE_FAIL");
        AssertOccursBefore(
            postFinalizeCycle,
            "FLASHBACK_BUFFER_CYCLE_FAIL",
            "RecordLastFlashbackFailure(ex);");
        AssertOccursBefore(
            postFinalizeCycle,
            "RecordLastFlashbackFailure(ex);",
            "BeginFlashbackBackendCleanup(ex);");

        AssertFlashbackAndLibAvMicrophoneRestartPolicies(
            flashbackStopRecordingBackend,
            microphoneMonitorText,
            libAvStopRecordingBackend);
        AssertFlashbackBackendCleanupPolicies(captureServiceSource, flashbackBackendSource);
        AssertRecordingQueueHealthSnapshotTelemetry(
            captureServiceSource,
            captureHealthSnapshotRootSource,
            captureSnapshotsSource,
            unifiedVideoCaptureSource);

        return Task.CompletedTask;
    }

    private static void AssertFlashbackAndLibAvMicrophoneRestartPolicies(
        string flashbackStopRecordingBackend,
        string microphoneMonitorText,
        string libAvStopRecordingBackend)
    {
        var flashbackMicMonitorRestart = ExtractSourceBlock(
            flashbackStopRecordingBackend,
            "// Restart mic monitoring if preview is still active",
            "if (fbResult.Succeeded)");
        AssertContains(flashbackMicMonitorRestart, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(flashbackMicMonitorRestart, "OnlyWhenMissing: true,");
        AssertContains(flashbackMicMonitorRestart, "FlashbackAttachReason: null,");
        AssertContains(flashbackMicMonitorRestart, "RestartLogEvent: null,");
        AssertContains(flashbackMicMonitorRestart, "DisposeWarningEvent: \"FLASHBACK_MIC_RESTART_DISPOSE_WARN\"");
        AssertContains(flashbackMicMonitorRestart, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(flashbackMicMonitorRestart, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(flashbackMicMonitorRestart, "FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        AssertDoesNotContain(flashbackMicMonitorRestart, "WasapiAudioCapture? micCapture = null;");
        AssertDoesNotContain(flashbackMicMonitorRestart, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneMonitorText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneMonitorText, "if (options.OnlyWhenMissing && _previewAudioGraph.MicrophoneCapture != null)");
        AssertContains(microphoneMonitorText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneMonitorText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneMonitorText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_previewAudioGraph.MicrophoneCapture = micCapture;");

        AssertContains(libAvStopRecordingBackend, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvStopRecordingBackend, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvStopRecordingBackend, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(libAvStopRecordingBackend, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken)");
        AssertContains(libAvStopRecordingBackend, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        AssertContains(libAvStopRecordingBackend, "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        var standardMicMonitorRestart = ExtractSourceBlock(
            libAvStopRecordingBackend,
            "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync",
            "        return cancellationException;");
        AssertContains(standardMicMonitorRestart, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(standardMicMonitorRestart, "OnlyWhenMissing: false,");
        AssertContains(standardMicMonitorRestart, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(standardMicMonitorRestart, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(standardMicMonitorRestart, "DisposeWarningEvent: \"MIC_MONITOR_RESTART_DISPOSE_WARN\"");
        AssertContains(standardMicMonitorRestart, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(standardMicMonitorRestart, "cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(standardMicMonitorRestart, "Mic monitor restart failed (non-fatal): ");
        AssertDoesNotContain(standardMicMonitorRestart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(microphoneMonitorText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
    }

    private static void AssertFlashbackBackendCleanupPolicies(string captureServiceSource, string flashbackBackendSource)
    {
        AssertContains(captureServiceSource, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback export cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback recording finalize cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(captureServiceSource, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(captureServiceSource, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(captureServiceSource, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertContains(captureServiceSource, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(captureServiceSource, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        var disposeFlashbackPreviewBackendCore = ExtractSourceBlock(
            captureServiceSource,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest");
        AssertContains(disposeFlashbackPreviewBackendCore, "_flashbackBackend.DisposePreviewBackendAsync(request)");
        var disposeFlashbackPreviewBackendResources = ExtractSourceBlock(
            flashbackBackendSource,
            "public async Task DisposePreviewBackendAsync",
            "public void ScheduleDeferredArtifactCleanup");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "request.CancellationToken.ThrowIfCancellationRequested();", "CleanupArtifactsAfterExportAsync(");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "TakePlaybackController()", "flashbackPlaybackController.GoLive();");
        AssertContains(disposeFlashbackPreviewBackendResources, "DetachProducers(");
        AssertContains(disposeFlashbackPreviewBackendResources, "\"FLASHBACK_PREVIEW_DETACH_WARN\"");
        AssertContains(disposeFlashbackPreviewBackendResources, "await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "DetachProducers(", "await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "Clear();", "request.CancellationToken.ThrowIfCancellationRequested();");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "ScheduleDeferredArtifactCleanup(", "request.CancellationToken.ThrowIfCancellationRequested();");
        AssertContains(disposeFlashbackPreviewBackendResources, "var cleanupCompleted = await CleanupArtifactsAfterExportAsync(");
        AssertContains(disposeFlashbackPreviewBackendResources, "ScheduleDeferredArtifactCleanup(\n                Task.Delay(TimeSpan.FromSeconds(1)),");
        var deferredFlashbackBackendCleanup = ExtractSourceBlock(
            captureServiceSource,
            "private void ScheduleDeferredFlashbackBackendCleanup",
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync");
        AssertContains(deferredFlashbackBackendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(deferredFlashbackBackendCleanup, "_flashbackBackend.ScheduleDeferredArtifactCleanup(");
        AssertContains(deferredFlashbackBackendCleanup, "WaitForFlashbackBackendCleanupExportLockAsync");
        AssertContains(deferredFlashbackBackendCleanup, "ReleaseFlashbackBackendCleanupExportLock");

        var deferredFlashbackBackendResourcesCleanup = ExtractSourceBlock(
            flashbackBackendSource,
            "public void ScheduleDeferredArtifactCleanup",
            "public async Task<bool> CleanupArtifactsAfterExportAsync");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "CleanupArtifactsAfterExportAsync(");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "if (cleanupCompleted)");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{request.Reason}' attempt={attempt}");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "else if (attempt < 3)");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY reason='{request.Reason}' attempt={attempt} next_attempt={nextAttempt}");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "Task.Delay(TimeSpan.FromSeconds(5))");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP reason='{request.Reason}' attempt={attempt} preserve_segments=true");
        var flashbackBackendArtifactCleanup = ExtractSourceBlock(
            flashbackBackendSource,
            "public async Task<bool> CleanupArtifactsAfterExportAsync",
            "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync");
        AssertContains(flashbackBackendArtifactCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(captureServiceSource, "WaitAsync(\n            TimeSpan.FromSeconds(30),\n            CancellationToken.None)");
        AssertContains(flashbackBackendArtifactCleanup, "acquireExportOperationLockAsync()");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "request.FlashbackExporter.Dispose();", "request.BufferManager.PurgeAllSegments();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "request.FlashbackExporter.Dispose();", "request.BufferManager.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "acquireExportOperationLockAsync()", "request.FlashbackExporter.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "acquireExportOperationLockAsync()", "request.BufferManager.PurgeAllSegments();");
    }

    /// <summary>
    /// A real IProcessSupervisor fake that returns crafted ffprobe output.
    /// This is the test seam that reviewers flagged as missing.
    /// </summary>
    private sealed class FakeProcessSupervisorImpl
    {
        private readonly List<(string FileName, string Arguments, string? PriorityClass)> _calls = new();
        private string _streamInfoOutput = string.Empty;
        private string _cadenceOutput = string.Empty;
        private string _hdrSideDataOutput = string.Empty;
        private bool _ffprobeVersionSucceeds = true;
        private int _exitCode;

        public IReadOnlyList<(string FileName, string Arguments, string? PriorityClass)> Calls => _calls;

        public FakeProcessSupervisorImpl WithStreamInfo(string output)
        {
            _streamInfoOutput = output;
            return this;
        }

        public FakeProcessSupervisorImpl WithCadenceJson(string json)
        {
            _cadenceOutput = json;
            return this;
        }

        public FakeProcessSupervisorImpl WithHdrSideDataJson(string json)
        {
            _hdrSideDataOutput = json;
            return this;
        }

        public FakeProcessSupervisorImpl WithFfprobeUnavailable()
        {
            _ffprobeVersionSucceeds = false;
            return this;
        }

        public FakeProcessSupervisorImpl WithExitCode(int code)
        {
            _exitCode = code;
            return this;
        }

        /// <summary>
        /// Creates an instance that implements IProcessSupervisor via a DispatchProxy.
        /// </summary>
        public object CreateProxy()
        {
            var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
            var specType = RequireType("Sussudio.Services.Runtime.ProcessSpec");

            // Use the generic DispatchProxy.Create<T, TProxy>() method
            var createMethod = typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2)
                .MakeGenericMethod(supervisorType, typeof(FakeSupervisorProxy));

            var proxy = createMethod.Invoke(null, null)!;

            // Set the callback on our proxy
            ((FakeSupervisorProxy)proxy).SetHandler(async (method, args) =>
            {
                var spec = args[0];
                var fileName = (string)specType.GetProperty("FileName")!.GetValue(spec)!;
                var arguments = (string)specType.GetProperty("Arguments")!.GetValue(spec)!;
                var priorityClass = specType.GetProperty("PriorityClass")!.GetValue(spec)?.ToString();
                _calls.Add((fileName, arguments, priorityClass));

                // Determine which probe this is based on arguments
                string stdout;
                if (arguments.Contains("-version"))
                {
                    return CreateProcessRunResult(
                        _ffprobeVersionSucceeds,
                        _ffprobeVersionSucceeds ? 0 : 1,
                        "ffprobe version N/A");
                }
                else if (arguments.Contains("-show_frames"))
                {
                    stdout = _cadenceOutput;
                }
                else if (arguments.Contains("side_data_list"))
                {
                    stdout = _hdrSideDataOutput;
                }
                else
                {
                    stdout = _streamInfoOutput;
                }

                return CreateProcessRunResult(true, _exitCode, stdout);
            });

            return proxy;
        }

        private static object CreateProcessRunResult(bool started, int exitCode, string stdOut)
        {
            var resultType = RequireType("Sussudio.Services.Runtime.ProcessRunResult");
            var result = RuntimeHelpers.GetUninitializedObject(resultType);
            SetPropertyBackingField(result, "Started", started);
            SetPropertyBackingField(result, "TimedOut", false);
            SetPropertyBackingField(result, "ExitConfirmed", true);
            SetPropertyBackingField(result, "ExitCode", (int?)exitCode);
            SetPropertyBackingField(result, "StdOut", stdOut);
            SetPropertyBackingField(result, "StdErr", string.Empty);
            return result;
        }
    }

    /// <summary>
    /// DispatchProxy implementation for IProcessSupervisor.
    /// The key challenge: Invoke must return Task&lt;ProcessRunResult&gt;, not Task&lt;object&gt;.
    /// We use a helper to wrap the result in the correctly-typed Task.
    /// </summary>
    public class FakeSupervisorProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[], Task<object>>? _handler;

        public void SetHandler(Func<MethodInfo, object?[], Task<object>> handler)
        {
            _handler = handler;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (_handler == null)
                throw new InvalidOperationException("Handler not set on FakeSupervisorProxy");

            // RunAsync returns Task<ProcessRunResult>. We must return that exact type,
            // not Task<object>. Use reflection to create a typed Task wrapper.
            var resultType = targetMethod!.ReturnType; // Task<ProcessRunResult>
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = resultType.GetGenericArguments()[0]; // ProcessRunResult
                return WrapAsTypedTask(_handler(targetMethod, args!), innerType);
            }

            return _handler(targetMethod, args!);
        }

        private static object WrapAsTypedTask(Task<object> objectTask, Type targetType)
        {
            // Create a TaskCompletionSource<ProcessRunResult> and wire it to our Task<object>
            var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(targetType);
            var tcs = Activator.CreateInstance(tcsType)!;
            var setResultMethod = tcsType.GetMethod("SetResult")!;
            var setExceptionMethod = tcsType.GetMethod("SetException", new[] { typeof(Exception) })!;
            var taskProp = tcsType.GetProperty("Task")!;

            objectTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    setExceptionMethod.Invoke(tcs, new object[] { t.Exception!.InnerException! });
                else if (t.IsCanceled)
                    tcsType.GetMethod("SetCanceled", Type.EmptyTypes)!.Invoke(tcs, null);
                else
                    setResultMethod.Invoke(tcs, new[] { t.Result });
            }, TaskScheduler.Default);

            return taskProp.GetValue(tcs)!;
        }
    }

    private static object BuildRuntimeSnapshotForVerificationEx(
        string? requestedFormat = "HevcMp4",
        bool requestedHdrEnabled = false,
        bool hdrOutputActive = false,
        bool requestedHdrMasteringMetadata = false,
        uint? negotiatedWidth = 1920,
        uint? negotiatedHeight = 1080,
        uint? negotiatedFrameRateNumerator = 60,
        uint? negotiatedFrameRateDenominator = 1,
        string? flashbackExportOutputPath = null,
        string? flashbackExportVerificationFormat = null,
        string? lastOutputPath = null,
        string? recordingBackend = null,
        string? recordingIntegrityBackend = null)
    {
        var type = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "HdrOutputActive", hdrOutputActive);
        SetPropertyOrBackingField(snapshot, "RequestedHdrMasteringMetadata", (bool?)requestedHdrMasteringMetadata);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        SetPropertyOrBackingField(snapshot, "FlashbackExportOutputPath", flashbackExportOutputPath);
        SetPropertyOrBackingField(snapshot, "FlashbackExportVerificationFormat", flashbackExportVerificationFormat);
        SetPropertyOrBackingField(snapshot, "LastOutputPath", lastOutputPath);
        SetPropertyOrBackingField(snapshot, "RecordingBackend", recordingBackend);
        SetPropertyOrBackingField(snapshot, "RecordingIntegrityBackend", recordingIntegrityBackend);
        return snapshot;
    }

    private static object CreateVerifierWithFake(object fakeSupervisor)
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
        var ctor = verifierType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { supervisorType, typeof(string) },
            modifiers: null)
            ?? throw new InvalidOperationException("RecordingVerifier internal constructor not found.");
        return ctor.Invoke(new object[] { fakeSupervisor, "ffprobe.exe" });
    }

    private static async Task<object> RunVerifyAsync(object verifier, string? outputPath, object snapshot)
    {
        var verifierType = verifier.GetType();
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");
        var task = verifyAsync.Invoke(verifier, new object?[] { outputPath, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    // ── Helper: build cadence JSON with uniform frame timestamps ──

    private static object BuildRuntimeSnapshotForVerification(
        string? requestedFormat = "HevcMp4",
        bool requestedHdrEnabled = false,
        uint? negotiatedWidth = 1920,
        uint? negotiatedHeight = 1080,
        uint? negotiatedFrameRateNumerator = 60,
        uint? negotiatedFrameRateDenominator = 1)
    {
        var type = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        return snapshot;
    }

    // RecordingVerifier early-exit paths and source-shape contracts.

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");

        var snapshot = BuildRuntimeSnapshotForVerification();
        var task = verifyAsync.Invoke(verifier, new object?[] { "/nonexistent/file.mp4", snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result")!;
        var result = resultProp.GetValue(task)!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual(false, GetBoolProperty(result, "FileExists"), "FileExists");
        AssertContains(GetStringProperty(result, "Message"), "does not exist");
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFileIsEmpty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_test_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, Array.Empty<byte>());
        try
        {
            var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
            var verifier = Activator.CreateInstance(verifierType)!;
            var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)!;

            var snapshot = BuildRuntimeSnapshotForVerification();
            var task = verifyAsync.Invoke(verifier, new object?[] { tempFile, snapshot, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "output-empty");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)!;

        var snapshot = BuildRuntimeSnapshotForVerification();
        var task = verifyAsync.Invoke(verifier, new object?[] { null, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
    }

    internal static Task RecordingVerifier_ImplementsIRecordingVerifier()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var interfaceType = RequireType("Sussudio.Services.Contracts.IRecordingVerifier");

        AssertEqual(true, interfaceType.IsAssignableFrom(verifierType), "RecordingVerifier implements IRecordingVerifier");

        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(verifyAsync, "RecordingVerifier.VerifyAsync");

        var parameters = verifyAsync!.GetParameters();
        AssertEqual(3, parameters.Length, "VerifyAsync parameter count");

        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        AssertEqual(true, verifyAsync.ReturnType.IsGenericType, "VerifyAsync returns generic Task");
        AssertEqual(resultType, verifyAsync.ReturnType.GetGenericArguments()[0], "VerifyAsync returns Task<RecordingVerificationResult>");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_CadenceAnalysisLivesWithVerifier()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public sealed class RecordingVerifier : IRecordingVerifier");
        AssertContains(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(rootText, "private static CadenceMetrics ComputeCadenceMetrics(");
        AssertContains(rootText, "private static double? TryGetFrameTimestampSeconds(JsonElement frame)");
        AssertContains(rootText, "private static double? TryGetJsonDouble(JsonElement element, string propertyName)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Cadence.cs")),
            "RecordingVerifier cadence ffprobe pass folded into verifier owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Ffprobe.cs")),
            "RecordingVerifier ffprobe helper partial folded into verifier owner");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_ProbeValidationAndResultShapingOwnership()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public async Task<RecordingVerificationResult> VerifyAsync(");
        AssertContains(rootText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertContains(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(rootText, "private static Dictionary<string, string> ParseKeyValueOutput(string output)");
        AssertContains(rootText, "private static double? TryParseRational(string? value)");
        AssertContains(rootText, "private ProcessSpec CreateFfprobeProcessSpec(");
        AssertContains(rootText, "private static void ValidateContainer(");
        AssertContains(rootText, "private static void ValidateCodec(");
        AssertContains(rootText, "private static void ValidateDimensions(");
        AssertContains(rootText, "private static double? ResolveExpectedFrameRate(");
        AssertContains(rootText, "private static void ValidateCadence(");
        AssertContains(rootText, "private readonly record struct HdrValidationResult(");
        AssertContains(rootText, "private static HdrValidationResult ValidateHdrMetadata(");
        AssertContains(rootText, "private static string ResolveExpectedFormat(");
        AssertContains(rootText, "private static bool IsFlashbackRecording(");
        AssertContains(rootText, "private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(");
        AssertContains(rootText, "private static HdrParityResult BuildHdrParityResult(");
        AssertContains(rootText, "private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(");
        AssertContains(rootText, "private static string? TryGetMismatchPart(");
        AssertContains(rootText, "private static RecordingVerificationResult CreateEarlyFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Results.cs")),
            "RecordingVerifier.Results.cs folded into RecordingVerifier.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Validation.cs")),
            "RecordingVerifier validation policy folded into RecordingVerifier.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Ffprobe.cs")),
            "RecordingVerifier ffprobe probe/process helpers folded into RecordingVerifier.cs");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerificationResult_HasExpectedProperties()
    {
        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");

        var expectedProps = new[]
        {
            "TimestampUtc", "Succeeded", "Message", "OutputPath", "FileExists", "FileSizeBytes",
            "VerificationMode", "DetectedContainer", "DetectedVideoCodec", "DetectedPixelFormat",
            "DetectedColorPrimaries", "DetectedColorTransfer", "DetectedColorSpace",
            "DetectedHdrSideDataTypes", "HdrMetadataPresent", "HdrColorimetryValid",
            "HdrMasteringMetadataPresent", "HdrVerificationLevel",
            "DetectedWidth", "DetectedHeight", "DetectedFrameRate",
            "CadenceSampleCount", "CadenceObservedFps", "CadenceExpectedIntervalMs",
            "CadenceAverageIntervalMs", "CadenceP95IntervalMs", "CadenceMaxIntervalMs",
            "CadenceJitterStdDevMs", "CadenceSevereGapCount", "CadenceSevereGapPercent",
            "CadenceEstimatedDroppedFrames", "CadenceEstimatedDropPercent",
            "PrimaryMismatchCode", "PrimaryMismatchExpected", "PrimaryMismatchActual",
            "Mismatches", "HdrParity"
        };

        foreach (var prop in expectedProps)
        {
            AssertNotNull(
                resultType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance),
                $"RecordingVerificationResult.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification()
    {
        var scriptText = ReadRepoFile("tools/verify-dedicated-libav-recording.ps1")
            .Replace("\r\n", "\n");

        AssertContains(scriptText, "SetFlashbackEnabled");
        AssertContains(scriptText, "SetRecordingEnabled");
        AssertContains(scriptText, "VerifyLastRecording");
        AssertContains(scriptText, "[int]$ResponseTimeoutBufferMs = 5000");
        AssertContains(scriptText, "-ResponseTimeoutMs $ResponseTimeoutMs");
        AssertContains(scriptText, "$responseTimeoutMs = [Math]::Max($TimeoutMs + $ResponseTimeoutBufferMs, 60000)");
        AssertContains(scriptText, "-ResponseTimeoutMs $responseTimeoutMs -AllowFailure");
        AssertContains(scriptText, "$rawText = ($raw | Out-String).Trim()");
        AssertContains(scriptText, "if ($AllowFailure -and $null -ne $response)");
        AssertContains(scriptText, "$initialOutputPath = [string](Get-PropertyValue $initialSnapshot \"OutputPath\" \"\")");
        AssertContains(scriptText, "$outputPathChanged = -not [string]::IsNullOrWhiteSpace($OutputDirectory)");
        AssertContains(scriptText, "function Assert-AutomationResponseSucceeded");
        AssertContains(scriptText, "Invoke-Automation -Command \"SetOutputPath\" -Payload @{ outputPath = $initialOutputPath } -AllowFailure");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $stopResponse -Context \"Cleanup recording stop\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $outputPathResponse -Context \"Output path restore\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $flashbackResponse -Context \"Flashback restore\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $previewResponse -Context \"Preview stop cleanup\"");
        AssertContains(scriptText, "$verificationCompleted = $true");
        AssertContains(scriptText, "Dedicated LibAv verification cleanup failed");
        AssertDoesNotContain(scriptText, "ConvertFrom-Json -Depth");
        AssertContains(scriptText, "[int]$ExpectedWidth = 3840");
        AssertContains(scriptText, "[int]$ExpectedHeight = 2160");
        AssertContains(scriptText, "[double]$ExpectedFrameRate = 120.0");
        AssertContains(scriptText, "[double]$FrameRateTolerance = 1.0");
        AssertContains(scriptText, "[double]$MinExpectedFrameFraction = 0.80");
        AssertContains(scriptText, "[int]$MaxEncoderLastWriteAgeMs = 2000");
        AssertContains(scriptText, "RecordingFileGrowing");
        AssertContains(scriptText, "RecordingStopped");
        AssertContains(scriptText, "VideoFramesFlowing");
        AssertContains(scriptText, "function Assert-ExpectedRuntimeMode");
        AssertContains(scriptText, "NegotiatedWidth");
        AssertContains(scriptText, "ActualWidth");
        AssertContains(scriptText, "NegotiatedFrameRate");
        AssertContains(scriptText, "ActualFrameRate");
        AssertContains(scriptText, "Get-PropertyValue $Snapshot \"RecordingBackend\" \"\") -eq \"LibAv\"");
        AssertContains(scriptText, "FlashbackActive=false");
        AssertContains(scriptText, "RecordingEncodingFailed");
        AssertContains(scriptText, "EncoderLastWriteAgeMs");
        AssertContains(scriptText, "VideoDropsQueueSaturated");
        AssertContains(scriptText, "VideoDropsBacklogEviction");
        AssertContains(scriptText, "RecordingGpuFramesDropped");
        AssertContains(scriptText, "RecordingCudaFramesDropped");
        AssertContains(scriptText, "CaptureCadenceEstimatedDroppedFrames");
        AssertContains(scriptText, "DetectedWidth");
        AssertContains(scriptText, "DetectedHeight");
        AssertContains(scriptText, "DetectedFrameRate");
        AssertContains(scriptText, "RecordingVerifier treats ffprobe cadence metrics as optional");
        AssertContains(scriptText, "Verification CadenceSampleCount must be greater than zero.");
        AssertContains(scriptText, "if ($null -ne $estimatedDrops)");
        AssertContains(scriptText, "if ($null -ne $severeGaps)");
        AssertContains(scriptText, "function Assert-FileLevelIntegrityFallback");
        AssertContains(scriptText, "-show_entries\", \"stream=nb_frames,duration");
        AssertContains(scriptText, "Fallback ffprobe frame count");
        AssertContains(scriptText, "if ($null -eq $cadenceSampleCount)");
        AssertContains(scriptText, "CadenceEstimatedDroppedFrames");
        AssertContains(scriptText, "CadenceSevereGapCount");
        AssertContains(scriptText, "VideoFramesEnqueued regressed");
        AssertContains(scriptText, "EncoderVideoFramesEncoded regressed");
        AssertContains(scriptText, "RecordingTotalBytes regressed");
        AssertContains(scriptText, "$minimumExpectedFrames = [long][Math]::Floor($ExpectedFrameRate * $DurationSeconds * $MinExpectedFrameFraction)");
        AssertContains(scriptText, "NoRestoreFlashback");
        AssertContains(scriptText, "initialFlashbackActive");
        AssertContains(scriptText, "restoreFlashbackAfterDisable");

        AssertOccursBefore(
            scriptText,
            "Invoke-Automation -Command \"SetFlashbackEnabled\" -Payload @{ enabled = $false }",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $true }");
        AssertOccursBefore(
            scriptText,
            "Assert-ExpectedRuntimeMode -Snapshot $preDisable -Context \"Pre-record\"",
            "Invoke-Automation -Command \"SetFlashbackEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Assert-LibAvRuntimeSnapshot -Snapshot $recordingStart",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Assert-Condition ($progressObservedCount -gt 0)",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Wait-Condition -ConditionName \"RecordingStopped\"",
            "$verifyResponse = Invoke-Automation -Command \"VerifyLastRecording\"");
        AssertOccursBefore(
            scriptText,
            "$verification = Get-PropertyValue",
            "Assert-Verification -Verification $verification");
        AssertOccursBefore(
            scriptText,
            "$verificationCompleted = $true",
            "if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0)");
        AssertOccursBefore(
            scriptText,
            "if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0)",
            "Write-Host \"Dedicated LibAv recording verification: PASS\"");

        return Task.CompletedTask;
    }

    private static string BuildCadenceJson(double fps, int frameCount)
    {
        var interval = 1.0 / fps;
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"frames\":[");
        for (var i = 0; i < frameCount; i++)
        {
            if (i > 0) sb.Append(',');
            var ts = i * interval;
            sb.Append($"{{\"best_effort_timestamp_time\":{ts:F6}}}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    // ── Integration test: ffprobe unavailable ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ffprobe_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }); // minimal mp4 header
        try
        {
            var fake = new FakeProcessSupervisorImpl().WithFfprobeUnavailable();
            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: ffprobe exit code failure ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_exit_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithExitCode(1)
                .WithStreamInfo("");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe-failed");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: codec match (HEVC) ──

    internal static async Task RecordingVerifier_RunsFfprobeBelowNormalPriority()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_priority_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            _ = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, fake.Calls.Count >= 2, "ffprobe calls recorded");
            foreach (var call in fake.Calls)
            {
                AssertEqual("BelowNormal", call.PriorityClass, $"ffprobe priority for {call.Arguments}");
            }
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hevc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
            AssertEqual((uint)1920, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedWidth")), "DetectedWidth");
            AssertEqual((uint)1080, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedHeight")), "DetectedHeight");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: codec mismatch ──

    internal static async Task RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_codec_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "codec-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: H264 codec match ──

    internal static async Task RecordingVerifier_PassesVerification_ForH264Format()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_h264_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("h264", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: resolution mismatch ──

    internal static async Task RecordingVerifier_UsesFlashbackExportVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportOutputPath: tempFile,
                flashbackExportVerificationFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_UsesFlashbackRecordingVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_recording_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportVerificationFormat: "HevcMp4",
                lastOutputPath: tempFile,
                recordingIntegrityBackend: "Flashback");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_DetectsResolutionMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_res_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1280\n" +
                    "height=720\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedWidth: 1920, negotiatedHeight: 1080);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "resolution-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: frame rate mismatch ──

    internal static async Task RecordingVerifier_DetectsFrameRateMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_fps_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=30/1\n" +
                    "r_frame_rate=30/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "fps-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: HDR validation passes with correct metadata ──

    internal static async Task RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // Use hdrOutputActive=true (not requestedHdrEnabled) to trigger HDR validation
            // without the ProbeHdrSideDataAsync JSON path (avoids System.Text.Json version mismatch)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=p010le\n" +
                    "color_primaries=bt2020\n" +
                    "color_transfer=smpte2084\n" +
                    "color_space=bt2020nc\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("p010le", GetStringProperty(result, "DetectedPixelFormat"), "DetectedPixelFormat");
            AssertEqual(true, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(true, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Verified", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(true, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");
            AssertEqual("ColorimetryOnly", GetStringProperty(hdrParity, "VerificationLevel"), "HdrParity.VerificationLevel");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: HDR colorimetry mismatch ──

    internal static async Task RecordingVerifier_DetectsHdrColorimetryMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_bad_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // SDR colorimetry on an HDR-active recording (use hdrOutputActive, not requestedHdrEnabled
            // to avoid ProbeHdrSideDataAsync JSON path)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n" +
                    "color_primaries=bt709\n" +
                    "color_transfer=bt709\n" +
                    "color_space=bt709\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            // Should have multiple HDR-related mismatches
            var mismatches = GetPropertyValue(result, "Mismatches") as System.Collections.IEnumerable;
            var mismatchList = new List<string>();
            foreach (var m in mismatches!) mismatchList.Add(m?.ToString() ?? "");
            var hasPixfmtMismatch = mismatchList.Any(m => m.Contains("pixfmt-not-10bit"));
            var hasColorimetryMismatch = mismatchList.Any(m => m.Contains("colorimetry-mismatch"));
            AssertEqual(true, hasPixfmtMismatch, "Has pixfmt-not-10bit mismatch");
            AssertEqual(true, hasColorimetryMismatch, "Has colorimetry-mismatch");

            AssertEqual(false, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(false, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Mismatch", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(false, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");

            var taxonomy = GetPropertyValue(hdrParity, "MismatchTaxonomy") as System.Collections.IEnumerable;
            var taxonomyEntries = new List<object>();
            foreach (var entry in taxonomy!) taxonomyEntries.Add(entry!);
            var hasHdrError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "HDR" &&
                GetStringProperty(entry, "Code") == "pixfmt-not-10bit" &&
                GetStringProperty(entry, "Severity") == "Error");
            var hasColorimetryError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "Colorimetry" &&
                GetStringProperty(entry, "Code") == "colorimetry-mismatch" &&
                GetStringProperty(entry, "Severity") == "Error");
            AssertEqual(true, hasHdrError, "HDR mismatch taxonomy is Error severity");
            AssertEqual(true, hasColorimetryError, "Colorimetry mismatch taxonomy is Error severity");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: NTSC frame rate tolerance ──

    internal static async Task RecordingVerifier_PassesNtscFrameRateWithinTolerance()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ntsc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // 59.94 fps (60000/1001) vs expected 60 fps — within 0.75 tolerance
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60000/1001\n" +
                    "r_frame_rate=60000/1001\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            // 60 - 59.94 = 0.06 which is within 0.75 tolerance
            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static Task UnifiedVideoCapture_SinkFanoutOwnsRecordingAndFlashbackFanout()
    {
        var fanoutSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackGpuFrame(IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(fanoutSource, "private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.SinkFanout.Flashback.cs")),
            "UnifiedVideoCapture Flashback fanout folded into the source-session owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.SinkFanout.cs")),
            "UnifiedVideoCapture recording/Flashback fanout folded into the source-session owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.FrameIngress.cs")),
            "UnifiedVideoCapture frame ingress folded into the source-session owner");
        AssertDoesNotContain(fanoutSource, "partial class UnifiedVideoCapture");

        return Task.CompletedTask;
    }

    internal static Task UnifiedVideoCapture_FrameIngressLivesWithSourceSessionRoot()
    {
        var frameIngressSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameIngressSource, "private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)");
        AssertContains(frameIngressSource, "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)");
        AssertContains(frameIngressSource, "private void OnDualFrameArrived(");
        AssertContains(frameIngressSource, "private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)");
        AssertContains(frameIngressSource, "private void FirePixelFormatObserverOnce(string format)");
        AssertContains(frameIngressSource, "private void SignalFatalError(Exception ex, string logMessage)");
        AssertContains(frameIngressSource, "private void OnMjpegPipelinePreviewFrameDecoded(PooledVideoFrameLease frame)");
        AssertContains(frameIngressSource, "private unsafe void SubmitPreviewRawFrame(");
        AssertContains(frameIngressSource, "private void TrackPreviewVisualFrame(");
        AssertContains(frameIngressSource, "private void MarkPreviewVisualCadenceUnavailable(string reason)");
        AssertContains(frameIngressSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(frameIngressSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(frameIngressSource, "private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.Preview.cs")),
            "UnifiedVideoCapture preview submission folded into the source-session owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.FrameIngress.cs")),
            "UnifiedVideoCapture frame ingress folded into the source-session owner");
        AssertContains(frameIngressSource, "internal sealed class UnifiedVideoCapture : IAsyncDisposable, ILiveVideoSource");
        AssertDoesNotContain(frameIngressSource, "partial class UnifiedVideoCapture");

        var rawIngress = ExtractSourceBlock(
            frameIngressSource,
            "private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)",
            "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)");
        AssertOccursBefore(rawIngress, "Interlocked.Increment(ref _videoFramesArrived)", "Interlocked.Exchange(ref _lastVideoFrameArrivedTick");
        AssertOccursBefore(rawIngress, "Interlocked.Exchange(ref _lastVideoFrameArrivedTick", "RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);");
        AssertOccursBefore(rawIngress, "FrameLedgerStage.CompressedQueued", "return;");
        AssertOccursBefore(rawIngress, "FirePixelFormatObserverOnce(isP010 ? \"P010\" : \"NV12\");", "EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);");
        AssertOccursBefore(rawIngress, "EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);", "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);");
        AssertOccursBefore(rawIngress, "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);", "SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);");

        var mjpegIngress = ExtractSourceBlock(
            frameIngressSource,
            "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)",
            "private void OnDualFrameArrived(");
        AssertOccursBefore(mjpegIngress, "FirePixelFormatObserverOnce(\"NV12\");", "EnqueueRecordingFrame(frame);");
        AssertOccursBefore(mjpegIngress, "EnqueueRecordingFrame(frame);", "EnqueueFlashbackFrame(frame);");

        var dualIngress = ExtractSourceBlock(
            frameIngressSource,
            "private void OnDualFrameArrived(",
            "private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)");
        AssertOccursBefore(dualIngress, "Interlocked.Increment(ref _videoFramesArrived)", "Interlocked.Exchange(ref _lastVideoFrameArrivedTick");
        AssertOccursBefore(dualIngress, "FirePixelFormatObserverOnce(isP010 ? \"P010\" : \"NV12\");", "var gpuEncoder = Volatile.Read(ref _gpuRecordingEncoder);");
        AssertOccursBefore(dualIngress, "EnqueueGpuRecordingFrame(gpuEncoder, gpuTexture, gpuSubresource, sourceSequence);", "EnqueueFlashbackGpuFrame(gpuTexture, gpuSubresource, sourceSequence);");
        AssertOccursBefore(dualIngress, "EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);", "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);");
        AssertOccursBefore(dualIngress, "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);", "previewSink.SubmitTexture(");
        AssertOccursBefore(dualIngress, "Volatile.Read(ref _strictPreviewTextureRequired)", "SignalFatalError(");
        AssertOccursBefore(dualIngress, "Volatile.Read(ref _strictPreviewTextureRequired)", "SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);");

        AssertOccursBefore(frameIngressSource, "Logger.Log(logMessage);", "Interlocked.Exchange(ref _fatalErrorSignaled, 1)");
        AssertOccursBefore(frameIngressSource, "Interlocked.Exchange(ref _fatalErrorSignaled, 1)", "FatalErrorOccurred?.Invoke(this, ex);");
        AssertContains(frameIngressSource, "UNIFIED_VIDEO_FATAL_CALLBACK_FAIL");

        return Task.CompletedTask;
    }

    internal static Task UnifiedVideoCapture_LifecycleLivesWithRootState()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var lifecycleSource = rootSource;
        var initializationSource = lifecycleSource;
        var mjpegStartupSource = lifecycleSource;
        var mjpegLifecycleSource = lifecycleSource;

        AssertContains(initializationSource, "public async Task InitializeAsync(");
        AssertContains(initializationSource, "var d3dManager = new SharedD3DDeviceManager();");
        AssertContains(initializationSource, "CreateExternalMjpegPipelineIfNeeded(");
        AssertContains(initializationSource, "InstallMjpegPreviewJitterBuffer(capture.Fps > 0 ? capture.Fps : fps);");
        AssertContains(initializationSource, "capture.FatalErrorOccurred += OnCaptureFatalError;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.Initialization.cs")),
            "UnifiedVideoCapture initialization folded into root source-session owner");
        AssertContains(lifecycleSource, "public void Start()");
        AssertContains(lifecycleSource, "public async Task StopAsync()");
        AssertContains(lifecycleSource, "public async ValueTask DisposeAsync()");
        AssertContains(lifecycleSource, "public async ValueTask DisposeForPreviewReinitAsync()");
        AssertContains(lifecycleSource, "private async ValueTask DisposeCoreAsync(bool disposeSharedD3DDeviceManager)");
        AssertContains(lifecycleSource, "private void ThrowIfDisposed()");
        AssertContains(lifecycleSource, "private void OnCaptureFatalError(object? sender, Exception ex)");
        AssertContains(mjpegStartupSource, "private static bool ShouldUseExternalMjpegDecode(");
        AssertContains(mjpegStartupSource, "private ParallelMjpegDecodePipeline? CreateExternalMjpegPipelineIfNeeded(");
        AssertContains(mjpegStartupSource, "private void InstallMjpegPreviewJitterBuffer(double fps)");
        AssertContains(mjpegLifecycleSource, "private void StopAndDisposeMjpegPipeline(ParallelMjpegDecodePipeline mjpegPipelineToStop)");
        AssertContains(mjpegLifecycleSource, "private static void DisposeMjpegPipelineResources(");
        AssertContains(mjpegLifecycleSource, "private void OnMjpegPipelineFatalError(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.MjpegPipelineLifecycle.cs")),
            "UnifiedVideoCapture MJPEG startup helpers folded into root source-session owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.Lifecycle.cs")),
            "UnifiedVideoCapture lifecycle folded into root source-session owner");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var contractsSource = ReadRepoFile("Sussudio/Services/Contracts/ServiceContracts.cs")
            .Replace("\r\n", "\n");
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var drainBlock = ExtractSourceBlock(
            wasapiSource,
            "private void DrainCapturePackets()",
            "private void OnCaptureFailed");
        AssertContains(drainBlock, "handoffToPlayback = DispatchConvertedAudioPacket(converted);");
        AssertDoesNotContain(drainBlock, "InvokeHotAudioWriter(");
        AssertDoesNotContain(drainBlock, "WriteAudioToSinkOnCaptureThread(");
        AssertDoesNotContain(drainBlock, ".GetAwaiter()");
        AssertDoesNotContain(drainBlock, "Volatile.Read(ref _recordingSink)");
        AssertContains(wasapiSource, "private void CaptureThreadMain()");
        AssertContains(wasapiSource, "private bool DispatchConvertedAudioPacket(ConvertedAudioPacket converted)");
        AssertContains(wasapiSource, "var audioWriter = Volatile.Read(ref _audioWriter);");
        AssertContains(wasapiSource, "var sink = Volatile.Read(ref _recordingSink);");
        AssertContains(wasapiSource, "var flashbackSink = Volatile.Read(ref _flashbackSink);");
        AssertContains(wasapiSource, "var playback = Volatile.Read(ref _playback);");
        AssertContains(wasapiSource, "playback.EnqueuePooledSamples(convertedBuffer, converted.Length);");
        AssertContains(wasapiSource, "private static void InvokeHotAudioWriter(");
        AssertContains(wasapiSource, "private static void WriteAudioToSinkOnCaptureThread(");
        AssertContains(wasapiSource, "private static void CompleteHotAudioWrite(Task task, string target)");
        AssertContains(wasapiSource, "if (!task.IsCompleted)");
        AssertContains(wasapiSource, "Audio writers must copy/enqueue synchronously and return Task.CompletedTask.");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Fanout.cs")),
            "converted audio fan-out lives with the WASAPI capture loop");
        AssertContains(contractsSource, "Hot WASAPI callback write.");
        AssertContains(contractsSource, "must not do blocking/async work");

        var libAvAudioWrite = ExtractSourceBlock(
            libAvSource,
            "public Task WriteAudioAsync",
            "public Task WriteMicrophoneAudioAsync");
        var flashbackAudioWrite = ExtractSourceBlock(
            flashbackSource,
            "public Task WriteAudioAsync",
            "public Task WriteMicrophoneAudioAsync");
        AssertContains(libAvAudioWrite, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(libAvAudioWrite, "return Task.CompletedTask;");
        AssertContains(flashbackAudioWrite, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(flashbackAudioWrite, "return Task.CompletedTask;");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_ConversionLivesWithLifecycleRoot()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "internal sealed class WasapiAudioCapture : IAsyncDisposable");
        AssertContains(wasapiSource, "private void CaptureThreadMain()");
        AssertContains(wasapiSource, "private void DrainCapturePackets()");
        AssertContains(wasapiSource, "public void AttachRecordingSink(IRecordingSink sink)");
        AssertContains(wasapiSource, "public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)");
        AssertContains(wasapiSource, "internal void SetPlayback(WasapiAudioPlayback? playback)");
        AssertContains(wasapiSource, "private ConvertedAudioPacket ConvertToOutputFormat(");
        AssertContains(wasapiSource, "private int ComputeResampledFrameCount(");
        AssertContains(wasapiSource, "private static void ResampleStereoLinear(");
        AssertContains(wasapiSource, "private static unsafe void DecodeToStereo(");
        AssertContains(wasapiSource, "private static unsafe float ReadSample(");
        AssertContains(wasapiSource, "private static void ReturnPacketBuffer(ConvertedAudioPacket packet)");
        AssertContains(wasapiSource, "private readonly struct ConvertedAudioPacket");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Conversion.cs")),
            "WASAPI capture conversion folded into capture lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.CaptureLoop.cs")),
            "WASAPI capture loop folded into capture lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_InitializationLivesWithLifecycleRoot()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "internal sealed class WasapiAudioCapture : IAsyncDisposable");
        AssertContains(wasapiSource, "public Task InitializeAsync(string audioDeviceId, CancellationToken ct)");
        AssertContains(wasapiSource, "WasapiComInterop.CreateDeviceEnumerator()");
        AssertContains(wasapiSource, "audioClient.GetMixFormat(out mixFormat)");
        AssertContains(wasapiSource, "WasapiComInterop.AllocFloatStereo48kFormat()");
        AssertContains(wasapiSource, "audioClient.IsFormatSupported(");
        AssertContains(wasapiSource, "WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, selectedFormat)");
        AssertContains(wasapiSource, "\"IAudioClient.Initialize(capture)\"");
        AssertContains(wasapiSource, "audioClient.SetEventHandle(captureEvent.SafeWaitHandle.DangerousGetHandle())");
        AssertContains(wasapiSource, "audioClient.GetService(ref iidCaptureClient, out var captureClientObject)");
        AssertContains(wasapiSource, "_fastPathCopy = _captureFormat.SampleRate == OutputSampleRate");
        AssertContains(wasapiSource, "_resampleRemainderNumerator = 0;");
        AssertContains(wasapiSource, "Interlocked.Exchange(ref _initialized, 1);");
        AssertContains(wasapiSource, "WasapiComInterop.CoTaskMemFree(desiredFormat);");
        AssertContains(wasapiSource, "WasapiComInterop.ReleaseComObject(ref audioCaptureClient);");
        AssertContains(wasapiSource, "public void Start()");
        AssertContains(wasapiSource, "public Task StopAsync()");
        AssertContains(wasapiSource, "public async ValueTask DisposeAsync()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Initialization.cs")),
            "WASAPI capture initialization stays folded into capture lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioPlayback_InitializationLivesWithLifecycleRoot()
    {
        var playbackSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackSource, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(playbackSource, "public Task InitializeAsync(CancellationToken ct)");
        AssertContains(playbackSource, "enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device)");
        AssertContains(playbackSource, "WasapiComInterop.AllocFloatStereo48kFormat()");
        AssertContains(playbackSource, "audioClient.IsFormatSupported(");
        AssertContains(playbackSource, "WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, desiredFormat)");
        AssertContains(playbackSource, "\"IAudioClient.Initialize(render)\"");
        AssertContains(playbackSource, "audioClient.GetBufferSize(out _bufferFrameCount)");
        AssertContains(playbackSource, "audioClient.GetStreamLatency(out var streamLatencyHundredNs)");
        AssertContains(playbackSource, "audioClient.SetEventHandle(renderEvent.SafeWaitHandle.DangerousGetHandle())");
        AssertContains(playbackSource, "audioClient.GetService(ref iidRenderClient, out var renderClientObject)");
        AssertContains(playbackSource, "Interlocked.Exchange(ref _renderCallbackCount, 0)");
        AssertContains(playbackSource, "Volatile.Write(ref _playbackQueueDepth, 0)");
        AssertContains(playbackSource, "Interlocked.Exchange(ref _initialized, 1)");
        AssertContains(playbackSource, "WasapiComInterop.CoTaskMemFree(desiredFormat)");
        AssertContains(playbackSource, "WasapiComInterop.ReleaseComObject(ref audioRenderClient)");
        AssertContains(playbackSource, "internal void EnqueuePooledSamples(byte[] pooledBuffer, int validLength, long ptsTicks = 0)");
        AssertContains(playbackSource, "private bool TryWriteChunk(PlaybackChunk chunk)");
        AssertContains(playbackSource, "private bool TryDequeueChunk(out PlaybackChunk chunk)");
        AssertContains(playbackSource, "private readonly record struct PlaybackChunk");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Initialization.cs")),
            "WASAPI playback initialization stays folded into playback lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Queue.cs")),
            "WASAPI playback queue state stays folded into playback lifecycle root");
        AssertContains(playbackSource, "public void Start()");
        AssertContains(playbackSource, "public void PauseRendering()");
        AssertContains(playbackSource, "public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)");
        AssertContains(playbackSource, "public void Flush()");
        AssertContains(playbackSource, "public void Stop()");
        AssertContains(playbackSource, "public void Dispose()");
        AssertContains(playbackSource, "private void RenderThreadMain()");
        AssertContains(playbackSource, "private unsafe void RenderAvailableFrames()");
        AssertContains(playbackSource, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackSource, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.RenderThread.cs")),
            "WASAPI playback render thread stays folded into playback lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_DiagnosticsLivesWithLifecycleRoot()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "TrackCaptureCallback(Environment.TickCount64);");
        AssertContains(wasapiSource, "TrackCapturePacketFlags(flags);");
        AssertContains(wasapiSource, "public long AudioFramesArrived => Interlocked.Read(ref _audioFramesArrived);");
        AssertContains(wasapiSource, "public (double AvgIntervalMs, double MaxIntervalMs) GetCaptureCallbackIntervalSnapshot()");
        AssertContains(wasapiSource, "private void RaiseAudioLevelIfDue(ReadOnlySpan<byte> f32leBytes)");
        AssertContains(wasapiSource, "private void TrackCaptureCallback(long callbackTickMs)");
        AssertContains(wasapiSource, "private CallbackIntervalMetrics GetCaptureCallbackIntervalMetrics()");
        AssertContains(wasapiSource, "private void TrackCapturePacketFlags(uint flags)");
        AssertContains(wasapiSource, "private readonly record struct CallbackIntervalMetrics");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Diagnostics.cs")),
            "WASAPI capture diagnostics folded into capture lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiComInterop_ContractsLiveWithInteropOwner()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootSource, "internal static class WasapiComInterop");
        AssertContains(rootSource, "internal static void ThrowIfFailed(int hr, string operation)");
        AssertContains(rootSource, "internal static void ReleaseComObject<T>(ref T? comObject)");
        AssertContains(rootSource, "internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)");
        AssertContains(rootSource, "private static WasapiSampleType ResolveSampleType(");
        AssertContains(rootSource, "internal static IMMDeviceEnumerator CreateDeviceEnumerator()");
        AssertContains(rootSource, "internal static IAudioClient ActivateAudioClient(IMMDevice device, out IAudioClient3? audioClient3)");
        AssertContains(rootSource, "internal static bool TryInitializeSharedStreamWithAudioClient3(");
        AssertContains(rootSource, "internal static float GetEndpointVolume(string deviceId)");
        AssertContains(rootSource, "internal static void SetEndpointVolume(string deviceId, float level)");
        AssertContains(rootSource, "internal enum EDataFlow");
        AssertContains(rootSource, "internal enum WasapiSampleType");
        AssertContains(rootSource, "internal readonly record struct WasapiAudioFormat(");
        AssertContains(rootSource, "internal struct WAVEFORMATEX");
        AssertContains(rootSource, "internal struct WAVEFORMATEXTENSIBLE");
        AssertContains(rootSource, "internal struct PropVariant : IDisposable");
        AssertContains(rootSource, "internal interface IMMDeviceEnumerator");
        AssertContains(rootSource, "internal interface IMMDevice");
        AssertContains(rootSource, "internal interface IMMDeviceCollection");
        AssertContains(rootSource, "internal interface IPropertyStore");
        AssertContains(rootSource, "internal interface IMMNotificationClient");
        AssertContains(rootSource, "internal interface IAudioClient");
        AssertContains(rootSource, "internal interface IAudioClient3 : IAudioClient");
        AssertContains(rootSource, "internal interface IAudioCaptureClient");
        AssertContains(rootSource, "internal interface IAudioRenderClient");
        AssertContains(rootSource, "internal interface IAudioEndpointVolume");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.CoreAudio.Contracts.cs")),
            "Core Audio contract shard folded into the single WASAPI interop owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.AudioClient.Contracts.cs")),
            "AudioClient contract shard folded into the single WASAPI interop owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.Formats.cs")),
            "WASAPI format helpers stay with the implementation root instead of a tiny partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.DeviceClients.cs")),
            "WASAPI device helpers stay with the implementation root instead of a tiny partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.Contracts.cs")),
            "old combined WASAPI COM contract file removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.CommonContracts.cs")),
            "shared WASAPI contracts stay with Core Audio contracts instead of a tiny file");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_StopUsesBoundedThreadJoin()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "private static readonly TimeSpan CaptureThreadJoinTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(wasapiSource, "JoinCaptureThread(_captureThread, \"WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT_START_FAILURE\")");
        AssertContains(wasapiSource, "JoinCaptureThread(thread, \"WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT_STOP\")");
        AssertContains(wasapiSource, "thread.Join(CaptureThreadJoinTimeout)");
        AssertDoesNotContain(wasapiSource, "_captureThread.Join();");
        AssertDoesNotContain(wasapiSource, "thread.Join();");
        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackBackendOwnershipUsesResourceAggregate()
    {
        var captureSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
        var backendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(backendSource, "internal sealed class FlashbackBackendResources");
        AssertContains(backendSource, "public FlashbackBufferManager? BufferManager { get; set; }");
        AssertContains(backendSource, "public FlashbackEncoderSink? Sink { get; set; }");
        AssertContains(backendSource, "public FlashbackExporter? Exporter { get; set; }");
        AssertContains(backendSource, "public FlashbackPlaybackController? PlaybackController { get; set; }");
        AssertContains(backendSource, "public CaptureSettings? SettingsSnapshot { get; set; }");
        AssertContains(backendSource, "public bool HasAnyResource");
        AssertContains(backendSource, "public bool PreserveSegmentsAfterFailedRecordingFinalize { get; private set; }");
        AssertContains(backendSource, "public void Install(");
        AssertContains(backendSource, "public void ClearRecoveryPreserve()");
        AssertContains(backendSource, "public bool ResolveSegmentPurge(bool requested, string reason)");
        AssertContains(backendSource, "public void PreserveRecoverySegments(string reason)");
        AssertContains(backendSource, "internal readonly record struct FlashbackPreviewBackendStartRequest(");
        AssertContains(backendSource, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(backendSource, "var bufferManager = new FlashbackBufferManager(");
        AssertContains(backendSource, "flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback);");
        AssertContains(backendSource, "flashbackSink.FrameEncoded += request.FrameEncodedHandler;");
        AssertContains(backendSource, "Install(");
        AssertContains(backendSource, "AttachProducers(");
        AssertContains(backendSource, "playbackController.Initialize(");
        AssertContains(backendSource, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(backendSource, "request.ScheduleDeferredCleanup(");
        AssertContains(backendSource, "internal readonly record struct FlashbackBackendArtifactCleanupRequest(");
        AssertContains(backendSource, "public void ScheduleDeferredArtifactCleanup(");
        AssertContains(backendSource, "public async Task<bool> CleanupArtifactsAfterExportAsync(");
        AssertContains(backendSource, "Func<Task<bool>> acquireExportOperationLockAsync,");
        AssertContains(backendSource, "Action<string> releaseExportOperationLock,");
        AssertContains(backendSource, "public async Task<FinalizeResult> FinalizeRecordingAsync(");
        AssertContains(backendSource, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertContains(backendSource, "public FlashbackPlaybackController? TakePlaybackController()");
        AssertContains(backendSource, "internal readonly record struct FlashbackProducerAttachRequest(");
        AssertContains(backendSource, "public void AttachProducers(FlashbackProducerAttachRequest request)");
        AssertContains(backendSource, "request.VideoCapture.SetFlashbackSink(flashbackSink);");
        AssertContains(backendSource, "private static void AttachAudioProducer(");
        AssertContains(backendSource, "FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
        AssertContains(backendSource, "private static void AttachMicrophoneProducer(");
        AssertContains(backendSource, "FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
        AssertContains(backendSource, "internal readonly record struct FlashbackProducerDetachRequest(");
        AssertContains(backendSource, "UnifiedVideoCapture? VideoCapture,");
        AssertContains(backendSource, "WasapiAudioCapture? AudioCapture,");
        AssertContains(backendSource, "WasapiAudioCapture? MicrophoneCapture,");
        AssertContains(backendSource, "string WarningToken,");
        AssertContains(backendSource, "bool DetachMicrophoneWriter);");
        AssertContains(backendSource, "public void DetachProducers(FlashbackProducerDetachRequest request)");
        AssertContains(backendSource, "internal readonly record struct FlashbackBufferCycleRequest(");
        AssertContains(backendSource, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertContains(backendSource, "newSink.SetFatalErrorCallback(request.FatalErrorCallback);");
        AssertContains(backendSource, "newSink.FrameEncoded += request.FrameEncodedHandler;");
        AssertContains(backendSource, "SettingsSnapshot = request.SettingsSnapshot;");
        AssertContains(backendSource, "public void ClearSinkAndSettings()");
        AssertContains(backendSource, "public void Clear()");

        AssertContains(captureSource, "private readonly FlashbackBackendResources _flashbackBackend = new();");
        AssertDoesNotContain(captureSource, "_flashbackBufferManager");
        AssertDoesNotContain(captureSource, "_flashbackSink");
        AssertDoesNotContain(captureSource, "_flashbackExporter");
        AssertDoesNotContain(captureSource, "_flashbackPlaybackController");
        AssertDoesNotContain(captureSource, "_flashbackBackendSettings");
        AssertContains(captureSource, "_flashbackBackend.HasAnyResource");
        AssertContains(captureSource, "_flashbackBackend.StartPreviewBackendAsync(");
        AssertContains(backendSource, "Install(");
        AssertDoesNotContain(captureSource, "_flashbackBackend.Install(");
        AssertContains(captureSource, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(backendSource, "TakePlaybackController()");
        AssertContains(backendSource, "AttachProducers(");
        AssertContains(backendSource, "new FlashbackProducerAttachRequest(");
        AssertContains(backendSource, "DetachProducers(");
        AssertContains(captureSource, "_flashbackBackend.ResolveSegmentPurge(");
        AssertContains(captureSource, "_flashbackBackend.PreserveRecoverySegments(");
        AssertContains(backendSource, "ClearRecoveryPreserve();");
        AssertContains(captureSource, "_flashbackBackend.FinalizeRecordingAsync(");
        AssertContains(backendSource, "ClearSinkAndSettings();");
        AssertContains(captureSource, "_flashbackBackend.DisposePreviewBackendAsync(request)");
        AssertContains(backendSource, "Clear();");
        AssertDoesNotContain(captureSource, "var bufferManager = new FlashbackBufferManager(");
        AssertDoesNotContain(captureSource, "FlashbackPlaybackController? playbackController = null;");

        return Task.CompletedTask;
    }

    internal static Task UnifiedVideoCapture_CpuMjpegEmitReportsNv12()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var observed = string.Empty;

        var setObserver = unifiedVideoCapture.GetType().GetMethod("SetPixelFormatDetectedCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SetPixelFormatDetectedCallback method not found.");
        setObserver.Invoke(unifiedVideoCapture, new object?[] { new Action<string>(value => observed = value) });

        var emitMethod = unifiedVideoCapture.GetType().GetMethod("OnMjpegPipelineFrameEmitted", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnMjpegPipelineFrameEmitted method not found.");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var rentMethod = frameType.GetMethod("Rent", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent method not found.");
        var frame = rentMethod.Invoke(
            null,
            new object[]
            {
                0L,
                0L,
                0L,
                2,
                2,
                Enum.Parse(formatType, "Nv12"),
                6
            })
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent returned null.");
        try
        {
            emitMethod.Invoke(unifiedVideoCapture, new[] { frame });
        }
        finally
        {
            ((IDisposable)frame).Dispose();
        }

        AssertEqual("NV12", observed, "UnifiedVideoCapture.OnMjpegPipelineFrameEmitted observer format");
        return Task.CompletedTask;
    }

    internal static async Task UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var pipeline = CreateUninitializedObject(pipelineType);
        SeedPipelineStopFailureState(pipeline, pipelineType);

        SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", pipeline);
        SetPrivateField(pipeline, "_emitThread", Thread.CurrentThread);

        try
        {
            var stopAsync = unifiedVideoCapture.GetType().GetMethod("StopAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("UnifiedVideoCapture.StopAsync method not found.");
            if (stopAsync.Invoke(unifiedVideoCapture, null) is not Task stopTask)
            {
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync did not return a Task.");
            }

            try
            {
                await stopTask.ConfigureAwait(false);
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync unexpectedly succeeded.");
            }
            catch (InvalidOperationException ex)
            {
                AssertContains(ex.Message, "emitter_self_join");
            }

            var retainedPipeline = GetPrivateField(unifiedVideoCapture, "_mjpegPipeline");
            AssertEqual(pipeline, retainedPipeline, "UnifiedVideoCapture._mjpegPipeline retained on stop failure");
        }
        finally
        {
            SetPrivateField(pipeline, "_emitThread", null);
            SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", null);

            var disposeMethod = pipelineType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ParallelMjpegDecodePipeline.Dispose method not found.");
            disposeMethod.Invoke(pipeline, null);

            await DisposeValueTaskAsync(unifiedVideoCapture).ConfigureAwait(false);
        }
    }
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.Flashback.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.Flashback.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
    };

    private static readonly string[] CaptureServicePreviewLifecycleFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.cs"
    };

    private static readonly string[] CaptureServiceRecordingIntegrityFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
    };

    private static string ReadCaptureServiceFlashbackOrchestrationSource()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingFinalizationSource()
        => string.Join(
            "\n",
            CaptureServiceRecordingFinalizationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServicePreviewLifecycleSource()
        => string.Join(
            "\n",
            CaptureServicePreviewLifecycleFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingIntegritySource()
        => string.Join(
            "\n",
            CaptureServiceRecordingIntegrityFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    private static string ReadCaptureServicePreviewLifecycleCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServicePreviewLifecycleFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    internal static Task CaptureService_FlashbackOrchestrationLivesInFocusedPartials()
    {
        var flashbackStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var previewBackendText = flashbackStateText;
        var settingsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");

        AssertContains(flashbackStateText, "public bool IsFlashbackActive => _flashbackBackend.Sink != null;");
        AssertContains(flashbackStateText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackStateText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackStateText, "FLASHBACK_ENABLE_DEFERRED");
        AssertContains(flashbackStateText, "public Task RestartFlashbackAsync(");
        AssertContains(flashbackStateText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(flashbackStateText, "UpdateEncodingSettings(settings);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackControls.cs")),
            "Flashback controls owner folded into CaptureService.Flashback.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackRecording.cs")),
            "Flashback recording owner folded into CaptureService.Flashback.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackAudioInputs.cs")),
            "Flashback audio input restoration folded into Flashback recording owner");
        AssertContains(flashbackRecordingText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackPreviewBackend.cs")),
            "Flashback preview backend lifecycle folded into Flashback controls owner");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(previewBackendText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(previewBackendText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(previewBackendText, "CreateFlashbackPreviewBackendDisposalRequest(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackPreviewBackendDisposal.cs")),
            "old Flashback preview backend disposal partial removed");
        AssertContains(backendResourcesText, "internal readonly record struct FlashbackPreviewBackendDisposalRequest(");
        AssertContains(backendResourcesText, "public async Task DisposePreviewBackendAsync(");
        AssertContains(settingsText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(settingsText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(settingsText, "public Task UpdateFlashbackSettingsAsync(");
        AssertContains(settingsText, "_currentSettings.FlashbackBufferMinutes = bufferMinutes;");
        AssertContains(settingsText, "_flashbackBackend.PlaybackController.GpuDecodeEnabled = gpuDecode;");
        AssertContains(settingsText, "public Task UpdateRecordingFormatAsync(");
        AssertContains(settingsText, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(settingsText, "FLASHBACK_FORMAT_CHANGE_ROLLBACK");
        AssertContains(settingsText, "private void UpdateEncodingSettings(CaptureSettings source)");
        AssertContains(settingsText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(settingsText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK");
        AssertContains(agentMapText, "CaptureService.Flashback.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackEnable.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackRestart.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackState.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackSettings.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(cleanupPlanText, "CaptureService.Flashback.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackEnable.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackRestart.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackState.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackSettings.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(agentMapText, "FlashbackBackendResources.cs");
        AssertContains(cleanupPlanText, "FlashbackBackendResources.cs");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.BufferCycle.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.BufferCycle.cs");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.Startup.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.Startup.cs");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.Teardown.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.Teardown.cs");
        AssertContains(agentMapText, "rollback cleanup");
        AssertContains(cleanupPlanText, "startup failure rollback cleanup");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.RecordingFinalize.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.RecordingFinalize.cs");
        AssertContains(agentMapText, "attach/detach request");
        AssertContains(cleanupPlanText, "attach/detach request");
        AssertContains(backendResourcesText, "private FlashbackBufferCyclePlaybackState DisposePlaybackForBufferCycle(");
        AssertContains(backendResourcesText, "private static async Task StopAndDisposeOldSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "private async Task<bool> TryStartReplacementSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "private static async Task CleanupFailedReplacementSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(backendResourcesText, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(backendResourcesText, "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN");
        AssertContains(backendResourcesText, "preview_init_rollback");
        AssertContains(backendResourcesText, "public async Task<FinalizeResult> FinalizeRecordingAsync(");
        AssertContains(backendResourcesText, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBackendResources.RecordingFinalize.cs")),
            "recording finalize policy folded into FlashbackBackendResources.cs");
        AssertContains(backendResourcesText, "public void AttachProducers(FlashbackProducerAttachRequest request)");
        AssertContains(backendResourcesText, "public void DetachProducers(FlashbackProducerDetachRequest request)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackState.cs")),
            "Flashback state owner folded into CaptureService.Flashback.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackSettings.cs")),
            "Flashback settings owner folded into CaptureService.Flashback.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackSettingsControls.cs")),
            "old broad Flashback settings controls file removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingFinalizationLivesInFocusedPartials()
    {
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        var flashbackBackendFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var libAvBackendFinalizationText =
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        var recordingLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");

        AssertContains(stopLifecycleText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertContains(stopLifecycleText, "StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken)");
        AssertContains(stopLifecycleText, "StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync(");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertContains(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(flashbackBackendFinalizationText, "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(");
        AssertContains(flashbackBackendFinalizationText, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(flashbackBackendFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(flashbackBackendFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertContains(flashbackBackendFinalizationText, "if (recordingBoundary.Captured)");
        AssertContains(flashbackBackendFinalizationText, "flashbackVideoCapture.EndFlashbackRecordingAccounting();");
        AssertContains(flashbackBackendFinalizationText, "recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(flashbackBackendFinalizationText, "recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(");
        AssertContains(flashbackBackendFinalizationText, "recordingBoundary.Captured = true;");
        AssertContains(flashbackBackendFinalizationText, "_flashbackBackend.PreserveRecoverySegments(\"recording_finalize_failed\");");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
        AssertContains(flashbackBackendFinalizationText, "await CycleFlashbackBufferAsync(cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackBackendFinalizationText, "BeginFlashbackBackendCleanup(ex);");
        AssertOccursBefore(flashbackBackendFinalizationText, "LogRecordingIntegritySummary(_lastRecordingIntegrity);", "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertOccursBefore(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(", "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs")),
            "old Flashback backend reconcile partial removed");
        AssertContains(libAvBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(");
        AssertContains(libAvBackendFinalizationText, "StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(libAvBackendFinalizationText, "DetachLibAvRecordingAudioBeforeSinkStopAsync(");
        AssertContains(libAvBackendFinalizationText, "StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(libAvBackendFinalizationText, "DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "FoldLibAvAudioFaultIntoFinalizeResult(");
        AssertContains(libAvBackendFinalizationText, "PublishLibAvRecordingIntegrity(");
        AssertContains(libAvBackendFinalizationText, "CompleteLibAvRecordingFinalizeStateAsync(");
        AssertContains(libAvBackendFinalizationText, "var sinkResult = libAvSink != null");
        AssertContains(libAvBackendFinalizationText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(libAvBackendFinalizationText, "reason: \"recording_stop_deferred_drain\"");
        AssertContains(libAvBackendFinalizationText, "_previewAudioGraph.DetachCapture(");
        AssertContains(libAvBackendFinalizationText, "Recording WASAPI capture dispose failed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvResources.cs")),
            "old broad LibAv resource finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvVideoBoundary.cs")),
            "old LibAv video-boundary finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvSink.cs")),
            "old LibAv sink finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvIdlePreview.cs")),
            "old LibAv idle-preview finalization partial removed");
        AssertContains(libAvBackendFinalizationText, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertContains(libAvBackendFinalizationText, "private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvBackendFinalizationText, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken)");
        AssertContains(libAvBackendFinalizationText, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        AssertContains(libAvBackendFinalizationText, "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(libAvBackendFinalizationText, "OnlyWhenMissing: false,");
        AssertContains(libAvBackendFinalizationText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(libAvBackendFinalizationText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvPreviewRestore.cs")),
            "old LibAv preview-restore finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeFlashback.cs")),
            "Flashback export-finalize helpers folded into CaptureService.Flashback.cs");
        AssertContains(recordingLifecycleText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(recordingLifecycleText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingOutcomeState.cs")),
            "old recording outcome-state partial removed");
        AssertDoesNotContain(stopLifecycleText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertDoesNotContain(stopLifecycleText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertDoesNotContain(stopLifecycleText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");

        return Task.CompletedTask;
    }

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
        var flashbackFinalizeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
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
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs"),
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
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs").Replace("\r\n", "\n"),
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
        AssertContains(lifecycleText, "internal void MarkRecordingFinalizationUnresolved(string statusMessage)");
        AssertContains(lifecycleText, "reason=existing_finalization_status");
        AssertContains(lifecycleText, "RecordingFinalizationRecoveryArtifacts.PreserveUnresolved(");
        AssertContains(lifecycleText, "FinalizeResult.Failure(fallbackOutputPath, statusMessage, preservedArtifacts)");

        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
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


// LibAv encoder contract implementations live with the recording xUnit wrappers.
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

    internal static Task FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "var activeEndPts = TimeSpan.FromTicks(Math.Max(activeStartPts.Ticks, Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var activeSizeBytes = Math.Max(0, _totalDiskBytes - _completedSegmentBytes);");
        AssertContains(source, "EndPtsMs = (long)activeEndPts.TotalMilliseconds,");
        AssertContains(source, "SizeBytes = activeSizeBytes,");
        AssertContains(source, "var safeActiveSegmentBytes = Math.Max(0, activeSegmentBytes);");
        AssertContains(source, "var accountedActiveSegmentBytes = safeActiveSegmentBytes;");
        AssertContains(source, "accountedActiveSegmentBytes = SubtractNonNegative(safeActiveSegmentBytes, _completedSegments[^1].SizeBytes);");
        AssertContains(source, "_totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, accountedActiveSegmentBytes);");
        AssertContains(source, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
        AssertContains(source, "private long GetCompletedSegmentBytesSaturated()");
        AssertContains(source, "_totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, retainedActiveBytes);");
        AssertContains(source, "freedBytes = AddNonNegativeSaturated(freedBytes, _completedSegments[i].SizeBytes);");
        AssertContains(source, "FLASHBACK_BUFFER_DELETE_WARN path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration()
    {
        var source = ReadFlashbackBufferManagerSource();
        var cleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");
        var budgetSource = cleanupSource;

        AssertContains(source, "var maxTicks = Math.Max(0, _options.BufferDuration.Ticks);");
        AssertContains(source, "var duration = NonNegativeDeltaTicks(ptsTicks, startTicks);");
        AssertContains(source, "var newStartTicks = Math.Max(0, ptsTicks - maxTicks);");
        AssertContains(source, "Interlocked.CompareExchange(ref _validStartPtsTicks, newStartTicks, startTicks);");
        AssertContains(source, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(source, "private static long SubtractNonNegative(long left, long right)");
        AssertContains(source, "private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)");
        AssertContains(source, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(source, "var totalDuration = NonNegativeDeltaTicks(latestTicks, startTicks);");
        AssertContains(source, "var evictTicks = ToNonNegativeLongSaturated(excessBytes / bytesPerTick);");
        AssertContains(source, "var newStart = AddNonNegativeSaturated(Math.Max(0, startTicks), evictTicks);");
        AssertContains(cleanupSource, "directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);");
        AssertContains(budgetSource, "directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);");
        AssertContains(budgetSource, "totalCacheBytes = AddNonNegativeSaturated(totalCacheBytes, directoryBytes);");
        AssertContains(budgetSource, "totalCacheBytes = SubtractNonNegative(totalCacheBytes, candidate.SizeBytes);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        updateDiskBytes.Invoke(manager, new object[] { 1000L });
        var completedPath = Path.Combine(tempDir, "completed-0.ts");
        File.WriteAllBytes(completedPath, new byte[] { 0x47 });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            completedPath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });
        AssertEqual(1200L, GetLongProperty(manager, "TotalBytesWritten"), "Final segment bytes counted at rotation");

        updateDiskBytes.Invoke(manager, new object[] { 100L });
        AssertEqual(1300L, GetLongProperty(manager, "TotalBytesWritten"), "First bytes from next segment counted after rotation");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SamePathCompletionExtendsLatestSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var getValidSegmentPaths = manager.GetType().GetMethod("GetValidSegmentPaths")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetValidSegmentPaths not found.");
            var getSegmentInfoList = manager.GetType().GetMethod("GetSegmentInfoList")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetSegmentInfoList not found.");

            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[] { 0x47 });

            updateDiskBytes.Invoke(manager, new object[] { 1000L });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                activePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                1000L
            });
            AssertEqual(1000L, GetLongProperty(manager, "TotalDiskBytes"), "Initial same-path completion tracks one physical active file");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Initial same-path completion does not double count active bytes");

            updateDiskBytes.Invoke(manager, new object[] { 1500L });
            AssertEqual(1500L, GetLongProperty(manager, "TotalDiskBytes"), "Same active file growth is counted as a delta after completion");
            AssertEqual(1500L, GetLongProperty(manager, "TotalBytesWritten"), "Same active file growth advances monotonic bytes by delta");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", Path.GetFileName(activePath)),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                2000L
            });

            var paths = ((IEnumerable<string>)getValidSegmentPaths.Invoke(manager, new object[]
            {
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(19)
            })!).ToArray();
            AssertEqual(1, paths.Length, "Extended same-path segment remains exportable for tail range");
            AssertEqual(activePath, paths[0], "Extended same-path segment export path");
            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Same-path extension keeps original segment sequence");
            AssertEqual(2000L, GetLongProperty(manager, "TotalDiskBytes"), "Extended same-path completion updates completed disk bytes");
            AssertEqual(2000L, GetLongProperty(manager, "TotalBytesWritten"), "Extended same-path completion advances monotonic bytes by growth delta");

            var infos = ((System.Collections.IEnumerable)getSegmentInfoList.Invoke(manager, Array.Empty<object>())!)
                .Cast<object>()
                .ToArray();
            var completedInfo = infos.First(info => GetPropertyValue(info, "IsActive") is false);
            AssertEqual(0L, (long)GetPropertyValue(completedInfo, "StartPtsMs")!, "Extended segment keeps original start");
            AssertEqual(20_000L, (long)GetPropertyValue(completedInfo, "EndPtsMs")!, "Extended segment updates end");
            AssertEqual(2000L, (long)GetPropertyValue(completedInfo, "SizeBytes")!, "Extended segment updates size");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "if (string.IsNullOrWhiteSpace(path))\n        {\n            Logger.Log(\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=empty_path\");\n            return;\n        }");
        AssertContains(source, "if (endPts <= startPts)\n        {\n            Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=invalid_range path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds}\");\n            return;\n        }");
        AssertContains(source, "if (!IsPathInSessionDirectory(path))\n            {\n                Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=outside_session path='{Path.GetFileName(path)}'\");\n                return;\n            }");
        AssertContains(source, "if (!File.Exists(path))\n            {\n                Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=missing_file path='{Path.GetFileName(path)}'\");\n                return;\n            }");
        AssertContains(source, "var existingIndex = _completedSegments.FindIndex(seg => IsSameSegmentPath(seg.Path, path));");
        AssertContains(source, "if (existingIndex >= 0)\n            {\n                if (!TryExtendCompletedSegment(existingIndex, path, startPts, endPts, safeSizeBytes, pathIsActiveSegment))");
        AssertContains(source, "private bool TryExtendCompletedSegment(");
        AssertContains(source, "if (!pathIsActiveSegment && !existing.AllowSamePathExtension)");
        AssertContains(source, "AllowSamePathExtension = pathIsActiveSegment");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertContains(source, "if (_completedSegments.Count > 0 && startPts < _completedSegments[^1].EndPts)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=non_monotonic");
        AssertContains(source, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_PATH_WARN");
        AssertContains(source, "var safeSizeBytes = Math.Max(0, sizeBytes);");
        AssertContains(source, "private int _completedSegmentSequence;");
        AssertContains(source, "var sequenceNumber = _completedSegmentSequence++;");
        AssertContains(source, "_completedSegments.Add(new CompletedSegment(path, sequenceNumber, startPts, endPts, safeSizeBytes)\n            {\n                AllowSamePathExtension = pathIsActiveSegment\n            });");
        AssertContains(source, "_completedSegmentBytes = AddNonNegativeSaturated(_completedSegmentBytes, safeSizeBytes);");
        AssertContains(source, "_previousActiveSegmentBytes = pathIsActiveSegment ? safeSizeBytes : 0;");
        AssertContains(source, "_completedSegmentSequence = 0;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var missingSegmentPath = Path.Combine(tempDir, "segment-missing.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                missingSegmentPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });

            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Missing segment should not allocate sequence");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Missing segment should not update bytes");

            var segment0Path = Path.Combine(tempDir, "segment-0.ts");
            File.WriteAllBytes(segment0Path, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                segment0Path,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });
            var overlappingSegmentPath = Path.Combine(tempDir, "segment-overlap.ts");
            File.WriteAllBytes(overlappingSegmentPath, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(6),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Duplicate segment path should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Duplicate segment path should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                1500L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Non-active duplicate segment growth should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Non-active duplicate segment growth should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                overlappingSegmentPath,
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(7),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Overlapping segment should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Overlapping segment should not update bytes");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbtest_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                outsidePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                1200L
            });

            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Outside segment path should not update bytes");
            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Outside segment path should not allocate sequence");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            try { Directory.Delete(outsideDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbdelete_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var tryDeleteFile = manager.GetType().GetMethod("TryDeleteFile", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.TryDeleteFile not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            File.WriteAllText(outsidePath, "keep");

            var result = (bool)tryDeleteFile.Invoke(manager, new object[] { outsidePath })!;
            AssertEqual(false, result, "Outside delete should be rejected");
            AssertEqual(true, File.Exists(outsidePath), "Outside delete should preserve file");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session");
            AssertOccursBefore(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session", "File.Delete(filePath);");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            try { Directory.Delete(outsideDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_IgnoresUpdatesAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var updateLatestPts = manager.GetType().GetMethod("UpdateLatestPts")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateLatestPts not found.");
        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        ((IDisposable)manager).Dispose();

        updateLatestPts.Invoke(manager, new object[] { TimeSpan.FromSeconds(5) });
        updateDiskBytes.Invoke(manager, new object[] { 4096L });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            Path.Combine(tempDir, "completed-after-dispose.ts"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });

        AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "LatestPts")!, "Disposed manager ignores latest PTS updates");
        AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Disposed manager ignores disk and segment byte updates");
        AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Disposed manager does not allocate segment sequence");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private volatile bool _disposed;");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=disposed");
        AssertContains(source, "public void UpdateLatestPts(TimeSpan pts)\n    {\n        if (_disposed)\n        {\n            return;\n        }");
        AssertContains(source, "public void UpdateDiskBytes(long activeSegmentBytes)\n    {\n        if (_disposed)\n        {\n            return;\n        }");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");
        var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
            ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");
        var finalizeCycle = manager.GetType().GetMethod("FinalizeActiveSegmentForCycle")
            ?? throw new InvalidOperationException("FlashbackBufferManager.FinalizeActiveSegmentForCycle not found.");

        ((IDisposable)manager).Dispose();

        purgeCompleted.Invoke(manager, null);
        purgeAll.Invoke(manager, null);
        abandonGenerated.Invoke(manager, new object?[] { activePath, null });
        finalizeCycle.Invoke(manager, null);

        AssertEqual(false, File.Exists(completedPath), "Dispose purges completed segment before post-dispose purge attempts");
        AssertEqual(false, File.Exists(activePath), "Dispose purges active segment before post-dispose purge attempts");
        AssertEqual(0, GetIntProperty(manager, "SegmentCount"), "Disposed destructive operations keep the disposed empty index stable");
        AssertEqual(string.Empty, GetStringProperty(manager, "ActiveFilePath"), "Disposed destructive operations keep active path cleared");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_PURGE_SKIP reason=disposed");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=disposed");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PreservesMarkedRecoverySessions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_recovery_preserve_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var markPreserved = manager.GetType().GetMethod("MarkSessionPreservedForRecovery")
            ?? throw new InvalidOperationException("FlashbackBufferManager.MarkSessionPreservedForRecovery not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

        markPreserved.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "IsSessionPreservedForRecovery"), "Recovery-preserved manager exposes preserved state");
        SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(2).Ticks);
        InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives normal eviction");

        purgeAll.Invoke(manager, null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives explicit purge");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives explicit purge");

        ((IDisposable)manager).Dispose();

        AssertEqual(true, Directory.Exists(tempDir), "Recovery-preserved session directory survives dispose");
        AssertEqual(true, File.Exists(Path.Combine(tempDir, ".flashback-recovery-preserve")), "Recovery marker survives dispose");
        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives dispose");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives dispose");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private bool _preserveSessionForRecovery;");
        AssertContains(source, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_EVICT_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentMutationLivesWithRootState()
    {
        var managerText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");

        AssertContains(managerText, "public string AcquireSegmentPath(out bool generated)");
        AssertContains(managerText, "public string GenerateSegmentPath()");
        AssertContains(managerText, "public void MarkActiveSegmentStart(string path, TimeSpan startPts)");
        AssertContains(managerText, "public void AbandonGeneratedSegmentPath(string generatedPath, string? restoreActivePath)");
        AssertContains(managerText, "public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)");
        AssertContains(managerText, "private bool TryExtendCompletedSegment(");
        AssertContains(managerText, "FLASHBACK_BUFFER_SEGMENT_COMPLETE");
        AssertContains(managerText, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertDoesNotContain(managerText, "partial class FlashbackBufferManager");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Segments.cs")),
            "FlashbackBufferManager.Segments.cs folded into FlashbackBufferManager.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_LiveAccountingLivesWithRootState()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public void ResetLatestPts()");
        AssertContains(rootText, "public void FinalizeActiveSegmentForCycle()");
        AssertContains(rootText, "public double EncodeFrameRate { get; set; }");
        AssertContains(rootText, "public void UpdateLatestPts(TimeSpan pts)");
        AssertContains(rootText, "public void UpdateDiskBytes(long activeSegmentBytes)");
        AssertContains(rootText, "FLASHBACK_BUFFER_DISK_EVICT");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.LiveAccounting.cs")),
            "FlashbackBufferManager.LiveAccounting.cs folded into FlashbackBufferManager.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_MathHelpersLiveWithRootState()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var mathText = rootText;

        AssertContains(mathText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(mathText, "private static long SubtractNonNegative(long left, long right)");
        AssertContains(mathText, "private long GetCompletedSegmentBytesSaturated()");
        AssertContains(mathText, "private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)");
        AssertContains(mathText, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertContains(mathText, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(mathText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Math.cs")),
            "FlashbackBufferManager.Math.cs folded into FlashbackBufferManager.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentQueriesLiveWithRootState()
    {
        var queryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var pathSafetyText = queryText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(queryText, "public int SegmentCount");
        AssertContains(queryText, "public string? ActiveFilePath");
        AssertContains(queryText, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)");
        AssertContains(queryText, "public string? GetValidSegmentFileForPosition(TimeSpan absolutePts)");
        AssertContains(queryText, "private string? GetOldestExistingSegmentPath()");
        AssertContains(queryText, "public string? GetNextSegmentFile(string currentPath)");
        AssertContains(queryText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(queryText, "public IReadOnlyList<string> GetValidSegmentPaths(TimeSpan inPoint, TimeSpan outPoint)");
        AssertContains(pathSafetyText, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(pathSafetyText, "FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator");
        AssertContains(pathSafetyText, "FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, sessionRoot)");
        AssertContains(pathSafetyText, "FLASHBACK_BUFFER_SEGMENT_PATH_WARN");

        AssertContains(queryText, "private TimeSpan GetActiveSegmentStartPts()");
        AssertContains(queryText, "private TimeSpan GetDefaultActiveSegmentStartPts()");
        AssertContains(queryText, "public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()");
        AssertDoesNotContain(queryText, "partial class FlashbackBufferManager");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Segments.cs")),
            "FlashbackBufferManager.Segments.cs folded into FlashbackBufferManager.cs");
        AssertContains(docsText, "FlashbackBufferManager.cs");
        AssertContains(docsText, "session-directory path safety");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_LifecycleHelpersLiveWithRootState()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = rootText;
        var recoveryPreserveText = rootText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public void SetSegmentExtension(string extension)");
        AssertContains(lifecycleText, "public void Initialize(string sessionId)");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "private void ThrowIfDisposed()");
        AssertContains(recoveryPreserveText, "public bool IsSessionPreservedForRecovery");
        AssertContains(recoveryPreserveText, "public void MarkSessionPreservedForRecovery()");
        AssertContains(recoveryPreserveText, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(recoveryPreserveText, "RecoveryPreserveMarkerFileName");
        AssertContains(recoveryPreserveText, "FLASHBACK_RECOVERY_PRESERVE_MARKER");
        AssertContains(recoveryPreserveText, "FLASHBACK_RECOVERY_PRESERVE_MARKER_CHECK_WARN");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Lifecycle.cs")),
            "FlashbackBufferManager.Lifecycle.cs folded into FlashbackBufferManager.cs");
        AssertContains(docsText, "FlashbackBufferManager.cs");
        AssertContains(docsText, "recovery-preserve state");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PurgeLivesWithLifecycleCleanup()
    {
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");
        var evictionText = lifecycleText;

        AssertContains(lifecycleText, "public void PurgeCompletedSegments()");
        AssertContains(lifecycleText, "public void PurgeAllSegments()");
        AssertContains(lifecycleText, "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()");
        AssertContains(lifecycleText, "private bool TryDeleteFile(string filePath)");
        AssertContains(lifecycleText, "FLASHBACK_PURGE_PARTIAL");
        AssertContains(lifecycleText, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(lifecycleText, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session");
        AssertContains(lifecycleText, "var (purgedSegments, purgedBytes) = PurgeAllSegmentsCore();");
        AssertContains(evictionText, "public void PauseEviction()");
        AssertContains(evictionText, "public (TimeSpan StartPts, TimeSpan EndPts) ResumeEviction()");
        AssertContains(evictionText, "public bool IsDiskWarningActive");
        AssertContains(evictionText, "public TimeSpan RecordingStartPts");
        AssertContains(evictionText, "public TimeSpan RecordingEndPts");
        AssertContains(evictionText, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(evictionText, "private void EvictOldestSegments()");
        AssertContains(evictionText, "private bool DeleteFileForEviction(string filePath, long sizeBytes, string reason)");
        AssertContains(evictionText, "private static bool DeleteEvictedFile(string fullPath, string sessionRoot, long sizeBytes, string reason)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Purge.cs")),
            "FlashbackBufferManager.Purge.cs folded into FlashbackBufferManager.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Lifecycle.cs")),
            "FlashbackBufferManager.Lifecycle.cs folded into FlashbackBufferManager.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Retention.cs")),
            "FlashbackBufferManager.Retention.cs folded into FlashbackBufferManager.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBufferManager.Segments.cs")),
            "FlashbackBufferManager.Segments.cs folded into FlashbackBufferManager.cs");

        return Task.CompletedTask;
    }
    internal static Task FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)\n        => GetValidSegmentFileForPosition(absolutePts);");

        // Add 3 segments: 0-5s, 5-10s, 10-15s
        var seg0 = Path.Combine(tempDir, "seg0.ts");
        var seg1 = Path.Combine(tempDir, "seg1.ts");
        var seg2 = Path.Combine(tempDir, "seg2.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(seg0, "segment");
        File.WriteAllText(seg1, "segment");
        File.WriteAllText(seg2, "segment");
        File.WriteAllText(active, "active");
        AddCompletedSegment(manager, seg0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 1000);
        AddCompletedSegment(manager, seg1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1000);
        AddCompletedSegment(manager, seg2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 1000);

        var method = manager.GetType().GetMethod("GetSegmentFileForPosition")!;

        // Position 7s → segment 1 (5-10s)
        var result1 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(7) }) as string;
        AssertEqual(seg1, result1!, "Position 7s");

        // Position 0s → segment 0 (0-5s)
        var result2 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(0) }) as string;
        AssertEqual(seg0, result2!, "Position 0s");

        // Position 20s → not in any completed segment → falls back to active
        var result3 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) }) as string;
        AssertContains(result3!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingOldest = Path.Combine(tempDir, "missing-oldest.ts");
        var existingFallback = Path.Combine(tempDir, "existing-fallback.ts");
        File.WriteAllText(existingFallback, "segment");

        AddCompletedSegment(manager, missingOldest, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingFallback, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentFileForPosition")!;

        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(existingFallback, fallback!, "Missing target should fall back to first existing completed segment");

        File.Delete(existingFallback);
        var missingAll = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(null, missingAll, "Missing completed and active segments should return null");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var oldest = Path.Combine(tempDir, "oldest.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(oldest, "oldest");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, oldest, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentFileForPosition")!;
        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(1) }) as string;

        AssertEqual(oldest, fallback!, "Position before first segment should use oldest existing segment, not active");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetNextSegmentFile_WalksForward()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        var c = Path.Combine(tempDir, "c.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");
        File.WriteAllText(c, "c");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, c, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;

        var nextA = method.Invoke(manager, new object[] { a }) as string;
        AssertEqual(b, nextA!, "a to b");

        var nextB = method.Invoke(manager, new object[] { b }) as string;
        AssertEqual(c, nextB!, "b to c");

        var nextC = method.Invoke(manager, new object[] { c }) as string;
        AssertContains(nextC!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(source, "Path.GetFullPath(left)");
        AssertContains(source, "Path.GetFullPath(right)");
        AssertContains(source, "FLASHBACK_BUFFER_PATH_COMPARE_WARN");
        AssertContains(source, "if (IsSameSegmentPath(_completedSegments[i].Path, currentPath))");
        AssertContains(source, "if (IsSameSegmentPath(seg.Path, path) && File.Exists(seg.Path))");
        AssertContains(source, "if (IsSameSegmentPath(_activeSegmentPath, path) &&");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var equivalentA = Path.Combine(tempDir, ".", "a.ts");
        var nextMethod = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = nextMethod.Invoke(manager, new object[] { equivalentA }) as string;
        AssertEqual(b, next!, "Equivalent completed segment path should walk to next segment");

        var startMethod = manager.GetType().GetMethod("GetSegmentStartPts")!;
        var start = (TimeSpan?)startMethod.Invoke(manager, new object[] { equivalentA });
        AssertEqual(TimeSpan.Zero, start!.Value, "Equivalent completed segment path should resolve start PTS");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentStartPts")!;

        var missingStart = (TimeSpan?)method.Invoke(manager, new object[] { missingCompleted });
        AssertEqual(null, missingStart, "Missing completed segment should not expose start PTS");

        var existingStart = (TimeSpan?)method.Invoke(manager, new object[] { existingCompleted });
        AssertEqual(TimeSpan.FromSeconds(5), existingStart!.Value, "Existing completed segment should expose start PTS");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var activeStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(TimeSpan.FromSeconds(12), activeStart!.Value, "Active segment should expose marked encoder start PTS");

        File.Delete(active);
        var missingActiveStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(null, missingActiveStart, "Missing active segment should not expose start PTS");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var current = Path.Combine(tempDir, "current.ts");
        var missingNext = Path.Combine(tempDir, "missing-next.ts");
        var existingNext = Path.Combine(tempDir, "existing-next.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(current, "current");
        File.WriteAllText(existingNext, "next");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, current, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, missingNext, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, existingNext, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = method.Invoke(manager, new object[] { current }) as string;

        AssertEqual(existingNext, next!, "Next segment lookup should skip missing indexed segment");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var s0 = Path.Combine(tempDir, "s0.ts");
        var s1 = Path.Combine(tempDir, "s1.ts");
        var s2 = Path.Combine(tempDir, "s2.ts");
        var s3 = Path.Combine(tempDir, "s3.ts");
        File.WriteAllText(s0, "segment");
        File.WriteAllText(s1, "segment");
        File.WriteAllText(s2, "segment");
        File.WriteAllText(s3, "segment");

        AddCompletedSegment(manager, s0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, s1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, s2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);
        AddCompletedSegment(manager, s3, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentPaths")!;

        var result = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(12) })!;
        AssertEqual(3, GetCountProperty(result), "3s-12s should span 3 segments");

        var narrow = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(1, GetCountProperty(narrow), "5s-5.5s should be 1 segment");

        File.Delete(s1);
        var missing = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(0, GetCountProperty(missing), "Missing overlapping file should not be returned");

        var emptyRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(8) })!;
        AssertEqual(0, GetCountProperty(emptyRange), "Empty range should not return segments");

        var invertedRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(3) })!;
        AssertEqual(0, GetCountProperty(invertedRange), "Inverted range should not return segments");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "if (!File.Exists(seg.Path))\n                {\n                    continue;\n                }");
        AssertContains(source, "if (TryGetExistingActiveSegmentPath(out var activePath))");
        AssertContains(source, "SequenceNumber = Math.Max(0, _nextSegmentIndex - 1),");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentInfoList")!;
        var result = method.Invoke(manager, null)!;

        AssertEqual(2, GetCountProperty(result), "Segment info should include existing completed plus active");
        var infos = ((System.Collections.IEnumerable)result).Cast<object>().ToArray();
        var activeInfo = infos.Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(3, GetIntProperty(activeInfo, "SequenceNumber"), "Active segment sequence should match current generated segment index");
        AssertEqual(10_000L, GetLongProperty(activeInfo, "StartPtsMs"), "Unmarked active segment start should fall back to completed end");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var markedResult = method.Invoke(manager, null)!;
        var markedActiveInfo = ((System.Collections.IEnumerable)markedResult)
            .Cast<object>()
            .Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(12_000L, GetLongProperty(markedActiveInfo, "StartPtsMs"), "Marked active segment start should follow encoder PTS");

        File.Delete(active);
        var withoutActive = method.Invoke(manager, null)!;

        AssertEqual(1, GetCountProperty(withoutActive), "Segment info should omit missing active file");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_ActiveFilePath_RequiresExistingFile()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return TryGetExistingActiveSegmentPath(out var activePath)\n                    ? activePath\n                    : null;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        AssertEqual(null, GetPropertyValue(manager, "ActiveFilePath"), "Missing active file should not be exposed");

        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(active, "active");

        AssertEqual(active, (string)GetPropertyValue(manager, "ActiveFilePath")!, "Existing active file should be exposed");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentCount_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return _completedSegments.Count(seg => File.Exists(seg.Path)) +\n                    (TryGetExistingActiveSegmentPath(out _) ? 1 : 0);");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        AssertEqual(2, GetIntProperty(manager, "SegmentCount"), "Segment count should include existing completed plus active");

        File.Delete(active);

        AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count should omit missing active file");

        return Task.CompletedTask;
    }
    internal static Task FlashbackBufferManager_CleansStaleSessionDirectories()
    {
        var bufferText = ReadFlashbackBufferManagerSource();
        var cleanupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");
        var budgetText = cleanupText;
        var scannerText = cleanupText
            .Replace("\r\n", "\n");
        var playbackSegmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackSegmentSwitchText = playbackSegmentEdgesText;
        var decoderSegmentReopenText = playbackSegmentEdgesText;

        AssertContains(cleanupText, "internal static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);");
        AssertContains(cleanupText, "private const int MaxStaleSessionDirectoryScansPerInit = 64;");
        AssertContains(budgetText, "private const int MaxStartupCacheSessionDirectoryScansPerInit = 256;");
        AssertContains(budgetText, "private const int MaxStartupCacheSessionDirectoriesPerInit = 32;");
        AssertContains(budgetText, "private const long StartupCacheBudgetMultiplier = 2;");
        AssertContains(cleanupText, "private const int MaxStaleRootSegmentFileScansPerInit = 512;");

        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleRootSegmentFiles(tempDirectory);");
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(tempDirectory, sessionDirectory);");
        AssertContains(bufferText, "var cacheCleanup = FlashbackStartupSessionCacheBudget.CleanupSessionCacheBudget(");
        AssertContains(bufferText, "FlashbackStartupSessionCacheBudget.CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));");
        AssertContains(bufferText, "var sessionDirectory = FlashbackSessionRecoveryScanner.BuildSessionDirectory(tempDirectory, sessionId);");

        AssertContains(scannerText, "internal static string BuildSessionDirectory(string tempDirectory, string sessionId)");
        AssertContains(scannerText, "Session id must be a simple file-name component.");
        AssertContains(scannerText, "Session id must resolve inside the flashback temp directory.");
        AssertContains(scannerText, "internal static string NormalizeSegmentExtension(string extension)");
        AssertContains(scannerText, "Flashback segment extension must be .ts or .mp4.");
        AssertContains(scannerText, "internal static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)");
        AssertContains(scannerText, "internal static bool IsReparsePoint(FileSystemInfo info)");
        AssertContains(scannerText, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");

        AssertContains(bufferText, "var normalizedExtension = FlashbackSessionRecoveryScanner.NormalizeSegmentExtension(extension);");
        AssertContains(bufferText, "public long TempDriveAvailableFreeBytes => FlashbackStartupCacheCleanup.TryGetTempDriveAvailableFreeBytes(_options.TempDirectory);");

        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=reparse_point");
        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
        AssertContains(budgetText, "FLASHBACK_CACHE_BUDGET_SKIP reason=outside_temp");
        AssertContains(budgetText, "FLASHBACK_SESSION_STATS_SKIP reason=reparse_point");
        AssertContains(cleanupText, "if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))");
        AssertContains(budgetText, "FLASHBACK_CACHE_BUDGET_PRESERVE_SKIP");
        AssertContains(budgetText, "FLASHBACK_CACHE_BUDGET_CLEANUP");
        AssertContains(cleanupText, "info.EnumerateFiles(\"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.EnumerateFiles(tempDirectory, \"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.Delete(fullPath, recursive: true);");

        AssertContains(bufferText, "if (IsSameSegmentPath(_activeSegmentPath, currentPath))\n                return TryGetExistingActiveSegmentPath(out var activePath) ? activePath : null;");
        AssertContains(bufferText, "return GetOldestExistingSegmentPath()\n                ?? (TryGetExistingActiveSegmentPath(out var fallbackActivePath) ? fallbackActivePath : null);");
        AssertContains(bufferText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(playbackSegmentEdgesText, "TrySwitchToNextSegment(");
        AssertContains(playbackSegmentSwitchText, "var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);");
        AssertContains(playbackSegmentSwitchText, "if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)");
        AssertContains(decoderSegmentReopenText, "var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);");
        AssertContains(decoderSegmentReopenText, "if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_startup_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            SetPrivateField(manager, "_activeSegmentPath", null);
            var startingIndex = (int)GetPrivateField(manager, "_nextSegmentIndex")!;
            var getFilePath = manager.GetType().GetMethod("AcquireSegmentPath", new[] { typeof(bool).MakeByRefType() })
                ?? throw new InvalidOperationException("FlashbackBufferManager.AcquireSegmentPath(out bool) not found.");
            var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
                ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");

            object?[] args = { false };
            var generatedPath = (string)getFilePath.Invoke(manager, args)!;
            AssertEqual(true, (bool)args[0]!, "Fresh AcquireSegmentPath reports generated path");
            AssertEqual(generatedPath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Generated path becomes raw active segment");
            AssertEqual(startingIndex + 1, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Generated path advances segment index");

            File.WriteAllBytes(generatedPath, new byte[17]);
            abandonGenerated.Invoke(manager, new object?[] { generatedPath, null });

            AssertEqual<string?>(null, (string?)GetPrivateField(manager, "_activeSegmentPath"), "Abandon clears startup-generated active path");
            AssertEqual(false, File.Exists(generatedPath), "Abandon deletes partial startup segment file");
            AssertEqual(startingIndex, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Abandon rolls back generated segment index");

            object?[] retryArgs = { false };
            var retryPath = (string)getFilePath.Invoke(manager, retryArgs)!;
            AssertEqual(true, (bool)retryArgs[0]!, "Retry after abandon generates a fresh path");
            AssertEqual(generatedPath, retryPath, "Retry reuses the rolled-back segment slot");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_RemovesStaleLegacyRootSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_legacy_cleanup_{Guid.NewGuid():N}");
        object? manager = null;
        Directory.CreateDirectory(tempDir);

        try
        {
            var staleRootSegment = Path.Combine(tempDir, "fb_legacy_0001.ts");
            var recentRootSegment = Path.Combine(tempDir, "fb_recent_0001.ts");
            var unrelatedFile = Path.Combine(tempDir, "unrelated.ts");
            File.WriteAllText(staleRootSegment, "stale");
            File.WriteAllText(recentRootSegment, "recent");
            File.WriteAllText(unrelatedFile, "keep");

            File.SetLastWriteTimeUtc(staleRootSegment, DateTime.UtcNow - TimeSpan.FromHours(13));
            File.SetLastWriteTimeUtc(recentRootSegment, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(unrelatedFile, DateTime.UtcNow - TimeSpan.FromHours(13));

            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            manager = Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");
            initialize.Invoke(manager, new object[] { "current-session" });

            AssertEqual(false, File.Exists(staleRootSegment), "Stale root fb_* segment removed");
            AssertEqual(true, File.Exists(recentRootSegment), "Recent root fb_* segment preserved");
            AssertEqual(true, File.Exists(unrelatedFile), "Unrelated root file preserved");
            AssertEqual(true, Directory.Exists(Path.Combine(tempDir, "current-session")), "Current session directory created");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_stale_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var staleFlashbackSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var unrelatedEmptyDirectory = Path.Combine(tempDir, "empty-but-not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(staleFlashbackSession);
            Directory.CreateDirectory(unrelatedEmptyDirectory);

            var staleTime = DateTime.UtcNow - TimeSpan.FromHours(13);
            Directory.SetLastWriteTimeUtc(staleFlashbackSession, staleTime);
            Directory.SetLastWriteTimeUtc(unrelatedEmptyDirectory, staleTime);

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupStaleSessionDirectories", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupStaleSessionDirectories not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession });

            AssertEqual(true, Directory.Exists(currentSession), "Current empty session directory preserved");
            AssertEqual(false, Directory.Exists(staleFlashbackSession), "Plausible stale empty flashback session removed");
            AssertEqual(true, Directory.Exists(unrelatedEmptyDirectory), "Unrelated stale empty directory preserved");

            var cleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
                .Replace("\r\n", "\n");
            var scannerSource = cleanupSource;
            AssertContains(cleanupSource, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
            AssertContains(scannerSource, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");
            AssertContains(scannerSource, "internal static bool IsLowerHexString(ReadOnlySpan<char> value)");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_TrimsStartupSessionCacheBudget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cache_budget_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, "current-session");
            var oldSession = Path.Combine(tempDir, "old-session");
            var recentSession = Path.Combine(tempDir, "recent-session");
            var preservedSession = Path.Combine(tempDir, "preserved-session");
            var nonFlashbackDirectory = Path.Combine(tempDir, "not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(oldSession);
            Directory.CreateDirectory(recentSession);
            Directory.CreateDirectory(preservedSession);
            Directory.CreateDirectory(nonFlashbackDirectory);

            WriteSizedFile(Path.Combine(currentSession, "fb_current_0001.ts"), 1);
            WriteSizedFile(Path.Combine(oldSession, "fb_old_0001.ts"), 20);
            WriteSizedFile(Path.Combine(recentSession, "fb_recent_0001.ts"), 10);
            WriteSizedFile(Path.Combine(preservedSession, "fb_preserved_0001.ts"), 100);
            File.WriteAllText(Path.Combine(preservedSession, ".flashback-recovery-preserve"), "keep");
            File.WriteAllText(Path.Combine(nonFlashbackDirectory, "notes.txt"), "keep");

            var now = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(Path.Combine(oldSession, "fb_old_0001.ts"), now - TimeSpan.FromHours(2));
            File.SetLastWriteTimeUtc(Path.Combine(recentSession, "fb_recent_0001.ts"), now - TimeSpan.FromMinutes(5));

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupSessionCacheBudget");
            var cleanup = cleanupType.GetMethod("CleanupSessionCacheBudget", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupSessionCacheBudget not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession, 25L });

            AssertEqual(true, Directory.Exists(currentSession), "Current session preserved");
            AssertEqual(false, Directory.Exists(oldSession), "Oldest session removed to satisfy budget");
            AssertEqual(true, Directory.Exists(recentSession), "Recent session preserved once budget is satisfied");
            AssertEqual(true, Directory.Exists(preservedSession), "Recovery-preserved session skipped");
            AssertEqual(true, Directory.Exists(nonFlashbackDirectory), "Non-flashback directory preserved");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_RejectsUnsafeSessionIds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_session_id_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");

            try
            {
                initialize.Invoke(manager, new object[] { "..\\outside-session" });
                throw new InvalidOperationException("Expected unsafe session id to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
            }

            AssertEqual(false, Directory.Exists(Path.Combine(Directory.GetParent(tempDir)!.FullName, "outside-session")), "Unsafe session id must not create outside directory");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_ValidatesSegmentExtensions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_segment_ext_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "safe-session" });

            var setExtension = managerType.GetMethod("SetSegmentExtension")
                ?? throw new InvalidOperationException("SetSegmentExtension not found.");
            var generatePath = managerType.GetMethod("GenerateSegmentPath")
                ?? throw new InvalidOperationException("GenerateSegmentPath not found.");

            setExtension.Invoke(manager, new object[] { ".TS" });
            var transportPath = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, transportPath.EndsWith(".ts", StringComparison.Ordinal), "Transport stream extension normalized");

            setExtension.Invoke(manager, new object[] { ".Mp4" });
            var mp4Path = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, mp4Path.EndsWith(".mp4", StringComparison.Ordinal), "MP4 extension normalized");

            try
            {
                setExtension.Invoke(manager, new object[] { "..\\escape.ts" });
                throw new InvalidOperationException("Expected unsafe segment extension to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_InitializeClearsRecordingPts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_init_pts_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            var manager = Activator.CreateInstance(managerType, new[] { options })
                ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
            using var disposableManager = manager as IDisposable;

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-a" });
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(10) });
            managerType.GetMethod("PauseEviction")!.Invoke(manager, null);
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) });
            managerType.GetMethod("ResumeEviction")!.Invoke(manager, null);

            AssertEqual(TimeSpan.FromSeconds(10), (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts before reinitialize");
            AssertEqual(TimeSpan.FromSeconds(20), (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts before reinitialize");

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-b" });
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts resets on Initialize");
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts resets on Initialize");

            var activePath = (string)managerType.GetMethod("AcquireSegmentPath", Type.EmptyTypes)!.Invoke(manager, null)!;
            File.WriteAllBytes(activePath, new byte[] { 1, 2, 3, 4 });
            var segmentInfo = (System.Collections.IEnumerable)managerType.GetMethod("GetSegmentInfoList")!.Invoke(manager, null)!;
            var activeInfo = segmentInfo.Cast<object>().Single(info => (bool)GetPropertyValue(info, "IsActive")!);
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "StartPtsMs")!, "Active segment start PTS resets on Initialize");
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "EndPtsMs")!, "Active segment end PTS resets on Initialize");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_bytes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed and active bytes");

            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            var deleteDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (File.Exists(firstSegment) && DateTime.UtcNow < deleteDeadline)
            {
                Thread.Sleep(25);
            }

            AssertEqual(false, File.Exists(firstSegment), "Eviction should delete the expired completed segment");
            AssertEqual(true, File.Exists(secondSegment), "Eviction should retain overlapping completed segment");
            AssertEqual(250L, GetLongProperty(manager, "TotalDiskBytes"), "Eviction subtracts deleted completed segment bytes");
            AssertEqual(200L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache matches retained segment");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_locked_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            SetPrivateField(manager, "_sessionDirectory", Path.Combine(tempDir, "different-session"));
            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            AssertEqual(true, File.Exists(firstSegment), "Rejected expired segment remains on disk");
            AssertEqual(true, File.Exists(secondSegment), "Later segment is not evicted past a rejected predecessor");
            AssertEqual(3, GetIntProperty(manager, "SegmentCount"), "Rejected completed segments remain tracked with active segment");
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Rejected segment bytes stay in disk accounting");
            AssertEqual(300L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache retains rejected segment");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "if (DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"valid_window\"))");
            AssertContains(source, "private static bool DeleteEvictedFile");
            AssertContains(source, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_EvictionPauseResume_Balanced()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var pauseMethod = manager.GetType().GetMethod("PauseEviction")!;
        var resumeMethod = manager.GetType().GetMethod("ResumeEviction")!;

        // Initially not paused
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "Initial EvictionPaused");

        // Pause -> paused
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 pause");

        // Double-pause -> still paused (count-based)
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 2 pauses");

        // Resume once -> still paused (count = 1)
        resumeMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 resume (count=1)");

        // Resume again -> unpaused (count = 0)
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After 2 resumes (count=0)");

        // Extra resume -> remains unpaused and must not underflow the pause counter.
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After unbalanced resume");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(source, "var unbalancedEndPts = ClampEndPtsToStart(_recordingStartPts, _recordingEndPts);");
        AssertContains(source, "_recordingEndPts = ClampEndPtsToStart(\n                    _recordingStartPts,\n                    TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var rangeSeconds = TimeSpan.FromTicks(NonNegativeDeltaTicks(_recordingEndPts.Ticks, _recordingStartPts.Ticks)).TotalSeconds;");
        AssertContains(source, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertDoesNotContain(source, "range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");

        return Task.CompletedTask;
    }


    internal static Task FlashbackBufferManager_PurgesRetainLockedActivePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_locked_active_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        string? activePath = null;

        try
        {
            activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[50]);
            File.SetAttributes(activePath, File.GetAttributes(activePath) | FileAttributes.ReadOnly);

            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
            var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Setup tracks active bytes");
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Setup tracks active bytes written");

            purgeCompleted.Invoke(manager, null);

            AssertEqual(true, File.Exists(activePath), "Read-only active file remains on disk");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Read-only active path remains tracked");
            AssertEqual(activePath, GetStringProperty(manager, "ActiveFilePath"), "ActiveFilePath still reports read-only active segment");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Read-only active bytes remain in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Read-only active byte baseline is preserved");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Same active bytes are not double-counted after failed purge");

            purgeAll.Invoke(manager, null);
            AssertEqual(true, File.Exists(activePath), "Read-only active file remains after full purge attempt");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Full purge keeps read-only active path tracked");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Full purge segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge keeps read-only active bytes in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Full purge keeps read-only active byte baseline");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(activePath) && File.Exists(activePath))
            {
                try { File.SetAttributes(activePath, FileAttributes.Normal); } catch { }
            }

            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_full_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var completedPath = Path.Combine(tempDir, "completed.ts");
            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(completedPath, new byte[300]);
            File.WriteAllBytes(activePath, new byte[50]);
            AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 300L);
            SetPrivateField(manager, "_completedSegmentBytes", 300L);
            SetPrivateField(manager, "_totalDiskBytes", 350L);

            var purgeCore = manager.GetType().GetMethod("PurgeAllSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegmentsCore not found.");
            var result = purgeCore.Invoke(manager, null)!;
            var segments = Convert.ToInt32(result.GetType().GetField("Item1")!.GetValue(result));
            var freedBytes = Convert.ToInt64(result.GetType().GetField("Item2")!.GetValue(result));

            AssertEqual(2, segments, "Full purge reports completed plus active segment");
            AssertEqual(350L, freedBytes, "Full purge reports completed plus active bytes exactly once");
            AssertEqual(false, File.Exists(completedPath), "Full purge deletes completed segment");
            AssertEqual(false, File.Exists(activePath), "Full purge deletes active segment");
            AssertEqual(0L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge resets total disk bytes");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Full purge resets monotonic bytes for a new buffer session");

            var source = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
                .Replace("\r\n", "\n");
            var purgeCoreBlock = ExtractTextBetween(
                source,
                "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()",
                "    private bool TryDeleteFile(string filePath)");
            AssertOccursBefore(purgeCoreBlock, "var activeBytes = _activeSegmentPath != null", "if (_activeSegmentPath != null)");
            AssertContains(purgeCoreBlock, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
            AssertContains(purgeCoreBlock, "var retainedActiveBytes = _activeSegmentPath != null ? activeBytes : 0;");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_partial_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        FileStream? lockedCompleted = null;

        try
        {
            var completedPath = Path.Combine(tempDir, "completed-locked.ts");
            var deletableCompletedPath = Path.Combine(tempDir, "completed-deletable.ts");
            var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
            File.WriteAllBytes(completedPath, new byte[100]);
            File.WriteAllBytes(deletableCompletedPath, new byte[200]);
            File.WriteAllBytes(activePath, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                completedPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                deletableCompletedPath,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed plus active bytes");

            lockedCompleted = new FileStream(completedPath, FileMode.Open, FileAccess.Read, FileShare.None);
            purgeCompleted.Invoke(manager, null);

            AssertEqual(false, File.Exists(activePath), "Partial purge should still delete stale active segment");
            AssertEqual(false, File.Exists(deletableCompletedPath), "Partial purge deletes unlocked completed segments");
            AssertEqual(true, File.Exists(completedPath), "Partial purge retains locked completed segments");
            AssertEqual(100L, GetLongProperty(manager, "TotalDiskBytes"), "Partial purge subtracts deleted completed and active bytes");
            AssertEqual(100L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Partial purge preserves retained completed byte accounting");
            AssertEqual(0L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Partial purge resets active byte baseline");

            updateDiskBytes.Invoke(manager, new object[] { 25L });
            AssertEqual(125L, GetLongProperty(manager, "TotalDiskBytes"), "Next active bytes are added to retained completed bytes");
            AssertEqual(375L, GetLongProperty(manager, "TotalBytesWritten"), "Next active segment bytes are counted after purge baseline reset");
        }
        finally
        {
            lockedCompleted?.Dispose();
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }
}

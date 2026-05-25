using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
    public Task RecordingVerifierCadenceAnalysisLivesInFocusedPartial()
        => global::Program.RecordingVerifier_CadenceAnalysisLivesInFocusedPartial();

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
    public Task LibAvEncoderOutputLifecycleLivesInFocusedPartials()
        => global::Program.LibAvEncoder_OutputLifecycleLivesInFocusedPartials();

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

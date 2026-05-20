using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddCoreRuntimeRecordingChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Recording verifier fails when output file is missing",
            RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist);
        await AddCheckAsync(results,
            "Recording verifier fails when output file is empty",
            RecordingVerifier_ReturnsFailure_WhenFileIsEmpty);
        await AddCheckAsync(results,
            "Recording verifier fails when output path is null",
            RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull);
        await AddCheckAsync(results,
            "Recording verifier implements verification interface",
            RecordingVerifier_ImplementsIRecordingVerifier);
        await AddCheckAsync(results,
            "Recording verifier cadence analysis lives in focused partial",
            RecordingVerifier_CadenceAnalysisLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Recording verifier probe validation and result shaping live in focused partials",
            RecordingVerifier_ProbeValidationAndResultsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "Recording verification result exposes expected properties",
            RecordingVerificationResult_HasExpectedProperties);
        await AddCheckAsync(results,
            "Recording verifier fails when ffprobe is unavailable",
            RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable);
        await AddCheckAsync(results,
            "Recording verifier runs ffprobe below normal priority",
            RecordingVerifier_RunsFfprobeBelowNormalPriority);
        await AddCheckAsync(results,
            "Recording verifier passes HEVC when all fields match",
            RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc);
        await AddCheckAsync(results,
            "Recording verifier detects H264 codec when HEVC is expected",
            RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc);
        await AddCheckAsync(results,
            "Recording verifier uses flashback export verification format",
            RecordingVerifier_UsesFlashbackExportVerificationFormat);
        await AddCheckAsync(results,
            "Recording verifier uses flashback recording verification format",
            RecordingVerifier_UsesFlashbackRecordingVerificationFormat);
        await AddCheckAsync(results,
            "Recording verifier detects resolution mismatch",
            RecordingVerifier_DetectsResolutionMismatch);
        await AddCheckAsync(results,
            "Recording verifier detects frame-rate mismatch",
            RecordingVerifier_DetectsFrameRateMismatch);
        await AddCheckAsync(results,
            "Recording verifier passes HDR validation when metadata is present",
            RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent);
        await AddCheckAsync(results,
            "Recording verifier detects HDR colorimetry mismatch",
            RecordingVerifier_DetectsHdrColorimetryMismatch);
        await AddCheckAsync(results,
            "Recording verifier passes H264 format",
            RecordingVerifier_PassesVerification_ForH264Format);
        await AddCheckAsync(results,
            "Recording verifier tolerates NTSC frame-rate drift",
            RecordingVerifier_PassesNtscFrameRateWithinTolerance);
        await AddCheckAsync(results,
            "Recording verifier fails when ffprobe exits nonzero",
            RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero);
        await AddCheckAsync(results,
            "LibAv encoder HDR bitstream filters map codecs",
            LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs);
        await AddCheckAsync(results,
            "LibAv encoder chains HDR and MPEG-TS bitstream filters",
            LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters);
        await AddCheckAsync(results,
            "LibAv encoder expected frame sizes match pixel formats",
            LibAvEncoder_GetExpectedFrameSizeBytes_CalculatesCorrectly);
        await AddCheckAsync(results,
            "LibAv encoder NVENC presets map correctly",
            LibAvEncoder_MapNvencPreset_MapsCorrectly);
        await AddCheckAsync(results,
            "LibAv encoder throws on negative native errors",
            LibAvEncoder_ThrowIfError_ThrowsOnNegative);
        await AddCheckAsync(results,
            "LibAv encoder rational inversion swaps numerator and denominator",
            LibAvEncoder_Invert_SwapsNumeratorDenominator);
        await AddCheckAsync(results,
            "LibAv encoder HDR rationals parse correctly",
            LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly);
        await AddCheckAsync(results,
            "LibAv encoder accepts valid options",
            LibAvEncoder_ValidateOptions_AcceptsValidOptions);
        await AddCheckAsync(results,
            "LibAv encoder rejects empty output path",
            LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath);
        await AddCheckAsync(results,
            "LibAv encoder rejects zero dimensions",
            LibAvEncoder_ValidateOptions_RejectsZeroDimensions);
        await AddCheckAsync(results,
            "LibAv encoder rejects HDR with H264",
            LibAvEncoder_ValidateOptions_RejectsHdrWithH264);
        await AddCheckAsync(results,
            "LibAv encoder rejects HDR without P010",
            LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010);
        await AddCheckAsync(results,
            "LibAv encoder rejects mismatched frame-rate parts",
            LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts);
        await AddCheckAsync(results,
            "LibAv encoder fragments MP4 tightly for flashback playback",
            LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback);
        await AddCheckAsync(results,
            "LibAv encoder dumps MPEG-TS headers for rotated flashback segments",
            LibAvEncoder_MpegTsNvencDumpsHeadersForRotatedSegments);
        await AddCheckAsync(results,
            "LibAv encoder packet writing lives in focused partial",
            LibAvEncoder_PacketWritingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv encoder frame copy lives in focused partial",
            LibAvEncoder_FrameCopyLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv encoder video submission lives in focused partial",
            LibAvEncoder_VideoSubmissionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv encoder initialization lives in focused partial",
            LibAvEncoder_InitializationLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv encoder diagnostics helpers live in focused partial",
            LibAvEncoder_DiagnosticsHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv encoder setup and models live in focused partials",
            LibAvEncoder_SetupAndModelsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "LibAv encoder output lifecycle lives in focused partials",
            LibAvEncoder_OutputLifecycleLivesInFocusedPartials);
        await AddCheckAsync(results,
            "Flashback integrity uses recording-scoped sequence gaps",
            FlashbackRecordingIntegrity_UsesRecordingScopedSequenceGaps);
        await AddCheckAsync(results,
            "Shared formatter renders recording integrity",
            SharedFormatter_RendersRecordingIntegrity);
        await AddCheckAsync(results,
            "Dedicated LibAv verification script uses flashback-off strict workflow",
            DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification);
    }
}

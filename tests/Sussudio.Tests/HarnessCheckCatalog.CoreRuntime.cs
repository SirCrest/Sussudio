using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddCoreRuntimeChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Observed telemetry uses explicit counters",
            GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts);
        await AddCheckAsync(results,
            "Runtime snapshot preserves MJPG source subtype when observed frames are NV12",
            GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded);
        await AddCheckAsync(results,
            "Telemetry alignment mismatch surfaces reason",
            GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest);
        await AddCheckAsync(results,
            "Telemetry unavailable maps to unavailable state",
            GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable);
        await AddCheckAsync(results,
            "HDR truth treats HDR source with SDR request as expected",
            Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected);
        await AddCheckAsync(results,
            "NativeXu telemetry accepts known 4K X product revisions",
            NativeXuTelemetry_AcceptsKnown4kXProductRevisions);
        await AddCheckAsync(results,
            "KS extension-unit native helper is split by boundary",
            KsExtensionUnitNative_SourceOwnership_IsSplitByNativeBoundary);
        await AddCheckAsync(results,
            "NativeXu telemetry rolling poll lives in focused partial",
            NativeXuAtCommandProvider_RollingPollLivesInFocusedPartial);
        await AddCheckAsync(results,
            "NativeXu audio command sequences live in focused partials",
            NativeXuAtCommandProvider_AudioCommandsLiveInFocusedPartial);
        await AddCheckAsync(results,
            "NativeXu payload decoding lives in focused partial",
            NativeXuAtCommandProvider_PayloadDecodingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "NativeXu telemetry details live in focused partials",
            NativeXuAtCommandProvider_TelemetryDetailsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "Health snapshot propagates structured source telemetry details",
            CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails);
        await AddCheckAsync(results,
            "Automation snapshots expose high-confidence source telemetry fields",
            AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields);
        await AddCheckAsync(results,
            "HDR idle snapshot reports ready pipeline parity",
            GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle);
        await AddCheckAsync(results,
            "HDR recording mismatch reports violation",
            GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr);
        await AddCheckAsync(results,
            "Thread health probes default cleanly when inactive",
            GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive);
        await AddCheckAsync(results,
            "CaptureService initialization lives in focused partial",
            CaptureService_InitializationLivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService runtime snapshot assembler owns DTO mapping",
            CaptureService_RuntimeSnapshotAssembler_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService runtime ingest audio projection lives in focused partial",
            CaptureService_RuntimeIngestAudioProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService runtime reader transport projection lives in focused partial",
            CaptureService_RuntimeReaderTransportProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService runtime HDR pipeline projection lives in focused partial",
            CaptureService_RuntimeHdrPipelineProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService runtime source telemetry projection lives in focused partial",
            CaptureService_RuntimeSourceTelemetryProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService runtime recording integrity projection lives in focused partial",
            CaptureService_RuntimeRecordingIntegrityProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService snapshot helper policy lives in focused partials",
            CaptureService_SnapshotHelperPolicy_LivesInFocusedPartials);
        await AddCheckAsync(results,
            "CaptureService encoder codec names map recording formats",
            CaptureService_ResolveEncoderCodecName_MapsFormats);
        await AddCheckAsync(results,
            "CaptureService encoder output pixel format distinguishes HDR",
            CaptureService_ResolveEncoderOutputPixelFormat_DistinguishesHdr);
        await AddCheckAsync(results,
            "CaptureService HDR warmup state resolves expected states",
            CaptureService_ResolveHdrWarmupState_ReturnsCorrectStates);
        await AddCheckAsync(results,
            "CaptureService observed pixel format normalization is stable",
            CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly);
        await AddCheckAsync(results,
            "CaptureService observed pixel telemetry lives in focused partial",
            CaptureService_ObservedPixelTelemetry_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "CaptureService source telemetry backend maps origins",
            CaptureService_ResolveSourceTelemetryBackend_MapsOrigins);
        await AddCheckAsync(results,
            "CaptureService encoder video profile maps formats and HDR",
            CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr);
        await AddCheckAsync(results,
            "CaptureService tick age uses empty-tick sentinel",
            CaptureService_ComputeTickAge_ReturnsCorrectValues);
        await AddCheckAsync(results,
            "CaptureService telemetry alignment detects mismatches",
            CaptureService_ResolveTelemetryAlignment_DetectsMismatches);
        await AddCheckAsync(results,
            "CaptureService telemetry circuit state resolves open and closed",
            CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState);
        await AddCheckAsync(results,
            "Health snapshot uses cached MJPEG timing metrics when capture is gone",
            GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone);
        await AddCheckAsync(results,
            "Diagnostics snapshot mirrors MJPEG timing metrics",
            GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics);
        await AddCheckAsync(results,
            "Automation snapshot contract exposes full CPU MJPEG metrics",
            AutomationSnapshot_ExposesFullCpuMjpegMetrics);
        await AddCheckAsync(results,
            "Frame ledger retains bounded recent events",
            FrameLedger_RetainsBoundedRecentEvents);
        await AddCheckAsync(results,
            "Frame ledger snapshot contract exposes recent events",
            FrameLedger_SnapshotContractExposesRecentEvents);
        await AddCheckAsync(results,
            "Recording integrity summary defaults explicitly",
            RecordingIntegritySummary_DefaultsAreExplicit);
        await AddCheckAsync(results,
            "Recording integrity snapshot contract exposes automation fields",
            RecordingIntegritySnapshotContract_ExposesAutomationFields);
        await AddCheckAsync(results,
            "Recording integrity automation projection lives in focused partial",
            RecordingIntegrityAutomationProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Recording integrity flags audio discontinuity and drift",
            RecordingIntegritySummary_FlagsAudioDiscontinuityAndDrift);
        await AddCheckAsync(results,
            "Recording integrity tolerates active in-flight frame",
            RecordingIntegritySummary_ToleratesSingleActiveInFlightFrame);
        await AddCheckAsync(results,
            "CaptureService recording integrity ownership lives in focused partials",
            CaptureService_RecordingIntegrityLivesInFocusedPartials);
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
            "Automation options contract exposes advanced MCP control state",
            AutomationOptionsSnapshot_ExposesAdvancedControlState);
        await AddCheckAsync(results,
            "Automation command maps stay aligned for advanced MCP controls",
            AutomationCommandMaps_StayAligned_ForAdvancedMcpControls);
        await AddCheckAsync(results,
            "Dedicated LibAv verification script uses flashback-off strict workflow",
            DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification);
    }
}

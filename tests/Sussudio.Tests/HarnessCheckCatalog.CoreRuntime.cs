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
        await AddCoreRuntimeRecordingChecksAsync(results);
    }
}

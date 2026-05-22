using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class CoreRuntimeContractsTests
{
    public CoreRuntimeContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ObservedTelemetryUsesExplicitCounters()
        => global::Program.GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts();

    [Fact]
    public Task RuntimeSnapshotPreservesMjpgSourceSubtypeWhenObservedFramesAreNv12()
        => global::Program.GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded();

    [Fact]
    public Task TelemetryAlignmentMismatchSurfacesReason()
        => global::Program.GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest();

    [Fact]
    public Task TelemetryUnavailableMapsToUnavailableState()
        => global::Program.GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable();

    [Fact]
    public Task HdrTruthTreatsHdrSourceWithSdrRequestAsExpected()
        => global::Program.Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected();

    [Fact]
    public Task NativeXuTelemetryAcceptsKnown4kXProductRevisions()
        => global::Program.NativeXuTelemetry_AcceptsKnown4kXProductRevisions();

    [Fact]
    public Task KsExtensionUnitNativeHelperIsSplitByBoundary()
        => global::Program.KsExtensionUnitNative_SourceOwnership_IsSplitByNativeBoundary();

    [Fact]
    public Task NativeXuTelemetryRollingPollLivesInFocusedPartial()
        => global::Program.NativeXuAtCommandProvider_RollingPollLivesInFocusedPartial();

    [Fact]
    public Task NativeXuAudioCommandSequencesLiveInFocusedPartials()
        => global::Program.NativeXuAtCommandProvider_AudioCommandsLiveInFocusedPartial();

    [Fact]
    public Task NativeXuPayloadDecodingLivesInFocusedPartial()
        => global::Program.NativeXuAtCommandProvider_PayloadDecodingLivesInFocusedPartial();

    [Fact]
    public Task NativeXuTelemetryDetailsLiveInFocusedPartials()
        => global::Program.NativeXuAtCommandProvider_TelemetryDetailsLiveInFocusedPartials();

    [Fact]
    public Task HealthSnapshotPropagatesStructuredSourceTelemetryDetails()
        => global::Program.CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails();

    [Fact]
    public Task HdrIdleSnapshotReportsReadyPipelineParity()
        => global::Program.GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle();

    [Fact]
    public Task HdrRecordingMismatchReportsViolation()
        => global::Program.GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr();

    [Fact]
    public Task ThreadHealthProbesDefaultCleanlyWhenInactive()
        => global::Program.GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive();

    [Fact]
    public Task CaptureServiceInitializationLivesWithServiceRoot()
        => global::Program.CaptureService_InitializationLivesWithServiceRoot();

    [Fact]
    public Task CaptureServiceRuntimeSnapshotAssemblerOwnsDtoMapping()
        => global::Program.CaptureService_RuntimeSnapshotAssembler_LivesInFocusedPartial();

    [Fact]
    public Task CaptureServiceRuntimeIngestAudioProjectionLivesWithRuntimeSnapshotSampler()
        => global::Program.CaptureService_RuntimeIngestAudioProjection_LivesWithRuntimeSnapshotSampler();

    [Fact]
    public Task CaptureServiceRuntimeReaderTransportProjectionLivesWithRuntimeSnapshotSampler()
        => global::Program.CaptureService_RuntimeReaderTransportProjection_LivesWithRuntimeSnapshotSampler();

    [Fact]
    public Task CaptureServiceRuntimeHdrPipelineProjectionLivesInFocusedPartial()
        => global::Program.CaptureService_RuntimeHdrPipelineProjection_LivesInFocusedPartial();

    [Fact]
    public Task CaptureServiceRuntimeSourceTelemetryProjectionLivesInFocusedPartial()
        => global::Program.CaptureService_RuntimeSourceTelemetryProjection_LivesInFocusedPartial();

    [Fact]
    public Task CaptureServiceRuntimeRecordingIntegrityProjectionLivesInFocusedPartial()
        => global::Program.CaptureService_RuntimeRecordingIntegrityProjection_LivesInFocusedPartial();

    [Fact]
    public Task CaptureServiceSnapshotHelperPolicyLivesInFocusedPartials()
        => global::Program.CaptureService_SnapshotHelperPolicy_LivesInFocusedPartials();

    [Fact]
    public Task CaptureServiceEncoderCodecNamesMapRecordingFormats()
        => global::Program.CaptureService_ResolveEncoderCodecName_MapsFormats();

    [Fact]
    public Task CaptureServiceEncoderOutputPixelFormatDistinguishesHdr()
        => global::Program.CaptureService_ResolveEncoderOutputPixelFormat_DistinguishesHdr();

    [Fact]
    public Task CaptureServiceHdrWarmupStateResolvesExpectedStates()
        => global::Program.CaptureService_ResolveHdrWarmupState_ReturnsCorrectStates();

    [Fact]
    public Task CaptureServiceObservedPixelFormatNormalizationIsStable()
        => global::Program.CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly();

    [Fact]
    public Task CaptureServiceObservedPixelTelemetryLivesWithCaptureFormatTelemetry()
        => global::Program.CaptureService_ObservedPixelTelemetry_LivesWithCaptureFormatTelemetry();

    [Fact]
    public Task CaptureServiceSourceTelemetryBackendMapsOrigins()
        => global::Program.CaptureService_ResolveSourceTelemetryBackend_MapsOrigins();

    [Fact]
    public Task CaptureServiceEncoderVideoProfileMapsFormatsAndHdr()
        => global::Program.CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr();

    [Fact]
    public Task CaptureServiceTickAgeUsesEmptyTickSentinel()
        => global::Program.CaptureService_ComputeTickAge_ReturnsCorrectValues();

    [Fact]
    public Task CaptureServiceTelemetryAlignmentDetectsMismatches()
        => global::Program.CaptureService_ResolveTelemetryAlignment_DetectsMismatches();

    [Fact]
    public Task CaptureServiceTelemetryCircuitStateResolvesOpenAndClosed()
        => global::Program.CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState();

    [Fact]
    public Task HealthSnapshotUsesCachedMjpegTimingMetricsWhenCaptureIsGone()
        => global::Program.GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone();

    [Fact]
    public Task DiagnosticsSnapshotMirrorsMjpegTimingMetrics()
        => global::Program.GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics();

    [Fact]
    public Task FrameLedgerRetainsBoundedRecentEvents()
        => global::Program.FrameLedger_RetainsBoundedRecentEvents();

    [Fact]
    public Task FrameLedgerSnapshotContractExposesRecentEvents()
        => global::Program.FrameLedger_SnapshotContractExposesRecentEvents();

    [Fact]
    public Task RecordingIntegritySummaryDefaultsExplicitly()
        => global::Program.RecordingIntegritySummary_DefaultsAreExplicit();

    [Fact]
    public Task RecordingIntegritySnapshotContractExposesAutomationFields()
        => global::Program.RecordingIntegritySnapshotContract_ExposesAutomationFields();

    [Fact]
    public Task RecordingIntegrityAutomationProjectionLivesInFocusedPartial()
        => global::Program.RecordingIntegrityAutomationProjection_LivesInFocusedPartial();

    [Fact]
    public Task RecordingIntegrityFlagsAudioDiscontinuityAndDrift()
        => global::Program.RecordingIntegritySummary_FlagsAudioDiscontinuityAndDrift();

    [Fact]
    public Task RecordingIntegrityToleratesActiveInFlightFrame()
        => global::Program.RecordingIntegritySummary_ToleratesSingleActiveInFlightFrame();

    [Fact]
    public Task CaptureServiceRecordingIntegrityOwnershipLivesInFocusedPartials()
        => global::Program.CaptureService_RecordingIntegrityLivesInFocusedPartials();
}

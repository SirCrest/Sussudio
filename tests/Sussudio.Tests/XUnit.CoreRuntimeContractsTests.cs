using System;
using System.Reflection;
using System.Threading;
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
    public Task KsExtensionUnitNativeHelperIsCohesiveNativeBridge()
        => global::Program.KsExtensionUnitNative_SourceOwnership_IsCohesiveNativeBridge();

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
    public Task CaptureServiceRuntimeHdrPipelineProjectionLivesWithRuntimeSnapshotSampler()
        => global::Program.CaptureService_RuntimeHdrPipelineProjection_LivesWithRuntimeSnapshotSampler();

    [Fact]
    public Task CaptureServiceRuntimeSourceTelemetryProjectionLivesWithRuntimeSnapshotSampler()
        => global::Program.CaptureService_RuntimeSourceTelemetryProjection_LivesWithRuntimeSnapshotSampler();

    [Fact]
    public Task CaptureServiceRuntimeRecordingIntegrityProjectionLivesWithRuntimeSnapshotSampler()
        => global::Program.CaptureService_RuntimeRecordingIntegrityProjection_LivesWithRuntimeSnapshotSampler();

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
    public Task CaptureServiceObservedPixelTelemetryLivesWithSourceTelemetry()
        => global::Program.CaptureService_ObservedPixelTelemetry_LivesWithSourceTelemetry();

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

// xUnit slice for capture/telemetry policy types. Each test resolves the
// production type from the staged Sussudio.dll so the test_coverage detector
// recognizes the file as exercised.
public class CapturePoliciesTests
{
    private const string DisabledTelemetryProviderType = "Sussudio.Services.Telemetry.DisabledSourceSignalTelemetryProvider";

    [Fact]
    public void Sussudio_Services_Capture_HdrOutputPolicy_GatesOnHdrEnabledAndMode()
    {
        var asm = SussudioAssembly.Load();
        var policy = asm.GetType("Sussudio.Services.Capture.HdrOutputPolicy", throwOnError: true)!;
        var settingsType = asm.GetType("Sussudio.Models.CaptureSettings", throwOnError: true)!;
        var modeType = asm.GetType("Sussudio.Models.HdrOutputMode", throwOnError: true)!;
        var hdrEnabledProp = settingsType.GetProperty("HdrEnabled")!;
        var hdrModeProp = settingsType.GetProperty("HdrOutputMode")!;
        var isEnabled = policy.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)!;

        var disabled = Activator.CreateInstance(settingsType)!;
        hdrEnabledProp.SetValue(disabled, false);
        hdrModeProp.SetValue(disabled, Enum.Parse(modeType, "Hdr10Pq"));
        Assert.False((bool)isEnabled.Invoke(null, new object?[] { disabled })!);

        var enabledOff = Activator.CreateInstance(settingsType)!;
        hdrEnabledProp.SetValue(enabledOff, true);
        hdrModeProp.SetValue(enabledOff, Enum.Parse(modeType, "Off"));
        Assert.False((bool)isEnabled.Invoke(null, new object?[] { enabledOff })!);

        var enabledHdr10 = Activator.CreateInstance(settingsType)!;
        hdrEnabledProp.SetValue(enabledHdr10, true);
        hdrModeProp.SetValue(enabledHdr10, Enum.Parse(modeType, "Hdr10Pq"));
        using (EnvVarScope.Push("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null))
        {
            Assert.True((bool)isEnabled.Invoke(null, new object?[] { enabledHdr10 })!);
        }
    }

    [Fact]
    public void Sussudio_Services_Capture_HdrOutputPolicy_ForceOffEnvironmentSwitchDisablesOutput()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateCaptureSettings(asm, hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
        var isEnabled = GetHdrOutputPolicyIsEnabled(asm);

        using var forceOff = EnvVarScope.Push("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", "true");

        Assert.False((bool)isEnabled.Invoke(null, new object?[] { settings })!);
    }

    [Fact]
    public void Sussudio_Services_Capture_HdrOutputPolicy_IgnoresRemovedLegacyEnabledEnvironmentSwitch()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateCaptureSettings(asm, hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
        var isEnabled = GetHdrOutputPolicyIsEnabled(asm);

        using var forceOff = EnvVarScope.Push("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
        using var legacyEnabled = EnvVarScope.Push("SUSSUDIO_HDR_OUTPUT_ENABLED", "false");

        Assert.True((bool)isEnabled.Invoke(null, new object?[] { settings })!);
    }

    [Fact]
    public async Task Sussudio_Services_Telemetry_DisabledSourceSignalTelemetryProvider_ReturnsUnavailableSnapshotWithDisabledReason()
    {
        var asm = SussudioAssembly.Load();
        var providerType = asm.GetType(DisabledTelemetryProviderType, throwOnError: true)!;
        var deviceType = asm.GetType("Sussudio.Models.CaptureDevice", throwOnError: true)!;
        Assert.NotNull(deviceType);

        var provider = Activator.CreateInstance(providerType)!;
        var readAsync = providerType.GetMethod("ReadAsync")!;
        var task = (Task)readAsync.Invoke(provider, new object?[] { null, CancellationToken.None })!;
        await task;

        var resultProp = task.GetType().GetProperty("Result")!;
        var snapshot = resultProp.GetValue(task)!;
        var snapshotType = snapshot.GetType();

        var availability = snapshotType.GetProperty("Availability")!.GetValue(snapshot)!;
        Assert.Equal("Unavailable", availability.ToString());

        var summary = (string?)snapshotType.GetProperty("DiagnosticSummary")!.GetValue(snapshot);
        Assert.Equal("telemetry-provider-disabled", summary);

        var origin = snapshotType.GetProperty("OriginDetail")!.GetValue(snapshot)!;
        Assert.Equal("Unavailable", origin.ToString());
    }

    [Fact]
    public void Sussudio_Services_Telemetry_DisabledSourceSignalTelemetryProvider_HonorsCancellation()
    {
        var asm = SussudioAssembly.Load();
        var providerType = asm.GetType(DisabledTelemetryProviderType, throwOnError: true)!;
        var provider = Activator.CreateInstance(providerType)!;
        var readAsync = providerType.GetMethod("ReadAsync")!;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var thrown = Assert.Throws<TargetInvocationException>(() =>
            readAsync.Invoke(provider, new object?[] { null, cts.Token }));
        Assert.IsAssignableFrom<OperationCanceledException>(thrown.InnerException);
    }

    private static MethodInfo GetHdrOutputPolicyIsEnabled(Assembly asm)
    {
        var policy = asm.GetType("Sussudio.Services.Capture.HdrOutputPolicy", throwOnError: true)!;
        return policy.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)!;
    }

    private static object CreateCaptureSettings(Assembly asm, bool hdrEnabled, string hdrOutputMode)
    {
        var settingsType = asm.GetType("Sussudio.Models.CaptureSettings", throwOnError: true)!;
        var modeType = asm.GetType("Sussudio.Models.HdrOutputMode", throwOnError: true)!;
        var settings = Activator.CreateInstance(settingsType)!;

        settingsType.GetProperty("HdrEnabled")!.SetValue(settings, hdrEnabled);
        settingsType.GetProperty("HdrOutputMode")!.SetValue(settings, Enum.Parse(modeType, hdrOutputMode));

        return settings;
    }
}

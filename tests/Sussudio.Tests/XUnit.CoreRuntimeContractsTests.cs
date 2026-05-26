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

// Lightweight reflection-based slice for the small pure helpers under
// Sussudio.Services.Runtime. Same load-from-staged-dll approach as the other
// xUnit wrappers here because the test project targets net8.0.
public class RuntimeHelpersTests
{
    private const string AtomicMaxType = "Sussudio.Services.Runtime.AtomicMax";
    private const string TelemetryAgeHelperType = "Sussudio.Services.Runtime.TelemetryAgeHelper";
    private const string EnvironmentHelpersType = "Sussudio.Services.Runtime.EnvironmentHelpers";
    private const string RingBufferHelpersType = "Sussudio.Services.Runtime.RingBufferHelpers";

    [Fact]
    public void AtomicMax_Int_UpdatesWhenCandidateIsGreater()
    {
        var method = ResolveAtomicMaxInt();
        var args = new object[] { 3, 7 };
        method.Invoke(null, args);
        Assert.Equal(7, (int)args[0]);
    }

    [Fact]
    public void AtomicMax_Int_NoOpWhenCandidateIsLessOrEqual()
    {
        var method = ResolveAtomicMaxInt();

        var lessArgs = new object[] { 10, 3 };
        method.Invoke(null, lessArgs);
        Assert.Equal(10, (int)lessArgs[0]);

        var equalArgs = new object[] { 10, 10 };
        method.Invoke(null, equalArgs);
        Assert.Equal(10, (int)equalArgs[0]);
    }

    [Fact]
    public void AtomicMax_Long_UpdatesWhenCandidateIsGreater()
    {
        var method = ResolveAtomicMaxLong();
        var args = new object[] { 3L, 7L };
        method.Invoke(null, args);
        Assert.Equal(7L, (long)args[0]);

        var noOpArgs = new object[] { 100L, 50L };
        method.Invoke(null, noOpArgs);
        Assert.Equal(100L, (long)noOpArgs[0]);
    }

    [Fact]
    public void TelemetryAgeHelper_ReturnsNullForNullTimestamp()
    {
        var method = ResolveTimestampOverload();
        var now = DateTimeOffset.UtcNow;
        var result = method.Invoke(null, new object?[] { null, now });
        Assert.Null(result);
    }

    [Fact]
    public void TelemetryAgeHelper_FloorsPositiveAge()
    {
        var method = ResolveTimestampOverload();
        var now = new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero);
        var past = now.AddSeconds(-12.7);
        var result = (int?)method.Invoke(null, new object?[] { past, now });
        Assert.Equal(12, result);
    }

    [Fact]
    public void TelemetryAgeHelper_ClampsNegativeAgeToZero()
    {
        var method = ResolveTimestampOverload();
        var now = new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero);
        var future = now.AddSeconds(5);
        var result = (int?)method.Invoke(null, new object?[] { future, now });
        Assert.Equal(0, result);
    }

    [Fact]
    public void TelemetryAgeHelper_ReportedAgeShortCircuitsAndClamps()
    {
        var method = ResolveReportedOverload();
        var now = DateTimeOffset.UtcNow;
        var result = (int?)method.Invoke(null, new object?[] { 42, now.AddSeconds(-1), now });
        Assert.Equal(42, result);

        var negativeReported = (int?)method.Invoke(null, new object?[] { -5, null, now });
        Assert.Equal(0, negativeReported);

        var fallthrough = (int?)method.Invoke(null, new object?[] { null, now.AddSeconds(-3), now });
        Assert.Equal(3, fallthrough);
    }

    [Fact]
    public void EnvironmentHelpers_GetIntFromEnv_ReturnsDefaultWhenUnset()
    {
        var method = ResolveGetIntFromEnv();
        var name = NewEnvVarName("INT_UNSET");
        using var _ = EnvVarScope.Push(name, null);
        var result = method.Invoke(null, new object[] { name, 50, 0, 100 });
        Assert.Equal(50, (int)result!);
    }

    [Fact]
    public void EnvironmentHelpers_GetIntFromEnv_ClampsToRange()
    {
        var method = ResolveGetIntFromEnv();
        var name = NewEnvVarName("INT_CLAMP");

        using (EnvVarScope.Push(name, "200"))
        {
            Assert.Equal(100, (int)method.Invoke(null, new object[] { name, 50, 0, 100 })!);
        }

        using (EnvVarScope.Push(name, "-50"))
        {
            Assert.Equal(0, (int)method.Invoke(null, new object[] { name, 50, 0, 100 })!);
        }

        using (EnvVarScope.Push(name, "75"))
        {
            Assert.Equal(75, (int)method.Invoke(null, new object[] { name, 50, 0, 100 })!);
        }
    }

    [Fact]
    public void EnvironmentHelpers_TryGetBoolFromEnv_RecognizesTextAndIntegerForms()
    {
        var method = ResolveTryGetBoolFromEnv();
        var name = NewEnvVarName("BOOL");

        AssertBoolEnv(method, name, "true", true, true);
        AssertBoolEnv(method, name, "False", true, false);
        AssertBoolEnv(method, name, "1", true, true);
        AssertBoolEnv(method, name, "0", true, false);
        AssertBoolEnv(method, name, "not-a-bool", false, false);
        AssertBoolEnv(method, name, null, false, false);
    }

    [Fact]
    public void RingBufferHelpers_Copy_ReturnsLatestSamplesInOrder()
    {
        var method = ResolveCopyDouble();
        var window = new[] { 1.0, 2.0, 3.0, 0.0 };
        var result = (double[])method.Invoke(null, new object?[] { window, 3, 3, null })!;
        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, result);
    }

    [Fact]
    public void RingBufferHelpers_Copy_HandlesRingWraparound()
    {
        var method = ResolveCopyDouble();
        var window = new[] { 5.0, 2.0, 3.0, 4.0 };
        var result = (double[])method.Invoke(null, new object?[] { window, 4, 1, null })!;
        Assert.Equal(new[] { 2.0, 3.0, 4.0, 5.0 }, result);
    }

    [Fact]
    public void RingBufferHelpers_Copy_MaxCountCapsResult()
    {
        var method = ResolveCopyDouble();
        var window = new[] { 1.0, 2.0, 3.0, 4.0 };
        var result = (double[])method.Invoke(null, new object?[] { window, 4, 0, 2 })!;
        Assert.Equal(new[] { 3.0, 4.0 }, result);

        var empty = (double[])method.Invoke(null, new object?[] { window, 4, 0, 0 })!;
        Assert.Empty(empty);
    }

    private static MethodInfo ResolveStatic(string typeName, string methodName, Type[] signature)
    {
        var type = SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;
        return type.GetMethod(methodName, ReflectionFlags.Static, signature)!;
    }

    private static MethodInfo ResolveAtomicMaxInt()
        => ResolveStatic(AtomicMaxType, "Update", new[] { typeof(int).MakeByRefType(), typeof(int) });

    private static MethodInfo ResolveAtomicMaxLong()
        => ResolveStatic(AtomicMaxType, "Update", new[] { typeof(long).MakeByRefType(), typeof(long) });

    private static MethodInfo ResolveTimestampOverload()
        => ResolveStatic(TelemetryAgeHelperType, "ComputeAgeSeconds", new[] { typeof(DateTimeOffset?), typeof(DateTimeOffset) });

    private static MethodInfo ResolveReportedOverload()
        => ResolveStatic(TelemetryAgeHelperType, "ComputeAgeSeconds", new[] { typeof(int?), typeof(DateTimeOffset?), typeof(DateTimeOffset) });

    private static MethodInfo ResolveGetIntFromEnv()
        => ResolveStatic(EnvironmentHelpersType, "GetIntFromEnv", new[] { typeof(string), typeof(int), typeof(int), typeof(int) });

    private static MethodInfo ResolveTryGetBoolFromEnv()
        => ResolveStatic(EnvironmentHelpersType, "TryGetBoolFromEnv", new[] { typeof(string), typeof(bool).MakeByRefType() });

    private static MethodInfo ResolveCopyDouble()
        => ResolveStatic(RingBufferHelpersType, "Copy", new[] { typeof(double[]), typeof(int), typeof(int), typeof(int?) });

    private static void AssertBoolEnv(MethodInfo method, string name, string? raw, bool expectedReturn, bool expectedValue)
    {
        using var _ = EnvVarScope.Push(name, raw);
        var args = new object?[] { name, false };
        var ok = (bool)method.Invoke(null, args)!;
        Assert.Equal(expectedReturn, ok);
        Assert.Equal(expectedValue, (bool)args[1]!);
    }

    private static string NewEnvVarName(string suffix)
        => $"SUSSUDIO_TEST_{suffix}_{Guid.NewGuid():N}";
}

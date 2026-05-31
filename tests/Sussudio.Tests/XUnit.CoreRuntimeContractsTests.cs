using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public Task NativeXuTelemetryActiveReadAndRollingPollLiveInProviderRoot()
        => global::Program.NativeXuAtCommandProvider_ActiveReadAndRollingPollLiveInProviderRoot();

    [Fact]
    public Task NativeXuDeviceCommandsOwnPublicCommandSurface()
        => global::Program.NativeXuAtCommandProvider_DeviceCommandsOwnPublicCommandSurface();

    [Fact]
    public Task NativeXuRootOwnsTransportAndPayloadDecoding()
        => global::Program.NativeXuAtCommandProvider_RootOwnsTransportAndPayloadDecoding();

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

// xUnit slice for no-hardware runtime contracts ported from the legacy runner.
public sealed class RuntimeContractsTests
{
    [Fact]
    public void RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot()
    {
        var runtimePathsType = SussudioAssembly.Load().GetType("Sussudio.RuntimePaths", throwOnError: true)!;
        var getRepoLogFile = runtimePathsType.GetMethod(
            "GetRepoLogFile",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        Assert.NotNull(getRepoLogFile);

        var logPath = Assert.IsType<string>(getRepoLogFile!.Invoke(null, new object[] { "test.log" }));

        Assert.Contains("test.log", logPath);
        Assert.True(Path.IsPathRooted(logPath), $"GetRepoLogFile returned non-rooted path: {logPath}");
    }

    [Fact]
    public void RuntimePaths_PathsContainExpectedDirectoryNames()
    {
        var runtimePathsType = SussudioAssembly.Load().GetType("Sussudio.RuntimePaths", throwOnError: true)!;

        var getRepoLogRoot = runtimePathsType.GetMethod("GetRepoLogRoot", BindingFlags.Public | BindingFlags.Static);
        var getRepoTempRoot = runtimePathsType.GetMethod("GetRepoTempRoot", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(getRepoLogRoot);
        Assert.NotNull(getRepoTempRoot);

        var logRoot = Assert.IsType<string>(getRepoLogRoot!.Invoke(null, null));
        var tempRoot = Assert.IsType<string>(getRepoTempRoot!.Invoke(null, null));

        Assert.Contains("logs", logRoot);
        Assert.Contains("temp", tempRoot);
    }

    [Fact]
    public void RuntimePaths_OwnsPublicApiAndResolutionPolicy()
    {
        var rootText = RuntimeContractSource.ReadRepoFile("Sussudio/RuntimePaths.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("public static class RuntimePaths", rootText);
        Assert.DoesNotContain("partial class RuntimePaths", rootText);
        Assert.Contains("public static string GetRepoRoot() => RepoRoot.Value;", rootText);
        Assert.Contains("public static string GetRepoLogFile(string fileName)", rootText);
        Assert.Contains("private static string ResolveRepoRoot()", rootText);
        Assert.Contains("private static string ResolveLogRoot()", rootText);
        Assert.Contains("private static bool TryResolveLatestBuildParent(", rootText);
        Assert.Contains("private static bool IsRepoMarkerDirectory(", rootText);
        Assert.Contains("private static bool TryEnsureDirectory(", rootText);
        Assert.Contains("RuntimePaths: {context}, falling back:", rootText);
    }

    [Fact]
    public void MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint()
    {
        var source = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Runtime/RuntimeHelpers.cs");

        Assert.Contains("internal sealed class MmcssThreadRegistration", source);
        Assert.Contains("EntryPoint = \"AvSetMmThreadCharacteristicsW\"", source);
        Assert.Contains("MMCSS registered task=", source);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "Sussudio", "Services", "Runtime", "MmcssThreadRegistration.cs")),
            "MMCSS registration lives with shared runtime helpers");
    }

    [Fact]
    public void ProcessSpec_DefaultTimeout_Is30Seconds()
    {
        var asm = SussudioAssembly.Load();
        var specType = asm.GetType("Sussudio.Services.Runtime.ProcessSpec", throwOnError: true)!;
        var resultType = asm.GetType("Sussudio.Services.Runtime.ProcessRunResult", throwOnError: true)!;

        var spec = Activator.CreateInstance(specType)!;

        Assert.Equal(30_000, specType.GetProperty("TimeoutMs")!.GetValue(spec));
        Assert.Equal(string.Empty, specType.GetProperty("Arguments")!.GetValue(spec));
        Assert.Equal(typeof(ProcessPriorityClass?), specType.GetProperty("PriorityClass")!.PropertyType);

        Assert.NotNull(resultType.GetProperty("Started"));
        Assert.NotNull(resultType.GetProperty("TimedOut"));
        Assert.Equal(string.Empty, resultType.GetProperty("StdOut")!.GetValue(Activator.CreateInstance(resultType)!));
        Assert.Equal(string.Empty, resultType.GetProperty("StdErr")!.GetValue(Activator.CreateInstance(resultType)!));

        var sourceText = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Runtime/RuntimeHelpers.cs");
        Assert.Contains("process.PriorityClass = priorityClass;", sourceText);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "Sussudio", "Services", "Runtime", "ProcessSupervisor.cs")),
            "bounded process supervision lives with shared runtime helpers");
    }

    [Fact]
    public void ExternalProcessProbes_UseBoundedProcessSupervisor()
    {
        var ffmpegText = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        var hdrText = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("internal static class FfmpegRuntimeLocator", ffmpegText);
        Assert.DoesNotContain("partial class FfmpegRuntimeLocator", ffmpegText);
        Assert.Contains("internal static bool TryResolveNativeRuntimeRoot", ffmpegText);
        Assert.Contains("internal static unsafe class FfmpegRuntimeInit", ffmpegText);
        Assert.Contains("internal static class FfmpegLogSuppressionScope", ffmpegText);
        Assert.Contains("internal static string FindToolPath", ffmpegText);
        Assert.Contains("private const int ProbeTimeoutMs = 10_000;", ffmpegText);
        Assert.Contains("new ProcessSupervisor().RunAsync", ffmpegText);
        Assert.Contains("TimeoutMs = ProbeTimeoutMs", ffmpegText);
        Assert.Contains("if (!result.Started || result.TimedOut || result.ExitCode != 0)", ffmpegText);
        Assert.Contains("return result.Started && !result.TimedOut && result.ExitCode == 0;", ffmpegText);
        Assert.Contains("private const int ValidationTimeoutMs = 30_000;", hdrText);
        Assert.Contains("new ProcessSupervisor().RunAsync", hdrText);
        Assert.Contains("validator-timeout", hdrText);
    }

    [Fact]
    public void FfmpegRuntimeLocator_PrefersAppLocalRuntimeFolder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ec-ffmpeg-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var localFfmpegDir = Path.Combine(tempRoot, "ffmpeg");
        Directory.CreateDirectory(localFfmpegDir);

        try
        {
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avcodec-62.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avutil-60.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffmpeg.exe"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffprobe.exe"), Array.Empty<byte>());

            var locatorType = SussudioAssembly.Load().GetType("Sussudio.Services.Runtime.FfmpegRuntimeLocator", throwOnError: true)!;
            var resolveRuntime = locatorType.GetMethod(
                "TryResolveNativeRuntimeRoot",
                ReflectionFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string).MakeByRefType() },
                modifiers: null);
            Assert.NotNull(resolveRuntime);

            var runtimeArgs = new object?[] { tempRoot, null };
            var resolved = Assert.IsType<bool>(resolveRuntime!.Invoke(null, runtimeArgs));

            Assert.True(resolved);
            Assert.Equal(localFfmpegDir, runtimeArgs[1]?.ToString());

            var findToolPath = locatorType.GetMethod(
                "FindToolPath",
                ReflectionFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null);
            Assert.NotNull(findToolPath);

            var ffmpegPath = findToolPath!.Invoke(null, new object?[] { "ffmpeg.exe", tempRoot })?.ToString();
            var ffprobePath = findToolPath.Invoke(null, new object?[] { "ffprobe.exe", tempRoot })?.ToString();

            Assert.Equal(Path.Combine(localFfmpegDir, "ffmpeg.exe"), ffmpegPath);
            Assert.Equal(Path.Combine(localFfmpegDir, "ffprobe.exe"), ffprobePath);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

internal static class RuntimeContractSource
{
    public static string GetRepoRoot()
        => FindRepoRoot();

    public static string ReadRepoFile(string relativePath)
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    public static string ReadAutomationPipeClientSource()
        => ReadSourceFamily(new[]
        {
            "tools/Common/AutomationPipeClient/AutomationPipeClient.cs"
        });

    public static string ReadAutomationSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/Common/AutomationSnapshotFormatter.cs"
        });

    public static string ReadSsctlSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/ssctl/Formatters.Snapshot.cs",
        });

    public static string ReadSourceFamily(IReadOnlyList<string> files)
    {
        var parts = new string[files.Count];
        for (var i = 0; i < files.Count; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        return string.Join("\n", parts);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
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

// xUnit slice for three small contract / policy types that the legacy
// reflection runner already covers (XUnit.AutomationContractsTests.cs,
// AutomationPipeSecurityPolicy reachable via Sussudio.Automation.Contracts,
// DiagnosticThresholds covered indirectly through snapshot tests).
// Lives here so each file is reachable through the xUnit discovery path too.
public class SmallContractsTests
{
    [Fact]
    public void Sussudio_Models_AudioInputDevice_DisplayNameUsesNameOrUnknownFallback()
    {
        var asm = SussudioAssembly.Load();
        var deviceType = asm.GetType("Sussudio.Models.AudioInputDevice", throwOnError: true)!;
        var idProperty = RequireProperty(deviceType, "Id", typeof(string), canWrite: true);
        var nameProperty = RequireProperty(deviceType, "Name", typeof(string), canWrite: true);
        var displayNameProperty = RequireProperty(deviceType, "DisplayName", typeof(string), canWrite: false);
        var device = Activator.CreateInstance(deviceType)!;

        Assert.Equal(string.Empty, idProperty.GetValue(device));
        Assert.Equal(string.Empty, nameProperty.GetValue(device));
        Assert.Equal("Unknown Audio Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Audio Device", device.ToString());

        nameProperty.SetValue(device, "   ");
        Assert.Equal("Unknown Audio Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Audio Device", device.ToString());

        idProperty.SetValue(device, "audio-1");
        nameProperty.SetValue(device, "Wave Link Microphone");
        Assert.Equal("audio-1", idProperty.GetValue(device));
        Assert.Equal("Wave Link Microphone", displayNameProperty.GetValue(device));
        Assert.Equal("Wave Link Microphone", device.ToString());
    }

    [Fact]
    public void Sussudio_Models_AudioLevelEventArgs_ExposesPeakRmsAndClippedState()
    {
        var asm = SussudioAssembly.Load();
        var argsType = asm.GetType("Sussudio.Models.AudioLevelEventArgs", throwOnError: true)!;

        Assert.True(typeof(EventArgs).IsAssignableFrom(argsType));
        var peakProperty = RequireProperty(argsType, "Peak", typeof(double), canWrite: false);
        var rmsProperty = RequireProperty(argsType, "Rms", typeof(double), canWrite: false);
        var clippedProperty = RequireProperty(argsType, "Clipped", typeof(bool), canWrite: false);
        var constructor = argsType.GetConstructor(new[] { typeof(double), typeof(double), typeof(bool) })!;

        var clippedArgs = constructor.Invoke(new object[] { 0.75d, 0.25d, true });
        Assert.Equal(0.75d, peakProperty.GetValue(clippedArgs));
        Assert.Equal(0.25d, rmsProperty.GetValue(clippedArgs));
        Assert.True((bool)clippedProperty.GetValue(clippedArgs)!);

        var unclippedArgs = constructor.Invoke(new object[] { 0.1d, 0.05d, false });
        Assert.Equal(0.1d, peakProperty.GetValue(unclippedArgs));
        Assert.Equal(0.05d, rmsProperty.GetValue(unclippedArgs));
        Assert.False((bool)clippedProperty.GetValue(unclippedArgs)!);
    }

    [Fact]
    public void Sussudio_Models_CaptureDevice_DisplayNameAndDefaultsPreserveDeviceMetadata()
    {
        var asm = SussudioAssembly.Load();
        var deviceType = asm.GetType("Sussudio.Models.CaptureDevice", throwOnError: true)!;
        var mediaFormatType = asm.GetType("Sussudio.Models.MediaFormat", throwOnError: true)!;
        var supportedFormatsType = typeof(ObservableCollection<>).MakeGenericType(mediaFormatType);
        var idProperty = RequireProperty(deviceType, "Id", typeof(string), canWrite: true);
        var nameProperty = RequireProperty(deviceType, "Name", typeof(string), canWrite: true);
        var nativeXuProperty = RequireProperty(deviceType, "NativeXuInterfacePath", typeof(string), canWrite: true);
        var audioDeviceIdProperty = RequireProperty(deviceType, "AudioDeviceId", typeof(string), canWrite: true);
        var audioDeviceNameProperty = RequireProperty(deviceType, "AudioDeviceName", typeof(string), canWrite: true);
        var isHdrCapableProperty = RequireProperty(deviceType, "IsHdrCapable", typeof(bool), canWrite: true);
        var supportedFormatsProperty = RequireProperty(deviceType, "SupportedFormats", supportedFormatsType, canWrite: true);
        var displayNameProperty = RequireProperty(deviceType, "DisplayName", typeof(string), canWrite: false);
        var device = Activator.CreateInstance(deviceType)!;

        Assert.Equal(string.Empty, idProperty.GetValue(device));
        Assert.Equal(string.Empty, nameProperty.GetValue(device));
        Assert.Null(nativeXuProperty.GetValue(device));
        Assert.Null(audioDeviceIdProperty.GetValue(device));
        Assert.Null(audioDeviceNameProperty.GetValue(device));
        Assert.False((bool)isHdrCapableProperty.GetValue(device)!);
        Assert.Equal("Unknown Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Device", device.ToString());

        nameProperty.SetValue(device, "   ");
        Assert.Equal("Unknown Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Device", device.ToString());

        var supportedFormats = supportedFormatsProperty.GetValue(device)!;
        Assert.Equal(supportedFormatsType, supportedFormats.GetType());
        Assert.Empty(((IEnumerable)supportedFormats).Cast<object>());

        var secondDevice = Activator.CreateInstance(deviceType)!;
        var secondSupportedFormats = supportedFormatsProperty.GetValue(secondDevice)!;
        Assert.NotSame(supportedFormats, secondSupportedFormats);

        var format = Activator.CreateInstance(mediaFormatType)!;
        supportedFormatsType.GetMethod("Add", new[] { mediaFormatType })!.Invoke(supportedFormats, new[] { format });
        Assert.Single(((IEnumerable)supportedFormats).Cast<object>());
        Assert.Empty(((IEnumerable)secondSupportedFormats).Cast<object>());

        var replacementFormats = Activator.CreateInstance(supportedFormatsType)!;
        var replacementFormat = Activator.CreateInstance(mediaFormatType)!;
        supportedFormatsType.GetMethod("Add", new[] { mediaFormatType })!.Invoke(replacementFormats, new[] { replacementFormat });
        supportedFormatsProperty.SetValue(device, replacementFormats);
        Assert.Same(replacementFormats, supportedFormatsProperty.GetValue(device));
        Assert.Single(((IEnumerable)replacementFormats).Cast<object>());

        idProperty.SetValue(device, "device-1");
        nameProperty.SetValue(device, "Game Capture 4K X");
        nativeXuProperty.SetValue(device, @"\\?\hid#vid_0fd9");
        audioDeviceIdProperty.SetValue(device, "audio-1");
        audioDeviceNameProperty.SetValue(device, "4K X Audio");
        isHdrCapableProperty.SetValue(device, true);

        Assert.Equal("device-1", idProperty.GetValue(device));
        Assert.Equal("Game Capture 4K X", displayNameProperty.GetValue(device));
        Assert.Equal("Game Capture 4K X", device.ToString());
        Assert.Equal(@"\\?\hid#vid_0fd9", nativeXuProperty.GetValue(device));
        Assert.Equal("audio-1", audioDeviceIdProperty.GetValue(device));
        Assert.Equal("4K X Audio", audioDeviceNameProperty.GetValue(device));
        Assert.True((bool)isHdrCapableProperty.GetValue(device)!);
    }

    [Fact]
    public void Sussudio_Models_AutomationWindowAction_HasExpectedValues()
    {
        var asm = SussudioAssembly.Load();
        var enumType = asm.GetType("Sussudio.Models.AutomationWindowAction", throwOnError: true)!;
        var expectedNames = new[]
        {
            "Minimize", "Maximize", "Restore", "Close",
            "SnapLeft", "SnapRight", "SnapTopLeft", "SnapTopRight",
            "SnapBottomLeft", "SnapBottomRight", "Center", "Move", "Resize"
        };

        Assert.Equal(expectedNames, Enum.GetNames(enumType));
    }

    [Fact]
    public void Sussudio_LoggingJsonContext_ExposesSourceGeneratedTypeInfoForKnownPayloads()
    {
        var asm = SussudioAssembly.Load();
        var contextType = asm.GetType("Sussudio.LoggingJsonContext", throwOnError: true)!;

        var defaultProp = contextType.GetProperty("Default", ReflectionFlags.Static);
        Assert.NotNull(defaultProp);

        var defaultInstance = defaultProp!.GetValue(null);
        Assert.NotNull(defaultInstance);

        Assert.NotNull(contextType.GetProperty("CaptureHealthSnapshot", ReflectionFlags.Instance));
        Assert.NotNull(contextType.GetProperty("CaptureDiagnosticsSnapshot", ReflectionFlags.Instance));
    }

    [Fact]
    public void Sussudio_Services_Automation_DiagnosticThresholds_ComputesPercentSafely()
    {
        var asm = SussudioAssembly.Load();
        var type = asm.GetType("Sussudio.Services.Automation.DiagnosticThresholds", throwOnError: true)!;

        var minSamples = (int)type.GetField("RendererDropWarningMinSamples", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.Equal(120, minSamples);

        var pctConst = (double)type.GetField("RendererDropWarningPercent", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.Equal(0.25, pctConst);

        var calc = type.GetMethod("CalculatePercent", ReflectionFlags.Static, new[] { typeof(long), typeof(long) })!;

        Assert.Equal(0.0, (double)calc.Invoke(null, new object[] { 5L, 0L })!);
        Assert.Equal(25.0, (double)calc.Invoke(null, new object[] { 25L, 100L })!);
        Assert.Equal(0.0, (double)calc.Invoke(null, new object[] { -10L, 100L })!);
    }

    [Fact]
    public void Sussudio_Tools_AutomationPipeSecurityPolicy_DisablesFallbackOnlyWhenWindowsAndUnauthenticated()
    {
        // Referenced from Sussudio.Automation.Contracts so the production
        // policy is tested without linking source into the test project.

        // Non-Windows: never disable, regardless of other flags.
        AssertResult(false, isWindows: false, hasExplicitSecurityDescriptor: false, explicitSecurityFailed: true, authTokenRequired: false);
        AssertResult(false, isWindows: false, hasExplicitSecurityDescriptor: true, explicitSecurityFailed: false, authTokenRequired: false);

        // Auth required: never disable, even on Windows with no explicit descriptor.
        AssertResult(false, isWindows: true, hasExplicitSecurityDescriptor: false, explicitSecurityFailed: false, authTokenRequired: true);

        // Windows, no explicit descriptor, no auth token: disable.
        AssertResult(true, isWindows: true, hasExplicitSecurityDescriptor: false, explicitSecurityFailed: false, authTokenRequired: false);

        // Windows, explicit descriptor set but failed, no auth token: disable.
        AssertResult(true, isWindows: true, hasExplicitSecurityDescriptor: true, explicitSecurityFailed: true, authTokenRequired: false);

        // Windows, explicit descriptor set and working, no auth: do NOT disable.
        AssertResult(false, isWindows: true, hasExplicitSecurityDescriptor: true, explicitSecurityFailed: false, authTokenRequired: false);
    }

    private static void AssertResult(
        bool expected,
        bool isWindows,
        bool hasExplicitSecurityDescriptor,
        bool explicitSecurityFailed,
        bool authTokenRequired)
    {
        var actual = Sussudio.Tools.AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
            isWindows,
            hasExplicitSecurityDescriptor,
            explicitSecurityFailed,
            authTokenRequired);
        Assert.Equal(expected, actual);
    }

    private static PropertyInfo RequireProperty(Type type, string name, Type expectedType, bool canWrite)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Equal(expectedType, property!.PropertyType);
        Assert.Equal(canWrite, property.CanWrite);
        return property;
    }
}

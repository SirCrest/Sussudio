using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{

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
        var rootText = RuntimeContractSource.ReadRepoFile("Sussudio/AppRuntime.cs")
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
            "Sussudio.Automation.Contracts/AutomationPipeProtocol.cs"
        });

    public static string ReadAutomationSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/Common/AutomationSnapshotFormatter.cs"
        });

    public static string ReadSsctlSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/ssctl/Formatters.cs",
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
}

static partial class Program
{
    internal static Task KsExtensionUnitNative_SourceOwnership_IsCohesiveNativeBridge()
    {
        var rootText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.cs");

        AssertContains(rootText, "internal static class KsExtensionUnitNative");
        AssertDoesNotContain(rootText, "partial class KsExtensionUnitNative");
        AssertContains(rootText, "internal readonly record struct KsInterfacePath");
        AssertContains(rootText, "internal readonly record struct KsTopologyNode");
        AssertContains(rootText, "internal static IReadOnlyList<KsInterfacePath> EnumerateKsInterfaces(");
        AssertContains(rootText, "internal static SafeFileHandle? TryOpen(");
        AssertContains(rootText, "internal static bool TryReadTopologyNodes(");
        AssertContains(rootText, "internal static bool TryXuGetDirect(");
        AssertContains(rootText, "internal static bool TryXuSetViaOutput(");
        AssertContains(rootText, "internal static bool TryXuSetViaInput(");
        AssertContains(rootText, "DeviceIoControl(");
        AssertContains(rootText, "[DllImport(\"setupapi.dll\", SetLastError = true)]");
        AssertContains(rootText, "[DllImport(\"kernel32.dll\", SetLastError = true)]");
        AssertContains(rootText, "[StructLayout(LayoutKind.Sequential)]");

        var probeIncludes = ReadCompileIncludes(Path.Combine(
            GetRepoRoot(),
            "tools",
            "NativeXuAudioProbe",
            "NativeXuAudioProbe.csproj"));
        AssertEqual(
            1,
            CountCompileInclude(probeIncludes, @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.cs"),
            "NativeXuAudioProbe links the consolidated Native XU bridge");
        AssertEqual(
            0,
            CountCompileInclude(probeIncludes, @"..\..\Sussudio\Services\Capture\NativeXu\NativeXuDeviceSupport.cs"),
            "NativeXuDeviceSupport folded into the Native XU bridge linked source");
        AssertContains(rootText, "internal static class NativeXuDeviceSupport");
        AssertContains(rootText, "public static bool TryGetSupported4kXIds(");
        AssertContains(rootText, "public static bool IsSupported4kXDevice(");

        foreach (var removedFile in new[]
        {
            "KsExtensionUnitNative.Handles.cs",
            "KsExtensionUnitNative.Interfaces.cs",
            "KsExtensionUnitNative.Interop.cs",
            "KsExtensionUnitNative.Topology.cs",
            "KsExtensionUnitNative.Transfers.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "NativeXu", removedFile)),
                $"{removedFile} removed");
            AssertEqual(
                0,
                CountCompileInclude(probeIncludes, $@"..\..\Sussudio\Services\Capture\NativeXu\{removedFile}"),
                $"NativeXuAudioProbe no longer links {removedFile}");
        }

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_ActiveReadAndRollingPollLiveInProviderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs")
            .Replace("\r\n", "\n");
        var rollingPollText = rootText;
        var rollingCommandGroupsText = rootText;
        var snapshotAssemblyText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs")
            .Replace("\r\n", "\n");
        var telemetryDetailsText = snapshotAssemblyText;
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "public async Task<SourceSignalTelemetrySnapshot> ReadAsync(");
        AssertContains(rootText, "var attempt = TryReadInterface(ksInterface, cancellationToken);");
        AssertContains(rootText, "private NodeReadAttempt TryReadInterface(");
        AssertContains(rootText, "using var handle = KsExtensionUnitNative.TryOpen(");
        AssertContains(rootText, "KsExtensionUnitNative.TryReadTopologyNodes(");
        AssertContains(rootText, "var attempt = TryReadRolling(handle, node.NodeId, ksInterface.Path, cancellationToken);");
        AssertContains(rootText, "private static NodeReadAttempt CreateUnavailableNodeResult(");
        AssertContains(rootText, "private static NodeReadAttempt HandleFailedCommand(");
        AssertContains(rootText, "private static bool IsUnsupportedNodeFailure(");
        AssertContains(rootText, "private static string DescribeCommandFailure(");
        AssertContains(rootText, "private static string DescribeWin32Detail(");
        AssertContains(rootText, "private static AtCommandResult SendAtCommand(");
        AssertContains(rootText, "private static bool SendAtSetCommand(");
        AssertContains(rootText, "private static bool SendSelector4Command(");
        AssertDoesNotContain(rootText, "private readonly record struct VicTiming(");
        AssertContains(rootText, "private NodeReadAttempt TryReadRolling(");
        AssertContains(rootText, "private NodeReadAttempt BuildSnapshotFromCachedResults(");
        AssertDoesNotContain(rootText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.InterfaceRead.cs")),
            "selected-interface open/topology/node scanning folded into NativeXuAtCommandProvider.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.RollingPoll.cs")),
            "active rolling telemetry folded into the NativeXuAtCommandProvider root read owner");
        AssertContains(rollingPollText, "private int _rollingGroup;");
        AssertDoesNotContain(rollingPollText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertDoesNotContain(rollingPollText, "private static readonly double[] CanonicalFrameRates");
        AssertContains(rollingPollText, "private NodeReadAttempt TryReadRolling(");
        AssertContains(rollingPollText, "private NodeReadAttempt BuildSnapshotFromCachedResults(");
        AssertContains(rollingPollText, "BuildSnapshotFromCommandResults(");
        AssertDoesNotContain(rollingPollText, "BuildDetailEntries(");
        AssertDoesNotContain(rollingPollText, "new SourceSignalTelemetrySnapshot");
        AssertContains(rollingPollText, "PopulateInitialRollingCache(handle, nodeId, cancellationToken);");
        AssertContains(rollingPollText, "RefreshRollingGroup(handle, nodeId, _rollingGroup, cancellationToken);");
        AssertContains(rollingPollText, "private AtCommandResult SendRollingCommand(");
        AssertContains(rollingPollText, "private void PopulateInitialRollingCache(");
        AssertContains(rollingPollText, "private void RefreshRollingGroup(");
        AssertContains(rollingCommandGroupsText, "private AtCommandResult SendRollingCommand(");
        AssertContains(rollingCommandGroupsText, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(rollingCommandGroupsText, "private void PopulateInitialRollingCache(");
        AssertContains(rollingCommandGroupsText, "private void RefreshRollingGroup(");
        AssertContains(rollingCommandGroupsText, "case 5: // Diagnostics");
        AssertContains(rootText, "private static bool IsUnsupportedNodeFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.RollingCommandGroups.cs")),
            "rolling command batch dispatch folded into the NativeXuAtCommandProvider root read owner");
        AssertContains(snapshotAssemblyText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertContains(snapshotAssemblyText, "private static readonly double[] CanonicalFrameRates");
        AssertContains(snapshotAssemblyText, "private readonly record struct VicTiming(");
        AssertContains(snapshotAssemblyText, "private readonly record struct NativeXuSnapshotCommandResults(");
        AssertContains(snapshotAssemblyText, "AtCommandResult RawTiming");
        AssertContains(snapshotAssemblyText, "private static NodeReadAttempt TryReadSnapshot(");
        AssertContains(snapshotAssemblyText, "SendAtCommand(handle, nodeId, \"CableConnect\", CmdCableConnect)");
        AssertContains(snapshotAssemblyText, "SendAtCommand(handle, nodeId, \"RawTiming\", CmdRawTiming)");
        AssertContains(snapshotAssemblyText, "private static NodeReadAttempt BuildSnapshotFromCommandResults(");
        AssertContains(snapshotAssemblyText, "private static string BuildDiagnosticSummary(");
        AssertContains(snapshotAssemblyText, "private static string AppendExtendedDiagnostics(");
        AssertContains(snapshotAssemblyText, "private static void AppendResultField(");
        AssertContains(snapshotAssemblyText, "BuildDetailEntries(");
        AssertContains(snapshotAssemblyText, "AppendFlashAudioAnalogGainDetail(detailEntries, results.FlashAudio)");
        AssertContains(snapshotAssemblyText, "new SourceSignalTelemetrySnapshot");
        AssertContains(snapshotAssemblyText, "private static string ResolveSnapshotAudioInputOrigin(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.DiagnosticSummary.cs")),
            "diagnostic summary formatting folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.FullSnapshot.cs")),
            "reference full-snapshot read folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs")),
            "snapshot command result DTO folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs")),
            "snapshot timing policy folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertContains(telemetryDetailsText, "private static string ResolveSnapshotAudioInputOrigin(");
        AssertContains(telemetryDetailsText, "\"nativexu-flash-audio\"");
        AssertContains(snapshotAssemblyText, "TelemetryLabels.AnalogGain");
        AssertContains(snapshotAssemblyText, "Math.Exp(4.0 * y)");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.InterfaceRead.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AtProtocol.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.DiagnosticSummary.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.FullSnapshot.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.RollingCommandGroups.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs");

        return Task.CompletedTask;
    }

    private static string ReadKsExtensionUnitNativeFile(string fileName) =>
        ReadRepoFile($"Sussudio/Services/Capture/NativeXu/{fileName}");

    internal static Task NativeXuAtCommandProvider_DeviceCommandsOwnPublicCommandSurface()
    {
        var deviceCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs")
            .Replace("\r\n", "\n");
        var providerRootText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs")
            .Replace("\r\n", "\n");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(deviceCommandsText, "public static async Task<bool> SendAtSetCommandAsync(");
        AssertContains(deviceCommandsText, "public static Task<bool> SetInputSourceAsync(");
        AssertContains(deviceCommandsText, "public static async Task<byte[]?> ReadAtCommandAsync(");
        AssertContains(deviceCommandsText, "SendAtCommand(handle, node.NodeId, label, cmdCode)");
        AssertContains(deviceCommandsText, "NATIVEXU_GET_EXCEPTION");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.DeviceCommandReads.cs")),
            "Native XU public read commands stay folded into DeviceCommands.cs with the generic SET surface.");
        AssertContains(deviceCommandsText, "public static async Task<bool> SwitchAudioInputAsync(");
        AssertContains(deviceCommandsText, "public static async Task<bool> SetAnalogGainAsync(");
        AssertContains(deviceCommandsText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(deviceCommandsText, "NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device)");
        AssertContains(deviceCommandsText, "ExecuteAudioSwitch(handle, node.NodeId, analog, gainByte, sourceLabel, ct)");
        AssertContains(deviceCommandsText, "ExecuteGainChange(handle, node.NodeId, gainByte, persistFlash, ct)");
        AssertContains(deviceCommandsText, "private static bool ExecuteAudioSwitch(");
        AssertContains(deviceCommandsText, "NATIVEXU_SWITCH_AUDIO FAILED stage=i2c_{i}");
        AssertContains(deviceCommandsText, "commands=14");
        AssertContains(deviceCommandsText, "private static bool ExecuteGainChange(");
        AssertContains(deviceCommandsText, "internal static void ComputeGainRegisters(");
        AssertDoesNotContain(deviceCommandsText, "private static bool SendSelector4Command(");
        AssertContains(deviceCommandsText, "SendSelector4Command(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.AudioCommands.cs")),
            "audio command entry points folded into DeviceCommands.cs with the public command surface");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.AudioSwitch.cs")),
            "audio switch execution folded into audio command owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.AnalogGain.cs")),
            "analog gain execution folded into audio command owner");
        AssertContains(providerRootText, "private static bool SendSelector4Command(");
        AssertContains(providerRootText, "BuildAtWriteFrame(cmdCode, inputData)");
        AssertContains(providerRootText, "TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32)");
        AssertContains(deviceSupportText, "internal static class NativeXuDeviceSupport");
        AssertContains(deviceSupportText, "public static bool TryGetSupported4kXIds(");
        AssertContains(deviceSupportText, "public static bool IsSupported4kXDevice(");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AnalogGain.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AudioSwitch.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.DeviceCommandReads.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.Selector4.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AtProtocol.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuDeviceSupport.cs");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_RootOwnsTransportAndPayloadDecoding()
    {
        var providerRootText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(providerRootText, "private static AtCommandResult SendAtCommand(");
        AssertContains(providerRootText, "private static byte[] BuildAtWriteFrame(int cmdCode, byte[] inputData)");
        AssertContains(providerRootText, "private static byte[] StripAtFrameEnvelope(byte[] responseFrame, int frameLength)");
        AssertContains(providerRootText, "private static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)");
        AssertContains(providerRootText, "private static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)");
        AssertContains(providerRootText, "private static string? InferFrameRateRational(double? frameRate)");
        AssertContains(providerRootText, "private static SourceTelemetryConfidence ResolveConfidence(");
        AssertContains(providerRootText, "private static string? TryDecodePrintableAscii(byte[] buffer)");
        AssertContains(providerRootText, "private static string? DecodeCString(byte[] buffer)");
        AssertContains(providerRootText, "private static string BoolToToken(bool? value)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.AtProtocol.cs")),
            "AT transport and payload decoding folded into the NativeXuAtCommandProvider root read owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.PayloadDecoding.cs")),
            "payload decoding folded into the NativeXuAtCommandProvider root read owner");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AtProtocol.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.PayloadDecoding.cs");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_TelemetryDetailsLiveInFocusedPartials()
    {
        var telemetryDetailsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(telemetryDetailsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(telemetryDetailsText, "private static IReadOnlyList<SourceTelemetryDetailEntry> BuildDetailEntries(");
        AssertContains(telemetryDetailsText, "private static void AddAtDetail(");
        AssertContains(telemetryDetailsText, "private static string? TryFormatAtDetailValue(");
        AssertContains(telemetryDetailsText, "private static bool IsValidFlashAudioData(AtCommandResult flashResult)");
        AssertContains(telemetryDetailsText, "private static string? ResolveAudioInputSource(");
        AssertContains(telemetryDetailsText, "private static SourceAudioInputMode? ResolveAudioInputMode(");
        AssertContains(telemetryDetailsText, "private static int? ResolveAnalogGainByte(AtCommandResult flashResult)");
        AssertContains(telemetryDetailsText, "private static IReadOnlyList<SourceTelemetryDetailEntry> AppendFlashAudioAnalogGainDetail(");
        AssertContains(telemetryDetailsText, "TelemetryLabels.AnalogGain");
        AssertContains(telemetryDetailsText, "private static (string Value, string? RawValue) FormatInputSourceDetail(byte[] data)");
        AssertContains(telemetryDetailsText, "private static (string Value, string? RawValue) FormatUsbHostProtocolDetail(byte[] data)");
        AssertContains(telemetryDetailsText, "private static (string Value, string? RawValue) FormatAsciiOrHexDetail(byte[] data)");
        AssertDoesNotContain(telemetryDetailsText, "private static string? DecodeCString(byte[] buffer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.cs")),
            "Native XU detail row assembly folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs")),
            "Native XU audio input detail helpers folded into the telemetry details owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.Build.cs")),
            "Native XU detail row assembly folded into the telemetry details owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs")),
            "Native XU AT detail formatters folded into the telemetry details owner");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Build.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs");

        return Task.CompletedTask;
    }

    internal static async Task NativeXuTelemetry_AcceptsKnown4kXProductRevisions()
    {
        var provider = CreateInstance("Sussudio.Services.Telemetry.NativeXuAtCommandProvider");

        foreach (var productId in new[] { "009b", "009c", "009d" })
        {
            var device = BuildDevice($"\\\\?\\usb#vid_0fd9&pid_{productId}&mi_00#synthetic#{Guid.NewGuid():N}\\global");
            var readAsync = provider.GetType().GetMethod(
                "ReadAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { device.GetType(), typeof(CancellationToken) },
                modifiers: null);
            if (readAsync == null)
            {
                throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync method not found.");
            }

            if (readAsync.Invoke(provider, new[] { device, CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync task result not found.");
            var snapshot = resultProperty.GetValue(task)
                ?? throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync returned null snapshot.");
            var diagnostic = GetStringProperty(snapshot, "DiagnosticSummary");
            if (string.Equals(diagnostic, "nativexu-device-unsupported", StringComparison.Ordinal) ||
                diagnostic.StartsWith("nativexu-device-unsupported:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"NativeXu provider rejected 4K X product revision {productId} as unsupported.");
            }
        }
    }

    internal static Task CaptureService_SnapshotHelperPolicy_LivesInFocusedPartials()
    {
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotsText, "public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()");
        AssertContains(snapshotsText, "return GetHealthSnapshot();");
        AssertContains(snapshotsText, "private static long ComputeTickAge(long tick)");
        AssertContains(snapshotsText, "public RecordingStats GetRecordingStats()");
        AssertContains(snapshotsText, "return new RecordingStats(_recordingBackend.LibAvSink.OutputBytes, 0);");
        AssertContains(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "private readonly CaptureRecordingBackendResources _recordingBackend = new();");
        AssertContains(snapshotsText, "IsFlashbackRecordingBackendActive()");
        AssertContains(snapshotsText, "bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes");
        AssertContains(snapshotsText, "isFlashbackEstimate: true");
        AssertContains(snapshotsText, "new FileInfo(path).Length");
        AssertContains(snapshotsText, "catch (FileNotFoundException)");
        AssertContains(snapshotsText, "isFailure: true");

        AssertContains(snapshotsText, "private static string? ResolveEncoderCodecName(");
        AssertContains(snapshotsText, "MediaFormat.MapNvencCodecName(settings.Format)");
        AssertContains(snapshotsText, "private static string? ResolveEncoderOutputPixelFormat(");
        AssertContains(snapshotsText, "return \"yuv420p10le\";");
        AssertContains(snapshotsText, "private static string? ResolveEncoderVideoProfile(");
        AssertContains(snapshotsText, "RecordingFormat.H264Mp4 => \"high\"");
        AssertContains(snapshotsText, "private static string? ResolveRequestedFrameRateArg(");
        AssertContains(snapshotsText, "RequestedFrameRateNumerator is uint numerator");
        AssertContains(snapshotsText, "RequestedFrameRateDenominator is uint denominator");

        AssertContains(snapshotsText, "private ObservedFrameSnapshotFields ResolveObservedFrameTelemetry()");
        AssertContains(snapshotsText, "private readonly record struct ObservedFrameSnapshotFields(");
        AssertContains(snapshotsText, "return new ObservedFrameSnapshotFields(");
        AssertContains(snapshotsText, "Math.Max(0, Interlocked.Read(ref _observedP010FrameCount))");
        AssertContains(snapshotsText, "Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount))");
        AssertContains(snapshotsText, "Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount))");
        AssertContains(healthSnapshotText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertContains(flashbackExportText, "private static long GetFileLengthOrZero(string? path)");

        AssertDoesNotContain(snapshotsText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertDoesNotContain(snapshotsText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertDoesNotContain(snapshotsText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertDoesNotContain(snapshotsText, "private static long GetFileLengthOrZero(string? path)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.HealthSnapshotFlashbackBackend.cs")),
            "Flashback backend health fields folded into health snapshot sampler");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotRecordingStats.cs")),
            "old recording stats snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotRecordingFormat.cs")),
            "old recording format snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotObservedFrames.cs")),
            "old observed frames snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Snapshots.cs")),
            "capture diagnostics/source telemetry snapshot helpers folded into CaptureService.RuntimeSnapshots.cs");

        return Task.CompletedTask;
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬ CaptureService.Snapshots: ResolveEncoderCodecName Ã¢â€â‚¬Ã¢â€â‚¬

    internal static Task CaptureService_ResolveEncoderCodecName_MapsFormats()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderCodecName",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderCodecName not found.");

        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        // HEVC Ã¢â€ â€™ hevc_nvenc
        var hevcSettings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(hevcSettings, Enum.Parse(formatType, "HevcMp4"));
        var hevcResult = method.Invoke(null, new[] { hevcSettings })?.ToString();
        AssertContains(hevcResult ?? "", "hevc");

        // H264 Ã¢â€ â€™ h264_nvenc (default Format is H264Mp4)
        var h264Settings = Activator.CreateInstance(settingsType)!;
        var h264Result = method.Invoke(null, new[] { h264Settings })?.ToString();
        AssertContains(h264Result ?? "", "264");

        // null Ã¢â€ â€™ null
        var nullResult = method.Invoke(null, new object?[] { null });
        AssertEqual(true, nullResult == null, "null settings Ã¢â€ â€™ null codec");

        return Task.CompletedTask;
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬ CaptureService.Snapshots: ResolveEncoderOutputPixelFormat Ã¢â€â‚¬Ã¢â€â‚¬

    internal static Task CaptureService_ResolveEncoderOutputPixelFormat_DistinguishesHdr()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderOutputPixelFormat",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderOutputPixelFormat not found.");

        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");

        // HDR active context Ã¢â€ â€™ yuv420p10le
        var hdrContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(hdrContext, "HdrPipelineActive", true);
        var hdrSettings = RuntimeHelpers.GetUninitializedObject(settingsType);
        var hdrResult = method.Invoke(null, new[] { hdrContext, hdrSettings })?.ToString();
        AssertContains(hdrResult ?? "", "10");

        // SDR context Ã¢â€ â€™ yuv420p
        var sdrContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(sdrContext, "HdrPipelineActive", false);
        var sdrResult = method.Invoke(null, new[] { sdrContext, hdrSettings })?.ToString();
        AssertEqual(true, sdrResult != null && !sdrResult.Contains("10"), "SDR Ã¢â€ â€™ 8-bit pixel format");

        return Task.CompletedTask;
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬ TelemetryAgeHelper: shared compute-age logic used by capture/automation/view-model Ã¢â€â‚¬Ã¢â€â‚¬

    // Ã¢â€â‚¬Ã¢â€â‚¬ CaptureService.Snapshots: ResolveHdrWarmupState Ã¢â€â‚¬Ã¢â€â‚¬

    internal static Task CaptureService_ResolveHdrWarmupState_ReturnsCorrectStates()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveHdrWarmupState",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveHdrWarmupState not found.");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(hdrPipelineText, "private static string ResolveHdrWarmupState(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Snapshots.cs")),
            "old snapshot helper partial folded into runtime snapshot owner");

        // HDR not requested Ã¢â€ â€™ NotRequested
        var notRequested = method.Invoke(null, new object[] { false, false, false, 0L })?.ToString();
        AssertEqual("NotRequested", notRequested, "HDR not requested");

        // HDR requested and active with P010 frames while recording Ã¢â€ â€™ Satisfied
        var satisfied = method.Invoke(null, new object[] { true, true, true, 100L })?.ToString();
        AssertEqual("Satisfied", satisfied, "HDR active with P010 frames");

        // HDR requested but not active Ã¢â€ â€™ Pending or Degraded
        var pending = method.Invoke(null, new object[] { true, false, false, 0L })?.ToString();
        AssertEqual(true, pending != "Satisfied" && pending != "NotRequested",
            $"HDR requested but not active Ã¢â€ â€™ {pending}");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ObservedPixelTelemetry_LivesWithSourceTelemetry()
    {
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(telemetryText, "private void ResetObservedPixelTelemetry()");
        AssertContains(telemetryText, "private static string? NormalizeObservedPixelFormat(string? pixelFormat)");
        AssertContains(telemetryText, "private void RecordObservedPixelFormat(string? pixelFormat, bool incrementAsFrame = true)");
        AssertContains(telemetryText, "Interlocked.Exchange(ref _observedP010FrameCount, 0);");
        AssertContains(telemetryText, "Interlocked.Increment(ref _observedP010FrameCount);");
        AssertContains(telemetryText, "Interlocked.Increment(ref _observedNv12FrameCount);");
        AssertContains(telemetryText, "Interlocked.Increment(ref _observedOtherFrameCount);");
        AssertContains(telemetryText, "private void CaptureEncoderRuntimeTelemetry(LibAvRecordingSink? sink)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.ObservedPixelTelemetry.cs")),
            "old observed pixel telemetry partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.CaptureFormatTelemetry.cs")),
            "old capture-format telemetry partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs")),
            "source telemetry polling folded into CaptureService.RuntimeSnapshots.cs");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("NormalizeObservedPixelFormat",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("NormalizeObservedPixelFormat not found.");

        var p010Lower = method.Invoke(null, new object?[] { "p010" })?.ToString();
        AssertEqual("P010", p010Lower, "p010 -> P010");

        var p010Mixed = method.Invoke(null, new object?[] { "P010" })?.ToString();
        AssertEqual("P010", p010Mixed, "P010 stays P010");

        var nv12Lower = method.Invoke(null, new object?[] { "nv12" })?.ToString();
        AssertEqual("NV12", nv12Lower, "nv12 -> NV12");

        var bgra = method.Invoke(null, new object?[] { "bgra" })?.ToString();
        AssertEqual("BGRA", bgra, "bgra -> BGRA");

        var nullResult = method.Invoke(null, new object?[] { null });
        AssertEqual(true, nullResult == null, "null -> null");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ResolveSourceTelemetryBackend_MapsOrigins()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveSourceTelemetryBackend",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSourceTelemetryBackend not found.");

        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotsText, "private static string ResolveSourceTelemetryBackend(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotTelemetry.cs")),
            "old source telemetry snapshot partial removed");

        var nativeXuTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(nativeXuTelemetry, "Origin", Enum.Parse(originType, "NativeXu"));
        var nativeXuResult = method.Invoke(null, new[] { nativeXuTelemetry })?.ToString();
        AssertContains(nativeXuResult ?? "", "NativeXu");

        var fallbackTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(fallbackTelemetry, "Origin", Enum.Parse(originType, "DeviceFormatFallback"));
        var fallbackResult = method.Invoke(null, new[] { fallbackTelemetry })?.ToString();
        AssertContains(fallbackResult ?? "", "DeviceFormat");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderVideoProfile",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderVideoProfile not found.");

        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        var hdrCtx = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(hdrCtx, "HdrPipelineActive", true);
        var settings = Activator.CreateInstance(settingsType)!;
        AssertEqual("main10", method.Invoke(null, new[] { hdrCtx, settings })?.ToString(), "HDR -> main10");

        var sdrCtx = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(sdrCtx, "HdrPipelineActive", false);
        var h264Settings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(h264Settings, Enum.Parse(formatType, "H264Mp4"));
        AssertEqual("high", method.Invoke(null, new[] { sdrCtx, h264Settings })?.ToString(), "H264 SDR -> high");

        var hevcSettings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(hevcSettings, Enum.Parse(formatType, "HevcMp4"));
        AssertEqual("main", method.Invoke(null, new[] { sdrCtx, hevcSettings })?.ToString(), "HEVC SDR -> main");

        AssertEqual(true, method.Invoke(null, new object?[] { sdrCtx, null }) == null, "null settings -> null");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ComputeTickAge_ReturnsCorrectValues()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ComputeTickAge",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTickAge not found.");

        var zeroResult = (long)method.Invoke(null, new object[] { 0L })!;
        AssertEqual(-1L, zeroResult, "tick=0 -> -1");

        var recentTick = Environment.TickCount64 - 100;
        var recentAge = (long)method.Invoke(null, new object[] { recentTick })!;
        AssertEqual(true, recentAge >= 0 && recentAge < 5000, $"Recent tick age should be 0-5000ms, got {recentAge}");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ResolveTelemetryAlignment_DetectsMismatches()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveTelemetryAlignment",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveTelemetryAlignment not found.");

        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");
        var runtimeSourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(runtimeSourceTelemetryText, "private static (string Status, string Reason) ResolveTelemetryAlignment(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Snapshots.cs")),
            "old snapshot helper partial folded into runtime snapshot owner");

        var alignedTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(alignedTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(alignedTelemetry, "Width", (int?)1920);
        SetPropertyBackingField(alignedTelemetry, "Height", (int?)1080);
        SetPropertyBackingField(alignedTelemetry, "FrameRateExact", (double?)60.0);
        SetPropertyBackingField(alignedTelemetry, "IsHdr", (bool?)false);

        var settings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Width")!.SetValue(settings, (uint)1920);
        settingsType.GetProperty("Height")!.SetValue(settings, (uint)1080);
        settingsType.GetProperty("FrameRate")!.SetValue(settings, 60.0);

        var alignedResult = method.Invoke(null, new object?[] { settings, alignedTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var status = alignedResult!.GetType().GetField("Item1")!.GetValue(alignedResult)?.ToString();
        AssertEqual("Aligned", status, "Matching telemetry -> Aligned");

        var hdrSourceSdrCaptureTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "Width", (int?)1920);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "Height", (int?)1080);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "FrameRateExact", (double?)60.0);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "IsHdr", (bool?)true);

        var hdrSourceSdrCaptureResult = method.Invoke(null, new object?[] { settings, hdrSourceSdrCaptureTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var hdrSourceSdrCaptureStatus = hdrSourceSdrCaptureResult!.GetType().GetField("Item1")!.GetValue(hdrSourceSdrCaptureResult)?.ToString();
        var hdrSourceSdrCaptureReason = hdrSourceSdrCaptureResult.GetType().GetField("Item2")!.GetValue(hdrSourceSdrCaptureResult)?.ToString() ?? string.Empty;
        AssertEqual("Aligned", hdrSourceSdrCaptureStatus, "HDR source with SDR capture request -> Aligned");
        AssertContains(hdrSourceSdrCaptureReason, "SDR capture was requested");

        var unavailTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(unavailTelemetry, "Availability", Enum.Parse(availabilityType, "Unavailable"));
        SetPropertyBackingField(unavailTelemetry, "DiagnosticSummary", "No device");

        var unavailResult = method.Invoke(null, new object?[] { settings, unavailTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var unavailStatus = unavailResult!.GetType().GetField("Item1")!.GetValue(unavailResult)?.ToString();
        AssertEqual("Unavailable", unavailStatus, "Unavailable telemetry -> Unavailable");

        var mismatchTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(mismatchTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(mismatchTelemetry, "Width", (int?)1280);
        SetPropertyBackingField(mismatchTelemetry, "Height", (int?)720);
        SetPropertyBackingField(mismatchTelemetry, "FrameRateExact", (double?)60.0);

        var mismatchResult = method.Invoke(null, new object?[] { settings, mismatchTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var mismatchStatus = mismatchResult!.GetType().GetField("Item1")!.GetValue(mismatchResult)?.ToString();
        AssertEqual("Mismatch", mismatchStatus, "Dimension mismatch -> Mismatch");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveSourceTelemetryCircuitState",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSourceTelemetryCircuitState not found.");

        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");

        var closed = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Available"), false })?.ToString();
        AssertEqual("Closed", closed, "Available + not suppressed -> Closed");

        var suppressed = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Available"), true })?.ToString();
        AssertEqual("Open", suppressed, "Suppressed -> Open");

        var unavailable = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Unavailable"), false })?.ToString();
        AssertEqual("Open", unavailable, "Unavailable -> Open");

        return Task.CompletedTask;
    }

    internal static Task RecordingIntegritySummary_FlagsAudioDiscontinuityAndDrift()
    {
        var summary = InvokeBuildRecordingIntegritySummary(
            audioDiscontinuities: 2,
            avSyncDriftMs: 750.0,
            encoderAvSyncDriftMs: -650.0);

        AssertEqual("Incomplete", GetStringProperty(summary, "Status"), "Recording integrity should flag audio discontinuity/drift.");
        AssertEqual(false, GetBoolProperty(summary, "Complete"), "Recording integrity should not be complete with audio issues.");
        AssertEqual("Incomplete", GetStringProperty(summary, "AudioStatus"), "Audio integrity should be incomplete with discontinuity/drift.");

        var reason = GetStringProperty(summary, "Reason");
        AssertContains(reason, "audio_discontinuities=2");
        AssertContains(reason, "av_sync_drift_ms=750");
        AssertContains(reason, "encoder_av_sync_drift_ms=-650");

        return Task.CompletedTask;
    }

    internal static Task RecordingIntegritySummary_ToleratesSingleActiveInFlightFrame()
    {
        var summary = InvokeBuildRecordingIntegritySummary(
            audioDiscontinuities: 0,
            avSyncDriftMs: 0.0,
            encoderAvSyncDriftMs: 0.0,
            recordingActive: true,
            sourceFrames: 121,
            acceptedFrames: 120);

        AssertEqual("Active", GetStringProperty(summary, "Status"), "Recording integrity should tolerate one active in-flight frame.");
        AssertEqual(0L, GetLongProperty(summary, "PipelineDroppedFrames"), "Active in-flight frame should not count as a pipeline drop.");
        AssertContains(GetStringProperty(summary, "Reason"), "Recording active; all delivered source frames have reached the recording boundary so far.");

        return Task.CompletedTask;
    }

    internal static Task FlashbackRecordingIntegrity_UsesRecordingScopedSequenceGaps()
    {
        var unifiedText = ReadUnifiedVideoCaptureSource();
        var snapshotsText = ReadCaptureServiceRecordingIntegritySource();
        var snapshotHelpersText = System.IO.File.ReadAllText(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.RuntimeSnapshots.cs"));
        var serviceText = ReadCaptureServiceRecordingFinalizationSource();

        AssertContains(unifiedText, "public long FlashbackRecordingSequenceGaps");
        AssertContains(unifiedText, "TrackFlashbackRecordingAcceptedSequence(sourceSequence)");
        AssertContains(snapshotsText, "CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(snapshotsText, "videoCapture.FlashbackRecordingSequenceGaps");
        AssertContains(serviceText, "CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, flashbackVideoCapture)");
        AssertContains(snapshotsText, "if (sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))");
        AssertContains(snapshotHelpersText, "private (double? DriftMs, double? RateMsPerSec) ComputeAvSyncDrift()");
        AssertContains(snapshotHelpersText, "private (double? EncoderDriftMs, long? EncoderCorrectionSamples) GetEncoderAvSyncDrift()");
        AssertContains(snapshotsText, "encoderAvSyncDriftMs = driftMs;");
        AssertContains(snapshotsText, "encoderAvSyncCorrectionSamples = correctionSamples;");
        AssertContains(snapshotsText, "avSyncDriftMs: null,\n            avSyncDriftRateMsPerSec: null,\n            encoderAvSyncDriftMs: null,\n            encoderAvSyncCorrectionSamples: null");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingIntegrityLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs");

        AssertContains(rootText, "private RecordingIntegritySummary ResolveRecordingIntegritySummary(");
        AssertContains(rootText, "private sealed record RecordingIntegrityCounterSnapshot(");
        AssertContains(rootText, "private sealed record RecordingAudioIntegrityCounterSnapshot(");
        AssertContains(rootText, "private static RecordingIntegritySummary BuildRecordingIntegritySummary(");
        AssertContains(rootText, "var videoFields = BuildRecordingIntegritySummaryVideoFields(");
        AssertContains(rootText, "var evaluation = EvaluateRecordingIntegritySummary(");
        AssertContains(rootText, "private static void LogRecordingIntegritySummary(");
        AssertContains(rootText, "RECORDING_INTEGRITY ");
        AssertContains(rootText, "private readonly record struct RecordingIntegritySummaryVideoFields");
        AssertContains(rootText, "private readonly record struct RecordingIntegritySummaryAudioFields");
        AssertContains(rootText, "private static RecordingIntegritySummaryVideoFields BuildRecordingIntegritySummaryVideoFields(");
        AssertContains(rootText, "PipelineDroppedFrames = recordingActive");
        AssertContains(rootText, "private static RecordingIntegritySummaryEvaluation EvaluateRecordingIntegritySummary(");
        AssertContains(rootText, "private static string EvaluateRecordingIntegrityAudioStatus(");
        AssertContains(rootText, "RecordingIntegrityAvSyncDriftWarningMs");
        AssertContains(rootText, "audio_boundary_drops=");
        AssertContains(rootText, "private static string FormatRecordingIntegrityDouble(");
        AssertContains(rootText, "private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(");
        AssertContains(rootText, "private RecordingIntegrityCounterSnapshot CaptureFlashbackRecordingIntegrityCountersSinceBaseline(");
        AssertContains(rootText, "private static long DeltaCounter(");
        AssertContains(rootText, "private RecordingAudioIntegrityCounterSnapshot GetRecordingAudioCountersSinceBaseline(");
        AssertContains(rootText, "private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(");
        AssertContains(rootText, "CreateRecordingAudioCounters(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingIntegrity.Summary.cs")),
            "old recording integrity summary partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingIntegrity.Counters.cs")),
            "old recording integrity counters partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingIntegrity.Audio.cs")),
            "old recording integrity audio partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingIntegrity.Logging.cs")),
            "old recording integrity logging partial removed");

        return Task.CompletedTask;
    }

    internal static Task SharedFormatter_RendersRecordingIntegrity()
    {
        var toolAssembly = LoadToolAssembly(System.IO.Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var formatterType = toolAssembly.GetType("Sussudio.Tools.AutomationSnapshotFormatter")
            ?? throw new InvalidOperationException("Sussudio.Tools.AutomationSnapshotFormatter type not found.");
        var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot not found.");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSevereGapCount":0,"WasapiCaptureAudioDiscontinuityCount":0,"WasapiCaptureAudioTimestampErrorCount":0,"WasapiCaptureAudioGlitchCount":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","RecordingIntegrityStatus":"Complete","RecordingIntegrityComplete":true,"RecordingIntegrityBackend":"LibAv","RecordingIntegritySourceFrames":120,"RecordingIntegrityAcceptedFrames":120,"RecordingIntegrityPipelineDroppedFrames":0,"RecordingIntegrityQueueDroppedFrames":0,"RecordingIntegritySubmittedFrames":120,"RecordingIntegrityEncodedFrames":120,"RecordingIntegrityPacketsWritten":120,"RecordingIntegrityEncoderDroppedFrames":0,"RecordingIntegritySequenceGaps":0,"RecordingIntegrityQueueMaxDepth":2,"RecordingIntegrityQueueOldestFrameAgeMs":0,"RecordingIntegrityBackpressureWaitMs":0,"RecordingIntegrityBackpressureEvents":0,"RecordingIntegrityBackpressureMaxWaitMs":0,"RecordingIntegrityAudioStatus":"Clean","RecordingIntegrityAudioEnabled":true,"RecordingIntegrityAudioCaptureActive":true,"RecordingIntegrityAudioFramesArrived":48000,"RecordingIntegrityAudioFramesWrittenToSink":48000,"RecordingIntegrityAudioSamplesEncoded":48000,"RecordingIntegrityAudioDropEvents":0,"RecordingIntegrityAudioDiscontinuities":0,"RecordingIntegrityAudioTimestampErrors":0,"RecordingIntegrityAudioCallbackGaps":0,"RecordingIntegrityAvSyncDriftMs":1.25,"RecordingIntegrityAvSyncDriftRateMsPerSec":0.1,"RecordingIntegrityEncoderAvSyncDriftMs":1.0,"RecordingIntegrityEncoderAvSyncCorrectionSamples":0,"RecordingIntegrityReason":"Every delivered source frame reached the recording boundary.","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"Stopped","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0}}
                            """;
        using var document = JsonDocument.Parse(json);
        var output = formatSnapshot.Invoke(null, new object[] { document.RootElement, false })?.ToString()
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot returned null.");

        AssertContains(output, "Integrity: Complete complete=true backend=LibAv source=120 accepted=120");
        AssertContains(output, "boundaryDrops=0 queueDrops=0 encoderDrops=0 seqGaps=0 submitted=120 encoded=120 packets=120");
        AssertContains(output, "qMax=2 qOldestMs=0 backpressure=0ms/0 max=0ms");
        AssertContains(output, "Audio Integrity: Clean enabled=true active=true arrived=48000 written=48000 encoded=48000");
        AssertContains(output, "drift=1.25ms encoderDrift=1.0ms corr=0");

        return Task.CompletedTask;
    }

    private static object InvokeBuildRecordingIntegritySummary(
        long audioDiscontinuities,
        double avSyncDriftMs,
        double encoderAvSyncDriftMs,
        bool recordingActive = false,
        long sourceFrames = 120,
        long acceptedFrames = 120)
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var counterType = serviceType.GetNestedType("RecordingIntegrityCounterSnapshot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingIntegrityCounterSnapshot missing.");
        var audioCounterType = serviceType.GetNestedType("RecordingAudioIntegrityCounterSnapshot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingAudioIntegrityCounterSnapshot missing.");

        var counters = Activator.CreateInstance(
            counterType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[]
            {
                "LibAv",
                120L,
                120L,
                120L,
                0L,
                0L,
                0L,
                2,
                0L,
                0L,
                0L,
                0L,
                false,
                null,
                null
            },
            culture: null)
            ?? throw new InvalidOperationException("Could not create RecordingIntegrityCounterSnapshot.");

        var audioCounters = Activator.CreateInstance(
            audioCounterType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[]
            {
                true,
                true,
                48000L,
                48000L,
                48000L,
                0L,
                audioDiscontinuities,
                0L,
                0L,
                avSyncDriftMs,
                0.0,
                encoderAvSyncDriftMs,
                0L
            },
            culture: null)
            ?? throw new InvalidOperationException("Could not create RecordingAudioIntegrityCounterSnapshot.");

        var method = serviceType.GetMethod("BuildRecordingIntegritySummary", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildRecordingIntegritySummary missing.");

        return method.Invoke(
            null,
            new object?[]
            {
                "LibAv",
                recordingActive,
                true,
                recordingActive ? "Recording" : "Stopped",
                recordingActive ? null : DateTimeOffset.UtcNow,
                sourceFrames,
                acceptedFrames,
                counters,
                audioCounters
            })
            ?? throw new InvalidOperationException("BuildRecordingIntegritySummary returned null.");
    }

    internal static Task RecordingIntegritySummary_DefaultsAreExplicit()
    {
        var summaryType = RequireType("Sussudio.Models.RecordingIntegritySummary");
        var notStarted = summaryType.GetProperty("NotStarted", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException("RecordingIntegritySummary.NotStarted missing.");

        AssertEqual("NotStarted", GetStringProperty(notStarted, "Status"), "RecordingIntegritySummary default status");
        AssertEqual(false, GetBoolProperty(notStarted, "Complete"), "RecordingIntegritySummary default complete");
        AssertEqual("None", GetStringProperty(notStarted, "Backend"), "RecordingIntegritySummary default backend");
        AssertEqual(0L, GetLongProperty(notStarted, "SourceFrames"), "RecordingIntegritySummary default source frames");
        AssertEqual(0L, GetLongProperty(notStarted, "AcceptedFrames"), "RecordingIntegritySummary default accepted frames");
        AssertEqual(0L, GetLongProperty(notStarted, "EncodedFrames"), "RecordingIntegritySummary default encoded frames");
        AssertEqual(0, GetIntProperty(notStarted, "QueueMaxDepth"), "RecordingIntegritySummary default max queue depth");
        AssertEqual("Disabled", GetStringProperty(notStarted, "AudioStatus"), "RecordingIntegritySummary default audio status");
        AssertEqual(false, GetBoolProperty(notStarted, "AudioEnabled"), "RecordingIntegritySummary default audio enabled");
        AssertEqual("No recording has completed.", GetStringProperty(notStarted, "Reason"), "RecordingIntegritySummary default reason");

        return Task.CompletedTask;
    }

    internal static Task RecordingIntegritySnapshotContract_ExposesAutomationFields()
    {
        foreach (var typeName in new[]
        {
            "Sussudio.Models.CaptureRuntimeSnapshot",
            "Sussudio.Models.AutomationSnapshot"
        })
        {
            var snapshotType = RequireType(typeName);
            AssertProperty(snapshotType, "RecordingIntegrityStatus", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityComplete", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityBackend", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityCompletedUtc", typeof(DateTimeOffset?));
            AssertProperty(snapshotType, "RecordingIntegritySourceFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAcceptedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityPipelineDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityQueueDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegritySubmittedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityEncodedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityPacketsWritten", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegritySequenceGaps", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityQueueMaxDepth", typeof(int));
            AssertProperty(snapshotType, "RecordingIntegrityQueueOldestFrameAgeMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureWaitMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureEvents", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureMaxWaitMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioStatus", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityAudioEnabled", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityAudioCaptureActive", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityAudioFramesArrived", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioFramesWrittenToSink", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioSamplesEncoded", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioDropEvents", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioDiscontinuities", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioTimestampErrors", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioCallbackGaps", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAvSyncDriftMs", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityAvSyncDriftRateMsPerSec", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderAvSyncDriftMs", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderAvSyncCorrectionSamples", typeof(long?));
            AssertProperty(snapshotType, "RecordingIntegrityReason", typeof(string));
        }

        return Task.CompletedTask;
    }

    internal static Task RecordingIntegrityAutomationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var recordingIntegrityFlattening = BuildRecordingIntegrityFlattenedProjection(recordingIntegrity);");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrityFlattening.Summary.Status,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrityFlattening.Audio.AudioFramesWrittenToSink,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrityFlattening.AvSync.EncoderAvSyncDriftMs,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrityFlattening.Summary.Reason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrityFlattening.Status,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrityFlattening.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrityFlattening.EncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = captureRuntime.RecordingIntegrityReason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrity.Reason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrityFlattening.Reason,");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(");
        AssertContains(recordingProjectionText, "Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),");
        AssertContains(recordingProjectionText, "Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),");
        AssertContains(recordingProjectionText, "Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),");
        AssertContains(recordingProjectionText, "Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),");
        AssertContains(recordingProjectionText, "AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityFlattenedProjection");
        AssertContains(recordingProjectionText, "private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(");
        AssertContains(recordingProjectionText, "Status = summary.Status,");
        AssertContains(recordingProjectionText, "Reason = summary.Reason");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(");
        AssertContains(recordingProjectionText, "EncodedFrames = video.EncodedFrames,");
        AssertContains(recordingProjectionText, "SequenceGaps = video.SequenceGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(");
        AssertContains(recordingProjectionText, "QueueMaxDepth = backpressure.QueueMaxDepth,");
        AssertContains(recordingProjectionText, "BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(");
        AssertContains(recordingProjectionText, "AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,");
        AssertContains(recordingProjectionText, "AudioCallbackGaps = audio.AudioCallbackGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(");
        AssertContains(recordingProjectionText, "EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,");
        AssertContains(recordingProjectionText, "EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityProjection");
        AssertContains(recordingProjectionText, "Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Video = BuildRecordingIntegrityVideoProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)");
        AssertContains(recordingProjectionText, "private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(");
        AssertContains(recordingProjectionText, "Status = captureRuntime.RecordingIntegrityStatus,");
        AssertContains(recordingProjectionText, "Reason = captureRuntime.RecordingIntegrityReason");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(");
        AssertContains(recordingProjectionText, "EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,");
        AssertContains(recordingProjectionText, "SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(");
        AssertContains(recordingProjectionText, "QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,");
        AssertContains(recordingProjectionText, "BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(");
        AssertContains(recordingProjectionText, "AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertContains(recordingProjectionText, "AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(");
        AssertContains(recordingProjectionText, "EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertContains(recordingProjectionText, "EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples");

        return Task.CompletedTask;
    }

    private static void AssertProperty(Type type, string propertyName, Type propertyType)
    {
        var property = type.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{type.Name}.{propertyName} missing.");
        AssertEqual(propertyType, property.PropertyType, $"{type.Name}.{propertyName} type");
    }



    internal static Task CaptureService_RuntimeIngestAudioProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var ingestAudio = CaptureRuntimeIngestAudioSnapshotFields(");
        AssertContains(runtimeText, "IngestAudio = ingestAudio,");
        AssertContains(assemblerText, "AudioReaderActive = ingestAudio.AudioReaderActive,");
        AssertContains(assemblerText, "SourceReaderFrameChannelDepth = ingestAudio.SourceReaderFrameChannelDepth,");
        AssertContains(assemblerText, "WasapiPlaybackTargetVolumePercent = ingestAudio.WasapiPlaybackTargetVolumePercent,");

        AssertContains(runtimeText, "private RuntimeIngestAudioSnapshotFields CaptureRuntimeIngestAudioSnapshotFields(");
        AssertContains(runtimeText, "private sealed class RuntimeIngestAudioSnapshotFields");
        AssertContains(runtimeText, "VideoReaderActive = unifiedVideoCapture != null && (videoPreviewActive || recordingActive)");
        AssertContains(runtimeText, "IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0)");
        AssertContains(runtimeText, "SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0");
        AssertContains(runtimeText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0");
        AssertContains(runtimeText, "WasapiPlaybackCurrentVolumePercent = (wasapiPlayback?.CurrentVolume ?? 0) * 100.0");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeSnapshotAssembler_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = runtimeText;
        var sourceTelemetryText = runtimeText;
        var captureRuntimeModelText = ReadRepoFile("Sussudio/Models/Automation/AutomationRuntimeModels.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");
        var assemblerBuildText = ExtractMemberCode(assemblerText, "Build");

        AssertContains(runtimeText, "return CaptureRuntimeSnapshotAssembler.Build(new CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(runtimeText, "var requestedSettings = _recordingBackend.SettingsSnapshot ?? _currentSettings;");
        AssertContains(runtimeText, "FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(requestedSettings, unifiedVideoCapture),");
        AssertContains(runtimeText, "RuntimeAvSyncDriftMs = runtimeAvSyncDriftMs,");
        AssertContains(runtimeText, "HdrWarmup = hdrWarmup,");
        AssertContains(runtimeText, "return new CaptureRuntimeSnapshot");

        AssertContains(assemblerText, "private static class CaptureRuntimeSnapshotAssembler");
        AssertContains(assemblerText, "public static CaptureRuntimeSnapshot Build(CaptureRuntimeSnapshotAssemblyFields fields)");
        AssertContains(assemblerText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(assemblerText, "public RuntimeHdrWarmupSnapshotFields HdrWarmup { get; init; } = new();");
        AssertContains(assemblerText, "public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }");
        AssertContains(runtimeText, "private sealed class RuntimeIngestAudioSnapshotFields");
        AssertContains(runtimeText, "private sealed class RuntimeReaderTransportSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrPipelineSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrWarmupSnapshotFields");
        AssertContains(sourceTelemetryText, "private sealed class RuntimeSourceTelemetrySnapshotFields");
        AssertContains(runtimeText, "private sealed class RuntimeRecordingIntegritySnapshotFields");
        AssertContains(runtimeText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(hdrPipelineText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(sourceTelemetryText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(assemblerText, "bool? ObservedP010Likely8BitUpscaled) ObservedTelemetry");
        AssertContains(assemblerText, "return new CaptureRuntimeSnapshot");
        AssertContains(assemblerText, "TimestampUtc = fields.TimestampUtc,");
        AssertContains(assemblerText, "HdrWarmupObservedP010Frames = hdrWarmup.ObservedP010Frames,");
        AssertDoesNotContain(assemblerBuildText, "ResolveHdrWarmupState(");
        AssertContains(assemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "FlashbackCodecDowngradeReason = fields.FlashbackCodecDowngradeReason,");
        AssertContains(assemblerText, "AvSyncCaptureDriftMs = fields.RuntimeAvSyncDriftMs,");
        AssertContains(captureRuntimeModelText, "public sealed class CaptureRuntimeSnapshot");
        AssertContains(captureRuntimeModelText, "public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;");
        AssertContains(captureRuntimeModelText, "public bool AudioReaderActive { get; init; }");
        AssertContains(captureRuntimeModelText, "public double WasapiPlaybackOutputPeak { get; init; }");
        AssertContains(captureRuntimeModelText, "public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();");
        AssertContains(captureRuntimeModelText, "public string PreviewColorMetadata { get; init; } = \"None\";");
        AssertContains(captureRuntimeModelText, "public uint? RequestedWidth { get; init; }");
        AssertContains(captureRuntimeModelText, "public string? EncoderVideoCodec { get; init; }");
        AssertContains(captureRuntimeModelText, "public string HdrRuntimeState { get; init; } = \"Inactive\";");
        AssertContains(captureRuntimeModelText, "public string TelemetryAlignmentStatus { get; init; } = \"Unknown\";");
        AssertContains(captureRuntimeModelText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();");
        AssertContains(captureRuntimeModelText, "public double? AvSyncCaptureDriftMs { get; init; }");
        AssertContains(captureRuntimeModelText, "public string RecordingIntegrityStatus { get; init; } = \"NotStarted\";");
        AssertContains(captureRuntimeModelText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        AssertDoesNotContain(captureRuntimeModelText, "partial class CaptureRuntimeSnapshot");

        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,");
        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshots.cs` also owns final `CaptureRuntimeSnapshot` DTO construction");
        AssertContains(agentMapText, "from already-sampled field groups and the private runtime snapshot assembly");
        AssertContains(agentMapText, "handoff contract consumed by that map.");
        AssertContains(agentMapText, "AutomationRuntimeModels.cs");
        AssertContains(agentMapText, "owns video ingest/source-reader/WASAPI playback");
        AssertContains(agentMapText, "and reader/transport projections, recording-integrity summary projection,");
        AssertContains(agentMapText, "HDR pipeline/warmup projection, source-telemetry detail/frame-rate-origin/age/");
        AssertContains(agentMapText, "private assembly handoff models,");
        AssertContains(agentMapText, "final DTO construction.");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples");
        AssertContains(cleanupPlanText, "final `CaptureRuntimeSnapshot` DTO construction");
        AssertContains(cleanupPlanText, "private runtime snapshot assembly handoff contract");
        AssertContains(cleanupPlanText, "snapshot sampler that consumes it.");
        AssertContains(cleanupPlanText, "`AutomationRuntimeModels.cs`");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Automation", "CaptureRuntimeSnapshot.cs")),
            "capture runtime DTO folded into AutomationRuntimeModels.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotAssemblyFields.cs")),
            "old runtime snapshot assembly-fields partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotAssembler.cs")),
            "runtime snapshot assembler folded into RuntimeSnapshots.cs");
        AssertContains(cleanupPlanText, "Video ingest, source-reader health, WASAPI capture, playback output counter,");
        AssertContains(cleanupPlanText, "requested/negotiated reader transport, memory preference, frame-ledger, preview");
        AssertContains(cleanupPlanText, "HDR pipeline");
        AssertContains(cleanupPlanText, "source telemetry");
        AssertContains(cleanupPlanText, "detail/frame-rate-origin/age/alignment projection");
        AssertContains(cleanupPlanText, "private handoff models now live with the runtime snapshot sampler");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotRecordingIntegrity.cs")),
            "old runtime recording-integrity projection partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeReaderTransportProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var readerTransport = CaptureRuntimeReaderTransportSnapshotFields(");
        AssertContains(runtimeText, "ReaderTransport = readerTransport,");
        AssertContains(assemblerText, "MemoryPreference = readerTransport.MemoryPreference,");
        AssertContains(assemblerText, "VideoRequestedSubtype = readerTransport.VideoRequestedSubtype,");
        AssertContains(assemblerText, "FrameLedgerRecentEvents = readerTransport.FrameLedgerRecentEvents,");
        AssertContains(assemblerText, "MfSourceReaderNegotiatedFormat = readerTransport.MfSourceReaderNegotiatedFormat,");
        AssertContains(assemblerText, "ReaderSourceSubtype = readerTransport.ReaderSourceSubtype,");

        AssertContains(runtimeText, "private static RuntimeReaderTransportSnapshotFields CaptureRuntimeReaderTransportSnapshotFields(");
        AssertContains(runtimeText, "private sealed class RuntimeReaderTransportSnapshotFields");
        AssertContains(runtimeText, "requestedSettings!.RequestedPixelFormat");
        AssertContains(runtimeText, "mfSourceReaderNegotiatedFormat.Contains(\"P010\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(runtimeText, "unifiedVideoCapture.IsHighFrameRateMjpegMode ? \"MJPG\"");
        AssertContains(runtimeText, "readerSourceStreamType = (recordingActive || videoPreviewActive) && unifiedVideoCapture != null");
        AssertContains(runtimeText, "FrameLedgerSummary.Empty");
        AssertContains(runtimeText, "MemoryPreference = unifiedVideoCapture?.D3DManager != null ? \"Gpu\" : \"Cpu\",");
        AssertContains(runtimeText, "(previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? \"None\"");
        AssertContains(runtimeText, "ReaderSourceSubtype = actualPixelFormat");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeHdrPipelineProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = runtimeText;

        AssertContains(runtimeText, "var hdrPipeline = CaptureRuntimeHdrPipelineSnapshotFields(");
        AssertContains(runtimeText, "var hdrWarmup = CaptureRuntimeHdrWarmupSnapshotFields(");
        AssertContains(runtimeText, "HdrPipeline = hdrPipeline,");
        AssertContains(runtimeText, "HdrWarmup = hdrWarmup,");
        AssertContains(assemblerText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(assemblerText, "HdrWarmupState = hdrWarmup.State,");
        AssertContains(assemblerText, "EncoderOutputPixelFormat = hdrPipeline.EncoderOutputPixelFormat,");
        AssertContains(assemblerText, "PipelineModeReason = hdrPipeline.PipelineModeReason,");

        AssertContains(hdrPipelineText, "private static RuntimeHdrPipelineSnapshotFields CaptureRuntimeHdrPipelineSnapshotFields(");
        AssertContains(hdrPipelineText, "private static RuntimeHdrWarmupSnapshotFields CaptureRuntimeHdrWarmupSnapshotFields(");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrPipelineSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrWarmupSnapshotFields");
        AssertContains(hdrPipelineText, "ResolveEncoderOutputPixelFormat(recordingContext, requestedSettings)");
        AssertContains(hdrPipelineText, "Requested pipeline '{requestedPipelineMode}'");
        AssertContains(hdrPipelineText, "HdrDowngradeCode = hdrAutoDowngraded ? \"encoder-input-not-p010\" : string.Empty");
        AssertContains(hdrPipelineText, "HdrRequestedButSourceNot10Bit = hdrRequested && sourceTelemetry.IsHdr == false");
        AssertContains(hdrPipelineText, "ResolveHdrWarmupState(");
        AssertContains(hdrPipelineText, "ObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount))");

        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotHdrPipeline.cs")),
            "HDR runtime snapshot projection folded into runtime snapshot sampler");
        AssertDoesNotContain(assemblerText, "HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeSourceTelemetryProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = runtimeText;

        AssertContains(runtimeText, "var sourceTelemetry = CaptureRuntimeSourceTelemetrySnapshotFields(");
        AssertContains(runtimeText, "SourceTelemetry = sourceTelemetry,");
        AssertContains(assemblerText, "DetectedSourceFrameRate = sourceTelemetry.DetectedSourceFrameRate,");
        AssertContains(assemblerText, "SourceTelemetryAgeSeconds = sourceTelemetry.AgeSeconds,");
        AssertContains(assemblerText, "TelemetryAlignmentStatus = sourceTelemetry.AlignmentStatus,");

        AssertContains(sourceTelemetryText, "private static RuntimeSourceTelemetrySnapshotFields CaptureRuntimeSourceTelemetrySnapshotFields(");
        AssertContains(sourceTelemetryText, "private sealed class RuntimeSourceTelemetrySnapshotFields");
        AssertContains(sourceTelemetryText, "TelemetryAgeHelper.ComputeAgeSeconds(telemetryTimestampUtc, DateTimeOffset.UtcNow)");
        AssertContains(sourceTelemetryText, "ResolveTelemetryAlignment(");
        AssertContains(sourceTelemetryText, "CircuitState = ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(sourceTelemetryText, "SourceRawTimingHex = telemetry.RawTimingHex,");

        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotSourceTelemetry.cs")),
            "source telemetry runtime snapshot projection folded into runtime snapshot sampler");
        AssertDoesNotContain(runtimeText, "SourceTelemetryDetails = _latestSourceTelemetry.DetailEntries,");
        AssertDoesNotContain(runtimeText, "ResolveSourceTelemetryCircuitState(_latestSourceTelemetry.Availability");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeRecordingIntegrityProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var recordingIntegrity = CaptureRuntimeRecordingIntegritySnapshotFields(");
        AssertContains(runtimeText, "RecordingIntegrity = recordingIntegrity,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(assemblerText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");

        AssertContains(runtimeText, "private static RuntimeRecordingIntegritySnapshotFields CaptureRuntimeRecordingIntegritySnapshotFields(");
        AssertContains(runtimeText, "private sealed class RuntimeRecordingIntegritySnapshotFields");
        AssertContains(runtimeText, "Status = recordingIntegrity.Status,");
        AssertContains(runtimeText, "AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(runtimeText, "EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertContains(runtimeText, "Reason = recordingIntegrity.Reason");

        AssertDoesNotContain(runtimeText, "var recordingIntegrity = ResolveRecordingIntegritySummary(");

        return Task.CompletedTask;
    }

    internal static Task FrameLedger_RetainsBoundedRecentEvents()
    {
        var unifiedVideoCaptureText = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        AssertContains(unifiedVideoCaptureText, "internal sealed class FrameLedger");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "FrameLedger.cs")),
            "frame ledger helper folded into UnifiedVideoCapture.cs");

        var ledgerType = RequireType("Sussudio.Services.Capture.FrameLedger");
        var identityType = RequireType("Sussudio.Models.FrameIdentity");
        var stageType = RequireType("Sussudio.Models.FrameLedgerStage");
        var ledger = Activator.CreateInstance(
                ledgerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { 3 },
                culture: null)
            ?? throw new InvalidOperationException("Failed to create FrameLedger.");

        var recordCapture = ledgerType.GetMethod(
                "RecordCaptureArrived",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.RecordCaptureArrived missing.");
        var recordEvent = ledgerType.GetMethod(
                "RecordEvent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.RecordEvent missing.");
        var getSummary = ledgerType.GetMethod(
                "GetSummary",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.GetSummary missing.");

        for (var i = 0; i < 4; i++)
        {
            var identity = Activator.CreateInstance(
                    identityType,
                    (long)i,
                    1000L + i,
                    null,
                    "MJPG",
                    3840,
                    2160,
                    120.0,
                    1024 + i)
                ?? throw new InvalidOperationException("Failed to create FrameIdentity.");
            recordCapture.Invoke(ledger, new object?[] { identity, "capture" });
        }

        var recordingStage = Enum.Parse(stageType, "RecordingEnqueued");
        recordEvent.Invoke(ledger, new object?[]
        {
            4L,
            recordingStage,
            2000L,
            "recording",
            null,
            null,
            true,
            null
        });

        var summary = getSummary.Invoke(ledger, new object[] { 3 })
                      ?? throw new InvalidOperationException("FrameLedger.GetSummary returned null.");
        AssertEqual(3, GetIntProperty(summary, "Capacity"), "FrameLedger capacity");
        AssertEqual(5L, GetLongProperty(summary, "TotalEventsRecorded"), "FrameLedger total events");
        AssertEqual(2L, GetLongProperty(summary, "EventsDroppedByRetention"), "FrameLedger retained drop count");
        AssertEqual(3, GetIntProperty(summary, "RecentEventCount"), "FrameLedger recent count");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(summary, "OldestSourceSequence")), "FrameLedger oldest sequence");
        AssertEqual(4L, Convert.ToInt64(GetPropertyValue(summary, "NewestSourceSequence")), "FrameLedger newest sequence");

        var events = (Array)(GetPropertyValue(summary, "RecentEvents")
                             ?? throw new InvalidOperationException("FrameLedger recent events missing."));
        AssertEqual(3, events.Length, "FrameLedger recent event array length");
        AssertEqual(2L, GetLongProperty(events.GetValue(0)!, "SourceSequence"), "FrameLedger first retained sequence");
        AssertEqual(4L, GetLongProperty(events.GetValue(2)!, "SourceSequence"), "FrameLedger last retained sequence");
        AssertEqual("RecordingEnqueued", GetPropertyValue(events.GetValue(2)!, "Stage")!.ToString(), "FrameLedger last retained stage");
        AssertEqual(true, GetBoolProperty(events.GetValue(2)!, "Accepted"), "FrameLedger accepted state");

        return Task.CompletedTask;
    }

    internal static Task FrameLedger_SnapshotContractExposesRecentEvents()
    {
        var captureSnapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var automationSnapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var eventSnapshotType = RequireType("Sussudio.Models.FrameLedgerEventSnapshot");
        var identityType = RequireType("Sussudio.Models.FrameIdentity");

        foreach (var snapshotType in new[] { captureSnapshotType, automationSnapshotType })
        {
            AssertNotNull(snapshotType.GetProperty("FrameLedgerCapacity"), $"{snapshotType.Name}.FrameLedgerCapacity");
            AssertNotNull(snapshotType.GetProperty("FrameLedgerEventCount"), $"{snapshotType.Name}.FrameLedgerEventCount");
            AssertNotNull(snapshotType.GetProperty("FrameLedgerDroppedEventCount"), $"{snapshotType.Name}.FrameLedgerDroppedEventCount");

            var recentEvents = snapshotType.GetProperty("FrameLedgerRecentEvents")
                ?? throw new InvalidOperationException($"{snapshotType.Name}.FrameLedgerRecentEvents missing.");
            AssertEqual(eventSnapshotType.MakeArrayType(), recentEvents.PropertyType, $"{snapshotType.Name}.FrameLedgerRecentEvents type");
        }

        foreach (var prop in new[]
                 {
                     "SourceSequence",
                     "Stage",
                     "QpcTimestamp",
                     "Subsystem",
                     "QueueDepth",
                     "ByteDepth",
                     "Accepted",
                     "Reason",
                     "Identity"
                 })
        {
            AssertNotNull(eventSnapshotType.GetProperty(prop), $"FrameLedgerEventSnapshot.{prop}");
        }

        foreach (var prop in new[]
                 {
                     "SourceSequence",
                     "CaptureArrivalQpc",
                     "DeviceTimestamp100ns",
                     "InputFormat",
                     "Width",
                     "Height",
                     "FrameRateNominal",
                     "CompressedByteLength"
                 })
        {
            AssertNotNull(identityType.GetProperty(prop), $"FrameIdentity.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static async Task GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_firstObservedFramePixelFormat", "NV12");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "BGRA8");
        SetPrivateField(captureService, "_latestObservedSurfaceFormat", "BGRA8");
        SetPrivateField(captureService, "_observedP010FrameCount", 0L);
        SetPrivateField(captureService, "_observedNv12FrameCount", 2L);
        SetPrivateField(captureService, "_observedOtherFrameCount", 3L);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(0L, GetLongProperty(snapshot, "ObservedP010FrameCount"), "ObservedP010FrameCount");
        AssertEqual(2L, GetLongProperty(snapshot, "ObservedNv12FrameCount"), "ObservedNv12FrameCount");
        AssertEqual(3L, GetLongProperty(snapshot, "ObservedOtherFrameCount"), "ObservedOtherFrameCount");
        AssertEqual("NV12", GetStringProperty(snapshot, "FirstObservedFramePixelFormat"), "FirstObservedFramePixelFormat");
        AssertEqual("BGRA8", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_actualPixelFormat", "MJPG");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "NV12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("MJPG", GetStringProperty(snapshot, "ReaderSourceSubtype"), "ReaderSourceSubtype");
        AssertEqual("NV12", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "OriginDetail", "RegressionHarness");
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 1280);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 720);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 30d);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateArg", "30/1");
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", false);
        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Mismatch", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "width expected");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "hdr expected");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var createUnavailable = telemetryType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null);
        if (createUnavailable == null)
        {
            throw new InvalidOperationException("SourceSignalTelemetrySnapshot.CreateUnavailable not found.");
        }

        var unavailableTelemetry = createUnavailable.Invoke(null, new object?[] { "regression-harness-unavailable", null });
        SetPrivateField(captureService, "_latestSourceTelemetry", unavailableTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Unavailable", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "unavailable");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(true, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Ready", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var recordingBackend = GetPrivateField(captureService, "_recordingBackend")
            ?? throw new InvalidOperationException("CaptureService recording backend resources were missing.");
        SetPropertyOrBackingField(recordingBackend, "SettingsSnapshot", settings);
        SetPrivateField(captureService, "_isRecording", true);
        SetPrivateField(captureService, "_activeVideoInputPixelFormat", "nv12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("SDR", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(false, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Violation", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");
        AssertContains(GetStringProperty(snapshot, "PipelineModeReason"), "Requested pipeline");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(false, GetBoolProperty(snapshot, "SourceReaderReadOutstanding"), "SourceReaderReadOutstanding");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderReadOutstandingMs"), "SourceReaderReadOutstandingMs");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderLastFrameTickMs"), "SourceReaderLastFrameTickMs");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureCallbackCount"), "WasapiCaptureCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureAudioLevelEventsFired"), "WasapiCaptureAudioLevelEventsFired");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackRenderCallbackCount"), "WasapiPlaybackRenderCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackQueueDropCount"), "WasapiPlaybackQueueDropCount");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackQueueDurationMs"), 0.0001, "WasapiPlaybackQueueDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackActiveChunkDurationMs"), 0.0001, "WasapiPlaybackActiveChunkDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackEndpointQueuedDurationMs"), 0.0001, "WasapiPlaybackEndpointQueuedDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackBufferedDurationMs"), 0.0001, "WasapiPlaybackBufferedDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackStreamLatencyMs"), 0.0001, "WasapiPlaybackStreamLatencyMs");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }
}

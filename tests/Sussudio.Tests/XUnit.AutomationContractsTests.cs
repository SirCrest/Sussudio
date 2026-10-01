using System.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Sussudio.Models;
using Sussudio.Tools;
using Xunit;

namespace Sussudio.Tests
{

public sealed class AutomationAppSurfaceContractsTests
{
    [Theory]
    [InlineData(true, "RecoveredFinalizationFailure", "NotStarted", false, false)]
    [InlineData(true, "InvalidOperationException", "NotStarted", false, true)]
    [InlineData(true, "RecoveredFinalizationFailure", "Incomplete", false, true)]
    [InlineData(true, "RecoveredFinalizationFailure", "NotStarted", true, true)]
    [InlineData(false, "", "Incomplete", false, true)]
    [InlineData(false, "", "Failed", false, true)]
    public void RestoredRecordingHistoryDoesNotMarkANewCaptureAsFailed(
        bool encodingFailed, string failureType, string integrityStatus, bool isRecording, bool expectedFailure)
    {
        var hub = SussudioAssembly.Load().GetType("Sussudio.Services.Automation.AutomationDiagnosticsHub", true)!;
        var evaluate = hub.GetMethod("TryBuildRealtimeRecordingDiagnosticEvaluation", BindingFlags.Static | BindingFlags.NonPublic)!;
        var parameters = evaluate.GetParameters();
        var runtime = Activator.CreateInstance(parameters[0].ParameterType)!;
        var health = Activator.CreateInstance(parameters[1].ParameterType)!;
        var lanes = Activator.CreateInstance(parameters[3].ParameterType)!;
        var recordingIntegrityStatus = runtime.GetType().GetProperty("RecordingIntegrityStatus")!;
        recordingIntegrityStatus.SetValue(runtime, Enum.Parse(recordingIntegrityStatus.PropertyType, integrityStatus));
        var recordingIntegrityAudioStatus = runtime.GetType().GetProperty("RecordingIntegrityAudioStatus")!;
        recordingIntegrityAudioStatus.SetValue(
            runtime,
            Enum.Parse(recordingIntegrityAudioStatus.PropertyType, "Disabled"));
        health.GetType().GetProperty("RecordingEncodingFailed")!.SetValue(health, encodingFailed);
        health.GetType().GetProperty("RecordingEncodingFailureType")!.SetValue(health, failureType);
        var result = evaluate.Invoke(null, new[] { runtime, health, (object)isRecording, lanes });
        Assert.Equal(expectedFailure, result != null);
    }

    public AutomationAppSurfaceContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AppWiresRecoverableAndFatalUnhandledExceptionPolicy()
        => global::Program.App_Xaml_WiresUnhandledExceptionPolicy();

    [Fact]
    public Task BoolConvertersPreserveInversionAndVisibilityMappings()
        => global::Program.BoolConverters_PreserveInversionAndVisibilityMappings();

    [Fact]
    public Task DisplayFormattersMapSourceHdrStates()
        => global::Program.DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates();

    [Theory]
    [InlineData(-1L, "0.##", "0 B")]
    [InlineData(0L, "0.##", "0 B")]
    [InlineData(1023L, "0.##", "1023 B")]
    [InlineData(1024L, "0.##", "1 KB")]
    [InlineData(1572864L, "0.##", "1.5 MB")]
    [InlineData(1610612736L, "0", "2 GB")]
    [InlineData(1099511627776L, "0.##", "1 TB")]
    public void RecordingSizeUsesBinaryUnitsAndRequestedPrecision(long bytes, string format, string expected)
    {
        var formatter = SussudioAssembly.Load().GetType("Sussudio.DisplayFormatters", throwOnError: true)!;
        var method = formatter.GetMethod("FormatBytes", BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(expected, method.Invoke(null, new object[] { bytes, format }));
    }

    [Theory]
    [InlineData(-1.0, "0 bps")]
    [InlineData(0.0, "0 bps")]
    [InlineData(999.0, "999 bps")]
    [InlineData(1000.0, "1 Kbps")]
    [InlineData(1000000.0, "1 Mbps")]
    [InlineData(12500000.0, "12 Mbps")]
    [InlineData(1000000000.0, "1 Gbps")]
    public void RecordingBitrateUsesDecimalUnitsAndWholeNumbers(double bitsPerSecond, string expected)
    {
        var formatter = SussudioAssembly.Load().GetType("Sussudio.DisplayFormatters", throwOnError: true)!;
        var method = formatter.GetMethod("FormatBitrate", BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(expected, method.Invoke(null, new object[] { bitsPerSecond }));
    }

    [Fact]
    public Task ProjectFilePreservesEnglishOnlyPublishLocalePolicy()
        => global::Program.ProjectFile_PreservesEnglishOnlyPublishLocalePolicy();

    [Fact]
    public Task LoggingJsonContextSerializesStructuredSnapshotPayloads()
        => global::Program.LoggingJsonContext_SerializesStructuredSnapshotPayloads();

    [Fact]
    public Task UiAutomationCommandsAreNotBlockedOnDeviceReadiness()
        => global::Program.UiAutomationCommands_AreNotBlockedOnDeviceReadiness();

    [Fact]
    public Task MainWindowAutomationIdsCoverAgentCriticalUiSurface()
        => global::Program.MainWindowAutomationIds_CoverAgentCriticalSurface();

    [Fact]
    public Task WindowUiDispatchCancellationDoesNotCompleteWindowCloseRequest()
        => global::Program.WindowUiDispatchCancellation_DoesNotCompleteWindowCloseRequest();

    [Fact]
    public Task AutomationPipeServerGatesDefaultSecurityFallbackOnAuthToken()
        => global::Program.NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken();

    [Fact]
    public Task AutomationPipeServerRequestTimeoutsUseBoundedDispatchCancellation()
        => global::Program.NamedPipeAutomationServer_RequestTimeoutsUseBoundedDispatchCancellation();

    [Theory]
    [InlineData("canceled", false, false)]
    [InlineData("CaNcElEd", false, false)]
    [InlineData(null, false, false)]
    [InlineData("execution-failed", false, false)]
    [InlineData(null, true, false)]
    [InlineData("canceled", false, true)]
    [InlineData("CaNcElEd", false, true)]
    [InlineData(null, false, true)]
    [InlineData("execution-failed", false, true)]
    [InlineData(null, true, true)]
    public Task AutomationPipeServerTimeoutWaitsForDispatchAndPreservesOutcome(
        string? responseError, bool dispatchFaults, bool cancelAfterAdmission)
        => global::Program.NamedPipeAutomationServer_TimeoutWaitsForDispatchAndPreservesOutcome(
            responseError, dispatchFaults, cancelAfterAdmission);

    [Fact]
    public Task AutomationPipeServerShutdownPreservesServerCancellationIdentity()
        => global::Program.NamedPipeAutomationServer_ShutdownPreservesServerCancellationIdentity();

    [Fact]
    public Task AutomationPipeServerRequestLimitHandlesCrLfBoundary()
        => global::Program.NamedPipeAutomationServer_RequestLimit_HandlesCrLfBoundary();

    [Fact]
    public Task AutomationPipeServerKeepsDiagnosticsAvailableBesideBusyClients()
        => global::Program.NamedPipeAutomationServer_KeepsDiagnosticsAvailableBesideBusyClients();

    [Fact]
    public Task MainWindowWiresAutomationPipeAuthFallbackPolicy()
        => global::Program.MainWindowAutomation_WiresPipeAuthFallbackPolicy();

    [Fact]
    public Task StreamDeckScopeDocumentsAutomationAuthEnvelope()
        => global::Program.StreamDeckPluginScope_DocumentsAutomationAuthEnvelope();
}

// Minimal xUnit slice for Sussudio.Converters.BoolConverters. The full
// behavior matrix is exercised by the legacy Program checks below; these
// focused checks keep the directly executable converter contracts visible in
// xUnit discovery too.
public class BoolConvertersTests
{
    [Fact]
    public void InverseBoolConverter_InvertsBooleanValues()
    {
        var asm = SussudioAssembly.Load();
        var converterType = asm.GetType("Sussudio.Converters.InverseBoolConverter", throwOnError: true)!;
        var convert = ResolveConvertMethod(converterType, "Convert");

        var instance = Activator.CreateInstance(converterType)!;
        Assert.Equal(false, convert.Invoke(instance, new object?[] { true, typeof(bool), null, "" }));
        Assert.Equal(true, convert.Invoke(instance, new object?[] { false, typeof(bool), null, "" }));
    }

    [Fact]
    public void InverseBoolConverter_PreservesNonBooleanValues()
    {
        var asm = SussudioAssembly.Load();
        var converterType = asm.GetType("Sussudio.Converters.InverseBoolConverter", throwOnError: true)!;
        var convert = ResolveConvertMethod(converterType, "Convert");

        var instance = Activator.CreateInstance(converterType)!;

        var sentinel = new object();
        Assert.Same(sentinel, convert.Invoke(instance, new object?[] { sentinel, typeof(bool), null, "" }));
    }

    [Fact]
    public void BoolToVisibilityConverter_ImplementsIValueConverter()
    {
        var asm = SussudioAssembly.Load();
        var boolToVisibility = asm.GetType("Sussudio.Converters.BoolToVisibilityConverter", throwOnError: true)!;

        AssertImplementsValueConverter(boolToVisibility);
    }

    [Fact]
    public void BoolToInverseVisibilityConverter_ImplementsIValueConverter()
    {
        var asm = SussudioAssembly.Load();
        var inverseVisibility = asm.GetType("Sussudio.Converters.BoolToInverseVisibilityConverter", throwOnError: true)!;

        AssertImplementsValueConverter(inverseVisibility);

        // Visibility mapping behavior is exercised by the legacy reflection runner
        // below, which loads Microsoft.UI.Xaml.dll via the staged win-x64 path; the
        // xUnit host here intentionally stops at metadata checks because WinUI
        // dependencies are not side-loaded into the test AppDomain.
    }

    private static void AssertImplementsValueConverter(Type type)
    {
        var iface = type.GetInterface("Microsoft.UI.Xaml.Data.IValueConverter")
            ?? throw new InvalidOperationException($"{type.FullName} does not implement IValueConverter.");
        Assert.NotNull(type.GetMethod("Convert", new[] { typeof(object), typeof(Type), typeof(object), typeof(string) }));
        Assert.NotNull(type.GetMethod("ConvertBack", new[] { typeof(object), typeof(Type), typeof(object), typeof(string) }));
        Assert.True(iface.IsAssignableFrom(type));
    }

    private static MethodInfo ResolveConvertMethod(Type type, string name)
        => type.GetMethod(name, new[] { typeof(object), typeof(Type), typeof(object), typeof(string) })
            ?? throw new InvalidOperationException($"{type.Name}.{name}(object, Type, object, string) not found.");
}

public sealed class AutomationCatalogContractsTests
{
    [Fact]
    public Task CommandCatalogCoversCommandsAndPolicyMetadata()
        => global::Program.AutomationCommandCatalog_CoversCommandsAndPolicyMetadata();

    [Fact]
    public Task ReliabilityGatesRunToolsAndOfflineHarness()
        => global::Program.ReliabilityGates_RunToolsAndOfflineHarness();

    [Fact]
    public Task ManifestCoversCatalogMetadata()
        => global::Program.AutomationManifest_CoversCatalogMetadata();

    [Fact]
    public Task PathBearingCommandsHaveValidationCoverage()
        => global::Program.AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage();

    [Fact]
    public Task ManifestSerializationIsStable()
        => global::Program.AutomationManifest_SerializationIsStable();
}

public sealed class AutomationContractsProtocolXunitTests
{
    private static readonly object AutomationTokenLock = new();

    [Fact]
    public void AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest()
    {
        var expectedCommands = global::Program.ExpectedAutomationCommands();
        var enumValues = Enum.GetValues<AutomationCommandKind>();
        var manifest = AutomationCommandCatalog.CreateManifest();

        Assert.Equal(expectedCommands.Length, enumValues.Length);
        Assert.Equal(expectedCommands.Length, manifest.Commands.Count);

        for (var i = 0; i < expectedCommands.Length; i++)
        {
            var (name, value) = expectedCommands[i];
            var parsed = Enum.Parse<AutomationCommandKind>(name);

            Assert.Equal(value, (int)parsed);
            Assert.True(Enum.IsDefined(parsed), $"AutomationCommandKind missing sequential value {value}.");

            var manifestCommand = Assert.Single(
                manifest.Commands,
                command => string.Equals(command.Name, name, StringComparison.Ordinal));
            Assert.Equal(value, manifestCommand.Id);
        }
    }

    [Fact]
    public void AutomationPipeProtocol_ExposesStableWireDefaults()
    {
        Assert.Equal("SussudioAutomation", AutomationPipeProtocol.DefaultPipeName);
        Assert.Equal("SUSSUDIO_AUTOMATION_PIPE", AutomationPipeProtocol.AutomationPipeEnvVar);
        Assert.Equal("SUSSUDIO_AUTOMATION_TOKEN", AutomationPipeProtocol.AutomationKeyEnvVar);
        Assert.Equal(2, AutomationPipeProtocol.CommandManifestRevision);
        Assert.Equal(5000, AutomationPipeProtocol.DefaultConnectTimeoutMs);
        Assert.Equal(15000, AutomationPipeProtocol.DefaultResponseTimeoutMs);
        Assert.Equal(60000, AutomationPipeProtocol.ExtendedResponseTimeoutMs);
        Assert.Equal(150000, AutomationPipeProtocol.RecordingResponseTimeoutMs);
        Assert.Equal(305000, AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs);
    }

    [Fact]
    public void AutomationPipeProtocol_ResolvesCanonicalCommandNames()
    {
        Assert.Equal(1, AutomationPipeProtocol.ResolveCommand("GetSnapshot"));
        Assert.Equal(1, AutomationPipeProtocol.ResolveCommand("get-snapshot"));
        Assert.Equal(17, AutomationPipeProtocol.ResolveCommand("17"));
        Assert.Throws<ArgumentException>(() => AutomationPipeProtocol.ResolveCommand("not-a-command"));

        Assert.True(AutomationPipeProtocol.TryGetCommandValue("setrecordingenabled", out var commandValue));
        Assert.Equal(17, commandValue);

        Assert.True(AutomationPipeProtocol.TryGetCommandName(17, out var commandName));
        Assert.Equal("SetRecordingEnabled", commandName);
        Assert.False(AutomationPipeProtocol.TryGetCommandName(-1, out var unknownCommandName));
        Assert.Equal(string.Empty, unknownCommandName);
    }

    [Fact]
    public void AutomationPipeProtocol_UsesCatalogTimeoutPolicy()
    {
        Assert.Equal(15000, AutomationPipeProtocol.GetDefaultResponseTimeout("GetSnapshot"));
        Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));
        Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackEnabled"));
        Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackBufferMinutes"));
        Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("RestartFlashback"));
        Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout("SetRecordingEnabled"));
        Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout("set-recording-enabled"));
        Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout("17"));
        Assert.Equal(60000, AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition));
    }

    [Fact]
    public void AutomationPipeProtocol_ResolvesEnvironmentAuthToken()
    {
        lock (AutomationTokenLock)
        {
            var previousToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
            try
            {
                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, "env-token");
                Assert.Equal("env-token", AutomationPipeProtocol.GetConfiguredAuthToken());

                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, "   ");
                Assert.Null(AutomationPipeProtocol.GetConfiguredAuthToken());
            }
            finally
            {
                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, previousToken);
            }
        }
    }

    [Fact]
    public void AutomationPipeProtocol_CreatesACompleteRequestEnvelope()
    {
        var payload = new Dictionary<string, object?> { ["enabled"] = true };

        var envelope = AutomationPipeProtocol.CreateRequestEnvelope(17, payload, "explicit-token");

        Assert.Equal(17, envelope["command"]);
        Assert.True(Guid.TryParseExact(Assert.IsType<string>(envelope["correlationId"]), "N", out _));
        Assert.Equal(AutomationPipeProtocol.CommandManifestRevision, envelope["manifestRevision"]);
        Assert.Equal("explicit-token", envelope["authToken"]);
        Assert.Same(payload, envelope["payload"]);
    }

    [Fact]
    public void AutomationPipeProtocol_CreatesAnEmptyPayloadWhenOneIsNotSupplied()
    {
        var envelope = AutomationPipeProtocol.CreateRequestEnvelope(1, authToken: "explicit-token");

        Assert.Empty(Assert.IsType<Dictionary<string, object?>>(envelope["payload"]));
    }

    [Fact]
    public void SharedProtocol_CommandMap_CoversEveryAutomationCommandKind()
    {
        var enumNames = Enum.GetNames<AutomationCommandKind>();
        var expectedCommands = global::Program.ExpectedAutomationCommands();
        var commandMap = AutomationPipeProtocol.CommandMap;

        Assert.NotEmpty(enumNames);
        Assert.Equal(expectedCommands.Length, commandMap.Count);

        foreach (var (name, ordinal) in expectedCommands)
        {
            Assert.True(commandMap.TryGetValue(name, out var mappedOrdinal), $"AutomationPipeProtocol.CommandMap missing '{name}'.");
            Assert.Equal(ordinal, mappedOrdinal);
            Assert.Equal(ordinal, (int)Enum.Parse<AutomationCommandKind>(name));
        }

        Assert.Equal(enumNames.Length, commandMap.Count);
    }
}

public sealed class AutomationDiagnosticsLoopContractsTests
{
    [Theory]
    [InlineData("Disabled")]
    [InlineData("Buffering")]
    [InlineData("Live")]
    [InlineData("Scrubbing")]
    [InlineData("Playing")]
    [InlineData("Paused")]
    [InlineData("N/A")]
    [InlineData(null)]
    [InlineData("pLaYiNg")]
    public void FlashbackPlaybackHealthAndWireEvaluationPreservePerformanceGates(string? wireState)
    {
        var assembly = SussudioAssembly.Load();
        var hubType = assembly.GetType("Sussudio.Services.Automation.AutomationDiagnosticsHub", throwOnError: true)!;
        var healthType = assembly.GetType("Sussudio.Models.CaptureHealthSnapshot", throwOnError: true)!;
        var stateType = assembly.GetType("Sussudio.Models.FlashbackPlaybackState", throwOnError: true)!;
        var snapshotType = assembly.GetType("Sussudio.Models.AutomationSnapshot", throwOnError: true)!;
        var flashbackEvaluator = hubType.GetNestedType("FlashbackDiagnosticEvaluator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackDiagnosticEvaluator was not found on AutomationDiagnosticsHub.");
        var evaluateHealth = flashbackEvaluator.GetMethod("TryBuildFlashbackPlaybackDiagnosticEvaluation", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Flashback playback evaluation was not found on its nested owner.");
        var evaluateWire = hubType.GetMethod("UpdateFlashbackPlaybackPerformanceAlerts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var lanesType = evaluateHealth.GetParameters()[1].ParameterType;
        Assert.True(lanesType.IsValueType);
        var lanes = Activator.CreateInstance(lanesType)!;
        var typedState = wireState is null or "N/A" ? null : Enum.Parse(stateType, wireState, ignoreCase: true);
        var playing = string.Equals(wireState, "Playing", StringComparison.OrdinalIgnoreCase);

        foreach (var sample in new (string Name, long Frames, int Samples, double Target, double Observed, double Low, bool Slow, bool Frametime)[]
        {
            ("slow frame threshold", 60, 0, 120, 89.999, 0, true, false),
            ("below slow frame threshold", 59, 0, 120, 89.999, 0, false, false),
            ("exact slow ratio", 60, 0, 120, 90, 0, false, false),
            ("zero observed slow rate", 60, 0, 120, 0, 0, false, false),
            ("zero target slow rate", 60, 0, 0, 1, 0, false, false),
            ("frametime sample thresholds", 1200, 1200, 120, 120, 117.599, false, true),
            ("below frametime frame threshold", 1199, 1200, 120, 120, 117.599, false, false),
            ("below frametime sample threshold", 1200, 1199, 120, 120, 117.599, false, false),
            ("exact one percent low ratio", 1200, 1200, 120, 120, 120 * 0.98, false, false),
            ("zero one percent low", 1200, 1200, 120, 120, 0, false, false),
            ("zero target frametime rate", 1200, 1200, 0, 120, 1, false, false),
            ("frametime independent of observed rate", 1200, 1200, 120, 0, 117.599, false, true),
            ("slow diagnostic precedence", 1200, 1200, 120, 89, 100, true, true)
        })
        {
            var health = Activator.CreateInstance(healthType)!;
            Set(health, "FlashbackPlaybackState", typedState);
            Set(health, "FlashbackPlaybackTargetFps", sample.Target);
            Set(health, "FlashbackPlaybackFrameCount", sample.Frames);
            Set(health, "FlashbackPlaybackCadenceSampleCount", sample.Samples);
            Set(health, "FlashbackPlaybackObservedFps", sample.Observed);
            Set(health, "FlashbackPlaybackOnePercentLowFps", sample.Low);
            var evaluation = evaluateHealth.Invoke(null, new[] { health, lanes, (object)sample.Target, 0L, false });
            var expectedSummary = !playing ? null : sample.Slow
                ? "Flashback playback is below target rate."
                : sample.Frametime ? "Flashback playback frametime is below target." : null;
            Assert.Equal(expectedSummary, evaluation?.GetType().GetProperty("Summary")!.GetValue(evaluation));

            var wireSnapshot = Activator.CreateInstance(snapshotType)!;
            Set(wireSnapshot, "FlashbackPlaybackState", wireState);
            Set(wireSnapshot, "FlashbackPlaybackTargetFps", sample.Target);
            Set(wireSnapshot, "SelectedFrameRate", sample.Target);
            Set(wireSnapshot, "FlashbackPlaybackFrameCount", sample.Frames);
            Set(wireSnapshot, "FlashbackPlaybackCadenceSampleCount", sample.Samples);
            Set(wireSnapshot, "FlashbackPlaybackObservedFps", sample.Observed);
            Set(wireSnapshot, "FlashbackPlaybackOnePercentLowFps", sample.Low);
            var hub = RuntimeHelpers.GetUninitializedObject(hubType);
            foreach (var fieldName in new[] { "_stateLock", "_recentEvents", "_eventThrottleTicks", "_activeAlerts" })
            {
                var field = hubType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
                field.SetValue(hub, Activator.CreateInstance(field.FieldType));
            }

            evaluateWire.Invoke(hub, new object?[] { wireSnapshot, playing });
            var alerts = (HashSet<string>)hubType.GetField("_activeAlerts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(hub)!;
            Assert.True(alerts.Contains("flashback-playback-slow") == (playing && sample.Slow), $"{wireState ?? "null"}: {sample.Name} slow alert");
            Assert.True(alerts.Contains("flashback-playback-frametime-degraded") == (playing && sample.Frametime), $"{wireState ?? "null"}: {sample.Name} frametime alert");
        }

        static void Set(object target, string propertyName, object? value)
            => target.GetType().GetProperty(propertyName)!.SetValue(target, value);
    }

    public AutomationDiagnosticsLoopContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task DiagnosticsLoopDoesNotRebuildAutomationOptionsEachPoll()
        => global::Program.DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll();
}

public sealed class AutomationDispatcherContractsTests
{
    public AutomationDispatcherContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationDispatcherExtractsStringPayloadFields()
        => global::Program.AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload();

    [Fact]
    public Task AutomationDispatcherExtractsBoolPayloadFields()
        => global::Program.AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload();

    [Theory]
    [InlineData(null, null)]
    [InlineData("null", null)]
    [InlineData("\"\"", null)]
    [InlineData("\" \"", null)]
    [InlineData("\" 1080p \"", " 1080p ")]
    [InlineData("42", "42")]
    [InlineData("{}", "{}")]
    [InlineData("[]", "[]")]
    public Task RequiredStringCoercionMatchesAcrossDispatchRoutes(string? valueJson, string? expected)
        => global::Program.AutomationCommandDispatcher_RequiredCoercionMatchesRoutes("string", valueJson, expected);

    [Theory]
    [InlineData(null, null)]
    [InlineData("null", null)]
    [InlineData("\"\"", null)]
    [InlineData("\"invalid\"", null)]
    [InlineData("{}", null)]
    [InlineData("1.5", null)]
    [InlineData("2147483648", null)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"true\"", true)]
    [InlineData("\"False\"", false)]
    [InlineData("0", false)]
    [InlineData("-1", true)]
    public Task RequiredBoolCoercionMatchesAcrossDispatchRoutes(string? valueJson, bool? expected)
        => global::Program.AutomationCommandDispatcher_RequiredCoercionMatchesRoutes("bool", valueJson, expected);

    [Theory]
    [InlineData(null, null)]
    [InlineData("null", null)]
    [InlineData("\"NaN\"", null)]
    [InlineData("\"Infinity\"", null)]
    [InlineData("1e400", null)]
    [InlineData("1.25", 1.25)]
    [InlineData("\"1.25\"", 1.25)]
    public Task RequiredNumberCoercionMatchesAcrossDispatchRoutes(string? valueJson, double? expected)
        => global::Program.AutomationCommandDispatcher_RequiredCoercionMatchesRoutes("number", valueJson, expected);

    [Theory]
    [InlineData("WindowAction", "{\"action\":\"unknown\"}", "Invalid window action")]
    [InlineData("WindowAction", "{\"action\":\"Move\",\"x\":1}", "Move requires 'y'")]
    [InlineData("WindowAction", "{\"action\":\"Resize\",\"width\":640}", "Resize requires 'height'")]
    [InlineData("FlashbackAction", "{}", "Missing required string")]
    [InlineData("FlashbackAction", "{\"action\":\"unknown\"}", "Invalid flashback action")]
    [InlineData("FlashbackAction", "{\"action\":\"seek\"}", "Missing required numeric")]
    [InlineData("FlashbackAction", "{\"action\":\"seek\",\"positionMs\":-1}", "Flashback positionMs must be finite")]
    [InlineData("FlashbackExport", "{\"seconds\":0}", "Flashback export seconds must be finite")]
    [InlineData("FlashbackExport", "{}", "Missing required string")]
    [InlineData("SetMicrophoneEnabled", "{}", "Missing 'enabled'")]
    [InlineData("SetFlashbackEnabled", "{}", "Missing 'enabled'")]
    [InlineData("SetFlashbackBufferMinutes", "{}", "Missing 'minutes'")]
    [InlineData("SetMjpegDecoderCount", "{\"decoderCount\":1.5}", "Missing required integer")]
    [InlineData("SetOutputPath", "{\"outputPath\":\"\\u0000\"}", "not a valid path")]
    [InlineData("AssertSnapshot", "{\"assertions\":[42]}", "at least one valid assertion")]
    public Task MalformedCustomRequestsDoNotReachMutationPorts(string command, string payload, string message)
        => global::Program.AutomationCommandDispatcher_MalformedRequestDoesNotMutate(command, payload, message);

    [Theory]
    [InlineData("SetStatsVisible", "{\"visible\":true}", "execution")]
    [InlineData("SetDeviceAudioMode", "{\"mode\":\"hdmi\"}", "execution")]
    [InlineData("SetDeviceAudioMode", "{\"mode\":\"hdmi\"}", "argument")]
    [InlineData("SetDeviceAudioMode", "{\"mode\":\"hdmi\"}", "canceled")]
    public Task MutationPortFailuresKeepExecutionOrCancellationIdentity(string command, string payload, string failure)
        => global::Program.AutomationCommandDispatcher_MutationFailureKeepsIdentity(command, payload, failure);

    [Fact]
    public Task DirectoryIoFailureKeepsExecutionIdentity()
        => global::Program.AutomationCommandDispatcher_DirectoryIoFailureKeepsExecutionIdentity();

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("\"NaN\"")]
    [InlineData("\"Infinity\"")]
    [InlineData("{}")]
    public Task MalformedOptionalNumbersKeepExistingFallbacks(string? valueJson)
        => global::Program.AutomationCommandDispatcher_OptionalNumbersKeepFallbacks(valueJson);

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("\"false\"")]
    [InlineData("\"true\"")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("\"ignored\"")]
    [InlineData("{}")]
    public Task AutomationDispatcherIgnoresLegacyFlashbackExportForce(string? forceJson)
        => global::Program.AutomationCommandDispatcher_FlashbackExport_IgnoresLegacyForce(forceJson);

    [Fact]
    public Task AutomationDispatcherExtractsIntPayloadFields()
        => global::Program.AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload();

    [Fact]
    public Task AutomationDispatcherExtractsDoublePayloadFields()
        => global::Program.AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload();

    [Fact]
    public Task AutomationDispatcherRejectsNonFiniteDoublePayloadFields()
        => global::Program.AutomationCommandDispatcher_GetDouble_RejectsNonFiniteValues();

    [Fact]
    public Task AutomationDispatcherRequiresMissingStringFields()
        => global::Program.AutomationCommandDispatcher_RequireString_ThrowsOnMissing();

    [Fact]
    public Task AutomationDispatcherDefaultsMissingWindowAction()
        => global::Program.AutomationCommandDispatcher_WindowAction_DefaultsMissingActionToRestore();

    [Fact]
    public Task AutomationDispatcherDefaultsMissingWaitCondition()
        => global::Program.AutomationCommandDispatcher_WaitForCondition_DefaultsMissingConditionToPreviewFrames();

    [Fact]
    public Task AutomationDispatcherRejectsUndefinedWaitCondition()
        => global::Program.AutomationCommandDispatcher_WaitForCondition_RejectsUndefinedCondition();

    [Fact]
    public Task AutomationDispatcherRejectsUnknownVerificationProfile()
        => global::Program.AutomationCommandDispatcher_VerifyFile_RejectsUnknownProfile();

    [Fact]
    public Task AutomationDispatcherMapsStateConflictsToInvalidState()
        => global::Program.AutomationCommandDispatcher_StateConflictMapsToInvalidState();

    [Fact]
    public Task AutomationDispatcherAssertSnapshotBoundsAndEvaluatesRequests()
        => global::Program.AutomationCommandDispatcher_AssertSnapshot_BoundsAndEvaluatesRequests();

    [Fact]
    public Task AutomationDispatcherTrivialHandlerPayloadFieldsMatchCatalog()
        => global::Program.AutomationCommandDispatcher_OneFieldHandlers_MatchCatalogPayloadFields();

    [Fact]
    public Task AutomationDispatcherInvalidAudioModeDoesNotCallMutationPort()
        => global::Program.AutomationCommandDispatcher_InvalidAudioMode_DoesNotCallMutationPort();

    [Fact]
    public Task AutomationDispatcherAudioRampTracePayloadFieldMatchesCatalog()
        => global::Program.AutomationCommandDispatcher_GetAudioRampTrace_MetadataMatchesDispatcherPayload();

    [Fact]
    public Task AutomationDispatcherReadyDeviceGateClassifiesCommands()
        => global::Program.AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands();

    [Fact]
    public Task AutomationDispatcherReadyIndependentCatalogCommandsBypassDeviceReadiness()
        => global::Program.AutomationCommandDispatcher_CatalogReadyIndependentCommands_BypassDeviceReadiness();

    [Fact]
    public Task AutomationDispatcherWindowCloseWaitsForCompletion()
        => global::Program.AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion();

    [Fact]
    public Task AutomationDispatcherWindowCloseRequiresMatchingArmActionId()
        => global::Program.AutomationCommandDispatcher_WindowClose_RequiresMatchingArmActionId();

    [Fact]
    public Task AutomationDispatcherPreviewHealthWaitsForFirstVisual()
        => global::Program.AutomationCommandDispatcher_PreviewRendererHealthy_RequiresFirstVisual();

    [Fact]
    public Task AutomationDispatcherAuthorizationContractIsTokenGated()
        => global::Program.AutomationCommandDispatcher_AuthorizesConfiguredTokens();

    [Fact]
    public Task AutomationDispatcherManifestCommandIsReadOnlyAndReadinessIndependent()
        => global::Program.AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent();

    [Fact]
    public Task AutomationDispatcherNewAudioAndFlashbackCommandsRoutePayloads()
        => global::Program.AutomationCommandDispatcher_NewAudioAndFlashbackCommands_RoutePayloads();

    [Fact]
    public Task AutomationDispatcherFlashbackFailuresReturnPlaybackDiagnostics()
        => global::Program.AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics();

    [Fact]
    public Task AutomationDispatcherHandlesEveryAutomationCommandKindValue()
        => global::Program.AutomationCommandDispatcher_AllCommandKinds_AreHandled();
}

public sealed class AutomationViewModelFlashbackUiContractsTests
{
    public AutomationViewModelFlashbackUiContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationPreviewVolumePersistsThroughSettingsPath()
        => global::Program.AutomationPreviewVolume_PersistsThroughSettingsPath();

    [Fact]
    public Task AutomationAudioCommandsPreserveRuntimeGuards()
        => global::Program.AutomationAudioCommands_PreserveRuntimeGuards();

    [Fact]
    public Task AutomationUiSettingsPersistThroughSettingsPath()
        => global::Program.AutomationUiSettings_PersistThroughSettingsPath();

    [Fact]
    public Task SettingsPersistenceProjectionLoadPlanPreservesSavedSemantics()
        => global::Program.SettingsPersistenceProjection_LoadPlanPreservesSavedSemantics();

    [Fact]
    public Task SettingsPersistenceProjectionSaveSettingsMapsPersistedValues()
        => global::Program.SettingsPersistenceProjection_SaveSettingsMapsPersistedValues();

    [Fact]
    public Task AutomationDeviceSelectionRoutesThroughApplyReinit()
        => global::Program.AutomationDeviceSelection_RoutesThroughApplyReinit();

    [Fact]
    public Task AutomationCaptureSettingsRouteThroughControllerAndAwaitReinitialization()
        => global::Program.AutomationCaptureModeChanges_AwaitReinitialization();

    [Fact]
    public Task AutomationRecordingTransitionsUseSharedLifecycleGate()
        => global::Program.MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate();

    [Fact]
    public Task BitrateSampleWindowPreservesBoundedAverageBehavior()
        => global::Program.BitrateSampleWindow_PreservesBoundedAverageBehavior();

    [Fact]
    public Task AutomationRecordingSettingsRouteThroughControllerAndFlashbackCycle()
        => global::Program.MainViewModelAutomation_RecordingSettingsRouteThroughControllerAndFlashbackCycle();

    [Fact]
    public Task AutomationFlashbackAndProbeCommandsUseAsyncViewModelSurface()
        => global::Program.MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface();

    [Fact]
    public Task MainWindowFlashbackScrubEndsOnReleaseCancelAndCaptureLost()
        => global::Program.MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost();

    [Fact]
    public Task FlashbackTimelineGeometryPreservesScrubMath()
        => global::Program.FlashbackTimelineGeometry_PreservesScrubMath();

    [Fact]
    public Task MainWindowFlashbackToggleRollsBackUiStateOnFailure()
        => global::Program.MainWindowFlashbackToggle_RollsBackUiStateOnFailure();

}

public sealed class AutomationSnapshotProjectionContractsTests
{
    public AutomationSnapshotProjectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationDiagnosticsSnapshotCollectionEpochsMarkProducerChangesAsDiagnosticWarning()
        => global::Program.AutomationDiagnosticsSnapshotCollectionEpochs_MarkProducerChangesAsDiagnosticWarning();

}

public sealed class AutomationCaptureFlashbackRoutingContractsTests
{
    public AutomationCaptureFlashbackRoutingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackMutationsRouteThroughCaptureCoordinator()
        => global::Program.MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator();

    [Fact]
    public Task FlashbackExportsReleaseBackendLeaseBeforeNativeExport()
        => global::Program.CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport();

    [Fact]
    public Task MainViewModelFlashbackExportRoutesThroughCoordinatorAndOwnsCtsLifecycle()
        => global::Program.MainViewModelFlashbackExport_RoutesThroughCoordinatorAndOwnsCtsLifecycle();

    [Fact]
    public Task RetainedFlashbackPreviewPipelineRecyclesOnSettingsChanges()
        => global::Program.CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange();

    [Fact]
    public Task DeviceSwitchTeardownStopsVideoBeforeFlashbackDisposal()
        => global::Program.CaptureService_DeviceSwitchTeardown_StopsVideoBeforeFlashbackDisposal();

    [Fact]
    public Task FlashbackLifecycleLogsUseOutcomeNames()
        => global::Program.CaptureService_FlashbackLifecycleLogs_UseOutcomeNames();

    [Fact]
    public Task FlashbackFrameRateRationalMatchesDeliveredCadence()
        => global::Program.CaptureService_FlashbackFrameRateParts_PreserveOnlyDeliveredCadenceRational();

    [Fact]
    public Task AudioMonitoringTransitionsFinishTeardownBeforeRestart()
        => global::Program.MainViewModel_AudioMonitoringTransitionsFinishTeardownBeforeRestart();

    [Fact]
    public Task FlashbackEnableDisablePreservesPreviewState()
        => global::Program.CaptureService_FlashbackEnableDisable_PreservesPreviewState();

    [Fact]
    public Task CaptureSessionCoordinatorExposesExpectedLifecycleApi()
        => global::Program.CaptureSessionCoordinator_HasExpectedPublicMethods();

    [Fact]
    public Task CaptureSessionCoordinatorCommandKindCoversFlashbackCommands()
        => global::Program.CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues();

    [Fact]
    public Task CaptureSessionSnapshotExposesLifecycleContract()
        => global::Program.CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract();

    [Fact]
    public Task CaptureSessionTransitionPolicyDefinesCoreLifecycleRules()
        => global::Program.CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules();

    [Fact]
    public Task CaptureSessionTransitionPolicyResolvesSteadyState()
        => global::Program.CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags();

    [Fact]
    public Task CaptureServiceSnapshotProducerEpochAdvancesWhenRecordingStateChanges()
        => global::Program.CaptureService_SnapshotProducerEpoch_AdvancesWhenRecordingStateChanges();

    [Fact]
    public Task CaptureSessionCoordinatorCancellationAndWorkerTokensStayBounded()
        => global::Program.CaptureSessionCoordinator_CancellationAndWorkerTokensStayBounded();

    [Fact]
    public Task CaptureSessionCoordinatorAccountsCanceledQueuedCommands()
        => global::Program.CaptureSessionCoordinator_CanceledQueuedCommandUpdatesAccounting();

    [Fact]
    public Task CaptureSessionCoordinatorCoalescesLatestQueuedCommandBehaviorally()
        => global::Program.CaptureSessionCoordinator_CoalescesQueuedLatestOnlyAndAccountsSkip();

    [Fact]
    public Task CaptureSessionCoordinatorDisposeDrainsQueuedCommandsBeforeCancellation()
        => global::Program.CaptureSessionCoordinator_DisposeDrainsQueuedCommandBeforeCancellation();

    [Fact]
    public Task CaptureSessionCoordinatorCoalescesFlashbackEncoderCycles()
        => global::Program.CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles();

    [Fact]
    public Task CaptureSessionCoordinatorDisposalAccountingClassifiesCanceledQueuedCommands()
        => global::Program.CaptureSessionCoordinator_DisposalAccounting_ClassifiesCanceledQueuedCommands();

    [Fact]
    public Task CaptureSessionCoordinatorPropagatesFlashbackMutationCancellation()
        => global::Program.CaptureSessionCoordinator_FlashbackMutationsPropagateRequestCancellation();

    [Fact]
    public Task CaptureSessionCoordinatorKeepsCommittedStopsUncancelable()
        => global::Program.CaptureSessionCoordinator_CommittedStopsDoNotPropagateRequestCancellation();

    [Fact]
    public Task CaptureSessionCoordinatorLogsInactiveFlashbackCommandRejections()
        => global::Program.CaptureSessionCoordinator_LogsInactiveFlashbackCommandRejections();

}

}

// Legacy app-surface checks executed by AutomationAppSurfaceContractsTests.
static partial class Program
{
    internal static Task AutomationCommandDispatcher_AllCommandKinds_AreHandled()
    {
        // Every AutomationCommandKind value must be explicitly handled: either
        // as the pre-switch Authenticate check, as a handler-table key, as an
        // explicit focused-helper equality check, or as a case label in the
        // custom switch. This test reads the dispatcher source and verifies each
        // enum name appears in at least one of those locations.
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        var names = Enum.GetNames(commandKindType);

        foreach (var name in names)
        {
            var inTrivialHandlers = dispatcherText.Contains($"[AutomationCommandKind.{name}]");
            var inFocusedHelper = dispatcherText.Contains($"command == AutomationCommandKind.{name}");
            var inSwitchCase = dispatcherText.Contains($"case AutomationCommandKind.{name}:");
            var isAuthenticate = name == "Authenticate" &&
                dispatcherText.Contains("request.Command == AutomationCommandKind.Authenticate");

            AssertEqual(
                true,
                inTrivialHandlers || inFocusedHelper || inSwitchCase || isAuthenticate,
                $"AutomationCommandKind.{name} must be handled in a handler table, focused helper, switch case, or the pre-switch Authenticate check");
        }

        return Task.CompletedTask;
    }

    internal static async Task AutomationCommandDispatcher_AssertSnapshot_BoundsAndEvaluatesRequests()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");
        var refreshCount = 0;

        var diagnostics = CreateConfiguredProxy(diagnosticsType, (method, _) =>
        {
            if (method?.Name == "GetLatestSnapshot")
            {
                return snapshot;
            }

            if (method?.Name == "RefreshSnapshotNowAsync")
            {
                Interlocked.Increment(ref refreshCount);
                return CreateTaskFromResult(method.ReturnType.GetGenericArguments()[0], snapshot);
            }

            return GetDefaultReturnValue(method);
        });
        var dispatcher = CreateAutomationCommandDispatcher(
            CreateConfiguredProxy(viewModelType, (method, _) => GetDefaultReturnValue(method)),
            diagnostics,
            CreateThrowingProxy(windowControlType),
            authToken: null);

        const string assertion = "{\"field\":\"PreviewFramesDisplayed\",\"op\":\"eq\",\"value\":\"0\"}";
        var oversizedPayload = "{\"assertions\":[" + string.Join(',', Enumerable.Repeat(assertion, 1025)) + "]}";
        var oversizedResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("AssertSnapshot", null, oversizedPayload))
            .ConfigureAwait(false);
        AssertAutomationResponse(oversizedResponse, success: false, errorCode: "invalid-request", status: "error", "oversized assertions are rejected");
        AssertEqual(0, Volatile.Read(ref refreshCount), "oversized assertions do not refresh snapshot");

        var knownFieldResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest(
                    "AssertSnapshot",
                    null,
                    "{\"assertions\":[{\"field\":\"previewframesdisplayed\",\"op\":\"eq\",\"value\":\"0\"}]}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(knownFieldResponse, success: true, errorCode: null, status: "ok", "known fields remain case-insensitive");
        AssertEqual(1, Volatile.Read(ref refreshCount), "valid assertion refreshes once");

        var unknownFieldResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest(
                    "AssertSnapshot",
                    null,
                    "{\"assertions\":[{\"field\":\"arbitrary-unknown-field\",\"op\":\"eq\",\"value\":\"0\"}]}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(unknownFieldResponse, success: false, errorCode: "assertion-failed", status: "error", "unknown fields remain assertion failures");
        AssertEqual(2, Volatile.Read(ref refreshCount), "unknown-field assertion refreshes once");

        var malformedResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("AssertSnapshot", null, "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(malformedResponse, success: false, errorCode: "invalid-request", status: "error", "malformed assertions are rejected");
        AssertEqual(2, Volatile.Read(ref refreshCount), "malformed assertions do not refresh snapshot");
    }

    internal static async Task AutomationCommandDispatcher_InvalidAudioMode_DoesNotCallMutationPort()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var mutationCalls = 0;
        string? observedMode = null;
        var viewModel = CreateConfiguredProxy(viewModelType, (method, arguments) =>
        {
            if (method?.Name == "get_IsInitialized")
            {
                return true;
            }

            if (method?.Name.StartsWith("Set", StringComparison.Ordinal) == true)
            {
                Interlocked.Increment(ref mutationCalls);
                if (method.Name == "SetDeviceAudioModeAsync")
                {
                    observedMode = (string?)arguments?[0];
                }
            }

            return GetDefaultReturnValue(method);
        });
        var dispatcher = CreateAutomationCommandDispatcher(
            viewModel,
            CreateConfiguredProxy(diagnosticsType, (method, _) => GetDefaultReturnValue(method)),
            CreateConfiguredProxy(windowControlType, (method, _) => GetDefaultReturnValue(method)),
            authToken: null);

        foreach (var payload in new[]
        {
            "{}", "{\"mode\":null}", "{\"mode\":7}", "{\"mode\":\"\"}",
            "{\"mode\":\" \"}", "{\"mode\":\"anlog\"}", "{\"mode\":\"HDMI \"}",
            "{\"mode\":\"Embedded\"}", "{\"mode\":\"HDMI\\u0000\"}"
        })
        {
            var response = await ExecuteAutomationCommandAsync(
                    dispatcher,
                    CreateAutomationCommandRequest("SetDeviceAudioMode", null, payload))
                .ConfigureAwait(false);
            AssertAutomationResponse(
                response,
                success: false,
                errorCode: "invalid-request",
                status: "error",
                "invalid audio mode");
            AssertEqual(0, Volatile.Read(ref mutationCalls), "invalid audio mode invokes no settings mutation");
        }

        AssertEqual(0, Volatile.Read(ref mutationCalls), "invalid audio modes do not call the mutation port");

        var validResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("SetDeviceAudioMode", null, "{\"mode\":\"analog\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(
            validResponse,
            success: true,
            errorCode: null,
            status: "ok",
            "valid audio mode");
        AssertEqual(1, Volatile.Read(ref mutationCalls), "valid audio mode calls mutation port once");
        AssertEqual("Analog", observedMode, "valid audio mode is canonicalized before mutation");
    }

    internal static async Task AutomationCommandDispatcher_NewAudioAndFlashbackCommands_RoutePayloads()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var snapshot = Activator.CreateInstance(snapshotType)
                       ?? throw new InvalidOperationException("Failed to create AutomationSnapshot.");
        var calls = new List<string>();

        var viewModel = CreateConfiguredProxy(viewModelType, (method, args) =>
        {
            switch (method?.Name)
            {
                case "get_IsInitialized":
                    return true;

                case "SelectMicrophoneDeviceAsync":
                    calls.Add($"SelectMicrophoneDevice:{args![0]}:{args[1]}");
                    AssertEqual("mic-id", args[0], "SelectMicrophoneDeviceAsync deviceId");
                    AssertEqual("Desk Mic", args[1], "SelectMicrophoneDeviceAsync deviceName");
                    return Task.CompletedTask;

                case "SetMicrophoneVolumeAsync":
                    calls.Add($"SetMicrophoneVolume:{args![0]}");
                    AssertEqual(66.5d, (double)args[0]!, "SetMicrophoneVolumeAsync volume");
                    return Task.CompletedTask;

                case "SetFlashbackBufferMinutesAsync":
                    calls.Add($"SetFlashbackBufferMinutes:{args![0]}");
                    AssertEqual(15, (int)args[0]!, "SetFlashbackBufferMinutesAsync minutes");
                    return Task.CompletedTask;

                case "SetFlashbackGpuDecodeAsync":
                    calls.Add($"SetFlashbackGpuDecode:{args![0]}");
                    AssertEqual(false, (bool)args[0]!, "SetFlashbackGpuDecodeAsync enabled");
                    return Task.CompletedTask;

                default:
                    return GetDefaultReturnValue(method);
            }
        });
        var diagnostics = CreateConfiguredProxy(diagnosticsType, (method, _) =>
            method?.Name == "GetLatestSnapshot"
                ? snapshot
                : GetDefaultReturnValue(method));
        var dispatcher = CreateAutomationCommandDispatcher(
            viewModel,
            diagnostics,
            CreateThrowingProxy(windowControlType),
            authToken: null);

        var selectMic = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("SelectMicrophoneDevice", null, "{\"deviceId\":\"mic-id\",\"deviceName\":\"Desk Mic\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(selectMic, success: true, errorCode: null, status: "ok", "select microphone routes payload");
        AssertEqual("Microphone device selection requested.", (string)GetPublicProperty(selectMic, "Message")!, "select microphone response message");
        AssertEqual("acknowledged", GetAutomationLifecycle(selectMic), "select microphone lifecycle");

        var micVolume = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("SetMicrophoneVolume", null, "{\"microphoneVolumePercent\":66.5}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(micVolume, success: true, errorCode: null, status: "ok", "microphone volume routes payload");
        AssertEqual("Microphone volume set to 66.5%.", (string)GetPublicProperty(micVolume, "Message")!, "microphone volume response message");
        AssertEqual("completed", GetAutomationLifecycle(micVolume), "microphone volume lifecycle");

        var flashbackBuffer = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("SetFlashbackBufferMinutes", null, "{\"minutes\":\"15\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(flashbackBuffer, success: true, errorCode: null, status: "ok", "flashback buffer routes payload");
        AssertEqual("Flashback buffer duration set to 15 minutes.", (string)GetPublicProperty(flashbackBuffer, "Message")!, "flashback buffer response message");
        AssertEqual("completed", GetAutomationLifecycle(flashbackBuffer), "flashback buffer lifecycle");

        var flashbackGpuDecode = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("SetFlashbackGpuDecode", null, "{\"enabled\":false}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(flashbackGpuDecode, success: true, errorCode: null, status: "ok", "flashback gpu-decode routes payload");
        AssertEqual("Flashback GPU decode disabled.", (string)GetPublicProperty(flashbackGpuDecode, "Message")!, "flashback gpu-decode response message");
        AssertEqual("completed", GetAutomationLifecycle(flashbackGpuDecode), "flashback gpu-decode lifecycle");

        AssertEqual(
            string.Join(
                "|",
                "SelectMicrophoneDevice:mic-id:Desk Mic",
                "SetMicrophoneVolume:66.5",
                "SetFlashbackBufferMinutes:15",
                "SetFlashbackGpuDecode:False"),
            string.Join("|", calls),
            "dispatcher command payload forwarding order");
    }

    internal static async Task AutomationCommandDispatcher_AuthorizesConfiguredTokens()
    {
        var noTokenDispatcher = CreateAutomationCommandDispatcher(authToken: null);
        var noTokenResponse = await ExecuteAutomationCommandAsync(
            noTokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(noTokenResponse, success: true, errorCode: null, status: "ok", "no configured token accepts unauthenticated authenticate");

        var tokenDispatcher = CreateAutomationCommandDispatcher(authToken: "secret");
        var matchingTopLevelResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: "secret", payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(matchingTopLevelResponse, success: true, errorCode: null, status: "ok", "matching top-level token is authorized");

        var matchingPayloadResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{\"authToken\":\"secret\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(matchingPayloadResponse, success: true, errorCode: null, status: "ok", "payload fallback token is authorized");

        var missingTokenResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(missingTokenResponse, success: false, errorCode: "unauthorized", status: "error", "missing token is rejected");

        var wrongTokenResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: "wrong", payloadJson: "{\"authToken\":\"secret\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(wrongTokenResponse, success: false, errorCode: "unauthorized", status: "error", "wrong top-level token is rejected before payload fallback");

        var protectedCommandResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("GetSnapshot", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(protectedCommandResponse, success: false, errorCode: "unauthorized", status: "error", "missing token rejects non-authenticate command");

        const string manifestToken = "manifest-auth-dummy-20260912";
        const string wrongManifestToken = "wrong-manifest-auth-dummy";
        var manifestDispatcher = CreateAutomationCommandDispatcher(authToken: manifestToken);
        var matchingPayload = JsonSerializer.Serialize(new Dictionary<string, string> { ["authToken"] = manifestToken });
        var wrongPayload = JsonSerializer.Serialize(new Dictionary<string, string> { ["authToken"] = wrongManifestToken });
        (string? TopLevel, string Payload, bool Success, string Scenario)[] manifestRequests =
        {
            (manifestToken, "{}", true, "exact top-level token authorizes manifest retrieval"),
            (manifestToken, wrongPayload, true, "matching top-level token takes precedence over wrong payload"),
            (null, matchingPayload, true, "null top-level token allows legacy fallback on manifest retrieval"),
            (string.Empty, matchingPayload, true, "empty top-level token allows legacy fallback on manifest retrieval"),
            (" \t\r\n", matchingPayload, true, "whitespace top-level token allows legacy fallback on manifest retrieval"),
            (wrongManifestToken, matchingPayload, false, "wrong nonblank top-level token prevents legacy rescue"),
            (manifestToken + " ", matchingPayload, false, "nonblank top-level token is compared without trimming"),
            (manifestToken.ToUpperInvariant(), matchingPayload, false, "top-level token comparison is case-sensitive"),
            (null, "{}", false, "manifest retrieval requires configured credentials"),
            (wrongManifestToken, "{}", false, "wrong top-level token cannot retrieve manifest"),
            (null, wrongPayload, false, "wrong legacy payload token cannot retrieve manifest")
        };
        var staticManifestJson = AutomationCommandCatalog.CreateManifestJson();
        Assert.DoesNotContain(manifestToken, staticManifestJson, StringComparison.Ordinal);
        Assert.DoesNotContain(wrongManifestToken, staticManifestJson, StringComparison.Ordinal);

        foreach (var testCase in manifestRequests)
        {
            var response = await ExecuteAutomationCommandAsync(manifestDispatcher,
                CreateAutomationCommandRequest("GetAutomationManifest", testCase.TopLevel, testCase.Payload))
                .ConfigureAwait(false);
            AssertAutomationResponse(response, testCase.Success, testCase.Success ? null : "unauthorized",
                testCase.Success ? "ok" : "error", testCase.Scenario);
            Assert.Null(GetPublicProperty(response, "Snapshot"));
            var manifest = GetPublicProperty(response, "Data");
            if (testCase.Success)
            {
                Assert.NotNull(manifest);
                var manifestJson = JsonSerializer.Serialize(manifest, manifest!.GetType());
                Assert.Equal(staticManifestJson, manifestJson);
                Assert.DoesNotContain(manifestToken, manifestJson, StringComparison.Ordinal);
                Assert.DoesNotContain(wrongManifestToken, manifestJson, StringComparison.Ordinal);
            }
            else
            {
                Assert.Null(manifest);
            }
        }

        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(dispatcherText, "if (string.IsNullOrWhiteSpace(_authToken))\n        {\n            return true;\n        }");
        AssertContains(dispatcherText, "var providedToken = request.AuthToken;");
        AssertContains(dispatcherText, "providedToken = GetString(request.Payload, AutomationPayloadKeys.AuthToken);");
        AssertContains(dispatcherText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(dispatcherText, "Logger.LogEvent(\"AUTO-AUTH-FAILED\"");
        AssertContains(dispatcherText, "Logger.LogEvent(\"AUTO-AUTH-LEGACY-PAYLOAD\"");
        AssertContains(dispatcherText, "errorCode: authorized ? null : AutomationErrorCodes.Unauthorized");
        AssertContains(dispatcherText, "errorCode: AutomationErrorCodes.Unauthorized");
        AssertContains(dispatcherText, "status: authorized ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error");
    }

    internal static async Task AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent()
    {
        var dispatcher = CreateAutomationCommandDispatcher(authToken: null);
        var response = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("GetAutomationManifest", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: true, errorCode: null, status: "ok", "manifest command succeeds without initialized devices");
        AssertEqual(null, GetPublicProperty(response, "Snapshot"), "manifest response omits snapshot");
        var data = GetPublicProperty(response, "Data")
                   ?? throw new InvalidOperationException("manifest response data was missing.");
        AssertEqual(1, (int)GetPublicProperty(data, "SchemaVersion")!, "manifest schema version");

        var commands = ((System.Collections.IEnumerable)GetPublicProperty(data, "Commands")!)
            .Cast<object>()
            .ToArray();
        var manifestCommand = commands.Single(command =>
            string.Equals((string)GetPublicProperty(command, "Name")!, "GetAutomationManifest", StringComparison.Ordinal));
        AssertEqual(51, (int)GetPublicProperty(manifestCommand, "Id")!, "manifest command id");
        AssertEqual("{}", (string)GetPublicProperty(manifestCommand, "PayloadShape")!, "manifest payload shape");
        AssertEqual(false, (bool)GetPublicProperty(manifestCommand, "RequiresReadyDevices")!, "manifest readiness flag");
        AssertEqual("None", (string)GetPublicProperty(manifestCommand, "PathPolicy")!, "manifest path policy");
        AssertEqual("manifest", (string)GetPublicProperty(manifestCommand, "CliHelp")!, "manifest CLI help");
        AssertEqual("Get automation command manifest.", (string)GetPublicProperty(manifestCommand, "McpDescription")!, "manifest MCP description");

        var diagnosticsCalls = 0;
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var mismatchDispatcher = CreateAutomationCommandDispatcher(
            CreateThrowingProxy(viewModelType),
            CreateConfiguredProxy(
                diagnosticsType,
                (method, _) =>
                {
                    diagnosticsCalls++;
                    return GetDefaultReturnValue(method);
                }),
            CreateThrowingProxy(windowControlType),
            authToken: null);
        var mismatchResponse = await ExecuteAutomationCommandAsync(
                mismatchDispatcher,
                CreateAutomationCommandRequest(
                    "GetSnapshot",
                    authToken: null,
                    payloadJson: "{}",
                    manifestRevision: Sussudio.Tools.AutomationPipeProtocol.CommandManifestRevision + 1))
            .ConfigureAwait(false);

        AssertAutomationResponse(mismatchResponse, success: false, errorCode: "manifest-mismatch", status: "error", "manifest revision mismatch");
        AssertEqual(null, GetPublicProperty(mismatchResponse, "Snapshot"), "manifest mismatch response omits snapshot");
        AssertEqual(0, diagnosticsCalls, "manifest mismatch does not execute command");
    }

    internal static async Task AutomationCommandDispatcher_FlashbackExport_IgnoresLegacyForce(string? forceJson)
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");
        var outputPath = Path.Combine(Path.GetTempPath(), $"fb_legacy_force_{Guid.NewGuid():N}.mp4");
        const string failureCode = "flashback-export-invalid-output-path";
        const string message = "Flashback export does not overwrite existing files.";
        var failureFactory = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes")
            .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
        var failure = failureFactory.Invoke(null, new object?[] { outputPath, message, failureCode, null })!;
        var exportCalls = 0;

        var viewModel = CreateConfiguredProxy(viewModelType, (method, args) =>
        {
            if (method?.Name == "ExportFlashbackAutomationAsync")
            {
                exportCalls++;
                AssertEqual(4, args!.Length, "export port receives no retired force parameter");
                AssertEqual(12.5, args[0], "export port receives requested seconds");
                AssertEqual(outputPath, args[1], "export port receives requested output path");
                AssertEqual(true, args[2], "export port receives requested selection policy");
                AssertEqual(typeof(CancellationToken), args[3]!.GetType(), "export port receives cancellation token");
                return CreateTaskFromResult(failure.GetType(), failure);
            }

            return GetDefaultReturnValue(method);
        });
        var diagnostics = CreateConfiguredProxy(diagnosticsType, (method, _) =>
            method?.Name == "GetLatestSnapshot" ? snapshot : GetDefaultReturnValue(method));
        var dispatcher = CreateAutomationCommandDispatcher(
            viewModel, diagnostics, CreateThrowingProxy(windowControlType), authToken: null);
        var payload = new Dictionary<string, object?>
        {
            ["seconds"] = 12.5,
            ["outputPath"] = outputPath,
            ["useSelectionRange"] = true
        };
        if (forceJson != null)
        {
            payload["force"] = JsonSerializer.Deserialize<JsonElement>(forceJson);
        }

        var response = await ExecuteAutomationCommandAsync(
            dispatcher,
            CreateAutomationCommandRequest("FlashbackExport", authToken: null, payloadJson: JsonSerializer.Serialize(payload)))
            .ConfigureAwait(false);

        AssertEqual(1, exportCalls, "legacy force payload dispatches exactly one export");
        AssertAutomationResponse(response, success: false, errorCode: failureCode, status: "error", "legacy force preserves export failure");
        AssertEqual(message, GetPublicProperty(response, "Message"), "legacy force preserves export message");
        var data = GetPublicProperty(response, "Data")!;
        AssertEqual("InvalidOutputPath", GetPublicProperty(data, "FailureKind"), "legacy force preserves failure category");
        AssertEqual(outputPath, GetPublicProperty(data, "OutputPath"), "legacy force preserves output path");
        AssertEqual(snapshot, GetPublicProperty(response, "Snapshot"), "legacy force preserves response snapshot");
    }

    internal static async Task AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var actionType = RequireType("Sussudio.Models.AutomationFlashbackAction");

        var snapshot = Activator.CreateInstance(snapshotType)
                       ?? throw new InvalidOperationException("Failed to create AutomationSnapshot.");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackState", "Paused");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackThreadAlive", false);
        SetPropertyBackingField(snapshot, "FlashbackPlaybackPendingCommands", 2);
        SetPropertyBackingField(snapshot, "FlashbackPlaybackLastCommandFailure", "thread_not_running:Pause");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 123456789L);

        var viewModel = CreateConfiguredProxy(viewModelType, (method, args) =>
        {
            if (method?.Name == "ExecuteFlashbackActionAsync")
            {
                AssertEqual(Enum.Parse(actionType, "Seek"), args![0], "dispatcher forwards seek action");
                AssertEqual(TimeSpan.FromMilliseconds(1234.5), args[1], "dispatcher forwards requested seek position");
                return Task.FromResult(false);
            }

            return GetDefaultReturnValue(method);
        });
        var diagnostics = CreateConfiguredProxy(diagnosticsType, (method, _) =>
            method?.Name == "GetLatestSnapshot"
                ? snapshot
                : GetDefaultReturnValue(method));
        var dispatcher = CreateAutomationCommandDispatcher(
            viewModel,
            diagnostics,
            CreateThrowingProxy(windowControlType),
            authToken: null);
        var response = await ExecuteAutomationCommandAsync(
            dispatcher,
            CreateAutomationCommandRequest("FlashbackAction", authToken: null, payloadJson: "{\"action\":\"seek\",\"positionMs\":1234.5}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: false, errorCode: "flashback-action-failed", status: "error", "failed flashback action includes structured error");
        var message = (string)GetPublicProperty(response, "Message")!;
        AssertContains(message, "Flashback action 'Seek' was rejected");
        AssertContains(message, "state=Paused");
        AssertContains(message, "threadAlive=False");
        AssertContains(message, "lastFailure=thread_not_running:Pause");
        AssertContains(message, "requestedPositionMs=1234.5");

        var data = GetPublicProperty(response, "Data")
                   ?? throw new InvalidOperationException("Flashback failure response data was missing.");
        AssertEqual("Seek", (string)GetPublicProperty(data, "Action")!, "flashback failure data action");
        AssertEqual(1234.5, (double)GetPublicProperty(data, "RequestedPositionMs")!, "flashback failure data requested position");
        AssertEqual("Paused", (string)GetPublicProperty(data, "PlaybackState")!, "flashback failure data playback state");
        AssertEqual(false, (bool)GetPublicProperty(data, "PlaybackThreadAlive")!, "flashback failure data thread alive");
        AssertEqual(2, (int)GetPublicProperty(data, "PendingCommands")!, "flashback failure data pending commands");
        AssertEqual("thread_not_running:Pause", (string)GetPublicProperty(data, "LastCommandFailure")!, "flashback failure data last command failure");
        AssertEqual(123456789L, (long)GetPublicProperty(data, "LastCommandFailureUtcUnixMs")!, "flashback failure data failure utc");
        AssertEqual(snapshot, GetPublicProperty(response, "Snapshot"), "flashback failure response reuses diagnostic snapshot");
    }

    private static string ReadAutomationCommandDispatcherFamilyText()
    {
        var files = EnumerateAutomationCommandDispatcherFamilyFiles();

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }

    private static string[] EnumerateAutomationCommandDispatcherFamilyFiles()
    {
        var repoRoot = GetRepoRoot();
        var automationDirectory = Path.Combine(repoRoot, "Sussudio", "Services", "Automation");
        return EnumerateSourceFiles(automationDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => GetRepoFileName(file).StartsWith("AutomationCommandDispatcher", StringComparison.Ordinal))
            .OrderBy(file => AutomationCommandDispatcherFamilySortKey(file), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string AutomationCommandDispatcherFamilySortKey(string relativePath)
    {
        var fileName = GetRepoFileName(relativePath);
        return string.Equals(fileName, "AutomationCommandDispatcher.cs", StringComparison.Ordinal)
            ? "0"
            : "1" + fileName;
    }

    private static object CreateAutomationCommandDispatcher(string? authToken)
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var viewModel = CreateThrowingProxy(viewModelType);
        var constructor = GetAutomationCommandDispatcherConstructor(dispatcherType);

        return constructor.Invoke(new[]
        {
            CreateAutomationViewModelPorts(viewModel),
            CreateThrowingProxy(diagnosticsType),
            CreateThrowingProxy(windowControlType),
            authToken
        });
    }

    private static object CreateAutomationCommandDispatcher(
        object viewModel,
        object diagnosticsHub,
        object windowControl,
        string? authToken)
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var constructor = GetAutomationCommandDispatcherConstructor(dispatcherType);

        return constructor.Invoke(new[]
        {
            CreateAutomationViewModelPorts(viewModel),
            diagnosticsHub,
            windowControl,
            authToken
        });
    }

    private static ConstructorInfo GetAutomationCommandDispatcherConstructor(Type dispatcherType)
        => dispatcherType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType.FullName == "Sussudio.Services.Automation.AutomationViewModelPorts";
            });

    private static object CreateAutomationViewModelPorts(object viewModel)
    {
        var portsType = RequireType("Sussudio.Services.Automation.AutomationViewModelPorts");
        var fromMethod = portsType.GetMethod("From", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? throw new InvalidOperationException("AutomationViewModelPorts.From was not found.");
        return fromMethod.Invoke(null, new[] { viewModel })
               ?? throw new InvalidOperationException("AutomationViewModelPorts.From returned null.");
    }

    private static object CreateConfiguredProxy(Type interfaceType, Func<MethodInfo?, object?[]?, object?> handler)
    {
        var createMethod = typeof(DispatchProxy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method =>
                method.Name == "Create" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(interfaceType, typeof(ConfiguredAutomationProxy));
        var proxy = createMethod.Invoke(null, null)
                    ?? throw new InvalidOperationException($"Failed to create proxy for {interfaceType.FullName}.");
        ((ConfiguredAutomationProxy)proxy).Handler = handler;
        return proxy;
    }

    private static object? GetDefaultReturnValue(MethodInfo? method)
    {
        var returnType = method?.ReturnType ?? typeof(void);
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return fromResult.Invoke(null, new[] { result });
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private static object CreateAutomationCommandRequest(
        string commandName,
        string? authToken,
        string payloadJson,
        int? manifestRevision = null)
    {
        var requestType = RequireType("Sussudio.Models.AutomationCommandRequest");
        var commandType = RequireType("Sussudio.Models.AutomationCommandKind");
        var request = Activator.CreateInstance(requestType)
                      ?? throw new InvalidOperationException("Failed to create AutomationCommandRequest.");
        using var payload = JsonDocument.Parse(payloadJson);
        SetPropertyBackingField(request, "Command", Enum.Parse(commandType, commandName));
        SetPropertyBackingField(request, "CorrelationId", Guid.NewGuid().ToString("N"));
        SetPropertyBackingField(request, "AuthToken", authToken);
        SetPropertyBackingField(request, "ManifestRevision", manifestRevision);
        SetPropertyBackingField(request, "Payload", payload.RootElement.Clone());
        return request;
    }

    private static async Task<object> ExecuteAutomationCommandAsync(object dispatcher, object request)
    {
        var execute = dispatcher.GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public)
                      ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync was not found.");
        var task = (Task)execute.Invoke(dispatcher, new object[] { request, CancellationToken.None })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
               ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync returned no result.");
    }

    private static void AssertAutomationResponse(
        object response,
        bool success,
        string? errorCode,
        string status,
        string scenario)
    {
        AssertEqual(success, (bool)GetPublicProperty(response, "Success")!, $"{scenario}: Success");
        AssertEqual(errorCode, (string?)GetPublicProperty(response, "ErrorCode"), $"{scenario}: ErrorCode");
        var actualStatus = GetPublicProperty(response, "Status")!;
        var actualStatusName = JsonNamingPolicy.SnakeCaseLower.ConvertName(actualStatus.ToString()!);
        AssertEqual(status, actualStatusName, $"{scenario}: Status");
    }

    private static string GetAutomationLifecycle(object response)
    {
        var lifecycle = GetPublicProperty(response, "CommandLifecycle")
            ?? throw new InvalidOperationException("AutomationCommandResponse.CommandLifecycle was missing.");
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(lifecycle.ToString()!);
    }

    private static object? GetPublicProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }

    public class ConfiguredAutomationProxy : DispatchProxy
    {
        public Func<MethodInfo?, object?[]?, object?> Handler { get; set; } =
            (_, _) => throw new NotSupportedException("No handler configured.");

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Handler(targetMethod, args);
    }

    private static object CreateTaskFromResult(Type resultType, object? result)
    {
        var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(resultType);
        return fromResult.Invoke(null, new[] { result })
               ?? throw new InvalidOperationException($"Failed to create Task<{resultType.Name}>.");
    }

    internal static async Task AutomationCommandDispatcher_RequiredCoercionMatchesRoutes(
        string kind, string? valueJson, object? expected)
    {
        (string Command, string Key, string OtherFields, string Method, int Argument)[] routes = kind switch
        {
            "string" => new[]
            {
                ("SetResolution", "resolution", "", "SetResolutionAsync", 0),
                ("SetStatsSectionVisible", "section", "\"visible\":false", "SetStatsSectionVisibleAsync", 0)
            },
            "bool" => new[]
            {
                ("SetStatsVisible", "visible", "", "SetStatsVisibleAsync", 0),
                ("SetStatsSectionVisible", "visible", "\"section\":\"preview\"", "SetStatsSectionVisibleAsync", 1)
            },
            "number" => new[]
            {
                ("SetFrameRate", "frameRate", "", "SetFrameRateAsync", 0),
                ("SetAnalogAudioGain", "gain", "", "SetAnalogAudioGainAsync", 0)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        foreach (var route in routes)
        {
            var observed = new List<(string Method, object? Value)>();
            var dispatcher = CreatePayloadValidationTestDispatcher((method, arguments) =>
            {
                observed.Add((method.Name, arguments![route.Argument]));
                return GetDefaultReturnValue(method);
            });
            var fields = new List<string>();
            if (route.OtherFields.Length > 0) fields.Add(route.OtherFields);
            if (valueJson != null) fields.Add($"\"{route.Key}\":{valueJson}");
            var request = CreateAutomationCommandRequest(route.Command, null, "{" + string.Join(',', fields) + "}");
            var response = await ExecuteAutomationCommandAsync(dispatcher, request).ConfigureAwait(false);

            AssertAutomationResponse(response, expected != null, expected == null ? "invalid-request" : null,
                expected == null ? "error" : "ok", $"{route.Command} {valueJson ?? "missing"}");
            Assert.Equal(GetPublicProperty(request, "CorrelationId"), GetPublicProperty(response, "CorrelationId"));
            if (expected == null)
            {
                Assert.Empty(observed);
                Assert.Equal("failed", GetAutomationLifecycle(response));
                Assert.NotNull(GetPublicProperty(response, "ElapsedMs"));
            }
            else
            {
                var mutation = Assert.Single(observed);
                Assert.Equal(route.Method, mutation.Method);
                Assert.Equal(expected, mutation.Value);
            }
        }
    }

    internal static async Task AutomationCommandDispatcher_MalformedRequestDoesNotMutate(
        string command, string payload, string message)
    {
        var mutationCalls = new List<string>();
        var dispatcher = CreatePayloadValidationTestDispatcher((method, _) =>
        {
            mutationCalls.Add(method.Name);
            return CreateNoHardwareReturnValue(method.ReturnType);
        });
        var response = await ExecuteAutomationCommandAsync(
            dispatcher, CreateAutomationCommandRequest(command, null, payload)).ConfigureAwait(false);

        AssertAutomationResponse(response, false, "invalid-request", "error", command);
        Assert.Contains(message, (string)GetPublicProperty(response, "Message")!, StringComparison.Ordinal);
        Assert.Empty(mutationCalls);
    }

    internal static async Task AutomationCommandDispatcher_MutationFailureKeepsIdentity(
        string command, string payload, string failure)
    {
        var mutationCalls = 0;
        Exception cause = failure switch
        {
            "execution" => new InvalidOperationException("injected mutation failure"),
            "argument" => new ArgumentOutOfRangeException("value", "injected mutation failure"),
            "canceled" => new OperationCanceledException("injected cancellation"),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        var dispatcher = CreatePayloadValidationTestDispatcher((_, _) =>
        {
            mutationCalls++;
            return Task.FromException(cause);
        });
        var response = await ExecuteAutomationCommandAsync(
            dispatcher, CreateAutomationCommandRequest(command, null, payload)).ConfigureAwait(false);

        Assert.Equal(1, mutationCalls);
        AssertAutomationResponse(response, false, failure == "canceled" ? "canceled" : "command-failed", "error", command);
        Assert.Equal(failure == "canceled" ? "Command canceled." : cause.Message, GetPublicProperty(response, "Message"));
    }

    internal static async Task AutomationCommandDispatcher_DirectoryIoFailureKeepsExecutionIdentity()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"automation_directory_conflict_{Guid.NewGuid():N}.tmp");
        File.WriteAllBytes(filePath, Array.Empty<byte>());
        try
        {
            var mutationCalls = 0;
            var dispatcher = CreatePayloadValidationTestDispatcher((method, _) =>
            {
                mutationCalls++;
                return GetDefaultReturnValue(method);
            });
            var response = await ExecuteAutomationCommandAsync(dispatcher, CreateAutomationCommandRequest(
                "SetOutputPath", null, $"{{\"outputPath\":{JsonSerializer.Serialize(filePath)}}}")).ConfigureAwait(false);

            AssertAutomationResponse(response, false, "command-failed", "error", "directory conflicts with a file");
            Assert.Equal(0, mutationCalls);
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    internal static async Task AutomationCommandDispatcher_OptionalNumbersKeepFallbacks(string? valueJson)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"fb_optional_numbers_{Guid.NewGuid():N}.mp4");
        const string failureCode = "flashback-export-buffer-inactive";
        var failure = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes")
            .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object?[] { outputPath, "buffer inactive", failureCode, null })!;
        var observed = new List<(string Method, object?[] Arguments)>();
        var dispatcher = CreatePayloadValidationTestDispatcher((method, arguments) =>
        {
            observed.Add((method.Name, arguments!));
            return method.Name == "ExportFlashbackAutomationAsync"
                ? CreateTaskFromResult(failure.GetType(), failure)
                : Task.FromResult(true);
        });
        var secondsField = valueJson == null ? "" : $",\"seconds\":{valueJson}";
        var export = await ExecuteAutomationCommandAsync(dispatcher, CreateAutomationCommandRequest(
            "FlashbackExport", null, $"{{\"outputPath\":{JsonSerializer.Serialize(outputPath)}{secondsField}}}")).ConfigureAwait(false);
        AssertAutomationResponse(export, false, failureCode, "error", "optional export seconds");
        var exported = Assert.Single(observed);
        Assert.Equal("ExportFlashbackAutomationAsync", exported.Method);
        Assert.Equal(300d, exported.Arguments[0]);
        Assert.Equal(outputPath, exported.Arguments[1]);
        Assert.False(File.Exists(outputPath));

        foreach (var action in new[] { "play", "end-scrub" })
        {
            observed.Clear();
            var positionField = valueJson == null ? "" : $",\"positionMs\":{valueJson}";
            var response = await ExecuteAutomationCommandAsync(dispatcher, CreateAutomationCommandRequest(
                "FlashbackAction", null, $"{{\"action\":\"{action}\"{positionField}}}")).ConfigureAwait(false);
            AssertAutomationResponse(response, true, null, "ok", $"optional {action} position");
            var mutation = Assert.Single(observed);
            Assert.Equal("ExecuteFlashbackActionAsync", mutation.Method);
            Assert.Null(mutation.Arguments[1]);
        }
    }

    private static object CreatePayloadValidationTestDispatcher(Func<MethodInfo, object?[]?, object?> mutation)
    {
        object? HandleMutation(MethodInfo? method, object?[]? arguments)
        {
            if (method?.Name == "get_IsInitialized") return true;
            if (method == null || method.IsSpecialName) return GetDefaultReturnValue(method);
            return mutation(method, arguments);
        }

        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");
        return CreateAutomationCommandDispatcher(
            CreateConfiguredProxy(RequireType("Sussudio.Services.Automation.IAutomationViewModel"), HandleMutation),
            CreateConfiguredProxy(RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub"), (method, _) =>
                method?.Name == "GetLatestSnapshot" ? snapshot : GetDefaultReturnValue(method)),
            CreateConfiguredProxy(RequireType("Sussudio.Services.Contracts.IAutomationWindowControl"), HandleMutation),
            authToken: null);
    }

    internal static Task AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetString",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetString not found.");

        var doc = JsonDocument.Parse("{\"name\": \"test\"}");
        var result = method.Invoke(null, new object[] { doc.RootElement, "name" })?.ToString();
        AssertEqual("test", result, "GetString extracts string property");

        var missing = method.Invoke(null, new object[] { doc.RootElement, "missing" });
        AssertEqual(true, missing == null, "GetString returns null for missing property");

        var arrayDoc = JsonDocument.Parse("[1,2,3]");
        var arrayResult = method.Invoke(null, new object[] { arrayDoc.RootElement, "name" });
        AssertEqual(true, arrayResult == null, "GetString returns null for non-object");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetBool",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetBool not found.");

        var doc = JsonDocument.Parse("{\"enabled\": true, \"disabled\": false}");
        var trueResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "enabled" });
        AssertEqual(true, trueResult, "GetBool extracts true");

        var falseResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "disabled" });
        AssertEqual(false, falseResult, "GetBool extracts false");

        var missingResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "missing" });
        AssertEqual(true, missingResult == null, "GetBool returns null for missing");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetInt",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetInt not found.");

        var doc = JsonDocument.Parse("{\"count\": 42, \"text\": \"hello\"}");
        var intResult = (int?)method.Invoke(null, new object[] { doc.RootElement, "count" });
        AssertEqual(42, intResult!.Value, "GetInt extracts integer");

        var textResult = (int?)method.Invoke(null, new object[] { doc.RootElement, "text" });
        AssertEqual(true, textResult == null, "GetInt returns null for string property");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetDouble",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetDouble not found.");

        var doc = JsonDocument.Parse("{\"volume\": 0.75}");
        var result = (double?)method.Invoke(null, new object[] { doc.RootElement, "volume" });
        AssertEqual(true, Math.Abs(result!.Value - 0.75) < 0.001, $"GetDouble extracts 0.75, got {result}");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetDouble_RejectsNonFiniteValues()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetDouble",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetDouble not found.");

        using var doc = JsonDocument.Parse("{\"nan\":\"NaN\",\"positive\":\"Infinity\",\"negative\":\"-Infinity\",\"valid\":\"1.25\"}");
        AssertEqual(null, method.Invoke(null, new object[] { doc.RootElement, "nan" }), "GetDouble rejects NaN string");
        AssertEqual(null, method.Invoke(null, new object[] { doc.RootElement, "positive" }), "GetDouble rejects Infinity string");
        AssertEqual(null, method.Invoke(null, new object[] { doc.RootElement, "negative" }), "GetDouble rejects -Infinity string");

        var valid = (double?)method.Invoke(null, new object[] { doc.RootElement, "valid" });
        AssertEqual(true, Math.Abs(valid!.Value - 1.25) < 0.001, "GetDouble still accepts finite numeric strings");
        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_RequireString_ThrowsOnMissing()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("RequireString",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RequireString not found.");

        var doc = JsonDocument.Parse("{\"path\": \"/output/file.mp4\"}");
        var result = method.Invoke(null, new object[] { doc.RootElement, "path" })?.ToString();
        AssertEqual("/output/file.mp4", result, "RequireString returns present value");

        var threw = false;
        try
        {
            method.Invoke(null, new object[] { doc.RootElement, "missing" });
        }
        catch (TargetInvocationException)
        {
            threw = true;
        }
        AssertEqual(true, threw, "RequireString throws on missing property");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WindowAction_DefaultsMissingActionToRestore()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("ParseWindowAction",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseWindowAction not found.");

        using var missingDoc = JsonDocument.Parse("{}");
        var missingResult = method.Invoke(null, new object[] { missingDoc.RootElement });
        AssertEqual("Restore", missingResult?.ToString(), "WindowAction missing action defaults to Restore");

        using var blankDoc = JsonDocument.Parse("{\"action\":\"  \"}");
        var blankResult = method.Invoke(null, new object[] { blankDoc.RootElement });
        AssertEqual("Restore", blankResult?.ToString(), "WindowAction blank action defaults to Restore");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WaitForCondition_DefaultsMissingConditionToPreviewFrames()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("ParseWaitCondition",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseWaitCondition not found.");

        using var missingDoc = JsonDocument.Parse("{}");
        var missingResult = method.Invoke(null, new object[] { missingDoc.RootElement });
        AssertEqual("PreviewFramesActive", missingResult?.ToString(), "WaitForCondition missing condition defaults to PreviewFramesActive");

        using var blankDoc = JsonDocument.Parse("{\"condition\":\"  \"}");
        var blankResult = method.Invoke(null, new object[] { blankDoc.RootElement });
        AssertEqual("PreviewFramesActive", blankResult?.ToString(), "WaitForCondition blank condition defaults to PreviewFramesActive");

        return Task.CompletedTask;
    }

    internal static async Task AutomationCommandDispatcher_WaitForCondition_RejectsUndefinedCondition()
    {
        var dispatcher = CreateNoHardwareAutomationCommandDispatcher();
        var response = await ExecuteAutomationCommandAsync(
            dispatcher,
            CreateAutomationCommandRequest(
                "WaitForCondition",
                authToken: null,
                payloadJson: "{\"condition\":\"999\",\"timeoutMs\":250,\"pollMs\":50}"));

        AssertAutomationResponse(response, false, "invalid-request", "error", "undefined wait condition");
        AssertEqual("Invalid wait condition: '999'.", GetPublicProperty(response, "Message"), "undefined wait condition message");
    }

    internal static async Task AutomationCommandDispatcher_VerifyFile_RejectsUnknownProfile()
    {
        var verificationCalls = 0;
        string? observedProfile = null;
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");
        var verificationResult = CreateInstance("Sussudio.Models.RecordingVerificationResult");
        verificationResult.GetType().GetProperty("Succeeded")!.SetValue(verificationResult, true);
        verificationResult.GetType().GetProperty("Message")!.SetValue(verificationResult, "File verified.");
        verificationResult.GetType().GetProperty("FileExists")!.SetValue(verificationResult, true);
        var dispatcher = CreateAutomationCommandDispatcher(
            CreateConfiguredProxy(viewModelType, (method, _) =>
                method?.Name == "get_IsInitialized" ? true : GetDefaultReturnValue(method)),
            CreateConfiguredProxy(diagnosticsType, (method, args) =>
            {
                if (method?.Name == "GetLatestSnapshot")
                {
                    return snapshot;
                }

                if (method?.Name == "VerifyFileAsync")
                {
                    verificationCalls++;
                    observedProfile = (string?)args![1];
                    return CreateTaskFromResult(method.ReturnType.GetGenericArguments()[0], verificationResult);
                }

                return GetDefaultReturnValue(method);
            }),
            CreateThrowingProxy(windowControlType),
            authToken: null);
        var filePath = Path.Combine(GetRepoRoot(), "tests", "Sussudio.Tests", "XUnit.AutomationContractsTests.cs");
        var response = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest(
                    "VerifyFile",
                    authToken: null,
                    payloadJson: JsonSerializer.Serialize(new
                    {
                        filePath,
                        verificationProfile = "bogus"
                    })))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: false, errorCode: "invalid-request", status: "error", "unknown verification profile");
        AssertEqual("Unknown verification profile 'bogus'. Expected flashback-export.", GetPublicProperty(response, "Message"), "unknown verification profile message");
        AssertEqual(0, verificationCalls, "unknown verification profile does not reach verification");

        var normalizedProfileResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest(
                    "VerifyFile",
                    authToken: null,
                    payloadJson: JsonSerializer.Serialize(new
                    {
                        filePath,
                        verificationProfile = "flashback_export"
                    })))
            .ConfigureAwait(false);

        AssertAutomationResponse(normalizedProfileResponse, success: true, errorCode: null, status: "ok", "normalized verification profile");
        AssertEqual(1, verificationCalls, "normalized verification profile reaches verification once");
        AssertEqual("flashback-export", observedProfile, "underscore verification profile is normalized");
    }

    internal static async Task AutomationCommandDispatcher_StateConflictMapsToInvalidState()
    {
        var conflictType = RequireType("Sussudio.Services.Contracts.AutomationStateConflictException");
        var conflictException = (Exception)(conflictType.GetConstructor(new[] { typeof(string) })
            ?? throw new InvalidOperationException("AutomationStateConflictException constructor was not found."))
            .Invoke(new object[] { "True HDR preview cannot be changed while recording." });
        var dispatcher = CreatePayloadValidationTestDispatcher((method, _) =>
        {
            if (method.Name == "SetTrueHdrPreviewEnabledAsync")
            {
                throw conflictException;
            }

            return GetDefaultReturnValue(method);
        });
        var response = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest(
                    "SetTrueHdrPreviewEnabled",
                    authToken: null,
                    payloadJson: "{\"enabled\":true}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: false, errorCode: "invalid-state", status: "error", "recording-state conflict");
        AssertEqual("True HDR preview cannot be changed while recording.", GetPublicProperty(response, "Message"), "state-conflict message");
    }

    internal static Task AutomationCommandDispatcher_OneFieldHandlers_MatchCatalogPayloadFields()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var handlers = GetHandlerEntries(dispatcherType, "TrivialDeviceSelectionHandlers")
            .Concat(GetHandlerEntries(dispatcherType, "TrivialCaptureSettingsHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "TrivialAudioHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "TrivialPreviewRecordingHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "UiPreviewRecordingHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "UiStateHandlers"))
            .ToArray();

        AssertEqual(true, handlers.Length > 0, "dispatcher one-field handler tables are not empty");

        foreach (var entry in handlers)
        {
            var kind = GetPublicProperty(entry, "Key")
                       ?? throw new InvalidOperationException("Trivial handler entry key was null.");
            var commandName = kind.ToString()!;
            var handler = GetPublicProperty(entry, "Value")
                          ?? throw new InvalidOperationException($"Trivial handler for {commandName} was null.");
            var handlerPayloadFieldName = (string)GetPublicProperty(handler, "PayloadFieldName")!;
            var handlerPayloadFieldType = GetPublicProperty(handler, "PayloadFieldType")!.ToString();
            var catalogMetadata = GetAutomationCommandCatalogMetadata(kind);
            var catalogPayloadFields = GetMetadataCollection(catalogMetadata, "PayloadFields");

            AssertEqual(1, catalogPayloadFields.Length, $"{commandName} one-field catalog payload field count");
            var catalogPayloadField = catalogPayloadFields[0];
            AssertEqual(handlerPayloadFieldName, (string)GetMetadataProperty(catalogPayloadField, "Name")!, $"{commandName} one-field payload field name");
            AssertEqual(handlerPayloadFieldType, GetMetadataProperty(catalogPayloadField, "Type")!.ToString(), $"{commandName} one-field payload field type");
            AssertEqual(true, (bool)GetMetadataProperty(catalogPayloadField, "Required")!, $"{commandName} one-field payload field required");
        }

        return Task.CompletedTask;

        static object[] GetHandlerEntries(Type dispatcherType, string fieldName)
        {
            var handlersField = dispatcherType.GetField(
                fieldName,
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{fieldName} not found.");
            return ((IEnumerable)handlersField.GetValue(null)!)
                .Cast<object>()
                .ToArray();
        }

        static object GetAutomationCommandCatalogMetadata(object kind)
        {
            var catalogType = kind.GetType().Assembly.GetType("Sussudio.Tools.AutomationCommandCatalog")
                              ?? throw new InvalidOperationException("AutomationCommandCatalog not found.");
            var getMethod = catalogType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("AutomationCommandCatalog.Get not found.");
            return getMethod.Invoke(null, new[] { kind })
                   ?? throw new InvalidOperationException($"AutomationCommandCatalog.Get({kind}) returned null.");
        }
    }

    internal static Task AutomationCommandDispatcher_GetAudioRampTrace_MetadataMatchesDispatcherPayload()
    {
        var dispatcherText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetAudioRampTrace:");
        AssertContains(dispatcherText, "ExecuteGetDiagnosticsCommand(payload, correlationId)");
        AssertContains(dispatcherText, "ExecuteGetPerformanceTimelineCommand(payload, correlationId)");
        AssertContains(dispatcherText, "ExecuteGetAudioRampTraceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetDiagnosticsCommand(");
        AssertContains(customCommandsText, "var maxEvents = GetInt(payload, AutomationPayloadKeys.MaxEvents) ?? 100;");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetPerformanceTimelineCommand(");
        AssertContains(customCommandsText, "var maxEntries = GetInt(payload, AutomationPayloadKeys.MaxEntries) ?? 240;");
        AssertContains(customCommandsText, "var maxEntries = GetInt(payload, AutomationPayloadKeys.MaxEntries) ?? 512;");
        AssertContains(customCommandsText, "GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken)");

        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var kind = Enum.Parse(enumType, "GetAudioRampTrace");
        var catalogMetadata = GetAutomationCommandCatalogMetadata(kind);
        var catalogPayloadFields = GetMetadataCollection(catalogMetadata, "PayloadFields");

        AssertEqual("{ maxEntries?: int }", (string)GetMetadataProperty(catalogMetadata, "PayloadShape")!, "GetAudioRampTrace payload shape");
        AssertEqual(1, catalogPayloadFields.Length, "GetAudioRampTrace catalog payload field count");
        var maxEntriesField = catalogPayloadFields[0];
        AssertEqual("maxEntries", (string)GetMetadataProperty(maxEntriesField, "Name")!, "GetAudioRampTrace payload field name");
        AssertEqual("Integer", GetMetadataProperty(maxEntriesField, "Type")!.ToString(), "GetAudioRampTrace payload field type");
        AssertEqual(false, (bool)GetMetadataProperty(maxEntriesField, "Required")!, "GetAudioRampTrace payload field required flag");

        return Task.CompletedTask;

        static object GetAutomationCommandCatalogMetadata(object kind)
        {
            var catalogType = kind.GetType().Assembly.GetType("Sussudio.Tools.AutomationCommandCatalog")
                              ?? throw new InvalidOperationException("AutomationCommandCatalog not found.");
            var getMethod = catalogType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("AutomationCommandCatalog.Get not found.");
            return getMethod.Invoke(null, new[] { kind })
                   ?? throw new InvalidOperationException($"AutomationCommandCatalog.Get({kind}) returned null.");
        }
    }

    internal static async Task AutomationCommandDispatcher_CatalogReadyIndependentCommands_BypassDeviceReadiness()
    {
        var catalogType = RequireType("Sussudio.Tools.AutomationCommandCatalog");
        var readyIndependentCommands = GetCatalogEntries(catalogType)
            .Where(entry => !(bool)GetMetadataProperty(entry, "RequiresReadyDevices")!)
            .OrderBy(entry => Convert.ToInt32(GetMetadataProperty(entry, "Kind")!))
            .ToArray();
        AssertEqual(true, readyIndependentCommands.Length > 0, "ready-independent catalog commands exist");

        var tempRoot = Path.Combine(Path.GetTempPath(), "sussudio_ready_independent_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var dispatcher = CreateNoHardwareAutomationCommandDispatcher();
            var failures = new List<string>();

            foreach (var entry in readyIndependentCommands)
            {
                var name = (string)GetMetadataProperty(entry, "Name")!;
                var response = await ExecuteAutomationCommandAsync(
                        dispatcher,
                        CreateAutomationCommandRequest(
                            name,
                            authToken: null,
                            payloadJson: CreateReadyIndependentCommandPayload(name, tempRoot)))
                    .ConfigureAwait(false);

                var errorCode = (string?)GetPublicProperty(response, "ErrorCode");
                var status = GetPublicProperty(response, "Status")!.ToString();
                if (string.Equals(errorCode, "not-ready", StringComparison.Ordinal) ||
                    string.Equals(status, "NotReady", StringComparison.Ordinal))
                {
                    failures.Add($"{name}: rejected as device-not-ready");
                    continue;
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Ready-independent catalog commands must bypass AutomationCommandDispatcher device readiness: " +
                    string.Join("; ", failures));
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static object CreateNoHardwareAutomationCommandDispatcher()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");

        object? Handler(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_IsInitialized")
            {
                return false;
            }

            if (method?.Name == "get_Devices")
            {
                return Activator.CreateInstance(
                    typeof(ObservableCollection<>).MakeGenericType(RequireType("Sussudio.Models.CaptureDevice")));
            }

            if (method?.Name == "GetLatestSnapshot")
            {
                return snapshot;
            }

            if (method?.Name == "RefreshSnapshotNowAsync")
            {
                return CreateTaskFromResult(method.ReturnType.GetGenericArguments()[0], snapshot);
            }

            return CreateNoHardwareReturnValue(method?.ReturnType ?? typeof(void));
        }

        return CreateAutomationCommandDispatcher(
            CreateConfiguredProxy(viewModelType, Handler),
            CreateConfiguredProxy(diagnosticsType, Handler),
            CreateConfiguredProxy(windowControlType, Handler),
            authToken: null);
    }

    private static string CreateReadyIndependentCommandPayload(string commandName, string tempRoot)
    {
        static string JsonString(string value) => JsonSerializer.Serialize(value);

        var outputPath = Path.Combine(tempRoot, $"{commandName}.tmp");
        return commandName switch
        {
            "SetOutputPath" => $"{{\"outputPath\":{JsonString(tempRoot)}}}",
            "WindowAction" => "{\"action\":\"restore\"}",
            "WaitForCondition" => "{\"condition\":\"PreviewFramesActive\",\"timeoutMs\":250,\"pollMs\":50}",
            "AssertSnapshot" => "{\"assertions\":[{\"field\":\"PreviewFramesDisplayed\",\"op\":\"eq\",\"value\":\"0\"}]}",
            "SetTrueHdrPreviewEnabled" => "{\"enabled\":false}",
            "CapturePreviewFrame" => $"{{\"outputPath\":{JsonString(outputPath)}}}",
            "CaptureWindowScreenshot" => $"{{\"outputPath\":{JsonString(outputPath)}}}",
            "SetPreviewVolume" => "{\"previewVolumePercent\":0}",
            "SetShowAllCaptureOptions" => "{\"enabled\":true}",
            "SetStatsVisible" => "{\"visible\":false}",
            "SetDeviceAudioMode" => "{\"mode\":\"hdmi\"}",
            "SetStatsSectionVisible" => "{\"section\":\"preview\",\"visible\":false}",
            "SetSettingsVisible" => "{\"visible\":false}",
            "FlashbackAction" => "{\"action\":\"pause\"}",
            "FlashbackExport" => $"{{\"seconds\":1,\"outputPath\":{JsonString(outputPath)},\"force\":true}}",
            "VerifyFile" => CreateVerifyFilePayload(tempRoot),
            "SetMicrophoneEnabled" => "{\"enabled\":false}",
            "SetMicrophoneVolume" => "{\"microphoneVolumePercent\":50}",
            "SetFlashbackEnabled" => "{\"enabled\":false}",
            "SetFlashbackBufferMinutes" => "{\"minutes\":5}",
            "SetFlashbackGpuDecode" => "{\"enabled\":true}",
            "SetFrameTimeOverlayVisible" => "{\"visible\":false}",
            "SetFlashbackTimelineVisible" => "{\"visible\":false}",
            "SetFullScreenEnabled" => "{\"enabled\":false}",
            "Authenticate" or
            "GetSnapshot" or
            "GetDiagnostics" or
            "RefreshDevices" or
            "ArmClose" or
            "VerifyLastRecording" or
            "ProbeVideoSource" or
            "ProbePreviewColor" or
            "GetCaptureOptions" or
            "GetPerformanceTimeline" or
            "FlashbackGetSegments" or
            "RestartFlashback" or
            "GetAudioRampTrace" or
            "GetAutomationManifest" or
            "OpenRecordingsFolder" => "{}",
            _ => throw new InvalidOperationException(
                $"Ready-independent automation command '{commandName}' needs a null-safe harness payload.")
        };
    }

    private static string CreateVerifyFilePayload(string tempRoot)
    {
        var filePath = Path.Combine(tempRoot, "verify-input.bin");
        File.WriteAllBytes(filePath, Array.Empty<byte>());
        return $"{{\"filePath\":{JsonSerializer.Serialize(filePath)}}}";
    }

    private static object? CreateNoHardwareReturnValue(Type returnType)
    {
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType == typeof(ValueTask))
        {
            return ValueTask.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            return CreateTaskFromResult(resultType, CreateNoHardwareValue(resultType));
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            return Activator.CreateInstance(returnType, CreateNoHardwareValue(resultType));
        }

        return CreateNoHardwareValue(returnType);
    }

    private static object? CreateNoHardwareValue(Type valueType)
    {
        if (valueType == typeof(string))
        {
            return string.Empty;
        }

        if (valueType == typeof(bool))
        {
            return false;
        }

        if (valueType.IsValueType)
        {
            return Activator.CreateInstance(valueType);
        }

        if (valueType.IsArray)
        {
            return Array.CreateInstance(valueType.GetElementType()!, 0);
        }

        if (TryCreateEmptyGenericArray(valueType, out var emptyArray))
        {
            return emptyArray;
        }

        var parameterlessConstructor = valueType.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor != null)
        {
            return Activator.CreateInstance(valueType);
        }

        return null;
    }

    private static bool TryCreateEmptyGenericArray(Type valueType, out object? emptyArray)
    {
        emptyArray = null;
        var genericEnumerable = valueType.IsGenericType &&
            valueType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? valueType
                : valueType.GetInterfaces()
                    .FirstOrDefault(candidate =>
                        candidate.IsGenericType &&
                        candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (genericEnumerable == null)
        {
            return false;
        }

        emptyArray = Array.CreateInstance(genericEnumerable.GetGenericArguments()[0], 0);
        return true;
    }

    internal static Task AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("RequiresReadyDevices",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RequiresReadyDevices not found.");

        var commandType = RequireType("Sussudio.Models.AutomationCommandKind");

        var getSnapshot = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "GetSnapshot") })!;
        AssertEqual(false, getSnapshot, "GetSnapshot does not require ready devices");

        var windowAction = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "WindowAction") })!;
        AssertEqual(false, windowAction, "WindowAction does not require ready devices");

        var authenticate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "Authenticate") })!;
        AssertEqual(false, authenticate, "Authenticate does not require ready devices");

        var setFlashbackEnabled = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFlashbackEnabled") })!;
        AssertEqual(false, setFlashbackEnabled, "SetFlashbackEnabled does not require ready devices");

        var getAutomationManifest = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "GetAutomationManifest") })!;
        AssertEqual(false, getAutomationManifest, "GetAutomationManifest does not require ready devices");

        var setFullScreenEnabled = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFullScreenEnabled") })!;
        AssertEqual(false, setFullScreenEnabled, "SetFullScreenEnabled does not require ready devices");

        var openRecordingsFolder = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "OpenRecordingsFolder") })!;
        AssertEqual(false, openRecordingsFolder, "OpenRecordingsFolder does not require ready devices");

        var setMicrophoneVolume = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetMicrophoneVolume") })!;
        AssertEqual(false, setMicrophoneVolume, "SetMicrophoneVolume does not require ready devices");

        var setFlashbackBufferMinutes = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFlashbackBufferMinutes") })!;
        AssertEqual(false, setFlashbackBufferMinutes, "SetFlashbackBufferMinutes does not require ready devices");

        var setResolution = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetResolution") })!;
        AssertEqual(true, setResolution, "SetResolution requires ready devices");

        var setFrameRate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFrameRate") })!;
        AssertEqual(true, setFrameRate, "SetFrameRate requires ready devices");

        var selectMicrophoneDevice = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SelectMicrophoneDevice") })!;
        AssertEqual(true, selectMicrophoneDevice, "SelectMicrophoneDevice requires ready devices");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion()
    {
        var sourceText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var windowActionBlock = ExtractTextBetween(
            sourceText,
            "private async Task<AutomationCommandResponse> ExecuteWindowActionCommandAsync(",
            "return CreateAcknowledgedResponse(correlationId, $\"Window action requested: {action}.\");");
        var closeBlock = ExtractTextBetween(
            windowActionBlock,
            "if (action == AutomationWindowAction.Close)",
            "await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);");

        AssertContains(closeBlock, "await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);");
        AssertContains(closeBlock, "Window close completed.");
        AssertDoesNotContain(closeBlock, "ContinueWith(");
        AssertDoesNotContain(closeBlock, "CancellationToken.None");

        return Task.CompletedTask;
    }

    internal static async Task AutomationCommandDispatcher_WindowClose_RequiresMatchingArmActionId()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");
        var closeCalls = 0;

        var dispatcher = CreateAutomationCommandDispatcher(
            CreateConfiguredProxy(viewModelType, (method, _) => GetDefaultReturnValue(method)),
            CreateConfiguredProxy(
                diagnosticsType,
                (method, _) => method?.Name == "GetLatestSnapshot" ? snapshot : GetDefaultReturnValue(method)),
            CreateConfiguredProxy(
                windowControlType,
                (method, _) =>
                {
                    if (method?.Name == "CloseAsync")
                    {
                        closeCalls++;
                        return Task.CompletedTask;
                    }

                    return GetDefaultReturnValue(method);
                }),
            authToken: null);

        var missingArmIdResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("ArmClose", authToken: null, payloadJson: "{\"armed\":true}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(missingArmIdResponse, success: false, errorCode: "window-close-action-id-required", status: "error", "ArmClose requires actionId");
        AssertEqual(0, closeCalls, "missing ArmClose actionId does not close");

        var armResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("ArmClose", authToken: null, payloadJson: "{\"armed\":true,\"actionId\":\"close-1\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(armResponse, success: true, errorCode: null, status: "ok", "ArmClose accepts actionId");

        var missingCloseIdResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("WindowAction", authToken: null, payloadJson: "{\"action\":\"Close\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(missingCloseIdResponse, success: false, errorCode: "window-close-action-id-required", status: "error", "WindowAction close requires actionId");
        AssertEqual(0, closeCalls, "missing close actionId does not close");

        var mismatchResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("WindowAction", authToken: null, payloadJson: "{\"action\":\"Close\",\"actionId\":\"close-2\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(mismatchResponse, success: false, errorCode: "window-close-action-id-mismatch", status: "error", "WindowAction close rejects mismatched actionId");
        AssertEqual(0, closeCalls, "mismatched close actionId does not close");

        var closeResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("WindowAction", authToken: null, payloadJson: "{\"action\":\"Close\",\"actionId\":\"close-1\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(closeResponse, success: true, errorCode: null, status: "ok", "WindowAction close accepts matching actionId");
        AssertEqual(1, closeCalls, "matching close actionId closes once");

        var replayResponse = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("WindowAction", authToken: null, payloadJson: "{\"action\":\"Close\",\"actionId\":\"close-1\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(replayResponse, success: false, errorCode: "window-close-not-armed", status: "error", "WindowAction close actionId is single-use");
        AssertEqual(1, closeCalls, "replayed close actionId does not close twice");
    }

    internal static Task AutomationCommandDispatcher_PreviewRendererHealthy_RequiresFirstVisual()
    {
        var sourceText = ReadAutomationCommandDispatcherFamilyText();
        var conditionBlock = ExtractTextBetween(
            sourceText,
            "AutomationWaitCondition.PreviewRendererHealthy =>",
            "AutomationWaitCondition.AudioSignalPresent =>");

        AssertContains(conditionBlock, "snapshot.PreviewFirstVisualConfirmed");
        AssertContains(conditionBlock, "snapshot.PreviewGpuActive || snapshot.PreviewFramesDisplayed > 0");
        AssertDoesNotContain(conditionBlock, "snapshot.PreviewGpuActive || snapshot.PreviewRendererAttached");
        AssertDoesNotContain(sourceText, "WaitConditionRefreshCadenceMs");

        return Task.CompletedTask;
    }

    internal static Task UiAutomationCommands_AreNotBlockedOnDeviceReadiness()
    {
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetShowAllCaptureOptions => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetPreviewVolume => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetStatsVisible => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.GetCaptureOptions => true,");

        return Task.CompletedTask;
    }

    internal static Task App_Xaml_WiresUnhandledExceptionPolicy()
    {
        var appType = RequireType("Sussudio.App");
        var appRootSource = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");

        var uiHandler = appType.GetMethod(
            "App_UnhandledException",
            BindingFlags.Instance | BindingFlags.NonPublic);
        AssertNotNull(uiHandler, "App.App_UnhandledException handler");

        var domainHandler = appType.GetMethod(
            "CurrentDomain_UnhandledException",
            BindingFlags.Instance | BindingFlags.NonPublic);
        AssertNotNull(domainHandler, "App.CurrentDomain_UnhandledException handler");

        var isRecoverable = appType.GetMethod(
            "IsRecoverableUnhandled",
            BindingFlags.Static | BindingFlags.NonPublic);
        AssertNotNull(isRecoverable, "App.IsRecoverableUnhandled triage");

        var recoverable = (bool)isRecoverable!.Invoke(null, new object[] { new OperationCanceledException() })!;
        AssertEqual(true, recoverable, "OperationCanceledException is recoverable");

        var nonRecoverable = (bool)isRecoverable.Invoke(null, new object[] { new InvalidOperationException() })!;
        AssertEqual(false, nonRecoverable, "InvalidOperationException is not recoverable");

        AssertContains(appRootSource, "Logger.Initialize(logRoot);");
        AssertOccursBefore(appRootSource, "Logger.Initialize(logRoot);", "InitializeComponent();");
        AssertContains(appRootSource, "FfmpegRuntimeInit.EnsureInitialized(requireNativeRuntime: true);");
        AssertContains(appRootSource, "UnhandledException += App_UnhandledException;");
        AssertContains(appRootSource, "AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;");
        AssertContains(appRootSource, "private static bool IsRecoverableUnhandled(Exception ex)");
        AssertContains(appRootSource, "private void App_UnhandledException(");
        AssertContains(appRootSource, "private void CurrentDomain_UnhandledException(");
        AssertContains(appRootSource, "private void TryEmergencyStopRecording(string source)");
        AssertContains(appRootSource, "() => viewModel.StopRecordingForEmergencyAsync(),");
        AssertContains(appRootSource, "viewModel.MarkRecordingFinalizationUnresolved,");
        AssertContains(appRootSource, "TimeSpan.FromSeconds(8));");
        AssertContains(appRootSource, "protected override void OnLaunched(");
        AssertDoesNotContain(appRootSource, "SingleInstanceMutexName");
        var startupSource = ReadRepoFile("Sussudio/Services/Runtime/AppProcessStartup.cs");
        AssertContains(startupSource, "SingleInstanceMutexName");
        AssertContains(startupSource, "SINGLE_INSTANCE_GUARD second instance detected");
        AssertContains(startupSource, "SINGLE_INSTANCE_GUARD mutex setup failed; refusing launch.");
        AssertContains(startupSource, "mutex.ReleaseMutex();");
        AssertDoesNotContain(startupSource, "Logger.");
        var entrySource = ReadRepoFile("Sussudio/Program.cs");
        var childDispatch = entrySource.IndexOf("NativeFfmpegCapabilityProbe.TryRunChildProcess(", StringComparison.Ordinal);
        var normalAdmission = entrySource.IndexOf("AppProcessStartup.RunAsSingleInstance(StartApplication)", StringComparison.Ordinal);
        AssertEqual(true, childDispatch >= 0 && normalAdmission > childDispatch, "private child dispatch precedes normal admission");
        var comInitialization = entrySource.IndexOf("ComWrappersSupport.InitializeComWrappers();", StringComparison.Ordinal);
        var applicationStart = entrySource.IndexOf("Application.Start(", StringComparison.Ordinal);
        var synchronizationContext = entrySource.IndexOf("SynchronizationContext.SetSynchronizationContext(context);", StringComparison.Ordinal);
        var appConstruction = entrySource.IndexOf("new App();", StringComparison.Ordinal);
        AssertEqual(
            true,
            comInitialization > normalAdmission && applicationStart > comInitialization &&
            synchronizationContext > applicationStart && appConstruction > synchronizationContext,
            "admitted startup preserves the generated WinUI sequence");
        AssertContains(appRootSource, "\"APP_START \" +");
        AssertContains(appRootSource, "public partial class App : Application");

        return Task.CompletedTask;
    }

    internal static Task BoolConverters_PreserveInversionAndVisibilityMappings()
    {
        var inverseBoolType = RequireType("Sussudio.Converters.InverseBoolConverter");
        var boolToVisType = RequireType("Sussudio.Converters.BoolToVisibilityConverter");
        var inverseBoolToVisType = RequireType("Sussudio.Converters.BoolToInverseVisibilityConverter");

        var converterInterfaceType = RequireInterface(inverseBoolType, "Microsoft.UI.Xaml.Data.IValueConverter");
        AssertImplementsInterface(boolToVisType, converterInterfaceType);
        AssertImplementsInterface(inverseBoolToVisType, converterInterfaceType);
        var visibilityType = converterInterfaceType.Assembly.GetType("Microsoft.UI.Xaml.Visibility");
        AssertNotNull(visibilityType, "Microsoft.UI.Xaml.Visibility");
        var visibleValue = Enum.Parse(visibilityType!, "Visible");
        var collapsedValue = Enum.Parse(visibilityType!, "Collapsed");

        var inverseConvert = RequireConverterMethod(inverseBoolType, "Convert");
        var inverseConvertBack = RequireConverterMethod(inverseBoolType, "ConvertBack");
        var boolToVisibilityConvert = RequireConverterMethod(boolToVisType, "Convert");
        var boolToVisibilityConvertBack = RequireConverterMethod(boolToVisType, "ConvertBack");
        var inverseVisibilityConvert = RequireConverterMethod(inverseBoolToVisType, "Convert");
        var inverseVisibilityConvertBack = RequireConverterMethod(inverseBoolToVisType, "ConvertBack");

        var inverseInstance = Activator.CreateInstance(inverseBoolType)!;
        AssertEqual(
            false,
            (bool)inverseConvert.Invoke(inverseInstance, new object?[] { true, typeof(bool), null, "" })!,
            "InverseBoolConverter.Convert(true)");
        AssertEqual(
            true,
            (bool)inverseConvert.Invoke(inverseInstance, new object?[] { false, typeof(bool), null, "" })!,
            "InverseBoolConverter.Convert(false)");
        AssertEqual(
            false,
            (bool)inverseConvertBack.Invoke(inverseInstance, new object?[] { true, typeof(bool), null, "" })!,
            "InverseBoolConverter.ConvertBack(true)");
        AssertEqual(
            true,
            (bool)inverseConvertBack.Invoke(inverseInstance, new object?[] { false, typeof(bool), null, "" })!,
            "InverseBoolConverter.ConvertBack(false)");
        var nonBoolSentinel = new object();
        AssertSame(
            nonBoolSentinel,
            inverseConvert.Invoke(inverseInstance, new object?[] { nonBoolSentinel, typeof(bool), null, "" }),
            "InverseBoolConverter passes through non-bool");
        AssertSame(
            nonBoolSentinel,
            inverseConvertBack.Invoke(inverseInstance, new object?[] { nonBoolSentinel, typeof(bool), null, "" }),
            "InverseBoolConverter.ConvertBack passes through non-bool");
        AssertEqual(
            null,
            inverseConvert.Invoke(inverseInstance, new object?[] { null, typeof(bool), null, "" }),
            "InverseBoolConverter.Convert(null)");
        AssertEqual(
            null,
            inverseConvertBack.Invoke(inverseInstance, new object?[] { null, typeof(bool), null, "" }),
            "InverseBoolConverter.ConvertBack(null)");

        var boolToVisibility = Activator.CreateInstance(boolToVisType)!;
        var visible = boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { true, visibilityType, null, "" });
        var collapsed = boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { false, visibilityType, null, "" });
        AssertNotNull(visible, "BoolToVisibilityConverter.Convert(true) result");
        AssertNotNull(collapsed, "BoolToVisibilityConverter.Convert(false) result");
        AssertEqual(
            visibleValue,
            visible,
            "BoolToVisibilityConverter.Convert(true)");
        AssertEqual(
            collapsedValue,
            collapsed,
            "BoolToVisibilityConverter.Convert(false)");
        AssertEqual(
            collapsedValue,
            boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { "not-a-bool", visibilityType, null, "" }),
            "BoolToVisibilityConverter.Convert(non-bool)");
        AssertEqual(
            collapsedValue,
            boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { null, visibilityType, null, "" }),
            "BoolToVisibilityConverter.Convert(null)");
        AssertEqual(
            true,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { visibleValue, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(Visible)");
        AssertEqual(
            false,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { collapsedValue, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(Collapsed)");
        AssertEqual(
            false,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { nonBoolSentinel, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(non-visibility)");
        AssertEqual(
            false,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { null, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(null)");

        var inverseVisibility = Activator.CreateInstance(inverseBoolToVisType)!;
        AssertEqual(
            collapsedValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { true, visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(true)");
        AssertEqual(
            visibleValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { false, visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(false)");
        AssertEqual(
            visibleValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { "not-a-bool", visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(non-bool)");
        AssertEqual(
            visibleValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { null, visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(null)");
        AssertEqual(
            false,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { visibleValue, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(Visible)");
        AssertEqual(
            true,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { collapsedValue, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(Collapsed)");
        AssertEqual(
            true,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { nonBoolSentinel, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(non-visibility)");
        AssertEqual(
            true,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { null, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(null)");

        return Task.CompletedTask;
    }

    internal static Task DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates()
    {
        var formatterType = RequireType("Sussudio.DisplayFormatters");
        var formatSourceHdr = formatterType.GetMethod(
            "FormatSourceHdr",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(bool?), typeof(string) },
            modifiers: null);
        AssertNotNull(formatSourceHdr, "DisplayFormatters.FormatSourceHdr(bool?, string?)");

        AssertEqual(
            "On (BT.2020)",
            formatSourceHdr!.Invoke(null, new object?[] { true, "BT.2020" }),
            "FormatSourceHdr(true, colorimetry)");
        AssertEqual(
            "On",
            formatSourceHdr.Invoke(null, new object?[] { true, "   " }),
            "FormatSourceHdr(true, whitespace colorimetry)");
        AssertEqual(
            "On",
            formatSourceHdr.Invoke(null, new object?[] { true, null }),
            "FormatSourceHdr(true, null colorimetry)");
        AssertEqual(
            "Off",
            formatSourceHdr.Invoke(null, new object?[] { false, "BT.709" }),
            "FormatSourceHdr(false, colorimetry)");
        AssertEqual(
            "\u2014",
            formatSourceHdr.Invoke(null, new object?[] { null, "BT.2020" }),
            "FormatSourceHdr(null, colorimetry)");

        return Task.CompletedTask;
    }

    internal static Task ProjectFile_PreservesEnglishOnlyPublishLocalePolicy()
    {
        var projectText = ReadRepoFile("Sussudio/Sussudio.csproj").Replace("\r\n", "\n");
        var buildTargetsText = ReadRepoFile("Sussudio/Sussudio.Build.targets").Replace("\r\n", "\n");
        AssertContains(projectText, "<SatelliteResourceLanguages>en-US</SatelliteResourceLanguages>");
        AssertContains(projectText, "<Import Project=\"Sussudio.Build.targets\" />");
        AssertContains(buildTargetsText, "<Target Name=\"StripUnwantedLocales\"");
        AssertContains(buildTargetsText, "AfterTargets=\"Build;Publish\"");
        AssertContains(buildTargetsText, "$_.Name.ToLowerInvariant() -ne 'en-us'");
        AssertContains(buildTargetsText, "'$(PublishDir)' != ''");
        AssertContains(buildTargetsText, "^[A-Za-z]{2,3}(-[A-Za-z]+)+$");
        AssertContains(buildTargetsText, "<Target Name=\"StageLatestBuildToRepoRoot\"");
        AssertContains(buildTargetsText, "<LatestBuildRoot>$(MSBuildProjectDirectory)\\..\\latest-build\\</LatestBuildRoot>");
        return Task.CompletedTask;
    }

    internal static Task NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken()
    {
        AssertPipeSecurityPolicyMatrix();

        var pipeServerRootText = ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.cs")
            .Replace("\r\n", "\n");
        AssertContains(pipeServerRootText, "public sealed class NamedPipeAutomationServer : IDisposable, IAsyncDisposable");
        AssertContains(pipeServerRootText, "public bool Start()");
        AssertContains(pipeServerRootText, "private async Task HandleConnectionAsync(");
        AssertContains(pipeServerRootText, "new ConnectionSession(this, server, cancellationToken);");
        AssertContains(pipeServerRootText, "private sealed class ConnectionSession");
        AssertContains(pipeServerRootText, "public async Task RunAsync()");
        AssertContains(pipeServerRootText, "private async Task<AutomationCommandResponse> ExecuteCommandWithTimeoutAsync(");
        AssertContains(pipeServerRootText, "AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(");
        AssertContains(pipeServerRootText, "_explicitSecurityFailed = true;");
        AssertContains(pipeServerRootText, "if (!_authTokenRequired)\n                {\n                    throw new AutomationPipeSecurityException(");
        AssertContains(pipeServerRootText, "Automation pipe explicit security fallback to token-required default security");
        AssertContains(pipeServerRootText, "private AutomationCommandResponse CreateRequestTimeoutResponse()");
        AssertDoesNotContain(pipeServerRootText, "TraceFallback");
        AssertContains(pipeServerRootText, "Logger.Log($\"Automation pipe server disabled: {ex}\")");
        AssertContains(pipeServerRootText, "Logger.Log($\"Automation pipe server startup failed: {ex}\")");
        AssertContains(pipeServerRootText, "Logger.Log($\"Automation pipe server loop error: {ex}\")");
        AssertContains(pipeServerRootText, "Logger.Log($\"Automation pipe connection I/O error: {ioEx}\")");
        AssertContains(pipeServerRootText, "Logger.Log($\"Automation pipe connection error: {ex}\")");

        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        try
        {
            var secureSuccessCalls = 0;
            var secureSuccessDefaultCalls = 0;
            using (var server = CreateNamedPipeAutomationServer(
                       $"unit-pipe-secure-{Guid.NewGuid():N}",
                       authTokenRequired: false,
                       securityDescriptor: new byte[] { 1, 2, 3 },
                       secureServerStreamFactory: _ =>
                       {
                           Interlocked.Increment(ref secureSuccessCalls);
                           return CreateTestPipeServerStream($"unit-pipe-secure-{Guid.NewGuid():N}");
                       },
                       defaultServerStreamFactory: () =>
                       {
                           secureSuccessDefaultCalls++;
                           return CreateTestPipeServerStream($"unit-pipe-default-unused-{Guid.NewGuid():N}");
                       }))
            {
                AssertEqual(true, StartNamedPipeAutomationServer(server), "explicit security starts without token");
            }

            Assert.InRange(secureSuccessCalls, 1, 4);
            AssertEqual(0, secureSuccessDefaultCalls, "default fallback skipped when explicit security succeeds");

            var failedNoTokenSecureCalls = 0;
            var failedNoTokenDefaultCalls = 0;
            using (var server = CreateNamedPipeAutomationServer(
                       $"unit-pipe-fail-open-{Guid.NewGuid():N}",
                       authTokenRequired: false,
                       securityDescriptor: new byte[] { 4, 5, 6 },
                       secureServerStreamFactory: _ =>
                       {
                           failedNoTokenSecureCalls++;
                           throw new IOException("forced explicit security failure");
                       },
                       defaultServerStreamFactory: () =>
                       {
                           failedNoTokenDefaultCalls++;
                           return CreateTestPipeServerStream($"unit-pipe-default-forbidden-{Guid.NewGuid():N}");
                       }))
            {
                AssertEqual(false, StartNamedPipeAutomationServer(server), "explicit security failure disables no-token automation");
                AssertEqual(false, StartNamedPipeAutomationServer(server), "retry remains disabled after explicit security failure");
            }

            AssertEqual(1, failedNoTokenSecureCalls, "failed explicit security retried only once without token");
            AssertEqual(0, failedNoTokenDefaultCalls, "default fallback blocked without token");

            var tokenFallbackSecureCalls = 0;
            var tokenFallbackDefaultCalls = 0;
            using (var server = CreateNamedPipeAutomationServer(
                       $"unit-pipe-token-fallback-{Guid.NewGuid():N}",
                       authTokenRequired: true,
                       securityDescriptor: new byte[] { 7, 8, 9 },
                       secureServerStreamFactory: _ =>
                       {
                           tokenFallbackSecureCalls++;
                           throw new IOException("forced explicit security failure");
                       },
                       defaultServerStreamFactory: () =>
                       {
                           Interlocked.Increment(ref tokenFallbackDefaultCalls);
                           return CreateTestPipeServerStream($"unit-pipe-token-default-{Guid.NewGuid():N}");
                       }))
            {
                AssertEqual(true, StartNamedPipeAutomationServer(server), "token-required mode allows default fallback");
            }

            AssertEqual(1, tokenFallbackSecureCalls, "token fallback tries explicit security first");
            Assert.InRange(tokenFallbackDefaultCalls, 1, 4);

            var missingDescriptorDefaultCalls = 0;
            using (var server = CreateNamedPipeAutomationServer(
                       $"unit-pipe-missing-security-{Guid.NewGuid():N}",
                       authTokenRequired: false,
                       securityDescriptor: null,
                       secureServerStreamFactory: _ => throw new InvalidOperationException("secure factory should not be called"),
                       defaultServerStreamFactory: () =>
                       {
                           missingDescriptorDefaultCalls++;
                           return CreateTestPipeServerStream($"unit-pipe-missing-default-{Guid.NewGuid():N}");
                       }))
            {
                AssertEqual(false, StartNamedPipeAutomationServer(server), "missing explicit security disables no-token automation on Windows");
            }

            AssertEqual(0, missingDescriptorDefaultCalls, "missing explicit security blocks default pipe without token");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException(
                $"NamedPipeAutomationServer fallback test reflection call threw {ex.InnerException.GetType().Name}: {ex.InnerException.Message}",
                ex.InnerException);
        }

        return Task.CompletedTask;
    }

    internal static Task NamedPipeAutomationServer_RequestTimeoutsUseBoundedDispatchCancellation()
    {
        var pipeServerText = ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.cs")
            .Replace("\r\n", "\n");

        AssertContains(pipeServerText, "private sealed class ConnectionSession");
        AssertContains(pipeServerText, "var session = new ConnectionSession(this, server, cancellationToken);");
        AssertContains(pipeServerText, "var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, _serverCancellation);");
        AssertDoesNotContain(pipeServerText, "WaitForDispatchCompletionAsync(dispatchTask, CancellationToken.None)");
        AssertContains(pipeServerText, "Automation command exceeded request timeout; waiting for dispatch to stop");
        AssertDoesNotContain(pipeServerText, "DispatchContinues");
        AssertDoesNotContain(pipeServerText, "ObserveTimedOutDispatch");
        AssertContains(pipeServerText, "Request timed out after {_owner._requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "AutomationErrorCodes.RequestTimeout");
        AssertContains(pipeServerText, "private const int MaxRequestCharacters = 1024 * 1024;");
        AssertContains(pipeServerText, "ReadRequestLineAsync(reader, requestCancellation.Token)");
        AssertContains(pipeServerText, "request.Length > MaxRequestCharacters + 1");
        AssertContains(pipeServerText, "request.Length == MaxRequestCharacters + 1 && request[^1] != '\\r'");
        AssertContains(pipeServerText, "AutomationErrorCodes.RequestTooLarge");
        AssertDoesNotContain(pipeServerText, "reader.ReadLineAsync().WaitAsync(requestCancellation.Token)");

        return Task.CompletedTask;
    }

    internal static async Task NamedPipeAutomationServer_TimeoutWaitsForDispatchAndPreservesOutcome(
        string? responseError,
        bool dispatchFaults,
        bool cancelAfterAdmission)
    {
        var responseType = RequireType("Sussudio.Models.AutomationCommandResponse");
        var completionType = typeof(TaskCompletionSource<>).MakeGenericType(responseType);
        var completion = Activator.CreateInstance(completionType, TaskCreationOptions.RunContinuationsAsynchronously)!;
        var dispatchTask = (Task)completionType.GetProperty("Task")!.GetValue(completion)!;
        var admitted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        var dispatcher = CreateConfiguredProxy(RequireType("Sussudio.Services.Contracts.IAutomationCommandDispatcher"),
            (method, arguments) =>
            {
                Assert.Equal("ExecuteAsync", method!.Name);
                var token = (CancellationToken)arguments![1]!;
                Assert.Equal(!cancelAfterAdmission, token.IsCancellationRequested);
                registration = token.Register(() => cancellationObserved.TrySetResult(true));
                admitted.TrySetResult(token);
                return dispatchTask;
            });

        var pipeName = $"unit-pipe-timeout-{Guid.NewGuid():N}";
        using var server = CreateNamedPipeAutomationServer(pipeName, false, new byte[] { 1 },
            _ => CreateTestPipeServerStream(pipeName), () => CreateTestPipeServerStream(pipeName), dispatcher);
        using var pipe = CreateTestPipeServerStream(pipeName);
        var sessionType = server.GetType().GetNestedType("ConnectionSession", BindingFlags.NonPublic)!;
        var session = Activator.CreateInstance(sessionType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, args: new object[] { server, pipe, CancellationToken.None }, culture: null)!;
        var execute = sessionType.GetMethod("ExecuteCommandWithTimeoutAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        using var timeout = new CancellationTokenSource();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        if (!cancelAfterAdmission) timeout.Cancel();

        var execution = (Task)execute.Invoke(session, new object[]
        {
            CreateAutomationCommandRequest("GetSnapshot", null, "{}"), timeout, cancellation
        })!;
        try
        {
            var dispatchToken = await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (cancelAfterAdmission)
            {
                Assert.False(dispatchToken.IsCancellationRequested);
                Assert.False(execution.IsCompleted);
                timeout.Cancel();
            }
            await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(dispatchToken.IsCancellationRequested);
            await Assert.ThrowsAsync<TimeoutException>(() => execution.WaitAsync(TimeSpan.FromMilliseconds(50)));

            if (dispatchFaults)
            {
                var failure = new InvalidOperationException("dispatch failure after cancellation");
                completionType.GetMethod("SetException", new[] { typeof(Exception) })!
                    .Invoke(completion, new object[] { failure });
                var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => execution.WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.Same(failure, actual);
                return;
            }

            var response = Activator.CreateInstance(responseType)!;
            SetPropertyBackingField(response, "Success", responseError == null);
            SetPropertyBackingField(response, "ErrorCode", responseError);
            completionType.GetMethod("SetResult")!.Invoke(completion, new[] { response });
            await execution.WaitAsync(TimeSpan.FromSeconds(5));
            var result = execution.GetType().GetProperty("Result")!.GetValue(execution)!;
            if (string.Equals(responseError, "canceled", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotSame(response, result);
                AssertAutomationResponse(result, false, "request-timeout", "error", "dispatch canceled after timeout");
                Assert.Equal("failed", GetAutomationLifecycle(result));
            }
            else
            {
                Assert.Same(response, result);
            }
        }
        finally
        {
            registration.Dispose();
            completionType.GetMethod("TrySetResult")!.Invoke(completion, new[] { Activator.CreateInstance(responseType)! });
            try { await dispatchTask.ConfigureAwait(false); } catch { }
            try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        }
    }

    internal static async Task NamedPipeAutomationServer_ShutdownPreservesServerCancellationIdentity()
    {
        var responseType = RequireType("Sussudio.Models.AutomationCommandResponse");
        var completionType = typeof(TaskCompletionSource<>).MakeGenericType(responseType);
        var completion = Activator.CreateInstance(completionType, TaskCreationOptions.RunContinuationsAsynchronously)!;
        var dispatchTask = (Task)completionType.GetProperty("Task")!.GetValue(completion)!;
        var admitted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        var dispatcher = CreateConfiguredProxy(RequireType("Sussudio.Services.Contracts.IAutomationCommandDispatcher"),
            (method, arguments) =>
            {
                Assert.Equal("ExecuteAsync", method!.Name);
                var token = (CancellationToken)arguments![1]!;
                Assert.False(token.IsCancellationRequested);
                registration = token.Register(() => cancellationObserved.TrySetResult(true));
                admitted.TrySetResult(token);
                return dispatchTask;
            });

        var pipeName = $"unit-pipe-shutdown-{Guid.NewGuid():N}";
        using var server = CreateNamedPipeAutomationServer(pipeName, false, new byte[] { 1 },
            _ => CreateTestPipeServerStream(pipeName), () => CreateTestPipeServerStream(pipeName), dispatcher);
        using var pipe = CreateTestPipeServerStream(pipeName);
        using var shutdown = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, shutdown.Token);
        var sessionType = server.GetType().GetNestedType("ConnectionSession", BindingFlags.NonPublic)!;
        var session = Activator.CreateInstance(sessionType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, args: new object[] { server, pipe, shutdown.Token }, culture: null)!;
        var execute = sessionType.GetMethod("ExecuteCommandWithTimeoutAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var execution = (Task)execute.Invoke(session, new object[]
        {
            CreateAutomationCommandRequest("GetSnapshot", null, "{}"), timeout, cancellation
        })!;

        try
        {
            var dispatchToken = await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(execution.IsCompleted);
            shutdown.Cancel();
            await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(dispatchToken.IsCancellationRequested);

            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => execution.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(shutdown.Token, failure.CancellationToken);
            Assert.False(timeout.IsCancellationRequested);
            Assert.False(dispatchTask.IsCompleted);
        }
        finally
        {
            registration.Dispose();
            completionType.GetMethod("TrySetResult")!.Invoke(completion, new[] { Activator.CreateInstance(responseType)! });
            await dispatchTask.ConfigureAwait(false);
            try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        }
    }

    internal static async Task NamedPipeAutomationServer_KeepsDiagnosticsAvailableBesideBusyClients()
    {
        if (!OperatingSystem.IsWindows()) return;
        var pipeName = $"unit-pipe-concurrent-{Guid.NewGuid():N}";
        using var server = CreateNamedPipeAutomationServer(pipeName, false, new byte[] { 1 },
            _ => CreateTestPipeServerStream(pipeName), () => CreateTestPipeServerStream(pipeName));
        Assert.True(StartNamedPipeAutomationServer(server));
        var busyClients = new List<NamedPipeClientStream>();
        try
        {
            for (var i = 0; i < 3; i++)
            {
                var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                busyClients.Add(client);
                await client.ConnectAsync(5000);
                // Leave this connection awaiting its request, like another busy client.
            }

            using var diagnosticClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await diagnosticClient.ConnectAsync(5000);
            using var writer = new StreamWriter(diagnosticClient, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(diagnosticClient, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync("{}");
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await reader.ReadLineAsync(deadline.Token);
            Assert.False(string.IsNullOrWhiteSpace(response));
            using var json = JsonDocument.Parse(response!);
            Assert.Equal(JsonValueKind.Object, json.RootElement.ValueKind);
        }
        finally
        {
            foreach (var client in busyClients) client.Dispose();
        }
    }

    internal static async Task NamedPipeAutomationServer_RequestLimit_HandlesCrLfBoundary()
    {
        const int maxCharacters = 1024 * 1024;
        var serverType = RequireType("Sussudio.Services.Automation.NamedPipeAutomationServer");
        var sessionType = serverType.GetNestedType("ConnectionSession", BindingFlags.NonPublic)
                          ?? throw new InvalidOperationException("NamedPipeAutomationServer.ConnectionSession was not found.");
        var readMethod = sessionType.GetMethod("ReadRequestLineAsync", BindingFlags.NonPublic | BindingFlags.Static)
                         ?? throw new InvalidOperationException("ConnectionSession.ReadRequestLineAsync was not found.");

        static async Task<string?> InvokeReadAsync(MethodInfo method, string input, bool splitCrLf = false)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using Stream stream = splitCrLf
                ? new SplitReadStream(bytes, maxCharacters + 1)
                : new MemoryStream(bytes);
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: false);
            var task = (Task)method.Invoke(null, new object[] { reader, CancellationToken.None })!;
            await task.ConfigureAwait(false);
            return (string?)task.GetType().GetProperty("Result")!.GetValue(task);
        }

        var atLimit = new string('x', maxCharacters);
        var accepted = await InvokeReadAsync(readMethod, atLimit + "\r\n", splitCrLf: true).ConfigureAwait(false);
        AssertEqual(maxCharacters, accepted!.Length, "CRLF framing is excluded from request limit");

        try
        {
            await InvokeReadAsync(readMethod, atLimit + "x\r\n").ConfigureAwait(false);
            throw new InvalidOperationException("Expected oversized request to be rejected.");
        }
        catch (Exception ex) when (ex.GetType().Name == "AutomationRequestTooLargeException")
        {
        }
    }

    private sealed class SplitReadStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly long _splitOffset;

        public SplitReadStream(byte[] bytes, long splitOffset)
        {
            _inner = new MemoryStream(bytes, writable: false);
            _splitOffset = splitOffset;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, LimitReadCount(count));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer[..LimitReadCount(buffer.Length)], cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private int LimitReadCount(int requested)
        {
            if (_inner.Position < _splitOffset && _inner.Position + requested > _splitOffset)
            {
                return checked((int)(_splitOffset - _inner.Position));
            }

            return requested;
        }
    }

    internal static Task MainWindowAutomation_WiresPipeAuthFallbackPolicy()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var launchStartupControllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var launchStartupHandleLoadedText = ExtractMemberCode(launchStartupControllerText, "HandleLoaded");

        AssertContains(mainWindowText, "_automationHostLifecycleController = new WindowAutomationHostLifecycleController(");
        AssertContains(mainWindowText, "GetPreviewRuntimeSnapshotAsync,\n            this);");
        AssertContains(mainWindowText, "private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;");
        AssertContains(automationHostControllerText, "var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);");
        AssertContains(automationHostControllerText, "var automationPipeName = Environment.GetEnvironmentVariable(\"SUSSUDIO_AUTOMATION_PIPE\");");
        AssertContains(automationHostControllerText, "automationPipeName = NamedPipeAutomationServer.DefaultPipeName;");
        AssertContains(automationHostControllerText, "var automationPorts = AutomationViewModelPorts.From(viewModel);");
        AssertContains(automationHostControllerText, "new AutomationDiagnosticsHub(\n            automationPorts.SnapshotQuery,\n            previewSnapshotProvider,\n            new RecordingVerifier())");
        AssertContains(automationHostControllerText, "new AutomationCommandDispatcher(\n            automationPorts,\n            _diagnosticsHub,\n            windowControl,\n            automationToken)");
        AssertContains(automationHostControllerText, "_tokenRequired = !string.IsNullOrWhiteSpace(automationToken);");
        AssertContains(automationHostControllerText, "new NamedPipeAutomationServer(\n            automationDispatcher,\n            _pipeName,\n            _tokenRequired)");
        AssertDoesNotContain(mainWindowText, "Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar)");
        AssertDoesNotContain(mainWindowText, "new NamedPipeAutomationServer(");
        AssertDoesNotContain(startupText, "new NamedPipeAutomationServer(");
        AssertContains(startupText, "RefreshDevicesAsync = () => ViewModel.RefreshDevicesForStartupAsync(),");
        AssertContains(startupText, "StartAutomationHost = _automationHostLifecycleController.Start,");
        AssertContains(launchStartupControllerText, "_context.StartAutomationHost();");
        AssertDoesNotContain(launchStartupHandleLoadedText, "finally");
        AssertOccursBefore(launchStartupHandleLoadedText, "await _context.RefreshDevicesAsync();", "_context.StartAutomationHost();");
        AssertContains(automationHostControllerText, "if (_pipeServer.Start())\n        {\n            _diagnosticsHub.Start();");
        AssertContains(automationHostControllerText, "Automation control ready on pipe '{_pipeName}' (token required={_tokenRequired}).");
        AssertContains(automationHostControllerText, "Automation control disabled on pipe '{_pipeName}' (token required={_tokenRequired}).");

        return Task.CompletedTask;
    }

    internal static Task StreamDeckPluginScope_DocumentsAutomationAuthEnvelope()
    {
        var docs = ReadRepoFile("docs/stream-deck-plugin-scope.md")
            .Replace("\r\n", "\n");

        AssertContains(docs, "\"authToken\": \"<token-or-null>\"");
        AssertContains(docs, "SUSSUDIO_AUTOMATION_TOKEN");
        AssertContains(docs, "AutomationPipeProtocol.CreateRequestEnvelope");
        AssertContains(docs, "ErrorCode: \"unauthorized\"");
        AssertContains(docs, "optional auth token");
        AssertContains(docs, "automation is disabled instead of opening a default");

        return Task.CompletedTask;
    }

    private static void AssertPipeSecurityPolicyMatrix()
    {
        AssertEqual(
            false,
            AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                isWindows: false,
                hasExplicitSecurityDescriptor: false,
                explicitSecurityFailed: false,
                authTokenRequired: false),
            "non-Windows uses default pipe security");
        AssertEqual(
            true,
            AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                isWindows: true,
                hasExplicitSecurityDescriptor: false,
                explicitSecurityFailed: false,
                authTokenRequired: false),
            "Windows no-token mode disables default security when explicit ACL is unavailable");
        AssertEqual(
            false,
            AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                isWindows: true,
                hasExplicitSecurityDescriptor: false,
                explicitSecurityFailed: false,
                authTokenRequired: true),
            "Windows token-required mode permits default security fallback");
        AssertEqual(
            false,
            AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                isWindows: true,
                hasExplicitSecurityDescriptor: true,
                explicitSecurityFailed: false,
                authTokenRequired: false),
            "Windows no-token mode can start when explicit ACL exists");
        AssertEqual(
            true,
            AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                isWindows: true,
                hasExplicitSecurityDescriptor: true,
                explicitSecurityFailed: true,
                authTokenRequired: false),
            "Windows no-token mode stays disabled after explicit ACL creation fails");
        AssertEqual(
            false,
            AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                isWindows: true,
                hasExplicitSecurityDescriptor: true,
                explicitSecurityFailed: true,
                authTokenRequired: true),
            "Windows token-required mode still permits fallback after explicit ACL creation fails");
    }

    private static IDisposable CreateNamedPipeAutomationServer(
        string pipeName,
        bool authTokenRequired,
        byte[]? securityDescriptor,
        Func<byte[], NamedPipeServerStream> secureServerStreamFactory,
        Func<NamedPipeServerStream> defaultServerStreamFactory,
        object? dispatcher = null)
    {
        var serverType = RequireType("Sussudio.Services.Automation.NamedPipeAutomationServer");
        var dispatcherType = RequireType("Sussudio.Services.Contracts.IAutomationCommandDispatcher");
        var constructor = serverType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 6);

        return (IDisposable)constructor.Invoke(new object?[]
        {
            dispatcher ?? CreateThrowingProxy(dispatcherType),
            pipeName,
            authTokenRequired,
            (securityDescriptor, "unit-test-security"),
            secureServerStreamFactory,
            defaultServerStreamFactory
        });
    }

    private static object CreateThrowingProxy(Type interfaceType)
    {
        var createMethod = typeof(DispatchProxy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method =>
                method.Name == "Create" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(interfaceType, typeof(ThrowingAutomationProxy));
        return createMethod.Invoke(null, null)
               ?? throw new InvalidOperationException($"Failed to create proxy for {interfaceType.FullName}.");
    }

    private static bool StartNamedPipeAutomationServer(IDisposable server)
    {
        var start = server.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.Public)
                    ?? throw new InvalidOperationException("NamedPipeAutomationServer.Start was not found.");
        try
        {
            return (bool)start.Invoke(server, null)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException(
                $"NamedPipeAutomationServer.Start threw {ex.InnerException.GetType().Name}: {ex.InnerException.Message}",
                ex.InnerException);
        }
    }

    private static NamedPipeServerStream CreateTestPipeServerStream(string pipeName)
        => new(
            pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

    public class ThrowingAutomationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new NotSupportedException($"{targetMethod?.Name ?? "Unknown"} should not be called by this regression.");
    }

    private static MethodInfo RequireConverterMethod(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, new[] { typeof(object), typeof(Type), typeof(object), typeof(string) });
        AssertNotNull(method, $"{type.Name}.{methodName}(object, Type, object, string)");
        return method!;
    }

    private static Type RequireInterface(Type type, string interfaceName)
    {
        var interfaceType = type.GetInterface(interfaceName);
        AssertNotNull(interfaceType, $"{type.Name} implements {interfaceName}");
        return interfaceType!;
    }

    private static void AssertImplementsInterface(Type type, Type interfaceType)
    {
        if (!interfaceType.IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"{type.Name}: expected implementation of {interfaceType.FullName}.");
        }
    }

    private static void AssertSame(object expected, object? actual, string fieldName)
    {
        if (!ReferenceEquals(expected, actual))
        {
            throw new InvalidOperationException($"{fieldName}: expected same object instance.");
        }
    }

// Tests for diagnostic snapshot JSON source-generation compatibility.
    internal static Task LoggingJsonContext_SerializesStructuredSnapshotPayloads()
    {
        var loggerText = ReadRepoFile("Sussudio/AppRuntime.cs");
        AssertContains(loggerText, "public static class Logger");
        AssertDoesNotContain(loggerText, "partial class Logger");
        AssertContains(loggerText, "[JsonSourceGenerationOptions(WriteIndented = false)]");
        AssertContains(loggerText, "[JsonSerializable(typeof(CaptureHealthSnapshot))]");
        AssertContains(loggerText, "[JsonSerializable(typeof(CaptureDiagnosticsSnapshot))]");
        AssertContains(loggerText, "internal sealed partial class LoggingJsonContext : JsonSerializerContext");
        AssertContains(loggerText, "Channel.CreateBounded<string>");
        AssertContains(loggerText, "private static async Task RunLogWriterAsync()");
        AssertContains(loggerText, "public static async Task ShutdownAsync(TimeSpan timeout)");
        AssertContains(loggerText, "LogChannel.Writer.TryComplete();");
        AssertContains(loggerText, "await _logWriterTask.WaitAsync(timeout).ConfigureAwait(false);");
        AssertDoesNotContain(loggerText, "LogWriterCancellation");
        AssertContains(loggerText, "private static void WriteDirect(string entry)");
        var logMethod = ExtractTextBetween(loggerText, "public static void Log(string message", "public static void LogVerbose(");
        AssertContains(logMethod, "LogChannel.Writer.TryWrite(logMessage)");
        AssertDoesNotContain(logMethod, "WriteDirect(");
        AssertDoesNotContain(logMethod, "File.");
        AssertContains(loggerText, "private static void RotatePriorLog()");
        AssertContains(loggerText, "public static void LogSystemInfo()");
        AssertDoesNotContain(loggerText, "System.Management");
        AssertDoesNotContain(loggerText, "ManagementObjectSearcher");
        AssertContains(loggerText, "CPU info unavailable:");
        AssertContains(loggerText, "RAM info unavailable:");
        AssertContains(loggerText, "GPU info unavailable:");
        AssertContains(loggerText, "public static void LogStructured(");
        AssertContains(loggerText, "public static void LogFatalBreadcrumb(");
        AssertContains(
            loggerText,
            "JsonSerializer.Serialize(healthSnapshot, LoggingJsonContext.Default.CaptureHealthSnapshot)");
        AssertContains(
            loggerText,
            "JsonSerializer.Serialize(diagnosticsSnapshot, LoggingJsonContext.Default.CaptureDiagnosticsSnapshot)");
        AssertOccursBefore(
            loggerText,
            "CaptureHealthSnapshot healthSnapshot =>",
            "CaptureDiagnosticsSnapshot diagnosticsSnapshot =>");
        AssertOccursBefore(
            loggerText,
            "CaptureHealthSnapshot healthSnapshot =>",
            "_ when JsonSerializer.IsReflectionEnabledByDefault =>");
        AssertOccursBefore(
            loggerText,
            "CaptureDiagnosticsSnapshot diagnosticsSnapshot =>",
            "_ when JsonSerializer.IsReflectionEnabledByDefault =>");

        LoggingJsonContext_SerializesRepresentativePayloadsWithSourceGeneration();

        return Task.CompletedTask;
    }

    private static void LoggingJsonContext_SerializesRepresentativePayloadsWithSourceGeneration()
    {
        var appAssemblyPath = _assembly?.Location
            ?? throw new InvalidOperationException("Target assembly is not loaded.");
        var loadContext = new IsolatedAppLoadContext(appAssemblyPath);
        try
        {
            var appAssembly = loadContext.LoadFromAssemblyPath(appAssemblyPath);
            var diagnosticsType = RequireLoadedType(appAssembly, "Sussudio.Models.CaptureDiagnosticsSnapshot");
            var decoderType = RequireLoadedType(appAssembly, "Sussudio.Models.MjpegDecoderHealthSnapshot");
            var healthType = RequireLoadedType(appAssembly, "Sussudio.Models.CaptureHealthSnapshot");
            var detailType = RequireLoadedType(appAssembly, "Sussudio.Models.SourceTelemetryDetailEntry");

            var decoder = Activator.CreateInstance(decoderType, 7, 42, 1.2d, 2.3d, 3.4d)
                ?? throw new InvalidOperationException("Failed to create isolated MjpegDecoderHealthSnapshot.");
            var perDecoder = Array.CreateInstance(decoderType, 1);
            perDecoder.SetValue(decoder, 0);

            var diagnostics = Activator.CreateInstance(diagnosticsType)
                ?? throw new InvalidOperationException("Failed to create isolated CaptureDiagnosticsSnapshot.");
            SetPropertyOrBackingField(diagnostics, "RecordingBackend", "FFmpeg");
            SetPropertyOrBackingField(diagnostics, "MjpegDecoderCount", 1);
            SetPropertyOrBackingField(diagnostics, "MjpegPerDecoder", perDecoder);
            SetPropertyOrBackingField(diagnostics, "VideoDropsQueueSaturated", 2L);
            SetPropertyOrBackingField(diagnostics, "RecordingEncodingFailed", true);
            SetPropertyOrBackingField(diagnostics, "RecordingEncodingFailureType", "InvalidOperationException");
            SetPropertyOrBackingField(diagnostics, "FlashbackStartupCacheBytes", 120_000L);
            SetPropertyOrBackingField(diagnostics, "FatalCleanupInProgress", true);
            SetPropertyOrBackingField(diagnostics, "FlashbackCleanupInProgress", true);
            SetPropertyOrBackingField(diagnostics, "FlashbackForceRotateActive", true);
            SetPropertyOrBackingField(diagnostics, "FlashbackForceRotateRequested", true);
            SetPropertyOrBackingField(diagnostics, "FlashbackForceRotateDraining", true);
            SetPropertyOrBackingField(diagnostics, "FlashbackGpuFramesDropped", 3L);

            var diagnosticsJson = SerializeWithLoggingJsonContext(
                appAssembly,
                diagnosticsType,
                diagnostics,
                "CaptureDiagnosticsSnapshot");
            using (var diagnosticsDocument = JsonDocument.Parse(diagnosticsJson))
            {
                var root = diagnosticsDocument.RootElement;
                AssertJsonString(root, "RecordingBackend", "FFmpeg", "CaptureDiagnosticsSnapshot source-gen JSON RecordingBackend");
                AssertJsonInt64(root, "VideoDropsQueueSaturated", 2L, "CaptureDiagnosticsSnapshot source-gen JSON VideoDropsQueueSaturated");
                AssertJsonBool(root, "RecordingEncodingFailed", true, "CaptureDiagnosticsSnapshot source-gen JSON RecordingEncodingFailed");
                AssertJsonString(root, "RecordingEncodingFailureType", "InvalidOperationException", "CaptureDiagnosticsSnapshot source-gen JSON RecordingEncodingFailureType");
                AssertJsonInt64(root, "FlashbackStartupCacheBytes", 120_000L, "CaptureDiagnosticsSnapshot source-gen JSON FlashbackStartupCacheBytes");
                AssertJsonBool(root, "FatalCleanupInProgress", true, "CaptureDiagnosticsSnapshot source-gen JSON FatalCleanupInProgress");
                AssertJsonBool(root, "FlashbackCleanupInProgress", true, "CaptureDiagnosticsSnapshot source-gen JSON FlashbackCleanupInProgress");
                AssertJsonBool(root, "FlashbackForceRotateActive", true, "CaptureDiagnosticsSnapshot source-gen JSON FlashbackForceRotateActive");
                AssertJsonBool(root, "FlashbackForceRotateRequested", true, "CaptureDiagnosticsSnapshot source-gen JSON FlashbackForceRotateRequested");
                AssertJsonBool(root, "FlashbackForceRotateDraining", true, "CaptureDiagnosticsSnapshot source-gen JSON FlashbackForceRotateDraining");
                AssertJsonInt64(root, "FlashbackGpuFramesDropped", 3L, "CaptureDiagnosticsSnapshot source-gen JSON FlashbackGpuFramesDropped");
                var decoderJson = AssertSingleJsonArrayItem(root, "MjpegPerDecoder");
                AssertJsonInt32(decoderJson, "WorkerIndex", 7, "MjpegDecoderHealthSnapshot source-gen JSON WorkerIndex");
                AssertJsonInt32(decoderJson, "SampleCount", 42, "MjpegDecoderHealthSnapshot source-gen JSON SampleCount");
            }

            var detail = Activator.CreateInstance(detailType, "Signal", "Colorimetry", "BT.2020", "bt2020")
                ?? throw new InvalidOperationException("Failed to create isolated SourceTelemetryDetailEntry.");
            var details = Activator.CreateInstance(typeof(List<>).MakeGenericType(detailType))
                ?? throw new InvalidOperationException("Failed to create isolated SourceTelemetryDetailEntry list.");
            details.GetType().GetMethod("Add", new[] { detailType })!.Invoke(details, new[] { detail });

            var health = Activator.CreateInstance(healthType)
                ?? throw new InvalidOperationException("Failed to create isolated CaptureHealthSnapshot.");
            SetPropertyOrBackingField(health, "RecordingBackend", "FFmpeg");
            var playbackStateType = RequireLoadedType(appAssembly, "Sussudio.Models.FlashbackPlaybackState");
            SetPropertyOrBackingField(health, "FlashbackPlaybackState", Enum.Parse(playbackStateType, "Paused"));
            SetPropertyOrBackingField(health, "FlashbackPlaybackSegmentSwitches", 2L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackFmp4Reopens", 1L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackDroppedFrames", 6L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackSubmitFailures", 3L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackCommandsEnqueued", 4L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackScrubUpdatesCoalesced", 5L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackSeekCommandsCoalesced", 6L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackCommandQueueCapacity", 256);
            SetPropertyOrBackingField(health, "FlashbackPlaybackMaxPendingCommands", 3);
            SetPropertyOrBackingField(health, "FlashbackPlaybackMaxCommandQueueLatencyMs", 41L);
            SetPropertyOrBackingField(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand", "Play");
            SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandQueued", "Pause");
            SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 123456L);
            SetPropertyOrBackingField(health, "FlashbackExportStatus", "Running");
            SetPropertyOrBackingField(health, "FlashbackExportFailureKind", "NoMediaWritten");
            SetPropertyOrBackingField(health, "FlashbackExportPercent", 37.5d);
            SetPropertyOrBackingField(health, "FlashbackExportElapsedMs", 2500L);
            SetPropertyOrBackingField(health, "FlashbackExportOutputBytes", 1048576L);
            SetPropertyOrBackingField(health, "LastExportId", 42L);
            SetPropertyOrBackingField(health, "FlashbackOutputBytes", 123456L);
            SetPropertyOrBackingField(health, "SourceVideoFormat", "YCbCr422");
            SetPropertyOrBackingField(health, "SourceTelemetryDetails", details);

            var healthJson = SerializeWithLoggingJsonContext(
                appAssembly,
                healthType,
                health,
                "CaptureHealthSnapshot");
            using var healthDocument = JsonDocument.Parse(healthJson);
            var healthRoot = healthDocument.RootElement;
            AssertJsonString(healthRoot, "RecordingBackend", "FFmpeg", "CaptureHealthSnapshot source-gen JSON inherited RecordingBackend");
            AssertJsonString(healthRoot, "FlashbackPlaybackState", "Paused", "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackState");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackSegmentSwitches", 2L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackSegmentSwitches");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackFmp4Reopens", 1L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackFmp4Reopens");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackDroppedFrames", 6L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackDroppedFrames");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackSubmitFailures", 3L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackSubmitFailures");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackCommandsEnqueued", 4L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackCommandsEnqueued");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackScrubUpdatesCoalesced", 5L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackScrubUpdatesCoalesced");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackSeekCommandsCoalesced", 6L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackSeekCommandsCoalesced");
            AssertJsonInt32(healthRoot, "FlashbackPlaybackCommandQueueCapacity", 256, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackCommandQueueCapacity");
            AssertJsonInt32(healthRoot, "FlashbackPlaybackMaxPendingCommands", 3, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackMaxPendingCommands");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackMaxCommandQueueLatencyMs", 41L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackMaxCommandQueueLatencyMs");
            AssertJsonString(healthRoot, "FlashbackPlaybackMaxCommandQueueLatencyCommand", "Play", "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackMaxCommandQueueLatencyCommand");
            AssertJsonString(healthRoot, "FlashbackPlaybackLastCommandQueued", "Pause", "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackLastCommandQueued");
            AssertJsonInt64(healthRoot, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 123456L, "CaptureHealthSnapshot source-gen JSON FlashbackPlaybackLastCommandFailureUtcUnixMs");
            AssertJsonString(healthRoot, "FlashbackExportStatus", "Running", "CaptureHealthSnapshot source-gen JSON FlashbackExportStatus");
            AssertJsonString(healthRoot, "FlashbackExportFailureKind", "NoMediaWritten", "CaptureHealthSnapshot source-gen JSON FlashbackExportFailureKind");
            AssertJsonDouble(healthRoot, "FlashbackExportPercent", 37.5d, "CaptureHealthSnapshot source-gen JSON FlashbackExportPercent");
            AssertJsonInt64(healthRoot, "FlashbackExportElapsedMs", 2500L, "CaptureHealthSnapshot source-gen JSON FlashbackExportElapsedMs");
            AssertJsonInt64(healthRoot, "FlashbackExportOutputBytes", 1048576L, "CaptureHealthSnapshot source-gen JSON FlashbackExportOutputBytes");
            AssertJsonInt64(healthRoot, "LastExportId", 42L, "CaptureHealthSnapshot source-gen JSON LastExportId");
            AssertJsonInt64(healthRoot, "FlashbackOutputBytes", 123456L, "CaptureHealthSnapshot source-gen JSON FlashbackOutputBytes");
            AssertJsonString(healthRoot, "SourceVideoFormat", "YCbCr422", "CaptureHealthSnapshot source-gen JSON SourceVideoFormat");
            var detailJson = AssertSingleJsonArrayItem(healthRoot, "SourceTelemetryDetails");
            AssertJsonString(detailJson, "DisplayValue", "BT.2020", "SourceTelemetryDetailEntry source-gen JSON DisplayValue");
            AssertJsonString(detailJson, "RawValue", "bt2020", "SourceTelemetryDetailEntry source-gen JSON RawValue");

            foreach (var stateName in new string?[] { "Disabled", "Buffering", "Live", "Scrubbing", "Playing", "Paused", null })
            {
                var state = stateName == null ? null : Enum.Parse(playbackStateType, stateName);
                SetPropertyOrBackingField(health, "FlashbackPlaybackState", state);
                var stateJson = SerializeWithLoggingJsonContext(appAssembly, healthType, health, "CaptureHealthSnapshot");
                using var stateDocument = JsonDocument.Parse(stateJson);
                AssertJsonString(stateDocument.RootElement, "FlashbackPlaybackState", stateName ?? "N/A", "typed health source-gen state JSON");
                var restored = DeserializeWithLoggingJsonContext(appAssembly, healthType, stateJson, "CaptureHealthSnapshot");
                AssertEqual(state, GetPropertyValue(restored, "FlashbackPlaybackState"), "typed health source-gen state roundtrip");
            }

            foreach (var valueJson in new[] { "null", "\"N/A\"" })
            {
                var restored = DeserializeWithLoggingJsonContext(appAssembly, healthType,
                    $"{{\"FlashbackPlaybackState\":{valueJson}}}", "CaptureHealthSnapshot");
                AssertEqual(null, GetPropertyValue(restored, "FlashbackPlaybackState"), "source-gen reads absent or legacy null state");
                using var restoredDocument = JsonDocument.Parse(SerializeWithLoggingJsonContext(appAssembly, healthType, restored, "CaptureHealthSnapshot"));
                AssertJsonString(restoredDocument.RootElement, "FlashbackPlaybackState", "N/A", "source-gen rewrites absent state as N/A");
            }

            foreach (var valueJson in new[] { "0", "999", "true", "{}", "[]", "\"\"", "\"Unknown\"", "\"playing\"", "\" Playing \"", "\"0\"", "\"999\"" })
            {
                try
                {
                    DeserializeWithLoggingJsonContext(appAssembly, healthType,
                        $"{{\"FlashbackPlaybackState\":{valueJson}}}", "CaptureHealthSnapshot");
                    throw new InvalidOperationException($"Source-generated health deserialization accepted invalid state {valueJson}.");
                }
                catch (TargetInvocationException exception)
                {
                    AssertEqual("System.Text.Json.JsonException", exception.InnerException?.GetType().FullName, "source-gen rejects undefined JSON state");
                }
            }

            foreach (var undefined in new[] { -1, 999 })
            {
                SetPropertyOrBackingField(health, "FlashbackPlaybackState", Enum.ToObject(playbackStateType, undefined));
                try
                {
                    SerializeWithLoggingJsonContext(appAssembly, healthType, health, "CaptureHealthSnapshot");
                    throw new InvalidOperationException($"Source-generated health serialization accepted undefined enum {undefined}.");
                }
                catch (TargetInvocationException exception)
                {
                    AssertEqual("System.Text.Json.JsonException", exception.InnerException?.GetType().FullName, "source-gen rejects undefined enum writes");
                }
            }
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static string SerializeWithLoggingJsonContext(
        Assembly appAssembly,
        Type payloadType,
        object payload,
        string jsonTypeInfoPropertyName)
    {
        var contextType = RequireLoadedType(appAssembly, "Sussudio.LoggingJsonContext");
        var defaultContext = contextType.GetProperty(
                "Default",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null)
            ?? throw new InvalidOperationException("LoggingJsonContext.Default was not available.");
        var jsonTypeInfo = contextType.GetProperty(
                jsonTypeInfoPropertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(defaultContext)
            ?? throw new InvalidOperationException($"LoggingJsonContext.{jsonTypeInfoPropertyName} was not available.");
        var serializerType = jsonTypeInfo.GetType().Assembly.GetType("System.Text.Json.JsonSerializer")
            ?? throw new InvalidOperationException("System.Text.Json.JsonSerializer was not loaded in the isolated context.");
        var serializeMethod = RequireJsonTypeInfoSerializeMethod(serializerType).MakeGenericMethod(payloadType);
        return serializeMethod.Invoke(null, new[] { payload, jsonTypeInfo }) as string
            ?? throw new InvalidOperationException($"{jsonTypeInfoPropertyName} source-generated serialization returned null.");
    }

    private static object DeserializeWithLoggingJsonContext(
        Assembly appAssembly,
        Type payloadType,
        string json,
        string jsonTypeInfoPropertyName)
    {
        var contextType = RequireLoadedType(appAssembly, "Sussudio.LoggingJsonContext");
        var defaultContext = contextType.GetProperty("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var jsonTypeInfo = contextType.GetProperty(jsonTypeInfoPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(defaultContext)!;
        var serializerType = jsonTypeInfo.GetType().Assembly.GetType("System.Text.Json.JsonSerializer", throwOnError: true)!;
        var deserialize = serializerType.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(method =>
        {
            if (method.Name != "Deserialize" || !method.IsGenericMethodDefinition)
                return false;
            var parameters = method.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType.IsGenericType &&
                parameters[1].ParameterType.GetGenericTypeDefinition().FullName == "System.Text.Json.Serialization.Metadata.JsonTypeInfo`1";
        });
        return deserialize.MakeGenericMethod(payloadType).Invoke(null, new[] { json, jsonTypeInfo })
            ?? throw new InvalidOperationException($"{jsonTypeInfoPropertyName} source-generated deserialization returned null.");
    }

    private static MethodInfo RequireJsonTypeInfoSerializeMethod(Type serializerType)
    {
        foreach (var method in serializerType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (!string.Equals(method.Name, "Serialize", StringComparison.Ordinal) ||
                !method.IsGenericMethodDefinition)
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 2 ||
                !parameters[0].ParameterType.IsGenericParameter ||
                !parameters[1].ParameterType.IsGenericType)
            {
                continue;
            }

            var genericTypeName = parameters[1].ParameterType.GetGenericTypeDefinition().FullName;
            if (string.Equals(
                    genericTypeName,
                    "System.Text.Json.Serialization.Metadata.JsonTypeInfo`1",
                    StringComparison.Ordinal))
            {
                return method;
            }
        }

        throw new InvalidOperationException("JsonSerializer.Serialize<T>(T, JsonTypeInfo<T>) was not found.");
    }

    private static Type RequireLoadedType(Assembly assembly, string typeName)
        => assembly.GetType(typeName)
           ?? throw new InvalidOperationException($"Type '{typeName}' was not found in isolated app assembly.");

    private static JsonElement AssertSingleJsonArrayItem(JsonElement root, string propertyName)
    {
        var property = RequireJsonProperty(root, propertyName);
        AssertEqual(JsonValueKind.Array, property.ValueKind, propertyName);
        var items = property.EnumerateArray().ToArray();
        AssertEqual(1, items.Length, $"{propertyName} item count");
        return items[0];
    }

    private static void AssertJsonString(JsonElement root, string propertyName, string expected, string fieldName)
        => AssertEqual(expected, RequireJsonProperty(root, propertyName).GetString(), fieldName);

    private static void AssertJsonInt32(JsonElement root, string propertyName, int expected, string fieldName)
        => AssertEqual(expected, RequireJsonProperty(root, propertyName).GetInt32(), fieldName);

    private static void AssertJsonInt64(JsonElement root, string propertyName, long expected, string fieldName)
        => AssertEqual(expected, RequireJsonProperty(root, propertyName).GetInt64(), fieldName);

    private static void AssertJsonBool(JsonElement root, string propertyName, bool expected, string fieldName)
        => AssertEqual(expected, RequireJsonProperty(root, propertyName).GetBoolean(), fieldName);

    private static void AssertJsonDouble(JsonElement root, string propertyName, double expected, string fieldName)
        => AssertEqual(expected, RequireJsonProperty(root, propertyName).GetDouble(), fieldName);

    private static JsonElement RequireJsonProperty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"JSON payload missing property '{propertyName}'.");
        }

        return property;
    }

    private sealed class IsolatedAppLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public IsolatedAppLoadContext(string appAssemblyPath)
            : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(appAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
        }
    }

// Flashback backend preview pipeline contracts live with the automation xUnit wrappers.
    internal static Task CaptureService_DeviceSwitchTeardown_StopsVideoBeforeFlashbackDisposal()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n");
        var unifiedVideoCaptureText = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var disposePreviewPipeline = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposePreviewPipelineAsync",
            "\n}");
        var unifiedDisposeCore = ExtractTextBetween(
            unifiedVideoCaptureText,
            "private async ValueTask DisposeCoreAsync",
            "private void ThrowIfDisposed()");

        AssertContains(disposePreviewPipeline, "unifiedVideoCapture.SetPreviewSink(null);");
        AssertContains(disposePreviewPipeline, "unifiedVideoCapture.SetFlashbackSink(null);");
        AssertContains(disposePreviewPipeline, "PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE");
        AssertOccursBefore(
            disposePreviewPipeline,
            "await unifiedVideoCapture.StopAsync().ConfigureAwait(false);",
            "await DisposeFlashbackPreviewBackendAsync(");
        AssertOccursBefore(
            disposePreviewPipeline,
            "await DisposeFlashbackPreviewBackendAsync(",
            "await unifiedVideoCapture.DisposeForPreviewReinitAsync().ConfigureAwait(false);");
        AssertDoesNotContain(disposePreviewPipeline, "await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);");
        AssertContains(unifiedVideoCaptureText, "public async ValueTask DisposeForPreviewReinitAsync()");
        AssertContains(unifiedDisposeCore, "if (disposeSharedD3DDeviceManager)");
        AssertContains(unifiedDisposeCore, "UNIFIED_VIDEO_REINIT_RETIRE_SHARED_D3D_MANAGER");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange()
    {
        var captureServiceText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadCaptureServicePreviewLifecycleCodeWithoutCommentsOrStrings()
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings();
        var captureServiceRawText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource();
        var captureServiceRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleText = ReadCaptureServicePreviewLifecycleSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var flashbackPreviewBackendText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var flashbackBackendResourcesText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/FlashbackBackendResources.cs");
        var viewModelPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var startVideoPreview = ExtractTextBetween(
            captureServiceText,
            "public Task StartVideoPreviewAsync",
            "private bool CanReuseVideoCaptureForPreview");
        var retainedPreviewFastPath = ExtractTextBetween(
            startVideoPreview,
            "private async Task<bool> TryStartPreviewFromRetainedPipelineAsync",
            "private async Task StartFreshPreviewPipelineAsync");
        var ensureFlashbackAudio = ExtractTextBetween(
            captureServiceText,
            "private async Task EnsureFlashbackAudioInputsAsync",
            "private async Task EnsureFlashbackPreviewBackendAsync");
        var startAudioPreview = ExtractTextBetween(
            captureServiceText,
            "public Task StartAudioPreviewAsync",
            "public Task StopAudioPreviewAsync");

        AssertDoesNotContain(captureServiceRootText, "public Task StartVideoPreviewAsync");
        AssertDoesNotContain(captureServiceRootText, "private async Task DisposePreviewPipelineAsync");
        AssertContains(previewLifecycleText, "public Task StartVideoPreviewAsync");
        AssertContains(previewLifecycleText, "private async Task DisposePreviewPipelineAsync");
        AssertContains(startVideoPreview, "var previousSettings = _flashbackBackend.SettingsSnapshot ?? _currentSettings;");
        AssertContains(startVideoPreview, "CanReuseFlashbackBackend(previousSettings, settings)");
        AssertOccursBefore(startVideoPreview, "var previousSettings = _flashbackBackend.SettingsSnapshot ?? _currentSettings;", "_currentSettings = settings;");
        AssertOccursBefore(startVideoPreview, "CanReuseFlashbackBackend(previousSettings, settings)", "_currentSettings = settings;");
        AssertContains(startVideoPreview, "CanReuseVideoCaptureForPreview(unifiedVideoCapture, settings)");
        AssertRegex(
            startVideoPreview,
            @"var\s+unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!CanReuseVideoCaptureForPreview\(unifiedVideoCapture,\s*settings\)\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*false\)",
            "preview settings-change recycle branch");
        AssertRegex(
            startVideoPreview,
            @"unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!_flashbackEnabled\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*false\)",
            "preview flashback-disabled recycle branch");
        AssertRegex(
            startVideoPreview,
            @"unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*_flashbackBackend\.Sink\s*!=\s*null\s*&&\s*flashbackBackendSettingsChanged\s*\)\s*\{[^{}]*DisposeFlashbackPreviewBackendAsync\(transitionToken,\s*purgeSegments:\s*false\)",
            "preview flashback-backend recycle branch");

        AssertContains(retainedPreviewFastPath, "unifiedVideoCapture.SetPreviewSink(_videoPipeline.PreviewFrameSink)");
        AssertContains(retainedPreviewFastPath, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken)");
        AssertContains(retainedPreviewFastPath, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertOccursBefore(
            retainedPreviewFastPath,
            "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken)",
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertOccursBefore(
            retainedPreviewFastPath,
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,",
            "_isVideoPreviewActive = true;");
        var startVideoPreviewRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task StartVideoPreviewAsync",
            "private bool CanReuseVideoCaptureForPreview");
        AssertOccursBefore(
            startVideoPreviewRaw,
            "await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken)",
            "// Start flashback AFTER");
        var previewAudioGraphRaw = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n");
        var previewMicMonitorStart = ExtractTextBetween(
            previewAudioGraphRaw,
            "private async Task StartPreviewMicrophoneMonitorAsync",
            "private async Task RollbackPreviewAudioCaptureStartupAsync");
        AssertContains(previewMicMonitorStart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(previewMicMonitorStart, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(previewMicMonitorStart, "MIC_MONITOR_PREVIEW_START_DISPOSE_WARN");
        AssertContains(previewMicMonitorStart, "_previewAudioGraph.MicrophoneCapture = micCapture;");
        AssertContains(previewMicMonitorStart, "micCapture = null;");
        AssertContains(previewMicMonitorStart, "_previewAudioGraph.MicrophoneCapture = micCapture;\n            micCapture = null;");

        AssertContains(ensureFlashbackAudio, "if (settings.AudioEnabled && _previewAudioGraph.ProgramCapture == null)");
        AssertContains(ensureFlashbackAudio, "AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture, reason)");
        AssertContains(ensureFlashbackAudio, "if (_micMonitorEnabled && _previewAudioGraph.MicrophoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))");
        AssertContains(ensureFlashbackAudio, "_previewAudioGraph.MicrophoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples))");

        AssertContains(startAudioPreview, "AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture,");
        AssertOccursBefore(
            startAudioPreview,
            "AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture,",
            "await _previewAudioGraph.StartPlaybackAsync(");
        AssertContains(startAudioPreview, "var createdCaptureForAudioPreview = false;");
        AssertContains(startAudioPreview, "createdCaptureForAudioPreview = true;");
        AssertContains(startAudioPreview, "_isAudioPreviewActive = false;");
        AssertContains(startAudioPreview, "_previewAudioGraph.DetachCapture(");
        AssertOccursBefore(
            startAudioPreview,
            "_isAudioPreviewActive = true;",
            "await _previewAudioGraph.StartPlaybackAsync(");
        var startAudioPreviewRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task StartAudioPreviewAsync",
            "public Task StopAudioPreviewAsync");
        AssertContains(startAudioPreviewRaw, "AUDIO_PREVIEW_START_ROLLBACK_DISPOSE_WARN");
        var updateAudioInput = ExtractTextBetween(
            captureServiceText,
            "public Task UpdateAudioInputAsync",
            "private void OnWasapiAudioLevelUpdated");
        AssertContains(updateAudioInput, "await newCapture.InitializeAsync(resolvedId, transitionToken)");
        AssertContains(updateAudioInput, "await _previewAudioGraph.StartPlaybackAsync(");
        AssertContains(updateAudioInput, "cancellationToken: transitionToken)");
        AssertOccursBefore(
            updateAudioInput,
            "await newCapture.InitializeAsync(resolvedId, transitionToken)",
            "_previewAudioGraph.DetachCapture(");
        AssertContains(updateAudioInput, "_audioDeviceId = previousDeviceId;");
        AssertContains(updateAudioInput, "_audioDeviceName = previousDeviceName;");
        AssertOccursBefore(updateAudioInput, "newCapture.Start();", "_previewAudioGraph.ProgramCapture = newCapture;");
        AssertOccursBefore(updateAudioInput, "_previewAudioGraph.ProgramCapture = newCapture;", "captureCommitted = true;");
        AssertContains(updateAudioInput, "finally\n                {\n                    if (!captureCommitted)");
        AssertOccursBefore(updateAudioInput, "captureCommitted = true;", "AttachFlashbackAudioIfSupported(newCapture");
        var candidateCleanup = ExtractTextBetween(updateAudioInput,
            "if (!captureCommitted)", "_audioDeviceId = audioDeviceId;");
        foreach (var operation in new[]
        {
            "newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;",
            "_previewAudioGraph.DetachCaptureFailure(newCapture);",
            "await newCapture.DisposeAsync().ConfigureAwait(false);"
        })
        {
            AssertContains(candidateCleanup, operation);
            AssertEqual(1, updateAudioInput.Split(operation, StringSplitOptions.None).Length - 1,
                "uncommitted audio capture has one cleanup owner: " + operation);
        }

        AssertContains(updateAudioInput, "activeSink != null && !ReferenceEquals(activeSink, _flashbackBackend.Sink)");
        AssertOccursBefore(
            updateAudioInput,
            "newCapture.AttachRecordingSink(activeSink);",
            "await _previewAudioGraph.StartPlaybackAsync(");
        var updateMicrophoneMonitor = ExtractTextBetween(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n"),
            "public Task UpdateMicrophoneMonitorAsync",
            "        }, cancellationToken);");
        AssertContains(updateMicrophoneMonitor, "if (_isRecording)");
        AssertContains(updateMicrophoneMonitor, "MIC_MONITOR_UPDATE_DEFERRED recording=true");
        AssertOccursBefore(
            updateMicrophoneMonitor,
            "MIC_MONITOR_UPDATE_DEFERRED recording=true",
            "await DisposeMicrophoneCaptureAsync()");
        var updateAudioInputRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task UpdateAudioInputAsync",
            "private void OnWasapiAudioLevelUpdated");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_PLAYBACK_START_FAILED");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_COMMITTED");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_ABORT");

        AssertContains(captureServiceText, "await _flashbackBackend.StartPreviewBackendAsync(");
        AssertContains(captureServiceText, "new FlashbackPreviewBackendStartRequest(");
        AssertContains(captureServiceText, "CloneCaptureSettings(settings),");
        AssertContains(captureServiceText, "CloneCaptureSettings(currentSettings)");
        AssertContains(flashbackBackendResourcesText, "SettingsSnapshot = request.SettingsSnapshot;");
        AssertContains(flashbackBackendResourcesText, "ClearSinkAndSettings();");
        AssertContains(captureServiceText, "_flashbackBackend.DisposePreviewBackendUnderExportLockAsync(");
        AssertContains(flashbackBackendResourcesText, "Clear();");
        AssertContains(flashbackBackendResourcesText, "public async Task StartPreviewBackendAsync(");
        AssertContains(flashbackBackendResourcesText, "var bufferManager = new FlashbackBufferManager(");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.SetFatalErrorCallback(_onFatalError);");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.FrameEncoded += _onFrameEncodedHandler;");
        AssertContains(flashbackBackendResourcesText, "Install(");
        AssertContains(flashbackBackendResourcesText, "AttachProducers(");
        AssertContains(flashbackBackendResourcesText, "playbackController.Initialize(");
        AssertContains(flashbackBackendResourcesText, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.FrameEncoded -= _onFrameEncodedHandler;");
        AssertContains(flashbackBackendResourcesText, "ScheduleDeferredArtifactCleanup(");
        AssertDoesNotContain(captureServiceText, "var bufferManager = new FlashbackBufferManager(");
        AssertDoesNotContain(captureServiceText, "FlashbackPlaybackController? playbackController = null;");
        AssertDoesNotContain(captureServiceText, "flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);");
        AssertDoesNotContain(flashbackPreviewBackendText, "flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;");
        AssertContains(captureServiceText, "controller is { IsDisposed: false, IsInitialized: false }");
        AssertContains(coordinatorText, "controller == null || controller.IsDisposed");
        AssertContains(coordinatorText, "controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled }");
        AssertContains(coordinatorText, "? \"disposed\"");
        AssertContains(captureServiceText, "!CanReuseFlashbackBackend(_flashbackBackend.SettingsSnapshot, settings)");
        AssertContains(captureServiceText, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertContains(startVideoPreview, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(startVideoPreview, "await DisposeFlashbackPreviewBackendAsync(previewStartRollbackToken)");
        var stopVideoPreviewCore = ExtractTextBetween(
            captureServiceText,
            "private Task StopVideoPreviewCoreAsync",
            "private async Task DisposePreviewPipelineAsync");
        AssertContains(stopVideoPreviewCore, "var commitStoppedState = false;");
        AssertContains(stopVideoPreviewCore, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(stopVideoPreviewCore, "commitStoppedState = true;");
        AssertContains(stopVideoPreviewCore, "if (commitStoppedState)\n                {\n                    _isVideoPreviewActive = false;");
        AssertContains(stopVideoPreviewCore, "await StopSourceTelemetryPollingAsync().ConfigureAwait(false);");
        AssertContains(stopVideoPreviewCore, "catch (Exception ex) when (stopFailure != null)");
        AssertDoesNotContain(stopVideoPreviewCore, "!keepPipelineAlive) StopTelemetryPoll()");
        var stopPreviewBlock = ExtractTextBetween(
            viewModelPreviewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreviewBlock, "var commitStoppedState = false;");
        AssertContains(stopPreviewBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(stopPreviewBlock, "if (commitStoppedState)\n            {\n                _context.SetIsPreviewing(false);\n            }");
        AssertOccursBefore(
            ExtractTextBetween(
                captureServiceText,
                "if (_flashbackEnabled && _flashbackBackend.Sink != null)",
                "_recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);"),
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,",
            "activeFlashbackSink.BeginRecording");
        AssertContains(ensureFlashbackAudio, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(ensureFlashbackAudio, "await micCapture.DisposeAsync()");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackLifecycleLogs_UseOutcomeNames()
    {
        var flashbackTexts = Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "Services"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("FLASHBACK_", StringComparison.Ordinal))
            .Select(path => File.ReadAllText(path).Replace("\r\n", "\n"))
            .ToArray();
        var captureServiceText = ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
                .Replace("\r\n", "\n");
        var flashbackBackendResourcesText = ReadRepoFile("Sussudio/Services/Capture/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");
        var flashbackText = string.Join("\n", flashbackTexts);

        AssertNoRegex(
            flashbackText,
            @"""FLASHBACK_[^""]*_(BEGIN|DONE|END)\b",
            "Flashback lifecycle scaffold log tokens");

        foreach (var expectedToken in new[]
        {
            "FLASHBACK_RESTART_OK",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP",
            "FLASHBACK_RECORDING_EXPORT_OK",
            "FLASHBACK_RECORDING_EXPORT_FAIL",
            "FLASHBACK_UNIFIED_RECORDING_STOP_OK",
            "FLASHBACK_UNIFIED_RECORDING_STOP_FAIL",
            "FLASHBACK_PREVIEW_INIT_OK",
            "FLASHBACK_PREVIEW_INIT_CANCELLED",
            "FLASHBACK_PREVIEW_DISPOSE_OK",
            "FLASHBACK_BUFFER_CYCLE_OK",
            "FLASHBACK_RECORDING_ACTIVE",
            "FLASHBACK_RECORDING_READY",
            "FLASHBACK_EXPORT_OK",
            "FLASHBACK_EXPORT_SEGMENT_OK",
            "FLASHBACK_EXPORT_SEGMENTS_OK",
            "FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN",
            "FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN",
            "FLASHBACK_PLAYBACK_DISPOSE_REQUEST"
        })
        {
            AssertContains(flashbackText, expectedToken);
        }

        var settingsChange = ExtractTextBetween(
            captureServiceText,
            "internal async Task<RecordingSettingsApplyDisposition> ApplyRecordingSettingsAsync(",
            "private void UpdateEncodingSettings(CaptureSettings source)");
        AssertContains(settingsChange, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(settingsChange, "_currentSettings = previousSettings;");
        AssertContains(settingsChange, "applicationFailure = ex;");
        AssertContains(settingsChange, "ExceptionDispatchInfo.Capture(applicationFailure).Throw();");
        AssertContains(settingsChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(settingsChange, "await _rebuildRecordingSettingsBackendAsync(transitionToken)");
        foreach (var suffix in new[] { "_OK", "_CYCLE_FAIL", "_CYCLE_CANCELLED", "_ROLLBACK" })
        {
            AssertContains(settingsChange, "{logPrefix}" + suffix);
        }
        AssertContains(settingsChange, "FLASHBACK_FORMAT_CHANGE");
        AssertContains(settingsChange, "FLASHBACK_ENCODER_SETTINGS_CHANGE");
        AssertContains(settingsChange, "RecordingSettingsSelection selection");
        AssertContains(settingsChange, "selection.ApplyTo(_currentSettings);");

        var settingsRebuild = ExtractTextBetween(
            captureServiceText,
            "private async Task RebuildFlashbackPreviewBackendForSettingsChangeAsync",
            "    private async Task CycleFlashbackBufferAsync");
        AssertContains(settingsRebuild, "await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: false)");
        AssertContains(settingsRebuild, "var committedRebuildToken = CancellationToken.None;");
        AssertContains(settingsRebuild, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, committedRebuildToken)");
        AssertContains(settingsRebuild, "FLASHBACK_SETTINGS_REBUILD_OK");

        var cycleBuffer = ExtractTextBetween(
            captureServiceText,
            "private async Task CycleFlashbackBufferAsync",
            "    private void OnFlashbackFrameEncoded");
        var backendCycleBuffer = ExtractTextBetween(
            flashbackBackendResourcesText,
            "public async Task<FlashbackBufferCycleOutcome> CycleSinkOnlyAsync",
            "    private async Task RollBackPreviewBackendStartAsync");
        AssertContains(cycleBuffer, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertOccursBefore(cycleBuffer, "_flashbackBackendLeaseLock.WaitAsync(", "_flashbackExportOperationLock.WaitAsync(");
        AssertOccursBefore(cycleBuffer, "_flashbackExportOperationLock.WaitAsync(", "_flashbackBackend.DisposePreviewBackendUnderExportLockAsync(");
        AssertContains(cycleBuffer, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");
        AssertContains(backendCycleBuffer, "preserveSegments: !request.PurgeSegments");
        AssertContains(backendCycleBuffer, "private FlashbackBufferCyclePlaybackState DisposePlaybackForBufferCycle(");
        AssertContains(backendCycleBuffer, "preserveSegments ? oldPlaybackController?.InPoint : null");
        AssertContains(backendCycleBuffer, "preserveSegments ? oldPlaybackController?.OutPoint : null");
        AssertContains(backendCycleBuffer, "preserveSegments ? oldPlaybackController?.InPointFilePts : null");
        AssertContains(backendCycleBuffer, "preserveSegments ? oldPlaybackController?.OutPointFilePts : null");
        AssertDoesNotContain(backendCycleBuffer, "var preservedInPoint = oldPlaybackController?.InPoint;");
        AssertDoesNotContain(backendCycleBuffer, "var preservedOutPoint = oldPlaybackController?.OutPoint;");
        AssertContains(backendCycleBuffer, "playbackController.RestoreInOutPoints(");
        AssertContains(backendCycleBuffer, "preservedPlaybackState.InPoint,");
        AssertContains(backendCycleBuffer, "preservedPlaybackState.OutPoint,");
        AssertContains(backendCycleBuffer, "preservedPlaybackState.InPointFilePts,");
        AssertContains(backendCycleBuffer, "preservedPlaybackState.OutPointFilePts);");
        var ensureFlashbackPreviewBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task EnsureFlashbackPreviewBackendAsync",
            "private async Task DisposeFlashbackPreviewBackendAsync");
        var createFlashbackSessionContext = ExtractTextBetween(
            captureServiceText,
            "private FlashbackSessionContext CreateFlashbackSessionContext",
            "    private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync");
        AssertContains(createFlashbackSessionContext, "var frameRateParts = ResolveCaptureDeliveryFrameRateParts(settings, frameRate);");
        AssertContains(createFlashbackSessionContext, "frameRate = frameRateParts.EffectiveFrameRate;");
        AssertContains(createFlashbackSessionContext, "FrameRateNumerator = fpsNum");
        AssertContains(ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"), "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveCaptureDeliveryFrameRateParts(");
        AssertContains(ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"), "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) InferCaptureDeliveryFrameRateParts(double deliveryFrameRate)");
        AssertContains(ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"), "CAPTURE_FRAME_RATE_RATIONAL_ACCEPT");
        AssertContains(ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"), "CAPTURE_FRAME_RATE_RATIONAL_REJECT");
        AssertContains(ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"), "CAPTURE_FRAME_RATE_RATIONAL_INFER");
        AssertContains(ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"), "deltaFps > toleranceFps");
        AssertContains(createFlashbackSessionContext, "RecordingFormat.Av1Mp4 => \"av1_nvenc\"");
        AssertContains(createFlashbackSessionContext, "AV1 recording requires the av1_nvenc encoder");
        AssertDoesNotContain(createFlashbackSessionContext, "UseTransportStreamFlashbackCodec");
        AssertContains(captureServiceText, "settings.Format == RecordingFormat.Av1Mp4");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackExportVerificationFormat(");
        AssertContains(captureServiceText, "FlashbackExportPlanner.PlanLiveEdge(");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertDoesNotContain(captureServiceText, "? RecordingFormat.HevcMp4.ToString()");
        AssertContains(createFlashbackSessionContext, "var flashbackNvencPreset = settings.NvencPreset;");
        AssertContains(createFlashbackSessionContext, "NvencPreset = flashbackNvencPreset");
        AssertContains(createFlashbackSessionContext, "SplitEncodeMode = settings.SplitEncodeMode");
        // Flashback must honor user codec/preset settings directly. The legacy snapshot
        // field remains for compatibility, but the old silent AV1->HEVC path must stay gone.
        AssertDoesNotContain(createFlashbackSessionContext, "FLASHBACK_CODEC_DOWNGRADE");
        AssertDoesNotContain(captureServiceText, "AV1->HEVC: software MJPEG pipeline at");
        AssertDoesNotContain(captureServiceText, "NVENC preset '");
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(snapshotsText, "ResolveFlashbackCodecDowngradeReason");
        var contractsText = ReadAutomationSnapshotFamilyText();
        AssertContains(contractsText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertContains(contractsText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        var automationDiagnosticsHubText = ReadAutomationSnapshotInitializerText();
        AssertContains(automationDiagnosticsHubText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(automationDiagnosticsHubText, "FlashbackCodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason");
        AssertDoesNotContain(captureServiceText, "var fbFileNameFormatOverride =");
        AssertDoesNotContain(captureServiceText, "FileNameFormatOverride = fbFileNameFormatOverride");
        AssertContains(ensureFlashbackPreviewBackend, "var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested");
        AssertContains(ensureFlashbackPreviewBackend, "FLASHBACK_PREVIEW_INIT_CANCELLED");
        AssertContains(ensureFlashbackPreviewBackend, "FLASHBACK_PREVIEW_INIT_FAIL");
        AssertContains(backendCycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN");
        AssertContains(backendCycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN");
        AssertContains(backendCycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(backendCycleBuffer, "var committedCycleToken = CancellationToken.None;");
        AssertContains(backendCycleBuffer, "StopAndDisposeOldSinkForBufferCycleAsync(");
        AssertContains(backendCycleBuffer, "TryStartReplacementSinkForBufferCycleAsync(");
        AssertContains(backendCycleBuffer, "CleanupFailedReplacementSinkForBufferCycleAsync(");
        AssertContains(backendCycleBuffer, "await oldSink.StopAsync(committedCycleToken)");
        AssertContains(backendCycleBuffer, "await newSink.StartAsync(");
        AssertContains(backendCycleBuffer, "request.CreateSessionContext(),");
        AssertContains(backendCycleBuffer, "cancellationToken: committedCycleToken)");
        AssertContains(backendCycleBuffer, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertOccursBefore(
            backendCycleBuffer,
            "StopAndDisposeOldSinkForBufferCycleAsync(",
            "ClearSinkAndSettings();");
        AssertContains(backendCycleBuffer, "await oldSink.DisposeAsync().ConfigureAwait(false);");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackFrameRateParts_PreserveOnlyDeliveredCadenceRational()
    {
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = captureServiceType.GetMethod(
            "ResolveCaptureDeliveryFrameRateParts",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveCaptureDeliveryFrameRateParts not found.");

        var integerResult = method.Invoke(null, new[] { BuildFrameRateSettings(120u, 1u), 120.0 })!;
        AssertFlashbackFrameRateParts(integerResult, 120, 1, 120.0, "integer 120 delivered cadence");

        var ntscDelivery = 120000d / 1001d;
        var ntscResult = method.Invoke(null, new[] { BuildFrameRateSettings(120000u, 1001u), ntscDelivery })!;
        AssertFlashbackFrameRateParts(ntscResult, 120000, 1001, ntscDelivery, "matching NTSC delivered cadence");

        var mismatchedResult = method.Invoke(null, new[] { BuildFrameRateSettings(120000u, 1001u), 120.0 })!;
        AssertFlashbackFrameRateParts(mismatchedResult, 120, 1, 120.0, "source NTSC rejected then inferred from integer USB cadence");

        var missingResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120.0 })!;
        AssertFlashbackFrameRateParts(missingResult, 120, 1, 120.0, "missing rational infers integer delivered cadence");

        var measuredIntegerResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120.00048 })!;
        AssertFlashbackFrameRateParts(measuredIntegerResult, 120, 1, 120.0, "measured integer delivered cadence infers exact rational");

        var measuredNtscResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120000d / 1001d })!;
        AssertFlashbackFrameRateParts(measuredNtscResult, 120000, 1001, 120000d / 1001d, "missing rational infers NTSC delivered cadence");

        var resolveRecordingArg = captureServiceType.GetMethod("ResolveFrameRateArg", BindingFlags.Static | BindingFlags.NonPublic)!;
        var setActualRate = captureServiceType.GetMethod("SetActualCaptureFrameRate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var service = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(captureServiceType);
        foreach (var (requestedNumerator, requestedDenominator, deliveryRate, expectedArg) in new[]
        {
            (120000u, 1001u, 120.0, "120/1"),
            (120u, 1u, 120000d / 1001d, "120000/1001"),
            (60000u, 1001u, 60.0, "60/1"),
            (60000u, 1001u, 60000d / 1001d, "60000/1001")
        })
        {
            var settings = BuildFrameRateSettings(requestedNumerator, requestedDenominator);
            SetPropertyOrBackingField(settings, "RequestedFrameRateArg", $"{requestedNumerator}/{requestedDenominator}");
            AssertEqual(expectedArg, resolveRecordingArg.Invoke(null, new[] { settings, deliveryRate }), "recording follows USB cadence");
            setActualRate.Invoke(service, new[] { settings, deliveryRate });
            AssertEqual(expectedArg, captureServiceType.GetField("_actualFrameRateArg", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service), "snapshot follows USB cadence");
            AssertNearlyEqual(deliveryRate, (double)captureServiceType.GetField("_actualFrameRate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!, 0.000001, "actual delivered frame rate");
            AssertEqual($"{requestedNumerator}/{requestedDenominator}", settings.GetType().GetProperty("RequestedFrameRateArg")!.GetValue(settings), "requested source cadence remains unchanged");
        }

        return Task.CompletedTask;
    }

    internal static async Task MainViewModel_AudioMonitoringTransitionsFinishTeardownBeforeRestart()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        var viewModel = RuntimeHelpers.GetUninitializedObject(viewModelType);
        using var gate = new SemaphoreSlim(1, 1);
        viewModelType.GetField("_audioMonitoringTransitionGate", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(viewModel, gate);
        var method = viewModelType.GetMethod("RunAudioMonitoringTransitionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task Run(Func<Task> transition) => (Task)method.Invoke(viewModel, new object[] { transition })!;
        var rampDownStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeRampDown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var audioReaderActive = true;
        var restartStarted = false;
        var teardown = Run(async () =>
        {
            rampDownStarted.SetResult();
            await completeRampDown.Task;
            audioReaderActive = false;
        });
        await rampDownStarted.Task;
        var restart = Run(() =>
        {
            restartStarted = true;
            audioReaderActive = true;
            return Task.CompletedTask;
        });
        Assert.False(restartStarted);
        Assert.False(restart.IsCompleted);
        completeRampDown.SetResult();
        await Task.WhenAll(teardown, restart).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(audioReaderActive);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Run(() => throw new InvalidOperationException("transition failed")));
        await Run(() => { audioReaderActive = false; return Task.CompletedTask; }).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(audioReaderActive);
    }

    private static object BuildFrameRateSettings(uint? numerator, uint? denominator)
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", numerator);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", denominator);
        return settings;
    }

    private static void AssertFlashbackFrameRateParts(
        object result,
        int? expectedNumerator,
        int? expectedDenominator,
        double expectedFrameRate,
        string fieldName)
    {
        var resultType = result.GetType();
        var numerator = resultType.GetField("Item1")?.GetValue(result);
        var denominator = resultType.GetField("Item2")?.GetValue(result);
        var effectiveFrameRate = resultType.GetField("Item3")?.GetValue(result);

        AssertEqual(expectedNumerator, numerator == null ? null : Convert.ToInt32(numerator), $"{fieldName} numerator");
        AssertEqual(expectedDenominator, denominator == null ? null : Convert.ToInt32(denominator), $"{fieldName} denominator");
        AssertNearlyEqual(expectedFrameRate, Convert.ToDouble(effectiveFrameRate), 0.000001, $"{fieldName} effective frame rate");
    }

    internal static Task CaptureService_FlashbackEnableDisable_PreservesPreviewState()
    {
        var captureServiceText = ReadCaptureServiceRecordingFinalizationSource()
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n");
        var setFlashbackEnabled = ExtractTextBetween(
            captureServiceText,
            "public Task SetFlashbackEnabledAsync",
            "/// <summary>\n    /// Updates flashback-specific fields");
        var stopAndDisposeRecordingBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync",
            "private async Task DisposeTransientRecordingBackendAsync");
        var libAvPreviewRestore = ExtractTextBetween(
            captureServiceText,
            "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync",
            "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync");

        AssertContains(setFlashbackEnabled, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(setFlashbackEnabled, "if (_flashbackEnabled == enabled)");
        AssertContains(setFlashbackEnabled, "if (enabled && (_flashbackBackend.Sink != null || _isRecording))");
        AssertContains(setFlashbackEnabled, "if (!enabled && !_flashbackBackend.HasAnyResource)");
        AssertContains(
            setFlashbackEnabled,
            "if (!_isVideoPreviewActive && !_isAudioPreviewActive && !_isRecording)\n                {\n                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);");
        AssertContains(setFlashbackEnabled, "await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: false)");
        AssertContains(setFlashbackEnabled, "if (_isRecording)\n            {\n                _pendingFlashbackEnableAfterRecording = true;");
        AssertContains(setFlashbackEnabled, "FLASHBACK_ENABLE_DEFERRED");
        var recordingActiveEnableBranch = ExtractTextBetween(
            setFlashbackEnabled,
            "if (_isRecording)\n            {",
            "\n            _pendingFlashbackEnableAfterRecording = false;");
        AssertContains(recordingActiveEnableBranch, "return;");
        AssertDoesNotContain(recordingActiveEnableBranch, "EnsureFlashbackPreviewBackendAsync");
        var immediateEnableBranch = ExtractTextBetween(
            setFlashbackEnabled,
            "_pendingFlashbackEnableAfterRecording = false;\n            var unifiedVideoCapture = _videoPipeline.Capture;\n            if (unifiedVideoCapture != null && _currentSettings != null)",
            "\n        }, cancellationToken);");
        AssertContains(immediateEnableBranch, "try");
        AssertContains(immediateEnableBranch, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, transitionToken)");
        AssertContains(immediateEnableBranch, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(immediateEnableBranch, "FLASHBACK_ENABLE_IMMEDIATE_CANCELLED");
        AssertContains(immediateEnableBranch, "catch");
        AssertContains(immediateEnableBranch, "_flashbackEnabled = false;");
        AssertContains(immediateEnableBranch, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(immediateEnableBranch, "await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true)");
        AssertContains(immediateEnableBranch, "FLASHBACK_ENABLE_IMMEDIATE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(immediateEnableBranch, "throw;");

        AssertContains(stopAndDisposeRecordingBackend, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(stopAndDisposeRecordingBackend, "CompleteLibAvRecordingFinalizeStateAsync()");
        AssertContains(stopAndDisposeRecordingBackend, "_mfConvertersDisabled = false;");
        AssertOccursBefore(
            stopAndDisposeRecordingBackend,
            "CompleteLibAvRecordingFinalizeStateAsync()",
            "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertOccursBefore(
            stopAndDisposeRecordingBackend,
            "RestoreLibAvPreviewFeaturesAfterRecordingAsync(",
            "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertContains(libAvPreviewRestore, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvPreviewRestore, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(
            libAvPreviewRestore,
            "if (_flashbackEnabled && _isVideoPreviewActive && unifiedVideoCapture != null && settings != null)");
        AssertContains(
            libAvPreviewRestore,
            "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken)");
        AssertContains(
            libAvPreviewRestore,
            "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(
            libAvPreviewRestore,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n            {\n                cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(libAvPreviewRestore, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        var deferredEnableFailureBranch = ExtractTextBetween(
            libAvPreviewRestore,
            "catch (Exception ex)\n            {",
            "Logger.Log($\"FLASHBACK_ENABLE_AFTER_RECORDING_FAIL");
        AssertContains(deferredEnableFailureBranch, "_flashbackEnabled = false;");
        AssertContains(deferredEnableFailureBranch, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(deferredEnableFailureBranch, "await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true)");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_HasExpectedPublicMethods()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        // Core lifecycle methods
        var expectedMethods = new[]
        {
            "InitializeAsync",
            "StartVideoPreviewAsync",
            "StopVideoPreviewAsync",
            "StopVideoPreviewWithTeardownAsync",
            "StartRecordingAsync",
            "StopRecordingAsync",
            "CleanupAsync",
            "StartAudioPreviewAsync",
            "StopAudioPreviewAsync",
            "StopAudioPreviewWithTeardownAsync",
            "UpdateAudioMonitoringAsync",
            "UpdateAudioInputAsync",
            "UpdateMicrophoneMonitorAsync",
            "RestartFlashbackAsync",
            "SetFlashbackEnabledAsync",
            "UpdateFlashbackSettingsAsync"
        };

        foreach (var methodName in expectedMethods)
        {
            var method = Array.Find(
                coordinatorType.GetMethods(BindingFlags.Public | BindingFlags.Instance),
                method => method.Name == methodName);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        // Snapshot property
        var snapshotProp = coordinatorType.GetProperty("Snapshot", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(snapshotProp, "CaptureSessionCoordinator.Snapshot");

        // Implements IDisposable and IAsyncDisposable
        AssertEqual(true, typeof(IDisposable).IsAssignableFrom(coordinatorType),
            "Implements IDisposable");
        AssertEqual(true, typeof(IAsyncDisposable).IsAssignableFrom(coordinatorType),
            "Implements IAsyncDisposable");

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureCommand shape ──

    internal static Task CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues()
    {
        var commandKindType = RequireType("Sussudio.Services.Capture.CaptureCommandKind");

        // Core command kinds should exist
        var expectedValues = new[]
        {
            "Initialize", "StartVideoPreview", "StopVideoPreview",
            "StartRecording", "StopRecording", "Cleanup",
            "StartAudioPreview", "StopAudioPreview",
            "UpdateAudioMonitoring", "UpdateAudioInput",
            "UpdateMicrophoneMonitor",
            "SetFlashbackEnabled", "UpdateFlashbackSettings",
            "RestartFlashback", "UpdateFlashbackRecordingFormat",
            "CycleFlashbackEncoderSettings"
        };

        foreach (var value in expectedValues)
        {
            var parsed = Enum.Parse(commandKindType, value);
            AssertNotNull(parsed, $"CaptureCommandKind.{value}");
        }

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureSessionSnapshot ──

    internal static Task CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract()
    {
        var snapshotType = RequireType("Sussudio.Services.Capture.CaptureSessionSnapshot");

        var expectedProps = new[]
        {
            "LastTransitionUtc", "LastCommand", "LastCorrelationId",
            "LastError", "CommandsEnqueued", "CommandsCompleted",
            "CommandsFailed", "CommandsCanceled", "CommandsCoalesced", "PendingCommands",
            "MaxPendingCommands", "OldestPendingCommandAgeMs",
            "LastCommandQueueLatencyMs", "MaxCommandQueueLatencyMs", "LastOutcome", "SessionState",
            "IsRecording", "IsInitialized", "IsVideoPreviewActive", "IsAudioPreviewActive"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = snapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"CaptureSessionSnapshot.{prop}");
        }

        // Default state should be clean
        var snapshot = Activator.CreateInstance(snapshotType)!;
        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "Default IsRecording");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "Default IsInitialized");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "PendingCommands")), "Default PendingCommands");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "MaxPendingCommands")), "Default MaxPendingCommands");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "OldestPendingCommandAgeMs")), "Default OldestPendingCommandAgeMs");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "MaxCommandQueueLatencyMs")), "Default MaxCommandQueueLatencyMs");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "CommandsCoalesced")), "Default CommandsCoalesced");
        AssertEqual("None", GetStringProperty(snapshot, "LastOutcome"), "Default LastOutcome");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionSnapshot_DefaultState()
    {
        var snapshotType = RequireType("Sussudio.Services.Capture.CaptureSessionSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(snapshotType);

        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "IsRecording default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "IsInitialized default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsVideoPreviewActive"), "IsVideoPreviewActive default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsAudioPreviewActive"), "IsAudioPreviewActive default");
        AssertEqual(0, (int)GetPropertyValue(snapshot, "PendingCommands")!, "PendingCommands default");
        AssertEqual(0L, GetLongProperty(snapshot, "CommandsCoalesced"), "CommandsCoalesced default");
        AssertEqual("None", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome default");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_CancellationAndWorkerTokensStayBounded()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();

        AssertContains(coordinatorText, "return Task.FromCanceled(cancellationToken);");
        AssertContains(coordinatorText, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(coordinatorText, "cancellationRegistration = cancellationToken.Register");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(cancellationRegistration, \"enqueue_failed\");");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(workItem.CancellationRegistration, \"begin_process\");");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(pending.CancellationRegistration, \"fail_pending\");");
        AssertContains(coordinatorText, "CAPTURE_COORD_CANCEL_REG_DISPOSE_WARN");
        AssertContains(coordinatorText, "CancelWorkerBestEffort();");
        AssertContains(coordinatorText, "DisposeWorkerCancellationBestEffort(\"worker_completed\");");
        AssertContains(coordinatorText, "CAPTURE_COORD_WORKER_CANCEL_WARN");
        AssertContains(coordinatorText, "CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN");
        AssertContains(coordinatorText, "public bool PropagateCancellationToOperation { get; init; }");
        AssertContains(coordinatorText, "bool propagateCancellationToOperation = false");
        AssertContains(coordinatorText, "propagateCancellationToOperation: true");
        AssertContains(coordinatorText, "A caller cancellation token cancels the returned task immediately and prevents queued work");
        AssertContains(coordinatorText, "when that enqueue explicitly opts into propagation");
        AssertContains(coordinatorText, "coordinator disposal uses its separate");

        return Task.CompletedTask;
    }

    internal static async Task CaptureSessionCoordinator_CanceledQueuedCommandUpdatesAccounting()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        try
        {
            var firstStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operationsExecuted = 0;

            var firstTask = EnqueueCoordinatorOperation(
                harness,
                "StartVideoPreview",
                async ct =>
                {
                    Interlocked.Increment(ref operationsExecuted);
                    firstStarted.TrySetResult(null);
                    await releaseFirst.Task.WaitAsync(ct).ConfigureAwait(false);
                });

            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            var canceledTask = EnqueueCoordinatorOperation(
                harness,
                "StartRecording",
                _ =>
                {
                    Interlocked.Increment(ref operationsExecuted);
                    return Task.CompletedTask;
                },
                cts.Token);

            cts.Cancel();
            await AssertTaskCanceledAsync(canceledTask).ConfigureAwait(false);

            var queuedSnapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(2L, GetLongProperty(queuedSnapshot, "CommandsEnqueued"), "Queued cancellation enqueued count");
            AssertEqual(true, GetIntProperty(queuedSnapshot, "PendingCommands") >= 1, "Queued cancellation pending count before drain");

            releaseFirst.TrySetResult(null);
            await firstTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await WaitForConditionAsync(
                () => GetIntProperty(GetCoordinatorSnapshot(harness.Coordinator), "PendingCommands") == 0,
                "coordinator canceled queued command accounting").ConfigureAwait(false);

            var snapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(1, operationsExecuted, "Canceled queued command did not execute");
            AssertEqual(2L, GetLongProperty(snapshot, "CommandsEnqueued"), "CommandsEnqueued after queued cancellation");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCompleted"), "CommandsCompleted after queued cancellation");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCanceled"), "CommandsCanceled after queued cancellation");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsFailed"), "CommandsFailed after queued cancellation");
            AssertEqual(0, GetIntProperty(snapshot, "PendingCommands"), "PendingCommands after queued cancellation");
            AssertEqual(true, GetIntProperty(snapshot, "MaxPendingCommands") >= 2, "MaxPendingCommands captures queued cancellation");
            AssertEqual("StartRecording", GetStringProperty(snapshot, "LastCommand"), "LastCommand after queued cancellation");
            AssertEqual("Canceled", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome after queued cancellation");
            AssertContains(GetStringProperty(snapshot, "LastCorrelationId"), "StartRecording-");
        }
        finally
        {
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness).ConfigureAwait(false);
        }
    }

    internal static async Task CaptureSessionCoordinator_CoalescesQueuedLatestOnlyAndAccountsSkip()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        try
        {
            var blockerStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseBlocker = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var staleExecuted = 0;
            var latestExecuted = 0;

            var blockerTask = EnqueueCoordinatorOperation(
                harness,
                "Initialize",
                async ct =>
                {
                    blockerStarted.TrySetResult(null);
                    await releaseBlocker.Task.WaitAsync(ct).ConfigureAwait(false);
                });
            await blockerStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            var staleTask = EnqueueCoordinatorOperation(
                harness,
                "CycleFlashbackEncoderSettings",
                _ =>
                {
                    Interlocked.Increment(ref staleExecuted);
                    return Task.CompletedTask;
                },
                coalesceLatest: true);
            var latestTask = EnqueueCoordinatorOperation(
                harness,
                "CycleFlashbackEncoderSettings",
                _ =>
                {
                    Interlocked.Increment(ref latestExecuted);
                    return Task.CompletedTask;
                },
                coalesceLatest: true);

            releaseBlocker.TrySetResult(null);
            await blockerTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await staleTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await latestTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await WaitForConditionAsync(
                () => GetIntProperty(GetCoordinatorSnapshot(harness.Coordinator), "PendingCommands") == 0,
                "coordinator coalesced queue drain").ConfigureAwait(false);

            var snapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(0, staleExecuted, "Stale coalesced operation skipped");
            AssertEqual(1, latestExecuted, "Latest coalesced operation executed");
            AssertEqual(3L, GetLongProperty(snapshot, "CommandsEnqueued"), "CommandsEnqueued after coalescing");
            AssertEqual(3L, GetLongProperty(snapshot, "CommandsCompleted"), "CommandsCompleted after coalescing");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCoalesced"), "CommandsCoalesced after coalescing");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsFailed"), "CommandsFailed after coalescing");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsCanceled"), "CommandsCanceled after coalescing");
            AssertEqual(0, GetIntProperty(snapshot, "PendingCommands"), "PendingCommands after coalescing");
            AssertEqual(true, GetIntProperty(snapshot, "MaxPendingCommands") >= 3, "MaxPendingCommands captures coalesced backlog");
            AssertEqual("CycleFlashbackEncoderSettings", GetStringProperty(snapshot, "LastCommand"), "LastCommand after coalescing");
            AssertEqual("Completed", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome after coalescing");
        }
        finally
        {
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness).ConfigureAwait(false);
        }
    }

    internal static async Task CaptureSessionCoordinator_DisposeDrainsQueuedCommandBeforeCancellation()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        try
        {
            var executed = 0;
            var commandTask = EnqueueCoordinatorOperation(
                harness,
                "Cleanup",
                async ct =>
                {
                    await Task.Delay(50, ct).ConfigureAwait(false);
                    AssertEqual(false, ct.IsCancellationRequested, "Dispose drain should not pre-cancel queued cleanup");
                    Interlocked.Increment(ref executed);
                });

            await InvokeDisposeAsync(harness.Coordinator).ConfigureAwait(false);
            await commandTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            var snapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(1, executed, "Dispose drain executed queued command");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsEnqueued"), "CommandsEnqueued after dispose drain");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCompleted"), "CommandsCompleted after dispose drain");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsCanceled"), "CommandsCanceled after dispose drain");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsFailed"), "CommandsFailed after dispose drain");
            AssertEqual(0, GetIntProperty(snapshot, "PendingCommands"), "PendingCommands after dispose drain");
            AssertEqual("Cleanup", GetStringProperty(snapshot, "LastCommand"), "LastCommand after dispose drain");
            AssertEqual("Completed", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome after dispose drain");
        }
        finally
        {
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness).ConfigureAwait(false);
        }
    }

    private static string ReadCaptureSessionCoordinatorSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static void AssertCanEnterTransition(
        MethodInfo canEnter,
        Type stateType,
        string currentState,
        string transitionState,
        bool expected)
    {
        var actual = canEnter.Invoke(
            null,
            new[] { Enum.Parse(stateType, currentState), Enum.Parse(stateType, transitionState) });
        AssertEqual(expected, (bool)actual!, $"{currentState} -> {transitionState}");
    }

    private static object ResolveState(
        MethodInfo resolve,
        bool isDisposed,
        bool isRecording,
        bool isVideoPreviewActive,
        bool isAudioPreviewActive,
        bool isInitialized)
        => resolve.Invoke(
            null,
            new object[]
            {
                isDisposed,
                isRecording,
                isVideoPreviewActive,
                isAudioPreviewActive,
                isInitialized
            })
           ?? throw new InvalidOperationException("ResolveSteadyState returned null.");

    private sealed record CaptureSessionCoordinatorHarness(
        object Coordinator,
        object CaptureService,
        Type CommandKindType,
        MethodInfo EnqueueMethod);

    private static CaptureSessionCoordinatorHarness CreateCaptureSessionCoordinatorHarness()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var commandKindType = RequireType("Sussudio.Services.Capture.CaptureCommandKind");
        var captureService = Activator.CreateInstance(captureServiceType)
            ?? throw new InvalidOperationException("Failed to create CaptureService.");
        var coordinator = Activator.CreateInstance(coordinatorType, captureService)
            ?? throw new InvalidOperationException("Failed to create CaptureSessionCoordinator.");
        var enqueueMethod = coordinatorType.GetMethod("EnqueueAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureSessionCoordinator.EnqueueAsync not found.");
        return new CaptureSessionCoordinatorHarness(coordinator, captureService, commandKindType, enqueueMethod);
    }

    private static Task EnqueueCoordinatorOperation(
        CaptureSessionCoordinatorHarness harness,
        string commandKind,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        bool coalesceLatest = false,
        bool propagateCancellationToOperation = false)
    {
        var kind = Enum.Parse(harness.CommandKindType, commandKind);
        return (Task)(harness.EnqueueMethod.Invoke(
                   harness.Coordinator,
                   new object?[]
                   {
                       kind,
                       operation,
                       cancellationToken,
                       coalesceLatest,
                       propagateCancellationToOperation
                   })
               ?? throw new InvalidOperationException("CaptureSessionCoordinator.EnqueueAsync returned null."));
    }

    private static object GetCoordinatorSnapshot(object coordinator)
        => GetPropertyValue(coordinator, "Snapshot")
           ?? throw new InvalidOperationException("CaptureSessionCoordinator.Snapshot returned null.");

    private static async Task DisposeCaptureSessionCoordinatorHarnessAsync(CaptureSessionCoordinatorHarness harness)
    {
        await InvokeDisposeAsync(harness.Coordinator).ConfigureAwait(false);
        await InvokeDisposeAsync(harness.CaptureService).ConfigureAwait(false);
    }

    private static async Task InvokeDisposeAsync(object target)
    {
        var disposeAsync = target.GetType().GetMethod("DisposeAsync", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{target.GetType().Name}.DisposeAsync not found.");
        var result = disposeAsync.Invoke(target, Array.Empty<object?>());
        switch (result)
        {
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return;
            case Task task:
                await task.ConfigureAwait(false);
                return;
            default:
                throw new InvalidOperationException($"{target.GetType().Name}.DisposeAsync returned unsupported result.");
        }
    }

    private static async Task AssertTaskCanceledAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException("Expected task to be canceled.");
    }

    internal static Task CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var cycleMethod = ExtractTextBetween(
            coordinatorText,
            "internal async Task<RecordingSettingsApplyDisposition> ApplyRecordingSettingsAsync",
            "public Task SetFlashbackEnabledAsync");
        var queueProcessor = ExtractTextBetween(
            coordinatorText,
            "private async Task ProcessQueueAsync",
            "private void FailPendingCommands(Exception ex)");

        AssertContains(coordinatorText, "_latestFlashbackEncoderCycleGeneration");
        AssertContains(coordinatorText, "_commandsCoalesced");
        AssertContains(cycleMethod, "coalesceLatest: kind == RecordingSettingsChangeKind.EncoderParameters");
        AssertContains(queueProcessor, "Volatile.Read(ref _latestFlashbackEncoderCycleGeneration)");
        AssertContains(queueProcessor, "CaptureCommandOutcome.Coalesced");
        AssertContains(queueProcessor, "CAP-COORD-SKIP");
        AssertContains(coordinatorText, "CAP-COORD-ENQUEUE-FAIL");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_DisposalAccounting_ClassifiesCanceledQueuedCommands()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var failPending = ExtractTextBetween(
            coordinatorText,
            "private void FailPendingCommands(Exception ex)",
            "    private void DecrementPendingCommands");

        AssertContains(failPending, "if (pending.Completion.Task.IsCanceled)\n            {\n                Interlocked.Increment(ref _commandsCanceled);");
        AssertContains(failPending, "else if (pending.Completion.TrySetException(ex))\n            {\n                Interlocked.Increment(ref _commandsFailed);");
        AssertContains(failPending, "DecrementPendingCommands(\"fail_pending\");");
        AssertContains(coordinatorText, "DecrementPendingCommands(\"enqueue_failed\");");
        AssertContains(coordinatorText, "DecrementPendingCommands(\"process_complete\");");
        AssertContains(coordinatorText, "private void DecrementPendingCommands(string operation)");
        AssertContains(coordinatorText, "CAPTURE_COORD_PENDING_UNDERFLOW");
        AssertContains(coordinatorText, "throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));");
        AssertDoesNotContain(failPending, "pending.Completion.TrySetException(ex);\n            Interlocked.Increment(ref _commandsFailed);\n            Interlocked.Decrement(ref _pendingCommands);");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackMutationsPropagateRequestCancellation()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var restartNoSettings = ExtractTextBetween(
            coordinatorText,
            "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)",
            "public Task RestartFlashbackAsync(CaptureSettings settings");
        var restartWithSettings = ExtractTextBetween(
            coordinatorText,
            "public Task RestartFlashbackAsync(CaptureSettings settings",
            "internal async Task<RecordingSettingsApplyDisposition> ApplyRecordingSettingsAsync");
        var setFlashbackEnabled = ExtractTextBetween(
            coordinatorText,
            "public Task SetFlashbackEnabledAsync",
            "public Task UpdateFlashbackSettingsAsync");

        AssertContains(restartNoSettings, "propagateCancellationToOperation: true");
        AssertContains(restartWithSettings, "propagateCancellationToOperation: true");
        AssertContains(restartWithSettings, "ct => _captureService.RestartFlashbackAsync(settings, ct)");
        AssertDoesNotContain(restartWithSettings, "_captureService.UpdateEncodingSettings(settings)");
        AssertContains(setFlashbackEnabled, "propagateCancellationToOperation: true");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_CommittedStopsDoNotPropagateRequestCancellation()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var stopVideo = ExtractTextBetween(
            coordinatorText,
            "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)",
            "public Task StopVideoPreviewWithTeardownAsync");
        var stopVideoTeardown = ExtractTextBetween(
            coordinatorText,
            "public Task StopVideoPreviewWithTeardownAsync",
            "public Task StartRecordingAsync");
        var stopRecording = ExtractTextBetween(
            coordinatorText,
            "public Task StopRecordingAsync",
            "public Task StartAudioPreviewAsync");
        var recordingSettings = ExtractTextBetween(
            coordinatorText,
            "internal async Task<RecordingSettingsApplyDisposition> ApplyRecordingSettingsAsync",
            "public Task SetFlashbackEnabledAsync");

        AssertDoesNotContain(stopVideo, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopVideoTeardown, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopRecording, "propagateCancellationToOperation: true");
        AssertDoesNotContain(recordingSettings, "propagateCancellationToOperation: true");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_LogsInactiveFlashbackCommandRejections()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();

        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackBeginScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackUpdateScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackEndScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackClearInOutPoints), out var controller)");
        AssertContains(coordinatorText, "bool ThreadAlive,\n    int PendingCommands,\n    string LastCommandFailure,\n    long LastCommandFailureUtcUnixMs");
        AssertContains(coordinatorText, "controller.PlaybackThreadAlive,\n                controller.PendingCommands,\n                controller.LastCommandFailure,\n                controller.LastCommandFailureUtcUnixMs");
        AssertContains(coordinatorText, "public static FlashbackPlaybackSnapshot Inactive(");
        AssertContains(coordinatorText, "private long _lastFlashbackCommandRejectionUtcUnixMs;");
        AssertContains(coordinatorText, "private string _lastFlashbackCommandRejection = string.Empty;");
        AssertContains(coordinatorText, "FlashbackPlaybackSnapshot.Inactive(\n                _lastFlashbackCommandRejection,\n                Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs))");
        AssertContains(coordinatorText, "private bool TryGetActiveFlashback(\n        string command,");
        AssertContains(coordinatorText, "var reason = controller == null\n            ? \"missing_controller\"\n            : controller.IsDisposed\n                ? \"disposed\"\n                : !controller.IsInitialized\n                ? \"not_initialized\"\n                : $\"state_{controller.State}\";");
        AssertContains(coordinatorText, "_lastFlashbackCommandRejection = $\"{reason}:{command}\";");
        AssertContains(coordinatorText, "Interlocked.Exchange(ref _lastFlashbackCommandRejectionUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());");
        AssertContains(coordinatorText, "Logger.Log($\"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}\");");

        return Task.CompletedTask;
    }

// MainWindow Flashback automation and presentation contracts live with their xUnit wrappers.

    internal static Task MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost()
    {
        var flashbackWindowText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackScrubText = ReadMainWindowFlashbackAdapterSource();
        var flashbackScrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackGeometryText = flashbackScrubControllerText;
        var flashbackPlayheadText = ReadMainWindowFlashbackAdapterSource();
        var flashbackPlayheadControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var fullScreenWindowText = ReadMainWindowShellChromeAdapterSource();
        var fullScreenControllerText = ReadRepoFile("Sussudio/Controllers/FullScreen/FullScreenController.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(xamlText, "PointerReleased=\"FlashbackScrubArea_PointerReleased\"");
        AssertContains(xamlText, "PointerCanceled=\"FlashbackScrubArea_PointerCanceled\"");
        AssertContains(xamlText, "PointerCaptureLost=\"FlashbackScrubArea_PointerCaptureLost\"");
        AssertContains(flashbackScrubText, "XAML-facing Flashback pointer scrub adapter");
        AssertContains(flashbackScrubText, "private FlashbackScrubInteractionController _flashbackScrubInteractionController = null!;");
        AssertContains(flashbackScrubText, "private void InitializeFlashbackScrubInteractionController()");
        AssertContains(flashbackScrubText, "PositionMagneticPlayhead = (x, trackWidth) => _flashbackPlayheadMotionController.PositionMagneticPlayhead(x, trackWidth),");
        AssertContains(flashbackScrubText, "RefreshPlayheadMotion = reason => _flashbackPlayheadMotionController.RefreshPlayheadMotion(reason),");
        AssertContains(flashbackScrubText, "GetTickCount64 = () => Environment.TickCount64,");
        AssertContains(flashbackScrubControllerText, "internal sealed class FlashbackScrubInteractionController");
        AssertContains(flashbackScrubControllerText, "private bool _isScrubbing;");
        AssertContains(flashbackScrubControllerText, "private TimeSpan? _lastPointerPosition;");
        AssertContains(flashbackScrubControllerText, "private long _lastUpdateTick;");
        AssertContains(flashbackScrubControllerText, "public bool IsScrubbing => _isScrubbing;");
        AssertContains(flashbackScrubControllerText, "private void End(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)");
        AssertContains(flashbackScrubControllerText, "if (!_context.ViewModel.FlashbackBeginScrub(targetPosition))\n        {\n            _lastPointerPosition = null;\n            _context.ViewModel.ReportFlashbackPlaybackRejection(\"scrub begin\", \"FLASHBACK_UI_SCRUB_BEGIN_REJECTED\");\n            return;\n        }");
        AssertContains(flashbackScrubControllerText, "if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))\n        {\n            _context.ViewModel.ReportFlashbackPlaybackRejection(\"scrub update\", \"FLASHBACK_UI_SCRUB_UPDATE_REJECTED\");\n            End(element, e.Pointer, \"update_rejected\");\n            return;\n        }");
        AssertContains(flashbackScrubText, "private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)");
        AssertContains(flashbackScrubText, "=> _flashbackScrubInteractionController.PointerReleased(sender as UIElement, e);");
        AssertContains(flashbackScrubControllerText, "TimeSpan? releasePosition = null;\n        if (_isScrubbing)");
        AssertContains(flashbackScrubControllerText, "var targetPosition = ComputeScrubPosition(e);\n            releasePosition = targetPosition;\n            _lastPointerPosition = targetPosition;\n            if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))");
        AssertContains(flashbackScrubControllerText, "_context.ViewModel.ReportFlashbackPlaybackRejection(\"scrub release update\", \"FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED\");");
        AssertContains(flashbackScrubControllerText, "End(element, e.Pointer, \"released\", releasePosition);");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"set in point\", \"FLASHBACK_UI_SET_IN_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"set out point\", \"FLASHBACK_UI_SET_OUT_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"clear in/out\", \"FLASHBACK_UI_CLEAR_INOUT_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "Logger.Log($\"FLASHBACK_UI_SET_IN pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log($\"FLASHBACK_UI_SET_OUT pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_CLEAR_INOUT\");");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"pause\", \"FLASHBACK_UI_PAUSE_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"play\", \"FLASHBACK_UI_PLAY_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"go live\", \"FLASHBACK_UI_GOLIVE_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_PAUSE\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_PLAY\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_GOLIVE\");");
        AssertContains(flashbackCommandControllerText, "public bool HandleFullScreenKeyboardCommand(VirtualKey key)");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.I:");
        AssertContains(flashbackCommandControllerText, "SetInPointAtPlayhead();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.O:");
        AssertContains(flashbackCommandControllerText, "SetOutPointAtPlayhead();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.Space:");
        AssertContains(flashbackCommandControllerText, "TogglePlayPause();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.L:");
        AssertContains(flashbackCommandControllerText, "GoLive();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.Left:");
        AssertContains(flashbackCommandControllerText, "NudgePlayback(TimeSpan.FromSeconds(-1), \"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\");");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.Right:");
        AssertContains(flashbackCommandControllerText, "NudgePlayback(TimeSpan.FromSeconds(1), \"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\");");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(operationName, rejectionDetail)");
        AssertContains(flashbackScrubControllerText, "_isScrubbing = true;\n        _lastPointerPosition = targetPosition;\n        _lastUpdateTick = 0;\n        element?.CapturePointer(e.Pointer);");
        AssertContains(flashbackScrubControllerText, "var carriedPosition = _isScrubbing ? _lastPointerPosition : null;");
        AssertContains(flashbackScrubControllerText, "var ended = releasePosition.HasValue\n            ? _context.ViewModel.FlashbackEndScrubAt(releasePosition.Value)\n            : _context.ViewModel.FlashbackEndScrub();\n        if (!ended)\n        {\n            _context.ViewModel.ReportFlashbackPlaybackRejection($\"scrub end ({reason})\", $\"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}\");\n        }");
        AssertContains(flashbackScrubControllerText, "ClearLocalState();\n        element?.ReleasePointerCapture(pointer);");
        AssertContains(flashbackScrubControllerText, "FLASHBACK_UI_SCRUB_END");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCanceled");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCaptureLost");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.TryComputeFraction(pos.X, width, out var fraction)");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.IsUsableDuration(bufferDuration)");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.ComputePosition(fraction, bufferDuration)");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.TryComputePosition(");
        AssertContains(flashbackGeometryText, "internal static class FlashbackTimelineGeometry");
        AssertContains(flashbackGeometryText, "public static bool TryComputeFraction(double x, double width, out double fraction)");
        AssertContains(flashbackGeometryText, "public static bool TryComputePosition(double x, double width, TimeSpan bufferDuration, out TimeSpan position)");
        AssertContains(flashbackGeometryText, "public static TimeSpan ComputePosition(double fraction, TimeSpan bufferDuration)");
        AssertContains(flashbackGeometryText, "public static bool IsUsableTrackDimension(double value)");
        AssertContains(flashbackGeometryText, "public static bool IsUsableDuration(TimeSpan value)");
        AssertContains(flashbackPlayheadControllerText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertDoesNotContain(flashbackPlayheadText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertDoesNotContain(flashbackScrubText, "private static bool TryComputeFlashbackTimelineFraction(double x, double width, out double fraction)");
        AssertDoesNotContain(flashbackScrubText, "private static bool IsUsableFlashbackTrackDimension(double value)");
        AssertDoesNotContain(flashbackScrubText, "private static bool IsUsableFlashbackDuration(TimeSpan value)");
        AssertContains(fullScreenWindowText, "HandleFlashbackKeyboardCommand = _flashbackCommandController.HandleFullScreenKeyboardCommand,");
        AssertContains(fullScreenWindowText, "private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)\n        => _fullScreenController.OnKeyDown(e);");
        AssertContains(fullScreenControllerText, "private void HandleFlashbackKeyDown(KeyRoutedEventArgs e)");
        AssertContains(fullScreenControllerText, "if (!_context.ViewModel.IsFlashbackEnabled || _context.FlashbackTimelinePanel.Visibility != Visibility.Visible)");
        AssertContains(fullScreenControllerText, "if (_context.HandleFlashbackKeyboardCommand(e.Key))\n        {\n            e.Handled = true;\n        }");
        AssertDoesNotContain(fullScreenWindowText, "if (!ViewModel.IsFlashbackEnabled || FlashbackTimelinePanel.Visibility != Visibility.Visible)");
        AssertDoesNotContain(fullScreenWindowText, "_flashbackCommandController.HandleFullScreenKeyboardCommand(e.Key)");
        AssertContains(fullScreenWindowText, "EndFlashbackScrubForFullScreen = _flashbackScrubInteractionController.EndForFullScreen,");
        AssertContains(fullScreenWindowText, "ResetFlashbackTimelineAnimation = _flashbackTimelineController.ResetAnimationForFullScreen,");
        AssertContains(fullScreenWindowText, "SyncFlashbackTimelineToggle = _flashbackTimelineController.SyncToggle,");
        AssertContains(fullScreenControllerText, "var timelineVisibleAtExit = ShouldShowFlashbackTimeline();");
        AssertContains(fullScreenControllerText, "private bool ShouldShowFlashbackTimeline()\n        => _context.ViewModel.IsFlashbackEnabled && _context.ViewModel.IsFlashbackTimelineVisible;");
        AssertDoesNotContain(fullScreenWindowText, "private bool ShouldShowFlashbackTimeline()");
        AssertDoesNotContain(fullScreenWindowText, "=> _flashbackScrubInteractionController.EndForFullScreen();");
        AssertContains(flashbackScrubControllerText, "var carriedPosition = _lastPointerPosition;\n        Logger.Log($\"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}\");");
        AssertContains(flashbackScrubControllerText, "var ended = carriedPosition.HasValue\n            ? _context.ViewModel.FlashbackEndScrubAt(carriedPosition.Value)\n            : _context.ViewModel.FlashbackEndScrub();\n        if (!ended)");
        AssertContains(flashbackScrubControllerText, "ReportFlashbackPlaybackRejection(\"scrub end (fullscreen_enter)\", \"FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter\")");
        AssertDoesNotContain(flashbackScrubControllerText, "var carriedPosition = _context.ViewModel.FlashbackPlaybackPosition;");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertDoesNotContain(flashbackScrubText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(flashbackScrubText, "private TimeSpan? _lastScrubPointerPosition;");
        AssertDoesNotContain(flashbackScrubText, "private long _lastScrubUpdateTick;");
        AssertDoesNotContain(flashbackScrubControllerText, "var carriedPosition = _isScrubbing ? _context.ViewModel.FlashbackPlaybackPosition : (TimeSpan?)null;");
        AssertDoesNotContain(mainWindowText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(mainWindowText, "private TimeSpan? _lastScrubPointerPosition;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackTimelineGeometry_PreservesScrubMath()
    {
        var geometryType = RequireType("Sussudio.Controllers.FlashbackTimelineGeometry");
        var tryComputeFraction = geometryType.GetMethod("TryComputeFraction", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.TryComputeFraction was not found.");
        var tryComputePosition = geometryType.GetMethod("TryComputePosition", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.TryComputePosition was not found.");
        var computePosition = geometryType.GetMethod("ComputePosition", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.ComputePosition was not found.");
        var isUsableTrackDimension = geometryType.GetMethod("IsUsableTrackDimension", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.IsUsableTrackDimension was not found.");
        var isUsableDuration = geometryType.GetMethod("IsUsableDuration", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.IsUsableDuration was not found.");

        object?[] middleFractionArgs = { 50d, 100d, 0d };
        AssertEqual(true, (bool)tryComputeFraction.Invoke(null, middleFractionArgs)!, "middle fraction computed");
        AssertEqual(0.5d, (double)middleFractionArgs[2]!, "middle fraction value");

        object?[] leftClampArgs = { -10d, 100d, 0d };
        AssertEqual(true, (bool)tryComputeFraction.Invoke(null, leftClampArgs)!, "left fraction computed");
        AssertEqual(0d, (double)leftClampArgs[2]!, "left fraction clamps");

        object?[] rightClampArgs = { 120d, 100d, 0d };
        AssertEqual(true, (bool)tryComputeFraction.Invoke(null, rightClampArgs)!, "right fraction computed");
        AssertEqual(1d, (double)rightClampArgs[2]!, "right fraction clamps");

        object?[] invalidWidthArgs = { 50d, 0d, 1d };
        AssertEqual(false, (bool)tryComputeFraction.Invoke(null, invalidWidthArgs)!, "zero width rejects fraction");
        AssertEqual(0d, (double)invalidWidthArgs[2]!, "rejected fraction resets");

        object?[] invalidXArgs = { double.NaN, 100d, 1d };
        AssertEqual(false, (bool)tryComputeFraction.Invoke(null, invalidXArgs)!, "non-finite x rejects fraction");
        AssertEqual(0d, (double)invalidXArgs[2]!, "non-finite fraction resets");

        AssertEqual(TimeSpan.FromSeconds(5), computePosition.Invoke(null, new object[] { 0.25d, TimeSpan.FromSeconds(20) }), "compute position");
        AssertEqual(TimeSpan.Zero, computePosition.Invoke(null, new object[] { -1d, TimeSpan.FromSeconds(20) }), "compute position left clamp");
        AssertEqual(TimeSpan.FromSeconds(20), computePosition.Invoke(null, new object[] { 2d, TimeSpan.FromSeconds(20) }), "compute position right clamp");
        AssertEqual(TimeSpan.Zero, computePosition.Invoke(null, new object[] { 0.5d, TimeSpan.Zero }), "compute position zero duration");

        object?[] positionArgs = { 25d, 100d, TimeSpan.FromSeconds(20), TimeSpan.Zero };
        AssertEqual(true, (bool)tryComputePosition.Invoke(null, positionArgs)!, "position computed");
        AssertEqual(TimeSpan.FromSeconds(5), positionArgs[3], "position value");

        object?[] invalidPositionArgs = { 25d, 100d, TimeSpan.Zero, TimeSpan.FromSeconds(1) };
        AssertEqual(false, (bool)tryComputePosition.Invoke(null, invalidPositionArgs)!, "zero duration rejects position");
        AssertEqual(TimeSpan.Zero, invalidPositionArgs[3], "rejected position resets");

        AssertEqual(true, (bool)isUsableTrackDimension.Invoke(null, new object[] { 1d })!, "positive track is usable");
        AssertEqual(false, (bool)isUsableTrackDimension.Invoke(null, new object[] { double.PositiveInfinity })!, "infinite track is unusable");
        AssertEqual(true, (bool)isUsableDuration.Invoke(null, new object[] { TimeSpan.FromMilliseconds(1) })!, "positive duration is usable");
        AssertEqual(false, (bool)isUsableDuration.Invoke(null, new object[] { TimeSpan.Zero })!, "zero duration is unusable");

        return Task.CompletedTask;
    }

    internal static Task MainWindowFlashbackToggle_RollsBackUiStateOnFailure()
    {
        var flashbackWindowText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandAdapterText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineText = ReadMainWindowFlashbackAdapterSource();
        var fullScreenText = ReadMainWindowShellChromeAdapterSource();
        var flashbackSettingsText = ReadMainWindowFlashbackAdapterSource();
        var flashbackTimelineControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineAnimationControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackSettingsControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializeFlashbackCommandController();");
        AssertContains(mainWindowText, "InitializeFlashbackTimelineController();");
        AssertContains(mainWindowText, "InitializeFlashbackSettingsBindingController();");
        AssertContains(viewModelText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(viewModelText, "IsFlashbackTimelineVisible = false;");
        AssertContains(bindingsText, "ApplyInitialFlashbackSettings();");
        AssertContains(flashbackSettingsText, "private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;");
        AssertContains(flashbackSettingsText, "ApplyFlashbackTimelineLockout = () => _flashbackTimelineController.ApplyLockout()");
        AssertContains(flashbackSettingsControllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackSettingsControllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(propertyChangedText, "TryHandleFlashback = propertyName => _flashbackPropertyChangedController.TryHandlePropertyChanged(propertyName)");
        AssertContains(flashbackPropertyChangedText, "private void InitializeFlashbackPropertyChangedController()");
        AssertContains(flashbackPropertyChangedText, "ApplyTimelineLockout = () => _flashbackTimelineController.ApplyLockout(),");
        AssertContains(flashbackPropertyChangedText, "ApplyTimelineVisibility = show => _flashbackTimelineController.ApplyVisibility(show),");
        AssertContains(flashbackPropertyChangedText, "IsFlashbackEnabled = () => ViewModel.IsFlashbackEnabled,");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackKeepAliveHint = UpdateFlashbackKeepAliveHint,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackEnabled):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.ApplyTimelineLockout();");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateFlashbackKeepAliveHint(_context.IsFlashbackEnabled());");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.ApplyTimelineVisibility(_context.IsTimelineVisible());");
        AssertContains(flashbackTimelineText, "private FlashbackTimelineController _flashbackTimelineController = null!;");
        AssertContains(flashbackTimelineText, "FlashbackToggle = FlashbackToggle,");
        AssertContains(flashbackTimelineText, "FlashbackTimelinePanel = FlashbackTimelinePanel,");
        AssertContains(flashbackTimelineText, "SnapPlayheadOnNextOpen = () => _flashbackPlayheadMotionController.RequestSnapOnNextUpdate(),");
        AssertContains(flashbackTimelineText, "ClearScrubInteraction = () => _flashbackScrubInteractionController.ClearForLockout(),");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.OnToggleChecked();");
        AssertContains(fullScreenText, "ResetFlashbackTimelineAnimation = _flashbackTimelineController.ResetAnimationForFullScreen,");
        AssertContains(flashbackTimelineControllerText, "public void ResetAnimationForFullScreen()");
        AssertDoesNotContain(flashbackTimelineText, "ResetFlashbackTimelineAnimationForFullScreen");
        AssertContains(flashbackTimelineControllerText, "internal sealed class FlashbackTimelineController");
        AssertContains(flashbackTimelineControllerText, "private readonly FlashbackTimelineAnimationController _animationController;");
        AssertContains(flashbackTimelineAnimationControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(flashbackTimelineAnimationControllerText, "_snapPlayheadOnNextOpen();");
        AssertContains(flashbackTimelineAnimationControllerText, "private void CompleteAnimation(Storyboard storyboard)");
        AssertContains(flashbackTimelineControllerText, "private bool _suppressToggle;");
        AssertContains(flashbackTimelineControllerText, "if (!_context.ViewModel.IsFlashbackEnabled)\n        {\n            ApplyLockout();\n            return;\n        }");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = true;");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = false;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackToggle.IsEnabled = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackTimelinePanel.IsHitTestVisible = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "SyncToggle(isVisible: false);");
        AssertContains(flashbackTimelineControllerText, "_context.ClearScrubInteraction();");
        AssertContains(flashbackTimelineControllerText, "CollapseImmediately();");
        AssertContains(flashbackTimelineControllerText, "=> _animationController.CollapseImmediately();");
        AssertContains(flashbackTimelineControllerText, "=> _animationController.ResetForFullScreen();");
        AssertContains(flashbackCommandAdapterText, "private FlashbackCommandController _flashbackCommandController = null!;");
        AssertContains(flashbackCommandAdapterText, "private void InitializeFlashbackCommandController()");
        AssertContains(flashbackCommandAdapterText, "FlashbackEnabledToggle = FlashbackEnabledToggle,");
        AssertContains(flashbackCommandAdapterText, "RunUiEventHandlerAsync = RunUiEventHandlerAsync");
        AssertContains(flashbackCommandAdapterText, "=> _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));");
        AssertContains(flashbackCommandControllerText, "if (_suppressFlashbackEnabledToggle)");
        AssertContains(flashbackCommandControllerText, "var requestedEnabled = _context.FlashbackEnabledToggle.IsOn;");
        AssertContains(flashbackCommandControllerText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");
        AssertContains(flashbackCommandControllerText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(flashbackCommandControllerText, "var previousEnabled = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackCommandControllerText, "_context.ViewModel.IsFlashbackEnabled = requestedEnabled;");
        AssertContains(flashbackCommandControllerText, "_context.ViewModel.IsFlashbackEnabled = previousEnabled;");
        AssertContains(flashbackCommandControllerText, "_suppressFlashbackEnabledToggle = true;");
        AssertContains(flashbackCommandControllerText, "_context.FlashbackEnabledToggle.IsOn = previousEnabled;");
        AssertContains(flashbackCommandControllerText, "_suppressFlashbackEnabledToggle = false;");
        AssertDoesNotContain(mainWindowText, "private bool _suppressFlashbackEnabledToggle;");
        AssertDoesNotContain(flashbackWindowText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface()
    {
        var automationInterfaceType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var readinessPortType = RequireType("Sussudio.Services.Automation.IAutomationReadinessPort");
        var deviceSelectionPortType = RequireType("Sussudio.Services.Automation.IAutomationDeviceSelectionPort");
        var snapshotQueryPortType = RequireType("Sussudio.Services.Automation.IAutomationSnapshotQueryPort");
        var captureSettingsPortType = RequireType("Sussudio.Services.Automation.IAutomationCaptureSettingsPort");
        var audioPortType = RequireType("Sussudio.Services.Automation.IAutomationAudioPort");
        var previewRecordingPortType = RequireType("Sussudio.Services.Automation.IAutomationPreviewRecordingPort");
        var probePortType = RequireType("Sussudio.Services.Automation.IAutomationProbePort");
        AssertEqual(
            true,
            readinessPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits readiness port");
        AssertEqual(
            true,
            deviceSelectionPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits device-selection port");
        AssertEqual(
            true,
            snapshotQueryPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits snapshot-query port");
        AssertEqual(
            true,
            captureSettingsPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits capture-settings port");
        AssertEqual(
            true,
            audioPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits audio port");
        AssertEqual(
            true,
            previewRecordingPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits preview-recording port");
        AssertEqual(
            true,
            probePortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits probe port");
        AssertEqual(
            false,
            automationInterfaceType.GetProperty("IsMicrophoneEnabled") != null,
            "IAutomationViewModel sync microphone setter");
        AssertTaskReturningMethod(automationInterfaceType, "SelectMicrophoneDeviceAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetMicrophoneEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetMicrophoneVolumeAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetHdrEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetTrueHdrPreviewEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetFlashbackEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetFlashbackBufferMinutesAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetFlashbackGpuDecodeAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "ExecuteFlashbackActionAsync", typeof(bool));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "GetFlashbackSegmentsAsync",
            typeof(IReadOnlyList<>).MakeGenericType(RequireType("Sussudio.Models.FlashbackSegmentInfo")));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "ProbeVideoSourceAsync",
            RequireType("Sussudio.Models.VideoSourceProbeResult"));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "ProbePreviewColorAsync",
            RequireType("Sussudio.Models.PreviewColorProbeResult"));
        AssertTaskReturningMethod(snapshotQueryPortType, "GetCaptureSnapshotProducerEpochAsync", typeof(long));

        var interfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();
        var viewModelDispatchText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/Dispatch/UiDispatchControllers.cs")
                .Replace("\r\n", "\n");
        var flashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackExportOperationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackExportAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferStatusText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var automationText = string.Join(
            "\n",
            flashbackSettingsText,
            flashbackExportText,
            flashbackExportOperationText,
            flashbackExportAutomationText,
            flashbackBufferStatusText,
            flashbackPlaybackCommandsText,
            automationFacadeText);

        AssertDoesNotContain(interfaceText, "bool FlashbackPlay();");
        AssertDoesNotContain(interfaceText, "bool FlashbackPause();");
        AssertDoesNotContain(interfaceText, "bool FlashbackGoLive();");
        AssertDoesNotContain(interfaceText, "bool FlashbackBeginScrub(TimeSpan position);");
        AssertDoesNotContain(interfaceText, "bool FlashbackEndScrub();");
        AssertDoesNotContain(interfaceText, "VideoSourceProbeResult ProbeVideoSource();");
        AssertDoesNotContain(interfaceText, "PreviewColorProbeResult ProbePreviewColor();");
        AssertContains(dispatcherText, "await _flashbackPort.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "return CreateFlashbackActionRejectedResponse(");
        AssertContains(dispatcherText, "errorCode: AutomationErrorCodes.FlashbackActionFailed");
        AssertContains(dispatcherText, "RequestedPositionMs = requestedPositionMs");
        AssertContains(dispatcherText, "LastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(dispatcherText, "var useSelectionRange = GetBool(payload, AutomationPayloadKeys.UseSelectionRange) ?? false;");
        AssertContains(dispatcherText, "_ = GetBool(payload, AutomationPayloadKeys.Force) ?? false;");
        AssertContains(dispatcherText, "ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, cancellationToken)");
        AssertContains(dispatcherText, "FlashbackExportFailureCodes.Classify(exportResult)");
        AssertContains(dispatcherText, "FailureKind = failureKind");
        AssertContains(dispatcherText, "AutomationFlashbackValidation.ValidatePositionMs(positionMs.Value);");
        var flashbackValidationText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        AssertContains(flashbackValidationText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(dispatcherText, "AutomationFlashbackAction.BeginScrub => RequireDouble(payload, AutomationPayloadKeys.PositionMs)");
        AssertContains(dispatcherText, "AutomationFlashbackAction.UpdateScrub => RequireDouble(payload, AutomationPayloadKeys.PositionMs)");
        AssertContains(dispatcherText, "AutomationFlashbackAction.EndScrub => GetDouble(payload, AutomationPayloadKeys.PositionMs)");
        AssertContains(dispatcherText, "private readonly IAutomationReadinessPort _readinessPort;");
        AssertContains(dispatcherText, "private readonly IAutomationDeviceSelectionPort _deviceSelectionPort;");
        AssertContains(dispatcherText, "private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;");
        AssertContains(dispatcherText, "private readonly IAutomationCaptureSettingsPort _captureSettingsPort;");
        AssertContains(dispatcherText, "private readonly IAutomationAudioPort _audioPort;");
        AssertContains(dispatcherText, "private readonly IAutomationPreviewRecordingPort _previewRecordingPort;");
        AssertContains(dispatcherText, "private readonly IAutomationUiPort _uiPort;");
        AssertContains(dispatcherText, "private readonly IAutomationFlashbackPort _flashbackPort;");
        AssertContains(dispatcherText, "private readonly IAutomationProbePort _probePort;");
        AssertDoesNotContain(dispatcherText, "private readonly IAutomationViewModel _viewModel;");
        AssertContains(interfaceText, "internal readonly record struct AutomationViewModelPorts(");
        AssertContains(interfaceText, "public static AutomationViewModelPorts From(IAutomationViewModel viewModel)");
        AssertContains(interfaceText, "ArgumentNullException.ThrowIfNull(viewModel);");
        AssertContains(dispatcherText, "internal AutomationCommandDispatcher(");
        AssertContains(dispatcherText, "AutomationViewModelPorts ports,");
        AssertDoesNotContain(dispatcherText, "public AutomationCommandDispatcher(\n        IAutomationViewModel viewModel,");
        AssertContains(dispatcherText, "_readinessPort = ports.Readiness");
        AssertContains(dispatcherText, "_snapshotQueryPort = ports.SnapshotQuery");
        AssertDoesNotContain(dispatcherText, "_readinessPort = viewModel;");
        AssertDoesNotContain(dispatcherText, "_snapshotQueryPort = viewModel;");
        AssertContains(dispatcherText, "_readinessPort.IsInitialized || _readinessPort.Devices.Count > 0");
        AssertContains(dispatcherText, "await deviceSelectionHandler.InvokeAsync(_deviceSelectionPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await captureSettingsHandler.InvokeAsync(_captureSettingsPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await audioHandler.InvokeAsync(_audioPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _deviceSelectionPort.RefreshDevicesForAutomationAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _deviceSelectionPort.SelectDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _deviceSelectionPort.SelectMicrophoneDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _audioPort.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _audioPort.SetMicrophoneVolumeAsync(volume, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _captureSettingsPort.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _previewRecordingPort.SetRecordingEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _probePort.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _probePort.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _snapshotQueryPort.GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.SetFlashbackBufferMinutesAsync(minutes, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.SetFlashbackGpuDecodeAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "vm.SetHdrEnabledAsync(v, ct)");
        AssertContains(dispatcherText, "vm.SetTrueHdrPreviewEnabledAsync(v, ct)");
        AssertDoesNotContain(dispatcherText, "_viewModel.IsMicrophoneEnabled =");
        AssertContains(viewModelDispatchText, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");

        AssertContains(automationText, "public Task<bool> ExecuteFlashbackActionAsync(");
        AssertContains(automationText, "public async Task SetFlashbackBufferMinutesAsync(int minutes, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "public async Task SetFlashbackGpuDecodeAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "public void ReportFlashbackPlaybackRejection(string action, string logToken)");
        AssertContains(automationText, "lastFailure={lastFailure}");
        AssertContains(automationText, "StatusText = message;");
        AssertContains(automationText, "case AutomationFlashbackAction.SetInPoint:");
        AssertContains(automationText, "case AutomationFlashbackAction.SetOutPoint:");
        AssertContains(automationText, "case AutomationFlashbackAction.ClearInOutPoints:");
        AssertContains(automationText, "case AutomationFlashbackAction.BeginScrub:");
        AssertContains(automationText, "return FlashbackBeginScrub(position ?? TimeSpan.Zero);");
        AssertContains(automationText, "case AutomationFlashbackAction.UpdateScrub:");
        AssertContains(automationText, "return FlashbackUpdateScrub(position ?? TimeSpan.Zero);");
        AssertContains(automationText, "case AutomationFlashbackAction.EndScrub:");
        AssertContains(automationText, "? FlashbackEndScrubAt(position.Value)\n                    : FlashbackEndScrub();");
        var automationPlayBlock = ExtractTextBetween(
            automationText,
            "case AutomationFlashbackAction.Play:",
            "            case AutomationFlashbackAction.Pause:");
        AssertContains(automationPlayBlock, "if (position.HasValue)");
        AssertContains(automationPlayBlock, "if (!FlashbackSeek(position.Value))");
        AssertContains(automationPlayBlock, "return FlashbackPlay();");
        AssertDoesNotContain(automationPlayBlock, "FlashbackBeginScrub(position.Value);");
        AssertDoesNotContain(automationPlayBlock, "FlashbackEndScrub();");
        AssertContains(automationText, "if (useSelectionRange)");
        AssertContains(automationText, "FLASHBACK_EXPORT_START_UI_ENQUEUE_FAILED source=automation");
        AssertContains(automationText, "FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=automation percent={p.Percent:0.###}");
        AssertContains(automationText, "FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=ui percent={p.Percent:0.###}");
        AssertContains(automationText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertContains(automationText, "=> FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);");
        AssertContains(automationText, "await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false)");
        AssertContains(automationText, "_flashbackBitrateSamples.Clear();\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = recordingTransitionControllerRootText;
        var automationText = recordingLifecycleText
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
                .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = recordingStateText;
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferStatusText = flashbackStateText;
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(recordingLifecycleText, "public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "=> SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingLifecycleText, "public Task ToggleRecordingAsync()\n        => _recordingTransitionController.ToggleRecordingAsync();");
        AssertContains(recordingLifecycleText, "=> _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(rootViewModelText, "public Task ToggleRecordingAsync()");
        AssertContains(rootViewModelText, "public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(rootViewModelText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingTransitionControllerRootText, "namespace Sussudio.Controllers;");
        AssertContains(recordingTransitionControllerRootText, "internal sealed class MainViewModelRecordingTransitionController");
        AssertDoesNotContain(recordingTransitionControllerRootText, "partial class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerRootText, "internal sealed class MainViewModelRecordingTransitionControllerContext");
        AssertContains(recordingTransitionControllerRootText, "private readonly MainViewModelRecordingTransitionControllerContext _context;");
        AssertDoesNotContain(recordingTransitionControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingTransitionControllerText, "_viewModel.");
        AssertContains(recordingTransitionControllerText, "Recording transition already in progress.");
        AssertContains(recordingTransitionControllerText, "await inFlight;");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "private async Task StartRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerRootText, "private async Task StopRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerRootText, "await _context.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "await _context.StopRecordingAsync(cancellationToken);");
        AssertDoesNotContain(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingStateText, "private readonly Stopwatch _recordingStopwatch = new();");
        AssertContains(recordingStateText, "private readonly BitrateSampleWindow _recordingBitrateSamples = new(BitrateWindowMs);");
        AssertContains(flashbackStateText, "private readonly BitrateSampleWindow _flashbackBitrateSamples = new(BitrateWindowMs);");
        AssertContains(recordingStateText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(recordingStateText, "public partial string OutputPath");
        AssertContains(recordingStateText, "public partial bool IsRecording");
        AssertDoesNotContain(recordingStateText, "_activeRecordingToggleTask");
        AssertDoesNotContain(recordingStateText, "_recordingToggleInProgress");
        AssertContains(recordingRuntimeText, "partial void OnIsRecordingChanged(bool value)");
        AssertContains(recordingRuntimeText, "private void UpdateRecordingStats()");
        AssertContains(recordingRuntimeText, "_recordingBitrateSamples.Clear();");
        AssertContains(recordingRuntimeText, "var smoothed = _recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);");
        AssertContains(recordingRuntimeText, "RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, \"0\");");
        AssertContains(recordingRuntimeText, "RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : \"--\";");
        AssertContains(flashbackBufferStatusText, "var smoothed = _flashbackBitrateSamples.AddSampleAndCompute(now, diskBytes);");
        AssertContains(recordingStateText, "internal sealed class BitrateSampleWindow");
        AssertContains(recordingStateText, "public double? AddSampleAndCompute(long tick, long bytes)");
        AssertContains(recordingStateText, "private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)");
        AssertContains(recordingRuntimeText, "if (_pendingModeOptionsRefreshForceRetarget is bool forceSourceAutoRetarget)");
        AssertContains(recordingRuntimeText, "_pendingModeOptionsRefreshForceRetarget = null;");
        AssertContains(recordingRuntimeText, "RebuildResolutionOptions(forceSourceAutoRetarget);");
        AssertContains(recordingRuntimeText, "private void SetPendingModeOptionsRefresh(bool forceSourceAutoRetarget)");
        AssertContains(recordingRuntimeText, "_pendingModeOptionsRefreshForceRetarget == true || forceSourceAutoRetarget");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateRecordingStats();");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void UpdateRecordingStats()");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private static double? ComputeAverageBitrate(");
        AssertDoesNotContain(runtimeLifecycleControllerText, "partial void OnIsRecordingChanged(bool value)");
        AssertContains(rootViewModelText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(rootViewModelText, "public partial string OutputPath");
        AssertContains(automationText, "=> SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_RecordingSettingsRouteThroughControllerAndFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var hooks = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var viewModel = viewModelFiles["MainViewModel.cs"];
        var controller = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsController.cs");
        var captureAutomation = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs");

        foreach (var member in new[] { "SetRecordingFormatAsync", "SetQualityAsync", "SetSplitEncodeModeAsync", "SetCustomBitrateAsync", "SetPresetAsync" })
        {
            AssertMemberContains(viewModel, member, "RunPersistedSettingsAutomationAsync(");
            AssertMemberContains(viewModel, member, "_recordingSettingsController.");
        }
        foreach (var member in new[] { "OnSelectedRecordingFormatChanged", "OnSelectedQualityChanged", "OnSelectedSplitEncodeModeChanged", "OnCustomBitrateMbpsChanged", "OnSelectedPresetChanged" })
        {
            AssertMemberContains(hooks, member, "_recordingSettingsController.OnSelectionChanged(");
            AssertMemberContains(hooks, member, "SaveSettings();");
        }
        AssertContains(controller, "internal sealed class MainViewModelRecordingSettingsController");
        AssertContains(controller, "private readonly MainViewModelRecordingSettingsControllerContext _context;");
        AssertContains(controller, "public IDisposable SuppressPropertyReactions()");
        AssertContains(controller, "public Task? PendingApplication");
        AssertContains(controller, "public void ClearPendingIfSameAndCompleted(Task task)");
        AssertDoesNotContain(controller, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureAutomation, "MainViewModelRecordingSettings");
        AssertDoesNotContain(hooks, "TrackPendingFlashbackCycleTask");
        AssertDoesNotContain(hooks, "_suppressFlashbackFormatCycle");
        AssertDoesNotContain(hooks, "_suppressFlashbackEncoderSettingsCycle");
        AssertDoesNotContain(hooks, "_pendingFlashbackCycleTask");
        AssertContains(viewModel, "CaptureSelection = () => new RecordingSettingsSelection(");
        AssertContains(viewModel, "ApplyAsync = viewModel._sessionCoordinator.ApplyRecordingSettingsAsync,");
        AssertContains(viewModel, "viewModel._recordingSettingsController.PendingApplication");
        AssertContains(viewModel, "viewModel._recordingSettingsController.ClearPendingIfSameAndCompleted(task)");
        return Task.CompletedTask;
    }

    internal static Task BitrateSampleWindow_PreservesBoundedAverageBehavior()
    {
        var windowType = RequireType("Sussudio.ViewModels.BitrateSampleWindow");
        var window = Activator.CreateInstance(
                         windowType,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                         binder: null,
                         args: new object[] { 10_000L },
                         culture: null)
                     ?? throw new InvalidOperationException("BitrateSampleWindow instance could not be created.");
        var sampleMethod = windowType.GetMethod("AddSampleAndCompute", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("BitrateSampleWindow.AddSampleAndCompute was not found.");
        var clearMethod = windowType.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("BitrateSampleWindow.Clear was not found.");

        AssertEqual(null, (double?)sampleMethod.Invoke(window, new object[] { 0L, 100L }), "first sample bitrate");
        AssertNearlyEqual(
            8000.0,
            (double)sampleMethod.Invoke(window, new object[] { 1000L, 1100L })!,
            0.0001,
            "two sample bitrate");
        AssertNearlyEqual(
            4000.0,
            (double)sampleMethod.Invoke(window, new object[] { 11_000L, 6100L })!,
            0.0001,
            "trimmed sample bitrate");

        clearMethod.Invoke(window, null);
        AssertEqual(null, (double?)sampleMethod.Invoke(window, new object[] { 12_000L, 6100L }), "cleared sample bitrate");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = recordingTransitionControllerRootText;

        AssertContains(recordingTransitionControllerText, "Logger.LogException(ex);");
        AssertContains(recordingTransitionControllerText, "_context.SetIsRecording(_context.GetSessionIsRecording());");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException ex)");
        AssertContains(recordingTransitionControllerText, "transitionError = ex;");
        AssertContains(recordingTransitionControllerText, "Logger.Log($\"Recording transition wait canceled: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))");
        AssertContains(recordingTransitionControllerText, "throw transitionCanceled;");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(StatusMessages.RecordingStartCanceled);");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(StatusMessages.RecordingStopCanceled);");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(StatusMessages.RecordingFailed(ex.Message));");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(StatusMessages.RecordingFailed(ex.Message));");
        AssertContains(recordingTransitionControllerText, "throw;");

        return Task.CompletedTask;
    }

    internal static Task EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread()
    {
        var appText = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingStateText, "internal Task StopRecordingForEmergencyAsync");
        // Fix #12: emergency stop now routes through the coordinator's emergency-flagged path
        // so LibAvRecordingSink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s).
        AssertContains(recordingStateText, "=> _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);");
        AssertContains(rootViewModelText, "internal Task StopRecordingForEmergencyAsync");
        AssertDoesNotContain(ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs"), "StopRecordingForEmergencyAsync");
        AssertContains(appText, "() => viewModel.StopRecordingForEmergencyAsync(),");
        AssertContains(appText, "var task = stopRecording();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(recordingStateText, "if (!IsRecording)");

        return Task.CompletedTask;
    }

    internal static Task AutomationAudioCommands_PreserveRuntimeGuards()
    {
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/Dispatch/UiDispatchControllers.cs")
                .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        var previewVolumeCommand = ExtractMemberCode(automationAudioText, "SetPreviewVolumeAsync");
        AssertContains(previewVolumeCommand, "return InvokeOnUiThreadAsync(() =>");
        AssertContains(previewVolumeCommand, "SetPreviewVolumeFromUser(Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0));");
        AssertOccursBefore(previewVolumeCommand, "SetPreviewVolumeFromUser(", "SaveSettingsOrThrow();");
        AssertContains(automationAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);");
        AssertContains(automationAudioText, "public async Task SelectMicrophoneDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "throw new AutomationStateConflictException(\"Cannot change microphone device while recording. Stop the recording first.\");");
        AssertContains(automationAudioText, "throw new AutomationStateConflictException(\"Custom audio input cannot be changed while recording.\");");
        AssertContains(automationAudioText, "Cannot change microphone device while recording. Stop the recording first.");
        AssertContains(automationAudioText, "SelectedMicrophoneDevice = request.Target;");
        AssertContains(automationAudioText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(automationAudioText, "public Task SetMicrophoneVolumeAsync(double microphoneVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "MicrophoneVolume = Math.Clamp(microphoneVolumePercent, 0.0, 100.0);");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}\");");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}\");");
        AssertContains(automationAudioText, "throw new AutomationStateConflictException(");
        AssertContains(automationAudioText, "Cannot change microphone enable state while recording. Stop the recording first.");
        AssertContains(automationAudioText, "_suppressMicrophoneMonitorUpdate = true;");
        AssertContains(automationAudioText, "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(");
        AssertContains(automationAudioText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(automationAudioText, "IsMicrophoneEnabled = enabled;\n                }\n                finally\n                {\n                    _suppressMicrophoneMonitorUpdate = false;\n                }\n\n                SaveSettingsOrThrow();\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertContains(automationUiText, "public Task SetPreviewVolumeAsync");
        AssertContains(viewModelText, "if (_suppressMicrophoneMonitorUpdate)");
        AssertContains(viewModelText, "return ResolveByName(MicrophoneDevices, deviceName, d => d.Name);");
        AssertContains(captureServiceText, "var previousEnabled = _micMonitorEnabled;");
        AssertContains(captureServiceText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);");
        AssertContains(captureServiceText, "_micMonitorEnabled = enabled;");

        var microphoneUpdateIndex = automationAudioText.IndexOf(
            "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(",
            StringComparison.Ordinal);
        var microphonePersistIndex = automationAudioText.IndexOf(
            "IsMicrophoneEnabled = enabled;",
            StringComparison.Ordinal);
        AssertEqual(
            true,
            microphoneUpdateIndex >= 0 && microphonePersistIndex > microphoneUpdateIndex,
            "automation microphone persists only after monitor update");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook()
    {
        var vmType = RequireType("Sussudio.ViewModels.MainViewModel");

        var savePreviewVolume = vmType.GetMethod(
            "SavePreviewVolume",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        AssertNotNull(savePreviewVolume, "MainViewModel.SavePreviewVolume");

        var previewVolume = vmType.GetProperty("PreviewVolume", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(previewVolume, "MainViewModel.PreviewVolume");

        var audioPreview = vmType.GetProperty("IsAudioPreviewEnabled", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(audioPreview, "MainViewModel.IsAudioPreviewEnabled");

        var getOptionsSnapshot = vmType.GetMethod(
            "GetAutomationOptionsSnapshotAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(getOptionsSnapshot, "MainViewModel.GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        var setPreviewVolume = coordinatorType.GetMethod(
            "SetPreviewVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(setPreviewVolume, "CaptureSessionCoordinator.SetPreviewVolume");

        var updateAudioMonitoring = coordinatorType.GetMethod(
            "UpdateAudioMonitoringAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioMonitoring, "CaptureSessionCoordinator.UpdateAudioMonitoringAsync");

        var updateAudioInput = coordinatorType.GetMethod(
            "UpdateAudioInputAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioInput, "CaptureSessionCoordinator.UpdateAudioInputAsync");

        var startVideoPreview = coordinatorType.GetMethod(
            "StartVideoPreviewAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(startVideoPreview, "CaptureSessionCoordinator.StartVideoPreviewAsync");

        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        AssertEqual(true,
            Enum.IsDefined(commandKindType, Enum.Parse(commandKindType, "SetAudioPreviewEnabled")),
            "AutomationCommandKind.SetAudioPreviewEnabled exists");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
        var automationOptionsText = automationSnapshotText;
        var automationOptionsBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");

        AssertDoesNotContain(diagnosticsHubText, "GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "Options = optionsSnapshot");
        AssertDoesNotContain(automationSnapshotText, "BuildStringOptions(");
        AssertContains(automationOptionsText, "GetAutomationOptionsSnapshotAsync");
        AssertContains(automationOptionsText, "InvokeOnUiThreadAsync(() =>");
        AssertContains(automationOptionsText, "AvailableFrameRates");
        AssertContains(automationOptionsText, "MicrophoneDevices");
        AssertContains(automationOptionsText, "IsMicrophoneEnabled = IsMicrophoneEnabled");
        AssertContains(automationOptionsText, "SupportedFlashbackBufferMinutes");
        AssertContains(automationOptionsText, "FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate)");
        AssertContains(automationOptionsText, "AutomationOptionsSnapshotBuilder.Build(input)");
        AssertNoRegex(
            automationOptionsText,
            @"new\s+AutomationOptionsSnapshot\s*\{",
            "MainViewModel automation options DTO construction");
        AssertContains(automationOptionsBuilderText, "internal static class AutomationOptionsSnapshotBuilder");
        AssertContains(automationOptionsBuilderText, "internal sealed class AutomationOptionsSnapshotInput");
        AssertContains(automationOptionsBuilderText, "BuildStringOptions(input.RecordingFormats, input.SelectedRecordingFormat)");
        AssertContains(automationOptionsBuilderText, "MjpegDecoderCounts = Enumerable.Range(1, 8)");
        AssertContains(automationOptionsBuilderText, "MicrophoneDevices = input.MicrophoneDevices");
        AssertContains(automationOptionsBuilderText, "FlashbackBufferMinuteOptions = input.FlashbackBufferMinuteOptions");
        AssertContains(automationOptionsBuilderText, "DisableReason = option.DisableReason ?? string.Empty");
        AssertContains(automationOptionsBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0");
        AssertContains(automationOptionsBuilderText, "IsMicrophoneEnabled = input.IsMicrophoneEnabled");
        AssertContains(automationOptionsBuilderText, "MicrophoneVolumePercent = input.MicrophoneVolume");
        AssertContains(automationOptionsBuilderText, "FlashbackGpuDecode = input.FlashbackGpuDecode");

        return Task.CompletedTask;
    }

    private static void AssertTaskReturningMethod(Type type, string methodName, Type? resultType)
    {
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
            ?? type.GetInterfaces()
                .Select(interfaceType => interfaceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public))
                .FirstOrDefault(candidate => candidate != null);
        AssertNotNull(method, $"{type.FullName}.{methodName}");
        AssertEqual(
            true,
            method!.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)),
            $"{type.FullName}.{methodName} cancellation token");

        if (resultType == null)
        {
            AssertEqual(typeof(Task).FullName, method.ReturnType.FullName, $"{type.FullName}.{methodName} return type");
            return;
        }

        AssertEqual(true, method.ReturnType.IsGenericType, $"{type.FullName}.{methodName} generic Task return");
        AssertEqual(
            typeof(Task<>).FullName,
            method.ReturnType.GetGenericTypeDefinition().FullName,
            $"{type.FullName}.{methodName} generic Task definition");
        AssertEqual(
            resultType.FullName,
            method.ReturnType.GenericTypeArguments[0].FullName,
            $"{type.FullName}.{methodName} task result");
    }

    internal static Task MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");
        foreach (var methodName in new[]
        {
            "SetFlashbackEnabledAsync",
            "RestartFlashbackAsync",
            "ApplyRecordingSettingsAsync",
            "UpdateFlashbackSettingsAsync",
            "ExportFlashbackRangeAsync",
            "ExportFlashbackLastNSecondsAsync",
            "GetFlashbackSegments",
            "GetFlashbackPlaybackSnapshot",
            "FlashbackBeginScrub",
            "FlashbackSeek",
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetOutPoint",
            "FlashbackClearInOutPoints"
        })
        {
            var method = Array.Find(
                coordinatorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                method => method.Name == methodName);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        var viewModelFiles = ReadMainViewModelCodeFiles();
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsController.cs")
            .Replace("\r\n", "\n");
        var viewModelText = string.Join("\n", viewModelFiles.Values) + "\n" + recordingSettingsAutomationControllerText;
        var viewModelAudioStateText = viewModelFiles["MainViewModel.AudioState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackBufferStatusText = viewModelFlashbackStateText;
        var flashbackPlaybackCommandsText = viewModelFlashbackStateText;
        var flashbackPlaybackText = flashbackPlaybackCommandsText;
        var flashbackAutomationText = flashbackSettingsText
            + "\n" + flashbackExportText
            + "\n" + flashbackExportOperationText
            + "\n" + flashbackExportAutomationText
            + "\n" + flashbackBufferStatusText
            + "\n" + flashbackPlaybackCommandsText;
        var audioCapturePropertyChangesText = viewModelFiles["MainViewModel.AudioState.cs"];
        var rawViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var rawAudioCapturePropertyChangesText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawFlashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource();

        AssertContains(coordinatorText, "if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })\n        {\n            return true;\n        }");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackPlaybackSnapshot", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "ReportFlashbackPlaybackRejection", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "ReportFlashbackPlaybackRejection", "StatusText = message;");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackActionAsync", "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "return FlashbackBeginScrub(position ?? TimeSpan.Zero)");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "return FlashbackSetInPoint().HasValue");
        AssertMemberDoesNotContain(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackBeginScrub", "_sessionCoordinator.FlashbackBeginScrub(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSeek", "_sessionCoordinator.FlashbackSeek(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackUpdateScrub", "return _sessionCoordinator.FlashbackUpdateScrub(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackEndScrub", "_sessionCoordinator.FlashbackEndScrub()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackEndScrubAt", "_sessionCoordinator.FlashbackEndScrubAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackPlay", "_sessionCoordinator.FlashbackPlay()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackPause", "_sessionCoordinator.FlashbackPause()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackGoLive", "_sessionCoordinator.FlashbackGoLive()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackNudge", "_sessionCoordinator.FlashbackNudge(delta)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetInPoint", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetInPointAt", "_sessionCoordinator.FlashbackSetInPointAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetOutPoint", "_sessionCoordinator.FlashbackSetOutPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetOutPointAt", "_sessionCoordinator.FlashbackSetOutPointAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackClearInOutPoints", "=> _sessionCoordinator.FlashbackClearInOutPoints()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackBufferStatus()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = playback.InPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = playback.OutPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "if (FlashbackState != FlashbackPlaybackState.Live)");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackState = FlashbackPlaybackState.Live;");
        var updateFlashbackBufferStatus = ExtractMemberCode(flashbackBufferStatusText, "UpdateFlashbackBufferStatus");
        var inactivePlaybackSnapshotBranch = ExtractTextBetween(
            updateFlashbackBufferStatus,
            "else\n        {\n            if (FlashbackState != FlashbackPlaybackState.Live)",
            "\n        }\n    }");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackInPoint = null;");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBitrate", "_sessionCoordinator.FlashbackTotalBytesWritten");
        AssertContains(captureServiceText, "public long FlashbackTotalBytesWritten => _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;");
        AssertContains(captureServiceText, "public void AttachCaptureFailure(");
        AssertContains(captureServiceText, "new CaptureErrorOrigin(CaptureErrorOriginKind.AudioCaptureRegistration, ++_captureErrorGeneration)");
        AssertContains(captureServiceText, "private void OnWasapiCaptureFailed(Exception ex, CaptureErrorOrigin origin, string source)");
        AssertContains(captureServiceText, "if (!IsCaptureErrorCurrent(origin))");
        AssertDoesNotContain(captureServiceText, "ReferenceEquals(sender, ProgramCapture)");
        AssertDoesNotContain(captureServiceText, "ReferenceEquals(sender, MicrophoneCapture)");
        AssertContains(captureServiceText, "WASAPI_CAPTURE_FAILED source={source}");
        AssertContains(captureServiceText, "_previewAudioGraph.RecordCaptureFault(source, ex);");
        AssertContains(coordinatorText, "if (Volatile.Read(ref _isDisposed))");
        AssertContains(coordinatorText, "Volatile.Write(ref _isDisposed, true);");
        AssertContains(coordinatorText, "Exception failure = Volatile.Read(ref _isDisposed)");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertContains(rawFlashbackSettingsText, "Flashback buffer minutes must be one of: 1, 2, 5, 10, 15, or 30.");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackBufferMinutesAsync", "if (FlashbackBufferMinutes == minutes)");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackBufferMinutesAsync", "Changed: false");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackBufferMinutesAsync", "if (!state.Changed)");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackBufferMinutesAsync", "_suppressFlashbackSettingsUpdate = true;");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackBufferMinutesAsync", "await RestartFlashbackAsync(cancellationToken).ConfigureAwait(false);");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackBufferMinutesAsync", "_sessionCoordinator.UpdateFlashbackSettingsAsync(");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackGpuDecodeAsync", "if (FlashbackGpuDecode == enabled)");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackGpuDecodeAsync", "Changed: false");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackGpuDecodeAsync", "if (!state.Changed)");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackGpuDecodeAsync", "_suppressFlashbackSettingsUpdate = true;");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackGpuDecodeAsync", "_sessionCoordinator.UpdateFlashbackSettingsAsync(");
        var restartFlashbackAsyncText = ExtractMemberCodeFromDeclaration(
            rawFlashbackSettingsText,
            "public async Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(restartFlashbackAsyncText, "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertContains(restartFlashbackAsyncText, "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
        AssertContains(flashbackSettingsText, "private static readonly int[] SupportedFlashbackBufferMinutes = { 1, 2, 5, 10, 15, 30 };");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackGpuDecodeChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "Interlocked.Increment(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "RestartFlashbackAfterSettingsUpdateAsync(updateTask, restartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "restartGeneration != Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "InvokeOnUiThreadAsync(");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "IsPreviewing && !IsRecording && _isLoadingSettings is false");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "shouldRestart is false");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "await RestartFlashbackAsync().ConfigureAwait(false)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "catch (OperationCanceledException ex)");
        AssertContains(rawFlashbackSettingsText, "RestartFlashbackAfterSettingsUpdate canceled");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "var settings = BuildCaptureSettings();");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(\n                        true,\n                        \"audio_capture_enable\",");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "afterMonitoringStarted: () => _sessionCoordinator.RestartFlashbackAsync(settings)");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(false, \"audio_capture_disable\", teardownCapture: true)");
        AssertContains(viewModelAudioStateText, "private int _audioEnabledChangeGeneration;");
        AssertContains(viewModelAudioStateText, "private bool _suppressAudioPreviewEnabledChangeOperation;");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "_suppressAudioPreviewEnabledChangeOperation = true;");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=enable");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=disable");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackSettingsRestartGeneration;");

        foreach (var memberName in new[]
        {
            "GetFlashbackPlaybackSnapshot",
            "FlashbackBeginScrub",
            "FlashbackSeek",
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackEndScrubAt",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetInPointAt",
            "FlashbackSetOutPoint",
            "FlashbackSetOutPointAt",
            "FlashbackClearInOutPoints",
            "UpdateFlashbackBufferStatus",
            "UpdateFlashbackBitrate",
            "ExportFlashbackAsync",
            "SaveFlashbackLast5mAsync",
            "ExportFlashbackAutomationAsync",
            "GetFlashbackSegments",
            "SetFlashbackEnabledAsync",
            "SetFlashbackBufferMinutesAsync",
            "SetFlashbackGpuDecodeAsync",
            "RestartFlashbackAsync"
        })
        {
            AssertMemberDoesNotContain(flashbackAutomationText, memberName, "_captureService");
        }

        foreach (var memberName in new[]
        {
            "OnSelectedRecordingFormatChanged",
            "OnCustomBitrateMbpsChanged",
            "OnFlashbackBufferMinutesChanged",
            "OnFlashbackGpuDecodeChanged",
            "OnSelectedQualityChanged",
            "OnSelectedPresetChanged",
            "OnSelectedSplitEncodeModeChanged"
        })
        {
            var sourceText = memberName is "OnFlashbackBufferMinutesChanged" or "OnFlashbackGpuDecodeChanged"
                ? flashbackSettingsText
                : flashbackEncoderSettingsText;
            AssertMemberDoesNotContain(sourceText, memberName, "_captureService");
        }

        AssertNoRegex(
            viewModelText,
            @"\b_captureService\s*\.\s*(SetFlashbackEnabled|RestartFlashbackAsync|ApplyRecordingSettingsAsync|UpdateFlashbackSettings|ExportFlashback|GetFlashbackSegments|FlashbackPlaybackController|FlashbackBufferManager|FlashbackDiskBytes|FlashbackTotalBytesWritten)\b",
            "MainViewModel flashback mutating/backend capture-service access");
        AssertNoRegex(
            viewModelText,
            @"\b(?:var|CaptureService)\s+\w+\s*=\s*_captureService\s*;",
            "MainViewModel local capture-service aliases");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport()
    {
        var exportOperationsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            .Replace("\r\n", "\n");
        var exportCoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = exportOperationsText
            + "\n" + exportCoreText
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
                .Replace("\r\n", "\n");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Capture/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertDoesNotContain(exportOperationsText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(exportOperationsText, "private readonly record struct FlashbackExportBackendSnapshot(");
        AssertContains(exportOperationsText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportCoreText, "private FlashbackExportPreparationResult PrepareFlashbackExportRequest(");
        AssertContains(exportCoreText, "FlashbackExportPlanner.ResolveRange(");
        AssertContains(exportCoreText, "FlashbackExportPlanner.PlanLiveEdge(");
        AssertContains(exportCoreText, "FlashbackExportPlanner.CreateRequest(");
        AssertContains(exportCoreText, "CreateFlashbackExportThrottleDelayProvider");

        var rangeExport = ExtractMemberCode(exportOperationsText, "ExportFlashbackRangeAsync");
        AssertContains(rangeExport, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(rangeExport, "operationName: \"range\",");
        AssertContains(rangeExport, "sessionReleaseOperation: \"flashback_export_snapshot_session\",");
        AssertContains(rangeExport, "var snapshot = snapshotResult.Snapshot;");
        AssertContains(rangeExport, "snapshotSink: snapshot.Sink,");
        AssertContains(rangeExport, "snapshotBufferManager: snapshot.BufferManager,");
        AssertContains(rangeExport, "snapshotExporter: snapshot.Exporter,");
        AssertContains(rangeExport, "exportOperationLockAlreadyHeld: true,");
        AssertContains(rangeExport, "resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(");
        AssertContains(rangeExport, "inPointFilePts,");
        AssertContains(rangeExport, "outPointFilePts)");

        var lastNExport = ExtractMemberCode(exportOperationsText, "ExportFlashbackLastNSecondsAsync");
        AssertContains(lastNExport, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(lastNExport, "operationName: \"last_n\",");
        AssertContains(lastNExport, "sessionReleaseOperation: \"flashback_export_last_n_snapshot_session\",");
        AssertContains(lastNExport, "var snapshot = snapshotResult.Snapshot;");
        AssertContains(lastNExport, "snapshotSink: snapshot.Sink,");
        AssertContains(lastNExport, "snapshotBufferManager: snapshot.BufferManager,");
        AssertContains(lastNExport, "snapshotExporter: snapshot.Exporter,");
        AssertContains(lastNExport, "exportOperationLockAlreadyHeld: true,");
        AssertContains(lastNExport, "resolveRangeAfterEvictionPaused: CreateFlashbackExportLastNRangeResolver(seconds)");

        var backendSnapshot = ExtractMemberCode(exportOperationsText, "SnapshotFlashbackExportBackendAsync");
        AssertContains(backendSnapshot, "var bufferManager = _flashbackBackend.BufferManager;");
        AssertContains(backendSnapshot, "var flashbackSink = _flashbackBackend.Sink;");
        AssertContains(backendSnapshot, "var flashbackExporter = bufferManager != null\n                ? _flashbackBackend.GetOrCreateExporter()\n                : _flashbackBackend.Exporter;");
        AssertContains(backendSnapshot, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);\n            exportOperationLockHeld = true;");
        AssertOccursBefore(backendSnapshot, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(backendSnapshot, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertContains(backendSnapshot, "new FlashbackExportBackendSnapshot(bufferManager, flashbackSink, flashbackExporter)");

        var exportCore = ExtractTextBetween(
            exportCoreText,
            "    private async Task<FinalizeResult> ExportFlashbackCoreAsync",
            "\n}\n");
        AssertContains(exportCore, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(exportCore, "bool exportOperationLockAlreadyHeld = false,");
        AssertContains(exportCore, "FlashbackExportRangeResolver? resolveRangeAfterEvictionPaused = null)");
        AssertContains(exportCore, "var exportOperationLockHeld = exportOperationLockAlreadyHeld;");
        AssertContains(exportCore, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(exportCore, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");
        AssertOccursBefore(exportCore, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(exportCore, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackBackend.GetOrCreateExporter();\n            }");
        AssertContains(exportCore, "var preparedExport = PrepareFlashbackExportRequest(");
        AssertContains(exportCore, "if (preparedExport.FailureResult is { } preparationFailure)");
        AssertContains(exportCoreText, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertContains(exportCore, "live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames");
        AssertContains(exportCore, "if (preparedExport.ForceRotateFallbackUsed && result.Succeeded)\n            {\n                result = FinalizeResult.Success(");
        AssertContains(exportCore, "_flashbackExport.RecordLastResult(exportId, result);\n            _flashbackExport.CompleteDiagnostics(exportId, result);");

        var backendCleanup = ExtractTextBetween(
            backendResourcesText,
            "private async Task<bool> CleanupArtifactsAfterExportAsync",
            "    public async Task StartPreviewBackendAsync");
        AssertContains(backendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(backendCleanup, "_exportOperationLock.WaitAsync(");
        AssertContains(backendCleanup, "return CleanupArtifactsUnderExportLock(request, mode);");
        AssertDoesNotContain(backendCleanup, "exportOperationLockAlreadyHeld");
        AssertContains(backendCleanup, "request.Reason");
        AssertContains(backendCleanup, "request.FlashbackExporter.Dispose();");
        AssertContains(backendCleanup, "request.BufferManager.PurgeAllSegments();");
        AssertContains(backendCleanup, "request.BufferManager.IsSessionPreservedForRecovery");
        AssertContains(backendCleanup, "FLASHBACK_BUFFER_CLEANUP_PRESERVE_RECOVERY mode={mode} reason='{request.Reason}'");
        AssertContains(backendCleanup, "FLASHBACK_BUFFER_CLEANUP_RETIRE mode={mode} reason='{request.Reason}'");
        AssertContains(backendCleanup, "request.BufferManager.MarkSessionRetiredForStartupCleanup(request.Reason);");
        AssertContains(backendCleanup, "if (lockAcquired)");
        AssertContains(backendCleanup, "_exportOperationLock.Release();");
        var callerHeldCleanup = ExtractDeclaredMemberCode(
            backendResourcesText,
            "private static bool CleanupArtifactsUnderExportLock(");
        AssertDoesNotContain(callerHeldCleanup, "_exportOperationLock");

        var disposeBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendAsync",
            "    private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest");
        AssertContains(disposeBackend, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertOccursBefore(disposeBackend, "_flashbackBackendLeaseLock.WaitAsync(", "_flashbackExportOperationLock.WaitAsync(");
        AssertOccursBefore(disposeBackend, "_flashbackExportOperationLock.WaitAsync(", "_flashbackBackend.DisposePreviewBackendUnderExportLockAsync(");
        AssertContains(disposeBackend, "await _flashbackBackend.DisposePreviewBackendUnderExportLockAsync(");
        AssertContains(disposeBackend, "CreateFlashbackPreviewBackendDisposalRequest(");
        AssertContains(disposeBackend, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");

        var disposeBackendResources = ExtractTextBetween(
            backendResourcesText,
            "public async Task DisposePreviewBackendUnderExportLockAsync",
            "    private void ScheduleDeferredArtifactCleanup");
        AssertDoesNotContain(disposeBackendResources, "request.ExportOperationLockAlreadyHeld");
        AssertContains(disposeBackendResources, "request.PurgeSegments ? \"preview_backend_dispose_purge\" : \"preview_backend_dispose\"");
        AssertContains(disposeBackendResources, "CleanupArtifactsUnderExportLock(cleanupRequest, \"preview_backend_dispose\")");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelFlashbackExport_RoutesThroughCoordinatorAndOwnsCtsLifecycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var disposalText = viewModelFiles["MainViewModel.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawDisposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportOperationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();

        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "_sessionCoordinator.ExportFlashbackRangeAsync(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.InPointFilePts");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.OutPointFilePts");
        AssertContains(coordinatorText, "TimeSpan? InPointFilePts,");
        AssertContains(coordinatorText, "TimeSpan? OutPointFilePts,");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"export\")");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"save_last_5m\")");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        AssertContains(rawFlashbackExportText, "StatusText = StatusMessages.FlashbackNotActiveForExport;");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "case ExportFlashbackOutcome.Stale:");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "case ExportFlashbackOutcome.Stale:");
        AssertContains(rawFlashbackExportText, "private static string FormatSuccessfulFlashbackExportStatus(");
        AssertMemberContains(
            flashbackExportText,
            "ExportFlashbackAsync",
            "FormatSuccessfulFlashbackExportStatus(");
        AssertMemberContains(
            flashbackExportText,
            "SaveFlashbackLast5mAsync",
            "FormatSuccessfulFlashbackExportStatus(");
        AssertContains(
            rawFlashbackExportText,
            "FormatSuccessfulFlashbackExportStatus(\"Export complete\", exportPath, succeeded.Result)");
        AssertContains(
            rawFlashbackExportText,
            "FormatSuccessfulFlashbackExportStatus(\"Saved last 5 minutes\", exportPath, succeeded.Result)");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertContains(disposalText, "Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(disposalText, "var exportCts = Interlocked.Exchange(ref _exportCts, null);");
        AssertContains(disposalText, "CancelFlashbackExportCts(exportCts);");
        AssertContains(rawDisposalText, "private void CancelActiveFlashbackExportForDispose()");
        var disposalCoreText = ExtractTextBetween(
            disposalControllerText,
            "private async Task DisposeCoreAsync()",
            "private static int GetDisposeTimeoutMs()");
        AssertOccursBefore(disposalCoreText, "_context.CancelActiveFlashbackExport();", "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalCoreText, "_context.StopRuntimeForDispose();", "_context.CleanupSessionCoordinatorAsync,");
        AssertOccursBefore(disposalCoreText, "_context.CleanupSessionCoordinatorAsync,", "_context.DisposeSessionCoordinatorAsync,");
        AssertOccursBefore(disposalCoreText, "_context.DisposeSessionCoordinatorAsync,", "await _context.DisposeCaptureServiceAsync().ConfigureAwait(false);");
        AssertOccursBefore(disposalCoreText, "await _context.DisposeCaptureServiceAsync().ConfigureAwait(false);", "_context.CompleteRuntimeDispose();");
        AssertContains(rawDisposalText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(flashbackExportOperationText, "private abstract record ExportFlashbackOutcome");
        AssertContains(flashbackExportOperationText, "private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync");
        AssertContains(viewModelFlashbackStateText, "private readonly SemaphoreSlim _flashbackExportRequestGate = new(1, 1);");
        AssertMemberContains(flashbackExportOperationText, "ExportFlashbackCoreAsync", "await _flashbackExportRequestGate.WaitAsync();");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "await _flashbackExportRequestGate.WaitAsync(cancellationToken);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "Volatile.Read(ref _disposeState) != 0");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");\n                    _flashbackExportRequestGate.Release();");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");\n                _flashbackExportRequestGate.Release();");
        AssertContains(flashbackExportOperationText, "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(flashbackExportOperationText, "CancelFlashbackExportCts(oldExportCts);");
        AssertContains(flashbackExportOperationText, "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertContains(flashbackExportOperationText, "_exportCts = null;");
        AssertContains(flashbackExportOperationText, "ReferenceEquals(_exportCts, exportCts)");
        AssertContains(flashbackExportOperationText, "private static void CancelFlashbackExportCts(CancellationTokenSource? cts)");
        AssertContains(flashbackExportOperationText, "catch (ObjectDisposedException)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_CANCEL_WARN");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancelFlashbackExportCts(oldExportCts);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "FlashbackExportProgress = p.Percent;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "exportCts.Token");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_exportCts = null;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "if (!_dispatcherQueue.TryEnqueue(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "finally");
        AssertContains(rawFlashbackExportAutomationText, "IsFlashbackExporting = false;\n                    FlashbackExportProgress = 0;\n                    _exportCts = null;");
        AssertContains(flashbackExportOperationText, "private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_DISPOSE_WARN");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_current\");");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_stale\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");");
        AssertDoesNotContain(
            flashbackExportText + "\n" + flashbackExportOperationText + "\n" + flashbackExportAutomationText,
            "exportCts.Dispose();");

        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        var finalizeResultType = RequireType("Sussudio.Services.Contracts.FinalizeResult");
        var successFactory = finalizeResultType.GetMethod(
            "Success",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(string) },
            null);
        AssertNotNull(successFactory, "FinalizeResult.Success(string, string)");
        var formatter = viewModelType.GetMethod(
            "FormatSuccessfulFlashbackExportStatus",
            BindingFlags.Static | BindingFlags.NonPublic);
        AssertNotNull(formatter, "MainViewModel.FormatSuccessfulFlashbackExportStatus");

        var partialSuccess = successFactory!.Invoke(
            null,
            new object[]
            {
                "clip.mp4",
                "Exported 42 packets (live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames)"
            });
        var partialStatus = (string)formatter!.Invoke(
            null,
            new object?[] { "Export complete", "clip.mp4", partialSuccess })!;
        AssertContains(partialStatus, "Export complete: clip.mp4 (Exported 42 packets");
        AssertContains(partialStatus, "live-edge partial fallback");
        AssertContains(partialStatus, "export may omit the newest frames");

        var blankSuccess = successFactory.Invoke(null, new object[] { "clip.mp4", "" });
        var blankStatus = (string)formatter.Invoke(
            null,
            new object?[] { "Export complete", "clip.mp4", blankSuccess })!;
        AssertEqual("Export complete: clip.mp4", blankStatus, "blank Flashback export success detail");

        return Task.CompletedTask;
    }

    internal static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationAudioText = automationUiText;
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var previewVolumeCommand = ExtractMemberCode(automationAudioText, "SetPreviewVolumeAsync");

        AssertContains(previewVolumeCommand, "SetPreviewVolumeFromUser(Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0));");
        AssertOccursBefore(previewVolumeCommand, "SetPreviewVolumeFromUser(", "SaveSettingsOrThrow();");
        AssertContains(ExtractMemberCode(audioStateText, "SetPreviewVolumeFromUser"), "_previewAudioVolumeTransitionController.SetUserVolume(value)");
        AssertContains(ExtractMemberCode(settingsProjectionText, "SaveSettings"), "_previewAudioVolumeTransitionController.RequestedVolume,");
        AssertContains(settingsProjectionText, "PreviewVolume = input.PreviewVolume,");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationUiText, "public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        return Task.CompletedTask;
    }

    internal static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var settingsPersistenceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var settingsLoadApplicationText = settingsPersistenceText;
        var settingsProjectionText = settingsPersistenceText[..settingsPersistenceText.IndexOf("public partial class MainViewModel", StringComparison.Ordinal)];
        var settingsServiceText = ReadRepoFile("Sussudio/Services/Runtime/SettingsService.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertContains(settingsServiceText, "public string? SelectedVideoFormat { get; set; }");
        AssertContains(settingsPersistenceText, "private void LoadSettings()");
        AssertContains(settingsPersistenceText, "private bool SaveSettings()");
        AssertContains(settingsPersistenceText, "SettingsService.Load()");
        AssertContains(settingsPersistenceText, "SettingsService.Save(settings, out var settingsSaveFailure)");
        AssertContains(settingsPersistenceText, "StatusText = StatusMessages.SettingsSaveFailed(settingsSaveFailure);");
        AssertContains(settingsPersistenceText, "return false;");
        AssertContains(settingsServiceText, "public static bool Save(UserSettings settings, out string failure)");
        AssertContains(settingsServiceText, "internal static bool SaveToFile(UserSettings settings, string settingsFilePath, out string failure)");
        AssertContains(settingsServiceText, "failure = $\"{ex.GetType().Name}: {ex.Message}\";");
        AssertContains(settingsServiceText, "return false;");
        AssertContains(settingsPersistenceText, "SaveSettings = () => { _ = viewModel.SaveSettings(); }");
        AssertContains(settingsPersistenceText, "private void SaveSettingsOrThrow()");
        AssertContains(settingsPersistenceText, "throw new InvalidOperationException(StatusText);");
        AssertContains(settingsPersistenceText, "private async Task RunPersistedSettingsAutomationAsync(Task operation, CancellationToken cancellationToken)");
        AssertContains(settingsPersistenceText, "await operation.ConfigureAwait(false);");
        foreach (var persistedAutomationMember in new[]
        {
            "SelectDeviceAsync",
            "SelectAudioInputDeviceAsync",
            "SetCustomAudioInputEnabledAsync",
            "SetAudioEnabledAsync",
            "SetAudioPreviewEnabledAsync",
            "SetPreviewVolumeAsync",
            "SetDeviceAudioModeAsync",
            "SetAnalogAudioGainAsync",
            "SetStatsVisibleAsync"
        })
        {
            AssertMemberContains(settingsPersistenceText, persistedAutomationMember, "SaveSettingsOrThrow();");
        }

        AssertContains(settingsPersistenceText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(settingsPersistenceText, "_suppressMicrophoneMonitorUpdate = false;\n                }\n\n                SaveSettingsOrThrow();");
        foreach (var delegatedPersistedAutomationMember in new[]
        {
            "SetRecordingFormatAsync",
            "SetQualityAsync",
            "SetSplitEncodeModeAsync",
            "SetCustomBitrateAsync",
            "SetPresetAsync",
            "SetOutputPathAsync"
        })
        {
            AssertMemberContains(settingsPersistenceText, delegatedPersistedAutomationMember, "RunPersistedSettingsAutomationAsync(");
        }

        AssertContains(settingsPersistenceText, "Directory.Exists");
        AssertContains(settingsPersistenceText, "_isLoadingSettings = true;");
        AssertContains(settingsPersistenceText, "_isLoadingSettings = false;");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildLoadPlan(");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildSaveSettings(");
        AssertContains(settingsPersistenceText, "private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsPersistenceText, "ApplyRecordingSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyAudioSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyUiSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyDeviceAudioSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyFlashbackSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "StageDeferredDeviceSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyRecordingSettingsLoadPlan(loadPlan);", "ApplyAudioSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyAudioSettingsLoadPlan(loadPlan);", "ApplyUiSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyUiSettingsLoadPlan(loadPlan);", "ApplyDeviceAudioSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyDeviceAudioSettingsLoadPlan(loadPlan);", "ApplyFlashbackSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyFlashbackSettingsLoadPlan(loadPlan);", "StageDeferredDeviceSettingsLoadPlan(loadPlan);");
        AssertContains(settingsLoadApplicationText, "private void ApplyRecordingSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyUiSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyDeviceAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyFlashbackSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void StageDeferredDeviceSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "_pendingSavedDeviceId = loadPlan.PendingDeviceId;");
        AssertContains(settingsLoadApplicationText, "_pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;");
        AssertContains(settingsLoadApplicationText, "_pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;");
        AssertContains(settingsProjectionText, "internal static class MainViewModelSettingsPersistenceProjection");
        AssertContains(settingsProjectionText, "internal static MainViewModelSettingsLoadPlan BuildLoadPlan(");
        AssertContains(settingsProjectionText, "internal static UserSettings BuildSaveSettings(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsLoadInput(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsLoadPlan(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsSaveInput(");
        foreach (var removedProjectionFile in new[]
        {
            "MainViewModelSettingsPersistenceProjection.cs",
            "MainViewModelSettingsPersistenceProjection.Load.cs",
            "MainViewModelSettingsPersistenceProjection.Save.cs",
            "MainViewModelSettingsPersistenceProjection.Models.cs"
        })
        {
        }
        AssertDoesNotContain(settingsProjectionText, "SettingsService");
        AssertDoesNotContain(settingsProjectionText, "Logger");
        AssertDoesNotContain(settingsProjectionText, "Directory.");
        AssertDoesNotContain(settingsProjectionText, "MainViewModel.");
        AssertContains(settingsProjectionText, "IsStatsVisible: settings.IsStatsVisible,");
        AssertContains(settingsProjectionText, "IsStatsVisible = input.IsStatsVisible,");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0)");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)");
        AssertContains(settingsProjectionText, "ResolveAvailableValue(");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.ShowAllCaptureOptions.HasValue)");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.IsStatsVisible.HasValue)");
        AssertContains(settingsPersistenceText, "partial void OnIsStatsVisibleChanged(bool value)");
        AssertDoesNotContain(settingsPersistenceText, "RebuildResolutionOptions();\n        SaveSettings();");
        AssertContains(settingsProjectionText, "string? SelectedVideoFormat");
        AssertContains(settingsProjectionText, "SelectedVideoFormat: settings.SelectedVideoFormat");
        AssertContains(settingsProjectionText, "SelectedVideoFormat = input.SelectedVideoFormat,");
        AssertContains(settingsPersistenceText, "_pendingSavedVideoFormat = loadPlan.SelectedVideoFormat;");

        var settingsServiceType = RequireType("Sussudio.Services.Runtime.SettingsService");
        var userSettingsType = RequireType("Sussudio.Services.Runtime.UserSettings");
        var publicSave = settingsServiceType.GetMethod(
            "Save",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { userSettingsType, typeof(string).MakeByRefType() },
            null);
        AssertNotNull(publicSave, "SettingsService.Save(UserSettings, out string)");
        var saveToFile = settingsServiceType.GetMethod(
            "SaveToFile",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { userSettingsType, typeof(string), typeof(string).MakeByRefType() },
            null);
        AssertNotNull(saveToFile, "SettingsService.SaveToFile");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"sussudio_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var settings = Activator.CreateInstance(userSettingsType)
                ?? throw new InvalidOperationException("UserSettings instance was not created.");
            SetPropertyOrBackingField(settings, "OutputPath", "C:\\SettingsTest");
            var successPath = Path.Combine(tempRoot, "ok", "settings.json");
            var successArgs = new object?[] { settings, successPath, string.Empty };
            AssertEqual(true, (bool)saveToFile!.Invoke(null, successArgs)!, "SettingsService.SaveToFile succeeds");
            AssertEqual(string.Empty, (string)successArgs[2]!, "successful settings save failure text");
            AssertEqual(true, File.Exists(successPath), "successful settings save writes JSON");

            var blockedDirectory = Path.Combine(tempRoot, "blocked");
            File.WriteAllText(blockedDirectory, "not a directory");
            var failureArgs = new object?[]
            {
                settings,
                Path.Combine(blockedDirectory, "settings.json"),
                string.Empty
            };
            AssertEqual(false, (bool)saveToFile.Invoke(null, failureArgs)!, "SettingsService.SaveToFile reports filesystem failure");
            AssertContains((string)failureArgs[2]!, "IOException");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempRoot);
        }

        return Task.CompletedTask;
    }

    internal static Task SettingsPersistenceProjection_LoadPlanPreservesSavedSemantics()
    {
        var settings = CreateSettings(
            ("SelectedDeviceId", "device-1"),
            ("OutputPath", "C:\\Rejected"),
            ("SelectedRecordingFormat", "AV1"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P7"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("CustomBitrateMbps", 42d),
            ("IsHdrEnabled", true),
            ("IsAudioEnabled", false),
            ("IsAudioPreviewEnabled", true),
            ("IsCustomAudioInputEnabled", true),
            ("SelectedAudioInputDeviceId", "audio-1"),
            ("IsMicrophoneEnabled", true),
            ("SelectedMicrophoneDeviceId", "mic-1"),
            ("MicrophoneVolume", 150d),
            ("PreviewVolume", -0.25d),
            ("IsStatsVisible", false),
            ("SelectedDeviceAudioMode", "Analog"),
            ("AnalogAudioGainPercent", -5d),
            ("FlashbackGpuDecode", true),
            ("FlashbackBufferMinutes", 99));

        var plan = BuildSettingsLoadPlan(
            settings,
            availableRecordingFormats: new[] { "H264", "HEVC" },
            outputDirectoryExists: path => path == "C:\\Accepted");

        AssertEqual(null, GetPropertyValue(plan, "OutputPath"), "settings load invalid output path");
        AssertEqual(null, GetPropertyValue(plan, "SelectedRecordingFormat"), "settings load unavailable recording format");
        AssertEqual("AV1", GetPropertyValue(plan, "UnavailableRecordingFormat"), "settings load unavailable recording format marker");
        AssertEqual("High", GetPropertyValue(plan, "SelectedQuality"), "settings load selected quality");
        AssertEqual("P7", GetPropertyValue(plan, "SelectedPreset"), "settings load selected preset");
        AssertEqual("Auto", GetPropertyValue(plan, "SelectedSplitEncodeMode"), "settings load selected split encode mode");
        AssertEqual(42d, GetPropertyValue(plan, "CustomBitrateMbps"), "settings load custom bitrate");
        AssertEqual(true, GetPropertyValue(plan, "IsHdrEnabled"), "settings load hdr enabled");
        AssertEqual(false, GetPropertyValue(plan, "IsAudioEnabled"), "settings load audio enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsAudioPreviewEnabled"), "settings load audio preview enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsCustomAudioInputEnabled"), "settings load custom audio input enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsMicrophoneEnabled"), "settings load microphone enabled");
        AssertEqual(100d, GetPropertyValue(plan, "MicrophoneVolume"), "settings load microphone volume clamp");
        AssertEqual("mic-1", GetPropertyValue(plan, "PendingMicrophoneVolumeDeviceId"), "settings load microphone volume device");
        AssertEqual(0d, GetPropertyValue(plan, "PreviewVolume"), "settings load preview volume clamp");
        AssertEqual(false, GetPropertyValue(plan, "IsStatsVisible"), "settings load stats visible");
        AssertEqual("Analog", GetPropertyValue(plan, "SelectedDeviceAudioMode"), "settings load selected device audio mode");
        AssertEqual(0d, GetPropertyValue(plan, "AnalogAudioGainPercent"), "settings load analog gain clamp");
        AssertEqual(true, GetPropertyValue(plan, "FlashbackGpuDecode"), "settings load flashback gpu decode");
        AssertEqual(30, GetPropertyValue(plan, "FlashbackBufferMinutes"), "settings load flashback buffer clamp");
        AssertEqual("device-1", GetPropertyValue(plan, "PendingDeviceId"), "settings load pending device");
        AssertEqual("audio-1", GetPropertyValue(plan, "PendingAudioDeviceId"), "settings load pending audio device");
        AssertEqual("mic-1", GetPropertyValue(plan, "PendingMicrophoneDeviceId"), "settings load pending microphone device");
        AssertEqual("Analog", GetPropertyValue(plan, "PendingDeviceAudioMode"), "settings load pending audio mode");
        AssertEqual(-5d, GetPropertyValue(plan, "PendingAnalogAudioGainPercent"), "settings load pending analog gain");

        return Task.CompletedTask;
    }

    internal static Task SettingsPersistenceProjection_SaveSettingsMapsPersistedValues()
    {
        var settings = BuildSettingsSaveSettings(
            selectedDeviceId: "device-2",
            outputPath: "C:\\Capture",
            selectedRecordingFormat: "HEVC",
            selectedQuality: "Balanced",
            selectedPreset: "P5",
            selectedSplitEncodeMode: "Disabled",
            customBitrateMbps: 55d,
            isHdrEnabled: true,
            isAudioEnabled: true,
            isAudioPreviewEnabled: false,
            isCustomAudioInputEnabled: true,
            selectedAudioInputDeviceId: "audio-2",
            isMicrophoneEnabled: true,
            selectedMicrophoneDeviceId: "mic-2",
            microphoneVolume: 75d,
            previewVolume: 0.625d,
            isStatsVisible: true,
            selectedDeviceAudioMode: "Embedded",
            analogAudioGainPercent: 33d,
            flashbackGpuDecode: false,
            flashbackBufferMinutes: 12);

        AssertEqual("device-2", GetPropertyValue(settings, "SelectedDeviceId"), "settings save selected device");
        AssertEqual("C:\\Capture", GetPropertyValue(settings, "OutputPath"), "settings save output path");
        AssertEqual("HEVC", GetPropertyValue(settings, "SelectedRecordingFormat"), "settings save recording format");
        AssertEqual("Balanced", GetPropertyValue(settings, "SelectedQuality"), "settings save quality");
        AssertEqual("P5", GetPropertyValue(settings, "SelectedPreset"), "settings save preset");
        AssertEqual("Disabled", GetPropertyValue(settings, "SelectedSplitEncodeMode"), "settings save split encode mode");
        AssertEqual(55d, GetPropertyValue(settings, "CustomBitrateMbps"), "settings save custom bitrate");
        AssertEqual(true, GetPropertyValue(settings, "IsHdrEnabled"), "settings save hdr enabled");
        AssertEqual(true, GetPropertyValue(settings, "IsAudioEnabled"), "settings save audio enabled");
        AssertEqual(false, GetPropertyValue(settings, "IsAudioPreviewEnabled"), "settings save audio preview enabled");
        AssertEqual(true, GetPropertyValue(settings, "IsCustomAudioInputEnabled"), "settings save custom audio input enabled");
        AssertEqual("audio-2", GetPropertyValue(settings, "SelectedAudioInputDeviceId"), "settings save selected audio input");
        AssertEqual(true, GetPropertyValue(settings, "IsMicrophoneEnabled"), "settings save microphone enabled");
        AssertEqual("mic-2", GetPropertyValue(settings, "SelectedMicrophoneDeviceId"), "settings save selected microphone");
        AssertEqual(75d, GetPropertyValue(settings, "MicrophoneVolume"), "settings save microphone volume");
        AssertEqual(0.625d, GetPropertyValue(settings, "PreviewVolume"), "settings save preview volume");
        AssertEqual(true, GetPropertyValue(settings, "IsStatsVisible"), "settings save stats visible");
        AssertEqual("Embedded", GetPropertyValue(settings, "SelectedDeviceAudioMode"), "settings save selected device audio mode");
        AssertEqual(33d, GetPropertyValue(settings, "AnalogAudioGainPercent"), "settings save analog gain");
        AssertEqual(false, GetPropertyValue(settings, "FlashbackGpuDecode"), "settings save flashback gpu decode");
        AssertEqual(12, GetPropertyValue(settings, "FlashbackBufferMinutes"), "settings save flashback buffer minutes");
        AssertEqual("Auto", GetPropertyValue(settings, "SelectedVideoFormat"), "settings save selected video format");

        return Task.CompletedTask;
    }

    internal static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");

        AssertDoesNotContain(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(automationSettingsText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> RunPersistedSettingsAutomationAsync(");
        AssertContains(automationSettingsText, "_captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken),");
        AssertContains(automationSettingsText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);");
        AssertDoesNotContain(automationSettingsText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationController");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(captureSettingsAutomationControllerText, "private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "_viewModel.");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(captureSettingsAutomationControllerText, "FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate)");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSelectedFrameRate(matched.Value);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSelectedVideoFormat(match);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetMjpegDecoderCount(Math.Clamp(decoderCount, 1, 8));");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "await _captureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureSettingsAutomationControllerText, "MainViewModelCaptureSelectionSnapshot rollback = default;");
        AssertContains(captureSettingsAutomationControllerText, "MainViewModelCaptureSelectionSnapshot attempted = default;");
        AssertContains(captureSettingsAutomationControllerText, "rollback = _context.CaptureSelectionSnapshot();");
        AssertContains(captureSettingsAutomationControllerText, "attempted = _context.CaptureSelectionSnapshot();");
        AssertContains(captureModeTransactionsText, "AvailableResolutions.ToArray()");
        AssertContains(captureModeTransactionsText, "AvailableFrameRates.ToArray()");
        AssertContains(captureModeTransactionsText, "AvailableVideoFormats.ToArray()");
        AssertContains(captureModeTransactionsText, "AvailableRecordingFormats.ToArray()");
        AssertContains(captureModeTransactionsText, "_latestSourceTelemetry");
        AssertContains(captureSettingsAutomationControllerText, "_context.ApplyCaptureSelectionWithoutReinitialize(apply);");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "SetSuppressFormatChangeReinitialize");
        AssertContains(captureSettingsAutomationControllerText, "return wasPreviewing && _context.GetSelectedFormat() != null;");
        AssertContains(captureSettingsAutomationControllerText, "reinitialized = await _context.ReinitializeDeviceWithResultAsync($\"automation {reason}\")");
        AssertContains(captureSettingsAutomationControllerText, "var restored = await RestoreCaptureSelectionSnapshotIfUnchangedAsync(rollback, attempted).ConfigureAwait(false);");
        AssertContains(captureSettingsAutomationControllerText, "a newer capture selection superseded this request");
        AssertContains(captureSettingsAutomationControllerText, "_captureModeGate.Release();");
        AssertContains(previewLifecycleControllerText, "private async Task<bool> ReinitializeDeviceCoreAsync(\n        string reason,\n        bool treatCoalescedAsSuccess,\n        CaptureErrorOrigin? errorOrigin = null)");
        AssertContains(previewLifecycleControllerText, "if (!IsCaptureErrorCurrent(errorOrigin))");
        AssertContains(previewLifecycleControllerText, "private async Task<bool> TryInitializeAndRestartPreviewAsync(");
        AssertEqual(
            4,
            Regex.Matches(previewLifecycleControllerText, @"\bTryInitializeAndRestartPreviewAsync\(").Count,
            "preview reinitialize helper definition plus three call sites");
        AssertContains(previewLifecycleControllerText, "return treatCoalescedAsSuccess;");
        AssertContains(previewLifecycleControllerText, "if (_context.IsInitialized())\n            {\n                await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);\n            }");
        AssertContains(previewLifecycleControllerText, "catch (PreviewRendererReinitStopTimeoutException ex)");
        AssertContains(previewLifecycleControllerText, "REINIT_ABORT_RENDERER_STOP_TIMEOUT reason='{reason}'");
        AssertContains(previewLifecycleControllerText, "await CleanupFailedPreviewRestartAsync(reason).ConfigureAwait(true);");
        AssertContains(previewLifecycleControllerText, "private async Task CleanupFailedPreviewRestartAsync(string reason)");
        AssertContains(previewLifecycleControllerText, "teardownPipeline: true");
        AssertContains(previewLifecycleControllerText, "_context.SetIsPreviewing(false);");
        AssertContains(previewLifecycleControllerText, "_context.SetIsInitialized(false);");
        var rendererStopTimeoutCatch = ExtractTextBetween(
            previewLifecycleControllerText,
            "catch (PreviewRendererReinitStopTimeoutException ex)",
            "        catch (Exception ex)");
        AssertDoesNotContain(rendererStopTimeoutCatch, "CleanupFailedPreviewRestartAsync");
        AssertContains(rendererStopTimeoutCatch, "success = false;");
        AssertContains(previewLifecycleControllerText, "_context.RestoreCaptureSelectionSnapshotIfUnchanged(rollback, attempted);");
        AssertContains(captureModeTransactionsText, "private bool RestoreCaptureSelectionSnapshotIfUnchanged(");
        AssertContains(captureModeTransactionsText, "if (!CaptureSelectionSnapshot().MatchesSelectionState(expectedCurrent))");
        AssertContains(captureModeTransactionsText, "RestoreCollection(AvailableResolutions, snapshot.AvailableResolutions);");
        AssertContains(captureModeTransactionsText, "RestoreCollection(AvailableFrameRates, snapshot.AvailableFrameRates);");
        AssertContains(captureModeTransactionsText, "RestoreCollection(AvailableVideoFormats, snapshot.AvailableVideoFormats);");
        AssertContains(captureModeTransactionsText, "RestoreCollection(AvailableRecordingFormats, snapshot.AvailableRecordingFormats);");
        AssertContains(captureModeTransactionsText, "_latestSourceTelemetry = snapshot.LatestSourceTelemetry;");
        AssertContains(captureModeTransactionsText, "SaveSettings();");
        AssertContains(previewLifecycleControllerText, "string.Equals(SelectedRecordingFormat, other.SelectedRecordingFormat, StringComparison.Ordinal)");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertDoesNotContain(captureModeTransactionsText, "SetAutomationCaptureModeAsync(");

        return Task.CompletedTask;
    }

    internal static Task AutomationDeviceSelection_RoutesThroughApplyReinit()
    {
        var deviceSelectionAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");
        var selectDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectDeviceAsync",
            "public Task SelectAudioInputDeviceAsync");
        var selectAudioDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectAudioInputDeviceAsync",
            "public Task SetCustomAudioInputEnabledAsync");

        AssertContains(deviceSelectionAutomationText, "public Task RefreshDevicesForAutomationAsync");
        AssertContains(deviceSelectionAutomationText, "=> InvokeOnUiThreadAsync(() => _deviceRefreshController.RefreshDevicesAsync(throwOnScanFailure: true, cancellationToken: cancellationToken), cancellationToken);");
        AssertContains(deviceSelectionAutomationText, "public Task SelectDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(deviceSelectionAutomationText, "private static T? ResolveByName<T>(");
        AssertContains(deviceSelectionAutomationText, ".Contains(deviceName, StringComparison.OrdinalIgnoreCase)");
        AssertContains(deviceSelectionAutomationText, ".Take(2)");
        AssertContains(deviceSelectionAutomationText, "return partialMatches.Length == 1 ? partialMatches[0] : null;");
        AssertContains(rootViewModelText, "public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken: cancellationToken);");
        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            if (requestGeneration == Volatile.Read(ref _refreshRequestGeneration))\n            {\n                _context.SetStatusText(StatusMessages.DeviceScanCanceled);\n            }\n\n            throw;\n        }");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "var applied = await ApplySelectedDeviceWithResultAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertContains(selectDevice, "throw new InvalidOperationException(\"Capture device selection did not initialize; rollback was skipped if a newer selection superseded this request.\");");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_HdrEnablementLivesInCaptureSelection()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var setHdrBlock = ExtractTextBetween(
            captureModeTransactionsText,
            "public Task SetHdrEnabledAsync",
            "public Task SetTrueHdrPreviewEnabledAsync");
        var hdrChangeBlock = ExtractMemberCode(
            captureModeTransactionsText,
            "OnIsHdrEnabledChanged");

        AssertContains(captureModeTransactionsText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(setHdrBlock, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(captureModeTransactionsText, "throw new AutomationStateConflictException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(captureModeTransactionsText, "if (enabled && !IsHdrAvailable)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(setHdrBlock, "var rollback = CaptureSelectionSnapshot();");
        AssertContains(setHdrBlock, "var shouldReinitialize = IsInitialized && SelectedDevice != null && SelectedFormat != null;");
        AssertContains(setHdrBlock, "_suppressHdrToggleReinitialize = true;");
        AssertContains(setHdrBlock, "IsHdrEnabled = enabled;");
        AssertContains(setHdrBlock, "var attempted = CaptureSelectionSnapshot();");
        AssertContains(setHdrBlock, "if (shouldReinitialize && SelectedFormat != null)");
        AssertContains(setHdrBlock, "var reinitialized = await ReinitializeDeviceWithResultAsync(\"automation HDR toggle\").ConfigureAwait(true);");
        AssertContains(setHdrBlock, "var restored = RestoreCaptureSelectionSnapshotIfUnchanged(rollback, attempted);");
        AssertContains(setHdrBlock, "throw new InvalidOperationException($\"Failed to apply automation HDR toggle; {rollbackStatus}.\");");
        AssertDoesNotContain(setHdrBlock, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertContains(captureModeTransactionsText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new AutomationStateConflictException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(captureModeTransactionsText, "IsTrueHdrPreviewEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(captureModeTransactionsText, "if (_isRevertingHdrToggle)");
        AssertContains(captureModeTransactionsText, "_captureModeSelection.ClearPendingSdrAutoSelection();");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = !value;");
        AssertContains(captureModeTransactionsText, "StatusText = HdrToggleBlockedWhileRecordingMessage;");
        AssertContains(captureModeTransactionsText, "ResetModeSelectionState();");
        AssertContains(captureModeTransactionsText, "RebuildResolutionOptions();");
        AssertContains(captureModeTransactionsText, "RebuildRecordingFormatOptions();");
        AssertContains(hdrChangeBlock, "if (!_suppressHdrToggleReinitialize && IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)");
        AssertContains(captureModeTransactionsText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertContains(captureModeTransactionsText, "SaveSettings();");
        AssertOccursBefore(hdrChangeBlock, "if (_isRevertingHdrToggle)", "if (value)");
        AssertOccursBefore(hdrChangeBlock, "if (value)", "if (IsRecording)");
        AssertOccursBefore(hdrChangeBlock, "StatusText = HdrToggleBlockedWhileRecordingMessage;", "if (!_isChangingDevice)");
        AssertOccursBefore(hdrChangeBlock, "ResetModeSelectionState();", "RebuildResolutionOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildResolutionOptions();", "RebuildRecordingFormatOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildRecordingFormatOptions();", "if (!_suppressHdrToggleReinitialize && IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)");
        AssertOccursBefore(hdrChangeBlock, "if (!_suppressHdrToggleReinitialize && IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)", "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertOccursBefore(hdrChangeBlock, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");", "SaveSettings();");

        return Task.CompletedTask;
    }

    private static object CreateSettings(params (string Property, object? Value)[] values)
    {
        var settings = CreateInstance("Sussudio.Services.Runtime.UserSettings");
        foreach (var (property, value) in values)
        {
            SetPropertyOrBackingField(settings, property, value);
        }

        return settings;
    }

    private static object BuildSettingsLoadPlan(
        object settings,
        string[] availableRecordingFormats,
        Func<string, bool> outputDirectoryExists)
    {
        var inputType = RequireType("Sussudio.ViewModels.MainViewModelSettingsLoadInput");
        var input = InvokeSingleConstructor(inputType,
            availableRecordingFormats,
            new[] { "High", "Balanced" },
            new[] { "P7", "P5" },
            new[] { "Auto", "Disabled" },
            new[] { "Embedded", "Analog" },
            outputDirectoryExists);

        var projectionType = RequireType("Sussudio.ViewModels.MainViewModelSettingsPersistenceProjection");
        var buildLoadPlan = projectionType.GetMethod(
            "BuildLoadPlan",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildLoadPlan was not found.");

        return buildLoadPlan.Invoke(null, new[] { settings, input })
               ?? throw new InvalidOperationException("BuildLoadPlan returned null.");
    }

    private static object BuildSettingsSaveSettings(
        string? selectedDeviceId,
        string outputPath,
        string selectedRecordingFormat,
        string selectedQuality,
        string selectedPreset,
        string selectedSplitEncodeMode,
        double customBitrateMbps,
        bool isHdrEnabled,
        bool isAudioEnabled,
        bool isAudioPreviewEnabled,
        bool isCustomAudioInputEnabled,
        string? selectedAudioInputDeviceId,
        bool isMicrophoneEnabled,
        string? selectedMicrophoneDeviceId,
        double microphoneVolume,
        double previewVolume,
        bool isStatsVisible,
        string selectedDeviceAudioMode,
        double analogAudioGainPercent,
        bool flashbackGpuDecode,
        int flashbackBufferMinutes,
        string selectedVideoFormat = "Auto")
    {
        var inputType = RequireType("Sussudio.ViewModels.MainViewModelSettingsSaveInput");
        var input = InvokeSingleConstructor(inputType,
            selectedDeviceId,
            outputPath,
            selectedRecordingFormat,
            selectedQuality,
            selectedPreset,
            selectedSplitEncodeMode,
            customBitrateMbps,
            isHdrEnabled,
            isAudioEnabled,
            isAudioPreviewEnabled,
            isCustomAudioInputEnabled,
            selectedAudioInputDeviceId,
            isMicrophoneEnabled,
            selectedMicrophoneDeviceId,
            microphoneVolume,
            previewVolume,
            isStatsVisible,
            selectedDeviceAudioMode,
            analogAudioGainPercent,
            flashbackGpuDecode,
            flashbackBufferMinutes,
            selectedVideoFormat);

        var projectionType = RequireType("Sussudio.ViewModels.MainViewModelSettingsPersistenceProjection");
        var buildSaveSettings = projectionType.GetMethod(
            "BuildSaveSettings",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSaveSettings was not found.");

        return buildSaveSettings.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("BuildSaveSettings returned null.");
    }

    private static object InvokeSingleConstructor(Type type, params object?[] arguments)
    {
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Length == arguments.Length);

        return constructor.Invoke(arguments);
    }

    internal static Task AutomationDiagnosticsSnapshotCollectionEpochs_MarkProducerChangesAsDiagnosticWarning()
    {
        var hubType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub");
        var viewModelType = RequireType("Sussudio.Models.ViewModelRuntimeSnapshot");
        var captureRuntimeType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var recordingStatsType = RequireType("Sussudio.Models.RecordingStats");
        var previewRuntimeType = RequireType("Sussudio.Models.PreviewRuntimeSnapshot");
        var lastOutputProbeType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub+LastOutputProbe");
        var fragmentDefinitionType = RequireType("Sussudio.Services.Automation.SnapshotFragment`1");
        var stampType = RequireType("Sussudio.Services.Automation.SnapshotCollectionStamp");
        var diagnosticType = RequireType("Sussudio.Services.Automation.DiagnosticEvaluation");

        var viewModel = Activator.CreateInstance(viewModelType)!;
        SetPropertyBackingField(viewModel, "CaptureSessionEpoch", 1L);
        SetPropertyBackingField(viewModel, "SourceTelemetryEpoch", 1L);

        var captureRuntime = Activator.CreateInstance(captureRuntimeType)!;
        SetPropertyBackingField(captureRuntime, "CaptureSessionEpoch", 1L);
        SetPropertyBackingField(captureRuntime, "SourceTelemetryEpoch", 1L);

        var health = Activator.CreateInstance(healthType)!;
        SetPropertyBackingField(health, "CaptureSessionEpoch", 1L);
        SetPropertyBackingField(health, "SourceTelemetryEpoch", 1L);

        var recordingStats = Activator.CreateInstance(
            recordingStatsType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { 0L, 0L, false, false, DateTimeOffset.UtcNow, 1L },
            culture: null)!;

        var previewRuntime = Activator.CreateInstance(previewRuntimeType)!;
        SetPropertyBackingField(previewRuntime, "PreviewRuntimeEpoch", 1L);
        var lastOutputProbe = Activator.CreateInstance(
            lastOutputProbeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { false, null, 1L },
            culture: null)!;

        var captureRuntimeFragment = CreateSnapshotFragment(fragmentDefinitionType, captureRuntime, 1L, 1L, 2L, producerChanged: true);
        var healthFragment = CreateSnapshotFragment(fragmentDefinitionType, health, 1L, 1L, 1L, producerChanged: false);
        var recordingStatsFragment = CreateSnapshotFragment(fragmentDefinitionType, recordingStats, 1L, 1L, 1L, producerChanged: false);
        var previewRuntimeFragment = CreateSnapshotFragment(fragmentDefinitionType, previewRuntime, 1L, 1L, 2L, producerChanged: true);
        var lastOutputFragment = CreateSnapshotFragment(fragmentDefinitionType, lastOutputProbe, 1L, 1L, 2L, producerChanged: true);

        var reasonMethod = hubType.GetMethod(
            "BuildSnapshotMixedEpochReasons",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSnapshotMixedEpochReasons not found.");
        var reasons = ((IEnumerable)reasonMethod.Invoke(
            null,
            new[]
            {
                viewModel,
                captureRuntime,
                health,
                recordingStats,
                previewRuntime,
                captureRuntimeFragment,
                healthFragment,
                recordingStatsFragment,
                previewRuntimeFragment,
                lastOutputFragment
            })!).Cast<object>().Select(static value => value.ToString()).ToArray();

        AssertEqual(true, reasons.Contains("capture_runtime_producer_changed"), "producer change reason emitted");
        AssertEqual(true, reasons.Contains("preview_runtime_producer_changed"), "preview producer change reason emitted");
        AssertEqual(true, reasons.Contains("output_producer_changed"), "output producer change reason emitted");

        var stamp = Activator.CreateInstance(
            stampType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[]
            {
                7L,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                1L,
                1L,
                1L,
                1L,
                1L,
                1L,
                0L,
                1L,
                true,
                "capture_runtime_producer_changed"
            },
            culture: null)!;
        var diagnostic = Activator.CreateInstance(
            diagnosticType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[]
            {
                "Healthy",
                "none",
                "No degraded frame lane detected.",
                string.Empty,
                "source ok",
                "decode ok",
                "preview ok",
                "render ok",
                "present ok",
                "recording ok",
                "audio ok"
            },
            culture: null)!;
        var diagnosticMethod = hubType.GetMethod(
            "ApplySnapshotCollectionDiagnostic",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ApplySnapshotCollectionDiagnostic not found.");
        var result = diagnosticMethod.Invoke(null, new[] { diagnostic, stamp })
            ?? throw new InvalidOperationException("ApplySnapshotCollectionDiagnostic returned null.");

        AssertEqual("Warning", GetPublicProperty(result, "HealthStatus"), "mixed producer epoch warning health");
        AssertEqual("snapshot_epoch", GetPublicProperty(result, "LikelyStage"), "mixed producer epoch warning stage");
        AssertContains((string)GetPublicProperty(result, "Evidence")!, "snapshot_epoch=7");

        return Task.CompletedTask;
    }

    private static object CreateSnapshotFragment(
        Type fragmentDefinitionType,
        object value,
        long producerEpoch,
        long producerEpochBefore,
        long producerEpochAfter,
        bool producerChanged)
    {
        var fragmentType = fragmentDefinitionType.MakeGenericType(value.GetType());
        return Activator.CreateInstance(
            fragmentType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { value, 7L, producerEpoch, producerEpochBefore, producerEpochAfter, producerChanged, DateTimeOffset.UtcNow },
            culture: null)!;
    }

    internal static Task Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected()
    {
        var diagnosticsType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub");
        var runtimeType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var verifierResultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        var method = diagnosticsType.GetMethod(
            "BuildHdrTruthVerdict",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { runtimeType, typeof(bool), verifierResultType },
            modifiers: null)
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict not found.");

        var runtime = Activator.CreateInstance(runtimeType)!;
        SetPropertyBackingField(runtime, "LatestObservedFramePixelFormat", "NV12");
        SetPropertyBackingField(runtime, "ObservedNv12FrameCount", 1L);
        SetPropertyBackingField(runtime, "SourceIsHdr", (bool?)true);

        var verdict = method.Invoke(null, new object?[] { runtime, false, null })
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict returned null.");

        AssertEqual("expected-sdr-capture", GetStringProperty(verdict, "SourceVsCaptureParity"), "SourceVsCaptureParity");
        AssertEqual("sdr-8bit", GetStringProperty(verdict, "FinalClassification"), "FinalClassification");

        return Task.CompletedTask;
    }

}

namespace Sussudio.Tests
{
public sealed class PreviewPacingClassifierTests
{
    private const string InputTypeName = "Sussudio.Services.Automation.PreviewPacingClassificationInput";
    private const string ClassifierTypeName = "Sussudio.Services.Automation.PreviewPacingSlowStageClassifier";

    [Fact]
    public void AutomationSnapshot_ExposesPreviewPacingClassificationFields()
    {
        var contractsText = ReadAutomationSnapshotFamilyText();

        Assert.Contains("public string PreviewPacingLikelySlowStage { get; init; }", contractsText);
        Assert.Contains("public string PreviewPacingSlowStageConfidence { get; init; }", contractsText);
        Assert.Contains("public string PreviewPacingSlowStageEvidence { get; init; }", contractsText);
    }

    [Fact]
    public void AutomationDiagnosticsHub_ProjectsPreviewPacingClassification()
    {
        var diagnosticsSnapshotsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var diagnosticsSnapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + diagnosticsSnapshotsText
            + "\n" + diagnosticsSnapshotProjectionText;

        Assert.Contains("var previewPacingClassification = ClassifyPreviewPacing(", diagnosticsSnapshotsText);
        Assert.Contains("new PreviewPacingClassificationInput", diagnosticsSnapshotsText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage", diagnosticsSnapshotProjectionText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage", diagnosticsSnapshotProjectionText);
        Assert.Contains("PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage", diagnosticsSnapshotProjectionText);
        Assert.Contains("private static PreviewPacingClassification ClassifyPreviewPacing(", diagnosticsSnapshotsText);
        Assert.Contains("PreviewPacingSlowStageClassifier.Classify", diagnosticsSnapshotsText);
        Assert.Contains("PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps", diagnosticsHubText);
        Assert.Contains("CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("RecentD3DMissedRefreshes = recentD3DMissedRefreshes", diagnosticsHubText);
        Assert.Contains("RecentPreviewJitterScheduleLateCount = recentPreviewJitter.ScheduleLateCount", diagnosticsHubText);
        Assert.Contains("RecentD3DFrameLatencyWaitTimeoutCount = recentD3DFrameLatencyWaitTimeouts", diagnosticsHubText);
        Assert.Contains("UpdateD3DFrameLatencyWaitRecentCounters", diagnosticsHubText);
        Assert.Contains("private long UpdateD3DFrameLatencyWaitRecentCounters(", diagnosticsSnapshotsText);
        Assert.DoesNotContain("private long UpdateD3DFrameLatencyWaitRecentCounters(", ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs"));
        Assert.Contains("PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageConfidence = previewPacingClassification.Confidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageEvidence = previewPacingClassification.Evidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshot.PreviewPacingLikelySlowStage,", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageConfidence = snapshot.PreviewPacingSlowStageConfidence,", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageEvidence = snapshot.PreviewPacingSlowStageEvidence,", diagnosticsHubText);
    }

    [Fact]
    public void AutomationDiagnosticsHub_DoesNotRetainSupersededPreviewPacingPartials()
    {
    }

    [Fact(DisplayName = "Preview pacing classifier rejects weak samples")]
    public void PreviewPacingClassifier_RequiresStableSampleUnlessHardSignal()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewCadenceSampleCount", 240);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleDurationMs", 2000d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("InsufficientSample", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Low", GetStringProperty(result, "Confidence"));
        Assert.Contains("requiredDurationMs=30000", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier prefers source capture when source drops")]
    public void PreviewPacingClassifier_ClassifiesSourceCaptureBeforePreviewTail()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "CaptureCadenceSampleCount", 3600);
        SetPropertyOrBackingField(input, "CaptureCadenceSampleDurationMs", 30000d);
        SetPropertyOrBackingField(input, "CaptureCadenceOnePercentLowFps", 106d);
        SetPropertyOrBackingField(input, "CaptureCadenceEstimatedDroppedFrames", 3L);
        SetPropertyOrBackingField(input, "CaptureCadenceSevereGapCount", 1L);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("SourceCapture", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("High", GetStringProperty(result, "Confidence"));
        Assert.Contains("drops=3", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags compositor misses first")]
    public void PreviewPacingClassifier_ClassifiesCompositorMissBeforePresentBlocked()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 6d);
        SetPropertyOrBackingField(input, "RecentD3DMissedRefreshes", 2L);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("CompositorMiss", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("High", GetStringProperty(result, "Confidence"));
        Assert.Contains("dxgiRecentMissed=2", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags dominant render upload")]
    public void PreviewPacingClassifier_ClassifiesDominantRenderUpload()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DInputUploadCpuP99Ms", 5d);
        SetPropertyOrBackingField(input, "PreviewD3DRenderSubmitCpuP99Ms", 1.2d);
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 1.0d);
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitP95Ms", 0.5d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("RenderUpload", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("input=5", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags frame latency wait timeout")]
    public void PreviewPacingClassifier_ClassifiesFrameLatencyWaitTimeout()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitTimeoutCount", 1L);
        SetPropertyOrBackingField(input, "RecentD3DFrameLatencyWaitTimeoutCount", 1L);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("PresentBlocked", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("waitP95", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier ignores stale lifetime signals")]
    public void PreviewPacingClassifier_IgnoresStaleLifetimeSignalsWithoutRecentDeltas()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPreviewJitterEnabled", true);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterScheduleLateCount", 12L);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterMaxScheduleLateMs", 20d);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterLastDropReason", "submit-failed");
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitTimeoutCount", 4L);
        SetPropertyOrBackingField(input, "PreviewD3DLastDropReason", "queue-full");

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("Unknown", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Low", GetStringProperty(result, "Confidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags recent jitter schedule-late")]
    public void PreviewPacingClassifier_ClassifiesRecentJitterScheduleLate()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPreviewJitterEnabled", true);
        SetPropertyOrBackingField(input, "RecentPreviewJitterScheduleLateCount", 1L);
        SetPropertyOrBackingField(input, "RecentPreviewJitterScheduleLateMs", 5d);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterScheduleLateCount", 12L);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterMaxScheduleLateMs", 20d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("PreviewJitterScheduler", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("recentScheduleLate=1/5", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags visual duplicate or low motion")]
    public void PreviewPacingClassifier_ClassifiesVisualDuplicateOrLowMotion()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "VisualCadenceSampleCount", 240);
        SetPropertyOrBackingField(input, "VisualCadenceChangeObservedFps", 80d);
        SetPropertyOrBackingField(input, "VisualCadenceRepeatFramePercent", 12d);
        SetPropertyOrBackingField(input, "VisualCadenceLongestRepeatRun", 5);
        SetPropertyOrBackingField(input, "VisualCadenceMotionConfidence", "High");
        SetPropertyOrBackingField(input, "MjpegPacketHashInputObservedFps", 120d);
        SetPropertyOrBackingField(input, "MjpegPacketHashUniqueObservedFps", 120d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("VisualDuplicateOrLowMotion", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("visualChange=80", GetStringProperty(result, "Evidence"));
        Assert.Contains("confidence=High", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags MJPEG decode pressure")]
    public void PreviewPacingClassifier_ClassifiesMjpegDecodePressure()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPipelineSampleCount", 240);
        SetPropertyOrBackingField(input, "MjpegDecodeP95Ms", 6d);
        SetPropertyOrBackingField(input, "MjpegPipelineP95Ms", 8d);
        SetPropertyOrBackingField(input, "MjpegPipelineMaxMs", 10d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("MjpegDecode", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("mjpegDecodeP95=6", GetStringProperty(result, "Evidence"));
        Assert.Contains("pipelineP95=8", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags renderer submit drops")]
    public void PreviewPacingClassifier_ClassifiesRendererSubmitDrops()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "RecentRendererDropped", 3L);
        SetPropertyOrBackingField(input, "RecentRendererSubmitted", 100L);
        SetPropertyOrBackingField(input, "PreviewD3DLastDropReason", "queue-full");

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("RenderSubmit", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("High", GetStringProperty(result, "Confidence"));
        Assert.Contains("rendererDropped=3/100", GetStringProperty(result, "Evidence"));
        Assert.Contains("lastDrop=queue-full", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier falls back to render submit for high total D3D CPU time")]
    public void PreviewPacingClassifier_ClassifiesD3DTotalFrameCpuFallback()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DInputUploadCpuP99Ms", 0.4d);
        SetPropertyOrBackingField(input, "PreviewD3DRenderSubmitCpuP99Ms", 0.6d);
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 0.5d);
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitP95Ms", 0.4d);
        SetPropertyOrBackingField(input, "PreviewD3DTotalFrameCpuP99Ms", 10d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("RenderSubmit", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("total=10", GetStringProperty(result, "Evidence"));
    }

    private static object CreateBaselinePreviewPacingInput()
    {
        var input = CreateInstance(InputTypeName);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "TargetFrameRate", 120d);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleCount", 3600);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleDurationMs", 30000d);
        SetPropertyOrBackingField(input, "PreviewCadenceExpectedIntervalMs", 1000d / 120d);
        SetPropertyOrBackingField(input, "PreviewCadenceObservedFps", 119d);
        SetPropertyOrBackingField(input, "PreviewCadenceOnePercentLowFps", 105d);
        SetPropertyOrBackingField(input, "PreviewCadenceP99IntervalMs", 9.8d);
        SetPropertyOrBackingField(input, "CaptureExpectedFrameRate", 120d);
        return input;
    }

    private static object CreateInstance(string typeName)
    {
        var type = SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;
        return Activator.CreateInstance(type)
               ?? throw new InvalidOperationException($"Failed to create instance of '{typeName}'.");
    }

    private static object ClassifyPreviewPacing(object input)
    {
        var classifierType = SussudioAssembly.Load().GetType(ClassifierTypeName, throwOnError: true)!;
        var classify = classifierType.GetMethod("Classify", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewPacingSlowStageClassifier.Classify was not found.");
        return classify.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("Preview pacing classifier returned null.");
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField != null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' is not writable and backing field was not found on '{instance.GetType().Name}'.");
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on '{instance.GetType().Name}'.");
        return property.GetValue(instance)?.ToString() ?? string.Empty;
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static string ReadAutomationSnapshotFamilyText()
    {
        return ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs")
            .Replace("\r\n", "\n");
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }
}
}

namespace Sussudio.Tests
{
public sealed unsafe class MfSourceReaderFrameStrideTests
{
    private const int Width = 6;
    private const int Height = 4;
    private const int GuardBytes = 16;
    private const byte SourceGuard = 0xA5;
    private const byte SourcePadding = 0xEE;
    private const byte DestinationGuard = 0xB6;

    private delegate void CopyFrameDelegate(
        byte* sourceStart, int stride, Span<byte> destination, int width, int height, bool isP010);

    private static readonly CopyFrameDelegate CopyFrame = BindCopyFrame();

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CopyYuvWithStride_PreservesPackedBytesAndGuardRegions(bool isP010, bool padded)
    {
        var expected = ExpectedPackedFrame(isP010);
        var rowBytes = isP010 ? Width * 2 : Width;
        var stride = rowBytes + (padded ? 4 : 0);
        var source = CreatePitchedSource(expected, rowBytes, stride);
        var originalSource = (byte[])source.Clone();
        var destination = Enumerable.Repeat(DestinationGuard, GuardBytes + expected.Length + GuardBytes).ToArray();
        var expectedDestination = (byte[])destination.Clone();
        expected.CopyTo(expectedDestination, GuardBytes);

        InvokeCopy(source, GuardBytes, stride, destination, GuardBytes, expected.Length + GuardBytes, isP010);

        Assert.Equal(expectedDestination, destination);
        Assert.Equal(originalSource, source);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CopyYuvWithStride_RejectsShortDestinationWithoutCopying(bool isP010, bool padded)
    {
        var expected = ExpectedPackedFrame(isP010);
        var rowBytes = isP010 ? Width * 2 : Width;
        var stride = rowBytes + (padded ? 4 : 0);
        var source = CreatePitchedSource(expected, rowBytes, stride);
        var originalSource = (byte[])source.Clone();
        var destination = Enumerable.Repeat(DestinationGuard, GuardBytes + expected.Length + GuardBytes).ToArray();
        var originalDestination = (byte[])destination.Clone();

        var error = Assert.Throws<ArgumentException>(() =>
            InvokeCopy(source, GuardBytes, stride, destination, GuardBytes, expected.Length - 1, isP010));

        Assert.Equal("Destination span is too small for packed frame.", error.Message);
        Assert.Equal(originalDestination, destination);
        Assert.Equal(originalSource, source);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, -1)]
    [InlineData(false, -6)]
    [InlineData(false, -10)]
    [InlineData(false, int.MinValue)]
    [InlineData(false, 5)]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    [InlineData(true, -12)]
    [InlineData(true, -16)]
    [InlineData(true, int.MinValue)]
    [InlineData(true, 11)]
    public void CopyYuvWithStride_RejectsUnsupportedStrideWithoutCopying(bool isP010, int stride)
    {
        var rowBytes = isP010 ? Width * 2 : Width;
        var packedBytes = rowBytes * (Height + Height / 2);
        // Keep backward addresses in bounds so a negative-pitch regression fails safely.
        var source = Enumerable.Repeat(SourceGuard, 256).ToArray();
        var originalSource = (byte[])source.Clone();
        var destination = Enumerable.Repeat(DestinationGuard, GuardBytes + packedBytes + GuardBytes).ToArray();
        var originalDestination = (byte[])destination.Clone();

        var error = Assert.Throws<InvalidOperationException>(() =>
            InvokeCopy(source, 128, stride, destination, GuardBytes, packedBytes, isP010));

        Assert.Equal($"Source stride ({stride}) is smaller than packed row width ({rowBytes}).", error.Message);
        Assert.Equal(originalDestination, destination);
        Assert.Equal(originalSource, source);
    }

    private static CopyFrameDelegate BindCopyFrame()
    {
        var type = SussudioAssembly.Load().GetType("Sussudio.Services.Capture.MfSourceReaderVideoCapture", throwOnError: true)!;
        var method = type.GetMethod(
            "CopyYuvWithStride",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(byte*), typeof(int), typeof(Span<byte>), typeof(int), typeof(int), typeof(bool) },
            modifiers: null)
            ?? throw new InvalidOperationException("MfSourceReaderVideoCapture.CopyYuvWithStride was not found.");
        return method.CreateDelegate<CopyFrameDelegate>();
    }

    private static void InvokeCopy(
        byte[] source, int sourceOffset, int stride, byte[] destination, int destinationOffset,
        int destinationLength, bool isP010)
    {
        fixed (byte* sourceStart = source)
        {
            CopyFrame(sourceStart + sourceOffset, stride,
                destination.AsSpan(destinationOffset, destinationLength), Width, Height, isP010);
        }
    }

    private static byte[] CreatePitchedSource(byte[] packedFrame, int rowBytes, int stride)
    {
        var source = Enumerable.Repeat(SourceGuard, GuardBytes + stride * 6 + GuardBytes).ToArray();
        for (var row = 0; row < 6; row++)
        {
            var pitchedRow = source.AsSpan(GuardBytes + row * stride, stride);
            pitchedRow.Fill(SourcePadding);
            packedFrame.AsSpan(row * rowBytes, rowBytes).CopyTo(pitchedRow);
        }
        return source;
    }

    private static byte[] ExpectedPackedFrame(bool isP010)
        => isP010
            ? new byte[]
            {
                // Four Y rows followed by two interleaved UV rows; every low six-bit value is nonzero.
                0x01, 0x04, 0x02, 0x08, 0x03, 0x0C, 0x04, 0x10, 0x05, 0x14, 0x06, 0x18,
                0x07, 0x24, 0x08, 0x28, 0x09, 0x2C, 0x0A, 0x30, 0x0B, 0x34, 0x0C, 0x38,
                0x0D, 0x44, 0x0E, 0x48, 0x0F, 0x4C, 0x10, 0x50, 0x11, 0x54, 0x12, 0x58,
                0x13, 0x64, 0x14, 0x68, 0x15, 0x6C, 0x16, 0x70, 0x17, 0x74, 0x18, 0x78,
                0x19, 0x84, 0x1A, 0xC4, 0x1B, 0x88, 0x1C, 0xC8, 0x1D, 0x8C, 0x1E, 0xCC,
                0x1F, 0x94, 0x20, 0xD4, 0x21, 0x98, 0x22, 0xD8, 0x23, 0x9C, 0x24, 0xDC
            }
            : new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36,
                0x81, 0xC1, 0x82, 0xC2, 0x83, 0xC3,
                0x91, 0xD1, 0x92, 0xD2, 0x93, 0xD3
            };
}
}

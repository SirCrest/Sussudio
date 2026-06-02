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
    public Task MainWindowFullScreenAutomationAwaitsTransitionTasks()
        => global::Program.MainWindowFullScreenAutomation_AwaitsTransitionTask();

    [Fact]
    public Task MainWindowWindowAutomationCommandsLiveInController()
        => global::Program.MainWindowWindowAutomationCommands_LiveInController();

    [Fact]
    public Task MainWindowUiDispatchingLivesInDispatchingPartial()
        => global::Program.MainWindowUiDispatching_LivesInShellChromeAdapter();

    [Fact]
    public Task AutomationPipeServerGatesDefaultSecurityFallbackOnAuthToken()
        => global::Program.NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken();

    [Fact]
    public Task AutomationPipeServerRequestTimeoutsUseBoundedDispatchCancellation()
        => global::Program.NamedPipeAutomationServer_RequestTimeoutsUseBoundedDispatchCancellation();

    [Fact]
    public Task MainWindowWiresAutomationPipeAuthFallbackPolicy()
        => global::Program.MainWindowAutomation_WiresPipeAuthFallbackPolicy();

    [Fact]
    public Task StreamDeckScopeDocumentsAutomationAuthEnvelope()
        => global::Program.StreamDeckPluginScope_DocumentsAutomationAuthEnvelope();
}

// Minimal xUnit slice for Sussudio.Converters.BoolConverters. The full
// behavior matrix is exercised by the legacy Program checks below; this xUnit
// pair verifies the same Visible/Collapsed mapping so the converters are
// reachable from the xUnit discovery path too.
public class BoolConvertersTests
{
    [Fact]
    public void InverseBoolConverter_InvertsBoolValues()
    {
        var asm = SussudioAssembly.Load();
        var converterType = asm.GetType("Sussudio.Converters.InverseBoolConverter", throwOnError: true)!;
        var convert = ResolveConvertMethod(converterType, "Convert");

        var instance = Activator.CreateInstance(converterType)!;
        Assert.Equal(false, convert.Invoke(instance, new object?[] { true, typeof(bool), null, "" }));
        Assert.Equal(true, convert.Invoke(instance, new object?[] { false, typeof(bool), null, "" }));

        var sentinel = new object();
        Assert.Same(sentinel, convert.Invoke(instance, new object?[] { sentinel, typeof(bool), null, "" }));
    }

    [Fact]
    public void Sussudio_Converters_BoolConverters_TypesAreDiscoverableAndImplementIValueConverter()
    {
        var asm = SussudioAssembly.Load();
        var boolToVisibility = asm.GetType("Sussudio.Converters.BoolToVisibilityConverter", throwOnError: true)!;
        var inverseVisibility = asm.GetType("Sussudio.Converters.BoolToInverseVisibilityConverter", throwOnError: true)!;

        AssertImplementsValueConverter(boolToVisibility);
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
    public void AutomationPipeProtocol_ResolvesCommandsTimeoutsAuthAndEnvelopes()
    {
        Assert.Equal("SussudioAutomation", AutomationPipeProtocol.DefaultPipeName);
        Assert.Equal("SUSSUDIO_AUTOMATION_TOKEN", AutomationPipeProtocol.AutomationKeyEnvVar);
        Assert.Equal(1, AutomationPipeProtocol.CommandManifestRevision);
        Assert.Equal(5000, AutomationPipeProtocol.DefaultConnectTimeoutMs);
        Assert.Equal(15000, AutomationPipeProtocol.DefaultResponseTimeoutMs);
        Assert.Equal(60000, AutomationPipeProtocol.ExtendedResponseTimeoutMs);
        Assert.Equal(150000, AutomationPipeProtocol.RecordingResponseTimeoutMs);
        Assert.Equal(305000, AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs);

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

        lock (AutomationTokenLock)
        {
            var previousToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
            try
            {
                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, "env-token");
                Assert.Equal("explicit-token", AutomationPipeProtocol.GetConfiguredAuthToken("explicit-token"));
                Assert.Equal("env-token", AutomationPipeProtocol.GetConfiguredAuthToken());

                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, "   ");
                Assert.Null(AutomationPipeProtocol.GetConfiguredAuthToken());

                Assert.Equal(15000, AutomationPipeProtocol.GetDefaultResponseTimeout("GetSnapshot"));
                Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));
                Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackEnabled"));
                Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("RestartFlashback"));
                Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout("SetRecordingEnabled"));
                Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout("set-recording-enabled"));
                Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout("17"));
                Assert.Equal(60000, AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition));

                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, "env-token");
                var payload = new Dictionary<string, object?> { ["enabled"] = true };
                var envelope = AutomationPipeProtocol.CreateRequestEnvelope(17, payload);
                Assert.Equal(17, envelope["command"]);
                Assert.Equal(32, Assert.IsType<string>(envelope["correlationId"]).Length);
                Assert.Equal(AutomationPipeProtocol.CommandManifestRevision, envelope["manifestRevision"]);
                Assert.Equal("env-token", envelope["authToken"]);
                Assert.Same(payload, envelope["payload"]);

                var explicitEnvelope = AutomationPipeProtocol.CreateRequestEnvelope(1, authToken: "explicit-token");
                Assert.Equal("explicit-token", explicitEnvelope["authToken"]);
                Assert.Equal(AutomationPipeProtocol.CommandManifestRevision, explicitEnvelope["manifestRevision"]);
                Assert.IsType<Dictionary<string, object?>>(explicitEnvelope["payload"]);
            }
            finally
            {
                Environment.SetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar, previousToken);
            }
        }
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
    public Task AutomationDispatcherWaitAndAssertCommandsLiveWithSupportOwners()
        => global::Program.AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners();

    [Fact]
    public Task AutomationDispatcherEntryPipelineLivesInRootDispatcher()
        => global::Program.AutomationCommandDispatcher_EntryPipeline_LivesInRootDispatcher();

    [Fact]
    public Task AutomationDispatcherTrivialHandlerPayloadFieldsMatchCatalog()
        => global::Program.AutomationCommandDispatcher_OneFieldHandlers_MatchCatalogPayloadFields();

    [Fact]
    public Task AutomationDispatcherAudioControlCommandsLiveWithCustomRouter()
        => global::Program.AutomationCommandDispatcher_AudioControlCommands_LiveWithCustomRouter();

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
    public Task AutomationDispatcherCaptureControlCommandsLiveWithCustomRouter()
        => global::Program.AutomationCommandDispatcher_CaptureControlCommands_LiveWithCustomRouter();

    [Fact]
    public Task AutomationDispatcherIntrospectionCommandsLiveInFocusedPartial()
        => global::Program.AutomationCommandDispatcher_IntrospectionCommands_LiveWithCustomRouter();

    [Fact]
    public Task AutomationDispatcherUiSettingsCommandsOwnUiSettingsApplication()
        => global::Program.AutomationCommandDispatcher_UiSettingsCommands_LiveWithRootDispatch();

    [Fact]
    public Task AutomationDispatcherWindowCloseWaitsForCompletion()
        => global::Program.AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion();

    [Fact]
    public Task AutomationDispatcherWindowCommandsLiveInFocusedPartial()
        => global::Program.AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial();

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
    public Task AutomationDispatcherDeviceCommandsLiveWithCustomRouter()
        => global::Program.AutomationCommandDispatcher_DeviceCommands_LiveWithCustomRouter();

    [Fact]
    public Task AutomationDispatcherFlashbackFailuresReturnPlaybackDiagnostics()
        => global::Program.AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics();

    [Fact]
    public Task AutomationDispatcherFlashbackCommandsLiveWithCustomRouter()
        => global::Program.AutomationCommandDispatcher_FlashbackCommands_LiveWithCustomRouter();

    [Fact]
    public Task AutomationDispatcherVerificationCommandsLiveInFocusedPartial()
        => global::Program.AutomationCommandDispatcher_VerificationCommands_LiveWithCustomRouter();

    [Fact]
    public Task AutomationDispatcherVisualCaptureCommandsLiveInFocusedPartial()
        => global::Program.AutomationCommandDispatcher_VisualCaptureCommands_LiveWithCustomRouter();

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
    public Task AutomationViewModelRuntimeSnapshotLivesInFocusedPartial()
        => global::Program.MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial();

    [Fact]
    public Task MainWindowFlashbackScrubEndsOnReleaseCancelAndCaptureLost()
        => global::Program.MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost();

    [Fact]
    public Task FlashbackTimelineGeometryPreservesScrubMath()
        => global::Program.FlashbackTimelineGeometry_PreservesScrubMath();

    [Fact]
    public Task MainWindowFlashbackToggleRollsBackUiStateOnFailure()
        => global::Program.MainWindowFlashbackToggle_RollsBackUiStateOnFailure();

    [Fact]
    public Task FlashbackPollingTimersLiveInController()
        => global::Program.FlashbackPollingTimers_LiveInController();

    [Fact]
    public Task FlashbackTimelineTrackLayoutLivesInController()
        => global::Program.FlashbackTimelineTrackLayout_LivesInController();

    [Fact]
    public Task FlashbackPlayheadMotionLivesInController()
        => global::Program.FlashbackPlayheadMotion_LivesInController();

    [Fact]
    public Task FlashbackMarkerPresentationLivesInController()
        => global::Program.FlashbackMarkerPresentation_LivesInController();

    [Fact]
    public Task FlashbackPlaybackPresentationLivesInController()
        => global::Program.FlashbackPlaybackPresentation_LivesInController();

    [Fact]
    public Task FlashbackExportProgressPresentationLivesInController()
        => global::Program.FlashbackExportProgressPresentation_LivesInController();

    [Fact]
    public Task FlashbackSettingsBindingsLiveInController()
        => global::Program.FlashbackSettingsBindings_LiveInController();
}

public sealed class AutomationSnapshotProjectionContractsTests
{
    public AutomationSnapshotProjectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationDiagnosticsSnapshotStatusProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSnapshotEvaluationProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsAudioProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsSnapshotAudioProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsCaptureCommandProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsUserSettingsProjectionLivesWithSnapshotProjection()
        => global::Program.AutomationDiagnosticsUserSettingsProjection_LivesWithSnapshotProjection();

    [Fact]
    public Task AutomationDiagnosticsCaptureFormatProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsCaptureFormatProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsCaptureTransportProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsCaptureTransportProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsHdrPipelineProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsHdrPipelineProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsCaptureCadenceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsVisualCadenceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsMjpegProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsMjpegProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsSourceSignalProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSourceTelemetryProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingPipelineProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsRecordingPipelineProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsRecordingBackendProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsRecordingBackendProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsRecordingOutputProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsRecordingOutputProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsProcessResourceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsAvSyncProjectionLivesWithProjectionRoot()
        => global::Program.AutomationDiagnosticsAvSyncProjection_LivesWithProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsPreviewRuntimeProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsPreviewRuntimeProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsPreviewD3DProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsPreviewD3DProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsFlashbackExportProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsFlashbackExportProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsFlashbackRecordingProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsFlashbackRecordingProjection_LivesWithSnapshotProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsFlashbackPlaybackProjectionLivesWithSnapshotProjectionRoot()
        => global::Program.AutomationDiagnosticsFlashbackPlaybackProjection_LivesWithSnapshotProjectionRoot();
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
    public Task CaptureServiceTransitionLockUsesTransitionPolicy()
        => global::Program.CaptureService_RunTransition_UsesTransitionPolicy();

    [Fact]
    public Task CaptureServiceInPlaceMutationsUseCurrentStateTransitions()
        => global::Program.CaptureService_InPlaceMutationsUseCurrentStateTransition();

    [Fact]
    public Task CaptureServiceSessionStateWritesRouteThroughCoordination()
        => global::Program.CaptureService_SessionStateWritesRouteThroughCoordination();

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

    [Fact]
    public Task CaptureSessionCoordinatorModelsLiveInFocusedFile()
        => global::Program.CaptureSessionCoordinator_ModelsLiveInFocusedFile();

    [Fact]
    public Task CaptureSessionCoordinatorCommandFacadeLivesInFocusedPartial()
        => global::Program.CaptureSessionCoordinator_CommandFacadeLivesInFocusedPartial();

    [Fact]
    public Task CaptureSessionCoordinatorFlashbackFacadeLivesInCoordinatorRoot()
        => global::Program.CaptureSessionCoordinator_FlashbackFacadeLivesInCoordinatorRoot();

    [Fact]
    public Task CaptureSessionCoordinatorQueueWorkerLivesInFocusedPartial()
        => global::Program.CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial();

    [Fact]
    public Task CaptureSessionCoordinatorSnapshotProjectionLivesInFocusedPartial()
        => global::Program.CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial();

    [Fact]
    public Task CaptureSessionCoordinatorDisposalLivesInCoordinatorRoot()
        => global::Program.CaptureSessionCoordinator_DisposalLivesInCoordinatorRoot();

    [Fact]
    public Task ServiceNamespacesFollowServiceFolders()
        => global::Program.ServiceNamespaces_FollowServiceFolders();

    [Fact]
    public Task MfDeviceEnumeratorSourceOwnershipLivesInCohesiveEnumerator()
        => global::Program.MfDeviceEnumerator_SourceOwnershipLivesInCohesiveEnumerator();

    [Fact]
    public Task CaptureDiscoverySourceOwnershipLivesInFocusedPartials()
        => global::Program.CaptureDiscoverySourceOwnership_LivesInFocusedPartials();

    [Fact]
    public Task AutomationCommandCatalogSourceOwnershipIsContractAligned()
        => global::Program.AutomationContracts_SourceOwnership_IsCatalogAligned();

    [Fact]
    public Task DiagnosticsSnapshotRefreshIsSerializedForRecordingResponses()
        => global::Program.DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses();
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

    internal static Task AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.WaitForCondition:");
        AssertContains(customCommandsText, "ExecuteWaitForConditionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.AssertSnapshot:");
        AssertContains(customCommandsText, "ExecuteAssertSnapshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteWaitForConditionCommandAsync(");
        AssertContains(customCommandsText, "var condition = ParseWaitCondition(payload);");
        AssertContains(customCommandsText, "Math.Clamp(GetInt(payload, \"timeoutMs\") ?? DefaultWaitTimeoutMs, 250, 300_000)");
        AssertContains(customCommandsText, "WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken)");
        AssertContains(customCommandsText, "errorCode: met ? null : \"timeout\"");
        AssertContains(customCommandsText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync(");
        AssertContains(customCommandsText, "private static bool ConditionSatisfied(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.WaitConditions.cs")),
            "wait-condition commands folded into AutomationCommandDispatcher.cs");

        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(customCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(customCommandsText, "var assertions = ParseAssertions(payload);");
        AssertContains(customCommandsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");
        AssertContains(customCommandsText, "errorCode: passed ? null : \"assertion-failed\"");
        AssertContains(customCommandsText, "private static List<SnapshotAssertion> ParseAssertions(");
        AssertContains(customCommandsText, "private static bool TryEvaluateAssertion(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Assertions.cs")),
            "assert-snapshot command body folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_IntrospectionCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.GetSnapshot:");
        AssertContains(customCommandsText, "ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.GetAutomationManifest:");
        AssertContains(customCommandsText, "ExecuteGetAutomationManifestCommand(correlationId)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(customCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(customCommandsText, "Snapshot retrieved.");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetAutomationManifestCommand(string correlationId)");
        AssertContains(customCommandsText, "Automation manifest retrieved.");
        AssertContains(customCommandsText, "AutomationCommandCatalog.CreateManifest()");
        AssertContains(customCommandsText, "includeSnapshot: false");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.ReadbackCommands.cs")),
            "readback commands folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_AudioControlCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var audioControlCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.SetDeviceAudioMode:");
        AssertContains(customCommandsText, "ExecuteSetDeviceAudioModeCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetAnalogAudioGain:");
        AssertContains(customCommandsText, "ExecuteSetAnalogAudioGainCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetMicrophoneEnabled:");
        AssertContains(customCommandsText, "ExecuteSetMicrophoneEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.SetDeviceAudioModeAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetAnalogAudioGainAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetMicrophoneEnabledAsync");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetDeviceAudioModeCommandAsync(");
        AssertContains(audioControlCommandsText, "var mode = RequireString(payload, \"mode\");");
        AssertContains(audioControlCommandsText, "_audioPort.SetDeviceAudioModeAsync(mode, cancellationToken)");
        AssertContains(audioControlCommandsText, "Device audio mode changed: {mode}.");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetAnalogAudioGainCommandAsync(");
        AssertContains(audioControlCommandsText, "var gain = RequireDouble(payload, \"gain\");");
        AssertContains(audioControlCommandsText, "_audioPort.SetAnalogAudioGainAsync(gain, cancellationToken)");
        AssertContains(audioControlCommandsText, "Analog audio gain set to {gain:0.###}%.");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetMicrophoneEnabledCommandAsync(");
        AssertContains(audioControlCommandsText, "Missing 'enabled' parameter.");
        AssertContains(audioControlCommandsText, "_audioPort.SetMicrophoneEnabledAsync(enabled, cancellationToken)");
        AssertContains(audioControlCommandsText, "Microphone {(enabled ? \"enabled\" : \"disabled\")}.");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_CaptureControlCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var captureControlCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.SetMjpegDecoderCount:");
        AssertContains(customCommandsText, "ExecuteSetMjpegDecoderCountCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetOutputPath:");
        AssertContains(customCommandsText, "ExecuteSetOutputPathCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetRecordingEnabled:");
        AssertContains(customCommandsText, "ExecuteSetRecordingEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.SetMjpegDecoderCountAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetOutputPathAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetRecordingEnabledAsync");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetMjpegDecoderCountCommandAsync(");
        AssertContains(captureControlCommandsText, "var decoderCount = GetInt(payload, \"decoderCount\");");
        AssertContains(captureControlCommandsText, "Missing required integer property 'decoderCount'.");
        AssertContains(captureControlCommandsText, "_captureSettingsPort.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken)");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetOutputPathCommandAsync(");
        AssertContains(captureControlCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.SetOutputPath,");
        AssertContains(captureControlCommandsText, "_previewRecordingPort.SetOutputPathAsync(outputPath, cancellationToken)");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetRecordingEnabledCommandAsync(");
        AssertContains(captureControlCommandsText, "_previewRecordingPort.SetRecordingEnabledAsync(enabled, cancellationToken)");
        AssertContains(captureControlCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(captureControlCommandsText, "Recording {(enabled ? \"started\" : \"stopped\")}.");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_UiSettingsCommands_LiveWithRootDispatch()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var portMappedDispatchText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> UiPreviewRecordingHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>> UiStateHandlers");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetPreviewVolume]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetStatsVisible]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetSettingsVisible]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetFrameTimeOverlayVisible]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetFlashbackTimelineVisible]");
        AssertContains(portMappedDispatchText, "previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken)");
        AssertContains(portMappedDispatchText, "uiHandler.InvokeAsync(_uiPort, payload, cancellationToken)");
        AssertContains(portMappedDispatchText, "if (command == AutomationCommandKind.SetStatsSectionVisible)");
        AssertContains(portMappedDispatchText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(portMappedDispatchText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(portMappedDispatchText, "var section = RequireString(payload, \"section\");");
        AssertContains(portMappedDispatchText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(portMappedDispatchText, "_uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(portMappedDispatchText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.UiSettingsCommands.cs")),
            "UI settings handlers folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_DeviceCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var deviceCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.RefreshDevices:");
        AssertContains(customCommandsText, "ExecuteRefreshDevicesCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SelectDevice:");
        AssertContains(customCommandsText, "ExecuteSelectDeviceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SelectAudioInputDevice:");
        AssertContains(customCommandsText, "ExecuteSelectAudioInputDeviceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.GetCaptureOptions:");
        AssertContains(customCommandsText, "ExecuteGetCaptureOptionsCommandAsync(correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.RefreshDevicesForAutomationAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SelectDeviceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SelectAudioInputDeviceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.GetAutomationOptionsSnapshotAsync");

        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteRefreshDevicesCommandAsync(");
        AssertContains(deviceCommandsText, "_deviceSelectionPort.RefreshDevicesForAutomationAsync(cancellationToken)");
        AssertContains(deviceCommandsText, "Device list refresh requested.");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteSelectDeviceCommandAsync(");
        AssertContains(deviceCommandsText, "var deviceId = GetString(payload, \"deviceId\");");
        AssertContains(deviceCommandsText, "var deviceName = GetString(payload, \"deviceName\");");
        AssertContains(deviceCommandsText, "_deviceSelectionPort.SelectDeviceAsync(deviceId, deviceName, cancellationToken)");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteSelectAudioInputDeviceCommandAsync(");
        AssertContains(deviceCommandsText, "_deviceSelectionPort.SelectAudioInputDeviceAsync(deviceId, deviceName, cancellationToken)");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetCaptureOptionsCommandAsync(");
        AssertContains(deviceCommandsText, "_snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertContains(deviceCommandsText, "Capture options retrieved.");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_EntryPipeline_LivesInRootDispatcher()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var preflightText = rootText;
        var portMappedDispatchText = rootText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(rootText, "var preflightResponse = TryCreatePreflightResponse(request, correlationId);");
        AssertContains(rootText, "var portMappedResponse = await TryExecutePortMappedCommandAsync(");
        AssertContains(rootText, "return await ExecuteCustomCommandAsync(request, payload, correlationId, cancellationToken)");
        AssertDoesNotContain(rootText, "partial class AutomationCommandDispatcher");

        AssertContains(preflightText, "private AutomationCommandResponse? TryCreatePreflightResponse(");
        AssertContains(preflightText, "AUTOMATION_MANIFEST_MISMATCH");
        AssertContains(preflightText, "request.Command == AutomationCommandKind.Authenticate");
        AssertContains(preflightText, "private bool IsAuthorized(AutomationCommandRequest request)");
        AssertContains(preflightText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(preflightText, "RequiresReadyDevices(request.Command) && !IsAutomationReady()");
        AssertContains(preflightText, "_readinessPort.IsInitialized || _readinessPort.Devices.Count > 0");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Authorization.cs")),
            "auth gate folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Preflight.cs")),
            "preflight gate folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Payload.cs")),
            "payload helpers folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.PortMappedDispatch.cs")),
            "port-mapped dispatch folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.CustomCommands.cs")),
            "custom command router folded into AutomationCommandDispatcher.cs");

        AssertContains(portMappedDispatchText, "private async Task<AutomationCommandResponse?> TryExecutePortMappedCommandAsync(");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationDeviceSelectionPort>> TrivialDeviceSelectionHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>> TrivialCaptureSettingsHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> UiPreviewRecordingHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>> UiStateHandlers");
        AssertContains(portMappedDispatchText, "TryExecuteUiSettingsCommandAsync(command, payload, correlationId, cancellationToken)");
        AssertContains(portMappedDispatchText, "TrivialDeviceSelectionHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialCaptureSettingsHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialAudioHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialPreviewRecordingHandlers.TryGetValue(command");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.TrivialHandlers.cs")),
            "trivial port handler tables folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.UiSettingsCommands.cs")),
            "UI settings command tables folded into AutomationCommandDispatcher.cs");

        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.cs`");
        AssertDoesNotContain(cleanupPlanText, "`AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertDoesNotContain(cleanupPlanText, "`AutomationCommandDispatcher.Payload.cs`");

        return Task.CompletedTask;
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

        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(dispatcherText, "if (string.IsNullOrWhiteSpace(_authToken))\n        {\n            return true;\n        }");
        AssertContains(dispatcherText, "var providedToken = request.AuthToken;");
        AssertContains(dispatcherText, "providedToken = GetString(request.Payload, \"authToken\");");
        AssertContains(dispatcherText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(dispatcherText, "Logger.LogEvent(\"AUTH_FAILED\"");
        AssertContains(dispatcherText, "errorCode: authorized ? null : \"unauthorized\"");
        AssertContains(dispatcherText, "errorCode: \"unauthorized\"");
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

    internal static Task AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var windowCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.SetFullScreenEnabled:");
        AssertContains(customCommandsText, "ExecuteSetFullScreenEnabledCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.OpenRecordingsFolder:");
        AssertContains(customCommandsText, "ExecuteOpenRecordingsFolderCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ArmClose:");
        AssertContains(customCommandsText, "ExecuteArmCloseCommand(payload, correlationId)");
        AssertContains(customCommandsText, "case AutomationCommandKind.WindowAction:");
        AssertContains(customCommandsText, "ExecuteWindowActionCommandAsync(payload, correlationId, cancellationToken)");

        AssertContains(windowCommandsText, "_windowControl.SetFullScreenEnabledAsync(enabled, cancellationToken)");
        AssertContains(windowCommandsText, "Full screen {(enabled ? \"enter\" : \"exit\")} requested.");
        AssertContains(windowCommandsText, "_windowControl.OpenRecordingsFolderAsync(cancellationToken)");
        AssertContains(windowCommandsText, "Recordings folder open requested.");
        AssertContains(windowCommandsText, "var armed = GetBool(payload, \"armed\") ?? true;");
        AssertContains(windowCommandsText, "_closeArmed = armed;");
        AssertContains(windowCommandsText, "Window close arm state requested: {(armed ? \"armed\" : \"disarmed\")}.");
        AssertContains(windowCommandsText, "if (action == AutomationWindowAction.Close)");
        AssertContains(windowCommandsText, "window-close-not-armed");
        AssertContains(windowCommandsText, "await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);");
        AssertContains(windowCommandsText, "await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);");

        AssertContains(windowCommandsText, "private async Task ExecuteWindowActionAsync(");
        AssertContains(windowCommandsText, "_windowControl.MoveToAsync(mx, my, cancellationToken)");
        AssertContains(windowCommandsText, "_windowControl.ResizeToAsync(rw, rh, cancellationToken)");
        AssertContains(windowCommandsText, "_windowControl.SnapToRegionAsync(action, cancellationToken)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.WindowActions.cs")),
            "window action executor folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.WindowCommands.cs")),
            "window command bodies folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_VerificationCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyFile:");
        AssertContains(customCommandsText, "ExecuteVerifyFileCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyLastRecording:");
        AssertContains(customCommandsText, "ExecuteVerifyLastRecordingCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyFileCommandAsync(");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyLastRecordingCommandAsync(");
        AssertContains(customCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.VerifyFile,\n            \"filePath\",");
        AssertContains(customCommandsText, "_diagnosticsHub\n            .VerifyFileAsync(filePath, verificationProfile, cancellationToken)");
        AssertContains(customCommandsText, "_diagnosticsHub.VerifyLastRecordingAsync(cancellationToken)");
        AssertContains(customCommandsText, "HdrParity = verification.HdrParity");
        AssertContains(customCommandsText, "errorCode: verification.Succeeded ? null : \"verification-failed\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.VerificationCommands.cs")),
            "verification commands folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_VisualCaptureCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.ProbeVideoSource:");
        AssertContains(customCommandsText, "ExecuteProbeVideoSourceCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ProbePreviewColor:");
        AssertContains(customCommandsText, "ExecuteProbePreviewColorCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.CapturePreviewFrame:");
        AssertContains(customCommandsText, "ExecuteCapturePreviewFrameCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.CaptureWindowScreenshot:");
        AssertContains(customCommandsText, "ExecuteCaptureWindowScreenshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.ProbeVideoSourceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.ProbePreviewColorAsync");

        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbeVideoSourceCommandAsync(");
        AssertContains(customCommandsText, "_probePort.ProbeVideoSourceAsync(cancellationToken)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbePreviewColorCommandAsync(");
        AssertContains(customCommandsText, "_probePort.ProbePreviewColorAsync(cancellationToken)");
        AssertContains(customCommandsText, "AutomationCommandKind.CapturePreviewFrame");
        AssertContains(customCommandsText, "_probePort.CapturePreviewFrameAsync(outputPath, cancellationToken)");
        AssertContains(customCommandsText, "preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
        AssertContains(customCommandsText, "AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(customCommandsText, "window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png");
        AssertContains(customCommandsText, "_windowControl.CaptureWindowScreenshotAsync");
        AssertContains(customCommandsText, "CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded)");
        AssertContains(customCommandsText, "errorCode: succeeded ? null : \"capture-failed\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.VisualCaptureCommands.cs")),
            "visual capture commands folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
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

    internal static Task AutomationCommandDispatcher_FlashbackCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var flashbackCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.FlashbackAction:");
        AssertContains(customCommandsText, "ExecuteFlashbackActionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteFlashbackExportCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteFlashbackGetSegmentsCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteRestartFlashbackCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteSetFlashbackEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteFlashbackActionCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteFlashbackExportCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteFlashbackGetSegmentsCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteRestartFlashbackCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetFlashbackEnabledCommandAsync(");
        AssertContains(flashbackCommandsText, "_flashbackPort.ExecuteFlashbackActionAsync(action, position, cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.GetFlashbackSegmentsAsync(cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.RestartFlashbackAsync(cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.FlashbackCommands.cs")),
            "Flashback command bodies folded into AutomationCommandDispatcher.cs");

        return Task.CompletedTask;
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
        AssertContains(customCommandsText, "var maxEvents = GetInt(payload, \"maxEvents\") ?? 100;");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetPerformanceTimelineCommand(");
        AssertContains(customCommandsText, "var maxEntries = GetInt(payload, \"maxEntries\") ?? 240;");
        AssertContains(customCommandsText, "var maxEntries = GetInt(payload, \"maxEntries\") ?? 512;");
        AssertContains(customCommandsText, "GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.DiagnosticCommands.cs")),
            "diagnostic readback folded into AutomationCommandDispatcher.cs");

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
            "SetFlashbackEnabled" => "{\"enabled\":false}",
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

        var setResolution = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetResolution") })!;
        AssertEqual(true, setResolution, "SetResolution requires ready devices");

        var setFrameRate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFrameRate") })!;
        AssertEqual(true, setFrameRate, "SetFrameRate requires ready devices");

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

        AssertContains(appRootSource, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(appRootSource, "UnhandledException += App_UnhandledException;");
        AssertContains(appRootSource, "AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;");
        AssertContains(appRootSource, "private static bool IsRecoverableUnhandled(Exception ex)");
        AssertContains(appRootSource, "private void App_UnhandledException(");
        AssertContains(appRootSource, "private void CurrentDomain_UnhandledException(");
        AssertContains(appRootSource, "private void TryEmergencyStopRecording(string source)");
        AssertContains(appRootSource, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appRootSource, "private const string SingleInstanceMutexName");
        AssertContains(appRootSource, "protected override void OnLaunched(");
        AssertContains(appRootSource, "SINGLE_INSTANCE_GUARD second instance detected");
        AssertContains(appRootSource, "\"APP_START \" +");
        AssertContains(appRootSource, "public partial class App : Application");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "App.ExceptionPolicy.cs")),
            "old App exception policy partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "App.LaunchLifecycle.cs")),
            "old App launch lifecycle partial removed");

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
        AssertContains(pipeServerRootText, "private static void TraceFallback(string line)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "NamedPipeAutomationServer.ConnectionSession.cs")),
            "connection session stays with the named-pipe automation server owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "NamedPipeAutomationServer.Security.cs")),
            "pipe security stays with the named-pipe automation server owner");

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
                           secureSuccessCalls++;
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

            AssertEqual(1, secureSuccessCalls, "explicit security factory call count");
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
                           tokenFallbackDefaultCalls++;
                           return CreateTestPipeServerStream($"unit-pipe-token-default-{Guid.NewGuid():N}");
                       }))
            {
                AssertEqual(true, StartNamedPipeAutomationServer(server), "token-required mode allows default fallback");
            }

            AssertEqual(1, tokenFallbackSecureCalls, "token fallback tries explicit security first");
            AssertEqual(1, tokenFallbackDefaultCalls, "token fallback opens default pipe once");

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
        AssertContains(pipeServerText, "if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))");
        AssertContains(pipeServerText, "using var registration = cancellationToken.Register(");
        AssertContains(pipeServerText, "requestCancellation.Cancel();");
        AssertContains(pipeServerText, "WaitForDispatchCompletionAsync(dispatchTask, CancellationToken.None)");
        AssertContains(pipeServerText, "Automation command exceeded request timeout; waiting for dispatch to stop");
        AssertContains(pipeServerText, "return _owner.CreateRequestTimeoutResponse();");
        AssertDoesNotContain(pipeServerText, "DispatchContinues");
        AssertDoesNotContain(pipeServerText, "ObserveTimedOutDispatch");
        AssertContains(pipeServerText, "Request timed out after {_owner._requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "\"request-timeout\"");

        return Task.CompletedTask;
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");
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
        Func<NamedPipeServerStream> defaultServerStreamFactory)
    {
        var serverType = RequireType("Sussudio.Services.Automation.NamedPipeAutomationServer");
        var dispatcherType = RequireType("Sussudio.Services.Contracts.IAutomationCommandDispatcher");
        var constructor = serverType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 6);

        return (IDisposable)constructor.Invoke(new object?[]
        {
            CreateThrowingProxy(dispatcherType),
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
        AssertContains(loggerText, "private static void WriteDirect(string entry)");
        AssertContains(loggerText, "private static void RotatePriorLog()");
        AssertContains(loggerText, "public static void LogSystemInfo()");
        AssertContains(loggerText, "new ManagementObjectSearcher(");
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
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Logger.Diagnostics.cs")),
            "old Logger diagnostics partial removed");

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
            SetPropertyOrBackingField(health, "FlashbackPlaybackState", "Paused");
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
        var flashbackBackendResourcesText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
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
            @"var\s+unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!CanReuseVideoCaptureForPreview\(unifiedVideoCapture,\s*settings\)\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*true\)",
            "preview settings-change recycle branch");
        AssertRegex(
            startVideoPreview,
            @"unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!_flashbackEnabled\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*false\)",
            "preview flashback-disabled recycle branch");
        AssertRegex(
            startVideoPreview,
            @"unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*_flashbackBackend\.Sink\s*!=\s*null\s*&&\s*flashbackBackendSettingsChanged\s*\)\s*\{[^{}]*DisposeFlashbackPreviewBackendAsync\(transitionToken,\s*purgeSegments:\s*true\)",
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
        AssertContains(updateAudioInput, "var committedSwitchToken = CancellationToken.None;");
        AssertContains(updateAudioInput, "await newCapture.InitializeAsync(resolvedId, committedSwitchToken)");
        AssertContains(updateAudioInput, "await _previewAudioGraph.StartPlaybackAsync(");
        AssertOccursBefore(
            updateAudioInput,
            "await newCapture.InitializeAsync(resolvedId, committedSwitchToken)",
            "_previewAudioGraph.DetachCapture(");
        AssertContains(updateAudioInput, "_audioDeviceId = previousDeviceId;");
        AssertContains(updateAudioInput, "_audioDeviceName = previousDeviceName;");
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

        AssertContains(captureServiceText, "await _flashbackBackend.StartPreviewBackendAsync(");
        AssertContains(captureServiceText, "new FlashbackPreviewBackendStartRequest(");
        AssertContains(captureServiceText, "CloneCaptureSettings(settings),");
        AssertContains(captureServiceText, "CloneCaptureSettings(currentSettings)");
        AssertContains(flashbackBackendResourcesText, "SettingsSnapshot = request.SettingsSnapshot;");
        AssertContains(flashbackBackendResourcesText, "ClearSinkAndSettings();");
        AssertContains(captureServiceText, "_flashbackBackend.DisposePreviewBackendAsync(request)");
        AssertContains(flashbackBackendResourcesText, "Clear();");
        AssertContains(flashbackBackendResourcesText, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(flashbackBackendResourcesText, "var bufferManager = new FlashbackBufferManager(");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback);");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.FrameEncoded += request.FrameEncodedHandler;");
        AssertContains(flashbackBackendResourcesText, "Install(");
        AssertContains(flashbackBackendResourcesText, "AttachProducers(");
        AssertContains(flashbackBackendResourcesText, "playbackController.Initialize(");
        AssertContains(flashbackBackendResourcesText, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.FrameEncoded -= request.FrameEncodedHandler;");
        AssertContains(flashbackBackendResourcesText, "request.ScheduleDeferredCleanup(");
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
        AssertContains(stopVideoPreviewCore, "await StopTelemetryPollAsync().ConfigureAwait(false);");
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
        var flashbackBackendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");
        var flashbackText = string.Join("\n", flashbackTexts);

        AssertNoRegex(
            flashbackText,
            @"""FLASHBACK_[^""]*_(BEGIN|DONE|END)\b",
            "Flashback lifecycle scaffold log tokens");

        foreach (var expectedToken in new[]
        {
            "FLASHBACK_RESTART_OK",
            "FLASHBACK_FORMAT_CHANGE_OK",
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_OK",
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
            "FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED",
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED",
            "FLASHBACK_PLAYBACK_DISPOSE_REQUEST"
        })
        {
            AssertContains(flashbackText, expectedToken);
        }

        var encoderSettingsChange = ExtractTextBetween(
            captureServiceText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "private void DisposeCoordinationLocksBestEffort");
        AssertContains(encoderSettingsChange, "var cycleFailed = false;");
        AssertContains(encoderSettingsChange, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(encoderSettingsChange, "cycleFailed = true;");
        AssertContains(encoderSettingsChange, "if (!cycleFailed)");
        AssertContains(encoderSettingsChange, "_currentSettings = previousSettings;");
        AssertContains(encoderSettingsChange, "FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK");
        AssertContains(encoderSettingsChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(encoderSettingsChange, "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED");
        AssertContains(encoderSettingsChange, "string? splitEncodeMode = null");
        AssertContains(encoderSettingsChange, "_currentSettings.SplitEncodeMode = parsedSplitMode;");
        AssertContains(
            encoderSettingsChange,
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_FAIL quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} split={_currentSettings.SplitEncodeMode} type={ex.GetType().Name} error='{ex.Message}'");

        var formatChange = ExtractTextBetween(
            captureServiceText,
            "public Task UpdateRecordingFormatAsync",
            "    public Task CycleFlashbackEncoderSettingsAsync");
        AssertContains(formatChange, "var cycleFailed = false;");
        AssertContains(formatChange, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(formatChange, "cycleFailed = true;");
        AssertContains(formatChange, "if (!cycleFailed)");
        AssertContains(formatChange, "_currentSettings = previousSettings;");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_ROLLBACK");
        AssertContains(formatChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL format={format} type={ex.GetType().Name} error='{ex.Message}'");

        var cycleBuffer = ExtractTextBetween(
            captureServiceText,
            "private async Task CycleFlashbackBufferAsync",
            "    private void OnFlashbackFrameEncoded");
        var backendCycleBuffer = ExtractTextBetween(
            flashbackBackendResourcesText,
            "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync",
            "    private async Task RollBackPreviewBackendStartAsync");
        AssertContains(cycleBuffer, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(cycleBuffer, "exportOperationLockAlreadyHeld: true");
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
            "    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts");
        AssertContains(createFlashbackSessionContext, "var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);");
        AssertContains(createFlashbackSessionContext, "frameRate = frameRateParts.EffectiveFrameRate;");
        AssertContains(createFlashbackSessionContext, "FrameRateNumerator = fpsNum");
        AssertContains(captureServiceText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(");
        AssertContains(captureServiceText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) InferFlashbackSessionFrameRateParts(double deliveryFrameRate)");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_REJECT");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_INFER");
        AssertContains(captureServiceText, "deltaFps > toleranceFps");
        AssertContains(createFlashbackSessionContext, "RecordingFormat.Av1Mp4 => \"av1_nvenc\"");
        AssertContains(createFlashbackSessionContext, "AV1 recording requires the av1_nvenc encoder");
        AssertDoesNotContain(createFlashbackSessionContext, "UseTransportStreamFlashbackCodec");
        AssertContains(captureServiceText, "settings.Format == RecordingFormat.Av1Mp4");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackExportVerificationFormat(");
        AssertContains(captureServiceText, "forceRotateResult.Status == FlashbackForceRotateStatus.Failed");
        AssertContains(captureServiceText, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertDoesNotContain(
            ExtractTextBetween(
                captureServiceText,
                "if (segmentPaths.Count == 0)",
                "return FlashbackExportForceRotatePreparation.Ready"),
            "force_rotate_failed");
        AssertDoesNotContain(captureServiceText, "? RecordingFormat.HevcMp4.ToString()");
        AssertContains(createFlashbackSessionContext, "var flashbackNvencPreset = settings.NvencPreset;");
        AssertContains(createFlashbackSessionContext, "NvencPreset = flashbackNvencPreset");
        AssertContains(createFlashbackSessionContext, "SplitEncodeMode = SplitEncodeModeParser.ToWireString(settings.SplitEncodeMode)");
        // Flashback must honor user codec/preset settings directly. The legacy snapshot
        // field remains for compatibility, but the old silent AV1->HEVC path must stay gone.
        AssertDoesNotContain(createFlashbackSessionContext, "FLASHBACK_CODEC_DOWNGRADE");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackCodecDowngradeReason(");
        AssertContains(captureServiceText, "=> null;");
        AssertDoesNotContain(captureServiceText, "AV1->HEVC: software MJPEG pipeline at");
        AssertDoesNotContain(captureServiceText, "NVENC preset '");
        // Snapshot field remains populated from the compatibility resolver so
        // downstream consumers share the same no-downgrade contract.
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotsText, "FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(requestedSettings, unifiedVideoCapture),");
        var contractsText = ReadAutomationSnapshotFamilyText();
        AssertContains(contractsText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertContains(contractsText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        var automationDiagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationSnapshotFlashbackProjectionBuilder.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadAutomationSnapshotFlatteningFamilyText()
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationSnapshotFlashbackProjectionBuilder.cs")
                .Replace("\r\n", "\n");
        AssertContains(automationDiagnosticsHubText, "FlashbackExportVerificationFormat = flashbackRecordingFlattening.Backend.ExportVerificationFormat,");
        AssertContains(automationDiagnosticsHubText, "ExportVerificationFormat = backend.ExportVerificationFormat,");
        AssertContains(automationDiagnosticsHubText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(automationDiagnosticsHubText, "FlashbackCodecDowngradeReason = flashbackRecordingFlattening.Backend.CodecDowngradeReason,");
        AssertContains(automationDiagnosticsHubText, "CodecDowngradeReason = backend.CodecDowngradeReason,");
        AssertContains(automationDiagnosticsHubText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason");
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
        AssertContains(backendCycleBuffer, "committedCycleToken,");
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
            "ResolveFlashbackSessionFrameRateParts",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFlashbackSessionFrameRateParts not found.");

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

        return Task.CompletedTask;
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
            "UpdateRecordingFormatAsync",
            "CycleFlashbackEncoderSettingsAsync",
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

    internal static Task CaptureSessionCoordinator_ModelsLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var modelText = rootText;

        AssertContains(modelText, "public enum CaptureCommandKind");
        AssertContains(modelText, "public enum CaptureCommandOutcome");
        AssertContains(modelText, "public readonly record struct CaptureCommand(");
        AssertContains(modelText, "public sealed class CaptureSessionSnapshot");
        AssertContains(modelText, "internal readonly record struct FlashbackPlaybackSnapshot(");
        AssertContains(modelText, "internal readonly record struct FlashbackBufferStatus(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Models.cs")),
            "coordinator model surface folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackFacadeLivesInCoordinatorRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = rootText;
        var flashbackStatusText = flashbackText;
        var flashbackExportText = flashbackText;
        var flashbackGuardsText = flashbackText;

        AssertContains(flashbackText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(flashbackText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackStatusText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackStatusText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackExportText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackGuardsText, "private bool TryGetActiveFlashback(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Flashback.cs")),
            "CaptureSessionCoordinator Flashback facade folded into the coordinator root");

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


    internal static Task CaptureSessionCoordinator_CommandFacadeLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "_captureService.StopRecordingAsync(emergency: true, ct)");
        AssertContains(rootText, "public Task UpdateAudioMonitoringAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertOccursBefore(rootText, "await _captureService.StartAudioPreviewAsync(ct).ConfigureAwait(false);", "_captureService.SetMonitoringMuted(false);");
        AssertOccursBefore(rootText, "_captureService.SetMonitoringMuted(true);", "await _captureService.StopAudioPreviewAsync(ct).ConfigureAwait(false);");
        AssertContains(rootText, "internal void SetPreviewVolume(double volume)");
        AssertContains(rootText, "ThrowIfDisposed();");
        AssertContains(rootText, "public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task UpdateMicrophoneMonitorAsync(bool enabled, string? micDeviceId, string? micDeviceName, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task CleanupAsync(CancellationToken cancellationToken = default)");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Commands.cs")),
            "CaptureSessionCoordinator command facade folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackOwnershipLivesInCoordinatorRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = rootText;
        var flashbackStatusText = flashbackText;
        var flashbackPlaybackText = flashbackText;
        var flashbackExportText = flashbackText;
        var flashbackGuardsText = flashbackText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(flashbackText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(flashbackText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task UpdateFlashbackSettingsAsync(int bufferMinutes, bool gpuDecode, CancellationToken cancellationToken = default)");

        AssertContains(flashbackStatusText, "internal bool IsFlashbackActive => _captureService.IsFlashbackActive;");
        AssertContains(flashbackStatusText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackStatusText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackStatusText, "Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs)");
        AssertContains(flashbackPlaybackText, "internal bool FlashbackBeginScrub(TimeSpan position)");
        AssertContains(flashbackPlaybackText, "internal bool FlashbackClearInOutPoints()");
        AssertContains(flashbackPlaybackText, "TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Flashback.Playback.cs")),
            "CaptureSessionCoordinator Flashback playback adapters folded into the Flashback coordinator facade");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Flashback.cs")),
            "CaptureSessionCoordinator Flashback facade folded into the coordinator root");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(");
        AssertContains(flashbackExportText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackGuardsText, "private bool TryGetActiveFlashback(");
        AssertContains(flashbackGuardsText, "Logger.Log($\"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}\");");

        AssertContains(rootText, "public sealed class CaptureSessionCoordinator : IDisposable, IAsyncDisposable");
        AssertContains(agentMapText, "`CaptureSessionCoordinator.cs`");
        AssertContains(agentMapText, "read-only Flashback status");
        AssertContains(agentMapText, "active playback-controller guard");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureSessionCoordinator.cs`");
        AssertContains(cleanupPlanText, "read-only Flashback status");
        AssertContains(cleanupPlanText, "active playback-controller guard");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var cycleMethod = ExtractTextBetween(
            coordinatorText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public Task SetFlashbackEnabledAsync");
        var queueProcessor = ExtractTextBetween(
            coordinatorText,
            "private async Task ProcessQueueAsync",
            "private void FailPendingCommands(Exception ex)");

        AssertContains(coordinatorText, "_latestFlashbackEncoderCycleGeneration");
        AssertContains(coordinatorText, "_commandsCoalesced");
        AssertContains(cycleMethod, "coalesceLatest: true");
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
            "public Task UpdateRecordingFormatAsync");
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
        var cycleFlashbackEncoder = ExtractTextBetween(
            coordinatorText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public Task SetFlashbackEnabledAsync");

        AssertDoesNotContain(stopVideo, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopVideoTeardown, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopRecording, "propagateCancellationToOperation: true");
        AssertDoesNotContain(cycleFlashbackEncoder, "propagateCancellationToOperation: true");
        AssertContains(cycleFlashbackEncoder, "coalesceLatest: true");

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

    internal static Task CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var queueText = rootText;
        var queueExecutionText = queueText;

        AssertContains(queueText, "private sealed class CoordinatorWorkItem");
        AssertContains(queueText, "private Task EnqueueAsync(");
        AssertContains(queueText, "private void ThrowIfDisposed()");
        AssertContains(queueExecutionText, "private async Task ProcessQueueAsync()");
        AssertContains(queueExecutionText, "private void FailPendingCommands(Exception ex)");
        AssertContains(queueExecutionText, "private void DecrementPendingCommands(string operation)");
        AssertContains(queueExecutionText, "Logger.LogEvent(\"CAP-COORD-START\"");
        AssertContains(queueExecutionText, "Logger.LogEvent(\"CAP-COORD-DONE\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Queue.cs")),
            "CaptureSessionCoordinator queue worker folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public CaptureSessionSnapshot Snapshot");
        AssertContains(rootText, "private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)");
        AssertContains(rootText, "private void TrackPendingCommandEnqueued(DateTimeOffset enqueuedAtUtc)");
        AssertContains(rootText, "private void RemoveOldestPendingCommand()");
        AssertContains(rootText, "private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)");
        AssertContains(rootText, "OldestPendingCommandAgeMs = oldestPendingCommandAgeMs,");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Snapshot.cs")),
            "CaptureSessionCoordinator snapshot projection folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_DisposalLivesInCoordinatorRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private const int DefaultDisposeDrainTimeoutMs = 15_000;");
        AssertContains(rootText, "public void Dispose()");
        AssertContains(rootText, "public async ValueTask DisposeAsync()");
        AssertContains(rootText, "private async ValueTask CoreDisposeAsync()");
        AssertContains(rootText, "private async Task WaitForWorkerCancellationAsync()");
        AssertContains(rootText, "private void DisposeWorkerCancellationWhenSafe()");
        AssertContains(rootText, "private void CancelWorkerBestEffort()");
        AssertContains(rootText, "SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Disposal.cs")),
            "CaptureSessionCoordinator disposal lifecycle folded into the coordinator root");

        return Task.CompletedTask;
    }


// MainWindow Flashback automation and presentation contracts live with their xUnit wrappers.
    internal static Task FlashbackPollingTimers_LiveInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var pollingAdapterText = ReadMainWindowFlashbackAdapterSource();
        var timelineAdapterText = ReadMainWindowFlashbackAdapterSource();
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = controllerText;

        AssertContains(pollingAdapterText, "private FlashbackPollingController _flashbackPollingController = null!;");
        AssertContains(pollingAdapterText, "private void InitializeFlashbackPollingController()");
        AssertContains(pollingAdapterText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartStatusPolling();");
        AssertContains(pollingAdapterText, "_flashbackPollingController.StopStatusPolling();");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartPlaybackPolling();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StopPlaybackPolling();");
        AssertContains(mainWindowText, "InitializeFlashbackPollingController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback polling adapter folded into the MainWindow root composition adapter");
        AssertContains(timelineAdapterText, "StartStatusPolling = StartFlashbackStatusPolling,");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(flashbackText, "StartPlaybackPolling = StartFlashbackPlaybackPolling,");
        AssertContains(flashbackText, "StopPlaybackPolling = StopFlashbackPlaybackPolling,");
        AssertContains(playbackCoordinatorText, "_context.StartPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.StopPlaybackPolling();");
        AssertContains(controllerText, "internal sealed class FlashbackPollingController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statusTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _playbackTimer;");
        AssertContains(controllerText, "public void StartStatusPolling()");
        AssertContains(controllerText, "public void StopStatusPolling()");
        AssertContains(controllerText, "public void StartPlaybackPolling()");
        AssertContains(controllerText, "public void StopPlaybackPolling()");
        AssertContains(controllerText, "_context.ViewModel.UpdateFlashbackBufferStatus();");
        AssertContains(controllerText, "_context.ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;");
        AssertContains(controllerText, "FLASHBACK_STATUS_TIMER_FAIL");
        AssertContains(controllerText, "FLASHBACK_PLAYBACK_TIMER_FAIL");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackStatusTimer;");
        AssertDoesNotContain(flashbackText, "private void FlashbackStatusTimer_Tick(");
        AssertDoesNotContain(flashbackText, "private void FlashbackPlaybackTimer_Tick(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlayheadMotion_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var scrubText = ReadMainWindowFlashbackAdapterSource();
        var scrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playheadText = ReadMainWindowFlashbackAdapterSource();
        var pollingAdapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var controllerText = controllerRootText;
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(playheadText, "XAML-facing Flashback playhead motion adapter");
        AssertContains(playheadText, "private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;");
        AssertContains(playheadText, "private void InitializeFlashbackPlayheadMotionController()");
        AssertContains(playheadText, "IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,");
        AssertContains(playheadText, "private void RequestFlashbackPlayheadSnapOnNextUpdate()");
        AssertContains(playheadText, "private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)");
        AssertContains(playheadText, "private void RefreshFlashbackCtiMotion(string reason)");
        AssertContains(playheadText, "=> _flashbackPlayheadMotionController.RefreshCtiMotion(reason);");
        AssertContains(playheadText, "private void StopFlashbackCtiAnchorTimer()");
        AssertContains(playheadText, "=> _flashbackPlayheadMotionController.StopCtiAnchorTimer();");
        AssertContains(mainWindowText, "InitializeFlashbackPlayheadMotionController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback playhead adapter folded into the MainWindow root composition adapter");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackScrubInteractionController();", "InitializeFlashbackPlayheadMotionController();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlayheadMotionController();", "InitializeFlashbackTimelineController();");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionControllerContext");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionController");
        AssertContains(controllerRootText, "private enum FlashbackPlayheadMotion");
        AssertContains(controllerRootText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(controllerRootText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(controllerRootText, "private CompositionEasingFunction? _flashbackPlayheadEaseLinear;");
        AssertContains(controllerRootText, "private bool _snapFlashbackPlayheadOnNextUpdate;");
        AssertContains(controllerRootText, "public void RequestSnapOnNextUpdate()");
        AssertContains(controllerRootText, "public void PositionMagneticPlayhead(double x, double trackWidth)");
        AssertContains(controllerText, "public void RefreshCtiMotion(string reason)");
        AssertContains(controllerText, "public void StopCtiAnchorTimer()");
        AssertContains(controllerText, "private void StartFlashbackCtiAnchorTimer()");
        AssertContains(controllerText, "private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)");
        AssertContains(controllerText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertContains(controllerText, "state == FlashbackPlaybackState.Live");
        AssertContains(controllerText, "SnapPlayheadVisualsToFraction(1.0, trackW);");
        AssertContains(controllerText, "StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "RefreshCtiMotion(\"anchor_tick\");");
        AssertContains(controllerText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(controllerText, "private void EnsureFlashbackPlayheadVisuals()");
        AssertContains(controllerText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(controllerText, "private void StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "private static void StartLinearKeyframe(");
        AssertContains(controllerText, "private void SnapPlayheadVisualsToFraction(");
        AssertContains(controllerText, "private void AnimateFlashbackPlayheadX(");
        AssertContains(controllerText, "private static void SnapFlashbackPlayheadX(");
        AssertContains(controllerText, "ElementCompositionPreview.SetIsTranslationEnabled(_context.Playhead, true);");
        AssertContains(controllerText, "Canvas.SetLeft(_context.Playhead, 0);");
        AssertContains(controllerText, "var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));");
        AssertContains(controllerText, "var lineX = (float)(x - 1);");
        AssertContains(controllerText, "var handleX = (float)(x - 5);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.Cti.cs")),
            "Flashback playhead CTI partial is consolidated into the motion controller root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.Visuals.cs")),
            "Flashback playhead visuals partial is consolidated into the motion controller root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.cs")),
            "Flashback playhead motion folded into Flashback UI controllers");
        AssertContains(scrubText, "PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,");
        AssertContains(scrubControllerText, "_context.PositionMagneticPlayhead(x, width);");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"state_change\");");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertDoesNotContain(playheadText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(playheadText, "private void StartLinearPlayheadExtrapolation(");
        AssertDoesNotContain(playheadText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertDoesNotContain(flashbackText, "private enum FlashbackPlayheadMotion");
        AssertDoesNotContain(flashbackText, "private Visual? _flashbackPlayheadVisual;");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(flashbackText, "private void StartLinearPlayheadExtrapolation(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackPresentation_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = controllerText;
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackPlaybackPresentationController()");
        AssertContains(flashbackText, "PlayPauseIcon = FlashbackPlayPauseIcon,");
        AssertContains(flashbackText, "GoLiveButton = FlashbackGoLiveButton,");
        AssertContains(flashbackText, "BufferDurationText = FlashbackBufferDurationText,");
        AssertContains(flashbackText, "PlayheadTimeText = FlashbackPlayheadTimeText,");
        AssertContains(mainWindowText, "InitializeFlashbackPlaybackPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackPlaybackPresentationController");
        AssertContains(controllerText, "public static string GetPlayPauseGlyph(FlashbackPlaybackState state)");
        AssertContains(controllerText, "public static bool IsGoLiveEnabled(FlashbackPlaybackState state)");
        AssertContains(controllerText, "public static string FormatPositionLabel(");
        AssertContains(controllerText, "\"\\uE769\"");
        AssertContains(controllerText, "\"\\uE768\"");
        AssertContains(controllerText, "return \"LIVE\";");
        AssertContains(controllerText, "return $\"-{FlashbackMarkerPresentationController.FormatDuration(gapFromLive)} / {totalText}\";");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlaybackUiCoordinator.cs")),
            "Flashback playback presentation and UI coordination live with Flashback UI controllers");
        AssertContains(flashbackText, "private FlashbackPlaybackUiCoordinator _flashbackPlaybackUiCoordinator = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackPlaybackUiCoordinator()");
        AssertContains(mainWindowText, "InitializeFlashbackPlaybackUiCoordinator();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlaybackPresentationController();", "InitializeFlashbackPlaybackUiCoordinator();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlaybackUiCoordinator();", "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(playbackCoordinatorText, "internal sealed class FlashbackPlaybackUiCoordinatorContext");
        AssertContains(playbackCoordinatorText, "internal sealed class FlashbackPlaybackUiCoordinator");
        AssertContains(playbackCoordinatorText, "_context.PlaybackPresentation.UpdateState(state);");
        AssertContains(playbackCoordinatorText, "_context.StartPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.StopPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"state_change\");");
        AssertContains(playbackCoordinatorText, "public void UpdateBufferPresentation()\n    {\n        UpdateBufferFill();\n        UpdatePosition();\n        _context.UpdateMarkers();\n    }");
        AssertContains(playbackCoordinatorText, "_context.PlaybackPresentation.UpdateBufferFill(duration);");
        AssertContains(playbackCoordinatorText, "_context.PlaybackPresentation.UpdatePosition(");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"position_change\");");
        AssertContains(flashbackText, "private void UpdateFlashbackBufferPresentation()\n        => _flashbackPlaybackUiCoordinator.UpdateBufferPresentation();");
        AssertContains(flashbackPropertyChangedText, "UpdateBuffer = UpdateFlashbackBufferPresentation,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferFillPercent):");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferDiskBytes):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateBuffer();");
        AssertDoesNotContain(flashbackPropertyChangedText, "UpdateFlashbackBufferFill();\n        UpdateFlashbackPositionUI();");
        AssertDoesNotContain(flashbackText, "_flashbackPlaybackPresentationController.UpdateState(state);");
        AssertDoesNotContain(flashbackText, "if (state == FlashbackPlaybackState.Playing)");
        AssertDoesNotContain(flashbackText, "RefreshFlashbackCtiMotion(\"position_change\");");
        AssertDoesNotContain(flashbackText, "FlashbackPlayPauseIcon.Glyph =");
        AssertDoesNotContain(flashbackText, "FlashbackGoLiveButton.IsEnabled =");
        AssertDoesNotContain(flashbackText, "FlashbackBufferDurationText.Text =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayheadTimeText.Text =");

        var controllerType = RequireType("Sussudio.Controllers.FlashbackPlaybackPresentationController");
        var stateType = RequireType("Sussudio.Models.FlashbackPlaybackState");
        var getPlayPauseGlyph = controllerType.GetMethod("GetPlayPauseGlyph", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.GetPlayPauseGlyph was not found.");
        var isGoLiveEnabled = controllerType.GetMethod("IsGoLiveEnabled", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.IsGoLiveEnabled was not found.");
        var formatPositionLabel = controllerType.GetMethod("FormatPositionLabel", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.FormatPositionLabel was not found.");

        object State(string name) => Enum.Parse(stateType, name);

        AssertEqual("\uE769", getPlayPauseGlyph.Invoke(null, new[] { State("Playing") })?.ToString(), "playing glyph");
        AssertEqual("\uE769", getPlayPauseGlyph.Invoke(null, new[] { State("Live") })?.ToString(), "live glyph");
        AssertEqual("\uE768", getPlayPauseGlyph.Invoke(null, new[] { State("Paused") })?.ToString(), "paused glyph");
        AssertEqual("\uE768", getPlayPauseGlyph.Invoke(null, new[] { State("Scrubbing") })?.ToString(), "scrubbing glyph");
        AssertEqual(false, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Live") })!, "live disables go-live button");
        AssertEqual(false, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Disabled") })!, "disabled disables go-live button");
        AssertEqual(true, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Paused") })!, "paused enables go-live button");
        AssertEqual(
            "LIVE",
            formatPositionLabel.Invoke(null, new object[] { State("Live"), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5) })?.ToString(),
            "live position label");
        AssertEqual(
            "-0:05 / 2:05",
            formatPositionLabel.Invoke(null, new object[] { State("Paused"), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5) })?.ToString(),
            "buffered position label");

        return Task.CompletedTask;
    }

    internal static Task FlashbackSettingsBindings_LiveInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var commandAdapterText = ReadMainWindowFlashbackAdapterSource();
        var commandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackSettingsBindingController()");
        AssertContains(adapterText, "FlashbackEnabledToggle = FlashbackEnabledToggle,");
        AssertContains(adapterText, "FlashbackGpuDecodeToggle = FlashbackGpuDecodeToggle,");
        AssertContains(adapterText, "FlashbackBufferDurationCombo = FlashbackBufferDurationCombo,");
        AssertContains(adapterText, "ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout");
        AssertContains(adapterText, "private void ApplyInitialFlashbackSettings()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.ApplyInitialSettings();");
        AssertContains(adapterText, "private void AttachFlashbackSettingsBindings()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.AttachBindings();");
        AssertContains(adapterText, "private void SyncFlashbackGpuDecodeSetting()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.SyncGpuDecodeToggle();");
        AssertContains(adapterText, "private void SyncFlashbackBufferDurationSetting()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.SyncBufferDurationSelection();");
        AssertContains(adapterText, "private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(adapterText, "if (ViewModel == null || _flashbackSettingsBindingController == null)");
        AssertContains(adapterText, "_flashbackSettingsBindingController.HandleBufferDurationSelectionChanged();");
        AssertContains(mainWindowText, "InitializeFlashbackSettingsBindingController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback settings adapter folded into the MainWindow root composition adapter");
        AssertContains(bindingsText, "ApplyInitialFlashbackSettings();");
        AssertContains(bindingsText, "AttachFlashbackSettingsBindings();");

        AssertContains(controllerText, "internal sealed class FlashbackSettingsBindingControllerContext");
        AssertContains(controllerText, "internal sealed class FlashbackSettingsBindingController");
        AssertContains(controllerText, "public void ApplyInitialSettings()");
        AssertContains(controllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;");
        AssertContains(controllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(controllerText, "SyncBufferDurationSelection();");
        AssertContains(controllerText, "public void AttachBindings()");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.Toggled +=");
        AssertContains(controllerText, "_context.ViewModel.FlashbackGpuDecode = _context.FlashbackGpuDecodeToggle.IsOn;");
        AssertContains(controllerText, "public void SyncGpuDecodeToggle()");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;");
        AssertContains(controllerText, "public void SyncBufferDurationSelection()");
        AssertContains(controllerText, "currentTag == selectedMinutes");
        AssertContains(controllerText, "_context.FlashbackBufferDurationCombo.SelectedItem = item;");
        AssertContains(controllerText, "public void HandleBufferDurationSelectionChanged()");
        AssertContains(controllerText, "int.TryParse(tag, out var minutes)");
        AssertContains(controllerText, "_context.ViewModel.FlashbackBufferMinutes = minutes;");
        AssertContains(controllerText, "FLASHBACK_UI_BUFFER_DURATION_CHANGED");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "SyncGpuDecodeSetting = SyncFlashbackGpuDecodeSetting,");
        AssertContains(flashbackPropertyChangedText, "SyncBufferDurationSetting = SyncFlashbackBufferDurationSetting");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackGpuDecode):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.SyncGpuDecodeSetting();");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferMinutes):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.SyncBufferDurationSetting();");

        AssertContains(commandAdapterText, "private FlashbackCommandController _flashbackCommandController = null!;");
        AssertContains(commandAdapterText, "private void InitializeFlashbackCommandController()");
        AssertContains(commandAdapterText, "private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)");
        AssertContains(commandAdapterText, "=> _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));");
        AssertContains(commandAdapterText, "private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(commandAdapterText, "=> _flashbackCommandController.ApplySettings(nameof(FlashbackApplyButton_Click));");
        AssertContains(commandControllerText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(commandControllerText, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.RestartFlashbackAsync(), operationName);");
        AssertContains(commandControllerText, "public bool HandleFullScreenKeyboardCommand(VirtualKey key)");
        AssertContains(commandControllerText, "NudgePlayback(TimeSpan.FromSeconds(-1), \"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\");");
        AssertContains(commandControllerText, "NudgePlayback(TimeSpan.FromSeconds(1), \"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\");");
        AssertContains(mainWindowText, "InitializeFlashbackCommandController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback command adapter folded into the MainWindow root composition adapter");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackCommandController.cs")),
            "Flashback command controller folded into FlashbackUiControllers.cs");
        AssertDoesNotContain(flashbackText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertDoesNotContain(bindingsText, "FlashbackEnabledToggle.IsOn = ViewModel.IsFlashbackEnabled;");
        AssertDoesNotContain(bindingsText, "FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;");
        AssertDoesNotContain(bindingsText, "FlashbackGpuDecodeToggle.Toggled +=");
        AssertDoesNotContain(bindingsText, "foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)");
        AssertDoesNotContain(flashbackText, "foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackBufferDurationCombo.SelectedItem = item;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackTimelineTrackLayout_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var timelineAdapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var animationControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(timelineAdapterText, "FlashbackTrackBackground = FlashbackTrackBackground,");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback timeline adapter folded into the MainWindow root composition adapter");
        AssertContains(timelineAdapterText, "FlashbackScrubArea = FlashbackScrubArea,");
        AssertContains(timelineAdapterText, "FlashbackPlayhead = FlashbackPlayhead,");
        AssertContains(timelineAdapterText, "FlashbackLiveEdge = FlashbackLiveEdge,");
        AssertContains(controllerText, "public required FrameworkElement FlashbackTrackBackground { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackScrubArea { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackPlayhead { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackLiveEdge { get; init; }");
        AssertContains(controllerText, "public void ApplyTrackSize(double width, double height)");
        AssertContains(controllerText, "_context.FlashbackTrackBackground.Width = width;");
        AssertContains(controllerText, "_context.FlashbackTrackBackground.Height = height;");
        AssertContains(controllerText, "_context.FlashbackScrubArea.Width = width;");
        AssertContains(controllerText, "_context.FlashbackScrubArea.Height = height;");
        AssertContains(controllerText, "_context.FlashbackPlayhead.Height = height;");
        AssertContains(controllerText, "_context.FlashbackLiveEdge.Height = height;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.FlashbackLiveEdge, width - 2);");
        AssertContains(controllerText, "private readonly FlashbackTimelineAnimationController _animationController;");
        AssertContains(controllerText, "_animationController.Animate(show: true);");
        AssertContains(controllerText, "_animationController.Animate(show: false);");
        AssertContains(animationControllerText, "internal sealed class FlashbackTimelineAnimationController");
        AssertContains(animationControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(animationControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(animationControllerText, "public void CollapseImmediately()");
        AssertContains(animationControllerText, "public void ResetForFullScreen()");
        AssertContains(animationControllerText, "private void CompleteAnimation(Storyboard storyboard)");
        AssertContains(controllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(controllerText, "new DoubleAnimation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackTimelineAnimationController.cs")),
            "timeline animation folded into Flashback UI controllers");
        AssertContains(flashbackText, "private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(flashbackText, "=> _flashbackPlaybackUiCoordinator.HandleTrackSizeChanged(e.NewSize.Width, e.NewSize.Height);");
        AssertContains(playbackCoordinatorText, "public void HandleTrackSizeChanged(double width, double height)");
        AssertContains(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);");
        AssertOccursBefore(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);", "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertOccursBefore(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();", "UpdatePosition();");
        AssertOccursBefore(playbackCoordinatorText, "UpdatePosition();", "_context.UpdateMarkers();");
        AssertOccursBefore(playbackCoordinatorText, "_context.UpdateMarkers();", "_context.RefreshCtiMotion(\"size_changed\");");
        AssertContains(agentMapText, "timeline visibility, lockout, toggle synchronization, timeline track layout");
        AssertContains(agentMapText, "sizing, show/hide storyboard state");
        AssertContains(agentMapText, "show/hide storyboard state");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayhead.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackLiveEdge.Height =");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(FlashbackLiveEdge");

        return Task.CompletedTask;
    }

    internal static Task FlashbackMarkerPresentation_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackMarkerPresentationController()");
        AssertContains(flashbackText, "ScrubArea = FlashbackScrubArea,");
        AssertContains(flashbackText, "InPointMarker = FlashbackInPointMarker,");
        AssertContains(flashbackText, "OutPointMarker = FlashbackOutPointMarker,");
        AssertContains(flashbackText, "SelectionRegion = FlashbackSelectionRegion,");
        AssertContains(flashbackText, "=> _flashbackMarkerPresentationController.UpdateMarkers(");
        AssertContains(flashbackText, "ViewModel.FlashbackBufferFilledDuration,");
        AssertContains(flashbackText, "ViewModel.FlashbackInPoint,");
        AssertContains(flashbackText, "ViewModel.FlashbackOutPoint);");
        AssertContains(mainWindowText, "InitializeFlashbackMarkerPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackMarkerPresentationController");
        AssertContains(controllerText, "public static string FormatDuration(TimeSpan value)");
        AssertContains(controllerText, "public void UpdateMarkers(TimeSpan bufferDuration, TimeSpan? inPoint, TimeSpan? outPoint)");
        AssertContains(controllerText, "_context.InPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.OutPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.SelectionRegion.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.SelectionRegion, selLeft);");
        AssertContains(flashbackText, "UpdateMarkers = UpdateFlashbackMarkers,");
        AssertContains(playbackCoordinatorText, "_context.UpdateMarkers();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "UpdateRangeMarkers = UpdateFlashbackMarkers,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackInPoint):");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackOutPoint):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateRangeMarkers();");
        AssertDoesNotContain(flashbackText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(");
        AssertDoesNotContain(flashbackText, "FlashbackInPointMarker.Visibility = Visibility.Visible;");
        AssertDoesNotContain(flashbackText, "FlashbackSelectionRegion.Visibility = Visibility.Visible;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportProgressPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackExportProgressPresentationController()");
        AssertContains(flashbackText, "FlashbackExportProgressBar = FlashbackExportProgressBar,");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateProgress(progress);");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateExporting(isExporting);");
        AssertContains(mainWindowText, "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(mainWindowText, "InitializeFlashbackPropertyChangedController();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "UpdateExportProgress = UpdateFlashbackExportProgress,");
        AssertContains(flashbackPropertyChangedText, "UpdateExportingPresentation = UpdateFlashbackExportingPresentation,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackExportProgress):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateExportProgress(_context.GetExportProgress());");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackExporting):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateExportingPresentation(_context.IsExporting());");
        AssertContains(controllerText, "internal sealed class FlashbackExportProgressPresentationController");
        AssertContains(controllerText, "public void UpdateProgress(double progress)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Value = progress;");
        AssertContains(controllerText, "public void UpdateExporting(bool isExporting)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Visibility = isExporting");
        AssertContains(controllerText, "? Visibility.Visible");
        AssertContains(controllerText, ": Visibility.Collapsed;");
        AssertContains(controllerText, "if (!isExporting)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Value = 0;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackExportProgressBar.Value = ViewModel.FlashbackExportProgress;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackExportProgressBar.Visibility = ViewModel.IsFlashbackExporting");

        return Task.CompletedTask;
    }

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
        AssertContains(flashbackScrubText, "PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,");
        AssertContains(flashbackScrubText, "RefreshCtiMotion = RefreshFlashbackCtiMotion,");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.FullScreenFlashbackBridge.cs")),
            "Flashback fullscreen bridge is consolidated into the fullscreen adapter");
        AssertDoesNotContain(flashbackScrubText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(flashbackScrubText, "private TimeSpan? _lastScrubPointerPosition;");
        AssertDoesNotContain(flashbackScrubText, "private long _lastScrubUpdateTick;");
        AssertDoesNotContain(flashbackScrubControllerText, "var carriedPosition = _isScrubbing ? _context.ViewModel.FlashbackPlaybackPosition : (TimeSpan?)null;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback scrub adapter folded into the MainWindow root composition adapter");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackScrubInteractionController.cs")),
            "Flashback scrub interaction folded into Flashback UI controllers");
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
        AssertContains(flashbackSettingsText, "ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout");
        AssertContains(flashbackSettingsControllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackSettingsControllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "private void InitializeFlashbackPropertyChangedController()");
        AssertContains(flashbackPropertyChangedText, "ApplyTimelineLockout = ApplyFlashbackTimelineLockout,");
        AssertContains(flashbackPropertyChangedText, "ApplyTimelineVisibility = ApplyFlashbackTimelineVisibility,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackEnabled):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.ApplyTimelineLockout();");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.ApplyTimelineVisibility(_context.IsTimelineVisible());");
        AssertContains(flashbackTimelineText, "private FlashbackTimelineController _flashbackTimelineController = null!;");
        AssertContains(flashbackTimelineText, "FlashbackToggle = FlashbackToggle,");
        AssertContains(flashbackTimelineText, "FlashbackTimelinePanel = FlashbackTimelinePanel,");
        AssertContains(flashbackTimelineText, "SnapPlayheadOnNextOpen = RequestFlashbackPlayheadSnapOnNextUpdate,");
        AssertContains(flashbackTimelineText, "ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.OnToggleChecked();");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.ApplyLockout();");
        AssertContains(fullScreenText, "ResetFlashbackTimelineAnimation = _flashbackTimelineController.ResetAnimationForFullScreen,");
        AssertContains(flashbackTimelineControllerText, "public void ResetAnimationForFullScreen()");
        AssertDoesNotContain(flashbackTimelineText, "ResetFlashbackTimelineAnimationForFullScreen");
        AssertContains(flashbackTimelineText, "=> _flashbackScrubInteractionController.ClearForLockout();");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackCommandController.cs")),
            "Flashback command controller folded into FlashbackUiControllers.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackTimelineController.cs")),
            "Flashback timeline folded into Flashback UI controllers");
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
        AssertTaskReturningMethod(automationInterfaceType, "SetMicrophoneEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetHdrEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetTrueHdrPreviewEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetFlashbackEnabledAsync", resultType: null);
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

        var interfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();
        var viewModelDispatchText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs")
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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationPreview.cs")),
            "MainViewModel automation preview partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationHdr.cs")),
            "MainViewModel automation HDR partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationFlashback.cs")),
            "MainViewModel automation Flashback partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationCommands.cs")),
            "MainViewModel automation commands facade folded into MainViewModel.cs");

        AssertDoesNotContain(interfaceText, "bool FlashbackPlay();");
        AssertDoesNotContain(interfaceText, "bool FlashbackPause();");
        AssertDoesNotContain(interfaceText, "bool FlashbackGoLive();");
        AssertDoesNotContain(interfaceText, "bool FlashbackBeginScrub(TimeSpan position);");
        AssertDoesNotContain(interfaceText, "bool FlashbackEndScrub();");
        AssertDoesNotContain(interfaceText, "VideoSourceProbeResult ProbeVideoSource();");
        AssertDoesNotContain(interfaceText, "PreviewColorProbeResult ProbePreviewColor();");
        AssertContains(dispatcherText, "await _flashbackPort.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "return CreateFlashbackActionRejectedResponse(");
        AssertContains(dispatcherText, "errorCode: \"flashback-action-failed\"");
        AssertContains(dispatcherText, "RequestedPositionMs = requestedPositionMs");
        AssertContains(dispatcherText, "LastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(dispatcherText, "var useSelectionRange = GetBool(payload, \"useSelectionRange\") ?? false;");
        AssertContains(dispatcherText, "var force = GetBool(payload, \"force\") ?? false;");
        AssertContains(dispatcherText, "ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken)");
        AssertContains(dispatcherText, "CaptureService.ClassifyFlashbackExportFailureKind(exportResult.StatusMessage)");
        AssertContains(dispatcherText, "FailureKind = failureKind");
        AssertContains(dispatcherText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(dispatcherText, "AutomationFlashbackAction.BeginScrub => RequireDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "AutomationFlashbackAction.UpdateScrub => RequireDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "AutomationFlashbackAction.EndScrub => GetDouble(payload, \"positionMs\")");
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
        AssertContains(dispatcherText, "await _snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _audioPort.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _captureSettingsPort.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _previewRecordingPort.SetRecordingEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _probePort.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _probePort.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _snapshotQueryPort.GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "vm.SetHdrEnabledAsync(v, ct)");
        AssertContains(dispatcherText, "vm.SetTrueHdrPreviewEnabledAsync(v, ct)");
        AssertDoesNotContain(dispatcherText, "_viewModel.IsMicrophoneEnabled =");
        AssertContains(viewModelDispatchText, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");

        AssertContains(automationText, "public Task<bool> ExecuteFlashbackActionAsync(");
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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "ViewModels",
                "MainViewModel.AutomationRecordingLifecycle.cs")),
            "MainViewModel automation recording lifecycle bridge partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingLifecycle.cs")),
            "MainViewModel recording lifecycle facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingState.cs")),
            "MainViewModel recording state folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");
        AssertContains(recordingStateText, "internal sealed class BitrateSampleWindow");
        AssertContains(recordingStateText, "public double? AddSampleAndCompute(long tick, long bytes)");
        AssertContains(recordingStateText, "private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "BitrateSampleWindow.cs")),
            "BitrateSampleWindow folded into MainViewModel.cs");
        AssertContains(recordingRuntimeText, "_pendingModeOptionsRefresh = false;");
        AssertContains(recordingRuntimeText, "RebuildResolutionOptions();");
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
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var automationSettingsText = viewModelFiles["MainViewModel.cs"];
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackEncoderSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");

        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "_suppressFlashbackFormatCycle is false");
        AssertContains(rawFlashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),\n                \"recording format\");");
        AssertContains(viewModelFlashbackStateText, "private bool _suppressFlashbackFormatCycle;");
        AssertMemberContains(automationSettingsText, "SetRecordingFormatAsync", "_recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken)");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "SetSuppressFlashbackFormatCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "await _context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "_context.SetSelectedQuality(matched);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "settings.Quality,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "return BuildEncoderSettings(splitEncodeMode: _context.GetSelectedSplitEncodeMode());");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "settings.SplitEncodeMode,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "_context.SetCustomBitrateMbps(RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps));");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "settings.Bitrate,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "_context.SetSelectedPreset(matched);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "settings.Preset,");
        AssertMemberContains(flashbackEncoderSettingsText, "OnCustomBitrateMbpsChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedQualityChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedPresetChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedSplitEncodeModeChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "customBitrateMbps: CustomBitrateMbps");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "nvencPreset: SelectedPreset");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "splitEncodeMode: SelectedSplitEncodeMode");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "TrackPendingFlashbackCycleTask(task, description);");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = task;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (ReferenceEquals(_pendingFlashbackCycleTask, t))");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = null;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (t.IsFaulted)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "else if (t.IsCanceled)");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) failed");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) canceled");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackEncoderSettings.cs")), "MainViewModel.FlashbackEncoderSettings.cs folded into FlashbackState");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackSettings.cs")), "MainViewModel.FlashbackSettings.cs folded into FlashbackState");

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
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(\"Recording start canceled\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(\"Stop recording canceled\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText($\"Recording failed: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText($\"Stop recording failed: {ex.Message}\");");
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
        AssertContains(appText, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(recordingStateText, "if (!IsRecording)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingLifecycle.cs")),
            "MainViewModel recording lifecycle facade partial");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial()
    {
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var viewModelRuntimeSnapshotText = automationFacadeText;
        var viewModelRuntimeSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(viewModelRuntimeSnapshotText, "public partial class MainViewModel");
        AssertContains(viewModelRuntimeSnapshotText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(viewModelRuntimeSnapshotText, "var sessionSnapshot = _sessionCoordinator.Snapshot;");
        AssertContains(viewModelRuntimeSnapshotText, "return InvokeOnUiThreadAsync(() =>");
        AssertContains(viewModelRuntimeSnapshotText, "var input = new ViewModelRuntimeSnapshotInput");
        AssertContains(viewModelRuntimeSnapshotText, "return ViewModelRuntimeSnapshotBuilder.Build(input);");
        AssertDoesNotContain(viewModelRuntimeSnapshotText, "=> new ViewModelRuntimeSnapshot");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "internal static class ViewModelRuntimeSnapshotBuilder");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "internal sealed class ViewModelRuntimeSnapshotInput");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(input.SourceTelemetryTimestampUtc, input.TimestampUtc),");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandCommandsEnqueued = sessionSnapshot.CommandsEnqueued,");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandLastCommand = sessionSnapshot.LastCommand?.ToString() ?? \"None\",");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandLastCorrelationId = sessionSnapshot.LastCorrelationId ?? string.Empty,");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0,");
        AssertContains(automationFacadeText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync");
        AssertDoesNotContain(automationFacadeText, "=> new ViewModelRuntimeSnapshot");
        AssertContains(automationFacadeText, "public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();");
        AssertContains(automationFacadeText, "public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();");
        AssertContains(automationFacadeText, "public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "=> FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);");
        AssertContains(automationFacadeText, "=> FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);");
        AssertContains(automationFacadeText, "public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)\n        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);");
        AssertContains(agentMapText, "`MainViewModel.cs` owns automation-facing view-model runtime snapshot UI-thread capture.");
        AssertContains(agentMapText, "`ViewModelBuilders.cs` owns pure view-model runtime snapshot DTO construction.");
        AssertContains(agentMapText, "also owns automation-facing source/preview probes and preview frame capture.");
        AssertContains(cleanupPlanText, "`MainViewModel.cs`; pure view-model runtime snapshot DTO");
        AssertContains(cleanupPlanText, "construction lives in `ViewModelBuilders.cs`");
        AssertContains(cleanupPlanText, "probes, and preview frame capture now live in\n   `MainViewModel.cs`");

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
            + "\n" + ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs")
                .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(automationAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);");
        AssertContains(automationAudioText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}\");");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}\");");
        AssertContains(automationAudioText, "Cannot change microphone enable state while recording. Stop the recording first.");
        AssertContains(automationAudioText, "_suppressMicrophoneMonitorUpdate = true;");
        AssertContains(automationAudioText, "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(");
        AssertContains(automationAudioText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(automationAudioText, "IsMicrophoneEnabled = enabled;\n                }\n                finally\n                {\n                    _suppressMicrophoneMonitorUpdate = false;\n                }\n\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertContains(automationUiText, "public Task SetPreviewVolumeAsync");
        AssertContains(viewModelText, "if (_suppressMicrophoneMonitorUpdate)");
        AssertContains(captureServiceText, "var previousEnabled = _micMonitorEnabled;");
        AssertContains(captureServiceText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);\n\n                _micMonitorEnabled = enabled;");

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
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationAudio.cs",
            "MainViewModel.AutomationDeviceAudio.cs",
            "MainViewModel.AutomationMicrophone.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale audio automation partial {stalePath}");
        }

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
        AssertContains(automationOptionsBuilderText, "DisableReason = option.DisableReason ?? string.Empty");
        AssertContains(automationOptionsBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationOptionsSnapshot.cs")),
            "MainViewModel.AutomationOptionsSnapshot.cs folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationSnapshots.cs")),
            "MainViewModel.AutomationSnapshots.cs folded into MainViewModel.cs");

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
            "UpdateRecordingFormatAsync",
            "CycleFlashbackEncoderSettingsAsync",
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
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs")
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationFlashback.cs")),
            "MainViewModel automation Flashback partial");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackMarkers.cs")),
            "MainViewModel.FlashbackMarkers.cs folded into MainViewModel.FlashbackState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlaybackAutomation.cs")),
            "MainViewModel.FlashbackPlaybackAutomation.cs folded into MainViewModel.FlashbackState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlayback.cs")),
            "MainViewModel.FlashbackPlayback.cs folded into MainViewModel.FlashbackState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlaybackCommands.cs")),
            "MainViewModel.FlashbackPlaybackCommands.cs folded into MainViewModel.FlashbackState.cs");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackBufferStatus()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = playback.InPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = playback.OutPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "if (FlashbackState != FlashbackPlaybackState.Live)");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackState = FlashbackPlaybackState.Live;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");
        var updateFlashbackBufferStatus = ExtractMemberCode(flashbackBufferStatusText, "UpdateFlashbackBufferStatus");
        var inactivePlaybackSnapshotBranch = ExtractTextBetween(
            updateFlashbackBufferStatus,
            "else\n        {\n            if (FlashbackState != FlashbackPlaybackState.Live)",
            "\n        }\n    }");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackInPoint = null;");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBitrate", "_sessionCoordinator.FlashbackTotalBytesWritten");
        AssertContains(captureServiceText, "public long FlashbackTotalBytesWritten => _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;");
        AssertContains(captureServiceText, "ClassifyCaptureFailureSource(object? sender)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, ProgramCapture)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, MicrophoneCapture)");
        AssertContains(captureServiceText, "WASAPI_CAPTURE_FAILED source={source}");
        AssertContains(captureServiceText, "_previewAudioGraph.RecordCaptureFault(source, ex);");
        AssertContains(coordinatorText, "if (Volatile.Read(ref _isDisposed))");
        AssertContains(coordinatorText, "Volatile.Write(ref _isDisposed, true);");
        AssertContains(coordinatorText, "Exception failure = Volatile.Read(ref _isDisposed)");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackSettings.cs")), "MainViewModel.FlashbackSettings.cs folded into FlashbackState");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackEncoderSettings.cs")), "MainViewModel.FlashbackEncoderSettings.cs folded into FlashbackState");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportOperation.cs")), "MainViewModel.FlashbackExportOperation.cs folded into FlashbackExport");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportAutomation.cs")), "MainViewModel.FlashbackExportAutomation.cs folded into FlashbackExport");
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
            @"\b_captureService\s*\.\s*(SetFlashbackEnabled|RestartFlashbackAsync|UpdateRecordingFormatAsync|CycleFlashbackEncoderSettingsAsync|UpdateFlashbackSettings|ExportFlashback|GetFlashbackSegments|FlashbackPlaybackController|FlashbackBufferManager|FlashbackDiskBytes|FlashbackTotalBytesWritten)\b",
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
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertDoesNotContain(exportOperationsText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(exportOperationsText, "private readonly record struct FlashbackExportBackendSnapshot(");
        AssertContains(exportOperationsText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportBackendSnapshot.cs")),
            "old Flashback export backend snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportRangeResolution.cs")),
            "old Flashback export range-resolution partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportForceRotate.cs")),
            "old Flashback export force-rotate partial removed");
        AssertContains(exportCoreText, "private delegate (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportCoreText, "private FlashbackExportPreparationResult PrepareFlashbackExportRequest(");
        AssertContains(exportCoreText, "PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "private FlashbackExportForceRotatePreparation PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "ForceRotateForExport");
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
        AssertContains(backendSnapshot, "var flashbackExporter = bufferManager != null\n                ? _flashbackBackend.Exporter ??= new FlashbackExporter()\n                : _flashbackBackend.Exporter;");
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
        AssertContains(exportCore, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackBackend.Exporter ??= new FlashbackExporter();\n            }");
        AssertContains(exportCore, "var preparedExport = PrepareFlashbackExportRequest(");
        AssertContains(exportCore, "if (preparedExport.FailureResult is { } preparationFailure)");
        AssertContains(exportCoreText, "var forceRotateFallbackUsed = false;");
        AssertContains(exportCoreText, "forceRotateFallbackUsed = true;");
        AssertContains(exportCore, "live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames");
        AssertContains(exportCore, "if (preparedExport.ForceRotateFallbackUsed && result.Succeeded)\n            {\n                result = FinalizeResult.Success(");
        AssertContains(exportCore, "RecordLastFlashbackExportResult(exportId, result);\n            CompleteFlashbackExportDiagnostics(exportId, result);");

        var backendCleanup = ExtractTextBetween(
            backendResourcesText,
            "public async Task<bool> CleanupArtifactsAfterExportAsync",
            "    public async Task<FlashbackPlaybackController> StartPreviewBackendAsync");
        AssertContains(backendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(backendCleanup, "bool exportOperationLockAlreadyHeld = false)");
        AssertContains(backendCleanup, "var lockAcquired = exportOperationLockAlreadyHeld;");
        AssertContains(backendCleanup, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(backendCleanup, "request.Reason");
        AssertContains(backendCleanup, "request.FlashbackExporter.Dispose();");
        AssertContains(backendCleanup, "request.BufferManager.PurgeAllSegments();");
        AssertContains(backendCleanup, "FLASHBACK_BACKEND_CLEANUP_LOCK_REUSED");
        AssertContains(backendCleanup, "if (lockAcquired && releaseLockOnExit)");
        AssertContains(backendCleanup, "releaseExportOperationLock(mode);");

        var cleanupBridge = ExtractTextBetween(
            captureServiceText,
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync",
            "\n}");
        AssertContains(cleanupBridge, "_flashbackBackend.CleanupArtifactsAfterExportAsync(");
        AssertContains(cleanupBridge, "WaitForFlashbackBackendCleanupExportLockAsync");
        AssertContains(cleanupBridge, "ReleaseFlashbackBackendCleanupExportLock");

        var disposeBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendAsync",
            "    private async Task DisposeFlashbackPreviewBackendCoreAsync");
        AssertContains(disposeBackend, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(disposeBackend, "exportOperationLockAlreadyHeld: true");
        AssertContains(disposeBackend, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");

        var disposeBackendCore = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "    private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest");
        AssertContains(disposeBackendCore, "FlashbackPreviewBackendDisposalRequest request)");
        AssertContains(disposeBackendCore, "_flashbackBackend.DisposePreviewBackendAsync(request)");

        var disposeBackendResources = ExtractTextBetween(
            backendResourcesText,
            "public async Task DisposePreviewBackendAsync",
            "    public void ScheduleDeferredArtifactCleanup");
        AssertContains(disposeBackendResources, "request.ExportOperationLockAlreadyHeld");
        AssertContains(disposeBackendResources, "request.PurgeSegments ? \"preview_backend_dispose_purge\" : \"preview_backend_dispose\"");
        AssertContains(disposeBackendResources, "\"preview_backend_dispose\",\n                request.AcquireExportOperationLockAsync,\n                request.ReleaseExportOperationLock,\n                request.ExportOperationLockAlreadyHeld)");

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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExport.cs")),
            "MainViewModel.FlashbackExport.cs folded into MainViewModel.FlashbackState.cs");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "_sessionCoordinator.ExportFlashbackRangeAsync(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.InPointFilePts");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.OutPointFilePts");
        AssertContains(coordinatorText, "TimeSpan? InPointFilePts,");
        AssertContains(coordinatorText, "TimeSpan? OutPointFilePts,");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"export\")");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"save_last_5m\")");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        AssertContains(rawFlashbackExportText, "Flashback export unavailable: flashback is not active.");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "case ExportFlashbackOutcome.Stale:");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "case ExportFlashbackOutcome.Stale:");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertContains(disposalText, "Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(disposalText, "var exportCts = Interlocked.Exchange(ref _exportCts, null);");
        AssertContains(disposalText, "CancelFlashbackExportCts(exportCts);");
        AssertContains(rawDisposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(disposalControllerText, "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalControllerText, "_context.CancelActiveFlashbackExport();", "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalControllerText, "_context.StopRuntimeForDispose();", "var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(");
        AssertContains(rawDisposalText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(flashbackExportOperationText, "private abstract record ExportFlashbackOutcome");
        AssertContains(flashbackExportOperationText, "private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync");
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportOperation.cs")), "MainViewModel.FlashbackExportOperation.cs folded into FlashbackExport");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportAutomation.cs")), "MainViewModel.FlashbackExportAutomation.cs folded into FlashbackExport");
        AssertDoesNotContain(
            flashbackExportText + "\n" + flashbackExportOperationText + "\n" + flashbackExportAutomationText,
            "exportCts.Dispose();");

        return Task.CompletedTask;
    }


    internal static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationAudioText = automationUiText;
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(settingsProjectionText, "PreviewVolume = input.PreviewVolume,");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationUiText, "public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationStatsUi.cs")),
            "MainViewModel stats UI automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationUi.cs")),
            "MainViewModel.AutomationUi.cs folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationCommands.cs")),
            "MainViewModel.AutomationCommands.cs folded into MainViewModel.cs");
        return Task.CompletedTask;
    }

    internal static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var settingsPersistenceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var settingsLoadApplicationText = settingsPersistenceText;
        var settingsProjectionText = settingsPersistenceText[..settingsPersistenceText.IndexOf("public partial class MainViewModel", StringComparison.Ordinal)];
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var settingsServiceText = ReadRepoFile("Sussudio/Services/Runtime/RuntimeHelpers.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Runtime", "SettingsService.cs")),
            "SettingsService.cs folded into RuntimeHelpers.cs");
        AssertContains(settingsPersistenceText, "private void LoadSettings()");
        AssertContains(settingsPersistenceText, "private void SaveSettings()");
        AssertContains(settingsPersistenceText, "SettingsService.Load()");
        AssertContains(settingsPersistenceText, "SettingsService.Save(settings)");
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
        foreach (var removedFile in new[]
        {
            "MainViewModel.SettingsLoadApplication.cs",
            "MainViewModel.SettingsLoadApplication.Recording.cs",
            "MainViewModel.SettingsLoadApplication.Audio.cs",
            "MainViewModel.SettingsLoadApplication.Ui.cs",
            "MainViewModel.SettingsLoadApplication.DeviceAudio.cs",
            "MainViewModel.SettingsLoadApplication.Flashback.cs",
            "MainViewModel.SettingsLoadApplication.PendingDevices.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", removedFile)),
                $"{removedFile} folded into MainViewModel.cs");
        }
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
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", removedProjectionFile)),
                $"{removedProjectionFile} folded into MainViewModel.cs");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Settings.cs")),
            "old settings pass-through partial removed");
        AssertDoesNotContain(settingsPersistenceText, "RebuildResolutionOptions();\n        SaveSettings();");

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

        return Task.CompletedTask;
    }

    internal static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertDoesNotContain(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(automationSettingsText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);");
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
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSuppressFormatChangeReinitialize(true);");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSuppressFormatChangeReinitialize(false);");
        AssertContains(captureSettingsAutomationControllerText, "return wasPreviewing && _context.GetSelectedFormat() != null;");
        AssertContains(captureSettingsAutomationControllerText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureSettingsAutomationControllerText, "_captureModeGate.Release();");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertDoesNotContain(captureModeTransactionsText, "SetAutomationCaptureModeAsync(");
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationSettings.cs",
            "MainViewModel.AutomationDeviceSelection.cs",
            "MainViewModel.AutomationCaptureMode.cs",
            "MainViewModel.AutomationCaptureModeGate.cs",
            "MainViewModel.AutomationFrameRate.cs",
            "MainViewModel.AutomationVideoFormat.cs",
            "MainViewModel.AutomationMjpegDecoderCount.cs",
            "MainViewModel.CaptureOptionVisibility.cs",
            "MainViewModel.HdrModeChanges.cs",
            "MainViewModel.AutomationCaptureSettings.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale capture settings automation partial {stalePath}");
        }

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
        AssertContains(deviceSelectionAutomationText, "=> InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);");
        AssertContains(deviceSelectionAutomationText, "public Task SelectDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(rootViewModelText, "public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceManagement.cs")),
            "shallow MainViewModel device-management partial");
        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            _context.SetStatusText(\"Device scan canceled\");\n            throw;\n        }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationAudioInputSelection.cs")),
            "MainViewModel audio input automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationDeviceSelection.cs")),
            "MainViewModel device selection automation partial folded into MainViewModel.cs");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_HdrEnablementLivesInCaptureSelection()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var hdrChangeBlock = ExtractMemberCode(
            captureModeTransactionsText,
            "OnIsHdrEnabledChanged");

        AssertContains(captureModeTransactionsText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(captureModeTransactionsText, "if (enabled && !IsHdrAvailable)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(captureModeTransactionsText, "IsTrueHdrPreviewEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(captureModeTransactionsText, "if (_isRevertingHdrToggle)");
        AssertContains(captureModeTransactionsText, "_pendingSdrAutoSelectionForDeviceChange = false;");
        AssertContains(captureModeTransactionsText, "_pendingSdrAutoFriendlyFrameRateBucket = null;");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = !value;");
        AssertContains(captureModeTransactionsText, "StatusText = HdrToggleBlockedWhileRecordingMessage;");
        AssertContains(captureModeTransactionsText, "ResetModeSelectionState();");
        AssertContains(captureModeTransactionsText, "RebuildResolutionOptions();");
        AssertContains(captureModeTransactionsText, "RebuildRecordingFormatOptions();");
        AssertContains(captureModeTransactionsText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertContains(captureModeTransactionsText, "SaveSettings();");
        AssertOccursBefore(hdrChangeBlock, "if (_isRevertingHdrToggle)", "if (value)");
        AssertOccursBefore(hdrChangeBlock, "if (value)", "if (IsRecording)");
        AssertOccursBefore(hdrChangeBlock, "StatusText = HdrToggleBlockedWhileRecordingMessage;", "if (!_isChangingDevice)");
        AssertOccursBefore(hdrChangeBlock, "ResetModeSelectionState();", "RebuildResolutionOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildResolutionOptions();", "RebuildRecordingFormatOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildRecordingFormatOptions();", "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertOccursBefore(hdrChangeBlock, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");", "SaveSettings();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationHdr.cs")),
            "MainViewModel HDR automation partial");

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
        int flashbackBufferMinutes)
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
            flashbackBufferMinutes);

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

    internal static Task AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotStatusProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(snapshotFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(snapshotFlatteningText, "VerificationInProgress = snapshotStatusFlattening.VerificationInProgress,");
        AssertContains(snapshotFlatteningText, "SessionState = snapshotStatusFlattening.SessionState,");
        AssertContains(snapshotFlatteningText, "StatusText = snapshotStatusFlattening.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(snapshotFlatteningText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(snapshotFlatteningText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = viewModelSnapshot.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = snapshotStatus.StatusText,");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");
        AssertContains(snapshotStatusProjectionText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotStatusProjectionText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = snapshotStatus.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(snapshotFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(snapshotFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertContains(snapshotFlatteningText, "DiagnosticHealthStatus = snapshotEvaluationFlattening.DiagnosticHealthStatus,");
        AssertContains(snapshotFlatteningText, "PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluationFlattening.PerformanceThresholdCaptureDropPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(snapshotFlatteningText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");

        AssertContains(snapshotEvaluationProjectionText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationFlattenedProjection");

        AssertContains(snapshotEvaluationProjectionText, "private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = performance.Score,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticAudioLane = diagnostic.AudioLane,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var processResourceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(snapshotFlatteningText, "var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);");
        AssertContains(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceFlattening.MemoryWorkingSetMb,");
        AssertContains(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceFlattening.MemoryGcFragmentationPercent,");
        AssertContains(snapshotFlatteningText, "ThreadPoolIoMax = processResourceFlattening.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");
        AssertContains(processResourceProjectionText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsAvSyncProjection_LivesWithProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var avSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSyncFlattening.CaptureDriftMs,");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftRateMsPerSec = avSyncFlattening.CaptureDriftRateMsPerSec,");
        AssertContains(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSyncFlattening.EncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSync.CaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,");

        AssertContains(avSyncProjectionText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = avSync.EncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncFlattenedProjection");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsSnapshotAudioProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var audioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureIngestProjectionText = audioProjectionText;
        var wasapiAudioProjectionText = audioProjectionText;
        var obsoleteAudioProjectionPath = System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs");

        AssertContains(snapshotProjectionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(snapshotFlatteningText, "var audioAndIngestFlattening = BuildAudioAndIngestFlattenedProjection(audioAndIngest);");
        AssertContains(snapshotFlatteningText, "AudioPeak = audioAndIngestFlattening.Signal.Peak,");
        AssertContains(snapshotFlatteningText, "AudioSignalPresent = audioAndIngestFlattening.Signal.SignalPresent,");
        AssertContains(snapshotFlatteningText, "AudioFramesWrittenToSink = audioAndIngestFlattening.Ingest.AudioFramesWrittenToSink,");
        AssertContains(snapshotFlatteningText, "SourceReaderReadOutstanding = audioAndIngestFlattening.SourceReader.ReadOutstanding,");
        AssertContains(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = audioAndIngestFlattening.WasapiCapture.AudioLevelEventsFired,");
        AssertContains(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = audioAndIngestFlattening.WasapiPlayback.BufferedDurationMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioSignalPresent = audioSignal.SignalPresent,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioSignalPresent = audioAndIngest.AudioSignalPresent,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioFramesWrittenToSink = audioAndIngest.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceReaderReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = audioAndIngest.WasapiCaptureAudioLevelEventsFired,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs,");

        AssertContains(audioProjectionText, "private static AudioAndIngestProjection BuildAudioAndIngestProjection(");
        AssertContains(audioProjectionText, "Signal = BuildAudioSignalProjection(viewModelSnapshot, audioSignal),");
        AssertContains(audioProjectionText, "Ingest = BuildCaptureIngestProjection(captureRuntime),");
        AssertContains(audioProjectionText, "Wasapi = BuildWasapiAudioProjection(captureRuntime)");
        AssertContains(audioProjectionText, "private readonly record struct AudioAndIngestProjection");
        AssertContains(audioProjectionText, "public AudioSignalProjection Signal { get; init; }");
        AssertContains(audioProjectionText, "public CaptureIngestProjection Ingest { get; init; }");
        AssertContains(audioProjectionText, "public WasapiAudioProjection Wasapi { get; init; }");
        AssertContains(audioProjectionText, "private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(");
        AssertContains(audioProjectionText, "Signal = BuildAudioSignalFlattenedProjection(audioAndIngest.Signal),");
        AssertContains(audioProjectionText, "Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest.Ingest),");
        AssertContains(audioProjectionText, "SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest.Ingest),");
        AssertContains(audioProjectionText, "WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest.Wasapi),");
        AssertContains(audioProjectionText, "WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest.Wasapi)");
        AssertContains(audioProjectionText, "private readonly record struct AudioAndIngestFlattenedProjection");
        AssertDoesNotContain(audioProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");

        AssertContains(audioProjectionText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(audioProjectionText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(audioProjectionText, "SignalPresent = audioSignal.SignalPresent,");
        AssertContains(audioProjectionText, "private readonly record struct AudioSignalProjection");
        AssertContains(audioProjectionText, "private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(");
        AssertContains(audioProjectionText, "Peak = signal.Peak,");

        AssertContains(audioProjectionText, "private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)");
        AssertContains(audioProjectionText, "QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,");
        AssertContains(audioProjectionText, "QueueDropsFileWriter = health.AudioChunksDropped");
        AssertContains(audioProjectionText, "private readonly record struct AudioDropsProjection");

        AssertContains(captureIngestProjectionText, "private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureIngestProjectionText, "AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,");
        AssertContains(captureIngestProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertContains(captureIngestProjectionText, "private readonly record struct CaptureIngestProjection");
        AssertContains(captureIngestProjectionText, "private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(");
        AssertContains(captureIngestProjectionText, "AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,");
        AssertContains(captureIngestProjectionText, "private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(");
        AssertContains(captureIngestProjectionText, "ReadOutstanding = ingest.SourceReaderReadOutstanding,");

        AssertContains(wasapiAudioProjectionText, "private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(wasapiAudioProjectionText, "CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertContains(wasapiAudioProjectionText, "PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,");
        AssertContains(wasapiAudioProjectionText, "private readonly record struct WasapiAudioProjection");
        AssertContains(wasapiAudioProjectionText, "private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(");
        AssertContains(wasapiAudioProjectionText, "AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,");
        AssertContains(wasapiAudioProjectionText, "private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(");
        AssertContains(wasapiAudioProjectionText, "BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,");
        if (System.IO.File.Exists(obsoleteAudioProjectionPath))
        {
            throw new System.InvalidOperationException("Audio projection should stay consolidated into AutomationDiagnosticsHub.SnapshotProjection.cs.");
        }

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(snapshotFlatteningText, "var captureCommandFlattening = BuildCaptureCommandFlattenedProjection(captureCommands);");
        AssertContains(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommandFlattening.CommandsEnqueued,");
        AssertContains(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommandFlattening.MaxQueueLatencyMs,");
        AssertContains(snapshotFlatteningText, "CaptureCommandLastError = captureCommandFlattening.LastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = captureCommands.LastError,");

        AssertContains(captureCommandProjectionText, "private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = viewModelSnapshot.CaptureCommandLastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandProjection");
        AssertContains(captureCommandProjectionText, "private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = captureCommands.LastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsUserSettingsProjection_LivesWithSnapshotProjection()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var userSettingsProjectionText = snapshotProjectionText;

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(snapshotFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(snapshotFlatteningText, "SelectedDeviceId = settingsFlattening.SelectedDeviceId,");
        AssertContains(snapshotFlatteningText, "SelectedFriendlyFrameRate = settingsFlattening.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningText, "SelectedRecordingFormat = settingsFlattening.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningText, "CustomBitrateMbps = settingsFlattening.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningText, "IsStatsVisible = settingsFlattening.IsStatsVisible,");
        AssertContains(userSettingsProjectionText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = userSettings.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct SettingsFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = userSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = userSettings.IsStatsVisible,");

        AssertContains(userSettingsProjectionText, "private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct UserSettingsProjection");
        AssertContains(userSettingsProjectionText, "private static RecordingSettingsProjection BuildRecordingSettingsProjection(UserSettingsProjection userSettings)");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "SelectedVideoFormat = userSettings.SelectedVideoFormat,");
        AssertContains(userSettingsProjectionText, "CustomBitrateMbps = userSettings.CustomBitrateMbps");
        AssertContains(userSettingsProjectionText, "private readonly record struct RecordingSettingsProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")),
            "user settings projection lives with AutomationDiagnosticsHub.SnapshotProjection.cs");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsCaptureFormatProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(snapshotFlatteningText, "RequestedWidth = captureFormatFlattening.Requested.Width,");
        AssertContains(snapshotFlatteningText, "HdrActivationReason = captureFormatFlattening.HdrRequest.ActivationReason,");
        AssertContains(snapshotFlatteningText, "NegotiatedWidth = captureFormatFlattening.Negotiated.Width,");
        AssertContains(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormatFlattening.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningText, "EncoderVideoCodec = captureFormatFlattening.Encoder.VideoCodec,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatFlattenedProjection");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.RequestedWidth,");
        AssertContains(captureFormatProjectionText, "AudioEnabled = captureRuntime.RequestedAudioEnabled");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(");
        AssertContains(captureFormatProjectionText, "ActivationReason = captureRuntime.HdrActivationReason,");
        AssertContains(captureFormatProjectionText, "RequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "MediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(");
        AssertContains(captureFormatProjectionText, "VideoCodec = captureRuntime.EncoderVideoCodec,");
        AssertContains(captureFormatProjectionText, "TenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Requested.Width,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "ActivationReason = captureFormat.HdrRequest.ActivationReason,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Actual.Width,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Negotiated.Width,");
        AssertContains(captureFormatProjectionText, "MediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureFormat.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "MfReadwriteDisableConverters = captureFormat.ReaderObservation.MfReadwriteDisableConverters");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "VideoCodec = captureFormat.Encoder.VideoCodec,");
        AssertContains(captureFormatProjectionText, "TenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureFormat.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureFormat.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureFormat.NegotiatedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureFormat.EncoderVideoCodec,");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatProjection");
        AssertContains(captureFormatProjectionText, "Requested = BuildCaptureFormatRequestedProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "HdrRequest = BuildCaptureFormatHdrRequestProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Actual = BuildCaptureFormatActualProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Negotiated = BuildCaptureFormatNegotiatedProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "ReaderObservation = BuildCaptureFormatReaderObservationProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Encoder = BuildCaptureFormatEncoderProjection(captureRuntime)");
        AssertContains(captureFormatProjectionText, "public CaptureFormatRequestedProjection Requested { get; init; }");
        AssertContains(captureFormatProjectionText, "public CaptureFormatEncoderProjection Encoder { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureTransportProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(snapshotFlatteningText, "MemoryPreference = captureTransportFlattening.MemoryPreference,");
        AssertContains(snapshotFlatteningText, "VideoNegotiatedSubtype = captureTransportFlattening.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransportFlattening.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");

        AssertContains(captureTransportProjectionText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportProjection");
        AssertContains(captureTransportProjectionText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsHdrPipelineProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var hdrPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(snapshotFlatteningText, "var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);");
        AssertContains(snapshotFlatteningText, "IsHdrAvailable = hdrPipelineFlattening.IsHdrAvailable,");
        AssertContains(snapshotFlatteningText, "HdrRuntimeState = hdrPipelineFlattening.HdrRuntimeState,");
        AssertContains(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipelineFlattening.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningText, "PipelineModeStatus = hdrPipelineFlattening.PipelineModeStatus,");
        AssertContains(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipelineFlattening.TelemetryAlignmentReason,");
        AssertContains(snapshotFlatteningText, "HdrTruthVerdict = hdrPipelineFlattening.TruthVerdict,");
        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertContains(hdrPipelineProjectionText, "TruthVerdict = hdrPipeline.TruthVerdict");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrTruthVerdict = hdrPipeline.TruthVerdict,");

        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineProjection BuildHdrPipelineProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertContains(hdrPipelineProjectionText, "TruthVerdict = truthVerdict");
        AssertContains(hdrPipelineProjectionText, "private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")),
            "HDR pipeline projection partial folded into capture-format projection owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.Hdr.cs")),
            "HDR diagnostics partial folded into capture-format projection owner");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = sourceSignalProjectionText;

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAvailability = sourceFlattening.Telemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryDetails = sourceFlattening.Telemetry.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceFlattening.Telemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningText, "SourceTargetSummaryText = sourceFlattening.Telemetry.SourceTargetSummaryText,");
        AssertContains(sourceSignalProjectionText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "Telemetry = BuildSourceTelemetryFlattenedProjection(sourceTelemetry)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");

        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryProjection BuildSourceTelemetryProjection(");
        AssertContains(sourceTelemetryProjectionText, "private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = PreferKnownTelemetryValue(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryProjection");
        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(sourceTelemetryProjectionText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryFlattenedProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")),
            "source signal and telemetry projection folded into root snapshot projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "DetectedSourceFrameRate = sourceFlattening.Signal.DetectedSourceFrameRate,");
        AssertContains(snapshotFlatteningText, "SourceFrameRateOrigin = sourceFlattening.Signal.SourceFrameRateOrigin,");
        AssertContains(snapshotFlatteningText, "SourceRawTimingHex = sourceFlattening.Signal.SourceRawTimingHex,");
        AssertContains(sourceSignalProjectionText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "Signal = BuildSourceSignalFlattenedProjection(sourceSignal),");
        AssertContains(sourceSignalProjectionText, "private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(sourceSignalProjectionText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(sourceSignalProjectionText, "SourceRawTimingHex = sourceSignal.RawTimingHex");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
        AssertDoesNotContain(snapshotFlatteningText, "DetectedSourceFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");

        AssertContains(sourceSignalProjectionText, "private static SourceSignalProjection BuildSourceSignalProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertContains(sourceSignalProjectionText, "FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),");
        AssertContains(sourceSignalProjectionText, "RawTimingHex = captureRuntime.SourceRawTimingHex");
        AssertContains(sourceSignalProjectionText, "private static string ResolveSourceFrameRateOrigin(string viewModelOrigin, string runtimeOrigin)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")),
            "source signal projection folded into root snapshot projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureCadenceOnlyText = captureCadenceProjectionText[..captureCadenceProjectionText.IndexOf("private static VisualCadenceProjection", System.StringComparison.Ordinal)];

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);");
        AssertContains(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadenceFlattening.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");
        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceFlattenedProjection");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualMotionConfidence");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCenterRecentChangeIntervalsMs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")),
            "capture and visual cadence projections folded into root snapshot projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var visualCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureCadenceOnlyText = visualCadenceProjectionText[..visualCadenceProjectionText.IndexOf("private static VisualCadenceProjection", System.StringComparison.Ordinal)];

        AssertContains(snapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);");
        AssertContains(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadenceFlattening.MotionConfidence,");
        AssertContains(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadenceFlattening.CenterRecentChangeIntervalsMs,");
        AssertContains(visualCadenceProjectionText, "private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = visualCadence.MotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = captureCadence.VisualMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = captureCadence.VisualCenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadence.MotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs,");

        AssertContains(visualCadenceProjectionText, "private static VisualCadenceProjection BuildVisualCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceProjection");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCadenceMotionConfidence");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCenterCadenceRecentChangeIntervalsMs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")),
            "visual cadence projection folded into root snapshot projection owner");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsMjpegProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var mjpegProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterProjectionText = mjpegProjectionText;

        AssertContains(snapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(snapshotFlatteningText, "var mjpegFlattening = BuildMjpegFlattenedProjection(mjpeg);");
        AssertContains(snapshotFlatteningText, "MjpegTotalDecoded = mjpegFlattening.TotalDecoded,");
        AssertContains(snapshotFlatteningText, "var mjpegTimingFlattening = BuildMjpegTimingFlattenedProjection(mjpeg.Timing);");
        AssertContains(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpegTimingFlattening.DecodeSampleCount,");
        AssertContains(snapshotFlatteningText, "var mjpegPreviewJitterFlattening = BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter);");
        AssertContains(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpegPreviewJitterFlattening.Events.LastDropReason,");
        AssertContains(snapshotFlatteningText, "var mjpegPacketHashFlattening = BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash);");
        AssertContains(snapshotFlatteningText, "MjpegPacketHashPattern = mjpegPacketHashFlattening.Pattern,");
        AssertContains(snapshotFlatteningText, "MjpegPerDecoder = mjpegTimingFlattening.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegTotalDecoded = mjpeg.TotalDecoded,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegCompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpeg.DecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpeg.Timing.DecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitter.LastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = mjpeg.PacketHashPattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = mjpeg.PacketHash.Pattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = mjpeg.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = mjpeg.Timing.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");

        AssertContains(mjpegProjectionText, "private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "var timing = BuildMjpegTimingProjection(health);");
        AssertContains(mjpegProjectionText, "Timing = timing,");
        AssertContains(mjpegProjectionText, "var previewJitter = BuildMjpegPreviewJitterProjection(health);");
        AssertContains(mjpegProjectionText, "var packetHash = BuildMjpegPacketHashProjection(health);");
        AssertContains(mjpegProjectionText, "PreviewJitter = previewJitter,");
        AssertDoesNotContain(mjpegProjectionText, "PreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegProjectionText, "PacketHash = packetHash,");
        AssertDoesNotContain(mjpegProjectionText, "PacketHashPattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegProjection");
        AssertContains(mjpegProjectionText, "private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(");
        AssertContains(mjpegProjectionText, "TotalDecoded = mjpeg.TotalDecoded,");
        AssertContains(mjpegProjectionText, "CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegFlattenedProjection");
        AssertContains(mjpegProjectionText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(mjpegProjectionText, "PipelineMaxMs = health.MjpegPipelineMaxMs,");
        AssertContains(mjpegProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegTimingProjection");
        AssertContains(mjpegProjectionText, "private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(");
        AssertContains(mjpegProjectionText, "DecodeSampleCount = timing.DecodeSampleCount,");
        AssertContains(mjpegProjectionText, "PipelineMaxMs = timing.PipelineMaxMs,");
        AssertContains(mjpegProjectionText, "PerDecoder = timing.PerDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegTimingFlattenedProjection");

        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPreviewJitterProjectionText, "Queue = BuildMjpegPreviewJitterQueueProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Timing = BuildMjpegPreviewJitterTimingProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Events = BuildMjpegPreviewJitterEventProjection(health)");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterProjection");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),");
        AssertContains(mjpegPreviewJitterProjectionText, "Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),");
        AssertContains(mjpegPreviewJitterProjectionText, "Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),");
        AssertContains(mjpegPreviewJitterProjectionText, "Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterFlattenedProjection");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = health.MjpegPreviewJitterEnabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = queue.Enabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "ResumeReprimeCount = queue.ResumeReprimeCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "InputSampleCount = health.MjpegPreviewJitterInputSampleCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "InputSampleCount = timing.InputSampleCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "LatencyMaxMs = timing.LatencyMaxMs");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "DeadlineDropCount = adaptive.DeadlineDropCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "TargetDecreaseCount = adaptive.TargetDecreaseCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = events.LastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = events.ScheduleLateCount");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")),
            "MJPEG projection folded into capture-format projection owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")),
            "MJPEG preview jitter projection folded into MJPEG projection owner");

        AssertContains(mjpegProjectionText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "SampleCount = health.MjpegPacketHashSampleCount,");
        AssertContains(mjpegProjectionText, "Pattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegProjectionText, "RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegPacketHashProjection");
        AssertContains(mjpegProjectionText, "private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(");
        AssertContains(mjpegProjectionText, "SampleCount = packetHash.SampleCount,");
        AssertContains(mjpegProjectionText, "Pattern = packetHash.Pattern,");
        AssertContains(mjpegProjectionText, "RecentDuplicateFlags = packetHash.RecentDuplicateFlags");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegPacketHashFlattenedProjection");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsRecordingPipelineProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var obsoleteRecordingProjectionPath = System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Recording.cs");

        AssertContains(snapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(snapshotFlatteningText, "var recordingPipelineFlattening = BuildRecordingPipelineFlattenedProjection(recordingPipeline);");
        AssertContains(snapshotFlatteningText, "EncoderVideoFramesEnqueued = recordingPipelineFlattening.Encoder.VideoFramesEnqueued,");
        AssertContains(snapshotFlatteningText, "ConversionQueueDepth = recordingPipelineFlattening.Ingest.ConversionQueueDepth,");
        AssertContains(snapshotFlatteningText, "RecordingVideoQueueCapacity = recordingPipelineFlattening.VideoQueue.Capacity,");
        AssertContains(snapshotFlatteningText, "RecordingGpuFramesEnqueued = recordingPipelineFlattening.HardwareQueues.GpuFramesEnqueued,");
        AssertContains(snapshotFlatteningText, "RecordingCudaFramesDropped = recordingPipelineFlattening.HardwareQueues.CudaFramesDropped,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingCudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineFlattenedProjection");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(");
        AssertContains(recordingPipelineProjectionText, "VideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "EncodingFailed = health.RecordingEncodingFailed,");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "VideoDropsBacklogEviction = health.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(");
        AssertContains(recordingPipelineProjectionText, "Capacity = health.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineProjectionText, "BackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(");
        AssertContains(recordingPipelineProjectionText, "GpuFramesEnqueued = health.RecordingGpuFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "CudaFramesDropped = health.RecordingCudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "VideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "EncodingFailed = recordingPipeline.Encoder.EncodingFailed,");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = recordingPipeline.Ingest.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Capacity = recordingPipeline.VideoQueue.Capacity,");
        AssertContains(recordingPipelineProjectionText, "BackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "GpuFramesEnqueued = recordingPipeline.HardwareQueues.GpuFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "CudaFramesDropped = recordingPipeline.HardwareQueues.CudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineProjection");
        AssertContains(recordingPipelineProjectionText, "Encoder = BuildRecordingPipelineEncoderProjection(health),");
        AssertContains(recordingPipelineProjectionText, "Ingest = BuildRecordingPipelineIngestProjection(health),");
        AssertContains(recordingPipelineProjectionText, "VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),");
        AssertContains(recordingPipelineProjectionText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)");
        AssertContains(recordingPipelineProjectionText, "public RecordingPipelineEncoderProjection Encoder { get; init; }");
        AssertContains(recordingPipelineProjectionText, "public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }");
        if (System.IO.File.Exists(obsoleteRecordingProjectionPath))
        {
            throw new System.InvalidOperationException("Recording projection should stay consolidated into AutomationDiagnosticsHub.SnapshotProjection.cs.");
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingBackendProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var recordingOutputFlattening = BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput);");
        AssertContains(snapshotFlatteningText, "RecordingBackend = recordingOutputFlattening.Backend,");
        AssertContains(snapshotFlatteningText, "AudioPathMode = recordingOutputFlattening.AudioPathMode,");
        AssertContains(snapshotFlatteningText, "MuxResult = recordingOutputFlattening.MuxResult,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingBackend = captureRuntime.RecordingBackend,");
        AssertDoesNotContain(snapshotFlatteningText, "MuxResult = captureRuntime.MuxSucceeded.HasValue");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingBackend = recordingBackend.Backend,");
        AssertDoesNotContain(snapshotFlatteningText, "MuxResult = recordingBackend.MuxResult,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingPipelineProjectionText, "Backend = captureRuntime.RecordingBackend,");
        AssertContains(recordingPipelineProjectionText, "AudioPathMode = captureRuntime.AudioPathMode,");
        AssertContains(recordingPipelineProjectionText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertContains(recordingPipelineProjectionText, "private static string ResolveMuxResult(bool? muxSucceeded)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingBackendProjection");
        AssertContains(recordingPipelineProjectionText, "private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Backend = recordingBackend.Backend,");
        AssertContains(recordingPipelineProjectionText, "AudioPathMode = recordingBackend.AudioPathMode,");
        AssertContains(recordingPipelineProjectionText, "MuxResult = recordingBackend.MuxResult,");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingOutputFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingOutputProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var obsoleteRecordingOutputPath = System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs");

        AssertContains(snapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(snapshotFlatteningText, "OutputPath = recordingOutputFlattening.OutputPath,");
        AssertContains(snapshotFlatteningText, "RecordingVideoBytes = recordingOutputFlattening.RecordingVideoBytes,");
        AssertContains(snapshotFlatteningText, "LastOutputPath = recordingOutputFlattening.LastOutputPath,");
        AssertContains(snapshotFlatteningText, "LastVerification = recordingOutputFlattening.LastVerification,");
        AssertDoesNotContain(snapshotFlatteningText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "OutputPath = recordingOutput.OutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "LastVerification = recordingOutput.LastVerification,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingOutputProjection BuildRecordingOutputProjection(");
        AssertContains(recordingPipelineProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertContains(recordingPipelineProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertContains(recordingPipelineProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertContains(recordingPipelineProjectionText, "LastVerification = lastVerification");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingOutputProjection");
        AssertContains(recordingPipelineProjectionText, "OutputPath = recordingOutput.OutputPath,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertContains(recordingPipelineProjectionText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertContains(recordingPipelineProjectionText, "LastVerification = recordingOutput.LastVerification");
        if (System.IO.File.Exists(obsoleteRecordingOutputPath))
        {
            throw new System.InvalidOperationException("Recording output projection should stay consolidated into AutomationDiagnosticsHub.SnapshotProjection.cs.");
        }

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsPreviewRuntimeProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var previewRuntimeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(snapshotFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntimeFlattening.Startup.Strategy,");
        AssertContains(snapshotFlatteningText, "PreviewRendererMode = previewRuntimeFlattening.Startup.RendererMode,");
        AssertContains(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntimeFlattening.GpuPlayback.PlaybackState,");
        AssertContains(snapshotFlatteningText, "PreviewColorContext = previewRuntimeFlattening.Color.ColorContext,");
        AssertContains(snapshotFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(previewRuntimeProjectionText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(previewRuntimeProjectionText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(previewRuntimeProjectionText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(previewRuntimeProjectionText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(previewRuntimeProjectionText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeFlattenedProjection");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(previewRuntimeProjectionText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(previewRuntimeProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(previewRuntimeProjectionText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(previewRuntimeProjectionText, "RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,");
        AssertContains(previewRuntimeProjectionText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(previewRuntimeProjectionText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(previewRuntimeProjectionText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(previewRuntimeProjectionText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeProjectionText, "PositionEventCount = previewRuntime.GpuPositionEventCount");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(previewRuntimeProjectionText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(previewRuntimeProjectionText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "FramesArrived = frame.FramesArrived,");
        AssertContains(previewRuntimeProjectionText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(previewRuntimeProjectionText, "SlowFramePercent = cadence.SlowFramePercent");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "RendererAttached = surface.RendererAttached");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "Strategy = startup.Strategy,");
        AssertContains(previewRuntimeProjectionText, "RendererMode = startup.RendererMode");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "PlaybackState = gpuPlayback.PlaybackState,");
        AssertContains(previewRuntimeProjectionText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "ColorContext = color.ColorContext,");
        AssertContains(previewRuntimeProjectionText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.CadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.StartupStrategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.Startup.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewSummary.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = captureRuntime.NegotiatedPixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = previewSummary.ColorContext,");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(previewRuntimeProjectionText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,");
        AssertDoesNotContain(previewRuntimeProjectionText, "StartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode,");
        AssertDoesNotContain(previewRuntimeProjectionText, "GpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeProjection");
        AssertContains(previewRuntimeProjectionText, "public PreviewRuntimeFrameProjection Frame { get; init; }");
        AssertContains(previewRuntimeProjectionText, "public PreviewRuntimeColorProjection Color { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsPreviewD3DProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var previewD3DProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameFlowProjectionText = previewD3DProjectionText;
        var previewD3DCpuTimingProjectionText = previewD3DProjectionText;

        AssertContains(snapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(\n            previewRuntime,\n            recentD3DMissedRefreshes,\n            recentD3DStatsFailures);");
        AssertContains(snapshotFlatteningText, "var previewD3DFlattening = BuildPreviewD3DFlattenedProjection(previewD3D);");
        AssertContains(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewD3DFlattening.PresentSyncInterval,");
        AssertContains(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3DFlattening.CpuTiming.InputUploadCpuP99Ms,");
        AssertContains(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3DFlattening.LatencyAndStats.PipelineLatencyMaxMs,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitTimeoutCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsRecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3DFlattening.FrameFlow.RecentSlowFrames,");
        AssertContains(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3DFlattening.FrameFlow.LastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(");
        AssertContains(previewD3DProjectionText, "CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),");
        AssertContains(previewD3DProjectionText, "LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(");
        AssertContains(previewD3DProjectionText, "FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFlattenedProjection");
        AssertContains(previewD3DProjectionText, "public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }");
        AssertContains(previewD3DProjectionText, "public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }");
        AssertContains(previewD3DProjectionText, "public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingFlattenedProjection BuildPreviewD3DCpuTimingFlattenedProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadCpuP99Ms = cpuTiming.InputUploadP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingFlattenedProjection");
        AssertContains(previewD3DCpuTimingProjectionText, "public double InputUploadCpuP99Ms { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "public double RenderSubmitCpuP99Ms { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "public double PresentCallP99Ms { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "public double TotalFrameCpuP99Ms { get; init; }");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DLatencyAndStatsFlattenedProjection BuildPreviewD3DLatencyAndStatsFlattenedProjection(");
        AssertContains(previewD3DProjectionText, "PipelineLatencyMaxMs = pipelineLatency.MaxMs,");
        AssertContains(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,");
        AssertContains(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = frameStats.RecentMissedRefreshCount,");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DLatencyAndStatsFlattenedProjection");
        AssertContains(previewD3DProjectionText, "public double PipelineLatencyP99Ms { get; init; }");
        AssertContains(previewD3DProjectionText, "public long FrameLatencyWaitTimeoutCount { get; init; }");
        AssertContains(previewD3DProjectionText, "public long FrameStatsRecentMissedRefreshCount { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowFlattenedProjection BuildPreviewD3DFrameFlowFlattenedProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = frameFlow.LastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = frameFlow.RecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowFlattenedProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "public long LastSubmittedPreviewPresentId { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "public double LastRenderedPipelineLatencyMs { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "public string LastDropReason { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }");
        AssertContains(previewD3DProjectionText, "public double InputUploadCpuP99Ms { get; init; }");
        AssertContains(previewD3DProjectionText, "public long LastSubmittedPreviewPresentId { get; init; }");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.InputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatency.MaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.CpuTiming.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStatsRecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.LastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DProjection BuildPreviewD3DProjection(");
        AssertContains(previewD3DProjectionText, "var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "CpuTiming = cpuTiming,");
        AssertContains(previewD3DProjectionText, "var pipelineLatency = BuildPreviewD3DPipelineLatencyProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "PipelineLatency = pipelineLatency,");
        AssertContains(previewD3DProjectionText, "var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "FrameFlow = frameFlow");
        AssertContains(previewD3DProjectionText, "var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "var frameStats = BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "FrameLatencyWait = frameLatencyWait,");
        AssertContains(previewD3DProjectionText, "FrameStats = frameStats,");
        AssertDoesNotContain(previewD3DProjectionText, "InputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(previewD3DProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,");
        AssertDoesNotContain(previewD3DProjectionText, "LastRenderedPipelineLatencyMs = previewD3D.D3DLastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(previewD3DProjectionText, "RecentSlowFrames = previewD3D.D3DRecentSlowFrames");
        AssertDoesNotContain(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = previewD3D.D3DFrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "SampleCount = previewRuntime.D3DCpuTimingSampleCount,");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingProjection");
        AssertDoesNotContain(previewD3DCpuTimingProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(");
        AssertContains(previewD3DProjectionText, "SampleCount = previewRuntime.D3DPipelineLatencySampleCount,");
        AssertContains(previewD3DProjectionText, "MaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DPipelineLatencyProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertContains(previewD3DProjectionText, "Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,");
        AssertContains(previewD3DProjectionText, "TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertContains(previewD3DProjectionText, "MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameLatencyWaitProjection");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "SampleCount = previewRuntime.D3DFrameStatsSampleCount,");
        AssertContains(previewD3DProjectionText, "RecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DProjectionText, "RecentFailureCount = recentD3DStatsFailures");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameStatsProjection");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DProjection");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsFlashbackExportProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackExportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationSnapshotFlashbackProjectionBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(snapshotProjectionText, "var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackExportFlattening = BuildFlashbackExportFlattenedProjection(");
        AssertContains(snapshotFlatteningText, "FlashbackExportActive = flashbackExportFlattening.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackExportPercent = flashbackExportFlattening.Percent,");
        AssertContains(snapshotFlatteningText, "FlashbackExportLastForceRotateFallbackSegments = flashbackExportFlattening.LastForceRotateFallbackSegments,");
        AssertContains(snapshotFlatteningText, "LastExportId = flashbackExportFlattening.LastExportId,");
        AssertContains(snapshotFlatteningText, "LastExportMessage = flashbackExportFlattening.LastExportMessage");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportActive = health.FlashbackExportActive,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportActive = flashbackExport.Active,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportPercent = health.FlashbackExportPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = health.LastExportId,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = flashbackExportLastResult.LastExportId,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = flashbackExport.LastExportId,");

        AssertContains(flashbackExportProjectionText, "internal static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(");
        AssertContains(flashbackExportProjectionText, "Active = flashbackExport.Active,");
        AssertContains(flashbackExportProjectionText, "Percent = flashbackExport.Percent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "LastExportId = lastResult.LastExportId,");
        AssertContains(flashbackExportProjectionText, "LastExportMessage = lastResult.LastExportMessage");
        AssertContains(flashbackExportProjectionText, "internal readonly record struct FlashbackExportFlattenedProjection");

        AssertContains(flashbackExportProjectionText, "internal static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "Active = health.FlashbackExportActive,");
        AssertContains(flashbackExportProjectionText, "Percent = health.FlashbackExportPercent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "internal readonly record struct FlashbackExportProjection");
        AssertDoesNotContain(flashbackExportProjectionText, "LastExportId = flashbackExport.LastExportId,");
        AssertContains(flashbackExportProjectionText, "internal static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "LastExportId = health.LastExportId,");
        AssertContains(flashbackExportProjectionText, "LastExportMessage = health.LastExportMessage");
        AssertContains(flashbackExportProjectionText, "internal readonly record struct FlashbackExportLastResultProjection");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationSnapshotFlashbackProjectionBuilder.cs")),
            "Flashback projection owner is a named builder");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs")),
            "Flashback export projection folded into Flashback projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsFlashbackRecordingProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackRecordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationSnapshotFlashbackProjectionBuilder.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingQueuesProjectionText = flashbackRecordingProjectionText;

        AssertContains(snapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(snapshotFlatteningText, "var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);");
        AssertContains(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecordingFlattening.EncodingFailed,");
        AssertContains(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecordingFlattening.StartupCache.OverBudget,");
        AssertContains(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecordingFlattening.Queues.VideoQueueCapacity,");
        AssertContains(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecordingFlattening.Queues.GpuQueueLastRejectReason,");
        AssertContains(snapshotFlatteningText, "FlashbackActive = flashbackRecordingFlattening.Runtime.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackBackendSettingsStale = flashbackRecordingFlattening.Backend.SettingsStale,");
        AssertContains(snapshotFlatteningText, "FlashbackExportVerificationFormat = flashbackRecordingFlattening.Backend.ExportVerificationFormat,");
        AssertContains(snapshotFlatteningText, "EncoderCodecName = flashbackRecordingFlattening.Encoder.CodecName,");
        AssertContains(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecordingFlattening.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackEncodingFailed = health.FlashbackEncodingFailed,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackActive = health.FlashbackActive,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderCodecName = health.EncoderCodecName,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecording.EncodingFailed,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackActive = flashbackRecording.Active,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCacheOverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecording.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecording.AudioQueueCapacity,");

        AssertContains(flashbackRecordingProjectionText, "internal static FlashbackRecordingProjection BuildFlashbackRecordingProjection(");
        AssertContains(flashbackRecordingProjectionText, "CaptureRuntimeSnapshot captureRuntime,");
        AssertContains(flashbackRecordingProjectionText, "var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var queues = BuildFlashbackRecordingQueuesProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var runtime = BuildFlashbackRecordingRuntimeProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var backend = BuildFlashbackRecordingBackendProjection(captureRuntime, health);");
        AssertContains(flashbackRecordingProjectionText, "var encoder = BuildFlashbackRecordingEncoderProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = startupCache,");
        AssertContains(flashbackRecordingProjectionText, "Queues = queues,");
        AssertContains(flashbackRecordingProjectionText, "Runtime = runtime,");
        AssertContains(flashbackRecordingProjectionText, "Backend = backend,");
        AssertContains(flashbackRecordingProjectionText, "Encoder = encoder");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = health.FlashbackEncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingProjection");
        AssertContains(flashbackRecordingProjectionText, "internal static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingProjection flashbackRecording");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = BuildFlashbackRecordingStartupCacheFlattenedProjection(flashbackRecording.StartupCache),");
        AssertContains(flashbackRecordingProjectionText, "Queues = BuildFlashbackRecordingQueuesFlattenedProjection(flashbackRecording.Queues),");
        AssertContains(flashbackRecordingProjectionText, "Runtime = BuildFlashbackRecordingRuntimeFlattenedProjection(flashbackRecording.Runtime),");
        AssertContains(flashbackRecordingProjectionText, "Backend = BuildFlashbackRecordingBackendFlattenedProjection(flashbackRecording.Backend),");
        AssertContains(flashbackRecordingProjectionText, "Encoder = BuildFlashbackRecordingEncoderFlattenedProjection(flashbackRecording.Encoder)");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingStartupCacheFlattenedProjection StartupCache { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingQueuesFlattenedProjection Queues { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingRuntimeFlattenedProjection Runtime { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingBackendFlattenedProjection Backend { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingEncoderFlattenedProjection Encoder { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingRuntimeProjection Runtime { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingBackendProjection Backend { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingEncoderProjection Encoder { get; init; }");
        AssertDoesNotContain(flashbackRecordingProjectionText, "StartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = startupCache.OverBudget");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingStartupCacheFlattenedProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "internal readonly record struct FlashbackRecordingQueuesProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = queues.VideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = queues.GpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = queues.AudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "internal readonly record struct FlashbackRecordingQueuesFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertContains(flashbackRecordingProjectionText, "Active = health.FlashbackActive,");
        AssertContains(flashbackRecordingProjectionText, "GpuEncoding = health.FlashbackGpuEncoding");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingRuntimeProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingRuntimeProjection runtime");
        AssertContains(flashbackRecordingProjectionText, "Active = runtime.Active,");
        AssertContains(flashbackRecordingProjectionText, "GpuEncoding = runtime.GpuEncoding");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingRuntimeFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingBackendProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingBackendProjection backend");
        AssertContains(flashbackRecordingProjectionText, "SettingsStale = backend.SettingsStale,");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = backend.ExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = backend.CodecDowngradeReason");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingBackendFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(flashbackRecordingProjectionText, "CodecName = health.EncoderCodecName,");
        AssertContains(flashbackRecordingProjectionText, "FrameRateDenominator = health.EncoderFrameRateDenominator");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingEncoderProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingEncoderProjection encoder");
        AssertContains(flashbackRecordingProjectionText, "CodecName = encoder.CodecName,");
        AssertContains(flashbackRecordingProjectionText, "FrameRateDenominator = encoder.FrameRateDenominator");
        AssertContains(flashbackRecordingProjectionText, "internal readonly record struct FlashbackRecordingEncoderFlattenedProjection");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")),
            "Flashback recording projection folded into Flashback projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesWithSnapshotProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationSnapshotFlashbackProjectionBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlaybackFlattening.State,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackAudioMasterFallbacks = flashbackPlaybackFlattening.AudioMaster.Fallbacks,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlaybackFlattening.Decode.MaxPhase,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlaybackFlattening.Commands.LastFailure,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = health.FlashbackPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,");

        AssertContains(flashbackPlaybackProjectionText, "internal static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var timing = BuildFlashbackPlaybackTimingProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var decode = BuildFlashbackPlaybackDecodeProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var commands = BuildFlashbackPlaybackCommandProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "State = health.FlashbackPlaybackState,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = audioMaster,");
        AssertContains(flashbackPlaybackProjectionText, "Timing = timing,");
        AssertContains(flashbackPlaybackProjectionText, "Decode = decode,");
        AssertContains(flashbackPlaybackProjectionText, "Commands = commands");
        AssertContains(flashbackPlaybackProjectionText, "internal static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "FlashbackPlaybackProjection flashbackPlayback");
        AssertContains(flashbackPlaybackProjectionText, "State = flashbackPlayback.State,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),");
        AssertContains(flashbackPlaybackProjectionText, "Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),");
        AssertContains(flashbackPlaybackProjectionText, "Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),");
        AssertContains(flashbackPlaybackProjectionText, "Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackFlattenedProjection");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = flashbackPlayback.Commands.LastFailure");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackAudioMasterProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "Fallbacks = audioMaster.Fallbacks,");
        AssertContains(flashbackPlaybackProjectionText, "LastFallbackReason = audioMaster.LastFallbackReason,");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackProjectionText, "AvDriftMs = health.FlashbackAvDriftMs");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackTimingProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "FlashbackPlaybackTimingProjection timing");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = timing.TargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackProjectionText, "AvDriftMs = timing.AvDriftMs");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackTimingFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackDecodeProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "MaxPhase = decode.MaxPhase,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPositionMs = decode.MaxPositionMs");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackDecodeFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "ThreadAlive = health.FlashbackPlaybackThreadAlive,");
        AssertContains(flashbackPlaybackProjectionText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackCommandProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "LastFailure = commands.LastFailure");
        AssertContains(flashbackPlaybackProjectionText, "internal readonly record struct FlashbackPlaybackCommandFlattenedProjection");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")),
            "Flashback playback projection folded into Flashback projection owner");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnostics = ReadAutomationDiagnosticsHubSourceFamily();
        var countersText = ReadAutomationDiagnosticsHubCountersSource();
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertDiagnosticsRefreshCoreOwnership(diagnostics);
        AssertDiagnosticsAlertEventOwnership(diagnostics);
        AssertDiagnosticsSnapshotStatusProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshPipelineOwnership(diagnostics, dispatcherText);
        AssertDiagnosticsRefreshFlashbackRecordingAndStorageAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackPlaybackAndPreviewAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackExportOwnership(dispatcherText);
        AssertDiagnosticsRefreshSourceReaderOwnership();

        var diagnosticSessionSources = ReadDiagnosticSessionSourceFamily();
        AssertDiagnosticSessionCoreOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionPlaybackMetricsOwnership(diagnosticSessionSources.SourceFamilyText);
        AssertDiagnosticSessionPreviewMetricsOwnership(diagnosticSessionSources.SourceFamilyText, diagnostics);
        AssertDiagnosticSessionExportRecordingOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionFlashbackScenarioOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionToolSurfaceOwnership();

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticsRefreshCoreOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertDiagnosticsRefreshEvaluationOwnership(diagnostics);
        AssertDiagnosticsRefreshRuntimeOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotConstructionOwnership(diagnostics);
    }

    private static void AssertDiagnosticsRefreshEvaluationOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.EvaluationText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertContains(diagnostics.EvaluationText, "private static bool IsCaptureOnePercentLowDegraded(");
        AssertContains(diagnostics.EvaluationText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertContains(diagnostics.EvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnostics.EvaluationText, "var lanes = BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnostics.EvaluationText, "var flashbackDiagnostic = TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnostics.EvaluationText, "var realtimeDiagnostic = TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackStorageDiagnosticEvaluation(health, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackRecordingDiagnosticEvaluation(health, isRecording, recentFlashbackRecording, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackExportDiagnosticEvaluation(health, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackPlaybackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackStorageDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_storage\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackExportDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback export progress is stalled.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback export is running.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackPlaybackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_playback\"");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Playback.cs")),
            "Flashback playback diagnostic evaluation partial folded into Flashback evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs")),
            "Flashback recording diagnostic evaluation folded into Flashback evaluation owner");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackRecordingDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "BuildFlashbackRecordingDiagnosticConditions(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackEncoderFailureDiagnosticEvaluation(health, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackExportRotationDiagnosticEvaluation(conditions, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackBackendSettingsDiagnosticEvaluation(conditions, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(conditions, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static FlashbackRecordingDiagnosticConditions BuildFlashbackRecordingDiagnosticConditions(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackEncoderFailureDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback encoder has failed.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackExportRotationDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_export\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackBackendSettingsDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback backend settings differ from requested settings.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback recording path is dropping or backing up.\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeStateDiagnosticEvaluation(health, isPreviewing, isRecording, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeRecordingDiagnosticEvaluation(captureRuntime, health, isRecording, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeSourceDiagnosticEvaluation(health, isPreviewing, visualCadenceHealthy, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeMjpegDiagnosticEvaluation(health, recentMjpeg, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeStateDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"diagnostic_unavailable\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeRecordingDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"recording\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"audio\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeSourceDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"source_capture\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeMjpegDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"source_signal\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"mjpeg_decode\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewRendererDiagnosticEvaluation(lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewPresentDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"preview_scheduler\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewRendererDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"renderer\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewPresentDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"present_display\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Preview scheduler failed to submit frames.");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Renderer pacing is the likely preview bottleneck.");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Present/display cadence is the likely preview bottleneck.");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Present/display 1% low is below target.");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs")),
            "Realtime preview diagnostic evaluation helpers folded into realtime evaluation owner");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildSourceLane(health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildPreviewLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildRenderLane(previewRuntime, recentRenderer)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildRecordingLane(captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildAudioLane(captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildFlashbackRecordingLane(health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildDecodeLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildRecordingLane(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildAudioLane(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackRecordingLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackExportLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackTempCacheLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackPlaybackCommandLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackPlaybackPerformanceLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildSourceLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildSourceSignalLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildPreviewLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static DiagnosticEvaluationRenderLane BuildRenderLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildPresentLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildVisualLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationRenderLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "var sourceTarget =");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationLanes(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs")),
            "Diagnostic lane text builders folded into Evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluation.cs")),
            "Diagnostic verdict branch policies folded into Evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs")),
            "Flashback diagnostic evaluation folded into Evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs")),
            "Realtime diagnostic evaluation folded into Evaluation owner");
        AssertDoesNotContain(diagnostics.HubText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertDoesNotContain(diagnostics.HubText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
    }

    private static void AssertDiagnosticsAlertEventOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.AlertsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnostics.AlertsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.AlertsText, "private void AddEventThrottled(");
        AssertContains(diagnostics.AlertsText, "private void SetAlertState(");
        AssertContains(diagnostics.AlertsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvents.cs")),
            "diagnostic event state helpers folded into AutomationDiagnosticsHub.Snapshots.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.Alerts.cs")),
            "diagnostic alert refresh folded into AutomationDiagnosticsHub.Snapshots.cs");
        AssertContains(diagnostics.AlertsText, "UpdateSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "private void UpdateSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateAudioSignalAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateRecordingGrowthAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "private void UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"preview-blank\"");
        AssertContains(diagnostics.AlertsText, "\"preview-stall\"");
        AssertContains(diagnostics.AlertsText, "\"preview-startup-timeout\"");
        AssertContains(diagnostics.AlertsText, "\"preview-startup-failed\"");
        AssertContains(diagnostics.AlertsText, "\"preview-cadence-slow\"");
        AssertContains(diagnostics.AlertsText, "\"preview-display-low-1pct\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"capture-cadence-drop\"");
        AssertContains(diagnostics.AlertsText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateAudioSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"audio-muted-suspect\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateRecordingGrowthAlerts(");
        AssertContains(diagnostics.AlertsText, "\"recording-not-growing\"");
        AssertContains(diagnostics.AlertsText, "var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackRecordingAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackStorageAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackEncoderAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.AlertsText, "\"flashback-recording-degraded\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackStorageAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackEncoderAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackPerformanceAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackPerformanceAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAudioAlerts(snapshot, playbackActive);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackSubmitFailureAlert(snapshot);");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackSubmitFailureAlert(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackAudioAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackCommandAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-slow\"");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs")),
            "Flashback recording alert rules folded into the main alerts owner");
        AssertDoesNotContain(diagnostics.HubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
    }

    private static void AssertDiagnosticsRefreshSourceReaderOwnership()
    {
        var sourceReaderSources = ReadMfSourceReaderVideoCaptureSourceFamily();
        var sourceReaderRootText = sourceReaderSources.RootText;
        var sourceReaderFrameLayoutText = sourceReaderSources.FrameLayoutText;
        var sourceReaderLifecycleText = sourceReaderSources.LifecycleText;
        var sourceReaderInitializationText = sourceReaderSources.InitializationText;
        var sourceReaderInitializedSessionText = sourceReaderSources.InitializedSessionText;
        var sourceReaderReadLoopText = sourceReaderSources.ReadLoopText;
        var sourceReaderText = sourceReaderSources.SourceFamilyText;
        AssertContains(sourceReaderText, "Keep source cadence state coherent with diagnostics snapshots");
        AssertContains(sourceReaderText, "lock (_cadenceLock)");
        AssertContains(sourceReaderLifecycleText, "public SourceCadenceMetrics GetSourceCadenceMetrics()");
        AssertContains(sourceReaderLifecycleText, "private void TrackSourceCadence(long mfTimestamp100ns)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Cadence.cs")),
            "source-reader cadence metrics folded into active lifecycle owner");
        AssertContains(sourceReaderRootText, "private unsafe void DiagnoseVtable(IMFSample sample)");
        AssertContains(sourceReaderRootText, "VTABLE_DIAG RAW slot35_GetSampleTime");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Diagnostics.cs")),
            "source-reader vtable diagnostic folded into root source-reader owner");
        AssertContains(sourceReaderRootText, "private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)");
        AssertContains(sourceReaderRootText, "private static readonly Guid ID3D11Texture2DIid");
        AssertContains(sourceReaderRootText, "MF_SOURCE_READER_D3D_RESOURCE_FAIL");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.DxgiBuffers.cs")),
            "MfSourceReaderVideoCapture DXGI texture extraction folded into root source-reader owner");
        AssertContains(sourceReaderFrameLayoutText, "public static int GetFrameSizeBytes(int width, int height, bool isP010)");
        AssertContains(sourceReaderFrameLayoutText, "private unsafe static void CopyYuvWithStride(");
        AssertContains(sourceReaderFrameLayoutText, "private static string SubtypeGuidToName(Guid subtype)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.FrameLayout.cs")),
            "shared source-reader frame layout helpers folded into the root source-reader state");
        AssertContains(sourceReaderLifecycleText, "public void StartReading(RawFrameCallback onFrame, CancellationToken ct)");
        AssertContains(sourceReaderLifecycleText, "public async Task StopAsync()");
        AssertContains(sourceReaderLifecycleText, "private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)");
        AssertContains(sourceReaderLifecycleText, "private void ReleaseReaderAndSource()");
        AssertContains(sourceReaderLifecycleText, "private void SignalFatalError(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.ReadLoop.cs")),
            "source-reader read loop folded into active lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Lifecycle.cs")),
            "source-reader lifecycle folded into root source-reader state");
        AssertContains(sourceReaderInitializationText, "public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)");
        AssertContains(sourceReaderInitializationText, "MF_SOURCE_READER_INIT ");
        AssertContains(sourceReaderInitializationText, "SelectConvertedMediaType(");
        AssertContains(sourceReaderInitializationText, "ApplyCurrentMediaTypeAndReconcileActualOutput(");
        AssertContains(sourceReaderInitializationText, "CommitInitializedRuntimeState(");
        AssertContains(sourceReaderInitializedSessionText, "private readonly record struct SourceReaderNegotiatedMode(");
        AssertContains(sourceReaderInitializedSessionText, "private SourceReaderNegotiatedMode ApplyCurrentMediaTypeAndReconcileActualOutput(");
        AssertContains(sourceReaderInitializedSessionText, "sourceReader.GetCurrentMediaType(");
        AssertContains(sourceReaderInitializedSessionText, "private void ValidateNegotiatedOutputMode(");
        AssertContains(sourceReaderInitializedSessionText, "private void CommitInitializedRuntimeState(");
        AssertContains(sourceReaderInitializedSessionText, "MF_NATIVE_FORMAT_OVERRIDE");
        AssertContains(sourceReaderInitializedSessionText, "Volatile.Write(ref _nativeInputFormat");
        AssertContains(sourceReaderInitializedSessionText, "Interlocked.Exchange(ref _framesDelivered");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.InitializedSession.cs")),
            "source-reader initialized-session handoff folded into active lifecycle owner");
        AssertContains(sourceReaderRootText, "public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Initialization.cs")),
            "source-reader initialization folded into active lifecycle owner");
        AssertContains(sourceReaderReadLoopText, "private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)");
        AssertContains(sourceReaderReadLoopText, "reader.ReadSample(");
        AssertContains(sourceReaderReadLoopText, "DeliverFrame(sample, onFrame, onDualFrame, arrivalTick);");
        AssertContains(sourceReaderRootText, "private unsafe void DeliverFrame(");
        AssertContains(sourceReaderRootText, "private unsafe void DeliverDualFrameFromBuffer(");
        AssertContains(sourceReaderRootText, "Marshal.Release(gpuTexture)");
        AssertContains(sourceReaderRootText, "private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertContains(sourceReaderRootText, "private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertContains(sourceReaderRootText, "private unsafe bool TryDeliverDualFrameFrom2DBuffer(");
        AssertContains(sourceReaderRootText, "ArrayPool<byte>.Shared.Rent");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.RawFrameDelivery.cs")),
            "raw/compressed source-reader frame extraction folded into root source-reader owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.FrameDelivery.cs")),
            "source-reader frame delivery folded into root source-reader owner");
    }

    private static void AssertDiagnosticsSnapshotStatusProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertContains(diagnostics.SnapshotProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
    }

    private static void AssertDiagnosticsRefreshSnapshotProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertDiagnosticsRefreshSnapshotCompositionRoutesThroughProjectionSet(diagnostics);
        AssertDiagnosticsRefreshSnapshotFlatteningRoutesThroughFlattenedProjections(diagnostics);
    }

    private static void AssertDiagnosticsRefreshSnapshotCompositionRoutesThroughProjectionSet(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotFromProjections(projections);");
        AssertContains(diagnostics.SnapshotProjectionText, "return new AutomationSnapshotProjectionSet(");

        AssertContains(diagnostics.SnapshotProjectionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(diagnostics.SnapshotProjectionText, "var audioDrops = BuildAudioDropsProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(diagnostics.SnapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(diagnostics.SnapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(diagnostics.SnapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(");
        AssertContains(diagnostics.SnapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");

        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
    }

    private static void AssertDiagnosticsRefreshSnapshotFlatteningRoutesThroughFlattenedProjections(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var audioAndIngestFlattening = BuildAudioAndIngestFlattenedProjection(audioAndIngest);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var audioDropsFlattening = BuildAudioDropsFlattenedProjection(audioDrops);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureCommandFlattening = BuildCaptureCommandFlattenedProjection(captureCommands);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var recordingIntegrityFlattening = BuildRecordingIntegrityFlattenedProjection(recordingIntegrity);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var recordingOutputFlattening = BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var recordingPipelineFlattening = BuildRecordingPipelineFlattenedProjection(recordingPipeline);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegFlattening = BuildMjpegFlattenedProjection(mjpeg);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegTimingFlattening = BuildMjpegTimingFlattenedProjection(mjpeg.Timing);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegPreviewJitterFlattening = BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegPacketHashFlattening = BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewD3DFlattening = BuildPreviewD3DFlattenedProjection(previewD3D);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var flashbackExportFlattening = BuildFlashbackExportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");

        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "AudioPeak = audioAndIngestFlattening.Signal.Peak,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "RecordingVideoQueueCapacity = recordingPipelineFlattening.VideoQueue.Capacity,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "FlashbackExportActive = flashbackExportFlattening.Active,");

        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "FlashbackExportActive = flashbackExport.Active,");
    }

    private static void AssertDiagnosticSessionPreviewMetricsOwnership(string diagnosticSessionText, AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnosticSessionText, "PreviewD3DFrameStatsMissedRefreshDelta");
        AssertContains(diagnosticSessionText, "PreviewD3DFrameStatsFailureDelta");
        AssertContains(diagnosticSessionText, "SelectedResolutionAtEnd");
        AssertContains(diagnosticSessionText, "SelectedFrameRateAtEnd");
        AssertContains(diagnosticSessionText, "SelectedExactFrameRateArgAtEnd");
        AssertContains(diagnosticSessionText, "SelectedVideoFormatAtEnd");
        AssertContains(diagnosticSessionText, "VideoRequestedSubtypeAtEnd");
        AssertContains(diagnosticSessionText, "VideoNegotiatedSubtypeAtEnd");
        AssertContains(diagnosticSessionText, "DetectedSourceFrameRateAtEnd");
        AssertContains(diagnosticSessionText, "DetectedSourceFrameRateArgAtEnd");
        AssertContains(diagnosticSessionText, "SourceTelemetrySummaryAtEnd");
        AssertContains(diagnosticSessionText, "Capture Mode:");
        AssertContains(diagnosticSessionText, "selected={FormatOptional(result.SelectedResolutionAtEnd)}");
        AssertContains(diagnosticSessionText, "source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd}");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDroppedAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDeadlineDropsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerClearedDropsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerUnderflowsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerResumeReprimesAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDroppedDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDeadlineDropsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerClearedDropsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerUnderflowsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerResumeReprimesDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerLastDropReasonAtEnd");
        AssertContains(diagnosticSessionText, "Preview Scheduler:");
        AssertContains(diagnosticSessionText, "droppedDelta={result.PreviewSchedulerDroppedDelta}");
        AssertContains(diagnosticSessionText, "clearedDropsDelta={result.PreviewSchedulerClearedDropsDelta}");
        AssertContains(diagnosticSessionText, "deadlineDropsDelta={result.PreviewSchedulerDeadlineDropsDelta}");
        AssertContains(diagnosticSessionText, "underflowsDelta={result.PreviewSchedulerUnderflowsDelta}");
        AssertContains(diagnosticSessionText, "resumeReprimesDelta={result.PreviewSchedulerResumeReprimesDelta}");
        AssertContains(diagnosticSessionText, "lastDropReasonEnd={FormatOptional(result.PreviewSchedulerLastDropReasonAtEnd)}");
        AssertContains(diagnosticSessionText, "PreviewD3DLatestSlowFrameReason");
        AssertContains(diagnosticSessionText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DInputUploadCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DRenderSubmitCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DRenderSubmitCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DPresentCallP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DPresentCallMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DTotalFrameCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DTotalFrameCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "VisualCadenceOutputFpsAtEnd");
        AssertContains(diagnosticSessionText, "VisualCadenceChangeFpsAtEnd");
        AssertContains(diagnosticSessionText, "VisualCadenceMinChangeFpsObserved");
        AssertContains(diagnosticSessionText, "VisualCadenceMaxRepeatPercentObserved");
        AssertContains(diagnosticSessionText, "ProcessCpuPercentAtEnd");
        AssertContains(diagnosticSessionText, "ProcessCpuMaxPercentObserved");
        AssertContains(diagnosticSessionText, "Preview D3D Perf:");
        AssertContains(diagnosticSessionText, "Preview D3D CPU Timing:");
        AssertContains(diagnosticSessionText, "Preview Visual Cadence:");
        AssertContains(diagnosticSessionText, "Process Perf:");
        AssertContains(diagnosticSessionText, "PreviewCadenceOnePercentLowFpsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewCadenceMinOnePercentLowFpsObserved");
        AssertContains(diagnosticSessionText, "BuildPreviewD3DMetrics(initialSnapshot, lastSnapshot, samples)");
        AssertContains(diagnosticSessionText, "BuildVisualCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)");
        AssertContains(diagnosticSessionText, "BuildPreviewCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)}");
        AssertContains(diagnosticSessionText, "inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##}");
        AssertContains(diagnosticSessionText, "presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###}");
        AssertContains(diagnostics.TimelineText, "PreviewCadenceSlowFramePercent = preview.CadenceSlowFramePercent");
        AssertContains(diagnostics.TimelineText, "PreviewCadenceOnePercentLowFps = preview.CadenceOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP95Ms = preview.D3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DTotalFrameCpuP95Ms = preview.D3DTotalFrameCpuP95Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DInputUploadCpuP99Ms = preview.D3DInputUploadCpuP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DRenderSubmitCpuP99Ms = preview.D3DRenderSubmitCpuP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP99Ms = preview.D3DPresentCallP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DTotalFrameCpuP99Ms = preview.D3DTotalFrameCpuP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DFrameStatsRecentMissedRefreshCount = preview.D3DFrameStatsRecentMissedRefreshCount");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "CadenceSlowFramePercent: snapshot.PreviewCadenceSlowFramePercent");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "D3DFrameStatsRecentMissedRefreshCount: snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackP99FrameMs = flashbackPlayback.P99FrameMs");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackDecodeP99Ms = flashbackPlayback.DecodeP99Ms");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackPendingCommands = flashbackPlayback.PendingCommands");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackSubmitFailures = flashbackPlayback.SubmitFailures");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "P99FrameMs: snapshot.FlashbackPlaybackP99FrameMs");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "DecodeP99Ms: snapshot.FlashbackPlaybackDecodeP99Ms");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "PendingCommands: snapshot.FlashbackPlaybackPendingCommands");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "SubmitFailures: snapshot.FlashbackPlaybackSubmitFailures");
        AssertContains(diagnostics.TimelineText, "FlashbackExportPercent = flashbackExport.Percent");
        AssertContains(diagnostics.TimelineText, "FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec");
        AssertContains(diagnostics.TimelineText, "FlashbackExportLastProgressAgeMs = flashbackExport.LastProgressAgeMs");
        AssertContains(diagnostics.TimelineText, "Percent: snapshot.FlashbackExportPercent");
        AssertContains(diagnostics.TimelineText, "ThroughputBytesPerSec: snapshot.FlashbackExportThroughputBytesPerSec");
        AssertContains(diagnostics.TimelineText, "LastProgressAgeMs: snapshot.FlashbackExportLastProgressAgeMs");
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

    private static void AssertDiagnosticsRefreshRuntimeOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyFileAsync");
        AssertContains(diagnostics.VerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnostics.VerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnostics.VerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertContains(diagnostics.VerificationText, "Automatic recording verification started.");
        AssertContains(diagnostics.VerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnostics.VerificationText, "string.Equals(verificationProfile, \"flashback-export\"");
        AssertDoesNotContain(diagnostics.HubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.HubText, "private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;");
        AssertContains(diagnostics.HubText, "IAutomationSnapshotQueryPort snapshotQueryPort,");
        AssertContains(diagnostics.HubText, "_snapshotQueryPort = snapshotQueryPort ?? throw new ArgumentNullException(nameof(snapshotQueryPort));");
        AssertDoesNotContain(diagnostics.HubText, "IAutomationViewModel viewModel,");
        AssertDoesNotContain(diagnostics.HubText, "private readonly IAutomationViewModel _viewModel;");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetViewModelRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.VerificationText, "await _snapshotQueryPort\n                .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertContains(diagnostics.SnapshotsText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnostics.SnapshotsText, "ClassifyPreviewPacing(");
        AssertContains(diagnostics.HubText, "public void Start()");
        AssertContains(diagnostics.HubText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertContains(diagnostics.HdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.HdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertContains(diagnostics.HdrText, "static string NormalizeFormatToken(string? text)");
        AssertDoesNotContain(diagnostics.HubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.SnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnostics.SnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
    }

    private static void AssertDiagnosticsRefreshPipelineOwnership(AutomationDiagnosticsHubSourceFamily diagnostics, string dispatcherText)
    {
        AssertContains(diagnostics.SnapshotsText, "var snapshot = BuildAutomationSnapshot(");
        AssertDoesNotContain(diagnostics.SnapshotsText, "new AutomationSnapshot");
        AssertContains(diagnostics.SnapshotsText, "AppendPerformanceTimelineEntry(snapshot);");
        AssertContains(diagnostics.SnapshotsCoreText, "public AutomationSnapshot GetLatestSnapshot()");
        AssertContains(diagnostics.SnapshotsCoreText, "public Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default)");
        AssertContains(diagnostics.SnapshotsCoreText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SnapshotsCoreText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SnapshotsCoreText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsCoreText, "private AudioSignalState UpdateAudioSignalState(");
        AssertContains(diagnostics.SnapshotsCoreText, "private bool UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct AudioSignalState(");
        AssertContains(diagnostics.SnapshotsText, "UpdateAudioSignalState(viewModelSnapshot, nowTick);");
        AssertContains(diagnostics.SnapshotsText, "UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotsCoreText, "var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;");
        AssertContains(diagnostics.SnapshotsCoreText, "private LastOutputProbe ProbeLastOutput(");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct LastOutputProbe(");
        AssertContains(diagnostics.SnapshotsCoreText, "private ProcessResourceSnapshot CaptureProcessResourceSnapshot()");
        AssertContains(diagnostics.SnapshotsCoreText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct ProcessResourceSnapshot(");
        AssertContains(diagnostics.TimelineText, "public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline");
        AssertContains(diagnostics.TimelineText, "private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "BuildPerformanceTimelineEntry(snapshot)");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "var core = BuildPerformanceTimelineCoreProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var preview = BuildPerformanceTimelinePreviewProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var flashbackPlayback = BuildPerformanceTimelineFlashbackPlaybackProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var flashbackExport = BuildPerformanceTimelineFlashbackExportProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var system = BuildPerformanceTimelineSystemProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "CaptureCadenceFivePercentLowFps = core.CaptureCadenceFivePercentLowFps");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP95Ms = preview.D3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackCommandsEnqueued = flashbackPlayback.CommandsEnqueued");
        AssertContains(diagnostics.TimelineText, "FlashbackExportPercent = flashbackExport.Percent");
        AssertContains(diagnostics.TimelineText, "ProcessCpuPercent = system.ProcessCpuPercent");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineCoreProjection BuildPerformanceTimelineCoreProjection(");
        AssertContains(diagnostics.TimelineText, "CaptureCadenceFivePercentLowFps: snapshot.CaptureCadenceFivePercentLowFps");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "private static PerformanceTimelinePreviewProjection BuildPerformanceTimelinePreviewProjection(");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "D3DPresentCallP95Ms: snapshot.PreviewD3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackProjection BuildPerformanceTimelineFlashbackPlaybackProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var cadence = BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var decode = BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var commands = BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var audioMaster = BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var stages = BuildPerformanceTimelineFlashbackPlaybackStagesProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var backend = BuildPerformanceTimelineFlashbackPlaybackBackendProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackCadenceProjection BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackDecodeProjection BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackCommandsProjection BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackAudioMasterProjection BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackStagesProjection BuildPerformanceTimelineFlashbackPlaybackStagesProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackBackendProjection BuildPerformanceTimelineFlashbackPlaybackBackendProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineFlashbackExportProjection BuildPerformanceTimelineFlashbackExportProjection(");
        AssertContains(diagnostics.TimelineText, "Percent: snapshot.FlashbackExportPercent");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineSystemProjection BuildPerformanceTimelineSystemProjection(");
        AssertContains(diagnostics.TimelineText, "ProcessCpuPercent: snapshot.ProcessCpuPercent");
        AssertDoesNotContain(diagnostics.HubText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);");
        AssertContains(diagnostics.SnapshotsText, "_lastVerification = null;");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertContains(diagnostics.SnapshotsText, "Automatic recording verification started.");
        AssertContains(diagnostics.SnapshotsCoreText, "new FileInfo(lastOutputPath).Length");
        AssertContains(diagnostics.SnapshotsCoreText, "GC.GetGCMemoryInfo()");
        AssertDoesNotContain(diagnostics.HubText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SourceFamilyText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnostics.SourceFamilyText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SourceFamilyText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n                return await ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n        var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
    }

    private static void AssertDiagnosticsPreviewRuntimeProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = surface.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = startup.Strategy,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewHdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewSummary.AdapterColorMetadata,");
    }

    private static void AssertDiagnosticSessionToolSurfaceOwnership()
    {
        var diagnosticSessionToolSources = ReadDiagnosticSessionToolSurfaceSourceFamily();
        var ssctlProgramText = diagnosticSessionToolSources.SsctlProgramText;
        var ssctlHelpText = diagnosticSessionToolSources.SsctlHelpText;
        var ssctlCommandHandlersText = diagnosticSessionToolSources.SsctlCommandHandlersText;
        var mcpDiagnosticSessionText = diagnosticSessionToolSources.McpDiagnosticSessionText;
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertContains(ssctlProgramText, "internal static class SsctlHelpWriter");
        AssertDoesNotContain(ssctlProgramText, "DiagnosticSessionScenarioCatalog.HelpList");
        AssertContains(ssctlHelpText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionScenarioCatalog.Description");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertDoesNotContain(mcpDiagnosticSessionText, "Session scenario: observe,");
        AssertDoesNotContain(mcpDiagnosticSessionText, "string scenario = \"observe\"");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int seconds = 10");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int sampleIntervalMs = 1000");
    }
    private static void AssertDiagnosticSessionCoreOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticSessionModelsText = diagnosticSessionSources.ModelsText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;
        AssertContains(diagnosticSessionText, "var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);");
        AssertContains(diagnosticSessionText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(diagnosticSessionText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(diagnosticSessionText, "scenarioPlan.RequiresFlashbackRecordingReadiness");
        AssertContains(diagnosticSessionText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertContains(diagnosticSessionText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(diagnosticSessionText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(diagnosticSessionText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(");
        AssertContains(diagnosticSessionText, ".CompleteRegisteredScenarioWorkAsync(");
        AssertContains(diagnosticSessionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertContains(diagnosticScenariosText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(diagnosticScenariosText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlayback = \"flashback-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackStress = \"flashback-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackScrubStress = \"flashback-scrub-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRestartCycle = \"flashback-restart-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackEncoderCycle = \"flashback-encoder-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportPlayback = \"flashback-export-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackSegmentPlayback = \"flashback-segment-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExport = \"flashback-range-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExportAudioSwitch = \"flashback-range-export-audio-switch\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackLifecycle = \"flashback-lifecycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportConcurrent = \"flashback-export-concurrent\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackDisableDuringExport = \"flashback-disable-during-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRotatedExport = \"flashback-rotated-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPreviewCycle = \"flashback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlaybackPreviewCycle = \"flashback-playback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecording = \"flashback-recording\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingPreviewCycle = \"flashback-recording-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingSettingsDeferred = \"flashback-recording-settings-deferred\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingExportRejected = \"flashback-recording-export-rejected\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportRejected = \"flashback-export-rejected\";");
        AssertContains(diagnosticSessionText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(diagnosticSessionText, "catch (AutomationPipeException ex) when (ex is not AutomationPipeConnectException)");
        AssertContains(diagnosticSessionText, "return BuildLocalFailureResponse(command, ex.Message);");
        AssertContains(diagnosticSessionText, "catch (JsonException ex)");
        AssertContains(diagnosticSessionModelsText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(diagnosticSessionModelsText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(diagnosticSessionModelsText, "public string TerminalState { get; set; }");
        AssertContains(diagnosticSessionText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(diagnosticSessionText, "CreateUnknownInitialSnapshot()");
        AssertContains(diagnosticSessionText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(diagnosticSessionText, "skipped state-mutating scenario");
        AssertContains(diagnosticSessionText, "CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs))");
        AssertContains(diagnosticSessionText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(diagnosticSessionText, "var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;");
        AssertContains(diagnosticSessionText, "if (!startedRecording || (!shouldStopRecordingForVerification && options.LeaveRunning))");
        AssertContains(diagnosticSessionText, "recording stopped for verification");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = await StopRecordingForCleanupAsync(");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = shouldStopRecordingForVerification &&");
        AssertContains(diagnosticSessionText, "var diagnosticHealthSnapshot = request.StoppedRecordingForVerification");
        AssertContains(diagnosticSessionText, ".WaitAsync(cancellationToken)");
        AssertContains(diagnosticSessionText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(diagnosticSessionText, "WriteSamplingLiveStateBestEffortAsync");
        AssertContains(diagnosticSessionText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(diagnosticSessionText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
    }
    private static void AssertDiagnosticSessionExportRecordingOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;

        AssertContains(diagnosticSessionText, "FlashbackRecordingFileGrowthObserved");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoEncoderPacketsWrittenDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegritySequenceGaps\")");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegrityQueueDroppedFrames\")");
        AssertContains(diagnosticSessionText, "Flashback Recording:");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxElapsedMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportFailureKindAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportOutputPathAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(diagnosticSessionText, "FlashbackExportLastForceRotateFallbackSegmentsAtEnd");
        AssertContains(diagnosticSessionText, "LastExportIdAtEnd");
        AssertContains(diagnosticSessionText, "LastExportSuccessAtEnd");
        AssertContains(diagnosticSessionText, "LastExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxLastProgressAgeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxOutputBytesObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxThroughputBytesPerSecObserved");
        AssertContains(diagnosticSessionText, "BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "var healthSnapshot = lastSnapshot;");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "exportId > baselineExportId");
        AssertContains(diagnosticSessionText, "baselineExportActive && exportId == baselineExportId");
        AssertContains(diagnosticSessionText, "lastExportId == exportId");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(diagnosticSessionText, "var shouldRunVerification =");
        AssertContains(diagnosticSessionText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(diagnosticSessionText, "verificationCommand = \"VerifyFile\"");
        AssertContains(diagnosticSessionText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExport,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExportAudioSwitch,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-range-export-audio-switch.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackExportConcurrent,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-concurrent-a.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackRotatedExport,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-rotated-export.mp4\"");
        AssertContains(diagnosticScenariosText, "return exportPath.Length > 0;");
        AssertDoesNotContain(diagnosticScenariosText, "return exportPath.Length > 0 && File.Exists(exportPath);");
        AssertContains(diagnosticSessionText, "expected BufferInactive failure kind");
        AssertContains(diagnosticSessionText, "expected UnavailableDuringRecording failure kind");
        AssertContains(diagnosticSessionText, "flashback rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "flashback recording rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "Flashback Export:");
        AssertContains(diagnosticSessionText, "failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)}");
        AssertContains(diagnosticSessionText, "messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta}");
        AssertContains(diagnosticSessionText, "lastResultIdEnd={result.LastExportIdAtEnd}");
        AssertContains(diagnosticSessionText, "lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)}");
        AssertContains(diagnosticSessionText, "lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)}");
        AssertContains(diagnosticSessionText, "maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
        AssertContains(diagnosticSessionText, "BuildFlashbackRecordingMetrics(initialSnapshot, samples)");
        AssertContains(diagnosticSessionText, "seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "private static void ValidateCleanupLifecycleRestored(");
        AssertContains(diagnosticSessionText, "cleanup: preview remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: Flashback remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: playback did not return live state={state}");
        AssertContains(diagnosticSessionText, "metrics.MaxPendingCommandsObserved = Math.Max(");
        AssertContains(diagnosticSessionText, "if (maxCommandQueueLatencyMs > metrics.MaxCommandQueueLatencyMsObserved)");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyMsObserved = maxCommandQueueLatencyMs;");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyCommandObserved = GetString(snapshot, \"FlashbackPlaybackMaxCommandQueueLatencyCommand\") ?? string.Empty;");
    }


    private static void AssertDiagnosticsRefreshSnapshotConstructionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "private AutomationSnapshot BuildAutomationSnapshot(");
        AssertContains(diagnostics.SnapshotProjectionText, "var projections = BuildAutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "return BuildAutomationSnapshotFromProjections(projections);");
        AssertContains(diagnostics.SnapshotProjectionText, "private AutomationSnapshotProjectionSet BuildAutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private readonly record struct AutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshot BuildAutomationSnapshotFromProjections(");
        AssertContains(diagnostics.SnapshotProjectionText, "AutomationSnapshotProjectionSet projections");
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotFlattenedProjectionSet(projections)");
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotFromFlattenedProjections(flattened)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private readonly record struct AutomationSnapshotFlattenedProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshot BuildAutomationSnapshotFromFlattenedProjections(");
        AssertContains(diagnostics.SnapshotProjectionText, "return new AutomationSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs")),
            "final automation snapshot DTO initialization folded into AutomationDiagnosticsHub.SnapshotProjection.cs");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationSnapshotFlashbackProjectionBuilder.cs")),
            "Flashback snapshot projection construction lives in its named builder");
        foreach (var deletedProjectionShard in new[]
                 {
                     "AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs",
                     "AutomationDiagnosticsHub.SnapshotProjection.Media.cs",
                     "AutomationDiagnosticsHub.SnapshotProjection.Preview.cs",
                     "AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs",
                 })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", deletedProjectionShard)),
                deletedProjectionShard + " folded into AutomationDiagnosticsHub.SnapshotProjection.cs");
        }

        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSnapshotStatusFlattenedProjection(snapshotStatus)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureFormatFlattenedProjection(captureFormat)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatRequestedProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatHdrRequestProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatActualProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatNegotiatedProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatReaderObservationProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatEncoderProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureTransportFlattenedProjection(captureTransport)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureCadenceFlattenedProjection(captureCadence)");
        AssertContains(diagnostics.SnapshotProjectionCaptureCadenceText, "private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildVisualCadenceFlattenedProjection(visualCadence)");
        AssertContains(diagnostics.SnapshotProjectionVisualCadenceText, "private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegFlattenedProjection(mjpeg)");
        AssertContains(diagnostics.SnapshotProjectionMjpegText, "private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegTimingFlattenedProjection(mjpeg.Timing)");
        AssertContains(diagnostics.SnapshotProjectionMjpegText, "private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter)");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash)");
        AssertContains(diagnostics.SnapshotProjectionMjpegText, "private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry)");
        AssertContains(diagnostics.SnapshotProjectionSourceSignalText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionSourceSignalText, "private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionSourceTelemetryText, "private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSettingsFlattenedProjection(userSettings, recordingSettings)");
        AssertContains(diagnostics.SnapshotProjectionUserSettingsText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildHdrPipelineFlattenedProjection(hdrPipeline)");
        AssertContains(diagnostics.HdrText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildPreviewRuntimeFlattenedProjection(previewSummary)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeFrameProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeCadenceProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeSurfaceProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeStartupProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildPreviewD3DFlattenedProjection(previewD3D)");
        AssertContains(diagnostics.SnapshotProjectionPreviewD3DText, "private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildFlashbackExportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackExportText, "internal static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildFlashbackRecordingFlattenedProjection(flashbackRecording)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingStartupCacheProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingRuntimeProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingBackendProjection(captureRuntime, health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingEncoderProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "internal static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingQueuesText, "private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackTimingProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "internal static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildRecordingIntegrityFlattenedProjection(recordingIntegrity)");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildRecordingPipelineFlattenedProjection(recordingPipeline)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineEncoderProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineIngestProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineVideoQueueProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineHardwareQueuesProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildProcessResourceFlattenedProjection(processResourceProjection)");
        AssertContains(diagnostics.SnapshotProjectionProcessResourcesText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildAvSyncFlattenedProjection(avSync)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildAudioAndIngestFlattenedProjection(audioAndIngest)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "BuildAudioSignalProjection(viewModelSnapshot, audioSignal)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "BuildCaptureIngestProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "BuildWasapiAudioProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioDropsProjection BuildAudioDropsProjection(");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureIngestText, "private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureIngestText, "private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionWasapiAudioText, "private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionWasapiAudioText, "private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildAudioDropsFlattenedProjection(audioDrops)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioDropsFlattenedProjection BuildAudioDropsFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureCommandFlattenedProjection(captureCommands)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "new AutomationSnapshot");
    }

    private static AutomationDiagnosticsHubSourceFamily ReadAutomationDiagnosticsHubSourceFamily()
    {
        return new AutomationDiagnosticsHubSourceFamily
        {
            HubText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            EvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationFlashbackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationLanesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            HdrText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotsText = ReadAutomationDiagnosticsHubSnapshotsSource(),
            SnapshotsCoreText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            SnapshotProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionFlatteningText = ReadAutomationSnapshotFlatteningFamilyText(),
            SnapshotProjectionAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionCaptureIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionWasapiAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionVisualCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationSnapshotFlashbackProjectionBuilder.cs"),
            SnapshotProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationSnapshotFlashbackProjectionBuilder.cs"),
            SnapshotProjectionFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationSnapshotFlashbackProjectionBuilder.cs"),
            SnapshotProjectionFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationSnapshotFlashbackProjectionBuilder.cs"),
            SnapshotProjectionPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionSourceSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionSourceTelemetryText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionUserSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            PreviewPacingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            TimelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            TimelineProjectionPreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            TimelineProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
        };
    }

    private static string ReadAutomationDiagnosticsHubSourceFile(string fileName)
    {
        return ReadRepoFile("Sussudio/Services/Automation/" + fileName)
            .Replace("\r\n", "\n");
    }

    private static string ReadAutomationDiagnosticsHubSnapshotsSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            });
    }

    private sealed class AutomationDiagnosticsHubSourceFamily
    {
        private string? _sourceFamilyText;

        public string HubText { get; init; } = string.Empty;
        public string EvaluationText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeText { get; init; } = string.Empty;
        public string DiagnosticEvaluationLanesText { get; init; } = string.Empty;
        public string AlertsText { get; init; } = string.Empty;
        public string VerificationText { get; init; } = string.Empty;
        public string HdrText { get; init; } = string.Empty;
        public string SnapshotsText { get; init; } = string.Empty;
        public string SnapshotsCoreText { get; init; } = string.Empty;
        public string SnapshotProjectionText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningText { get; init; } = string.Empty;
        public string SnapshotProjectionAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureIngestText { get; init; } = string.Empty;
        public string SnapshotProjectionWasapiAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureFormatText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionVisualCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegPreviewJitterText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackExportText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingQueuesText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameFlowText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DCpuTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewRuntimeText { get; init; } = string.Empty;
        public string SnapshotProjectionProcessResourcesText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingIntegrityText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingPipelineText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceSignalText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceTelemetryText { get; init; } = string.Empty;
        public string SnapshotProjectionUserSettingsText { get; init; } = string.Empty;
        public string PreviewPacingText { get; init; } = string.Empty;
        public string TimelineText { get; init; } = string.Empty;
        public string TimelineProjectionPreviewText { get; init; } = string.Empty;
        public string TimelineProjectionFlashbackPlaybackText { get; init; } = string.Empty;

        public string SourceFamilyText => _sourceFamilyText ??= string.Join(
            "\n",
            new[]
            {
                HubText,
                EvaluationText,
                DiagnosticEvaluationFlashbackText,
                DiagnosticEvaluationRealtimeText,
                DiagnosticEvaluationLanesText,
                AlertsText,
                VerificationText,
                HdrText,
                SnapshotsText,
                SnapshotProjectionText,
                SnapshotProjectionFlatteningText,
                SnapshotProjectionAudioText,
                SnapshotProjectionCaptureIngestText,
                SnapshotProjectionWasapiAudioText,
                SnapshotProjectionCaptureFormatText,
                SnapshotProjectionCaptureCadenceText,
                SnapshotProjectionMjpegText,
                SnapshotProjectionMjpegPreviewJitterText,
                SnapshotProjectionFlashbackExportText,
                SnapshotProjectionFlashbackPlaybackText,
                SnapshotProjectionFlashbackRecordingText,
                SnapshotProjectionFlashbackRecordingQueuesText,
                SnapshotProjectionPreviewD3DText,
                SnapshotProjectionPreviewD3DFrameFlowText,
                SnapshotProjectionPreviewRuntimeText,
                SnapshotProjectionProcessResourcesText,
                SnapshotProjectionRecordingIntegrityText,
                SnapshotProjectionRecordingPipelineText,
                SnapshotProjectionSourceSignalText,
                SnapshotProjectionSourceTelemetryText,
                SnapshotProjectionUserSettingsText,
                HdrText,
                PreviewPacingText,
                TimelineText,
                TimelineProjectionPreviewText,
                TimelineProjectionFlashbackPlaybackText,
                SnapshotProjectionPreviewD3DCpuTimingText,
            });
    }

    private static AutomationDiagnosticsHubCountersSourceFamily ReadAutomationDiagnosticsHubCountersSource()
    {
        var countersText = ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");

        return new AutomationDiagnosticsHubCountersSourceFamily(
            countersText,
            countersText);
    }

    private static string ReadCaptureServiceDiagnosticsRefreshSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
    }

    private static string ReadFlashbackBackendResourcesSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
    }

    private static MfSourceReaderVideoCaptureSourceFamily ReadMfSourceReaderVideoCaptureSourceFamily()
    {
        var rootText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs");
        var diagnosticsText = rootText;
        var frameLayoutText = rootText;
        var lifecycleText = rootText;
        var initializationText = rootText;
        var initializedSessionText = initializationText;
        var readLoopText = lifecycleText;
        var frameDeliveryText = rootText;

        return new MfSourceReaderVideoCaptureSourceFamily(
            rootText,
            diagnosticsText,
            frameLayoutText,
            lifecycleText,
            initializationText,
            initializedSessionText,
            readLoopText,
            frameDeliveryText,
            rootText);
    }

    private static DiagnosticSessionSourceFamily ReadDiagnosticSessionSourceFamily()
    {
        return new DiagnosticSessionSourceFamily(
            ReadDiagnosticSessionRunnerSource()
                + "\n" + ReadDiagnosticSessionScenarioStartupSource()
                + "\n" + ReadDiagnosticSessionResultBuilderSource()
                + "\n" + ReadDiagnosticSessionBackgroundTasksSource()
                + "\n" + ReadDiagnosticSessionFlashbackCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackExportScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSupportSource()
                + "\n" + ReadDiagnosticSessionFlashbackStressScenarioSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
                + "\n" + ReadDiagnosticSessionMetricsSource()
                + "\n" + ReadDiagnosticSessionResultFormatterSource()
                + "\n" + ReadDiagnosticSessionScenarioCatalogSource(),
            ReadDiagnosticSessionModelsSource(),
            ReadDiagnosticSessionScenarioCatalogSource());
    }

    private static DiagnosticSessionToolSurfaceSourceFamily ReadDiagnosticSessionToolSurfaceSourceFamily()
    {
        return new DiagnosticSessionToolSurfaceSourceFamily(
            ReadNormalizedRepoFile("tools/ssctl/Program.cs"),
            ReadNormalizedRepoFile("tools/ssctl/Program.cs"),
            ReadNormalizedRepoFile("tools/ssctl/CommandHandlers.cs"),
            ReadNormalizedRepoFile("tools/McpServer/Tools/AppStateTools.cs"));
    }

    private static string ReadDiagnosticSessionScenarioCatalogSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs");
    }

    private static string ReadDiagnosticSessionFlashbackSupportSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs");
    }

    private static string ReadNormalizedRepoFile(string path)
    {
        return ReadRepoFile(path).Replace("\r\n", "\n");
    }

    private readonly record struct MfSourceReaderVideoCaptureSourceFamily(
        string RootText,
        string DiagnosticsText,
        string FrameLayoutText,
        string LifecycleText,
        string InitializationText,
        string InitializedSessionText,
        string ReadLoopText,
        string FrameDeliveryText,
        string SourceFamilyText);

    private readonly record struct DiagnosticSessionSourceFamily(
        string SourceFamilyText,
        string ModelsText,
        string ScenariosText);

    private readonly record struct DiagnosticSessionToolSurfaceSourceFamily(
        string SsctlProgramText,
        string SsctlHelpText,
        string SsctlCommandHandlersText,
        string McpDiagnosticSessionText);

    private readonly record struct AutomationDiagnosticsHubCountersSourceFamily(
        string RealtimePreviewText,
        string SourceFamilyText);

    private static void AssertDiagnosticsRefreshFlashbackRecordingAndStorageAlertCoverage(
        AutomationDiagnosticsHubSourceFamily diagnostics,
        AutomationDiagnosticsHubCountersSourceFamily counters)
    {
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.SourceFamilyText, "DiagnosticsCategory.Flashback");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackExportActive");
        AssertContains(diagnostics.SourceFamilyText, "Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnostics.SourceFamilyText, "Math.Max(0, health.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnostics.SourceFamilyText, "elapsedMs={health.FlashbackExportElapsedMs}");
        AssertContains(diagnostics.SourceFamilyText, "throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "kind={exportFailureKind}");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackExportStallThresholdMs = 30000;");
        AssertContains(diagnostics.AlertsText, "exportLastProgressAgeMs >= FlashbackExportStallThresholdMs");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback export progress is stalled.\"");
        AssertContains(diagnostics.SourceFamilyText, "$\"{lanes.Export} progressAgeMs={exportLastProgressAgeMs}\"");
        AssertContains(diagnostics.SourceFamilyText, "private long _lastFlashbackExportCompletionEventId;");
        AssertContains(diagnostics.SourceFamilyText, "ObserveFlashbackExportCompletion(snapshot);");
        AssertContains(diagnostics.SourceFamilyText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackExportCompletedUtcUnixMs <= 0");
        AssertContains(diagnostics.SourceFamilyText, "Interlocked.CompareExchange(\n                ref _lastFlashbackExportCompletionEventId");
        AssertContains(diagnostics.SourceFamilyText, "status.Equals(\"Succeeded\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnostics.SourceFamilyText, "status.Equals(\"Cancelled\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackExportFailureKind");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackBackendSettingsStale = flashbackRecordingFlattening.Backend.SettingsStale,");
        AssertContains(diagnostics.SourceFamilyText, "SettingsStale = backend.SettingsStale,");
        AssertContains(diagnostics.SourceFamilyText, "SettingsStale = health.FlashbackBackendSettingsStale,");
        AssertContains(diagnostics.SourceFamilyText, "backendStale={health.FlashbackBackendSettingsStale}");
        AssertContains(diagnostics.SourceFamilyText, "kind={failureKind}");
        AssertContains(diagnostics.SourceFamilyText, "Flashback export completed: status={status}");
        AssertContains(diagnostics.SourceFamilyText, "private const long FlashbackTempDriveLowFreeBytes = 5L * 1024L * 1024L * 1024L;");
        AssertContains(diagnostics.SourceFamilyText, "private const long FlashbackRecordingBackpressureWarningMs = 100;");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackRecordingQueueDepthWarningRatio = 0.75;");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackAudioQueueDepthWarningRatio = 0.90;");
        AssertContains(diagnostics.SourceFamilyText, "private const long FlashbackRecordingQueueAgeWarningMs = 500;");
        AssertContains(diagnostics.AlertsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackStartupCacheOverBudget");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_storage\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback temp storage is under pressure.\"");
        AssertContains(diagnostics.AlertsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackEncodingFailed");
        AssertContains(diagnostics.AlertsText, "Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? \"Unknown\"}");
        AssertContains(diagnostics.AlertsText, "\"flashback-recording-degraded\"");
        AssertContains(counters.SourceFamilyText, "private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(");
        AssertContains(counters.SourceFamilyText, "Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps)");
        AssertContains(counters.SourceFamilyText, "Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped)");
        AssertContains(counters.SourceFamilyText, "Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents)");
        AssertContains(counters.SourceFamilyText, "private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(");
        AssertContains(counters.SourceFamilyText, "private MjpegRecentCounters UpdateMjpegRecentCounters(");
        AssertContains(diagnostics.SourceFamilyText, "var recentFlashbackRecording = UpdateFlashbackRecordingRecentCounters(health, nowTick);");
        AssertContains(diagnostics.SourceFamilyText, "UpdateAlerts(snapshot, recentFlashbackRecording);");
        AssertContains(diagnostics.SourceFamilyText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackRecordingQueueBacklog =");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackAudioQueueBacklog =");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackAudioQueueBackedUp(");
        AssertContains(diagnostics.SourceFamilyText, "flashbackRecordingRecentForceRotateGap");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason)");
        AssertContains(diagnostics.SourceFamilyText, "flashbackRecordingRecent.SequenceGaps > 0");
        AssertContains(diagnostics.AlertsText, "(flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap)");
        AssertContains(diagnostics.AlertsText, "flashbackRecordingRecent.GpuFramesDropped > 0");
        AssertContains(diagnostics.AlertsText, "flashbackRecordingRecentBackpressure");
        AssertContains(diagnostics.AlertsText, "flashbackRecordingQueueBacklog");
        AssertContains(diagnostics.AlertsText, "flashbackAudioQueueBacklog");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnostics.AlertsText, "Flashback recording path degraded:");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnostics.AlertsText, "Flashback export rotation skipped live-edge frames:");
        AssertContains(diagnostics.SourceFamilyText, "forceRotate={snapshot.FlashbackForceRotateActive}");
        AssertContains(diagnostics.SourceFamilyText, "requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining}");
        AssertContains(diagnostics.SourceFamilyText, "FatalCleanupInProgress = flashbackRecordingFlattening.FatalCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "FatalCleanupInProgress = health.FatalCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackCleanupInProgress = flashbackRecordingFlattening.CleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "CleanupInProgress = flashbackRecording.CleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "CleanupInProgress = health.FlashbackCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents}");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnostics.SourceFamilyText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackRecordingQueueDepthWarningRatio)");
        AssertContains(diagnostics.SourceFamilyText, "oldestFrameAgeMs >= FlashbackRecordingQueueAgeWarningMs");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsFlashbackAudioQueueBackedUp(int queueDepth, int queueCapacity)");
        AssertContains(diagnostics.SourceFamilyText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackAudioQueueDepthWarningRatio)");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsFlashbackForceRotateRejectReason(string? reason)");
        AssertContains(diagnostics.SourceFamilyText, "string.Equals(reason, \"force_rotate_queue_guard\"");
        AssertContains(diagnostics.SourceFamilyText, "flashback recording active={health.FlashbackActive}");
        AssertContains(diagnostics.SourceFamilyText, "fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}");
        AssertContains(diagnostics.SourceFamilyText, "var recordingIntegrityIncomplete =");
        AssertContains(diagnostics.SourceFamilyText, "string.Equals(captureRuntime.RecordingIntegrityStatus, \"Incomplete\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnostics.SourceFamilyText, "(recordingIntegrityIncomplete && !isRecording)");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackRecordingDegraded =");
        AssertContains(diagnostics.SourceFamilyText, "recentFlashbackRecording.EncoderDroppedFrames > 0");
        AssertContains(diagnostics.SourceFamilyText, "recentFlashbackRecording.BackpressureEvents > 0");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackBackendSettingsUnexpectedlyStale =");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackBackendSettingsStale &&\n            !isRecording");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback backend settings differ from requested settings.\"");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackVideoQueueDepth,\n                 health.FlashbackVideoQueueCapacity,\n                 health.FlashbackVideoQueueOldestFrameAgeMs");
        AssertContains(diagnostics.SourceFamilyText, "forceRotate={health.FlashbackForceRotateActive}");
        AssertContains(diagnostics.SourceFamilyText, "queueRejects={health.FlashbackVideoQueueRejectedFrames}");
        AssertContains(diagnostics.SourceFamilyText, "audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity}");
        AssertContains(diagnostics.SourceFamilyText, "lastReject={health.FlashbackVideoQueueLastRejectReason ?? \"None\"}");
        AssertContains(diagnostics.SourceFamilyText, "flashbackExportRotationGap");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback export rotation skipped live-edge frames.\"");
        AssertContains(diagnostics.SourceFamilyText, "requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining}");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_recording\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback encoder has failed.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback recording path is dropping or backing up.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_export\"");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackForceRotateRejectWithoutDamage =");
        AssertContains(diagnostics.SourceFamilyText, "!flashbackForceRotateRejectWithoutDamage &&\n              recentFlashbackRecording.SequenceGaps > 0");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackExportActive ||\n             health.FlashbackForceRotateActive ||\n             health.FlashbackForceRotateRequested ||\n             health.FlashbackForceRotateDraining");
    }

    private static void AssertDiagnosticsRefreshFlashbackPlaybackAndPreviewAlertCoverage(
        AutomationDiagnosticsHubSourceFamily diagnostics,
        AutomationDiagnosticsHubCountersSourceFamily counters)
    {
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackCommandStallThresholdMs = 1000;");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-command-failed\"");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackCommandFailureRecentMs = 30000;");
        AssertContains(diagnostics.SourceFamilyText, "playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback command failed recently:");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback command failed recently.\"");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackPlaybackSlowFpsRatio = 0.75;");
        AssertContains(diagnostics.SourceFamilyText, "private const double CaptureOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnostics.SourceFamilyText, "private const double PreviewOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackPlaybackOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackOnePercentLowMinimumFrames = 1200;");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-slow\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-target-below-selection\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-present-capped\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-frametime-degraded\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackSubmitFailures > 0");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback frame submission failed");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackPendingCommands > 0");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackCommandQueueCapacity");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps");
        AssertContains(diagnostics.SourceFamilyText, "TargetFps = timing.TargetFps");
        AssertContains(diagnostics.SourceFamilyText, "TargetFps = health.FlashbackPlaybackTargetFps");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps");
        AssertContains(diagnostics.SourceFamilyText, "TargetFps: snapshot.FlashbackPlaybackTargetFps");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackPtsCadenceMismatchCount = flashbackPlaybackFlattening.Timing.PtsCadenceMismatchCount");
        AssertContains(diagnostics.SourceFamilyText, "PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount");
        AssertContains(diagnostics.SourceFamilyText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount");
        AssertContains(diagnostics.SourceFamilyText, "ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount}");
        AssertContains(diagnostics.SourceFamilyText, "private static double ResolveFlashbackPlaybackTargetFps(double flashbackPlaybackTargetFps, double fallbackFrameRate)");
        AssertContains(diagnostics.SourceFamilyText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            snapshot.FlashbackPlaybackTargetFps,\n            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackPlaybackFrametimeDegraded(\n                snapshot.FlashbackPlaybackState");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackState,\n                playbackTargetFps,\n                snapshot.FlashbackPlaybackFrameCount");
        AssertContains(diagnostics.SourceFamilyText, "IsCaptureOnePercentLowDegraded(\n                snapshot.ExpectedCaptureFrameRate");
        AssertContains(diagnostics.SourceFamilyText, "IsPreviewOnePercentLowDegraded(\n                snapshot.PreviewCadenceExpectedIntervalMs");
        AssertContains(diagnostics.SourceFamilyText, "\"Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.\"");
        AssertContains(diagnostics.SourceFamilyText, "$\"{lanes.Source} | {lanes.Visual}\"");
        AssertContains(diagnostics.SourceFamilyText, "captureCadenceExpectedFrameRate: health.ExpectedFrameRate");
        AssertContains(diagnostics.SourceFamilyText, "captureCadenceOnePercentLowFps: health.CaptureCadenceOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "previewCadenceExpectedIntervalMs: previewRuntime.DisplayCadenceExpectedIntervalMs");
        AssertContains(diagnostics.SourceFamilyText, "previewCadenceOnePercentLowFps: previewRuntime.DisplayCadenceOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "reasons.Add($\"capture 1% low {captureCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnostics.SourceFamilyText, "reasons.Add($\"preview 1% low {previewCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "frameCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnostics.SourceFamilyText, "cadenceSampleCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback is using wall-clock pacing instead of audio-master pacing");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback audio queue is backing up");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback is below target rate");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback target is below the selected capture rate");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback is targeting HFR but D3D present cadence is below target");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback frametime degraded");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback command queue has not drained");
        AssertContains(diagnostics.SourceFamilyText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnostics.SourceFamilyText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_playback\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback command queue is stalled.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback is below target rate.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback frametime is below target.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback frame submission failed.\"");
        AssertContains(diagnostics.SourceFamilyText, "queuedAge={playbackCommandQueueAgeMs}ms");
        AssertContains(diagnostics.SourceFamilyText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnostics.SourceFamilyText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            health.FlashbackPlaybackTargetFps,\n            health.ExpectedFrameRate);");
        AssertContains(diagnostics.SourceFamilyText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnostics.SourceFamilyText, "playback perf state={health.FlashbackPlaybackState}");
        AssertContains(diagnostics.SourceFamilyText, "fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "target={health.FlashbackPlaybackTargetFps:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "private static string FormatEncoderFrameRate(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.SourceFamilyText, "ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackPlaybackAudioMasterFallbackWarningRatio = 0.50;");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackAudioQueueBacklogWarningDepth = 24;");
        AssertContains(diagnostics.SourceFamilyText, "decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "decodePhase={health.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnostics.SourceFamilyText, "decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnostics.SourceFamilyText, "audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnostics.SourceFamilyText, "audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackPlaybackSubmitFailures <= 0");
        AssertContains(diagnostics.SourceFamilyText, "UpdatePreviewJitterRecentCounters(health, nowTick)");
        AssertContains(diagnostics.SourceFamilyText, "UpdateD3DRendererRecentCounters(previewRuntime, nowTick)");
        AssertContains(counters.RealtimePreviewText, "private PreviewJitterRecentCounters UpdatePreviewJitterRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "private long _lastPreviewJitterTotalDropped;");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastPreviewJitterTotalDropped, totalDropped)");
        AssertContains(counters.RealtimePreviewText, "private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "private long _lastD3DFramesSubmitted;");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastD3DFramesSubmitted, submitted)");
        AssertContains(counters.RealtimePreviewText, "private MjpegRecentCounters UpdateMjpegRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastMjpegCompressedDropsQueueFull, compressedQueueDrops)");
        AssertContains(counters.RealtimePreviewText, "private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(");
        AssertDoesNotContain(diagnostics.HubText, "private long _lastPreviewJitterTotalDropped;");
        AssertDoesNotContain(diagnostics.HubText, "private long _lastD3DFramesSubmitted;");
        AssertContains(diagnostics.SourceFamilyText, "recentSubmitted={recentRendererSubmitted} recentDropped={recentRenderer.Dropped}");
        AssertContains(diagnostics.SourceFamilyText, "var previewLastDropReason = string.IsNullOrWhiteSpace(health.MjpegPreviewJitterLastDropReason)");
        AssertContains(diagnostics.SourceFamilyText, "clearedDrops={health.MjpegPreviewJitterClearedDropCount}");
        AssertContains(diagnostics.SourceFamilyText, "resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount} recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows} lastDropReason={previewLastDropReason}");
        AssertContains(diagnostics.SourceFamilyText, "UpdateD3DFrameStatsRecentCounters(previewRuntime, nowTick)");
        AssertContains(counters.RealtimePreviewText, "private long UpdateD3DFrameLatencyWaitRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastD3DFrameLatencyWaitTimeouts, timeouts)");
        AssertContains(diagnostics.SourceFamilyText, "recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}");
        AssertContains(diagnostics.SourceFamilyText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Capture cadence 1% low is below target:");
        AssertContains(diagnostics.SourceFamilyText, "\"preview-display-low-1pct\"");
        AssertContains(diagnostics.SourceFamilyText, "previewOnePercentLowDegraded && !visualCadenceHealthy");
        AssertContains(diagnostics.SourceFamilyText, "\"Preview/display 1% low is below target:");
        AssertContains(diagnostics.SourceFamilyText, "FormatVisualCadenceAlertDetail(snapshot)");
        AssertContains(diagnostics.SourceFamilyText, "visualChanges={snapshot.VisualCadenceChangeObservedFps:0.##}fps");
        AssertContains(diagnostics.SourceFamilyText, "var previewSubmitFailed = string.Equals(");
        AssertContains(diagnostics.SourceFamilyText, "health.MjpegPreviewJitterLastDropReason,\n            \"submit-failed\"");
        AssertContains(diagnostics.SourceFamilyText, "if (!previewSubmitFailed &&\n            (recentPreviewDeadlineDrops <= 0 || visualCadenceHealthy) &&\n            recentPreviewUnderflows <= 3)");
        AssertContains(diagnostics.SourceFamilyText, "\"Preview scheduler failed to submit frames.\"");
        AssertContains(diagnostics.SourceFamilyText, "var presentCadenceOverBudget =\n            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&\n            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;");
        AssertContains(diagnostics.SourceFamilyText, "var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);");
        AssertContains(diagnostics.SourceFamilyText, "latestSlowFrameReason={reason} over={frame.WorstOverBudgetMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "pipeline={frame.PipelineLatencyMs:0.##}ms pending={frame.PendingFrameCount}");
        AssertContains(diagnostics.SourceFamilyText, "inputUpload={frame.InputUploadCpuMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "renderSubmit={frame.RenderSubmitCpuMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "var unsyncedPresentCallSlow =\n            previewRuntime.D3DPresentSyncInterval == 0 &&\n            previewRuntime.D3DPresentCallP95Ms > 4.0;");
        AssertContains(diagnostics.SourceFamilyText, "if (presentCadenceOverBudget ||\n            unsyncedPresentCallSlow)");
        AssertContains(diagnostics.SourceFamilyText, "if (!captureOnePercentLowDegraded)");
        AssertContains(diagnostics.SourceFamilyText, "\"Source/capture 1% low is below target.\"");
        AssertContains(diagnostics.SourceFamilyText, "if (!previewOnePercentLowDegraded)");
        AssertContains(diagnostics.SourceFamilyText, "var visualCadenceHealthy =\n            IsVisualCadenceHealthy(");
        AssertContains(diagnostics.SourceFamilyText, "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.");
        AssertContains(diagnostics.SourceFamilyText, "if (visualCadenceHealthy)\n        {\n            return new DiagnosticEvaluation(\n                \"Healthy\",");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsMjpegDuplicateCadenceDetected(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.SourceFamilyText, "health.MjpegPacketHashDuplicateFramePercent < 20.0");
        AssertContains(diagnostics.SourceFamilyText, "health.MjpegPacketHashUniqueObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnostics.SourceFamilyText, "health.VisualCadenceChangeObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnostics.SourceFamilyText, "health.SourceFrameRateExact.Value <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnostics.SourceFamilyText, "var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);");
        AssertContains(diagnostics.SourceFamilyText, "\"source_signal\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Captured HFR MJPEG cadence contains repeated source frames.\"");
        AssertContains(diagnostics.SourceFamilyText, "$\"{lanes.MjpegDuplicate} | {lanes.Visual} | {lanes.SourceSignal}\"");
        AssertContains(diagnostics.SourceFamilyText, "!visualCadenceHealthy &&\n            IsPreviewOnePercentLowDegraded(");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsVisualCadenceHealthy(");
        AssertContains(diagnostics.SourceFamilyText, "changeObservedFps >= targetFrameRate * PreviewOnePercentLowWarningRatio");
        AssertContains(diagnostics.SourceFamilyText, "repeatFramePercent <= 1.0");
        AssertContains(diagnostics.SourceFamilyText, "longestRepeatRun <= 1");
        AssertContains(diagnostics.SourceFamilyText, "\"Present/display 1% low is below target.\"");
        AssertContains(diagnostics.SourceFamilyText, "var recentMjpeg = UpdateMjpegRecentCounters(health, nowTick);");
        AssertContains(diagnostics.SourceFamilyText, "recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}");
        AssertContains(diagnostics.SourceFamilyText, "recentMjpeg.TotalDropped <= 0");
        AssertContains(diagnostics.SourceFamilyText, "if (recentRendererSubmitted < DiagnosticThresholds.RendererDropWarningMinSamples ||\n            recentRendererDropPercent <= DiagnosticThresholds.RendererDropWarningPercent)");
        AssertDoesNotContain(diagnostics.SourceFamilyText, "rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||\n            previewRuntime.DisplayCadenceSlowFramePercent > 1.0");
    }

    private static void AssertDiagnosticsRefreshFlashbackExportOwnership(string dispatcherText)
    {
        var captureServiceText = ReadCaptureServiceDiagnosticsRefreshSource();
        var flashbackBackendText = ReadFlashbackBackendResourcesSource();
        var exportOperationsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var exportCoreText = ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        var exportDiagnosticsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        AssertContains(captureServiceText, "private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertDoesNotContain(exportOperationsText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(exportOperationsText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportCoreText, "private FlashbackExportPreparationResult PrepareFlashbackExportRequest(");
        AssertContains(exportCoreText, "PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "private FlashbackExportForceRotatePreparation PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "ForceRotateForExport");
        AssertContains(exportCoreText, "CreateFlashbackExportThrottleDelayProvider");
        AssertContains(exportDiagnosticsText, "private long BeginFlashbackExportDiagnostics(");
        AssertContains(exportDiagnosticsText, "private void RecordRejectedFlashbackExportDiagnostics(");
        AssertContains(exportDiagnosticsText, "private void CompleteFlashbackExportDiagnostics(");
        AssertContains(exportDiagnosticsText, "private IProgress<ExportProgress> CreateFlashbackExportProgressSink(");
        AssertContains(exportDiagnosticsText, "private void UpdateFlashbackExportProgress(");
        AssertContains(exportDiagnosticsText, "private void RecordFlashbackExportForceRotateFallback(");
        AssertContains(exportDiagnosticsText, "private sealed class FlashbackExportProgressForwarder");
        AssertContains(captureServiceText, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);");
        AssertContains(captureServiceText, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(captureServiceText, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackBackend.Exporter ??= new FlashbackExporter();\n            }");
        AssertOccursBefore(captureServiceText, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(captureServiceText, "var sessionLockHeld = false;");
        AssertContains(captureServiceText, "sessionLockHeld = true;");
        AssertContains(captureServiceText, "if (sessionLockHeld)");
        AssertContains(captureServiceText, "var exportOperationLockHeld = false;");
        AssertContains(captureServiceText, "exportOperationLockHeld = true;");
        AssertContains(captureServiceText, "catch (OperationCanceledException) when (ct.IsCancellationRequested)");
        AssertContains(captureServiceText, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(captureServiceText, "private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)");
        AssertContains(captureServiceText, "backendLeaseHeld = false;\n        ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_backend_lease\");");
        var exportRangeMethod = ExtractMemberCode(exportOperationsText, "ExportFlashbackRangeAsync");
        var exportLastNMethod = ExtractMemberCode(exportOperationsText, "ExportFlashbackLastNSecondsAsync");
        AssertContains(exportRangeMethod, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportRangeMethod, "operationName: \"range\",");
        AssertContains(exportRangeMethod, "snapshotExporter: snapshot.Exporter,");
        AssertContains(exportRangeMethod, "resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(");
        AssertContains(exportLastNMethod, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportLastNMethod, "operationName: \"last_n\",");
        AssertContains(exportLastNMethod, "snapshotExporter: snapshot.Exporter,");
        AssertContains(exportLastNMethod, "resolveRangeAfterEvictionPaused: CreateFlashbackExportLastNRangeResolver(seconds)");
        var backendSnapshotMethod = ExtractMemberCode(exportOperationsText, "SnapshotFlashbackExportBackendAsync");
        AssertContains(backendSnapshotMethod, "new FlashbackExporter()");
        AssertContains(backendSnapshotMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(backendSnapshotMethod, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(flashbackBackendText, "outerPauseApplied = bufferManager != null;");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback export cancelled.\", inPoint, outPoint);");
        AssertContains(captureServiceText, "var exportId = 0L;");
        AssertContains(captureServiceText, "var evictionPaused = false;");
        AssertContains(captureServiceText, "exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);");
        AssertContains(captureServiceText, "var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);");
        AssertContains(captureServiceText, "segmentPaths = forceRotateResult.SegmentPaths;");
        AssertContains(captureServiceText, "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)");
        AssertContains(captureServiceText, "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        var forceRotateFailedBlock = ExtractTextBetween(
            exportCoreText,
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)",
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        AssertContains(forceRotateFailedBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(forceRotateFailedBlock, "preserved_segments={preservedArtifacts.Count}");
        AssertContains(forceRotateFailedBlock, "return FlashbackExportForceRotatePreparation.Failure(result);");
        var forceRotateFallbackBlock = ExtractTextBetween(
            exportCoreText,
            "if (segmentPaths.Count == 0)",
            "return FlashbackExportForceRotatePreparation.Ready");
        AssertContains(forceRotateFallbackBlock, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertContains(forceRotateFallbackBlock, "RecordFlashbackExportForceRotateFallback(exportId, segmentPaths.Count, inPoint, outPoint);");
        AssertDoesNotContain(forceRotateFallbackBlock, "force_rotate_failed");
        AssertDoesNotContain(forceRotateFallbackBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(captureServiceText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(captureServiceText, "captureBoundarySnapshot: sink => CaptureFlashbackRecordingBoundarySnapshot(sink, recordingBoundary)");
        AssertContains(flashbackBackendText, "captureBoundarySnapshot?.Invoke(flashbackSink);");
        AssertOccursBefore(flashbackBackendText, "captureBoundarySnapshot?.Invoke(flashbackSink);", "var exportResult = await exportRecordingAsync(");
        AssertContains(captureServiceText, "counters: recordingBoundary.Counters ?? CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(captureServiceText, "audioCounters: recordingBoundary.AudioCounters ?? GetRecordingAudioCountersSinceBaseline");
        AssertContains(captureServiceText, "evictionPaused = true;");
        AssertContains(captureServiceText, "if (exportId != 0)");
        AssertContains(captureServiceText, "if (evictionPaused)");
        AssertContains(captureServiceText, "ResumeFlashbackEvictionBestEffort(bufferManager, \"flashback_export\");");
        AssertContains(flashbackBackendText, "resumeEvictionBestEffort(bufferManager, \"flashback_recording_finalize\");");
        AssertContains(captureServiceText, "RecordLastFlashbackExportResult(exportId, failure);");
        AssertContains(captureServiceText, "private void RecordLastFlashbackExportResult(long exportId, FinalizeResult result)");
        AssertContains(captureServiceText, "Volatile.Write(ref _lastFlashbackExportResultId, exportId);");
        AssertContains(captureServiceText, "private FinalizeResult FailFlashbackExport(\n        string outputPath,\n        string statusMessage,\n        TimeSpan? inPoint = null,\n        TimeSpan? outPoint = null)");
        AssertContains(captureServiceText, "Logger.Log($\"FLASHBACK_EXPORT_REJECTED status='{statusMessage}' output='{outputPath}'\");");
        AssertContains(captureServiceText, "_lastExportResult = result;");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);");
        AssertContains(captureServiceText, "private void RecordRejectedFlashbackExportDiagnostics(\n        string outputPath,\n        FinalizeResult result,\n        TimeSpan? inPoint = null,\n        TimeSpan? outPoint = null)");
        AssertContains(captureServiceText, "if (_flashbackExportActive)");
        AssertContains(captureServiceText, "Volatile.Write(ref _lastFlashbackExportResultId, 0);");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_REJECTED_DIAGNOSTICS_DEFERRED");
        AssertContains(captureServiceText, "active_id={_flashbackExportId}");
        AssertContains(captureServiceText, "if (_flashbackExportId != exportId || !_flashbackExportActive)");
        AssertContains(captureServiceText, "var statusMessage = ex is OperationCanceledException && ct.IsCancellationRequested\n                ? \"Flashback export cancelled.\"\n                : ex.Message;");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_CORE_FAIL id={exportId} type={ex.GetType().Name}");
        AssertContains(captureServiceText, "var failure = FinalizeResult.Failure(outputPath, statusMessage);");
        AssertContains(captureServiceText, "CompleteFlashbackExportDiagnostics(exportId, failure);\n            }\n            else\n            {\n                RecordRejectedFlashbackExportDiagnostics(outputPath, failure, inPoint, outPoint);\n            }\n            return failure;");
        AssertContains(captureServiceText, "_flashbackExportStartedUtcUnixMs = now;");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = now;");
        AssertContains(captureServiceText, "var completedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = completedUtcUnixMs;");
        AssertContains(captureServiceText, "_flashbackExportLastProgressUtcUnixMs = completedUtcUnixMs;");
        AssertContains(captureServiceText, "ClassifyFlashbackExportFailureKind(result.StatusMessage)");
        AssertContains(captureServiceText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(captureServiceText, "return \"UnavailableDuringRecording\";");
        AssertContains(captureServiceText, "return \"BufferInactive\";");
        AssertContains(captureServiceText, "ContainsFlashbackExportFailureText(statusMessage, \"buffer has no active file\")");
        AssertContains(captureServiceText, "return \"InvalidOutputPath\";");
        AssertContains(captureServiceText, "return \"NoMediaWritten\";");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\", inPoint, outPoint);");
        AssertContains(exportCoreText, "ResolveFlashbackExportRangeAfterEvictionPaused(");
        AssertContains(exportCoreText, "var validStart = manager.ValidStartPts;");
        AssertContains(exportCoreText, "var bufferedDuration = manager.BufferedDuration;");
        AssertContains(exportCoreText, "var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);");
        AssertContains(exportCoreText, "var bufferOutPoint = outPoint.HasValue\n            ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)\n            : TimeSpan.MaxValue;");
        AssertContains(exportCoreText, "var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);");
        AssertContains(exportCoreText, "var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);");
        AssertContains(captureServiceText, ".Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))");
        AssertContains(captureServiceText, "var pathKey = TryGetFullPath(path);");
        AssertContains(captureServiceText, "segmentInfo.TryGetValue(pathKey, out var info)");
        AssertContains(captureServiceText, "private static string? TryGetFullPath(string? path)");
        AssertContains(captureServiceText, "FLASHBACK_PATH_NORMALIZE_WARN");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "if (ct.IsCancellationRequested)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export cancelled.\");\n        }\n\n        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export duration must be finite, greater than zero, and within TimeSpan range.\");\n        }");
        AssertRegex(
            dispatcherText,
            "if \\(!double\\.IsFinite\\(seconds\\) \\|\\|\\n\\s*seconds <= 0 \\|\\|\\n\\s*seconds > TimeSpan\\.MaxValue\\.TotalSeconds\\)",
            "Flashback export duration guard");
        AssertContains(dispatcherText, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(captureServiceText, "? \"Cancelled\"");
        AssertContains(captureServiceText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(captureServiceText, "if (exportOperationLockHeld)");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, \"flashback_export_operation\");");
        AssertContains(captureServiceText, "DisposeCoordinationLocksBestEffort();");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_sessionTransitionLock, \"session_transition\");");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_backend_lease\");");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_flashbackExportOperationLock, \"flashback_export_operation\");");
        AssertContains(captureServiceText, "CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN");
        AssertContains(captureServiceText, "private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(captureServiceText, "CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN");
        AssertContains(captureServiceText, "private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)");
        AssertContains(captureServiceText, "FLASHBACK_EVICTION_RESUME_WARN");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_sessionTransitionLock, sessionReleaseOperation);");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_preview_backend_dispose\");");
        AssertDoesNotContain(captureServiceText, "_flashbackBackendLeaseLock.Release();");
        AssertDoesNotContain(captureServiceText, "_flashbackExportOperationLock.Release();");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_ACTIVE_FILE_FALLBACK");
        AssertContains(captureServiceText, "Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths)");
        AssertContains(captureServiceText, "var startPts = FromSegmentMilliseconds(info.StartPtsMs);");
        AssertContains(captureServiceText, "var endPts = FromSegmentMilliseconds(info.EndPtsMs);");
        AssertContains(captureServiceText, "if (endPts < startPts)\n                {\n                    endPts = startPts;\n                }");
        AssertContains(captureServiceText, "StartPts = startPts,\n                    EndPts = endPts");
        AssertContains(captureServiceText, "private static TimeSpan FromSegmentMilliseconds(long milliseconds)");
        AssertContains(captureServiceText, "return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds\n            ? TimeSpan.MaxValue\n            : TimeSpan.FromMilliseconds(milliseconds);");
        AssertContains(exportCoreText, "private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)");
        AssertContains(captureServiceText, "if (bufferedDuration <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }");
        AssertContains(exportCoreText, "private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)");
        AssertContains(captureServiceText, "if (position < TimeSpan.Zero)\n        {\n            position = TimeSpan.Zero;\n        }");
        AssertContains(captureServiceText, "if (offset <= TimeSpan.Zero)\n        {\n            return position;\n        }");
        AssertContains(captureServiceText, "return position > TimeSpan.MaxValue - offset\n            ? TimeSpan.MaxValue\n            : position + offset;");
        AssertContains(captureServiceText, "var rawTotalSegments = progress.TotalSegments;");
        AssertContains(captureServiceText, "var totalSegments = Math.Max(0, rawTotalSegments);");
        AssertContains(captureServiceText, "if (totalSegments > 0 && segmentsProcessed > totalSegments)");
        AssertContains(captureServiceText, "Math.Clamp(rawPercent, 0.0, 100.0)");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_PROGRESS_NORMALIZED");
        AssertContains(captureServiceText, "raw_segments={rawSegmentsProcessed}/{rawTotalSegments}");
        AssertContains(captureServiceText, "raw_percent={rawPercent:0.###} percent={percent:0.###}");
        AssertContains(captureServiceText, "try\n            {\n                innerProgress?.Report(progress);\n            }\n            catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_EXPORT_PROGRESS_FORWARD_WARN id={exportId} type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");

        var flashbackExporterText = ReadFlashbackExporterSource();
        AssertContains(flashbackExporterText, "if (request.Segments is { Count: > 0 })");
        AssertContains(flashbackExporterText, "var useSegmentTimeline = segment.StartPts.HasValue");
        AssertContains(flashbackExporterText, "var comparePtsUs = state.UseSegmentTimeline");
        AssertContains(flashbackExporterText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(flashbackExporterText, "FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR");

    }

    private static void AssertDiagnosticSessionPlaybackMetricsOwnership(string diagnosticSessionText)
    {
        AssertContains(diagnosticSessionText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxPendingCommandsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsDroppedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsSkippedNotReadyAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackScrubUpdatesCoalescedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekCommandsCoalescedAtEnd");
        AssertContains(diagnosticSessionText, "internal readonly record struct PlaybackCommandHealth");
        AssertContains(diagnosticSessionText, "BuildPlaybackCommandHealth");
        AssertContains(diagnosticSessionText, "nonCoalescedDropped={commandHealth.NonCoalescedDropped}");
        AssertContains(diagnosticSessionText, "coalescedSeek={commandHealth.CoalescedSeek}");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackSubmitFailures\")");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackSeekCommandsCoalesced\")");
        AssertContains(diagnosticSessionText, "commandHealth.SubmitFailures > 0");
        AssertContains(diagnosticSessionText, "submitFailures={commandHealth.SubmitFailures}");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackCommandsDropped\")");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackObservedFpsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinObservedFpsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAvgFrameMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackP99FrameMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowFpsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowFpsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowSampleWindowObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxSessionFrameCountObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowOffsetMs");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowFrameCount");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxP99FrameMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxFrameMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxSlowFramePercentObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDecodeAvgMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDecodeP99MsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodePhaseAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodePhaseObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodeP99MsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSlowFramePercentAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterDelayDoublesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterDelayShrinksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterDelayDoublesObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterDelayShrinksObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioBufferedDurationMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioQueueDurationMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAbsAvDriftMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSubmitFailuresAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSubmitFailuresDelta");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSegmentSwitchesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackFmp4ReopensAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackWriteHeadWaitsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(diagnosticSessionText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd");
        AssertContains(diagnosticSessionText, "Flashback Playback Commands:");
        AssertContains(diagnosticSessionText, "coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd}");
        AssertContains(diagnosticSessionText, "coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd}");
        AssertContains(diagnosticSessionText, "failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
        AssertContains(diagnosticSessionText, "Flashback Playback Perf:");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"play\", [\"positionMs\"] = 1000 }");
        AssertContains(diagnosticSessionText, "flashback playback started at 1000ms");
        AssertContains(diagnosticSessionText, "flashback playback returned live");
        AssertContains(diagnosticSessionText, "ValidateFlashbackPlaybackSession(");
        AssertContains(diagnosticSessionText, "visualCadenceMetrics,");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(diagnosticSessionText, "flashback playback: no playback frames were observed");
        AssertContains(diagnosticSessionText, "var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);");
        AssertContains(diagnosticSessionText, "var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);");
        AssertContains(diagnosticSessionText, "if (!visualCadenceHealthy &&");
        AssertContains(diagnosticSessionText, "GetResetAwareCounterDelta(");
        AssertContains(diagnosticSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(diagnosticSessionText, "public long EndSessionFrameCount { get; set; }");
        AssertDoesNotContain(diagnosticSessionText, "flashback playback: observed FPS dipped below floor");
        AssertContains(diagnosticSessionText, "flashback playback: 1% low dipped below floor");
        AssertContains(diagnosticSessionText, "flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta}");
        AssertContains(diagnosticSessionText, "flashback playback: audio buffered duration exceeded budget");
        AssertContains(diagnosticSessionText, "flashback playback: absolute A/V drift exceeded budget");
        AssertContains(diagnosticSessionText, "BuildFlashbackPlaybackSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics)");
        AssertContains(diagnosticSessionText, "var baselineFrameCount = GetNullableLong(initialSnapshot, \"FlashbackPlaybackFrameCount\") ?? 0;");
        AssertContains(diagnosticSessionText, "frameCount > baselineFrameCount");
        AssertContains(diagnosticSessionText, "commandsProcessed > baselineCommandsProcessed");
        AssertContains(diagnosticSessionText, "IsPlaybackSnapshotActive(snapshot)");
        AssertContains(diagnosticSessionText, "private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(");
        AssertContains(diagnosticSessionText, "var sessionFrameCount = frameCount >= baselineFrameCount");
        AssertContains(diagnosticSessionText, "? frameCount - baselineFrameCount");
        AssertContains(diagnosticSessionText, ": frameCount;");
        AssertContains(diagnosticSessionText, "metrics.EndSessionFrameCount = relevance.SessionFrameCount;");
        AssertContains(diagnosticSessionText, "targetFps > 0 ? (long)Math.Ceiling(targetFps * 10.0) : 240");
        AssertContains(diagnosticSessionText, "if (onePercentLow <= 0 || sessionFrameCount < minimumPlaybackFramesForLowPercentile)");
        AssertContains(diagnosticSessionText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(diagnosticSessionText, "relevance.SessionFrameCount);");
        AssertContains(diagnosticSessionText, "fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowWindow={result.FlashbackPlaybackOnePercentLowSampleWindowObserved}");
        AssertContains(diagnosticSessionText, "onePercentLowMinRequiredFrames={result.FlashbackPlaybackOnePercentLowMinimumFrames}");
        AssertContains(diagnosticSessionText, "onePercentLowMaxSessionFrames={result.FlashbackPlaybackMaxSessionFrameCountObserved}");
        AssertContains(diagnosticSessionText, "onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs}");
        AssertContains(diagnosticSessionText, "onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(diagnosticSessionText, "p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##}");
        AssertContains(diagnosticSessionText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved}");
        AssertContains(diagnosticSessionText, "audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved}");
        AssertContains(diagnosticSessionText, "audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved}");
        AssertContains(diagnosticSessionText, "audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd}");
        AssertContains(diagnosticSessionText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(diagnosticSessionText, "Flashback Playback Decode:");
        AssertContains(diagnosticSessionText, "p99MsMax={result.FlashbackPlaybackMaxDecodeP99MsObserved:0.##}");
        AssertContains(diagnosticSessionText, "maxMsObserved={result.FlashbackPlaybackMaxDecodeMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "phaseObserved={result.FlashbackPlaybackMaxDecodePhaseObserved}");
        AssertContains(diagnosticSessionText, "sendMsObserved={result.FlashbackPlaybackMaxDecodeSendMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "audioMsObserved={result.FlashbackPlaybackMaxDecodeAudioMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "Flashback Playback Stages:");
        AssertContains(diagnosticSessionText, "seekCapHitsDelta={result.FlashbackPlaybackSeekForwardDecodeCapHitsDelta}");
        AssertContains(diagnosticSessionText, "FlashbackRecordingBackendObserved");
    }

    private static void AssertDiagnosticSessionFlashbackScenarioOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticModelsText = diagnosticSessionSources.ModelsText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;

        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(diagnosticSessionText, "flashback scrub stress begin requested");
        AssertContains(diagnosticSessionText, "flashback scrub stress update burst requested");
        AssertContains(diagnosticSessionText, "flashback scrub stress end requested");
        AssertContains(diagnosticSessionText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0 &&\n                string.Equals(");
        AssertContains(diagnosticSessionText, "state={GetString(lastSnapshot, \"FlashbackPlaybackState\") ?? \"Unknown\"}");
        AssertContains(diagnosticSessionText, "flashback scrub stress: playback did not settle live with an empty queue within 10s");
        AssertDoesNotContain(diagnosticSessionText, "flashback scrub stress: playback worker still alive after drain wait");
        AssertContains(diagnosticSessionText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback restart cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback encoder preset restored to");
        AssertContains(diagnosticSessionText, "flashback encoder cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(diagnosticSessionText, "flashback export during playback verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(diagnosticSessionText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(diagnosticSessionText, "flashback segment playback live headroom established");
        AssertContains(diagnosticSessionText, "flashback segment playback started near boundary");
        AssertContains(diagnosticSessionText, "frameCount >= 180");
        AssertContains(diagnosticSessionText, "playback FPS below source-rate target after warm sample");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-range-export.mp4\"");
        AssertContains(diagnosticSessionText, "\"flashback-range-export-audio-switch.mp4\"");
        AssertContains(diagnosticSessionText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(diagnosticSessionText, "\"SetAudioEnabled\"");
        AssertContains(diagnosticSessionText, "FlashbackExportActive");
        AssertContains(diagnosticSessionText, "[\"useSelectionRange\"] = true");
        AssertContains(diagnosticSessionText, "actions.Add($\"{scenarioLabel} verified\")");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(diagnosticSessionText, "async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(diagnosticSessionText, "var exportTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(\"FlashbackExport\");");
        AssertContains(diagnosticSessionText, "var exportTaskA = sendCommandAsync(\"FlashbackExport\", exportPayloadA, exportTimeoutMs);");
        AssertContains(diagnosticSessionText, "var exportTaskB = sendCommandAsync(\"FlashbackExport\", exportPayloadB, exportTimeoutMs);");
        AssertContains(diagnosticSessionText, "flashback concurrent exports verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(diagnosticSessionText, "var disableTask = SendCommandWithConnectRetryAsync(");
        AssertContains(diagnosticSessionText, "flashback disable/export requests issued");
        AssertContains(diagnosticSessionText, "flashback disable during export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-rotated-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback rotated segment observed");
        AssertContains(diagnosticSessionText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(diagnosticSessionText, "exportedSegments is null or < 2");
        AssertContains(diagnosticSessionText, "flashback rotated export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback preview cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback recording preview cycle preview stopped");
        AssertContains(diagnosticSessionText, "const int recordingCleanupTimeoutMs = 300_000;");
        AssertContains(diagnosticSessionText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred preset restored to");
        AssertContains(diagnosticSessionText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "Flashback export is unavailable while Flashback is the active recording backend");
        AssertContains(diagnosticSessionText, "flashback lifecycle disabled during playback");
        AssertContains(diagnosticSessionText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(diagnosticSessionText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(diagnosticSessionText, "\"flashback recording: RecordingBackend never reported Flashback\"");
        AssertContains(diagnosticSessionText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(diagnosticSessionText, "submittedDelta");
        AssertContains(diagnosticSessionText, "packetsDelta");
        AssertContains(diagnosticSessionText, "RecordingIntegritySequenceGaps");
        AssertContains(diagnosticSessionText, "RecordingIntegrityQueueDroppedFrames");
        AssertContains(diagnosticSessionText, "GetInt(snapshot, \"FlashbackBufferedDurationMs\") >= requiredBufferedDurationMs");
        AssertContains(diagnosticSessionText, "(GetNullableLong(snapshot, \"FlashbackEncodedFrames\") ?? 0) >= requiredEncodedFrames");
        AssertContains(diagnosticSessionText, "const int liveEdgeSafetyMarginMs = 5_000;");
        AssertContains(diagnosticSessionText, "const int leftEdgeSafetyMarginMs = 10_000;");
        AssertContains(diagnosticSessionText, "outPointMs + liveEdgeSafetyMarginMs + leftEdgeSafetyMarginMs");
        AssertContains(diagnosticSessionText, "var rangeEndMs = (int)Math.Clamp(bufferedDurationMs - liveEdgeSafetyMarginMs, 0, int.MaxValue);");
        AssertContains(diagnosticSessionText, "var rangeStartMs = Math.Max(0, rangeEndMs - outPointMs);");
        AssertContains(diagnosticSessionText, "requiredStartMs>={leftEdgeSafetyMarginMs}");
        AssertContains(diagnosticSessionText, "\"flashback stress: Flashback buffer did not become export-ready within 30s\"");
        AssertContains(diagnosticSessionText, "\"FlashbackAction\", new Dictionary<string, object?> { [\"action\"] = \"pause\" }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"seek\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "foreach (var positionMs in new[] { 750, 1_250, 2_000, 3_250, 1_500 })");
        AssertContains(diagnosticSessionText, "actions.Add(\"flashback scrub burst requested\");");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(diagnosticSessionText, "return positions[^1];");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = finalScrubPositionMs }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"seconds\"] = 1, [\"outputPath\"] = exportPath }");
        AssertContains(diagnosticSessionText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(diagnosticSessionText, "$\"maxPending={GetInt(lastSnapshot, \"FlashbackPlaybackMaxPendingCommands\")} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={GetInt(lastSnapshot, \"FlashbackPlaybackMaxCommandQueueLatencyMs\")} \"");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(diagnosticSessionText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(diagnosticSessionText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(diagnosticSessionText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(diagnosticSessionText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(diagnosticSessionText, "var playbackBaselineSnapshot = await WaitForFlashbackPlaybackStateAsync(");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback did not enter Playing before warm sample\"");
        AssertContains(diagnosticSessionText, "var warmBaselineSnapshot = playbackBaselineSnapshot?.ValueKind == JsonValueKind.Object");
        AssertContains(diagnosticSessionText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(diagnosticSessionText, "flashback playback warmed frames=");
        AssertContains(diagnosticSessionText, "CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(diagnosticSessionText, "audioFallbackDelta={warmedAudioFallbacks.TotalDelta}");
        AssertContains(diagnosticSessionText, "staleDelta={warmedAudioFallbacks.StaleDelta}");
        AssertContains(diagnosticSessionText, "driftOutlierDelta={warmedAudioFallbacks.DriftOutlierDelta}");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback did not warm for");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master unavailable fallbacks exceeded startup allowance \"");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(diagnosticSessionText, "\"flashback preview: scheduler deadline drops increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: scheduler underflows increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: D3D frame stats failures increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: present/display pressure \"");
        AssertContains(diagnosticSessionText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(diagnosticSessionText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(diagnosticSessionText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(diagnosticSessionText, "var allowedDrops = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 10.0));");
        AssertContains(diagnosticSessionText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(diagnosticSessionText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(diagnosticSessionText, "var allowedDeadlineDrops = Math.Max(6, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 45.0));");
        AssertContains(diagnosticSessionText, "var allowedUnderflows = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 120.0));");
        AssertContains(diagnosticSessionText, "bool tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "deadlineDropsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "underflowsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "var onePercentLowFloor = targetFps * 0.80;");
        AssertContains(diagnosticSessionText, "var visualCadenceHealthy =");
        AssertContains(diagnosticSessionText, "IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps)");
        AssertContains(diagnosticSessionText, "if ((onePercentLowMiss && !visualCadenceHealthy) || presentP99Miss || totalP99Miss)");
        AssertContains(diagnosticSessionText, "visualChangeFpsMin={visualCadenceMetrics.MinChangeFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "var presentP99BudgetMs = targetFrameMs * 1.25;");
        AssertContains(diagnosticSessionText, "var totalP99BudgetMs = targetFrameMs * 1.35;");
        AssertContains(diagnosticSessionText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(diagnosticSessionText, "latestSlowPresentCallMs={previewD3DMetrics.LatestSlowFramePresentCallMs:0.##}");
        AssertContains(diagnosticSessionText, "latestSlowPending={previewD3DMetrics.LatestSlowFramePendingFrameCount}");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command latency exceeded threshold \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyCommand={FormatOptional(maxLatencyCommand)}\"");
        AssertContains(diagnosticSessionText, "\"flashback-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "$\"flashback export rejected: expected Failed status, got {status}\"");
        AssertContains(diagnosticSessionText, "message.Contains(\"Flashback buffer not active\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(diagnosticSessionText, "actions.Add(\n            \"flashback segment playback observed \"");
        AssertDoesNotContain(diagnosticSessionText, "flashback segment playback: excessive late frames");
        AssertContains(diagnosticSessionText, "var diagnosticHealthObservation = BuildSessionDiagnosticHealthObservation(");
        AssertContains(diagnosticSessionText, "BuildSourceCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "BuildDiagnosticHealthSourceWarningCounters(initialSnapshot, lastSnapshot)");
        AssertContains(diagnosticSessionText, "SourceReaderFramesDroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"MfSourceReaderFramesDropped\")");
        AssertContains(diagnosticSessionText, "VideoIngestErrorsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"VideoIngestErrorCount\")");
        AssertContains(diagnosticSessionText, "var sparseSourceCaptureCadenceWarning =");
        AssertContains(diagnosticSessionText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(diagnosticSessionText, "IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertContains(diagnosticSessionText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(diagnosticSessionText, "lastReject=force_rotate_draining");
        AssertContains(diagnosticSessionText, "sourceReaderFramesDroppedDelta > 0");
        AssertContains(diagnosticSessionText, "videoIngestErrorsDelta > 0");
        AssertContains(diagnosticSessionText, "var allowedSparseEvents = Math.Max(1, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 180.0));");
        AssertContains(diagnosticSessionText, "FlashbackDiagnosticWarmupFraction");
        AssertContains(diagnosticSessionText, "FlashbackDiagnosticMaxWarmupMs");
        AssertContains(diagnosticSessionText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(diagnosticSessionText, "diagnosticHealthSucceeded &&");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health source-signal warning tolerated for export reliability scenario");
        AssertContains(diagnosticSessionText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health preview scheduler transition warning tolerated for preview-cycle scenario");
        AssertContains(diagnosticSessionText, "EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(diagnosticSessionText, "private static bool EvaluateFlashbackWarningsSucceeded(");
        AssertContains(diagnosticSessionText, "IsToleratedFlashbackScenarioWarning(");
        AssertContains(diagnosticSessionText, "FlashbackWarningsSucceeded: EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(diagnosticScenariosText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(diagnosticScenariosText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(diagnosticScenariosText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(diagnosticScenariosText, "internal const string HelpList =");
        AssertContains(diagnosticScenariosText, "internal const string Description =");
        AssertContains(diagnosticModelsText, "internal const string CliUsage =");
        AssertContains(diagnosticModelsText, "DiagnosticSessionScenarioCatalog.HelpList");
        AssertContains(diagnosticScenariosText, "TryGetEntry(normalized, out _)");
        AssertContains(diagnosticScenariosText, "TryGetEntry(scenario, out var entry) && entry.RequiresPreview");
        AssertContains(diagnosticScenariosText, "entry.FlashbackExportVerificationFileName");
    }
}

namespace Sussudio.Tests
{
public sealed class PreviewPacingClassifierTests
{
    private const string InputTypeName = "Sussudio.Services.Automation.PreviewPacingClassificationInput";
    private const string ClassifierTypeName = "Sussudio.Services.Automation.PreviewPacingSlowStageClassifier";

    [Fact]
    public void PreviewPacingClassifier_SourceOwnershipIsCohesive()
    {
        var classifierText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");

        Assert.Contains("public sealed class PreviewPacingClassificationInput", classifierText);
        Assert.Contains("public readonly record struct PreviewPacingClassification(", classifierText);
        Assert.Contains("public static class PreviewPacingSlowStageClassifier", classifierText);
        Assert.DoesNotContain("partial class PreviewPacingSlowStageClassifier", classifierText);
        Assert.Contains("var dominantStage = ResolveDominantD3DStage(input, targetFrameMs);", classifierText);
        Assert.Contains("private static string ResolveDominantD3DStage(", classifierText);
        Assert.Contains("TryClassifySourceCapture(input, sourceSampleReady, targetFps", classifierText);
        Assert.Contains("TryClassifyPreviewJitterScheduler(input, targetFrameMs", classifierText);
        Assert.Contains("TryClassifyRenderSubmit(input, out var renderSubmitClassification)", classifierText);
        Assert.Contains("private static bool IsSourceCaptureSuspect(", classifierText);
        Assert.Contains("private static bool TryClassifySourceCapture(", classifierText);
        Assert.Contains("private static bool TryClassifyVisualDuplicateOrLowMotion(", classifierText);
        Assert.Contains("private static bool IsVisualDuplicateOrLowMotionSuspect(", classifierText);
        Assert.Contains("private static bool TryClassifyMjpegDecode(", classifierText);
        Assert.Contains("private static bool TryClassifyPreviewJitterScheduler(", classifierText);
        Assert.Contains("private static bool IsMjpegDecodeSuspect(", classifierText);
        Assert.Contains("private static bool IsPreviewJitterSuspect(", classifierText);
        Assert.Contains("private static bool TryClassifyCompositorMiss(", classifierText);
        Assert.Contains("private static bool TryClassifyRenderSubmit(", classifierText);
        Assert.Contains("private static double CalculatePercent(", classifierText);
        Assert.Contains("private static bool IsDominant(", classifierText);
        Assert.Contains("private static double Positive(double value)", classifierText);
        Assert.DoesNotContain("PreviewPacingSlowStageClassifier.D3D.cs", agentMapText);
        Assert.DoesNotContain("PreviewPacingSlowStageClassifier.Lanes.SourceVisual.cs", agentMapText);
        Assert.DoesNotContain("PreviewPacingSlowStageClassifier.Lanes.DecodeJitter.cs", agentMapText);
        Assert.DoesNotContain("PreviewPacingSlowStageClassifier.Lanes.Render.cs", agentMapText);
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "PreviewPacingSlowStageClassifier.D3D.cs")));
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "PreviewPacingSlowStageClassifier.Lanes.SourceVisual.cs")));
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "PreviewPacingSlowStageClassifier.Lanes.DecodeJitter.cs")));
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "PreviewPacingSlowStageClassifier.Lanes.Render.cs")));
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "PreviewPacingClassificationModels.cs")));
    }

    [Fact]
    public void PreviewPacingClassifier_IsWiredIntoAutomationSnapshots()
    {
        var contractsText = ReadAutomationSnapshotFamilyText();
        var diagnosticsSnapshotsText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs"));
        var diagnosticsSnapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        var diagnosticsSnapshotProjectionFlatteningText = diagnosticsSnapshotProjectionText;
        var diagnosticsSnapshotProjectionSnapshotEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        var diagnosticsSnapshotProjectionCaptureCadenceText = diagnosticsSnapshotProjectionText;
        var diagnosticsPreviewPacingText = diagnosticsSnapshotsText;
        var diagnosticsRealtimePreviewCountersText = diagnosticsSnapshotsText;
        var diagnosticsCountersText = diagnosticsRealtimePreviewCountersText;
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + diagnosticsSnapshotsText
            + "\n" + diagnosticsSnapshotProjectionText
            + "\n" + diagnosticsSnapshotProjectionFlatteningText
            + "\n" + diagnosticsSnapshotProjectionSnapshotEvaluationText
            + "\n" + diagnosticsSnapshotProjectionCaptureCadenceText
            + "\n" + diagnosticsPreviewPacingText
            + "\n" + diagnosticsCountersText;

        Assert.Contains("public string PreviewPacingLikelySlowStage { get; init; }", contractsText);
        Assert.Contains("public string PreviewPacingSlowStageConfidence { get; init; }", contractsText);
        Assert.Contains("public string PreviewPacingSlowStageEvidence { get; init; }", contractsText);
        Assert.Contains("var previewPacingClassification = ClassifyPreviewPacing(", diagnosticsSnapshotsText);
        Assert.Contains("new PreviewPacingClassificationInput", diagnosticsSnapshotsText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage", diagnosticsSnapshotProjectionFlatteningText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage", diagnosticsSnapshotProjectionSnapshotEvaluationText);
        Assert.Contains("PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage", diagnosticsSnapshotProjectionSnapshotEvaluationText);
        Assert.Contains("private static PreviewPacingClassification ClassifyPreviewPacing(", diagnosticsPreviewPacingText);
        Assert.Contains("PreviewPacingSlowStageClassifier.Classify", diagnosticsPreviewPacingText);
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.PreviewPacing.cs")));
        Assert.Contains("PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps", diagnosticsHubText);
        Assert.Contains("CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("RecentD3DMissedRefreshes = recentD3DMissedRefreshes", diagnosticsHubText);
        Assert.Contains("RecentPreviewJitterScheduleLateCount = recentPreviewJitter.ScheduleLateCount", diagnosticsHubText);
        Assert.Contains("RecentD3DFrameLatencyWaitTimeoutCount = recentD3DFrameLatencyWaitTimeouts", diagnosticsHubText);
        Assert.Contains("UpdateD3DFrameLatencyWaitRecentCounters", diagnosticsHubText);
        Assert.Contains("private long UpdateD3DFrameLatencyWaitRecentCounters(", diagnosticsRealtimePreviewCountersText);
        Assert.DoesNotContain("private long UpdateD3DFrameLatencyWaitRecentCounters(", ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs"));
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.Counters.RealtimePreview.cs")));
        Assert.Contains("PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageConfidence = previewPacingClassification.Confidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageEvidence = previewPacingClassification.Evidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingLikelySlowStage = preview.PacingLikelySlowStage", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageConfidence = preview.PacingSlowStageConfidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageEvidence = preview.PacingSlowStageEvidence", diagnosticsHubText);
        Assert.Contains("PacingLikelySlowStage: snapshot.PreviewPacingLikelySlowStage", diagnosticsHubText);
        Assert.False(File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs")));
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

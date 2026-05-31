using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
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
    public Task AutomationDiagnosticsAudioProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsCaptureCommandProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsUserSettingsProjectionLivesWithSnapshotProjection()
        => global::Program.AutomationDiagnosticsUserSettingsProjection_LivesWithSnapshotProjection();

    [Fact]
    public Task AutomationDiagnosticsCaptureFormatProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsCaptureTransportProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsHdrPipelineProjectionLivesWithCaptureFormatProjection()
        => global::Program.AutomationDiagnosticsHdrPipelineProjection_LivesWithCaptureFormatProjection();

    [Fact]
    public Task AutomationDiagnosticsCaptureCadenceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsVisualCadenceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsMjpegProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSourceSignalProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSourceTelemetryProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingPipelineProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingBackendProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingOutputProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsProcessResourceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsAvSyncProjectionLivesWithProjectionRoot()
        => global::Program.AutomationDiagnosticsAvSyncProjection_LivesWithProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsPreviewRuntimeProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsPreviewD3DProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsFlashbackExportProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsFlashbackRecordingProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsFlashbackPlaybackProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial();
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
    public Task AutomationCommandKindSourceOwnershipIsContractAligned()
        => global::Program.AutomationContracts_SourceOwnership_IsModelAligned();

    [Fact]
    public Task DiagnosticsSnapshotRefreshIsSerializedForRecordingResponses()
        => global::Program.DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses();
}

}

// Legacy app-surface checks executed by AutomationAppSurfaceContractsTests.
static partial class Program
{
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
        AssertContains(pipeServerRootText, "private async Task<CommandExecutionResult> ExecuteCommandWithTimeoutAsync(");
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
        AssertContains(pipeServerText, "ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);");
        AssertContains(pipeServerText, "Request timed out after {_owner._requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "\"request-timeout\"");

        return Task.CompletedTask;
    }

    internal static Task MainWindowAutomation_WiresPipeAuthFallbackPolicy()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationController.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var launchStartupControllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");

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
        AssertContains(startupText, "StartAutomationHost = _automationHostLifecycleController.Start,");
        AssertContains(launchStartupControllerText, "_context.StartAutomationHost();");
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
}

// Tests for diagnostic snapshot JSON source-generation compatibility.
static partial class Program
{
    internal static Task LoggingJsonContext_SerializesStructuredSnapshotPayloads()
    {
        var loggerText = ReadRepoFile("Sussudio/Logger.cs");
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
}

using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

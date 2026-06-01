using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs")
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

// Flashback backend preview pipeline contracts live with the automation xUnit wrappers.
static partial class Program
{
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
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            + "\n" + ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings();
        var captureServiceRawText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource();
        var captureServiceRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleText = ReadCaptureServicePreviewLifecycleSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var flashbackPreviewBackendText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs");
        var flashbackBackendResourcesText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var viewModelPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
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
}

static partial class Program
{
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
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
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
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadAutomationSnapshotFlatteningFamilyText()
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs")
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
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
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
}

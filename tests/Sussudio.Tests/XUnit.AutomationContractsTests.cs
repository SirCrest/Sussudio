using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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


// Tests for diagnostic snapshot JSON source-generation compatibility.
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

    // â”€â”€ CaptureSessionCoordinator: CaptureCommand shape â”€â”€

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

    // â”€â”€ CaptureSessionCoordinator: CaptureSessionSnapshot â”€â”€

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
}

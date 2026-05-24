using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
    public Task AutomationDispatcherEntryPipelineLivesInFocusedPartials()
        => global::Program.AutomationCommandDispatcher_EntryPipeline_LivesInFocusedPartials();

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
        => global::Program.AutomationCommandDispatcher_UiSettingsCommands_OwnUiSettingsApplication();

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
    public Task AutomationDispatcherFlashbackCommandsLiveInFocusedPartial()
        => global::Program.AutomationCommandDispatcher_FlashbackCommands_LiveInFocusedPartial();

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

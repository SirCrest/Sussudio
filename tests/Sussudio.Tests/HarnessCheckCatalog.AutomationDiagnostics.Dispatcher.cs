using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsDispatcherChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Automation dispatcher extracts string payload fields",
            AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher extracts bool payload fields",
            AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher extracts int payload fields",
            AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher extracts double payload fields",
            AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher rejects non-finite double payload fields",
            AutomationCommandDispatcher_GetDouble_RejectsNonFiniteValues);
        await AddCheckAsync(results,
            "Automation dispatcher requires missing string fields",
            AutomationCommandDispatcher_RequireString_ThrowsOnMissing);
        await AddCheckAsync(results,
            "Automation dispatcher defaults missing window action",
            AutomationCommandDispatcher_WindowAction_DefaultsMissingActionToRestore);
        await AddCheckAsync(results,
            "Automation dispatcher defaults missing wait condition",
            AutomationCommandDispatcher_WaitForCondition_DefaultsMissingConditionToPreviewFrames);
        await AddCheckAsync(results,
            "Automation dispatcher trivial handler payload fields match catalog",
            AutomationCommandDispatcher_TrivialHandlers_MatchCatalogPayloadFields);
        await AddCheckAsync(results,
            "Automation dispatcher audio ramp trace payload field matches catalog",
            AutomationCommandDispatcher_GetAudioRampTrace_MetadataMatchesDispatcherPayload);
        await AddCheckAsync(results,
            "Automation dispatcher ready-device gate classifies commands",
            AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands);
        await AddCheckAsync(results,
            "Automation dispatcher ready-independent catalog commands bypass device readiness",
            AutomationCommandDispatcher_CatalogReadyIndependentCommands_BypassDeviceReadiness);
        await AddCheckAsync(results,
            "Automation dispatcher window close waits for completion",
            AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion);
        await AddCheckAsync(results,
            "Automation dispatcher preview health waits for first visual",
            AutomationCommandDispatcher_PreviewRendererHealthy_RequiresFirstVisual);
        await AddCheckAsync(results,
            "Automation dispatcher authorization contract is token-gated",
            AutomationCommandDispatcher_AuthorizesConfiguredTokens);
        await AddCheckAsync(results,
            "Automation dispatcher manifest command is read-only and readiness-independent",
            AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent);
        await AddCheckAsync(results,
            "Automation dispatcher device commands live in focused partial",
            AutomationCommandDispatcher_DeviceCommands_LiveInFocusedPartial);
        await AddCheckAsync(results,
            "Automation dispatcher flashback failures return playback diagnostics",
            AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics);
        await AddCheckAsync(results,
            "Automation dispatcher Flashback commands live in focused partial",
            AutomationCommandDispatcher_FlashbackCommands_LiveInFocusedPartial);
        await AddCheckAsync(results,
            "Automation dispatcher verification commands live in focused partial",
            AutomationCommandDispatcher_VerificationCommands_LiveInFocusedPartial);
        await AddCheckAsync(results,
            "Automation dispatcher visual capture commands live in focused partial",
            AutomationCommandDispatcher_VisualCaptureCommands_LiveInFocusedPartial);
        await AddCheckAsync(results,
            "Automation dispatcher handles every AutomationCommandKind value",
            AutomationCommandDispatcher_AllCommandKinds_AreHandled);
    }
}

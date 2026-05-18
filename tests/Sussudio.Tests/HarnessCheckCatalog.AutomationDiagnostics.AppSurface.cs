using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsAppShellAndFormatterChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "App wires recoverable and fatal unhandled exception policy",
            App_Xaml_WiresUnhandledExceptionPolicy);
        await AddCheckAsync(results,
            "Bool converters preserve inversion and visibility mappings",
            BoolConverters_PreserveInversionAndVisibilityMappings);
        await AddCheckAsync(results,
            "Display formatters map source HDR states",
            DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates);
        await AddCheckAsync(results,
            "Logging JSON context serializes structured snapshot payloads",
            LoggingJsonContext_SerializesStructuredSnapshotPayloads);
        await AddCheckAsync(results,
            "UI automation commands are not blocked on device readiness",
            UiAutomationCommands_AreNotBlockedOnDeviceReadiness);
    }

    private static async Task AddAutomationDiagnosticsMainWindowSurfaceChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainWindow automation IDs cover the agent-critical UI surface",
            MainWindowAutomationIds_CoverAgentCriticalSurface);
        await AddCheckAsync(results,
            "MainWindow full-screen automation awaits transition tasks",
            MainWindowFullScreenAutomation_AwaitsTransitionTask);
        await AddCheckAsync(results,
            "MainWindow window automation commands live in controller",
            MainWindowWindowAutomationCommands_LiveInController);
        await AddCheckAsync(results,
            "MainWindow UI dispatching lives in dispatching partial",
            MainWindowUiDispatching_LivesInDispatchingPartial);
    }

    private static async Task AddAutomationDiagnosticsPipeServerAndAuthChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Automation pipe server gates default security fallback on auth token",
            NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken);
        await AddCheckAsync(results,
            "Automation pipe server request timeouts use bounded dispatch cancellation",
            NamedPipeAutomationServer_RequestTimeoutsUseBoundedDispatchCancellation);
        await AddCheckAsync(results,
            "MainWindow wires automation pipe auth fallback policy",
            MainWindowAutomation_WiresPipeAuthFallbackPolicy);
        await AddCheckAsync(results,
            "Stream Deck scope documents automation auth envelope",
            StreamDeckPluginScope_DocumentsAutomationAuthEnvelope);
    }
}

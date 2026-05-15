using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsPipeServerAndAuthChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Automation pipe server gates default security fallback on auth token",
            NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken);
        await AddCheckAsync(results,
            "MainWindow wires automation pipe auth fallback policy",
            MainWindowAutomation_WiresPipeAuthFallbackPolicy);
        await AddCheckAsync(results,
            "Stream Deck scope documents automation auth envelope",
            StreamDeckPluginScope_DocumentsAutomationAuthEnvelope);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsChecksAsync(List<CheckResult> results)
    {
        await AddAutomationDiagnosticsAppShellAndFormatterChecksAsync(results);
        await AddAutomationDiagnosticsMainWindowSurfaceChecksAsync(results);
        await AddAutomationDiagnosticsDispatcherChecksAsync(results);
        await AddAutomationDiagnosticsPipeServerAndAuthChecksAsync(results);
        await AddAutomationDiagnosticsViewModelAndFlashbackUiChecksAsync(results);
        await AddAutomationDiagnosticsCaptureAndFlashbackRoutingChecksAsync(results);
        await AddAutomationDiagnosticsSnapshotProjectionChecksAsync(results);
        await AddCheckAsync(results,
            "Automation SetRecordingEnabled uses recording-sized client timeout",
            AutomationProtocol_SetRecordingUsesRecordingSizedTimeout);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task<List<CheckResult>> RunAllChecksAsync()
    {
        var results = new List<CheckResult>();
        await AddCoreRuntimeChecksAsync(results);
        await AddAutomationDiagnosticsChecksAsync(results);
        await AddPresentationPreviewChecksAsync(results);
        await AddMcpDiagnosticsPipelineChecksAsync(results);
        await AddFlashbackChecksAsync(results);
        return results;
    }

    private static async Task AddCheckAsync(List<CheckResult> results, string name, Func<Task> check)
        => results.Add(await RunCheckAsync(name, check));

    private static async Task AddAutomationDiagnosticsChecksAsync(List<CheckResult> results)
    {
        await AddAutomationDiagnosticsAppShellAndFormatterChecksAsync(results);
        await AddAutomationDiagnosticsMainWindowSurfaceChecksAsync(results);
        await AddAutomationDiagnosticsDispatcherChecksAsync(results);
        await AddAutomationDiagnosticsPipeServerAndAuthChecksAsync(results);
        await AddAutomationDiagnosticsViewModelAndFlashbackUiChecksAsync(results);
        await AddAutomationDiagnosticsCaptureAndFlashbackRoutingChecksAsync(results);
        await AddAutomationDiagnosticsSnapshotProjectionChecksAsync(results);
    }

    private static async Task AddPresentationPreviewChecksAsync(List<CheckResult> results)
    {
        await AddPresentationPreviewMainViewModelInitialChecksAsync(results);
        await AddPresentationPreviewMainWindowInitialChecksAsync(results);
        await AddPresentationPreviewCaptureChecksAsync(results);
        await AddPresentationPreviewMainViewModelChecksAsync(results);
        await AddPresentationPreviewMainWindowChecksAsync(results);
        await AddPresentationPreviewD3DChecksAsync(results);
        await AddPresentationPreviewPacingChecksAsync(results);
    }

    private static async Task AddMcpDiagnosticsPipelineChecksAsync(List<CheckResult> results)
    {
        await AddDiagnosticSessionChecksAsync(results);
    }

    private static async Task AddFlashbackChecksAsync(List<CheckResult> results)
    {
        await AddFlashbackDecoderChecksAsync(results);
        await AddFlashbackExporterChecksAsync(results);
    }
}

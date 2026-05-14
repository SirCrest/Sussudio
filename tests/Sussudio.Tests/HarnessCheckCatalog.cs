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
        await AddRecordingModelChecksAsync(results);
        await AddFlashbackChecksAsync(results);
        await AddToolContractChecksAsync(results);
        return results;
    }

    private static async Task AddCheckAsync(List<CheckResult> results, string name, Func<Task> check)
        => results.Add(await RunCheckAsync(name, check));
}
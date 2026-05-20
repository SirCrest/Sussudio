using System;
using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task<List<CheckResult>> RunAllChecksAsync()
    {
        var results = new List<CheckResult>();
        await AddPresentationPreviewChecksAsync(results);
        await AddMcpDiagnosticsPipelineChecksAsync(results);
        return results;
    }

    private static async Task AddCheckAsync(List<CheckResult> results, string name, Func<Task> check)
        => results.Add(await RunCheckAsync(name, check));

    private static async Task AddPresentationPreviewChecksAsync(List<CheckResult> results)
    {
        await AddPresentationPreviewCaptureChecksAsync(results);
        await AddPresentationPreviewMainViewModelChecksAsync(results);
    }

    private static async Task AddMcpDiagnosticsPipelineChecksAsync(List<CheckResult> results)
    {
        await AddDiagnosticSessionChecksAsync(results);
    }

}

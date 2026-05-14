using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewChecksAsync(List<CheckResult> results)
    {
        await AddPresentationPreviewMainViewModelInitialChecksAsync(results);
        await AddPresentationPreviewMainWindowInitialChecksAsync(results);
        await AddPresentationPreviewCaptureChecksAsync(results);
        await AddPresentationPreviewMainViewModelChecksAsync(results);
        await AddPresentationPreviewStatsInitialChecksAsync(results);
        await AddPresentationPreviewMainWindowChecksAsync(results);
        await AddPresentationPreviewStatsChecksAsync(results);
        await AddPresentationPreviewD3DChecksAsync(results);
        await AddPresentationPreviewPacingChecksAsync(results);
    }
}

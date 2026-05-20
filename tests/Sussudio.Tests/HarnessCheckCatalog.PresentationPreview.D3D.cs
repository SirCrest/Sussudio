using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewD3DChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "D3D preview render passes live in focused partial",
            D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview shader rendering cache lives in focused partial",
            D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview shader sources live in focused file",
            D3D11PreviewRenderer_ShaderSourcesLiveInFocusedFile);
        await AddCheckAsync(results,
            "D3D preview frame-latency wait lives in focused partial",
            D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview render thread lives in focused partial",
            D3D11PreviewRenderer_RenderThreadLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview present accounting lives in focused partial",
            D3D11PreviewRenderer_PresentAccountingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview viewport helpers live in focused partial",
            D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview screenshot encoding lives in focused partial",
            D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture);
    }

}

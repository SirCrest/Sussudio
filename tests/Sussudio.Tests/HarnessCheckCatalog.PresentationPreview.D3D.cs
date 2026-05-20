using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewD3DChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "D3D preview diagnostics expose swap-chain and render timing contract",
            D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming);
        await AddCheckAsync(results,
            "D3D preview diagnostics expose snapshot model contract",
            D3D11PreviewRenderer_DiagnosticsContract_SnapshotModelsExposeExpectedProperties);
        await AddCheckAsync(results,
            "D3D preview diagnostics expose performance timeline contract",
            D3D11PreviewRenderer_DiagnosticsContract_PerformanceTimelineExposesExpectedProperties);
        await AddCheckAsync(results,
            "D3D preview configuration lives in focused partial",
            D3D11PreviewRenderer_ConfigurationLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview native interop lives in focused partial",
            D3D11PreviewRenderer_NativeInteropLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame types live in focused partial",
            D3D11PreviewRenderer_FrameTypesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame submission lives in focused partial",
            D3D11PreviewRenderer_SubmissionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame ownership lives in focused partial",
            D3D11PreviewRenderer_FrameOwnershipLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview DXGI frame statistics live in focused partial",
            D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview panel binding lives in focused partial",
            D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview shared-device handoff lives in focused partial",
            D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame upload lives in focused partial",
            D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial);
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
            "D3D preview slow-frame diagnostics live in focused partial",
            D3D11PreviewRenderer_SlowFrameDiagnosticsLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview metric tracking lives in focused partial",
            D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial);
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
            "D3D preview input resources live in focused partial",
            D3D11PreviewRenderer_InputResourcesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview lifecycle lives in focused partial",
            D3D11PreviewRenderer_LifecycleLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview device initialization lives in focused partial",
            D3D11PreviewRenderer_DeviceInitializationLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview viewport helpers live in focused partial",
            D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview screenshot encoding lives in focused partial",
            D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture);
    }

}

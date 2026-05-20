using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewMainWindowChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainWindow property changed routing delegates to focused controllers",
            MainWindowPropertyChangedRouting_DelegatesToFocusedControllers);
        await AddCheckAsync(results,
            "Preview runtime snapshot controller preserves null-D3D policy",
            PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy);
        await AddCheckAsync(results,
            "Preview runtime snapshot health policy preserves suspicion rules",
            PreviewRuntimeSnapshotHealthPolicy_PreservesSuspicionRules);
        await AddCheckAsync(results,
            "Preview runtime snapshot health input factory projects controller inputs",
            PreviewRuntimeSnapshotHealthInputFactory_ProjectsControllerInputs);
        await AddCheckAsync(results,
            "Preview runtime snapshot surface projection preserves visibility and health fields",
            PreviewRuntimeSnapshotSurfaceProjectionPolicy_PreservesVisibilityAndHealthFields);
        await AddCheckAsync(results,
            "Preview runtime snapshot startup projection preserves sampled startup fields",
            PreviewRuntimeSnapshotStartupProjectionPolicy_PreservesSampledStartupFields);
        await AddCheckAsync(results,
            "Preview runtime snapshot GPU playback projection preserves renderer and event fields",
            PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy_PreservesRendererAndEventFields);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-counter policy preserves CPU fallback counters",
            PreviewRuntimeD3DFrameCounterPolicy_PreservesCpuFallbackCounters);
        await AddCheckAsync(results,
            "Preview runtime D3D projection builder applies policy groups",
            PreviewRuntimeD3DProjectionBuilder_AppliesPolicyGroups);
        await AddCheckAsync(results,
            "Preview runtime D3D renderer state policy preserves null renderer defaults",
            PreviewRuntimeD3DRendererStatePolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D display cadence policy preserves null renderer defaults",
            PreviewRuntimeD3DDisplayCadencePolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D render CPU timing policy preserves null renderer defaults",
            PreviewRuntimeD3DRenderCpuTimingPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D pipeline latency policy preserves null renderer defaults",
            PreviewRuntimeD3DPipelineLatencyPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-statistics policy preserves null renderer defaults",
            PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-latency wait policy preserves null renderer defaults",
            PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-ownership policy preserves null renderer defaults",
            PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults);
    }
}

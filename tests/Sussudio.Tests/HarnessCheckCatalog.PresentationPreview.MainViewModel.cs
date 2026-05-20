using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewMainViewModelChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainViewModel automation routes preview volume persistence through save hook",
            MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook);
        await AddCheckAsync(results,
            "MainViewModel automation preview enablement lives in preview lifecycle controller",
            MainViewModelAutomation_PreviewEnablementLivesInPreviewLifecycleController);
        await AddCheckAsync(results,
            "MainViewModel automation HDR enablement lives in capture-mode transactions",
            MainViewModelAutomation_HdrEnablementLivesInCaptureModeTransactions);
        await AddCheckAsync(results,
            "MainViewModel capture routes audio monitoring through coordinator",
            MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator);
        await AddCheckAsync(results,
            "MainViewModel capture settings projection lives in focused partial",
            MainViewModelCaptureSettings_OwnsSettingsProjection);
        await AddCheckAsync(results,
            "MainViewModel capture settings frame-rate projection preserves precedence",
            MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence);
        await AddCheckAsync(results,
            "MainViewModel preview lifecycle lives in controller",
            MainViewModelPreviewLifecycle_LivesInController);
        await AddCheckAsync(results,
            "Audio ramp trace exposes control and render-side envelope telemetry",
            AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry);
    }
}

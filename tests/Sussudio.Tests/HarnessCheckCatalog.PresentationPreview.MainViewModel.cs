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
            "MainViewModel uses dependency composition seam",
            MainViewModel_UsesDependencyCompositionSeam);
        await AddCheckAsync(results,
            "MainViewModel UI dispatch controller uses dependency composition context",
            MainViewModelUiDispatchController_UsesDependencyCompositionContext);
        await AddCheckAsync(results,
            "MainViewModel presentation controllers use dependency composition contexts",
            MainViewModelPresentationControllers_UseDependencyCompositionContexts);
        await AddCheckAsync(results,
            "MainViewModel recording transition uses dependency composition context",
            MainViewModelRecordingTransition_UsesDependencyCompositionContext);
        await AddCheckAsync(results,
            "MainViewModel capture and device controllers use dependency composition contexts",
            MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts);
        await AddCheckAsync(results,
            "MainViewModel runtime controllers use dependency composition contexts",
            MainViewModelRuntimeControllers_UseDependencyCompositionContexts);
        await AddCheckAsync(results,
            "Audio ramp trace exposes control and render-side envelope telemetry",
            AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry);
    }
}

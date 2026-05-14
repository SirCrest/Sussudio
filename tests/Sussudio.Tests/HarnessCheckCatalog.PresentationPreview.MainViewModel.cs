using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewMainViewModelInitialChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Recording start and stop failures propagate to callers",
            MainViewModelCapture_RecordingFailuresPropagateToCallers);
    }

    private static async Task AddPresentationPreviewMainViewModelChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainViewModel automation routes preview volume persistence through save hook",
            MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook);
        await AddCheckAsync(results,
            "MainViewModel capture routes audio monitoring through coordinator",
            MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator);
        await AddCheckAsync(results,
            "MainViewModel capture settings projection lives in focused partial",
            MainViewModelCaptureSettings_OwnsSettingsProjection);
        await AddCheckAsync(results,
            "MainViewModel audio controls map analog gain curve and clamp endpoints",
            MainViewModelAudioControls_MapsAnalogGainCurveAndClamps);
        await AddCheckAsync(results,
            "MainViewModel audio monitoring preserves volume persistence and ramped routing",
            MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting);
        await AddCheckAsync(results,
            "MainViewModel audio controls preserve microphone and device guards",
            MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards);
        await AddCheckAsync(results,
            "Native XU audio control profiles live in focused partial",
            NativeXuAudioControlService_ProfilesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "Native XU audio control transport lives in focused partial",
            NativeXuAudioControlService_TransportLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MainViewModel audio meters own callback meter state",
            MainViewModelAudioMeters_OwnCallbackMeterState);
        await AddCheckAsync(results,
            "MainViewModel uses dependency composition seam",
            MainViewModel_UsesDependencyCompositionSeam);
        await AddCheckAsync(results,
            "Audio ramp trace exposes control and render-side envelope telemetry",
            AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry);
        await AddCheckAsync(results,
            "Live pixel format surfaces prefer source subtype over decoded output",
            LivePixelFormatSurfaces_PreferReaderSourceSubtype);
    }
}

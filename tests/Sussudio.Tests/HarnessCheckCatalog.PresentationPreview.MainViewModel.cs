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
            "MainViewModel output path selection lives in focused partial",
            MainViewModelOutputPathSelection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Output drive free-space presentation builder handles invalid paths",
            OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty);
        await AddCheckAsync(results,
            "Output drive free-space presentation lives in focused helper",
            OutputDriveSpacePresentationBuilder_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "MainViewModel preview lifecycle lives in controller",
            MainViewModelPreviewLifecycle_LivesInController);
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
            "MainViewModel device-audio request controller owns request lifetime",
            MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime);
        await AddCheckAsync(results,
            "MainViewModel audio-device selection policy lives in focused helper",
            AudioDeviceSelectionPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "MainViewModel audio-device startup policy filters capture audio and uses saved fallbacks",
            AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks);
        await AddCheckAsync(results,
            "MainViewModel audio-device startup policy preserves previous selections",
            AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections);
        await AddCheckAsync(results,
            "MainViewModel audio-device refresh policy preserves selections",
            AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback);
        await AddCheckAsync(results,
            "MainViewModel audio-device selection policy handles empty lists",
            AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections);
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
            "MainViewModel runtime controllers use dependency composition contexts",
            MainViewModelRuntimeControllers_UseDependencyCompositionContexts);
        await AddCheckAsync(results,
            "Audio ramp trace exposes control and render-side envelope telemetry",
            AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry);
        await AddCheckAsync(results,
            "Source telemetry presentation builder preserves summary and target text",
            SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText);
        await AddCheckAsync(results,
            "Source telemetry presentation builder lives in focused helper",
            SourceTelemetryPresentationBuilder_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Live signal text projection preserves pixel format fallback order",
            LiveSignalTextProjection_PreservesPixelFormatFallbackOrder);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsViewModelAndFlashbackUiChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Automation preview volume persists through the settings path",
            AutomationPreviewVolume_PersistsThroughSettingsPath);
        await AddCheckAsync(results,
            "Automation audio commands preserve runtime guards",
            AutomationAudioCommands_PreserveRuntimeGuards);
        await AddCheckAsync(results,
            "Automation UI settings persist through the settings path",
            AutomationUiSettings_PersistThroughSettingsPath);
        await AddCheckAsync(results,
            "Settings persistence projection load plan preserves saved semantics",
            SettingsPersistenceProjection_LoadPlanPreservesSavedSemantics);
        await AddCheckAsync(results,
            "Settings persistence projection save settings maps persisted values",
            SettingsPersistenceProjection_SaveSettingsMapsPersistedValues);
        await AddCheckAsync(results,
            "Automation device selection routes through apply reinit",
            AutomationDeviceSelection_RoutesThroughApplyReinit);
        await AddCheckAsync(results,
            "Automation capture settings route through controller and await reinitialization",
            AutomationCaptureModeChanges_AwaitReinitialization);
        await AddCheckAsync(results,
            "Automation recording transitions use shared lifecycle gate",
            MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate);
        await AddCheckAsync(results,
            "Automation flashback and probe commands use async view-model surface",
            MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface);
        await AddCheckAsync(results,
            "Automation view-model runtime snapshot lives in focused partial",
            MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Main window flashback scrub ends on release cancel and capture lost",
            MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost);
        await AddCheckAsync(results,
            "Flashback timeline geometry preserves scrub math",
            FlashbackTimelineGeometry_PreservesScrubMath);
        await AddCheckAsync(results,
            "Main window flashback toggle rolls back UI state on failure",
            MainWindowFlashbackToggle_RollsBackUiStateOnFailure);
        await AddCheckAsync(results,
            "Flashback polling timers live in controller",
            FlashbackPollingTimers_LiveInController);
        await AddCheckAsync(results,
            "Flashback timeline track layout lives in controller",
            FlashbackTimelineTrackLayout_LivesInController);
        await AddCheckAsync(results,
            "Flashback playhead motion lives in controller",
            FlashbackPlayheadMotion_LivesInController);
        await AddCheckAsync(results,
            "Flashback marker presentation lives in controller",
            FlashbackMarkerPresentation_LivesInController);
        await AddCheckAsync(results,
            "Flashback playback presentation lives in controller",
            FlashbackPlaybackPresentation_LivesInController);
        await AddCheckAsync(results,
            "Flashback export progress presentation lives in controller",
            FlashbackExportProgressPresentation_LivesInController);
        await AddCheckAsync(results,
            "Flashback settings bindings live in controller",
            FlashbackSettingsBindings_LiveInController);
    }
}

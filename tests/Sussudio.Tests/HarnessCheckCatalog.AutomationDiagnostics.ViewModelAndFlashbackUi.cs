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
            "Automation UI settings persist through the settings path",
            AutomationUiSettings_PersistThroughSettingsPath);
        await AddCheckAsync(results,
            "Automation device selection routes through apply reinit",
            AutomationDeviceSelection_RoutesThroughApplyReinit);
        await AddCheckAsync(results,
            "Automation capture mode changes await reinitialization",
            AutomationCaptureModeChanges_AwaitReinitialization);
        await AddCheckAsync(results,
            "Automation recording transitions use shared lifecycle gate",
            MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate);
        await AddCheckAsync(results,
            "Automation flashback and probe commands use async view-model surface",
            MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface);
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
            "Flashback playhead motion lives in focused partial",
            FlashbackPlayheadMotion_LivesInFocusedPartial);
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

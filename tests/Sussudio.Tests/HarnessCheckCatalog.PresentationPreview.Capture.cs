using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewCaptureChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Show all capture options unlocks source-filtered frame rates",
            ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates);
        await AddCheckAsync(results,
            "Frame-rate source filter policy lives in focused helper",
            FrameRateSourceFilterPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Frame-rate auto selection policy lives in focused helper",
            FrameRateAutoSelectionPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Frame-rate auto selection policy preserves behavior",
            FrameRateAutoSelectionPolicy_PreservesSelectionBehavior);
        await AddCheckAsync(results,
            "Resolution selection policy lives in focused partial",
            ResolutionSelectionPolicy_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture resolution selection policy preserves HDR source retarget behavior",
            CaptureResolutionSelectionPolicy_PreservesHdrSourceRetargetBehavior);
        await AddCheckAsync(results,
            "Capture resolution selection policy preserves SDR auto bucket preference",
            CaptureResolutionSelectionPolicy_PreservesSdrAutoBucketPreference);
        await AddCheckAsync(results,
            "Auto capture selection policy preserves source-bounded selection",
            AutoCaptureSelectionPolicy_PreservesSourceBoundedSelection);
        await AddCheckAsync(results,
            "Device format probe retarget policy lives in focused helper",
            DeviceFormatProbeRetargetPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Device format probe retarget policy preserves retarget decision behavior",
            DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior);
        await AddCheckAsync(results,
            "Device format probe retarget application lives in focused partial",
            DeviceFormatProbeRetargetApplication_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Mode selection state lives in focused partial",
            ModeSelectionState_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Frame-rate timing policy lives in focused partial",
            FrameRateTimingPolicy_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Frame-rate timing policy preserves pure timing behavior",
            FrameRateTimingPolicy_PreservesPureTimingBehavior);
        await AddCheckAsync(results,
            "Capture format selection policy lives in focused helper",
            CaptureFormatSelectionPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Capture format selection policy preserves behavior",
            CaptureFormatSelectionPolicy_PreservesSelectionBehavior);
        await AddCheckAsync(results,
            "Recording settings selection policy lives in focused helper",
            RecordingSettingsSelectionPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Diagnostics loop does not rebuild automation options each poll",
            DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll);
        await AddCheckAsync(results,
            "Preview startup session and reinit ownership lives in focused controllers",
            PreviewStartupSessionReinitOwnership_LivesInFocusedControllers);
        await AddCheckAsync(results,
            "Preview startup watchdog ownership lives in focused controller",
            PreviewStartupWatchdogOwnership_LivesInFocusedController);
        await AddCheckAsync(results,
            "Preview startup signal ownership lives in focused controllers",
            PreviewStartupSignalsOwnership_LivesInFocusedControllers);
        await AddCheckAsync(results,
            "Preview startup lifecycle event ownership lives in focused controller",
            PreviewStartupLifecycleEventOwnership_LivesInFocusedController);
        await AddCheckAsync(results,
            "Preview startup watchdog controller preserves timeout contracts",
            PreviewStartupWatchdogController_PreservesTimeoutContracts);
        await AddCheckAsync(results,
            "Preview startup watchdog controller gates failure-stop scheduling",
            PreviewStartupWatchdogController_GatesFailureStopScheduling);
        await AddCheckAsync(results,
            "Preview startup session controller preserves attempt state contracts",
            PreviewStartupSessionController_PreservesAttemptStateContracts);
        await AddCheckAsync(results,
            "Preview reinit transition controller preserves transition state contracts",
            PreviewReinitTransitionController_PreservesTransitionStateContracts);
        await AddCheckAsync(results,
            "Preview reinitialization waits for pending Flashback cycles",
            PreviewReinitialization_WaitsForPendingFlashbackCycle);
        await AddCheckAsync(results,
            "Preview startup signal formatter preserves string contracts",
            PreviewStartupSignalFormatter_PreservesSignalStrings);
        await AddCheckAsync(results,
            "Preview startup readiness signal controller preserves state contracts",
            PreviewStartupReadinessSignalController_PreservesSignalStateContracts);
        await AddCheckAsync(results,
            "Preview startup failure text formatter preserves string contracts",
            PreviewStartupFailureTextFormatter_PreservesFailureStrings);
        await AddCheckAsync(results,
            "Preview startup tolerates missing audio capture devices",
            PreviewStartup_ToleratesMissingAudioCaptureDevices);
        await AddCheckAsync(results,
            "Capture service preview lifecycle lives in focused partials",
            CaptureService_PreviewLifecycleLivesInFocusedPartials);
        await AddCheckAsync(results,
            "Preview startup begins device discovery before recording capability probes finish",
            PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish);
        await AddCheckAsync(results,
            "Preview startup primes UI and audio before preview reveal",
            PreviewStartup_PrimesUiAndAudioBeforePreviewReveal);
        await AddCheckAsync(results,
            "Preview stop ramps audio down before preview teardown",
            PreviewStop_RampsAudioDownBeforePreviewTeardown);
        await AddCheckAsync(results,
            "Audio preview stays inactive when no audio capture device exists",
            AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists);
        await AddCheckAsync(results,
            "Audio monitoring visuals follow runtime preview activity",
            AudioMonitoringVisuals_FollowRuntimePreviewActivity);
        await AddCheckAsync(results,
            "Preview backend log reflects video-only fallback",
            PreviewBackendLog_ReflectsVideoOnlyFallback);
    }
}

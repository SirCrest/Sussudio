using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewCaptureChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "External FFmpeg and HDR probes use bounded process supervision",
            ExternalProcessProbes_UseBoundedProcessSupervisor);
        await AddCheckAsync(results,
            "Recording stop propagates unified video stop failures",
            RecordingStop_PropagatesUnifiedVideoStopFailure);
        await AddCheckAsync(results,
            "Preview stop compatibility overloads are preserved",
            PreviewStopCompatibilityOverloads_ArePreserved);
        await AddCheckAsync(results,
            "Preview stop API surface has no default-literal ambiguity",
            PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity);
        await AddCheckAsync(results,
            "Emergency recording stop does not dispatch to blocked UI thread",
            EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread);
        await AddCheckAsync(results,
            "Flashback buffer manager cleans stale session directories",
            FlashbackBufferManager_CleansStaleSessionDirectories);
        await AddCheckAsync(results,
            "Flashback buffer manager preserves marked recovery sessions",
            FlashbackBufferManager_PreservesMarkedRecoverySessions);
        await AddCheckAsync(results,
            "Project file preserves main's English-only publish locale policy",
            ProjectFile_PreservesEnglishOnlyPublishLocalePolicy);
        await AddCheckAsync(results,
            "Show all capture options unlocks source-filtered frame rates",
            ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates);
        await AddCheckAsync(results,
            "Frame-rate source filter policy lives in focused helper",
            FrameRateSourceFilterPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Resolution selection policy lives in focused partial",
            ResolutionSelectionPolicy_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Frame-rate timing policy lives in focused partial",
            FrameRateTimingPolicy_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Recording format selection policy lives in focused helper",
            RecordingFormatSelectionPolicy_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Diagnostics loop does not rebuild automation options each poll",
            DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll);
        await AddCheckAsync(results,
            "Preview startup state lives in preview startup partial",
            PreviewStartup_StateLivesInPreviewStartupPartial);
        await AddCheckAsync(results,
            "Preview startup signal formatter preserves string contracts",
            PreviewStartupSignalFormatter_PreservesSignalStrings);
        await AddCheckAsync(results,
            "Preview startup tolerates missing audio capture devices",
            PreviewStartup_ToleratesMissingAudioCaptureDevices);
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

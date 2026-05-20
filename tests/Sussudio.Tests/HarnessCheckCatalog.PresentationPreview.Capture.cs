using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewCaptureChecksAsync(List<CheckResult> results)
    {
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

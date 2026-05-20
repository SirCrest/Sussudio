using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewCaptureChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Preview startup begins device discovery before recording capability probes finish",
            PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish);
        await AddCheckAsync(results,
            "Preview startup primes UI and audio before preview reveal",
            PreviewStartup_PrimesUiAndAudioBeforePreviewReveal);
        await AddCheckAsync(results,
            "Preview stop ramps audio down before preview teardown",
            PreviewStop_RampsAudioDownBeforePreviewTeardown);
    }
}

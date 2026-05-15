using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelHdrOutputPolicyChecksAsync(List<CheckResult> results)
    {
        // --- HdrOutputPolicy ---
        await AddCheckAsync(results,
            "HdrOutputPolicy returns true when HDR and Hdr10Pq requested",
            HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested);
        await AddCheckAsync(results,
            "HdrOutputPolicy returns false when HDR disabled",
            HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled);
        await AddCheckAsync(results,
            "HdrOutputPolicy returns false for non-Hdr10Pq mode",
            HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq);
        await AddCheckAsync(results,
            "HdrOutputPolicy force-off env disables HDR output",
            HdrOutputPolicy_ReturnsFalse_WhenForceOffEnvSet);
        await AddCheckAsync(results,
            "HdrOutputPolicy ignores removed legacy enabled env switch",
            HdrOutputPolicy_IgnoresLegacyEnabledEnvSwitch);
    }
}

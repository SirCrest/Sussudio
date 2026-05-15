using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelDeviceModelChecksAsync(List<CheckResult> results)
    {
        // --- Device Models ---
        await AddCheckAsync(results,
            "AudioInputDevice display name falls back to unknown",
            AudioInputDevice_DisplayName_UsesNameOrUnknownFallback);
        await AddCheckAsync(results,
            "AudioLevelEventArgs exposes peak RMS and clipped state",
            AudioLevelEventArgs_ExposesPeakRmsAndClippedState);
        await AddCheckAsync(results,
            "CaptureDevice preserves display and metadata defaults",
            CaptureDevice_DisplayNameAndDefaults_PreserveDeviceMetadata);
        await AddCheckAsync(results,
            "CaptureDiagnosticsSnapshot preserves diagnostics telemetry contract",
            CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry);
        await AddCheckAsync(results,
            "CaptureHealthSnapshot extends diagnostics with health telemetry",
            CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync);
    }
}

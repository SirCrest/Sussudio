using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelSourceSignalTelemetryChecksAsync(List<CheckResult> results)
    {
        // --- SourceSignalTelemetrySnapshot ---
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot defaults have expected values",
            SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues);
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot properties round-trip",
            SourceSignalTelemetrySnapshot_PropertiesRoundTrip);
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot preserves full telemetry contract",
            SourceSignalTelemetrySnapshot_PreservesFullTelemetryContract);
    }
}

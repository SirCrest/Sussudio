using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static class DiagnosticSessionPostRunSnapshots
{
    internal static async Task<DiagnosticSessionPostRunSnapshotResult> CaptureAsync(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        JsonElement? timeline = null;
        try
        {
            setStage("timeline");
            var timelineResponse = await sendAsync(
                    "GetPerformanceTimeline",
                    new Dictionary<string, object?> { ["maxEntries"] = 240 },
                    null)
                .ConfigureAwait(false);
            if (timelineResponse.TryGetProperty("Data", out var timelineData))
            {
                timeline = timelineData.Clone();
            }
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "timeline");
        }

        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthSnapshot = lastSnapshot;
        try
        {
            setStage("final-snapshot");
            var finalSnapshotResponse = await sendAsync("GetSnapshot", null, null).ConfigureAwait(false);
            healthSnapshot = TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)
                ? finalSnapshot
                : lastSnapshot;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "final-snapshot");
        }

        return new DiagnosticSessionPostRunSnapshotResult(healthSnapshot, timeline);
    }
}

internal readonly record struct DiagnosticSessionPostRunSnapshotResult(
    JsonElement HealthSnapshot,
    JsonElement? Timeline);

using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackWaits
{
    internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long baselineFrameCount,
        double minimumSeconds,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
                var sessionFrameCount = frameCount >= baselineFrameCount
                    ? frameCount - baselineFrameCount
                    : frameCount;
                var targetFps = GetDouble(snapshot, "FlashbackPlaybackTargetFps");
                if (targetFps <= 0)
                {
                    targetFps = GetDouble(snapshot, "SelectedExactFrameRate");
                }

                var minimumFrames = Math.Max(
                    240,
                    targetFps > 0
                        ? (long)Math.Ceiling(targetFps * minimumSeconds)
                        : 240);
                if (sessionFrameCount >= minimumFrames &&
                    string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }
}

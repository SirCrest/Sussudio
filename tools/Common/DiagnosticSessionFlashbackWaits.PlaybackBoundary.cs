using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackWaits
{
    internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long boundaryMs,
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
                var positionMs = GetNullableLong(snapshot, "FlashbackPlaybackPositionMs") ?? 0;
                var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
                var pending = GetInt(snapshot, "FlashbackPlaybackPendingCommands");
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                if (positionMs >= boundaryMs + 1_500 &&
                    frameCount >= 180 &&
                    pending == 0 &&
                    string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }
}

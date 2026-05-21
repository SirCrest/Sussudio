using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackWaits
{
    internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        string expectedState,
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
                if (string.Equals(state, expectedState, StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }
}

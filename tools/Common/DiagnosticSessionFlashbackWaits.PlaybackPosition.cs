using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackWaits
{
    internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        int targetPositionMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                var position = GetInt(snapshot, "FlashbackPlaybackPositionMs");
                if (Math.Abs(position - targetPositionMs) <= 1_500)
                {
                    return true;
                }
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

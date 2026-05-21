using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegments
{
    internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long boundaryMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        const int requiredHeadroomMs = 8_000;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                var bufferedDurationMs = GetNullableLong(snapshot, "FlashbackBufferedDurationMs") ?? 0;
                if (bufferedDurationMs >= boundaryMs + requiredHeadroomMs)
                {
                    return true;
                }
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

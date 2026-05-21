using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackWaits
{
    internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        int requiredBufferedDurationMs = 8_000,
        long requiredEncodedFrames = 240,
        TimeSpan? timeout = null)
    {
        var started = Stopwatch.GetTimestamp();
        var waitTimeout = timeout ?? TimeSpan.FromSeconds(30);
        while (Stopwatch.GetElapsedTime(started) < waitTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") &&
                GetInt(snapshot, "FlashbackBufferedDurationMs") >= requiredBufferedDurationMs &&
                (GetNullableLong(snapshot, "FlashbackEncodedFrames") ?? 0) >= requiredEncodedFrames)
            {
                return true;
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

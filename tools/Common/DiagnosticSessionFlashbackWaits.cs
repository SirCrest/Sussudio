using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackWaits
{
    internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        bool expectedActive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") == expectedActive)
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<JsonElement?> WaitForPreviewActiveAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        bool expectedActive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "IsPreviewing") == expectedActive)
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "IsRecording") &&
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase) &&
                GetBool(snapshot, "RecordingFileGrowing"))
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

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

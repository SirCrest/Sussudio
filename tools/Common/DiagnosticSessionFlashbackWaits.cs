using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackWaits
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

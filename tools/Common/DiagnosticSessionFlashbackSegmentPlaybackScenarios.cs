using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios
{
    internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackSegmentPlayback)
        {
            return;
        }

        backgroundTasks.AddScenario(
            7,
            "flashback-segment-playback-task",
            RunFlashbackSegmentPlaybackAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken));
        actions.Add("flashback segment playback started");
    }

    internal static async Task RunFlashbackSegmentPlaybackAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback segment playback: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        var playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is null)
        {
            var rotationOk = await CreateFlashbackCompletedSegmentViaRecordingAsync(
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!rotationOk)
            {
                return;
            }

            playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                    sendCommandAsync,
                    TimeSpan.FromSeconds(20),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (playbackTarget is null)
        {
            warnings.Add("flashback segment playback: no playable completed segment became available after recording-assisted rotation");
            return;
        }

        var target = playbackTarget.Value;
        var completedSegment = target.Segment;
        actions.Add(
            "flashback segment playback live headroom established " +
            $"validStartMs={target.ValidStartPtsMs} boundaryPosMs={target.BoundaryPositionMs} " +
            $"bufferedMs={target.BufferedDurationMs}");

        var seekPositionMs = Math.Max(0, target.BoundaryPositionMs - 500);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = seekPositionMs },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play" },
                null)
            .ConfigureAwait(false);
        actions.Add(
            "flashback segment playback started near boundary " +
            $"segment={completedSegment.SequenceNumber} seekMs={seekPositionMs} " +
            $"boundaryPosMs={target.BoundaryPositionMs} endMs={completedSegment.EndPtsMs}");

        var playbackSnapshot = await WaitForFlashbackPlaybackBoundaryCrossAsync(
                sendCommandAsync,
                target.BoundaryPositionMs,
                TimeSpan.FromSeconds(35),
                cancellationToken)
            .ConfigureAwait(false);
        if (playbackSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback segment playback: no playback snapshot returned");
            return;
        }

        ValidateFlashbackSegmentPlaybackSnapshot(
            actions,
            warnings,
            playbackSnapshot.Value,
            baselineSnapshot,
            target);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback go-live requested");

        var finalSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Live",
                TimeSpan.FromSeconds(3),
                cancellationToken)
            .ConfigureAwait(false);
        if (finalSnapshot?.ValueKind == JsonValueKind.Object)
        {
            var finalState = GetString(finalSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
            if (!string.Equals(finalState, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback segment playback: playback ended in state {finalState}");
            }
        }
    }

}

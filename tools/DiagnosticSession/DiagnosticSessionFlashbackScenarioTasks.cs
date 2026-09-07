using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(
    string? OriginalPreset,
    string? DeferredPreset);

internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios
{
    internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var recordingReadySnapshot = await WaitForFlashbackRecordingReadyAsync(
                (command, payload, timeoutMs) => sendCommandAsync(command, payload, timeoutMs, false),
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (recordingReadySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording settings deferred: Flashback recording backend did not become ready");
            return default;
        }

        var originalPreset = GetString(recordingReadySnapshot.Value, "SelectedPreset") ?? "P1";
        var cycledPreset = string.Equals(originalPreset, "P1", StringComparison.OrdinalIgnoreCase) ? "P2" : "P1";
        var presetState = new FlashbackRecordingSettingsDeferredPresetState(originalPreset, cycledPreset);
        var originalFilePath = GetString(recordingReadySnapshot.Value, "FlashbackFilePath") ?? string.Empty;
        var submittedBefore = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsBefore = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoEncoderPacketsWritten") ?? 0;

        var presetResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = cycledPreset },
                null,
                false)
            .ConfigureAwait(false);
        actions.Add($"flashback recording settings deferred preset changed to {cycledPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(presetResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: preset change failed - {AutomationSnapshotFormatter.Get(presetResponse, "Message", "unknown error")}");
            return presetState;
        }

        await VerifyFlashbackRestartRejectedDuringRecordingAsync(
                actions,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);

        await VerifyFlashbackDisableRejectedDuringRecordingAsync(
                actions,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);

        await VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(
                warnings,
                originalFilePath,
                submittedBefore,
                packetsBefore,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return presetState;
    }

    internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(
        List<string> actions,
        List<string> warnings,
        FlashbackRecordingSettingsDeferredPresetState presetState,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presetState.DeferredPreset))
        {
            warnings.Add("flashback recording settings deferred: no expected preset was captured for post-stop verification");
            return;
        }

        var temporarilyEnabledFlashback = await TryEnableFlashbackForPostStopSettingsVerificationAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
            {
                warnings.Add("flashback recording settings deferred: Flashback buffer did not become ready after recording stop");
                return;
            }

            var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (!TryGetSnapshot(snapshotResponse, out var snapshot))
            {
                warnings.Add("flashback recording settings deferred: no post-stop snapshot returned");
                return;
            }

            if (!string.Equals(GetString(snapshot, "SelectedPreset"), presetState.DeferredPreset, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(
                    "flashback recording settings deferred: selected preset was not preserved after stop " +
                    $"expected={presetState.DeferredPreset} actual={GetString(snapshot, "SelectedPreset") ?? "<null>"}");
            }

            if (!GetBool(snapshot, "FlashbackActive"))
            {
                warnings.Add("flashback recording settings deferred: Flashback inactive after recording stop");
                return;
            }

            if (GetNullableLong(snapshot, "FlashbackEncodedFrames") is not > 0)
            {
                warnings.Add("flashback recording settings deferred: post-stop Flashback encoder did not produce frames");
            }

            actions.Add("flashback recording settings deferred post-stop buffer verified");

            await RestoreFlashbackRecordingSettingsOriginalPresetAsync(
                    actions,
                    warnings,
                    presetState,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (temporarilyEnabledFlashback)
            {
                await RestoreTemporarilyEnabledFlashbackAsync(actions, warnings, sendCommandAsync).ConfigureAwait(false);
            }
        }
    }

    private static async Task<bool> TryEnableFlashbackForPostStopSettingsVerificationAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot) ||
            GetBool(snapshot, "FlashbackActive"))
        {
            return false;
        }

        var enableResponse = await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                305_000)
            .ConfigureAwait(false);
        actions.Add("flashback recording settings deferred post-stop flashback re-enabled");
        if (!AutomationSnapshotFormatter.IsSuccess(enableResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: post-stop Flashback re-enable failed - {AutomationSnapshotFormatter.Get(enableResponse, "Message", "unknown error")}");
            return false;
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static async Task RestoreTemporarilyEnabledFlashbackAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var disableResponse = await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                305_000)
            .ConfigureAwait(false);
        actions.Add("flashback recording settings deferred post-stop flashback restored off");
        if (!AutomationSnapshotFormatter.IsSuccess(disableResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: post-stop Flashback restore-off failed - {AutomationSnapshotFormatter.Get(disableResponse, "Message", "unknown error")}");
        }
    }

    private static async Task VerifyFlashbackRestartRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        await VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
                actions,
                warnings,
                "RestartFlashback",
                null,
                null,
                "flashback recording settings deferred restart rejection requested",
                "flashback recording settings deferred: RestartFlashback unexpectedly succeeded during recording",
                "flashback recording settings deferred: restart rejection message did not mention recording",
                sendCommandAsync)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackDisableRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        await VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
                actions,
                warnings,
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                305_000,
                "flashback recording settings deferred disable rejection requested",
                "flashback recording settings deferred: SetFlashbackEnabled(false) unexpectedly succeeded during recording",
                "flashback recording settings deferred: disable rejection message did not mention recording",
                sendCommandAsync)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        string commandName,
        Dictionary<string, object?>? payload,
        int? timeoutMs,
        string requestedAction,
        string unexpectedSuccessWarning,
        string messageWarningPrefix,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        var response = await sendCommandAsync(
                commandName,
                payload,
                timeoutMs,
                true)
            .ConfigureAwait(false);
        actions.Add(requestedAction);

        if (AutomationSnapshotFormatter.IsSuccess(response))
        {
            warnings.Add(unexpectedSuccessWarning);
            return;
        }

        var message = AutomationSnapshotFormatter.Get(response, "Message", string.Empty);
        if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{messageWarningPrefix} - {message}");
        }
    }

    private static async Task VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(
        List<string> warnings,
        string originalFilePath,
        long submittedBefore,
        long packetsBefore,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var afterResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(afterResponse, out var afterSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-mutation recording snapshot returned");
            return;
        }

        if (!GetBool(afterSnapshot, "IsRecording") ||
            !string.Equals(GetString(afterSnapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording settings deferred: Flashback recording backend did not remain active after mutations");
        }

        var afterFilePath = GetString(afterSnapshot, "FlashbackFilePath") ?? string.Empty;
        if (!string.Equals(afterFilePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording settings deferred: Flashback file path changed during recording settings deferral");
        }

        var submittedAfter = GetNullableLong(afterSnapshot, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsAfter = GetNullableLong(afterSnapshot, "FlashbackVideoEncoderPacketsWritten") ?? 0;
        if (submittedAfter <= submittedBefore || packetsAfter <= packetsBefore)
        {
            warnings.Add(
                "flashback recording settings deferred: recording counters did not advance after mutation attempts " +
                $"submitted={submittedBefore}->{submittedAfter} packets={packetsBefore}->{packetsAfter}");
        }
    }

    private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(
        List<string> actions,
        List<string> warnings,
        FlashbackRecordingSettingsDeferredPresetState presetState,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presetState.OriginalPreset) ||
            string.Equals(presetState.OriginalPreset, presetState.DeferredPreset, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var restoreResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = presetState.OriginalPreset },
                null)
            .ConfigureAwait(false);
        actions.Add($"flashback recording settings deferred preset restored to {presetState.OriginalPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(restoreResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: preset restore failed - {AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error")}");
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording settings deferred: Flashback buffer did not become ready after preset restore");
            return;
        }

        var restoredSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(restoredSnapshotResponse, out var restoredSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-restore snapshot returned");
            return;
        }

        if (!string.Equals(GetString(restoredSnapshot, "SelectedPreset"), presetState.OriginalPreset, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "flashback recording settings deferred: selected preset was not restored " +
                $"expected={presetState.OriginalPreset} actual={GetString(restoredSnapshot, "SelectedPreset") ?? "<null>"}");
        }
    }
}

internal static class DiagnosticSessionFlashbackSegmentPlaybackScenarios
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

        var playbackTarget = await AcquireFlashbackSegmentPlaybackTargetAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is null)
        {
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

        await ReturnFlashbackSegmentPlaybackLiveAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is not null)
        {
            return playbackTarget;
        }

        var rotationOk = await CreateFlashbackCompletedSegmentViaRecordingAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
        if (!rotationOk)
        {
            return null;
        }

        playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is null)
        {
            warnings.Add("flashback segment playback: no playable completed segment became available after recording-assisted rotation");
        }

        return playbackTarget;
    }

    private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
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

    private static void ValidateFlashbackSegmentPlaybackSnapshot(
        List<string> actions,
        List<string> warnings,
        JsonElement playbackSnapshot,
        JsonElement baselineSnapshot,
        FlashbackSegmentPlaybackTarget target)
    {
        var completedSegment = target.Segment;
        var state = GetString(playbackSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        var positionMs = GetNullableLong(playbackSnapshot, "FlashbackPlaybackPositionMs") ?? 0;
        var frameCount = GetNullableLong(playbackSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var observedFps = GetDouble(playbackSnapshot, "FlashbackPlaybackObservedFps");
        var lateFrames = GetNullableLong(playbackSnapshot, "FlashbackPlaybackLateFrames") ?? 0;
        var commandHealth = BuildPlaybackCommandHealth(playbackSnapshot, baselineSnapshot);
        var pending = GetInt(playbackSnapshot, "FlashbackPlaybackPendingCommands");
        actions.Add(
            "flashback segment playback observed " +
            $"positionMs={positionMs} frames={frameCount} late={lateFrames} fps={observedFps:0.##}");

        if (!string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback segment playback: expected Playing after boundary playback, got {state}");
        }

        if (positionMs < target.BoundaryPositionMs + 250)
        {
            warnings.Add(
                "flashback segment playback: playback position did not cross completed segment boundary " +
                $"positionMs={positionMs} boundaryMs={target.BoundaryPositionMs} " +
                $"absoluteBoundaryMs={completedSegment.EndPtsMs} validStartMs={target.ValidStartPtsMs}");
        }

        var targetFps = GetDouble(playbackSnapshot, "DetectedSourceFrameRate");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(playbackSnapshot, "SelectedFrameRate");
        }

        if (frameCount <= 0)
        {
            warnings.Add(
                "flashback segment playback: playback frames did not advance " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }
        else if (frameCount >= 120 && observedFps <= 1)
        {
            warnings.Add(
                "flashback segment playback: playback FPS did not warm after enough frames " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }
        else if (targetFps >= 100 && frameCount >= 180 && observedFps < targetFps * 0.85)
        {
            warnings.Add(
                "flashback segment playback: playback FPS below source-rate target after warm sample " +
                $"frames={frameCount} observedFps={observedFps:0.##} targetFps={targetFps:0.##}");
        }

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0 || pending > 0)
        {
            warnings.Add(
                "flashback segment playback: command queue unhealthy " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures} pending={pending}");
        }
    }

    private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var startResponse = await sendCommandAsync(
                "SetRecordingEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback recording-assisted rotation started");
        if (!AutomationSnapshotFormatter.IsSuccess(startResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted start failed - {AutomationSnapshotFormatter.Get(startResponse, "Message", "unknown error")}");
            return false;
        }

        var readySnapshot = await WaitForFlashbackRecordingReadyAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (readySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback segment playback: recording-assisted Flashback backend did not become ready");
            await TryStopRecordingAsync(sendCommandAsync).ConfigureAwait(false);
            return false;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);

        var stopResponse = await sendCommandAsync(
                "SetRecordingEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback recording-assisted rotation stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted stop failed - {AutomationSnapshotFormatter.Get(stopResponse, "Message", "unknown error")}");
            return false;
        }

        var stoppedResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "RecordingStopped",
                    ["timeoutMs"] = 30_000,
                    ["pollMs"] = 250
                },
                32_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(stoppedResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted stop did not settle - {AutomationSnapshotFormatter.Get(stoppedResponse, "Message", "not met")}");
            return false;
        }

        return true;
    }

    private static async Task TryStopRecordingAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        try
        {
            await sendCommandAsync(
                    "SetRecordingEnabled",
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup for diagnostics; the caller records the primary warning.
        }
    }
}

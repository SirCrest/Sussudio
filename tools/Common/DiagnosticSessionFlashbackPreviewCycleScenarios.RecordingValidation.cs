using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    private readonly record struct RecordingPreviewCycleCounters(
        long SubmittedBeforeStop,
        long PacketsBeforeStop);

    private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var recordingReadySnapshot = await WaitForFlashbackRecordingReadyAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (recordingReadySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording preview cycle: Flashback recording backend did not become ready");
            return null;
        }

        return new RecordingPreviewCycleCounters(
            SubmittedBeforeStop: GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoFramesSubmittedToEncoder") ?? 0,
            PacketsBeforeStop: GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoEncoderPacketsWritten") ?? 0);
    }

    private static async Task<bool> ValidateRecordingPreviewCycleStoppedAsync(
        RecordingPreviewCycleCounters countersBeforeStop,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var previewStoppedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStoppedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording preview cycle: preview did not report stopped");
            return false;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "IsRecording") ||
            !string.Equals(GetString(previewStoppedSnapshot.Value, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording preview cycle: Flashback recording backend stopped with preview");
            return false;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var previewOffSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(previewOffSnapshotResponse, out var previewOffSnapshot))
        {
            warnings.Add("flashback recording preview cycle: no preview-off recording snapshot returned");
            return false;
        }

        var submittedPreviewOff = GetNullableLong(previewOffSnapshot, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsPreviewOff = GetNullableLong(previewOffSnapshot, "FlashbackVideoEncoderPacketsWritten") ?? 0;
        if (!GetBool(previewOffSnapshot, "IsRecording") ||
            !string.Equals(GetString(previewOffSnapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording preview cycle: recording inactive while preview was off");
        }

        if (submittedPreviewOff <= countersBeforeStop.SubmittedBeforeStop ||
            packetsPreviewOff <= countersBeforeStop.PacketsBeforeStop)
        {
            warnings.Add(
                "flashback recording preview cycle: recording counters did not advance while preview was off " +
                $"submitted={countersBeforeStop.SubmittedBeforeStop}->{submittedPreviewOff} packets={countersBeforeStop.PacketsBeforeStop}->{packetsPreviewOff}");
        }

        return true;
    }

    private static async Task ValidateRecordingPreviewCycleRestartedAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var previewStartedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStartedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording preview cycle: preview did not report active after restart");
            return;
        }

        if (!GetBool(previewStartedSnapshot.Value, "IsRecording") ||
            !string.Equals(GetString(previewStartedSnapshot.Value, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording preview cycle: Flashback recording backend inactive after preview restart");
        }

        var framesFlowingResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "VideoFramesFlowing",
                    ["timeoutMs"] = 15_000,
                    ["pollMs"] = 250
                },
                17_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(framesFlowingResponse))
        {
            warnings.Add(
                $"flashback recording preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }
}

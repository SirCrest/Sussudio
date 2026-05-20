using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    private static async Task ValidatePlaybackPreviewCycleRestartedAsync(
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
            warnings.Add("flashback playback preview cycle: preview did not report active after restart");
            return;
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
                $"flashback playback preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }
}

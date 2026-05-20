using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var positions = new[]
        {
            250, 500, 750, 1_000, 1_250, 1_500, 1_750, 2_000,
            2_250, 2_500, 2_750, 3_000, 2_400, 1_800, 1_200, 600
        };
        var updateTasks = new Task<JsonElement>[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            updateTasks[i] = sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "update-scrub", ["positionMs"] = positions[i] },
                null);
        }

        var updateResponses = await Task.WhenAll(updateTasks).ConfigureAwait(false);
        actions.Add("flashback scrub stress update burst requested");
        var failedUpdates = 0;
        foreach (var response in updateResponses)
        {
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                failedUpdates++;
            }
        }

        if (failedUpdates > 0)
        {
            warnings.Add($"flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        }

        return positions[^1];
    }
}

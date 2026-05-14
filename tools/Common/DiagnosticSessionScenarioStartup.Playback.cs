using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioStartup
{
    private static async Task<bool> TryStartFlashbackPlaybackAsync(
        DiagnosticSessionScenarioPlan scenarioPlan,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackPlayback)
        {
            return false;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback playback: Flashback buffer did not become playback-ready within 30s");
        }

        var playResponse = await sendAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                null)
            .ConfigureAwait(false);
        if (!IsSuccess(playResponse))
        {
            warnings.Add($"flashback playback: play command failed - {Get(playResponse, "Message", "unknown error")}");
            return false;
        }

        actions.Add("flashback playback started at 1000ms");
        var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playingSnapshot is null)
        {
            warnings.Add("flashback playback: playback did not report Playing within 5s");
        }

        return true;
    }
}

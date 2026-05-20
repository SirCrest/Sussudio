using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(
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
            warnings.Add("flashback playback preview cycle: preview did not report stopped");
            return false;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback playback preview cycle: Flashback became inactive when preview stopped");
            return false;
        }

        var playbackStateAfterStop = GetString(previewStoppedSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateAfterStop, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback playback preview cycle: playback did not return live after preview stop state={playbackStateAfterStop}");
        }

        return true;
    }
}

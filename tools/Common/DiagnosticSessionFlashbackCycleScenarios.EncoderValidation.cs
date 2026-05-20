using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackCycleScenarios
{
    private static void ValidateFlashbackEncoderCycleSnapshot(
        JsonElement afterSnapshot,
        string originalFilePath,
        List<string> warnings)
    {
        var framesAfter = GetNullableLong(afterSnapshot, "FlashbackEncodedFrames") ?? 0;
        if (framesAfter < 240)
        {
            warnings.Add($"flashback encoder cycle: post-cycle encoder did not reach readiness frame count frames={framesAfter}");
        }

        var afterFilePath = GetString(afterSnapshot, "FlashbackFilePath") ?? string.Empty;
        if (string.Equals(afterFilePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback encoder cycle: Flashback file path did not change after preset cycle");
        }

        if (GetInt(afterSnapshot, "FlashbackPlaybackPendingCommands") > 0 ||
            GetBool(afterSnapshot, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add(
                "flashback encoder cycle: playback state not clean after preset cycle " +
                $"pending={GetInt(afterSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"threadAlive={GetBool(afterSnapshot, "FlashbackPlaybackThreadAlive")}");
        }
    }
}

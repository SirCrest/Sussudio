using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private readonly record struct FlashbackPlaybackSnapshotRelevance(
        long FrameCount,
        long SessionFrameCount,
        bool IsRelevant);

    private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(
        JsonElement snapshot,
        long baselineFrameCount,
        long baselineCommandsEnqueued,
        long baselineCommandsProcessed,
        bool baselinePlaybackActive)
    {
        var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var sessionFrameCount = frameCount >= baselineFrameCount
            ? frameCount - baselineFrameCount
            : frameCount;
        var commandsEnqueued = GetNullableLong(snapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var commandsProcessed = GetNullableLong(snapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        var isRelevant =
            IsPlaybackSnapshotActive(snapshot) ||
            GetInt(snapshot, "FlashbackPlaybackPendingCommands") > 0 ||
            frameCount > baselineFrameCount ||
            commandsEnqueued > baselineCommandsEnqueued ||
            commandsProcessed > baselineCommandsProcessed ||
            baselinePlaybackActive;

        return new FlashbackPlaybackSnapshotRelevance(
            FrameCount: frameCount,
            SessionFrameCount: sessionFrameCount,
            IsRelevant: isRelevant);
    }

    private static bool IsPlaybackSnapshotActive(JsonElement snapshot)
    {
        var state = GetString(snapshot, "FlashbackPlaybackState") ?? string.Empty;
        return GetBool(snapshot, "FlashbackPlaybackThreadAlive") ||
               string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Paused", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Seeking", StringComparison.OrdinalIgnoreCase);
    }
}

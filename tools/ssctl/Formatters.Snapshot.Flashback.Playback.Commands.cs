using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Playback: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackState")} | Pos: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDecoderHwAccel")}");
        builder.AppendLine($"Playback Commands: pending={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPendingCommands")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms maxLatencyCommand={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand")} enq={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} coalescedSeek={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSeekCommandsCoalesced")} threadAlive={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")} failureUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs")}");
    }
}
